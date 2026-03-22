using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.UI
{
    public sealed class EndScreenPresenter
    {
        public string BuildRunOverSummary(RunResult result)
        {
            return $"Depth {result.GardenDepthReached} | Gold {result.GoldEarned} | XP {result.XpEarned} | Mistakes {result.MistakesMade}";
        }

        public string BuildVictorySummary(RunResult result)
        {
            return $"Victory! Time {result.SecondsPlayed}s | Boss Phase {result.BossPhaseReached}";
        }

        /// <summary>Builds a per-tile XP breakdown for the end-of-run screen.</summary>
        public string BuildXpBreakdown(RunResult result)
        {
            if (result.TileXpEntries == null || result.TileXpEntries.Count == 0)
                return $"XP Earned: {result.XpEarned}";

            var sb = new StringBuilder();
            sb.AppendLine("--- XP Breakdown ---");
            for (var i = 0; i < result.TileXpEntries.Count; i++)
            {
                var tile = result.TileXpEntries[i];
                var label = tile.IsBoss ? "Boss" : "Puzzle";
                var line = $"  {label} {tile.BoardSize}x{tile.BoardSize} {tile.Stars}\u2605: {tile.TotalXp} XP";
                if (tile.PerfectBonus > 0)
                    line += " (Perfect +25)";
                if (tile.ModifierBonus > 0)
                    line += $" (Mods +{tile.ModifierBonus})";
                sb.AppendLine(line);
            }
            sb.Append($"Total XP: {result.XpEarned}");
            return sb.ToString();
        }

        public string BuildAnalyticsSummary(RunResult result)
        {
            var analytics = result?.Analytics;
            if (analytics == null)
                return "No analytics available.";

            var hardest = $"Hardest: {analytics.HardestPuzzleStars}\u2605 {analytics.HardestPuzzleModifier} ({analytics.HardestPuzzleTier})";
            var mistakes = $"Mistakes: total {analytics.TotalMistakes}, peak puzzle {analytics.HighestSinglePuzzleMistakes}";
            var tip = analytics.ImprovementSuggestions.Count > 0 ? analytics.ImprovementSuggestions[0] : "No tip.";
            return $"{hardest} | {mistakes} | Tip: {tip}";
        }
    }
}
