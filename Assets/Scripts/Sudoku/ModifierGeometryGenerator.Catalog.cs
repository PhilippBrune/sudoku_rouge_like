using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // [REQ: GEN-MOD-002] Geometry for batches B1 and B2 of the modifier catalog (docs/design/modifier-catalog-plan.md).
    // Everything is read off the solution, so the solution always satisfies it and the board stays unique under its
    // base rules. Lines run in 8 directions without crossing themselves and avoid cells other modifiers already use.
    public static partial class ModifierGeometryGenerator
    {
        private static int BaseCount(int size, int small, int medium, int large) =>
            size <= 6 ? small : size <= 8 ? medium : large;

        // ── B1: line variants ──

        private static void GenerateCatalogLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale,
            BossModifierId modifier)
        {
            var size = board.Size;
            switch (modifier)
            {
                case BossModifierId.NonconsecLine:
                    GrowLineSet(board, overlay, rng, scale, LineType.NonconsecLine, BaseCount(size, 2, 3, 4), 4, 6,
                        (path, next) => Math.Abs(S(board, next) - S(board, path[path.Count - 1])) != 1);
                    break;
                case BossModifierId.HighLowAlternating:
                    GrowLineSet(board, overlay, rng, scale, LineType.HighLowAlternating, BaseCount(size, 2, 3, 3), 4, 6,
                        (path, next) => HighLowLineRule.IsLow(S(board, next), size) != HighLowLineRule.IsLow(S(board, path[path.Count - 1]), size));
                    break;
                case BossModifierId.NabnerLine:
                    GrowLineSet(board, overlay, rng, scale, LineType.NabnerLine, BaseCount(size, 2, 3, 4), 3, Math.Min(5, (size + 1) / 2),
                        (path, next) => path.TrueForAll(c => Math.Abs(S(board, c) - S(board, next)) >= 2));
                    break;
                case BossModifierId.EntropicLine:
                case BossModifierId.ModularLine:
                    var type = modifier == BossModifierId.EntropicLine ? LineType.EntropicLine : LineType.ModularLine;
                    GrowLineSet(board, overlay, rng, scale, type, BaseCount(size, 2, 3, 3), 4, 6, (path, next) =>
                    {
                        var g = TriadLineRule.Group(type, S(board, next));
                        if (g == TriadLineRule.Group(type, S(board, path[path.Count - 1]))) return false;
                        return path.Count < 2 || g != TriadLineRule.Group(type, S(board, path[path.Count - 2]));
                    });
                    break;
                case BossModifierId.FastThermo:
                    GrowLineSet(board, overlay, rng, scale, LineType.FastThermo, BaseCount(size, 2, 2, 3), 3,
                        Math.Min(5, 1 + (size - 1) / FastThermoRule.DefaultStep),
                        (path, next) => S(board, next) >= S(board, path[path.Count - 1]) + FastThermoRule.DefaultStep,
                        line => line.LineValue = FastThermoRule.DefaultStep);
                    break;
                case BossModifierId.AmbiguousThermo:
                    // Built rising from the low end, then turned round half of the time: the line shows no bulb, and
                    // the first cell (where the alt-symbol label sits) must not give the direction away.
                    GrowLineSet(board, overlay, rng, scale, LineType.AmbiguousThermo, BaseCount(size, 2, 3, 3), 4, 6,
                        (path, next) => S(board, next) > S(board, path[path.Count - 1]),
                        line => { if (rng.Next(2) == 0) line.Cells.Reverse(); });
                    break;
                case BossModifierId.ZipperLine:
                    GenerateZipperLines(board, overlay, rng, scale);
                    break;
            }
        }

        private static int S(SudokuBoard board, CellCoord c) => board.Solution[c.Row, c.Col];

        /// <summary>
        /// True when the diagonal step from <paramref name="a"/> to <paramref name="b"/> would cross a diagonal step the
        /// path already takes between the other two cells of their 2×2 square (the line would form an X).
        /// </summary>
        private static bool CrossesPath(List<CellCoord> path, CellCoord a, CellCoord b)
        {
            if (a.Row == b.Row || a.Col == b.Col) return false;
            var corner = new CellCoord(a.Row, b.Col);
            var other = new CellCoord(b.Row, a.Col);
            var i = path.IndexOf(corner);
            if (i < 0) return false;
            return (i > 0 && path[i - 1].Equals(other)) || (i < path.Count - 1 && path[i + 1].Equals(other));
        }

        /// <summary>True when two diagonal steps of the line cross in one 2×2 square.</summary>
        private static bool CrossesItself(List<CellCoord> cells)
        {
            for (var i = 0; i < cells.Count - 1; i++)
                if (CrossesPath(cells, cells[i], cells[i + 1])) return true;
            return false;
        }

        // Grows up to the scaled count of lines of one type from random free start cells; canExtend decides whether a
        // free neighbour of the path's end may join. A line needs at least two cells the player fills in.
        private static void GrowLineSet(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale, LineType type,
            int baseCount, int minLength, int maxLength, Func<List<CellCoord>, CellCoord, bool> canExtend,
            Action<ModifierLine> finish = null)
        {
            var size = board.Size;
            if (maxLength < minLength) return;
            var target = ScaledCount(baseCount, scale, 1, 6);
            var used = new bool[size, size];
            var made = 0;
            var candidates = new List<CellCoord>(8);
            for (var attempt = 0; attempt < target * 30 && made < target; attempt++)
            {
                var start = new CellCoord(rng.Next(size), rng.Next(size));
                if (!IsCellFree(start.Row, start.Col, used)) continue;
                var path = new List<CellCoord> { start };
                var length = rng.Next(minLength, maxLength + 1);
                while (path.Count < length)
                {
                    candidates.Clear();
                    var end = path[path.Count - 1];
                    for (var d = 0; d < Dirs.Length; d++)
                    {
                        var next = new CellCoord(end.Row + Dirs[d].Dr, end.Col + Dirs[d].Dc);
                        if (next.Row < 0 || next.Row >= size || next.Col < 0 || next.Col >= size) continue;
                        if (!IsCellFree(next.Row, next.Col, used) || path.Contains(next) || CrossesPath(path, end, next)) continue;
                        if (canExtend(path, next)) candidates.Add(next);
                    }
                    if (candidates.Count == 0) break;
                    path.Add(candidates[rng.Next(candidates.Count)]);
                }
                if (path.Count < minLength || CountOpen(board, path) < 2) continue;

                var line = new ModifierLine { Type = type };
                line.Cells.AddRange(path);
                finish?.Invoke(line);
                overlay.Lines.Add(line);
                foreach (var c in path) used[c.Row, c.Col] = true;
                made++;
            }
        }

        private static int CountOpen(SudokuBoard board, List<CellCoord> cells)
        {
            var open = 0;
            foreach (var c in cells)
                if (!board.GivenMask[c.Row, c.Col]) open++;
            return open;
        }

        // Zipper lines grow outward from the middle: from a middle cell (5 or 7 cells; each pair sums to the middle
        // digit) or from two neighbouring cells (4 or 6 cells; each pair sums to their total).
        private static void GenerateZipperLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var target = ScaledCount(BaseCount(size, 2, 2, 3), scale, 1, 6);
            var used = new bool[size, size];
            var made = 0;
            for (var attempt = 0; attempt < target * 40 && made < target; attempt++)
            {
                var line = TryGrowZipper(board, rng, used, size);
                if (line == null || CountOpen(board, line.Cells) < 2) continue;
                overlay.Lines.Add(line);
                foreach (var c in line.Cells) used[c.Row, c.Col] = true;
                made++;
            }
        }

        private static ModifierLine TryGrowZipper(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var first = new CellCoord(rng.Next(size), rng.Next(size));
            if (!IsCellFree(first.Row, first.Col, used)) return null;
            var left = new List<CellCoord> { first };
            var right = new List<CellCoord> { first };
            int total;
            if (rng.Next(2) == 0)
            {
                total = S(board, first);                       // odd length: the middle digit is the total
                if (total < 3) return null;
            }
            else
            {
                var neighbours = new List<CellCoord>(8);
                for (var d = 0; d < Dirs.Length; d++)
                {
                    var n = new CellCoord(first.Row + Dirs[d].Dr, first.Col + Dirs[d].Dc);
                    if (IsFreeLineCell(n, size, used, left, right)) neighbours.Add(n);
                }
                if (neighbours.Count == 0) return null;
                var second = neighbours[rng.Next(neighbours.Count)];
                right[0] = second;
                total = S(board, first) + S(board, second);   // even length: the middle pair sets the total
            }

            var halves = rng.Next(2, 4);
            var pairs = new List<(CellCoord A, CellCoord B)>();
            for (var h = 0; h < halves; h++)
            {
                pairs.Clear();
                var leftEnd = left[left.Count - 1];
                var rightEnd = right[right.Count - 1];
                for (var d = 0; d < Dirs.Length; d++)
                {
                    var a = new CellCoord(leftEnd.Row + Dirs[d].Dr, leftEnd.Col + Dirs[d].Dc);
                    if (!IsFreeLineCell(a, size, used, left, right)) continue;
                    for (var e = 0; e < Dirs.Length; e++)
                    {
                        var b = new CellCoord(rightEnd.Row + Dirs[e].Dr, rightEnd.Col + Dirs[e].Dc);
                        if (b.Equals(a) || !IsFreeLineCell(b, size, used, left, right)) continue;
                        if (S(board, a) + S(board, b) == total) pairs.Add((a, b));
                    }
                }
                if (pairs.Count == 0) break;
                var (pa, pb) = pairs[rng.Next(pairs.Count)];
                left.Add(pa);
                right.Add(pb);
            }

            var oddLength = left[0].Equals(right[0]);
            if (left.Count < 2 + (oddLength ? 1 : 0)) return null;   // at least 4 cells, or 5 around a middle cell
            var line = new ModifierLine { Type = LineType.ZipperLine };
            for (var i = left.Count - 1; i >= 0; i--) line.Cells.Add(left[i]);
            for (var i = oddLength ? 1 : 0; i < right.Count; i++) line.Cells.Add(right[i]);
            // The two arms grow apart from the middle and can still cross each other.
            return CrossesItself(line.Cells) ? null : line;
        }

        // ── B2: complete Kropki sets, Difference ≥ K, Multiples, Mirror cells ──

        // Full dot sets come last and do not claim cells, so they never crowd out other geometry: every neighbour pair
        // that differs by 1 (white) or stands in a 1:2 ratio (black) gets its dot.
        private static void GenerateFullKropkiSet(SudokuBoard board, ModifierOverlayData overlay, bool black)
        {
            if (black) overlay.FullBlackKropkiNeg = true; else overlay.FullWhiteKropkiNeg = true;
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (c + 1 < size) AddSetDot(board, overlay, new CellCoord(r, c), new CellCoord(r, c + 1), black);
                if (r + 1 < size) AddSetDot(board, overlay, new CellCoord(r, c), new CellCoord(r + 1, c), black);
            }
        }

        private static void AddSetDot(SudokuBoard board, ModifierOverlayData overlay, CellCoord a, CellCoord b, bool black)
        {
            int va = S(board, a), vb = S(board, b);
            var matches = black ? Math.Max(va, vb) == 2 * Math.Min(va, vb) : Math.Abs(va - vb) == 1;
            if (!matches) return;
            foreach (var dot in overlay.KropkiDots)
                if (dot.SumValue == 0 && dot.IsBlack == black && SameEdge(dot.CellA, dot.CellB, a, b)) return;
            overlay.KropkiDots.Add(new KropkiDot { CellA = a, CellB = b, IsBlack = black });
        }

        private static bool SameEdge(CellCoord a1, CellCoord b1, CellCoord a2, CellCoord b2) =>
            (a1.Equals(a2) && b1.Equals(b2)) || (a1.Equals(b2) && b1.Equals(a2));

        /// <summary>True when a dot or pair clue already sits on the edge between the two cells.</summary>
        private static bool HasEdgeMarker(ModifierOverlayData overlay, CellCoord a, CellCoord b)
        {
            foreach (var dot in overlay.KropkiDots)
                if (SameEdge(dot.CellA, dot.CellB, a, b)) return true;
            foreach (var pair in overlay.PairConstraints)
                if (SameEdge(pair.CellA, pair.CellB, a, b)) return true;
            return false;
        }

        private static void GenerateDifferenceKPairs(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var k = DifferenceKPairRule.DefaultK;
            overlay.DifferenceKValue = k;
            var edges = new List<(CellCoord A, CellCoord B)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var a = new CellCoord(r, c);
                foreach (var b in new[] { new CellCoord(r, c + 1), new CellCoord(r + 1, c) })
                {
                    if (b.Row >= size || b.Col >= size) continue;
                    if (board.GivenMask[a.Row, a.Col] || board.GivenMask[b.Row, b.Col]) continue;
                    if (Math.Abs(S(board, a) - S(board, b)) < k || HasEdgeMarker(overlay, a, b)) continue;
                    if (UsedCells.Contains(CellKey(a.Row, a.Col)) || UsedCells.Contains(CellKey(b.Row, b.Col))) continue;
                    edges.Add((a, b));
                }
            }
            Shuffle(edges, rng);
            var count = Math.Min(edges.Count, ScaledCount(BaseCount(size, 6, 8, 10), scale, 2, Math.Max(2, edges.Count)));
            for (var i = 0; i < count; i++)
            {
                overlay.PairConstraints.Add(new AdjacentPairConstraint
                    { CellA = edges[i].A, CellB = edges[i].B, Type = PairConstraintType.DifferenceK, Value = k });
                // Claim both cells, so lines generated after this never run across the tag.
                UsedCells.Add(CellKey(edges[i].A.Row, edges[i].A.Col));
                UsedCells.Add(CellKey(edges[i].B.Row, edges[i].B.Col));
            }
        }

        private static void GenerateMultipleCellMarkers(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var cells = new List<CellCoord>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var cell = new CellCoord(r, c);
                if (board.GivenMask[r, c] || S(board, cell) % MultipleCellsRule.Divisor != 0) continue;
                if (HasMarker(overlay, cell) || UsedCells.Contains(CellKey(r, c))) continue;
                cells.Add(cell);
            }
            Shuffle(cells, rng);
            var count = Math.Min(cells.Count, ScaledCount(BaseCount(size, 4, 6, 8), scale, 2, Math.Max(2, cells.Count)));
            for (var i = 0; i < count; i++)
                overlay.CellMarkers.Add(new CellMarker { Cell = cells[i], Type = MarkerType.Multiple });
        }

        // Mirror pairs: a cell and its reflection through the centre of the grid hold the same digit; at least one of
        // the two is open.
        private static bool HasCornerBadge(ModifierOverlayData overlay, CellCoord cell) =>
            overlay.CellPairs.Exists(p => p.CellA.Equals(cell) || p.CellB.Equals(cell))
            || overlay.CellMarkers.Exists(m => m.Type == MarkerType.DistanceCellSource && m.Cell.Equals(cell));

        private static void GenerateMirrorCellPairs(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var pairs = new List<(CellCoord A, CellCoord B)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var a = new CellCoord(r, c);
                var b = new CellCoord(size - 1 - r, size - 1 - c);
                if (r * size + c >= b.Row * size + b.Col) continue;   // each pair once, never the centre cell
                if (S(board, a) != S(board, b)) continue;
                if (board.GivenMask[a.Row, a.Col] && board.GivenMask[b.Row, b.Col]) continue;
                // The letter badge sits in the upper-right corner, so a mirror cell may also carry a line or a marker,
                // but not another corner badge (opposite sums, distance pairs, distance cells).
                if (HasCornerBadge(overlay, a) || HasCornerBadge(overlay, b)) continue;
                pairs.Add((a, b));
            }
            Shuffle(pairs, rng);
            // Pairs with two open cells first: a given cell would simply reveal its partner's digit.
            var open = pairs.FindAll(p => !board.GivenMask[p.A.Row, p.A.Col] && !board.GivenMask[p.B.Row, p.B.Col]);
            pairs.RemoveAll(p => open.Contains(p));
            pairs.InsertRange(0, open);
            var count = Math.Min(pairs.Count, ScaledCount(BaseCount(size, 2, 3, 3), scale, 1, 4));
            for (var i = 0; i < count; i++)
            {
                overlay.CellPairs.Add(new CellPairConstraint { CellA = pairs[i].A, CellB = pairs[i].B, PairType = CellPairType.Mirror });
                UsedCells.Add(CellKey(pairs[i].A.Row, pairs[i].A.Col));
                UsedCells.Add(CellKey(pairs[i].B.Row, pairs[i].B.Col));
            }
        }
    }
}
