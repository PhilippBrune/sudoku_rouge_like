using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class FirstRunSetupPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalActiveSlot;
        private string _savePath;
        private string _backupPath;
        private string _tempPath;
        private string _originalSaveJson;
        private string _originalBackupJson;
        private string _originalTempJson;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();

            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;

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

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();

            for (var i = _createdRoots.Count - 1; i >= 0; i--)
            {
                if (_createdRoots[i] != null)
                    Object.Destroy(_createdRoots[i]);
            }

            _createdRoots.Clear();
            RestoreFile(_savePath, _originalSaveJson);
            RestoreFile(_backupPath, _originalBackupJson);
            RestoreFile(_tempPath, _originalTempJson);
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            LocalizationService.SetLanguage(LanguageOption.English);

            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstRunModal_PersistsLanguageAccessibilityLegalAndShowsTutorialPrompt()
        {
            var host = BuildMainMenuHarness();
            var menu = host.GetComponent<MainMenuController>();
            var callbackCompleted = false;

            menu.ShowFirstRunSetupPrompt(() =>
            {
                callbackCompleted = true;
                menu.ShowSudokuBasicsPrompt();
            });
            yield return null;

            AssertPanelActive(host, "FirstRunSetupModal");
            ClickButton(host, "BtnLanguageGerman");
            yield return null;

            Assert.AreEqual("Weiter", GetButtonLabel(host, "BtnContinueFirstRun"));

            SetToggle(host, "ToggleColorblind", true);
            SetToggle(host, "ToggleContrast", true);
            SetToggle(host, "ToggleMotion", true);
            SetToggle(host, "ToggleScreenReader", true);

            ClickButton(host, "BtnContinueFirstRun");
            SaveFileService.FlushSharedPendingWrites();
            yield return null;

            Assert.IsTrue(callbackCompleted, "First-run completion callback was not invoked.");
            AssertPanelActive(host, "SudokuBasicsPrompt");

            var envelope = new SaveFileService(TestSlot).Load();
            var options = envelope.PlayerProfile.Options;
            var firstRun = options.FirstRun;

            Assert.AreEqual(LanguageOption.German, options.Language);
            Assert.IsTrue(options.Accessibility.ColorblindMode);
            Assert.IsTrue(options.Accessibility.HighContrastMode);
            Assert.IsTrue(options.Accessibility.ReduceMotion);
            Assert.IsTrue(options.Accessibility.ScreenReaderEnabled);
            Assert.IsTrue(firstRun.LanguageSelected);
            Assert.IsTrue(firstRun.AccessibilityReviewed);
            Assert.AreEqual(
                StartupFlowController.CurrentLegalNoticeVersion,
                firstRun.AcceptedLegalNoticeVersion);
            Assert.IsFalse(string.IsNullOrEmpty(firstRun.CompletedUtc));
            Assert.AreEqual(LanguageOption.German, LocalizationService.Current);
        }

        private GameObject BuildMainMenuHarness()
        {
            EnsureEventSystem();
            var host = new GameObject("FirstRunSetupPlayModeHost");
            _createdRoots.Add(host);

            var builder = host.AddComponent<MainMenuBlueprintBuilder>();
            builder.Build();

            var root = FindObject(host, "MainMenuRoot");
            var rootGroup = root.GetComponent<CanvasGroup>();
            if (rootGroup != null)
                rootGroup.alpha = 1f;

            var menu = host.GetComponent<MainMenuController>();
            Assert.NotNull(menu, "MainMenuController was not created by MainMenuBlueprintBuilder.");
            menu.ShowMainMenu();

            return host;
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject(
                "FirstRunSetupPlayModeEventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            _createdRoots.Add(eventSystem);
        }

        private static void ClickButton(GameObject root, string name)
        {
            var button = FindComponent<Button>(root, name);
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"Button is not active in hierarchy: {name}");
            Assert.IsTrue(button.interactable, $"Button is not interactable: {name}");
            button.onClick.Invoke();
        }

        private static void SetToggle(GameObject root, string name, bool value)
        {
            var toggle = FindComponent<Toggle>(root, name);
            Assert.IsTrue(toggle.gameObject.activeInHierarchy, $"Toggle is not active in hierarchy: {name}");
            toggle.isOn = value;
        }

        private static string GetButtonLabel(GameObject root, string buttonName)
        {
            var button = FindComponent<Button>(root, buttonName);
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            Assert.NotNull(label, $"Button label missing: {buttonName}");
            return label.text;
        }

        private static void AssertPanelActive(GameObject root, string name)
        {
            var panel = FindObject(root, name);
            Assert.IsTrue(panel.activeInHierarchy, $"Expected active panel/control: {name}");
        }

        private static GameObject FindObject(GameObject root, string name)
        {
            var match = root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == name);
            Assert.NotNull(match, $"Missing generated UI object: {name}");
            return match.gameObject;
        }

        private static T FindComponent<T>(GameObject root, string name) where T : Component
        {
            var match = root
                .GetComponentsInChildren<T>(true)
                .FirstOrDefault(component => component.name == name);
            Assert.NotNull(match, $"Missing generated UI component: {name}");
            return match;
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
