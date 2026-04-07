using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class EndlessZenService
    {
        private readonly Random _random;

        public EndlessZenService(int seed)
        {
            _random = new Random(seed);
        }

        public LevelConfig BuildNextLevel(int depth, int preferredSize = 0, int preferredStars = 0)
        {
            // Depth-based star scaling: Clamp(1 + depth/4, 1, 5)
            var stars = Math.Clamp(1 + depth / 4, 1, 5);
            if (preferredStars > 0) stars = Math.Clamp(preferredStars, 1, 6);

            // Depth-based board size: starts at 5, grows every 3 depth
            var size = Math.Clamp(5 + depth / 3, 5, 9);
            if (preferredSize > 0) size = Math.Clamp(preferredSize, 5, 9);

            // Modifier cap: 0 until depth 8, then 1 per 4 depth, max 3
            var modCap = depth < 8 ? 0 : Math.Min((depth - 8) / 4 + 1, 3);

            var config = new LevelConfig
            {
                BoardSize = size,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                RegionVariant = _random.Next(4),
                IsBoss = false,
                Seed = _random.Next(),
                Difficulty = DifficultyTier.Diff1
            };

            if (modCap > 0)
            {
                var pool = new List<BossModifierId>();
                foreach (BossModifierId mod in Enum.GetValues(typeof(BossModifierId)))
                    pool.Add(mod);

                for (var i = 0; i < modCap && pool.Count > 0; i++)
                {
                    var idx = _random.Next(pool.Count);
                    config.ActiveModifiers.Add(pool[idx]);
                    pool.RemoveAt(idx);
                }
            }

            return config;
        }

        public static RunState CreateZenRunState(ClassId classId, int seed)
        {
            var stats = Classes.ClassCatalog.GetDefinition(classId);
            return new RunState
            {
                ClassId = classId,
                Mode = GameMode.EndlessZen,
                Seed = seed,
                RunNumber = 1,
                CurrentHP = stats.BaseHP,
                MaxHP = stats.BaseHP,
                CurrentPencil = stats.BasePencil,
                MaxPencil = stats.BasePencil,
                CurrentGold = 0,
                ItemSlots = stats.BaseItemSlots,
                TotalFloors = 1,
                DisableProgressionRewards = true
            };
        }
    }
}
