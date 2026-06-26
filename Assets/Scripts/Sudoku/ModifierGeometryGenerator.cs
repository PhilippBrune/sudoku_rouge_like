using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class ModifierGeometryGenerator
    {
        private static readonly (int Dr, int Dc)[] Dirs = { (-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, 1), (-1, 1), (1, -1) };
        private static readonly (int Dr, int Dc)[] CageDirs = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        private static float IntensityScale(BossModifierIntensity intensity) => intensity switch
        {
            BossModifierIntensity.Low      => 0.5f,
            BossModifierIntensity.High     => 1.5f,
            BossModifierIntensity.VeryHigh => 2.0f,
            _                              => 1.0f
        };

        private static int ScaledCount(int baseCount, float scale, int min, int max)
            => max < min ? max : Math.Clamp((int)Math.Round(baseCount * scale), min, max);

        // [ThreadStatic] ensures each thread (main thread and Task.Run background boss-bake)
        // gets its own HashSet instance, eliminating concurrent-access corruption.
        [System.ThreadStatic]
        private static HashSet<long> _usedCells;
        private static HashSet<long> UsedCells => _usedCells ??= new HashSet<long>();

        private static long CellKey(int r, int c) => (long)r * 1000 + c;

        private static void MarkCellsUsed(List<CellCoord> cells)
        {
            for (var i = 0; i < cells.Count; i++)
                UsedCells.Add(CellKey(cells[i].Row, cells[i].Col));
        }

        /// <summary>
        /// True when the cell is not claimed by another line in this modifier pass
        /// AND not claimed by any previous modifier in this generation run.
        /// </summary>
        private static bool IsCellFree(int r, int c, bool[,] used)
            => !used[r, c] && !UsedCells.Contains(CellKey(r, c));

        // [REQ: GEN-MOD-001] [REQ: BOSS-MOD-003] Generate: single entry point for all modifier overlays; dispatches per-modifier to dedicated geometry generators
        public static ModifierOverlayData Generate(SudokuBoard board, List<BossModifierId> modifiers, int seed,
            BossModifierIntensity intensity = BossModifierIntensity.Medium)
        {
            var overlay = new ModifierOverlayData();
            var rng = new Random(seed);
            var scale = IntensityScale(intensity);
            UsedCells.Clear();

            for (var i = 0; i < modifiers.Count; i++)
            {
                var linesBefore = overlay.Lines.Count;
                var dotsBefore = overlay.KropkiDots.Count;
                var cagesBefore = overlay.KillerCages.Count;
                var arrowsBefore = overlay.Arrows.Count;
                var markersBefore = overlay.CellMarkers.Count;

                switch (modifiers[i])
                {
                    case BossModifierId.GermanWhispers:
                        GenerateWhisperLines(board, overlay, rng, LineType.GermanWhispers, 5, scale);
                        break;
                    case BossModifierId.DutchWhispers:
                        GenerateWhisperLines(board, overlay, rng, LineType.DutchWhispers, 4, scale);
                        break;
                    case BossModifierId.ParityLines:
                        GenerateParityLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.RenbanLines:
                        GenerateRenbanLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.DifferenceKropki:
                        GenerateKropkiDots(board, overlay, rng, false, scale);
                        break;
                    case BossModifierId.RatioKropki:
                        GenerateKropkiDots(board, overlay, rng, true, scale);
                        break;
                    case BossModifierId.KillerCages:
                        GenerateKillerCages(board, overlay, rng, scale);
                        break;
                    case BossModifierId.ArrowSums:
                        GenerateArrows(board, overlay, rng, scale);
                        break;
                    case BossModifierId.FogOfWar:
                        GenerateFog(board, overlay, rng);
                        break;
                    case BossModifierId.Palindrome:
                        GeneratePalindromeLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.Thermo:
                        GenerateThermoLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.BetweenLines:
                        GenerateBetweenLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.EvenOdd:
                        GenerateEvenOddMarkers(board, overlay, rng, scale);
                        break;
                    // Nonconsecutive and Antiknight are global — no geometry needed

                    // Extended global negative constraints (IDs 15-20): no geometry
                    case BossModifierId.Antiking:
                    case BossModifierId.AntiBishop:
                    case BossModifierId.NonconsecDiagonal:
                    case BossModifierId.DistanceGe2:
                    case BossModifierId.EntropyGlobal:
                    case BossModifierId.ModularRegions:
                        break; // global rules — no geometry needed

                    case BossModifierId.ConsecutiveLine:
                        GenerateConsecutiveLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.SlowThermo:
                        GenerateSlowThermoLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.UniqueSetLine:
                        GenerateUniqueSetLines(board, overlay, rng, scale);
                        break;
                    case BossModifierId.FullKropki:
                        GenerateFullKropki(board, overlay);
                        break;
                    case BossModifierId.SumKropki:
                        GenerateSumKropkiDots(board, overlay, rng, scale);
                        break;
                    case BossModifierId.GreaterLessThan:
                        GenerateGreaterLessThanPairs(board, overlay, rng, scale);
                        break;
                    case BossModifierId.XVPairs:
                        GenerateXVPairs(board, overlay, rng, scale);
                        break;
                    case BossModifierId.PrimeCells:
                        GeneratePrimeCellMarkers(board, overlay, rng, scale);
                        break;
                    case BossModifierId.FortressCells:
                        GenerateFortressCellMarkers(board, overlay, rng, scale);
                        break;
                }

                MarkOverlayCellsUsed(overlay, linesBefore, dotsBefore, cagesBefore, arrowsBefore, markersBefore);
            }

            return overlay;
        }

        // ── Fog ──

        private static void GenerateFog(SudokuBoard board, ModifierOverlayData overlay, Random rng)
        {
            var size = board.Size;

            // Build fog as a HashSet during generation to allow O(1) removals.
            var fogSet = new HashSet<(int, int)>();
            var givenCells = new List<CellCoord>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (board.GivenMask[r, c])
                    givenCells.Add(new CellCoord(r, c));
                else
                    fogSet.Add((r, c));
            }

            Shuffle(givenCells, rng);
            var revealCount = Math.Max(1, givenCells.Count / 3);
            for (var i = 0; i < revealCount; i++)
            {
                var gr = givenCells[i].Row;
                var gc = givenCells[i].Col;
                fogSet.Remove((gr, gc));
                if (gr > 0)        fogSet.Remove((gr - 1, gc));
                if (gr < size - 1) fogSet.Remove((gr + 1, gc));
                if (gc > 0)        fogSet.Remove((gr, gc - 1));
                if (gc < size - 1) fogSet.Remove((gr, gc + 1));
            }

            foreach (var (r, c) in fogSet)
                overlay.FogCells.Add(new CellCoord(r, c));
        }

        public static void RevealAdjacentFog(ModifierOverlayData overlay, int row, int col, int size)
        {
            RemoveFogCell(overlay, row, col);
            if (row > 0) RemoveFogCell(overlay, row - 1, col);
            if (row < size - 1) RemoveFogCell(overlay, row + 1, col);
            if (col > 0) RemoveFogCell(overlay, row, col - 1);
            if (col < size - 1) RemoveFogCell(overlay, row, col + 1);
        }

        private static void RemoveFogCell(ModifierOverlayData overlay, int row, int col)
        {
            for (var i = overlay.FogCells.Count - 1; i >= 0; i--)
            {
                if (overlay.FogCells[i].Row == row && overlay.FogCells[i].Col == col)
                {
                    overlay.FogCells.RemoveAt(i);
                    return;
                }
            }
        }

        // ── Whisper Lines (German / Dutch) ──

        private static void GenerateWhisperLines(SudokuBoard board, ModifierOverlayData overlay,
            Random rng, LineType type, int minDiff, float scale)
        {
            var size = board.Size;
            if (size - 1 < minDiff) return;
            var baseTarget = size <= 6 ? 2 : size <= 8 ? 3 : 4;
            var target = ScaledCount(baseTarget, scale, 1, 8);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 20 && count < target; attempt++)
            {
                var line = TryBuildWhisperLine(board, rng, used, minDiff, size);
                if (line == null) continue;

                line.Type = type;
                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildWhisperLine(SudokuBoard board, Random rng,
            bool[,] used, int minDiff, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;
            // Prefer starting on a non-given cell so the line has visible constraint
            if (board.GivenMask[startRow, startCol] && rng.NextDouble() < 0.8) return null;

            var line = new ModifierLine();
            line.Cells.Add(new CellCoord(startRow, startCol));

            var targetLen = rng.Next(4, 7);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used) || IsInLine(line, nr, nc)) continue;

                    var lastVal = board.Solution[last.Row, last.Col];
                    var nextVal = board.Solution[nr, nc];
                    if (Math.Abs(lastVal - nextVal) >= minDiff)
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 4 ? line : null;
        }

        private static void GenerateParityLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : size <= 8 ? 3 : 4;
            var target = ScaledCount(baseTarget, scale, 1, 8);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 20 && count < target; attempt++)
            {
                var line = TryBuildParityLine(board, rng, used, size);
                if (line == null) continue;

                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildParityLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var line = new ModifierLine { Type = LineType.ParityLine };
            line.Cells.Add(new CellCoord(startRow, startCol));

            var targetLen = rng.Next(4, 7);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used) || IsInLine(line, nr, nc)) continue;

                    var lastVal = board.Solution[last.Row, last.Col];
                    var nextVal = board.Solution[nr, nc];
                    if ((lastVal % 2) != (nextVal % 2))
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 4 ? line : null;
        }

        private static void GenerateRenbanLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 30 && count < target; attempt++)
            {
                var line = TryBuildRenbanLine(board, rng, used, size);
                if (line == null) continue;

                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildRenbanLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var line = new ModifierLine { Type = LineType.RenbanLine };
            line.Cells.Add(new CellCoord(startRow, startCol));
            var values = new List<int> { board.Solution[startRow, startCol] };

            var targetLen = rng.Next(4, 6);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var candidates = new List<(CellCoord Coord, int Val)>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used) || IsInLine(line, nr, nc)) continue;

                    var nextVal = board.Solution[nr, nc];
                    if (values.Contains(nextVal)) continue;

                    var testMin = nextVal;
                    var testMax = nextVal;
                    for (var v = 0; v < values.Count; v++)
                    {
                        if (values[v] < testMin) testMin = values[v];
                        if (values[v] > testMax) testMax = values[v];
                    }

                    if (testMax - testMin < targetLen)
                        candidates.Add((new CellCoord(nr, nc), nextVal));
                }

                if (candidates.Count == 0) break;
                var pick = candidates[rng.Next(candidates.Count)];
                line.Cells.Add(pick.Coord);
                values.Add(pick.Val);
            }

            if (line.Cells.Count < 4) return null;

            values.Sort();
            if (values[values.Count - 1] - values[0] != line.Cells.Count - 1) return null;

            return line;
        }

        // ── Palindrome Lines ──

        private static void GeneratePalindromeLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 20 && count < target; attempt++)
            {
                var line = TryBuildPalindromeLine(board, rng, used, size);
                if (line == null) continue;

                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildPalindromeLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var cells = new List<CellCoord> { new CellCoord(startRow, startCol) };
            var rawLen = rng.Next(4, 7);
            var targetLen = rawLen % 2 == 0 ? rawLen + 1 : rawLen;

            for (var step = 1; step < targetLen; step++)
            {
                var last = cells[cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used)) continue;
                    var alreadyIn = false;
                    for (var ci = 0; ci < cells.Count; ci++)
                        if (cells[ci].Row == nr && cells[ci].Col == nc) { alreadyIn = true; break; }
                    if (!alreadyIn) candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            if (cells.Count < 4) return null;

            var n = cells.Count;
            for (var j = 0; j < n / 2; j++)
            {
                var a = cells[j];
                var b = cells[n - 1 - j];
                if (board.Solution[a.Row, a.Col] != board.Solution[b.Row, b.Col]) return null;
            }

            var line = new ModifierLine { Type = LineType.Palindrome };
            line.Cells.AddRange(cells);
            return line;
        }

        // ── Thermo Lines ──

        private static void GenerateThermoLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 30 && count < target; attempt++)
            {
                var line = TryBuildThermoLine(board, rng, used, size);
                if (line == null) continue;

                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildThermoLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var line = new ModifierLine { Type = LineType.Thermo };
            line.Cells.Add(new CellCoord(startRow, startCol));

            var targetLen = rng.Next(4, 7);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var lastVal = board.Solution[last.Row, last.Col];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used) || IsInLine(line, nr, nc)) continue;
                    if (board.Solution[nr, nc] > lastVal)
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 4 ? line : null;
        }

        // ── Between Lines ──

        private static void GenerateBetweenLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 30 && count < target; attempt++)
            {
                var line = TryBuildBetweenLine(board, rng, used, size);
                if (line == null) continue;

                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildBetweenLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var cells = new List<CellCoord> { new CellCoord(startRow, startCol) };
            var targetLen = rng.Next(4, 6);

            for (var step = 1; step < targetLen; step++)
            {
                var last = cells[cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used)) continue;
                    var alreadyIn = false;
                    for (var ci = 0; ci < cells.Count; ci++)
                        if (cells[ci].Row == nr && cells[ci].Col == nc) { alreadyIn = true; break; }
                    if (!alreadyIn) candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            if (cells.Count < 3) return null;

            var endA = cells[0];
            var endB = cells[cells.Count - 1];
            var valA = board.Solution[endA.Row, endA.Col];
            var valB = board.Solution[endB.Row, endB.Col];
            if (valA == valB) return null;

            var lo = Math.Min(valA, valB);
            var hi = Math.Max(valA, valB);

            for (var j = 1; j < cells.Count - 1; j++)
            {
                var v = board.Solution[cells[j].Row, cells[j].Col];
                if (v <= lo || v >= hi) return null;
            }

            var line = new ModifierLine { Type = LineType.BetweenLine };
            line.Cells.AddRange(cells);
            return line;
        }

        // ── Kropki Dots ──

        private static void GenerateKropkiDots(SudokuBoard board, ModifierOverlayData overlay,
            Random rng, bool isBlack, float scale)
        {
            var size = board.Size;
            var pairs = new List<(CellCoord A, CellCoord B)>();

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = board.Solution[r, c];

                if (c + 1 < size)
                {
                    var v2 = board.Solution[r, c + 1];
                    // Neither cell may be pre-filled — Kropki dots must not connect to given cells
                    if (MatchesDot(v, v2, isBlack) && !board.GivenMask[r, c] && !board.GivenMask[r, c + 1])
                        pairs.Add((new CellCoord(r, c), new CellCoord(r, c + 1)));
                }

                if (r + 1 < size)
                {
                    var v2 = board.Solution[r + 1, c];
                    if (MatchesDot(v, v2, isBlack) && !board.GivenMask[r, c] && !board.GivenMask[r + 1, c])
                        pairs.Add((new CellCoord(r, c), new CellCoord(r + 1, c)));
                }
            }

            var baseCount = size <= 6 ? 6 : size <= 8 ? 8 : 12;
            var dotCount = Math.Min(pairs.Count, ScaledCount(baseCount, scale, 2, pairs.Count));
            Shuffle(pairs, rng);

            for (var i = 0; i < dotCount; i++)
            {
                overlay.KropkiDots.Add(new KropkiDot
                {
                    CellA = pairs[i].A,
                    CellB = pairs[i].B,
                    IsBlack = isBlack
                });
            }
        }

        private static bool MatchesDot(int v1, int v2, bool isBlack)
        {
            if (!isBlack) return Math.Abs(v1 - v2) == 1;
            var bigger = Math.Max(v1, v2);
            var smaller = Math.Min(v1, v2);
            return smaller > 0 && bigger == 2 * smaller;
        }

        // ── Killer Cages ──

        private static void GenerateKillerCages(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 3 : size <= 8 ? 4 : 5;
            var target = ScaledCount(baseTarget, scale, 1, 10);
            var used = new bool[size, size];
            var starts = new List<CellCoord>(size * size);
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (IsCellFree(r, c, used))
                    starts.Add(new CellCoord(r, c));
            Shuffle(starts, rng);

            for (var attempt = 0; attempt < starts.Count && overlay.KillerCages.Count < target; attempt++)
            {
                var start = starts[attempt];
                if (!IsCellFree(start.Row, start.Col, used)) continue;

                var cage = TryBuildCage(board, rng, used, size, start);
                if (cage == null) continue;

                overlay.KillerCages.Add(cage);
                for (var c = 0; c < cage.Cells.Count; c++)
                    used[cage.Cells[c].Row, cage.Cells[c].Col] = true;
            }
        }

        private static KillerCage TryBuildCage(SudokuBoard board, Random rng, bool[,] used, int size, CellCoord start)
        {
            var startRow = start.Row;
            var startCol = start.Col;
            if (!IsCellFree(startRow, startCol, used)) return null;

            var cage = new KillerCage();
            cage.Cells.Add(new CellCoord(startRow, startCol));
            var values = new HashSet<int> { board.Solution[startRow, startCol] };

            var targetSize = rng.Next(2, 5);

            for (var step = 1; step < targetSize; step++)
            {
                var candidates = new List<CellCoord>();
                var candidateKeys = new HashSet<long>();

                for (var ci = 0; ci < cage.Cells.Count; ci++)
                {
                    var cell = cage.Cells[ci];
                    for (var d = 0; d < CageDirs.Length; d++)
                    {
                        var nr = cell.Row + CageDirs[d].Dr;
                        var nc = cell.Col + CageDirs[d].Dc;
                        if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                        if (used[nr, nc] || IsInCage(cage, nr, nc)) continue;

                        var val = board.Solution[nr, nc];
                        if (values.Contains(val)) continue;
                        if (!candidateKeys.Add(CellKey(nr, nc))) continue;

                        candidates.Add(new CellCoord(nr, nc));
                    }
                }

                if (candidates.Count == 0) break;
                var pick = candidates[rng.Next(candidates.Count)];
                cage.Cells.Add(pick);
                values.Add(board.Solution[pick.Row, pick.Col]);
            }

            if (cage.Cells.Count < 2) return null;

            var sum = 0;
            for (var c = 0; c < cage.Cells.Count; c++)
                sum += board.Solution[cage.Cells[c].Row, cage.Cells[c].Col];
            cage.Sum = sum;

            return cage;
        }

        // ── Arrow Sums ──

        private static void GenerateArrows(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];

            for (var attempt = 0; attempt < target * 30 && overlay.Arrows.Count < target; attempt++)
            {
                var arrow = TryBuildArrow(board, rng, used, size);
                if (arrow == null) continue;

                overlay.Arrows.Add(arrow);
                used[arrow.CircleCell.Row, arrow.CircleCell.Col] = true;
                for (var c = 0; c < arrow.ArrowCells.Count; c++)
                    used[arrow.ArrowCells[c].Row, arrow.ArrowCells[c].Col] = true;
            }
        }

        private static ArrowConstraint TryBuildArrow(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var circleRow = rng.Next(size);
            var circleCol = rng.Next(size);
            if (used[circleRow, circleCol]) return null;

            var circleVal = board.Solution[circleRow, circleCol];
            if (circleVal < 3) return null;

            var arrow = new ArrowConstraint { CircleCell = new CellCoord(circleRow, circleCol) };
            var runningSum = 0;

            var pathLen = rng.Next(2, 4);
            var lastRow = circleRow;
            var lastCol = circleCol;

            for (var step = 0; step < pathLen; step++)
            {
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = lastRow + Dirs[d].Dr;
                    var nc = lastCol + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (used[nr, nc] || (nr == circleRow && nc == circleCol)) continue;
                    if (IsOnArrowPath(arrow, nr, nc)) continue;

                    var nextVal = board.Solution[nr, nc];
                    if (runningSum + nextVal <= circleVal)
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                var pick = candidates[rng.Next(candidates.Count)];
                arrow.ArrowCells.Add(pick);
                runningSum += board.Solution[pick.Row, pick.Col];
                lastRow = pick.Row;
                lastCol = pick.Col;
            }

            if (arrow.ArrowCells.Count < 2 || runningSum != circleVal) return null;

            return arrow;
        }

        // ── EvenOdd Markers ──

        private static void GenerateEvenOddMarkers(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseCount = size <= 6 ? 6 : size <= 8 ? 8 : 12;
            var count = ScaledCount(baseCount, scale, 2, size * size / 2);

            var allCells = new List<CellCoord>(size * size);
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                allCells.Add(new CellCoord(r, c));

            Shuffle(allCells, rng);

            for (var i = 0; i < allCells.Count && overlay.CellMarkers.Count < count; i++)
            {
                var cell = allCells[i];
                var v = board.Solution[cell.Row, cell.Col];
                overlay.CellMarkers.Add(new CellMarker
                {
                    Cell = cell,
                    Type = v % 2 == 0 ? MarkerType.Even : MarkerType.Odd
                });
            }
        }

        // ── Consecutive Lines ──

        private static void GenerateConsecutiveLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 30 && count < target; attempt++)
            {
                var line = TryBuildConsecutiveLine(board, rng, used, size);
                if (line == null) continue;
                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildConsecutiveLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var line = new ModifierLine { Type = LineType.ConsecutiveLine };
            line.Cells.Add(new CellCoord(startRow, startCol));
            var targetLen = rng.Next(3, 6);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var lastVal = board.Solution[last.Row, last.Col];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used) || IsInLine(line, nr, nc)) continue;
                    if (Math.Abs(board.Solution[nr, nc] - lastVal) == 1)
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 4 ? line : null;
        }

        // ── Slow Thermo Lines ──

        private static void GenerateSlowThermoLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 30 && count < target; attempt++)
            {
                var line = TryBuildSlowThermoLine(board, rng, used, size);
                if (line == null) continue;
                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildSlowThermoLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var line = new ModifierLine { Type = LineType.SlowThermo };
            line.Cells.Add(new CellCoord(startRow, startCol));
            var targetLen = rng.Next(3, 6);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var lastVal = board.Solution[last.Row, last.Col];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used) || IsInLine(line, nr, nc)) continue;
                    if (board.Solution[nr, nc] >= lastVal)
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 4 ? line : null;
        }

        // ── Unique Set Lines ──

        private static void GenerateUniqueSetLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 30 && count < target; attempt++)
            {
                var line = TryBuildUniqueSetLine(board, rng, used, size);
                if (line == null) continue;
                overlay.Lines.Add(line);
                for (var c = 0; c < line.Cells.Count; c++)
                    used[line.Cells[c].Row, line.Cells[c].Col] = true;
                count++;
            }
        }

        private static ModifierLine TryBuildUniqueSetLine(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (!IsCellFree(startRow, startCol, used)) return null;

            var line = new ModifierLine { Type = LineType.UniqueSetLine };
            line.Cells.Add(new CellCoord(startRow, startCol));
            var seenVals = new System.Collections.Generic.HashSet<int> { board.Solution[startRow, startCol] };
            var targetLen = rng.Next(4, 6);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (!IsCellFree(nr, nc, used) || IsInLine(line, nr, nc)) continue;
                    var v = board.Solution[nr, nc];
                    if (!seenVals.Contains(v))
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                var pick = candidates[rng.Next(candidates.Count)];
                line.Cells.Add(pick);
                seenVals.Add(board.Solution[pick.Row, pick.Col]);
            }

            return line.Cells.Count >= 4 ? line : null;
        }

        // ── Full Kropki ──

        private static void GenerateFullKropki(SudokuBoard board, ModifierOverlayData overlay)
        {
            overlay.FullKropkiNegativeInference = true;
            var size = board.Size;

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = board.Solution[r, c];

                if (c + 1 < size)
                {
                    var v2 = board.Solution[r, c + 1];
                    if (Math.Abs(v - v2) == 1)
                        overlay.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(r, c), CellB = new CellCoord(r, c + 1), IsBlack = false });
                    else if (Math.Max(v, v2) == 2 * Math.Min(v, v2))
                        overlay.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(r, c), CellB = new CellCoord(r, c + 1), IsBlack = true });
                }

                if (r + 1 < size)
                {
                    var v2 = board.Solution[r + 1, c];
                    if (Math.Abs(v - v2) == 1)
                        overlay.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(r, c), CellB = new CellCoord(r + 1, c), IsBlack = false });
                    else if (Math.Max(v, v2) == 2 * Math.Min(v, v2))
                        overlay.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(r, c), CellB = new CellCoord(r + 1, c), IsBlack = true });
                }
            }
        }

        // ── Sum Kropki ──

        private static void GenerateSumKropkiDots(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var pairs = new List<(CellCoord A, CellCoord B, int Sum)>();

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (c + 1 < size && !board.GivenMask[r, c] && !board.GivenMask[r, c + 1])
                    pairs.Add((new CellCoord(r, c), new CellCoord(r, c + 1), board.Solution[r, c] + board.Solution[r, c + 1]));
                if (r + 1 < size && !board.GivenMask[r, c] && !board.GivenMask[r + 1, c])
                    pairs.Add((new CellCoord(r, c), new CellCoord(r + 1, c), board.Solution[r, c] + board.Solution[r + 1, c]));

            }

            var baseCount = size <= 6 ? 5 : size <= 8 ? 8 : 10;
            var dotCount = Math.Min(pairs.Count, ScaledCount(baseCount, scale, 2, pairs.Count));
            Shuffle(pairs, rng);

            for (var i = 0; i < dotCount; i++)
            {
                overlay.KropkiDots.Add(new KropkiDot
                {
                    CellA = pairs[i].A,
                    CellB = pairs[i].B,
                    IsBlack = false,
                    SumValue = pairs[i].Sum
                });
            }
        }

        // ── Greater/Less Than Pairs ──

        private static void GenerateGreaterLessThanPairs(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var allPairs = new List<(CellCoord A, CellCoord B)>();

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (c + 1 < size) allPairs.Add((new CellCoord(r, c), new CellCoord(r, c + 1)));
                if (r + 1 < size) allPairs.Add((new CellCoord(r, c), new CellCoord(r + 1, c)));
            }

            var baseCount = size <= 6 ? 6 : size <= 8 ? 8 : 12;
            var count = Math.Min(allPairs.Count, ScaledCount(baseCount, scale, 3, allPairs.Count));
            Shuffle(allPairs, rng);

            for (var i = 0; i < count; i++)
            {
                var (a, b) = allPairs[i];
                var va = board.Solution[a.Row, a.Col];
                var vb = board.Solution[b.Row, b.Col];
                if (va == vb) continue;
                overlay.PairConstraints.Add(new AdjacentPairConstraint
                {
                    CellA = a,
                    CellB = b,
                    Type = va > vb ? PairConstraintType.GreaterThan : PairConstraintType.LessThan
                });
            }
        }

        // ── XV Pairs ──

        private static void GenerateXVPairs(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var xPairs = new List<(CellCoord A, CellCoord B)>();
            var vPairs = new List<(CellCoord A, CellCoord B)>();

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                void CheckPair(int r2, int c2)
                {
                    if (r2 >= size || c2 >= size) return;
                    var s = board.Solution[r, c] + board.Solution[r2, c2];
                    if (s == 10) xPairs.Add((new CellCoord(r, c), new CellCoord(r2, c2)));
                    else if (s == 5) vPairs.Add((new CellCoord(r, c), new CellCoord(r2, c2)));
                }
                CheckPair(r, c + 1);
                CheckPair(r + 1, c);
            }

            var baseCount = size <= 6 ? 4 : 6;
            Shuffle(xPairs, rng);
            Shuffle(vPairs, rng);

            var xCount = Math.Min(xPairs.Count, ScaledCount(baseCount / 2, scale, 1, xPairs.Count));
            var vCount = Math.Min(vPairs.Count, ScaledCount(baseCount / 2, scale, 1, vPairs.Count));

            for (var i = 0; i < xCount; i++)
                overlay.PairConstraints.Add(new AdjacentPairConstraint { CellA = xPairs[i].A, CellB = xPairs[i].B, Type = PairConstraintType.SumX });
            for (var i = 0; i < vCount; i++)
                overlay.PairConstraints.Add(new AdjacentPairConstraint { CellA = vPairs[i].A, CellB = vPairs[i].B, Type = PairConstraintType.SumV });
        }

        // ── Prime Cell Markers ──

        private static void GeneratePrimeCellMarkers(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var primeCells = new List<CellCoord>();

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = board.Solution[r, c];
                if (v == 2 || v == 3 || v == 5 || v == 7)
                    primeCells.Add(new CellCoord(r, c));
            }

            var baseCount = size <= 6 ? 5 : size <= 8 ? 8 : 12;
            var count = Math.Min(primeCells.Count, ScaledCount(baseCount, scale, 2, primeCells.Count));
            Shuffle(primeCells, rng);

            for (var i = 0; i < count; i++)
                overlay.CellMarkers.Add(new CellMarker { Cell = primeCells[i], Type = MarkerType.Prime });
        }

        // ── Fortress Cell Markers ──

        private static void GenerateFortressCellMarkers(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale)
        {
            var size = board.Size;
            var fortCells = new List<CellCoord>();

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = board.Solution[r, c];
                var isFortress = true;
                if (r > 0 && board.Solution[r - 1, c] >= v) isFortress = false;
                if (r < size - 1 && board.Solution[r + 1, c] >= v) isFortress = false;
                if (c > 0 && board.Solution[r, c - 1] >= v) isFortress = false;
                if (c < size - 1 && board.Solution[r, c + 1] >= v) isFortress = false;
                if (isFortress) fortCells.Add(new CellCoord(r, c));
            }

            var baseCount = size <= 6 ? 4 : size <= 8 ? 6 : 9;
            var count = Math.Min(fortCells.Count, ScaledCount(baseCount, scale, 1, fortCells.Count));
            Shuffle(fortCells, rng);

            for (var i = 0; i < count; i++)
                overlay.CellMarkers.Add(new CellMarker { Cell = fortCells[i], Type = MarkerType.Fortress });
        }

        // ── Helpers ──

        private static bool IsInLine(ModifierLine line, int row, int col)
        {
            for (var i = 0; i < line.Cells.Count; i++)
                if (line.Cells[i].Row == row && line.Cells[i].Col == col) return true;
            return false;
        }

        private static bool IsInCage(KillerCage cage, int row, int col)
        {
            for (var i = 0; i < cage.Cells.Count; i++)
                if (cage.Cells[i].Row == row && cage.Cells[i].Col == col) return true;
            return false;
        }

        private static bool IsOnArrowPath(ArrowConstraint arrow, int row, int col)
        {
            for (var i = 0; i < arrow.ArrowCells.Count; i++)
                if (arrow.ArrowCells[i].Row == row && arrow.ArrowCells[i].Col == col) return true;
            return false;
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void MarkOverlayCellsUsed(ModifierOverlayData overlay,
            int linesBefore, int dotsBefore, int cagesBefore, int arrowsBefore, int markersBefore)
        {
            for (var i = linesBefore; i < overlay.Lines.Count; i++)
                MarkCellsUsed(overlay.Lines[i].Cells);

            for (var i = dotsBefore; i < overlay.KropkiDots.Count; i++)
            {
                UsedCells.Add(CellKey(overlay.KropkiDots[i].CellA.Row, overlay.KropkiDots[i].CellA.Col));
                UsedCells.Add(CellKey(overlay.KropkiDots[i].CellB.Row, overlay.KropkiDots[i].CellB.Col));
            }

            for (var i = cagesBefore; i < overlay.KillerCages.Count; i++)
                MarkCellsUsed(overlay.KillerCages[i].Cells);

            for (var i = arrowsBefore; i < overlay.Arrows.Count; i++)
            {
                UsedCells.Add(CellKey(overlay.Arrows[i].CircleCell.Row, overlay.Arrows[i].CircleCell.Col));
                MarkCellsUsed(overlay.Arrows[i].ArrowCells);
            }

            for (var i = markersBefore; i < overlay.CellMarkers.Count; i++)
                UsedCells.Add(CellKey(overlay.CellMarkers[i].Cell.Row, overlay.CellMarkers[i].Cell.Col));
        }
    }
}
