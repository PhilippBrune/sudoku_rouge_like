using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuGenerator
    {
        public static SudokuBoard CreatePuzzle(int size, float missingPercent, int seed,
            int regionVariant = 0, bool nonconsecutive = false, bool antiknight = false,
            bool nonconsecDiagonal = false)
        {
            var random = new Random(seed);
            var regionMap = BuildRegionMap(size, regionVariant);
            var solution = GenerateSolvedBoard(size, regionMap, random, nonconsecutive, antiknight, nonconsecDiagonal);
            var puzzle = (int[,])solution.Clone();

            var totalCells = size * size;
            // missingPercent == 1.0 means 7-star empty grid (tutorial only) — allow all cells removed
            var maxRemove = missingPercent >= 1.0f ? totalCells : totalCells - 1;
            var removeCount = Math.Clamp((int)Math.Round(totalCells * missingPercent), 1, maxRemove);
            var allIndices = new List<int>(totalCells);
            for (var i = 0; i < totalCells; i++)
                allIndices.Add(i);
            Shuffle(allIndices, random);

            for (var i = 0; i < removeCount; i++)
            {
                var index = allIndices[i];
                puzzle[index / size, index % size] = 0;
            }

            return new SudokuBoard(size, solution, puzzle, regionMap);
        }

        public static SudokuBoard CreatePuzzleWithUniquenessCheck(int size, float missingPercent, int seed,
            int regionVariant = 0, SudokuConstraintEngine constraintEngine = null, int maxAttempts = 5)
        {
            // Uniqueness solver not yet implemented; fall through to a single attempt.
            // TODO: when SudokuBacktrackingSolver is ready, restore the multi-attempt loop.
            return CreatePuzzle(size, missingPercent, seed, regionVariant);
        }

        private static int[,] GenerateSolvedBoard(int size, int[,] regionMap, Random random,
            bool nonconsecutive = false, bool antiknight = false, bool nonconsecDiagonal = false)
        {
            var board = new int[size, size];
            var rowMask = new int[size];
            var colMask = new int[size];
            var regionCount = CountDistinctRegions(regionMap, size);
            var regionMask = new int[Math.Max(regionCount, size)];

            if (!FillBoard(board, regionMap, size, rowMask, colMask, regionMask, random, nonconsecutive, antiknight, nonconsecDiagonal))
                throw new InvalidOperationException($"Failed to generate solved board for size {size}.");

            return board;
        }

        private static bool FillBoard(int[,] board, int[,] regionMap, int size,
            int[] rowMask, int[] colMask, int[] regionMask, Random random,
            bool nonconsecutive = false, bool antiknight = false, bool nonconsecDiagonal = false)
        {
            if (!FindNextCell(board, regionMap, size, rowMask, colMask, regionMask,
                    out var row, out var col, out var candidates))
                return true;

            Shuffle(candidates, random);
            var region = regionMap[row, col];

            for (var i = 0; i < candidates.Count; i++)
            {
                var value = candidates[i];

                // Global negative pre-checks (applied before committing to avoid dead ends)
                if (nonconsecutive && ViolatesNonconsecutive(board, row, col, value, size)) continue;
                if (antiknight && ViolatesAntiknight(board, row, col, value, size)) continue;
                if (nonconsecDiagonal && ViolatesNonconsecDiagonal(board, row, col, value, size)) continue;

                var bit = 1 << value;

                board[row, col] = value;
                rowMask[row] |= bit;
                colMask[col] |= bit;
                regionMask[region] |= bit;

                if (FillBoard(board, regionMap, size, rowMask, colMask, regionMask, random, nonconsecutive, antiknight, nonconsecDiagonal))
                    return true;

                board[row, col] = 0;
                rowMask[row] &= ~bit;
                colMask[col] &= ~bit;
                regionMask[region] &= ~bit;
            }

            return false;
        }

        private static bool ViolatesNonconsecutive(int[,] board, int row, int col, int value, int size)
        {
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };
            for (var d = 0; d < 4; d++)
            {
                var nr = row + dr[d]; var nc = col + dc[d];
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                var v = board[nr, nc];
                if (v != 0 && Math.Abs(value - v) == 1) return true;
            }
            return false;
        }

        private static bool ViolatesAntiknight(int[,] board, int row, int col, int value, int size)
        {
            int[] dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] dc = { -1, 1, -2, 2, -2, 2, -1, 1 };
            for (var i = 0; i < 8; i++)
            {
                var nr = row + dr[i]; var nc = col + dc[i];
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                if (board[nr, nc] == value) return true;
            }
            return false;
        }

        private static bool ViolatesNonconsecDiagonal(int[,] board, int row, int col, int value, int size)
        {
            int[] dr = { -1, -1, 1, 1 };
            int[] dc = { -1, 1, -1, 1 };
            for (var i = 0; i < 4; i++)
            {
                var nr = row + dr[i]; var nc = col + dc[i];
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                var v = board[nr, nc];
                if (v != 0 && Math.Abs(value - v) == 1) return true;
            }
            return false;
        }

        private static bool FindNextCell(int[,] board, int[,] regionMap, int size,
            int[] rowMask, int[] colMask, int[] regionMask,
            out int bestRow, out int bestCol, out List<int> bestCandidates)
        {
            bestRow = -1;
            bestCol = -1;
            bestCandidates = null;
            var bestCount = int.MaxValue;

            for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
            {
                if (board[row, col] != 0) continue;

                var region = regionMap[row, col];
                var usedMask = rowMask[row] | colMask[col] | regionMask[region];
                var candidates = new List<int>(size);
                for (var value = 1; value <= size; value++)
                {
                    if ((usedMask & (1 << value)) == 0)
                        candidates.Add(value);
                }

                if (candidates.Count == 0)
                {
                    bestRow = row;
                    bestCol = col;
                    bestCandidates = candidates;
                    return true;
                }

                if (candidates.Count < bestCount)
                {
                    bestCount = candidates.Count;
                    bestRow = row;
                    bestCol = col;
                    bestCandidates = candidates;
                    if (bestCount == 1) return true;
                }
            }

            return bestRow >= 0;
        }

        // ── Region Maps ──

        internal static int[,] BuildRegionMap(int size, int variant = 0)
        {
            var regionMap = new int[size, size];

            switch (size)
            {
                case 5:
                    FillTemplate(regionMap, Get5x5Template(variant));
                    return regionMap;
                case 6:
                    if (variant == 2) FillTemplate(regionMap, Get6x6Template());
                    else if (variant == 3) FillTemplate(regionMap, Get6x6TemplateB());
                    else if (variant % 2 == 0) FillRectangular(regionMap, size, 2, 3);
                    else FillRectangular(regionMap, size, 3, 2);
                    return regionMap;
                case 7:
                    FillTemplate(regionMap, Get7x7Template(variant));
                    return regionMap;
                case 8:
                    if (variant == 2) FillTemplate(regionMap, Get8x8Template());
                    else if (variant == 3) FillTemplate(regionMap, Get8x8TemplateB());
                    else if (variant % 2 == 0) FillRectangular(regionMap, size, 2, 4);
                    else FillRectangular(regionMap, size, 4, 2);
                    return regionMap;
                case 9:
                    if (variant == 2) FillTemplate(regionMap, Get9x9Template());
                    else if (variant == 3) FillTemplate(regionMap, Get9x9TemplateB());
                    else FillRectangular(regionMap, size, 3, 3);
                    return regionMap;
                default:
                    for (var r = 0; r < size; r++)
                    for (var c = 0; c < size; c++)
                        regionMap[r, c] = (r + c) % size;
                    return regionMap;
            }
        }

        // ── Templates ──

        private static int[,] Get5x5Template(int variant)
        {
            if (variant % 2 == 0)
                return new[,]
                {
                    { 0, 0, 1, 1, 1 },
                    { 0, 0, 2, 1, 1 },
                    { 0, 2, 2, 2, 3 },
                    { 4, 4, 2, 3, 3 },
                    { 4, 4, 4, 3, 3 }
                };
            return new[,]
            {
                { 0, 0, 0, 1, 1 },
                { 2, 2, 0, 0, 1 },
                { 2, 3, 3, 1, 1 },
                { 2, 3, 4, 4, 4 },
                { 2, 3, 3, 4, 4 }
            };
        }

        private static int[,] Get6x6Template()
        {
            return new[,]
            {
                { 0, 2, 2, 2, 1, 1 },
                { 0, 2, 2, 3, 1, 1 },
                { 0, 2, 3, 3, 3, 1 },
                { 0, 0, 0, 3, 3, 1 },
                { 4, 4, 4, 4, 5, 5 },
                { 4, 4, 5, 5, 5, 5 }
            };
        }

        private static int[,] Get6x6TemplateB()
        {
            return new[,]
            {
                { 0, 0, 2, 2, 2, 1 },
                { 0, 0, 3, 2, 2, 1 },
                { 0, 3, 3, 3, 2, 1 },
                { 0, 3, 3, 1, 1, 1 },
                { 5, 5, 4, 4, 4, 4 },
                { 5, 5, 5, 5, 4, 4 }
            };
        }

        private static int[,] Get7x7Template(int variant)
        {
            if (variant % 2 == 0)
                return new[,]
                {
                    { 0, 0, 0, 0, 1, 1, 1 },
                    { 2, 2, 0, 0, 0, 1, 1 },
                    { 2, 2, 2, 3, 3, 1, 1 },
                    { 4, 2, 2, 3, 3, 3, 5 },
                    { 4, 4, 6, 6, 3, 3, 5 },
                    { 4, 4, 6, 6, 6, 5, 5 },
                    { 4, 4, 6, 6, 5, 5, 5 }
                };
            return new[,]
            {
                { 4, 4, 4, 4, 2, 2, 0 },
                { 4, 4, 4, 2, 2, 2, 0 },
                { 6, 6, 6, 2, 2, 0, 0 },
                { 6, 6, 6, 3, 3, 0, 0 },
                { 5, 6, 3, 3, 3, 0, 1 },
                { 5, 5, 3, 3, 1, 1, 1 },
                { 5, 5, 5, 5, 1, 1, 1 }
            };
        }

        private static int[,] Get8x8Template()
        {
            return new[,]
            {
                { 0, 0, 0, 0, 2, 2, 2, 2 },
                { 0, 0, 1, 1, 3, 3, 2, 2 },
                { 0, 1, 1, 1, 3, 3, 3, 2 },
                { 0, 1, 1, 1, 3, 3, 3, 2 },
                { 4, 5, 5, 5, 7, 7, 7, 6 },
                { 4, 5, 5, 5, 7, 7, 7, 6 },
                { 4, 5, 5, 4, 6, 7, 7, 6 },
                { 4, 4, 4, 4, 6, 6, 6, 6 }
            };
        }

        private static int[,] Get8x8TemplateB()
        {
            return new[,]
            {
                { 0, 0, 0, 0, 1, 1, 1, 1 },
                { 0, 0, 0, 2, 2, 2, 2, 1 },
                { 0, 3, 3, 3, 2, 2, 2, 1 },
                { 4, 4, 3, 3, 3, 2, 1, 1 },
                { 4, 4, 4, 3, 3, 5, 5, 5 },
                { 4, 4, 4, 6, 6, 6, 6, 5 },
                { 7, 7, 7, 7, 6, 6, 6, 5 },
                { 7, 7, 7, 7, 6, 5, 5, 5 }
            };
        }

        private static int[,] Get9x9Template()
        {
            return new[,]
            {
                { 0, 0, 0, 0, 1, 1, 1, 1, 1 },
                { 0, 0, 0, 2, 2, 2, 2, 1, 1 },
                { 0, 0, 2, 2, 2, 2, 2, 3, 1 },
                { 4, 4, 4, 5, 5, 3, 3, 3, 1 },
                { 4, 4, 5, 5, 5, 5, 3, 3, 3 },
                { 4, 4, 5, 5, 5, 6, 6, 3, 3 },
                { 4, 4, 6, 6, 6, 6, 6, 7, 7 },
                { 8, 8, 6, 6, 8, 8, 7, 7, 7 },
                { 8, 8, 8, 8, 8, 7, 7, 7, 7 }
            };
        }

        private static int[,] Get9x9TemplateB()
        {
            return new[,]
            {
                { 1, 1, 1, 1, 1, 0, 0, 0, 0 },
                { 1, 1, 2, 2, 2, 2, 0, 0, 0 },
                { 1, 3, 2, 2, 2, 2, 2, 0, 0 },
                { 1, 3, 3, 3, 5, 5, 4, 4, 4 },
                { 3, 3, 3, 5, 5, 5, 5, 4, 4 },
                { 3, 3, 6, 6, 5, 5, 5, 4, 4 },
                { 7, 7, 6, 6, 6, 6, 6, 4, 4 },
                { 7, 7, 7, 8, 8, 6, 6, 8, 8 },
                { 7, 7, 7, 7, 8, 8, 8, 8, 8 }
            };
        }

        // ── Helpers ──

        private static void FillRectangular(int[,] regionMap, int size, int boxRows, int boxCols)
        {
            for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
                regionMap[row, col] = (row / boxRows) * (size / boxCols) + (col / boxCols);
        }

        private static void FillTemplate(int[,] regionMap, int[,] template)
        {
            var size = regionMap.GetLength(0);
            for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
                regionMap[row, col] = template[row, col];
        }

        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static int CountDistinctRegions(int[,] regionMap, int size)
        {
            var max = 0;
            for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
                if (regionMap[row, col] > max)
                    max = regionMap[row, col];
            return max + 1;
        }
    }
}
