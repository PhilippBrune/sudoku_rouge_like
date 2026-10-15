"""Mirrors published Steam announcements into the games' Discord news channels (discord-server-setup.md, section 6).

Reads each game's public Steam news (ISteamNews/GetNewsForApp, community announcements only, no key needed) and
posts every announcement it has not posted before to the game's news channel as an embed, pinging the opt-in
"Patch Notes" role; in an announcement channel the post is also published to following servers. Only announcements
already published on Steam appear in that feed, so nothing goes out early.

The games and channels are the "steam_news" list in layout.json. The first run for a game only records what is
already there (no flood of old posts). State and log live next to the bot token, outside the repository:
%USERPROFILE%/.claude/channels/discord/steam_news_state.json and steam_news.log.

    python tools/discord/steam_news.py check                    # what is published, what would be posted
    python tools/discord/steam_news.py run                      # post new announcements (scheduled task)
    python tools/discord/steam_news.py preview rotn             # latest announcement to #release-drafts, no ping
    python tools/discord/steam_news.py publish-latest rotn      # latest announcement to its news channel, with ping
    python tools/discord/steam_news.py install-task [minutes]   # Windows Task Scheduler, every 30 min by default
    python tools/discord/steam_news.py remove-task
"""
from __future__ import annotations

import datetime as dt
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import discord_admin as da  # noqa: E402

NEWS_API = ('https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/'
            '?appid={app}&count=10&maxlength=0&feeds=steam_community_announcements')
CLAN_IMAGES = 'https://clan.akamai.steamstatic.com/images/'
STATE_DIR = Path(os.environ.get('DISCORD_STATE_DIR') or Path.home() / '.claude' / 'channels' / 'discord')
STATE = STATE_DIR / 'steam_news_state.json'
LOG = STATE_DIR / 'steam_news.log'
TASK_NAME = 'Kratossos Steam News to Discord'
EXCERPT_CHARS = 1800
LOG_KEEP_LINES = 500


class NotPublic(Exception):
    """The app has no public news yet (Steam answers 403/404 before release)."""


# --- Steam -------------------------------------------------------------------------------------------------------------

def fetch_news(app_id: int) -> list[dict]:
    """Published community announcements, newest first."""
    try:
        with urllib.request.urlopen(NEWS_API.format(app=app_id), timeout=30) as response:
            data = json.load(response)
    except urllib.error.HTTPError as e:
        if e.code in (403, 404):
            raise NotPublic(f'app {app_id}: Steam news not public yet (HTTP {e.code})') from None
        raise
    items = data.get('appnews', {}).get('newsitems', [])
    return sorted((i for i in items if i.get('feedname') == 'steam_community_announcements'),
                  key=lambda i: i.get('date', 0), reverse=True)


def announcement_url(item: dict) -> str:
    """The community announcement page the API link redirects to; the API link itself if that fails."""
    url = item.get('url', '')
    try:
        with urllib.request.urlopen(urllib.request.Request(url, method='HEAD'), timeout=20) as response:
            return response.geturl()
    except (urllib.error.URLError, ValueError):
        return url


def first_image(contents: str) -> str | None:
    match = re.search(r'\[img\](.*?)\[/img\]', contents, re.I | re.S)
    if not match:
        return None
    url = match[1].strip().replace('{STEAM_CLAN_IMAGE}', CLAN_IMAGES.rstrip('/'))
    return url if url.startswith('http') else None


def to_markdown(contents: str) -> str:
    """Steam announcement BBCode -> Discord Markdown."""
    text = contents.replace('\r\n', '\n')
    text = re.sub(r'\[img\].*?\[/img\]', '', text, flags=re.I | re.S)
    text = re.sub(r'\[previewyoutube=[^\]]*\].*?\[/previewyoutube\]', '', text, flags=re.I | re.S)
    text = re.sub(r'\[h[1-6]\](.*?)\[/h[1-6]\]', r'**\1**', text, flags=re.I | re.S)
    text = re.sub(r'\[url=([^\]]+)\](.*?)\[/url\]', r'[\2](\1)', text, flags=re.I | re.S)
    for tag, md in (('b', '**'), ('i', '*'), ('u', '__'), ('strike', '~~')):
        text = re.sub(rf'\[{tag}\](.*?)\[/{tag}\]', rf'{md}\1{md}', text, flags=re.I | re.S)
    text = re.sub(r'\[\*\]\s*', '• ', text)
    text = re.sub(r'\[/?[a-z0-9]+(?:=[^\]]*)?\]', '', text, flags=re.I)
    text = re.sub(r'[ \t]+\n', '\n', text)
    return re.sub(r'\n{3,}', '\n\n', text).strip()


