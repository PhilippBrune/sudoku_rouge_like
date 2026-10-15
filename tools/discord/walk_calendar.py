"""The game's date-based challenges, computed exactly like the game does, for the Discord posts.

- Daily Walk: three goals per local date (DailyGoalService.RefreshIfNewDay)
- Seed of the week: a Garden Run seed per ISO week (WeeklySeedService, RUN-WEEK-001)
- Monthly Walk: a fixed 9×9 board per month (SeasonalChallengeService.BuildChallengeConfig)

Everything is derived from the date alone, so every player gets the same content without a server. Goal texts and
month themes are read from the C# sources, so there is one source of truth; DotNetRandom reproduces System.Random's
seeded sequence (the same in Unity's runtime; pinned by WalkCalendarParityTests in the game and test_walk_calendar.py).
"""
from __future__ import annotations

import datetime as dt
import re
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DAILY_SRC = REPO / 'Assets' / 'Scripts' / 'Run' / 'DailyGoalService.cs'
SEASON_SRC = REPO / 'Assets' / 'Scripts' / 'Run' / 'SeasonalChallengeService.cs'

TIER1, TIER2, TIER3 = list(range(0, 7)), list(range(7, 14)), list(range(14, 21))
# BossModifierId 0..14 (the Monthly Walk pool) with BossService.GetModifierName's English names.
MODIFIER_NAMES = ['Fog of War', 'Arrow Sums', 'German Whispers', 'Dutch Whispers', 'Parity Lines', 'Renban Lines',
                  'Killer Cages', 'Difference Kropki', 'Ratio Kropki', 'Palindrome', 'Thermometer', 'Between Lines',
                  'Even/Odd', 'Nonconsecutive', 'Anti-Knight']


class DotNetRandom:
    """System.Random(int seed): Knuth's subtractive generator as in the .NET reference source."""
    MBIG, MSEED = 2147483647, 161803398

    def __init__(self, seed: int):
        seed_array = [0] * 56
        subtraction = self.MBIG if seed == -2147483648 else abs(seed)
        mj = self.MSEED - subtraction
        seed_array[55] = mj
        mk = 1
        for i in range(1, 55):
            ii = (21 * i) % 55
            seed_array[ii] = mk
            mk = mj - mk
            if mk < 0:
                mk += self.MBIG
            mj = seed_array[ii]
        for _ in range(1, 5):
            for i in range(1, 56):
                seed_array[i] -= seed_array[1 + (i + 30) % 55]
                if seed_array[i] < 0:
                    seed_array[i] += self.MBIG
        self._a, self._inext, self._inextp = seed_array, 0, 21

    def _sample(self) -> int:
        inext, inextp = self._inext + 1, self._inextp + 1
        if inext >= 56:
            inext = 1
        if inextp >= 56:
            inextp = 1
        ret = self._a[inext] - self._a[inextp]
        if ret == self.MBIG:
            ret -= 1
        if ret < 0:
            ret += self.MBIG
        self._a[inext] = ret
        self._inext, self._inextp = inext, inextp
        return ret

    def next(self, max_value: int) -> int:
        return int(self._sample() * (1.0 / self.MBIG) * max_value)


@dataclass(frozen=True)
class Goal:
    id: str
    tier: str
    description: str
    reward: str


def all_goals() -> list:
    pattern = re.compile(r'Id = "(\w+)",\s*Tier = GoalTier\.(\w+),\s*Description = "([^"]+)",\s*RewardDescription = "([^"]+)"')
    goals = [Goal(*m.groups()) for m in pattern.finditer(DAILY_SRC.read_text(encoding='utf-8-sig'))]
    if len(goals) != 21:
        raise RuntimeError(f'expected 21 daily goals in {DAILY_SRC.name}, found {len(goals)}')
    return goals


def _pick_distinct(rng: DotNetRandom, pool: list, exclude: int) -> int:
    while True:
        pick = pool[rng.next(len(pool))]
        if pick != exclude:
            return pick


def daily_goal_ids(day: dt.date) -> list:
    seed = day.year * 10000 + day.month * 100 + day.day
    rng = DotNetRandom(seed)
    t1 = TIER1[rng.next(len(TIER1))]
    t2 = _pick_distinct(rng, TIER2, -1)
    t3 = _pick_distinct(rng, TIER2, t2) if seed % 2 == 0 else _pick_distinct(rng, TIER3, -1)
    return [t1, t2, t3]


def daily_goals(day: dt.date) -> list:
    goals = all_goals()
    return [goals[i] for i in daily_goal_ids(day)]


def iso_week(day: dt.date) -> tuple:
    year, week, _ = day.isocalendar()
    return year, week


def week_start(day: dt.date) -> dt.date:
    return day - dt.timedelta(days=day.weekday())


def weekly_seed(iso_year: int, iso_week_no: int) -> int:
    h = 2166136261
    for b in f'week:{iso_year:04d}-W{iso_week_no:02d}'.encode('ascii'):
        h ^= b
        h = (h * 16777619) & 0xFFFFFFFF
    return 100000 + h % 900000


@dataclass(frozen=True)
class MonthlyWalk:
    seed: int
    theme: str
    size: int
    stars: int
    irregular: bool
    modifiers: tuple


def month_themes() -> list:
    src = SEASON_SRC.read_text(encoding='utf-8-sig')
    block = src.split('ThemeFallbacks', 1)[1].split('};', 1)[0]
    return re.findall(r'"([^"]+)"', block)


def monthly_walk(year: int, month: int) -> MonthlyWalk:
    seed = year * 100 + month
    rng = DotNetRandom(seed)
    region_variant = seed % 4
    mod_count = 2 if seed % 3 == 0 else 1
    pool = list(range(15))
    for i in range(len(pool) - 1, 0, -1):
        j = rng.next(i + 1)
        pool[i], pool[j] = pool[j], pool[i]
    themes = month_themes()
    return MonthlyWalk(seed, themes[seed % len(themes)], 9, 4, region_variant >= 2,
                       tuple(MODIFIER_NAMES[m] for m in pool[:mod_count]))
