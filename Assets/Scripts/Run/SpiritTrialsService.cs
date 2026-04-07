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

        public LevelConfig BuildTrialLevel(SpiritTrialsTier tier)
        {
            // All tiers use 9×9 board per spec
            const int size = 9;
            int stars, modCount;

            switch (tier)
            {
                case SpiritTrialsTier.Apprentice:
                    stars = 3; modCount = 0; break;
                case SpiritTrialsTier.Adept:
                    stars = 4; modCount = 1; break;
                case SpiritTrialsTier.Master:
                    stars = 5; modCount = 2; break;
                default: // Grandmaster
                    stars = 6; modCount = 3; break;
            }

            var config = new LevelConfig
            {
                BoardSize = size,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                RegionVariant = _random.Next(4),
                IsBoss = modCount > 0,
                Seed = _random.Next(),
                Difficulty = (DifficultyTier)Math.Min((int)tier + 1, 5)
            };

            if (modCount > 0)
            {
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

        public static RunState CreateTrialRunState(SpiritTrialsTier tier, int seed)
        {
            int hp, pencil;
            switch (tier)
            {
                case SpiritTrialsTier.Apprentice:
                    hp = 5; pencil = 12; break;
                case SpiritTrialsTier.Adept:
                    hp = 4; pencil = 10; break;
                case SpiritTrialsTier.Master:
                    hp = 3; pencil = 8; break;
                default: // Grandmaster
                    hp = 2; pencil = 5; break;
            }

            return new RunState
            {
                Mode = GameMode.SpiritTrials,
                Seed = seed,
                RunNumber = 1,
                CurrentHP = hp,
                MaxHP = hp,
                CurrentPencil = pencil,
                MaxPencil = pencil,
                CurrentGold = 0,
                ItemSlots = 2,
                TotalFloors = 1,
                DisableProgressionRewards = false
            };
        }

        /// <summary>
        /// Calculate Spirit Trials score.
        /// FinalScore = (BasePoints × SpeedMultiplier) + ConstraintBonus + PencilBonus - MistakePenalty
        /// </summary>
        public static int CalculateScore(SpiritTrialsTier tier, float secondsPlayed, int modCount,
            int pencilRemaining, int mistakes)
        {
            // Base points by tier
            int basePoints;
            switch (tier)
            {
                case SpiritTrialsTier.Apprentice: basePoints = 1000; break;
                case SpiritTrialsTier.Adept: basePoints = 2000; break;
                case SpiritTrialsTier.Master: basePoints = 3500; break;
                default: basePoints = 5000; break;
            }

            // Speed multiplier: 2.0 at 60s, linearly decreasing to 0.5 at 600s
            var speedMult = Math.Max(0.5f, 2.0f - (secondsPlayed - 60f) / 360f);

            // Constraint bonus: +500 per active modifier
            var constraintBonus = modCount * 500;

            // Pencil bonus: +50 per pencil remaining
            var pencilBonus = pencilRemaining * 50;

            // Mistake penalty: -200 per mistake
            var mistakePenalty = mistakes * 200;

            return (int)(basePoints * speedMult) + constraintBonus + pencilBonus - mistakePenalty;
        }
    }
}
