using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    public sealed class GeneratedUiPlayModeTraversalTests
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
            Time.timeScale = 1f;
            LocalizationService.SetLanguage(LanguageOption.English);
            RestoreFile(_savePath, _originalSaveJson);
            RestoreFile(_backupPath, _originalBackupJson);
            RestoreFile(_tempPath, _originalTempJson);
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuButtons_OpenAndReturnFromPrimaryPanels()
        {
            var host = BuildMainMenuHarness();
            yield return null;

            var flows = new[]
            {
                new PanelFlow("BtnStart", "ClassSelectPanel", "BtnClassBack"),
                new PanelFlow("BtnMeta", "MetaProgressionPanel", "BtnMetaBack"),
                new PanelFlow("BtnModes", "GameModesPanel", "BtnModesBack"),
                new PanelFlow("BtnItems", "ItemsPanel", "BtnItemsBack"),
                new PanelFlow("BtnTutorial", "TutorialSetupPanel", "BtnTutBack"),
                new PanelFlow("BtnProfiles", "ProfileSelectPanel", "BtnProfileBack"),
                new PanelFlow("BtnOptions", "OptionsPanel", "BtnOptBack"),
                new PanelFlow("BtnCredits", "CreditsPanel", "BtnCredBack")
            };

            AssertPanelActive(host, "MenuCard");

            foreach (var flow in flows)
            {
                ClickButton(host, flow.OpenButton);
                yield return null;

                AssertPanelActive(host, flow.ExpectedPanel);

                ClickButton(host, flow.BackButton);
                yield return null;

                AssertPanelActive(host, "MenuCard");
            }
        }

        [UnityTest]
        public IEnumerator NestedMenuButtons_ReturnToOwningPanels()
        {
            var host = BuildMainMenuHarness();
            yield return null;

            ClickButton(host, "BtnOptions");
            yield return null;
            AssertPanelActive(host, "OptionsPanel");

            ClickButton(host, "BtnKeybindings");
            yield return null;
            AssertPanelActive(host, "KeybindingsPanel");

            ClickButton(host, "BtnKeybindBack");
            yield return null;
            AssertPanelActive(host, "OptionsPanel");

            ClickButton(host, "BtnAccessibility");
            yield return null;
            AssertPanelActive(host, "AccessibilityPanel");

            ClickButton(host, "BtnAccBack");
            yield return null;
            AssertPanelActive(host, "OptionsPanel");

            ClickButton(host, "BtnOptBack");
            yield return null;
            AssertPanelActive(host, "MenuCard");

            ClickButton(host, "BtnModes");
            yield return null;
            AssertPanelActive(host, "GameModesPanel");

            ClickButton(host, "BtnZenRecords");
            yield return null;
            AssertPanelActive(host, "EndlessZenLeaderboardPanel");

            ClickButton(host, "BtnZenLeaderboardBack");
            yield return null;
            AssertPanelActive(host, "GameModesPanel");

            ClickButton(host, "BtnTrials");
            yield return null;
            AssertPanelActive(host, "SpiritTrialsTierSelectPanel");

            ClickButton(host, "BtnTrialsSelectBack");
            yield return null;
            AssertPanelActive(host, "GameModesPanel");

            ClickButton(host, "BtnDailyWalk");
            yield return null;
            AssertPanelActive(host, "DailyWalkPanel");

            ClickButton(host, "BtnDailyBack");
            yield return null;
            AssertPanelActive(host, "GameModesPanel");

            ClickButton(host, "BtnMonthlyWalk");
            yield return null;
            AssertPanelActive(host, "MonthlyWalkPanel");

            ClickButton(host, "BtnMonthlyBack");
            yield return null;
            AssertPanelActive(host, "GameModesPanel");
        }

        [UnityTest]
        public IEnumerator OptionsPanelTabsAndLanguageConfirmation_AreRuntimeNavigable()
        {
            var host = BuildMainMenuHarness();
            yield return null;

            ClickButton(host, "BtnOptions");
            yield return null;
            AssertPanelActive(host, "OptionsPanel");
            AssertPanelActive(host, "AudioSection");
            AssertPanelInactive(host, "DisplaySection");
            AssertPanelInactive(host, "GameSection");
            AssertButtonActive(host, "MasterVolumeDec");
            AssertButtonActive(host, "MasterVolumeInc");
            AssertToggleActive(host, "MuteAll");
            AssertDropdownActive(host, "MenuMusicStyleDropdown");

            ClickButton(host, "TabDisplay");
            yield return null;
            AssertPanelInactive(host, "AudioSection");
            AssertPanelActive(host, "DisplaySection");
            AssertPanelInactive(host, "GameSection");
            AssertDropdownActive(host, "ResolutionDropdown");
            AssertDropdownActive(host, "LanguageDropdown");

            var languageDropdown = FindComponent<Dropdown>(host, "LanguageDropdown");
            var originalLanguage = languageDropdown.value;
            languageDropdown.value = originalLanguage == 0 ? 1 : 0;
            yield return null;
            AssertPanelActive(host, "LangConfirmRow");

            ClickButton(host, "BtnLangCancel");
            yield return null;
            AssertPanelInactive(host, "LangConfirmRow");
            Assert.AreEqual(originalLanguage, languageDropdown.value);

            ClickButton(host, "TabGame");
            yield return null;
            AssertPanelInactive(host, "AudioSection");
            AssertPanelInactive(host, "DisplaySection");
            AssertPanelActive(host, "GameSection");
            AssertToggleActive(host, "HighlightErrors");
            AssertToggleActive(host, "ControllerRumble");

            ClickButton(host, "BtnOptBack");
            yield return null;
            AssertPanelActive(host, "MenuCard");
        }

        [UnityTest]
        public IEnumerator InRunOptionsButtons_OpenCloseWithoutBreakingPuzzlePanel()
        {
            var host = BuildInRunHarness();
            yield return null;

            AssertPanelActive(host, "PathOverviewPanel");
            AssertPanelInactive(host, "InGameOptionsPanel");

            var sudokuPanel = FindObject(host, "SudokuGameplayPanel");
            sudokuPanel.SetActive(true);
            yield return null;

            ClickButton(host, "BtnSudokuOptions");
            yield return null;
            AssertPanelActive(host, "InGameOptionsPanel");

            ClickButton(host, "BtnInGameOptionsClose");
            yield return null;
            AssertPanelInactive(host, "InGameOptionsPanel");

            ClickButton(host, "BtnSudokuSaveQuit");
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            AssertPanelActive(host, "SudokuGameplayPanel");
        }

        [UnityTest]
        public IEnumerator InRunOptionsControls_CanBeChangedWithoutLeavingOptionsPanel()
        {
            var host = BuildInRunHarness();
            yield return null;

            var sudokuPanel = FindObject(host, "SudokuGameplayPanel");
            sudokuPanel.SetActive(true);
            yield return null;

            ClickButton(host, "BtnSudokuOptions");
            yield return null;
            AssertPanelActive(host, "InGameOptionsPanel");

            SetSliderValue(host, "IGMasterSlider", 0.35f);
            SetSliderValue(host, "IGMusicSlider", 0.40f);
            SetSliderValue(host, "IGSfxSlider", 0.45f);
            ToggleValue(host, "IGMuteWhenUnfocusedToggle");
            ToggleValue(host, "IGHighlightToggle");
            ToggleValue(host, "IGColorblindToggle");
            ToggleValue(host, "IGReduceMotionToggle");
            ToggleValue(host, "IGAltSymbolsToggle");

            yield return null;
            AssertPanelActive(host, "InGameOptionsPanel");
            AssertPanelActive(host, "SudokuGameplayPanel");

            ClickButton(host, "BtnInGameOptionsClose");
            yield return null;
            AssertPanelInactive(host, "InGameOptionsPanel");
            AssertPanelActive(host, "SudokuGameplayPanel");
        }

        [UnityTest]
        public IEnumerator GermanLargeFont_GeneratedMenusBuildAndNavigateWithoutMissingLocalization()
        {
            SeedGermanLargeFontOptions();
            LocalizationService.ResetDiagnostics();

            var menuHost = BuildMainMenuHarness();
            yield return null;

            Assert.AreEqual(LanguageOption.German, LocalizationService.Current);
            AssertPanelActive(menuHost, "MenuCard");

            ClickButton(menuHost, "BtnOptions");
            yield return null;
            AssertPanelActive(menuHost, "OptionsPanel");

            ClickButton(menuHost, "BtnAccessibility");
            yield return null;
            AssertPanelActive(menuHost, "AccessibilityPanel");
            Assert.AreEqual(1.5f, FindComponent<Slider>(menuHost, "FontScaleSlider").value, 0.01f);
            AssertNoMissingGermanKeys("main menu at 150% font scale");

            var runHost = BuildInRunHarness();
            yield return null;

            var sudokuPanel = FindObject(runHost, "SudokuGameplayPanel");
            sudokuPanel.SetActive(true);
            yield return null;

            ClickButton(runHost, "BtnSudokuOptions");
            yield return null;
            AssertPanelActive(runHost, "InGameOptionsPanel");
            AssertNoMissingGermanKeys("in-run UI at 150% font scale");
        }

        private GameObject BuildMainMenuHarness()
        {
            EnsureEventSystem();
            var host = new GameObject("GeneratedUiPlayModeMainMenuHost");
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

        private GameObject BuildInRunHarness()
        {
            EnsureCamera();
            EnsureEventSystem();

            var host = new GameObject("GeneratedUiPlayModeInRunHost");
            _createdRoots.Add(host);

            var builder = host.AddComponent<InRunUiBlueprintBuilder>();
            builder.BuildBlueprint();

            Assert.NotNull(FindObject(host, "InRunUI"));
            return host;
        }

        private void SeedGermanLargeFontOptions()
        {
            var seedHost = new GameObject("GeneratedUiGermanLargeFontOptionsSeed");
            _createdRoots.Add(seedHost);

            var controller = seedHost.AddComponent<OptionsController>();
            controller.LoadOptions();
            controller.SetLanguage(LanguageOption.German);
            controller.SetFontScale(1.5f);
            SaveFileService.FlushSharedPendingWrites();
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject(
                "GeneratedUiPlayModeEventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            _createdRoots.Add(eventSystem);
        }

        private void EnsureCamera()
        {
            if (Object.FindFirstObjectByType<Camera>() != null)
                return;

            var camera = new GameObject("GeneratedUiPlayModeCamera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().orthographic = true;
            _createdRoots.Add(camera);
        }

        private static void ClickButton(GameObject root, string name)
        {
            var button = FindComponent<Button>(root, name);
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"Button is not active in hierarchy: {name}");
            Assert.IsTrue(button.interactable, $"Button is not interactable: {name}");
            button.onClick.Invoke();
        }

        private static void ToggleValue(GameObject root, string name)
        {
            var toggle = FindComponent<Toggle>(root, name);
            Assert.IsTrue(toggle.gameObject.activeInHierarchy, $"Toggle is not active in hierarchy: {name}");
            Assert.IsTrue(toggle.interactable, $"Toggle is not interactable: {name}");
            toggle.isOn = !toggle.isOn;
        }

        private static void SetSliderValue(GameObject root, string name, float value)
        {
            var slider = FindComponent<Slider>(root, name);
            Assert.IsTrue(slider.gameObject.activeInHierarchy, $"Slider is not active in hierarchy: {name}");
            Assert.IsTrue(slider.interactable, $"Slider is not interactable: {name}");
            slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
        }

        private static void AssertButtonActive(GameObject root, string name)
        {
            var button = FindComponent<Button>(root, name);
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"Expected active button: {name}");
            Assert.IsTrue(button.interactable, $"Expected interactable button: {name}");
        }

        private static void AssertToggleActive(GameObject root, string name)
        {
            var toggle = FindComponent<Toggle>(root, name);
            Assert.IsTrue(toggle.gameObject.activeInHierarchy, $"Expected active toggle: {name}");
            Assert.IsTrue(toggle.interactable, $"Expected interactable toggle: {name}");
        }

        private static void AssertDropdownActive(GameObject root, string name)
        {
            var dropdown = FindComponent<Dropdown>(root, name);
            Assert.IsTrue(dropdown.gameObject.activeInHierarchy, $"Expected active dropdown: {name}");
            Assert.IsTrue(dropdown.interactable, $"Expected interactable dropdown: {name}");
            Assert.Greater(dropdown.options.Count, 0, $"Dropdown has no options: {name}");
        }

        private static void AssertNoMissingGermanKeys(string context)
        {
            CollectionAssert.IsEmpty(
                LocalizationService.MissingGermanKeys,
                $"Missing German localization keys while building {context}.");
        }

        private static void AssertPanelActive(GameObject root, string name)
        {
            var panel = FindObject(root, name);
            Assert.IsTrue(panel.activeInHierarchy, $"Expected active panel/control: {name}");
        }

        private static void AssertPanelInactive(GameObject root, string name)
        {
            var panel = FindObject(root, name);
            Assert.IsFalse(panel.activeInHierarchy, $"Expected inactive panel/control: {name}");
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

        private readonly struct PanelFlow
        {
            public PanelFlow(string openButton, string expectedPanel, string backButton)
            {
                OpenButton = openButton;
                ExpectedPanel = expectedPanel;
                BackButton = backButton;
            }

            public string OpenButton { get; }
            public string ExpectedPanel { get; }
            public string BackButton { get; }
        }
    }
}
