"""Discord server administration for the Kratossos studio server (Run of the Nine, Corebound).

Talks to the Discord REST API (v10) as the studio bot. The token comes from DISCORD_BOT_TOKEN or from the Claude Code
Discord plugin's file (%USERPROFILE%/.claude/channels/discord/.env, or DISCORD_STATE_DIR/.env); it is never printed,
logged or written anywhere else. Standard library only.

    python tools/discord/discord_admin.py status
    python tools/discord/discord_admin.py plan  [layout.json]   # dry run: what apply would change
    python tools/discord/discord_admin.py apply [layout.json]   # create/update roles, channels, overwrites, AutoMod
    python tools/discord/discord_admin.py post    <channel-name> <message.md>  # split at '---' lines and 2,000 characters
    python tools/discord/discord_admin.py replace <channel-name> <message.md>  # delete the bot's posts there, then post
    python tools/discord/discord_admin.py set-icon <png>                       # server icon (brand/server_icon.png)
    python tools/discord/discord_admin.py invite [channel]                     # the permanent invite link
    python tools/discord/discord_admin.py export-credits                       # Playtester/Translator names -> game credits
    python tools/discord/discord_admin.py poll <channel> "Question?" "🎵 Answer one" "Answer two" ...  # 72 h poll

Forum channels need Community (Server Settings -> Enable Community); until then plan/apply list them as waiting.
"""
import json, os, sys, time, urllib.error, urllib.request
from pathlib import Path

API = 'https://discord.com/api/v10'
HERE = Path(__file__).resolve().parent
DEFAULT_LAYOUT = HERE / 'layout.json'

# Permission bits (https://discord.com/developers/docs/topics/permissions)
P = {
    'CREATE_INSTANT_INVITE': 1 << 0, 'KICK_MEMBERS': 1 << 1, 'BAN_MEMBERS': 1 << 2, 'ADMINISTRATOR': 1 << 3,
    'MANAGE_CHANNELS': 1 << 4, 'MANAGE_GUILD': 1 << 5, 'ADD_REACTIONS': 1 << 6, 'VIEW_AUDIT_LOG': 1 << 7,
    'STREAM': 1 << 9, 'VIEW_CHANNEL': 1 << 10, 'SEND_MESSAGES': 1 << 11, 'MANAGE_MESSAGES': 1 << 13,
    'EMBED_LINKS': 1 << 14, 'ATTACH_FILES': 1 << 15, 'READ_MESSAGE_HISTORY': 1 << 16, 'MENTION_EVERYONE': 1 << 17,
    'USE_EXTERNAL_EMOJIS': 1 << 18, 'CONNECT': 1 << 20, 'SPEAK': 1 << 21, 'USE_VAD': 1 << 25,
    'CHANGE_NICKNAME': 1 << 26, 'MANAGE_ROLES': 1 << 28, 'MANAGE_WEBHOOKS': 1 << 29,
    'USE_APPLICATION_COMMANDS': 1 << 31, 'MANAGE_EVENTS': 1 << 33, 'MANAGE_THREADS': 1 << 34,
    'CREATE_PUBLIC_THREADS': 1 << 35, 'CREATE_PRIVATE_THREADS': 1 << 36, 'SEND_MESSAGES_IN_THREADS': 1 << 38,
    'MODERATE_MEMBERS': 1 << 40, 'CREATE_EVENTS': 1 << 44,
}
CHANNEL_TYPES = {'text': 0, 'voice': 2, 'category': 4, 'announcement': 5, 'forum': 15}


def perms(names):
    value = 0
    for n in names:
        value |= P[n]
    return value


def perm_names(value):
    return [n for n, bit in P.items() if value & bit]


def load_token():
    token = os.environ.get('DISCORD_BOT_TOKEN')
    if token:
        return token.strip()
    state = Path(os.environ.get('DISCORD_STATE_DIR') or Path.home() / '.claude' / 'channels' / 'discord')
    env = state / '.env'
    if env.exists():
        for line in env.read_text(encoding='utf-8').splitlines():
            if line.startswith('DISCORD_BOT_TOKEN='):
                return line.split('=', 1)[1].strip()
    sys.exit(f'No bot token: set DISCORD_BOT_TOKEN or run /discord:configure (expected {env})')


