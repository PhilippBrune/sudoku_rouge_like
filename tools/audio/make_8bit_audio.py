"""Renders the whole 8-bit set in code: 21 music loops and 35 sound effects (docs/audio-8bit-prompts.md).

    python tools/audio/make_8bit_audio.py                 # everything
    python tools/audio/make_8bit_audio.py music M1 M3     # some tracks
    python tools/audio/make_8bit_audio.py sfx
    python tools/audio/make_8bit_audio.py --roll          # also a piano-roll PNG per track (visual check)

Writes WAV 44.1 kHz 16-bit (music stereo, effects mono) to steam_assets/audio_8bit/synth/<music|sfx|ui>/,
mirroring the game's Resources/audio folders, plus report.json (length, loudness, peak, loop seam per file).
Needs numpy, scipy and soundfile (tools/audio/requirements.txt). Deterministic: same code, same files.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

sys.path.insert(0, str(Path(__file__).resolve().parent))
import chip_music as cm  # noqa: E402
import chip_sfx as cs  # noqa: E402
from chip_synth import SR, lufs, peak_db, sfx_master  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / 'steam_assets' / 'audio_8bit' / 'synth'


def seam_ratio(x: np.ndarray) -> float:
    """Jump from the last to the first sample, relative to the loop's 99.9th-percentile sample-to-sample step."""
    jump = np.max(np.abs(x[0] - x[-1]))
    typical = np.percentile(np.abs(np.diff(x, axis=0)), 99.9)
    return float(jump / typical) if typical > 0 else 0.0


def render_music(track: cm.Track, out: Path, roll: bool) -> dict:
    notes = [] if roll else None
    x = cm.render(track, notes)
    path = out / 'music' / f'{track.file}.wav'
    path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(path, x, SR, subtype='PCM_16')
    if roll:
        draw_roll(track, notes, out / 'rolls' / f'{track.file}.png')
    return {'file': str(path.relative_to(REPO)), 'id': track.id, 'seconds': round(len(x) / SR, 3),
            'expected_seconds': round(cm.loop_seconds(track), 3), 'lufs': round(lufs(x), 2),
            'peak_dbfs': round(peak_db(x), 2), 'seam_ratio': round(seam_ratio(x), 3)}


def render_sfx(name: str, out: Path) -> dict:
    func, peak = cs.EFFECTS[name]
    x = sfx_master(func(), peak)
    path = out / f'{name}.wav'
    path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(path, x, SR, subtype='PCM_16')
    lead = int(0.005 * SR)
    return {'file': str(path.relative_to(REPO)), 'seconds': round(len(x) / SR, 3), 'peak_dbfs': round(peak_db(x), 2),
            'starts_at_once': bool(np.max(np.abs(x[:lead])) > 1e-3)}


def draw_roll(track: cm.Track, notes: list, path: Path) -> None:
    from PIL import Image, ImageDraw
    seconds = cm.loop_seconds(track)
    w, h, low, high = 1600, 420, 30, 102
    img = Image.new('RGB', (w, h), (18, 20, 18))
    d = ImageDraw.Draw(img)
    colors = {'p1': (240, 200, 70), 'p2': (90, 170, 230), 'tri': (120, 200, 120)}
    bar = 4 * 60.0 / track.bpm
    for b in range(int(round(seconds / bar)) + 1):
        x = int(b / (seconds / bar) * w)
        d.line([(x, 0), (x, h)], fill=(60, 60, 60) if b % 8 else (120, 120, 120))
    for ch, t0, dur, midi in notes:
        if ch not in colors:
            continue
        x0 = int(t0 % seconds / seconds * w)
        x1 = max(x0 + 1, int((t0 % seconds + dur) / seconds * w))
        y = int(h - (midi - low) / (high - low) * h)
        d.rectangle([x0, y - 2, x1, y + 2], fill=colors[ch])
    d.text((8, 6), f'{track.id} {track.file} {track.bpm} BPM {track.scale} ({seconds:.1f} s)', fill=(230, 230, 230))
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path)


def main(argv: list[str]) -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('what', nargs='?', default='all', choices=['all', 'music', 'sfx'])
    parser.add_argument('tracks', nargs='*', help='music track ids (M1 ...) or file names')
    parser.add_argument('--out', type=Path, default=OUT)
    parser.add_argument('--roll', action='store_true')
    args = parser.parse_args(argv)

    report_path = args.out / 'report.json'
    report = json.loads(report_path.read_text(encoding='utf-8')) if report_path.exists() else {}
    if args.what in ('all', 'music'):
        wanted = {t.lower() for t in args.tracks}
        for track in cm.TRACKS:
            if wanted and track.id.lower() not in wanted and track.file not in wanted:
                continue
            row = render_music(track, args.out, args.roll)
            report[f'music/{track.file}'] = row
            print(f'{track.id:<4} {track.file:<30} {row["seconds"]:7.1f} s  {row["lufs"]:6.1f} LUFS  '
                  f'peak {row["peak_dbfs"]:5.1f} dBFS  seam {row["seam_ratio"]:.2f}', flush=True)
    if args.what in ('all', 'sfx'):
        for name in cs.EFFECTS:
            row = render_sfx(name, args.out)
            report[name] = row
            print(f'     {name:<30} {row["seconds"]:7.2f} s  peak {row["peak_dbfs"]:5.1f} dBFS', flush=True)
    args.out.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=1), encoding='utf-8')
    print(f'Wrote {args.out.relative_to(REPO)} ({len(report)} files in report.json)')


if __name__ == '__main__':
    main(sys.argv[1:])
