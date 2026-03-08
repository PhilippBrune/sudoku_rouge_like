using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class ModifierGeometryGenerator
    {
        private static readonly (int Dr, int Dc)[] Dirs = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        public static ModifierOverlayData Generate(SudokuBoard board, List<BossModifierId> modifiers, int seed)
        {
            var overlay = new ModifierOverlayData();
            var rng = new Random(seed);

            for (var i = 0; i < modifiers.Count; i++)
            {
                switch (modifiers[i])
                {
                    case BossModifierId.GermanWhispers:
                        GenerateWhisperLines(board, overlay, rng, LineType.GermanWhispers, 5);
                        break;
                    case BossModifierId.DutchWhispers:
                        GenerateWhisperLines(board, overlay, rng, LineType.DutchWhispers, 4);
                        break;
                    case BossModifierId.ParityLines:
                        GenerateParityLines(board, overlay, rng);
                        break;
                    case BossModifierId.RenbanLines:
                        GenerateRenbanLines(board, overlay, rng);
                        break;
                    case BossModifierId.DifferenceKropki:
                        GenerateKropkiDots(board, overlay, rng, DotType.White);
                        break;
                    case BossModifierId.RatioKropki:
                        GenerateKropkiDots(board, overlay, rng, DotType.Black);
                        break;
                    case BossModifierId.KillerCages:
                        GenerateKillerCages(board, overlay, rng);
                        break;
                    case BossModifierId.ArrowSums:
                        GenerateArrows(board, overlay, rng);
                        break;
                    case BossModifierId.FogOfWar:
                        GenerateFog(board, overlay, rng);
                        break;
                }
            }

            return overlay;
        }

        private static void GenerateWhisperLines(SudokuBoard board, ModifierOverlayData overlay,
            Random rng, LineType type, int minDiff)
        {
            var size = board.Size;
            var target = size <= 6 ? 2 : size <= 8 ? 3 : 4;
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

        private static void GenerateParityLines(SudokuBoard board, ModifierOverlayData overlay, Random rng)
        {
            var size = board.Size;
            var target = size <= 6 ? 2 : size <= 8 ? 3 : 4;
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

        private static void GenerateRenbanLines(SudokuBoard board, ModifierOverlayData overlay, Random rng)
        {
            var size = board.Size;
            var target = size <= 6 ? 2 : 3;
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
            Random rng, DotType targetType)
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

            var dotCount = Math.Min(pairs.Count, size <= 6 ? 6 : size <= 8 ? 8 : 12);
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

        private static void GenerateKillerCages(SudokuBoard board, ModifierOverlayData overlay, Random rng)
        {
            var size = board.Size;
            var target = size <= 6 ? 3 : size <= 8 ? 4 : 5;
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

        private static void GenerateArrows(SudokuBoard board, ModifierOverlayData overlay, Random rng)
        {
            var size = board.Size;
            var target = size <= 6 ? 2 : 3;
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

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
