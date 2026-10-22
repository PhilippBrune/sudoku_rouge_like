using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    /// <summary>
    /// [REQ: STAKES-001] What a puzzle pays before it is played, so a choice shows its stakes in numbers (one-more-run
    /// concept RM-115): the same steps as RunDirector.CompleteLevelAndGrantRewards for gold (the table, the cursed
    /// multiplier, the Bounty floor, the Harmony multiplier) and XP (XpTable, the cursed multiplier). One-off bonuses from
    /// relics, the Golden Koi or a perfect solve are left out.
    /// </summary>
    public static class RewardPreview
    {
        public const float BountyGoldMultiplier = 1.4f;

        public static int PuzzleGold(RunState state, int boardSize, int stars, float cursedMultiplier = 1f)
        {
            var gold = GoldTable.CalculatePuzzleGold(boardSize, stars);
            if (cursedMultiplier != 1f) gold = (int)(gold * cursedMultiplier);
            if (state != null && state.HasPositiveFloorEffect && state.ActivePositiveFloorEffect == PositiveFloorEffect.Bounty)
                gold = (int)(gold * BountyGoldMultiplier);
            if (state != null && state.GlobalGoldMultiplier > 0f)
                gold = (int)Math.Round(gold * state.GlobalGoldMultiplier);
            return gold;
        }

        public static int PuzzleXp(int boardSize, int stars, float cursedMultiplier = 1f)
        {
            var xp = XpService.CalculateTile(boardSize, stars, 0, false, false).TotalXp;
            return cursedMultiplier != 1f ? (int)(xp * cursedMultiplier) : xp;
        }
    }
}
