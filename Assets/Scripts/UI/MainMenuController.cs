using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;

namespace SudokuRoguelike.UI
{
    public sealed partial class MainMenuController : MonoBehaviour
    {
        // ── Panel References ──
        private GameObject _mainMenuPanel;
        private GameObject _classSelectPanel;
        private GameObject _optionsPanel;
        private GameObject _creditsPanel;
        private GameObject _tutorialSetupPanel;
        private GameObject _metaProgressionPanel;
        private GameObject _gameModesPanel;
        private GameObject _itemsPanel;
        private GameObject _profileSelectPanel;
        private GameObject _keybindingsPanel;
        private GameObject _monthlyWalkPanel;
        private GameObject _spiritTrialsTierSelectPanel;
        private GameObject _zenLeaderboardPanel;

        private SpiritTrialsTierSelectController _trialsSelectCtrl;
        private EndlessZenLeaderboardController  _zenLeaderboardCtrl;

        // ── Controllers ──
        private OptionsController _optionsController;
        private TutorialMenuController _tutorialMenuController;
        private MetaProgressionPanelController _metaProgressionController;
        private GameModesPanelController _gameModesController;
        private ItemsMenuController _itemsMenuController;
        private KeybindingsPanelController _keybindingsController;
        private GameBootstrap _gameBootstrap;

        // ── State ──
        private ClassId _selectedClass = ClassId.NumberFreak;
        private bool _allowIrregularPuzzles = true; // SPEC-CONF-031: matches the toggle and RunState default
        private bool _debugEnableAllFeatures;
        private GameMode _pendingLaunchMode = GameMode.GardenRun;
        private SpiritTrialsTier _pendingTrialsTier = SpiritTrialsTier.Apprentice;

        // ── Services ──
        private MenuFlowService _menu;
        private SaveFileService _save;
        private ProfileService _profile;

        private ProfileService Profile
        {
            get { EnsureInitialized(); return _profile; }
        }
        private SaveProfileService _profileSlots;
        private ClassUnlockService _classUnlockService;
        private InputRemapService _inputRemap;
        public  InputRemapService  InputRemap => _inputRemap;

        // ── Canvas reference (F) ──
        private Canvas _menuCanvas;

        // ── UI Widgets ──
        private Text _statusText;
        private Button _resumeButton;
        private Text _harmonyReadout;
        private Text _harmonyDescriptionLabel;
        private Button _harmonyPrevClassBtn;
        private Button _harmonyNextClassBtn;
        private readonly Dictionary<ClassId, Button> _classButtons = new Dictionary<ClassId, Button>();
        private readonly Dictionary<ClassId, GameObject> _classLockOverlays = new Dictionary<ClassId, GameObject>();
        private ClassId? _highlightedClassId;
        private GameObject _sudokuBasicsOverlay;      // #14 dismiss
        private GameObject _firstRunSetupOverlay;

        private static string HarmonyName(int level)
        {
            return LocalizationService.T($"Harmony.Name.{Math.Clamp(level, 0, 10)}",
                HarmonyDifficultyService.GetDisplayName(level));
        }

        private static string HarmonyPerkName(HarmonyPerkId perk)
        {
            return LocalizationService.T($"Harmony.Perk.Name.{perk}",
                HarmonyDifficultyService.GetPerkDisplayName(perk));
        }

        private static string HarmonyPerkDescription(HarmonyPerkId perk)
        {
            return LocalizationService.T($"Harmony.Perk.Description.{perk}",
                HarmonyDifficultyService.GetPerkDescription(perk));
        }

        private void Awake() => EnsureInitialized();

        // The generated menu builder configures this controller and calls into it while building,
        // which in edit-mode tests happens before Awake runs; initialise on first use as well.
        private void EnsureInitialized()
        {
            if (_menu != null) return;
            _inputRemap = new InputRemapService();
            _menu = new MenuFlowService();
            _menu.OnPanelShown += SelectFirstInPanel;
            _menu.OnPanelShown += _ => FindFirstObjectByType<MenuMusicController>()?.SetCreditsActive(_menu.CurrentScreen == MenuScreen.Credits);
            _profileSlots = new SaveProfileService();
            _save = new SaveFileService(SaveProfileService.ActiveSlot);
            _profile = new ProfileService(_save);
            _classUnlockService = new ClassUnlockService();
        }

        // [REQ: INPUT-PROFILE-001] [REQ: REQ-UI-100] GAP-006: GameBootstrap's startup flow can pick a screen (Profile
        // Select) before this Start runs; showing the main menu here unconditionally replaced it on a fresh install.
        private bool _startupScreenChosen;

        private void Start()
        {
            if (_startupScreenChosen) return;
            ShowMainMenu();
        }

