using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Boss
{
    public sealed class BossService
    {
        private readonly Random _random;

        private static readonly Dictionary<BossModifierId, (BossModifierTier Tier, float Impact)> ModifierData = new()
        {
            [BossModifierId.ParityLines] = (BossModifierTier.Tier1, 0.15f),
            [BossModifierId.DifferenceKropki] = (BossModifierTier.Tier1, 0.20f),
            [BossModifierId.DutchWhispers] = (BossModifierTier.Tier2, 0.30f),
            [BossModifierId.RenbanLines] = (BossModifierTier.Tier2, 0.35f),
            [BossModifierId.RatioKropki] = (BossModifierTier.Tier2, 0.40f),
            [BossModifierId.KillerCages] = (BossModifierTier.Tier3, 0.50f),
            [BossModifierId.ArrowSums] = (BossModifierTier.Tier3, 0.55f),
            [BossModifierId.FogOfWar] = (BossModifierTier.Tier4, 0.60f),
            [BossModifierId.GermanWhispers] = (BossModifierTier.Tier5, 0.75f)
        };

        public BossService(int seed)
        {
            _random = new Random(seed);
        }

        public float GetImpact(BossModifierId id)
        {
            return ModifierData[id].Impact;
        }

        public List<BossModifierId> RollBossChoices(int runNumber, int stars)
        {
            var pool = BuildModifierPool(runNumber, stars);
            var choiceA = pool[_random.Next(pool.Count)];
            var choiceB = pool[_random.Next(pool.Count)];

            while (choiceB == choiceA)
            {
                choiceB = pool[_random.Next(pool.Count)];
            }

            return new List<BossModifierId> { choiceA, choiceB };
        }

        public List<BossPhase> BuildFinalThreePhaseBoss()
        {
            return new List<BossPhase>
            {
                new()
                {
                    PhaseIndex = 1,
                    Difficulty = DifficultyTier.Diff5,
                    Stars = 4,
                    Modifiers = new List<BossModifierId> { BossModifierId.FogOfWar },
                    MistakePenalty = 1
                },
                new()
                {
                    PhaseIndex = 2,
                    Difficulty = DifficultyTier.Diff5,
                    Stars = 5,
                    Modifiers = new List<BossModifierId>
                    {
                        _random.NextDouble() < 0.5 ? BossModifierId.RenbanLines : BossModifierId.DutchWhispers
                    },
                    StartingPencilPenalty = 1,
                    MistakePenalty = 1
                },
                new()
                {
                    PhaseIndex = 3,
                    Difficulty = DifficultyTier.Diff5,
                    Stars = 5,
                    Modifiers = _random.NextDouble() < 0.5
                        ? new List<BossModifierId> { BossModifierId.GermanWhispers }
                        : new List<BossModifierId> { BossModifierId.KillerCages, BossModifierId.RatioKropki },
                    MistakePenalty = 2
                }
            };
        }

        public List<BossPhase> BuildHiddenDualModifierBoss()
        {
            return new List<BossPhase>
            {
                new()
                {
                    PhaseIndex = 1,
                    Difficulty = DifficultyTier.Diff5,
                    Stars = 5,
                    Modifiers = new List<BossModifierId> { BossModifierId.FogOfWar, BossModifierId.ParityLines },
                    MistakePenalty = 2
                },
                new()
                {
                    PhaseIndex = 2,
                    Difficulty = DifficultyTier.Diff5,
                    Stars = 5,
                    Modifiers = new List<BossModifierId> { BossModifierId.RenbanLines, BossModifierId.KillerCages },
                    StartingPencilPenalty = 1,
                    MistakePenalty = 2
                },
                new()
                {
                    PhaseIndex = 3,
                    Difficulty = DifficultyTier.Diff5,
                    Stars = 5,
                    Modifiers = new List<BossModifierId> { BossModifierId.GermanWhispers, BossModifierId.RatioKropki },
                    MistakePenalty = 3
                }
            };
        }

        private static List<BossModifierId> BuildModifierPool(int runNumber, int stars)
        {
            var maxTier = runNumber switch
            {
                <= 2 => BossModifierTier.Tier1,
                <= 4 => BossModifierTier.Tier2,
                <= 6 => BossModifierTier.Tier3,
                <= 8 => BossModifierTier.Tier4,
                _ => BossModifierTier.Tier5
            };

            if (runNumber >= 9)
            {
                maxTier = BossModifierTier.Tier5;
            }

            var pool = new List<BossModifierId>();
            foreach (var pair in ModifierData)
            {
                if (pair.Value.Tier <= maxTier)
                {
                    if (pair.Key == BossModifierId.GermanWhispers && stars < 5 && runNumber < 10)
                    {
                        continue;
                    }

                    pool.Add(pair.Key);
                }
            }

            return pool;
        }
    }
}
