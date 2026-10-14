"""Generates the 8-bit soundtrack takes with the Mureka API (docs/audio-8bit-prompts.md, section 7).

Reads the style block and the M1..M21 prompts from the prompt sheet (AUDIO-MUSIC-001), sends "style block + prompt"
to Mureka's instrumental endpoint, waits for the task and downloads every take. The API key comes from
MUREKA_API_KEY or %USERPROFILE%/.mureka/.env (MUREKA_ENV_FILE overrides the path); it is never printed, logged or
written anywhere else. Standard library only.

    python tools/audio/mureka_generate.py list
    python tools/audio/mureka_generate.py show M3
    python tools/audio/mureka_generate.py credit
    python tools/audio/mureka_generate.py generate M1 M2 [--yes] [--again] [--timeout 900]
    python tools/audio/mureka_generate.py generate --all [--yes]
    python tools/audio/mureka_generate.py resume

Every request costs Mureka credit, so "generate" only sends with --yes and skips tracks that already have takes.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SHEET = REPO / 'docs' / 'audio-8bit-prompts.md'
OUT = REPO / 'steam_assets' / 'audio_8bit' / 'mureka'
PENDING = OUT / 'pending.json'
API = os.environ.get('MUREKA_API_URL', 'https://api.mureka.ai')
MODEL = 'auto'  # Mureka picks its latest model
PROMPT_WARN = 1024  # Mureka documents no limit; keep prompts short so no model truncates them
POLL_SECONDS = 5
DONE = 'succeeded'
FAILED = ('failed', 'cancelled', 'timeouted')
TRACK_RE = re.compile(r'^\*\*(M\d+) `([a-z0-9_]+)`\*\* — (.+)$')


@dataclass(frozen=True)
class Track:
    id: str
    file: str
    prompt: str


class ApiError(Exception):
    pass


# --- prompt sheet ------------------------------------------------------------------------------------------------------

def read_sheet(path: Path = SHEET) -> tuple[str, list[Track]]:
    """Style block (section 2) and the music prompts (section 4.2) of the prompt sheet."""
    text = path.read_text(encoding='utf-8-sig')
    section = text.split('## 2. Style block', 1)[1].split('\n## ', 1)[0]
    style = next(line[2:].strip() for line in section.splitlines() if line.startswith('> '))
    tracks = []
    for line in text.splitlines():
        match = TRACK_RE.match(line.strip())
        if match:
            tracks.append(Track(match[1], match[2], match[3].strip()))
    return style, tracks


def full_prompt(style: str, track: Track) -> str:
    return f'{style} {track.prompt}'


def select(tracks: list[Track], names: list[str]) -> list[Track]:
    by_key = {t.id.lower(): t for t in tracks} | {t.file.lower(): t for t in tracks}
    missing = [n for n in names if n.lower() not in by_key]
    if missing:
        sys.exit('Unknown track(s): ' + ', '.join(missing) + ' (see "list")')
    return [by_key[n.lower()] for n in names]


# --- takes on disk -----------------------------------------------------------------------------------------------------

def track_dir(track: Track) -> Path:
    return OUT / track.file


def existing_takes(track: Track) -> list[Path]:
    folder = track_dir(track)
    return sorted(folder.glob(f'{track.file}_take*.*')) if folder.exists() else []


def take_number(path: Path) -> int:
    match = re.search(r'_take(\d+)', path.stem)
    return int(match[1]) if match else 0


def take_downloads(result: dict, base: str, first_take: int) -> list[tuple[str, str]]:
    """(url, file name) for every audio link of every choice: <base>_take<n>.<ext>, one take number per choice."""
    out = []
    for offset, choice in enumerate(result.get('choices') or []):
        number = first_take + offset
        links = [(k, v) for k, v in choice.items() if k.endswith('url') and isinstance(v, str) and v.startswith('http')]
        used = set()
        for field, url in links:
            ext = Path(urllib.parse.urlparse(url).path).suffix.lower() or '.bin'
            name = f'{base}_take{number}{ext}'
            if name in used:
                name = f'{base}_take{number}_{field}{ext}'
            used.add(name)
            out.append((url, name))
    return out


# --- API ---------------------------------------------------------------------------------------------------------------

def load_key() -> str:
    key = os.environ.get('MUREKA_API_KEY', '').strip()
    if key:
        return key
    env = Path(os.environ.get('MUREKA_ENV_FILE') or Path.home() / '.mureka' / '.env')
    if env.exists():
        for line in env.read_text(encoding='utf-8-sig').splitlines():
            if line.startswith('MUREKA_API_KEY='):
                value = line.split('=', 1)[1].strip()
                if value:
                    return value
    sys.exit(f'No Mureka API key: set MUREKA_API_KEY or create {env} (docs/audio-8bit-prompts.md, section 7)')


def api(method: str, path: str, body: dict | None = None) -> dict:
    """One API call. GETs are retried on 429/5xx; a POST only on 429, so a generation is never sent twice."""
    key = load_key()
    data = None if body is None else json.dumps(body).encode('utf-8')
    for attempt in range(5):
        request = urllib.request.Request(API + path, data=data, method=method)
        request.add_header('Authorization', 'Bearer ' + key)
        request.add_header('User-Agent', 'run-of-the-nine-audio/1.0')
        if data is not None:
            request.add_header('Content-Type', 'application/json')
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                raw = response.read()
                return json.loads(raw) if raw else {}
        except urllib.error.HTTPError as e:
            payload = e.read().decode('utf-8', 'replace')[:500]
            retry = e.code == 429 or (method == 'GET' and e.code >= 500)
            if retry and attempt < 4:
                time.sleep(2 * 2 ** attempt)
                continue
            raise ApiError(f'{method} {path} -> HTTP {e.code}: {payload}') from None
        except urllib.error.URLError as e:
            if method == 'GET' and attempt < 4:
                time.sleep(2 * 2 ** attempt)
                continue
            raise ApiError(f'{method} {path} -> {e.reason}') from None
    raise ApiError(f'{method} {path}: gave up after retries')


def download(url: str, dest: Path) -> None:
    tmp = dest.with_suffix(dest.suffix + '.part')
    with urllib.request.urlopen(url, timeout=300) as response, open(tmp, 'wb') as f:
        shutil.copyfileobj(response, f)
    tmp.replace(dest)


def wait(task_id: str, timeout: float) -> dict:
    start, last = time.time(), None
    while True:
        result = api('GET', f'/v1/instrumental/query/{urllib.parse.quote(str(task_id))}')
        status = result.get('status', '?')
        if status != last:
            print(f'    task {task_id}: {status} ({time.time() - start:.0f} s)', flush=True)
            last = status
        if status == DONE or status in FAILED:
            return result
        if time.time() - start > timeout:
            raise ApiError(f'task {task_id} still "{status}" after {timeout:.0f} s; run "resume" later')
        time.sleep(POLL_SECONDS)


def load_pending() -> list[dict]:
    return json.loads(PENDING.read_text(encoding='utf-8')) if PENDING.exists() else []


def save_pending(items: list[dict]) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    PENDING.write_text(json.dumps(items, indent=1), encoding='utf-8')


def finish(track: Track, task_id: str, timeout: float) -> list[Path]:
    """Waits for a submitted task, stores its JSON and downloads the takes."""
    result = wait(task_id, timeout)
    folder = track_dir(track)
    folder.mkdir(parents=True, exist_ok=True)
    (folder / f'task_{task_id}.json').write_text(json.dumps(result, indent=1), encoding='utf-8')
    save_pending([p for p in load_pending() if str(p['task_id']) != str(task_id)])
    if result.get('status') != DONE:
        raise ApiError(f'{track.id} {track.file}: task {task_id} ended "{result.get("status")}" '
                       f'({result.get("failed_reason") or "no reason given"})')
    first = max((take_number(p) for p in existing_takes(track)), default=0) + 1
    saved = []
    for url, name in take_downloads(result, track.file, first):
        dest = folder / name
        download(url, dest)
        saved.append(dest)
        print(f'    saved {dest.relative_to(REPO)} ({dest.stat().st_size // 1024} KB)', flush=True)
    if not saved:
        raise ApiError(f'{track.id}: task {task_id} succeeded but had no audio links (see task_{task_id}.json)')
    return saved


# --- commands ----------------------------------------------------------------------------------------------------------

def cmd_list(style: str, tracks: list[Track]) -> None:
    print(f'{len(tracks)} tracks from {SHEET.relative_to(REPO)}; style block {len(style)} characters')
    for t in tracks:
        length = len(full_prompt(style, t))
        warn = '  (long)' if length > PROMPT_WARN else ''
        print(f'  {t.id:<4} {t.file:<30} prompt {length:>4} chars  takes {len(existing_takes(t))}{warn}')
    pending = load_pending()
    if pending:
        print(f'{len(pending)} task(s) still open; run "resume"')


def cmd_generate(style: str, tracks: list[Track], args: argparse.Namespace) -> None:
    chosen = tracks if args.all else select(tracks, args.tracks)
    if not chosen:
        sys.exit('Name tracks (M1 M2 ... or file names) or use --all')
    todo = [t for t in chosen if args.again or not existing_takes(t)]
    for t in chosen:
        if t not in todo:
            print(f'  skip {t.id} {t.file}: has takes (--again to generate more)')
    if not todo:
        return
    print(f'{len(todo)} request(s) to Mureka (model "{MODEL}"); each returns one or more takes:')
    for t in todo:
        print(f'  {t.id:<4} {t.file:<30} {len(full_prompt(style, t))} chars')
    if not args.yes:
        print('Dry run: nothing sent. Add --yes to generate (uses Mureka credit).')
        return
    failures = []
    for t in todo:
        print(f'{t.id} {t.file}: sending', flush=True)
        try:
            task = api('POST', '/v1/instrumental/generate', {'model': MODEL, 'prompt': full_prompt(style, t)})
            task_id = task.get('id')
            if not task_id:
                raise ApiError(f'no task id in the answer: {json.dumps(task)[:300]}')
            save_pending(load_pending() + [{'task_id': task_id, 'track': t.id, 'file': t.file, 'sent': time.time()}])
            finish(t, task_id, args.timeout)
        except ApiError as e:
            print(f'  FAILED: {e}', flush=True)
            failures.append(t.id)
    if failures:
        sys.exit(f'{len(failures)} track(s) failed: {", ".join(failures)}')


def cmd_resume(tracks: list[Track], timeout: float) -> None:
    pending = load_pending()
    if not pending:
        print('No open tasks.')
        return
    by_id = {t.id: t for t in tracks}
    for item in pending:
        track = by_id.get(item['track'])
        if track is None:
            print(f'  skip task {item["task_id"]}: track {item["track"]} is no longer in the sheet')
            continue
        print(f'{track.id} {track.file}: waiting for task {item["task_id"]}', flush=True)
        try:
            finish(track, item['task_id'], timeout)
        except ApiError as e:
            print(f'  FAILED: {e}', flush=True)


def main(argv: list[str]) -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest='cmd', required=True)
    sub.add_parser('list')
    show = sub.add_parser('show')
    show.add_argument('track')
    sub.add_parser('credit')
    gen = sub.add_parser('generate')
    gen.add_argument('tracks', nargs='*')
    gen.add_argument('--all', action='store_true')
    gen.add_argument('--yes', action='store_true', help='really send (costs credit)')
    gen.add_argument('--again', action='store_true', help='also tracks that already have takes')
    gen.add_argument('--timeout', type=float, default=900)
    res = sub.add_parser('resume')
    res.add_argument('--timeout', type=float, default=900)
    args = parser.parse_args(argv)

    style, tracks = read_sheet()
    try:
        if args.cmd == 'list':
            cmd_list(style, tracks)
        elif args.cmd == 'show':
            print(full_prompt(style, select(tracks, [args.track])[0]))
        elif args.cmd == 'credit':
            print(json.dumps(api('GET', '/v1/account/billing'), indent=1))
        elif args.cmd == 'generate':
            cmd_generate(style, tracks, args)
        elif args.cmd == 'resume':
            cmd_resume(tracks, args.timeout)
    except ApiError as e:
        sys.exit(str(e))


if __name__ == '__main__':
    main(sys.argv[1:])
