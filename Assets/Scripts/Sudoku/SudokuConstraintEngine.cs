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
    }
}
