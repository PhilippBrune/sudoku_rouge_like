using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class MidRunAdaptationService
    {
        public void AdaptDifficulty(LevelConfig config, RunState state, LevelState lastLevel)
        {
            if (lastLevel == null) return;

            // If player made many mistakes, reduce difficulty slightly
            if (lastLevel.Mistakes >= 3 && config.Stars > 1)
            {
                config.Stars = Math.Max(1, config.Stars - 1);
            }

            // If player had a perfect run, allow slight increase
            if (lastLevel.PerfectSoFar && lastLevel.CorrectPlacements > 10)
            {
                if (config.Stars < 6)
                    config.Stars = Math.Min(6, config.Stars + 1);
            }
        }
    }
}