TOKEN = None


class ApiError(Exception):
    def __init__(self, method, path, status, payload):
        super().__init__(f'{method} {path} -> HTTP {status}: {payload[:400]}')
        self.status = status


def api(method, path, body=None, reason=None):
    global TOKEN
    TOKEN = TOKEN or load_token()
    data = None if body is None else json.dumps(body).encode('utf-8')
    for _ in range(5):
        req = urllib.request.Request(API + path, data=data, method=method)
        req.add_header('Authorization', 'Bot ' + TOKEN)
        req.add_header('User-Agent', 'DiscordBot (https://github.com/PhilippBrune, 1.0) kratossos-admin')
        if data is not None:
            req.add_header('Content-Type', 'application/json')
        if reason:
            req.add_header('X-Audit-Log-Reason', reason[:500])
        try:
            with urllib.request.urlopen(req, timeout=30) as resp:
                raw = resp.read()
                return json.loads(raw) if raw else None
        except urllib.error.HTTPError as e:
            payload = e.read().decode('utf-8', 'replace')
            if e.code == 429:
                time.sleep(float(json.loads(payload).get('retry_after', 1.0)) + 0.1)
                continue
            raise ApiError(method, path, e.code, payload)
    raise ApiError(method, path, 429, 'rate limited too often')


def guild_id(layout=None):
    gid = os.environ.get('DISCORD_GUILD_ID') or (layout or {}).get('guild_id')
    if not gid:
        sys.exit('No guild id (layout.json "guild_id" or DISCORD_GUILD_ID)')
    return gid


def load_layout(path):
    return json.loads(Path(path).read_text(encoding='utf-8'))


def cmd_status(layout):
    gid = guild_id(layout)
    me = api('GET', '/users/@me')
    g = api('GET', f'/guilds/{gid}?with_counts=true')
    roles = api('GET', f'/guilds/{gid}/roles')
    member = api('GET', f'/guilds/{gid}/members/{me["id"]}')
    channels = api('GET', f'/guilds/{gid}/channels')
    role_by_id = {r['id']: r for r in roles}
    bot_perms = 0
    for rid in member['roles'] + [gid]:
        bot_perms |= int(role_by_id.get(rid, {}).get('permissions', 0))
    top = max((role_by_id[r]['position'] for r in member['roles'] if r in role_by_id), default=0)
    print(f'Bot: {me["username"]} (id {me["id"]})')
    print(f'Server: {g["name"]} (id {gid}), members ~{g.get("approximate_member_count")}, owner id {g["owner_id"]}')
    print(f'Features: {", ".join(sorted(g.get("features", []))) or "(none)"}')
    print(f'Verification level {g["verification_level"]}, explicit content filter {g["explicit_content_filter"]}, '
          f'MFA level {g["mfa_level"]}, default notifications {g["default_message_notifications"]}')
    print(f'Bot permissions: {"ADMINISTRATOR" if bot_perms & P["ADMINISTRATOR"] else ", ".join(perm_names(bot_perms))}')
    print(f'Bot top role position: {top}')
    print('Roles:', ', '.join(f'{r["name"]}({r["position"]}{", managed" if r.get("managed") else ""})'
                              for r in sorted(roles, key=lambda r: -r['position'])))
    kinds = {v: k for k, v in CHANNEL_TYPES.items()}
    cats = {c['id']: c['name'] for c in channels if c['type'] == 4}
    for c in sorted(channels, key=lambda c: (c.get('parent_id') or c['id'] if c['type'] != 4 else c['id'], c['type'] != 4, c['position'])):
        if c['type'] == 4:
            continue
        print(f'  #{c["name"]} [{kinds.get(c["type"], c["type"])}] in {cats.get(c.get("parent_id"), "-")}')
    print('Categories:', ', '.join(cats.values()) or '(none)')


# --- plan / apply ------------------------------------------------------------------------------------------------------

