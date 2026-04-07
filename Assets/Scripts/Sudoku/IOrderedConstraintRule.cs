using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public interface IOrderedConstraintRule
    {
        ConstraintRuleCategory Category { get; }
        bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlayData);
    }
}
