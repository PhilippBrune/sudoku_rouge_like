"""Checks for tools/audio/mureka_generate.py and the prompt sheet it reads (no network, no API key needed).

    python -m unittest discover -s tools/audio -p "test_*.py"
"""
import os
import re
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))
import mureka_generate as mg  # noqa: E402

AUDIO_ASSETS = mg.REPO / 'Assets' / 'Scripts' / 'UI' / 'AudioAssetService.cs'
# Words that pull an AI model towards real instruments instead of chip sound (prompt sheet 1.1, section 2).
REAL_INSTRUMENTS = ('flute', 'taiko', 'piano', 'string', 'guitar', 'violin', 'koto', 'shamisen', 'bell', 'chime',
                    'choir', 'orchestra', 'synth pad')


class PromptSheetTests(unittest.TestCase):
    """AUDIO-MUSIC-001: the sheet lists every music slot of the game with a chip-only prompt."""

    def setUp(self):
        self.style, self.tracks = mg.read_sheet()

    def test_twenty_one_tracks_in_order_with_unique_files(self):
        self.assertEqual([f'M{i}' for i in range(1, 22)], [t.id for t in self.tracks])
        self.assertEqual(len(self.tracks), len({t.file for t in self.tracks}))

    def test_every_track_is_a_music_slot_the_game_loads(self):
        source = AUDIO_ASSETS.read_text(encoding='utf-8-sig')
        slots = set(re.findall(r'"audio/music/([a-z0-9_]+)"', source))
        self.assertEqual(slots, {t.file for t in self.tracks})

    def test_style_block_asks_for_chiptune(self):
        for word in ('8-bit', 'chiptune', 'triangle', 'noise', 'no vocals'):
            self.assertIn(word, self.style)

    def test_prompts_name_no_real_instruments(self):
        for track in self.tracks:
            for word in REAL_INSTRUMENTS:
                self.assertNotIn(word, track.prompt.lower(), f'{track.id} mentions "{word}"')

    def test_full_prompts_stay_short(self):
        for track in self.tracks:
            self.assertLessEqual(len(mg.full_prompt(self.style, track)), mg.PROMPT_WARN, track.id)
            self.assertRegex(track.prompt, r'\d+ BPM', track.id)


class TakeTests(unittest.TestCase):
    def test_every_audio_link_of_every_choice_is_a_numbered_take(self):
        result = {'choices': [
            {'url': 'https://cdn.example/a/x.mp3', 'flac_url': 'https://cdn.example/a/x.flac', 'duration': 90},
            {'url': 'https://cdn.example/b/y.mp3'},
        ]}
        self.assertEqual([
            ('https://cdn.example/a/x.mp3', 'menu_loop_take3.mp3'),
            ('https://cdn.example/a/x.flac', 'menu_loop_take3.flac'),
            ('https://cdn.example/b/y.mp3', 'menu_loop_take4.mp3'),
        ], mg.take_downloads(result, 'menu_loop', 3))

    def test_no_choices_means_no_downloads(self):
        self.assertEqual([], mg.take_downloads({'status': 'succeeded'}, 'menu_loop', 1))


class KeyTests(unittest.TestCase):
    def test_key_comes_from_the_env_file_when_the_variable_is_unset(self):
        with tempfile.TemporaryDirectory() as tmp:
            env = Path(tmp) / '.env'
            env.write_text('﻿MUREKA_API_KEY=test-key-123\n', encoding='utf-8')
            with mock.patch.dict(os.environ, {'MUREKA_ENV_FILE': str(env)}, clear=False):
                os.environ.pop('MUREKA_API_KEY', None)
                self.assertEqual('test-key-123', mg.load_key())

    def test_dry_run_sends_nothing(self):
        args = mg.argparse.Namespace(all=False, tracks=['M1'], again=True, yes=False, timeout=1)
        with mock.patch.object(mg, 'api', side_effect=AssertionError('no request in a dry run')):
            mg.cmd_generate(*mg.read_sheet(), args)


if __name__ == '__main__':
    unittest.main()
