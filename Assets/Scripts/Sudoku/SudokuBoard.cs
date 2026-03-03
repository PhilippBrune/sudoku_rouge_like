using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    [Serializable]
    public sealed class SudokuBoard
    {
        public int Size { get; }
        public int[,] Solution { get; }
        public int[,] Cells { get; }
        public bool[,] GivenMask { get; }
        public int[,] RegionMap { get; }

        private readonly HashSet<int>[,] _pencil;

        public SudokuBoard(int size, int[,] solution, int[,] puzzle, bool[,] givenMask, int[,] regionMap)
        {
            Size = size;
            Solution = solution;
            Cells = puzzle;
            GivenMask = givenMask;
            RegionMap = regionMap;
            _pencil = new HashSet<int>[size, size];

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    _pencil[row, col] = new HashSet<int>();
                }
            }
        }

        public bool IsGiven(int row, int col) => GivenMask[row, col];

        public int GetCell(int row, int col) => Cells[row, col];

        public bool IsEmpty(int row, int col) => Cells[row, col] == 0;

        public void SetCell(int row, int col, int value)
        {
            Cells[row, col] = value;
            _pencil[row, col].Clear();
        }

        public void ClearCell(int row, int col)
        {
            if (!GivenMask[row, col])
            {
                Cells[row, col] = 0;
            }
        }

        public HashSet<int> GetPencilSet(int row, int col) => _pencil[row, col];

        public bool IsComplete()
        {
            for (var row = 0; row < Size; row++)
            {
                for (var col = 0; col < Size; col++)
                {
                    if (Cells[row, col] != Solution[row, col])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsCorrectAt(int row, int col)
        {
            return Cells[row, col] != 0 && Cells[row, col] == Solution[row, col];
        }
    }
}
