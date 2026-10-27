"""Writes the Steamworks achievement localization files (Key/Values, UTF-8) for German, French, Spanish and Brazilian
Portuguese from section 5 of docs/Achievements.md, for the upload on Stats & Achievements -> Achievement Localization.

Usage: python tools/steam/achievement_loc_vdf.py
Writes steam_assets/achievements/localization/4858470_loc_<language>.vdf.
"""
import os

import achievement_icons as icons
from achievement_sheet import read_spec

OUT = os.path.join(icons.ROOT, 'steam_assets', 'achievements', 'localization')
STEAM_LANGUAGES = ['german', 'french', 'spanish', 'brazilian']   # the order of the section 5 columns
# Steamworks named the tokens NEW_ACHIEVEMENT_1_<n> in the order the achievements were created
TOKEN_ORDER = ['harmony_1_win', 'harmony_3_win', 'harmony_5_win', 'harmony_7_win', 'harmony_9_win', 'harmony_10_win',
               'harmony_10_perfect', 'harmony_all_classes', 'first_puzzle', 'first_boss', 'floor_5', 'first_win',
               'perfect_run', 'no_items_win', 'one_hp_win', 'cursed_three', 'nine_by_nine', 'five_stars', 'class_second',
               'class_all', 'class_level_10', 'class_level_40', 'win_every_class', 'endless_25', 'trials_grandmaster',
               'monthly_walk', 'daily_streak_7', 'codex_half', 'codex_all']


def vdf(language, texts):
    lines = ['"lang"', '{', f'\t"Language"\t"{language}"', '\t"Tokens"', '\t{']
    for number, api in enumerate(TOKEN_ORDER):
        for suffix, value in zip(('NAME', 'DESC'), texts[api]):
            assert not any(c in value for c in '"\\\t\n'), f'{api}: Key/Values would read a quote or escape here'
            lines.append(f'\t\t"NEW_ACHIEVEMENT_1_{number}_{suffix}"\t"{value}"')
    return '\n'.join(lines + ['\t}', '}', ''])


def main():
    _, others = read_spec()
    missing = [api for api in TOKEN_ORDER if len(others.get(api, [])) != len(STEAM_LANGUAGES)]
    assert not missing, f'section 5 lacks a row or a language for {missing}'
    os.makedirs(OUT, exist_ok=True)
    for column, language in enumerate(STEAM_LANGUAGES):
        path = os.path.join(OUT, f'4858470_loc_{language}.vdf')
        with open(path, 'w', encoding='utf-8', newline='\n') as file:
            file.write(vdf(language, {api: others[api][column] for api in TOKEN_ORDER}))
        print(path)


if __name__ == '__main__':
    main()
