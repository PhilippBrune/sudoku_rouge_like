using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class EndlessZenService
    {
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

        public LevelConfig BuildLevel(int depth)
        {
            var stars = Math.Clamp(1 + depth / 4, 1, 5);
            return new LevelConfig
            {
                BoardSize = 9,
                Difficulty = DifficultyTier.Diff5,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                IsBoss = false
            };
        }
    }
}
