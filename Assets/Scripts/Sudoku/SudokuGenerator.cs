using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuGenerator
    {
        public static SudokuBoard CreatePuzzle(int size, float missingPercent, int seed, int regionVariant = 0)
        {
            var random = new Random(seed);
            var regionMap = BuildRegionMap(size, regionVariant);
            var solution = GenerateSolvedBoard(size, regionMap, random);
            var puzzle = (int[,])solution.Clone();

            var totalCells = size * size;
            var removeCount = Math.Clamp((int)Math.Round(totalCells * missingPercent), 1, totalCells - 1);
            var startDigitsTarget = totalCells - removeCount;
            UnityEngine.Debug.Log(
                $"[SudokuGenerator] BoardSize:{size} Missing:{missingPercent * 100f:F0}% " +
                $"StartDigitsTarget:{startDigitsTarget} RemoveCount:{removeCount} TotalCells:{totalCells}");
            var allIndices = new List<int>(totalCells);

            for (var i = 0; i < totalCells; i++)
            {
                allIndices.Add(i);
            }

            Shuffle(allIndices, random);

            var givenMask = new bool[size, size];
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    givenMask[row, col] = true;
                }
            }

            for (var i = 0; i < removeCount; i++)
            {
                var index = allIndices[i];
                var row = index / size;
                var col = index % size;
                puzzle[row, col] = 0;
                givenMask[row, col] = false;
            }

            var actualGiven = 0;
            for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    if (puzzle[r, c] != 0) actualGiven++;
            UnityEngine.Debug.Log($"[SudokuGenerator] ActualStartDigits:{actualGiven} (target:{startDigitsTarget})");

            return new SudokuBoard(size, solution, puzzle, givenMask, regionMap);
        }

        private static int[,] GenerateSolvedBoard(int size, int[,] regionMap, Random random)
        {
            var board = new int[size, size];
            var rowMask = new int[size];
            var colMask = new int[size];
            var regionCount = CountDistinctRegions(regionMap, size);
            var regionMask = new int[Math.Max(regionCount, size)];

            var solved = FillBoard(board, regionMap, size, rowMask, colMask, regionMask, random);
            if (!solved)
            {
                throw new InvalidOperationException($"Failed to generate solved board for size {size}.");
            }

            return board;
        }

        private static bool FillBoard(int[,] board, int[,] regionMap, int size, int[] rowMask, int[] colMask, int[] regionMask, Random random)
        {
            if (!FindNextCell(board, regionMap, size, rowMask, colMask, regionMask, out var row, out var col, out var candidates))
            {
                return true;
            }

            Shuffle(candidates, random);
            var region = regionMap[row, col];

            for (var i = 0; i < candidates.Count; i++)
            {
                var value = candidates[i];
                var bit = 1 << value;

                board[row, col] = value;
                rowMask[row] |= bit;
                colMask[col] |= bit;
                regionMask[region] |= bit;

                if (FillBoard(board, regionMap, size, rowMask, colMask, regionMask, random))
                {
                    return true;
                }

                board[row, col] = 0;
                rowMask[row] &= ~bit;
                colMask[col] &= ~bit;
                regionMask[region] &= ~bit;
            }

            return false;
        }

        internal static int[,] BuildRegionMap(int size, int variant = 0)
        {
            var regionMap = new int[size, size];

            if (size == 6)
            {
                if (variant == 2)
                    FillTemplateRegions(regionMap, Get6x6Template());
                else if (variant == 3)
                    FillTemplateRegions(regionMap, Get6x6TemplateB());
                else if (variant % 2 == 0)
                    FillRectangularRegions(regionMap, size, 2, 3);
                else
                    FillRectangularRegions(regionMap, size, 3, 2);
                return regionMap;
            }

            if (size == 8)
            {
                if (variant == 2)
                    FillTemplateRegions(regionMap, Get8x8Template());
                else if (variant == 3)
                    FillTemplateRegions(regionMap, Get8x8TemplateB());
                else if (variant % 2 == 0)
                    FillRectangularRegions(regionMap, size, 2, 4);
                else
                    FillRectangularRegions(regionMap, size, 4, 2);
                return regionMap;
            }

            if (size == 5)
            {
                FillTemplateRegions(regionMap, Get5x5Template(variant));
                return regionMap;
            }

            if (size == 7)
            {
                FillTemplateRegions(regionMap, Get7x7Template(variant));
                return regionMap;
            }

            var boxRoot = (int)Math.Sqrt(size);
            if (boxRoot * boxRoot == size)
            {
                if (size == 9 && variant == 2)
                    FillTemplateRegions(regionMap, Get9x9Template());
                else if (size == 9 && variant == 3)
                    FillTemplateRegions(regionMap, Get9x9TemplateB());
                else
                    FillRectangularRegions(regionMap, size, boxRoot, boxRoot);
                return regionMap;
            }

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    regionMap[row, col] = (row + col) % size;
                }
            }

            return regionMap;
        }

        private static int[,] Get5x5Template(int variant)
        {
            if (variant % 2 == 0)
            {
                return new[,]
                {
                    { 0, 0, 1, 1, 1 },
                    { 0, 0, 2, 1, 1 },
                    { 0, 2, 2, 2, 3 },
                    { 4, 4, 2, 3, 3 },
                    { 4, 4, 4, 3, 3 }
                };
            }

            return new[,]
            {
                { 0, 0, 0, 1, 1 },
                { 2, 2, 0, 0, 1 },
                { 2, 3, 3, 1, 1 },
                { 2, 3, 4, 4, 4 },
                { 2, 3, 3, 4, 4 }
            };
        }

        private static int[,] Get7x7Template(int variant)
        {
            if (variant % 2 == 0)
            {
                // Horizontal wave bands — 7 regions, 7 cells each, all contiguous.
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
            }

            // Vertical wave bands (90° rotation of template 0).
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

        private static int[,] Get6x6TemplateB()
        {
            // Mirror of Get6x6Template — 6 regions of 6 cells each, all contiguous
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

        private static int[,] Get8x8TemplateB()
        {
            // Staircase jigsaw — 8 regions of 8 cells each, all contiguous
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

        private static int[,] Get9x9TemplateB()
        {
            // Mirror of Get9x9Template — 9 regions of 9 cells each, all contiguous
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

        private static void FillRectangularRegions(int[,] regionMap, int size, int boxRows, int boxCols)
        {
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var region = (row / boxRows) * (size / boxCols) + (col / boxCols);
                    regionMap[row, col] = region;
                }
            }
        }

        private static void FillTemplateRegions(int[,] regionMap, int[,] template)
        {
            var size = regionMap.GetLength(0);
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    regionMap[row, col] = template[row, col];
                }
            }
        }

        private static List<int> CreateIndexList(int size)
        {
            var list = new List<int>(size);
            for (var i = 0; i < size; i++)
            {
                list.Add(i);
            }

            return list;
        }

        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static bool FindNextCell(int[,] board, int[,] regionMap, int size, int[] rowMask, int[] colMask, int[] regionMask, out int bestRow, out int bestCol, out List<int> bestCandidates)
        {
            bestRow = -1;
            bestCol = -1;
            bestCandidates = null;

            var bestCount = int.MaxValue;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    if (board[row, col] != 0)
                    {
                        continue;
                    }

                    var region = regionMap[row, col];
                    var usedMask = rowMask[row] | colMask[col] | regionMask[region];
                    var candidates = new List<int>(size);
                    for (var value = 1; value <= size; value++)
                    {
                        var bit = 1 << value;
                        if ((usedMask & bit) == 0)
                        {
                            candidates.Add(value);
                        }
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
                        if (bestCount == 1)
                        {
                            return true;
                        }
                    }
                }
            }

            return bestRow >= 0;
        }

        private static int CountDistinctRegions(int[,] regionMap, int size)
        {
            var max = 0;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    if (regionMap[row, col] > max)
                    {
                        max = regionMap[row, col];
                    }
                }
            }

            return max + 1;
        }

        // ── Post-Removal Uniqueness Checker ──

        /// <summary>
        /// Creates a puzzle and verifies it has a unique solution given base Sudoku rules
        /// plus any active constraint rules. If not unique, regenerates with a shifted seed.
        /// Used for Spirit Trials (always) and optionally for Garden Run boss puzzles.
        /// </summary>
        public static SudokuBoard CreatePuzzleWithUniquenessCheck(int size, float missingPercent, int seed,
            int regionVariant = 0, SudokuConstraintEngine constraintEngine = null, int maxAttempts = 5)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var board = CreatePuzzle(size, missingPercent, seed + attempt, regionVariant);
                if (constraintEngine == null || HasUniqueSolution(board, constraintEngine))
                    return board;

                UnityEngine.Debug.Log($"[SudokuGenerator] Uniqueness check failed (attempt {attempt + 1}), retrying with shifted seed");
            }

            // Fallback: return last attempt even if not unique
            UnityEngine.Debug.LogWarning($"[SudokuGenerator] Could not find unique-solution puzzle after {maxAttempts} attempts");
            return CreatePuzzle(size, missingPercent, seed + maxAttempts, regionVariant);
        }

        /// <summary>
        /// Checks if the puzzle has exactly one solution by running a backtracking solver.
        /// Returns true if unique, false if >1 solutions found.
        /// </summary>
        public static bool HasUniqueSolution(SudokuBoard board, SudokuConstraintEngine engine)
        {
            var size = board.Size;
            var grid = new int[size, size];
            for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    grid[r, c] = board.GetCell(r, c);

            var solutionCount = 0;
            CountSolutions(grid, board, engine, size, ref solutionCount, 2);
            return solutionCount == 1;
        }

        private static bool CountSolutions(int[,] grid, SudokuBoard board, SudokuConstraintEngine engine,
            int size, ref int count, int maxCount)
        {
            // Find next empty cell
            var bestRow = -1;
            var bestCol = -1;
            var minCandidates = size + 1;

            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (grid[r, c] != 0) continue;
                    var candidates = CountCandidates(grid, board, engine, size, r, c);
                    if (candidates < minCandidates)
                    {
                        minCandidates = candidates;
                        bestRow = r;
                        bestCol = c;
                    }
                }
            }

            // All cells filled — found a solution
            if (bestRow == -1)
            {
                count++;
                return count >= maxCount;
            }

            // No candidates — dead end
            if (minCandidates == 0) return false;

            // Try each value
            for (var v = 1; v <= size; v++)
            {
                if (!IsValidPlacement(grid, board, engine, size, bestRow, bestCol, v))
                    continue;

                grid[bestRow, bestCol] = v;
                if (CountSolutions(grid, board, engine, size, ref count, maxCount))
                {
                    grid[bestRow, bestCol] = 0;
                    return true;
                }
                grid[bestRow, bestCol] = 0;
            }

            return false;
        }

        private static int CountCandidates(int[,] grid, SudokuBoard board, SudokuConstraintEngine engine,
            int size, int row, int col)
        {
            var count = 0;
            for (var v = 1; v <= size; v++)
            {
                if (IsValidPlacement(grid, board, engine, size, row, col, v))
                    count++;
            }
            return count;
        }

        private static bool IsValidPlacement(int[,] grid, SudokuBoard board, SudokuConstraintEngine engine,
            int size, int row, int col, int value)
        {
            // Row check
            for (var c = 0; c < size; c++)
                if (grid[row, c] == value) return false;

            // Column check
            for (var r = 0; r < size; r++)
                if (grid[r, col] == value) return false;

            // Region check
            var regionId = board.RegionMap[row, col];
            for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    if (board.RegionMap[r, c] == regionId && grid[r, c] == value) return false;

            // Constraint engine rules (modifiers)
            if (engine != null)
            {
                // Temporarily place the value to check constraints
                var prevVal = grid[row, col];
                grid[row, col] = value;
                var valid = engine.ValidateAll(board, row, col, value);
                grid[row, col] = prevVal;
                if (!valid) return false;
            }

            return true;
        }
    }
}
