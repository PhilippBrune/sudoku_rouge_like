using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // IncenseWalkMode.md: the walk as a run of its own in RunDirector.
    [TestFixture]
    public sealed class IncenseWalkRunTests
    {
        [Test]
        [Requirement("INCENSE-CLOCK-001", "INCENSE-CLOCK-003", "INCENSE-PUZZLE-001")]
        public void TheWalk_StartsOnTheClock_AMistakeCostsTimeNotHp_AndPencilIsFree()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.IncenseWalk, ClassLevel = 1 }, 4242);
            Assert.AreEqual(GameMode.IncenseWalk, run.State.Mode);
            Assert.IsNotNull(run.Incense);
            Assert.That(run.CurrentBoard.Size, Is.InRange(5, 6), "the first puzzles are small");
            Assert.AreEqual(480f, run.Incense.SecondsLeft);

            var (row, col) = EmptyCell(run, 0);
            var wrong = run.CurrentBoard.Solution[row, col] % run.CurrentBoard.Size + 1;
            var hp = run.State.CurrentHP;
            run.PlaceNumber(row, col, wrong);
            Assert.AreEqual(hp, run.State.CurrentHP, "no HP lost");
            Assert.AreEqual(470f, run.Incense.SecondsLeft, "10 s instead");

            var (r2, c2) = EmptyCell(run, 1);
            Assert.AreEqual(0, run.State.CurrentPencil);
            Assert.IsTrue(run.TryAddPencilMark(r2, c2, 1), "pencil marks cost nothing in the walk");
        }

        [Test]
        [Requirement("INCENSE-PUZZLE-001")]
        public void TheNextPuzzle_CountsOn_AndAnotherModeClearsTheClock()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.GardenMonk, Mode = GameMode.IncenseWalk, ClassLevel = 1 }, 77);
            run.AdvanceIncenseLevel();
            Assert.AreEqual(1, run.State.Depth);
            Assert.IsNotNull(run.CurrentBoard);
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 77);
            Assert.IsNull(run.Incense);
        }

        [Test]
        [Requirement("INCENSE-CLASS-001")]
        public void AFreePencilMark_StillCountsAsPencilUsed_ForTheReedDuelistsBonus()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.ReedDuelist, Mode = GameMode.IncenseWalk, ClassLevel = 1 }, 99);
            var (row, col) = EmptyCell(run, 0);
            Assert.IsTrue(run.TryAddPencilMark(row, col, 1));
            Assert.AreEqual(1, run.CurrentLevelState.PencilMarksUsed);
        }

        [Test]
        [Requirement("INCENSE-CLOCK-004")]
        public void TheWalk_IsNeverSavedForResume()
        {
            const int slot = Save.SaveFileService.MaxSlots - 1;
            var path = Save.SaveFileService.GetSlotPath(slot);
            try
            {
                var run = new RunDirector();
                run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.IncenseWalk, ClassLevel = 1 }, 5);
                var save = new Save.SaveFileService(slot);
                new Save.RunAutoSaveCoordinator(save).Save(run.State, null);
                Save.SaveFileService.FlushSharedPendingWrites();
                Assert.IsNull(save.HasSaveFile() ? save.Load().ActiveRunState : null);
            }
            finally
            {
                Save.SaveFileService.FlushSharedPendingWrites();
                foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                    if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
        }

        [Test]
        [Requirement("INCENSE-CLASS-001")]
        public void TheKoiGamblersFreeMistake_DoesNotCountAtAll()
        {
            var seed = Enumerable.Range(1, 200).First(s => new System.Random(s ^ 1).NextDouble() < IncenseWalkService.KoiFreeMistakeChance);
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.KoiGambler, Mode = GameMode.IncenseWalk, ClassLevel = 1 }, seed);
            var (row, col) = EmptyCell(run, 0);
            run.PlaceNumber(row, col, run.CurrentBoard.Solution[row, col] % run.CurrentBoard.Size + 1);
            Assert.IsTrue(run.Incense.LastMistakeFree);
            Assert.AreEqual(480f, run.Incense.SecondsLeft, "no time");
            Assert.AreEqual(0, run.CurrentLevelState.Mistakes, "no mistake on the puzzle");
            Assert.IsTrue(run.CurrentLevelState.PerfectSoFar, "the perfect bonus stays");
            Assert.AreEqual(0, run.GetAnalytics().TotalMistakes);
        }

        [Test]
        [Requirement("INCENSE-XP-001")]
        public void AnIncenseWalk_IsNoRunTowardEndlessZen_ButKeepsItsXp()
        {
            const int slot = Save.SaveFileService.MaxSlots - 1;
            var path = Save.SaveFileService.GetSlotPath(slot);
            try
            {
                Save.SaveFileService.FlushSharedPendingWrites();   // a slot left by an earlier test starts this one empty
                foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                    if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                var save = new Save.SaveFileService(slot);
                new Save.ProfileService(save).RecordRunAndGetNewUnlocks(new RunResult
                    { PlayedClassId = ClassId.NumberFreak, Mode = GameMode.IncenseWalk, XpEarned = 90, FloorReached = 1 });
                var envelope = save.Load();
                Assert.AreEqual(0, envelope.Statistics.TotalRunsStarted);
                Assert.AreEqual(90, new Meta.ClassGardenProgressionService().GetTotalXp(envelope.MetaProgress, ClassId.NumberFreak));
            }
            finally
            {
                Save.SaveFileService.FlushSharedPendingWrites();
                foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                    if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
        }

        private static (int, int) EmptyCell(RunDirector run, int skip)
        {
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.Cells[r, c] == 0 && skip-- <= 0) return (r, c);
            Assert.Fail("no empty cell");
            return (0, 0);
        }
    }
}