        // ── Controller support ──
        // B (JoystickButton1) → back  |  A (JoystickButton0) → confirm selected button
        // R3 (JoystickButton9) kept as legacy confirm alias
        // left-stick / d-pad → navigate via EventSystem
        private void Update()
        {
            if (FeedbackScreen.IsOpen) return;   // [REQ: FEEDBACK-001] keys go to the feedback form
            // [REQ: UI-MENU-TITLE-001] GAP-097: nothing is chosen under the title screen, so A or Enter there presses nothing.
            if (SudokuRoguelike.Bootstrap.SplashScreenController.Showing)
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            InputRemapService.UpdateCursorVisibility();
            // [REQ: STORY-SCENE-001] A story scene from the Codex takes A, B and Start until it ends.
            if (StoryScene.Blocking) return;

            // MM-2/MM-6: axis-driven scroll for scrollable panels (runs every frame, before anyKeyDown guard)
            ScrollActivePanelIfNeeded();

            _inputRemap.Tick();

            // Run every frame so D-pad axis input can navigate without a button press first.
            EnsureControllerSelection();
            HandleMenuGamepadNavigation();

            // [REQ: INPUT-PAD-005] The owner's pad is never a legacy "any key": its presses pass here too.
            if (!Input.anyKeyDown && !PadButtons.AnyPressed()) return;

            if (_firstRunSetupOverlay != null && _firstRunSetupOverlay.activeSelf)
            {
                if (PadButtons.ConfirmPressed(KeyCode.Return))
                {
                    var es = EventSystem.current;
                    var sel = es?.currentSelectedGameObject;
                    if (sel != null)
                    {
                        var btn = sel.GetComponent<Button>();
                        if (btn != null && btn.interactable)
                            btn.onClick.Invoke();
                        else
                        {
                            var tgl = sel.GetComponent<Toggle>();
                            if (tgl != null && tgl.interactable)
                                tgl.isOn = !tgl.isOn;
                            else
                                ExecuteEvents.Execute(sel, new BaseEventData(es), ExecuteEvents.submitHandler);
                        }
                    }
                }
                return;
            }

            // #14 — B/Escape dismisses the first-run Sudoku basics onboarding overlay
            if (_sudokuBasicsOverlay != null && _sudokuBasicsOverlay.activeSelf)
            {
                // MM-5: A/Enter confirms (same action as B/Escape — both dismiss the basics prompt)
                if (PadButtons.Pressed(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return)
                    || PadButtons.Pressed(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    ExecuteMainMenuCancelRoute(CancelRouteAction.DismissSudokuBasicsOverlay);
                    return;
                }
            }

            // [REQ: INPUT-PAD-005] Start opens the Options and closes them again (back to the screen before); not while a
            // key binding waits for its button.
            if (PadButtons.Pressed(KeyCode.JoystickButton7) && !KeybindingsPanelController.Listening)
            {
                if (_menu.CurrentScreen == MenuScreen.Options) BackFromPanel();
                else OpenOptions();
                return;
            }

            // B button / Escape = back/cancel  (X-1: Escape now handles class select + all panels)
            if (TryHandleMainMenuCancelInput())
                return;

            // A (JoystickButton0) / R3 (JoystickButton9, legacy alias) = confirm selected element (G1-A)
            if (PadButtons.ConfirmPressed() || PadButtons.Pressed(KeyCode.JoystickButton9))
            {
                var es = EventSystem.current;
                var sel = es?.currentSelectedGameObject;
                if (sel != null)
                {
                    var btn = sel.GetComponent<Button>();
                    if (btn != null && btn.interactable)
                    {
                        btn.onClick.Invoke();
                    }
                    else
                    {
                        var tgl = sel.GetComponent<Toggle>();
                        if (tgl != null && tgl.interactable)
                            tgl.isOn = !tgl.isOn;
                        else
                            ExecuteEvents.Execute(sel, new BaseEventData(es), ExecuteEvents.submitHandler);
                    }
                }
                return;
            }

            EnsureControllerSelection();
        }

        private bool TryHandleMainMenuCancelInput()
        {
            if (!PadButtons.Pressed(KeyCode.JoystickButton1) && !Input.GetKeyDown(KeyCode.Escape))
                return false;
            if (CloseGameModesOverlay()) return true;   // the stamp shop or the waiting XP first

            ExecuteMainMenuCancelRoute(CancelRouteContract.ResolveMainMenuCancel(
                new MainMenuCancelContext(
                    IsActive(_firstRunSetupOverlay),
                    IsActive(_sudokuBasicsOverlay),
                    false,   // the start-run confirmation window is gone (UI-STYLE-024)
                    _menu?.CurrentScreen ?? MenuScreen.MainMenu)));
            return true;
        }

        private void ExecuteMainMenuCancelRoute(CancelRouteAction route)
        {
            switch (route)
            {
                case CancelRouteAction.DismissSudokuBasicsOverlay:
                    DismissSudokuBasicsOverlay();
                    break;
                case CancelRouteAction.BackFromMenuPanel:
                    BackFromPanel();
                    break;
            }
        }

        private void DismissSudokuBasicsOverlay()
        {
            if (_sudokuBasicsOverlay == null) return;
            Destroy(_sudokuBasicsOverlay);
            _sudokuBasicsOverlay = null;
            MarkSudokuBasicsComplete();
        }

        private static bool IsActive(GameObject go) => go != null && go.activeSelf;

        // MM-2/MM-6: scroll the active panel's first ScrollRect using the vertical axis input.
        private void ScrollActivePanelIfNeeded()
        {
            var panel = _menu?.CurrentPanel;
            if (panel == null) return;
            var sr = panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (sr == null) return;
            var v = Input.GetAxis("Vertical");
            if (Mathf.Abs(v) < 0.1f) return;
            sr.verticalNormalizedPosition = Mathf.Clamp01(
                sr.verticalNormalizedPosition + v * Time.unscaledDeltaTime * 0.8f);
        }

        // [REQ: INPUT-NAV-001] The D-pad steps along the panel's links, once per press (GAP-062). It used to cycle through the
        // panel in creation order on every direction, arrow keys and stick included, which the input module also moved on.
        private void HandleMenuGamepadNavigation()
        {
            var direction = MenuDpad.ReadPressed();
            if (direction == MoveDirection.None) direction = MenuDpad.ReadRemapped(_inputRemap);
            if (direction == MoveDirection.None) return;
            var panel = _menu?.CurrentPanel;
            var es = EventSystem.current;
            if (panel == null || es == null) return;

            var selected = es.currentSelectedGameObject;
            var current = selected != null ? selected.GetComponent<Selectable>() : null;
            if (current == null || !current.transform.IsChildOf(panel.transform))
            {
                SelectFirstInPanel(panel);
                return;
            }
            var next = MenuDpad.Step(current, direction);
            if (next != null) es.SetSelectedGameObject(next.gameObject);
        }

        private void EnsureControllerSelection()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null || es.currentSelectedGameObject != null) return;
            SelectFirstInPanel(_menu.CurrentPanel);
        }

        private static void SelectFirstInPanel(GameObject panel)
        {
            if (panel == null) return;
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;
            foreach (var btn in panel.GetComponentsInChildren<Button>(true))
            {
                if (!btn.interactable || !btn.gameObject.activeInHierarchy) continue;
                es.SetSelectedGameObject(btn.gameObject);
                return;
            }
        }

        // ── Configuration (called by BlueprintBuilder) ──

        public void ConfigureUi(
            GameBootstrap bootstrap,
            GameObject mainMenuPanel, GameObject classSelectPanel,
            GameObject optionsPanel, GameObject creditsPanel,
            GameObject tutorialSetupPanel, GameObject metaProgressionPanel,
            GameObject gameModesPanel, GameObject itemsPanel,
            GameObject profileSelectPanel, GameObject keybindingsPanel,
            OptionsController optionsCtrl, TutorialMenuController tutorialCtrl,
            MetaProgressionPanelController metaCtrl, GameModesPanelController modesCtrl,
            ItemsMenuController itemsCtrl, KeybindingsPanelController keybindCtrl,
            Text statusText, Button resumeButton,
            GameObject monthlyWalkPanel = null,
            GameObject accessibilityPanel = null,
            GameObject spiritTrialsTierSelectPanel = null, GameObject zenLeaderboardPanel = null,
            SpiritTrialsTierSelectController trialsSelectCtrl = null,
            EndlessZenLeaderboardController zenLeaderboardCtrl = null)
        {
            EnsureInitialized();
            _gameBootstrap = bootstrap;
            _mainMenuPanel = mainMenuPanel;
            _classSelectPanel = classSelectPanel;
            _optionsPanel = optionsPanel;
            _creditsPanel = creditsPanel;
            _tutorialSetupPanel = tutorialSetupPanel;
            _metaProgressionPanel = metaProgressionPanel;
            _gameModesPanel = gameModesPanel;
            _itemsPanel = itemsPanel;
            _profileSelectPanel = profileSelectPanel;
            _keybindingsPanel = keybindingsPanel;
            _monthlyWalkPanel = monthlyWalkPanel;
            _spiritTrialsTierSelectPanel = spiritTrialsTierSelectPanel;
            _zenLeaderboardPanel = zenLeaderboardPanel;
            _trialsSelectCtrl   = trialsSelectCtrl;
            _zenLeaderboardCtrl = zenLeaderboardCtrl;
            _optionsController = optionsCtrl;
            _tutorialMenuController = tutorialCtrl;
            _metaProgressionController = metaCtrl;
            if (metaCtrl != null) metaCtrl.PrestigeRequested = PrestigeClass;
            _gameModesController = modesCtrl;
            _itemsMenuController = itemsCtrl;
            _keybindingsController = keybindCtrl;
            _statusText = statusText;
            _resumeButton = resumeButton;

            // Register panels with flow service
            _menu.RegisterPanel(MenuScreen.MainMenu, mainMenuPanel);
            _menu.RegisterPanel(MenuScreen.ClassSelect, classSelectPanel);
            _menu.RegisterPanel(MenuScreen.Options, optionsPanel);
            if (monthlyWalkPanel != null) _menu.RegisterPanel(MenuScreen.MonthlyWalk, monthlyWalkPanel);   // MENU-BACK-001
            _menu.RegisterPanel(MenuScreen.Credits, creditsPanel);
            _menu.RegisterPanel(MenuScreen.TutorialSetup, tutorialSetupPanel);
            _menu.RegisterPanel(MenuScreen.MetaProgression, metaProgressionPanel);
            _menu.RegisterPanel(MenuScreen.GameModes, gameModesPanel);
            _menu.RegisterPanel(MenuScreen.Items, itemsPanel);
            _menu.RegisterPanel(MenuScreen.ProfileSelect, profileSelectPanel);
            _menu.RegisterPanel(MenuScreen.Keybindings, keybindingsPanel);
            if (accessibilityPanel != null)
                _menu.RegisterPanel(MenuScreen.Accessibility, accessibilityPanel);
            if (spiritTrialsTierSelectPanel != null)
                _menu.RegisterPanel(MenuScreen.SpiritTrialsTierSelect, spiritTrialsTierSelectPanel);
            if (zenLeaderboardPanel != null)
                _menu.RegisterPanel(MenuScreen.EndlessZenLeaderboard, zenLeaderboardPanel);

            SetupResumeButton();
        }

