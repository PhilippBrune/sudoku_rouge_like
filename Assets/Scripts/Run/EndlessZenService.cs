using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class EndlessZenService
    {
        // All 14 modifiers (all except German Whispers, which requires ≥7×7 and has narrow valid set)
        private static readonly BossModifierId[] ModifierPool =
        {
            BossModifierId.ParityLines,
            BossModifierId.DifferenceKropki,
            BossModifierId.DutchWhispers,
            BossModifierId.RenbanLines,
            BossModifierId.RatioKropki,
            BossModifierId.KillerCages,
            BossModifierId.ArrowSums,
            BossModifierId.FogOfWar,
            BossModifierId.Palindrome,
            BossModifierId.Thermo,
            BossModifierId.BetweenLines,
            BossModifierId.EvenOdd,
            BossModifierId.Nonconsecutive,
            BossModifierId.Antiknight
        };

        public int ModifierCap(int depth)
        {
            // Depth <10: 1 modifier, Depth >=10: 2 modifiers (no cap)
            return depth < 10 ? 1 : 2;
        }

        public LevelConfig BuildLevel(int depth, int seed = 0)
        {
            var stars = Math.Clamp(1 + depth / 4, 1, 5);
            var config = new LevelConfig
            {
                BoardSize = 9,
                Difficulty = DifficultyTier.Diff5,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                IsBoss = false
            };

            var rng = new Random(seed + depth * 97);
            var count = depth >= 10 ? 2 : 1;
            var used = new HashSet<int>();
            for (var i = 0; i < count && i < ModifierPool.Length; i++)
            {
                int idx;
                do { idx = rng.Next(ModifierPool.Length); } while (used.Contains(idx));
                used.Add(idx);
                config.ActiveModifiers.Add(ModifierPool[idx]);
            }

            return config;
        }
    }
}
