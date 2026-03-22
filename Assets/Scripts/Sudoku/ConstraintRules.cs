using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public sealed class GermanWhispersRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;
        public int Order => 0;

        private readonly List<ModifierLine> _lines;

        public GermanWhispersRule(List<ModifierLine> lines) { _lines = lines; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (line.Type != LineType.GermanWhispers) continue;

                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row != row || c.Col != col) continue;

                    if (j > 0)
                    {
                        var prev = line.Cells[j - 1];
                        var prevVal = board.GetCell(prev.Row, prev.Col);
                        if (prevVal != 0 && Math.Abs(value - prevVal) < 5) return false;
                    }

                    if (j < line.Cells.Count - 1)
                    {
                        var next = line.Cells[j + 1];
                        var nextVal = board.GetCell(next.Row, next.Col);
                        if (nextVal != 0 && Math.Abs(value - nextVal) < 5) return false;
                    }
                }
            }

            return true;
        }
    }

    public sealed class DutchWhispersRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;
        public int Order => 1;

        private readonly List<ModifierLine> _lines;

        public DutchWhispersRule(List<ModifierLine> lines) { _lines = lines; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (line.Type != LineType.DutchWhispers) continue;

                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row != row || c.Col != col) continue;

                    if (j > 0)
                    {
                        var prev = line.Cells[j - 1];
                        var prevVal = board.GetCell(prev.Row, prev.Col);
                        if (prevVal != 0 && Math.Abs(value - prevVal) < 4) return false;
                    }

                    if (j < line.Cells.Count - 1)
                    {
                        var next = line.Cells[j + 1];
                        var nextVal = board.GetCell(next.Row, next.Col);
                        if (nextVal != 0 && Math.Abs(value - nextVal) < 4) return false;
                    }
                }
            }

            return true;
        }
    }

    public sealed class ParityLinesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;
        public int Order => 2;

        private readonly List<ModifierLine> _lines;

        public ParityLinesRule(List<ModifierLine> lines) { _lines = lines; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (line.Type != LineType.Parity) continue;

                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row != row || c.Col != col) continue;

                    var valueParity = value % 2;

                    if (j > 0)
                    {
                        var prev = line.Cells[j - 1];
                        var prevVal = board.GetCell(prev.Row, prev.Col);
                        if (prevVal != 0 && (prevVal % 2) == valueParity) return false;
                    }

                    if (j < line.Cells.Count - 1)
                    {
                        var next = line.Cells[j + 1];
                        var nextVal = board.GetCell(next.Row, next.Col);
                        if (nextVal != 0 && (nextVal % 2) == valueParity) return false;
                    }
                }
            }

            return true;
        }
    }

    public sealed class RenbanLinesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;
        public int Order => 3;

        private readonly List<ModifierLine> _lines;

        public RenbanLinesRule(List<ModifierLine> lines) { _lines = lines; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (line.Type != LineType.Renban) continue;

                var onLine = false;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    if (line.Cells[j].Row == row && line.Cells[j].Col == col)
                    {
                        onLine = true;
                        break;
                    }
                }

                if (!onLine) continue;

                // No duplicate on same line
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row == row && c.Col == col) continue;
                    if (board.GetCell(c.Row, c.Col) == value) return false;
                }

                // Collect placed values including the new one; check consecutive feasibility
                var min = value;
                var max = value;
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row == row && c.Col == col) continue;
                    var v = board.GetCell(c.Row, c.Col);
                    if (v <= 0) continue;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                if (max - min >= line.Cells.Count) return false;
            }

            return true;
        }
    }

    public sealed class DifferenceKropkiRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;
        public int Order => 0;

        private readonly List<KropkiDot> _dots;

        public DifferenceKropkiRule(List<KropkiDot> dots) { _dots = dots; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _dots.Count; i++)
            {
                var dot = _dots[i];
                if (dot.Type != DotType.White) continue;

                int otherVal;
                if (dot.CellA.Row == row && dot.CellA.Col == col)
                    otherVal = board.GetCell(dot.CellB.Row, dot.CellB.Col);
                else if (dot.CellB.Row == row && dot.CellB.Col == col)
                    otherVal = board.GetCell(dot.CellA.Row, dot.CellA.Col);
                else
                    continue;

                if (otherVal != 0 && Math.Abs(value - otherVal) != 1) return false;
            }

            return true;
        }
    }

    public sealed class RatioKropkiRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;
        public int Order => 1;

        private readonly List<KropkiDot> _dots;

        public RatioKropkiRule(List<KropkiDot> dots) { _dots = dots; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _dots.Count; i++)
            {
                var dot = _dots[i];
                if (dot.Type != DotType.Black) continue;

                int otherVal;
                if (dot.CellA.Row == row && dot.CellA.Col == col)
                    otherVal = board.GetCell(dot.CellB.Row, dot.CellB.Col);
                else if (dot.CellB.Row == row && dot.CellB.Col == col)
                    otherVal = board.GetCell(dot.CellA.Row, dot.CellA.Col);
                else
                    continue;

                if (otherVal != 0)
                {
                    var bigger = Math.Max(value, otherVal);
                    var smaller = Math.Min(value, otherVal);
                    if (smaller == 0 || bigger != 2 * smaller) return false;
                }
            }

            return true;
        }
    }

    public sealed class KillerCageRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;
        public int Order => 0;

        private readonly List<KillerCage> _cages;

        public KillerCageRule(List<KillerCage> cages) { _cages = cages; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _cages.Count; i++)
            {
                var cage = _cages[i];
                var inCage = false;
                var sum = 0;
                var emptyCount = 0;

                for (var j = 0; j < cage.Cells.Count; j++)
                {
                    var c = cage.Cells[j];
                    if (c.Row == row && c.Col == col)
                    {
                        inCage = true;
                        continue;
                    }

                    var v = board.GetCell(c.Row, c.Col);
                    if (v > 0)
                    {
                        if (v == value) return false;
                        sum += v;
                    }
                    else
                    {
                        emptyCount++;
                    }
                }

                if (!inCage) continue;

                sum += value;

                if (emptyCount == 0 && sum != cage.Sum) return false;
                if (sum > cage.Sum) return false;

                if (emptyCount > 0)
                {
                    var remaining = cage.Sum - sum;
                    if (remaining < emptyCount) return false;
                }
            }

            return true;
        }
    }

    public sealed class ArrowSumRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;
        public int Order => 1;

        private readonly List<ArrowConstraint> _arrows;

        public ArrowSumRule(List<ArrowConstraint> arrows) { _arrows = arrows; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _arrows.Count; i++)
            {
                var arrow = _arrows[i];
                var isCircle = arrow.Circle.Row == row && arrow.Circle.Col == col;
                var onPath = false;

                if (!isCircle)
                {
                    for (var j = 0; j < arrow.Path.Count; j++)
                    {
                        if (arrow.Path[j].Row == row && arrow.Path[j].Col == col)
                        {
                            onPath = true;
                            break;
                        }
                    }
                }

                if (!isCircle && !onPath) continue;

                var circleVal = isCircle ? value : board.GetCell(arrow.Circle.Row, arrow.Circle.Col);

                var pathSum = 0;
                var pathEmpty = 0;
                for (var j = 0; j < arrow.Path.Count; j++)
                {
                    var c = arrow.Path[j];
                    var v = (c.Row == row && c.Col == col) ? value : board.GetCell(c.Row, c.Col);

                    if (v > 0) pathSum += v;
                    else pathEmpty++;
                }

                if (circleVal == 0) continue;

                if (pathEmpty == 0 && pathSum != circleVal) return false;
                if (pathSum > circleVal) return false;
            }

            return true;
        }
    }

    public sealed class FogOfWarRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.FogPostProcess;
        public int Order => 0;

        public FogOfWarRule(ModifierOverlayData overlay) { }

        // Fog does not restrict moves; visibility is managed by the UI layer.
        public bool ValidateMove(SudokuBoard board, int row, int col, int value) => true;
    }

    public sealed class PalindromeRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;
        public int Order => 4;

        private readonly List<ModifierLine> _lines;

        public PalindromeRule(List<ModifierLine> lines) { _lines = lines; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (line.Type != LineType.Palindrome) continue;

                var n = line.Cells.Count;
                for (var j = 0; j < n; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row != row || c.Col != col) continue;

                    var mirror = line.Cells[n - 1 - j];
                    var mirrorVal = board.GetCell(mirror.Row, mirror.Col);
                    if (mirrorVal != 0 && mirrorVal != value) return false;
                }
            }

            return true;
        }
    }

    public sealed class ThermoRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;
        public int Order => 5;

        private readonly List<ModifierLine> _lines;

        public ThermoRule(List<ModifierLine> lines) { _lines = lines; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (line.Type != LineType.Thermo) continue;

                for (var j = 0; j < line.Cells.Count; j++)
                {
                    var c = line.Cells[j];
                    if (c.Row != row || c.Col != col) continue;

                    for (var k = 0; k < j; k++)
                    {
                        var prev = line.Cells[k];
                        var prevVal = board.GetCell(prev.Row, prev.Col);
                        if (prevVal != 0 && prevVal >= value) return false;
                    }

                    for (var k = j + 1; k < line.Cells.Count; k++)
                    {
                        var next = line.Cells[k];
                        var nextVal = board.GetCell(next.Row, next.Col);
                        if (nextVal != 0 && nextVal <= value) return false;
                    }
                }
            }

            return true;
        }
    }

    public sealed class BetweenLinesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;
        public int Order => 6;

        private readonly List<ModifierLine> _lines;

        public BetweenLinesRule(List<ModifierLine> lines) { _lines = lines; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (line.Type != LineType.BetweenLines || line.Cells.Count < 3) continue;

                var first = line.Cells[0];
                var last = line.Cells[line.Cells.Count - 1];
                var isFirst = first.Row == row && first.Col == col;
                var isLast = last.Row == row && last.Col == col;

                var firstVal = isFirst ? value : board.GetCell(first.Row, first.Col);
                var lastVal = isLast ? value : board.GetCell(last.Row, last.Col);

                if (firstVal == 0 || lastVal == 0) continue;

                var lo = Math.Min(firstVal, lastVal);
                var hi = Math.Max(firstVal, lastVal);

                if (isFirst || isLast)
                {
                    if (firstVal == lastVal) return false;
                    for (var j = 1; j < line.Cells.Count - 1; j++)
                    {
                        var pc = line.Cells[j];
                        var pv = board.GetCell(pc.Row, pc.Col);
                        if (pv != 0 && (pv <= lo || pv >= hi)) return false;
                    }
                }
                else
                {
                    var onLine = false;
                    for (var j = 1; j < line.Cells.Count - 1; j++)
                    {
                        if (line.Cells[j].Row == row && line.Cells[j].Col == col) { onLine = true; break; }
                    }

                    if (onLine && (value <= lo || value >= hi)) return false;
                }
            }

            return true;
        }
    }

    public sealed class EvenOddRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;
        public int Order => 0;

        private readonly List<CellMarker> _markers;

        public EvenOddRule(List<CellMarker> markers) { _markers = markers; }

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            for (var i = 0; i < _markers.Count; i++)
            {
                var m = _markers[i];
                if (m.Cell.Row != row || m.Cell.Col != col) continue;
                if (m.Type == MarkerType.Even && value % 2 != 0) return false;
                if (m.Type == MarkerType.Odd && value % 2 == 0) return false;
            }

            return true;
        }
    }

    public sealed class NonconsecutiveRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;
        public int Order => 0;

        private static readonly (int Dr, int Dc)[] OrthoDir = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            var size = board.Size;
            for (var d = 0; d < OrthoDir.Length; d++)
            {
                var nr = row + OrthoDir[d].Dr;
                var nc = col + OrthoDir[d].Dc;
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                var neighbor = board.GetCell(nr, nc);
                if (neighbor != 0 && Math.Abs(value - neighbor) == 1) return false;
            }

            return true;
        }
    }

    public sealed class AntiknightRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.GlobalNegative;
        public int Order => 1;

        private static readonly (int Dr, int Dc)[] KnightMoves =
        {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2),  (1, 2),  (2, -1),  (2, 1)
        };

        public bool ValidateMove(SudokuBoard board, int row, int col, int value)
        {
            var size = board.Size;
            for (var d = 0; d < KnightMoves.Length; d++)
            {
                var nr = row + KnightMoves[d].Dr;
                var nc = col + KnightMoves[d].Dc;
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                if (board.GetCell(nr, nc) == value) return false;
            }

            return true;
        }
    }
}
