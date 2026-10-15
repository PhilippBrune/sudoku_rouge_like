"""Posts the game's date-based challenges to #rotn-seeds-and-walks (RUN-WEEK-002, discord-server-setup.md §12).

- every day: the Daily Walk's three goals (the same for everyone playing that day)
- every week (Monday): the Seed of the week, a Discord Event for the week, @Events ping
- every month (1st): the Monthly Walk board, a Discord Event for the month, @Events ping
Each post opens a thread for results. The content comes from walk_calendar.py, which computes it like the game does.

    python tools/discord/walk_posts.py preview                 # all three to #release-drafts (no ping, no event)
    python tools/discord/walk_posts.py run                     # post what is due (scheduled task, every 30 min)
    python tools/discord/walk_posts.py champion <member name>  # give the Seed Champion role
    python tools/discord/walk_posts.py champion --reset        # take it from everyone
    python tools/discord/walk_posts.py install-task | remove-task

A run catches up: if the PC was off on Monday, the week's post goes out at the next run. State and log live next to
the bot token (%USERPROFILE%/.claude/channels/discord/walk_posts_state.json, walk_posts.log).
"""
from __future__ import annotations

import datetime as dt
import json
import subprocess
import sys
import urllib.parse
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import discord_admin as da  # noqa: E402
import walk_calendar as wc  # noqa: E402
from steam_news import STATE_DIR, log as _log  # noqa: E402

STATE = STATE_DIR / 'walk_posts_state.json'
CHANNEL = 'rotn-seeds-and-walks'
PING_ROLE = 'Events'
CHAMPION_ROLE = 'Seed Champion'
TASK_NAME = 'Kratossos Walk Posts'


def log(message: str) -> None:
    _log('[walks] ' + message)


def load_state() -> dict:
    return json.loads(STATE.read_text(encoding='utf-8')) if STATE.exists() else {}


def save_state(state: dict) -> None:
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    STATE.write_text(json.dumps(state, indent=1), encoding='utf-8')


# --- texts -------------------------------------------------------------------------------------------------------------

def daily_text(day: dt.date) -> str:
    lines = [f'**🌿 Daily Walk · {day:%A, %d %B}**',
             'Today\'s goals, the same for everyone who plays today:']
    for i, g in enumerate(wc.daily_goals(day), 1):
        lines.append(f'{i}. {g.description} — {g.reward}')
    lines.append('Finish all three to keep your streak. How did it go? Tell us in the thread.')
    return '\n'.join(lines)


def weekly_text(day: dt.date) -> str:
    year, week = wc.iso_week(day)
    start = wc.week_start(day)
    end = start + dt.timedelta(days=6)
    seed = wc.weekly_seed(year, week)
    return (f'**🌱 Seed of the week · Week {week} ({start:%d %b} – {end:%d %b})**\n'
            f'Seed **{seed}**: Start Run, then type it into the seed field '
            f'(from the next update the "Week {week} seed" button fills it in).\n'
            'Any class, any Harmony. Post your result in the thread: floor reached, run XP and a screenshot. '
            f'The best run of the week gets the **{CHAMPION_ROLE}** role.')


def monthly_text(day: dt.date) -> str:
    walk = wc.monthly_walk(day.year, day.month)
    regions = 'irregular regions' if walk.irregular else 'regular boxes'
    mods = ', '.join(walk.modifiers)
    label = 'modifiers' if len(walk.modifiers) > 1 else 'modifier'
    return (f'**🏮 Monthly Walk · {day:%B}: {walk.theme}**\n'
            f'One {walk.size}×{walk.size} {"★" * walk.stars} board for everyone this month, {regions}, {label}: {mods}.\n'
            'Main menu, then Monthly Walk. Post your grade and score in the thread; '
            f'the best score of the month gets the **{CHAMPION_ROLE}** role.')


# --- Discord -----------------------------------------------------------------------------------------------------------

def post(state_obj, channel_name: str, text: str, ping: bool, thread: str | None, archive_minutes: int = 1440):
    channel = state_obj.channel(channel_name)
    role = state_obj.role(PING_ROLE) if ping else None
    body = {'content': (f'<@&{role["id"]}> ' if role else '') + text,
            'allowed_mentions': {'parse': [], 'roles': [role['id']] if role else []}}
    msg = da.api('POST', f'/channels/{channel["id"]}/messages', body, 'walk post')
    if thread:
        da.api('POST', f'/channels/{channel["id"]}/messages/{msg["id"]}/threads',
               {'name': thread[:100], 'auto_archive_duration': archive_minutes}, 'results thread')
    return msg


def create_event(gid: str, name: str, description: str, location: str, start: dt.datetime, end: dt.datetime):
    now = dt.datetime.now().astimezone()
    start = max(start, now + dt.timedelta(minutes=2))  # Discord needs a start in the future
    da.api('POST', f'/guilds/{gid}/scheduled-events', {
        'name': name[:100], 'description': description[:1000], 'privacy_level': 2, 'entity_type': 3,
        'entity_metadata': {'location': location[:100]},
        'scheduled_start_time': start.isoformat(), 'scheduled_end_time': end.isoformat()}, 'walk event')


