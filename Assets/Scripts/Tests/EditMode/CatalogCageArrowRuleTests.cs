using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: BOSS-MOD-006] Cage, arrow and sum-line rules of the modifier catalog on hand-made 9×9 boards: a digit is
    // rejected only when it breaks the rule with the digits already placed (modifier-catalog-plan.md, B3 and B6).
    [TestFixture]
    [Requirement("BOSS-MOD-006")]
    public sealed class CatalogCageArrowRuleTests
    {
        private static SudokuBoard Empty(int size = 9)
        {
            var regions = new int[size, size];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                regions[r, c] = size == 9 ? (r / 3) * 3 + c / 3 : r;
            return new SudokuBoard(size, new int[size, size], new int[size, size], regions);
        }

        private static SudokuBoard With(params int[] firstRow)
        {
            var board = Empty();
            for (var c = 0; c < firstRow.Length; c++) board.Cells[0, c] = firstRow[c];
            return board;
        }

        private static ModifierOverlayData Cage(KillerCageType type, int label, params (int r, int c)[] cells)
        {
            var cage = new KillerCage { CageType = type, Sum = label };
            foreach (var (r, c) in cells) cage.Cells.Add(new CellCoord(r, c));
            var overlay = new ModifierOverlayData();
            overlay.KillerCages.Add(cage);
            return overlay;
        }

        private static ModifierOverlayData Arrow(ArrowType type, (int r, int c) circle, (int r, int c) second, params (int r, int c)[] path)
        {
            var arrow = new ArrowConstraint
                { ArrowType = type, CircleCell = new CellCoord(circle.r, circle.c), SecondCircle = new CellCoord(second.r, second.c) };
            foreach (var (r, c) in path) arrow.ArrowCells.Add(new CellCoord(r, c));
            var overlay = new ModifierOverlayData();
            overlay.Arrows.Add(arrow);
            return overlay;
        }

        private static ModifierOverlayData FirstRowLine(LineType type, int from, int to, int lineValue = 0)
        {
            var line = new ModifierLine { Type = type, LineValue = lineValue };
            for (var c = from; c <= to; c++) line.Cells.Add(new CellCoord(0, c));
            var overlay = new ModifierOverlayData();
            overlay.Lines.Add(line);
            return overlay;
        }

        [Test]
        [Requirement("BOSS-LIB-045")]
        public void RegionSumLine_EveryPieceMatchesTheFinishedOne()
        {
            // Columns 1-2 lie in the first box, 3-4 in the second: two pieces.
            var o = FirstRowLine(LineType.RegionSumLine, 1, 4);
            var rule = new RegionSumLineRule(LineType.RegionSumLine);
            Assert.IsTrue(rule.IsValid(With(0, 4, 5), 0, 3, 6, o), "6 + something can still make 9");
            Assert.IsFalse(rule.IsValid(With(0, 4, 5), 0, 3, 9, o), "9 + at least 1 overshoots 9");
            Assert.IsTrue(rule.IsValid(With(0, 4, 5, 6), 0, 4, 3, o));
            Assert.IsFalse(rule.IsValid(With(0, 4, 5, 6), 0, 4, 4, o), "10 against 9");
            Assert.IsTrue(rule.IsValid(Empty(), 0, 1, 9, o), "nothing finished yet");
        }

        [Test]
        [Requirement("BOSS-LIB-064")]
        public void HiddenKiller_ChecksRepeatsOnly()
        {
            var o = Cage(KillerCageType.HiddenSum, 11, (0, 0), (0, 1));
            var rule = new TypedCageRule(KillerCageType.HiddenSum);
            Assert.IsFalse(rule.IsValid(With(5), 0, 1, 5, o));
            Assert.IsTrue(rule.IsValid(With(5), 0, 1, 9, o), "the hidden sum (11) is never checked, so it cannot leak");
        }

        [Test]
        [Requirement("BOSS-LIB-065")]
        public void ProductCage_ProductSoFarMustDivideTheLabel()
        {
            var o = Cage(KillerCageType.Product, 24, (0, 0), (0, 1), (1, 0));
            var rule = new TypedCageRule(KillerCageType.Product);
            Assert.IsTrue(rule.IsValid(With(2), 0, 1, 3, o));
            Assert.IsFalse(rule.IsValid(With(2), 0, 1, 5, o), "10 does not divide 24");
            var board = With(2, 3);
            Assert.IsTrue(rule.IsValid(board, 1, 0, 4, o));
            Assert.IsFalse(rule.IsValid(board, 1, 0, 1, o), "6 ≠ 24 when the cage is full");
        }

        [Test]
        [Requirement("BOSS-LIB-066")]
        public void DifferenceCage_TwoCellsDifferByTheLabel()
        {
            var o = Cage(KillerCageType.Difference, 3, (0, 0), (0, 1));
            var rule = new TypedCageRule(KillerCageType.Difference);
            Assert.IsTrue(rule.IsValid(With(2), 0, 1, 5, o));
            Assert.IsFalse(rule.IsValid(With(2), 0, 1, 4, o));
            Assert.IsTrue(rule.IsValid(Empty(), 0, 1, 8, o), "5 is three below 8");
            var small = Empty(5);
            Assert.IsFalse(rule.IsValid(small, 0, 1, 3, o), "on 5×5 no digit is three away from 3");
        }

        [Test]
        [Requirement("BOSS-LIB-069")]
        public void RenbanCage_DigitsSpanNoMoreThanTheCage()
        {
            var o = Cage(KillerCageType.RenbanSet, 0, (0, 0), (0, 1), (0, 2));
            var rule = new TypedCageRule(KillerCageType.RenbanSet);
            Assert.IsTrue(rule.IsValid(With(4), 0, 1, 6, o));
            Assert.IsFalse(rule.IsValid(With(4), 0, 1, 7, o), "4 to 7 needs four cells");
            Assert.IsFalse(rule.IsValid(With(4), 0, 1, 4, o), "repeat");
        }

        [Test]
        [Requirement("BOSS-LIB-070")]
        public void ArrowAverage_CircleIsTheMeanOfThePath()
        {
            var o = Arrow(ArrowType.Average, (0, 0), (0, 0), (0, 1), (0, 2));
            var rule = new TypedArrowRule(ArrowType.Average);
            Assert.IsTrue(rule.IsValid(With(4, 3), 0, 2, 5, o), "(3 + 5) / 2 = 4");
            Assert.IsFalse(rule.IsValid(With(4, 3), 0, 2, 6, o));
            Assert.IsFalse(rule.IsValid(With(0, 3, 6), 0, 0, 5, o), "the path averages 4.5, not 5");
            Assert.IsFalse(rule.IsValid(With(0, 3), 0, 2, 6, o), "circle still empty: 9 / 2 is no digit");
            Assert.IsTrue(rule.IsValid(With(0, 3), 0, 2, 7, o), "circle still empty: 10 / 2 = 5");
            Assert.IsFalse(rule.IsValid(With(1), 0, 1, 3, o), "a circle of 1 needs a path of 1s");
            Assert.IsTrue(rule.IsValid(Empty(), 0, 1, 9, o));
        }

        [Test]
        [Requirement("BOSS-LIB-073")]
        public void DoubleArrow_PathEqualsBothCircles()
        {
            var o = Arrow(ArrowType.DoubleEndpoint, (0, 0), (0, 3), (0, 1), (0, 2));
            var rule = new TypedArrowRule(ArrowType.DoubleEndpoint);
            Assert.IsTrue(rule.IsValid(With(2, 4, 0, 7), 0, 2, 5, o), "4 + 5 = 2 + 7");
            Assert.IsFalse(rule.IsValid(With(2, 4, 0, 7), 0, 2, 6, o));
            Assert.IsFalse(rule.IsValid(With(1, 9, 0, 1), 0, 2, 1, o), "the path is already above both circles");
            Assert.IsTrue(rule.IsValid(With(2, 4), 0, 3, 9, o), "the open path cell can still make 11");
        }
    }
}
