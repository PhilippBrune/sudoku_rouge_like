using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuController : MonoBehaviour
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
        private bool _allowIrregularPuzzles = false;
        private bool _debugEnableAllFeatures;
        private GameMode _pendingLaunchMode = GameMode.GardenRun;
        private SpiritTrialsTier _pendingTrialsTier = SpiritTrialsTier.Apprentice;

        // ── Services ──
        private MenuFlowService _menu;
        private SaveFileService _save;
        private ProfileService _profile;
        private SaveProfileService _profileSlots;
        private ClassUnlockService _classUnlockService;
        private InputRemapService _inputRemap;
        public  InputRemapService  InputRemap => _inputRemap;

        // ── Canvas reference (F) ──
        private Canvas _menuCanvas;

        // ── UI Widgets ──
        private Text _statusText;
        private Button _resumeButton;
        private Text _classInfoText;
        private Text _harmonyReadout;
        private Text _harmonyDescriptionLabel;
        private Button _harmonyPrevClassBtn;
        private Button _harmonyNextClassBtn;
        private readonly Dictionary<ClassId, Button> _classButtons = new Dictionary<ClassId, Button>();
        private readonly Dictionary<ClassId, GameObject> _classLockOverlays = new Dictionary<ClassId, GameObject>();
        private ClassId? _highlightedClassId;
        private GameObject _startRunConfirmOverlay;   // #8 modal
        private GameObject _sudokuBasicsOverlay;      // #14 dismiss
        private GameObject _firstRunSetupOverlay;
        private GameObject _classLockTooltipPanel;    // C-3 lock tooltip
        private Text       _classLockTooltipText;

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

        private void Awake()
        {
            _inputRemap = new InputRemapService();
            _menu = new MenuFlowService();
            _menu.OnPanelShown += SelectFirstInPanel;
            _profileSlots = new SaveProfileService();
            _save = new SaveFileService(SaveProfileService.ActiveSlot);
            _profile = new ProfileService(_save);
            _classUnlockService = new ClassUnlockService();
        }

        private void Start()
        {
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

            if (!Input.anyKeyDown) return;

            if (_firstRunSetupOverlay != null && _firstRunSetupOverlay.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return))
                {
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    var selected = es?.currentSelectedGameObject;
                    var btn = selected != null ? selected.GetComponent<Button>() : null;
                    if (btn != null && btn.interactable)
                        btn.onClick.Invoke();
                }
                return;
            }

            // #14 — B/Escape dismisses the first-run Sudoku basics onboarding overlay
            if (_sudokuBasicsOverlay != null && _sudokuBasicsOverlay.activeSelf)
            {
                // MM-5: A/Enter confirms (same action as B/Escape — both dismiss the basics prompt)
                if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    ExecuteMainMenuCancelRoute(CancelRouteAction.DismissSudokuBasicsOverlay);
                    return;
                }
            }

            // #8 — B/Escape dismisses the Start Run confirmation modal
            if (_startRunConfirmOverlay != null && _startRunConfirmOverlay.activeSelf)
            {
                // MM-5: A/Enter confirms (triggers the Start action); B/Escape cancels
                if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return))
                {
                    var confirmBtn = _startRunConfirmOverlay.GetComponentInChildren<Button>();
                    if (confirmBtn != null && confirmBtn.interactable)
                        confirmBtn.onClick.Invoke();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    ExecuteMainMenuCancelRoute(CancelRouteAction.DismissStartRunConfirmation);
                    return;
                }
            }

            // B button / Escape = back/cancel  (X-1: Escape now handles class select + all panels)
            if (TryHandleMainMenuCancelInput())
                return;

            // A (JoystickButton0) / R3 (JoystickButton9, legacy alias) = confirm selected button (G1-A)
            if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton9))
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                var sel = es?.currentSelectedGameObject;
                if (sel != null)
                {
                    var btn = sel.GetComponent<Button>();
                    if (btn != null && btn.interactable)
                        btn.onClick.Invoke();
                }
                return;
            }

            // Ensure an EventSystem selection exists so the stick can navigate.
            // We set the first interactable button in the active panel whenever selection is lost.
            EnsureControllerSelection();
        }

        private bool TryHandleMainMenuCancelInput()
        {
            if (!Input.GetKeyDown(KeyCode.JoystickButton1) && !Input.GetKeyDown(KeyCode.Escape))
                return false;

            ExecuteMainMenuCancelRoute(CancelRouteContract.ResolveMainMenuCancel(
                new MainMenuCancelContext(
                    IsActive(_firstRunSetupOverlay),
                    IsActive(_sudokuBasicsOverlay),
                    IsActive(_startRunConfirmOverlay),
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
                case CancelRouteAction.DismissStartRunConfirmation:
                    DismissStartRunConfirmation();
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

        private void DismissStartRunConfirmation()
        {
            if (_startRunConfirmOverlay == null) return;
            Destroy(_startRunConfirmOverlay);
            _startRunConfirmOverlay = null;
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
            _menu.ShowMainMenu();
            SetupResumeButton();
            RefreshClassLockStates();
            RefreshProfileSelectCards();
        }

        public void StartGame()
        {
            _pendingLaunchMode = GameMode.GardenRun;
            RefreshClassLockStates();
            _menu.Show(MenuScreen.ClassSelect);
            SetStatus(LocalizationService.T(
                "MainMenu.Status.SelectGardenClass",
                "Select a class for Garden Run."));
        }
        public void OpenTutorial()
        {
            var meta = _profile.LoadMetaProgress();
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
            var meta = _profile.LoadMetaProgress();
            _itemsMenuController?.Refresh(meta);
            _menu.Show(MenuScreen.Items);
        }

        public void OpenMetaProgression()
        {
            var meta = _profile.LoadMetaProgress();
            _metaProgressionController?.Refresh(meta);
            _menu.Show(MenuScreen.MetaProgression);
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
        public void OpenAccessibility() => _menu.Show(MenuScreen.Accessibility);

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
            _gameBootstrap.LaunchSeasonalChallenge();
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

            LocalizationService.SetLanguage(_profile.LoadOptions().Language);
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
                        s.LastPlayedClass,
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
                    ClassCatalog.GetDefinition(rs.ClassId).Name,
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

        // [REQ: META-UNLOCK-003] RefreshClassLockStates: unlocked classes shown fully interactive with normal colours
        // [REQ: META-UNLOCK-004] Locked classes are greyed out (button non-interactive, lock icon overlay visible, muted text colour)
        public void RefreshClassLockStates()
        {
            var meta = _profile.LoadMetaProgress();
            foreach (var kvp in _classButtons)
            {
                var locked = !IsClassUnlockedOrDebug(kvp.Key, meta);
                // C-3 — Block interaction on locked buttons
                kvp.Value.interactable = !locked;
                var img = kvp.Value.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                    img.color = locked ? new Color(0.82f, 0.75f, 0.62f, 0.92f) : new Color(0.40f, 0.37f, 0.32f, 0.80f);
                var lbl = kvp.Value.transform.Find("Label")?.GetComponent<Text>();
                if (lbl != null)
                    lbl.color = locked ? new Color(0.12f, 0.10f, 0.08f, 1f) : new Color(0.65f, 0.60f, 0.50f, 1f);

                // #6 — show/hide lock icon overlay
                if (_classLockOverlays.TryGetValue(kvp.Key, out var lockGo) && lockGo != null)
                    lockGo.SetActive(locked);
            }
            RefreshHarmonyReadout();
        }

        public void SetHarmonyReadout(Text readout) => _harmonyReadout = readout;
        public void SetHarmonyDescriptionLabel(Text lbl) => _harmonyDescriptionLabel = lbl;
        public void SetHarmonyPrevClassBtn(Button btn) => _harmonyPrevClassBtn = btn;
        public void SetHarmonyNextClassBtn(Button btn) => _harmonyNextClassBtn = btn;

        public void StepHarmonyClassLevel(int delta)
        {
            var meta = _profile.LoadMetaProgress();
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
            var meta = _profile.LoadMetaProgress();
            var maxUnlocked = _gameModesController.GetMaxUnlockedHarmonyLevel(meta);
            if (_harmonyPrevClassBtn != null) _harmonyPrevClassBtn.interactable = level > 0;
            if (_harmonyNextClassBtn != null) _harmonyNextClassBtn.interactable = level < maxUnlocked;
        }

        // C-3 — Hover tooltip showing unlock condition for locked class buttons
        public void ShowClassLockTooltip(ClassId classId, Vector2 screenPos)
        {
            var meta = _profile.LoadMetaProgress();
            var isUnlocked = IsClassUnlockedOrDebug(classId, meta);

            // [UX-FIX] Never show unlock progress tooltip for classes the player already has.
            if (isUnlocked) return;

            var progress = MainMenuClassInfoPresenter.GetUnlockProgress(classId, meta);

            // Only show tooltip if there is unlock progress to display
            if (progress == null) return;

            // Lazy-create the tooltip panel on first use
            if (_classLockTooltipPanel == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null) return;
                _classLockTooltipPanel = new GameObject("ClassLockTooltip",
                    typeof(RectTransform), typeof(UnityEngine.UI.Image));
                _classLockTooltipPanel.transform.SetParent(canvas.transform, false);
                var rt = _classLockTooltipPanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(240f, 52f);
                _classLockTooltipPanel.GetComponent<UnityEngine.UI.Image>().color =
                    new Color(0.05f, 0.06f, 0.10f, 0.96f);
                _classLockTooltipText = InRunUiFactory.CreateText(
                    _classLockTooltipPanel.transform, "LockTipText", "",
                    10, TextAnchor.UpperLeft, new Color(0.90f, 0.87f, 0.72f, 1f));
                var textRt = _classLockTooltipText.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(6f, 4f);
                textRt.offsetMax = new Vector2(-6f, -4f);
                _classLockTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _classLockTooltipText.verticalOverflow   = VerticalWrapMode.Overflow;
            }

            _classLockTooltipText.text = isUnlocked ? progress + " \u2713" : progress;

            // Use the canvas's own RectTransform — transform.root may be a non-UI scene root
            var parentCanvas = _classLockTooltipPanel.GetComponentInParent<Canvas>();
            var canvasRt = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;
            if (canvasRt == null) { _classLockTooltipPanel.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, null, out var local);
            var tipRt = _classLockTooltipPanel.GetComponent<RectTransform>();
            const float TipW = 220f, TipH = 52f, TipOff = 14f;
            var halfW = canvasRt.rect.width  * 0.5f;
            var halfH = canvasRt.rect.height * 0.5f;
            var tx = local.x + TipOff;
            var ty = local.y + TipOff;
            if (tx + TipW >  halfW) tx = local.x - TipW - TipOff;
            if (ty - TipH < -halfH) ty = local.y + TipH + TipOff;
            tx = Mathf.Clamp(tx, -halfW,         halfW - TipW);
            ty = Mathf.Clamp(ty, -halfH + TipH,  halfH);
            tipRt.anchoredPosition = new Vector2(tx, ty);
            _classLockTooltipPanel.SetActive(true);
            _classLockTooltipPanel.transform.SetAsLastSibling();
        }

        public void HideClassLockTooltip()
        {
            if (_classLockTooltipPanel != null) _classLockTooltipPanel.SetActive(false);
        }

        public void SetClassInfoText(Text infoText)
        {
            _classInfoText = infoText;
        }

        public void SetSelectedClass(ClassId classId)
        {
            _selectedClass = classId;
            _highlightedClassId = classId;

            // Update button outlines
            var goldColor = new Color(1f, 0.84f, 0f, 1f);
            var defaultColor = new Color(0f, 0f, 0f, 0f);

            foreach (var kvp in _classButtons)
            {
                var outline = kvp.Value.GetComponent<Outline>();
                if (outline == null)
                    outline = kvp.Value.gameObject.AddComponent<Outline>();

                if (kvp.Key == classId)
                {
                    outline.effectColor = goldColor;
                    outline.effectDistance = new Vector2(2, -2);
                    outline.enabled = true;
                }
                else
                {
                    outline.effectColor = defaultColor;
                    outline.enabled = false;
                }
            }

            // Update class info text
            if (_classInfoText != null)
            {
                var meta = _profile.LoadMetaProgress();
                var isUnlocked = IsClassUnlockedOrDebug(classId, meta);
                var view = MainMenuClassInfoPresenter.Build(classId, meta, isUnlocked);

                _classInfoText.supportRichText = true;
                _classInfoText.color = view.Color;
                _classInfoText.text = view.Text;
            }
        }

        public void ConfirmClassAndStart()
        {
            if (_gameBootstrap == null) return;

            var meta = _profile.LoadMetaProgress();
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

            if (_pendingLaunchMode == GameMode.GardenRun)
            {
                request.HarmonyLevel = _gameModesController?.SelectedHarmonyLevel ?? 0;
                request.HarmonyPerk = _gameModesController?.SelectedHarmonyPerk ?? HarmonyPerkId.None;
            }
            else if (_pendingLaunchMode == GameMode.SpiritTrials)
            {
                request.SelectedTier = _pendingTrialsTier;
            }

            _gameBootstrap.LaunchRun(request);
        }

        // #8 — Shows a confirmation modal before launching; called by Start Run button
        public void ShowStartRunModal()
        {
            if (_gameBootstrap == null) return;

            var meta = _profile.LoadMetaProgress();
            if (!IsClassUnlockedOrDebug(_selectedClass, meta))
            {
                SetStatus(LocalizationService.T(
                    "MainMenu.Status.ClassLocked",
                    "Class not yet unlocked."));
                return;
            }

            if (_startRunConfirmOverlay != null) Destroy(_startRunConfirmOverlay);

            // F — use stored canvas ref to avoid slow scene-wide search
            var canvas = _menuCanvas != null ? _menuCanvas : FindFirstObjectByType<Canvas>();
            if (canvas == null) { ConfirmClassAndStart(); return; }

            var shell = InRunUiFactory.CreateThemedModalShell(canvas.transform, "StartRunModal",
                new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.72f), "bg_class_select", 0.12f);
            var overlayRoot = shell.Root;
            var overlay = shell.Panel.gameObject;
            _startRunConfirmOverlay = overlayRoot;

            // F / 6 — fade in + pop scale on the modal
            var overlayCg = overlayRoot.AddComponent<CanvasGroup>();
            overlayCg.alpha = 0f;
            StartCoroutine(AnimationHelper.FadeIn(overlayCg, 0.15f));
            StartCoroutine(AnimationHelper.PulseScale(overlay.transform, 1f, 1.04f, 0.15f));

            var def = ClassCatalog.GetDefinition(_selectedClass);

            // Class icon
            var iconSprite = Resources.Load<Sprite>("class/icon_" + ClassCatalog.GetIconName(_selectedClass));
            if (iconSprite != null)
            {
                var iconGo = new GameObject("ClassIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(overlay.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.38f, 0.72f);
                iconRt.anchorMax = new Vector2(0.62f, 0.96f);
                iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Class name
            var nameTxt = InRunUiFactory.CreateText(overlay.transform, "ClassName",
                LocalizationService.T(def.Name), 22, TextAnchor.UpperCenter, GamePalette.AccentGold);
            nameTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.60f);
            nameTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.76f);
            nameTxt.rectTransform.offsetMin = nameTxt.rectTransform.offsetMax = Vector2.zero;
            nameTxt.fontStyle = FontStyle.Bold;

            // Passive description
            var passiveTxt = InRunUiFactory.CreateText(overlay.transform, "Passive",
                MainMenuClassInfoPresenter.GetClassPassive(def), 12, TextAnchor.UpperCenter,
                new Color(0.90f, 0.87f, 0.72f, 1f));
            passiveTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.38f);
            passiveTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.62f);
            passiveTxt.rectTransform.offsetMin = passiveTxt.rectTransform.offsetMax = Vector2.zero;
            passiveTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Stats line
            var statsTxt = InRunUiFactory.CreateText(overlay.transform, "Stats",
                LocalizationService.Format("RunConfirm.StatsLine", "HP: {0}  -  Pencil: {1}  -  Item Slots: {2}", def.BaseHP, def.BasePencil, def.BaseItemSlots),
                11, TextAnchor.UpperCenter, new Color(0.75f, 0.72f, 0.58f, 1f));
            statsTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.28f);
            statsTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.40f);
            statsTxt.rectTransform.offsetMin = statsTxt.rectTransform.offsetMax = Vector2.zero;

            var modeLabel = _pendingLaunchMode switch
            {
                GameMode.EndlessZen => LocalizationService.T("RunConfirm.Mode.EndlessZen", "Mode: Endless Zen"),
                GameMode.SpiritTrials => LocalizationService.Format("RunConfirm.Mode.SpiritTrials", "Mode: Spirit Trials  -  Tier: {0}", _pendingTrialsTier),
                _ => LocalizationService.Format("RunConfirm.Mode.GardenRun", "Mode: Garden Run  -  Harmony: {0}", HarmonyName(_gameModesController?.SelectedHarmonyLevel ?? 0))
            };
            var modeTxt = InRunUiFactory.CreateText(overlay.transform, "ModeState",
                modeLabel, 10, TextAnchor.UpperCenter, new Color(0.86f, 0.82f, 0.66f, 1f));
            modeTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.22f);
            modeTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.30f);
            modeTxt.rectTransform.offsetMin = modeTxt.rectTransform.offsetMax = Vector2.zero;

            // C-4 — Show irregular toggle state in modal
            var irregularLine = LocalizationService.T(_allowIrregularPuzzles ? "RunConfirm.Irregular.On" : "RunConfirm.Irregular.Off", _allowIrregularPuzzles ? "Irregular puzzles: ON" : "Irregular puzzles: off");
            var irregularTxt = InRunUiFactory.CreateText(overlay.transform, "IrregularState",
                irregularLine, 10, TextAnchor.UpperCenter,
                _allowIrregularPuzzles
                    ? new Color(0.90f, 0.75f, 0.30f, 1f)
                    : new Color(0.60f, 0.58f, 0.48f, 0.70f));
            irregularTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.16f);
            irregularTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.23f);
            irregularTxt.rectTransform.offsetMin = irregularTxt.rectTransform.offsetMax = Vector2.zero;

            // "Begin" button — shifted down to leave room for C-4 irregular line
            var beginBtn = InRunUiFactory.CreateActionButton(overlay.transform, "BtnBegin",
                new Vector2(0.08f, 0.03f), new Vector2(0.46f, 0.20f), LocalizationService.T("Begin"), UiAction.Begin);
            var beginLbl = beginBtn.transform.Find("Label")?.GetComponent<Text>();
            if (beginLbl != null) beginLbl.color = GamePalette.AccentGold;
            beginBtn.onClick.AddListener(() =>
            {
                Destroy(overlayRoot);
                _startRunConfirmOverlay = null;
                ConfirmClassAndStart();
            });

            // "Back" button
            var backBtn = InRunUiFactory.CreateActionButton(overlay.transform, "BtnBack",
                new Vector2(0.54f, 0.03f), new Vector2(0.92f, 0.20f), LocalizationService.T("Back"), UiAction.Back);
            backBtn.onClick.AddListener(() =>
            {
                Destroy(overlayRoot);
                _startRunConfirmOverlay = null;
            });

            // C1 — explicit D-pad nav between Begin ↔ Back, and focus Begin immediately on open
            var beginNav = beginBtn.navigation;
            beginNav.mode = Navigation.Mode.Explicit;
            beginNav.selectOnRight = backBtn;
            beginBtn.navigation = beginNav;

            var backNav = backBtn.navigation;
            backNav.mode = Navigation.Mode.Explicit;
            backNav.selectOnLeft = beginBtn;
            backBtn.navigation = backNav;

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(beginBtn.gameObject);
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
            var meta = _profile.LoadMetaProgress();
            if (!_debugEnableAllFeatures && !(meta?.EndlessZenUnlocked ?? false))
            {
                SetStatus(LocalizationService.T(
                    "MainMenu.Status.EndlessZenLocked",
                    "Endless Zen unlocks after 10 run starts."));
                return;
            }

            _pendingLaunchMode = GameMode.EndlessZen;
            RefreshClassLockStates();
            _menu.Show(MenuScreen.ClassSelect);
            SetStatus(LocalizationService.T(
                "MainMenu.Status.SelectEndlessZenClass",
                "Select a class for Endless Zen."));
        }

        public void ShowSpiritTrialsTierSelect()
        {
            var stats = _profile?.LoadStats();
            _trialsSelectCtrl?.Refresh(stats);
            _menu.Show(MenuScreen.SpiritTrialsTierSelect);
        }

        public void ShowZenLeaderboard()
        {
            var stats = _profile?.LoadStats();
            _zenLeaderboardCtrl?.Refresh(stats);
            _menu.Show(MenuScreen.EndlessZenLeaderboard);
        }

        public void StartSpiritTrials(SpiritTrialsTier tier)
        {
            var meta = _profile.LoadMetaProgress();
            if (!_debugEnableAllFeatures && !(meta?.SpiritTrialsUnlocked ?? false))
            {
                SetStatus(LocalizationService.T(
                    "MainMenu.Status.SpiritTrialsLocked",
                    "Spirit Trials unlock after your first boss defeat."));
                return;
            }

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
            var lang = _profile.LoadOptions().Language;
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

        private void RefreshGameModesPanel()
        {
            var meta = _profile.LoadMetaProgress();
            _gameModesController?.LoadFromMeta(meta);

            var endlessUnlocked = _debugEnableAllFeatures || (meta?.EndlessZenUnlocked ?? false);
            var trialsUnlocked = _debugEnableAllFeatures || (meta?.SpiritTrialsUnlocked ?? false);
            var harmonyLevel = _gameModesController?.SelectedHarmonyLevel ?? 0;
            var maxUnlocked = _gameModesController?.GetMaxUnlockedHarmonyLevel(meta) ?? 0;
            var perk = _gameModesController?.SelectedHarmonyPerk ?? HarmonyPerkId.None;

            SetPanelButtonState("BtnEndless", endlessUnlocked, LocalizationService.T(endlessUnlocked ? "Start Endless Zen" : "Endless Zen (Locked)"));
            SetPanelButtonState("BtnZenRecords", endlessUnlocked, LocalizationService.T("Records"));
            SetPanelButtonState("BtnTrials", trialsUnlocked, LocalizationService.T(trialsUnlocked ? "Spirit Trials" : "Spirit Trials (Locked)"));
            SetPanelButtonState("BtnHarmonyPrev", harmonyLevel > 0, "<");
            SetPanelButtonState("BtnHarmonyNext", harmonyLevel < maxUnlocked, ">");

            SetPanelText("HarmonySelectedLabel", LocalizationService.Format("Harmony.SelectedLabel",
                "{0}  ({1})",
                HarmonyName(harmonyLevel),
                HarmonyDifficultyService.GetHudLabel(harmonyLevel)));
            SetPanelText("HarmonySubLabel", LocalizationService.Format("Harmony.SubLabel", "Unlocked range: H0-H{0}  -  XP x{1:0.0}", maxUnlocked, HarmonyDifficultyService.GetXpMultiplier(harmonyLevel)));

            var perkUnlocked = harmonyLevel >= 4;
            SetPanelButtonState("BtnHarmonyPerk", perkUnlocked,
                perkUnlocked
                    ? LocalizationService.Format("Harmony.Perk.Button", "Perk: {0}", HarmonyPerkName(perk))
                    : LocalizationService.T("Perk: Locked until H4"));
            SetPanelText("HarmonyPerkDesc",
                perkUnlocked
                    ? HarmonyPerkDescription(perk)
                    : LocalizationService.T(
                        "MainMenu.Perks.UnlockHint",
                        "Perks unlock at H4, H6, H8, and H10."));
        }

        private void SetPanelButtonState(string buttonName, bool interactable, string label)
        {
            Button btn = null;
            if (_gameModesPanel != null)
                foreach (var candidate in _gameModesPanel.GetComponentsInChildren<Button>(true))
                    if (candidate.name == buttonName) { btn = candidate; break; }
            if (btn == null) return;
            btn.interactable = interactable;
            var txt = btn.transform.Find("Label")?.GetComponent<Text>();
            if (txt != null) txt.text = label;
        }

        private void SetPanelText(string textName, string value)
        {
            Text txt = null;
            if (_gameModesPanel != null)
                foreach (var candidate in _gameModesPanel.GetComponentsInChildren<Text>(true))
                    if (candidate.name == textName) { txt = candidate; break; }
            if (txt != null) txt.text = value;
        }

        // ── Sudoku Basics First-Time Tutorial ──

        public void ShowFirstRunSetupPrompt(Action onCompleted)
        {
            if (_firstRunSetupOverlay != null)
                Destroy(_firstRunSetupOverlay);

            var canvas = _menuCanvas != null ? _menuCanvas : FindFirstObjectByType<Canvas>();
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

            var canvas = FindFirstObjectByType<Canvas>();
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
