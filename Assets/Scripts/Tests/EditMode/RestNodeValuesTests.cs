using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // SPEC-CONF-017 / GAP-024 (2026-10-07): the Rest choices use the values documented in GDS §Rest Node — Choice.
    [TestFixture]
    [Requirement("REQ-RUN-104", "CURSE-INT-010")]
    public sealed class RestNodeValuesTests
    {
        private static RunDirector StartRun()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 6104);
            return run;
        }

        [Test]
        public void Heal_RestoresAThirdOfMaxHp_AtLeastOne_CappedAtMax()
        {
            var run = StartRun();
            run.State.MaxHP = 9;
            run.State.CurrentHP = 2;
            Assert.AreEqual(3, run.GetRestHealAmount());
            run.AcceptRestHeal();
            Assert.AreEqual(5, run.State.CurrentHP);

            run.State.MaxHP = 2;
            run.State.CurrentHP = 2;
            Assert.AreEqual(1, run.GetRestHealAmount(), "never below 1");
            run.AcceptRestHeal();
            Assert.AreEqual(2, run.State.CurrentHP, "capped at max HP");
        }

        [Test]
        public void Pencil_RestoresFour_CappedAtMax()
        {
            var run = StartRun();
            run.State.MaxPencil = 10;
            run.State.CurrentPencil = 1;
            run.AcceptRestPencilBoost();
            Assert.AreEqual(5, run.State.CurrentPencil);
            run.State.CurrentPencil = 8;
            run.AcceptRestPencilBoost();
            Assert.AreEqual(10, run.State.CurrentPencil);
        }

        [Test]
        public void Reroll_AddsOneToken_AndCleanseRemovesTheOldestCurse()
        {
            var run = StartRun();
            var tokens = run.State.RerollTokens;
            run.AcceptRestRerollShop();
            Assert.AreEqual(tokens + 1, run.State.RerollTokens);

            run.State.ActiveCurseIds.Clear();
            Assert.IsFalse(run.AcceptRestCurseRemoval(), "nothing to cleanse");
            CurseService.ApplyCurse(run.State, "bad_luck");
            CurseService.ApplyCurse(run.State, "price_gouge");
            Assert.IsTrue(run.AcceptRestCurseRemoval());
            CollectionAssert.AreEqual(new[] { "price_gouge" }, run.State.ActiveCurseIds);
        }
    }
}