        // F — stored by MainMenuBlueprintBuilder after Build() to avoid FindFirstObjectByType in modals
        public void SetMainCanvas(Canvas c) => _menuCanvas = c;

        // ── Navigation ──

        public void ShowMainMenu()
        {
            _startupScreenChosen = true;
            _menu.ShowMainMenu();
            SetupResumeButton();
            RefreshClassLockStates();
            RefreshProfileSelectCards();
        }

        public void StartGame()
        {
            ClearSeed();
            _pendingLaunchMode = GameMode.GardenRun;
            RefreshClassLockStates();
            _menu.Show(MenuScreen.ClassSelect);
            SetStatus(LocalizationService.T(
                "MainMenu.Status.SelectGardenClass",
                "Select a class for Garden Run."));
        }
        public void OpenTutorial()
        {
            var meta = Profile.LoadMetaProgress();
            var unlocked = new List<ClassId>();
            var allClasses = new[]
            {
                ClassId.NumberFreak, ClassId.GardenMonk, ClassId.ShrineArchivist,
                ClassId.KoiGambler, ClassId.StoneGardener, ClassId.LanternSeer,
                ClassId.ReedDuelist, ClassId.QuietCartographer
            };
            foreach (var cls in allClasses)
                if (IsClassUnlockedOrDebug(cls, meta)) unlocked.Add(cls);
            if (unlocked.Count == 0) unlocked.Add(ClassId.NumberFreak);
            _tutorialMenuController?.RefreshClassDropdown(unlocked);
            RefreshCustomContinue();
            _menu.Show(MenuScreen.TutorialSetup);
            InRunUiFactory.SelectFirstInteractable(_tutorialSetupPanel);
        }
        public void OpenOptions() => _menu.Show(MenuScreen.Options);
        public void OpenCredits() => _menu.Show(MenuScreen.Credits);
        public void OpenItems()
        {
            var meta = Profile.LoadMetaProgress();
            if (_itemsMenuController != null) _itemsMenuController.Refresh(meta);   // no ?. on a MonoBehaviour
            _menu.Show(MenuScreen.Items);
            if (_itemsMenuController != null) _itemsMenuController.FocusFirst();   // UI-STYLE-018: start on the first tile
        }

        public void OpenMetaProgression()
        {
            var meta = Profile.LoadMetaProgress();
            if (_metaProgressionController != null)   // no ?. on a MonoBehaviour
                _metaProgressionController.Refresh(meta, Profile.LoadCompletionPercent());
            _menu.Show(MenuScreen.MetaProgression);
            if (_metaProgressionController != null) _metaProgressionController.FocusSelected();   // UI-STYLE-019
        }

        // [REQ: META-PRESTIGE-004] GAP-017: prestige a level-40 class from the Meta Progression panel.
        private void PrestigeClass(ClassId classId)
        {
            var done = Profile.TryPrestigeClass(classId);
            Debug.Log($"[MainMenu] Prestige {classId}: {(done ? "done" : "not eligible")}.");
            // The rebuilt plaques drop the selection: the focus goes back to the prestiged class (UI-STYLE-019).
            if (_metaProgressionController == null) return;
            _metaProgressionController.Refresh(Profile.LoadMetaProgress(), Profile.LoadCompletionPercent());
            _metaProgressionController.FocusSelected();
        }

        public void OpenGameModes()    => ShowGameModes();
        public void ShowGameModes()
        {
            if (_monthlyWalkPanel != null) _monthlyWalkPanel.SetActive(false);
            DropGameModesOverlays();
            RefreshGameModesPanel();
            _menu.Show(MenuScreen.GameModes);
        }
        public void OpenKeybindings()  => _menu.Show(MenuScreen.Keybindings);

        /// <summary>
        /// Forces the options panel to re-read option values and refresh all UI widgets.
        /// Called after a Reset-to-Defaults action so sliders and toggles snap to default positions.
        /// </summary>
        public void RefreshOptionsPanel() => _optionsController?.NotifyOptionsChangedForUiRefresh();

        public void BackToMainMenu() => _menu.ShowMainMenu();
        public void BackFromPanel() => _menu.Back();

        // [REQ: MENU-BACK-001] A screen of the menu's history (0.3.1 playtest): Back returns to the main menu or to Game
        // Modes, wherever the walk was opened from.
        public void ShowMonthlyWalkPanel()
        {
            if (_monthlyWalkPanel == null) return;
            DropGameModesOverlays();
            _menu.Show(MenuScreen.MonthlyWalk);
            RefreshMonthlyWalkPanel();
        }

        /// <summary>[REQ: DAILY-GOAL-004] [REQ: UI-STYLE-027] The main menu's Daily Walk tag opens the walk's curtain on Game Modes (0.3.1
        /// playtest: it opened the old goals panel).</summary>
        public void OpenDailyWalk()
        {
            ShowGameModes();
            if (_gameModesScreen != null) _gameModesScreen.Show(GameModesScreen.Mode.Daily);
        }

