using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // Batch B4 of the modifier catalog (docs/design/modifier-catalog-plan.md): clues outside the grid (Skyscrapers,
    // Sandwich, X-Sums, Little Killer) and Quadruples on the inner corners. Each rule compares what the placed digits
    // already decide with the clue and rejects a digit only when no completion could meet it, so it is safe on partial
    // boards.

    /// <summary>The cells an outside clue reads, in reading order from the clue.</summary>
    public static class OutsideClueGeometry
    {
        public static List<CellCoord> Cells(OutsideClue clue, int size)
        {
            var cells = new List<CellCoord>(size);
            switch (clue.Edge)
            {
                case OutsideClueEdge.Top:
                    for (var r = 0; r < size; r++) cells.Add(new CellCoord(r, clue.Index));
                    break;
                case OutsideClueEdge.Bottom:
                    for (var r = size - 1; r >= 0; r--) cells.Add(new CellCoord(r, clue.Index));
                    break;
                case OutsideClueEdge.Left:
                    for (var c = 0; c < size; c++) cells.Add(new CellCoord(clue.Index, c));
                    break;
                case OutsideClueEdge.Right:
                    for (var c = size - 1; c >= 0; c--) cells.Add(new CellCoord(clue.Index, c));
                    break;
                case OutsideClueEdge.DiagonalDownRight:
                case OutsideClueEdge.DiagonalDownLeft:
                {
                    var step = clue.Edge == OutsideClueEdge.DiagonalDownRight ? 1 : -1;
                    // From the top edge (FromStart) the index is the start column; otherwise the start row on the left
                    // (down-right) or right (down-left) edge.
                    var r = clue.FromStart ? 0 : clue.Index;
                    var c = clue.FromStart ? clue.Index : (step > 0 ? 0 : size - 1);
                    for (; r < size && c >= 0 && c < size; r++, c += step) cells.Add(new CellCoord(r, c));
                    break;
                }
            }
            return cells;
        }

        /// <summary>Whether the clue reads (row, col), without building its cell list.</summary>
        public static bool Reads(OutsideClue clue, int row, int col, int size)
        {
            switch (clue.Edge)
            {
                case OutsideClueEdge.Top:
                case OutsideClueEdge.Bottom:
                    return col == clue.Index;
                case OutsideClueEdge.Left:
                case OutsideClueEdge.Right:
                    return row == clue.Index;
                default:
                {
                    // A diagonal: the cell lies on it when its offset from the start cell is the same down and across.
                    var startRow = clue.FromStart ? 0 : clue.Index;
                    var startCol = clue.FromStart ? clue.Index : (clue.Edge == OutsideClueEdge.DiagonalDownRight ? 0 : size - 1);
                    var down = row - startRow;
                    var across = clue.Edge == OutsideClueEdge.DiagonalDownRight ? col - startCol : startCol - col;
                    return down >= 0 && down == across;
                }
            }
        }

        internal static bool Contains(List<CellCoord> cells, int row, int col)
        {
            foreach (var c in cells)
                if (c.Row == row && c.Col == col) return true;
            return false;
        }
    }

    // [REQ: BOSS-LIB-081] Skyscrapers: the clue counts the digits seen from its side, where a higher digit hides the lower
    // ones behind it. [REQ: BOSS-LIB-082] Sandwich: the clue is the sum of the digits between the 1 and the highest digit
    // of the row or column. [REQ: BOSS-LIB-083] X-Sums: the first digit from the clue's side says how many digits to add;
    // the clue is their sum. [REQ: BOSS-LIB-084] Little Killer: the digits along the arrow's diagonal add up to the clue
    // (digits may repeat on a diagonal).
    public sealed class OutsideClueRule : IOrderedConstraintRule
    {
        private readonly OutsideClueType _type;

        public OutsideClueRule(OutsideClueType type) => _type = type;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null || overlay.OutsideClues == null) return true;
            var size = board.Size;
            foreach (var clue in overlay.OutsideClues)
            {
                if (clue.Type != _type || !OutsideClueGeometry.Reads(clue, row, col, size)) continue;
                var cells = OutsideClueGeometry.Cells(clue, size);
                var digits = new int[cells.Count];
                for (var i = 0; i < cells.Count; i++)
                    digits[i] = cells[i].Row == row && cells[i].Col == col ? value : board.Cells[cells[i].Row, cells[i].Col];
                if (!Fits(digits, clue.Value, size)) return false;
            }
            return true;
        }

        private bool Fits(int[] digits, int clue, int size) => _type switch
        {
            OutsideClueType.Skyscrapers => SkyscrapersFit(digits, clue, size),
            OutsideClueType.Sandwich => SandwichFits(digits, clue, size),
            OutsideClueType.XSums => XSumsFit(digits, clue, size),
            OutsideClueType.LittleKiller => SumFits(digits, 0, digits.Length, clue, 1, size),
            _ => true
        };

        // The visible count of the filled run next to the clue is fixed; the cells behind it can add one building for
        // each digit above the highest so far, and must add at least one while the highest digit is still to come.
        internal static bool SkyscrapersFit(int[] digits, int clue, int size)
        {
            int seen = 0, highest = 0, prefix = 0;
            for (; prefix < digits.Length && digits[prefix] != 0; prefix++)
                if (digits[prefix] > highest) { highest = digits[prefix]; seen++; }
            if (highest == size || prefix == digits.Length) return seen == clue;
            var more = Math.Min(digits.Length - prefix, size - highest);
            return clue >= seen + 1 && clue <= seen + more;
        }

        // Once the 1 and the highest digit are placed, the digits between them (each 2 to size − 1) must still be able
        // to reach the clue.
        internal static bool SandwichFits(int[] digits, int clue, int size)
        {
            int one = Array.IndexOf(digits, 1), top = Array.IndexOf(digits, size);
            if (one < 0 || top < 0) return true;
            return SumFits(digits, Math.Min(one, top) + 1, Math.Max(one, top), clue, 2, size - 1);
        }

        internal static bool XSumsFit(int[] digits, int clue, int size)
        {
            var x = digits[0];
            if (x == 0) return true;
            return x <= digits.Length && SumFits(digits, 0, x, clue, 1, size);
        }

        // The digits in [from, to): the filled ones count as they are, each empty one as low to high.
        internal static bool SumFits(int[] digits, int from, int to, int clue, int low, int high)
        {
            int min = 0, max = 0;
            for (var i = from; i < to; i++)
            {
                if (digits[i] == 0) { min += low; max += high; }
                else { min += digits[i]; max += digits[i]; }
            }
            return clue >= min && clue <= max;
        }
    }

    // [REQ: BOSS-LIB-079] Quadruples: every digit in a corner circle appears in at least one of the four cells around it.
    public sealed class QuadrupleRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null || overlay.Quadruples == null) return true;
            foreach (var quad in overlay.Quadruples)
            {
                int top = quad.TopLeft.Row, left = quad.TopLeft.Col;
                if (row < top || row > top + 1 || col < left || col > left + 1) continue;
                var empty = 0;
                var present = 0;      // bit d: digit d is in one of the four cells
                for (var r = top; r <= top + 1; r++)
                for (var c = left; c <= left + 1; c++)
                {
                    var v = r == row && c == col ? value : board.Cells[r, c];
                    if (v == 0) empty++;
                    else present |= 1 << v;
                }
                var missing = 0;
                var listed = 0;     // each listed digit counts once, even if a loaded clue lists it twice
                foreach (var d in quad.Digits)
                {
                    if ((listed & (1 << d)) != 0) continue;
                    listed |= 1 << d;
                    if ((present & (1 << d)) == 0) missing++;
                }
                if (missing > empty) return false;
            }
            return true;
        }
    }
}
