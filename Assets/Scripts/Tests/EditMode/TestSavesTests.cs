using System;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: SAVE-TEST-001] The Editor and development builds keep their profiles in their own folder and start empty.
    [TestFixture]
    [Requirement("SAVE-TEST-001")]
    public sealed class TestSavesTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "sr_testsaves_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [Test]
        public void InTheEditor_TheDefaultRootIsTheTestFolder_NotTheRealSaves()
        {
            var previous = SaveFileService.RootDirectoryOverride;
            SaveFileService.RootDirectoryOverride = null;
            try
            {
                Assert.IsTrue(TestSaves.Active);
                Assert.AreEqual(TestSaves.Folder, SaveFileService.SaveRoot);
                StringAssert.EndsWith(TestSaves.FolderName, SaveFileService.SaveRoot);
            }
            finally
            {
                SaveFileService.RootDirectoryOverride = previous;
            }
        }

        [Test]
        public void TestPrefs_AreApartFromThePlayersPrefs()
        {
            Assert.AreEqual("test.ActiveProfileSlot", TestSaves.PrefKey("ActiveProfileSlot"));
        }

        [Test]
        public void Clear_EmptiesTheFolderAndItsPrefs()
        {
            File.WriteAllText(Path.Combine(_folder, "save_profile_0.json"), "{}");
            File.WriteAllText(Path.Combine(_folder, "save_profile_0.json.bak"), "{}");
            PlayerPrefs.SetInt(TestSaves.PrefKey("ActiveProfileSlot"), 2);

            TestSaves.Clear(_folder);

            Assert.IsEmpty(Directory.GetFiles(_folder));
            Assert.IsFalse(PlayerPrefs.HasKey(TestSaves.PrefKey("ActiveProfileSlot")));
        }

        [Test]
        public void Clear_KeepsEverything_WhenTheKeepMarkerIsThere()
        {
            File.WriteAllText(Path.Combine(_folder, "save_profile_0.json"), "{}");
            File.WriteAllText(Path.Combine(_folder, TestSaves.KeepMarker), "");

            TestSaves.Clear(_folder);

            Assert.IsTrue(File.Exists(Path.Combine(_folder, "save_profile_0.json")));
        }
    }
}