        /// <summary>[REQ: INPUT-PAD-005] A (or a click) on a class's seal chooses it and puts the focus on Begin
        /// (Abandon &amp; Begin), so a second A starts the run (0.3.1 playtest).</summary>
        public void ChooseClass(ClassId classId)
        {
            SetSelectedClass(classId);
            var begin = _classSelectPanel != null ? _classSelectPanel.transform.Find("ClassScroll/BtnConfirmClass") : null;
            if (begin == null && _classSelectPanel != null)
                foreach (var b in _classSelectPanel.GetComponentsInChildren<Button>(false))
                    if (b.name == "BtnConfirmClass") { begin = b.transform; break; }
            var button = begin != null ? begin.GetComponent<Button>() : null;
            if (button != null && button.interactable && button.gameObject.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(button.gameObject);
        }

        public void LaunchSeasonalChallenge()
        {
            if (_gameBootstrap == null) return;
            if (!_gameBootstrap.LaunchSeasonalChallenge())
            {
                SetStatus(LocalizationService.T(
                    "Seasonal.Status.GenerationFailed",
                    "This month's puzzle could not be prepared. Please try again."));
            }
        }

        // [REQ: SEASON-UI-001] [REQ: UI-STYLE-033] The Monthly Walk under the old tree: the month and its garden, the board and
        // its rules, the month's best with its grade and what the next grade needs, the days of the month as ink marks.
        private void RefreshMonthlyWalkPanel()
        {
            if (_monthlyWalkPanel == null) return;
            var now = DateTime.Today;
            var culture = LocalizationService.Culture;
            var cfg = SeasonalChallengeService.BuildChallengeConfig(now.Year, now.Month);
            var walker = SeasonalChallengeService.CreateChallengeRunState(now.Year, now.Month);
            var month = culture.DateTimeFormat.GetMonthName(now.Month);

            FindAndSetText(_monthlyWalkPanel, "MonthlyEyebrow",
                LocalizationService.Format("Monthly.Eyebrow", "Monthly Walk · {0}", month).ToUpper(culture));
            FindAndSetText(_monthlyWalkPanel, "MonthlyThemeLabel", SeasonalChallengeService.GetThemeName(now.Year, now.Month));
            FindAndSetText(_monthlyWalkPanel, "MonthlySubtitleLabel", LocalizationService.Format("Monthly.Facts",
                "{0}×{0}   {1}   HP {2}   Pencil {3}", cfg.BoardSize, new string('\u2605', cfg.Stars), walker.MaxHP, walker.MaxPencil));
            FillMonthlyRules(_monthlyWalkPanel.transform.Find("MonthlySlip/MonthlyRules"), cfg);

            var seasonal = (_save.HasSaveFile() ? _save.Load() : new SaveFileEnvelope()).SeasonalChallenge;
            var best = seasonal?.GetBest(now.Year, now.Month) ?? 0;
            var grade = best > 0 ? SeasonalChallengeService.GetGrade(best, true) : string.Empty;
            FindAndSetText(_monthlyWalkPanel, "MonthlyBestLabel", best > 0
                ? best.ToString("N0", culture)
                : LocalizationService.T("Monthly.NotWalked", "Not walked yet"));
            FindAndSetText(_monthlyWalkPanel, "MonthlyGrade", grade);
            var ring = _monthlyWalkPanel.transform.Find("MonthlyBestPanel/GradeRing");
            if (ring != null) ring.gameObject.SetActive(best > 0);
            FindAndSetText(_monthlyWalkPanel, "MonthlyBestBreakdownLabel", NextGradeLine(best, culture));

            FindAndSetText(_monthlyWalkPanel, "MonthName", month.ToUpper(culture));
            FillMonthlyDays(_monthlyWalkPanel.transform.Find("MonthlyDaysPaper/MonthlyDays"), now);
            FindAndSetText(_monthlyWalkPanel, "MonthlyCountdownLabel", SeasonalChallengeService.GetCountdownLabel(now) + " · "
                + LocalizationService.T("Monthly.NewGarden", "then a new garden"));
        }

        // What the next grade needs; a walk without a mistake adds 1 000.
        private static string NextGradeLine(int best, System.Globalization.CultureInfo culture)
        {
            if (best <= 0) return LocalizationService.T("Monthly.FirstWalk", "One board, the same for everyone this month.");
            foreach (var (grade, score) in new[] { ("C", 2500), ("B", 4500), ("A", 6500), ("S", 8500) })
                if (best < score)
                    return LocalizationService.Format("Monthly.NextGrade", "{0} more for {1} · no mistake adds {2}",
                        (score - best).ToString("N0", culture), grade, 1000.ToString("N0", culture));
            return LocalizationService.T("Monthly.TopGrade", "The top grade. A walk again can still raise the score.");
        }

        // One row per rule: its icon, name and text.
        private static void FillMonthlyRules(Transform area, LevelConfig cfg)
        {
            if (area == null) return;
            ScrollUiKit.DestroyChildrenNow(area);
            var rules = cfg?.ActiveModifiers ?? new List<BossModifierId>();
            var height = rules.Count > 1 ? 0.5f : 1f;
            for (var i = 0; i < rules.Count; i++)
            {
                var top = 1f - i * height;
                var rule = rules[i];
                var icon = ScrollUiKit.Icon(area, "RuleIcon_" + i, CustomPuzzleScreen.IconPath(rule),
                    new Vector2(0f, top - Mathf.Min(height, 0.42f)), new Vector2(0.11f, top), Color.white);
                icon.raycastTarget = false;
                ScrollUiKit.Label(area, "RuleName_" + i, Boss.BossService.GetModifierName(rule), 25, TextAnchor.MiddleLeft,
                    ScrollUiKit.Vermilion, new Vector2(0.14f, top - height * 0.30f), new Vector2(1f, top), 14);
                ScrollUiKit.Label(area, "RuleText_" + i, Boss.BossService.GetModifierDescription(rule), 17, TextAnchor.UpperLeft,
                    ScrollUiKit.Ink, new Vector2(0.14f, top - height * 0.98f), new Vector2(1f, top - height * 0.31f), 11);
            }
        }

        // One ink mark per day: the days gone dark, today vermilion, the days left open.
        private static void FillMonthlyDays(Transform area, DateTime today)
        {
            if (area == null) return;
            ScrollUiKit.DestroyChildrenNow(area);
            var days = DateTime.DaysInMonth(today.Year, today.Month);
            var step = 1f / days;
            for (var d = 1; d <= days; d++)
            {
                var x = (d - 1) * step;
                var past = d < today.Day;
                var mark = ScrollUiKit.Fill(area, "Day_" + d, new Vector2(x + step * 0.18f, d == today.Day ? 0f : 0.10f),
                    new Vector2(x + step * 0.82f, d == today.Day ? 1f : 0.90f),
                    d == today.Day ? ScrollUiKit.Vermilion : past ? ScrollUiKit.Ink : Color.clear, ScrollUiKit.Rounded(6f));
                mark.raycastTarget = false;
                if (d > today.Day) ScrollUiKit.InkFrame(mark.transform, ScrollUiKit.InkSoft, 1.5f);
            }
        }

        private static void FindAndSetText(GameObject panel, string childName, string text)
        {
            // Search recursively in case the label is inside a sub-container
            var all = panel.GetComponentsInChildren<Text>(true);
            foreach (var t in all)
            {
                if (t.gameObject.name == childName)
                {
                    t.text = text;
                    return;
                }
            }
        }

        // ── Profile Select ──

        public void ShowProfileSelect()
        {
            _startupScreenChosen = true;
            RefreshProfileSelectCards();
            _menu.Show(MenuScreen.ProfileSelect);
        }

        // [REQ: INPUT-PROFILE-002] SelectProfileSlot/DeleteProfileSlot/RefreshProfileSelectCards: manage 3-slot profile selection, deletion, and card display
        public void SelectProfileSlot(int slot)
        {
            _profileSlots.SelectSlot(slot);

            // Re-wire save services to new slot
            _gameBootstrap?.ApplySlotChange();
            _save = new SaveFileService(SaveProfileService.ActiveSlot);
            _profile = new ProfileService(_save);
            OptionsController.ReloadAfterProfileChange();   // UI-OPT-004

            LocalizationService.SetLanguage(Profile.LoadOptions().Language);
            if (_gameBootstrap != null)
            {
                _gameBootstrap.ContinueStartupAfterProfileSelection(this);
                return;
            }

            ShowMainMenu();

            // New profile (tutorial not yet seen) → prompt to play the intro tutorial.
            if (!HasCompletedSudokuBasics())
                ShowSudokuBasicsPrompt();
        }

        private const float DeleteConfirmSeconds = 3f;
        private int _deleteAskedSlot = -1;
        private float _deleteAskedAt = float.NegativeInfinity;

        // [REQ: UI-STYLE-034] Delete asks first, on its own button ("Sure?"), which reads Delete again after 3 s; a second
        // press within them deletes.
        public void DeleteProfileSlot(int slot)
        {
            if (_deleteAskedSlot != slot || Time.unscaledTime - _deleteAskedAt > DeleteConfirmSeconds)
            {
                _deleteAskedSlot = slot;
                _deleteAskedAt = Time.unscaledTime;
                var label = _profileSelectPanel?.transform.Find($"Content/SlotCard_{slot}/BtnDelete_{slot}/Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.text = LocalizationService.T("ProfileSelect.DeleteSure", "Sure?");
                    StartCoroutine(ResetDeleteLabel(label, slot));
                }
                return;
            }
            _deleteAskedSlot = -1;
            _profileSlots.DeleteSlot(slot);

            // Re-wire save services — ActiveSlot may have changed (e.g. slot 0 deleted → still 0 but file is gone).
            _gameBootstrap?.ApplySlotChange();
            _save = new SaveFileService(SaveProfileService.ActiveSlot);
            _profile = new ProfileService(_save);
            OptionsController.ReloadAfterProfileChange();   // UI-OPT-004

            RefreshProfileSelectCards();
            SetStatus(LocalizationService.Format("ProfileSelect.Status.Deleted", "Profile {0} deleted.", slot + 1));
        }

        private System.Collections.IEnumerator ResetDeleteLabel(Text label, int slot)
        {
            yield return new WaitForSecondsRealtime(DeleteConfirmSeconds);
            if (label != null && _deleteAskedSlot == slot) label.text = LocalizationService.T("Delete");
            if (_deleteAskedSlot == slot) _deleteAskedSlot = -1;
        }

        // [REQ: UI-STYLE-034] Each ema: the class last walked with its level, the record and a seal for a saved run, or the
        // invitation to a new garden; the active one framed in vermilion with its lantern lit.
        private void RefreshProfileSelectCards()
        {
            if (_profileSelectPanel == null) return;
            var summaries = _profileSlots.GetAllSlotSummaries();
            var content = _profileSelectPanel.transform.Find("Content");
            if (content == null) return;

            for (var i = 0; i < summaries.Length; i++)
            {
                var s = summaries[i];
                var card = content.Find($"SlotCard_{i}");
                if (card == null) continue;
                var active = i == SaveProfileService.ActiveSlot && !s.IsEmpty;
                content.Find($"SlotFrame_{i}")?.gameObject.SetActive(active);
                content.Find($"SlotGlow_{i}")?.gameObject.SetActive(active);
                content.Find($"SlotActive_{i}")?.gameObject.SetActive(active);

                var icon = card.Find("ClassDisc/ClassIcon")?.GetComponent<Image>();
                if (icon != null)
                {
                    var iconName = s.IsEmpty ? null : ClassCatalog.GetIconName(s.LastPlayedClass);
                    icon.sprite = string.IsNullOrEmpty(iconName) ? null : InRunUiFactory.LoadResourceSprite("class/icon_" + iconName);
                    icon.color = icon.sprite != null ? Color.white : Color.clear;
                }
                card.Find("ClassDisc/Plus")?.gameObject.SetActive(s.IsEmpty);
                SetText(card, "Who", s.IsEmpty ? LocalizationService.T("Profile.EmptySlot")
                    : LocalizationService.Format("ProfileSelect.Who", "{0} · Lv {1}", ClassCatalog.DisplayName(s.LastPlayedClass), s.ClassLevel));
                // A hand-edited or broken time reads as "Never" rather than stopping the menu; shown in local time.
                var lastPlayed = DateTime.TryParse(s.LastPlayedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var played)
                    ? played.ToLocalTime().ToString("d MMM", LocalizationService.Culture)
                    : LocalizationService.T("Never");
                SetText(card, $"SlotInfo_{i}", s.IsEmpty ? LocalizationService.T("ProfileSelect.NewGarden")
                    : LocalizationService.T("ProfileSelect.Rows"));
                SetText(card, "Numbers", s.IsEmpty ? string.Empty
                    : LocalizationService.Format("ProfileSelect.Numbers", "{0} · {1} won\n{2}\n{3}",
                        s.TotalRunsStarted, s.TotalRunsCompleted, s.TotalBossesDefeated, lastPlayed));
                var info = card.Find($"SlotInfo_{i}")?.GetComponent<Text>();
                if (info != null) info.alignment = s.IsEmpty ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
                card.Find("RunSeal")?.gameObject.SetActive(s.HasActiveRun);
                if (s.HasActiveRun)
                    SetText(card, "RunSeal/Text", LocalizationService.Format("ProfileSelect.RunSeal", "Run in progress · Floor {0}", s.RunFloor)
                        .ToUpper(LocalizationService.Culture));
                SetText(card, $"BtnLoad_{i}/Label", s.IsEmpty ? LocalizationService.T("ProfileSelect.Create") : LocalizationService.T("ProfileSelect.Play"));
                SetText(card, $"BtnDelete_{i}/Label", LocalizationService.T("Delete"));
                card.Find($"BtnDelete_{i}")?.gameObject.SetActive(!s.IsEmpty);
            }
            WireProfileNavigation(content, summaries.Length);
        }

        // [REQ: INPUT-NAV-001] Left and right walk the shown buttons across the plaques (an empty plaque has no Delete), down
        // goes to Back.
        private static void WireProfileNavigation(Transform content, int slots)
        {
            var row = new List<Selectable>();
            for (var i = 0; i < slots; i++)
                foreach (var name in new[] { $"SlotCard_{i}/BtnLoad_{i}", $"SlotCard_{i}/BtnDelete_{i}" })
                {
                    var button = content.Find(name)?.GetComponent<Selectable>();
                    if (button != null && button.gameObject.activeSelf) row.Add(button);
                }
            var back = content.Find("BtnProfileBack")?.GetComponent<Selectable>();
            for (var i = 0; i < row.Count; i++)
                row[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? row[i - 1] : null,
                    selectOnRight = i < row.Count - 1 ? row[i + 1] : null,
                    selectOnDown = back,
                };
            if (back != null && row.Count > 0)
                back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = row[Math.Min(row.Count - 1, 2)] };
        }

