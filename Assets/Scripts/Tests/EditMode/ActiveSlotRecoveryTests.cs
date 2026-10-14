using System;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // GAP-043: the active slot lives in PlayerPrefs, the profiles in files, and Steam Cloud restores only the files.
    // Without a stored slot the most recently written profile becomes active instead of a new, empty slot 0.
    // Uses its own temporary save root; the stored ActiveProfileSlot pref is put back afterwards.
    [TestFixture]
    [Requirement("SAVE-SLOT-004")]
    public sealed class ActiveSlotRecoveryTests
    {
        private const string PrefKey = "ActiveProfileSlot";

        private string _root;
        private string _previousRoot;
        private bool _hadPref;
        private int _previousPref;

        [SetUp]
        public void SetUp()
        {
            _hadPref = PlayerPrefs.HasKey(PrefKey);
            _previousPref = PlayerPrefs.GetInt(PrefKey, 0);
            _previousRoot = SaveFileService.RootDirectoryOverride;
            _root = Path.Combine(Path.GetTempPath(), "sr_activeslot_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            SaveFileService.RootDirectoryOverride = _root;
            PlayerPrefs.DeleteKey(PrefKey);
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.RootDirectoryOverride = _previousRoot;
            if (_hadPref) PlayerPrefs.SetInt(PrefKey, _previousPref);
            else PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private void Profile(int slot, int minutesAgo)
        {
            var path = SaveFileService.GetSlotPath(slot);
            File.WriteAllText(path, "{}");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-minutesAgo));
        }

        [Test]
        public void MissingPref_SelectsAndStoresTheMostRecentlyWrittenProfile()
        {
            Profile(0, 90);
            Profile(2, 5);

            Assert.AreEqual(2, SaveProfileService.ActiveSlot);
            Assert.IsTrue(PlayerPrefs.HasKey(PrefKey), "the recovered slot is stored");
            Assert.AreEqual(2, PlayerPrefs.GetInt(PrefKey, -1));
        }

        [Test]
        public void MissingPref_WithoutAnyProfile_StaysSlotZeroAndStoresNothing()
        {
            Assert.AreEqual(0, SaveProfileService.ActiveSlot);
            Assert.IsFalse(PlayerPrefs.HasKey(PrefKey), "a fresh install keeps profile select");
            Assert.AreEqual(-1, SaveProfileService.NewestExistingSlot());
        }

        [Test]
        public void StoredPref_WinsOverANewerProfile()
        {
            Profile(1, 60);
            Profile(2, 1);
            SaveProfileService.ActiveSlot = 1;

            Assert.AreEqual(1, SaveProfileService.ActiveSlot);
            Assert.AreEqual(1, SaveProfileService.EnsureActiveSlot());
        }
    }
}
