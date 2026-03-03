using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuLogicalAnalyzer
    {
        public static PuzzleAnalysis Analyze(SudokuBoard board, List<BossModifierId> activeModifiers, bool allowBruteForce)
        {
            var analysis = new PuzzleAnalysis();
            var work = (int[,])board.Cells.Clone();
            var size = board.Size;
            var progress = true;

            while (progress)
            {
                progress = false;

                if (ApplyNakedSingles(work, board.RegionMap, size, analysis))
                {
                    progress = true;
                    continue;
                }

                if (ApplyHiddenSingles(work, board.RegionMap, size, analysis))
                {
                    progress = true;
                    continue;
                }

                if (ApplyNakedPairs(work, board.RegionMap, size, analysis))
                {
                    progress = true;
                    continue;
                }

                if (ApplyPointingPairs(work, board.RegionMap, size, analysis))
                {
                    progress = true;
                    continue;
                }
            }

            analysis.IsLogicallySolvable = IsSolved(work, size);
            if (!analysis.IsLogicallySolvable && allowBruteForce)
            {
                analysis.IsLogicallySolvable = true;
            }

            analysis.HasUniqueSolution = SudokuBacktrackingSolver.CountSolutions(board, 2) == 1;
            analysis.DependencyDepth = EstimateDependencyDepth(analysis.StepCount, analysis.HighestTechnique);
            analysis.ModifierComplexityWeight = EstimateModifierWeight(activeModifiers);
            analysis.DifficultyScore = SudokuDifficultyGrader.ComputeDifficultyScore(analysis.TechniqueUsage, analysis.DependencyDepth, analysis.ModifierComplexityWeight);
            analysis.DifficultyTier = SudokuDifficultyGrader.TierFromTechnique(analysis.HighestTechnique);
            return analysis;
        }

        private static bool ApplyNakedSingles(int[,] cells, int[,] regionMap, int size, PuzzleAnalysis analysis)
        {
            var changed = false;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    if (cells[row, col] != 0)
                    {
                        continue;
                    }

                    var count = 0;
                    var candidate = 0;
                    for (var value = 1; value <= size; value++)
                    {
                        if (IsValid(cells, regionMap, size, row, col, value))
                        {
                            count++;
                            candidate = value;
                        }
                    }

                    if (count == 1)
                    {
                        cells[row, col] = candidate;
                        RecordStep(analysis, SudokuTechnique.NakedSingle);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool ApplyHiddenSingles(int[,] cells, int[,] regionMap, int size, PuzzleAnalysis analysis)
        {
            var changed = false;

            for (var row = 0; row < size; row++)
            {
                for (var value = 1; value <= size; value++)
                {
                    var foundCol = -1;
                    var count = 0;
                    for (var col = 0; col < size; col++)
                    {
                        if (cells[row, col] == 0 && IsValid(cells, regionMap, size, row, col, value))
                        {
                            foundCol = col;
                            count++;
                        }
                    }

                    if (count == 1)
                    {
                        cells[row, foundCol] = value;
                        RecordStep(analysis, SudokuTechnique.HiddenSingle);
                        changed = true;
                    }
                }
            }

            for (var col = 0; col < size; col++)
            {
                for (var value = 1; value <= size; value++)
                {
                    var foundRow = -1;
                    var count = 0;
                    for (var row = 0; row < size; row++)
                    {
                        if (cells[row, col] == 0 && IsValid(cells, regionMap, size, row, col, value))
                        {
                            foundRow = row;
                            count++;
                        }
                    }

                    if (count == 1)
                    {
                        cells[foundRow, col] = value;
                        RecordStep(analysis, SudokuTechnique.HiddenSingle);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool ApplyNakedPairs(int[,] cells, int[,] regionMap, int size, PuzzleAnalysis analysis)
        {
            var detected = false;

            for (var row = 0; row < size; row++)
            {
                var pairCols = new Dictionary<string, List<int>>();
                for (var col = 0; col < size; col++)
                {
                    if (cells[row, col] != 0)
                    {
                        continue;
                    }

                    var candidates = GetCandidates(cells, regionMap, size, row, col);
                    if (candidates.Count != 2)
                    {
                        continue;
                    }

                    var key = $"{candidates[0]}-{candidates[1]}";
                    if (!pairCols.ContainsKey(key))
                    {
                        pairCols[key] = new List<int>();
                    }

                    pairCols[key].Add(col);
                }

                foreach (var pair in pairCols)
                {
                    if (pair.Value.Count != 2)
                    {
                        continue;
                    }

                    var tokens = pair.Key.Split('-');
                    var a = int.Parse(tokens[0]);
                    var b = int.Parse(tokens[1]);

                    for (var col = 0; col < size; col++)
                    {
                        if (pair.Value.Contains(col) || cells[row, col] != 0)
                        {
                            continue;
                        }

                        var cand = GetCandidates(cells, regionMap, size, row, col);
                        if (cand.Count == 1)
                        {
                            continue;
                        }

                        if (cand.Contains(a) || cand.Contains(b))
                        {
                            detected = true;
                        }
                    }
                }
            }

            if (detected)
            {
                RecordStep(analysis, SudokuTechnique.NakedPair);
            }

            return false;
        }

        private static bool ApplyPointingPairs(int[,] cells, int[,] regionMap, int size, PuzzleAnalysis analysis)
        {
            var detected = false;

            var regions = new Dictionary<int, List<(int Row, int Col)>>();
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var region = regionMap[row, col];
                    if (!regions.ContainsKey(region))
                    {
                        regions[region] = new List<(int Row, int Col)>();
                    }

                    regions[region].Add((row, col));
                }
            }

            foreach (var region in regions)
            {
                for (var value = 1; value <= size; value++)
                {
                    var cellsForValue = new List<(int Row, int Col)>();
                    for (var i = 0; i < region.Value.Count; i++)
                    {
                        var cell = region.Value[i];
                        if (cells[cell.Row, cell.Col] == 0 && IsValid(cells, regionMap, size, cell.Row, cell.Col, value))
                        {
                            cellsForValue.Add(cell);
                        }
                    }

                    if (cellsForValue.Count < 2)
                    {
                        continue;
                    }

                    var sameRow = true;
                    var rowTarget = cellsForValue[0].Row;
                    for (var i = 1; i < cellsForValue.Count; i++)
                    {
                        if (cellsForValue[i].Row != rowTarget)
                        {
                            sameRow = false;
                            break;
                        }
                    }

                    if (sameRow)
                    {
                        for (var col = 0; col < size; col++)
                        {
                            if (regionMap[rowTarget, col] == region.Key || cells[rowTarget, col] != 0)
                            {
                                continue;
                            }

                            var candidates = GetCandidates(cells, regionMap, size, rowTarget, col);
                            if (candidates.Contains(value))
                            {
                                detected = true;
                            }
                        }
                    }
                }
            }

            if (detected)
            {
                RecordStep(analysis, SudokuTechnique.PointingPair);
            }

            return false;
        }

        private static List<int> GetCandidates(int[,] cells, int[,] regionMap, int size, int row, int col)
        {
            var candidates = new List<int>();
            if (cells[row, col] != 0)
            {
                return candidates;
            }

            for (var value = 1; value <= size; value++)
            {
                if (IsValid(cells, regionMap, size, row, col, value))
                {
                    candidates.Add(value);
                }
            }

            return candidates;
        }

        private static bool IsValid(int[,] cells, int[,] regionMap, int size, int row, int col, int value)
        {
            for (var i = 0; i < size; i++)
            {
                if (cells[row, i] == value || cells[i, col] == value)
                {
                    return false;
                }
            }

            var region = regionMap[row, col];
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (regionMap[r, c] == region && cells[r, c] == value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsSolved(int[,] cells, int size)
        {
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    if (cells[row, col] == 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static int EstimateDependencyDepth(int stepCount, SudokuTechnique highestTechnique)
        {
            var baseDepth = highestTechnique switch
            {
                SudokuTechnique.NakedSingle or SudokuTechnique.HiddenSingle => 1,
                SudokuTechnique.NakedPair or SudokuTechnique.PointingPair => 2,
                SudokuTechnique.BoxLineReduction or SudokuTechnique.NakedTriples => 3,
                _ => 4
            };

            return baseDepth + Math.Max(0, stepCount / 12);
        }

        private static float EstimateModifierWeight(List<BossModifierId> modifiers)
        {
            var weight = 0f;
            for (var i = 0; i < modifiers.Count; i++)
            {
                weight += modifiers[i] switch
                {
                    BossModifierId.ParityLines => 0.5f,
                    BossModifierId.DifferenceKropki => 0.7f,
                    BossModifierId.DutchWhispers => 1.0f,
                    BossModifierId.RenbanLines => 1.1f,
                    BossModifierId.RatioKropki => 1.2f,
                    BossModifierId.KillerCages => 1.5f,
                    BossModifierId.ArrowSums => 1.6f,
                    BossModifierId.FogOfWar => 1.8f,
                    BossModifierId.GermanWhispers => 2.0f,
                    _ => 0.5f
                };
            }

            return weight;
        }

        private static void RecordStep(PuzzleAnalysis analysis, SudokuTechnique technique)
        {
            analysis.StepCount++;
            if (technique > analysis.HighestTechnique)
            {
                analysis.HighestTechnique = technique;
            }

            for (var i = 0; i < analysis.TechniqueUsage.Count; i++)
            {
                if (analysis.TechniqueUsage[i].Technique == technique)
                {
                    analysis.TechniqueUsage[i].Count++;
                    return;
                }
            }

            analysis.TechniqueUsage.Add(new PuzzleStepStat
            {
                Technique = technique,
                Count = 1
            });
        }
    }
}
