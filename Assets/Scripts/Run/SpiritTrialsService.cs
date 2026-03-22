using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Spirit Trials mode per SpiritTrialsMode spec.
    /// Timed competitive mode: single 9×9 puzzle, tier-based difficulty,
    /// scoring: floor((Base × SpeedMult) + ConstraintBonus + PencilBonus - MistakePenalty).
    /// </summary>
    public sealed class SpiritTrialsService
    {
        // ── Per-Tier Configuration ──

        public static SpiritTrialsTierConfig GetTierConfig(SpiritTrialsTier tier)
        {
            return tier switch
            {
                SpiritTrialsTier.Apprentice => new SpiritTrialsTierConfig
                {
                    Tier = SpiritTrialsTier.Apprentice,
                    Stars = 3, ModifierCount = 0,
                    HP = 5, Pencil = 15,
                    PointsPerCell = 10, PenaltyPerMistake = 20,
                    ParTimeSeconds = 300,
                    StartingItemRarity = ItemRarity.Normal
                },
                SpiritTrialsTier.Adept => new SpiritTrialsTierConfig
                {
                    Tier = SpiritTrialsTier.Adept,
                    Stars = 3, ModifierCount = 1,
                    HP = 4, Pencil = 12,
                    PointsPerCell = 15, PenaltyPerMistake = 30,
                    ParTimeSeconds = 420,
                    StartingItemRarity = ItemRarity.Normal
                },
                SpiritTrialsTier.Master => new SpiritTrialsTierConfig
                {
                    Tier = SpiritTrialsTier.Master,
                    Stars = 4, ModifierCount = 1,
                    HP = 3, Pencil = 10,
                    PointsPerCell = 20, PenaltyPerMistake = 50,
                    ParTimeSeconds = 540,
                    StartingItemRarity = ItemRarity.Rare
                },
                SpiritTrialsTier.Grandmaster => new SpiritTrialsTierConfig
                {
                    Tier = SpiritTrialsTier.Grandmaster,
                    Stars = 5, ModifierCount = 2,
                    HP = 2, Pencil = 8,
                    PointsPerCell = 30, PenaltyPerMistake = 75,
                    ParTimeSeconds = 720,
                    StartingItemRarity = ItemRarity.Rare
                },
                _ => GetTierConfig(SpiritTrialsTier.Apprentice)
            };
        }

        // ── Scoring ──

        /// <summary>
        /// SpeedMultiplier = max(0.5, 2.0 − (elapsed / parTime))
        /// Interpolates linearly: 2.0 at 0s → 1.0 at par → 0.5 at 2×par.
        /// </summary>
        public static float SpeedMultiplier(int elapsedSeconds, int parTimeSeconds)
        {
            if (parTimeSeconds <= 0) return 0.5f;
            return Math.Max(0.5f, 2.0f - (float)elapsedSeconds / parTimeSeconds);
        }

        /// <summary>Flat constraint bonus based on active modifier count.</summary>
        public static int ConstraintBonus(int modifierCount)
        {
            return modifierCount switch
            {
                0 => 0,
                1 => 100,
                2 => 300,
                _ => 300
            };
        }

        /// <summary>
        /// PencilBonus = max(0, (threshold - marksUsed) × 5).
        /// Threshold = 30 for all tiers. Max bonus = 150.
        /// </summary>
        public static int PencilBonus(int pencilMarksUsed)
        {
            const int threshold = 30;
            return Math.Max(0, (threshold - pencilMarksUsed) * 5);
        }

        /// <summary>
        /// FinalScore = floor((BasePoints × SpeedMult) + ConstraintBonus + PencilBonus - MistakePenalty)
        /// Minimum 0.
        /// </summary>
        public static SpiritTrialsScoreBreakdown CalculateScore(
            SpiritTrialsTierConfig config, int elapsedSeconds, int mistakes, int pencilMarksUsed,
            int cellsToFill)
        {
            var basePoints = cellsToFill * config.PointsPerCell;
            var speedMult = SpeedMultiplier(elapsedSeconds, config.ParTimeSeconds);
            var constraint = ConstraintBonus(config.ModifierCount);
            var pencil = PencilBonus(pencilMarksUsed);
            var penalty = mistakes * config.PenaltyPerMistake;

            var final = (int)Math.Floor(basePoints * speedMult) + constraint + pencil - penalty;
            final = Math.Max(0, final);

            return new SpiritTrialsScoreBreakdown
            {
                BasePoints = basePoints,
                SpeedMultiplier = speedMult,
                ConstraintBonus = constraint,
                PencilBonus = pencil,
                MistakePenalty = penalty,
                FinalScore = final
            };
        }

        // ── Level Building ──

        /// <summary>Build a Spirit Trials level config for the given tier and seed.</summary>
        public LevelConfig BuildTrialLevel(SpiritTrialsTier tier, int seed)
        {
            var config = GetTierConfig(tier);
            var random = new Random(seed);

            var modifiers = new List<BossModifierId>();
            if (config.ModifierCount > 0)
            {
                var pool = (BossModifierId[])Enum.GetValues(typeof(BossModifierId));
                var available = new List<BossModifierId>(pool);
                for (var i = 0; i < config.ModifierCount && available.Count > 0; i++)
                {
                    var idx = random.Next(available.Count);
                    modifiers.Add(available[idx]);
                    available.RemoveAt(idx);
                }
            }

            var level = new LevelConfig
            {
                BoardSize = 9,
                Difficulty = DifficultyTier.Diff5,
                Stars = config.Stars,
                MissingPercent = StarDensityService.MissingPercentForStars(config.Stars),
                IsBoss = false
            };
            level.ActiveModifiers.AddRange(modifiers);
            return level;
        }

        // ── Personal Best Tracking ──

        /// <summary>
        /// Updates personal bests for the given tier. Returns true if a new record was set.
        /// </summary>
        public static bool UpdatePersonalBest(SpiritTrialsPersonalBest[] bests,
            SpiritTrialsTier tier, int score, int timeSeconds, int mistakes)
        {
            var idx = (int)tier;
            bests[idx] ??= new SpiritTrialsPersonalBest();
            var pb = bests[idx];
            pb.TotalSessionsPlayed++;

            var newRecord = false;

            if (score > pb.BestScore)
            {
                pb.BestScore = score;
                newRecord = true;
            }

            if (pb.BestTimeSeconds == 0 || timeSeconds < pb.BestTimeSeconds)
            {
                pb.BestTimeSeconds = timeSeconds;
                newRecord = true;
            }

            if (mistakes == 0 && (pb.BestNoMistakeTimeSeconds == 0 || timeSeconds < pb.BestNoMistakeTimeSeconds))
            {
                pb.BestNoMistakeTimeSeconds = timeSeconds;
                newRecord = true;
            }

            return newRecord;
        }
    }

    /// <summary>Per-tier configuration for Spirit Trials.</summary>
    public sealed class SpiritTrialsTierConfig
    {
        public SpiritTrialsTier Tier;
        public int Stars;
        public int ModifierCount;
        public int HP;
        public int Pencil;
        public int PointsPerCell;
        public int PenaltyPerMistake;
        public int ParTimeSeconds;
        public ItemRarity StartingItemRarity;
    }

    /// <summary>Score breakdown for results screen display.</summary>
    public sealed class SpiritTrialsScoreBreakdown
    {
        public int BasePoints;
        public float SpeedMultiplier;
        public int ConstraintBonus;
        public int PencilBonus;
        public int MistakePenalty;
        public int FinalScore;
    }
}
