using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Boss;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Manages the Monthly Walk — a fixed monthly puzzle challenge seeded by year+month.
    /// Same board and modifiers for all players in the same calendar month.
    /// No XP, gold, class passives, relics, or curses apply.
    /// </summary>
    public static class SeasonalChallengeService
    {
        private static readonly string[] ThemeNames =
        {
            "The Entrance Garden",  // seed % 12 == 0
            "The Pond Path",
            "The Old Tree Grove",
            "The Fountain Plaza",
            "The Far Bench",
            "Morning Mist",
            "Autumn Colours",
            "After Rain",
            "The Stone Bridge",
            "The Ice Cream Cart",
            "Lantern Festival",
            "The Iron Gate"         // seed % 12 == 11
        };

        // ── Seed & Theme ─────────────────────────────────────────────────────

        public static int BuildSeed(int year, int month) => year * 100 + month;

        public static string GetThemeName(int year, int month)
        {
            var seed = BuildSeed(year, month);
            return ThemeNames[seed % 12];
        }

        // ── Config & RunState ────────────────────────────────────────────────

        public static LevelConfig BuildChallengeConfig(int year, int month)
        {
            var seed = BuildSeed(year, month);
            var rng = new Random(seed);

            var regionVariant = seed % 4;
            var modCount = (seed % 3 == 0) ? 2 : 1;
            var modifiers = RollModifiers(rng, modCount);

            return new LevelConfig
            {
                BoardSize = 9,
                Stars = 4,
                RegionVariant = regionVariant,
                IsBoss = false,
                Intensity = BossModifierIntensity.High,
                Seed = seed,
                ActiveModifiers = modifiers
            };
        }

        public static RunState CreateChallengeRunState(int year, int month)
        {
            return new RunState
            {
                Mode = GameMode.SeasonalChallenge,
                IsSeasonalChallenge = true,
                RunNumber = -1,
                Seed = BuildSeed(year, month),
                CurrentHP = 10,
                MaxHP = 10,
                CurrentPencil = 12,
                MaxPencil = 12,
                CurrentGold = 0,
                DisableProgressionRewards = true,
                AllowIrregularPuzzles = true
            };
        }

        // ── Scoring ──────────────────────────────────────────────────────────

        public static int CalculateScore(int cellsCorrect, int mistakes, int pencilRemaining, int maxPencil)
        {
            var basePoints = 9 * 9 * 100;                                          // 8 100
            var mistakePenalty = mistakes * 300;
            var pencilBonus = maxPencil > 0
                ? (int)Math.Round((float)pencilRemaining / maxPencil * 500f)
                : 0;
            var perfectBonus = (mistakes == 0) ? 1000 : 0;

            var raw = basePoints - mistakePenalty + pencilBonus + perfectBonus;
            return Math.Max(raw, 0);
        }

        public static string GetGrade(int score, bool completed)
        {
            if (!completed) return "—";
            if (score >= 8500) return "S";
            if (score >= 6500) return "A";
            if (score >= 4500) return "B";
            if (score >= 2500) return "C";
            return "D";
        }

        // ── Display Helpers ──────────────────────────────────────────────────

        public static string GetCountdownLabel(DateTime today)
        {
            var firstOfNext = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            var daysLeft = (firstOfNext.Date - today.Date).Days;

            if (daysLeft == 0) return "Resets today";
            if (daysLeft == 1) return "Resets tomorrow";
            return $"Resets in {daysLeft} days";
        }

        public static string GetPanelSubtitle(LevelConfig cfg)
        {
            if (cfg == null || cfg.ActiveModifiers == null || cfg.ActiveModifiers.Count == 0)
                return "9\u00d79  \u2605\u2605\u2605\u2605";

            var mods = new System.Text.StringBuilder();
            foreach (var m in cfg.ActiveModifiers)
            {
                if (mods.Length > 0) mods.Append(", ");
                mods.Append(m.ToString());
            }
            return $"9\u00d79  \u2605\u2605\u2605\u2605  {mods}";
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private static List<BossModifierId> RollModifiers(Random rng, int count)
        {
            // Build eligible pool: active modifiers (IDs 0–14), no debuffs
            var eligible = new List<BossModifierId>();
            for (var i = 0; i <= (int)BossModifierId.Antiknight; i++)
                eligible.Add((BossModifierId)i);

            // Fisher-Yates shuffle with seeded rng
            for (var i = eligible.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = eligible[i];
                eligible[i] = eligible[j];
                eligible[j] = tmp;
            }

            var result = new List<BossModifierId>(count);
            for (var i = 0; i < Math.Min(count, eligible.Count); i++)
                result.Add(eligible[i]);
            return result;
        }
    }
}
