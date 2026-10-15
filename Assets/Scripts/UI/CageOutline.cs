using System.Collections.Generic;
using SudokuRoguelike.Sudoku;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// The dashed outline of one killer cage as straight runs, inset into the cage's cells. Coordinates start at the
    /// board's top-left corner with y growing downwards. A run stops <c>inset</c> inside the cell at a convex corner and
    /// reaches <c>inset</c> into the neighbouring cage cell at a concave corner, so the runs meet and the cage reads as one
    /// shape instead of one box per cell. The layout mockups use the same rule (cageRuns() in
    /// docs/design/layout-proposals/src/common.js).
    /// </summary>
    // [REQ: UI-STYLE-006]
    public static class CageOutline
    {
        public readonly struct Run
        {
            public Run(Vector2 from, Vector2 to)
            {
                From = from;
                To = to;
            }

            public Vector2 From { get; }
            public Vector2 To { get; }
            public bool Horizontal => Mathf.Approximately(From.y, To.y);
            public float Length => Vector2.Distance(From, To);
        }

        private static readonly int[] Sides = { -1, 1 };

        public static List<Run> Build(IReadOnlyList<CellCoord> cells, int boardSize,
            float cellW, float cellH, float gapX, float gapY, float inset)
        {
            var runs = new List<Run>();
            if (cells == null || cells.Count == 0) return runs;

            var cage = new HashSet<(int, int)>();
            for (var i = 0; i < cells.Count; i++) cage.Add((cells[i].Row, cells[i].Col));
            bool In(int r, int c) => cage.Contains((r, c));
            float Left(int c) => c * (cellW + gapX);
            float Top(int r) => r * (cellH + gapY);

            // Top (side -1) and bottom (side +1) borders, row by row.
            foreach (var side in Sides)
            for (var r = 0; r < boardSize; r++)
            {
                var start = -1;
                for (var c = 0; c <= boardSize; c++)
                {
                    if (c < boardSize && In(r, c) && !In(r + side, c))
                    {
                        if (start < 0) start = c;
                        continue;
                    }
                    if (start < 0) continue;
                    var y = side < 0 ? Top(r) + inset : Top(r) + cellH - inset;
                    var x0 = In(r, start - 1) ? Left(start - 1) + cellW - inset : Left(start) + inset;
                    var x1 = In(r, c) ? Left(c) + inset : Left(c - 1) + cellW - inset;
                    runs.Add(new Run(new Vector2(x0, y), new Vector2(x1, y)));
                    start = -1;
                }
            }

            // Left (side -1) and right (side +1) borders, column by column.
            foreach (var side in Sides)
            for (var c = 0; c < boardSize; c++)
            {
                var start = -1;
                for (var r = 0; r <= boardSize; r++)
                {
                    if (r < boardSize && In(r, c) && !In(r, c + side))
                    {
                        if (start < 0) start = r;
                        continue;
                    }
                    if (start < 0) continue;
                    var x = side < 0 ? Left(c) + inset : Left(c) + cellW - inset;
                    var y0 = In(start - 1, c) ? Top(start - 1) + cellH - inset : Top(start) + inset;
                    var y1 = In(r, c) ? Top(r) + inset : Top(r - 1) + cellH - inset;
                    runs.Add(new Run(new Vector2(x, y0), new Vector2(x, y1)));
                    start = -1;
                }
            }
            return runs;
        }

        /// <summary>
        /// Dashes along a run of <paramref name="length"/> as (offset, length) pairs. The gaps are stretched so the
        /// first dash starts at 0 and the last one ends at <paramref name="length"/>: the outline's corners stay closed.
        /// </summary>
        public static List<(float offset, float length)> Dashes(float length, float dash, float gap)
        {
            var dashes = new List<(float, float)>();
            if (length <= 0f) return dashes;
            var count = Mathf.Max(1, Mathf.FloorToInt((length + gap) / (dash + gap)));
            if (count == 1)
            {
                dashes.Add((0f, length));
                return dashes;
            }
            var stretchedGap = (length - count * dash) / (count - 1);
            for (var i = 0; i < count; i++)
                dashes.Add((i * (dash + stretchedGap), dash));
            return dashes;
        }
    }
}
