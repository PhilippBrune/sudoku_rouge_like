using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // GAP-053: the run score follows GameDesignSpec §9c — the bonuses and the mistake penalty are shares of the base XP and
    // are added, not multiplied one after another — and the rows shown on the end screen add up to the score.
    [TestFixture]
    [Requirement("UI-STYLE-016", "MAP-NODE-CURSED-004")]
    public sealed class RunScoreBreakdownTests
    {
        [Test]
        public void SpecExample_Base1200_Gives1728()
        {
            // GameDesignSpec §9c: base 1 200, 3 perfect, 2 cursed, victory, 8 mistakes.
            var score = RunScoreBreakdown.Calculate(1200, 3, 2, 8, true);

            CollectionAssert.AreEqual(new[] { 180, 240, 300, -192 }, score.Lines.Select(l => l.Points));
            CollectionAssert.AreEqual(new[] { 15, 20, 25, -16 }, score.Lines.Select(l => l.Percent));
            Assert.AreEqual(1728, score.Total, "1 200 × (1 + 0.15 + 0.20 + 0.25 − 0.16)");
            Assert.AreEqual(1728, PostRunAnalyticsService.CalculateRunScore(
                new System.Collections.Generic.List<TileXpEntry> { new TileXpEntry { TotalXp = 1200 } }, 3, 2, 8, true),
                "the stored score uses the same rule");
        }

        [Test]
        public void Rows_AlwaysAddUpToTheScore_AndThePenaltyStopsAtHalf()
        {
            foreach (var baseXp in new[] { 0, 1, 7, 99, 388, 1201, 4567 })
            for (var perfect = 0; perfect <= 6; perfect += 3)
            for (var cursed = 0; cursed <= 2; cursed++)
            for (var mistakes = 0; mistakes <= 40; mistakes += 13)
            foreach (var victory in new[] { false, true })
            {
                var score = RunScoreBreakdown.Calculate(baseXp, perfect, cursed, mistakes, victory);
                Assert.AreEqual(score.Total, score.BaseXp + score.Lines.Sum(l => l.Points),
                    $"base {baseXp}, perfect {perfect}, cursed {cursed}, mistakes {mistakes}, victory {victory}");
            }

            var capped = RunScoreBreakdown.Calculate(1000, 0, 0, 40, false);
            Assert.AreEqual(-50, capped.Lines.Single().Percent, "40 mistakes cost 50 %, not 80 %");
            Assert.AreEqual(500, capped.Total);
            Assert.IsEmpty(RunScoreBreakdown.Calculate(1000, 0, 0, 0, false).Lines, "no row for what did not happen");
        }

        // GAP-054: a cleared cursed puzzle reached neither the score nor the result; nothing recorded it.
        [Test]
        [Requirement("MAP-NODE-CURSED-004")]
        public void ClearedCursedPuzzle_CountsForTheRunScore()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 8803);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            run.StartLevel(new LevelConfig { BoardSize = 5, Stars = 1, Seed = 3, IsCursed = true, CursedGoldMult = 1.5f,
                CursedXpMult = 1.5f, MissingPercent = StarDensityService.MissingPercentForStars(1) });
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0) run.PlaceNumber(r, c, b.Solution[r, c]);
            Assert.IsTrue(run.IsLevelComplete);

            run.CompleteLevelAndGrantRewards();
            var result = RunResultFactory.Build(run, false, 0, 0);

            Assert.AreEqual(1, result.Analytics.CursedPuzzlesCompleted, "the cleared cursed puzzle is counted");
            Assert.AreEqual(1, result.CursedPuzzlesAccepted);
            var cursed = RunScoreBreakdown.For(result).Lines.Single(l => l.Kind == RunScoreLineKind.Cursed);
            Assert.AreEqual(RunScoreBreakdown.CursedPercent, cursed.Percent, "+10 % of the base");
            Assert.AreEqual(result.RunScore, RunScoreBreakdown.For(result).Total);
        }

        [Test]
        public void AnalyticsWithoutBaseXp_FallBackToTheResult()
        {
            var result = new RunResult { Victory = true, PerfectPuzzleCount = 1, MistakesMade = 2,
                Analytics = new PostRunAnalytics { TotalMistakes = 2 } };
            result.TileXpEntries.Add(new TileXpEntry { TotalXp = 450 });

            var shown = RunScoreBreakdown.For(result);

            Assert.AreEqual(450, shown.BaseXp, "the tiles' XP, not the missing analytics field");
            Assert.AreEqual(450 + 23 + 113 - 18, shown.Total);
        }

        [Test]
        public void FinishedRun_EndScreenBreakdown_MatchesTheStoredScore()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 4242);
            var analytics = run.GetAnalytics();
            analytics.RecordTileXp(new TileXpEntry { TotalXp = 388 });
            for (var m = 0; m < 3; m++) analytics.RecordMistake();
            analytics.RecordPuzzleSolved(3, 10);
            analytics.RecordPerfectPuzzle();
            analytics.RecordCursedPuzzleCompleted();
            analytics.RecordRunComplete();

            var result = RunResultFactory.Build(run, true, 0, 0);
            var shown = RunScoreBreakdown.For(result);

            Assert.AreEqual(result.RunScore, shown.Total, "the end screen shows the score the run was given");
            Assert.AreEqual(388, shown.BaseXp);
            CollectionAssert.AreEqual(
                new[] { RunScoreLineKind.Perfect, RunScoreLineKind.Cursed, RunScoreLineKind.Victory, RunScoreLineKind.Mistakes },
                shown.Lines.Select(l => l.Kind));
            var text = new EndScreenPresenter().BuildRunScoreBreakdown(result);
            StringAssert.Contains("+97 (25", text, "the text breakdown names points and share");
            StringAssert.Contains(shown.Total.ToString(), text);
        }
    }
}
