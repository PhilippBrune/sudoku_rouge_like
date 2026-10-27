using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: BOSS-MOD-006] Batch B6 rules of the modifier catalog on hand-made 9×9 boards (BossMechanicsSystem.md "Modifier catalog"): a digit
    // is rejected only when it breaks the rule with the digits already placed.
    [TestFixture]
    [Requirement("BOSS-MOD-006")]
    public sealed class CatalogB6RuleTests
    {
        private static SudokuBoard Empty()
        {
            var regions = new int[9, 9];
            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
                regions[r, c] = (r / 3) * 3 + c / 3;
            return new SudokuBoard(9, new int[9, 9], new int[9, 9], regions);
        }

        private static SudokuBoard With(params int[] firstRow)
        {
            var board = Empty();
            for (var c = 0; c < firstRow.Length; c++) board.Cells[0, c] = firstRow[c];
            return board;
        }

        private static ModifierOverlayData FirstRowLine(LineType type, int length, int lineValue = 0)
        {
            var line = new ModifierLine { Type = type, LineValue = lineValue };
            for (var c = 0; c < length; c++) line.Cells.Add(new CellCoord(0, c));
            var overlay = new ModifierOverlayData();
            overlay.Lines.Add(line);
            return overlay;
        }

        private static ModifierOverlayData Pair(CellPairType type, int value, (int r, int c) a, (int r, int c) b)
        {
            var overlay = new ModifierOverlayData();
            overlay.CellPairs.Add(new CellPairConstraint
                { CellA = new CellCoord(a.r, a.c), CellB = new CellCoord(b.r, b.c), PairType = type, Value = value });
            return overlay;
        }

        [Test]
        [Requirement("BOSS-LIB-037")]
        public void WhisperK_NeighboursDifferByTheLinesNumber()
        {
            var rule = new WhisperKLineRule();
            var k3 = FirstRowLine(LineType.WhisperGeneralized, 4, 3);
            Assert.IsFalse(rule.IsValid(With(5), 0, 1, 7, k3));
            Assert.IsTrue(rule.IsValid(With(5), 0, 1, 8, k3));
            var k6 = FirstRowLine(LineType.WhisperGeneralized, 4, 6);
            Assert.IsFalse(rule.IsValid(Empty(), 0, 1, 4, k6), "no digit is 6 away from 4 on 9×9");
            Assert.IsTrue(rule.IsValid(Empty(), 0, 1, 2, k6));
        }

        [Test]
        [Requirement("BOSS-LIB-093")]
        public void InvertedZone_FlipsTheWhisperForStepsInsideTheZone()
        {
            var o = FirstRowLine(LineType.ZoneWhisper, 5);
            foreach (var c in new[] { 0, 1, 2 }) o.NegationZone.Add(new CellCoord(0, c));
            var rule = new ZoneWhisperRule();
            Assert.IsTrue(rule.IsValid(With(2), 0, 1, 5, o), "inside: 3 apart is fine");
            Assert.IsFalse(rule.IsValid(With(2), 0, 1, 8, o), "inside: 6 apart is too far");
            Assert.IsFalse(rule.IsValid(With(0, 0, 2), 0, 3, 6, o), "leaving the zone: 4 apart is too close");
            Assert.IsTrue(rule.IsValid(With(0, 0, 2), 0, 3, 7, o));
            Assert.IsFalse(rule.IsValid(Empty(), 0, 3, 5, o), "outside: no digit is 5 away from 5");
        }

        [Test]
        [Requirement("BOSS-LIB-061")]
        public void RatioN_OneDigitIsThreeTimesTheOther()
        {
            var o = new ModifierOverlayData();
            o.PairConstraints.Add(new AdjacentPairConstraint
                { CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1), Type = PairConstraintType.RatioN, Value = 3 });
            var rule = new RatioNPairRule();
            Assert.IsTrue(rule.IsValid(With(2), 0, 1, 6, o));
            Assert.IsFalse(rule.IsValid(With(2), 0, 1, 4, o));
            Assert.IsFalse(rule.IsValid(Empty(), 0, 1, 5, o), "15 is too big and 5 is not a multiple of 3");
            Assert.IsTrue(rule.IsValid(Empty(), 0, 1, 9, o));
        }

        [Test]
        [Requirement("BOSS-LIB-063", "BOSS-LIB-077")]
        public void DistancePairsAndOppositeSums_CheckThePartner()
        {
            var board = Empty();
            board.Cells[1, 1] = 5;
            var distance = new CellPairValueRule(CellPairType.Distance);
            var d = Pair(CellPairType.Distance, 2, (0, 0), (1, 1));
            Assert.IsTrue(distance.IsValid(board, 0, 0, 3, d));
            Assert.IsTrue(distance.IsValid(board, 0, 0, 7, d));
            Assert.IsFalse(distance.IsValid(board, 0, 0, 4, d));

            board.Cells[8, 8] = 3;
            var opposite = new CellPairValueRule(CellPairType.OppositeSum);
            var o = Pair(CellPairType.OppositeSum, 10, (0, 0), (8, 8));
            Assert.IsTrue(opposite.IsValid(board, 0, 0, 7, o));
            Assert.IsFalse(opposite.IsValid(board, 0, 0, 6, o));
            Assert.IsFalse(opposite.IsValid(Empty(), 0, 0, 1, Pair(CellPairType.OppositeSum, 11, (0, 0), (8, 8))),
                "1 would need a 10 opposite");
        }

        [Test]
        [Requirement("BOSS-LIB-078")]
        public void CountingCircles_EachCircledDigitCountsItself()
        {
            var o = new ModifierOverlayData();
            foreach (var c in new[] { 0, 1, 2 })
                o.CellMarkers.Add(new CellMarker { Cell = new CellCoord(0, c), Type = MarkerType.CountingCircle });
            var rule = new CountingCirclesRule();
            Assert.IsTrue(rule.IsValid(With(2), 0, 1, 2, o), "two circles may hold 2");
            Assert.IsFalse(rule.IsValid(With(2, 2), 0, 2, 2, o), "a third 2 is one too many");
            Assert.IsTrue(rule.IsValid(With(2, 2), 0, 2, 1, o), "2, 2 and 1: each digit counted right");
            Assert.IsFalse(rule.IsValid(With(2, 2), 0, 2, 3, o), "a single 3 would need two more circles holding 3");
            Assert.IsTrue(rule.IsValid(With(2, 2), 1, 1, 9, o), "an uncircled cell is free");
        }

        [Test]
        [Requirement("BOSS-LIB-080")]
        public void DistanceCells_HoldTheirStepsToTheRing()
        {
            var o = new ModifierOverlayData();
            o.CellMarkers.Add(new CellMarker { Cell = new CellCoord(4, 4), Type = MarkerType.DistanceCellTarget });
            o.CellMarkers.Add(new CellMarker { Cell = new CellCoord(5, 6), Type = MarkerType.DistanceCellSource });
            var rule = new DistanceCellRule();
            Assert.IsTrue(rule.IsValid(Empty(), 5, 6, 3, o));
            Assert.IsFalse(rule.IsValid(Empty(), 5, 6, 2, o));
            Assert.IsTrue(rule.IsValid(Empty(), 4, 4, 9, o), "the ring cell itself is free");
        }
    }
}
