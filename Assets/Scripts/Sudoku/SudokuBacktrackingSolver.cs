using System;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuBacktrackingSolver
    {
        public static bool HasUniqueSolution(SudokuBoard board, ModifierOverlayData overlay = null,
            SudokuConstraintEngine engine = null,
            GenerationDeadline deadline = default)
        {
            return CountSolutions(board, 2, overlay, engine, deadline) == 1;
        }

        public static int CountSolutions(SudokuBoard board, int maxCount = 2,
            ModifierOverlayData overlay = null, SudokuConstraintEngine engine = null,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            var size = board.Size;
            var grid = new int[size, size];
            Array.Copy(board.Cells, grid, board.Cells.Length);
            var workingBoard = new SudokuBoard(size, new int[size, size], grid, board.RegionMap);

            if (!ExistingCellsAreValid(workingBoard, overlay, engine, deadline))
                return 0;

            var count = 0;
            SolveCount(workingBoard, overlay, engine, ref count, maxCount, deadline);
            return count;
        }

        public static bool TrySolve(SudokuBoard board, out int[,] solution,
            ModifierOverlayData overlay = null, SudokuConstraintEngine engine = null,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            var size = board.Size;
            var grid = new int[size, size];
            Array.Copy(board.Cells, grid, board.Cells.Length);
            var workingBoard = new SudokuBoard(size, new int[size, size], grid, board.RegionMap);

            if (!ExistingCellsAreValid(workingBoard, overlay, engine, deadline))
            {
                solution = null;
                return false;
            }

            if (SolveFirst(workingBoard, overlay, engine, deadline))
            {
                solution = grid;
                return true;
            }

            solution = null;
            return false;
        }

        private static void SolveCount(SudokuBoard workingBoard, ModifierOverlayData overlay,
            SudokuConstraintEngine engine, ref int count, int maxCount,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            var grid = workingBoard.Cells;
            var size = workingBoard.Size;
            if (!FindMostConstrainedEmpty(workingBoard, overlay, engine, out var row, out var col, deadline))
            {
                count++;
                return;
            }

            for (var v = 1; v <= size; v++)
            {
                if (!IsCandidateValid(workingBoard, overlay, engine, row, col, v)) continue;

                grid[row, col] = v;
                SolveCount(workingBoard, overlay, engine, ref count, maxCount, deadline);
                grid[row, col] = 0;
                if (count >= maxCount) return;
            }
        }

        private static bool SolveFirst(SudokuBoard workingBoard, ModifierOverlayData overlay,
            SudokuConstraintEngine engine,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            var grid = workingBoard.Cells;
            var size = workingBoard.Size;
            if (!FindMostConstrainedEmpty(workingBoard, overlay, engine, out var row, out var col, deadline))
                return true;

            for (var v = 1; v <= size; v++)
            {
                if (!IsCandidateValid(workingBoard, overlay, engine, row, col, v)) continue;

                grid[row, col] = v;
                if (SolveFirst(workingBoard, overlay, engine, deadline))
                    return true;
                grid[row, col] = 0;
            }

            return false;
        }

        private static bool FindMostConstrainedEmpty(SudokuBoard board, ModifierOverlayData overlay,
            SudokuConstraintEngine engine, out int row, out int col,
            GenerationDeadline deadline = default)
        {
            row = -1;
            col = -1;
            var bestCandidateCount = int.MaxValue;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                deadline.ThrowIfExceeded();
                if (board.Cells[r, c] != 0) continue;

                var candidateCount = 0;
                for (var value = 1; value <= board.Size; value++)
                {
                    if (IsCandidateValid(board, overlay, engine, r, c, value))
                        candidateCount++;
                }

                if (candidateCount < bestCandidateCount)
                {
                    bestCandidateCount = candidateCount;
                    row = r;
                    col = c;
                    if (candidateCount == 0)
                        return true;
                    if (candidateCount == 1)
                        return true;
                }
            }

            if (row >= 0)
                return true;

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

        private static bool ExistingCellsAreValid(SudokuBoard board, ModifierOverlayData overlay,
            SudokuConstraintEngine engine,
            GenerationDeadline deadline = default)
        {
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                deadline.ThrowIfExceeded();
                var value = board.Cells[r, c];
                if (value == 0) continue;
                if (value < 1 || value > size) return false;

                board.Cells[r, c] = 0;
                var valid = engine != null
                    ? engine.ValidateAll(board, r, c, value, overlay)
                    : SudokuValidator.IsMoveValid(board, r, c, value);
                board.Cells[r, c] = value;

                if (!valid) return false;
            }

            return true;
        }

        private static bool IsCandidateValid(SudokuBoard board, ModifierOverlayData overlay,
            SudokuConstraintEngine engine, int row, int col, int value)
        {
            if (!IsPlacementValid(board.Cells, board.RegionMap, board.Size, row, col, value))
                return false;

            return engine == null || engine.ValidateAll(board, row, col, value, overlay);
        }
    }
}
