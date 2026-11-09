using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // [REQ: BOSS-MOD-008] A rule's piece must tell the player something (owner, 0.4.3 playtest): a no-repeat line inside one box
    // repeats the box's own rule, and a line, cage, arrow or marked cell that only touches givens has nothing left to say.
    // [REQ: BOSS-MOD-009] Lines may run as long as the board is wide.
    public static partial class ModifierGeometryGenerator
    {
        public static int LongestLine(int size) => Math.Max(6, size);

        /// <summary>[REQ: BOSS-MOD-010] Owner, 0.4.3 playtest: a rule showed up only two or three times in its puzzle. A rule alone
        /// gets twice as many pieces; the more rules share the board, the less room each has, down to the old count from five.</summary>
        public static float PiecesPerRule(int rules) => Math.Max(1f, 2.25f - 0.25f * rules);

        private static int LineLength(Random rng, int min, int size) => rng.Next(min, LongestLine(size) + 1);

        /// <summary>True when two of the cells share no row, column or region, so "no repeats" adds something.</summary>
        public static bool SaysMoreThanTheHouses(SudokuBoard board, List<CellCoord> cells)
        {
            for (var i = 0; i < cells.Count; i++)
            for (var j = i + 1; j < cells.Count; j++)
            {
                var a = cells[i];
                var b = cells[j];
                if (a.Row != b.Row && a.Col != b.Col && board.RegionMap[a.Row, a.Col] != board.RegionMap[b.Row, b.Col]) return true;
            }
            return false;
        }

        public static void DropPiecesWithoutGain(SudokuBoard board, ModifierOverlayData overlay)
        {
            Drop(board, overlay.Lines, l => (int)l.Type, l => l.Cells);
            Drop(board, overlay.KillerCages, c => (int)c.CageType, c => c.Cells);
            Drop(board, overlay.Arrows, a => (int)a.ArrowType, a =>
            {
                var cells = new List<CellCoord>(a.ArrowCells) { a.CircleCell };
                if (a.ArrowType == ArrowType.DoubleEndpoint || a.ArrowType == ArrowType.Pill) cells.Add(a.SecondCircle);
                return cells;
            });
            // Only markers that speak about their own cell: a counting circle or a fortress also tells about the others.
            var plain = overlay.CellMarkers.FindAll(m => m.Type == MarkerType.Even || m.Type == MarkerType.Odd || m.Type == MarkerType.Prime);
            var before = new List<CellMarker>(plain);
            Drop(board, plain, m => (int)m.Type, m => new[] { m.Cell });
            overlay.CellMarkers.RemoveAll(m => before.Contains(m) && !plain.Contains(m));
        }

        // A piece goes only while another of its kind still speaks: a rule never disappears from the board.
        private static void Drop<T>(SudokuBoard board, List<T> pieces, Func<T, int> kind, Func<T, IEnumerable<CellCoord>> cells)
        {
            bool Open(T piece)
            {
                foreach (var c in cells(piece))
                    if (!board.GivenMask[c.Row, c.Col]) return true;
                return false;
            }
            var speaking = new HashSet<int>();
            foreach (var piece in pieces)
                if (Open(piece)) speaking.Add(kind(piece));
            pieces.RemoveAll(piece => !Open(piece) && speaking.Contains(kind(piece)));
        }
    }
}
