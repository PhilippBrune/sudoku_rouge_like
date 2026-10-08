using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // SPEC-MAC-003 (2026-10-07): acceptance tests for the curse effects listed in CurseSystem §Curse Pool.
    [TestFixture]
    [Requirement("CURSE-STACK-002", "CURSE-REMOVE-001", "CURSE-INT-002", "CURSE-INT-003", "CURSE-INT-004",
        "CURSE-INT-005", "CURSE-INT-008", "CURSE-POOL-007", "CURSE-POOL-012")]
    public sealed class CurseEffectTests
    {
        [Test]
        public void ACurse_IsAppliedOnce_AndCleanseRemovesTheOldest()
        {
            var state = new RunState();
            Assert.IsTrue(CurseService.ApplyCurse(state, "bad_luck"));
            Assert.IsFalse(CurseService.ApplyCurse(state, "bad_luck"), "no duplicates");
            CurseService.ApplyCurse(state, "price_gouge");

            Assert.IsTrue(CurseService.TryRemoveCurse(state));
            CollectionAssert.AreEqual(new[] { "price_gouge" }, state.ActiveCurseIds);
        }

        [Test]
        public void CrumblingFocus_AndWeightOfStone_LowerTheMaximumsAtOnce()
        {
            var state = new RunState { MaxPencil = 10, CurrentPencil = 10, MaxHP = 2, CurrentHP = 2 };
            CurseService.ApplyCurse(state, "crumbling_focus");
            Assert.AreEqual(6, state.MaxPencil);
            Assert.AreEqual(6, state.CurrentPencil);

            CurseService.ApplyCurse(state, "weight_of_stone");
            Assert.AreEqual(1, state.MaxHP, "never below 1");
            Assert.AreEqual(1, state.CurrentHP);
        }

        [Test]
        public void ShopAndRewardCurses_ChangePricesSlotsAndNothingChance()
        {
            var state = new RunState();
            Assert.AreEqual(1f, CurseService.GetShopPriceMultiplier(state));
            Assert.AreEqual(0, CurseService.GetSlotPenalty(state));
            Assert.AreEqual(0f, CurseService.GetNothingChanceBonus(state));

            CurseService.ApplyCurse(state, "price_gouge");
            CurseService.ApplyCurse(state, "misfortune");
            CurseService.ApplyCurse(state, "bad_luck");
            Assert.AreEqual(1.35f, CurseService.GetShopPriceMultiplier(state), 0.0001f);
            Assert.AreEqual(-1, CurseService.GetSlotPenalty(state));
            Assert.AreEqual(0.15f, CurseService.GetNothingChanceBonus(state), 0.0001f);
        }

        [Test]
        public void TremblingHand_CostsOneHpOnTheFirstPlacementOfEachPuzzle()
        {
            var state = new RunState();
            CurseService.ApplyCurse(state, "trembling_hand");
            Assert.AreEqual(1, CurseService.GetTremblingHandDamage(state));
            Assert.AreEqual(0, CurseService.GetTremblingHandDamage(state));
            CurseService.OnPuzzleStart(state);
            Assert.AreEqual(1, CurseService.GetTremblingHandDamage(state), "fires again in the next puzzle");
        }

        [Test]
        public void FrayingThread_LowersMaxHpEachFloor_NeverBelowOne()
        {
            var state = new RunState { MaxHP = 2, CurrentHP = 2 };
            CurseService.ApplyCurse(state, "fraying_thread");
            CurseService.OnFloorStart(state);
            Assert.AreEqual(1, state.MaxHP);
            CurseService.OnFloorStart(state);
            Assert.AreEqual(1, state.MaxHP);
        }

        [Test]
        public void RestlessInventory_DrainsOneChargeAfterAPuzzle()
        {
            var state = new RunState();
            state.HeldItems.Add(new ItemInstance { Id = "a", Type = ItemType.InkWell, Charges = 1 });
            CurseService.ApplyCurse(state, "restless_inventory");

            new CurseService(1).OnPuzzleComplete(state, perfect: false);

            Assert.AreEqual(0, state.HeldItems.Count, "an item at 0 charges is discarded");
        }

        [Test]
        public void PhantomPain_MakesAWrongDigitCostTwoHp()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9601);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            CurseService.ApplyCurse(run.State, "phantom_pain");
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            run.StartLevel(run.BuildLevelConfig(false, false, node.Index));
            var board = run.CurrentBoard;
            var (r, c) = Enumerable.Range(0, board.Size * board.Size)
                .Select(i => (i / board.Size, i % board.Size)).First(p => board.Cells[p.Item1, p.Item2] == 0);
            var hp = run.State.CurrentHP;

            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(r, c, board.Solution[r, c] % board.Size + 1));
            Assert.AreEqual(hp - 2, run.State.CurrentHP);
        }
    }
}
