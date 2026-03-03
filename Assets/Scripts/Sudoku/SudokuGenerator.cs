using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuGenerator
    {
        public static SudokuBoard CreatePuzzle(int size, float missingPercent, int seed)
        {
            var random = new Random(seed);
            var solution = GenerateSolvedLatinBoard(size, random);
            var regionMap = BuildRegionMap(size);
            var puzzle = (int[,])solution.Clone();

            var totalCells = size * size;
            var removeCount = Math.Clamp((int)Math.Round(totalCells * missingPercent), 1, totalCells - size);
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

            return new SudokuBoard(size, solution, puzzle, givenMask, regionMap);
        }

        private static int[,] GenerateSolvedLatinBoard(int size, Random random)
        {
            var board = new int[size, size];

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    board[row, col] = ((row + col) % size) + 1;
                }
            }

            var rowOrder = CreateIndexList(size);
            var colOrder = CreateIndexList(size);
            var symbolOrder = CreateIndexList(size);

            Shuffle(rowOrder, random);
            Shuffle(colOrder, random);
            Shuffle(symbolOrder, random);

            var remapped = new int[size, size];
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    var original = board[rowOrder[r], colOrder[c]];
                    remapped[r, c] = symbolOrder[original - 1] + 1;
                }
            }

            return remapped;
        }

        private static int[,] BuildRegionMap(int size)
        {
            var regionMap = new int[size, size];
            var boxRoot = (int)Math.Sqrt(size);

            if (boxRoot * boxRoot == size)
            {
                for (var row = 0; row < size; row++)
                {
                    for (var col = 0; col < size; col++)
                    {
                        var region = (row / boxRoot) * boxRoot + (col / boxRoot);
                        regionMap[row, col] = region;
                    }
                }
            }
            else
            {
                for (var row = 0; row < size; row++)
                {
                    for (var col = 0; col < size; col++)
                    {
                        regionMap[row, col] = (row + col) % size;
                    }
                }
            }

            return regionMap;
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
    }
}
