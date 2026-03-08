using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class EndlessZenService
    {
        private static readonly BossModifierId[] ModifierPool =
        {
            BossModifierId.ParityLines,
            BossModifierId.DifferenceKropki,
            BossModifierId.DutchWhispers,
            BossModifierId.RenbanLines,
            BossModifierId.RatioKropki,
            BossModifierId.KillerCages,
            BossModifierId.ArrowSums,
            BossModifierId.FogOfWar
        };

        public float ComputeEndlessHeat(int depth)
        {
            return 1.2f + (0.18f * depth) + (0.02f * MathF.Pow(depth, 1.3f));
        }

        public int ModifierCap(int depth)
        {
            if (depth < 10) return 1;
            if (depth < 20) return 2;
            return 3;
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
