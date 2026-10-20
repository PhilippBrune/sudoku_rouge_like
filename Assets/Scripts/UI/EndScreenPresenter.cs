using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    public sealed class EndScreenPresenter
    {
        public string BuildRunOverSummary(RunResult result)
        {
            return F(
                "EndPresenter.RunOverSummary",
                "Depth {0} | Gold {1} | XP {2} | Mistakes {3}",
                result.GardenDepthReached,
                result.GoldEarned,
                result.XpEarned,
                result.MistakesMade);
        }

        public string BuildVictorySummary(RunResult result)
        {
            return F(
                "EndPresenter.VictorySummary",
                "Victory! Time {0}s | Boss Phase {1}",
                result.SecondsPlayed,
                result.BossPhaseReached);
        }

        public string BuildXpBreakdown(RunResult result)
        {
            if (result.TileXpEntries == null || result.TileXpEntries.Count == 0)
                return F("EndPresenter.XpEarned", "XP Earned: {0}", result.XpEarned);

            var sb = new StringBuilder();
            sb.AppendLine(T("EndPresenter.XpBreakdownHeader", "--- XP Breakdown ---"));
            for (var i = 0; i < result.TileXpEntries.Count; i++)
            {
                var tile = result.TileXpEntries[i];
                var label = tile.IsBoss
                    ? T("EndPresenter.Tile.Boss", "Boss")
                    : T("EndPresenter.Tile.Puzzle", "Puzzle");
                var line = F(
                    "EndPresenter.TileLine",
                    "  {0} {1}×{1} {2}★: {3} XP",
                    label,
                    tile.BoardSize,
                    tile.Stars,
                    tile.TotalXp);
                if (tile.PerfectBonus > 0)
                    line += T("EndPresenter.PerfectBonus", " (Perfect +25)");
                if (tile.ModifierBonus > 0)
                    line += F("EndPresenter.ModifierBonus", " (Mods +{0})", tile.ModifierBonus);
                sb.AppendLine(line);
            }
            sb.Append(F("EndPresenter.TotalXp", "Total XP: {0}", result.XpEarned));
            return sb.ToString();
        }

        public string BuildAnalyticsSummary(RunResult result)
        {
            var analytics = result?.Analytics;
            if (analytics == null)
                return T("EndPresenter.NoAnalytics", "No analytics available.");

            var hardest = F(
                "EndPresenter.Analytics.Hardest",
                "Hardest: {0}* {1} ({2})",
                analytics.HardestPuzzleStars,
                analytics.HardestPuzzleModifier,
                analytics.HardestPuzzleTier);
            var mistakes = F(
                "EndPresenter.Analytics.Mistakes",
                "Mistakes: total {0}, peak puzzle {1}",
                analytics.TotalMistakes,
                analytics.HighestSinglePuzzleMistakes);
            var tip = analytics.ImprovementSuggestions != null && analytics.ImprovementSuggestions.Count > 0
                ? analytics.ImprovementSuggestions[0]
                : T("EndPresenter.Analytics.NoTip", "No tip.");
            return F("EndPresenter.Analytics.Summary", "{0} | {1} | Tip: {2}", hardest, mistakes, tip);
        }

        public string BuildRunScoreBreakdown(RunResult result)
        {
            var sb = new StringBuilder();
            // GameDesignSpec §9c: each row's points and its share of the base; the rows add up to the final score (GAP-053).
            var score = RunScoreBreakdown.For(result);
            sb.AppendLine(T("EndPresenter.RunScore.Header", "== Run Score =="));
            sb.AppendLine(F("EndPresenter.RunScore.BaseXp", "  Base XP total:         {0}", score.BaseXp));
            foreach (var line in score.Lines)
            {
                var points = System.Math.Abs(line.Points);
                var percent = System.Math.Abs(line.Percent);
                sb.AppendLine(line.Kind switch
                {
                    RunScoreLineKind.Perfect => F("EndPresenter.RunScore.Perfect", "  Perfect puzzles x{0}:   +{1} ({2}%)", line.Count, points, percent),
                    RunScoreLineKind.Rough => F("EndPresenter.RunScore.Rough", "  Rough puzzles x{0}:     -{1} (-{2}%)", line.Count, points, percent),
                    RunScoreLineKind.Messy => F("EndPresenter.RunScore.Messy", "  Messy puzzles x{0}:     -{1} (-{2}%)", line.Count, points, percent),
                    RunScoreLineKind.Cursed => F("EndPresenter.RunScore.Cursed", "  Cursed clears x{0}:     +{1} ({2}%)", line.Count, points, percent),
                    RunScoreLineKind.Victory => F("EndPresenter.RunScore.Victory", "  Victory bonus:         +{0} ({1}%)", points, percent),
                    _ => F("EndPresenter.RunScore.Gold", "  Gold kept ({0}):         +{1}", line.Count, points)
                });
            }
            // [REQ: SCORE-GRADE-003] The grades in the order played, floors apart.
            if (score.Grades.Count > 0)
                sb.AppendLine(F("EndPresenter.RunScore.Grades", "  Puzzles: {0}", RunGradeStamps.Line(result)));
            sb.Append(F("EndPresenter.RunScore.Final", "  Final Score: {0}", score.Total));
            return sb.ToString();
        }

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string F(string key, string fallback, params object[] args) =>
            LocalizationService.Format(key, fallback, args);
    }
}
