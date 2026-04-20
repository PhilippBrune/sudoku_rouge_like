using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunVarianceService
    {
        private readonly Random _random;

        public RunVarianceService(int seed)
        {
            _random = new Random(seed);
        }

        public void ApplyVariance(LevelConfig config, int depth, int floorIndex, int maxRegionVariant = 3)
        {
            // Note: star difficulty is now set deterministically by RollStars (floor table + 20% upward
            // variance baked in). No star mutation is applied here.
            // Size variance: +/-1 for non-boss
            if (!config.IsBoss && _random.NextDouble() < 0.15)
            {
                var delta = _random.NextDouble() < 0.5 ? -1 : 1;
                config.BoardSize = Math.Clamp(config.BoardSize + delta, 5, 9);
            }

            // Region variant refresh — clamped to allowed range
            if (_random.NextDouble() < 0.30)
            {
                config.RegionVariant = _random.Next(maxRegionVariant + 1);
            }
        }
    }
}
