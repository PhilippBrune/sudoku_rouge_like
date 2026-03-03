using System;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuBacktrackingSolver
    {
        public static int CountSolutions(SudokuBoard board, int maxCount = 2)
        {
            var work = (int[,])board.Cells.Clone();
            return SolveCount(work, board.RegionMap, board.Size, maxCount);
        }

        private static int SolveCount(int[,] cells, int[,] regionMap, int size, int maxCount)
        {
            if (!FindEmpty(cells, size, out var row, out var col))
            {
                return 1;
            }

            var solutions = 0;
            for (var value = 1; value <= size; value++)
            {
                if (!IsValid(cells, regionMap, size, row, col, value))
                {
                    continue;
                }

                cells[row, col] = value;
                solutions += SolveCount(cells, regionMap, size, maxCount);
                if (solutions >= maxCount)
                {
                    cells[row, col] = 0;
                    return solutions;
                }

                cells[row, col] = 0;
            }

            return solutions;
        }

        private static bool FindEmpty(int[,] cells, int size, out int row, out int col)
        {
            for (row = 0; row < size; row++)
            {
                for (col = 0; col < size; col++)
                {
                    if (cells[row, col] == 0)
                    {
                        return true;
                    }
                }
            }

            row = -1;
            col = -1;
            return false;
        }

        private static bool IsValid(int[,] cells, int[,] regionMap, int size, int row, int col, int value)
        {
            for (var i = 0; i < size; i++)
            {
                if (cells[row, i] == value || cells[i, col] == value)
                {
                    return false;
                }
            }

            var region = regionMap[row, col];
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (regionMap[r, c] == region && cells[r, c] == value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