READ_ONLY_DENY = ['SEND_MESSAGES', 'CREATE_PUBLIC_THREADS', 'CREATE_PRIVATE_THREADS', 'SEND_MESSAGES_IN_THREADS']
STAFF_SEND = ['VIEW_CHANNEL', 'SEND_MESSAGES', 'EMBED_LINKS', 'ATTACH_FILES', 'MENTION_EVERYONE',
              'SEND_MESSAGES_IN_THREADS', 'CREATE_PUBLIC_THREADS']
REQUIRED_BOT_PERMS = ['MANAGE_CHANNELS', 'MANAGE_ROLES', 'MANAGE_GUILD', 'MANAGE_WEBHOOKS', 'MANAGE_MESSAGES',
                      'MANAGE_THREADS', 'MODERATE_MEMBERS', 'MANAGE_EVENTS', 'VIEW_CHANNEL', 'SEND_MESSAGES']


def color_int(value):
    return int(value.lstrip('#'), 16) if value else 0


class State:
    """Current server state plus name -> id resolution for roles that exist or will exist after apply."""

    def __init__(self, gid):
        self.gid = gid
        self.guild = api('GET', f'/guilds/{gid}')
        self.me = api('GET', '/users/@me')
        self.member = api('GET', f'/guilds/{gid}/members/{self.me["id"]}')
        self.reload()

    def reload(self):
        self.roles = api('GET', f'/guilds/{self.gid}/roles')
        self.channels = api('GET', f'/guilds/{self.gid}/channels')
        self.guild = api('GET', f'/guilds/{self.gid}')
        self.emojis = api('GET', f'/guilds/{self.gid}/emojis')

    def emoji(self, name):
        return next((e for e in self.emojis if e['name'] == name), None)

    @property
    def community(self):
        return 'COMMUNITY' in self.guild.get('features', [])

    def role(self, name):
        if name == '@everyone':
            return next(r for r in self.roles if r['id'] == self.gid)
        if name == '@bot':
            mine = [r for r in self.roles if r['id'] in self.member['roles']]
            return max(mine, key=lambda r: r['position']) if mine else None
        return next((r for r in self.roles if r['name'] == name), None)

    def bot_perms(self):
        value = int(self.role('@everyone')['permissions'])
        for r in self.roles:
            if r['id'] in self.member['roles']:
                value |= int(r['permissions'])
        return value

    def category(self, name):
        return next((c for c in self.channels if c['type'] == 4 and c['name'] == name), None)

    def channel(self, name, parent_id=None, kinds=None):
        for c in self.channels:
            if c['type'] == 4 or c['name'].lower() != name.lower():
                continue
            if kinds is not None and c['type'] not in kinds:
                continue
            if parent_id is not None and c.get('parent_id') != parent_id:
                continue
            return c
        return None


def overwrites_for(state, category, channel=None):
    """Permission overwrites (by role name) for a category or a channel in it."""
    ow = {}

    def add(role, allow=(), deny=()):
        a, d = ow.get(role, (set(), set()))
        ow[role] = (a | set(allow), d | set(deny))

    visible = (channel or {}).get('visible_to_only', None)
    if visible is None:
        visible = category.get('visible_to')
    if visible is not None:
        add('@everyone', deny=['VIEW_CHANNEL'])
        for r in visible + ['@bot']:
            add(r, allow=['VIEW_CHANNEL'])
    if category.get('read_only') or (channel or {}).get('read_only'):
        add('@everyone', deny=READ_ONLY_DENY)
        for r in ['@bot', 'Moderator']:
            add(r, allow=STAFF_SEND)
    return ow


def resolve_overwrites(state, ow):
    out = []
    for name, (allow, deny) in sorted(ow.items()):
        role = state.role(name)
        if role is None:
            continue  # created later in the same apply; resolved on the next pass
        out.append({'id': role['id'], 'type': 0, 'allow': str(perms(allow)), 'deny': str(perms(deny))})
    return out


def overwrites_equal(current, wanted):
    def norm(items):
        return sorted((o['id'], int(o['allow']), int(o['deny'])) for o in items if int(o['allow']) or int(o['deny']))
    return norm(current) == norm(wanted)