def excerpt(markdown: str, limit: int = EXCERPT_CHARS) -> str:
    if len(markdown) <= limit:
        return markdown
    cut = markdown.rfind('\n', 0, limit)
    return markdown[:cut if cut > limit // 2 else limit].rstrip() + '\n…'


# --- state and log -----------------------------------------------------------------------------------------------------

def load_state() -> dict:
    return json.loads(STATE.read_text(encoding='utf-8')) if STATE.exists() else {}


def save_state(state: dict) -> None:
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    STATE.write_text(json.dumps(state, indent=1), encoding='utf-8')


def log(message: str) -> None:
    line = f'{dt.datetime.now().isoformat(timespec="seconds")} {message}'
    print(line, flush=True)
    try:
        STATE_DIR.mkdir(parents=True, exist_ok=True)
        lines = LOG.read_text(encoding='utf-8').splitlines() if LOG.exists() else []
        LOG.write_text('\n'.join((lines + [line])[-LOG_KEEP_LINES:]) + '\n', encoding='utf-8')
    except OSError:
        pass


# --- Discord -----------------------------------------------------------------------------------------------------------

def feeds(layout: dict) -> list[dict]:
    return layout.get('steam_news', [])


def find_feed(layout: dict, key: str) -> dict:
    for feed in feeds(layout):
        if key.lower() in (str(feed['app_id']), feed['key'].lower(), feed['game'].lower()):
            return feed
    sys.exit(f'Unknown game "{key}"; known: ' + ', '.join(f['key'] for f in feeds(layout)))


def build_message(feed: dict, item: dict, url: str, role_id: str | None) -> dict:
    embed = {
        'title': item['title'][:256],
        'url': url,
        'description': excerpt(to_markdown(item.get('contents', ''))) + f'\n\n[Full notes on Steam]({url})',
        'color': da.color_int(feed.get('color')),
        'timestamp': dt.datetime.fromtimestamp(item['date'], dt.timezone.utc).isoformat(),
        'footer': {'text': f'{feed["game"]} · Steam news'},
    }
    image = first_image(item.get('contents', ''))
    if image:
        embed['image'] = {'url': image}
    message = {'embeds': [embed], 'allowed_mentions': {'parse': [], 'roles': [role_id] if role_id else []}}
    if role_id:
        message['content'] = f'<@&{role_id}> New on Steam: **{item["title"]}**'
    return message


def post(state_obj: da.State, channel_name: str, message: dict) -> None:
    channel = state_obj.channel(channel_name)
    if channel is None:
        raise da.ApiError('POST', f'#{channel_name}', 404, 'channel not found')
    sent = da.api('POST', f'/channels/{channel["id"]}/messages', message, 'Steam news')
    if channel['type'] == da.CHANNEL_TYPES['announcement']:
        da.api('POST', f'/channels/{channel["id"]}/messages/{sent["id"]}/crosspost', None, 'Steam news')


def role_id(state_obj: da.State, name: str | None) -> str | None:
    role = state_obj.role(name) if name else None
    return role['id'] if role else None


# --- commands ----------------------------------------------------------------------------------------------------------

def cmd_check(layout: dict) -> None:
    state = load_state()
    for feed in feeds(layout):
        seen = set(state.get(str(feed['app_id']), {}).get('seen', []))
        baselined = str(feed['app_id']) in state
        try:
            items = fetch_news(feed['app_id'])
        except NotPublic as e:
            print(f'{feed["game"]}: {e}')
            continue
        print(f'{feed["game"]} (app {feed["app_id"]}) -> #{feed["channel"]}: {len(items)} published announcement(s)'
              + ('' if baselined else '; first run will only record them'))
        for item in items:
            mark = 'posted/seen' if item['gid'] in seen else ('NEW' if baselined else 'existing')
            when = dt.datetime.fromtimestamp(item['date']).strftime('%Y-%m-%d %H:%M')
            print(f'  [{mark}] {when}  {item["title"]}')


def cmd_run(layout: dict) -> None:
    state = load_state()
    discord_state = None
    for feed in feeds(layout):
        key = str(feed['app_id'])
        try:
            items = fetch_news(feed['app_id'])
        except NotPublic as e:
            log(f'{feed["game"]}: {e}')
            continue
        except (urllib.error.URLError, json.JSONDecodeError) as e:
            log(f'{feed["game"]}: Steam news unavailable ({e}); trying again next run')
            continue
        if key not in state:
            state[key] = {'seen': [i['gid'] for i in items]}
            save_state(state)
            log(f'{feed["game"]}: first run, recorded {len(items)} existing announcement(s) without posting')
            continue
        seen = set(state[key]['seen'])
        new = [i for i in items if i['gid'] not in seen]
        if not new:
            log(f'{feed["game"]}: nothing new ({len(items)} published)')
        for item in reversed(new):  # oldest first
            discord_state = discord_state or da.State(da.guild_id(layout))
            message = build_message(feed, item, announcement_url(item), role_id(discord_state, feed.get('ping_role')))
            try:
                post(discord_state, feed['channel'], message)
            except da.ApiError as e:
                log(f'{feed["game"]}: posting "{item["title"]}" failed: {e}')
                break
            state[key]['seen'].append(item['gid'])
            save_state(state)
            log(f'{feed["game"]}: posted "{item["title"]}" to #{feed["channel"]}')


def cmd_preview(layout: dict, key: str, channel: str, ping: bool, mark_seen: bool) -> None:
    feed = find_feed(layout, key)
    items = fetch_news(feed['app_id'])
    if not items:
        sys.exit(f'{feed["game"]}: no published announcement yet')
    item = items[0]
    discord_state = da.State(da.guild_id(layout))
    rid = role_id(discord_state, feed.get('ping_role')) if ping else None
    post(discord_state, channel, build_message(feed, item, announcement_url(item), rid))
    if mark_seen:
        state = load_state()
        entry = state.setdefault(str(feed['app_id']), {'seen': []})
        if item['gid'] not in entry['seen']:
            entry['seen'].append(item['gid'])
        save_state(state)
    log(f'{feed["game"]}: posted "{item["title"]}" to #{channel}{" with ping" if rid else ""}')


def cmd_install_task(minutes: int) -> None:
    # The base installation, not a project venv that may be deleted (the script needs the standard library only).
    pythonw = Path(sys.base_prefix) / 'pythonw.exe'
    runner = pythonw if pythonw.exists() else Path(sys.executable)
    command = f'"{runner}" "{Path(__file__).resolve()}" run'
    subprocess.run(['schtasks', '/Create', '/F', '/TN', TASK_NAME, '/SC', 'MINUTE', '/MO', str(minutes), '/TR', command],
                   check=True)
    print(f'Task "{TASK_NAME}" runs every {minutes} min while you are logged on: {command}')


def cmd_remove_task() -> None:
    subprocess.run(['schtasks', '/Delete', '/F', '/TN', TASK_NAME], check=True)


def main(argv: list[str]) -> None:
    if not argv or argv[0] in ('-h', '--help'):
        print(__doc__)
        return
    layout = da.load_layout(da.DEFAULT_LAYOUT)
    cmd, rest = argv[0], argv[1:]
    try:
        if cmd == 'check':
            cmd_check(layout)
        elif cmd == 'run':
            cmd_run(layout)
        elif cmd == 'preview' and len(rest) == 1:
            cmd_preview(layout, rest[0], 'release-drafts', ping=False, mark_seen=False)
        elif cmd == 'publish-latest' and len(rest) == 1:
            cmd_preview(layout, rest[0], find_feed(layout, rest[0])['channel'], ping=True, mark_seen=True)
        elif cmd == 'install-task':
            cmd_install_task(int(rest[0]) if rest else 30)
        elif cmd == 'remove-task':
            cmd_remove_task()
        else:
            sys.exit('unknown command or arguments: ' + ' '.join(argv))
    except NotPublic as e:
        sys.exit(str(e))
    except da.ApiError as e:
        log(f'Discord error: {e}')
        sys.exit(1)


if __name__ == '__main__':
    main(sys.argv[1:])
