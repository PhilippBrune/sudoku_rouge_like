using System;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuBacktrackingSolver
    {
        public static bool HasUniqueSolution(SudokuBoard board, ModifierOverlayData overlay = null,
            SudokuConstraintEngine engine = null)
        {
            return CountSolutions(board, 2, overlay, engine) == 1;
        }

        public static int CountSolutions(SudokuBoard board, int maxCount = 2,
            ModifierOverlayData overlay = null, SudokuConstraintEngine engine = null)
        {
            var size = board.Size;
            var grid = new int[size, size];
            Array.Copy(board.Cells, grid, board.Cells.Length);

            var count = 0;
            SolveCount(grid, board.RegionMap, size, overlay, engine, ref count, maxCount);
            return count;
        }

        public static bool TrySolve(SudokuBoard board, out int[,] solution,
            ModifierOverlayData overlay = null, SudokuConstraintEngine engine = null)
        {
            var size = board.Size;
            var grid = new int[size, size];
            Array.Copy(board.Cells, grid, board.Cells.Length);

            if (SolveFirst(grid, board.RegionMap, size, overlay, engine))
            {
                solution = grid;
                return true;
            }

            solution = null;
            return false;
        }

        private static void SolveCount(int[,] grid, int[,] regionMap, int size,
            ModifierOverlayData overlay, SudokuConstraintEngine engine, ref int count, int maxCount)
        {
            if (!FindEmpty(grid, size, out var row, out var col))
            {
                count++;
                return;
            }

            for (var v = 1; v <= size; v++)
            {
                if (!IsPlacementValid(grid, regionMap, size, row, col, v)) continue;
                // TODO: add constraint engine validation when SudokuBoard can be efficiently reused

                grid[row, col] = v;
                SolveCount(grid, regionMap, size, overlay, engine, ref count, maxCount);
                grid[row, col] = 0;
                if (count >= maxCount) return;
            }
        }

        private static bool SolveFirst(int[,] grid, int[,] regionMap, int size,
            ModifierOverlayData overlay, SudokuConstraintEngine engine)
        {
            if (!FindEmpty(grid, size, out var row, out var col))
                return true;

            for (var v = 1; v <= size; v++)
            {
                if (!IsPlacementValid(grid, regionMap, size, row, col, v)) continue;

                grid[row, col] = v;
                if (SolveFirst(grid, regionMap, size, overlay, engine))
                    return true;
                grid[row, col] = 0;
            }

            return false;
        }

        private static bool FindEmpty(int[,] grid, int size, out int row, out int col)
        {
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (grid[r, c] == 0)
                {
                    row = r;
                    col = c;
                    return true;
                }
            }

            row = -1;
            col = -1;
            return false;
        }

        private static bool IsPlacementValid(int[,] grid, int[,] regionMap, int size,
            int row, int col, int value)
        {
            for (var c = 0; c < size; c++)
                if (c != col && grid[row, c] == value) return false;

            for (var r = 0; r < size; r++)
                if (r != row && grid[r, col] == value) return false;

            var regionId = regionMap[row, col];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if ((r != row || c != col) && regionMap[r, c] == regionId && grid[r, c] == value)
                    return false;
            }

            return true;
        }
    }
}