def build_plan(state, layout):
    """Returns (problems, actions); each action is (description, callable)."""
    gid = state.gid
    actions, problems = [], []
    state.waiting = []
    missing = [p for p in REQUIRED_BOT_PERMS if not state.bot_perms() & P[p]] if not state.bot_perms() & P['ADMINISTRATOR'] else []
    if missing:
        problems.append('Bot role lacks: ' + ', '.join(missing))

    settings = {k: v for k, v in layout.get('settings', {}).items() if state.guild.get(k) != v}
    if settings:
        actions.append((f'Server settings: {settings}', lambda s=settings: api('PATCH', f'/guilds/{gid}', s, 'server safety settings')))

    everyone = state.role('@everyone')
    want_everyone = perms(layout.get('everyone_permissions', []))
    if int(everyone['permissions']) != want_everyone:
        removed = perm_names(int(everyone['permissions']) & ~want_everyone)
        added = perm_names(want_everyone & ~int(everyone['permissions']))
        actions.append((f'@everyone permissions: +{added or "-"} -{removed or "-"}',
                        lambda: api('PATCH', f'/guilds/{gid}/roles/{gid}', {'permissions': str(want_everyone)}, 'base permissions')))

    import community  # noqa: E402 (Community features: emojis, onboarding, welcome screen, country roles)

    for spec in community.role_specs(layout):
        body = {'name': spec['name'], 'permissions': str(perms(spec.get('permissions', []))), 'color': color_int(spec.get('color')),
                'hoist': spec.get('hoist', False), 'mentionable': spec.get('mentionable', False)}
        role = state.role(spec['name'])
        if role is None:
            actions.append((f'Create role "{spec["name"]}" {spec.get("permissions") or ""}',
                            lambda b=body: api('POST', f'/guilds/{gid}/roles', b, 'server layout')))
        elif any(str(role[k]) != str(body[k]) for k in ('permissions', 'color', 'hoist', 'mentionable')):
            actions.append((f'Update role "{spec["name"]}"',
                            lambda r=role, b=body: api('PATCH', f'/guilds/{gid}/roles/{r["id"]}', b, 'server layout')))

    # Role hierarchy, top to bottom, directly above @everyone. Discord gives new roles position 1, so without this
    # they tie with the bot's role, and the bot can only manage roles strictly below its own.
    order = [state.role(n) for n in community.role_order(layout)]
    if order and all(order):
        wanted = {r['id']: len(order) - i for i, r in enumerate(order)}
        if any(r['position'] != wanted[r['id']] for r in order):
            names = ' > '.join(r['name'] for r in order)
            actions.append((f'Order roles: {names}',
                            lambda w=wanted: api('PATCH', f'/guilds/{gid}/roles',
                                                 [{'id': k, 'position': v} for k, v in w.items()], 'role hierarchy')))

    for spec in layout.get('remove', []):
        kind = CHANNEL_TYPES[spec['type']]
        if kind == 4:
            target = next((c for c in state.channels if c['type'] == 4 and c['name'].lower() == spec['name'].lower()), None)
        else:
            parent = state.category(spec.get('parent')) if spec.get('parent') else None
            target = state.channel(spec['name'], parent['id'] if parent else None, [kind])
        if target is not None:
            actions.append((f'Delete default {spec["type"]} "{target["name"]}"',
                            lambda t=target: api('DELETE', f'/channels/{t["id"]}', None, 'replaced by server layout')))

    for cat in layout.get('categories', []):
        existing_cat = state.category(cat['name'])
        cat_ow = overwrites_for(state, cat)
        if existing_cat is None:
            actions.append((f'Create category "{cat["name"]}"',
                            lambda c=cat, o=cat_ow: api('POST', f'/guilds/{gid}/channels',
                                                        {'name': c['name'], 'type': 4, 'permission_overwrites': resolve_overwrites(state, o)},
                                                        'server layout')))
        elif not overwrites_equal(existing_cat.get('permission_overwrites', []), resolve_overwrites(state, cat_ow)):
            actions.append((f'Update permissions of category "{cat["name"]}"',
                            lambda c=existing_cat, o=cat_ow: api('PATCH', f'/channels/{c["id"]}',
                                                                 {'permission_overwrites': resolve_overwrites(state, o)}, 'server layout')))
        for ch in cat['channels']:
            kind = ch['type']
            want_type = CHANNEL_TYPES['text'] if kind == 'announcement' and not state.community else CHANNEL_TYPES[kind]
            parent_id = existing_cat['id'] if existing_cat else None
            existing = state.channel(ch['name'], parent_id, [0, 5, 2, 15]) if existing_cat else None
            ow = overwrites_for(state, cat, ch)
            label = f'#{ch["name"]} [{kind}{" (text until Community)" if want_type != CHANNEL_TYPES[kind] else ""}] in {cat["name"]}'
            if existing is None and kind == 'forum' and not state.community:
                state.waiting.append(f'#{ch["name"]} [forum] in {cat["name"]}: needs Community')
                continue
            if existing is None:
                def create(c=cat, ch=ch, o=ow, t=want_type):
                    parent = state.category(c['name'])
                    body = {'name': ch['name'], 'type': t, 'parent_id': parent['id'] if parent else None,
                            'permission_overwrites': resolve_overwrites(state, o)}
                    if ch.get('topic') and t != CHANNEL_TYPES['voice']:
                        body['topic'] = ch['topic']
                    if t == CHANNEL_TYPES['forum']:
                        body['available_tags'] = [{'name': n} for n in ch.get('tags', [])]
                    return api('POST', f'/guilds/{gid}/channels', body, 'server layout')
                actions.append((f'Create {label}', create))
                continue
            changes = {}
            if existing['type'] != want_type and {existing['type'], want_type} == {0, 5}:
                changes['type'] = want_type
            if ch.get('topic') and existing.get('topic') != ch['topic'] and want_type != CHANNEL_TYPES['voice']:
                changes['topic'] = ch['topic']
            if not overwrites_equal(existing.get('permission_overwrites', []), resolve_overwrites(state, ow)):
                changes['permission_overwrites'] = None  # resolved at apply time
            if want_type == CHANNEL_TYPES['forum']:
                have = sorted(t['name'] for t in existing.get('available_tags', []))
                if have != sorted(ch.get('tags', [])):
                    changes['available_tags'] = [{'name': n} for n in ch.get('tags', [])]
            if changes:
                def update(e=existing, chg=changes, o=ow):
                    body = dict(chg)
                    if 'permission_overwrites' in body:
                        body['permission_overwrites'] = resolve_overwrites(state, o)
                    return api('PATCH', f'/channels/{e["id"]}', body, 'server layout')
                actions.append((f'Update {label}: {", ".join(changes)}', update))

    rules = api('GET', f'/guilds/{gid}/auto-moderation/rules') if state.bot_perms() & (P['MANAGE_GUILD'] | P['ADMINISTRATOR']) else []
    for spec in layout.get('automod', []):
        if any(r['name'] == spec['name'] for r in rules):
            continue

        def create_rule(s=spec):
            log = state.channel('mod-log')
            acts = [{'type': 1, 'metadata': {'custom_message': 'Blocked by the server rules.'}}]
            if log is not None:
                acts.append({'type': 2, 'metadata': {'channel_id': log['id']}})
            body = {'name': s['name'], 'event_type': 1, 'trigger_type': s['trigger_type'],
                    'trigger_metadata': s.get('trigger_metadata', {}), 'actions': acts, 'enabled': True}
            return api('POST', f'/guilds/{gid}/auto-moderation/rules', body, 'server layout')
        actions.append((f'Create AutoMod rule "{spec["name"]}" (alerts to #mod-log)', create_rule))
    actions += community.actions(state, layout)
    return problems, actions


