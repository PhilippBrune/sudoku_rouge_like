using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    public interface IConstraintRule
    {
        bool ValidateMove(SudokuBoard board, int row, int col, int value);
    }

    public sealed class SudokuConstraintEngine
    {
        private readonly List<IConstraintRule> _rules = new();

        public void SetRules(IEnumerable<IConstraintRule> rules)
        {
            _rules.Clear();
            _rules.AddRange(rules);
        }

        public void SetRulesDeterministic(IEnumerable<IOrderedConstraintRule> rules)
        {
            _rules.Clear();
            _rules.AddRange(ConstraintRuleRegistry.BuildDeterministicOrdered(rules));
        }

        public bool ValidateAll(SudokuBoard board, int row, int col, int value)
        {
            foreach (var rule in _rules)
            {
                if (!rule.ValidateMove(board, row, col, value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
