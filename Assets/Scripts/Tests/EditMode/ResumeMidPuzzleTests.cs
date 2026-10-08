using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: SAVE-RESUME-001] [REQ: REQ-SAVE-100] [REQ: PRESSURE-SAVE-002]
    // GAP-001: a run saved mid-puzzle must resume into a playable puzzle with the same rules,
    // overlay and level counters. Resume goes through RunResumeService.ApplyToRunDirector,
    // the same sequence GameBootstrap.LaunchResume uses.
    [TestFixture]
    [Requirement("SAVE-RESUME-001", "REQ-SAVE-100", "PRESSURE-SAVE-002")]
    public sealed class ResumeMidPuzzleTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            DeleteSlotFiles();
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            DeleteSlotFiles();
        }

        [TestCase(6161, 0)]
        [TestCase(6162, 1)]
        [TestCase(6163, 2)]
        public void ResumeMidPuzzle_RestoresRulesOverlayCountersAndLevelCompletes(int seed, int floorsToAdvance)
        {
            var run = StartGardenRun(seed);
            for (var i = 0; i < floorsToAdvance; i++) run.AdvanceToNextFloor();
            var node = FirstReachablePuzzle(run);
            Assert.IsNotNull(node, "no reachable puzzle node");
            run.TryAdvanceToNode(node.Index);
            run.StartLevel(run.BuildLevelConfig(false, node.Type == NodeType.ElitePuzzle, node.Index,
                node.Type == NodeType.PreBoss));
            Assert.IsNotNull(run.CurrentBoard, "puzzle generation failed for the test seed");

            Assert.AreEqual(2, PlaceCorrectDigits(run, 2));
            var conflict = FindConflictingPlacement(run);
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(conflict.row, conflict.col, conflict.value));
            // Clear the wrong digit again, as the UI's erase does, so the save holds a clean board.
            run.CurrentBoard.ClearValue(conflict.row, conflict.col);

            var resumed = SaveAndResume(run);

            Assert.IsNotNull(resumed.CurrentLevelConfig, "level config must be restored");
            Assert.IsNotNull(resumed.ConstraintEngine, "constraint engine must be restored");
            Assert.AreEqual(run.CurrentLevelConfig.BoardSize, resumed.CurrentLevelConfig.BoardSize);
            Assert.AreEqual(run.CurrentLevelConfig.Stars, resumed.CurrentLevelConfig.Stars);
            Assert.AreEqual(run.CurrentLevelConfig.Seed, resumed.CurrentLevelConfig.Seed);
            CollectionAssert.AreEqual(run.CurrentLevelConfig.ActiveModifiers, resumed.CurrentLevelConfig.ActiveModifiers);
            Assert.AreEqual(run.ConstraintEngine.RuleCount, resumed.ConstraintEngine.RuleCount, "rule count");
            Assert.AreEqual(OverlaySignature(run.CurrentOverlay), OverlaySignature(resumed.CurrentOverlay), "overlay");
            Assert.AreEqual(run.CurrentLevelState.Mistakes, resumed.CurrentLevelState.Mistakes, "mistakes");
            Assert.AreEqual(run.CurrentLevelState.CorrectPlacements, resumed.CurrentLevelState.CorrectPlacements);
            Assert.IsFalse(resumed.CurrentLevelState.PerfectSoFar, "a mistake before saving must keep the level imperfect");
            Assert.AreEqual(run.CurrentLevelState.ActiveThreats.Count, resumed.CurrentLevelState.ActiveThreats.Count,
                "pressure re-initialises from the level seed");

            var resumedConflict = FindConflictingPlacement(resumed);
            Assert.AreEqual(PlaceResult.Invalid,
                resumed.PlaceNumber(resumedConflict.row, resumedConflict.col, resumedConflict.value));
            resumed.CurrentBoard.ClearValue(resumedConflict.row, resumedConflict.col);

            FillWithSolution(resumed);
            Assert.IsTrue(resumed.IsLevelComplete);
            var goldBefore = resumed.State.CurrentGold;
            Assert.DoesNotThrow(() => resumed.CompleteLevelAndGrantRewards());
            Assert.Greater(resumed.State.CurrentGold, goldBefore, "completing a resumed puzzle grants gold");
        }

        [Test]
        public void ResumeBossPuzzle_KeepsChosenBossModifiersActive()
        {
            var run = StartGardenRun(7301);
            var choices = run.RollBossModifierChoices();
            run.GetBossModifierCounts(out _, out var picks);
            run.ChooseBossModifiers(choices.Take(picks).ToList());
            var boss = run.CurrentFloorGraph.First(n => n.Type == NodeType.Boss);
            run.TryAdvanceToNode(boss.Index, forced: true);
            run.StartLevel(run.BuildLevelConfig(true, false, boss.Index));
            Assert.IsNotNull(run.CurrentBoard, "boss generation failed for the test seed");
            PlaceCorrectDigits(run, 1);

            var resumed = SaveAndResume(run);

            Assert.IsTrue(resumed.CurrentLevelConfig.IsBoss);
            CollectionAssert.IsSubsetOf(run.State.ChosenBossModifiers, resumed.CurrentLevelConfig.ActiveModifiers);
            Assert.AreEqual(run.ConstraintEngine.RuleCount, resumed.ConstraintEngine.RuleCount);
            Assert.AreEqual(OverlaySignature(run.CurrentOverlay), OverlaySignature(resumed.CurrentOverlay));
            Assert.AreEqual(1, PlaceCorrectDigits(resumed, 1));
        }

        [Test]
        public void ResumeLegacySaveWithoutLevelDefinition_IsPlayable()
        {
            var run = StartGardenRun(6164);
            run.AdvanceToNextFloor();
            var node = FirstReachablePuzzle(run);
            run.TryAdvanceToNode(node.Index);
            run.StartLevel(run.BuildLevelConfig(false, node.Type == NodeType.ElitePuzzle, node.Index,
                node.Type == NodeType.PreBoss));
            Assert.IsNotNull(run.CurrentBoard, "puzzle generation failed for the test seed");
            PlaceCorrectDigits(run, 2);

            // Saves written before the GAP-001 fix carry none of the new fields.
            var resumed = SaveAndResume(run, puzzle =>
            {
                puzzle.HasLevelConfig = false;
                puzzle.HasOverlay = false;
                puzzle.HasLevelCounters = false;
            });

            Assert.IsNotNull(resumed.CurrentLevelConfig);
            Assert.IsNotNull(resumed.ConstraintEngine);
            Assert.AreEqual(run.CurrentLevelConfig.BoardSize, resumed.CurrentLevelConfig.BoardSize);
            var conflict = FindConflictingPlacement(resumed);
            Assert.AreEqual(PlaceResult.Invalid, resumed.PlaceNumber(conflict.row, conflict.col, conflict.value));
            resumed.CurrentBoard.ClearValue(conflict.row, conflict.col);
            FillWithSolution(resumed);
            Assert.DoesNotThrow(() => resumed.CompleteLevelAndGrantRewards());
        }

        // [REQ: SAVE-AUTO-001] GameBootstrap reuses one RunDirector for every launch. The second run of a
        // session must still autosave (it used to write nothing, so Save & Quit lost it), and binding it must
        // not write back a profile copy that was loaded before profile data changed between the runs.
        [Test]
        [Requirement("SAVE-AUTO-001")]
        public void SecondRunInSameSession_AutosavesAndKeepsProfileWritesFromBetweenRuns()
        {
            var host = new UnityEngine.GameObject("RunMapAutosaveTestHost");
            try
            {
                var map = host.AddComponent<UI.RunMapController>();
                var run = StartGardenRun(7101);
                map.BindRun(run, new SaveFileService(TestSlot));
                map.SaveNow();
                SaveFileService.FlushSharedPendingWrites();
                Assert.AreEqual(7101, new SaveFileService(TestSlot).Load().ActiveRunState?.Seed, "first run autosave");

                // Between the runs another service writes the profile (codex discovery, run-end commit).
                new ProfileService(new SaveFileService(TestSlot)).RecordItemDiscovery(ItemType.Solver);

                run.StartRun(new LaunchRequest
                {
                    ClassId = ClassId.NumberFreak,
                    Mode = GameMode.GardenRun,
                    AllowIrregularPuzzles = true,
                    ClassLevel = 1
                }, 7102);
                map.BindRun(run, new SaveFileService(TestSlot));
                var node = FirstReachablePuzzle(run);
                Assert.IsNotNull(node, "no reachable puzzle node");
                run.TryAdvanceToNode(node.Index);
                run.StartLevel(run.BuildLevelConfig(false, node.Type == NodeType.ElitePuzzle, node.Index,
                    node.Type == NodeType.PreBoss));
                Assert.IsNotNull(run.CurrentBoard, "puzzle generation failed for the test seed");
                Assert.AreEqual(1, PlaceCorrectDigits(run, 1));
                map.SaveNow();
                SaveFileService.FlushSharedPendingWrites();

                var envelope = new SaveFileService(TestSlot).Load();
                Assert.AreEqual(7102, envelope.ActiveRunState?.Seed, "second run must autosave");
                Assert.AreEqual(run.CurrentBoard.Size, envelope.ActivePuzzle?.BoardSize, "mid-puzzle state saved");
                Assert.IsTrue(envelope.MetaProgress.ItemCodex.Entries.Any(e => e.ItemId == "Solver" && e.Discovered),
                    "autosave must not overwrite profile data written before the run was bound");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        // ── helpers ──

        private static RunDirector StartGardenRun(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = true,
                ClassLevel = 1
            }, seed);
            return run;
        }

        private static RunDirector SaveAndResume(RunDirector run, System.Action<PuzzleSaveState> mutate = null)
        {
            var save = new SaveFileService(TestSlot);
            var puzzle = run.ExportPuzzleSaveState();
            Assert.IsNotNull(puzzle);
            mutate?.Invoke(puzzle);
            new RunAutoSaveCoordinator(save).Save(run.State, puzzle);
            save.FlushPendingWrites();

            Assert.IsTrue(new RunResumeService(save).TryResumeFromSave(out var runState, out var puzzleState));
            Assert.IsNotNull(puzzleState, "mid-puzzle save must be resumable");
            var resumed = new RunDirector();
            RunResumeService.ApplyToRunDirector(resumed, runState, puzzleState);
            return resumed;
        }

        private static RunNode FirstReachablePuzzle(RunDirector run)
        {
            foreach (var index in run.GetReachableNodes())
            {
                var type = run.CurrentFloorGraph[index].Type;
                if (type == NodeType.Puzzle || type == NodeType.ElitePuzzle || type == NodeType.PreBoss)
                    return run.CurrentFloorGraph[index];
            }
            return null;
        }

        private static bool IsFogged(RunDirector run, int row, int col)
        {
            var fog = run.CurrentOverlay?.FogCells;
            return fog != null && fog.Any(f => f.Row == row && f.Col == col);
        }

        private static int PlaceCorrectDigits(RunDirector run, int count)
        {
            var board = run.CurrentBoard;
            var placed = 0;
            for (var r = 0; r < board.Size && placed < count; r++)
            for (var c = 0; c < board.Size && placed < count; c++)
            {
                if (board.Cells[r, c] != 0 || IsFogged(run, r, c)) continue;
                if (run.PlaceNumber(r, c, board.Solution[r, c]) == PlaceResult.Correct) placed++;
            }
            return placed;
        }

        // An empty, unfogged cell plus a digit that already appears in its row: invalid under base rules.
        private static (int row, int col, int value) FindConflictingPlacement(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                if (board.Cells[r, c] != 0 || IsFogged(run, r, c)) continue;
                for (var cc = 0; cc < board.Size; cc++)
                {
                    var v = board.Cells[r, cc];
                    if (v != 0 && v != board.Solution[r, c]) return (r, c, v);
                }
            }
            Assert.Fail("no conflicting placement available");
            return default;
        }

        private static void FillWithSolution(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                if (board.GivenMask[r, c] || board.Cells[r, c] == board.Solution[r, c]) continue;
                board.PlaceValue(r, c, board.Solution[r, c]);
            }
        }

        private static string OverlaySignature(ModifierOverlayData o)
        {
            if (o == null) return "null";
            return $"L{o.Lines.Count} K{o.KropkiDots.Count} C{o.KillerCages.Count} A{o.Arrows.Count} " +
                   $"M{o.CellMarkers.Count} F{o.FogCells.Count} P{o.PairConstraints.Count}";
        }

        private static void DeleteSlotFiles()
        {
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }
    }
}