def cmd_plan(layout, apply=False):
    state = State(guild_id(layout))
    problems, actions = build_plan(state, layout)
    print(f'Server "{state.guild["name"]}", Community: {"on" if state.community else "off"}')
    for p in problems:
        print('PROBLEM:', p)
    for w in getattr(state, 'waiting', []):
        print('WAITING:', w)
    if not actions:
        print('Nothing to change.')
        return
    print(f'{len(actions)} change(s){" to apply" if apply else " (dry run, nothing changed)"}:')
    for i, (desc, _) in enumerate(actions, 1):
        print(f'  {i:2}. {desc}')
    if not apply:
        return
    if problems:
        sys.exit('Not applied: fix the problems above first.')
    failed = run_actions(state, actions, 'done')
    # Channels created in this pass can need their overwrites resolved against roles created in the same pass.
    # Later passes: overwrites for roles created in this run, forums once Community is on, onboarding for new channels.
    for n in (2, 3):
        _, follow_up = build_plan(state, layout)
        follow_up = [a for a in follow_up if a[0] not in {d for d, _ in failed}]
        if not follow_up:
            break
        failed += run_actions(state, follow_up, f'done (pass {n})')
    if failed:
        print(f'{len(failed)} change(s) failed:')
        for desc, err in failed:
            print(f'  FAILED: {desc}: {err}')
        sys.exit(1)


