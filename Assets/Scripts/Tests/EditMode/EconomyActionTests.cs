using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-ECON-100] [REQ: REQ-ITEM-100] [REQ: REQ-ECON-101] GAP-016 (buy pencil), GAP-015 (reward reroll),
    // SPEC-AMB-007 (Nothing gold) and GAP-027 (collected gold counts gains, not the end balance).
    [TestFixture]
    [Requirement("REQ-ECON-100", "REQ-ITEM-100", "REQ-ECON-101")]
    public sealed class EconomyActionTests
    {
        [Test]
        public void BuyPencil_CostsTwentyMoreEachTime_AndAddsThreePencil()
        {
            var run = StartPuzzle(15001, ClassId.NumberFreak);
            run.State.CurrentGold = 200;
            run.State.MaxPencil = 20;
            run.State.CurrentPencil = 1;

            Assert.AreEqual(20, run.GetPencilBuyCost());
            Assert.IsTrue(run.TryBuyPencil());
            Assert.AreEqual(4, run.State.CurrentPencil);
            Assert.AreEqual(180, run.State.CurrentGold);
            Assert.AreEqual(40, run.GetPencilBuyCost());
            Assert.IsTrue(run.TryBuyPencil());
            Assert.AreEqual(140, run.State.CurrentGold);
            Assert.AreEqual(60, run.GetPencilBuyCost());
        }

        [Test]
        public void BuyPencil_IsRefused_WithoutGold_AtFullPencil_AndForGardenMonk()
        {
            var poor = StartPuzzle(15002, ClassId.NumberFreak);
            poor.State.CurrentGold = 19;
            poor.State.CurrentPencil = 0;
            Assert.IsFalse(poor.TryBuyPencil());
            Assert.AreEqual(19, poor.State.CurrentGold);

            var full = StartPuzzle(15003, ClassId.NumberFreak);
            full.State.CurrentGold = 100;
            full.State.CurrentPencil = full.State.MaxPencil;
            Assert.IsFalse(full.TryBuyPencil(), "nothing to fill");

            var monk = StartPuzzle(15004, ClassId.GardenMonk);
            monk.State.CurrentGold = 100;
            monk.State.CurrentPencil = 0;
            Assert.IsFalse(monk.TryBuyPencil(), "Garden Monk cannot buy pencil mid-level (GDS §6)");
        }

        [Test]
        public void RewardReroll_UsesTokensFirst_ThenGold_AndKeepsNothingSlots()
        {
            var run = StartPuzzle(15005, ClassId.NumberFreak);
            var slots = run.BuildItemRewardSlots();
            Assume.That(slots.Any(s => s != null));
            var nothingAt = Enumerable.Range(0, slots.Count).Where(i => slots[i] == null).ToList();
            run.State.RerollTokens = 1;
            run.State.CurrentGold = 100;

            Assert.IsNotNull(run.RerollRewardSlots());
            Assert.AreEqual(0, run.State.RerollTokens, "token first");
            Assert.AreEqual(100, run.State.CurrentGold);

            Assert.AreEqual(40, run.GetRewardRerollCost(), "20 + 20 × rerolls this run");
            var rerolled = run.RerollRewardSlots();
            Assert.IsNotNull(rerolled);
            Assert.AreEqual(60, run.State.CurrentGold);
            foreach (var i in nothingAt)
                Assert.IsNull(rerolled[i], "Nothing slots stay locked");
            Assert.AreEqual(slots.Count, rerolled.Count);

            run.State.CurrentGold = 0;
            Assert.IsNull(run.RerollRewardSlots(), "no tokens and not enough gold");
        }

        [Test]
        public void NothingReward_GrantsFiveGoldPerFloor_AndCountsAsCollected()
        {
            var run = StartPuzzle(15006, ClassId.NumberFreak);
            run.AdvanceToNextFloor();
            run.AdvanceToNextFloor();
            var gold = run.State.CurrentGold;
            var collected = run.State.GoldCollectedThisRun;

            Assert.AreEqual(15, run.TakeNothingReward(), "floor 3: 5 × 3");
            Assert.AreEqual(gold + 15, run.State.CurrentGold);
            Assert.AreEqual(collected + 15, run.State.GoldCollectedThisRun);
        }

        [Test]
        public void CollectedGold_CountsGains_EvenAfterSpending()
        {
            var host = new UnityEngine.GameObject("EconomyRunResultHost");
            try
            {
                var map = host.AddComponent<UI.RunMapController>();
                var run = StartPuzzle(15007, ClassId.NumberFreak);
                map.BindRun(run, new SaveFileService(SaveFileService.MaxSlots - 1));
                run.State.GainGold(100);
                run.State.CurrentGold -= 60;     // e.g. a shop purchase

                var result = map.BuildRunResult(false, 0, 1);

                Assert.AreEqual(run.State.GoldCollectedThisRun, result.GoldEarned);
                Assert.GreaterOrEqual(result.GoldEarned, 100);
                Assert.Greater(result.GoldEarned, run.State.CurrentGold, "spent gold still counts as collected");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static RunDirector StartPuzzle(int seed, ClassId classId)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = classId, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 2, Seed = seed,
                MissingPercent = StarDensityService.MissingPercentForStars(2) });
            Assert.NotNull(run.CurrentBoard);
            return run;
        }
    }
}
