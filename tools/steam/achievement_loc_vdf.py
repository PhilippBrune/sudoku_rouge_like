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
# Steamworks names a token NEW_ACHIEVEMENT_<series>_<n> in the order the achievements were created; a reload of the
# achievements page starts a new series (the story achievements from the fourth on came after one; the class
# challenges continue that series).
FIRST_SERIES = ['harmony_1_win', 'harmony_3_win', 'harmony_5_win', 'harmony_7_win', 'harmony_9_win', 'harmony_10_win',
                'harmony_10_perfect', 'harmony_all_classes', 'first_puzzle', 'first_boss', 'floor_5', 'first_win',
                'perfect_run', 'no_items_win', 'one_hp_win', 'cursed_three', 'nine_by_nine', 'five_stars', 'class_second',
                'class_all', 'class_level_10', 'class_level_40', 'win_every_class', 'endless_25', 'trials_grandmaster',
                'monthly_walk', 'daily_streak_7', 'codex_half', 'codex_all',
                'story_new_page', 'story_tale_number_freak', 'story_tale_garden_monk']
FIFTH_SERIES = ['story_tale_shrine_archivist', 'story_tale_koi_gambler', 'story_tale_stone_gardener',
                'story_tale_lantern_seer', 'story_tale_reed_duelist', 'story_tale_quiet_cartographer', 'story_ninth_keeper',
                # the class challenges (RM-116), created in this order: NEW_ACHIEVEMENT_5_7 to _30
                'challenge_number_freak_1', 'challenge_number_freak_2', 'challenge_number_freak_3',
                'challenge_garden_monk_1', 'challenge_garden_monk_2', 'challenge_garden_monk_3',
                'challenge_shrine_archivist_1', 'challenge_shrine_archivist_2', 'challenge_shrine_archivist_3',
                'challenge_koi_gambler_1', 'challenge_koi_gambler_2', 'challenge_koi_gambler_3',
                'challenge_stone_gardener_1', 'challenge_stone_gardener_2', 'challenge_stone_gardener_3',
                'challenge_lantern_seer_1', 'challenge_lantern_seer_2', 'challenge_lantern_seer_3',
                'challenge_reed_duelist_1', 'challenge_reed_duelist_2', 'challenge_reed_duelist_3',
                'challenge_quiet_cartographer_1', 'challenge_quiet_cartographer_2', 'challenge_quiet_cartographer_3']
TOKENS = {**{api: f'NEW_ACHIEVEMENT_1_{n}' for n, api in enumerate(FIRST_SERIES)},
          **{api: f'NEW_ACHIEVEMENT_5_{n}' for n, api in enumerate(FIFTH_SERIES)}}


def vdf(language, texts):
    lines = ['"lang"', '{', f'\t"Language"\t"{language}"', '\t"Tokens"', '\t{']
    for api, token in TOKENS.items():
        for suffix, value in zip(('NAME', 'DESC'), texts[api]):
            assert not any(c in value for c in '"\\\t\n'), f'{api}: Key/Values would read a quote or escape here'
            lines.append(f'\t\t"{token}_{suffix}"\t"{value}"')
    return '\n'.join(lines + ['\t}', '}', ''])


def main():
    _, others = read_spec()
    missing = [api for api in TOKENS if len(others.get(api, [])) != len(STEAM_LANGUAGES)]
    assert not missing, f'section 5 lacks a row or a language for {missing}'
    os.makedirs(OUT, exist_ok=True)
    for column, language in enumerate(STEAM_LANGUAGES):
        path = os.path.join(OUT, f'4858470_loc_{language}.vdf')
        with open(path, 'w', encoding='utf-8', newline='\n') as file:
            file.write(vdf(language, {api: others[api][column] for api in TOKENS}))
        print(path)


if __name__ == '__main__':
    main()
