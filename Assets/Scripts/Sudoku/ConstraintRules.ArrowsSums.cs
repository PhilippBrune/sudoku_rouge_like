using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // Arrow variants and sum lines of the modifier catalog (docs/design/modifier-catalog-plan.md). Each check compares
    // what the filled digits allow on both sides: the lowest and highest total the path can still reach against the
    // totals the circle digits can still stand for. A digit is rejected only when the two ranges no longer meet, so the
    // rules are safe on partial boards.

    internal static class SumRange
    {
        /// <summary>Lowest and highest total of the cells, with <paramref name="value"/> in (row, col).</summary>
        internal static (int Min, int Max, bool Complete) Of(SudokuBoard board, System.Collections.Generic.List<CellCoord> cells,
            int row, int col, int value)
        {
            int min = 0, max = 0;
            var complete = true;
            foreach (var c in cells)
            {
                var v = c.Row == row && c.Col == col ? value : board.Cells[c.Row, c.Col];
                if (v == 0) { min += 1; max += board.Size; complete = false; }
                else { min += v; max += v; }
            }
            return (min, max, complete);
        }

        internal static int Digit(SudokuBoard board, CellCoord c, int row, int col, int value) =>
            c.Row == row && c.Col == col ? value : board.Cells[c.Row, c.Col];

        internal static bool Meet(int minA, int maxA, int minB, int maxB) => minA <= maxB && minB <= maxA;
    }

    // [REQ: BOSS-LIB-070] Arrow Average: the circle holds the average of the digits on its arrow.
    // [REQ: BOSS-LIB-072] Pill Arrow: the two cells of the pill read as a two-digit number (top or left cell first),
    // and the arrow adds up to it.
    // [REQ: BOSS-LIB-073] Double Arrow: the arrow between two circles adds up to the two circle digits together.
    public sealed class TypedArrowRule : IOrderedConstraintRule
    {
        private readonly ArrowType _type;

        public TypedArrowRule(ArrowType type) => _type = type;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Arrows.Count; i++)
            {
                var arrow = overlay.Arrows[i];
                if (arrow.ArrowType != _type || !Touches(arrow, row, col)) continue;
                if (!Fits(board, arrow, row, col, value)) return false;
            }
            return true;
        }

        private bool Touches(ArrowConstraint arrow, int row, int col)
        {
            if (arrow.CircleCell.Row == row && arrow.CircleCell.Col == col) return true;
            if (_type != ArrowType.Average && arrow.SecondCircle.Row == row && arrow.SecondCircle.Col == col) return true;
            return arrow.ArrowCells.Exists(c => c.Row == row && c.Col == col);
        }

        private bool Fits(SudokuBoard board, ArrowConstraint arrow, int row, int col, int value)
        {
            var size = board.Size;
            var path = SumRange.Of(board, arrow.ArrowCells, row, col, value);
            var a = SumRange.Digit(board, arrow.CircleCell, row, col, value);
            var aMin = a == 0 ? 1 : a;
            var aMax = a == 0 ? size : a;
            switch (_type)
            {
                case ArrowType.Average:
                {
                    var k = arrow.ArrowCells.Count;
                    if (path.Complete)
                        return a != 0 ? path.Min == a * k : path.Min % k == 0 && path.Min / k <= size;
                    return SumRange.Meet(path.Min, path.Max, aMin * k, aMax * k);
                }
                case ArrowType.DoubleEndpoint:
                {
                    var b = SumRange.Digit(board, arrow.SecondCircle, row, col, value);
                    if (a != 0 && b != 0 && path.Complete) return path.Min == a + b;
                    return SumRange.Meet(path.Min, path.Max, aMin + (b == 0 ? 1 : b), aMax + (b == 0 ? size : b));
                }
                case ArrowType.Pill:
                {
                    var b = SumRange.Digit(board, arrow.SecondCircle, row, col, value);
                    if (a != 0 && b != 0 && path.Complete) return path.Min == a * 10 + b;
                    return SumRange.Meet(path.Min, path.Max, aMin * 10 + (b == 0 ? 1 : b), aMax * 10 + (b == 0 ? size : b));
                }
                default:
                    return true;
            }
        }
    }

    // [REQ: BOSS-LIB-045] Region Sum Line: the line falls into pieces at every region border, and every piece adds up
    // to the same total. [REQ: BOSS-LIB-046] N-Line: every piece adds up to the line's number (LineValue, 10).
    public sealed class RegionSumLineRule : IOrderedConstraintRule
    {
        private readonly LineType _type;

        public RegionSumLineRule(LineType type) => _type = type;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null || board.RegionMap == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != _type || LineRuleSupport.IndexOf(line, row, col) < 0) continue;
                if (!Fits(board, line, row, col, value)) return false;
            }
            return true;
        }

        // Two passes over the pieces, without allocating: the first finds the common total (the N-line's number, or
        // the sum of a finished piece, which every other finished piece must match); the second checks that every
        // piece can still reach it. With no total yet, some total must be reachable by every piece.
        private bool Fits(SudokuBoard board, ModifierLine line, int row, int col, int value)
        {
            var target = _type == LineType.NLine ? line.LineValue : 0;
            var start = 0;
            while (NextPiece(board, line, ref start, row, col, value, out var min, out _, out var complete))
            {
                if (!complete) continue;
                if (target == 0) target = min;
                else if (min != target) return false;
            }
            int low = target > 0 ? target : int.MinValue, high = target > 0 ? target : int.MaxValue;
            start = 0;
            while (NextPiece(board, line, ref start, row, col, value, out var min, out var max, out _))
            {
                low = Math.Max(low, min);
                high = Math.Min(high, max);
            }
            return low <= high;
        }

        /// <summary>The range of the piece starting at <paramref name="start"/> (the run of line cells in one region);
        /// moves <paramref name="start"/> to the next piece. False after the last piece.</summary>
        private static bool NextPiece(SudokuBoard board, ModifierLine line, ref int start, int row, int col, int value,
            out int min, out int max, out bool complete)
        {
            min = 0; max = 0; complete = true;
            if (start >= line.Cells.Count) return false;
            var first = line.Cells[start];
            var region = board.RegionMap[first.Row, first.Col];
            var i = start;
            for (; i < line.Cells.Count; i++)
            {
                var c = line.Cells[i];
                if (board.RegionMap[c.Row, c.Col] != region) break;
                var v = c.Row == row && c.Col == col ? value : board.Cells[c.Row, c.Col];
                if (v == 0) { min += 1; max += board.Size; complete = false; }
                else { min += v; max += v; }
            }
            start = i;
            return true;
        }
    }

    // [REQ: BOSS-LIB-048] Sum Line: all digits on the line add up to the number at its end (LineValue).
    public sealed class SumLineRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.SumLine || LineRuleSupport.IndexOf(line, row, col) < 0) continue;
                var r = SumRange.Of(board, line.Cells, row, col, value);
                if (r.Min > line.LineValue || r.Max < line.LineValue) return false;
            }
            return true;
        }
    }
}
