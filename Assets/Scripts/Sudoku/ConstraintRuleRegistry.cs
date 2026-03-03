using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public interface IOrderedConstraintRule : IConstraintRule
    {
        ConstraintRuleCategory Category { get; }
        int Order { get; }
    }

    public static class ConstraintRuleRegistry
    {
        public static List<IConstraintRule> BuildDeterministicOrdered(IEnumerable<IOrderedConstraintRule> rules)
        {
            var list = new List<IOrderedConstraintRule>(rules);
            list.Sort((a, b) =>
            {
                var category = a.Category.CompareTo(b.Category);
                if (category != 0)
                {
                    return category;
                }

                return a.Order.CompareTo(b.Order);
            });

            var output = new List<IConstraintRule>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                output.Add(list[i]);
            }

            return output;
        }
    }
}
