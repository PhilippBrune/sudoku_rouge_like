namespace SudokuRoguelike.Sudoku
{
    public static class SudokuValidator
    {
        public static bool IsMoveValid(SudokuBoard board, int row, int col, int value)
        {
            var size = board.Size;

            for (var c = 0; c < size; c++)
                if (c != col && board.Cells[row, c] == value) return false;

            for (var r = 0; r < size; r++)
                if (r != row && board.Cells[r, col] == value) return false;

            var regionId = board.RegionMap[row, col];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if ((r != row || c != col) && board.RegionMap[r, c] == regionId && board.Cells[r, c] == value)
                    return false;

            return true;
        }

        public static bool IsValidBoard(SudokuBoard board)
        {
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = board.Solution[r, c];
                if (v < 1 || v > size) return false;
            }

            for (var r = 0; r < size; r++)
            {
                var seen = new bool[size + 1];
                for (var c = 0; c < size; c++)
                {
                    var v = board.Solution[r, c];
                    if (seen[v]) return false;
                    seen[v] = true;
                }
            }

            for (var c = 0; c < size; c++)
            {
                var seen = new bool[size + 1];
                for (var r = 0; r < size; r++)
                {
                    var v = board.Solution[r, c];
                    if (seen[v]) return false;
                    seen[v] = true;
                }
            }

            return true;
        }
    }
}
