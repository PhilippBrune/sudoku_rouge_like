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
            [BossModifierId.Nonconsecutive] = (BossModifierTier.Tier1, 0.25f),
            [BossModifierId.DutchWhispers] = (BossModifierTier.Tier2, 0.30f),
            [BossModifierId.RenbanLines] = (BossModifierTier.Tier2, 0.35f),
            [BossModifierId.RatioKropki] = (BossModifierTier.Tier2, 0.40f),
            [BossModifierId.Palindrome] = (BossModifierTier.Tier2, 0.30f),
            [BossModifierId.Antiknight] = (BossModifierTier.Tier2, 0.35f),
            [BossModifierId.KillerCages] = (BossModifierTier.Tier3, 0.50f),
            [BossModifierId.ArrowSums] = (BossModifierTier.Tier3, 0.55f),
            [BossModifierId.Thermo] = (BossModifierTier.Tier3, 0.45f),
            [BossModifierId.BetweenLines] = (BossModifierTier.Tier3, 0.45f),
            [BossModifierId.EvenOdd] = (BossModifierTier.Tier2, 0.25f),
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
            return RollBossOptions(2, null);
        }

        /// <summary>Rolls N unique modifier options, excluding any already used this run.</summary>
        public List<BossModifierId> RollBossOptions(int count, List<BossModifierId> excludeUsed)
        {
            var pool = BuildModifierPool(0, 0);
            if (excludeUsed != null)
            {
                for (var i = pool.Count - 1; i >= 0; i--)
                {
                    for (var j = 0; j < excludeUsed.Count; j++)
                    {
                        if (pool[i] == excludeUsed[j])
                        {
                            pool.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            var result = new List<BossModifierId>();
            var safeCount = Math.Min(count, pool.Count);
            for (var picked = 0; picked < safeCount; picked++)
            {
                var idx = _random.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return result;
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
            var pool = new List<BossModifierId>();
            foreach (var pair in ModifierData)
            {
                pool.Add(pair.Key);
            }

            return pool;
        }
    }
}
