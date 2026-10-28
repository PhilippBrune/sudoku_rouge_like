using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: SHOP-RULE-001] RM-211, owner (0.3.0 playtest): from Floor 2 the market rolls the floor's rule again for
    // 100 gold, once per floor.
    [TestFixture]
    [Requirement("SHOP-RULE-001")]
    public sealed class FloorRuleRerollTests
    {
        private static RunDirector StartRun(int floorIndex, int gold)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9130);
            for (var f = 0; f < floorIndex; f++) run.AdvanceToNextFloor();
            run.State.CurrentGold = gold;
            return run;
        }

        [Test]
        public void OnFloor2_TheRuleChanges_ForOneHundredGold_OncePerFloor()
        {
            var run = StartRun(1, 150);
            var old = new System.Collections.Generic.List<BossModifierId>(run.State.ActiveFloorModifiers);
            Assume.That(old.Count, Is.GreaterThan(0), "Floor 2 has a rule");

            Assert.IsTrue(run.OffersFloorRuleReroll());
            Assert.IsTrue(run.RerollFloorRule());

            Assert.AreEqual(50, run.State.CurrentGold, "100 gold");
            Assert.AreEqual(old.Count, run.State.ActiveFloorModifiers.Count, "as many rules as before");
            Assert.IsFalse(run.State.ActiveFloorModifiers.Exists(old.Contains), "none of the old rules");
            Assert.AreEqual(-1, run.State.NodeConfigsFloor, "the floor's puzzles are built again");
            foreach (var rule in run.State.ActiveFloorModifiers)
                Assert.IsTrue(run.State.SeenBossModifiers.Contains(rule), "the new rule counts as seen");

            run.State.CurrentGold = 500;
            Assert.IsFalse(run.OffersFloorRuleReroll(), "once per floor");
            Assert.IsFalse(run.RerollFloorRule());
            Assert.AreEqual(500, run.State.CurrentGold, "a refused roll costs nothing");

            run.AdvanceToNextFloor();
            Assert.IsTrue(run.OffersFloorRuleReroll(), "the next floor offers it again");
        }

        [Test]
        public void OnFloor1_OrWithoutTheGold_NothingHappens()
        {
            var first = StartRun(0, 500);
            Assert.IsFalse(first.OffersFloorRuleReroll(), "not on Floor 1");
            Assert.IsFalse(first.RerollFloorRule());

            var poor = StartRun(1, 99);
            var old = new System.Collections.Generic.List<BossModifierId>(poor.State.ActiveFloorModifiers);
            Assert.IsFalse(poor.RerollFloorRule());
            Assert.AreEqual(99, poor.State.CurrentGold);
            CollectionAssert.AreEqual(old, poor.State.ActiveFloorModifiers);
        }

        [Test]
        public void TheRoll_IsKeptInTheSave()
        {
            var run = StartRun(1, 150);
            Assume.That(run.RerollFloorRule());
            var loaded = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State));
            Assert.AreEqual(2, loaded.FloorRuleRerolledOnFloor);
            CollectionAssert.AreEqual(run.State.ActiveFloorModifiers, loaded.ActiveFloorModifiers);
            Assert.AreEqual(0, JsonUtility.FromJson<RunState>("{}").FloorRuleRerolledOnFloor, "an older save has not rolled");
        }
    }
}
