"""Pins the walk calendar to the same values as the game's WalkCalendarParityTests (RUN-WEEK-001/002).

    python -m unittest discover -s tools/discord -p "test_*.py"
"""
import datetime as dt
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import walk_calendar as w  # noqa: E402


class WalkCalendarTests(unittest.TestCase):
    def test_seeded_random_matches_dotnet(self):
        rng = w.DotNetRandom(20261015)
        self.assertEqual([853, 829, 927, 976, 551], [rng.next(1000) for _ in range(5)])

    def test_daily_goals(self):
        for (y, m, d), ids in {(2026, 10, 15): [5, 12, 20], (2026, 1, 1): [2, 11, 14], (2027, 1, 1): [4, 12, 20]}.items():
            self.assertEqual(ids, w.daily_goal_ids(dt.date(y, m, d)))
        self.assertEqual(21, len(w.all_goals()))

    def test_seed_of_the_week(self):
        for (y, m, d), (iy, iw, seed) in {(2026, 10, 15): (2026, 42, 390609), (2026, 1, 1): (2026, 1, 160412),
                                          (2027, 1, 1): (2026, 53, 444943)}.items():
            self.assertEqual((iy, iw), w.iso_week(dt.date(y, m, d)))
            self.assertEqual(seed, w.weekly_seed(iy, iw))

    def test_monthly_walk(self):
        october = w.monthly_walk(2026, 10)
        self.assertEqual(('Anti-Knight',), october.modifiers)
        self.assertTrue(october.irregular)
        self.assertEqual(('Palindrome',), w.monthly_walk(2026, 12).modifiers)
        self.assertEqual(12, len(w.month_themes()))


if __name__ == '__main__':
    unittest.main()
