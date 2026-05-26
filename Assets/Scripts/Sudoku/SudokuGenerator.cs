using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuGenerator
    {
        public static SudokuBoard CreatePuzzle(int size, float missingPercent, int seed,
            int regionVariant = 0, bool nonconsecutive = false, bool antiknight = false,
            bool nonconsecDiagonal = false, bool antibishop = false,
            bool antiking = false, bool distanceGe2 = false)
        {
            var random = new Random(seed);
            var regionMap = BuildRegionMap(size, regionVariant);
            var solution = GenerateSolvedBoard(size, regionMap, random, nonconsecutive, antiknight,
                nonconsecDiagonal, antibishop, antiking, distanceGe2);
            var puzzle = (int[,])solution.Clone();

            var totalCells = size * size;
            // [REQ: GEN-REMOVE-004] 7★ (missingPercent == 1.0) bypasses the totalCells−1 clamp — all cells may be removed
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

        /// <summary>
        /// Generates a puzzle guaranteed to have exactly one valid solution.
        /// Clue removal is done cell-by-cell in shuffled order; each tentative removal is
        /// accepted only when uniqueness is confirmed via:
        ///   Phase 1 — naked-singles constraint propagation (O(n³), fast path).
        ///   Phase 2 — bounded backtracking capped at 2 solutions on the propagation-reduced
        ///              board (only reached for harder eliminations).
        /// The resulting clue count may be slightly higher than <paramref name="missingPercent"/>
        /// demands when no further unique removal is possible.
        /// </summary>
        // [REQ: GEN-REMOVE-001] Cell-by-cell removal with uniqueness guarantee (Phase 1: propagation, Phase 2: bounded backtracking)
        public static SudokuBoard CreatePuzzleWithUniquenessCheck(int size, float missingPercent, int seed,
            int regionVariant = 0, bool nonconsecutive = false, bool antiknight = false,
            bool nonconsecDiagonal = false, bool antibishop = false,
            bool antiking = false, bool distanceGe2 = false,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            var random = new Random(seed);
            var regionMap = BuildRegionMap(size, regionVariant);
            var solution = GenerateSolvedBoard(size, regionMap, random, nonconsecutive, antiknight,
                nonconsecDiagonal, antibishop, antiking, distanceGe2, deadline);

            var totalCells = size * size;

            // Tutorial empty grid (missingPercent == 1.0) — uniqueness not applicable.
            if (missingPercent >= 1.0f)
            {
                var emptyPuzzle = new int[size, size];
                return new SudokuBoard(size, solution, emptyPuzzle, regionMap);
            }

            var targetRemove = Math.Clamp((int)Math.Round(totalCells * missingPercent), 1, totalCells - 1);
            var indices = new List<int>(totalCells);
            for (var i = 0; i < totalCells; i++) indices.Add(i);
            Shuffle(indices, random);

            var puzzle = (int[,])solution.Clone();
            var removed = 0;

            for (var i = 0; i < totalCells && removed < targetRemove; i++)
            {
                deadline.ThrowIfExceeded();
                var idx = indices[i];
                var row = idx / size;
                var col = idx % size;
                var saved = puzzle[row, col];
                puzzle[row, col] = 0;

                if (HasUniqueSolution(puzzle, regionMap, size, nonconsecutive, antiknight,
                        nonconsecDiagonal, antibishop, antiking, distanceGe2, deadline))
                {
                    removed++;
                }
                else
                {
                    puzzle[row, col] = saved; // restore — removal breaks uniqueness
                }
            }

            return new SudokuBoard(size, solution, puzzle, regionMap);
        }

        // [REQ: GEN-SOLVE-001] GenerateSolvedBoard: seeded shuffle + up to 64 retries; delegates to FillBoard (MRV backtracking)
        private static int[,] GenerateSolvedBoard(int size, int[,] regionMap, Random random,
            bool nonconsecutive = false, bool antiknight = false, bool nonconsecDiagonal = false,
            bool antibishop = false, bool antiking = false, bool distanceGe2 = false,
            GenerationDeadline deadline = default)
        {
            const int maxAttempts = 64;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                deadline.ThrowIfExceeded();
                var board = new int[size, size];
                var rowMask = new int[size];
                var colMask = new int[size];
                var regionCount = CountDistinctRegions(regionMap, size);
                var regionMask = new int[Math.Max(regionCount, size)];

                if (FillBoard(board, regionMap, size, rowMask, colMask, regionMask, random,
                        nonconsecutive, antiknight, nonconsecDiagonal, antibishop, antiking, distanceGe2,
                        deadline))
                    return board;

                // Reseed with a deterministic but different value so retries are reproducible
                random = new Random(random.Next());
            }

            throw new InvalidOperationException(
                $"Failed to generate solved board for size {size} after {maxAttempts} attempts " +
                $"(nonconsecutive={nonconsecutive}, antiknight={antiknight}, " +
                $"nonconsecDiagonal={nonconsecDiagonal}, antibishop={antibishop}, " +
                $"antiking={antiking}, distanceGe2={distanceGe2}).");
        }

        // [REQ: GEN-SOLVE-001] FillBoard: MRV (minimum remaining values) + seeded shuffle of candidates + recursive backtracking
        private static bool FillBoard(int[,] board, int[,] regionMap, int size,
            int[] rowMask, int[] colMask, int[] regionMask, Random random,
            bool nonconsecutive = false, bool antiknight = false, bool nonconsecDiagonal = false,
            bool antibishop = false, bool antiking = false, bool distanceGe2 = false,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            if (!FindNextCell(board, regionMap, size, rowMask, colMask, regionMask,
                    nonconsecutive, antiknight, nonconsecDiagonal, antibishop, antiking, distanceGe2,
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
                if (antibishop && ViolatesAntiBishop(board, row, col, value, size)) continue;
                if ((antiking || distanceGe2) && ViolatesAntiking(board, row, col, value, size)) continue;

                var bit = 1 << value;

                board[row, col] = value;
                rowMask[row] |= bit;
                colMask[col] |= bit;
                regionMask[region] |= bit;

                if (FillBoard(board, regionMap, size, rowMask, colMask, regionMask, random,
                        nonconsecutive, antiknight, nonconsecDiagonal, antibishop, antiking, distanceGe2,
                        deadline))
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

        private static bool ViolatesAntiking(int[,] board, int row, int col, int value, int size)
        {
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                var nr = row + dr;
                var nc = col + dc;
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

        private static bool ViolatesAntiBishop(int[,] board, int row, int col, int value, int size)
        {
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (r == row && c == col) continue;
                if (Math.Abs(r - row) != Math.Abs(c - col)) continue;
                if (board[r, c] == value) return true;
            }
            return false;
        }

        private static bool FindNextCell(int[,] board, int[,] regionMap, int size,
            int[] rowMask, int[] colMask, int[] regionMask,
            bool nonconsecutive, bool antiknight, bool nonconsecDiagonal,
            bool antibishop, bool antiking, bool distanceGe2,
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
                    if ((usedMask & (1 << value)) != 0) continue;
                    if (!AllowsStructuralPlacement(board, row, col, value, size,
                            nonconsecutive, antiknight, nonconsecDiagonal,
                            antibishop, antiking, distanceGe2))
                        continue;
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

        private static bool AllowsStructuralPlacement(int[,] board, int row, int col, int value, int size,
            bool nonconsecutive, bool antiknight, bool nonconsecDiagonal,
            bool antibishop, bool antiking, bool distanceGe2)
        {
            if (nonconsecutive && ViolatesNonconsecutive(board, row, col, value, size)) return false;
            if (antiknight && ViolatesAntiknight(board, row, col, value, size)) return false;
            if (nonconsecDiagonal && ViolatesNonconsecDiagonal(board, row, col, value, size)) return false;
            if (antibishop && ViolatesAntiBishop(board, row, col, value, size)) return false;
            if ((antiking || distanceGe2) && ViolatesAntiking(board, row, col, value, size)) return false;
            return true;
        }

        // ── Region Maps ──

        // [REQ: GEN-REGION-001] BuildRegionMap: 4 layout variants per board size (rectangular × 2 + template × 2); used by both CreatePuzzle paths
        // [REQ: GEN-REGION-002] Each region contains exactly `size` cells and is spatially contiguous (guaranteed by templates and FillRectangular)
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

        // ── Uniqueness Helpers ──

        // Returns the bitmask of values already placed in the row, column, and region of (row, col).
        private static int ComputeUsedMask(int[,] board, int[,] regionMap, int size, int row, int col)
        {
            var mask = 0;
            var region = regionMap[row, col];
            for (var c = 0; c < size; c++) if (board[row, c] != 0) mask |= 1 << board[row, c];
            for (var r = 0; r < size; r++) if (board[r, col] != 0) mask |= 1 << board[r, col];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (regionMap[r, c] == region && board[r, c] != 0) mask |= 1 << board[r, c];
            return mask;
        }

        // Fills naked singles (cells with exactly one candidate) until no further progress.
        // Returns true when every cell is filled, meaning the puzzle resolves uniquely by
        // propagation alone and no backtracking is needed.
        private static bool PropagateNakedSingles(int[,] board, int[,] regionMap, int size,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            // Build incremental bitmask arrays once; maintain them as naked singles are placed
            // so candidate lookup is O(1) per cell instead of O(n²) via ComputeUsedMask.
            var rowMask    = new int[size];
            var colMask    = new int[size];
            var regionCount = CountDistinctRegions(regionMap, size);
            var regionMask = new int[Math.Max(regionCount, size)];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = board[r, c];
                if (v == 0) continue;
                var bit = 1 << v;
                rowMask[r]                  |= bit;
                colMask[c]                  |= bit;
                regionMask[regionMap[r, c]] |= bit;
            }

            bool progress;
            do
            {
                deadline.ThrowIfExceeded();
                progress = false;
                for (var row = 0; row < size; row++)
                for (var col = 0; col < size; col++)
                {
                    if (board[row, col] != 0) continue;
                    var usedMask = rowMask[row] | colMask[col] | regionMask[regionMap[row, col]];
                    var count = 0;
                    var single = 0;
                    for (var v = 1; v <= size; v++)
                    {
                        if ((usedMask & (1 << v)) != 0) continue;
                        count++;
                        single = v;
                        if (count > 1) break;
                    }
                    if (count == 0) return false; // contradiction — no solution exists
                    if (count == 1)
                    {
                        board[row, col] = single;
                        var bit = 1 << single;
                        rowMask[row]                  |= bit;
                        colMask[col]                  |= bit;
                        regionMask[regionMap[row, col]] |= bit;
                        progress = true;
                    }
                }
            } while (progress);

            for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
                if (board[row, col] == 0) return false;
            return true;
        }

        // Returns true if `puzzle` has exactly one valid solution.
        // Fast path: naked-singles propagation. Falls back to bounded backtracking (cap = 2).
        private static bool HasUniqueSolution(int[,] puzzle, int[,] regionMap, int size,
            bool nonconsecutive, bool antiknight, bool nonconsecDiagonal,
            bool antibishop, bool antiking, bool distanceGe2,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            var work = (int[,])puzzle.Clone();
            if (PropagateNakedSingles(work, regionMap, size, deadline))
                return true; // propagation fully solved the board — definitively unique

            // Build incremental bitmask arrays from the post-propagation board so
            // CountSolutionsCapped can do O(1) mask lookups instead of O(n²) scans.
            var rowMask    = new int[size];
            var colMask    = new int[size];
            var regionCount = CountDistinctRegions(regionMap, size);
            var regionMask = new int[Math.Max(regionCount, size)];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = work[r, c];
                if (v == 0) continue;
                var bit = 1 << v;
                rowMask[r]                  |= bit;
                colMask[c]                  |= bit;
                regionMask[regionMap[r, c]] |= bit;
            }

            // Count solutions on the propagation-reduced board; bail early at 2.
            var count = 0;
            CountSolutionsCapped(work, regionMap, size, rowMask, colMask, regionMask,
                nonconsecutive, antiknight, nonconsecDiagonal, antibishop, antiking, distanceGe2,
                ref count, limit: 2, deadline);
            return count == 1;
        }

        // Counts valid completions of `board` via MRV backtracking, stopping when `count` reaches `limit`.
        // rowMask/colMask/regionMask are incremental bitmask arrays kept in sync with `board` so
        // candidate computation is O(1) per cell instead of O(n²).
        private static void CountSolutionsCapped(int[,] board, int[,] regionMap, int size,
            int[] rowMask, int[] colMask, int[] regionMask,
            bool nonconsecutive, bool antiknight, bool nonconsecDiagonal,
            bool antibishop, bool antiking, bool distanceGe2,
            ref int count, int limit,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            if (count >= limit) return;

            // MRV: choose the empty cell with the fewest remaining candidates.
            var bestRow = -1; var bestCol = -1; var bestCount = int.MaxValue;
            var foundOne = false;
            for (var row = 0; row < size && !foundOne; row++)
            for (var col = 0; col < size && !foundOne; col++)
            {
                if (board[row, col] != 0) continue;
                var used = rowMask[row] | colMask[col] | regionMask[regionMap[row, col]];
                var cnt = 0;
                for (var v = 1; v <= size; v++)
                {
                    if ((used & (1 << v)) != 0) continue;
                    if (AllowsStructuralPlacement(board, row, col, v, size,
                            nonconsecutive, antiknight, nonconsecDiagonal,
                            antibishop, antiking, distanceGe2))
                        cnt++;
                }
                if (cnt == 0) return; // dead end — this branch has no solution
                if (cnt < bestCount) { bestCount = cnt; bestRow = row; bestCol = col; }
                if (bestCount == 1) foundOne = true;
            }

            if (bestRow == -1) { count++; return; } // all cells filled — valid solution found

            var region       = regionMap[bestRow, bestCol];
            var usedForBest  = rowMask[bestRow] | colMask[bestCol] | regionMask[region];
            for (var v = 1; v <= size; v++)
            {
                if ((usedForBest & (1 << v)) != 0) continue;
                if (!AllowsStructuralPlacement(board, bestRow, bestCol, v, size,
                        nonconsecutive, antiknight, nonconsecDiagonal,
                        antibishop, antiking, distanceGe2))
                    continue;

                var bit = 1 << v;
                board[bestRow, bestCol] = v;
                rowMask[bestRow]        |= bit;
                colMask[bestCol]        |= bit;
                regionMask[region]      |= bit;

                CountSolutionsCapped(board, regionMap, size, rowMask, colMask, regionMask,
                    nonconsecutive, antiknight, nonconsecDiagonal, antibishop, antiking, distanceGe2,
                    ref count, limit, deadline);

                board[bestRow, bestCol] = 0;
                rowMask[bestRow]        &= ~bit;
                colMask[bestCol]        &= ~bit;
                regionMask[region]      &= ~bit;
                if (count >= limit) return;
            }
        }
    }
}
