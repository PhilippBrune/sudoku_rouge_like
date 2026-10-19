using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // Cage variants of the modifier catalog (docs/design/modifier-catalog-plan.md, BossMechanicsSystem.md "Arithmetic /
    // Cage Variants"). Every cage holds no repeated digit. Each rule reads only the digits already on the board, so it is
    // safe on partial boards: a digit is rejected only when it breaks the rule with the digits already placed.

    internal static class CageRuleSupport
    {
        internal static bool Contains(KillerCage cage, int row, int col)
        {
            for (var j = 0; j < cage.Cells.Count; j++)
                if (cage.Cells[j].Row == row && cage.Cells[j].Col == col) return true;
            return false;
        }

        /// <summary>
        /// Walks the cage with <paramref name="value"/> in (row, col): false when the digit repeats in the cage. Returns the
        /// filled digits' minimum, maximum, sum, product and count through the out parameters.
        /// </summary>
        internal static bool Scan(SudokuBoard board, KillerCage cage, int row, int col, int value,
            out int min, out int max, out int filled, out long product)
        {
            min = value; max = value; filled = 1; product = value;
            for (var j = 0; j < cage.Cells.Count; j++)
            {
                var c = cage.Cells[j];
                if (c.Row == row && c.Col == col) continue;
                var v = board.Cells[c.Row, c.Col];
                if (v == 0) continue;
                if (v == value) return false;
                min = Math.Min(min, v);
                max = Math.Max(max, v);
                product *= v;
                filled++;
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-064] Hidden Killer: a cage without a sum holds no repeated digit. The sum it was cut from stays
    // hidden: the rule checks only what the player can see, so a conflict never gives the sum away.
    // [REQ: BOSS-LIB-065] Product Cage: the digits multiply to the label.
    // [REQ: BOSS-LIB-066] Difference Cage: two cells that differ by the label.
    // [REQ: BOSS-LIB-067] Ratio Cage: two cells, the larger is the label times the smaller.
    // [REQ: BOSS-LIB-069] Renban Cage: the digits form a run of consecutive numbers in any order.
    public sealed class TypedCageRule : IOrderedConstraintRule
    {
        private readonly KillerCageType _type;

        public TypedCageRule(KillerCageType type) => _type = type;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Arithmetic;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.KillerCages.Count; i++)
            {
                var cage = overlay.KillerCages[i];
                if (cage.CageType != _type || !CageRuleSupport.Contains(cage, row, col)) continue;
                if (!CageRuleSupport.Scan(board, cage, row, col, value, out var min, out var max, out var filled, out var product))
                    return false;
                if (!Fits(cage, board.Size, value, min, max, filled, product)) return false;
            }
            return true;
        }

        private bool Fits(KillerCage cage, int size, int value, int min, int max, int filled, long product)
        {
            var complete = filled == cage.Cells.Count;
            switch (_type)
            {
                case KillerCageType.Product:
                    // The digits still missing are whole numbers, so the product so far must divide the label.
                    if (cage.Sum % product != 0) return false;
                    return !complete || product == cage.Sum;
                case KillerCageType.Difference:
                    if (filled == 2) return max - min == cage.Sum;
                    return value + cage.Sum <= size || value - cage.Sum >= 1;
                case KillerCageType.Ratio:
                    if (cage.Sum < 2) return true;   // no ratio stored (an old or damaged save): nothing to check
                    if (filled == 2) return max == min * cage.Sum;
                    return value * cage.Sum <= size || value % cage.Sum == 0;
                case KillerCageType.RenbanSet:
                    return max - min <= cage.Cells.Count - 1;
                default:
                    return true;   // HiddenSum: no repeats is the whole visible rule
            }
        }
    }
}