def tidy_events(state_obj) -> None:
    """Starts our events once their start has passed and ends them after their end (external events do not do this)."""
    now = dt.datetime.now(dt.timezone.utc)
    for ev in da.api('GET', f'/guilds/{state_obj.gid}/scheduled-events'):
        if ev.get('creator_id') != state_obj.me['id']:
            continue
        start = dt.datetime.fromisoformat(ev['scheduled_start_time'].replace('Z', '+00:00'))
        end = dt.datetime.fromisoformat(ev['scheduled_end_time'].replace('Z', '+00:00')) if ev.get('scheduled_end_time') else None
        if ev['status'] == 1 and start <= now:
            da.api('PATCH', f'/guilds/{state_obj.gid}/scheduled-events/{ev["id"]}', {'status': 2}, 'event started')
            log(f'event started: {ev["name"]}')
        elif ev['status'] == 2 and end and end <= now:
            da.api('PATCH', f'/guilds/{state_obj.gid}/scheduled-events/{ev["id"]}', {'status': 3}, 'event ended')
            log(f'event ended: {ev["name"]}')


def _local_midnight(day: dt.date) -> dt.datetime:
    return dt.datetime.combine(day, dt.time()).astimezone()


# --- commands ----------------------------------------------------------------------------------------------------------

def cmd_run(layout: dict) -> None:
    today = dt.date.today()
    state = load_state()
    s = da.State(da.guild_id(layout))
    year, week = wc.iso_week(today)
    week_key, month_key = f'{year}-W{week:02d}', f'{today:%Y-%m}'

    if state.get('daily') != today.isoformat():
        post(s, CHANNEL, daily_text(today), ping=False, thread=f'Daily Walk · {today:%d %b}')
        state['daily'] = today.isoformat()
        save_state(state)
        log(f'daily walk posted for {today}')
    if state.get('weekly') != week_key:
        start = wc.week_start(today)
        seed = wc.weekly_seed(year, week)
        post(s, CHANNEL, weekly_text(today), ping=True, thread=f'Seed of the week · Week {week}', archive_minutes=10080)
        create_event(s.gid, f'Seed of the week · Week {week}', weekly_text(today).replace('**', ''),
                     f'Run of the Nine · seed {seed}', _local_midnight(start), _local_midnight(start + dt.timedelta(days=7)))
        state['weekly'] = week_key
        save_state(state)
        log(f'seed of the week posted for {week_key} (seed {seed})')
    if state.get('monthly') != month_key:
        first = today.replace(day=1)
        next_first = (first + dt.timedelta(days=32)).replace(day=1)
        post(s, CHANNEL, monthly_text(today), ping=True, thread=f'Monthly Walk · {today:%B %Y}', archive_minutes=10080)
        create_event(s.gid, f'Monthly Walk · {today:%B}', monthly_text(today).replace('**', ''),
                     'Run of the Nine · Game Modes → Monthly Walk', _local_midnight(first), _local_midnight(next_first))
        state['monthly'] = month_key
        save_state(state)
        log(f'monthly walk posted for {month_key}')
    tidy_events(s)


def cmd_preview(layout: dict) -> None:
    today = dt.date.today()
    s = da.State(da.guild_id(layout))
    for text in (daily_text(today), weekly_text(today), monthly_text(today)):
        post(s, 'release-drafts', '**PREVIEW (walk posts)**\n' + text, ping=False, thread=None)
    print('Posted 3 previews to #release-drafts')


def cmd_champion(layout: dict, who: str) -> None:
    s = da.State(da.guild_id(layout))
    role = s.role(CHAMPION_ROLE)
    if who == '--reset':
        members = da.list_members(s.gid)
        for m in members:
            if role['id'] in m['roles']:
                da.api('DELETE', f'/guilds/{s.gid}/members/{m["user"]["id"]}/roles/{role["id"]}', None, 'champion reset')
                print(f'removed from {m["user"]["username"]}')
        return
    found = da.api('GET', f'/guilds/{s.gid}/members/search?query={urllib.parse.quote(who)}&limit=5')
    if len(found) != 1:
        sys.exit(f'{len(found)} members match "{who}": ' + ', '.join(m['user']['username'] for m in found))
    member = found[0]
    da.api('PUT', f'/guilds/{s.gid}/members/{member["user"]["id"]}/roles/{role["id"]}', None, 'Seed Champion')
    print(f'{member["user"]["username"]} is a {CHAMPION_ROLE}')


def cmd_install_task(minutes: int) -> None:
    pythonw = Path(sys.base_prefix) / 'pythonw.exe'
    runner = pythonw if pythonw.exists() else Path(sys.executable)
    command = f'"{runner}" "{Path(__file__).resolve()}" run'
    subprocess.run(['schtasks', '/Create', '/F', '/TN', TASK_NAME, '/SC', 'MINUTE', '/MO', str(minutes), '/TR', command],
                   check=True)
    print(f'Task "{TASK_NAME}" runs every {minutes} min while you are logged on: {command}')


def main(argv: list) -> None:
    if not argv or argv[0] in ('-h', '--help'):
        print(__doc__)
        return
    layout = da.load_layout(da.DEFAULT_LAYOUT)
    cmd, rest = argv[0], argv[1:]
    try:
        if cmd == 'run':
            cmd_run(layout)
        elif cmd == 'preview':
            cmd_preview(layout)
        elif cmd == 'champion' and rest:
            cmd_champion(layout, ' '.join(rest))
        elif cmd == 'install-task':
            cmd_install_task(int(rest[0]) if rest else 30)
        elif cmd == 'remove-task':
            subprocess.run(['schtasks', '/Delete', '/F', '/TN', TASK_NAME], check=True)
        else:
            sys.exit('unknown command or arguments: ' + ' '.join(argv))
    except da.ApiError as e:
        log(f'Discord error: {e}')
        sys.exit(1)


if __name__ == '__main__':
    main(sys.argv[1:])
