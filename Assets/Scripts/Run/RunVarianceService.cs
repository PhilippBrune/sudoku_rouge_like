using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunVarianceService
    {
        public void ApplyVariance(LevelConfig config, float expectedHeat, bool isRiskPath, Random random, bool allowSpike)
        {
            config.ExpectedHeat = expectedHeat;
            config.VarianceBand = isRiskPath ? 0.15f : 0.05f;

            var delta = ((float)random.NextDouble() * 2f - 1f) * config.VarianceBand;
            config.MissingPercent = Math.Clamp(config.MissingPercent * (1f + delta), 0.05f, 0.80f);

            if (allowSpike && random.NextDouble() < 0.02)
            {
                config.Stars = Math.Min(5, config.Stars + 1);
                config.Difficulty = (DifficultyTier)Math.Min((int)DifficultyTier.Diff5, (int)config.Difficulty + 1);
                config.ActiveModifiers.Add(BossModifierId.ParityLines);
            }
        }
    }
}
