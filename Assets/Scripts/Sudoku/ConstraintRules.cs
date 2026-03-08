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
}
