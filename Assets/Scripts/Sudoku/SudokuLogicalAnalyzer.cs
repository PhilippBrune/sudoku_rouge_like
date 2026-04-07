namespace SudokuRoguelike.Sudoku
{
    public static class SudokuLogicalAnalyzer
    {
        public static AnalysisResult Analyze(SudokuBoard board)
        {
            var size = board.Size;
            var nakedSingles = 0;
            var hiddenSingles = 0;

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (board.Cells[r, c] != 0) continue;

                var candidateCount = CountCandidates(board, r, c);
                if (candidateCount == 1)
                    nakedSingles++;
                else if (HasHiddenSingle(board, r, c))
                    hiddenSingles++;
            }

            return new AnalysisResult
            {
                NakedSingles = nakedSingles,
                HiddenSingles = hiddenSingles,
                EmptyCells = board.EmptyCellCount()
            };
        }

        private static int CountCandidates(SudokuBoard board, int row, int col)
        {
            var size = board.Size;
            var count = 0;
            for (var v = 1; v <= size; v++)
            {
                if (SudokuValidator.IsMoveValid(board, row, col, v))
                    count++;
            }
            return count;
        }

        private static bool HasHiddenSingle(SudokuBoard board, int row, int col)
        {
            var size = board.Size;
            for (var v = 1; v <= size; v++)
            {
                if (!SudokuValidator.IsMoveValid(board, row, col, v)) continue;

                // Check if v can only go in this cell within the row
                var uniqueInRow = true;
                for (var c = 0; c < size; c++)
                {
                    if (c == col || board.Cells[row, c] != 0) continue;
                    if (SudokuValidator.IsMoveValid(board, row, c, v))
                    {
                        uniqueInRow = false;
                        break;
                    }
                }
                if (uniqueInRow) return true;

                // Check if v can only go in this cell within the column
                var uniqueInCol = true;
                for (var r = 0; r < size; r++)
                {
                    if (r == row || board.Cells[r, col] != 0) continue;
                    if (SudokuValidator.IsMoveValid(board, r, col, v))
                    {
                        uniqueInCol = false;
                        break;
                    }
                }
                if (uniqueInCol) return true;

                // Check if v can only go in this cell within the region
                var regionId = board.RegionMap[row, col];
                var uniqueInRegion = true;
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                {
                    if ((r == row && c == col) || board.RegionMap[r, c] != regionId || board.Cells[r, c] != 0) continue;
                    if (SudokuValidator.IsMoveValid(board, r, c, v))
                    {
                        uniqueInRegion = false;
                        break;
                    }
                    if (!uniqueInRegion) break;
                }
                if (uniqueInRegion) return true;
            }

            return false;
        }

        public struct AnalysisResult
        {
            public int NakedSingles;
            public int HiddenSingles;
            public int EmptyCells;

            public float SinglesRatio =>
                EmptyCells > 0 ? (float)(NakedSingles + HiddenSingles) / EmptyCells : 0f;
        }
    }
}
