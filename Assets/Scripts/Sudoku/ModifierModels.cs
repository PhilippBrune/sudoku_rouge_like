using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    [Serializable]
    public sealed class ModifierOverlayData
    {
        public List<ModifierLine> Lines = new List<ModifierLine>();
        public List<KropkiDot> KropkiDots = new List<KropkiDot>();
        public List<KillerCage> KillerCages = new List<KillerCage>();
        public List<ArrowConstraint> Arrows = new List<ArrowConstraint>();
        public List<CellMarker> CellMarkers = new List<CellMarker>();
        public List<CellCoord> FogCells = new List<CellCoord>();
        public List<AdjacentPairConstraint> PairConstraints = new List<AdjacentPairConstraint>();
        // When true, any orthogonal adjacent pair NOT in KropkiDots is forbidden from diff=1 or ratio=1:2
        public bool FullKropkiNegativeInference;

        // New overlay data for catalog-batch-2 modifiers
        public List<ExtraRegion> ExtraRegions = new List<ExtraRegion>();
        public List<OutsideClue> OutsideClues = new List<OutsideClue>();
        public List<QuadrupleConstraint> Quadruples = new List<QuadrupleConstraint>();
        public List<CellPairConstraint> CellPairs = new List<CellPairConstraint>();

        // Global flags for modifiers that need no geometry but change validation scope
        public bool IsXSudoku;          // XSudoku: main diagonals are no-repeat regions
        public bool IsHyperSudoku;      // HyperSudoku: 4 extra 3×3 regions active
        public bool IsDisjointGroups;   // DisjointGroups: relative-box-position constraint
        public bool IsToroidal;         // Toroidal: row/col wrap-around adjacency
        public int  ModularGlobalN;     // ModularGlobal: residue class size (default 3)
        public int  DifferenceKValue;   // DifferenceK: minimum difference (default 3)
        public int  RatioNValue;        // RatioN: ratio multiplier (default 3)
        public int  OppositeSumConstant;// OppositeCellsSum: A+B must equal this
        public bool FullWhiteKropkiNeg; // FullWhiteKropki: absence forbids diff=1
        public bool FullBlackKropkiNeg; // FullBlackKropki: absence forbids ratio=1:2
        public bool IsIndexParity;      // IndexParity: (row+col)%2 parity groups
    }

    [Serializable]
    public sealed class ModifierLine
    {
        public LineType Type;
        public List<CellCoord> Cells = new List<CellCoord>();
        public int LineValue;  // multipurpose: K for WhisperGeneralized, sum for NLine/SumLine/ZipperLine, N for FastThermo
    }

    [Serializable]
    public sealed class KropkiDot
    {
        public CellCoord CellA;
        public CellCoord CellB;
        public bool IsBlack;
        public int SumValue; // > 0 when this is a Sum Kropki dot (CellA + CellB = SumValue)
    }

    [Serializable]
    public sealed class AdjacentPairConstraint
    {
        public CellCoord CellA;
        public CellCoord CellB;
        public PairConstraintType Type;
        public int Value; // used for SumK type
    }

    [Serializable]
    public sealed class KillerCage
    {
        public int Sum;
        public List<CellCoord> Cells = new List<CellCoord>();
        public KillerCageType CageType; // default Sum
    }

    [Serializable]
    public sealed class ArrowConstraint
    {
        public CellCoord CircleCell;
        public List<CellCoord> ArrowCells = new List<CellCoord>();
        public ArrowType ArrowType; // default Sum
        public CellCoord SecondCircle; // for DoubleEndpoint and Pill second cell
    }

    [Serializable]
    public sealed class CellMarker
    {
        public CellCoord Cell;
        public MarkerType Type;
    }

    [Serializable]
    public sealed class ExtraRegion
    {
        public List<CellCoord> Cells = new List<CellCoord>();
        public int SumTarget; // 0 = no sum constraint; >0 = all cells must sum to this
    }

    [Serializable]
    public sealed class OutsideClue
    {
        public OutsideClueEdge Edge;
        public int Index;       // row or column index (0-based); for diagonal = starting col or row
        public int Value;       // the clue number
        public OutsideClueType Type;
        public bool FromStart;  // true = clue is from the near edge; false = from far edge
        public int SentinelA;   // for Sandwich: first sentinel digit
        public int SentinelB;   // for Sandwich: second sentinel digit
    }

    [Serializable]
    public sealed class QuadrupleConstraint
    {
        public CellCoord TopLeft; // top-left of the 2×2 group the corner sits between
        public List<int> Digits = new List<int>(); // digits that must appear in the 4 surrounding cells
    }

    [Serializable]
    public sealed class CellPairConstraint
    {
        public CellCoord CellA;
        public CellCoord CellB;
        public CellPairType PairType;
        public int Value; // for OppositeSum: the required sum; for Mirror: ignored
    }

    public enum CellPairType
    {
        Mirror,       // CellA and CellB must contain the same digit
        OppositeSum   // CellA + CellB = Value
    }

    [Serializable]
    public struct CellCoord : IEquatable<CellCoord>
    {
        public int Row;
        public int Col;

        public CellCoord(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool Equals(CellCoord other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is CellCoord other && Equals(other);
        public override int GetHashCode() => Row * 31 + Col;
        public static bool operator ==(CellCoord a, CellCoord b) => a.Equals(b);
        public static bool operator !=(CellCoord a, CellCoord b) => !a.Equals(b);
        public override string ToString() => $"({Row},{Col})";
    }
}
