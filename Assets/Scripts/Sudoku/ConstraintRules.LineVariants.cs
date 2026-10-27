using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // Batch B1 of the modifier catalog (docs/BossMechanicsSystem.md "Modifier catalog" and "Line Constraints
    // (Extended)"). Every rule reads only the digits already on the board, so it is safe on partial boards: a digit is
    // rejected only when it breaks the rule with a digit that is already placed.

    internal static class LineRuleSupport
    {
        /// <summary>The index of (row, col) on the line, or -1.</summary>
        internal static int IndexOf(ModifierLine line, int row, int col)
        {
            for (var j = 0; j < line.Cells.Count; j++)
                if (line.Cells[j].Row == row && line.Cells[j].Col == col) return j;
            return -1;
        }

        /// <summary>The digit at index k of the line; the candidate <paramref name="value"/> when k is the checked cell.</summary>
        internal static int ValueAt(SudokuBoard board, ModifierLine line, int k, int j, int value) =>
            k == j ? value : board.Cells[line.Cells[k].Row, line.Cells[k].Col];
    }

    // [REQ: BOSS-LIB-038] Nonconsecutive Line: neighbours on the line never differ by exactly 1.
    public sealed class NonconsecLineRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.NonconsecLine) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                if (j > 0)
                {
                    var p = board.Cells[line.Cells[j - 1].Row, line.Cells[j - 1].Col];
                    if (p != 0 && Math.Abs(value - p) == 1) return false;
                }
                if (j < line.Cells.Count - 1)
                {
                    var q = board.Cells[line.Cells[j + 1].Row, line.Cells[j + 1].Col];
                    if (q != 0 && Math.Abs(value - q) == 1) return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-040] High-Low Alternating: digits alternate between low (at most the midpoint, 1–5 on 9×9) and high
    // (above it) along the line. Two cells an even distance apart are on the same side, an odd distance apart on
    // opposite sides.
    public sealed class HighLowLineRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        internal static bool IsLow(int value, int size) => value <= (size + 1) / 2;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            var low = IsLow(value, board.Size);
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.HighLowAlternating) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                for (var k = 0; k < line.Cells.Count; k++)
                {
                    if (k == j) continue;
                    var v = board.Cells[line.Cells[k].Row, line.Cells[k].Col];
                    if (v == 0) continue;
                    var sameSide = IsLow(v, board.Size) == low;
                    if (sameSide != (Math.Abs(j - k) % 2 == 0)) return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-041] Nabner Line: no two digits on the line repeat or are consecutive.
    public sealed class NabnerLineRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.NabnerLine) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                for (var k = 0; k < line.Cells.Count; k++)
                {
                    if (k == j) continue;
                    var v = board.Cells[line.Cells[k].Row, line.Cells[k].Col];
                    if (v != 0 && Math.Abs(value - v) <= 1) return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-042] [REQ: BOSS-LIB-043] Entropic and Modular Lines: any three neighbours on the line hold one digit
    // of each group (Entropic: 1–3, 4–6, 7–9; Modular: the three remainders mod 3). Equivalently, two cells hold the
    // same group exactly when their distance on the line is a multiple of 3.
    public sealed class TriadLineRule : IOrderedConstraintRule
    {
        private readonly LineType _type;

        public TriadLineRule(LineType type)
        {
            if (type != LineType.EntropicLine && type != LineType.ModularLine)
                throw new ArgumentException("Triad lines are Entropic or Modular.", nameof(type));
            _type = type;
        }

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        internal static int Group(LineType type, int value) => type == LineType.EntropicLine ? (value - 1) / 3 : value % 3;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            var group = Group(_type, value);
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != _type) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                for (var k = 0; k < line.Cells.Count; k++)
                {
                    if (k == j) continue;
                    var v = board.Cells[line.Cells[k].Row, line.Cells[k].Col];
                    if (v == 0) continue;
                    if ((Group(_type, v) == group) != (Math.Abs(j - k) % 3 == 0)) return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-044] Zipper Line: cells the same distance from the middle of the line sum to the same total; on a
    // line with a middle cell, that total is the middle cell's digit.
    public sealed class ZipperLineRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.ZipperLine || line.Cells.Count < 2) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                if (!IsValidOnLine(board, line, j, value)) return false;
            }
            return true;
        }

        private static bool IsValidOnLine(SudokuBoard board, ModifierLine line, int j, int value)
        {
            var count = line.Cells.Count;
            var hasMiddle = count % 2 == 1;
            var middle = (count - 1) / 2;
            // The total every pair must reach: the middle digit, or the sum of a pair that is already complete.
            var total = 0;
            if (hasMiddle) total = LineRuleSupport.ValueAt(board, line, middle, j, value);
            for (var a = 0; a < count / 2; a++)
            {
                var va = LineRuleSupport.ValueAt(board, line, a, j, value);
                var vb = LineRuleSupport.ValueAt(board, line, count - 1 - a, j, value);
                if (va == 0 || vb == 0) continue;
                if (total == 0) total = va + vb;
                else if (va + vb != total) return false;
            }
            if (total == 0) return true;
            // A complete pair whose sum no middle digit can match (the middle cell is still empty).
            if (total > board.Size && hasMiddle) return false;
            // A half-filled pair needs a partner of at least 1, so its digit stays below the total.
            for (var a = 0; a < count / 2; a++)
            {
                var va = LineRuleSupport.ValueAt(board, line, a, j, value);
                var vb = LineRuleSupport.ValueAt(board, line, count - 1 - a, j, value);
                if ((va != 0) == (vb != 0)) continue;
                var known = va != 0 ? va : vb;
                if (known >= total || total - known > board.Size) return false;
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-051] Fast Thermo: from the bulb, each digit is at least LineValue (default 2) above the one before.
    public sealed class FastThermoRule : IOrderedConstraintRule
    {
        public const int DefaultStep = 2;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.FastThermo) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                var step = line.LineValue > 0 ? line.LineValue : DefaultStep;
                var last = line.Cells.Count - 1;
                if (value < 1 + step * j || value > board.Size - step * (last - j)) return false;
                for (var k = 0; k <= last; k++)
                {
                    if (k == j) continue;
                    var v = board.Cells[line.Cells[k].Row, line.Cells[k].Col];
                    if (v == 0) continue;
                    if (k < j && v > value - step * (j - k)) return false;
                    if (k > j && v < value + step * (k - j)) return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-052] Ambiguous Thermo: the digits strictly increase along the line in one of the two directions;
    // the player is not told which. The rule accepts a digit while either direction still fits.
    public sealed class AmbiguousThermoRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.AmbiguousThermo) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                if (!Fits(board, line, j, value, true) && !Fits(board, line, j, value, false)) return false;
            }
            return true;
        }

        // Rising from the first cell (or from the last): every placed digit leaves room for the cells before and after
        // it, and two placed digits differ by at least their distance.
        private static bool Fits(SudokuBoard board, ModifierLine line, int j, int value, bool risingFromStart)
        {
            var count = line.Cells.Count;
            var previousPos = -1;
            var previousValue = 0;
            for (var step = 0; step < count; step++)
            {
                var k = risingFromStart ? step : count - 1 - step;
                var v = LineRuleSupport.ValueAt(board, line, k, j, value);
                if (v == 0) continue;
                if (v < 1 + step || v > board.Size - (count - 1 - step)) return false;
                if (previousPos >= 0 && v - previousValue < step - previousPos) return false;
                previousPos = step;
                previousValue = v;
            }
            return true;
        }
    }
}
