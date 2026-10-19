using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // [REQ: GEN-MOD-002] Overlay for batch B5 of the modifier catalog (modifier-catalog-plan.md). X-Sudoku, Hyper Sudoku,
    // Disjoint Groups and the magic square are built into the solution by the generator (SolutionShape); here each one
    // is switched on only when the solution really keeps it, so a board made without it reports the modifier missing
    // instead of showing a rule its solution breaks. Extra regions are read off the solution like the other catalog
    // geometry.
    public static partial class ModifierGeometryGenerator
    {
        private static bool HouseHolds(SudokuBoard board, int[] house)
        {
            var size = board.Size;
            var seen = 0;
            foreach (var index in house)
            {
                var bit = 1 << board.Solution[index / size, index % size];
                if ((seen & bit) != 0) return false;
                seen |= bit;
            }
            return true;
        }

        private static void GenerateXSudoku(SudokuBoard board, ModifierOverlayData overlay) =>
            overlay.IsXSudoku = HouseHolds(board, SolutionShape.Diagonal(board.Size, false))
                                && HouseHolds(board, SolutionShape.Diagonal(board.Size, true));

        private static void GenerateHyperSudoku(SudokuBoard board, ModifierOverlayData overlay)
        {
            if (board.Size != 9) return;
            var holds = true;
            foreach (var top in new[] { 1, 5 })
            foreach (var left in new[] { 1, 5 })
                holds &= HouseHolds(board, SolutionShape.Window(9, top, left));
            overlay.IsHyperSudoku = holds;
        }

        private static void GenerateDisjointGroups(SudokuBoard board, ModifierOverlayData overlay)
        {
            if (board.Size != 9) return;
            var holds = true;
            for (var dr = 0; dr < 3; dr++)
            for (var dc = 0; dc < 3; dc++)
                holds &= HouseHolds(board, SolutionShape.DisjointGroup(9, dr, dc));
            overlay.IsDisjointGroups = holds;
        }

        // The 3×3 box whose rows, columns and diagonals all add up to 15 (the generator filled one with a magic square).
        private static void GenerateMagicSquare(SudokuBoard board, ModifierOverlayData overlay)
        {
            if (board.Size != 9) return;
            for (var box = 0; box < 9; box++)
            {
                var cells = new List<CellCoord>(9);
                for (var i = 0; i < 9; i++) cells.Add(new CellCoord(box / 3 * 3 + i / 3, box % 3 * 3 + i % 3));
                if (!IsMagic(board, cells)) continue;
                var cage = new KillerCage { CageType = KillerCageType.MagicSquare, Sum = MagicSquareRule.LineSum };
                cage.Cells.AddRange(cells);
                overlay.KillerCages.Add(cage);
                return;
            }
        }

        private static bool IsMagic(SudokuBoard board, List<CellCoord> cells)
        {
            int V(int i) => S(board, cells[i]);
            for (var k = 0; k < 3; k++)
            {
                if (V(3 * k) + V(3 * k + 1) + V(3 * k + 2) != MagicSquareRule.LineSum) return false;
                if (V(k) + V(k + 3) + V(k + 6) != MagicSquareRule.LineSum) return false;
            }
            return V(0) + V(4) + V(8) == MagicSquareRule.LineSum && V(2) + V(4) + V(6) == MagicSquareRule.LineSum;
        }

        // Extra regions: a connected group of `size` cells with all digits different that is no row, column or region.
        private static void GenerateExtraRegions(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var target = ScaledCount(1, scale, 1, 2);
            var taken = new HashSet<long>();
            foreach (var region in overlay.ExtraRegions)
                foreach (var c in region.Cells) taken.Add(CellKey(c.Row, c.Col));
            for (var attempt = 0; attempt < 200 && overlay.ExtraRegions.Count < target; attempt++)
            {
                var start = new CellCoord(rng.Next(size), rng.Next(size));
                if (taken.Contains(CellKey(start.Row, start.Col))) continue;
                var cells = new List<CellCoord> { start };
                var digits = 1 << S(board, start);
                var options = new List<CellCoord>();
                while (cells.Count < size)
                {
                    options.Clear();
                    foreach (var cell in cells)
                    foreach (var (dr, dc) in CageDirs)
                    {
                        var n = new CellCoord(cell.Row + dr, cell.Col + dc);
                        if (n.Row < 0 || n.Row >= size || n.Col < 0 || n.Col >= size) continue;
                        if (cells.Contains(n) || options.Contains(n) || taken.Contains(CellKey(n.Row, n.Col))) continue;
                        if ((digits & (1 << S(board, n))) == 0) options.Add(n);
                    }
                    if (options.Count == 0) break;
                    var pick = options[rng.Next(options.Count)];
                    cells.Add(pick);
                    digits |= 1 << S(board, pick);
                }
                if (cells.Count < size || CountOpen(board, cells) < 2 || IsExistingHouse(board, cells)) continue;
                var extra = new ExtraRegion();
                extra.Cells.AddRange(cells);
                overlay.ExtraRegions.Add(extra);
                foreach (var c in cells) taken.Add(CellKey(c.Row, c.Col));
            }
        }

        private static bool IsExistingHouse(SudokuBoard board, List<CellCoord> cells)
        {
            bool sameRow = true, sameCol = true, sameRegion = true;
            foreach (var c in cells)
            {
                sameRow &= c.Row == cells[0].Row;
                sameCol &= c.Col == cells[0].Col;
                sameRegion &= board.RegionMap == null || board.RegionMap[c.Row, c.Col] == board.RegionMap[cells[0].Row, cells[0].Col];
            }
            return sameRow || sameCol || sameRegion;
        }
    }
}
