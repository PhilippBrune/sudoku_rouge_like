using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // [REQ: GEN-MOD-002] Geometry for batch B4 of the modifier catalog (BossMechanicsSystem.md "Modifier catalog"): outside clues read off
    // the solution (Skyscrapers, Sandwich, X-Sums on rows and columns, Little Killer on diagonals) and Quadruples on inner
    // corners. Outside clues sit in the ring round the grid and claim no cells; a clue whose cells are all givens is
    // skipped. Quadruples claim their four cells.
    public static partial class ModifierGeometryGenerator
    {
        private static readonly OutsideClueEdge[] RingEdges =
            { OutsideClueEdge.Top, OutsideClueEdge.Bottom, OutsideClueEdge.Left, OutsideClueEdge.Right };

        private static void GenerateRingClues(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale, OutsideClueType type)
        {
            var size = board.Size;
            var slots = new List<(OutsideClueEdge Edge, int Index)>();
            foreach (var edge in RingEdges)
                for (var i = 0; i < size; i++)
                    if (!overlay.OutsideClues.Exists(c => c.Edge == edge && c.Index == i)) slots.Add((edge, i));
            Shuffle(slots, rng);
            var target = ScaledCount(BaseCount(size, 5, 7, 9), scale, 2, slots.Count);
            var made = 0;
            foreach (var (edge, index) in slots)
            {
                if (made >= target) break;
                var clue = new OutsideClue { Edge = edge, Index = index, Type = type };
                var cells = OutsideClueGeometry.Cells(clue, size);
                if (CountOpen(board, cells) == 0) continue;
                var digits = cells.ConvertAll(c => S(board, c)).ToArray();
                clue.Value = type switch
                {
                    OutsideClueType.Skyscrapers => VisibleCount(digits),
                    OutsideClueType.Sandwich => SandwichSum(digits, size),
                    _ => XSum(digits)
                };
                if (type == OutsideClueType.Sandwich) { clue.SentinelA = 1; clue.SentinelB = size; }
                overlay.OutsideClues.Add(clue);
                made++;
            }
        }

        internal static int VisibleCount(int[] digits)
        {
            int seen = 0, highest = 0;
            foreach (var d in digits)
                if (d > highest) { highest = d; seen++; }
            return seen;
        }

        internal static int SandwichSum(int[] digits, int size)
        {
            int one = Array.IndexOf(digits, 1), top = Array.IndexOf(digits, size), sum = 0;
            for (var i = Math.Min(one, top) + 1; i < Math.Max(one, top); i++) sum += digits[i];
            return sum;
        }

        internal static int XSum(int[] digits)
        {
            var sum = 0;
            for (var i = 0; i < digits[0]; i++) sum += digits[i];
            return sum;
        }

        // Little Killer: diagonals of three cells or more that start on the top edge, or on the left (down-right) or right
        // (down-left) edge; the clue is the sum along the diagonal.
        private static void GenerateLittleKillers(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var options = new List<OutsideClue>();
            foreach (var edge in new[] { OutsideClueEdge.DiagonalDownRight, OutsideClueEdge.DiagonalDownLeft })
            {
                for (var col = 0; col < size; col++) options.Add(new OutsideClue { Edge = edge, Index = col, FromStart = true });
                for (var row = 1; row < size; row++) options.Add(new OutsideClue { Edge = edge, Index = row, FromStart = false });
            }
            Shuffle(options, rng);
            var target = ScaledCount(BaseCount(size, 3, 4, 5), scale, 1, 8);
            var made = 0;
            var spots = new HashSet<long>();   // the outside cell each clue is drawn in: one clue per spot
            foreach (var clue in options)
            {
                if (made >= target) break;
                var cells = OutsideClueGeometry.Cells(clue, size);
                if (cells.Count < 3 || CountOpen(board, cells) < 2) continue;
                var dc = clue.Edge == OutsideClueEdge.DiagonalDownRight ? 1 : -1;
                // The spot is one row above and one column before the start cell (column shifted by one, so it is never negative).
                if (!spots.Add(CellKey(cells[0].Row, cells[0].Col - dc + 1))) continue;
                clue.Type = OutsideClueType.LittleKiller;
                clue.Value = SumOf(board, cells);
                overlay.OutsideClues.Add(clue);
                made++;
            }
        }

        // Quadruples: two to four digits of the four cells round an inner corner, never on four givens; neighbouring
        // circles keep a cell apart.
        private static void GenerateQuadruples(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var corners = new List<CellCoord>();
            for (var r = 0; r < size - 1; r++)
            for (var c = 0; c < size - 1; c++)
                corners.Add(new CellCoord(r, c));
            Shuffle(corners, rng);
            var target = ScaledCount(BaseCount(size, 3, 4, 5), scale, 1, 8);
            foreach (var corner in corners)
            {
                if (overlay.Quadruples.Count >= target) break;
                var cells = new List<CellCoord>
                {
                    corner, new CellCoord(corner.Row, corner.Col + 1),
                    new CellCoord(corner.Row + 1, corner.Col), new CellCoord(corner.Row + 1, corner.Col + 1)
                };
                if (CountOpen(board, cells) == 0 || cells.Exists(c => UsedCells.Contains(CellKey(c.Row, c.Col)))) continue;
                if (overlay.Quadruples.Exists(q => Math.Abs(q.TopLeft.Row - corner.Row) < 2 && Math.Abs(q.TopLeft.Col - corner.Col) < 2)) continue;

                // Digits of open cells first: a given's digit in the circle tells the player nothing new.
                var digits = new List<int>();
                foreach (var c in cells)
                    if (!board.GivenMask[c.Row, c.Col] && !digits.Contains(S(board, c))) digits.Add(S(board, c));
                Shuffle(digits, rng);
                foreach (var c in cells)
                    if (!digits.Contains(S(board, c))) digits.Add(S(board, c));
                var count = Math.Min(digits.Count, 2 + rng.Next(3));
                if (count < 2) continue;
                var quad = new QuadrupleConstraint { TopLeft = corner };
                quad.Digits.AddRange(digits.GetRange(0, count));
                quad.Digits.Sort();
                overlay.Quadruples.Add(quad);
                foreach (var c in cells) UsedCells.Add(CellKey(c.Row, c.Col));
            }
        }
    }
}
