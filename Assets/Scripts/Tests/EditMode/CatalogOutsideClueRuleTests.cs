using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: BOSS-MOD-006] Batch B4 rules of the modifier catalog on hand-made 9×9 boards (modifier-catalog-plan.md): clues
    // outside the grid and quadruples reject a digit only when the placed digits already rule the clue out.
    [TestFixture]
    [Requirement("BOSS-MOD-006")]
    public sealed class CatalogOutsideClueRuleTests
    {
        private static SudokuBoard Empty()
        {
            var regions = new int[9, 9];
            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
                regions[r, c] = (r / 3) * 3 + c / 3;
            return new SudokuBoard(9, new int[9, 9], new int[9, 9], regions);
        }

        private static SudokuBoard Row(params int[] firstRow)
        {
            var board = Empty();
            for (var c = 0; c < firstRow.Length; c++) board.Cells[0, c] = firstRow[c];
            return board;
        }

        private static ModifierOverlayData Clue(OutsideClueType type, OutsideClueEdge edge, int index, int value, bool fromStart = true)
        {
            var overlay = new ModifierOverlayData();
            overlay.OutsideClues.Add(new OutsideClue { Type = type, Edge = edge, Index = index, Value = value, FromStart = fromStart });
            return overlay;
        }

        [Test]
        [Requirement("BOSS-LIB-081")]
        public void Skyscrapers_CountVisibleBuildingsFromTheClue()
        {
            var rule = new OutsideClueRule(OutsideClueType.Skyscrapers);
            var three = Clue(OutsideClueType.Skyscrapers, OutsideClueEdge.Left, 0, 3);
            Assert.IsTrue(rule.IsValid(Row(2, 5), 0, 2, 3, three), "2 and 5 seen, the 9 still to come makes three");
            Assert.IsTrue(rule.IsValid(Row(2, 5), 0, 2, 9, three), "2-5-9: three seen, the 9 hides the rest");
            Assert.IsFalse(rule.IsValid(Row(2, 5), 0, 2, 8, three), "2-5-8 shows three and the 9 behind adds a fourth");
        }

        [Test]
        [Requirement("BOSS-LIB-081")]
        public void Skyscrapers_BoundsFromTheFilledFront()
        {
            var rule = new OutsideClueRule(OutsideClueType.Skyscrapers);
            var one = Clue(OutsideClueType.Skyscrapers, OutsideClueEdge.Left, 0, 1);
            Assert.IsTrue(rule.IsValid(Empty(), 0, 0, 9, one), "only a 9 in front shows one building");
            Assert.IsFalse(rule.IsValid(Empty(), 0, 0, 8, one), "the 9 behind an 8 would be seen too");
            var two = Clue(OutsideClueType.Skyscrapers, OutsideClueEdge.Right, 0, 2);
            Assert.IsFalse(rule.IsValid(Empty(), 0, 8, 9, two), "from the right, a 9 first shows only one");
            Assert.IsTrue(rule.IsValid(Empty(), 0, 8, 4, two));
            var nine = Clue(OutsideClueType.Skyscrapers, OutsideClueEdge.Top, 0, 9);
            Assert.IsFalse(rule.IsValid(Empty(), 0, 0, 2, nine), "nine buildings need 1 at the top");
        }

        [Test]
        [Requirement("BOSS-LIB-082")]
        public void Sandwich_SumsTheDigitsBetweenOneAndNine()
        {
            var rule = new OutsideClueRule(OutsideClueType.Sandwich);
            var six = Clue(OutsideClueType.Sandwich, OutsideClueEdge.Left, 0, 6);
            Assert.IsTrue(rule.IsValid(Row(1, 4, 2), 0, 3, 9, six), "1-4-2-9: 4 + 2 = 6");
            Assert.IsFalse(rule.IsValid(Row(1, 4, 3), 0, 3, 9, six), "4 + 3 = 7");
            Assert.IsTrue(rule.IsValid(Row(1, 0, 0), 0, 3, 9, six), "two open cells can still make 6");
            Assert.IsFalse(rule.IsValid(Row(1), 0, 1, 9, six), "nothing between them makes 0");
            Assert.IsTrue(rule.IsValid(Row(1), 0, 5, 3, six), "the 9 is not placed yet");
        }

        [Test]
        [Requirement("BOSS-LIB-083")]
        public void XSums_FirstDigitSaysHowManyToAdd()
        {
            var rule = new OutsideClueRule(OutsideClueType.XSums);
            var nine = Clue(OutsideClueType.XSums, OutsideClueEdge.Top, 0, 9);
            var board = Empty();
            board.Cells[0, 0] = 3;
            board.Cells[1, 0] = 1;
            Assert.IsTrue(rule.IsValid(board, 2, 0, 5, nine), "3 + 1 + 5 = 9");
            Assert.IsFalse(rule.IsValid(board, 2, 0, 6, nine));
            Assert.IsTrue(rule.IsValid(board, 3, 0, 9, nine), "the fourth cell is not counted");
            Assert.IsFalse(rule.IsValid(Empty(), 0, 0, 9, nine), "nine digits add up to 45");
            Assert.IsTrue(rule.IsValid(Empty(), 0, 0, 2, nine), "2 + 7 = 9");
        }

        [Test]
        [Requirement("BOSS-LIB-084")]
        public void LittleKiller_AddsTheDiagonal()
        {
            var rule = new OutsideClueRule(OutsideClueType.LittleKiller);
            // From the top edge at column 6 down to the right: (0,6), (1,7), (2,8).
            var o = Clue(OutsideClueType.LittleKiller, OutsideClueEdge.DiagonalDownRight, 6, 12);
            var board = Empty();
            board.Cells[0, 6] = 3;
            board.Cells[1, 7] = 4;
            Assert.IsTrue(rule.IsValid(board, 2, 8, 5, o));
            Assert.IsFalse(rule.IsValid(board, 2, 8, 6, o));
            Assert.IsTrue(rule.IsValid(board, 3, 8, 9, o), "not on the diagonal");
            // Reads agrees with Cells for every diagonal start on both edges.
            foreach (var edge in new[] { OutsideClueEdge.DiagonalDownRight, OutsideClueEdge.DiagonalDownLeft })
            for (var index = 0; index < 9; index++)
            foreach (var top in new[] { true, false })
            {
                var clue = new OutsideClue { Edge = edge, Index = index, FromStart = top };
                var cells = OutsideClueGeometry.Cells(clue, 9);
                for (var r = 0; r < 9; r++)
                for (var c = 0; c < 9; c++)
                    Assert.AreEqual(cells.Contains(new CellCoord(r, c)), OutsideClueGeometry.Reads(clue, r, c, 9), $"{edge} {index} {top} ({r},{c})");
            }
            var fromLeft = Clue(OutsideClueType.LittleKiller, OutsideClueEdge.DiagonalDownRight, 7, 3, fromStart: false);
            Assert.AreEqual(2, OutsideClueGeometry.Cells(fromLeft.OutsideClues[0], 9).Count, "(7,0) and (8,1)");
            Assert.IsFalse(rule.IsValid(Empty(), 7, 0, 3, fromLeft), "3 + at least 1 is more than 3");
        }

        [Test]
        [Requirement("BOSS-LIB-079")]
        public void Quadruples_ListedDigitsNeedRoomAroundTheCorner()
        {
            var o = new ModifierOverlayData();
            o.Quadruples.Add(new QuadrupleConstraint { TopLeft = new CellCoord(0, 0), Digits = { 1, 2, 3 } });
            var rule = new QuadrupleRule();
            var board = Empty();
            board.Cells[0, 0] = 1;
            board.Cells[0, 1] = 5;
            Assert.IsFalse(rule.IsValid(board, 1, 0, 7, o), "2 and 3 still need a cell, only one is left");
            Assert.IsTrue(rule.IsValid(board, 1, 0, 2, o));
            Assert.IsTrue(rule.IsValid(board, 2, 2, 7, o), "outside the four cells");
        }
    }
}
