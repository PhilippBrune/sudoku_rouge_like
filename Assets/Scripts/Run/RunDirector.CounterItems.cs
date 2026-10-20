using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Run
{
    // [REQ: ITEM-COUNTER-001..006] Counter items (gold-and-items concept L3): each one finds the cells of its modifier
    // family on the current board and writes their correct digits, starting at the selected cell when it belongs to the
    // family, else at the group closest to done. Digits go through PlaceNumber, so combos, pressure and fog react as to a
    // placement by hand.
    public sealed partial class RunDirector
    {
        /// <summary>The cells <paramref name="type"/> would fill now, in order; empty when the board has nothing for it.</summary>
        public List<(int row, int col)> GetCounterItemTargets(ItemType type, int row = -1, int col = -1)
        {
            var result = new List<(int, int)>();
            var overlay = CurrentOverlay;
            if (CurrentBoard == null || overlay == null || !CounterItems.IsCounterItem(type)) return result;
            var max = CounterItems.CellsPerUse(type);
            switch (type)
            {
                case ItemType.WaterBasin:   // [REQ: ITEM-COUNTER-001] lines
                {
                    var lines = new List<List<CellCoord>>();
                    foreach (var line in overlay.Lines) lines.Add(line.Cells);
                    return FromClosestGroup(lines, row, col, max);
                }
                case ItemType.TwinPebbles:  // [REQ: ITEM-COUNTER-002] dots and pair marks
                {
                    var pairs = new List<List<CellCoord>>();
                    foreach (var dot in overlay.KropkiDots) pairs.Add(new List<CellCoord> { dot.CellA, dot.CellB });
                    foreach (var pair in overlay.PairConstraints) pairs.Add(new List<CellCoord> { pair.CellA, pair.CellB });
                    return FromClosestGroup(pairs, row, col, max);
                }
                case ItemType.RiceBowl:     // [REQ: ITEM-COUNTER-003] cages and arrows
                {
                    var groups = new List<List<CellCoord>>();
                    foreach (var cage in overlay.KillerCages) groups.Add(cage.Cells);
                    foreach (var arrow in overlay.Arrows)
                    {
                        var cells = new List<CellCoord> { arrow.CircleCell };
                        if (arrow.ArrowType == ArrowType.Pill || arrow.ArrowType == ArrowType.DoubleEndpoint)
                            cells.Add(arrow.SecondCircle);
                        cells.AddRange(arrow.ArrowCells);
                        groups.Add(cells);
                    }
                    return FromClosestGroup(groups, row, col, max);
                }
                case ItemType.MossStone:    // [REQ: ITEM-COUNTER-004] marked, paired and fogged cells
                {
                    var cells = new List<CellCoord>();
                    foreach (var marker in overlay.CellMarkers) cells.Add(marker.Cell);
                    foreach (var pair in overlay.CellPairs) { cells.Add(pair.CellA); cells.Add(pair.CellB); }
                    cells.AddRange(overlay.FogCells);
                    return FromPool(cells, row, col, max, byCandidates: false);
                }
                case ItemType.CompassOfOrder: // [REQ: ITEM-COUNTER-005] whole-board rules: the most constrained cells
                    // Rules without houses of their own (Anti-Knight, Nonconsecutive ...) cover the whole board; a board
                    // without any whole-board rule gives the compass nothing to read.
                    return HasWholeBoardRule()
                        ? FromPool(WholeBoardHouseCells(overlay), row, col, max, byCandidates: true)
                        : result;
                case ItemType.SunMedallion: // [REQ: ITEM-COUNTER-006] outside clues: the first cells each clue reads
                {
                    var rays = new List<List<CellCoord>>();
                    foreach (var clue in overlay.OutsideClues) rays.Add(OutsideClueGeometry.Cells(clue, CurrentBoard.Size));
                    foreach (var quad in overlay.Quadruples)
                        rays.Add(new List<CellCoord>
                        {
                            quad.TopLeft, new CellCoord(quad.TopLeft.Row, quad.TopLeft.Col + 1),
                            new CellCoord(quad.TopLeft.Row + 1, quad.TopLeft.Col), new CellCoord(quad.TopLeft.Row + 1, quad.TopLeft.Col + 1)
                        });
                    return FromClosestGroup(rays, row, col, max, inReadingOrder: true);
                }
            }
            return result;
        }

        private ItemEffectResult ApplyCounterItem(ItemInstance item, int row, int col)
        {
            var filled = new List<(int, int)>();
            var locked = GetLockedCells();
            foreach (var (r, c) in GetCounterItemTargets(item.Type, row, col))
            {
                if (State.CurrentHP <= 0) break;   // a curse that costs HP per placement can end the use
                if (locked.Contains((r, c))) continue;   // Cell Lock (DEBUFF-HOOK-013) holds against items too
                if (PlaceNumber(r, c, CurrentBoard.Solution[r, c]) == PlaceResult.Correct) filled.Add((r, c));
            }
            return filled.Count == 0
                ? ItemEffectResult.Message("Nothing to fill for this item on this board.")
                : ItemEffectResult.CellsFilled(filled, $"Filled {filled.Count} cell(s).");
        }

        private bool HasWholeBoardRule()
        {
            var rules = CurrentLevelConfig?.ActiveModifiers;
            if (rules == null) return false;
            foreach (var id in rules)
                if (SudokuRoguelike.Boss.ModifierFamilies.TryGetFamily(id, out var family)
                    && family == SudokuRoguelike.Boss.ModifierFamily.WholeBoard)
                    return true;
            return false;
        }

        // An empty cell the player may write (not given, inside the board, with a known solution digit).
        private bool IsOpenCell(CellCoord cell)
        {
            var size = CurrentBoard.Size;
            return cell.Row >= 0 && cell.Row < size && cell.Col >= 0 && cell.Col < size
                && CurrentBoard.Cells[cell.Row, cell.Col] == 0 && !CurrentBoard.IsGiven(cell.Row, cell.Col)
                && CurrentBoard.Solution[cell.Row, cell.Col] > 0;
        }

        // The group through the selected cell, else the one with the fewest open cells (closest to done); its open cells
        // with the selected one first (or in the group's own order for a clue's reading line).
        private List<(int, int)> FromClosestGroup(List<List<CellCoord>> groups, int row, int col, int max,
            bool inReadingOrder = false)
        {
            List<CellCoord> best = null;
            var bestOpen = int.MaxValue;
            var bestHasSelected = false;
            foreach (var group in groups)
            {
                var open = 0;
                var hasSelected = false;
                foreach (var cell in group)
                {
                    if (IsOpenCell(cell)) open++;
                    if (cell.Row == row && cell.Col == col) hasSelected = true;
                }
                if (open == 0) continue;
                var better = best == null || (hasSelected && !bestHasSelected)
                    || (hasSelected == bestHasSelected && open < bestOpen);
                if (!better) continue;
                best = group;
                bestOpen = open;
                bestHasSelected = hasSelected;
            }
            var result = new List<(int, int)>();
            if (best == null) return result;
            if (!inReadingOrder && bestHasSelected && IsOpenCell(new CellCoord(row, col))) result.Add((row, col));
            foreach (var cell in best)
            {
                if (result.Count >= max) break;
                if (IsOpenCell(cell) && !result.Contains((cell.Row, cell.Col))) result.Add((cell.Row, cell.Col));
            }
            return result;
        }

        // Open cells of a pool, the selected one first, then by fewest candidates (row, column, region) or board order.
        private List<(int, int)> FromPool(List<CellCoord> pool, int row, int col, int max, bool byCandidates)
        {
            var open = new List<CellCoord>();
            foreach (var cell in pool)
                if (IsOpenCell(cell) && !open.Contains(cell)) open.Add(cell);
            if (open.Count == 0 && byCandidates)
                for (var r = 0; r < CurrentBoard.Size; r++)
                for (var c = 0; c < CurrentBoard.Size; c++)
                    if (IsOpenCell(new CellCoord(r, c))) open.Add(new CellCoord(r, c));
            open.Sort((a, b) =>
            {
                var aSelected = a.Row == row && a.Col == col;
                var bSelected = b.Row == row && b.Col == col;
                if (aSelected != bSelected) return aSelected ? -1 : 1;
                if (byCandidates)
                {
                    var byCount = BasicCandidateCount(a).CompareTo(BasicCandidateCount(b));
                    if (byCount != 0) return byCount;
                }
                return (a.Row * CurrentBoard.Size + a.Col).CompareTo(b.Row * CurrentBoard.Size + b.Col);
            });
            var result = new List<(int, int)>();
            for (var i = 0; i < open.Count && result.Count < max; i++) result.Add((open[i].Row, open[i].Col));
            return result;
        }

        // The extra houses of the whole-board rules (diagonals, windows, extra regions, the inverted zone).
        private List<CellCoord> WholeBoardHouseCells(ModifierOverlayData overlay)
        {
            var size = CurrentBoard.Size;
            var cells = new List<CellCoord>();
            if (overlay.IsXSudoku)
                for (var i = 0; i < size; i++) { cells.Add(new CellCoord(i, i)); cells.Add(new CellCoord(i, size - 1 - i)); }
            if (overlay.IsHyperSudoku && size == 9)
                foreach (var top in new[] { 1, 5 })
                foreach (var left in new[] { 1, 5 })
                    for (var r = top; r < top + 3; r++)
                    for (var c = left; c < left + 3; c++) cells.Add(new CellCoord(r, c));
            foreach (var region in overlay.ExtraRegions) cells.AddRange(region.Cells);
            cells.AddRange(overlay.NegationZone);
            return cells;
        }

        // Digits not yet in the cell's row, column or region.
        private int BasicCandidateCount(CellCoord cell)
        {
            var board = CurrentBoard;
            var used = new HashSet<int>();
            for (var i = 0; i < board.Size; i++)
            {
                used.Add(board.Cells[cell.Row, i]);
                used.Add(board.Cells[i, cell.Col]);
            }
            var region = board.RegionMap[cell.Row, cell.Col];
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.RegionMap[r, c] == region) used.Add(board.Cells[r, c]);
            used.Remove(0);
            return board.Size - used.Count;
        }
    }
}
