using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class BalanceBudgetContractTests
    {
        [Test]
        public void RewardBudgets_IncreaseWithBoardSizeAndNeverDropWithStars()
        {
            var previousGoldBase = 0;
            var previousXpBase = 0;
            for (var size = 5; size <= 9; size++)
            {
                var goldBase = GoldTable.BaseGoldForBoardSize(size);
                var xpBase = XpTable.BaseXpForBoardSize(size);
                Assert.GreaterOrEqual(goldBase, previousGoldBase, $"Gold base dropped at {size}x{size}.");
                Assert.GreaterOrEqual(xpBase, previousXpBase, $"XP base dropped at {size}x{size}.");
                previousGoldBase = goldBase;
                previousXpBase = xpBase;

                var previousGold = 0;
                var previousXp = 0;
                for (var stars = 1; stars <= 6; stars++)
                {
                    var gold = GoldTable.CalculatePuzzleGold(size, stars);
                    var xp = XpTable.CalculateTileXp(size, stars, activeModCount: 0, isBoss: false, perfectSolve: false);
                    Assert.GreaterOrEqual(gold, previousGold, $"Gold dropped at {size}x{size}, {stars} stars.");
                    Assert.GreaterOrEqual(xp, previousXp, $"XP dropped at {size}x{size}, {stars} stars.");
                    previousGold = gold;
                    previousXp = xp;
                }
            }
        }

        [Test]
        public void RewardSlotBudgets_ImproveWithStarDifficulty()
        {
            var previousSlots = 0;
            var previousNothingChance = 1.0f;
            var previousMissingPercent = 0.0f;

            for (var stars = 1; stars <= 6; stars++)
            {
                var slots = ItemService.GetSlotCount(stars);
                var nothingChance = ItemService.GetNothingChance(stars);
                var missingPercent = StarDensityService.MissingPercentForStars(stars);

                Assert.GreaterOrEqual(slots, previousSlots, $"Reward slots dropped at {stars} stars.");
                Assert.LessOrEqual(nothingChance, previousNothingChance, $"Nothing chance increased at {stars} stars.");
                Assert.Greater(missingPercent, previousMissingPercent, $"Missing-cell percent did not increase at {stars} stars.");

                previousSlots = slots;
                previousNothingChance = nothingChance;
                previousMissingPercent = missingPercent;
            }
        }

        [Test]
        public void HarmonyDifficultyBudgets_AreMonotonicAcrossLevels()
        {
            var previous = HarmonyDifficultyService.BuildConfig(0);
            AssertWeightBudget(previous.GridSizeWeights, "H0 grid");
            AssertWeightBudget(previous.StarWeights, "H0 stars");

            for (var level = 1; level <= 10; level++)
            {
                var current = HarmonyDifficultyService.BuildConfig(level);
                AssertWeightBudget(current.GridSizeWeights, $"H{level} grid");
                AssertWeightBudget(current.StarWeights, $"H{level} stars");

                Assert.LessOrEqual(current.GoldMultiplier, previous.GoldMultiplier, $"Gold multiplier eased at H{level}.");
                Assert.GreaterOrEqual(current.ShopSurcharge, previous.ShopSurcharge, $"Shop surcharge eased at H{level}.");
                Assert.GreaterOrEqual(current.MistakeHpCost, previous.MistakeHpCost, $"Mistake HP cost eased at H{level}.");
                Assert.GreaterOrEqual(current.NothingChanceBonus, previous.NothingChanceBonus, $"Nothing chance bonus eased at H{level}.");
                Assert.LessOrEqual(current.PositiveEffectMult, previous.PositiveEffectMult, $"Positive floor effect chance eased at H{level}.");
                Assert.GreaterOrEqual(current.XpMultiplier, previous.XpMultiplier, $"XP multiplier dropped at H{level}.");

                previous = current;
            }
        }

        [Test]
        public void BossBudgets_KeepOneModifierExclusionAcrossFloorsAndHarmony()
        {
            for (var floor = 0; floor <= 4; floor++)
            for (var harmonyBonus = 0; harmonyBonus <= 3; harmonyBonus++)
            {
                BossService.GetBossModifierCounts(floor, out var shown, out var picks, harmonyBonus);

                Assert.AreEqual(1, shown - picks, $"Boss gate exclusion count changed on floor {floor}, bonus {harmonyBonus}.");
                Assert.GreaterOrEqual(shown, 2);
                Assert.GreaterOrEqual(picks, 1);
            }

            Assert.AreEqual(BossModifierIntensity.Low, BossService.IntensityForRunNumber(1));
            Assert.AreEqual(BossModifierIntensity.Low, BossService.IntensityForRunNumber(2));
            Assert.AreEqual(BossModifierIntensity.Medium, BossService.IntensityForRunNumber(3));
            Assert.AreEqual(BossModifierIntensity.High, BossService.IntensityForRunNumber(6));
            Assert.AreEqual(BossModifierIntensity.VeryHigh, BossService.IntensityForRunNumber(9));
        }

        [Test]
        public void UnsupportedAntiBishop_IsExcludedFromRandomModifierRolls()
        {
            var method = typeof(BossService).GetMethod(
                "BuildEligiblePool",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var service = new BossService(seed: 33);
            var sizeSix = (List<BossModifierId>)method.Invoke(service, new object[] { null, 6 });
            var sizeSeven = (List<BossModifierId>)method.Invoke(service, new object[] { null, 7 });

            Assert.IsFalse(BossService.IsSupportedForGeneratedRun(BossModifierId.AntiBishop));
            CollectionAssert.DoesNotContain(sizeSix, BossModifierId.AntiBishop);
            CollectionAssert.DoesNotContain(sizeSeven, BossModifierId.AntiBishop);
        }

        [Test]
        public void DistanceGe2_IsNotRolledBelowItsSupportedSixBySixMinimum()
        {
            var method = typeof(BossService).GetMethod(
                "BuildEligiblePool",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var service = new BossService(seed: 34);
            var sizeFive = (List<BossModifierId>)method.Invoke(service, new object[] { null, 5 });
            var sizeSix = (List<BossModifierId>)method.Invoke(service, new object[] { null, 6 });

            CollectionAssert.DoesNotContain(sizeFive, BossModifierId.DistanceGe2);
            CollectionAssert.Contains(sizeSix, BossModifierId.DistanceGe2);
        }

        [Test]
        public void RatioKropkiAndDistanceGe2_AreNotRolledTogether()
        {
            Assert.IsFalse(BossService.CanCombineModifiers(
                new List<BossModifierId> { BossModifierId.RatioKropki },
                BossModifierId.DistanceGe2));
            Assert.IsFalse(BossService.CanCombineModifiers(
                new List<BossModifierId> { BossModifierId.DistanceGe2 },
                BossModifierId.RatioKropki));

            for (var seed = 0; seed < 200; seed++)
            {
                var service = new BossService(seed);
                var floorModifiers = service.RollFloorModifiers(floor: 4, minBoardSize: 9, harmonyLevel: 0);
                AssertNoUnstableKropkiDistancePair(floorModifiers, $"floor seed {seed}");

                var bossOptions = service.RollBossChoices(
                    floor: 4,
                    activeFloorModifiers: new List<BossModifierId>(),
                    minBoardSize: 9,
                    harmonyLevel: 0);
                AssertNoUnstableKropkiDistancePair(bossOptions, $"boss seed {seed}");

                var againstDistance = service.RollBossChoices(
                    floor: 4,
                    activeFloorModifiers: new List<BossModifierId> { BossModifierId.DistanceGe2 },
                    minBoardSize: 9,
                    harmonyLevel: 0);
                CollectionAssert.DoesNotContain(
                    againstDistance,
                    BossModifierId.RatioKropki,
                    $"boss options reintroduced the excluded pair for seed {seed}.");
            }
        }

        private static void AssertWeightBudget(float[] weights, string label)
        {
            Assert.NotNull(weights, label);
            Assert.IsTrue(weights.All(weight => weight >= 0f), label);
            Assert.AreEqual(100f, weights.Sum(), 0.001f, label);
        }

        private static void AssertNoUnstableKropkiDistancePair(List<BossModifierId> modifiers, string context)
        {
            Assert.IsFalse(
                modifiers.Contains(BossModifierId.RatioKropki)
                && modifiers.Contains(BossModifierId.DistanceGe2),
                $"RatioKropki and DistanceGe2 were rolled together for {context}.");
        }
    }
}
