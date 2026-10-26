using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
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
        private GameObject _dailyWalkPanel;
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
            InputRemapService.UpdateCursorVisibility();


            // MM-2/MM-6: axis-driven scroll for scrollable panels (runs every frame, before anyKeyDown guard)
            ScrollActivePanelIfNeeded();

            _inputRemap.Tick();

            // Run every frame so D-pad axis input can navigate without a button press first.
            EnsureControllerSelection();
            HandleMenuGamepadNavigation();

            if (!Input.anyKeyDown) return;

            if (_firstRunSetupOverlay != null && _firstRunSetupOverlay.activeSelf)
            {
                if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)) || Input.GetKeyDown(KeyCode.Return))
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
                if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)) || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton1)) || Input.GetKeyDown(KeyCode.Escape))
                {
                    ExecuteMainMenuCancelRoute(CancelRouteAction.DismissSudokuBasicsOverlay);
                    return;
                }
            }

            // B button / Escape = back/cancel  (X-1: Escape now handles class select + all panels)
            if (TryHandleMainMenuCancelInput())
                return;

            // A (JoystickButton0) / R3 (JoystickButton9, legacy alias) = confirm selected element (G1-A)
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)) || Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton9)))
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
            if (!Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton1)) && !Input.GetKeyDown(KeyCode.Escape))
                return false;

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
            GameObject dailyWalkPanel = null, GameObject monthlyWalkPanel = null,
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
            _dailyWalkPanel = dailyWalkPanel;
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
            if (_dailyWalkPanel   != null) _dailyWalkPanel.SetActive(false);
            if (_monthlyWalkPanel != null) _monthlyWalkPanel.SetActive(false);
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

        // [REQ: DAILY-GOAL-004] Daily Walk widget: shows today's 3 goals, streak, InkStamp count, reset countdown
        public void ShowDailyWalkPanel()
        {
            HideAllPanels();
            if (_dailyWalkPanel != null)
            {
                _dailyWalkPanel.SetActive(true);
                RefreshDailyWalkPanel();
            }
        }

        public void ShowMonthlyWalkPanel()
        {
            HideAllPanels();
            if (_monthlyWalkPanel != null)
            {
                _monthlyWalkPanel.SetActive(true);
                RefreshMonthlyWalkPanel();
            }
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

        private void HideAllPanels()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_classSelectPanel != null) _classSelectPanel.SetActive(false);
            if (_gameModesPanel != null) _gameModesPanel.SetActive(false);
            if (_dailyWalkPanel != null) _dailyWalkPanel.SetActive(false);
            if (_monthlyWalkPanel != null) _monthlyWalkPanel.SetActive(false);
        }

        private void RefreshDailyWalkPanel()
        {
            if (_dailyWalkPanel == null) return;
            var envelope = _save.HasSaveFile() ? _save.Load() : new SaveFileEnvelope();
            var goals = envelope.DailyGoals ?? new DailyGoalState();

            // Ensure goals are up-to-date for today before displaying
            DailyGoalService.RefreshIfNewDay(goals, DateTime.Today);

            var defs = DailyGoalService.GetTodayGoals(goals);
            var completedCount = 0;
            for (var i = 0; i < 3; i++) if (goals.TodayGoalCompleted[i]) completedCount++;

            var tomorrow = DateTime.Today.AddDays(1);
            var resetIn = tomorrow - DateTime.Now;
            var resetLabel = LocalizationService.Format(
                "Challenge.Daily.ResetIn",
                "Resets in {0:D2}:{1:D2}",
                (int)resetIn.TotalHours,
                resetIn.Minutes);

            FindAndSetText(_dailyWalkPanel, "StreakLabel",
                LocalizationService.FormatPlural(
                    goals.CurrentStreak,
                    "Challenge.Daily.Streak.One",
                    "Challenge.Daily.Streak.Many",
                    "Streak: {0} day",
                    "Streak: {0} days"));
            FindAndSetText(_dailyWalkPanel, "StampsLabel",
                LocalizationService.Format(
                    "Challenge.Daily.StampsSummary",
                    "Ink Stamps: {0}  -  {1}/3 today  -  {2}",
                    goals.TotalInkStamps,
                    completedCount,
                    resetLabel));

            for (var i = 0; i < 3; i++)
            {
                var done = goals.TodayGoalCompleted[i];
                var tierTag = defs[i].Tier switch
                {
                    DailyGoalService.GoalTier.Easy   => LocalizationService.T("Challenge.Daily.Tier.Easy", "[Easy]   "),
                    DailyGoalService.GoalTier.Medium => LocalizationService.T("Challenge.Daily.Tier.Medium", "[Medium] "),
                    DailyGoalService.GoalTier.Hard   => LocalizationService.T("Challenge.Daily.Tier.Hard", "[Hard]   "),
                    _                                => ""
                };
                var statusMark = done ? "\u2713 " : "\u25cb ";
                var rewardPart = done
                    ? ""
                    : LocalizationService.Format(
                        "Challenge.Daily.GoalRewardSuffix",
                        "  -> {0}",
                        DailyGoalService.GetRewardDescription(defs[i]));
                FindAndSetText(_dailyWalkPanel, $"Goal{i}",
                    $"{statusMark}{tierTag}{DailyGoalService.GetGoalDescription(defs[i])}{rewardPart}");
            }
        }

        // [REQ: SEASON-UI-001] Monthly Walk panel: prominent main menu panel showing theme, personal best, countdown-to-reset
        private void RefreshMonthlyWalkPanel()
        {
            if (_monthlyWalkPanel == null) return;
            var now = System.DateTime.Today;
            var theme = SeasonalChallengeService.GetThemeName(now.Year, now.Month);
            var cfg = SeasonalChallengeService.BuildChallengeConfig(now.Year, now.Month);
            var subtitle = SeasonalChallengeService.GetPanelSubtitle(cfg);
            var countdown = SeasonalChallengeService.GetCountdownLabel(now);

            FindAndSetText(_monthlyWalkPanel, "MonthlyThemeLabel", theme);
            FindAndSetText(_monthlyWalkPanel, "MonthlySubtitleLabel", subtitle);
            FindAndSetText(_monthlyWalkPanel, "MonthlyCountdownLabel", countdown);

            var envelope = _save.HasSaveFile() ? _save.Load() : new SaveFileEnvelope();
            var seasonal = envelope.SeasonalChallenge;
            var best = seasonal?.GetBest(now.Year, now.Month) ?? 0;

            FindAndSetText(_monthlyWalkPanel, "MonthlyBestLabel",
                best > 0
                    ? LocalizationService.Format("Challenge.Monthly.PersonalBest", "Personal Best: {0:N0}", best)
                    : LocalizationService.T("Challenge.Monthly.PersonalBest.None", "Personal Best: -"));

            if (best > 0 && seasonal != null
                && seasonal.TryGetBestBreakdown(now.Year, now.Month,
                    out _, out var hp, out var pencilUsed, out var timeSeconds))
            {
                var hpPart = hp >= 0
                    ? LocalizationService.Format("Challenge.Monthly.Best.Hp", "HP: {0}", hp)
                    : LocalizationService.T("Challenge.Monthly.Best.Hp.None", "HP: -");
                var pencilPart = pencilUsed >= 0
                    ? LocalizationService.Format("Challenge.Monthly.Best.PencilUsed", "Pencil used: {0}", pencilUsed)
                    : LocalizationService.T("Challenge.Monthly.Best.PencilUsed.None", "Pencil used: -");
                var timePart = timeSeconds >= 0
                    ? LocalizationService.Format(
                        "Challenge.Monthly.Best.Time",
                        "Time: {0}",
                        $"{timeSeconds / 60:00}:{timeSeconds % 60:00}")
                    : LocalizationService.T("Challenge.Monthly.Best.Time.None", "Time: -");

                FindAndSetText(_monthlyWalkPanel, "MonthlyBestBreakdownLabel",
                    LocalizationService.Format(
                        "Challenge.Monthly.BestBreakdown",
                        "{0}  -  {1}  -  {2}",
                        hpPart,
                        pencilPart,
                        timePart));
            }
            else
            {
                FindAndSetText(_monthlyWalkPanel, "MonthlyBestBreakdownLabel", "");
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

        public void DeleteProfileSlot(int slot)
        {
            _profileSlots.DeleteSlot(slot);

            // Re-wire save services — ActiveSlot may have changed (e.g. slot 0 deleted → still 0 but file is gone).
            _gameBootstrap?.ApplySlotChange();
            _save = new SaveFileService(SaveProfileService.ActiveSlot);
            _profile = new ProfileService(_save);

            RefreshProfileSelectCards();
            SetStatus(LocalizationService.Format("ProfileSelect.Status.Deleted", "Profile {0} deleted.", slot + 1));
        }

        private void RefreshProfileSelectCards()
        {
            if (_profileSelectPanel == null) return;
            var summaries = _profileSlots.GetAllSlotSummaries();

            for (var i = 0; i < summaries.Length; i++)
            {
                var s = summaries[i];
                var infoName = $"SlotInfo_{i}";
                var infoT = _profileSelectPanel.transform.Find($"Content/SlotCard_{i}/{infoName}");
                if (infoT == null) continue;

                var txt = infoT.GetComponent<Text>();
                if (txt == null) continue;

                if (s.IsEmpty)
                {
                    txt.text = LocalizationService.T("Profile.EmptySlot");
                }
                else
                {
                    var runInfo = s.HasActiveRun ? " (" + LocalizationService.T("run in progress") + ")" : "";
                    var lastPlayed = string.IsNullOrEmpty(s.LastPlayedUtc) ? LocalizationService.T("Never")
                        : System.DateTime.Parse(s.LastPlayedUtc).ToString("yyyy-MM-dd");
                    txt.text = LocalizationService.Format("Profile.SlotInfo",
                        "Class: {0}\nRuns: {1} started / {2} won\nBosses: {3}\nLast played: {4}{5}",
                        ClassCatalog.DisplayName(s.LastPlayedClass),
                        s.TotalRunsStarted,
                        s.TotalRunsCompleted,
                        s.TotalBossesDefeated,
                        lastPlayed,
                        runInfo);
                }

                // Mark active slot with gold outline on the card
                var card = _profileSelectPanel.transform.Find($"Content/SlotCard_{i}");
                if (card == null) continue;
                var outline = card.GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = (i == SaveProfileService.ActiveSlot)
                        ? GamePalette.AccentGold
                        : GamePalette.ButtonOutline;
            }
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

        private void SetupResumeButton()
        {
            var hasRun = _gameBootstrap != null && _gameBootstrap.HasResumableRun();
            if (_resumeButton == null) return;

            // M-7 — Grey and relabel when no active run
            _resumeButton.interactable = hasRun;
            var lbl = _resumeButton.transform.Find("Label")?.GetComponent<Text>();
            if (!hasRun)
            {
                if (lbl != null)
                {
                    // [UX-FIX] Show plain "Resume" (greyed) when no run is active — no run details.
                    lbl.text  = LocalizationService.T("Resume");
                    lbl.color = GamePalette.WithAlpha(GamePalette.AccentGold, 0.35f);
                }
                return;
            }

            // Restore full label opacity for an active run
            if (lbl != null)
                lbl.color = GamePalette.AccentGold;

            // N-1 — Show run summary in the Resume button label
            var env = _save.HasSaveFile() ? _save.Load() : null;
            var rs = env?.ActiveRunState;
            if (rs != null && lbl != null)
                lbl.text = LocalizationService.Format("Resume.Button.Active",
                    "Resume  -  {0}  -  Floor {1}  -  HP {2}/{3}",
                    ClassCatalog.DisplayName(rs.ClassId), // GAP-019: class name was English; the Daily Walk's walker
                    rs.CurrentFloor + 1,
                    rs.CurrentHP,
                    rs.MaxHP);
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
            RefreshGameModesHarmony(meta);
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

            _gameBootstrap.LaunchTutorial(setup);
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

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
