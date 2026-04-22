using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

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
        private readonly Dictionary<ClassId, Button> _classButtons = new Dictionary<ClassId, Button>();
        private readonly Dictionary<ClassId, GameObject> _classLockOverlays = new Dictionary<ClassId, GameObject>();
        private ClassId? _highlightedClassId;
        private GameObject _startRunConfirmOverlay;   // #8 modal
        private GameObject _sudokuBasicsOverlay;      // #14 dismiss
        private GameObject _classLockTooltipPanel;    // C-3 lock tooltip
        private Text       _classLockTooltipText;

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
        // B (JoystickButton1) → back  |  R3 (JoystickButton9) → confirm selected button
        // left-stick / d-pad → navigate via EventSystem
        private void Update()
        {
            if (!Input.anyKeyDown) return;

            // #14 — B/Escape dismisses the first-run Sudoku basics onboarding overlay
            if (_sudokuBasicsOverlay != null && _sudokuBasicsOverlay.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    Destroy(_sudokuBasicsOverlay);
                    _sudokuBasicsOverlay = null;
                    MarkSudokuBasicsComplete();
                    return;
                }
            }

            // #8 — B/Escape dismisses the Start Run confirmation modal
            if (_startRunConfirmOverlay != null && _startRunConfirmOverlay.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    Destroy(_startRunConfirmOverlay);
                    _startRunConfirmOverlay = null;
                    return;
                }
            }

            // B button / Escape = back/cancel  (X-1: Escape now handles class select + all panels)
            if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
            {
                var current = _menu.CurrentScreen;
                if (current == MenuScreen.MainMenu)
                    return; // nothing to go back to on the root screen
                BackFromPanel();
                return;
            }

            // R3 = confirm (same as pressing the currently selected button)
            if (Input.GetKeyDown(KeyCode.JoystickButton9))
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
            GameObject accessibilityPanel = null)
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
            RefreshClassLockStates();
            _menu.Show(MenuScreen.ClassSelect);
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

        public void OpenGameModes()    => _menu.Show(MenuScreen.GameModes);
        public void ShowGameModes()
        {
            if (_dailyWalkPanel   != null) _dailyWalkPanel.SetActive(false);
            if (_monthlyWalkPanel != null) _monthlyWalkPanel.SetActive(false);
            _menu.Show(MenuScreen.GameModes);
        }
        public void OpenKeybindings()  => _menu.Show(MenuScreen.Keybindings);
        public void OpenAccessibility() => _menu.Show(MenuScreen.Accessibility);

        /// <summary>
        /// Forces the options panel to re-read option values and refresh all UI widgets.
        /// Called after a Reset-to-Defaults action so sliders and toggles snap to default positions.
        /// </summary>
        public void RefreshOptionsPanel() => _optionsController?.OnSlotChanged?.Invoke();

        public void BackToMainMenu() => _menu.ShowMainMenu();
        public void BackFromPanel() => _menu.Back();

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
            var resetLabel = $"Resets in {(int)resetIn.TotalHours:D2}:{resetIn.Minutes:D2}";

            FindAndSetText(_dailyWalkPanel, "StreakLabel",
                $"Streak: {goals.CurrentStreak} day{(goals.CurrentStreak == 1 ? "" : "s")}");
            FindAndSetText(_dailyWalkPanel, "StampsLabel",
                $"Ink Stamps: {goals.TotalInkStamps}  ·  {completedCount}/3 today  ·  {resetLabel}");

            for (var i = 0; i < 3; i++)
            {
                var done = goals.TodayGoalCompleted[i];
                var tierTag = defs[i].Tier switch
                {
                    DailyGoalService.GoalTier.Easy   => "[Easy]   ",
                    DailyGoalService.GoalTier.Medium => "[Medium] ",
                    DailyGoalService.GoalTier.Hard   => "[Hard]   ",
                    _                                => ""
                };
                var statusMark = done ? "\u2713 " : "\u25cb ";
                var rewardPart = done ? "" : $"  \u2192 {defs[i].RewardDescription}";
                FindAndSetText(_dailyWalkPanel, $"Goal{i}",
                    $"{statusMark}{tierTag}{defs[i].Description}{rewardPart}");
            }
        }

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
                best > 0 ? $"Personal Best: {best:N0}" : "Personal Best: —");

            if (best > 0 && seasonal != null
                && seasonal.TryGetBestBreakdown(now.Year, now.Month,
                    out _, out var hp, out var pencilUsed, out var timeSeconds))
            {
                var hpPart = hp >= 0 ? $"HP: {hp}" : "HP: —";
                var pencilPart = pencilUsed >= 0 ? $"Pencil used: {pencilUsed}" : "Pencil used: —";
                var timePart = timeSeconds >= 0
                    ? $"Time: {timeSeconds / 60:00}:{timeSeconds % 60:00}"
                    : "Time: —";

                FindAndSetText(_monthlyWalkPanel, "MonthlyBestBreakdownLabel",
                    $"{hpPart}  ·  {pencilPart}  ·  {timePart}");
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

        public void SelectProfileSlot(int slot)
        {
            _profileSlots.SelectSlot(slot);

            // Re-wire save services to new slot
            _gameBootstrap?.ApplySlotChange();
            _save = new SaveFileService(SaveProfileService.ActiveSlot);
            _profile = new ProfileService(_save);

            LocalizationService.SetLanguage(_profile.LoadOptions().Language);
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
            SetStatus($"Profile {slot + 1} deleted.");
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
                    txt.text = "Empty\n\nClick Load to\ncreate a new profile.";
                }
                else
                {
                    var runInfo = s.HasActiveRun ? " (run in progress)" : "";
                    var lastPlayed = string.IsNullOrEmpty(s.LastPlayedUtc) ? "Never"
                        : System.DateTime.Parse(s.LastPlayedUtc).ToString("yyyy-MM-dd");
                    txt.text = $"Class: {s.LastPlayedClass}\n"
                             + $"Runs: {s.TotalRunsStarted} started / {s.TotalRunsCompleted} won\n"
                             + $"Bosses: {s.TotalBossesDefeated}\n"
                             + $"Last played: {lastPlayed}{runInfo}";
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
                SetStatus(LocalizationService.T("No run to resume."));
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
                    lbl.text  = "No Active Run";
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
                lbl.text = $"Resume  ·  {ClassCatalog.GetDefinition(rs.ClassId).Name}  ·  Floor {rs.CurrentFloor + 1}  ·  HP {rs.CurrentHP}/{rs.MaxHP}";
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
        }

        // C-3 — Hover tooltip showing unlock condition for locked class buttons
        public void ShowClassLockTooltip(ClassId classId, Vector2 screenPos)
        {
            var meta = _profile.LoadMetaProgress();
            var isUnlocked = IsClassUnlockedOrDebug(classId, meta);
            var progress = GetUnlockProgress(classId, meta);

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
                var def = ClassCatalog.GetDefinition(classId);
                var meta = _profile.LoadMetaProgress();
                var isUnlocked = IsClassUnlockedOrDebug(classId, meta);
                var allUnlocks = ClassCatalog.GetAllUnlocks(classId);

                _classInfoText.supportRichText = true;
                if (isUnlocked)
                {
                    // Derive level and XP progress
                    var garden = meta?.GardenProgression;
                    var totalXp = 0;
                    if (garden != null)
                        for (var i = 0; i < garden.ClassEntries.Count; i++)
                            if (garden.ClassEntries[i].ClassId == classId) { totalXp = garden.ClassEntries[i].TotalXp; break; }

                    var level = XpTable.DeriveLevel(totalXp);
                    var xpIntoLevel = totalXp - XpTable.CumulativeXpForLevel(level);
                    var xpToNext = level < 40 ? XpTable.XpToNextLevel(level) : 0;

                    _classInfoText.color = new Color(0.96f, 0.93f, 0.82f, 1f);
                    var sb = new StringBuilder();
                    sb.Append($"<b>{LocalizationService.T(def.Name)}</b>  Lv{level}{(level >= 40 ? " MAX" : "")}");
                    sb.AppendLine(level < 40 ? $"   XP: {xpIntoLevel}/{xpToNext}" : $"   XP: MAX");
                    sb.AppendLine($"HP:{def.BaseHP}  Pencil:{def.BasePencil}  Slots:{def.BaseItemSlots}  |  {def.PassiveDescription}");
                    // Compact unlock table: 3 per row, highlight achieved (gold) vs upcoming (dim)
                    sb.AppendLine("─ Level Unlocks ─");
                    var itemsOnRow = 0;
                    for (var ui = 0; ui < allUnlocks.Length; ui++)
                    {
                        var (ulvl, udesc) = allUnlocks[ui];
                        var achieved = ulvl <= level;
                        if (achieved)
                            sb.Append($"<color=#C8A44A>[L{ulvl}✓]</color> {udesc}");
                        else
                            sb.Append($"<color=#888888>[L{ulvl}]</color> {udesc}");
                        itemsOnRow++;
                        if (itemsOnRow == 2 || ui == allUnlocks.Length - 1) { sb.AppendLine(); itemsOnRow = 0; }
                        else sb.Append("  ");
                    }
                    sb.AppendLine("─ Exclusive Unlocks ─");
                    var exItem = ItemService.GetExclusiveItemForClass(classId);
                    var exRelic = RelicService.GetExclusiveRelicForClass(classId);
                    if (exItem.HasValue)
                    {
                        sb.Append(level >= 15
                            ? $"<color=#C8A44A>[L15✓] {ItemService.GetItemName(exItem.Value)}</color>"
                            : "<color=#888888>[L15] Unlocks at Level 15: ???</color>");
                        sb.Append("  ");
                    }
                    if (exRelic.HasValue)
                        sb.AppendLine(level >= 30
                            ? $"<color=#C8A44A>[L30✓] {RelicService.GetRelicName(exRelic.Value)}</color>"
                            : "<color=#888888>[L30] Unlocks at Level 30: ???</color>");
                    _classInfoText.text = sb.ToString().TrimEnd();
                }
                else
                {
                    _classInfoText.color = new Color(0.80f, 0.55f, 0.35f, 1f);
                    var sb = new StringBuilder();
                    sb.AppendLine($"<b>{LocalizationService.T(def.Name)}  —  LOCKED</b>");
                    sb.Append($"Unlock: {def.UnlockCondition}");
                    var progress = GetUnlockProgress(classId, meta);
                    if (!string.IsNullOrEmpty(progress)) sb.AppendLine($"   ({progress})");
                    else sb.AppendLine();
                    sb.AppendLine($"HP:{def.BaseHP}  Pencil:{def.BasePencil}  Slots:{def.BaseItemSlots}  |  {def.PassiveDescription}");
                    sb.AppendLine("─ Level Unlocks (preview) ─");
                    var itemsOnRow = 0;
                    for (var ui = 0; ui < allUnlocks.Length; ui++)
                    {
                        var (ulvl, udesc) = allUnlocks[ui];
                        sb.Append($"<color=#888888>[L{ulvl}]</color> {udesc}");
                        itemsOnRow++;
                        if (itemsOnRow == 2 || ui == allUnlocks.Length - 1) { sb.AppendLine(); itemsOnRow = 0; }
                        else sb.Append("  ");
                    }
                    sb.AppendLine("─ Exclusive Unlocks ─");
                    var exItemL = ItemService.GetExclusiveItemForClass(classId);
                    var exRelicL = RelicService.GetExclusiveRelicForClass(classId);
                    if (exItemL.HasValue)
                        sb.Append("<color=#888888>[L15] Unlocks at Level 15: ???</color>  ");
                    if (exRelicL.HasValue)
                        sb.AppendLine("<color=#888888>[L30] Unlocks at Level 30: ???</color>");
                    _classInfoText.text = sb.ToString().TrimEnd();
                }
            }
        }

        public void ConfirmClassAndStart()
        {
            if (_gameBootstrap == null) return;

            var meta = _profile.LoadMetaProgress();
            if (!IsClassUnlockedOrDebug(_selectedClass, meta))
            {
                SetStatus(LocalizationService.T("Class not yet unlocked."));
                return;
            }

            var request = new LaunchRequest
            {
                ClassId = _selectedClass,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = _allowIrregularPuzzles
            };

            _gameBootstrap.LaunchRun(request);
        }

        // #8 — Shows a confirmation modal before launching; called by Start Run button
        public void ShowStartRunModal()
        {
            if (_gameBootstrap == null) return;

            var meta = _profile.LoadMetaProgress();
            if (!IsClassUnlockedOrDebug(_selectedClass, meta))
            {
                SetStatus(LocalizationService.T("Class not yet unlocked."));
                return;
            }

            if (_startRunConfirmOverlay != null) Destroy(_startRunConfirmOverlay);

            // F — use stored canvas ref to avoid slow scene-wide search
            var canvas = _menuCanvas != null ? _menuCanvas : FindFirstObjectByType<Canvas>();
            if (canvas == null) { ConfirmClassAndStart(); return; }

            var overlay = new GameObject("StartRunModal", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(canvas.transform, false);
            var rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.18f, 0.28f);
            rt.anchorMax = new Vector2(0.82f, 0.72f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.97f);
            _startRunConfirmOverlay = overlay;

            // F / 6 — fade in + pop scale on the modal
            var overlayCg = overlay.AddComponent<CanvasGroup>();
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
                def.PassiveDescription, 12, TextAnchor.UpperCenter,
                new Color(0.90f, 0.87f, 0.72f, 1f));
            passiveTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.38f);
            passiveTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.62f);
            passiveTxt.rectTransform.offsetMin = passiveTxt.rectTransform.offsetMax = Vector2.zero;
            passiveTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Stats line
            var statsTxt = InRunUiFactory.CreateText(overlay.transform, "Stats",
                $"HP: {def.BaseHP}  ·  Pencil: {def.BasePencil}  ·  Item Slots: {def.BaseItemSlots}",
                11, TextAnchor.UpperCenter, new Color(0.75f, 0.72f, 0.58f, 1f));
            statsTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.28f);
            statsTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.40f);
            statsTxt.rectTransform.offsetMin = statsTxt.rectTransform.offsetMax = Vector2.zero;

            // C-4 — Show irregular toggle state in modal
            var irregularLine = _allowIrregularPuzzles ? "Irregular puzzles: ON" : "Irregular puzzles: off";
            var irregularTxt = InRunUiFactory.CreateText(overlay.transform, "IrregularState",
                irregularLine, 10, TextAnchor.UpperCenter,
                _allowIrregularPuzzles
                    ? new Color(0.90f, 0.75f, 0.30f, 1f)
                    : new Color(0.60f, 0.58f, 0.48f, 0.70f));
            irregularTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.22f);
            irregularTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.30f);
            irregularTxt.rectTransform.offsetMin = irregularTxt.rectTransform.offsetMax = Vector2.zero;

            // "Begin" button — shifted down to leave room for C-4 irregular line
            var beginBtn = InRunUiFactory.CreatePanelButton(overlay.transform, "BtnBegin",
                new Vector2(0.08f, 0.03f), new Vector2(0.46f, 0.20f), "Begin");
            var beginLbl = beginBtn.transform.Find("Label")?.GetComponent<Text>();
            if (beginLbl != null) beginLbl.color = GamePalette.AccentGold;
            beginBtn.onClick.AddListener(() =>
            {
                Destroy(overlay);
                _startRunConfirmOverlay = null;
                ConfirmClassAndStart();
            });

            // "Back" button
            var backBtn = InRunUiFactory.CreatePanelButton(overlay.transform, "BtnBack",
                new Vector2(0.54f, 0.03f), new Vector2(0.92f, 0.20f), "Back");
            backBtn.onClick.AddListener(() =>
            {
                Destroy(overlay);
                _startRunConfirmOverlay = null;
            });
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
            _gameBootstrap.LaunchTutorial(setup);
        }

        // ── Game Modes ──

        public void StartEndlessZen()
        {
            if (_gameBootstrap == null) return;

            var request = new LaunchRequest
            {
                Mode = GameMode.EndlessZen,
                ClassId = _selectedClass
            };
            _gameBootstrap.LaunchRun(request);
        }

        public void StartSpiritTrials()
        {
            if (_gameBootstrap == null) return;

            var request = new LaunchRequest
            {
                Mode = GameMode.SpiritTrials,
                ClassId = _selectedClass
            };
            _gameBootstrap.LaunchRun(request);
        }

        // ── Debug ──

        public void OnDebugEnableAllChanged(bool isOn)
        {
            _debugEnableAllFeatures = isOn;
            SetStatus(isOn ? "Debug: All features enabled." : "Debug mode off.");
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

        // ── Sudoku Basics First-Time Tutorial ──

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

            var overlay = new GameObject("SudokuBasicsPrompt", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(canvas.transform, false);
            var rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.20f, 0.28f);
            rt.anchorMax = new Vector2(0.80f, 0.72f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.97f);
            _sudokuBasicsOverlay = overlay; // #14 — store ref for keyboard dismiss

            // Title
            var title = InRunUiFactory.CreateText(overlay.transform, "Title",
                "Welcome to Run of the Nine.",
                18, TextAnchor.UpperCenter, GamePalette.AccentGold);
            title.rectTransform.anchorMin = new Vector2(0.05f, 0.72f);
            title.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            var body = InRunUiFactory.CreateText(overlay.transform, "Body",
                "Do you want a quick introduction to how Sudoku puzzles work?\n(This takes about 2 minutes.)",
                14, TextAnchor.MiddleCenter, new Color(0.92f, 0.90f, 0.82f));
            body.rectTransform.anchorMin = new Vector2(0.05f, 0.40f);
            body.rectTransform.anchorMax = new Vector2(0.95f, 0.70f);
            body.rectTransform.offsetMin = body.rectTransform.offsetMax = Vector2.zero;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;

            // "Yes, show me" button
            var yesBtn = InRunUiFactory.CreatePanelButton(overlay.transform, "BtnYes",
                new Vector2(0.08f, 0.08f), new Vector2(0.46f, 0.32f), "Yes, show me");
            yesBtn.onClick.AddListener(() =>
            {
                _sudokuBasicsOverlay = null;
                Destroy(overlay);
                MarkSudokuBasicsComplete();
                LaunchSudokuBasicsTutorial();
            });

            // "Skip" button
            var skipBtn = InRunUiFactory.CreatePanelButton(overlay.transform, "BtnSkip",
                new Vector2(0.54f, 0.08f), new Vector2(0.92f, 0.32f), "Skip");
            skipBtn.onClick.AddListener(() =>
            {
                _sudokuBasicsOverlay = null;
                Destroy(overlay);
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

        /// <summary>Returns a "X/Y" progress string for the class's unlock condition, or null if N/A.</summary>
        private static string GetUnlockProgress(ClassId classId, MetaProgressionState meta)
        {
            var p = meta?.ClassUnlocks;
            if (p == null) return null;
            return classId switch
            {
                ClassId.GardenMonk        => $"Bosses defeated: {p.BossesDefeated}/2",
                ClassId.ShrineArchivist   => $"Items used: {p.ItemsUsed}/15",
                ClassId.KoiGambler        => $"Relics collected: {p.RelicsCollected}/10",
                ClassId.StoneGardener     => $"Bosses defeated: {p.BossesDefeated}/10",
                ClassId.LanternSeer       => $"Gold collected: {p.GoldCollected:N0}/50,000",
                ClassId.ReedDuelist       => p.ItemCodexComplete ? "Item codex: complete!" : "Item codex: not yet complete",
                ClassId.QuietCartographer => p.PerfectNoPencilStage ? "Perfect run: achieved!" : "Perfect run: not yet achieved",
                _ => null
            };
        }
    }
}
