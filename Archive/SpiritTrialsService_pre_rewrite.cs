using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class SpiritTrialsService
    {
        public int ComputeFinalTimeSeconds(int solveTimeSeconds, int mistakes, int hints)
        {
            return solveTimeSeconds + (mistakes * 8) + (hints * 3);
        }

        public string GetRank(int finalTimeSeconds)
        {
            if (finalTimeSeconds <= 510) return "S";
            if (finalTimeSeconds <= 630) return "A";
            if (finalTimeSeconds <= 780) return "B";
            return "C";
        }

        public int ComputeScore(int basePoints, float speedMultiplier, float constraintBonus, int mistakePenalty)
        {
            return Math.Max(0, (int)Math.Round(basePoints * speedMultiplier * constraintBonus) - mistakePenalty);
        }

        public float SpeedMultiplier(int solveTimeSeconds)
        {
            if (solveTimeSeconds <= 180) return 2.0f;
            if (solveTimeSeconds <= 300) return 1.5f;
            if (solveTimeSeconds <= 480) return 1.2f;
            if (solveTimeSeconds <= 600) return 1.0f;
            return 0.8f;
        }

        public float ConstraintBonus(int activeModifierCount)
        {
            return 1f + (activeModifierCount * 0.15f);
        }

        public int MistakePenalty(int mistakes)
        {
            return mistakes * 50;
        }

        public LevelConfig BuildDailyTrialLevel(int seed)
        {
            var random = new Random(seed);
            var stars = 3;
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