        // ── Resume ──

        public void ResumeGame()
        {
            if (_gameBootstrap == null) return;

            if (_gameBootstrap.HasResumableRun())
            {
                _gameBootstrap.LaunchResume();
            }
            else
            {
                SetStatus(LocalizationService.T(
                    "MainMenu.Status.NoRunToResume",
                    "No run to resume."));
            }
        }

        // [REQ: UI-STYLE-032] The run card shows the saved Garden Run (class, floor, garden, HP, gold, Harmony) and hides
        // without one; the profile tag top right and the Custom Puzzle's Continue pill follow the save too.
        private void SetupResumeButton()
        {
            if (_resumeButton == null) return;
            if (_gameBootstrap != null && _gameBootstrap.EndWalkOfAnotherDay())   // [REQ: DAILY-WALK-010]
                SetStatus(LocalizationService.T("MainMenu.WalkEnded"));
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var env = save.HasSaveFile() ? save.Load() : null;
            var rs = env?.ActiveRunState;
            var hasRun = save.HasActiveRun() && rs != null;   // the slot's own file, as the bootstrap's resume reads it
            _resumeButton.interactable = hasRun;
            _resumeButton.gameObject.SetActive(hasRun);
            if (hasRun) FillRunCard(_resumeButton.transform, rs);

            var card = _resumeButton.transform.parent;
            RefreshHomeProfile(card, env);
            RefreshHomeContinue(card, save);
            MainMenuBlueprintBuilder.WireHomeNavigation(card);
        }

