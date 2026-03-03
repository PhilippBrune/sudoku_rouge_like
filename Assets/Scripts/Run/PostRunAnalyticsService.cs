using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class PostRunAnalyticsService
    {
        public PostRunAnalytics Build(RunState runState, RunResult runResult, LevelConfig lastLevel, LevelState lastLevelState)
        {
            var analytics = new PostRunAnalytics();
            if (runState == null || runResult == null)
            {
                return analytics;
            }

            for (var i = 0; i < runState.HeatHistory.Count; i++)
            {
                analytics.HeatCurve.Add(runState.HeatHistory[i]);
            }

            analytics.TotalMistakes = runResult.MistakesMade;
            analytics.MistakesPerPuzzle.Add(runResult.MistakesMade);
            analytics.HighestSinglePuzzleMistakes = runResult.MistakesMade;
            analytics.HardestPuzzleStars = lastLevel?.Stars ?? 1;
            analytics.HardestPuzzleModifier = lastLevel != null && lastLevel.ActiveModifiers.Count > 0
                ? lastLevel.ActiveModifiers[0]
                : BossModifierId.ParityLines;
            analytics.HardestPuzzleTier = analytics.HardestPuzzleStars >= 5 ? PuzzleDifficultyTier.Tier4 : analytics.HardestPuzzleStars >= 4 ? PuzzleDifficultyTier.Tier3 : analytics.HardestPuzzleStars >= 3 ? PuzzleDifficultyTier.Tier2 : PuzzleDifficultyTier.Tier1;
            analytics.ModifierImpactRating = (lastLevel?.ActiveModifiers.Count ?? 0) * 0.35f;

            if (runState.CurrentHP <= 2)
            {
                analytics.ImprovementSuggestions.Add("Consider reducing early modifier stacking.");
            }

            if (runResult.PeakCombo > 0 && runResult.MistakesMade > 2)
            {
                analytics.ImprovementSuggestions.Add("Combo build collapsed due to low HP reserve.");
            }

            if (analytics.ImprovementSuggestions.Count == 0)
            {
                analytics.ImprovementSuggestions.Add("Route selection was stable; push one higher-risk branch next run.");
            }

            return analytics;
        }
    }
}
