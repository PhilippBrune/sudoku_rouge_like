using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class SudokuDifficultyGrader
    {
        private static readonly Dictionary<SudokuTechnique, float> TechniqueWeights = new()
        {
            [SudokuTechnique.NakedSingle] = 1f,
            [SudokuTechnique.HiddenSingle] = 1.5f,
            [SudokuTechnique.NakedPair] = 3f,
            [SudokuTechnique.PointingPair] = 3.5f,
            [SudokuTechnique.BoxLineReduction] = 5f,
            [SudokuTechnique.NakedTriples] = 6f,
            [SudokuTechnique.XWing] = 8f,
            [SudokuTechnique.Swordfish] = 12f
        };

        public static float ComputeDifficultyScore(IEnumerable<PuzzleStepStat> steps, int dependencyDepth, float modifierComplexityWeight)
        {
            var score = 0f;
            foreach (var step in steps)
            {
                if (!TechniqueWeights.TryGetValue(step.Technique, out var weight))
                {
                    continue;
                }

                score += weight * step.Count;
            }

            return score + dependencyDepth + modifierComplexityWeight;
        }

        public static PuzzleDifficultyTier TierFromTechnique(SudokuTechnique technique)
        {
            return technique switch
            {
                SudokuTechnique.NakedSingle => PuzzleDifficultyTier.Tier1,
                SudokuTechnique.HiddenSingle => PuzzleDifficultyTier.Tier1,
                SudokuTechnique.NakedPair => PuzzleDifficultyTier.Tier2,
                SudokuTechnique.PointingPair => PuzzleDifficultyTier.Tier2,
                SudokuTechnique.BoxLineReduction => PuzzleDifficultyTier.Tier3,
                SudokuTechnique.NakedTriples => PuzzleDifficultyTier.Tier3,
                SudokuTechnique.XWing => PuzzleDifficultyTier.Tier4,
                SudokuTechnique.Swordfish => PuzzleDifficultyTier.Tier4,
                _ => PuzzleDifficultyTier.Tier1
            };
        }
    }
}
