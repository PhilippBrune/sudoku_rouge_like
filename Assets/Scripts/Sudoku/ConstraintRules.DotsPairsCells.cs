using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // Batch B2 of the modifier catalog (docs/BossMechanicsSystem.md "Modifier catalog"): complete Kropki sets, Difference ≥ K
    // pairs, Multiples cells and Mirror cells. Partial-safe like the other rules.

    // [REQ: BOSS-LIB-058] [REQ: BOSS-LIB-059] Full White / Full Black Kropki: every pair of neighbours that differs by 1
    // (white) or stands in a 1:2 ratio (black) carries that dot, so a missing dot rules the relation out. The dots that
    // are there are checked by DifferenceKropkiRule and RatioKropkiRule.
    public sealed class FullKropkiSetRule : IOrderedConstraintRule
    {
        private readonly bool _black;

        public FullKropkiSetRule(bool black) => _black = black;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null || !(_black ? overlay.FullBlackKropkiNeg : overlay.FullWhiteKropkiNeg)) return true;
            var size = board.Size;
            for (var d = 0; d < 4; d++)
            {
                var nr = row + (d == 0 ? -1 : d == 1 ? 1 : 0);
                var nc = col + (d == 2 ? -1 : d == 3 ? 1 : 0);
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                var nv = board.Cells[nr, nc];
                if (nv == 0 || HasDot(overlay, row, col, nr, nc)) continue;
                var related = _black ? value * 2 == nv || nv * 2 == value : Math.Abs(value - nv) == 1;
                if (related) return false;
            }
            return true;
        }

        private bool HasDot(ModifierOverlayData overlay, int r1, int c1, int r2, int c2)
        {
            for (var i = 0; i < overlay.KropkiDots.Count; i++)
            {
                var dot = overlay.KropkiDots[i];
                if (dot.SumValue != 0 || dot.IsBlack != _black) continue;
                if ((dot.CellA.Row == r1 && dot.CellA.Col == c1 && dot.CellB.Row == r2 && dot.CellB.Col == c2) ||
                    (dot.CellA.Row == r2 && dot.CellA.Col == c2 && dot.CellB.Row == r1 && dot.CellB.Col == c1))
                    return true;
            }
            return false;
        }
    }

    // [REQ: BOSS-LIB-060] Difference ≥ K: the two cells of a marked pair differ by at least K (default 3).
    public sealed class DifferenceKPairRule : IOrderedConstraintRule
    {
        public const int DefaultK = 3;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        internal static int K(AdjacentPairConstraint pair, ModifierOverlayData overlay) =>
            pair.Value > 0 ? pair.Value : overlay.DifferenceKValue > 0 ? overlay.DifferenceKValue : DefaultK;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.PairConstraints.Count; i++)
            {
                var pair = overlay.PairConstraints[i];
                if (pair.Type != PairConstraintType.DifferenceK) continue;
                CellCoord other;
                if (pair.CellA.Row == row && pair.CellA.Col == col) other = pair.CellB;
                else if (pair.CellB.Row == row && pair.CellB.Col == col) other = pair.CellA;
                else continue;
                var k = K(pair, overlay);
                var ov = board.Cells[other.Row, other.Col];
                if (ov != 0) { if (Math.Abs(value - ov) < k) return false; }
                else if (value - k < 1 && value + k > board.Size) return false;   // no partner digit could exist
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-074] Multiples: a marked cell holds a multiple of 3.
    public sealed class MultipleCellsRule : IOrderedConstraintRule
    {
        public const int Divisor = 3;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.CellMarkers.Count; i++)
            {
                var m = overlay.CellMarkers[i];
                if (m.Type == MarkerType.Multiple && m.Cell.Row == row && m.Cell.Col == col && value % Divisor != 0)
                    return false;
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-076] Mirror Cells: the two cells of a pair (mirrored through the centre of the grid) hold the same
    // digit.
    public sealed class MirrorCellsRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.CellPairs.Count; i++)
            {
                var pair = overlay.CellPairs[i];
                if (pair.PairType != CellPairType.Mirror) continue;
                CellCoord other;
                if (pair.CellA.Row == row && pair.CellA.Col == col) other = pair.CellB;
                else if (pair.CellB.Row == row && pair.CellB.Col == col) other = pair.CellA;
                else continue;
                var ov = board.Cells[other.Row, other.Col];
                if (ov != 0 && ov != value) return false;
            }
            return true;
        }
    }
}
