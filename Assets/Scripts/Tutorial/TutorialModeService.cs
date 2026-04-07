using System;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Tutorial
{
    public sealed class TutorialModeService
    {
        /// <summary>
        /// Returns missing percent range for a given star rating.
        /// Min = stars% (e.g. 6★ = 90%), Max = next_star% - 1% (e.g. 6★ max = 99%).
        /// 7★ is always 100% missing (empty grid).
        /// </summary>
        public static (float min, float max) GetMissingRange(int stars)
        {
            if (stars >= 7) return (1.0f, 1.0f);
            var min = StarDensityService.MissingPercentForStars(stars);
            // Next star's value minus a small epsilon
            var next = StarDensityService.MissingPercentForStars(stars + 1);
            return (min, next - 0.01f);
        }

        public LevelConfig BuildTutorialLevel(TutorialSetupConfig setup, int seed)
        {
            var stars = Math.Clamp(setup.Stars, 1, 7);
            var boardSize = Math.Clamp(setup.BoardSize, 5, 9);

            float missingPercent;
            if (stars >= 7)
            {
                missingPercent = 1.0f; // 100% — empty grid
            }
            else
            {
                var (minP, maxP) = GetMissingRange(stars);
                var rng = new Random(seed);
                missingPercent = (float)(minP + rng.NextDouble() * (maxP - minP));
            }

            var config = new LevelConfig
            {
                BoardSize = boardSize,
                Stars = stars,
                MissingPercent = missingPercent,
                RegionVariant = setup.RegionVariant,
                IsBoss = setup.SelectedModifiers.Count > 0,
                Seed = seed,
                Difficulty = DifficultyTier.Diff1
            };

            config.ActiveModifiers.AddRange(setup.SelectedModifiers);

            return config;
        }

        public static RunState CreateTutorialRunState(TutorialSetupConfig setup, int seed)
        {
            var isFree = setup.ResourceMode == TutorialResourceMode.Free;

            int hp, pencil, slots;
            if (isFree)
            {
                hp = 999; pencil = 999; slots = 3;
            }
            else
            {
                var def = ClassCatalog.GetDefinition(setup.SimulationClassId);
                hp = def.BaseHP;
                pencil = def.BasePencil;
                slots = def.BaseItemSlots;
            }

            return new RunState
            {
                ClassId = setup.SimulationClassId,
                Mode = GameMode.Tutorial,
                Seed = seed,
                RunNumber = 1,
                CurrentHP = hp,
                MaxHP = hp,
                CurrentPencil = pencil,
                MaxPencil = pencil,
                CurrentGold = 0,
                ItemSlots = slots,
                TotalFloors = 1,
                TutorialMode = true,
                DisableProgressionRewards = true,
                AllowIrregularPuzzles = setup.RegionVariant >= 2
            };
        }
    }
}
