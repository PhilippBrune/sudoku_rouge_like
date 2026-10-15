"""Community features of the studio Discord, applied by discord_admin.py plan/apply (discord-server-setup.md).

- custom emojis (the games' icons, used in Onboarding),
- Community mode (rules channel, community-updates channel, server description),
- the welcome screen,
- Onboarding questions, which members also find any time under "Channels & Roles": games (game roles), pings,
  country (a flag role shown on the profile) and volunteering,
- the generated country roles ("🇩🇪 Germany").
"""
from __future__ import annotations

import base64
import time

import discord_admin as da

_SNOWFLAKE_COUNTER = [0]


def snowflake() -> str:
    """A new Discord-style id for onboarding prompts and options that do not exist yet."""
    _SNOWFLAKE_COUNTER[0] += 1
    return str(((int(time.time() * 1000) - 1420070400000) << 22) | (_SNOWFLAKE_COUNTER[0] & 0xFFF))


def country_role_name(flag: str, name: str) -> str:
    return f'{flag} {name}'


def role_specs(layout: dict) -> list:
    """The layout's roles plus one plain role per country."""
    specs = list(layout.get('roles', []))
    for flag, name in layout.get('countries', []):
        specs.append({'name': country_role_name(flag, name), 'color': None, 'hoist': False, 'mentionable': False,
                      'permissions': []})
    return specs


def role_order(layout: dict) -> list:
    """Top to bottom: the layout's role_order, then the country roles."""
    return list(layout.get('role_order', [])) + [country_role_name(f, n) for f, n in layout.get('countries', [])]


def _data_uri(path) -> str:
    return 'data:image/png;base64,' + base64.b64encode(path.read_bytes()).decode('ascii')


def actions(state, layout: dict) -> list:
    gid = state.gid
    out = []
    for spec in layout.get('emojis', []):
        if state.emoji(spec['name']) is None:
            out.append((f'Upload emoji :{spec["name"]}:',
                        lambda s=spec: da.api('POST', f'/guilds/{gid}/emojis',
                                              {'name': s['name'], 'image': _data_uri(da.HERE / s['file'])}, 'server layout')))

    community = layout.get('community')
    if community and not state.community:
        def enable(c=community):
            body = {'features': sorted(set(state.guild.get('features', [])) | {'COMMUNITY'}),
                    'rules_channel_id': state.channel(c['rules_channel'])['id'],
                    'public_updates_channel_id': state.channel(c['updates_channel'])['id'],
                    'description': c.get('description'), 'preferred_locale': c.get('preferred_locale', 'en-US')}
            return da.api('PATCH', f'/guilds/{gid}', body, 'enable Community')
        out.append((f'Enable Community (rules #{community["rules_channel"]}, updates #{community["updates_channel"]})', enable))
        return out  # welcome screen and onboarding need Community; the next pass adds them

    if not state.community:
        return out
    if community and community.get('description') and state.guild.get('description') != community['description']:
        out.append(('Set the server description', lambda c=community: da.api(
            'PATCH', f'/guilds/{gid}', {'description': c['description']}, 'server layout')))
    if layout.get('welcome_screen'):
        wanted = _welcome_body(state, layout['welcome_screen'])
        try:
            current = da.api('GET', f'/guilds/{gid}/welcome-screen')
        except da.ApiError as e:
            if e.status != 404:  # 404: no welcome screen yet
                raise
            current = {}
        enabled = 'WELCOME_SCREEN_ENABLED' in state.guild.get('features', [])
        if _welcome_key(current) != _welcome_key(wanted) or not enabled:
            out.append(('Set the welcome screen', lambda: da.api('PATCH', f'/guilds/{gid}/welcome-screen',
                                                                 _welcome_body(state, layout['welcome_screen']), 'server layout')))
    if layout.get('onboarding'):
        current = da.api('GET', f'/guilds/{gid}/onboarding')
        wanted = onboarding_body(state, layout, current)
        if _onboarding_key(current) != _onboarding_key(wanted):
            out.append((f'Set Onboarding and Channels & Roles ({len(wanted["prompts"])} questions)',
                        lambda: da.api('PUT', f'/guilds/{gid}/onboarding',
                                       onboarding_body(state, layout, da.api('GET', f'/guilds/{gid}/onboarding')),
                                       'server layout')))
    return out


# --- welcome screen ----------------------------------------------------------------------------------------------------

def _emoji_fields(state, spec: dict) -> dict:
    if spec.get('custom_emoji'):
        e = state.emoji(spec['custom_emoji'])
        return {'emoji_id': e['id'], 'emoji_name': e['name']} if e else {}
    return {'emoji_name': spec['emoji']} if spec.get('emoji') else {}


def _welcome_body(state, spec: dict) -> dict:
    channels = []
    for ch in spec.get('channels', []):
        c = state.channel(ch['channel'])
        if c:
            channels.append({'channel_id': c['id'], 'description': ch['description'], **_emoji_fields(state, ch)})
    return {'enabled': True, 'description': spec['description'], 'welcome_channels': channels}


def _welcome_key(body: dict):
    return (body.get('description'),
            [(c['channel_id'], c['description'], c.get('emoji_name')) for c in body.get('welcome_channels', [])])


# --- onboarding --------------------------------------------------------------------------------------------------------

def _expand_options(prompt: dict, layout: dict) -> list:
    if prompt.get('options_from') == 'countries':
        return [{'title': name, 'emoji': flag, 'roles': [country_role_name(flag, name)]}
                for flag, name in layout.get('countries', [])]
    return prompt['options']


def onboarding_body(state, layout: dict, current: dict | None) -> dict:
    """The PUT body for Onboarding; prompts and options keep their ids when their titles match."""
    spec = layout['onboarding']
    by_title = {p['title']: p for p in (current or {}).get('prompts', [])}

    def ids(names, lookup):
        found = [lookup(n) for n in names]
        return [x['id'] for x in found if x]

    prompts = []
    for p in spec['prompts']:
        old = by_title.get(p['title'], {})
        old_options = {o['title']: o for o in old.get('options', [])}
        options = []
        for o in _expand_options(p, layout):
            options.append({'id': old_options.get(o['title'], {}).get('id') or snowflake(), 'title': o['title'],
                            'description': o.get('description') or None,
                            'role_ids': ids(o.get('roles', []), state.role),
                            'channel_ids': ids(o.get('channels', []), state.channel),
                            **_emoji_fields(state, o)})
        prompts.append({'id': old.get('id') or snowflake(), 'type': 1 if p.get('dropdown') else 0, 'title': p['title'],
                        'single_select': p.get('single_select', False), 'required': p.get('required', False),
                        'in_onboarding': True, 'options': options})
    return {'prompts': prompts, 'default_channel_ids': ids(spec['default_channels'], state.channel),
            'enabled': True, 'mode': 0}


def _onboarding_key(body: dict):
    def emoji(o):
        return (o.get('emoji') or {}).get('name') or o.get('emoji_name')
    return (bool(body.get('enabled')), sorted(body.get('default_channel_ids', [])),
            [(p['title'], p['type'], p['single_select'], p['required'],
              [(o['title'], sorted(o.get('role_ids', [])), sorted(o.get('channel_ids', [])), emoji(o)) for o in p['options']])
             for p in body.get('prompts', [])])
