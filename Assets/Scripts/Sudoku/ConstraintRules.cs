using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // German Whispers: adjacent cells on line differ by >= 5
    public sealed class GermanWhispersRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.GermanWhispers) continue;
                if (!CheckLineConstraint(line, board, row, col, value, 5, true)) return false;
            }
            return true;
        }

        private static bool CheckLineConstraint(ModifierLine line, SudokuBoard board, int row, int col, int value, int threshold, bool greaterOrEqual)
        {
            for (var j = 0; j < line.Cells.Count; j++)
            {
                var cell = line.Cells[j];
                if (cell.Row != row || cell.Col != col) continue;

                if (j > 0)
                {
                    var prev = line.Cells[j - 1];
                    var pv = board.Cells[prev.Row, prev.Col];
                    if (pv != 0 && Math.Abs(value - pv) < threshold) return false;
                }
                if (j < line.Cells.Count - 1)
                {
                    var next = line.Cells[j + 1];
                    var nv = board.Cells[next.Row, next.Col];
                    if (nv != 0 && Math.Abs(value - nv) < threshold) return false;
                }
            }
            return true;
        }
    }

    // Dutch Whispers: adjacent cells on line differ by >= 4
    public sealed class DutchWhispersRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.DutchWhispers) continue;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row != row || cell.Col != col) continue;
                    if (j > 0) { var p = line.Cells[j - 1]; var pv = board.Cells[p.Row, p.Col]; if (pv != 0 && Math.Abs(value - pv) < 4) return false; }
                    if (j < line.Cells.Count - 1) { var n = line.Cells[j + 1]; var nv = board.Cells[n.Row, n.Col]; if (nv != 0 && Math.Abs(value - nv) < 4) return false; }
                }
            }
            return true;
        }
    }

    // Parity Lines: adjacent cells alternate odd/even
    public sealed class ParityLinesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.ParityLine) continue;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row != row || cell.Col != col) continue;
                    var isEven = value % 2 == 0;
                    if (j > 0) { var p = line.Cells[j - 1]; var pv = board.Cells[p.Row, p.Col]; if (pv != 0 && (pv % 2 == 0) == isEven) return false; }
                    if (j < line.Cells.Count - 1) { var n = line.Cells[j + 1]; var nv = board.Cells[n.Row, n.Col]; if (nv != 0 && (nv % 2 == 0) == isEven) return false; }
                }
            }
            return true;
        }
    }

    // Renban Lines: digits on line form consecutive set (no repeats, max - min = length - 1)
    public sealed class RenbanLinesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.RenbanLine) continue;
                if (!IsOnLine(line, row, col)) continue;

                var min = value;
                var max = value;
                var filled = 1;
                var seen = new bool[10];
                seen[value] = true;

                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row == row && c.Col == col) continue;
                    var v = board.Cells[c.Row, c.Col];
                    if (v == 0) continue;
                    if (seen[v]) return false;
                    seen[v] = true;
                    if (v < min) min = v;
                    if (v > max) max = v;
                    filled++;
                }

                if (max - min >= line.Cells.Count) return false;
            }
            return true;
        }

        private static bool IsOnLine(ModifierLine line, int row, int col)
        {
            for (var j = 0; j < line.Cells.Count; j++)
                if (line.Cells[j].Row == row && line.Cells[j].Col == col) return true;
            return false;
        }
    }

    // Palindrome: line reads same forwards and backwards
    public sealed class PalindromeRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.Palindrome) continue;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row != row || cell.Col != col) continue;
                    var mirror = line.Cells.Count - 1 - j;
                    if (mirror == j) continue;
                    var m = line.Cells[mirror];
                    var mv = board.Cells[m.Row, m.Col];
                    if (mv != 0 && mv != value) return false;
                }
            }
            return true;
        }
    }

    // Thermo: digits increase along thermometer from bulb to tip
    public sealed class ThermoRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.Thermo) continue;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row != row || cell.Col != col) continue;
                    for (var k = 0; k < j; k++)
                    {
                        var prev = line.Cells[k];
                        var pv = board.Cells[prev.Row, prev.Col];
                        if (pv != 0 && pv >= value) return false;
                    }
                    for (var k = j + 1; k < line.Cells.Count; k++)
                    {
                        var next = line.Cells[k];
                        var nv = board.Cells[next.Row, next.Col];
                        if (nv != 0 && nv <= value) return false;
                    }
                }
            }
            return true;
        }
    }

    // Between Lines: digits on line between endpoints are strictly between endpoint values
    public sealed class BetweenLinesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.BetweenLine) continue;
                if (line.Cells.Count < 3) continue;

                var first = line.Cells[0];
                var last = line.Cells[line.Cells.Count - 1];
                var fv = (first.Row == row && first.Col == col) ? value : board.Cells[first.Row, first.Col];
                var lv = (last.Row == row && last.Col == col) ? value : board.Cells[last.Row, last.Col];

                for (var j = 1; j < line.Cells.Count - 1; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row != row || cell.Col != col) continue;
                    if (fv != 0 && lv != 0)
                    {
                        var lo = Math.Min(fv, lv);
                        var hi = Math.Max(fv, lv);
                        if (value <= lo || value >= hi) return false;
                    }
                }
            }
            return true;
        }
    }

    // Difference Kropki (white dot): adjacent cells differ by exactly 1
    public sealed class DifferenceKropkiRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.KropkiDots.Count; i++)
            {
                var dot = overlay.KropkiDots[i];
                if (dot.IsBlack) continue;
                int otherVal;
                if (dot.CellA.Row == row && dot.CellA.Col == col)
                    otherVal = board.Cells[dot.CellB.Row, dot.CellB.Col];
                else if (dot.CellB.Row == row && dot.CellB.Col == col)
                    otherVal = board.Cells[dot.CellA.Row, dot.CellA.Col];
                else continue;

                if (otherVal != 0 && Math.Abs(value - otherVal) != 1) return false;
            }
            return true;
        }
    }

    // Ratio Kropki (black dot): one cell is double the other
    public sealed class RatioKropkiRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.KropkiDots.Count; i++)
            {
                var dot = overlay.KropkiDots[i];
                if (!dot.IsBlack) continue;
                int otherVal;
                if (dot.CellA.Row == row && dot.CellA.Col == col)
                    otherVal = board.Cells[dot.CellB.Row, dot.CellB.Col];
                else if (dot.CellB.Row == row && dot.CellB.Col == col)
                    otherVal = board.Cells[dot.CellA.Row, dot.CellA.Col];
                else continue;

                if (otherVal != 0 && value != otherVal * 2 && otherVal != value * 2) return false;
            }
            return true;
        }
    }

    // Killer Cages: cells in cage sum to target, no repeats within cage
    public sealed class KillerCagesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.KillerCages.Count; i++)
            {
                var cage = overlay.KillerCages[i];
                if (!InCage(cage, row, col)) continue;

                var sum = value;
                var filled = 1;
                for (var j = 0; j < cage.Cells.Count; j++)
                {
                    var c = cage.Cells[j];
                    if (c.Row == row && c.Col == col) continue;
                    var v = board.Cells[c.Row, c.Col];
                    if (v == 0) continue;
                    if (v == value) return false;
                    sum += v;
                    filled++;
                }

                if (sum > cage.Sum) return false;
                if (filled == cage.Cells.Count && sum != cage.Sum) return false;
            }
            return true;
        }

        private static bool InCage(KillerCage cage, int row, int col)
        {
            for (var j = 0; j < cage.Cells.Count; j++)
                if (cage.Cells[j].Row == row && cage.Cells[j].Col == col) return true;
            return false;
        }
    }

    // Arrow Sums: circle cell = sum of arrow cells
    public sealed class ArrowSumsRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Arrows.Count; i++)
            {
                var arrow = overlay.Arrows[i];
                var isCircle = arrow.CircleCell.Row == row && arrow.CircleCell.Col == col;
                var isArrow = false;
                for (var j = 0; j < arrow.ArrowCells.Count; j++)
                    if (arrow.ArrowCells[j].Row == row && arrow.ArrowCells[j].Col == col) { isArrow = true; break; }

                if (!isCircle && !isArrow) continue;

                var circleVal = isCircle ? value : board.Cells[arrow.CircleCell.Row, arrow.CircleCell.Col];
                var arrowSum = 0;
                var allFilled = true;
                for (var j = 0; j < arrow.ArrowCells.Count; j++)
                {
                    var ac = arrow.ArrowCells[j];
                    var av = (ac.Row == row && ac.Col == col) ? value : board.Cells[ac.Row, ac.Col];
                    if (av == 0) { allFilled = false; continue; }
                    arrowSum += av;
                }

                if (circleVal != 0 && arrowSum > circleVal) return false;
                if (circleVal != 0 && allFilled && arrowSum != circleVal) return false;
            }
            return true;
        }
    }

    // EvenOdd: marked cells must match their parity
    public sealed class EvenOddRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.CellMarkers.Count; i++)
            {
                var marker = overlay.CellMarkers[i];
                if (marker.Cell.Row != row || marker.Cell.Col != col) continue;
                var isEven = value % 2 == 0;
                if (marker.Type == MarkerType.Even && !isEven) return false;
                if (marker.Type == MarkerType.Odd && isEven) return false;
            }
            return true;
        }
    }

    // Nonconsecutive: no orthogonally adjacent cells can have consecutive values
    public sealed class NonconsecutiveRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            if (row > 0) { var v = board.Cells[row - 1, col]; if (v != 0 && Math.Abs(value - v) == 1) return false; }
            if (row < size - 1) { var v = board.Cells[row + 1, col]; if (v != 0 && Math.Abs(value - v) == 1) return false; }
            if (col > 0) { var v = board.Cells[row, col - 1]; if (v != 0 && Math.Abs(value - v) == 1) return false; }
            if (col < size - 1) { var v = board.Cells[row, col + 1]; if (v != 0 && Math.Abs(value - v) == 1) return false; }
            return true;
        }
    }

    // Antiknight: no cell a knight's move away can have the same value
    public sealed class AntiknightRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;

        private static readonly int[] Dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
        private static readonly int[] Dc = { -1, 1, -2, 2, -2, 2, -1, 1 };

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            for (var i = 0; i < 8; i++)
            {
                var nr = row + Dr[i];
                var nc = col + Dc[i];
                if (nr >= 0 && nr < size && nc >= 0 && nc < size)
                    if (board.Cells[nr, nc] == value) return false;
            }
            return true;
        }
    }

    // Fog of War: visibility-only post-process, does not affect value validity
    public sealed class FogOfWarRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.FogPostProcess;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            return true;
        }
    }

    // Antiking: no cell a chess king's move away can have the same digit
    public sealed class AntikingRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                var nr = row + dr; var nc = col + dc;
                if (nr >= 0 && nr < size && nc >= 0 && nc < size)
                    if (board.Cells[nr, nc] == value) return false;
            }
            return true;
        }
    }

    // AntiBishop: no cell on the same diagonal can have the same digit
    public sealed class AntiBishopRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (r == row && c == col) continue;
                if (Math.Abs(r - row) == Math.Abs(c - col))
                    if (board.Cells[r, c] == value) return false;
            }
            return true;
        }
    }

    // NonconsecDiagonal: diagonally adjacent cells cannot be consecutive
    public sealed class NonconsecDiagonalRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            int[] dr = { -1, -1, 1, 1 };
            int[] dc = { -1, 1, -1, 1 };
            for (var i = 0; i < 4; i++)
            {
                var nr = row + dr[i]; var nc = col + dc[i];
                if (nr >= 0 && nr < size && nc >= 0 && nc < size)
                {
                    var v = board.Cells[nr, nc];
                    if (v != 0 && Math.Abs(value - v) == 1) return false;
                }
            }
            return true;
        }
    }

    // DistanceGe2: equal digits must be at Chebyshev distance >= 2 (no king-adjacent equal digits)
    public sealed class DistanceGe2Rule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                var nr = row + dr; var nc = col + dc;
                if (nr >= 0 && nr < size && nc >= 0 && nc < size)
                    if (board.Cells[nr, nc] == value) return false;
            }
            return true;
        }
    }

    // EntropyGlobal: every 3 consecutive cells in any row/col must contain one from each group {1-3},{4-6},{7-9}
    public sealed class EntropyGlobalRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;

        private static int Group(int v) => v <= 3 ? 0 : v <= 6 ? 1 : 2;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            // Check all windows of 3 in the same row that include col
            for (var start = Math.Max(0, col - 2); start <= Math.Min(size - 3, col); start++)
            {
                if (!CheckWindow(board, row, col, value, start, start + 1, start + 2, true)) return false;
            }
            // Check all windows of 3 in the same col that include row
            for (var start = Math.Max(0, row - 2); start <= Math.Min(size - 3, row); start++)
            {
                if (!CheckWindow(board, row, col, value, start, start + 1, start + 2, false)) return false;
            }
            return true;
        }

        private static bool CheckWindow(SudokuBoard board, int row, int col, int value,
            int a, int b, int c, bool isRow)
        {
            int v0 = GetVal(board, row, col, value, isRow ? row : a, isRow ? a : col);
            int v1 = GetVal(board, row, col, value, isRow ? row : b, isRow ? b : col);
            int v2 = GetVal(board, row, col, value, isRow ? row : c, isRow ? c : col);
            if (v0 == 0 || v1 == 0 || v2 == 0) return true; // window not fully filled
            var groups = (1 << Group(v0)) | (1 << Group(v1)) | (1 << Group(v2));
            return groups == 0b111; // all three groups present
        }

        private static int GetVal(SudokuBoard board, int row, int col, int value, int r, int c)
            => (r == row && c == col) ? value : board.Cells[r, c];
    }

    // ModularRegions: each region must contain at least one digit from {1-3}, {4-6}, {7-9}
    public sealed class ModularRegionsRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Region;

        private static int Group(int v) => v <= 3 ? 0 : v <= 6 ? 1 : 2;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            var size = board.Size;
            var regionId = board.RegionMap[row, col];
            var groupSeen = new bool[3];
            var emptyCells = 0;
            groupSeen[Group(value)] = true;

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (board.RegionMap[r, c] != regionId) continue;
                if (r == row && c == col) continue;
                var v = board.Cells[r, c];
                if (v == 0) emptyCells++;
                else groupSeen[Group(v)] = true;
            }

            // If region is complete, all groups must be represented
            if (emptyCells == 0)
                return groupSeen[0] && groupSeen[1] && groupSeen[2];

            return true;
        }
    }

    // ConsecutiveLine: adjacent cells on line must differ by exactly 1
    public sealed class ConsecutiveLineRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.ConsecutiveLine) continue;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row != row || cell.Col != col) continue;
                    if (j > 0)
                    {
                        var prev = line.Cells[j - 1];
                        var pv = board.Cells[prev.Row, prev.Col];
                        if (pv != 0 && Math.Abs(value - pv) != 1) return false;
                    }
                    if (j < line.Cells.Count - 1)
                    {
                        var next = line.Cells[j + 1];
                        var nv = board.Cells[next.Row, next.Col];
                        if (nv != 0 && Math.Abs(value - nv) != 1) return false;
                    }
                }
            }
            return true;
        }
    }

    // SlowThermo: non-strict increase from bulb — each cell >= the previous
    public sealed class SlowThermoRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.SlowThermo) continue;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row != row || cell.Col != col) continue;
                    // All earlier cells must be <= value
                    for (var k = 0; k < j; k++)
                    {
                        var prev = line.Cells[k];
                        var pv = board.Cells[prev.Row, prev.Col];
                        if (pv != 0 && pv > value) return false;
                    }
                    // All later cells must be >= value
                    for (var k = j + 1; k < line.Cells.Count; k++)
                    {
                        var next = line.Cells[k];
                        var nv = board.Cells[next.Row, next.Col];
                        if (nv != 0 && nv < value) return false;
                    }
                }
            }
            return true;
        }
    }

    // UniqueSetLine: no digit may repeat on the line
    public sealed class UniqueSetLineRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.UniqueSetLine) continue;
                var onLine = false;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row == row && cell.Col == col) { onLine = true; continue; }
                    if (board.Cells[cell.Row, cell.Col] == value) { if (onLine || IsOnLine(line, row, col)) return false; }
                }
                if (!onLine) continue;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var cell = line.Cells[j];
                    if (cell.Row == row && cell.Col == col) continue;
                    if (board.Cells[cell.Row, cell.Col] == value) return false;
                }
            }
            return true;
        }

        private static bool IsOnLine(ModifierLine line, int row, int col)
        {
            for (var j = 0; j < line.Cells.Count; j++)
                if (line.Cells[j].Row == row && line.Cells[j].Col == col) return true;
            return false;
        }
    }

    // FullKropki: when FullKropkiNegativeInference=true, orthogonal pairs with no dot cannot have diff=1 or ratio=2
    public sealed class FullKropkiRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null || !overlay.FullKropkiNegativeInference) return true;
            var size = board.Size;
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };
            for (var d = 0; d < 4; d++)
            {
                var nr = row + dr[d]; var nc = col + dc[d];
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                var nv = board.Cells[nr, nc];
                if (nv == 0) continue;
                // Check if a dot exists between (row,col) and (nr,nc)
                if (HasDot(overlay, row, col, nr, nc)) continue;
                // No dot → neither diff=1 nor ratio=2 allowed
                if (Math.Abs(value - nv) == 1) return false;
                if (value != 0 && nv != 0 && (value * 2 == nv || nv * 2 == value)) return false;
            }
            return true;
        }

        private static bool HasDot(ModifierOverlayData overlay, int r1, int c1, int r2, int c2)
        {
            for (var i = 0; i < overlay.KropkiDots.Count; i++)
            {
                var dot = overlay.KropkiDots[i];
                if ((dot.CellA.Row == r1 && dot.CellA.Col == c1 && dot.CellB.Row == r2 && dot.CellB.Col == c2) ||
                    (dot.CellA.Row == r2 && dot.CellA.Col == c2 && dot.CellB.Row == r1 && dot.CellB.Col == c1))
                    return true;
            }
            return false;
        }
    }

    // SumKropki: labelled dot — CellA + CellB = SumValue
    public sealed class SumKropkiRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.KropkiDots.Count; i++)
            {
                var dot = overlay.KropkiDots[i];
                if (dot.SumValue <= 0) continue;
                int av = -1, bv = -1;
                if (dot.CellA.Row == row && dot.CellA.Col == col) { av = value; bv = board.Cells[dot.CellB.Row, dot.CellB.Col]; }
                else if (dot.CellB.Row == row && dot.CellB.Col == col) { bv = value; av = board.Cells[dot.CellA.Row, dot.CellA.Col]; }
                else continue;
                if (av > 0 && bv > 0 && av + bv != dot.SumValue) return false;
            }
            return true;
        }
    }

    // GreaterLessThan: chevron pair — CellA > CellB or CellA < CellB
    public sealed class GreaterLessThanRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.PairConstraints.Count; i++)
            {
                var pair = overlay.PairConstraints[i];
                if (pair.Type != PairConstraintType.GreaterThan && pair.Type != PairConstraintType.LessThan) continue;
                int av = -1, bv = -1;
                if (pair.CellA.Row == row && pair.CellA.Col == col) { av = value; bv = board.Cells[pair.CellB.Row, pair.CellB.Col]; }
                else if (pair.CellB.Row == row && pair.CellB.Col == col) { bv = value; av = board.Cells[pair.CellA.Row, pair.CellA.Col]; }
                else continue;
                if (av <= 0 || bv <= 0) continue;
                if (pair.Type == PairConstraintType.GreaterThan && av <= bv) return false;
                if (pair.Type == PairConstraintType.LessThan && av >= bv) return false;
            }
            return true;
        }
    }

    // XVPairs: X = pair sums to 10, V = pair sums to 5
    public sealed class XVPairsRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.PairConstraints.Count; i++)
            {
                var pair = overlay.PairConstraints[i];
                if (pair.Type != PairConstraintType.SumX && pair.Type != PairConstraintType.SumV && pair.Type != PairConstraintType.SumK) continue;
                int av = -1, bv = -1;
                if (pair.CellA.Row == row && pair.CellA.Col == col) { av = value; bv = board.Cells[pair.CellB.Row, pair.CellB.Col]; }
                else if (pair.CellB.Row == row && pair.CellB.Col == col) { bv = value; av = board.Cells[pair.CellA.Row, pair.CellA.Col]; }
                else continue;
                if (av <= 0 || bv <= 0) continue;
                var target = pair.Type == PairConstraintType.SumX ? 10
                           : pair.Type == PairConstraintType.SumV ? 5
                           : pair.Value;
                if (av + bv != target) return false;
            }
            return true;
        }
    }

    // PrimeCells: marked cells must contain a prime digit (2, 3, 5, or 7)
    public sealed class PrimeCellsRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.CellMarkers.Count; i++)
            {
                var m = overlay.CellMarkers[i];
                if (m.Type != MarkerType.Prime) continue;
                if (m.Cell.Row != row || m.Cell.Col != col) continue;
                if (value != 2 && value != 3 && value != 5 && value != 7) return false;
            }
            return true;
        }
    }

    // FortressCells: shaded cells must be strictly greater than all orthogonally adjacent non-fortress cells
    public sealed class FortressCellsRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            var size = board.Size;
            var isFortress = IsFortress(overlay, row, col);
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };

            if (isFortress)
            {
                // Fortress cell must be > all orthogonal non-fortress neighbors
                for (var d = 0; d < 4; d++)
                {
                    var nr = row + dr[d]; var nc = col + dc[d];
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (IsFortress(overlay, nr, nc)) continue;
                    var nv = board.Cells[nr, nc];
                    if (nv != 0 && value <= nv) return false;
                }
            }
            else
            {
                // Non-fortress cell must be < all orthogonal fortress neighbors
                for (var d = 0; d < 4; d++)
                {
                    var nr = row + dr[d]; var nc = col + dc[d];
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsFortress(overlay, nr, nc)) continue;
                    var fv = (nr == row && nc == col) ? value : board.Cells[nr, nc];
                    if (fv == 0) continue;
                    // Get the fortress neighbor's value properly
                    var fortressVal = board.Cells[nr, nc];
                    if (fortressVal != 0 && value >= fortressVal) return false;
                }
            }
            return true;
        }

        private static bool IsFortress(ModifierOverlayData overlay, int row, int col)
        {
            for (var i = 0; i < overlay.CellMarkers.Count; i++)
            {
                var m = overlay.CellMarkers[i];
                if (m.Type == MarkerType.Fortress && m.Cell.Row == row && m.Cell.Col == col) return true;
            }
            return false;
        }
    }
}
