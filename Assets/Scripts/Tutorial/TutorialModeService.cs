using System;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

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

        // [REQ: TUTO-BASICS-BOARD-001] Builds level config for the tutorial; seed drives missing-percent selection
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

        /// <summary>
        /// Builds a fixed 4×4 board for the Sudoku Basics first-time tutorial.
        /// Uses seed 999 for a deterministic, beginner-friendly puzzle.
        /// No modifiers, no HP cost, no gold.
        /// </summary>
        // [REQ: TUTO-BASICS-BOARD-001] Fixed 6×6 board, 2×3 boxes, seed=999, no modifiers
        public LevelConfig BuildSudokuBasicsLevel()
        {
            return new LevelConfig
            {
                BoardSize = 6,
                Stars = 1,
                MissingPercent = 0.25f, // ~9 of 36 cells empty (easy)
                RegionVariant = 0,       // standard 2×3 boxes
                IsBoss = false,
                Seed = 999,
                Difficulty = DifficultyTier.Diff1
                // No ActiveModifiers — clean board
            };
        }

        /// <summary>Creates a RunState for the Sudoku Basics tutorial (invincible, free pencil, no rewards).</summary>
        // [REQ: TUTO-BASICS-BOARD-002] Mistakes not penalised: HP=999, Pencil=999, DisableProgressionRewards=true
        public static RunState CreateSudokuBasicsRunState()
        {
            return new RunState
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.Tutorial,
                Seed = 999,
                RunNumber = 0,
                CurrentHP = 999,
                MaxHP = 999,
                CurrentPencil = 999,
                MaxPencil = 999,
                CurrentGold = 0,
                ItemSlots = 0,
                TotalFloors = 1,
                TutorialMode = true,
                DisableProgressionRewards = true,
                AllowIrregularPuzzles = false
            };
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
        /// <summary>
        /// Creates the deterministic 6×6 board used by the Sudoku Basics tutorial.
        // [REQ: TUTO-BASICS-BOARD-001] Hardcoded layout guarantees specific teaching cells at (3,1) and (3,2)
        /// Hardcoded to guarantee a specific teaching layout — independent of the
        /// generator so the tutorial script can reference exact cell positions.
        ///
        /// Regions: six 2×3 boxes (2 rows × 3 cols each).
        ///
        /// Solution:          Given cells (0=empty):
        ///   1 2 3 4 5 6        1 · 3 4 5 ·
        ///   4 5 6 1 2 3        · 5 6 1 · 3
        ///   2 3 4 5 6 1        2 3 · 5 6 1
        ///   5 6 1 2 3 4        5 · · 2 3 4
        ///   3 4 5 6 1 2        3 4 5 · 1 2
        ///   6 1 2 3 4 5        6 1 · 3 4 5
        ///
        /// Teaching cells:
        ///   (3,1)=6 — eliminated by row {5,2,3,4} ∩ col {5,3,4,1} → only 6 remains
        ///   (3,2)=1 — after placing 6, row 3 has exactly one gap
        /// </summary>
        public static SudokuBoard BuildBasicsTutorialBoard()
        {
            var solution = new int[6, 6]
            {
                { 1, 2, 3, 4, 5, 6 },
                { 4, 5, 6, 1, 2, 3 },
                { 2, 3, 4, 5, 6, 1 },
                { 5, 6, 1, 2, 3, 4 },
                { 3, 4, 5, 6, 1, 2 },
                { 6, 1, 2, 3, 4, 5 }
            };
            // Non-zero cells become givens; zeros are empty cells the player fills in.
            var cells = new int[6, 6]
            {
                { 1, 0, 3, 4, 5, 0 },
                { 0, 5, 6, 1, 0, 3 },
                { 2, 3, 0, 5, 6, 1 },
                { 5, 0, 0, 2, 3, 4 },
                { 3, 4, 5, 0, 1, 2 },
                { 6, 1, 0, 3, 4, 5 }
            };
            // Six 2×3 boxes: 0=rows0-1/cols0-2, 1=rows0-1/cols3-5,
            //                 2=rows2-3/cols0-2, 3=rows2-3/cols3-5,
            //                 4=rows4-5/cols0-2, 5=rows4-5/cols3-5
            var regionMap = new int[6, 6]
            {
                { 0, 0, 0, 1, 1, 1 },
                { 0, 0, 0, 1, 1, 1 },
                { 2, 2, 2, 3, 3, 3 },
                { 2, 2, 2, 3, 3, 3 },
                { 4, 4, 4, 5, 5, 5 },
                { 4, 4, 4, 5, 5, 5 }
            };
            return new SudokuBoard(6, solution, cells, regionMap);
        }
    }
}
