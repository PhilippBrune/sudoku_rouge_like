using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Platform;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Tests
{
    // The owner's picks after the 0.4.2 playtest (2026-11-07): LS1, SA1, U1, the item ticks, FB4, K3.
    [TestFixture]
    public sealed class Playtest042PicksTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [SetUp]
        public void SetUp()
        {
            var path = SaveFileService.GetSlotPath(TestSlot);
            if (File.Exists(path)) File.Delete(path);
        }

        // ── CLASS-PASSIVE-003 ──

        [Test]
        [Requirement("CLASS-PASSIVE-003")]
        public void LanternSeer_TheRightDigitCatchesTheLight_ItPaysAndMovesOn_EveryThirdWritesADigit()
        {
            var run = StartPuzzle(94001, ClassId.LanternSeer, 1);
            Assert.AreEqual(1, run.LanternLightCells().Count, "one light below Level 15");
            for (var caught = 1; caught <= 3; caught++)
            {
                var light = run.LanternLightCells().Single();
                var before = (run.State.CurrentHP, run.State.CurrentGold, run.State.CurrentPencil);
                var emptyBefore = EmptyCells(run);
                run.State.CurrentHP = run.State.MaxHP - 1;   // room for the HP reward
                run.State.CurrentPencil = 0;
                Assert.AreEqual(PlaceResult.Correct, run.PlaceNumber(light.row, light.col, run.CurrentBoard.Solution[light.row, light.col]));
                Assert.AreEqual(caught, run.State.LanternCaught);
                Assert.IsNotNull(run.LastLanternReward, "the status line says what it gave");
                Assert.AreNotEqual(light, run.LanternLightCells().Single(), "the light moves on");
                Assert.AreEqual(caught == 3 ? emptyBefore - 2 : emptyBefore - 1, EmptyCells(run), $"catch {caught}");
                run.LastLanternReward = null;
            }
            Assert.AreEqual(2, ClassPassiveService.LanternLights(new RunState { ClassId = ClassId.LanternSeer, ClassLevel = 15 }));
            Assert.AreEqual(0, StartPuzzle(94001, ClassId.NumberFreak, 1).LanternLightCells().Count, "no lights for other classes");
        }

        [Test]
        [Requirement("CLASS-PASSIVE-003")]
        public void ShrineArchivist_FirstMarkFree_AndANarrowedCellSometimesWritesItself()
        {
            var run = StartPuzzle(94002, ClassId.ShrineArchivist, 1);
            var board = run.CurrentBoard;
            var (r0, c0) = FirstEmpty(run);
            var pencil = run.State.CurrentPencil;
            var wrong = board.Solution[r0, c0] % board.Size + 1;
            Assert.IsTrue(run.TryAddPencilMark(r0, c0, wrong));
            Assert.AreEqual(pencil, run.State.CurrentPencil, "the first mark of a cell is free");
            Assert.IsTrue(run.TryAddPencilMark(r0, c0, wrong % board.Size + 1));
            Assert.AreEqual(pencil - 1, run.State.CurrentPencil, "the second costs below Level 20");

            var found = 0;
            var rolled = 0;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                if (board.Cells[r, c] != 0 || board.IsGiven(r, c) || board.GetPencilMarks(r, c).Count > 0) continue;
                run.State.CurrentPencil = 99;
                run.TryAddPencilMark(r, c, board.Solution[r, c]);
                rolled++;
                if (board.Cells[r, c] == 0) continue;
                found++;
                Assert.AreEqual(board.Solution[r, c], board.Cells[r, c]);
            }
            Assert.Greater(found, 0, $"some of {rolled} cells write themselves");
            Assert.Less(found, rolled, "not every cell");
        }

        // ── ITEM-REWORK-001 ──

        [Test]
        [Requirement("ITEM-REWORK-001")]
        public void GoldenKintsugiJar_TurnsTheNextThreeMistakesIntoTheRightDigit()
        {
            var run = StartPuzzle(94003, ClassId.NumberFreak, 1);
            run.State.KintsugiCharges = RunDirector.KintsugiMends;
            run.State.ComboStreak = 4;
            var hp = run.State.CurrentHP;
            for (var i = 0; i < RunDirector.KintsugiMends; i++)
            {
                var (r, c) = FirstEmpty(run);
                var wrong = run.CurrentBoard.Solution[r, c] % run.CurrentBoard.Size + 1;
                Assert.AreEqual(PlaceResult.Correct, run.PlaceNumber(r, c, wrong));
                Assert.AreEqual(run.CurrentBoard.Solution[r, c], run.CurrentBoard.Cells[r, c]);
            }
            Assert.AreEqual(hp, run.State.CurrentHP, "no HP lost");
            Assert.AreEqual(0, run.CurrentLevelState.Mistakes);
            Assert.Greater(run.State.ComboStreak, 4, "the combo stays");
            var (r4, c4) = FirstEmpty(run);
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(r4, c4, run.CurrentBoard.Solution[r4, c4] % run.CurrentBoard.Size + 1),
                "the fourth is a mistake again");
        }

        [Test]
        [Requirement("ITEM-REWORK-001")]
        public void DoubleOrQuits_DoublesThePuzzlesGold_OrCostsOneHp()
        {
            var run = StartPuzzle(94004, ClassId.StoneGardener, 1);
            Assert.That(run.ApplyClassItem(new ItemInstance { Type = ItemType.DoubleOrQuits }), Is.Not.Empty);
            Assert.IsTrue(run.State.DoubleOrQuitsActive);
            var hp = run.State.CurrentHP;
            Solve(run);
            run.CompleteLevelAndGrantRewards();
            Assert.IsFalse(run.State.DoubleOrQuitsActive);
            if (run.LastDoubleOrQuits > 0) Assert.AreEqual(hp, run.State.CurrentHP, "won: gold, no HP lost");
            else Assert.AreEqual(-1, run.LastDoubleOrQuits);
        }

        [Test]
        [Requirement("ITEM-REWORK-001")]
        public void ClassItems_DoWhatTheirTextSays()
        {
            var run = StartPuzzle(94005, ClassId.GardenMonk, 15);
            run.ApplyClassItem(new ItemInstance { Type = ItemType.MonksBeads });
            Assert.AreEqual(5, run.State.MonksBeadsCountdown);
            run.ApplyClassItem(new ItemInstance { Type = ItemType.WornChisel });
            Assert.IsTrue(run.State.WornChiselActive);
            run.ApplyClassItem(new ItemInstance { Type = ItemType.DimLantern });
            Assert.IsTrue(run.State.DimLanternUsed);
            run.ApplyClassItem(new ItemInstance { Type = ItemType.ReedPledge });
            Assert.IsTrue(run.State.PledgeActive);
            run.ApplyClassItem(new ItemInstance { Type = ItemType.SurveyNotes });
            Assert.AreEqual(2, run.State.SurveyEase);
            Solve(run);
            run.CompleteLevelAndGrantRewards();
            Assert.AreEqual(1, run.State.SurveyEase, "the next puzzle is the easier one");
            var full = StarDensityService.MissingPercentForStars(3);
            Assert.AreEqual(StarDensityService.MissingPercentForStars(2), RunDirector.SurveyEasedMissingPercent(run.State, 3, full), 1e-4f);
        }

        [Test]
        [Requirement("ITEM-REWORK-001")]
        public void GardenRake_AndTheSandSifter_WorkOnTheNotes()
        {
            var run = StartPuzzle(94006, ClassId.NumberFreak, 1);
            var board = run.CurrentBoard;
            var placed = Enumerable.Range(0, board.Size * board.Size).Select(i => (r: i / board.Size, c: i % board.Size))
                .First(p => board.Cells[p.r, p.c] != 0);
            var (r, c) = Enumerable.Range(0, board.Size).Select(i => (placed.r, i)).First(p => board.Cells[p.Item1, p.Item2] == 0);
            board.AddPencilMark(r, c, board.Cells[placed.r, placed.c]);   // the row already has it
            board.AddPencilMark(r, c, board.Solution[r, c]);
            run.State.CurrentPencil = 0;
            Assert.AreEqual(1, run.RakeImpossibleNotes());
            CollectionAssert.AreEquivalent(new[] { board.Solution[r, c] }, board.GetPencilMarks(r, c));

            Assert.LessOrEqual(run.WriteTwinCandidates(4), 4);
            Assert.AreEqual(2, ItemService.GetFinderHighlightCount(ItemRarity.Normal));
            Assert.AreEqual(4, ItemService.GetFinderHighlightCount(ItemRarity.Rare));
        }

        [Test]
        [Requirement("ITEM-REWORK-001")]
        public void PatternScroll_IsOfferedOnlyWhereTheFloorHasRules()
        {
            var items = new ItemService(5) { RulesAhead = () => false };
            for (var i = 0; i < 400; i++)
            {
                items.ReseedForDecision(i);
                foreach (var item in items.RollSlots(2, 1))
                    if (item != null) Assert.AreNotEqual(ItemType.PatternScroll, item.Type, $"roll {i}");
            }
            var shop = new ShopService(5) { RulesAhead = () => false };
            for (var i = 0; i < 100; i++)
            {
                shop.ReseedForDecision(i);
                foreach (var offer in shop.BuildOffers(0, 1, 1f, 0f))
                    if (offer?.Item != null) Assert.AreNotEqual(ItemType.PatternScroll, offer.Item.Type, $"shop {i}");
            }
        }

        // ── GAP-123 (U1) ──

        [Test]
        [Requirement("ITEM-REWORK-001")]
        public void TheReedDuelistsCodex_CountsOnlyItemsEveryClassCanFind()
        {
            var meta = new MetaProgressionState();
            var required = CodexProgress.RequiredItems(meta).Total;
            var expected = System.Enum.GetValues(typeof(ItemType)).Cast<ItemType>()
                .Count(t => !CounterItems.IsCounterItem(t) && !ItemService.IsClassExclusive(t) && !ItemService.IsRetired(t));
            Assert.AreEqual(expected, required);
            Assert.IsTrue(ItemService.IsRetired(ItemType.BambooCompass));
            Assert.Less(CodexProgress.Items(meta).Total, System.Enum.GetValues(typeof(ItemType)).Length, "the Bamboo Compass is out of the Codex");
        }

        // ── SAVE-DAILY-001 ──

        [Test]
        [Requirement("SAVE-DAILY-001")]
        public void TheDailyWalk_SavesBesideTheGardenRun_AndAnOldSaveMovesOver()
        {
            var save = new SaveFileService(TestSlot);
            var auto = new RunAutoSaveCoordinator(save);
            var garden = new RunDirector();
            garden.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 4242);
            auto.Save(garden.State, null);
            var walk = new RunDirector();
            walk.StartRun(new LaunchRequest { ClassId = 0, Mode = GameMode.GardenRun, DailyWalk = true }, DailyGoalService.WalkSeed(System.DateTime.Today));
            walk.State.DailyWalk = true;
            walk.State.TotalFloors = DailyGoalService.WalkFloors;
            auto.Save(walk.State, null);

            var envelope = save.Load();
            Assert.AreEqual(4242, envelope.ActiveRunState.Seed, "the Garden Run waits");
            Assert.IsTrue(envelope.ActiveDailyRunState.DailyWalk);
            Assert.IsTrue(save.HasActiveDailyWalk(System.DateTime.Today));

            // A save from before 0.4.3: the walk in the run's slot.
            envelope.ActiveRunState = envelope.ActiveDailyRunState;
            envelope.ActiveDailyRunState = null;
            save.Save(envelope);
            var loaded = save.Load();
            Assert.IsNull(loaded.ActiveRunState);
            Assert.IsTrue(loaded.ActiveDailyRunState.DailyWalk, "moved to the walk's slot");
        }

        // ── FEEDBACK-001/002 ──

        [Test]
        [Requirement("FEEDBACK-001", "FEEDBACK-002")]
        public void FeedbackReport_NamesTheRun_HidesTheUser_AndZips()
        {
            var run = StartPuzzle(94007, ClassId.NumberFreak, 1);
            var text = FeedbackReport.Build("Bug", "the 6 vanished", "paul#1", "v0.4.3", "Windows", "German", true, run, true);
            StringAssert.Contains($"Run seed: {run.State.Seed}", text);
            StringAssert.Contains("Board code: " + FeedbackReport.BoardCode(run.CurrentLevelConfig), text);
            StringAssert.Contains("the 6 vanished", text);
            StringAssert.DoesNotContain("Run seed", FeedbackReport.Build("Idea", "x", "", "v", "d", "l", false, run, false), "unticked: no run");

            Assert.AreEqual("at %USERPROFILE%/AppData and %USERNAME%",
                FeedbackReport.Redact("at C:/Users/Philipp/AppData and Philipp", @"C:\Users\Philipp", "Philipp"));

            var folder = Path.Combine(Path.GetTempPath(), "sr_feedback_test");
            var zip = FeedbackSender.WriteZip(new FeedbackSender.Report { Text = text, Log = "log", Screenshot = new byte[] { 1, 2, 3 } }, folder);
            Assert.IsNotNull(zip);
            using (var archive = ZipFile.OpenRead(zip))
                CollectionAssert.AreEquivalent(new[] { "report.txt", "screenshot.jpg", "log.txt" }, archive.Entries.Select(e => e.FullName));
            Directory.Delete(folder, true);
        }

        // ── helpers ──

        private static RunDirector StartPuzzle(int seed, ClassId classId, int classLevel)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = classId, Mode = GameMode.GardenRun, FiveFloors = true, AllowIrregularPuzzles = false,
                ClassLevel = classLevel }, seed);
            run.State.HasPositiveFloorEffect = false;
            run.State.HeldRelics.Clear();
            run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 2, Seed = seed, MissingPercent = StarDensityService.MissingPercentForStars(2) });
            return run;
        }

        private static int EmptyCells(RunDirector run)
        {
            var n = 0;
            foreach (var v in run.CurrentBoard.Cells) if (v == 0) n++;
            return n;
        }

        private static (int, int) FirstEmpty(RunDirector run)
        {
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0 && !b.IsGiven(r, c)) return (r, c);
            return (-1, -1);
        }

        private static void Solve(RunDirector run)
        {
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0) run.PlaceNumber(r, c, b.Solution[r, c]);
        }
    }
}
