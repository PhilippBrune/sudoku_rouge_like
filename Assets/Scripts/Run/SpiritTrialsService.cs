using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class SpiritTrialsService
    {
        private readonly Random _random;

        public SpiritTrialsService(int seed)
        {
            _random = new Random(seed);
        }

        public LevelConfig BuildTrialLevel(SpiritTrialsTier tier, bool allowIrregularPuzzles)
        {
            // [REQ: TRIAL-SESSION-001] All tiers use 9×9 board per spec
            const int size = 9;
            int stars, modCount;

            // [REQ: TRIAL-TIER-001]
            switch (tier)
            {
                case SpiritTrialsTier.Apprentice:
                    stars = 3; modCount = 0; break;
                case SpiritTrialsTier.Adept:
                    stars = 3; modCount = 1; break;  // 3★, 1 modifier
                case SpiritTrialsTier.Master:
                    stars = 4; modCount = 1; break;  // 4★, 1 modifier
                default: // Grandmaster
                    stars = 5; modCount = 2; break;  // 5★, 2 modifiers
            }

            var config = new LevelConfig
            {
                BoardSize = size,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                RegionVariant = allowIrregularPuzzles ? _random.Next(4) : _random.Next(2),
                IsBoss = modCount > 0,
                Seed = _random.Next(),
                Difficulty = (DifficultyTier)Math.Min((int)tier + 1, 5),
                RequireUniqueModifierSolution = modCount > 0
            };

            if (modCount > 0)
            {
                // [REQ: TRIAL-TIER-002] Modifiers assigned randomly (seeded); player cannot choose
                // Draw from all 15 modifiers
                var pool = new List<BossModifierId>();
                foreach (BossModifierId mod in Enum.GetValues(typeof(BossModifierId)))
                    pool.Add(mod);

                for (var i = 0; i < modCount && pool.Count > 0; i++)
                {
                    var idx = _random.Next(pool.Count);
                    config.ActiveModifiers.Add(pool[idx]);
                    pool.RemoveAt(idx);
                }
            }

            return config;
        }

        public static RunState CreateTrialRunState(ClassId classId, SpiritTrialsTier tier, int seed, bool allowIrregularPuzzles)
        {
            // [TRIAL-RES-001] HP and Pencil per tier
            int hp, pencil;
            switch (tier)
            {
                case SpiritTrialsTier.Apprentice:
                    hp = 5; pencil = 15; break;
                case SpiritTrialsTier.Adept:
                    hp = 4; pencil = 12; break;
                case SpiritTrialsTier.Master:
                    hp = 3; pencil = 10; break;
                default: // Grandmaster
                    hp = 2; pencil = 8; break;
            }

            return new RunState
            {
                ClassId = classId,
                Mode = GameMode.SpiritTrials,
                Seed = seed,
                RunNumber = 1,
                CurrentHP = hp,
                MaxHP = hp,
                CurrentPencil = pencil,
                MaxPencil = pencil,
                CurrentGold = 0,
                ItemSlots = 0,
                TotalFloors = 1,
                DisableProgressionRewards = true,
                SpiritTrialsTier = tier,
                AllowIrregularPuzzles = allowIrregularPuzzles,
                HarmonyConfig = HarmonyDifficultyService.BuildConfig(0)
            };
        }

        /// <summary>
        /// Calculate Spirit Trials score per spec [REQ: TRIAL-SCORE-005].
        /// FinalScore = floor((BasePoints × SpeedMultiplier) + ConstraintBonus + PencilBonus − MistakePenalty)
        /// </summary>
        /// <param name="cellsToFill">Cells the player had to fill (board cells − givens).</param>
        /// <param name="pencilMarksUsed">Total pencil marks placed during the session.</param>
        public static int CalculateScore(SpiritTrialsTier tier, float secondsPlayed, int modCount,
            int cellsToFill, int pencilMarksUsed, int mistakes)
        {
            // [REQ: TRIAL-SCORE-001] BasePoints = CellsToFill × PointsPerCell
            int pointsPerCell = tier switch
            {
                SpiritTrialsTier.Apprentice => 10,
                SpiritTrialsTier.Adept      => 15,
                SpiritTrialsTier.Master     => 20,
                _                           => 30   // Grandmaster
            };
            var basePoints = cellsToFill * pointsPerCell;

            // [REQ: TRIAL-SCORE-002] SpeedMultiplier = max(0.5, 2.0 − ElapsedSeconds / ParTime)
            float parTime = tier switch
            {
                SpiritTrialsTier.Apprentice => 300f,
                SpiritTrialsTier.Adept      => 420f,
                SpiritTrialsTier.Master     => 540f,
                _                           => 720f  // Grandmaster
            };
            var speedMult = Math.Max(0.5f, 2.0f - secondsPlayed / parTime);

            // [REQ: TRIAL-SCORE-003] ConstraintBonus
            var constraintBonus = modCount switch { 1 => 100, 2 => 300, _ => 0 };

            // [REQ: TRIAL-SCORE-004] Mistake penalty per tier
            int penaltyPerMistake = tier switch
            {
                SpiritTrialsTier.Apprentice => 20,
                SpiritTrialsTier.Adept      => 30,
                SpiritTrialsTier.Master     => 50,
                _                           => 75  // Grandmaster
            };
            var mistakePenalty = mistakes * penaltyPerMistake;

            // PencilBonus: max(0, (30 − pencilMarksUsed) × 5)
            var pencilBonus = Math.Max(0, (30 - pencilMarksUsed) * 5);

            // [REQ: TRIAL-SCORE-005] Final = floor(base × speed) + constraint + pencil − mistakes
            // [REQ: TRIAL-SCORE-006] Minimum score 0 (never negative)
            return Math.Max(0, (int)(basePoints * speedMult) + constraintBonus + pencilBonus - mistakePenalty);
        }
    }
}