def run_actions(state, actions, label):
    """Applies each action; a failed one is reported and the rest still run (apply can be rerun safely)."""
    failed = []
    for desc, act in actions:
        try:
            act()
            print(f'{label}: {desc}')
        except ApiError as e:
            failed.append((desc, str(e)))
        state.reload()
    return failed


PARAGRAPH = '\n\n'
BREAK = '\n---\n'


def split_message(text: str) -> list:
    """Messages of at most 2,000 characters: a line '---' forces a new message, paragraphs are never cut."""
    chunks = []
    for part in text.split(BREAK):
        chunks += _pack(part.strip())
    return [c for c in chunks if c]


def cmd_post(layout, channel_name, message_path, replace=False):
    """Posts a Markdown file to a channel (see split_message); `replace` first deletes the bot's own posts there."""
    state = State(guild_id(layout))
    channel = state.channel(channel_name)
    if channel is None:
        sys.exit(f'No channel #{channel_name}')
    text = Path(message_path).read_text(encoding='utf-8').strip()
    # <:name:> -> the server's custom emoji (Discord needs its id)
    import re
    text = re.sub(r'<:(\w+):>', lambda m: f'<:{m[1]}:{state.emoji(m[1])["id"]}>' if state.emoji(m[1]) else f':{m[1]}:', text)
    chunks = split_message(text)
    if replace:
        old = [m for m in api('GET', f'/channels/{channel["id"]}/messages?limit=50') if m['author']['id'] == state.me['id']]
        for m in old:
            api('DELETE', f'/channels/{channel["id"]}/messages/{m["id"]}', None, 'replaced by a new version')
        print(f'Deleted {len(old)} earlier post(s) in #{channel["name"]}')
    for chunk in chunks:
        api('POST', f'/channels/{channel["id"]}/messages',
            {'content': chunk, 'allowed_mentions': {'parse': []}}, 'post')
    print(f'Posted {len(chunks)} message(s) to #{channel["name"]}')


def cmd_set_icon(layout, png_path):
    import community
    gid = guild_id(layout)
    api('PATCH', f'/guilds/{gid}', {'icon': community._data_uri(Path(png_path))}, 'server icon')
    print(f'Server icon set from {png_path}')


def cmd_invite(layout, channel_name='welcome'):
    """The server's permanent invite (never expires, unlimited uses); reuses an existing one."""
    state = State(guild_id(layout))
    channel = state.channel(channel_name)
    for inv in api('GET', f'/channels/{channel["id"]}/invites'):
        if inv.get('max_age') == 0 and inv.get('max_uses') == 0 and inv.get('inviter', {}).get('id') == state.me['id']:
            print(f'https://discord.gg/{inv["code"]}')
            return
    inv = api('POST', f'/channels/{channel["id"]}/invites', {'max_age': 0, 'max_uses': 0, 'unique': True}, 'permanent invite')
    print(f'https://discord.gg/{inv["code"]}')


def cmd_poll(layout, channel_name, question, answers, hours=72, multi=False):
    """A native Discord poll (2-10 answers; 'emoji Text' gives the answer an emoji)."""
    state = State(guild_id(layout))
    channel = state.channel(channel_name)
    if channel is None:
        sys.exit(f'No channel #{channel_name}')
    media = []
    for a in answers:
        first, _, rest = a.partition(' ')
        media.append({'poll_media': {'text': rest, 'emoji': {'name': first}}} if rest and not first.isascii()
                     else {'poll_media': {'text': a}})
    api('POST', f'/channels/{channel["id"]}/messages',
        {'poll': {'question': {'text': question}, 'answers': media, 'duration': hours, 'allow_multiselect': multi}}, 'poll')
    print(f'Poll posted in #{channel["name"]} ({hours} h)')


