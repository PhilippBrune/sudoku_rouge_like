using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    /// <summary>
    /// New XP system based on ClassXpProgressionSystem spec.
    /// RunXP = sum of per-tile XP across all completed tiles.
    /// Per-tile: BaseXP(boardSize) × StarMultiplier × BossMultiplier + ModifierBonus + PerfectBonus
    /// XP curve: two-phase (linear L1-15, accelerated L16-40).
    /// </summary>
    public sealed class XpService
    {
        public const int MaxPlayerLevel = 40;

        // ---- Per-Tile XP ----

        public static int BaseXpForBoardSize(int boardSize)
        {
            return boardSize switch
            {
                <= 5 => 30,
                6    => 45,
                7    => 60,
                8    => 90,
                _    => 120
            };
        }

        public static float StarMultiplier(int stars)
        {
            var s = Math.Clamp(stars, 1, 6);
            return s switch
            {
                1 => 1.0f,
                2 => 1.2f,
                3 or 4 => 1.5f,
                _ => 2.0f  // 5 or 6
            };
        }

        /// <summary>Calculates XP for a single completed puzzle tile.</summary>
        public static TileXpEntry CalculateTile(int boardSize, int stars, int activeModCount,
            bool isBoss, bool perfectSolve)
        {
            var baseXp = BaseXpForBoardSize(boardSize);
            var starMult = StarMultiplier(stars);
            var tileXp = (int)Math.Floor(baseXp * starMult);

            var bossMultiplier = isBoss ? 1.5f : 1.0f;
            tileXp = (int)Math.Floor(tileXp * bossMultiplier);

            var modBonus = isBoss ? activeModCount * 15 : 0;
            var perfectBonus = perfectSolve ? 25 : 0;

            var total = tileXp + modBonus + perfectBonus;

            return new TileXpEntry
            {
                BoardSize = boardSize,
                Stars = stars,
                BaseXp = baseXp,
                StarMult = starMult,
                TileXp = tileXp,
                IsBoss = isBoss,
                ModifierBonus = modBonus,
                PerfectBonus = perfectBonus,
                TotalXp = Math.Max(1, total)
            };
        }

        /// <summary>Sums all tile XP entries into a run total breakdown.</summary>
        public static RunXpBreakdown CalculateRunXp(List<TileXpEntry> tiles)
        {
            var breakdown = new RunXpBreakdown();
            var total = 0;
            for (var i = 0; i < tiles.Count; i++)
            {
                total += tiles[i].TotalXp;
            }
            breakdown.Tiles = tiles;
            breakdown.TotalXp = Math.Max(1, total);
            return breakdown;
        }

        // ---- Two-Phase Level Curve ----

        /// <summary>
        /// XP required to advance FROM the given level.
        /// Phase 1 (L1-15): 50 + (n-1)*10
        /// Phase 2 (L16-40): previous + 35 each level
        /// </summary>
        public static int XpToNextLevel(int level)
        {
            var n = Math.Clamp(level, 1, MaxPlayerLevel);
            if (n <= 15)
                return 50 + (n - 1) * 10;

            // Phase 2: start from L15 value (190) and add 35 per level above 15
            return 190 + (n - 15) * 35;
        }

        /// <summary>
        /// Derives (level, progressXpIntoCurrentLevel, xpToNext) from a cumulative total XP.
        /// </summary>
        public static (int Level, int ProgressXp, int XpToNext) DeriveLevel(int totalXp)
        {
            var level = 1;
            var remaining = totalXp;
            while (level < MaxPlayerLevel)
            {
                var needed = XpToNextLevel(level);
                if (remaining < needed)
                    return (level, remaining, needed);
                remaining -= needed;
                level++;
            }
            return (MaxPlayerLevel, remaining, 0); // max level reached
        }
    }

    public sealed class TileXpEntry
    {
        public int BoardSize;
        public int Stars;
        public int BaseXp;
        public float StarMult;
        public int TileXp;
        public bool IsBoss;
        public int ModifierBonus;
        public int PerfectBonus;
        public int TotalXp;
    }

    public sealed class RunXpBreakdown
    {
        public List<TileXpEntry> Tiles = new();
        public int TotalXp;
    }
}
