using System;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // The save folder is LocalLow/Kratossos/sudoku_rouge_like (live 1.0 build, Steam Auto-Cloud root); saves of the
    // pre-release builds (DefaultCompany/RunOfTheNine) are copied once.
    // Uses a temporary LocalLow tree only; the real save folders are never touched.
    [TestFixture]
    [Requirement("SAVE-MIGRATE-001")]
    public sealed class LegacySaveMigrationTests
    {
        private string _localLow;
        private string _saveRoot;

        [SetUp]
        public void SetUp()
        {
            _localLow = Path.Combine(Path.GetTempPath(), "sr_migration_" + Guid.NewGuid().ToString("N"));
            _saveRoot = Path.Combine(_localLow, "Kratossos", "sudoku_rouge_like");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_localLow)) Directory.Delete(_localLow, true);
        }

        private string Legacy(string relative, params (string name, string content, int minutesAgo)[] files)
        {
            var dir = Path.Combine(_localLow, relative);
            Directory.CreateDirectory(dir);
            foreach (var (name, content, minutesAgo) in files)
            {
                var path = Path.Combine(dir, name);
                File.WriteAllText(path, content);
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-minutesAgo));
            }
            return dir;
        }

        [Test]
        public void CopiesProfilesAndBackups_FromThePreReleaseFolder_AndPicksTheNewestSlot()
        {
            var old = Legacy(Path.Combine("DefaultCompany", "RunOfTheNine"),
                ("save_profile_0.json", "slot0", 30), ("save_profile_2.json", "slot2", 5),
                ("save_profile_2.json.bak", "slot2bak", 6), ("save_profile_2.json.tmp", "partial", 1));

            Assert.AreEqual(2, LegacySaveMigration.Run(_saveRoot, _localLow));

            Assert.AreEqual("slot0", File.ReadAllText(Path.Combine(_saveRoot, "save_profile_0.json")));
            Assert.AreEqual("slot2", File.ReadAllText(Path.Combine(_saveRoot, "save_profile_2.json")));
            Assert.AreEqual("slot2bak", File.ReadAllText(Path.Combine(_saveRoot, "save_profile_2.json.bak")));
            Assert.IsFalse(File.Exists(Path.Combine(_saveRoot, "save_profile_2.json.tmp")), "temp files are not copied");
            Assert.IsTrue(File.Exists(Path.Combine(old, "save_profile_2.json")), "originals stay");
        }

        [Test]
        public void DoesNothing_WhenTheCurrentFolderAlreadyHasAProfile_OrNoLegacyFolderExists()
        {
            Assert.AreEqual(-1, LegacySaveMigration.Run(_saveRoot, _localLow), "no legacy folder");

            Directory.CreateDirectory(_saveRoot);
            File.WriteAllText(Path.Combine(_saveRoot, "save_profile_0.json"), "current");
            Legacy(Path.Combine("DefaultCompany", "RunOfTheNine"), ("save_profile_0.json", "old", 5));

            Assert.AreEqual(-1, LegacySaveMigration.Run(_saveRoot, _localLow));
            Assert.AreEqual("current", File.ReadAllText(Path.Combine(_saveRoot, "save_profile_0.json")));
        }

        // SAVE-FOLDER-001: the live 1.0 build (Steam build 24153478, app.info "Kratossos\nsudoku_rouge_like") and the
        // Steam Auto-Cloud root use this folder. Renaming the company or product would hide every player's saves.
        [Test]
        [Requirement("SAVE-FOLDER-001")]
        public void PlayerSettings_KeepTheSaveFolderOfTheReleasedGame()
        {
            Assert.AreEqual("Kratossos", Application.companyName);
            Assert.AreEqual("sudoku_rouge_like", Application.productName);
        }
    }
}
