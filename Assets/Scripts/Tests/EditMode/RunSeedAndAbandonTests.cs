using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-RUN-100] GAP-020: a seed entered on the start-run screen reproduces the run.
    // [REQ: REQ-RUN-101] GAP-020 / GAP-032: abandoning a run (in-run, or by starting a new run over a saved one)
    // records it as a defeat with its XP and clears the saved run.
    [TestFixture]
    [Requirement("REQ-RUN-100", "REQ-RUN-101")]
    public sealed class RunSeedAndAbandonTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [SetUp]
        [TearDown]
        public void CleanSlot()
        {
            SaveFileService.FlushSharedPendingWrites();
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }

        [TestCase("12345", true, 12345)]
        [TestCase(" 42 ", true, 42)]
        [TestCase("999999999", true, 999999999)]
        [TestCase("", false, 0)]
        [TestCase(null, false, 0)]
        [TestCase("-5", false, 0)]
        [TestCase("12a", false, 0)]
        [TestCase("1234567890", false, 0)]
        public void SeedText_ParsesDigitsOnly(string text, bool ok, int expected)
        {
            Assert.AreEqual(ok, MainMenuController.TryParseSeed(text, out var seed));
            Assert.AreEqual(expected, seed);
        }

        [Test]
        public void SameSeed_ReproducesTheFirstFloor()
        {
            string Describe(RunDirector run) => string.Join(";", run.CurrentFloorGraph.Select(node =>
            {
                var config = run.BuildNodeLevelConfig(node, node.Index);
                return $"{node.Type}:{config.BoardSize}x{config.Stars}:{config.RegionVariant}:{config.Seed}";
            }));

            Assert.AreEqual(Describe(Start(4242)), Describe(Start(4242)));
            Assert.AreNotEqual(Describe(Start(4242)), Describe(Start(4243)));
        }

        [Test]
        public void AbandoningTheSavedRun_RecordsItsXpAndClearsTheSave()
        {
            var run = Start(9501);
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            run.StartLevel(run.BuildNodeLevelConfig(node, node.Index));
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0)
                    run.PlaceNumber(r, c, board.Solution[r, c]);
            var xp = run.CompleteLevelAndGrantRewards().TotalXp;
            Assert.Greater(xp, 0);

            var save = new SaveFileService(TestSlot);
            new RunAutoSaveCoordinator(save).Save(run.State, run.ExportPuzzleSaveState());
            save.FlushPendingWrites();
            Assert.IsTrue(new RunResumeService(save).HasActiveRun());

            var profile = new ProfileService(save);
            var result = new RunResumeService(save).AbandonSavedRun(profile);
            SaveFileService.FlushSharedPendingWrites();

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Victory);
            Assert.AreEqual(xp, result.XpEarned, "the abandoned run keeps the XP of its completed tiles");
            var reloaded = new SaveFileService(TestSlot);
            Assert.IsFalse(new RunResumeService(reloaded).HasActiveRun(), "the saved run is gone");
            Assert.Greater(new ClassGardenProgressionService().GetTotalXp(reloaded.Load().MetaProgress, ClassId.NumberFreak), 0);
        }

        [Test]
        public void AbandonWithoutASavedRun_DoesNothing()
        {
            var save = new SaveFileService(TestSlot);
            Assert.IsNull(new RunResumeService(save).AbandonSavedRun(new ProfileService(save)));
        }

        private static RunDirector Start(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                ClassLevel = 1,
                HasSeed = true,
                Seed = seed
            }, seed);
            return run;
        }
    }
}
