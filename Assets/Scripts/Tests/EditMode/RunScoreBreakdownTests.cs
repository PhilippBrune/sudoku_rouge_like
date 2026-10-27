using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // GameDesignSpec §9c: the run score is the base XP plus shares of it for each puzzle's grade (SCORE-GRADE-001, owner pick
    // C after the 0.2.0 playtest), cursed clears and victory, plus a point per 5 gold kept (SCORE-GOLD-001). The rows are
    // added, not multiplied (GAP-053), and the rows shown on the end screen add up to the score.
    [TestFixture]
    [Requirement("UI-STYLE-016", "MAP-NODE-CURSED-004", "SCORE-GRADE-001", "SCORE-GRADE-004", "SCORE-GOLD-001")]
    public sealed class RunScoreBreakdownTests
    {
        private static List<PuzzleGrade> Grades(params int[] mistakes) =>
            mistakes.Select(RunScoreBreakdown.GradeFor).ToList();

        // [REQ: SCORE-GRADE-001] Perfect 0 mistakes, Clean 1, Rough 2–3, Messy 4 or more.
        [Test]
        public void GradeFor_FollowsTheMistakesOfThatPuzzle()
        {
            CollectionAssert.AreEqual(
                new[] { PuzzleGrade.Perfect, PuzzleGrade.Clean, PuzzleGrade.Rough, PuzzleGrade.Rough, PuzzleGrade.Messy, PuzzleGrade.Messy },
                Grades(0, 1, 2, 3, 4, 9));
        }

        // The proposal's example (GameDesignSpec.md §9c, C): base 400; six puzzles with 0, 1, 0, 3, 0, 2 mistakes.
        [Test]
        public void SpecExample_Base400_Gives444()
        {
            var score = RunScoreBreakdown.Calculate(400, Grades(0, 1, 0, 3, 0, 2), 0, false, 0);

            CollectionAssert.AreEqual(new[] { RunScoreLineKind.Perfect, RunScoreLineKind.Rough }, score.Lines.Select(l => l.Kind));
            CollectionAssert.AreEqual(new[] { 15, -4 }, score.Lines.Select(l => l.Percent));
            CollectionAssert.AreEqual(new[] { 60, -16 }, score.Lines.Select(l => l.Points));
            Assert.AreEqual(444, score.Total, "400 × (1 + 0.15 − 0.04)");

            var analytics = new PostRunAnalyticsService();
            analytics.RecordTileXp(new TileXpEntry { TotalXp = 400 });
            foreach (var mistakes in new[] { 0, 1, 0, 3, 0, 2 }) analytics.RecordPuzzleSolved(mistakes, 0, 0);
            Assert.AreEqual(444, analytics.Build().RunScore, "the stored score uses the same rule");
        }

        // GameDesignSpec §9c's breakdown example: base 1 200, a Perfect, Clean, Rough and Messy run with cursed clears,
        // victory and 212 gold kept.
        [Test]
        public void SpecBreakdown_Base1200()
        {
            var score = RunScoreBreakdown.Calculate(1200, Grades(0, 0, 0, 1, 2, 3, 5), 2, true, 212);

            CollectionAssert.AreEqual(new[]
            {
                RunScoreLineKind.Perfect, RunScoreLineKind.Rough, RunScoreLineKind.Messy, RunScoreLineKind.Cursed,
                RunScoreLineKind.Victory, RunScoreLineKind.Gold
            }, score.Lines.Select(l => l.Kind));
            CollectionAssert.AreEqual(new[] { 180, -48, -60, 240, 300, 42 }, score.Lines.Select(l => l.Points));
            Assert.AreEqual(1854, score.Total);
        }

        // [REQ: SCORE-GRADE-004] Rough and Messy cost at most half the base; every combination adds up.
        [Test]
        public void Rows_AlwaysAddUpToTheScore_AndThePenaltyStopsAtHalf()
        {
            var random = new System.Random(53);
            foreach (var baseXp in new[] { 0, 1, 7, 99, 388, 1201, 4567 })
            for (var run = 0; run < 12; run++)
            {
                var grades = Enumerable.Range(0, random.Next(0, 30)).Select(_ => (PuzzleGrade)random.Next(0, 4)).ToList();
                var score = RunScoreBreakdown.Calculate(baseXp, grades, random.Next(0, 3), random.Next(0, 2) == 1, random.Next(0, 900));
                Assert.AreEqual(score.Total, score.BaseXp + score.Lines.Sum(l => l.Points), $"base {baseXp}, run {run}");
                Assert.GreaterOrEqual(score.Lines.Where(l => l.Percent < 0).Sum(l => l.Percent), -50, "the penalty stops at half");
            }

            var capped = RunScoreBreakdown.Calculate(1000, Enumerable.Repeat(PuzzleGrade.Messy, 14).ToList(), 0, false, 0);
            Assert.AreEqual(-50, capped.Lines.Single().Percent, "14 messy puzzles cost 50 %, not 70 %");
            Assert.AreEqual(500, capped.Total);
            Assert.IsEmpty(RunScoreBreakdown.Calculate(1000, Grades(1, 1), 0, false, 4).Lines,
                "clean puzzles and less than 5 gold add no row");
        }

        // [REQ: SCORE-GOLD-001] A point per 5 gold kept, at most 15 % of the base.
        [Test]
        public void KeptGold_AddsAPointPerFiveGold_UpToFifteenPercent()
        {
            var some = RunScoreBreakdown.Calculate(388, null, 0, false, 212).Lines.Single();
            Assert.AreEqual(RunScoreLineKind.Gold, some.Kind);
            Assert.AreEqual(212, some.Count, "the row names the gold kept");
            Assert.AreEqual(42, some.Points, "212 / 5");

            var capped = RunScoreBreakdown.Calculate(100, null, 0, false, 500).Lines.Single();
            Assert.AreEqual(15, capped.Points, "at most 15 % of a base of 100");
            Assert.AreEqual(15, capped.Percent);
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
            CollectionAssert.AreEqual(new[] { PuzzleGrade.Perfect }, RunScoreBreakdown.For(result).Grades,
                "a solved puzzle is not the puzzle that ended the run");
        }

        [Test]
        public void AnalyticsWithoutBaseXp_FallBackToTheResult()
        {
            var result = new RunResult { Victory = true, PerfectPuzzleCount = 1, MistakesMade = 2,
                Analytics = new PostRunAnalytics { TotalMistakes = 2 } };
            result.TileXpEntries.Add(new TileXpEntry { TotalXp = 450 });

            var shown = RunScoreBreakdown.For(result);

            Assert.AreEqual(450, shown.BaseXp, "the tiles' XP, not the missing analytics field");
            Assert.AreEqual(450 + 23 + 113, shown.Total, "the perfect puzzle and the victory; without grades no penalty");
        }

        [Test]
        public void FinishedRun_EndScreenBreakdown_MatchesTheStoredScore()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 4242);
            run.State.CurrentGold = 100;
            var analytics = run.GetAnalytics();
            analytics.RecordTileXp(new TileXpEntry { TotalXp = 388 });
            for (var m = 0; m < 3; m++) analytics.RecordMistake();
            analytics.RecordPuzzleSolved(3, 10, 0);
            analytics.RecordCursedPuzzleCompleted();
            analytics.RecordRunComplete();

            var result = RunResultFactory.Build(run, true, 0, 0);
            var shown = RunScoreBreakdown.For(result);

            Assert.AreEqual(result.RunScore, shown.Total, "the end screen shows the score the run was given");
            Assert.AreEqual(388, shown.BaseXp);
            CollectionAssert.AreEqual(
                new[] { RunScoreLineKind.Rough, RunScoreLineKind.Cursed, RunScoreLineKind.Victory, RunScoreLineKind.Gold },
                shown.Lines.Select(l => l.Kind));
            Assert.AreEqual(20, shown.Lines.Last().Points, "100 gold kept: 20 points");
            var text = new EndScreenPresenter().BuildRunScoreBreakdown(result);
            StringAssert.Contains("+97 (25", text, "the text breakdown names points and share");
            StringAssert.Contains(shown.Total.ToString(), text);
            StringAssert.Contains(": R", text, "the grades in the order played");
        }
    }
}
