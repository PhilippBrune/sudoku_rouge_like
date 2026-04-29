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

        public LevelConfig BuildNextLevel(int depth, bool allowIrregularPuzzles, int preferredSize = 0, int preferredStars = 0)
        {
            // [ZENMODE-SESSION-001] Board size always 9×9 per spec
            const int size = 9;

            // [ZENMODE-DEPTH-002] Depth-based star scaling: Clamp(1 + depth/4, 1, 5)
            var stars = Math.Clamp(1 + depth / 4, 1, 5);
            if (preferredStars > 0) stars = Math.Clamp(preferredStars, 1, 5);

            // [ZENMODE-DEPTH-004] Modifier cap: depth<10→1, depth<20→2, depth≥20→3
            var modCap = depth < 10 ? 1 : depth < 20 ? 2 : 3;

            var config = new LevelConfig
            {
                BoardSize = size,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                RegionVariant = allowIrregularPuzzles ? _random.Next(4) : _random.Next(2),
                IsBoss = false,
                Seed = _random.Next(),
                Difficulty = DifficultyTier.Diff5   // [ZENMODE-SESSION-001] always Diff5
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

        public static RunState CreateZenRunState(ClassId classId, int seed, bool allowIrregularPuzzles)
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
                ItemSlots = 0,
                TotalFloors = 1,
                DisableProgressionRewards = true,
                AllowIrregularPuzzles = allowIrregularPuzzles,
                HarmonyConfig = HarmonyDifficultyService.BuildConfig(0)
            };
        }
    }
}
