using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // 0.3.0 playtest: rules behind the puzzle screen (GAP-085, GAP-091, GAP-092).
    [TestFixture]
    public sealed class PuzzleScreenRulesTests
    {
        [Test]
        [Requirement("RULE-TAP-001")]
        public void ARuleTap_MarksTheChosenCellsNeighbours_NotTheWholeGrid()
        {
            var knight = BoardViewController.RuleCells(BossModifierId.Antiknight, 2, 2, 6);
            Assert.AreEqual(1 + 8, knight.Count, "the cell and its eight knight moves");
            Assert.AreEqual(1 + 2, BoardViewController.RuleCells(BossModifierId.Antiknight, 0, 0, 6).Count, "a corner has two");
            var side = BoardViewController.RuleCells(BossModifierId.Nonconsecutive, 0, 3, 6);
            CollectionAssert.AreEquivalent(new[] { (0, 3), (1, 3), (0, 2), (0, 4) }, side);
            Assert.IsEmpty(BoardViewController.RuleCells(BossModifierId.Antiknight, -1, -1, 6), "no cell chosen: nothing");
            Assert.IsEmpty(BoardViewController.RuleCells(BossModifierId.KillerCages, 2, 2, 6), "only whole-board rules");
        }

        [Test]
        [Requirement("PLACE-RULE-001")]
        public void ADigit_IsComplete_WhenEveryCopyIsOnTheBoard()
        {
            var solution = new[,] { { 1, 2, 3, 4 }, { 3, 4, 1, 2 }, { 2, 1, 4, 3 }, { 4, 3, 2, 1 } };
            var cells = new[,] { { 1, 0, 0, 0 }, { 0, 0, 1, 0 }, { 0, 1, 0, 0 }, { 0, 0, 0, 0 } };
            var regions = new[,] { { 0, 0, 1, 1 }, { 0, 0, 1, 1 }, { 2, 2, 3, 3 }, { 2, 2, 3, 3 } };
            var board = new SudokuBoard(4, solution, cells, regions);
            Assert.IsFalse(board.IsDigitComplete(1), "three of four 1s");
            board.PlaceValue(3, 3, 1);
            Assert.IsTrue(board.IsDigitComplete(1));
            Assert.IsFalse(board.IsDigitComplete(2));
        }

        [Test]
        [Requirement("INCENSE-PUZZLE-002")]
        public void TheIncenseWalk_SellsNoPencil()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.KoiGambler, Mode = GameMode.IncenseWalk, ClassLevel = 1 }, 4242);
            run.State.CurrentGold = 500;
            Assert.IsFalse(run.CanBuyPencil(), "pencil marks are free in the walk; there is nothing to buy");
        }
    }
}
