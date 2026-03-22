using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class ModifierGeometryGenerator
    {
        private static readonly (int Dr, int Dc)[] Dirs = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        /// <summary>Returns a multiplier (0.5 / 1.0 / 1.5 / 2.0) for the given intensity.</summary>
        private static float IntensityScale(BossModifierIntensity intensity) => intensity switch
        {
            BossModifierIntensity.Low      => 0.5f,
            BossModifierIntensity.High     => 1.5f,
            BossModifierIntensity.VeryHigh => 2.0f,
            _                              => 1.0f   // Medium
        };

        /// <summary>Scales a base count by intensity and clamps it to [min, max].</summary>
        private static int ScaledCount(int baseCount, float scale, int min, int max)
            => Math.Clamp((int)Math.Round(baseCount * scale), min, max);

        /// <summary>
        /// Tracks cells used by previous modifiers for spatial separation.
        /// Line generators bias start positions away from these cells.
        /// </summary>
        private static readonly HashSet<long> _usedCells = new();

        private static long CellKey(int r, int c) => (long)r * 1000 + c;

        /// <summary>Pick a start cell biased away from already-used cells.</summary>
        private static CellCoord PickBiasedStart(SudokuBoard board, Random rng)
        {
            var size = board.Size;
            // Try up to 10 random cells, prefer ones not in _usedCells
            CellCoord best = new CellCoord { Row = rng.Next(size), Col = rng.Next(size) };
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var r = rng.Next(size);
                var c = rng.Next(size);
                if (!_usedCells.Contains(CellKey(r, c)))
                    return new CellCoord { Row = r, Col = c };
            }
            return best;
        }

        /// <summary>Mark cells as used for spatial separation tracking.</summary>
        private static void MarkCellsUsed(List<CellCoord> cells)
        {
            for (var i = 0; i < cells.Count; i++)
                _usedCells.Add(CellKey(cells[i].Row, cells[i].Col));
        }

        public static ModifierOverlayData Generate(SudokuBoard board, List<BossModifierId> modifiers, int seed,
            BossModifierIntensity intensity = BossModifierIntensity.Medium)
        {
            var overlay = new ModifierOverlayData();
            var rng = new Random(seed);
            var scale = IntensityScale(intensity);
            _usedCells.Clear();

            for (var i = 0; i < modifiers.Count; i++)
            {
                // Snapshot counts before this modifier generates geometry
                var linesBefore = overlay.Lines.Count;
                var dotsBefore = overlay.Dots.Count;
                var cagesBefore = overlay.Cages.Count;
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
                        GenerateKropkiDots(board, overlay, rng, DotType.White, scale);
                        break;
                    case BossModifierId.RatioKropki:
                        GenerateKropkiDots(board, overlay, rng, DotType.Black, scale);
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
                }

                // Mark cells used by this modifier for spatial separation
                MarkOverlayCellsUsed(overlay, linesBefore, dotsBefore, cagesBefore, arrowsBefore, markersBefore);
            }

            return overlay;
        }

        private static void GenerateWhisperLines(SudokuBoard board, ModifierOverlayData overlay,
            Random rng, LineType type, int minDiff, float scale = 1f)
        {
            var size = board.Size;
            // On a board of size N, max possible difference is N-1.
            // German Whispers (minDiff=5) needs size >= 6; Dutch (minDiff=4) needs size >= 5.
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
            if (used[startRow, startCol]) return null;

            var line = new ModifierLine();
            line.Cells.Add(new CellCoord(startRow, startCol));

            var targetLen = rng.Next(3, 6);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (used[nr, nc] || IsInLine(line, nr, nc)) continue;

                    var lastVal = board.Solution[last.Row, last.Col];
                    var nextVal = board.Solution[nr, nc];
                    if (Math.Abs(lastVal - nextVal) >= minDiff)
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 3 ? line : null;
        }

        private static void GenerateParityLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
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

        private static ModifierLine TryBuildParityLine(SudokuBoard board, Random rng,
            bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (used[startRow, startCol]) return null;

            var line = new ModifierLine { Type = LineType.Parity };
            line.Cells.Add(new CellCoord(startRow, startCol));

            var targetLen = rng.Next(3, 6);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (used[nr, nc] || IsInLine(line, nr, nc)) continue;

                    var lastVal = board.Solution[last.Row, last.Col];
                    var nextVal = board.Solution[nr, nc];
                    if ((lastVal % 2) != (nextVal % 2))
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 3 ? line : null;
        }

        private static void GenerateRenbanLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
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

        private static ModifierLine TryBuildRenbanLine(SudokuBoard board, Random rng,
            bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (used[startRow, startCol]) return null;

            var line = new ModifierLine { Type = LineType.Renban };
            line.Cells.Add(new CellCoord(startRow, startCol));
            var values = new List<int> { board.Solution[startRow, startCol] };

            var targetLen = rng.Next(3, 5);

            for (var step = 1; step < targetLen; step++)
            {
                var last = line.Cells[line.Cells.Count - 1];
                var candidates = new List<(CellCoord Coord, int Val)>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (used[nr, nc] || IsInLine(line, nr, nc)) continue;

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

            if (line.Cells.Count < 3) return null;

            values.Sort();
            if (values[values.Count - 1] - values[0] != line.Cells.Count - 1) return null;

            return line;
        }

        private static void GenerateKropkiDots(SudokuBoard board, ModifierOverlayData overlay,
            Random rng, DotType targetType, float scale = 1f)
        {
            var size = board.Size;
            var pairs = new List<(CellCoord A, CellCoord B)>();

            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    var v = board.Solution[r, c];

                    if (c + 1 < size)
                    {
                        var v2 = board.Solution[r, c + 1];
                        if (MatchesDot(v, v2, targetType))
                            pairs.Add((new CellCoord(r, c), new CellCoord(r, c + 1)));
                    }

                    if (r + 1 < size)
                    {
                        var v2 = board.Solution[r + 1, c];
                        if (MatchesDot(v, v2, targetType))
                            pairs.Add((new CellCoord(r, c), new CellCoord(r + 1, c)));
                    }
                }
            }

            var baseCount = size <= 6 ? 6 : size <= 8 ? 8 : 12;
            var dotCount = Math.Min(pairs.Count, ScaledCount(baseCount, scale, 2, pairs.Count));
            Shuffle(pairs, rng);

            for (var i = 0; i < dotCount; i++)
            {
                overlay.Dots.Add(new KropkiDot
                {
                    CellA = pairs[i].A,
                    CellB = pairs[i].B,
                    Type = targetType
                });
            }
        }

        private static bool MatchesDot(int v1, int v2, DotType type)
        {
            if (type == DotType.White) return Math.Abs(v1 - v2) == 1;
            var bigger = Math.Max(v1, v2);
            var smaller = Math.Min(v1, v2);
            return smaller > 0 && bigger == 2 * smaller;
        }

        private static void GenerateKillerCages(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 3 : size <= 8 ? 4 : 5;
            var target = ScaledCount(baseTarget, scale, 1, 10);
            var used = new bool[size, size];

            for (var attempt = 0; attempt < target * 20 && overlay.Cages.Count < target; attempt++)
            {
                var cage = TryBuildCage(board, rng, used, size);
                if (cage == null) continue;

                overlay.Cages.Add(cage);
                for (var c = 0; c < cage.Cells.Count; c++)
                    used[cage.Cells[c].Row, cage.Cells[c].Col] = true;
            }
        }

        private static KillerCage TryBuildCage(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var startRow = rng.Next(size);
            var startCol = rng.Next(size);
            if (used[startRow, startCol]) return null;

            var cage = new KillerCage();
            cage.Cells.Add(new CellCoord(startRow, startCol));
            var values = new HashSet<int> { board.Solution[startRow, startCol] };

            var targetSize = rng.Next(2, 5);

            for (var step = 1; step < targetSize; step++)
            {
                var candidates = new List<CellCoord>();

                for (var ci = 0; ci < cage.Cells.Count; ci++)
                {
                    var cell = cage.Cells[ci];
                    for (var d = 0; d < Dirs.Length; d++)
                    {
                        var nr = cell.Row + Dirs[d].Dr;
                        var nc = cell.Col + Dirs[d].Dc;
                        if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                        if (used[nr, nc] || IsInCage(cage, nr, nc)) continue;

                        var val = board.Solution[nr, nc];
                        if (values.Contains(val)) continue;

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

        private static void GenerateArrows(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
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
                used[arrow.Circle.Row, arrow.Circle.Col] = true;
                for (var c = 0; c < arrow.Path.Count; c++)
                    used[arrow.Path[c].Row, arrow.Path[c].Col] = true;
            }
        }

        private static ArrowConstraint TryBuildArrow(SudokuBoard board, Random rng, bool[,] used, int size)
        {
            var circleRow = rng.Next(size);
            var circleCol = rng.Next(size);
            if (used[circleRow, circleCol]) return null;

            var circleVal = board.Solution[circleRow, circleCol];
            if (circleVal < 3) return null;

            var arrow = new ArrowConstraint { Circle = new CellCoord(circleRow, circleCol) };
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
                arrow.Path.Add(pick);
                runningSum += board.Solution[pick.Row, pick.Col];
                lastRow = pick.Row;
                lastCol = pick.Col;
            }

            if (arrow.Path.Count < 2 || runningSum != circleVal) return null;

            return arrow;
        }

        private static void GenerateFog(SudokuBoard board, ModifierOverlayData overlay, Random rng)
        {
            var size = board.Size;

            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (!board.GivenMask[r, c])
                        overlay.SetFog(r, c);
                }
            }

            var givenCells = new List<CellCoord>();
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (board.GivenMask[r, c])
                        givenCells.Add(new CellCoord(r, c));
                }
            }

            Shuffle(givenCells, rng);
            var revealCount = Math.Max(1, givenCells.Count / 3);
            for (var i = 0; i < revealCount; i++)
                RevealAdjacentFog(overlay, givenCells[i].Row, givenCells[i].Col, size);
        }

        public static void RevealAdjacentFog(ModifierOverlayData overlay, int row, int col, int size)
        {
            overlay.ClearFog(row, col);
            if (row > 0) overlay.ClearFog(row - 1, col);
            if (row < size - 1) overlay.ClearFog(row + 1, col);
            if (col > 0) overlay.ClearFog(row, col - 1);
            if (col < size - 1) overlay.ClearFog(row, col + 1);
        }

        private static bool IsInLine(ModifierLine line, int row, int col)
        {
            for (var i = 0; i < line.Cells.Count; i++)
            {
                if (line.Cells[i].Row == row && line.Cells[i].Col == col) return true;
            }
            return false;
        }

        private static bool IsInCage(KillerCage cage, int row, int col)
        {
            for (var i = 0; i < cage.Cells.Count; i++)
            {
                if (cage.Cells[i].Row == row && cage.Cells[i].Col == col) return true;
            }
            return false;
        }

        private static bool IsOnArrowPath(ArrowConstraint arrow, int row, int col)
        {
            for (var i = 0; i < arrow.Path.Count; i++)
            {
                if (arrow.Path[i].Row == row && arrow.Path[i].Col == col) return true;
            }
            return false;
        }

        private static void GeneratePalindromeLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
        {
            var size = board.Size;
            var baseTarget = size <= 6 ? 2 : 3;
            var target = ScaledCount(baseTarget, scale, 1, 6);
            var used = new bool[size, size];
            var count = 0;

            for (var attempt = 0; attempt < target * 40 && count < target; attempt++)
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
            if (used[startRow, startCol]) return null;

            var cells = new List<CellCoord> { new CellCoord(startRow, startCol) };
            var rawLen = rng.Next(3, 6);
            var targetLen = rawLen % 2 == 0 ? rawLen + 1 : rawLen; // prefer odd length

            for (var step = 1; step < targetLen; step++)
            {
                var last = cells[cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (used[nr, nc]) continue;
                    var alreadyIn = false;
                    for (var ci = 0; ci < cells.Count; ci++)
                        if (cells[ci].Row == nr && cells[ci].Col == nc) { alreadyIn = true; break; }
                    if (!alreadyIn) candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            if (cells.Count < 3) return null;

            // Verify palindrome in solution
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

        private static void GenerateThermoLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
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
            if (used[startRow, startCol]) return null;

            var line = new ModifierLine { Type = LineType.Thermo };
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
                    if (used[nr, nc] || IsInLine(line, nr, nc)) continue;
                    if (board.Solution[nr, nc] > lastVal)
                        candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                line.Cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            return line.Cells.Count >= 3 ? line : null;
        }

        private static void GenerateBetweenLines(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
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
            if (used[startRow, startCol]) return null;

            var cells = new List<CellCoord> { new CellCoord(startRow, startCol) };
            var targetLen = rng.Next(3, 5);

            for (var step = 1; step < targetLen; step++)
            {
                var last = cells[cells.Count - 1];
                var candidates = new List<CellCoord>();

                for (var d = 0; d < Dirs.Length; d++)
                {
                    var nr = last.Row + Dirs[d].Dr;
                    var nc = last.Col + Dirs[d].Dc;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                    if (used[nr, nc]) continue;
                    var alreadyIn = false;
                    for (var ci = 0; ci < cells.Count; ci++)
                        if (cells[ci].Row == nr && cells[ci].Col == nc) { alreadyIn = true; break; }
                    if (!alreadyIn) candidates.Add(new CellCoord(nr, nc));
                }

                if (candidates.Count == 0) break;
                cells.Add(candidates[rng.Next(candidates.Count)]);
            }

            if (cells.Count < 3) return null;

            // Verify: all interior cells have values strictly between the two endpoints
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

            var line = new ModifierLine { Type = LineType.BetweenLines };
            line.Cells.AddRange(cells);
            return line;
        }

        private static void GenerateEvenOddMarkers(SudokuBoard board, ModifierOverlayData overlay, Random rng, float scale = 1f)
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

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        /// <summary>Mark all cells generated by a modifier since the snapshot for spatial separation.</summary>
        private static void MarkOverlayCellsUsed(ModifierOverlayData overlay,
            int linesBefore, int dotsBefore, int cagesBefore, int arrowsBefore, int markersBefore)
        {
            for (var i = linesBefore; i < overlay.Lines.Count; i++)
                MarkCellsUsed(overlay.Lines[i].Cells);

            for (var i = dotsBefore; i < overlay.Dots.Count; i++)
            {
                _usedCells.Add(CellKey(overlay.Dots[i].CellA.Row, overlay.Dots[i].CellA.Col));
                _usedCells.Add(CellKey(overlay.Dots[i].CellB.Row, overlay.Dots[i].CellB.Col));
            }

            for (var i = cagesBefore; i < overlay.Cages.Count; i++)
                MarkCellsUsed(overlay.Cages[i].Cells);

            for (var i = arrowsBefore; i < overlay.Arrows.Count; i++)
            {
                _usedCells.Add(CellKey(overlay.Arrows[i].Circle.Row, overlay.Arrows[i].Circle.Col));
                MarkCellsUsed(overlay.Arrows[i].Path);
            }

            for (var i = markersBefore; i < overlay.CellMarkers.Count; i++)
                _usedCells.Add(CellKey(overlay.CellMarkers[i].Cell.Row, overlay.CellMarkers[i].Cell.Col));
        }
    }
}
