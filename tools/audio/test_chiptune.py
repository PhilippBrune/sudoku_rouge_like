"""Checks for the code-made 8-bit set (chip_synth, chip_sfx, chip_music); no files are written.

    python -m unittest discover -s tools/audio -p "test_*.py"
"""
import re
import sys
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import chip_music as cm  # noqa: E402
import chip_sfx as cs  # noqa: E402
import make_8bit_audio as mk  # noqa: E402
from chip_synth import SR, lufs, peak_db, sfx_master  # noqa: E402

AUDIO_ASSETS = mk.REPO / 'Assets' / 'Scripts' / 'UI' / 'AudioAssetService.cs'


def game_slots(kind):
    source = AUDIO_ASSETS.read_text(encoding='utf-8-sig')
    return set(re.findall(rf'"audio/({kind}/[a-z0-9_]+)"', source))


class CoverageTests(unittest.TestCase):
    """AUDIO-MUSIC-001: every sound the game loads has a generator, and nothing else."""

    def test_every_music_slot_has_a_track(self):
        self.assertEqual({s.split('/')[1] for s in game_slots('music')}, {t.file for t in cm.TRACKS})
        self.assertEqual([f'M{i}' for i in range(1, 22)], [t.id for t in cm.TRACKS])

    def test_every_effect_slot_has_a_generator(self):
        self.assertEqual(game_slots('sfx') | game_slots('ui'), set(cs.EFFECTS))


class MusicTests(unittest.TestCase):
    """AUDIO-MUSIC-002: loops are whole bars, about -16 LUFS, peaks below -1 dBFS, seamless, deterministic."""

    def test_loops_are_whole_bars_of_their_tempo(self):
        for t in cm.TRACKS:
            self.assertAlmostEqual(cm.loop_seconds(t), len(t.sections) * 8 * 240.0 / t.bpm, places=6, msg=t.id)
            self.assertTrue(45 <= cm.loop_seconds(t) <= 130, t.id)

    def test_a_rendered_loop_meets_the_delivery_rules(self):
        track = cm.tracks_by_id()['M17']
        x = cm.render(track)
        self.assertEqual(int(round(cm.loop_seconds(track) * SR)), len(x))
        self.assertAlmostEqual(-16.0, lufs(x), delta=0.5)
        self.assertLessEqual(peak_db(x), -1.0)
        self.assertLess(mk.seam_ratio(x), 1.0)
        np.testing.assert_array_equal(x, cm.render(track))

    def test_boss_tracks_play_their_stage_melody(self):
        tracks = cm.tracks_by_id()
        for stage, boss in (('M2', 'M3'), ('M4', 'M5'), ('M6', 'M7'), ('M8', 'M9'), ('M10', 'M11')):
            a, b = tracks[stage], tracks[boss]
            self.assertEqual(a.theme, b.theme, boss)
            self.assertEqual(cm.section_notes(a, cm.Section('A')), cm.section_notes(b, cm.Section('A')), boss)


class EffectTests(unittest.TestCase):
    def test_effects_start_at_once_and_hit_their_peak(self):
        for name, (func, peak) in cs.EFFECTS.items():
            x = sfx_master(func(), peak)
            self.assertAlmostEqual(peak, peak_db(x), delta=0.2, msg=name)
            self.assertGreater(np.max(np.abs(x[: int(0.005 * SR)])), 1e-3, name)
            self.assertLessEqual(len(x) / SR, 4.5, name)


if __name__ == '__main__':
    unittest.main()