        private static void FillRunCard(Transform runCard, RunState rs)
        {
            SetText(runCard, "RunLine", rs.DailyWalk   // [REQ: DAILY-WALK-010] a saved walk waits until midnight
                ? LocalizationService.Format("MainMenu.WalkLine", "Daily Walk · Floor {0} of {1} · continue it today",
                    rs.WalkFloor, rs.TotalFloors)
                : LocalizationService.Format("MainMenu.RunLine", "Floor {0} · {1} · {2}",
                    rs.WalkFloor, FloorThemeData.GetFloorName(rs.CurrentFloor), ClassCatalog.DisplayName(rs.ClassId)));
            SetText(runCard, "RunChips", LocalizationService.Format("MainMenu.RunChips", "HP {0}/{1} · Gold {2} · Harmony {3}",
                rs.CurrentHP, rs.MaxHP, rs.CurrentGold, rs.HarmonyLevel));
            var icon = runCard.Find("ClassDisc/ClassIcon")?.GetComponent<Image>();
            if (icon == null) return;
            var name = ClassCatalog.IsClass(rs.ClassId) ? ClassCatalog.GetIconName(rs.ClassId) : null;
            icon.sprite = string.IsNullOrEmpty(name) ? null : InRunUiFactory.LoadResourceSprite("class/icon_" + name);
            icon.color = icon.sprite != null ? Color.white : Color.clear;
        }

        // "Profile 1 · Number Freak Lv 23": the slot, and the class of the saved run or the highest level.
        private static void RefreshHomeProfile(Transform card, SaveFileEnvelope env)
        {
            var slot = SaveProfileService.ActiveSlot + 1;
            var meta = env?.MetaProgress;
            var classId = env?.ActiveRunState != null && ClassCatalog.IsClass(env.ActiveRunState.ClassId)
                ? env.ActiveRunState.ClassId : (ClassId)0;
            var garden = new Meta.ClassGardenProgressionService();
            if (classId == 0 && meta != null)
                foreach (ClassId id in Enum.GetValues(typeof(ClassId)))
                    if (classId == 0 || garden.GetLevel(meta, id) > garden.GetLevel(meta, classId)) classId = id;
            SetText(card, "HomeProfile/HomeProfileText", classId == 0 || meta == null
                ? LocalizationService.Format("MainMenu.ProfileTagEmpty", "Profile {0}", slot)
                : LocalizationService.Format("MainMenu.ProfileTag", "Profile {0} · {1} Lv {2}", slot, ClassCatalog.DisplayName(classId),
                    garden.GetLevel(meta, classId)));
        }

        // [REQ: CUSTOM-SAVE-001] Continue on the Custom Puzzle tablet with the saved board (owner, RM-209).
        private static void RefreshHomeContinue(Transform card, SaveFileService save)
        {
            var pill = card.Find("BtnTutorial/BtnHomeContinueCustom");
            if (pill == null) return;
            var saved = save.SavedCustomPuzzle();
            pill.gameObject.SetActive(saved != null);
            if (saved != null)
                SetText(pill, "Label", LocalizationService.Format("CustomPuzzle.Continue", "Continue · {0}×{0} {1}", saved.BoardSize,
                    new string('\u2605', Mathf.Clamp(saved.Stars, 1, 7))));
        }

        private static void SetText(Transform root, string path, string text)
        {
            var label = root.Find(path)?.GetComponent<Text>();
            if (label != null) label.text = text;
        }

        // ── Class Select ──

        public void RegisterClassButton(ClassId classId, Button button)
        {
            _classButtons[classId] = button;
        }

        // #6 — called by BlueprintBuilder to register each class button's lock icon overlay
        public void RegisterLockOverlay(ClassId classId, GameObject overlay)
        {
            _classLockOverlays[classId] = overlay;
        }

        // [REQ: META-UNLOCK-003] Unlocked classes can be chosen; [REQ: META-UNLOCK-004] locked ones are dark seals with their
        // unlock goal and no click (UI-STYLE-024), the LockOverlay showing.
        public void RefreshClassLockStates()
        {
            var meta = Profile.LoadMetaProgress();
            foreach (var kvp in _classButtons)
            {
                var locked = !IsClassUnlockedOrDebug(kvp.Key, meta);
                kvp.Value.interactable = !locked;
                var colors = kvp.Value.colors;
                colors.disabledColor = Color.white;   // the seal draws its locked look itself
                kvp.Value.colors = colors;
                if (_classLockOverlays.TryGetValue(kvp.Key, out var lockGo) && lockGo != null)
                    lockGo.SetActive(locked);
            }
            if (_classSelectScreen != null)
            {
                _classSelectScreen.RefreshSeals(meta, id => IsClassUnlockedOrDebug(id, meta));
                // The scroll shows the chosen class as it is now (another profile, a prestige); a class this profile has
                // not unlocked falls back to the Number Freak, which every profile has.
                if (!IsClassUnlockedOrDebug(_selectedClass, meta)) _selectedClass = ClassId.NumberFreak;
                _highlightedClassId = _selectedClass;
                _classSelectScreen.ShowClass(_selectedClass, meta, true);
            }
            RefreshHarmonyReadout();
        }

        public void SetHarmonyReadout(Text readout) => _harmonyReadout = readout;
        public void SetHarmonyDescriptionLabel(Text lbl) => _harmonyDescriptionLabel = lbl;
        public void SetHarmonyPrevClassBtn(Button btn) => _harmonyPrevClassBtn = btn;
        public void SetHarmonyNextClassBtn(Button btn) => _harmonyNextClassBtn = btn;

        public void StepHarmonyClassLevel(int delta)
        {
            var meta = Profile.LoadMetaProgress();
            _gameModesController?.StepHarmonyLevel(delta, meta);
            RefreshHarmonyReadout();
        }

