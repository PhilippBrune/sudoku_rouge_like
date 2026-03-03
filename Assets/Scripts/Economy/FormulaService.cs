using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public static class FormulaService
    {
        private static readonly Dictionary<DifficultyTier, int> BaseDifficultyGold = new()
        {
            [DifficultyTier.Diff1] = 20,
            [DifficultyTier.Diff2] = 30,
            [DifficultyTier.Diff3] = 45,
            [DifficultyTier.Diff4] = 65,
            [DifficultyTier.Diff5] = 100
        };

        private static readonly Dictionary<int, float> GridSizeFactor = new()
        {
            [5] = 1.0f,
            [6] = 1.2f,
            [7] = 1.5f,
            [8] = 1.9f,
            [9] = 2.4f
        };

        private static readonly Dictionary<int, float> StarFactor = new()
        {
            [1] = 1.0f,
            [2] = 1.15f,
            [3] = 1.35f,
            [4] = 1.6f,
            [5] = 1.9f
        };

        public static int CalculateGold(DifficultyTier difficulty, int stars)
        {
            var baseGold = BaseDifficultyGold[difficulty];
            var multiplier = 1f + stars * 0.2f;
            return (int)MathF.Round(baseGold * multiplier);
        }

        public static int CalculateXp(DifficultyTier difficulty, int stars)
        {
            return (int)difficulty * stars * 50;
        }

        public static int XpToNextLevel(int level)
        {
            return (int)MathF.Round(100f * MathF.Pow(level, 1.5f));
        }

        public static int PencilBuyCost(int purchasesThisRun)
        {
            return 20 + (20 * purchasesThisRun);
        }

        public static int RerollCost(int rerollsThisRun)
        {
            return 20 + (20 * rerollsThisRun);
        }

        public static float GetDifficultyScore(int boardSize, int stars, float bossModifierImpact, int runNumber)
        {
            var gridFactor = GridSizeFactor.TryGetValue(boardSize, out var g) ? g : 1.0f;
            var starFactor = StarFactor.TryGetValue(stars, out var s) ? s : MathF.Max(1f, stars * 0.2f + 0.8f);
            var modifierMultiplier = 1f + bossModifierImpact;
            var runDepthMultiplier = 1f + (runNumber * 0.05f);
            return gridFactor * starFactor * modifierMultiplier * runDepthMultiplier;
        }
    }
}
