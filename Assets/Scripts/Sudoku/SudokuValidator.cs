using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuValidator
    {
        public static bool IsMoveValid(SudokuBoard board, int row, int col, int value, SudokuConstraintEngine extraConstraints = null)
        {
            if (value < 1 || value > board.Size)
            {
                return false;
            }

            if (board.IsGiven(row, col))
            {
                return false;
            }

            if (!IsRowValid(board, row, col, value))
            {
                return false;
            }

            if (!IsColumnValid(board, row, col, value))
            {
                return false;
            }

            if (!IsRegionValid(board, row, col, value))
            {
                return false;
            }

            return extraConstraints == null || extraConstraints.ValidateAll(board, row, col, value);
        }

        public static List<int> GetCandidates(SudokuBoard board, int row, int col, SudokuConstraintEngine extraConstraints = null)
        {
            var candidates = new List<int>();

            if (board.GetCell(row, col) != 0)
            {
                return candidates;
            }

            for (var value = 1; value <= board.Size; value++)
            {
                if (IsMoveValid(board, row, col, value, extraConstraints))
                {
                    candidates.Add(value);
                }
            }

            return candidates;
        }

        private static bool IsRowValid(SudokuBoard board, int row, int col, int value)
        {
            for (var currentCol = 0; currentCol < board.Size; currentCol++)
            {
                if (currentCol == col)
                {
                    continue;
                }

                if (board.GetCell(row, currentCol) == value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsColumnValid(SudokuBoard board, int row, int col, int value)
        {
            for (var currentRow = 0; currentRow < board.Size; currentRow++)
            {
                if (currentRow == row)
                {
                    continue;
                }

                if (board.GetCell(currentRow, col) == value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsRegionValid(SudokuBoard board, int row, int col, int value)
        {
            var targetRegion = board.RegionMap[row, col];

            for (var currentRow = 0; currentRow < board.Size; currentRow++)
            {
                for (var currentCol = 0; currentCol < board.Size; currentCol++)
                {
                    if (currentRow == row && currentCol == col)
                    {
                        continue;
                    }

                    if (board.RegionMap[currentRow, currentCol] == targetRegion && board.GetCell(currentRow, currentCol) == value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
