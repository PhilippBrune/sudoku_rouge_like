using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public sealed class EndScreenPresenter
    {
        public string BuildRunOverSummary(RunResult result)
        {
            return $"Depth {result.GardenDepthReached} | Heat {result.FinalHeatScore:0.00} | Gold {result.GoldEarned} | XP {result.XpEarned} | Essence {result.EssenceEarned} | Mistakes {result.MistakesMade}";
        }

        public string BuildVictorySummary(RunResult result)
        {
            return $"Victory! Peak Heat {result.PeakHeatScore:0.00} | Time {result.SecondsPlayed}s | Boss Phase {result.BossPhaseReached} | Essence {result.EssenceEarned}";
        }

        public string BuildAnalyticsSummary(RunResult result)
        {
            var analytics = result?.Analytics;
            if (analytics == null)
            {
                return "No analytics available.";
            }

            var hardest = $"Hardest: {analytics.HardestPuzzleStars}★ {analytics.HardestPuzzleModifier} ({analytics.HardestPuzzleTier})";
            var mistakes = $"Mistakes: total {analytics.TotalMistakes}, peak puzzle {analytics.HighestSinglePuzzleMistakes}";
            var tip = analytics.ImprovementSuggestions.Count > 0 ? analytics.ImprovementSuggestions[0] : "No tip.";
            return $"{hardest} | {mistakes} | Tip: {tip}";
        }
    }
}
