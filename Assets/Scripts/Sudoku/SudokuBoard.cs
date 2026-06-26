using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    public sealed class SudokuBoard
    {
        public int Size { get; }
        public int[,] Solution { get; }
        public int[,] Cells { get; }
        public bool[,] GivenMask { get; }
        public int[,] RegionMap { get; }
        public bool IsUniqueSolution { get; set; }

        private readonly HashSet<int>[,] _pencilMarks;

        public SudokuBoard(int size, int[,] solution, int[,] cells, int[,] regionMap)
        {
            Size = size;
            Solution = solution;
            Cells = cells;
            RegionMap = regionMap;
            GivenMask = new bool[size, size];
            _pencilMarks = new HashSet<int>[size, size];

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                GivenMask[r, c] = cells[r, c] != 0;
                _pencilMarks[r, c] = new HashSet<int>();
            }
        }

        public bool IsGiven(int row, int col) => GivenMask[row, col];

        // [REQ: GEN-VALID-001] IsCorrectAt: placement is valid when it satisfies all standard row/column/region constraints
        // (solution value not consulted — any valid completion is accepted, supporting multiple-solution puzzles)
        public bool IsCorrectAt(int row, int col)
        {
            var v = Cells[row, col];
            if (v == 0) return false;
            for (var i = 0; i < Size; i++)
            {
                if (i != col && Cells[row, i] == v) return false;
                if (i != row && Cells[i, col] == v) return false;
            }
            var regionId = RegionMap[row, col];
            for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
            {
                if ((r != row || c != col) && RegionMap[r, c] == regionId && Cells[r, c] == v)
                    return false;
            }
            return true;
        }

        // Puzzle is complete when every cell is filled and the board satisfies all
        // standard Sudoku constraints (no duplicates in any row, column, or region).
        // Any valid completion is accepted — not only the originally generated one.
        public bool IsComplete()
        {
            var size = Size;
            // Build region count
            var regionCount = 0;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (RegionMap[r, c] >= regionCount) regionCount = RegionMap[r, c] + 1;

            var rowSeen = new bool[size, size + 1];
            var colSeen = new bool[size, size + 1];
            var regSeen = new bool[regionCount, size + 1];

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = Cells[r, c];
                if (v == 0) return false;
                var region = RegionMap[r, c];
                if (rowSeen[r, v] || colSeen[c, v] || regSeen[region, v]) return false;
                rowSeen[r, v] = true;
                colSeen[c, v] = true;
                regSeen[region, v] = true;
            }
            return true;
        }

        public int EmptyCellCount()
        {
            var count = 0;
            for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
            {
                if (Cells[r, c] == 0)
                    count++;
            }
            return count;
        }

        public HashSet<int> GetPencilMarks(int row, int col) => _pencilMarks[row, col];

        public void AddPencilMark(int row, int col, int value) => _pencilMarks[row, col].Add(value);

        public void RemovePencilMark(int row, int col, int value) => _pencilMarks[row, col].Remove(value);

        public void ClearPencilMarks(int row, int col) => _pencilMarks[row, col].Clear();

        public bool HasAnyPencilMark(int row, int col) => _pencilMarks[row, col].Count > 0;

        public void TogglePencilMark(int row, int col, int value)
        {
            if (!_pencilMarks[row, col].Add(value))
                _pencilMarks[row, col].Remove(value);
        }

        public void PlaceValue(int row, int col, int value)
        {
            if (!GivenMask[row, col])
            {
                Cells[row, col] = value;
                if (value != 0)
                    _pencilMarks[row, col].Clear();
            }
        }

        public void ClearValue(int row, int col)
        {
            if (!GivenMask[row, col])
                Cells[row, col] = 0;
        }
    }
}
