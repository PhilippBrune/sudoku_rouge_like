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
    [Requirement("REQ-UI-102")]
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

            _savePath = Path.Combine(SaveFileService.SaveRoot, $"save_profile_{TestSlot}.json");
            _backupPath = _savePath + ".bak";
            _tempPath = _savePath + ".tmp";

            Directory.CreateDirectory(SaveFileService.SaveRoot);

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
            // Endless Zen records and Spirit Trials are locked on a fresh profile by design
            // (ConditionalButtonStateTests); unlock them so every nested panel can be visited.
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope
            {
                MetaProgress = new MetaProgressionState { EndlessZenUnlocked = true, SpiritTrialsUnlocked = true }
            });
            SaveFileService.FlushSharedPendingWrites();

            var host = BuildMainMenuHarness();
            yield return null;

            ClickButton(host, "BtnOptions");
            yield return null;
            AssertPanelActive(host, "OptionsPanel");

            ClickButton(host, "TabControls");   // UI-STYLE-017: keybindings live on the Controls page
            yield return null;
            ClickButton(host, "BtnKeybindings");
            yield return null;
            AssertPanelActive(host, "KeybindingsPanel");

            ClickButton(host, "BtnKeybindBack");
            yield return null;
            AssertPanelActive(host, "OptionsPanel");

            // UI-STYLE-017: Accessibility is a page of the Options scroll (the separate panel is gone).
            ClickButton(host, "TabAccessibility");
            yield return null;
            AssertPanelActive(host, "AccessibilitySection");
            AssertPanelActive(host, "AccessPreview");

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

            // [REQ: UI-STYLE-027] The Daily Walk's curtain starts the day's walk (no game here to start it, so Game Modes
            // stays); its goals panel opens from the home widget (HomeWidgets_OpenDailyAndMonthly...).
            ClickButton(host, "BtnDailyWalk");
            yield return null;
            AssertPanelActive(host, "GameModesPanel");

            ClickButton(host, "BtnMonthlyWalk");
            yield return null;
            AssertPanelActive(host, "MonthlyWalkPanel");

            ClickButton(host, "BtnMonthlyBack");
            yield return null;
            AssertPanelActive(host, "GameModesPanel");
        }

        // 1.1 (2026-10-07): Daily/Monthly widgets on the main menu (SPEC-CONF-027, GAP-028) and the
        // "Allow irregular puzzles" toggle ON by default (SPEC-CONF-031, GAP-029).
        [UnityTest]
        [Requirement("REQ-UI-106", "DAILY-GOAL-004", "SEASON-UI-001", "GEN-REGION-003")]
        public IEnumerator HomeWidgets_OpenDailyAndMonthly_AndIrregularPuzzlesDefaultOn()
        {
            var host = BuildMainMenuHarness();
            yield return null;

            AssertPanelActive(host, "MenuCard");
            AssertPanelActive(host, "DailyWidget");
            AssertPanelActive(host, "MonthlyWidget");
            Assert.IsNotEmpty(FindObject(FindObject(host, "DailyWidget"), "WidgetInfo").GetComponent<Text>().text);
            Assert.IsNotEmpty(FindObject(FindObject(host, "MonthlyWidget"), "WidgetInfo").GetComponent<Text>().text);

            ClickButton(host, "BtnDailyWidget");
            yield return null;
            AssertPanelActive(host, "DailyWalkPanel");
            ClickButton(host, "BtnDailyBack");
            yield return null;
            ClickButton(host, "BtnModesBack");
            yield return null;
            AssertPanelActive(host, "MenuCard");

            ClickButton(host, "BtnMonthlyWidget");
            yield return null;
            AssertPanelActive(host, "MonthlyWalkPanel");
            ClickButton(host, "BtnMonthlyBack");
            yield return null;
            ClickButton(host, "BtnModesBack");
            yield return null;
            AssertPanelActive(host, "MenuCard");

            ClickButton(host, "BtnStart");
            yield return null;
            AssertPanelActive(host, "ClassSelectPanel");
            Assert.IsTrue(FindObject(host, "TglAllowIrregular").GetComponent<Toggle>().isOn, "irregular puzzles are ON by default");
        }

        [UnityTest]
        [Requirement("DISPLAY-MODE-001")]
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
            AssertToggleActive(host, "MuteAllToggle");
            AssertDropdownActive(host, "MenuMusicStyleDropdown");

            ClickButton(host, "TabDisplay");
            yield return null;
            AssertPanelInactive(host, "AudioSection");
            AssertPanelActive(host, "DisplaySection");
            AssertPanelInactive(host, "GameSection");
            AssertDropdownActive(host, "ResolutionDropdown");
            AssertDisplayModeOptions(host);

            // The open list shows every mode: before 1.1 the list was sized for 28 px items while the layout used
            // 30 px plus spacing, so the last option was cut off.
            var displayModeDropdown = FindComponent<Dropdown>(host, "DisplayModeDropdown");
            displayModeDropdown.Show();
            yield return null;
            AssertOpenListShowsAllItems(displayModeDropdown);
            displayModeDropdown.Hide();
            yield return new WaitForSecondsRealtime(0.3f);

            ClickButton(host, "TabLanguage");   // UI-STYLE-017: Language is its own page
            yield return null;
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
            AssertToggleActive(host, "HighlightErrorsToggle");
            AssertToggleActive(host, "ControllerRumbleToggle");

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
        [Requirement("UI-OPT-001", "UI-OPT-002")]
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
            var options = host.GetComponent<OptionsController>();
            Assert.NotNull(options, "the in-run builder owns the OptionsController its controls write to");

            // Audio: the arrow row changes the option and its readout.
            AssertPanelActive(host, "IGAudioSection");
            var master = options.GetOptions().Audio.MasterVolume;
            ClickButton(host, master >= 1f ? "IGMasterVolumeDec" : "IGMasterVolumeInc");
            var changed = options.GetOptions().Audio.MasterVolume;
            Assert.AreNotEqual(master, changed, "the master volume arrow changes the option");
            Assert.AreEqual(Mathf.RoundToInt(changed * 100f) + "%", FindComponent<Text>(host, "IGMasterVolumeVal").text);
            ClickButton(host, master >= 1f ? "IGMasterVolumeInc" : "IGMasterVolumeDec");
            Assert.AreEqual(master, options.GetOptions().Audio.MasterVolume, 0.001f);
            ToggleValue(host, "IGMuteUnfocusedToggle");
            AssertDropdownActive(host, "IGMenuMusicStyleDropdown");

            // Display: same controls as the main menu; the language is changed there (hint instead of the row).
            ClickButton(host, "IGTabDisplay");
            yield return null;
            AssertPanelInactive(host, "IGAudioSection");
            AssertPanelActive(host, "IGDisplaySection");
            var shake = options.GetOptions().Graphics.ScreenShake;
            ToggleValue(host, "IGScreenShakeToggle");
            Assert.AreNotEqual(shake, options.GetOptions().Graphics.ScreenShake);
            ToggleValue(host, "IGScreenShakeToggle");
            AssertDropdownActive(host, "IGResolutionDropdown");
            AssertDisplayModeOptions(host, "IGDisplayModeDropdown");
            var displayModeDropdown = FindComponent<Dropdown>(host, "IGDisplayModeDropdown");
            displayModeDropdown.Show();
            yield return null;
            AssertOpenListShowsAllItems(displayModeDropdown);
            displayModeDropdown.Hide();
            yield return new WaitForSecondsRealtime(0.3f);
            ClickButton(host, "IGTabLanguage");   // UI-STYLE-017: Language is its own page
            yield return null;
            AssertPanelActive(host, "IGLangHint");

            // Gameplay
            ClickButton(host, "IGTabGame");
            yield return null;
            AssertPanelActive(host, "IGGameSection");
            var keepMarks = options.GetOptions().Gameplay.KeepPencilMarks;   // Auto Pencil Cleanup off = keep (GAP-061)
            ToggleValue(host, "IGAutoPencilToggle");
            Assert.AreNotEqual(keepMarks, options.GetOptions().Gameplay.KeepPencilMarks);
            ToggleValue(host, "IGHighlightErrorsToggle");

            // Accessibility: the toggles write the option; Reset restores the defaults and refreshes the toggles.
            ClickButton(host, "IGTabAccessibility");
            yield return null;
            AssertPanelActive(host, "IGAccessibilitySection");
            var colorblind = options.GetOptions().Accessibility.ColorblindMode;
            ToggleValue(host, "IGColorblindToggle");
            Assert.AreNotEqual(colorblind, options.GetOptions().Accessibility.ColorblindMode);
            ToggleValue(host, "IGReduceMotionToggle");
            ToggleValue(host, "IGAltSymbolsToggle");
            ClickButton(host, "IGBtnResetAccessibility");
            yield return null;
            Assert.AreEqual(options.GetOptions().Accessibility.ColorblindMode, FindComponent<Toggle>(host, "IGColorblindToggle").isOn,
                "Reset refreshes the toggles");

            // Run: the run-only actions.
            ClickButton(host, "IGTabRun");
            yield return null;
            AssertPanelActive(host, "IGRunSection");
            AssertPanelActive(host, "IGRunSeed");
            AssertButtonActive(host, "BtnInGameOptionsModifiers");
            AssertButtonActive(host, "BtnInGameOptionsSaveQuit");

            yield return null;
            AssertPanelActive(host, "InGameOptionsPanel");
            AssertPanelActive(host, "SudokuGameplayPanel");

            ClickButton(host, "BtnInGameOptionsClose");
            yield return null;
            AssertPanelInactive(host, "InGameOptionsPanel");
            AssertPanelActive(host, "SudokuGameplayPanel");
        }

        // [REQ: UI-OPT-001] Every control of the main-menu Options tabs and Accessibility panel exists in the run
        // ("IG" prefix), built by the same factory; only the language row stays in the main menu.
        [UnityTest]
        [Requirement("UI-OPT-001", "UI-OPT-002")]
        public IEnumerator InRunOptions_OfferEveryMainMenuOptionsControl()
        {
            var menuHost = BuildMainMenuHarness();
            var runHost = BuildInRunHarness();
            yield return null;

            var mainOnly = new HashSet<string> { "LanguageDropdown", "BtnLangApply", "BtnLangCancel", "FontScaleDec", "FontScaleInc" };
            var checkedCount = 0;
            foreach (var container in new[] { "AudioSection", "DisplaySection", "GameSection", "AccessibilitySection" })
            {
                foreach (var control in OwnSelectables(FindObject(menuHost, container)))
                {
                    if (mainOnly.Contains(control.name)) continue;
                    var twin = runHost.GetComponentsInChildren<Selectable>(true).FirstOrDefault(s => s.name == "IG" + control.name);
                    Assert.NotNull(twin, $"in-run Options lack {control.name} from {container}");
                    Assert.AreEqual(control.GetType(), twin.GetType(), control.name);
                    checkedCount++;
                }
            }
            Assert.Greater(checkedCount, 25, "the comparison found the main Options controls");

            foreach (var tab in new[] { "IGTabAudio", "IGTabDisplay", "IGTabGame", "IGTabAccessibility", "IGTabLanguage", "IGTabRun" })
                FindComponent<Button>(runHost, tab);
            Assert.NotNull(FindObject(menuHost, "OptionsPanel").GetComponentInChildren<OptionsTabSet>(true));
            Assert.NotNull(FindObject(runHost, "InGameOptionsPanel").GetComponentInChildren<OptionsTabSet>(true));
        }

        // [REQ: UI-OPT-003] From each tab, the D-pad reaches every control of its section, the other tabs and Back.
        [UnityTest]
        [Requirement("UI-OPT-003")]
        public IEnumerator OptionsPanels_EveryControlIsReachableWithTheDpad()
        {
            var menuHost = BuildMainMenuHarness();
            yield return null;
            ClickButton(menuHost, "BtnOptions");
            yield return null;
            yield return AssertEveryTabReachable(FindObject(menuHost, "OptionsPanel"), "BtnOptBack");

            var runHost = BuildInRunHarness();
            yield return null;
            FindObject(runHost, "SudokuGameplayPanel").SetActive(true);
            yield return null;
            ClickButton(runHost, "BtnSudokuOptions");
            yield return null;
            yield return AssertEveryTabReachable(FindObject(runHost, "InGameOptionsPanel"), "BtnInGameOptionsClose");
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

            ClickButton(menuHost, "TabAccessibility");
            yield return null;
            AssertPanelActive(menuHost, "AccessibilitySection");
            Assert.AreEqual("150%", FindComponent<Text>(menuHost, "FontScaleVal").text);
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

        // [REQ: DISPLAY-MODE-001] Options → Display offers the four modes in DisplayModeOption order.
        private static void AssertDisplayModeOptions(GameObject root, string name = "DisplayModeDropdown")
        {
            AssertDropdownActive(root, name);
            var labels = FindComponent<Dropdown>(root, name).options.ConvertAll(option => option.text);
            CollectionAssert.AreEqual(new[] { "Fullscreen", "Borderless Fullscreen", "Windowed", "Frameless Window" }, labels);
        }

        private static void AssertOpenListShowsAllItems(Dropdown dropdown)
        {
            var list = dropdown.transform.Find("Dropdown List") as RectTransform;
            Assert.NotNull(list, "dropdown list did not open");
            Canvas.ForceUpdateCanvases();
            var viewport = list.Find("Viewport") as RectTransform;
            var viewportCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            var items = list.GetComponentsInChildren<Toggle>(false); // the inactive item template stays in the list
            Assert.AreEqual(dropdown.options.Count, items.Length, "one item per option");
            var itemCorners = new Vector3[4];
            foreach (var item in items)
            {
                ((RectTransform)item.transform).GetWorldCorners(itemCorners);
                Assert.GreaterOrEqual(itemCorners[0].y, viewportCorners[0].y - 0.5f, $"{item.name} is cut off at the bottom");
            }
        }

        // Controls a player selects: not the items inside a dropdown's list template.
        private static IEnumerable<Selectable> OwnSelectables(GameObject container, bool includeInactive = true)
        {
            return container.GetComponentsInChildren<Selectable>(includeInactive)
                .Where(s => s.transform.parent.GetComponentInParent<Dropdown>(true) == null);
        }

        private static IEnumerator AssertEveryTabReachable(GameObject panel, string backName)
        {
            var tabs = panel.GetComponentInChildren<OptionsTabSet>(true);
            Assert.NotNull(tabs, panel.name + " has no OptionsTabSet");
            var back = FindComponent<Button>(panel, backName);
            for (var i = 0; i < tabs.Count; i++)
            {
                tabs.Tabs[i].onClick.Invoke();
                yield return null;
                var reached = Reachable(tabs.Tabs[i]);
                var expected = OwnSelectables(tabs.Sections[i].gameObject, false).Where(s => s.IsInteractable()).ToList();
                // A page with only a note (the run's Language page says where to change it) still leads to Back.
                if (!tabs.Sections[i].name.EndsWith("LanguageSection"))
                    Assert.IsNotEmpty(expected, $"{panel.name}/{tabs.Sections[i].name} has no controls");
                expected.Add(back);
                expected.AddRange(tabs.Tabs);
                foreach (var control in expected)
                    Assert.IsTrue(reached.Contains(control),
                        $"{panel.name}/{tabs.Sections[i].name}: {control.name} cannot be reached with the D-pad from {tabs.Tabs[i].name}");
            }
        }

        private static HashSet<Selectable> Reachable(Selectable start)
        {
            var seen = new HashSet<Selectable> { start };
            var queue = new Queue<Selectable>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in new[]
                         {
                             current.FindSelectableOnUp(), current.FindSelectableOnDown(),
                             current.FindSelectableOnLeft(), current.FindSelectableOnRight()
                         })
                {
                    if (next != null && next.IsActive() && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            return seen;
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
