using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class ConditionalButtonStateTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private readonly List<PreservedSaveFile> _preservedFiles = new List<PreservedSaveFile>();
        private int _originalActiveSlot;
        private LanguageOption _originalLanguage;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();

            _originalActiveSlot = SaveProfileService.ActiveSlot;
            _originalLanguage = LocalizationService.Current;

            Directory.CreateDirectory(Application.persistentDataPath);
            PreserveAndClearSlot(TestSlot);

            SaveProfileService.ActiveSlot = TestSlot;
            LocalizationService.SetLanguage(LanguageOption.English);
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();

            foreach (var preserved in _preservedFiles)
                preserved.Restore();
            _preservedFiles.Clear();

            SaveProfileService.ActiveSlot = _originalActiveSlot;
            LocalizationService.SetLanguage(_originalLanguage);
        }

        [Test]
        public void GeneratedMenu_ReflectsLockedDefaultsForResumeClassesAndGameModes()
        {
            var hadEventSystem = Object.FindAnyObjectByType<EventSystem>() != null;
            var host = new GameObject("ConditionalButtonDefaultsHost");

            try
            {
                var controller = BuildMenu(host);

                controller.ShowMainMenu();
                var resume = FindButton(host, "BtnResume");
                Assert.IsFalse(resume.interactable);
                Assert.AreEqual(LocalizationService.T("Resume"), FindButtonLabel(resume).text);

                controller.StartGame();
                AssertClassButtonState(host, ClassId.NumberFreak, true);
                AssertClassButtonState(host, ClassId.GardenMonk, false);

                controller.ShowGameModes();
                AssertButtonState(host, "BtnEndless", false, LocalizationService.T("Endless Zen (Locked)"));
                AssertButtonState(host, "BtnZenRecords", false, LocalizationService.T("Records"));
                AssertButtonState(host, "BtnTrials", false, LocalizationService.T("Spirit Trials (Locked)"));
            }
            finally
            {
                Object.DestroyImmediate(host);
                DestroyGeneratedEventSystemIfNeeded(hadEventSystem);
            }
        }

        [Test]
        public void GeneratedMenu_RefreshesConditionalButtonsAfterMetaUnlocksChange()
        {
            var hadEventSystem = Object.FindAnyObjectByType<EventSystem>() != null;
            var host = new GameObject("ConditionalButtonUnlockedHost");

            try
            {
                var controller = BuildMenu(host);

                SaveMetaProgress(new MetaProgressionState
                {
                    UnlockedClasses = new List<ClassId> { ClassId.NumberFreak, ClassId.GardenMonk },
                    EndlessZenUnlocked = true,
                    SpiritTrialsUnlocked = true
                });

                controller.StartGame();
                AssertClassButtonState(host, ClassId.NumberFreak, true);
                AssertClassButtonState(host, ClassId.GardenMonk, true);
                AssertClassButtonState(host, ClassId.StoneGardener, false);

                controller.ShowGameModes();
                AssertButtonState(host, "BtnEndless", true, LocalizationService.T("Start Endless Zen"));
                AssertButtonState(host, "BtnZenRecords", true, LocalizationService.T("Records"));
                AssertButtonState(host, "BtnTrials", true, LocalizationService.T("Spirit Trials"));
            }
            finally
            {
                Object.DestroyImmediate(host);
                DestroyGeneratedEventSystemIfNeeded(hadEventSystem);
            }
        }

        private static MainMenuController BuildMenu(GameObject host)
        {
            var builder = host.AddComponent<MainMenuBlueprintBuilder>();
            builder.Build();

            var controller = host.GetComponent<MainMenuController>();
            Assert.NotNull(controller);
            return controller;
        }

        private static void SaveMetaProgress(MetaProgressionState meta)
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope { MetaProgress = meta });
            SaveFileService.FlushSharedPendingWrites();
        }

        private static void AssertButtonState(GameObject host, string buttonName, bool expectedInteractable, string expectedLabel)
        {
            var button = FindButton(host, buttonName);
            Assert.AreEqual(expectedInteractable, button.interactable, buttonName);
            Assert.AreEqual(expectedLabel, FindButtonLabel(button).text, buttonName);
        }

        private static void AssertClassButtonState(GameObject host, ClassId classId, bool expectedUnlocked)
        {
            var button = FindButton(host, $"BtnClass{classId}");
            Assert.AreEqual(expectedUnlocked, button.interactable, classId.ToString());

            var lockOverlay = button.transform.Find("LockOverlay");
            Assert.NotNull(lockOverlay, classId.ToString());
            Assert.AreEqual(!expectedUnlocked, lockOverlay.gameObject.activeSelf, classId.ToString());
        }

        private static Button FindButton(GameObject host, string name)
        {
            foreach (var button in host.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                    return button;
            }

            Assert.Fail($"Button '{name}' was not found.");
            return null;
        }

        private static Text FindButtonLabel(Button button)
        {
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            Assert.NotNull(label, button.name);
            return label;
        }

        private static void DestroyGeneratedEventSystemIfNeeded(bool hadEventSystem)
        {
            if (hadEventSystem)
                return;

            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
                Object.DestroyImmediate(eventSystem.gameObject);
        }

        private void PreserveAndClearSlot(int slotIndex)
        {
            var savePath = Path.Combine(Application.persistentDataPath, $"save_profile_{slotIndex}.json");
            PreserveAndDelete(savePath);
            PreserveAndDelete(savePath + ".bak");
            PreserveAndDelete(savePath + ".tmp");
        }

        private void PreserveAndDelete(string path)
        {
            _preservedFiles.Add(new PreservedSaveFile(
                path,
                File.Exists(path) ? File.ReadAllText(path) : null));

            if (File.Exists(path))
                File.Delete(path);
        }

        private readonly struct PreservedSaveFile
        {
            public PreservedSaveFile(string path, string contents)
            {
                Path = path;
                Contents = contents;
            }

            private string Path { get; }
            private string Contents { get; }

            public void Restore()
            {
                if (Contents == null)
                {
                    if (File.Exists(Path))
                        File.Delete(Path);
                    return;
                }

                File.WriteAllText(Path, Contents);
            }
        }
    }
}
