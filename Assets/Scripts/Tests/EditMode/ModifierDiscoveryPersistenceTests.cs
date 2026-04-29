using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class ModifierDiscoveryPersistenceTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private string _savePath;
        private string _backupPath;
        private string _tempPath;
        private string _originalSaveJson;
        private string _originalBackupJson;
        private string _originalTempJson;

        [SetUp]
        public void SetUp()
        {
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
            RestoreFile(_savePath, _originalSaveJson);
            RestoreFile(_backupPath, _originalBackupJson);
            RestoreFile(_tempPath, _originalTempJson);
        }

        [Test]
        public void MergeIntoMeta_DeduplicatesAndKeepsExistingDiscoveries()
        {
            var meta = new MetaProgressionState
            {
                DiscoveredBossModifiers = new List<BossModifierId>
                {
                    BossModifierId.FogOfWar
                }
            };

            var added = ModifierDiscoveryService.MergeIntoMeta(meta, new[]
            {
                BossModifierId.FogOfWar,
                BossModifierId.Antiknight,
                BossModifierId.Antiknight
            });

            Assert.AreEqual(1, added);
            CollectionAssert.AreEquivalent(
                new[] { BossModifierId.FogOfWar, BossModifierId.Antiknight },
                meta.DiscoveredBossModifiers);
        }

        [Test]
        public void RecordRunAndGetNewUnlocks_PersistsLiveRunDiscoveryWithoutAutosave()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());

            var liveRunState = new RunState();
            liveRunState.SeenBossModifiers.Add(BossModifierId.FogOfWar);
            liveRunState.SeenBossModifiers.Add(BossModifierId.Antiknight);

            var profile = new ProfileService(save);
            profile.RecordRunAndGetNewUnlocks(new RunResult
            {
                PlayedClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                Victory = false
            }, liveRunState);

            var loaded = save.Load();
            CollectionAssert.AreEquivalent(
                new[] { BossModifierId.FogOfWar, BossModifierId.Antiknight },
                loaded.MetaProgress.DiscoveredBossModifiers);
        }

        [Test]
        public void RunAutoSave_MergesSeenModifiersIntoMetaProgress()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());

            var coordinator = new RunAutoSaveCoordinator(save);
            var runState = new RunState();
            runState.Mode = GameMode.GardenRun;
            runState.SeenBossModifiers.Add(BossModifierId.GermanWhispers);

            coordinator.Save(runState, null);

            var loaded = save.Load();
            CollectionAssert.AreEqual(
                new[] { BossModifierId.GermanWhispers },
                loaded.MetaProgress.DiscoveredBossModifiers);
        }

        [Test]
        public void Load_SanitizesMissingDiscoveredBossModifiersFromLegacyJson()
        {
            var legacyJson = "{\"SaveVersion\":\"1.0.0\",\"MetaProgress\":{\"SpiritTrialsUnlocked\":true}}";
            File.WriteAllText(_savePath, legacyJson);

            var loaded = new SaveFileService(TestSlot).Load();

            Assert.IsNotNull(loaded.MetaProgress.DiscoveredBossModifiers);
            Assert.AreEqual(0, loaded.MetaProgress.DiscoveredBossModifiers.Count);
        }

        [Test]
        public void SeedRunSeenModifiers_UnionsMetaDiscoveryWithExistingRunState()
        {
            var runState = new RunState();
            runState.SeenBossModifiers.Add(BossModifierId.FogOfWar);

            var added = ModifierDiscoveryService.SeedRunSeenModifiers(runState, new[]
            {
                BossModifierId.FogOfWar,
                BossModifierId.Antiknight
            });

            Assert.AreEqual(1, added);
            CollectionAssert.AreEquivalent(
                new[] { BossModifierId.FogOfWar, BossModifierId.Antiknight },
                runState.SeenBossModifiers);
            CollectionAssert.AreEquivalent(
                runState.SeenBossModifiers,
                runState.SeenBossModifierList);
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
