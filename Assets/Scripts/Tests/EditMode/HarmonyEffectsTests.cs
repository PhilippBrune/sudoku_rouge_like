using System;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-HARM-100] [REQ: REQ-HARM-101] GAP-008: every HarmonyConfig field reaches its consumer.
    // One H0-vs-HN effect test per field, plus the boss-gate invariant (shown − picks = 1) for H0–H10 × floors 1–5.
    [TestFixture]
    [Requirement("REQ-HARM-100", "REQ-HARM-101")]
    public sealed class HarmonyEffectsTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [Test]
        [Requirement("HARMONY-BOSS-002")]
        public void BossGate_ShowsExactlyOneMoreOptionThanPicks_ForEveryLevelAndFloor()
        {
            for (var harmony = 0; harmony <= 10; harmony++)
            {
                var run = StartRun(7700 + harmony, harmony);
                var bonus = HarmonyDifficultyService.GetBossOptionsBonus(harmony);
                for (var floor = 0; floor < 5; floor++)
                {
                    if (floor > 0) run.AdvanceToNextFloor();
                    run.GetBossModifierCounts(out var shown, out var picks);
                    Assert.AreEqual(1, shown - picks, $"H{harmony} floor {floor + 1}");
                    Assert.AreEqual(floor + 2 + bonus, shown, $"H{harmony} floor {floor + 1}: options shown");
                    Assert.AreEqual(shown, run.RollBossModifierChoices().Count, $"H{harmony} floor {floor + 1}: rolled choices");
                }
            }
        }

        [Test]
        public void BossOptionsAndFloorBonus_MatchSpecTable()
        {
            int[] expectedOptionsBonus = { 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3 };
            for (var h = 0; h <= 10; h++)
                Assert.AreEqual(expectedOptionsBonus[h], HarmonyDifficultyService.GetBossOptionsBonus(h), $"H{h}");

            // H3–H5: +1 on floors 2–4 only; H6–H8 +1 everywhere; H9–H10 +2 everywhere.
            CollectionAssert.AreEqual(new[] { 0, 1, 1, 1, 0 }, Enumerable.Range(0, 5).Select(f => HarmonyDifficultyService.GetBonusFloorModifiers(4, f)));
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 1 }, Enumerable.Range(0, 5).Select(f => HarmonyDifficultyService.GetBonusFloorModifiers(7, f)));
            CollectionAssert.AreEqual(new[] { 2, 2, 2, 2, 2 }, Enumerable.Range(0, 5).Select(f => HarmonyDifficultyService.GetBonusFloorModifiers(10, f)));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0 }, Enumerable.Range(0, 5).Select(f => HarmonyDifficultyService.GetBonusFloorModifiers(2, f)));
        }

        [TestCase(0, 1)]
        [TestCase(5, 2)]
        [TestCase(9, 3)]
        [Requirement("HARMONY-SURV-002")]
        public void MistakeCostsTheHarmonyHpCost(int harmony, int expectedLoss)
        {
            var run = StartPuzzle(7801, harmony);
            var hp = run.State.CurrentHP;
            var (r, c, v) = FindWrongDigit(run);
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(r, c, v));
            Assert.AreEqual(expectedLoss, hp - run.State.CurrentHP);
        }

        [Test]
        [Requirement("HARMONY-FLAG-003")]
        public void FromH7_MistakesAlsoDrainOnePencil()
        {
            foreach (var (harmony, drain) in new[] { (6, 0), (7, 1) })
            {
                var run = StartPuzzle(7802, harmony);
                var pencil = run.State.CurrentPencil;
                var (r, c, v) = FindWrongDigit(run);
                run.PlaceNumber(r, c, v);
                Assert.AreEqual(drain, pencil - run.State.CurrentPencil, $"H{harmony}");
            }
        }

        [Test]
        [Requirement("HARMONY-ECON-001")]
        public void PuzzleGold_IsScaledByTheHarmonyGoldMultiplier()
        {
            var baseline = GoldForSolvedPuzzle(0);
            var h10 = GoldForSolvedPuzzle(10);
            Assert.Greater(baseline, 0);
            Assert.AreEqual((int)Math.Round(baseline * 0.60f), h10);
        }

        [Test]
        [Requirement("HARMONY-FLAG-004")]
        public void H10_HalvesTheRestHeal()
        {
            var h9 = StartRun(7803, 9);
            var h10 = StartRun(7803, 10);
            var full = Math.Max(1, h10.State.MaxHP / 4);   // ECON-BAL-004: a quarter
            Assert.AreEqual(Math.Max(1, h9.State.MaxHP / 4), h9.GetRestHealAmount());
            Assert.AreEqual((full + 1) / 2, h10.GetRestHealAmount());
        }

        [Test]
        [Requirement("HARMONY-FLAG-005")]
        public void FromH9_CursedPuzzlesLoseTheirBonus()
        {
            Assert.AreEqual(1.5f, StartRun(7804, 8).BuildCursedLevelConfig(3).CursedGoldMult);
            var h9 = StartRun(7804, 9).BuildCursedLevelConfig(3);
            Assert.AreEqual(1f, h9.CursedGoldMult);
            Assert.AreEqual(1f, h9.CursedXpMult);
        }

        [Test]
        [Requirement("HARMONY-SURV-003")]
        public void FromH7_TheCarriedComboIsHalvedAtEachPuzzleStart()
        {
            foreach (var (harmony, expected) in new[] { (6, 10), (7, 5) })
            {
                var run = StartRun(7805, harmony);
                run.State.ComboStreak = 10;
                run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 1, Seed = 9,
                    MissingPercent = StarDensityService.MissingPercentForStars(1) });
                Assert.AreEqual(expected, run.State.ComboStreak, $"H{harmony}");
            }
        }

        [Test]
        [Requirement("HARMONY-ITEM-001")]
        public void NothingChance_RisesWithHarmonyAndNeverEmptiesAReward()
        {
            var h0 = NothingRate(0, out var h0Empty);
            var h10 = NothingRate(10, out var h10Empty);
            Assert.AreEqual(0, h0Empty + h10Empty, "a reward always keeps at least one item");
            Assert.That(h10 - h0, Is.InRange(0.05, 0.25), $"H0 {h0:P0}, H10 {h10:P0}");
        }

        [Test]
        public void GridWeights_FavourLargerBoardsAtHighHarmony()
        {
            var h0 = ShareOfSize(0, 3, 9);
            var h10 = ShareOfSize(10, 3, 9);
            Assert.Less(h0, 0.55, "H0 on floor 4: 9×9 weight 10 vs 8×8 weight 15");
            Assert.Greater(h10, 0.60, "H10 on floor 4: 9×9 weight 75 vs 8×8 weight 25");
        }

        [Test]
        [Requirement("REQ-RUN-106")]
        public void StarVariance_UsesHarmonyWeights_AndRespectsTheEarlyFloorCap()
        {
            Assert.LessOrEqual(MaxStars(0, 2), 4, "H0 star weights have no 5★ or 6★");
            Assert.GreaterOrEqual(MaxStars(10, 2), 5, "H10 star weights reach 5★/6★ on floor 3");
            Assert.LessOrEqual(MaxStars(10, 0), 2, "floor 1 stays within one star of its default (GAP-033)");
            Assert.LessOrEqual(MaxStars(10, 1), 3, "floor 2 stays within one star of its default");
        }

        [Test]
        [Requirement("HARMONY-XP-001")]
        public void XpMultiplier_AppliesOnDefeatToo_WithoutFlatBonus()
        {
            foreach (var victory in new[] { false, true })
            {
                var save = new SaveFileService(TestSlot);
                save.Save(new SaveFileEnvelope());
                new ProfileService(save).RecordRunAndGetNewUnlocks(new RunResult
                {
                    PlayedClassId = ClassId.NumberFreak,
                    Mode = GameMode.GardenRun,
                    Victory = victory,
                    HarmonyLevel = 5,
                    XpEarned = 100
                });
                SaveFileService.FlushSharedPendingWrites();
                var xp = new ClassGardenProgressionService().GetTotalXp(new SaveFileService(TestSlot).Load().MetaProgress, ClassId.NumberFreak);
                Assert.AreEqual(150, xp, victory ? "victory" : "defeat");
            }
        }

        [Test]
        [Requirement("XP-CURVE-002", "REQ-XP-101")]
        public void XpAboveLevel40_IsDiscarded()
        {
            var meta = new MetaProgressionState();
            var garden = new ClassGardenProgressionService();
            var cap = XpTable.CumulativeXpForLevel(XpTable.MaxLevel);
            Assert.AreEqual(16860, cap);
            garden.AddXp(meta, ClassId.GardenMonk, cap - 10);
            garden.AddXp(meta, ClassId.GardenMonk, 500);
            Assert.AreEqual(cap, garden.GetTotalXp(meta, ClassId.GardenMonk));
            Assert.AreEqual(40, garden.GetLevel(meta, ClassId.GardenMonk));
        }

        // ── helpers ──

        private static RunDirector StartRun(int seed, int harmony)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = false,
                ClassLevel = 1,
                HarmonyLevel = harmony
            }, seed);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            return run;
        }

        private static RunDirector StartPuzzle(int seed, int harmony)
        {
            var run = StartRun(seed, harmony);
            run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 2, Seed = seed,
                MissingPercent = StarDensityService.MissingPercentForStars(2) });
            Assert.NotNull(run.CurrentBoard);
            return run;
        }

        private static (int r, int c, int v) FindWrongDigit(RunDirector run)
        {
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0 && !b.IsGiven(r, c))
                    return (r, c, b.Solution[r, c] % b.Size + 1);
            Assert.Fail("no empty cell");
            return default;
        }

        private static int GoldForSolvedPuzzle(int harmony)
        {
            var run = StartPuzzle(7810, harmony);
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0) run.PlaceNumber(r, c, b.Solution[r, c]);
            Assert.IsTrue(run.IsLevelComplete);
            var before = run.State.CurrentGold;
            run.CompleteLevelAndGrantRewards();
            return run.State.CurrentGold - before;
        }

        private static double NothingRate(int harmony, out int emptyRewards)
        {
            var run = StartRun(7820, harmony);
            run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 1, Seed = 3,
                MissingPercent = StarDensityService.MissingPercentForStars(1) });
            int slots = 0, nothing = 0;
            emptyRewards = 0;
            for (var depth = 0; depth < 400; depth++)
            {
                run.State.Depth = depth;
                var rolled = run.BuildItemRewardSlots();
                slots += rolled.Count;
                nothing += rolled.Count(s => s == null);
                if (rolled.Count > 0 && rolled.All(s => s == null)) emptyRewards++;
            }
            return nothing / (double)slots;
        }

        private static double ShareOfSize(int harmony, int floorIndex, int size)
        {
            var run = StartRun(7830, harmony);
            for (var f = 0; f < floorIndex; f++) run.AdvanceToNextFloor();
            var hits = 0;
            const int samples = 300;
            for (var node = 0; node < samples; node++)
                if (run.BuildLevelConfig(false, false, node).BoardSize == size) hits++;
            return hits / (double)samples;
        }

        private static int MaxStars(int harmony, int floorIndex)
        {
            var run = StartRun(7840, harmony);
            for (var f = 0; f < floorIndex; f++) run.AdvanceToNextFloor();
            var max = 0;
            for (var node = 0; node < 400; node++)
                max = Math.Max(max, run.BuildLevelConfig(false, false, node).Stars);
            return max;
        }
    }
}
