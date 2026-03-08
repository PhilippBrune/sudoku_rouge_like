using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuGenerationService
    {
        public static PuzzleGenerationResult Generate(PuzzleGenerationRequest request)
        {
            var random = new Random(request.Seed);
            var solved = BuildSolvedBoard(request.BoardSize, request.RegionVariant, random);
            var regionMap = BuildRegionMap(request.BoardSize, request.RegionVariant);
            var puzzle = (int[,])solved.Clone();
            var given = BuildGivenMask(request.BoardSize, true);

            var total = request.BoardSize * request.BoardSize;
            var targetMissing = Math.Clamp((int)Math.Round(total * StarDensityService.MissingPercentForStars(request.Stars)), 1, total - request.BoardSize);
            var order = BuildRemovalOrder(total, random);

            var removed = 0;
            for (var i = 0; i < order.Count; i++)
            {
                if (removed >= targetMissing)
                {
                    break;
                }

                var index = order[i];
                var row = index / request.BoardSize;
                var col = index % request.BoardSize;

                var old = puzzle[row, col];
                puzzle[row, col] = 0;
                given[row, col] = false;

                var candidateBoard = new SudokuBoard(request.BoardSize, solved, puzzle, given, regionMap);
                var analysis = SudokuLogicalAnalyzer.Analyze(candidateBoard, request.ActiveModifiers, request.AllowBruteForceOnly);
                var matchesTier = analysis.DifficultyTier <= request.TargetTier || request.AllowBruteForceOnly;

                if (!analysis.HasUniqueSolution || !analysis.IsLogicallySolvable || !matchesTier)
                {
                    puzzle[row, col] = old;
                    given[row, col] = true;
                    continue;
                }

                removed++;
            }

            var resultBoard = new SudokuBoard(request.BoardSize, solved, puzzle, given, regionMap);
            var finalAnalysis = SudokuLogicalAnalyzer.Analyze(resultBoard, request.ActiveModifiers, request.AllowBruteForceOnly);

            if (!finalAnalysis.HasUniqueSolution)
            {
                return new PuzzleGenerationResult { Success = false, FailureReason = "Failed uniqueness validation." };
            }

            if (!request.AllowBruteForceOnly && !finalAnalysis.IsLogicallySolvable)
            {
                return new PuzzleGenerationResult { Success = false, FailureReason = "Failed logical-solvability validation." };
            }

            return new PuzzleGenerationResult
            {
                Success = true,
                Board = resultBoard,
                Analysis = finalAnalysis
            };
        }

        private static int[,] BuildSolvedBoard(int size, int regionVariant, Random random)
        {
            var solved = SudokuGenerator.CreatePuzzle(size, 0f, random.Next(), regionVariant);
            return solved.Solution;
        }

        private static int[,] BuildRegionMap(int size, int regionVariant)
        {
            return SudokuGenerator.BuildRegionMap(size, regionVariant);
        }

        private static bool[,] BuildGivenMask(int size, bool value)
        {
            var mask = new bool[size, size];
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    mask[row, col] = value;
                }
            }

            return mask;
        }

        private static List<int> BuildRemovalOrder(int total, Random random)
        {
            var list = new List<int>(total);
            for (var i = 0; i < total; i++)
            {
                list.Add(i);
            }

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }
    }
}
