using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuDifficultyGrader
    {
        public static DifficultyTier GradePuzzle(SudokuBoard board)
        {
            var size = board.Size;
            var empty = board.EmptyCellCount();
            var total = size * size;
            var percent = (float)empty / total;

            if (percent < 0.35f) return DifficultyTier.Diff1;
            if (percent < 0.50f) return DifficultyTier.Diff2;
            if (percent < 0.65f) return DifficultyTier.Diff3;
            if (percent < 0.80f) return DifficultyTier.Diff4;
            return DifficultyTier.Diff5;
        }

        public static int EstimateScore(SudokuBoard board, int modifierCount, bool isBoss)
        {
            var baseDifficulty = (int)GradePuzzle(board) + 1;
            var modBonus = modifierCount * 2;
            var bossBonus = isBoss ? 3 : 0;
            return baseDifficulty + modBonus + bossBonus;
        }
    }
}
