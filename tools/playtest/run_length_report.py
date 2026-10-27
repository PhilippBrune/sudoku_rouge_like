"""Reports the run-length measures of the one-more-run concept (GameDesignSpec.md §14, RUN-LENGTH-001) from the runs a
profile kept (RUN-LENGTH-001: `RunHistory` in the save, the last 200 runs, on this PC only).

    python tools/playtest/run_length_report.py                 # every profile of this PC
    python tools/playtest/run_length_report.py <save.json> ...  # given save files (e.g. sent by a playtester)

It prints the length of won Garden Runs at Harmony 0 (RM-113 "A short walk" is meant for runs over 45 minutes), the time
from the end of a run to the first choice of the next, the share of sessions with two or more runs and where runs end.
"""
import glob
import json
import os
import statistics
import sys
from collections import Counter

GARDEN_RUN = 0                     # Core.GameMode as JsonUtility writes it
SHORT_WALK_THRESHOLD_MIN = 45      # GameDesignSpec §14, one more run: a won H0 run should take under 45 minutes
WAY_BACK_TARGET_S = 30             # GameDesignSpec §14, one more run: end of a run to the next first choice
MIN_RUNS = 3                       # fewer won H0 runs than this say nothing yet


def default_saves():
    folder = os.path.join(os.environ.get('USERPROFILE', ''), 'AppData', 'LocalLow', 'Kratossos', 'sudoku_rouge_like')
    return sorted(glob.glob(os.path.join(folder, 'save_profile_*.json')))


def load_history(paths):
    entries = []
    for path in paths:
        with open(path, encoding='utf-8-sig') as f:
            entries.extend(json.load(f).get('RunHistory') or [])
    return entries


def summarize(entries):
    garden = [e for e in entries if e.get('Mode', GARDEN_RUN) == GARDEN_RUN]
    won_h0 = [e['ActiveSeconds'] / 60 for e in garden if e.get('Victory') and e.get('HarmonyLevel', 0) == 0]
    way_back = [e['SecondsSincePreviousRun'] + e['SecondsToFirstChoice'] for e in entries
                if e.get('SecondsSincePreviousRun', -1) >= 0 and e.get('SecondsToFirstChoice', -1) >= 0]
    sessions = Counter(e.get('SessionId') for e in entries if e.get('SessionId'))
    return {
        'runs': len(entries),
        'garden_runs': len(garden),
        'won_h0_count': len(won_h0),
        'won_h0_median_min': statistics.median(won_h0) if won_h0 else None,
        'way_back_median_s': statistics.median(way_back) if way_back else None,
        'sessions': len(sessions),
        'sessions_with_two_plus': sum(1 for n in sessions.values() if n >= 2),
        'garden_end_floors': dict(sorted(Counter(e.get('FloorReached', 0) for e in garden if not e.get('Victory')).items())),
        'garden_wins': sum(1 for e in garden if e.get('Victory')),
    }


def verdict(summary):
    if summary['won_h0_count'] < MIN_RUNS:
        return f'Not enough won Garden Runs at Harmony 0 yet ({summary["won_h0_count"]} of {MIN_RUNS}).'
    if summary['won_h0_median_min'] > SHORT_WALK_THRESHOLD_MIN:
        return f'A won run takes over {SHORT_WALK_THRESHOLD_MIN} min: RM-113 (a short walk) is worth building.'
    return f'A won run takes under {SHORT_WALK_THRESHOLD_MIN} min: five floors are short enough, RM-113 is not needed.'


def format_report(summary):
    def minutes(v):
        return '-' if v is None else f'{v:.0f} min'

    def seconds(v):
        return '-' if v is None else f'{v:.0f} s'

    sessions = summary['sessions']
    share = f'{summary["sessions_with_two_plus"] / sessions:.0%}' if sessions else '-'
    lines = [
        f'Runs recorded: {summary["runs"]} ({summary["garden_runs"]} Garden Runs, {summary["garden_wins"]} won)',
        f'Won Garden Run at Harmony 0, median length: {minutes(summary["won_h0_median_min"])} '
        f'(target under {SHORT_WALK_THRESHOLD_MIN} min, from {summary["won_h0_count"]} runs)',
        f'End of a run to the next first choice, median: {seconds(summary["way_back_median_s"])} (target under {WAY_BACK_TARGET_S} s)',
        f'Sessions with two or more runs: {summary["sessions_with_two_plus"]} of {sessions} ({share}; target more than half)',
        'Where lost Garden Runs end (floor: runs): '
        + (', '.join(f'{floor}: {n}' for floor, n in summary['garden_end_floors'].items()) or '-'),
        'Verdict: ' + verdict(summary),
    ]
    return '\n'.join(lines)


def main(argv):
    paths = argv or default_saves()
    if not paths:
        print('No save files found.')
        return 1
    print(format_report(summarize(load_history(paths))))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