        private void RefreshHarmonyReadout()
        {
            if (_gameModesController == null) return;
            var level = _gameModesController.SelectedHarmonyLevel;
            if (_harmonyReadout != null)
                _harmonyReadout.text = LocalizationService.Format("Harmony.Readout",
                    "{0}  ({1})",
                    HarmonyName(level),
                    HarmonyDifficultyService.GetHudLabel(level));
            if (_harmonyDescriptionLabel != null)
                _harmonyDescriptionLabel.text = LocalizationService.T(
                    "Harmony.Description." + level,
                    HarmonyDifficultyService.GetDescription(level));
            var meta = Profile.LoadMetaProgress();
            var maxUnlocked = _gameModesController.GetMaxUnlockedHarmonyLevel(meta);
            if (_harmonyPrevClassBtn != null) _harmonyPrevClassBtn.interactable = level > 0;
            if (_harmonyNextClassBtn != null) _harmonyNextClassBtn.interactable = level < maxUnlocked;
            var perk = _gameModesController.SelectedHarmonyPerk;
            if (_classSelectScreen != null)
                _classSelectScreen.ShowRunSettings(_pendingLaunchMode, _pendingTrialsTier, level, maxUnlocked, perk, HarmonyPerkName(perk),
                    _gameBootstrap != null && _gameBootstrap.HasResumableRun());
        }

        public void SetSelectedClass(ClassId classId)
        {
            _selectedClass = classId;
            _highlightedClassId = classId;
            if (_classSelectScreen == null) return;
            var meta = Profile.LoadMetaProgress();
            _classSelectScreen.ShowClass(classId, meta, IsClassUnlockedOrDebug(classId, meta));
        }

        public void ConfirmClassAndStart()
        {
            if (_gameBootstrap == null) return;

            var meta = Profile.LoadMetaProgress();
            if (!IsClassUnlockedOrDebug(_selectedClass, meta))
            {
                SetStatus(LocalizationService.T(
                    "MainMenu.Status.ClassLocked",
                    "Class not yet unlocked."));
                return;
            }

            var request = new LaunchRequest
            {
                ClassId = _selectedClass,
                Mode = _pendingLaunchMode,
                AllowIrregularPuzzles = _allowIrregularPuzzles
            };
            ApplyPendingSeed(request);

            if (_pendingLaunchMode == GameMode.GardenRun)
            {
                request.HarmonyLevel = _gameModesController?.SelectedHarmonyLevel ?? 0;
                request.HarmonyPerk = _gameModesController?.SelectedHarmonyPerk ?? HarmonyPerkId.None;
            }
            else if (_pendingLaunchMode == GameMode.SpiritTrials)
            {
                request.SelectedTier = _pendingTrialsTier;
            }
            else if (_pendingLaunchMode == GameMode.IncenseWalk && _incenseWeekly)
            {
                request.HasSeed = true;   // [REQ: INCENSE-BOARD-001] the week's seed: the same walk for everyone this week
                request.Seed = WeeklySeedService.SeedFor(DateTime.Today);
            }

            _gameBootstrap.LaunchRun(request);
        }

        public void SetAllowIrregularPuzzles(bool allow)
        {
            _allowIrregularPuzzles = allow;
        }

        // ── Tutorial ──

        public void StartTutorialGame()
        {
            if (_gameBootstrap == null || _tutorialMenuController == null) return;

            var setup = _tutorialMenuController.GetConfig();
            if (!TutorialModeService.TryValidateCustomSetup(setup, out var failureMessage))
            {
                SetStatus(failureMessage);
                return;
            }

            // [REQ: CUSTOM-SAVE-001] Over a saved Custom Puzzle, Start asks first: a second press within 3 s replaces it.
            if (new SaveFileService(SaveProfileService.ActiveSlot).SavedCustomPuzzle() != null
                && Time.unscaledTime - _customReplaceAskedAt > CustomReplaceSeconds)
            {
                _customReplaceAskedAt = Time.unscaledTime;
                _tutorialSetupPanel?.GetComponent<CustomPuzzleScreen>()?.ShowMessage(
                    LocalizationService.T("CustomPuzzle.ReplaceSaved", "This replaces your saved puzzle. Press Start again."));
                return;
            }

            _gameBootstrap.LaunchTutorial(setup);
        }

        private const float CustomReplaceSeconds = 3f;
        private float _customReplaceAskedAt = float.NegativeInfinity;

        /// <summary>[REQ: CUSTOM-SAVE-001] Continues the saved Custom Puzzle (RM-209).</summary>
        public void ContinueCustomPuzzle()
        {
            if (_gameBootstrap == null || _gameBootstrap.LaunchCustomResume()) return;
            SetStatus(LocalizationService.T("CustomPuzzle.ContinueFailed", "The saved puzzle could not be opened."));
            RefreshCustomContinue();
        }

        /// <summary>Shows Continue on the Custom Puzzle screen with the saved board's size, stars and rule count.</summary>
        private void RefreshCustomContinue()
        {
            var button = _tutorialSetupPanel != null ? _tutorialSetupPanel.transform.Find("BtnTutContinue") : null;
            if (button == null) return;
            var saved = new SaveFileService(SaveProfileService.ActiveSlot).SavedCustomPuzzle();
            button.gameObject.SetActive(saved != null);
            var label = button.Find("Label")?.GetComponent<Text>();
            if (saved != null && label != null)
                label.text = LocalizationService.Format("CustomPuzzle.Continue", "Continue · {0}×{0} {1}", saved.BoardSize,
                    new string('★', Mathf.Clamp(saved.Stars, 1, 7)));
        }

        // ── Game Modes ──

        public void StartEndlessZen()
        {
            var meta = Profile.LoadMetaProgress();
            if (!_debugEnableAllFeatures && !(meta?.EndlessZenUnlocked ?? false))
            {
                SetStatus(LocalizationService.T(
                    "MainMenu.Status.EndlessZenLocked",
                    "Endless Zen unlocks after 10 run starts."));
                return;
            }

            ClearSeed();
            _pendingLaunchMode = GameMode.EndlessZen;
            RefreshClassLockStates();
            _menu.Show(MenuScreen.ClassSelect);
            SetStatus(LocalizationService.T(
                "MainMenu.Status.SelectEndlessZenClass",
                "Select a class for Endless Zen."));
        }

        public void ShowSpiritTrialsTierSelect()
        {
            var stats = Profile.LoadStats();
            _trialsSelectCtrl?.Refresh(stats);
            _menu.Show(MenuScreen.SpiritTrialsTierSelect);
        }

        public void ShowZenLeaderboard()
        {
            var stats = Profile.LoadStats();
            _zenLeaderboardCtrl?.Refresh(stats);
            _menu.Show(MenuScreen.EndlessZenLeaderboard);
        }

