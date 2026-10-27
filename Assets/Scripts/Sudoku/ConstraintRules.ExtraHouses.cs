using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // Batch B5 of the modifier catalog (docs/BossMechanicsSystem.md "Modifier catalog"): extra houses that hold no repeated digit
    // (the X-Sudoku diagonals, the Hyper windows, the Disjoint groups, extra regions) and the magic-square box. Each
    // rule reads only the digits already on the board, so it is safe on partial boards.

    // [REQ: BOSS-LIB-030] X-Sudoku: no digit repeats on either main diagonal.
    // [REQ: BOSS-LIB-031] Hyper Sudoku: no digit repeats in the four shaded windows (9×9).
    // [REQ: BOSS-LIB-032] Disjoint Groups: cells in the same position of their box (all top-left cells, …) hold
    // different digits (9×9).
    // [REQ: BOSS-LIB-053] Extra Region: no digit repeats in an outlined extra region.
    public sealed class ExtraHouseRule : IOrderedConstraintRule
    {
        private readonly BossModifierId _modifier;

        public ExtraHouseRule(BossModifierId modifier) => _modifier = modifier;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Region;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            var size = board.Size;
            switch (_modifier)
            {
                case BossModifierId.XSudoku:
                    if (!overlay.IsXSudoku) return true;
                    for (var i = 0; i < size; i++)
                    {
                        if (row == col && i != row && board.Cells[i, i] == value) return false;
                        if (row + col == size - 1 && i != row && board.Cells[i, size - 1 - i] == value) return false;
                    }
                    return true;
                case BossModifierId.HyperSudoku:
                    if (!overlay.IsHyperSudoku || size != 9) return true;
                    foreach (var window in SolutionShape.HyperWindows)
                        if (InHouse(window, row * size + col) && Repeats(board, window, row, col, value)) return false;
                    return true;
                case BossModifierId.DisjointGroups:
                    if (!overlay.IsDisjointGroups || size != 9) return true;
                    return !Repeats(board, SolutionShape.DisjointGroups[row % 3 * 3 + col % 3], row, col, value);
                case BossModifierId.ExtraRegion:
                    foreach (var region in overlay.ExtraRegions)
                    {
                        var inside = false;
                        foreach (var c in region.Cells)
                            if (c.Row == row && c.Col == col) { inside = true; break; }
                        if (!inside) continue;
                        foreach (var c in region.Cells)
                            if ((c.Row != row || c.Col != col) && board.Cells[c.Row, c.Col] == value) return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private static bool InHouse(int[] house, int cell) => Array.IndexOf(house, cell) >= 0;

        private static bool Repeats(SudokuBoard board, int[] house, int row, int col, int value)
        {
            var size = board.Size;
            foreach (var index in house)
            {
                int r = index / size, c = index % size;
                if ((r != row || c != col) && board.Cells[r, c] == value) return true;
            }
            return false;
        }
    }

    // [REQ: BOSS-LIB-068] Magic Square: in the gold box every row, column and diagonal adds up to 15 (9×9).
    public sealed class MagicSquareRule : IOrderedConstraintRule
    {
        public const int LineSum = 15;

        private static readonly int[][] Lines =
        {
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
            new[] { 0, 4, 8 }, new[] { 2, 4, 6 }
        };

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Region;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            foreach (var cage in overlay.KillerCages)
            {
                if (cage.CageType != KillerCageType.MagicSquare || cage.Cells.Count != 9) continue;
                var at = cage.Cells.FindIndex(c => c.Row == row && c.Col == col);
                if (at < 0) continue;
                foreach (var line in Lines)
                {
                    if (Array.IndexOf(line, at) < 0) continue;
                    int min = 0, max = 0;
                    foreach (var i in line)
                    {
                        var v = i == at ? value : board.Cells[cage.Cells[i].Row, cage.Cells[i].Col];
                        if (v == 0) { min += 1; max += board.Size; }
                        else { min += v; max += v; }
                    }
                    if (min > LineSum || max < LineSum) return false;
                }
            }
            return true;
        }
    }
}
