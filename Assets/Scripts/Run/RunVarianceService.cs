using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;

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
            ApplyVariance(config, depth, floorIndex, maxRegionVariant, _random);
        }

        // [REQ: REQ-PATH-101] Size variance stays inside the floor's size range (GAP-013). The caller passes the
        // node's own seeded RNG, so a node's config is the same before and after Save & Quit (GAP-018).
        public void ApplyVariance(LevelConfig config, int depth, int floorIndex, int maxRegionVariant, Random rng)
        {
            if (config == null) return;
            rng ??= _random;
            // Note: star difficulty is now set deterministically by RollStars (floor table + 20% upward
            // variance baked in). No star mutation is applied here.
            // Size variance: +/-1 for non-boss
            if (!config.IsBoss && rng.NextDouble() < 0.15)
            {
                var delta = rng.NextDouble() < 0.5 ? -1 : 1;
                var min = FloorThemeData.GetMinBoardSize(floorIndex);
                var max = FloorThemeData.GetMaxBoardSize(floorIndex);
                config.BoardSize = Math.Clamp(config.BoardSize + delta, Math.Min(min, config.BoardSize), Math.Max(max, config.BoardSize));
            }

            // Region variant refresh — clamped to allowed range
            if (rng.NextDouble() < 0.30)
            {
                config.RegionVariant = rng.Next(maxRegionVariant + 1);
            }
        }
    }
}
