"""Tests for run_length_report.py:  python -m unittest discover tools/playtest"""
import json
import os
import tempfile
import unittest

import run_length_report as report


def run(minutes, victory=True, harmony=0, mode=0, session='a', floor=5, gap=-1, first=-1):
    return {'Mode': mode, 'HarmonyLevel': harmony, 'Victory': victory, 'ActiveSeconds': minutes * 60,
            'SessionId': session, 'FloorReached': floor, 'SecondsSincePreviousRun': gap, 'SecondsToFirstChoice': first}


class SummaryTests(unittest.TestCase):
    def test_median_of_won_h0_runs_only(self):
        entries = [run(40), run(50), run(60), run(90, harmony=2), run(10, victory=False, floor=2)]
        summary = report.summarize(entries)
        self.assertEqual(3, summary['won_h0_count'])
        self.assertEqual(50, summary['won_h0_median_min'])
        self.assertEqual({2: 1}, summary['garden_end_floors'])

    def test_way_back_needs_both_times(self):
        entries = [run(40, gap=20, first=8), run(40, gap=-1, first=8), run(40, gap=30, first=-1)]
        self.assertEqual(28, report.summarize(entries)['way_back_median_s'])

    def test_sessions_with_two_or_more_runs(self):
        summary = report.summarize([run(40, session='a'), run(40, session='a'), run(40, session='b')])
        self.assertEqual((2, 1), (summary['sessions'], summary['sessions_with_two_plus']))

    def test_other_modes_do_not_count_as_garden_runs(self):
        summary = report.summarize([run(5, mode=2, floor=14, victory=False), run(40)])
        self.assertEqual(1, summary['garden_runs'])
        self.assertEqual({}, summary['garden_end_floors'])


class BalanceTests(unittest.TestCase):
    def test_balance_of_won_garden_runs_with_numbers_only(self):
        won = dict(run(40), GoldLeft=60, ShopVisits=4, ShopVisitsWithPurchase=3, ItemsUsed=5)
        won2 = dict(run(40), GoldLeft=100, ShopVisits=2, ShopVisitsWithPurchase=1, ItemsUsed=3)
        lost = dict(run(10, victory=False, floor=2), GoldLeft=500, ShopVisits=1, ShopVisitsWithPurchase=0, ItemsUsed=0)
        summary = report.summarize([won, won2, lost, run(40)])   # the last one: recorded before 0.4.1
        self.assertEqual(2, summary['balance_runs'])
        self.assertEqual(80, summary['gold_left_mean'])
        self.assertAlmostEqual(4 / 6, summary['shop_purchase_share'])
        self.assertEqual(4, summary['items_used_mean'])
        self.assertIn('gold left 80', report.format_report(summary))

    def test_no_numbers_yet(self):
        self.assertIn('no won Garden Run with the numbers', report.balance_line(report.summarize([run(40)])))


class VerdictTests(unittest.TestCase):
    def test_too_few_runs(self):
        self.assertIn('Not enough', report.verdict(report.summarize([run(80), run(80)])))

    def test_long_runs_point_to_a_short_walk(self):
        self.assertIn('worth building', report.verdict(report.summarize([run(50), run(60), run(70)])))

    def test_short_runs_keep_five_floors(self):
        self.assertIn('not needed', report.verdict(report.summarize([run(30), run(35), run(40)])))


class FileTests(unittest.TestCase):
    def test_reads_the_run_history_of_a_save(self):
        with tempfile.TemporaryDirectory() as folder:
            path = os.path.join(folder, 'save_profile_0.json')
            with open(path, 'w', encoding='utf-8') as f:
                json.dump({'RunHistory': [run(40), run(41)], 'MetaProgress': {}}, f)
            self.assertEqual(2, len(report.load_history([path])))
            self.assertIn('Runs recorded: 2', report.format_report(report.summarize(report.load_history([path]))))

    def test_a_save_without_history_reads_as_empty(self):
        with tempfile.TemporaryDirectory() as folder:
            path = os.path.join(folder, 'old.json')
            with open(path, 'w', encoding='utf-8') as f:
                json.dump({'MetaProgress': {}}, f)
            self.assertEqual([], report.load_history([path]))
            self.assertIn('Not enough', report.format_report(report.summarize([])))


if __name__ == '__main__':
    unittest.main()
