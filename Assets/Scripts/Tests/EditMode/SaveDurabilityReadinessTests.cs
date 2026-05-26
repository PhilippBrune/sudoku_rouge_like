using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class SaveDurabilityReadinessTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private const string GameBootstrapPath = "Assets/Scripts/Bootstrap/GameBootstrap.cs";

        private string _savePath;
        private string _backupPath;
        private string _tempPath;
        private string _originalSaveJson;
        private string _originalBackupJson;
        private string _originalTempJson;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            SaveWriteQueue.Shared.ResetDiagnostics();

            _savePath = Path.Combine(Application.persistentDataPath, $"save_profile_{TestSlot}.json");
            _backupPath = _savePath + ".bak";
            _tempPath = _savePath + ".tmp";

            Directory.CreateDirectory(Application.persistentDataPath);

            _originalSaveJson = File.Exists(_savePath) ? File.ReadAllText(_savePath) : null;
            _originalBackupJson = File.Exists(_backupPath) ? File.ReadAllText(_backupPath) : null;
            _originalTempJson = File.Exists(_tempPath) ? File.ReadAllText(_tempPath) : null;

            DeleteIfExists(_savePath);
            DeleteIfExists(_backupPath);
            DeleteIfExists(_tempPath);
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            SaveWriteQueue.Shared.ResetDiagnostics();

            RestoreFile(_savePath, _originalSaveJson);
            RestoreFile(_backupPath, _originalBackupJson);
            RestoreFile(_tempPath, _originalTempJson);
        }

        [Test]
        public void SharedShutdownFlush_WritesLatestQueuedEnvelope()
        {
            var save = new SaveFileService(TestSlot);

            save.Save(BuildEnvelopeWithPrestige(1));
            save.Save(BuildEnvelopeWithPrestige(2));

            SaveFileService.FlushSharedPendingWrites();

            Assert.AreEqual(2, SaveWriteQueue.Shared.EnqueuedWrites);
            Assert.AreEqual(2, SaveWriteQueue.Shared.CompletedWrites);
            Assert.AreEqual(0, SaveWriteQueue.Shared.FailedWrites);
            Assert.IsFalse(SaveWriteQueue.Shared.HasPendingWrites);
            Assert.IsTrue(File.Exists(_savePath));

            var loaded = save.Load();
            Assert.AreEqual(2, loaded.MetaProgress.PrestigeCount);
        }

        [Test]
        public void DeleteSaveFile_FlushesPendingWriteBeforeDelete()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);

            save.Save(BuildEnvelopeWithPrestige(3));
            save.DeleteSaveFile();
            queue.Flush();

            Assert.AreEqual(1, queue.EnqueuedWrites);
            Assert.AreEqual(1, queue.CompletedWrites);
            Assert.AreEqual(0, queue.FailedWrites);
            Assert.IsFalse(queue.HasPendingWrites);
            Assert.IsFalse(File.Exists(_savePath));
            Assert.IsFalse(File.Exists(_tempPath));
        }

        [Test]
        public void ClearActiveRun_FlushesPendingAutosaveAndPreventsResume()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);
            var coordinator = new RunAutoSaveCoordinator(save);

            coordinator.Save(BuildRunState(7, 2), null);
            coordinator.ClearActiveRun();

            var loaded = save.Load();
            var canResume = new RunResumeService(save).TryResumeFromSave(out var runState, out var puzzleState);

            Assert.IsNull(loaded.ActiveRunState);
            Assert.IsNull(loaded.ActivePuzzle);
            Assert.IsFalse(canResume);
            Assert.IsNull(runState);
            Assert.IsNull(puzzleState);
            Assert.AreEqual(2, queue.EnqueuedWrites);
            Assert.AreEqual(2, queue.CompletedWrites);
            Assert.AreEqual(0, queue.FailedWrites);
        }

        [Test]
        public void ResumeService_FlushesPendingAutosaveBeforeReading()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);
            var coordinator = new RunAutoSaveCoordinator(save);

            coordinator.Save(BuildRunState(11, 4), null);

            var resumed = new RunResumeService(save).TryResumeFromSave(out var runState, out var puzzleState);

            Assert.IsTrue(resumed);
            Assert.IsNotNull(runState);
            Assert.IsNull(puzzleState);
            Assert.AreEqual(11, runState.CurrentHP);
            Assert.AreEqual(4, runState.CurrentNodeIndex);
            Assert.AreEqual(1, queue.EnqueuedWrites);
            Assert.AreEqual(1, queue.CompletedWrites);
            Assert.AreEqual(0, queue.FailedWrites);
            Assert.IsFalse(queue.HasPendingWrites);
        }

        [Test]
        public void ResumeService_IgnoresJsonUtilityDefaultActiveRunPlaceholder()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);

            save.Save(new SaveFileEnvelope
            {
                ActiveRunState = new RunState(),
                ActivePuzzle = new PuzzleSaveState()
            });
            queue.Flush();

            var loaded = save.Load();
            var resumed = new RunResumeService(save).TryResumeFromSave(out var runState, out var puzzleState);

            Assert.IsFalse(save.HasActiveRun());
            Assert.IsFalse(resumed);
            Assert.IsNull(loaded.ActiveRunState);
            Assert.IsNull(loaded.ActivePuzzle);
            Assert.IsNull(runState);
            Assert.IsNull(puzzleState);
        }

        [Test]
        public void ResumeService_DropsDefaultPuzzlePlaceholderForValidRun()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);

            save.Save(new SaveFileEnvelope
            {
                ActiveRunState = BuildRunState(8, 1),
                ActivePuzzle = new PuzzleSaveState()
            });
            queue.Flush();

            var resumed = new RunResumeService(save).TryResumeFromSave(out var runState, out var puzzleState);

            Assert.IsTrue(resumed);
            Assert.IsNotNull(runState);
            Assert.AreEqual(8, runState.CurrentHP);
            Assert.IsNull(puzzleState);
        }

        [Test]
        public void RunDirector_RejectsInvalidZeroSizePuzzleResumeState()
        {
            var run = new RunDirector();

            Assert.IsFalse(run.TryRestorePuzzleSaveState(new PuzzleSaveState()));
            Assert.IsNull(run.CurrentBoard);
            Assert.IsNull(run.CurrentLevelState);
        }

        [Test]
        public void Autosave_SkipsTutorialAndNonSeasonalProgressionDisabledRuns()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);
            var coordinator = new RunAutoSaveCoordinator(save);

            coordinator.Save(new RunState
            {
                Mode = GameMode.Tutorial,
                TutorialMode = true,
                DisableProgressionRewards = true,
                Seed = 100
            }, null);
            coordinator.Save(new RunState
            {
                Mode = GameMode.SpiritTrials,
                DisableProgressionRewards = true,
                IsSeasonalChallenge = false,
                Seed = 200
            }, null);

            queue.Flush();

            Assert.AreEqual(0, queue.EnqueuedWrites);
            Assert.AreEqual(0, queue.CompletedWrites);
            Assert.AreEqual(0, queue.FailedWrites);
            Assert.IsFalse(File.Exists(_savePath));
        }

        [Test]
        public void Autosave_StoresSeasonalRunSeparatelyFromGardenRun()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);
            var coordinator = new RunAutoSaveCoordinator(save);

            var gardenRun = BuildRunState(9, 1);
            var seasonalRun = BuildRunState(6, 0);
            seasonalRun.Mode = GameMode.SeasonalChallenge;
            seasonalRun.IsSeasonalChallenge = true;
            seasonalRun.DisableProgressionRewards = true;
            seasonalRun.Seed = 202605;

            coordinator.Save(gardenRun, null);
            coordinator.Save(seasonalRun, null);

            var loaded = save.Load();

            Assert.NotNull(loaded.ActiveRunState);
            Assert.NotNull(loaded.ActiveSeasonalRunState);
            Assert.AreEqual(GameMode.GardenRun, loaded.ActiveRunState.Mode);
            Assert.AreEqual(GameMode.SeasonalChallenge, loaded.ActiveSeasonalRunState.Mode);
            Assert.AreEqual(9, loaded.ActiveRunState.CurrentHP);
            Assert.AreEqual(6, loaded.ActiveSeasonalRunState.CurrentHP);
            Assert.IsNull(loaded.ActiveSeasonalPuzzle);
            Assert.AreEqual(2, queue.EnqueuedWrites);
            Assert.AreEqual(2, queue.CompletedWrites);
            Assert.AreEqual(0, queue.FailedWrites);
        }

        [Test]
        public void GameBootstrap_QuitHookFlushesSharedPendingWrites()
        {
            var source = File.ReadAllText(GameBootstrapPath);

            StringAssert.Contains("private void OnApplicationQuit()", source);
            StringAssert.Contains("SaveFileService.FlushSharedPendingWrites();", source);
        }

        [Test]
        public void GameBootstrap_FocusHookHandlesEarlyProfileServiceNull()
        {
            var source = File.ReadAllText(GameBootstrapPath);

            StringAssert.Contains("private void OnApplicationFocus(bool hasFocus)", source);
            StringAssert.Contains("var opts = _profileService?.LoadOptions();", source);
            StringAssert.Contains("var muteWhenUnfocused = opts?.Audio?.MuteWhenUnfocused ?? false;", source);
            StringAssert.Contains("AudioListener.pause = muteWhenUnfocused && !hasFocus;", source);
            Assert.That(
                source,
                Does.Not.Contain("_profileService.LoadOptions()"),
                "Focus changes can arrive before GameBootstrap services are initialized.");
        }

        private static SaveFileEnvelope BuildEnvelopeWithPrestige(int prestigeCount)
        {
            return new SaveFileEnvelope
            {
                MetaProgress = new MetaProgressionState
                {
                    PrestigeCount = prestigeCount
                }
            };
        }

        private static RunState BuildRunState(int hp, int nodeIndex)
        {
            return new RunState
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                Seed = 12345,
                RunNumber = 1,
                CurrentHP = hp,
                MaxHP = hp,
                CurrentPencil = 5,
                MaxPencil = 5,
                CurrentNodeIndex = nodeIndex
            };
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void RestoreFile(string path, string contents)
        {
            if (contents == null)
            {
                DeleteIfExists(path);
                return;
            }

            File.WriteAllText(path, contents);
        }
    }
}
