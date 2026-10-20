using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // GAP-059 (0.2.0 playtest): "only the mistakes of the last puzzle count" on the end screen. Every mistake of the run
    // counts toward the run score, including those in the puzzle that ended the run.
    [TestFixture]
    [Requirement("UI-STYLE-016")]
    public sealed class RunMistakeCountTests
    {
        [Test]
        public void EveryPuzzlesMistakes_CountAtTheEnd_AlsoThePuzzleThatEndedTheRun()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 8901);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            run.State.CurrentHP = run.State.MaxHP = 50;

            SolveWithMistakes(run, 2, 1);
            SolveWithMistakes(run, 1, 2);
            // The third puzzle is lost: three mistakes, never completed.
            StartLevel(run, 3);
            for (var m = 0; m < 3; m++) MakeMistake(run);

            var result = RunResultFactory.Build(run, false, 0, 0);

            Assert.AreEqual(6, result.MistakesMade, "2 + 1 solved, 3 in the lost puzzle");
            Assert.AreEqual(6, result.Analytics.TotalMistakes);
            var row = RunScoreBreakdown.For(result).Lines;
            Assert.IsTrue(System.Linq.Enumerable.Any(row, l => l.Kind == RunScoreLineKind.Mistakes && l.Count == 6));
        }

        // [REQ: REQ-BOSS-107] The boss-defeated screen shows the whole floor: every solved puzzle's mistakes, HP lost, items
        // and pencil marks add up, and a new floor starts at zero (0.2.0 playtest).
        [Test]
        [Requirement("REQ-BOSS-107")]
        public void FloorTally_AddsTheFloorsPuzzles_AndResetsOnTheNextFloor()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 8902);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            run.State.CurrentHP = run.State.MaxHP = 50;

            SolveWithMistakes(run, 2, 4);
            SolveWithMistakes(run, 1, 5);
            var boss = SudokuRoguelike.UI.EndScreenViewController.BuildBossClearedStats(run);
            var floor = SudokuRoguelike.UI.EndScreenViewController.BuildFloorStats(run);

            Assert.AreEqual(1, boss.Mistakes, "the boss column is the last puzzle");
            Assert.AreEqual(3, floor.Mistakes, "the floor column adds every solved puzzle");
            Assert.AreEqual(2, run.State.FloorTally.Puzzles);
            Assert.Greater(floor.HpLost, boss.HpLost, "HP lost adds up too");

            run.AdvanceToNextFloor();
            Assert.AreEqual(0, SudokuRoguelike.UI.EndScreenViewController.BuildFloorStats(run).Mistakes, "a new floor starts at zero");
        }

        // A mistake that Ginkgo Leaf takes back no longer counts toward the run.
        [Test]
        public void UndoneMistake_DoesNotCount()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 8903);
            run.State.HeldRelics.Clear();
            run.State.CurrentHP = run.State.MaxHP = 50;
            StartLevel(run, 6);
            MakeMistake(run);
            MakeMistake(run);
            Assert.AreEqual(2, run.GetAnalytics().TotalMistakes);

            run.ApplyItemEffect(new ItemInstance { Id = "ginkgo", Type = ItemType.GinkgoLeaf, Charges = 1 });

            Assert.AreEqual(1, run.CurrentLevelState.Mistakes, "the leaf takes the last mistake back");
            Assert.AreEqual(1, run.GetAnalytics().TotalMistakes, "and the run total follows");
        }

        private static void SolveWithMistakes(RunDirector run, int mistakes, int seed)
        {
            StartLevel(run, seed);
            for (var m = 0; m < mistakes; m++) MakeMistake(run);
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0) run.PlaceNumber(r, c, b.Solution[r, c]);
            Assert.IsTrue(run.IsLevelComplete);
            run.CompleteLevelAndGrantRewards();
        }

        private static void StartLevel(RunDirector run, int seed) =>
            run.StartLevel(new LevelConfig { BoardSize = 5, Stars = 1, Seed = seed,
                MissingPercent = StarDensityService.MissingPercentForStars(1) });

        // A wrong digit in the first empty cell.
        private static void MakeMistake(RunDirector run)
        {
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
            {
                if (b.Cells[r, c] != 0) continue;
                var wrong = b.Solution[r, c] % b.Size + 1;
                Assert.AreNotEqual(PlaceResult.Correct, run.PlaceNumber(r, c, wrong));
                return;
            }
        }
    }
}
