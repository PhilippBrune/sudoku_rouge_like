using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public sealed class XpService
    {
        public static TileXpEntry CalculateTile(int boardSize, int stars, int activeModCount,
            bool isBoss, bool perfectSolve)
        {
            var baseXp = XpTable.BaseXpForBoardSize(boardSize);
            var starMult = XpTable.StarMultiplier(stars);
            var tileXp = (int)Math.Floor(baseXp * starMult);

            var bossMult = isBoss ? XpTable.BossMultiplier : 1.0f;
            tileXp = (int)Math.Floor(tileXp * bossMult);

            var modBonus = isBoss ? activeModCount * XpTable.ModifierBonusPerMod : 0;
            var perfectBonus = perfectSolve ? XpTable.PerfectBonus : 0;

            var total = tileXp + modBonus + perfectBonus;

            return new TileXpEntry
            {
                BaseXp = baseXp,
                StarMultiplier = starMult,
                BossMultiplier = bossMult,
                ModifierBonus = modBonus,
                PerfectBonus = perfectBonus,
                TotalXp = Math.Max(1, total)
            };
        }

        public static int SumRunXp(List<TileXpEntry> tiles)
        {
            var total = 0;
            for (var i = 0; i < tiles.Count; i++)
                total += tiles[i].TotalXp;
            return Math.Max(1, total);
        }
    }
}
