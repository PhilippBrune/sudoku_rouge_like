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
