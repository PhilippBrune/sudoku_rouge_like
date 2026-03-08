using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Sudoku
{
    [Serializable]
    public struct CellCoord
    {
        public int Row;
        public int Col;

        public CellCoord(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }

    public enum LineType { GermanWhispers, DutchWhispers, Parity, Renban }

    public enum DotType { White, Black }

    [Serializable]
    public sealed class ModifierLine
    {
        public LineType Type;
        public readonly List<CellCoord> Cells = new();
    }

    [Serializable]
    public sealed class ArrowConstraint
    {
        public CellCoord Circle;
        public readonly List<CellCoord> Path = new();
    }

    [Serializable]
    public sealed class KillerCage
    {
        public int Sum;
        public readonly List<CellCoord> Cells = new();
    }

    [Serializable]
    public sealed class KropkiDot
    {
        public CellCoord CellA;
        public CellCoord CellB;
        public DotType Type;
    }

    [Serializable]
    public sealed class ModifierOverlayData
    {
        public readonly List<ModifierLine> Lines = new();
        public readonly List<ArrowConstraint> Arrows = new();
        public readonly List<KillerCage> Cages = new();
        public readonly List<KropkiDot> Dots = new();
        public readonly HashSet<long> FogCells = new();

        public static long PackCoord(int row, int col) => ((long)row << 16) | (long)(col & 0xFFFF);

        public static (int Row, int Col) UnpackCoord(long packed) =>
            ((int)(packed >> 16), (int)(packed & 0xFFFF));

        public bool IsFogged(int row, int col) => FogCells.Contains(PackCoord(row, col));

        public void SetFog(int row, int col) => FogCells.Add(PackCoord(row, col));

        public void ClearFog(int row, int col) => FogCells.Remove(PackCoord(row, col));
    }
}
