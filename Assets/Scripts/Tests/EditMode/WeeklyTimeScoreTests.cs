using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: SCORE-TIME-001] A won run on the seed of the week scores its pace: +1 % of the base per minute under 60, at most
    // +30 %. Other runs, and lost ones, have no time row.
    [TestFixture]
    [Requirement("SCORE-TIME-001")]
    public sealed class WeeklyTimeScoreTests
    {
        private static readonly List<PuzzleGrade> Clean = new List<PuzzleGrade> { PuzzleGrade.Clean };

        [Test]
        public void AWonWeeklyRunOf40Minutes_Gets20Percent()
        {
            var score = RunScoreBreakdown.Calculate(1000, Clean, 0, true, 0, weeklySeconds: 40 * 60);

            var time = score.Lines[score.Lines.Count - 1];
            Assert.AreEqual(RunScoreLineKind.Time, time.Kind);
            Assert.AreEqual(20, time.Percent);
            Assert.AreEqual(200, time.Points);
            Assert.AreEqual(40, time.Count, "the row shows the minutes");
            Assert.AreEqual(1000 + 250 + 200, score.Total, "base + victory + time");
        }

        [Test]
        public void ThePaceBonus_IsCappedAndEndsAtTheHour()
        {
            Assert.AreEqual(RunScoreBreakdown.MaxTimePercent,
                Find(RunScoreBreakdown.Calculate(1000, Clean, 0, true, 0, weeklySeconds: 12 * 60)).Value.Percent);
            Assert.IsNull(Find(RunScoreBreakdown.Calculate(1000, Clean, 0, true, 0, weeklySeconds: 60 * 60)), "no row at the hour");
            Assert.AreEqual(1, Find(RunScoreBreakdown.Calculate(1000, Clean, 0, true, 0, weeklySeconds: 58 * 60 + 30)).Value.Percent,
                "a started minute counts as a whole one");
        }

        [Test]
        public void LostRunsAndOtherRuns_HaveNoTimeRow()
        {
            Assert.IsNull(Find(RunScoreBreakdown.Calculate(1000, Clean, 0, false, 0, weeklySeconds: 20 * 60)), "a quick loss earns nothing");
            Assert.IsNull(Find(RunScoreBreakdown.Calculate(1000, Clean, 0, true, 0)), "not the week's seed");
        }

        [Test]
        public void TheStoredScore_AndTheShownBreakdown_UseTheSameRule()
        {
            var analytics = new PostRunAnalyticsService();
            analytics.RecordTileXp(new TileXpEntry { TotalXp = 1000 });
            analytics.RecordPuzzleSolved(1, 0, 0);
            analytics.RecordRunComplete();
            var built = analytics.Build(0, -1, 0, weeklySeconds: 30 * 60);

            var shown = RunScoreBreakdown.For(new RunResult { Analytics = built, Victory = true });
            Assert.AreEqual(built.RunScore, shown.Total);
            Assert.AreEqual(30, Find(shown).Value.Percent);
        }

        private static RunScoreLine? Find(RunScoreBreakdown score)
        {
            foreach (var line in score.Lines)
                if (line.Kind == RunScoreLineKind.Time) return line;
            return null;
        }
    }
}
