using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // [REQ: GEN-MOD-002] Geometry for batch B6 of the modifier catalog (modifier-catalog-plan.md): whisper lines with
    // their own K, the inverted whisper zone, 1:3 dots, distance pairs, opposite sums, counting circles and distance
    // cells. Everything is read off the solution and avoids cells other modifiers already use; each piece keeps at least
    // one open cell.
    public static partial class ModifierGeometryGenerator
    {
        // ── whisper lines with their own K ──

        // One K per puzzle, shown on every line's tag: 2 or 3 up to 7×7, 3 or 6 on larger boards.
        private static void GenerateWhisperKLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var k = size <= 7 ? 2 + rng.Next(2) : 3 + 3 * rng.Next(2);
            GrowLineSet(board, overlay, rng, scale, LineType.WhisperGeneralized, BaseCount(size, 2, 3, 3), k >= 6 ? 3 : 4, 6,
                (path, next) => Math.Abs(S(board, next) - S(board, path[path.Count - 1])) >= k,
                line => line.LineValue = k);
        }

        // ── inverted whisper zone ──

        public const int NegationZoneSize = 3;

        // A 3×3 zone, then whisper lines that start inside it and run out of it: each line has at least one step inside
        // the zone (neighbours differ by less than 5) and one outside (5 or more).
        private static void GenerateNegationZone(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            if (size < NegationZoneSize + 2) return;
            var top = rng.Next(size - NegationZoneSize + 1);
            var left = rng.Next(size - NegationZoneSize + 1);
            var zone = new List<CellCoord>();
            for (var r = top; r < top + NegationZoneSize; r++)
            for (var c = left; c < left + NegationZoneSize; c++)
                zone.Add(new CellCoord(r, c));

            bool Inside(CellCoord c) => c.Row >= top && c.Row < top + NegationZoneSize && c.Col >= left && c.Col < left + NegationZoneSize;
            var target = ScaledCount(2, scale, 1, 4);
            var used = new bool[size, size];
            var made = 0;
            var options = new List<CellCoord>(8);
            for (var attempt = 0; attempt < target * 60 && made < target; attempt++)
            {
                var start = zone[rng.Next(zone.Count)];
                if (!IsCellFree(start.Row, start.Col, used)) continue;
                var path = new List<CellCoord> { start };
                var length = rng.Next(5, 8);
                while (path.Count < length)
                {
                    options.Clear();
                    var end = path[path.Count - 1];
                    foreach (var (dr, dc) in Dirs)
                    {
                        var next = new CellCoord(end.Row + dr, end.Col + dc);
                        if (next.Row < 0 || next.Row >= size || next.Col < 0 || next.Col >= size) continue;
                        if (!IsCellFree(next.Row, next.Col, used) || path.Contains(next) || CrossesPath(path, end, next)) continue;
                        var difference = Math.Abs(S(board, next) - S(board, end));
                        var inside = Inside(end) && Inside(next);
                        if (inside ? difference < ZoneWhisperRule.Gap : difference >= ZoneWhisperRule.Gap) options.Add(next);
                    }
                    if (options.Count == 0) break;
                    path.Add(options[rng.Next(options.Count)]);
                }
                var insideSteps = 0;
                for (var i = 0; i < path.Count - 1; i++)
                    if (Inside(path[i]) && Inside(path[i + 1])) insideSteps++;
                if (insideSteps == 0 || insideSteps == path.Count - 1 || path.Count < 4 || CountOpen(board, path) < 2) continue;

                var line = new ModifierLine { Type = LineType.ZoneWhisper };
                line.Cells.AddRange(path);
                overlay.Lines.Add(line);
                foreach (var c in path) used[c.Row, c.Col] = true;
                made++;
            }
            if (made == 0) return;
            overlay.NegationZone.AddRange(zone);
            // The modifiers after this one keep out of the zone (complete Kropki dot sets still cover the whole board).
            foreach (var c in zone) UsedCells.Add(CellKey(c.Row, c.Col));
        }

        // ── 1:3 dots ──

        private static void GenerateRatioNPairs(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var n = RatioNPairRule.DefaultN;
            var edges = new List<(CellCoord A, CellCoord B)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var a = new CellCoord(r, c);
                foreach (var b in new[] { new CellCoord(r, c + 1), new CellCoord(r + 1, c) })
                {
                    if (b.Row >= size || b.Col >= size) continue;
                    if (board.GivenMask[a.Row, a.Col] && board.GivenMask[b.Row, b.Col]) continue;
                    int va = S(board, a), vb = S(board, b);
                    if (Math.Max(va, vb) != Math.Min(va, vb) * n || HasEdgeMarker(overlay, a, b)) continue;
                    if (UsedCells.Contains(CellKey(a.Row, a.Col)) || UsedCells.Contains(CellKey(b.Row, b.Col))) continue;
                    edges.Add((a, b));
                }
            }
            Shuffle(edges, rng);
            var count = Math.Min(edges.Count, ScaledCount(BaseCount(size, 3, 4, 5), scale, 1, 8));
            for (var i = 0; i < count; i++)
            {
                overlay.PairConstraints.Add(new AdjacentPairConstraint
                    { CellA = edges[i].A, CellB = edges[i].B, Type = PairConstraintType.RatioN, Value = n });
                UsedCells.Add(CellKey(edges[i].A.Row, edges[i].A.Col));
                UsedCells.Add(CellKey(edges[i].B.Row, edges[i].B.Col));
            }
        }

        // ── distance pairs ──

        // Pairs a diagonal step (2 steps apart) or a knight's move (3 steps) apart, so the link crosses only a corner or
        // an edge and never the digit of a cell in between.
        private static readonly (int Dr, int Dc)[] DistanceOffsets = { (1, 1), (1, -1), (1, 2), (2, 1), (1, -2), (2, -1) };

        private static void GenerateDistancePairs(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var pairs = new List<(CellCoord A, CellCoord B, int Steps)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            foreach (var (dr, dc) in DistanceOffsets)
            {
                var a = new CellCoord(r, c);
                var b = new CellCoord(r + dr, c + dc);
                if (b.Row >= size || b.Col < 0 || b.Col >= size) continue;
                var steps = Math.Abs(dr) + Math.Abs(dc);
                if (Math.Abs(S(board, a) - S(board, b)) != steps) continue;
                if (board.GivenMask[a.Row, a.Col] || board.GivenMask[b.Row, b.Col]) continue;
                if (UsedCells.Contains(CellKey(a.Row, a.Col)) || UsedCells.Contains(CellKey(b.Row, b.Col))) continue;
                pairs.Add((a, b, steps));
            }
            Shuffle(pairs, rng);
            var target = ScaledCount(BaseCount(size, 2, 3, 4), scale, 1, 6);
            var taken = new HashSet<long>();
            var made = 0;
            foreach (var (a, b, steps) in pairs)
            {
                if (made >= target) break;
                if (taken.Contains(CellKey(a.Row, a.Col)) || taken.Contains(CellKey(b.Row, b.Col))) continue;
                overlay.CellPairs.Add(new CellPairConstraint { CellA = a, CellB = b, PairType = CellPairType.Distance, Value = steps });
                foreach (var cell in new[] { a, b })
                {
                    taken.Add(CellKey(cell.Row, cell.Col));
                    UsedCells.Add(CellKey(cell.Row, cell.Col));
                }
                made++;
            }
        }

        // ── opposite sums ──

        // The sum most cell pairs mirrored through the centre share; up to a few of those pairs carry it.
        private static void GenerateOppositeSums(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var bySum = new Dictionary<int, List<(CellCoord A, CellCoord B)>>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var a = new CellCoord(r, c);
                var b = new CellCoord(size - 1 - r, size - 1 - c);
                if (r * size + c >= b.Row * size + b.Col) continue;   // each pair once, never the centre cell
                if (board.GivenMask[a.Row, a.Col] && board.GivenMask[b.Row, b.Col]) continue;
                if (UsedCells.Contains(CellKey(a.Row, a.Col)) || UsedCells.Contains(CellKey(b.Row, b.Col))) continue;
                var sum = S(board, a) + S(board, b);
                if (!bySum.TryGetValue(sum, out var list)) bySum[sum] = list = new List<(CellCoord, CellCoord)>();
                list.Add((a, b));
            }
            if (bySum.Count == 0) return;
            var sums = new List<int>(bySum.Keys);
            sums.Sort();
            var best = sums[0];
            foreach (var s in sums)
                if (bySum[s].Count > bySum[best].Count) best = s;
            var chosen = bySum[best];
            Shuffle(chosen, rng);
            var count = Math.Min(chosen.Count, ScaledCount(BaseCount(size, 2, 3, 3), scale, 1, 5));
            for (var i = 0; i < count; i++)
            {
                overlay.CellPairs.Add(new CellPairConstraint
                    { CellA = chosen[i].A, CellB = chosen[i].B, PairType = CellPairType.OppositeSum, Value = best });
                UsedCells.Add(CellKey(chosen[i].A.Row, chosen[i].A.Col));
                UsedCells.Add(CellKey(chosen[i].B.Row, chosen[i].B.Col));
            }
        }

        // ── counting circles ──

        // Small digits d, each circled exactly d times, on open cells only: 2 to 7 circles in all.
        private static readonly int[][] CountingSets = { new[] { 1, 2 }, new[] { 1, 3 }, new[] { 2, 3 }, new[] { 1, 2, 3 }, new[] { 3 }, new[] { 2, 4 }, new[] { 1, 2, 4 } };

        private static void GenerateCountingCircles(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var byDigit = new Dictionary<int, List<CellCoord>>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var cell = new CellCoord(r, c);
                if (board.GivenMask[r, c] || HasMarker(overlay, cell) || UsedCells.Contains(CellKey(r, c))) continue;
                var d = S(board, cell);
                if (!byDigit.TryGetValue(d, out var list)) byDigit[d] = list = new List<CellCoord>();
                list.Add(cell);
            }
            var sets = new List<int[]>(CountingSets);
            Shuffle(sets, rng);
            foreach (var set in sets)
            {
                if (!Array.TrueForAll(set, d => d <= size && byDigit.TryGetValue(d, out var cells) && cells.Count >= d)) continue;
                foreach (var d in set)
                {
                    var cells = byDigit[d];
                    Shuffle(cells, rng);
                    for (var i = 0; i < d; i++)
                        overlay.CellMarkers.Add(new CellMarker { Cell = cells[i], Type = MarkerType.CountingCircle });
                }
                return;
            }
        }

        // ── distance cells ──

        private static void GenerateDistanceCells(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var target = ScaledCount(BaseCount(size, 2, 3, 4), scale, 1, 6);
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var ring = new CellCoord(rng.Next(size), rng.Next(size));
                if (UsedCells.Contains(CellKey(ring.Row, ring.Col)) || HasMarker(overlay, ring)) continue;
                var sources = new List<CellCoord>();
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                {
                    var cell = new CellCoord(r, c);
                    if (cell.Equals(ring) || board.GivenMask[r, c] || HasMarker(overlay, cell) || UsedCells.Contains(CellKey(r, c))) continue;
                    if (S(board, cell) == Math.Abs(r - ring.Row) + Math.Abs(c - ring.Col)) sources.Add(cell);
                }
                if (sources.Count == 0) continue;
                Shuffle(sources, rng);
                overlay.CellMarkers.Add(new CellMarker { Cell = ring, Type = MarkerType.DistanceCellTarget });
                for (var i = 0; i < Math.Min(target, sources.Count); i++)
                    overlay.CellMarkers.Add(new CellMarker { Cell = sources[i], Type = MarkerType.DistanceCellSource });
                return;
            }
        }
    }
}
