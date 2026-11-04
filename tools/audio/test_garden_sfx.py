"""Tests for garden_sfx.py:  python -m unittest test_garden_sfx  (in tools/audio)"""
import io
import unittest
import wave

import numpy as np

import garden_sfx as g


def samples(data: bytes) -> np.ndarray:
    with wave.open(io.BytesIO(data)) as w:
        assert (w.getnchannels(), w.getsampwidth(), w.getframerate()) == (1, 2, g.SR)
        return np.frombuffer(w.readframes(w.getnframes()), dtype='<i2') / 32768


class GardenSfxTests(unittest.TestCase):
    def test_every_event_has_two_audible_proposals(self):
        self.assertEqual(23, len(g.EVENTS), 'the 21 dropped sounds and the two too low ones')
        for event in g.EVENTS:
            for data in g.render(event):
                x = samples(data)
                peak_db = 20 * np.log10(np.max(np.abs(x)))
                self.assertAlmostEqual(g.EVENTS[event][3], peak_db, delta=0.5, msg=event)
                self.assertLess(len(x) / g.SR, 3.0, event)
                self.assertLess(abs(x[0]) + abs(x[-1]), 0.01, f'{event}: starts or ends with a click')
                spec = np.abs(np.fft.rfft(x)) ** 2
                above = spec[np.fft.rfftfreq(len(x), 1 / g.SR) >= 300].sum() / spec.sum()
                self.assertGreater(above, 0.6, f'{event}: small speakers would barely play it')

    def test_the_proposals_differ_and_are_the_same_every_time(self):
        for event in ('sfx/item_use', 'sfx/boss_gate_open'):
            a, b = g.render(event)
            self.assertNotEqual(a, b)
            self.assertEqual((a, b), g.render(event))

    def test_install_writes_each_pick_over_its_clip(self):
        import os, tempfile
        self.assertEqual(set(g.EVENTS), set(g.PICKS), 'one pick per event')
        with tempfile.TemporaryDirectory() as root:
            for folder in ('sfx', 'ui'):
                os.makedirs(os.path.join(root, folder))
            paths = g.install(root)
            self.assertEqual(23, len(paths))
            with open(os.path.join(root, 'sfx', 'combo_05.wav'), 'rb') as f:
                self.assertEqual(g.render('sfx/combo_05')[1], f.read(), 'combo_05 is pick B')
            with open(os.path.join(root, 'ui', 'item_reward.wav'), 'rb') as f:
                self.assertEqual(g.render('ui/item_reward')[1], f.read())

    def test_a_low_sound_is_named_low(self):
        low = g.wav_bytes(0.5 * np.sin(2 * np.pi * 90 * np.arange(g.SR // 4) / g.SR))
        self.assertTrue(g.low_only(low))
        self.assertFalse(g.low_only(g.render('sfx/item_use')[0]))

    def test_the_page_lists_every_event_with_three_choices(self):
        html = g.page({e: (w, b'RIFF', b'RIFF', b'') for e, (w, *_rest) in g.EVENTS.items()})
        for event in g.EVENTS:
            self.assertIn(f'name="{event.split("/")[1]}"', html)
        self.assertEqual(23, html.count('<i>none</i>'), 'a missing 8-bit file shows as none')


if __name__ == '__main__':
    unittest.main()