        public void StartSpiritTrials(SpiritTrialsTier tier)
        {
            var meta = Profile.LoadMetaProgress();
            if (!_debugEnableAllFeatures && !(meta?.SpiritTrialsUnlocked ?? false))
            {
                SetStatus(LocalizationService.T(
                    "MainMenu.Status.SpiritTrialsLocked",
                    "Spirit Trials unlock after your first boss defeat."));
                return;
            }

            ClearSeed();
            _pendingLaunchMode = GameMode.SpiritTrials;
            _pendingTrialsTier = tier;
            RefreshClassLockStates();
            _menu.Show(MenuScreen.ClassSelect);
            SetStatus(LocalizationService.Format("MainMenu.Status.SelectSpiritTrialsClass",
                "Select a class for Spirit Trials ({0}).", tier));
        }

        public void StartSpiritTrials()
        {
            ShowSpiritTrialsTierSelect();
        }

        // ── Debug ──

        public void OnDebugEnableAllChanged(bool isOn)
        {
            _debugEnableAllFeatures = isOn;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugHotkeys.SetRuntimeDebugEnabled(isOn);
#endif
            SetStatus(isOn
                ? LocalizationService.T("MainMenu.Status.DebugEnabled", "Debug: All features enabled.")
                : LocalizationService.T("MainMenu.Status.DebugDisabled", "Debug mode off."));
        }

        // ── Quit ──

        private float _quitArmedUntil = -1f;

        // [REQ: UI-MENU-QUIT-001] Quit closes the game only on a second press within 3 s; the first one says so on the status
        // line. A stray press (a pad's A on the last button) no longer closes it (GAP-074).
        public void ExitGame()
        {
            if (Time.unscaledTime > _quitArmedUntil)
            {
                _quitArmedUntil = Time.unscaledTime + 3f;
                SetStatus(LocalizationService.T("MainMenu.Status.QuitConfirm", "Press Quit again to close the game."));
                return;
            }
            Debug.Log("[Quit] Quit button pressed twice.");
            QuitScreen.Show();   // [REQ: UI-STYLE-039] the garden at dusk, then the game closes
        }

        // ── Status ──

        public void SetStatus(string text)
        {
            if (_statusText != null) _statusText.text = text;
            if (OptionsController.LiveAccessibility.ScreenReaderEnabled)
                AccessibilityAnnouncementOverlay.Announce(text);
        }

        // ── Language ──

        public void RefreshLanguage()
        {
            var lang = Profile.LoadOptions().Language;
            // Rebuild menu to apply new language
            var builder = GetComponent<MainMenuBlueprintBuilder>();
            if (builder != null)
            {
                // Force rebuild with new language
                builder.ForceRebuild();
            }
        }

        // ── Helpers ──

        private bool IsClassUnlockedOrDebug(ClassId classId, MetaProgressionState meta)
        {
            if (_debugEnableAllFeatures) return true;
            if (meta?.UnlockedClasses == null) return classId == ClassId.NumberFreak;
            return meta.UnlockedClasses.Contains(classId);
        }

        // ── Sudoku Basics First-Time Tutorial ──

        public void ShowFirstRunSetupPrompt(Action onCompleted)
        {
            if (_firstRunSetupOverlay != null)
                Destroy(_firstRunSetupOverlay);

            var canvas = _menuCanvas != null ? _menuCanvas : FindFirstObjectByType<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                Debug.LogWarning("[MainMenuController] Cannot show first-run setup: menu canvas not found.");
                onCompleted?.Invoke();
                return;
            }

            var modal = FirstRunSetupModalController.Show(canvas, _save, () =>
            {
                _firstRunSetupOverlay = null;
                onCompleted?.Invoke();
            });

            _firstRunSetupOverlay = modal != null ? modal.Root : null;
        }

        public bool HasCompletedSudokuBasics()
        {
            var save = _save.HasSaveFile() ? _save.Load() : null;
            return save?.TutorialProgress?.CompletedKeys?.Contains("sudoku_basics") == true;
        }

        /// <summary>
        /// Shows the first-time "Do you want a Sudoku basics intro?" prompt over the main menu.
        /// Called from GameBootstrap after main menu is shown, if the flag is not set.
        /// </summary>
        public void ShowSudokuBasicsPrompt()
        {
            if (HasCompletedSudokuBasics()) return;

            var canvas = FindFirstObjectByType<Canvas>()?.rootCanvas;
            if (canvas == null) return;

            var shell = InRunUiFactory.CreateThemedModalShell(canvas.transform, "SudokuBasicsPrompt",
                new Vector2(0.20f, 0.28f), new Vector2(0.80f, 0.72f), "bg_tutorial_setup", 0.12f);
            var overlayRoot = shell.Root;
            var overlay = shell.Panel.gameObject;
            _sudokuBasicsOverlay = overlayRoot; // #14 - store ref for keyboard dismiss

            // Title
            var title = InRunUiFactory.CreateText(overlay.transform, "Title",
                LocalizationService.T("Welcome to Run of the Nine."),
                18, TextAnchor.UpperCenter, GamePalette.AccentGold);
            title.rectTransform.anchorMin = new Vector2(0.05f, 0.72f);
            title.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            var body = InRunUiFactory.CreateText(overlay.transform, "Body",
                LocalizationService.T("SudokuBasicsPrompt.Body"),
                14, TextAnchor.MiddleCenter, new Color(0.92f, 0.90f, 0.82f));
            body.rectTransform.anchorMin = new Vector2(0.05f, 0.40f);
            body.rectTransform.anchorMax = new Vector2(0.95f, 0.70f);
            body.rectTransform.offsetMin = body.rectTransform.offsetMax = Vector2.zero;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;

            // "Yes, show me" button
            var yesBtn = InRunUiFactory.CreateActionButton(overlay.transform, "BtnYes",
                new Vector2(0.08f, 0.08f), new Vector2(0.46f, 0.32f), LocalizationService.T("Yes, show me"), UiAction.Start);
            ConfigureModalActionLabel(yesBtn);
            yesBtn.onClick.AddListener(() =>
            {
                _sudokuBasicsOverlay = null;
                Destroy(overlayRoot);
                MarkSudokuBasicsComplete();
                LaunchSudokuBasicsTutorial();
            });

            // "Skip" button
            var skipBtn = InRunUiFactory.CreateActionButton(overlay.transform, "BtnSkip",
                new Vector2(0.54f, 0.08f), new Vector2(0.92f, 0.32f), LocalizationService.T("Skip"), UiAction.Skip);
            ConfigureModalActionLabel(skipBtn);
            skipBtn.onClick.AddListener(() =>
            {
                _sudokuBasicsOverlay = null;
                Destroy(overlayRoot);
                MarkSudokuBasicsComplete();
            });
        }

        public void LaunchSudokuBasicsTutorial()
        {
            if (_gameBootstrap == null) return;
            _gameBootstrap.LaunchSudokuBasicsTutorial();
        }

        // [REQ: TUTO-BASICS-DONE-001] Writes "sudoku_basics" to CompletedKeys and saves — prevents re-trigger
        private void MarkSudokuBasicsComplete()
        {
            var envelope = _save.HasSaveFile() ? _save.Load() : new SaveFileEnvelope();
            if (envelope.TutorialProgress == null) envelope.TutorialProgress = new TutorialProgressState();
            if (!envelope.TutorialProgress.CompletedKeys.Contains("sudoku_basics"))
                envelope.TutorialProgress.CompletedKeys.Add("sudoku_basics");
            _save.Save(envelope);
        }

    }
}
