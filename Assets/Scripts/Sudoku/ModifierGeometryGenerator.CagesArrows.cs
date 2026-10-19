using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // [REQ: GEN-MOD-002] Geometry for the cage, arrow and sum-line modifiers of the catalog (modifier-catalog-plan.md,
    // batches B3 and B6). Everything is read off the solution and avoids cells other modifiers already use. Each piece
    // keeps at least one cell open, so it never sits on givens alone.
    public static partial class ModifierGeometryGenerator
    {
        // ── cages ──

        private static void GenerateTypedCages(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale,
            KillerCageType type)
        {
            var size = board.Size;
            var baseCount = type == KillerCageType.HiddenSum || type == KillerCageType.Difference
                ? BaseCount(size, 3, 4, 5)
                : BaseCount(size, 2, 3, 4);
            var target = ScaledCount(baseCount, scale, 1, 8);
            var used = new bool[size, size];
            var made = 0;
            for (var attempt = 0; attempt < target * 40 && made < target; attempt++)
            {
                var cage = TryGrowTypedCage(board, rng, used, size, type);
                if (cage == null) continue;
                overlay.KillerCages.Add(cage);
                foreach (var c in cage.Cells) used[c.Row, c.Col] = true;
                made++;
            }
        }

        private static KillerCage TryGrowTypedCage(SudokuBoard board, Random rng, bool[,] used, int size, KillerCageType type)
        {
            var pair = type == KillerCageType.Difference || type == KillerCageType.Ratio;
            var maxCells = pair ? 2 : type == KillerCageType.Product ? 3 : 4;
            var length = pair ? 2 : rng.Next(2, maxCells + 1);
            var start = new CellCoord(rng.Next(size), rng.Next(size));
            if (!IsCellFree(start.Row, start.Col, used)) return null;

            var cells = new List<CellCoord> { start };
            var candidates = new List<CellCoord>(8);
            while (cells.Count < length)
            {
                candidates.Clear();
                foreach (var cell in cells)
                foreach (var (dr, dc) in CageDirs)
                {
                    var n = new CellCoord(cell.Row + dr, cell.Col + dc);
                    if (n.Row < 0 || n.Row >= size || n.Col < 0 || n.Col >= size) continue;
                    if (!IsCellFree(n.Row, n.Col, used) || cells.Contains(n) || candidates.Contains(n)) continue;
                    if (CageAccepts(board, cells, n, type, length)) candidates.Add(n);
                }
                if (candidates.Count == 0) break;
                cells.Add(candidates[rng.Next(candidates.Count)]);
            }
            if (cells.Count < 2 || CountOpen(board, cells) < 1) return null;
            if (type == KillerCageType.RenbanSet && !IsRun(board, cells)) return null;

            var cage = new KillerCage { CageType = type, Sum = CageLabel(board, cells, type) };
            cage.Cells.AddRange(cells);
            return cage;
        }

        private static bool CageAccepts(SudokuBoard board, List<CellCoord> cells, CellCoord next, KillerCageType type, int length)
        {
            var v = S(board, next);
            foreach (var c in cells)
                if (S(board, c) == v) return false;
            switch (type)
            {
                case KillerCageType.Ratio:
                {
                    int a = S(board, cells[0]), lo = Math.Min(a, v), hi = Math.Max(a, v);
                    return hi % lo == 0 && hi / lo >= 2;
                }
                case KillerCageType.RenbanSet:
                {
                    int lo = v, hi = v;
                    foreach (var c in cells) { lo = Math.Min(lo, S(board, c)); hi = Math.Max(hi, S(board, c)); }
                    return hi - lo <= length - 1;
                }
                default:
                    return true;
            }
        }

        private static bool IsRun(SudokuBoard board, List<CellCoord> cells)
        {
            int lo = int.MaxValue, hi = int.MinValue;
            foreach (var c in cells) { lo = Math.Min(lo, S(board, c)); hi = Math.Max(hi, S(board, c)); }
            return hi - lo == cells.Count - 1;   // distinct digits spanning exactly the cage size
        }

        private static int CageLabel(SudokuBoard board, List<CellCoord> cells, KillerCageType type)
        {
            int sum = 0, product = 1, lo = int.MaxValue, hi = int.MinValue;
            foreach (var c in cells)
            {
                var v = S(board, c);
                sum += v; product *= v; lo = Math.Min(lo, v); hi = Math.Max(hi, v);
            }
            return type switch
            {
                KillerCageType.Product => product,
                KillerCageType.Difference => hi - lo,
                KillerCageType.Ratio => hi / lo,
                _ => sum   // Hidden sums are stored but never drawn; Renban cages carry no number
            };
        }

        // ── arrows ──

        private static void GenerateTypedArrows(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale, ArrowType type)
        {
            var size = board.Size;
            var target = ScaledCount(BaseCount(size, 2, 2, 3), scale, 1, 6);
            var used = new bool[size, size];
            var made = 0;
            for (var attempt = 0; attempt < target * 60 && made < target; attempt++)
            {
                var arrow = type switch
                {
                    ArrowType.Average => TryBuildAverageArrow(board, rng, used, size),
                    ArrowType.DoubleEndpoint => TryBuildDoubleArrow(board, rng, used, size),
                    ArrowType.Pill => TryBuildPillArrow(board, rng, used, size),
                    _ => null
                };
                if (arrow == null) continue;
                overlay.Arrows.Add(arrow);
                foreach (var c in ArrowCellsOf(arrow)) used[c.Row, c.Col] = true;
                made++;
            }
        }

        private static IEnumerable<CellCoord> ArrowCellsOf(ArrowConstraint arrow)
        {
            yield return arrow.CircleCell;
            if (arrow.ArrowType == ArrowType.DoubleEndpoint || arrow.ArrowType == ArrowType.Pill) yield return arrow.SecondCircle;
            foreach (var c in arrow.ArrowCells) yield return c;
        }

        // A random walk in 8 directions from the end of `taken`, over free cells not taken yet.
        private static List<CellCoord> RandomWalk(Random rng, bool[,] used, int size, List<CellCoord> taken, CellCoord from, int steps)
        {
            var path = new List<CellCoord>();
            var end = from;
            var options = new List<CellCoord>(8);
            for (var s = 0; s < steps; s++)
            {
                options.Clear();
                foreach (var (dr, dc) in Dirs)
                {
                    var n = new CellCoord(end.Row + dr, end.Col + dc);
                    if (n.Row < 0 || n.Row >= size || n.Col < 0 || n.Col >= size) continue;
                    if (!IsCellFree(n.Row, n.Col, used) || taken.Contains(n) || path.Contains(n)) continue;
                    options.Add(n);
                }
                if (options.Count == 0) return null;
                end = options[rng.Next(options.Count)];
                path.Add(end);
            }
            return path;
        }

        private static int SumOf(SudokuBoard board, List<CellCoord> cells)
        {
            var sum = 0;
            foreach (var c in cells) sum += S(board, c);
            return sum;
        }

        private static ArrowConstraint TryBuildAverageArrow(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var circle = new CellCoord(rng.Next(size), rng.Next(size));
            if (!IsCellFree(circle.Row, circle.Col, used)) return null;
            var path = RandomWalk(rng, used, size, new List<CellCoord> { circle }, circle, rng.Next(2, 4));
            if (path == null || SumOf(board, path) != S(board, circle) * path.Count) return null;
            if (CountOpen(board, path) + (board.GivenMask[circle.Row, circle.Col] ? 0 : 1) < 2) return null;
            var arrow = new ArrowConstraint { CircleCell = circle, ArrowType = ArrowType.Average };
            arrow.ArrowCells.AddRange(path);
            return arrow;
        }

        private static ArrowConstraint TryBuildDoubleArrow(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var first = new CellCoord(rng.Next(size), rng.Next(size));
            if (!IsCellFree(first.Row, first.Col, used)) return null;
            var taken = new List<CellCoord> { first };
            var path = RandomWalk(rng, used, size, taken, first, rng.Next(2, 4));
            if (path == null) return null;
            taken.AddRange(path);
            var want = SumOf(board, path) - S(board, first);
            var ends = RandomWalkOptions(used, size, taken, path[path.Count - 1]).FindAll(c => S(board, c) == want);
            if (ends.Count == 0) return null;
            var second = ends[rng.Next(ends.Count)];
            if (CountOpen(board, path) < 2) return null;
            var arrow = new ArrowConstraint { CircleCell = first, SecondCircle = second, ArrowType = ArrowType.DoubleEndpoint };
            arrow.ArrowCells.AddRange(path);
            return arrow;
        }

        private static ArrowConstraint TryBuildPillArrow(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            // The pill: two cells side by side or one above the other, read top or left first.
            var tens = new CellCoord(rng.Next(size), rng.Next(size));
            var units = rng.Next(2) == 0 ? new CellCoord(tens.Row, tens.Col + 1) : new CellCoord(tens.Row + 1, tens.Col);
            if (units.Row >= size || units.Col >= size) return null;
            if (!IsCellFree(tens.Row, tens.Col, used) || !IsCellFree(units.Row, units.Col, used)) return null;
            var number = S(board, tens) * 10 + S(board, units);
            var taken = new List<CellCoord> { tens, units };
            var path = RandomWalk(rng, used, size, taken, units, rng.Next(2, 6));
            if (path == null) return null;
            // Cut the walk where it reaches the number; a walk that never hits it exactly is thrown away.
            var sum = 0;
            for (var i = 0; i < path.Count; i++)
            {
                sum += S(board, path[i]);
                if (sum < number) continue;
                if (sum > number || i < 1) return null;
                path.RemoveRange(i + 1, path.Count - i - 1);
                if (CountOpen(board, path) < 1) return null;
                var arrow = new ArrowConstraint { CircleCell = tens, SecondCircle = units, ArrowType = ArrowType.Pill };
                arrow.ArrowCells.AddRange(path);
                return arrow;
            }
            return null;
        }

        private static List<CellCoord> RandomWalkOptions(bool[,] used, int size, List<CellCoord> taken, CellCoord from)
        {
            var options = new List<CellCoord>(8);
            foreach (var (dr, dc) in Dirs)
            {
                var n = new CellCoord(from.Row + dr, from.Col + dc);
                if (n.Row < 0 || n.Row >= size || n.Col < 0 || n.Col >= size) continue;
                if (IsCellFree(n.Row, n.Col, used) && !taken.Contains(n)) options.Add(n);
            }
            return options;
        }

        // ── region sum lines and N-lines ──

        // Built one piece at a time: a first piece inside one region sets the total (or the N-line's number does), each
        // further piece steps into a region the line has not visited and is searched to hit the total exactly.
        private static void GenerateRegionSumLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale, LineType type)
        {
            if (board.RegionMap == null) return;
            var size = board.Size;
            var target = ScaledCount(BaseCount(size, 2, 2, 3), scale, 1, 5);
            var used = new bool[size, size];
            var made = 0;
            for (var attempt = 0; attempt < target * 60 && made < target; attempt++)
            {
                var line = TryGrowRegionSumLine(board, rng, used, size, type);
                if (line == null || CountOpen(board, line.Cells) < 2) continue;
                overlay.Lines.Add(line);
                foreach (var c in line.Cells) used[c.Row, c.Col] = true;
                made++;
            }
        }

        public const int NLineTotal = 10;

        private static ModifierLine TryGrowRegionSumLine(SudokuBoard board, Random rng, bool[,] used, int size, LineType type)
        {
            var start = new CellCoord(rng.Next(size), rng.Next(size));
            if (!IsCellFree(start.Row, start.Col, used)) return null;
            var path = new List<CellCoord>();
            var visited = new HashSet<int>();
            var total = type == LineType.NLine ? NLineTotal : 0;

            if (total == 0)
            {
                // The first piece: one to three cells inside the start region; its sum becomes the total.
                path.Add(start);
                var extra = RandomWalkInRegion(board, rng, used, size, path, start, rng.Next(0, 3));
                if (extra == null) return null;
                path.AddRange(extra);
                total = SumOf(board, path);
            }
            else if (!TryPiece(board, rng, used, size, path, start, total)) return null;
            visited.Add(board.RegionMap[start.Row, start.Col]);

            var pieces = rng.Next(2, 4);
            for (var p = 1; p < pieces; p++)
            {
                var entries = RandomWalkOptions(used, size, path, path[path.Count - 1])
                    .FindAll(c => !visited.Contains(board.RegionMap[c.Row, c.Col]));
                Shuffle(entries, rng);
                var placed = false;
                foreach (var entry in entries)
                {
                    if (!TryPiece(board, rng, used, size, path, entry, total)) continue;
                    visited.Add(board.RegionMap[entry.Row, entry.Col]);
                    placed = true;
                    break;
                }
                if (!placed) break;
            }
            if (visited.Count < 2) return null;

            var line = new ModifierLine { Type = type, LineValue = type == LineType.NLine ? NLineTotal : 0 };
            line.Cells.AddRange(path);
            return line;
        }

        // Adds a piece that starts at `entry`, stays in its region and sums to exactly `total` (depth-first, random order).
        private static bool TryPiece(SudokuBoard board, Random rng, bool[,] used, int size, List<CellCoord> path, CellCoord entry, int total)
        {
            var piece = new List<CellCoord> { entry };
            if (!SearchPiece(board, rng, used, size, path, piece, S(board, entry), total, board.RegionMap[entry.Row, entry.Col])) return false;
            path.AddRange(piece);
            return true;
        }

        private static bool SearchPiece(SudokuBoard board, Random rng, bool[,] used, int size, List<CellCoord> path,
            List<CellCoord> piece, int sum, int total, int region)
        {
            if (sum == total) return true;
            if (sum > total || piece.Count >= 4) return false;
            var options = RandomWalkOptions(used, size, path, piece[piece.Count - 1])
                .FindAll(c => board.RegionMap[c.Row, c.Col] == region && !piece.Contains(c));
            Shuffle(options, rng);
            foreach (var next in options)
            {
                piece.Add(next);
                if (SearchPiece(board, rng, used, size, path, piece, sum + S(board, next), total, region)) return true;
                piece.RemoveAt(piece.Count - 1);
            }
            return false;
        }

        private static List<CellCoord> RandomWalkInRegion(SudokuBoard board, Random rng, bool[,] used, int size,
            List<CellCoord> taken, CellCoord from, int steps)
        {
            var region = board.RegionMap[from.Row, from.Col];
            var walk = new List<CellCoord>();
            var end = from;
            for (var s = 0; s < steps; s++)
            {
                var all = new List<CellCoord>(taken);
                all.AddRange(walk);
                var options = RandomWalkOptions(used, size, all, end).FindAll(c => board.RegionMap[c.Row, c.Col] == region);
                if (options.Count == 0) break;
                end = options[rng.Next(options.Count)];
                walk.Add(end);
            }
            return walk;
        }

        // ── sum lines ──

        private static void GenerateSumLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            GrowLineSet(board, overlay, rng, scale, LineType.SumLine, BaseCount(board.Size, 2, 2, 3), 3, 5,
                (path, next) => true,
                line => line.LineValue = SumOf(board, line.Cells));
        }
    }
}
