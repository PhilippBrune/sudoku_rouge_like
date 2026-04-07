using System;
using UnityEngine;

namespace SudokuRoguelike.Economy
{
    public static class XpTable
    {
        public static int BaseXpForBoardSize(int boardSize)
        {
            switch (boardSize)
            {
                case 5: return 30;
                case 6: return 45;
                case 7: return 60;
                case 8: return 90;
                case 9: return 120;
                default: return 30;
            }
        }

        public static float StarMultiplier(int stars)
        {
            switch (stars)
            {
                case 1: return 1.0f;
                case 2: return 1.2f;
                case 3:
                case 4: return 1.5f;
                case 5:
                case 6: return 2.0f;
                default: return 1.0f;
            }
        }

        public const float BossMultiplier = 1.5f;
        public const int ModifierBonusPerMod = 15;
        public const int PerfectBonus = 25;

        public static int XpToNextLevel(int currentLevel)
        {
            if (currentLevel < 1) return 50;
            if (currentLevel <= 15)
                return 50 + (currentLevel - 1) * 10;

            var prev = XpToNextLevel(15);
            for (var i = 16; i <= currentLevel; i++)
                prev += 35;
            return prev;
        }

        public static int CumulativeXpForLevel(int level)
        {
            var total = 0;
            for (var i = 1; i < level; i++)
                total += XpToNextLevel(i);
            return total;
        }

        public static int DeriveLevel(int totalXp)
        {
            var cumulative = 0;
            for (var level = 1; level <= 40; level++)
            {
                var needed = XpToNextLevel(level);
                if (cumulative + needed > totalXp)
                    return level;
                cumulative += needed;
            }
            return 40;
        }

        public static int CalculateTileXp(int boardSize, int stars, int activeModCount, bool isBoss, bool perfectSolve)
        {
            var baseXp = BaseXpForBoardSize(boardSize);
            var xp = Mathf.RoundToInt(baseXp * StarMultiplier(stars));

            if (isBoss)
            {
                xp = Mathf.RoundToInt(xp * BossMultiplier);
                xp += activeModCount * ModifierBonusPerMod;
            }

            if (perfectSolve)
                xp += PerfectBonus;

            return xp;
        }
    }
}
