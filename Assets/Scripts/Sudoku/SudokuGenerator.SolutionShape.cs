using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // [REQ: GEN-SOLVE-002] [REQ: GEN-SOLVE-003] Rules that shape the solution itself (BossMechanicsSystem.md "Modifier catalog", batch B5):
    // extra houses the filled grid must keep free of repeats (the X-Sudoku diagonals, the four Hyper windows, the
    // Disjoint groups) and a box filled with a magic square before the rest of the grid. The givens are still chosen
    // for a single solution under the base rules (owner decision 2026-10-19: givens stay as today), which the extra
    // houses keep.
    public sealed class SolutionShape
    {
        /// <summary>Each extra house as cell indices (row × size + column).</summary>
        public readonly List<int[]> Houses = new List<int[]>();

        /// <summary>True: one 3×3 box is filled with a magic square (rows, columns and diagonals add up to 15).</summary>
        public bool MagicSquareBox;

        public bool IsEmpty => Houses.Count == 0 && !MagicSquareBox;

        /// <summary>The shape the active modifiers ask for, or null when none of them shapes the solution.</summary>
        public static SolutionShape For(IList<BossModifierId> modifiers, int size)
        {
            if (modifiers == null) return null;
            var shape = new SolutionShape();
            if (modifiers.Contains(BossModifierId.XSudoku))
            {
                shape.Houses.Add(Diagonal(size, false));
                shape.Houses.Add(Diagonal(size, true));
            }
            if (modifiers.Contains(BossModifierId.HyperSudoku) && size == 9)
                foreach (var top in new[] { 1, 5 })
                foreach (var left in new[] { 1, 5 })
                    shape.Houses.Add(Window(size, top, left));
            if (modifiers.Contains(BossModifierId.DisjointGroups) && size == 9)
                for (var dr = 0; dr < 3; dr++)
                for (var dc = 0; dc < 3; dc++)
                    shape.Houses.Add(DisjointGroup(size, dr, dc));
            // A magic square always holds consecutive neighbours (7 beside 6, 4 and 6 diagonal to 5). Those pairs are blocked;
            // the shape leaves the box out on its own too, because the pre-placed digits are never checked against them.
            shape.MagicSquareBox = modifiers.Contains(BossModifierId.MagicSquareRegion) && size == 9
                                   && !modifiers.Contains(BossModifierId.Nonconsecutive)
                                   && !modifiers.Contains(BossModifierId.NonconsecDiagonal);
            return shape.IsEmpty ? null : shape;
        }

        /// <summary>The four Hyper windows of a 9×9 (top-left corners at rows and columns 1 and 5).</summary>
        public static readonly int[][] HyperWindows =
            { Window(9, 1, 1), Window(9, 1, 5), Window(9, 5, 1), Window(9, 5, 5) };

        /// <summary>The nine Disjoint groups of a 9×9, indexed by (row % 3) × 3 + column % 3.</summary>
        public static readonly int[][] DisjointGroups =
        {
            DisjointGroup(9, 0, 0), DisjointGroup(9, 0, 1), DisjointGroup(9, 0, 2),
            DisjointGroup(9, 1, 0), DisjointGroup(9, 1, 1), DisjointGroup(9, 1, 2),
            DisjointGroup(9, 2, 0), DisjointGroup(9, 2, 1), DisjointGroup(9, 2, 2)
        };

        public static int[] Diagonal(int size, bool anti)
        {
            var cells = new int[size];
            for (var i = 0; i < size; i++) cells[i] = i * size + (anti ? size - 1 - i : i);
            return cells;
        }

        public static int[] Window(int size, int top, int left)
        {
            var cells = new int[9];
            for (var i = 0; i < 9; i++) cells[i] = (top + i / 3) * size + left + i % 3;
            return cells;
        }

        public static int[] DisjointGroup(int size, int dr, int dc)
        {
            var cells = new int[9];
            for (var i = 0; i < 9; i++) cells[i] = (i / 3 * 3 + dr) * size + i % 3 * 3 + dc;
            return cells;
        }

        // The Lo Shu square; every 3×3 magic square of 1–9 is one of its eight turns and mirror images.
        private static readonly int[,] LoShu = { { 2, 7, 6 }, { 9, 5, 1 }, { 4, 3, 8 } };

        internal static int[,] RandomMagicSquare(Random random)
        {
            var square = new int[3, 3];
            var turn = random.Next(4);
            var mirror = random.Next(2) == 1;
            for (var r = 0; r < 3; r++)
            for (var c = 0; c < 3; c++)
            {
                int sr = r, sc = mirror ? 2 - c : c;
                for (var t = 0; t < turn; t++) (sr, sc) = (sc, 2 - sr);
                square[r, c] = LoShu[sr, sc];
            }
            return square;
        }

        /// <summary>Per-attempt bookkeeping: which digits each extra house already holds.</summary>
        internal sealed class State
        {
            private readonly int[][] _housesOfCell;
            private readonly int[] _mask;

            internal State(SolutionShape shape, int size)
            {
                _mask = new int[shape.Houses.Count];
                var lists = new List<int>[size * size];
                for (var h = 0; h < shape.Houses.Count; h++)
                    foreach (var cell in shape.Houses[h])
                        (lists[cell] ??= new List<int>()).Add(h);
                _housesOfCell = new int[size * size][];
                for (var i = 0; i < lists.Length; i++) _housesOfCell[i] = lists[i]?.ToArray() ?? Array.Empty<int>();
            }

            internal int Used(int cell)
            {
                var used = 0;
                foreach (var h in _housesOfCell[cell]) used |= _mask[h];
                return used;
            }

            internal void Set(int cell, int bit, bool on)
            {
                foreach (var h in _housesOfCell[cell])
                    _mask[h] = on ? _mask[h] | bit : _mask[h] & ~bit;
            }
        }
    }

    public static partial class SudokuGenerator
    {
        // Places the magic square in a random 3×3 box of a 9×9 grid with square boxes, before the fill.
        private static void PrefillMagicSquare(int[,] board, int[,] regionMap, int size, int[] rowMask, int[] colMask,
            int[] regionMask, SolutionShape.State houses, Random random)
        {
            if (size != 9) return;
            var box = random.Next(9);
            int top = box / 3 * 3, left = box % 3 * 3;
            var square = SolutionShape.RandomMagicSquare(random);
            for (var r = 0; r < 3; r++)
            for (var c = 0; c < 3; c++)
            {
                int row = top + r, col = left + c, value = square[r, c], bit = 1 << value;
                board[row, col] = value;
                rowMask[row] |= bit;
                colMask[col] |= bit;
                regionMask[regionMap[row, col]] |= bit;
                houses?.Set(row * size + col, bit, true);
            }
        }
    }
}