def list_members(gid):
    """All members (needs "Server Members Intent" on the bot: Developer Portal -> Bot -> Privileged Gateway Intents)."""
    try:
        return api('GET', f'/guilds/{gid}/members?limit=1000')
    except ApiError as e:
        if e.status == 403:
            sys.exit('Listing members needs the "Server Members Intent": Discord Developer Portal -> your bot -> Bot -> '
                     'Privileged Gateway Intents -> Server Members Intent on, then Save. Nothing was changed.')
        raise


CREDITS_FILE = HERE.parents[1] / 'Assets' / 'Resources' / 'credits_community.json'


def cmd_export_credits(layout):
    """Writes the game's community credits (CREDITS-COMMUNITY-001) from the Playtester and Translator roles.
    Names are the members' server display names; review the file before a build. A translator's language comes from
    an existing entry with the same name (kept when re-exporting)."""
    state = State(guild_id(layout))
    roles = {name: state.role(name) for name in ('Playtester', 'Translator')}
    members = list_members(state.gid)
    old = json.loads(CREDITS_FILE.read_text(encoding='utf-8-sig')) if CREDITS_FILE.exists() else {}
    languages = {t.get('name'): t.get('language', '') for t in old.get('translators', [])}

    def display(m):
        return m.get('nick') or m['user'].get('global_name') or m['user']['username']

    def has(m, role_name):
        return roles[role_name] is not None and roles[role_name]['id'] in m['roles'] and not m['user'].get('bot')

    data = {'discord': old.get('discord', ''),
            'playtesters': sorted({display(m) for m in members if has(m, 'Playtester')}, key=str.lower),
            'translators': [{'name': n, 'language': languages.get(n, '')}
                            for n in sorted({display(m) for m in members if has(m, 'Translator')}, key=str.lower)]}
    CREDITS_FILE.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'{len(data["playtesters"])} playtester(s), {len(data["translators"])} translator(s) -> {CREDITS_FILE}')


def _pack(text: str) -> list:
    chunks, current = [], ''
    for para in text.split(PARAGRAPH):
        candidate = (current + PARAGRAPH + para) if current else para
        if len(candidate) <= 2000:
            current = candidate
            continue
        if current:
            chunks.append(current)
        if len(para) > 2000:
            sys.exit(f'A paragraph is longer than 2,000 characters: {para[:60]}...')
        current = para
    if current:
        chunks.append(current)
    return chunks


def main(argv):
    if not argv or argv[0] in ('-h', '--help'):
        print(__doc__)
        return
    cmd, rest = argv[0], argv[1:]
    layout_path = rest[0] if cmd in ('plan', 'apply') and rest else DEFAULT_LAYOUT
    layout = load_layout(layout_path) if Path(layout_path).exists() else {}
    try:
        if cmd == 'status':
            cmd_status(layout)
        elif cmd == 'plan':
            cmd_plan(layout)
        elif cmd == 'apply':
            cmd_plan(layout, apply=True)
        elif cmd == 'post' and len(rest) == 2:
            cmd_post(layout, rest[0], rest[1])
        elif cmd == 'replace' and len(rest) == 2:
            cmd_post(layout, rest[0], rest[1], replace=True)
        elif cmd == 'set-icon' and len(rest) == 1:
            cmd_set_icon(layout, rest[0])
        elif cmd == 'export-credits':
            cmd_export_credits(layout)
        elif cmd == 'invite':
            cmd_invite(layout, *rest[:1])
        elif cmd == 'poll' and len(rest) >= 4:
            cmd_poll(layout, rest[0], rest[1], rest[2:])
        else:
            sys.exit('unknown command or arguments: ' + ' '.join(argv))
    except ApiError as e:
        sys.exit(str(e))


if __name__ == '__main__':
    main(sys.argv[1:])
