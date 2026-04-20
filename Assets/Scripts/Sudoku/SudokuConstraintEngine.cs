using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public sealed class SudokuConstraintEngine
    {
        private readonly List<IOrderedConstraintRule> _rules = new List<IOrderedConstraintRule>();

        public void RegisterRule(IOrderedConstraintRule rule)
        {
            _rules.Add(rule);
            _rules.Sort((a, b) => a.Category.CompareTo(b.Category));
        }

        public void ClearRules() => _rules.Clear();

        public bool ValidateAll(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlayData)
        {
            if (!SudokuValidator.IsMoveValid(board, row, col, value))
                return false;

            for (var i = 0; i < _rules.Count; i++)
            {
                if (!_rules[i].IsValid(board, row, col, value, overlayData))
                    return false;
            }

            return true;
        }

        public int RuleCount => _rules.Count;

        /// <summary>
        /// Returns true if the value already placed at (<paramref name="r"/>, <paramref name="c"/>)
        /// violates any active constraint (including modifier rules) given the current board state.
        /// Temporarily clears the cell, probes via ValidateAll, then restores.
        /// </summary>
        public bool HasConstraintConflict(SudokuBoard board, int r, int c, ModifierOverlayData overlayData)
        {
            var val = board.Cells[r, c];
            if (val == 0) return false;
            board.Cells[r, c] = 0;
            var valid = ValidateAll(board, r, c, val, overlayData);
            board.Cells[r, c] = val;
            return !valid;
        }

        /// <summary>
        /// Returns the set of already-placed cells that would be in conflict if
        /// <paramref name="value"/> were placed at (<paramref name="row"/>, <paramref name="col"/>).
        /// Works by temporarily writing the hypothetical value, probing every other placed cell
        /// through the full rule set, then restoring the board to its original state.
        /// </summary>
        public HashSet<(int, int)> ComputeConflictCells(
            SudokuBoard board, int row, int col, int value, ModifierOverlayData overlayData)
        {
            var result = new HashSet<(int, int)>();
            if (board == null || value == 0) return result;
            var sz    = board.Size;
            var prev  = board.Cells[row, col];
            board.Cells[row, col] = value;
            try
            {
                for (var r = 0; r < sz; r++)
                for (var c = 0; c < sz; c++)
                {
                    if (r == row && c == col) continue;
                    var v = board.Cells[r, c];
                    if (v == 0) continue;
                    board.Cells[r, c] = 0;
                    var valid = ValidateAll(board, r, c, v, overlayData);
                    board.Cells[r, c] = v;
                    if (!valid) result.Add((r, c));
                }
            }
            finally { board.Cells[row, col] = prev; }
            return result;
        }
    }
}
