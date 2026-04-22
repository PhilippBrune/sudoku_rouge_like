using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Orchestrator: path overview, input, item effects.
    /// Board/HUD/panels delegated to sub-controllers.
    /// </summary>
    public sealed class InRunController : MonoBehaviour
    {
        // ── Injected ──
        private RunMapController _map;
        private GameObject _pathPanel;
        private GameObject _sudokuPanel;
        private GameObject _gameOverPanel;
        private Text _pathStatsBar;
        private Text _goldLabel;
        private Image _goldIcon;
        private Text _statusText;

        // ── Panel CanvasGroups for fade transitions (V6) ──
        private CanvasGroup _pathCg;
        private CanvasGroup _sudokuCg;

        // ── Sub-controllers ──
        private BoardViewController _boardView;
        private HudViewController _hudView;
        private NumpadViewController _numpadView;
        private RewardViewController _rewardView;
        private ShopViewController _shopView;
        private BossGateViewController _bossGateView;
        private EndScreenViewController _endScreenView;
        private EventChoiceScreenController _eventChoiceView;
        private InRunUiFlowController _uiFlowCtrl;

        // ── Game state ──
        private int _selectedRow = -1;
        private int _selectedCol = -1;
        private int _highlightValue;
        private bool _pencilMode;
        private bool _completionHandled;
        private int  _pendingKeyboardDigit;
        private bool _resumeApplied;
        private bool _tutorialShown;

        // Input safety: block digit entry for a short window after cell selection
        // to prevent accidental placements on touch/tap.
        private float _cellSelectedTime = -1f;
        private const float CellSelectInputDelay = 0.5f;

        // ── Sudoku Basics Tutorial ──
        private SudokuBasicsTutorialController _basicsTutorial;

        // ── Blurred Sight curse ──
        private readonly HashSet<(int, int)> _blurredPencilCells = new HashSet<(int, int)>();

        // ── Gamepad overlay focus — reused each frame to avoid per-frame allocation ──
        private readonly List<Button> _overlayBtnCache = new List<Button>();

        // ── Swap state (Silk Fan item) ──
        private bool _swapMode;
        private int _swapRow1 = -1, _swapCol1 = -1;
        private int _lastMistakeRow = -1, _lastMistakeCol = -1;
        private int _lastPlacedRow  = -1, _lastPlacedCol  = -1; // tracks last correctly placed digit for Y-undo

        // ── Koi Reflection pending cell-select state ──
        private bool _koiReflectionPending;
        private int  _koiReflectionChargesLeft;

        // ── WindChime pending house-select state ──
        private bool       _windChimePending;
        private ItemRarity _windChimeRarity;
        private bool       _windChimeChoosingHouses; // phase 1: pick which house(s) to fill
        private bool       _windChimeFillRow, _windChimeFillCol, _windChimeFillBox;
        private int        _windChimeMaxHouses; // 1 Normal / 2 Rare

        // ── TempleIncense pending cell-fill state ──
        private bool _templeIncensePending;
        private int  _templeIncenseChargesLeft;

        // ── PatternScroll pending mechanic-select state ──
        // States: Idle (_patternScrollPending=false) → Selecting (_pending=true, staged=-1)
        //         → Confirmed (_pending=true, staged≥0) → Applied (back to Idle)
        private bool       _patternScrollPending;
        private int        _patternScrollChargesLeft;
        private GameObject _patternScrollOverlay;
        // Stable mechanic list built once per activation; solved indices excluded from subsequent rounds
        private List<(string label, List<CellCoord> cells)> _patternScrollMechanics;
        private HashSet<int> _patternScrollSolvedIds;
        private int          _patternScrollStagedIndex = -1; // -1 = nothing staged

        // ── Gamepad controller state ──
        private readonly InputRemapService _inputRemap = new InputRemapService();
        private bool  _gbControllerInItemsFocus;         // false = puzzle, true = items bag
        private int   _gbNumpadRow = 1, _gbNumpadCol = 1; // right-stick numpad cursor (default = "5")
        private float _gbRStickPrevH, _gbRStickPrevV;
        private const string RightStickH       = "RightStickHorizontal";
        private const string RightStickV       = "RightStickVertical";
        private const float  RightStickThresh  = 0.5f;
        private const string TriggerAxisL      = "Axis 9";   // LT on Xbox/generic Unity layout
        private const string TriggerAxisR      = "Axis 10";  // RT on Xbox/generic Unity layout
        private const float  TriggerThresh     = 0.3f;
        private float _triggerLPrev, _triggerRPrev;
        private float _pathBConfirmTimer;   // >0 while waiting for second B press to confirm SaveAndQuit

        // ── Item state ──
        private HashSet<(int row, int col)> _bagHighlightCells;

        // Fog moves are stored in RunState.FogDisabledMoves (persisted); read via this accessor
        private int _fogDisabledMovesRemaining => _map?.Run?.State?.FogDisabledMoves ?? 0;
        private float _bagHighlightEndTime;

        // ── Path overlay ──
        private RectTransform _pathOverlayRoot;
        private RectTransform _pathViewport;
        private ScrollRect    _pathScrollRect;
        private GameObject    _nodeTooltipPanel;
        private Text          _nodeTooltipText;
        private string _pathMessage = string.Empty;
        private RawImage      _floorBgRaw;          // Background PNG for the current floor (bg_*.png)
        private RawImage      _floorBgPrev;         // Previous floor background — used for cross-fade
        private int           _lastBuiltFloor = -1; // detects floor advances to trigger cross-fade
        private Image         _bgScrimImg;          // Dark scrim between the background art and UI content
        private Transform     _bossNodeTransform;   // Thematic 6: boss-gate pulse reference
        private Coroutine     _bossRingPulse;       // Thematic 6: boss pulse loop coroutine
        private bool          _saveQuitPending;     // two-step confirm: true after first tap
        private Coroutine     _saveQuitResetCo;     // resets confirm state after timeout
        private Image         _saveQuitImg;         // reference to current BtnSaveQuit background
        private Text          _saveQuitLbl;         // reference to current BtnSaveQuit label
        private RectTransform _floorProgressStrip;  // Thematic 4: floor progress dot strip
        private PathAmbientParticles _ambientParticles; // Suggestion 3: seasonal particles
        private int           _ambientParticlesFloor = -1; // tracks last initialized floor
        private const float   LegendH = 52f;        // Fix D: shared legend height constant

        // #3 — track previously-reachable nodes to pulse newly-reachable ones
        private readonly HashSet<int> _prevReachableIndices = new HashSet<int>();

        // #9 — node just completed: gold ring shown after reward screen is dismissed
        private int  _justCompletedNodeIndex  = -1;
        private bool _completedRingArmed      = false; // true only while OnPostRewardReady ShowPath is pending

        // P-1 — first-pick callout band
        private GameObject _firstPickBand;

        // #12 — intro card shown once at start of a new run
        private bool _introCardShown;
        private bool _introCardDismissed;  // G-1 guard

        // ── Path controller navigation ──
        // Parallel lists: node index → button, populated by RebuildPathCanvas
        private readonly List<int>    _gbReachableNodeIndices = new List<int>();
        private readonly List<Button> _gbReachableNodeButtons = new List<Button>();
        private int _gbPathCursor = 0;  // index into the above lists

        // ── Options / Boss ──
        private GameObject _inGameOptionsPanel;
        private int _bossNodeIndex = -1;

        // ── Boss Awakening async loading ──
        private LevelConfig _pendingBossLevel;
        private GameObject _bossAwakeningPanel;
        private CanvasGroup _bossAwakeningCg;

        // ── Controller feedback ──
        private RectTransform _uiRoot;
        private Text  _controllerIndicatorText;
        private Image _victoryFlashOverlay;
        private float _controllerIndicatorTimer;

        // ── Systems ──
        private RunAudioController _audio;
        private AccessibilityService _accessibility = new AccessibilityService();

        // ─────────────────────────── Lifecycle ───────────────────────────

        private void Awake()
        {
            _bagHighlightCells = new HashSet<(int, int)>();
        }

        /// <summary>
        /// Bootstraps the controller by discovering all named UI elements under <paramref name="uiRoot"/>.
        /// Called by InRunUiBlueprintBuilder after the full hierarchy is built.
        /// </summary>
        public void Configure(RunMapController map, RectTransform uiRoot)
        {
            _map = map;

            // ── Discover panels by name ──
            _pathPanel     = uiRoot.Find("PathOverviewPanel")?.gameObject;
            _sudokuPanel   = uiRoot.Find("SudokuGameplayPanel")?.gameObject;
            _gameOverPanel = uiRoot.Find("GameOverPanel")?.gameObject;
            _statusText    = _sudokuPanel?.transform.Find("SudokuGameplayStatus")?.GetComponent<Text>();
            _inGameOptionsPanel = uiRoot.Find("InGameOptionsPanel")?.gameObject;

            _pathCg   = EnsureCanvasGroup(_pathPanel);
            _sudokuCg = EnsureCanvasGroup(_sudokuPanel);

            // Build Boss Awakening overlay panel (fullscreen dark + label, hidden by default).
            BuildBossAwakeningPanel(uiRoot);

            // ── Discover deeper refs for sub-controllers ──
            var sudokuRt   = _sudokuPanel?.transform;
            var hpText     = sudokuRt?.Find("SudokuGameplayHp")?.GetComponent<Text>();
            var pencilText = sudokuRt?.Find("SudokuGameplayPencil")?.GetComponent<Text>();
            var gridRoot   = sudokuRt?.Find("SudokuGameplayGridRoot") as RectTransform;
            var numpadRoot = sudokuRt?.Find("SudokuGameplayNumpadRoot") as RectTransform;

            var goRt      = _gameOverPanel?.transform;
            var goSummary = goRt?.Find("GameOverTitle")?.GetComponent<Text>();
            var goDetails = goRt?.Find("GameOverDetails")?.GetComponent<Text>();
            var goBack    = goRt?.Find("BtnGameOverBack")?.GetComponent<Button>();

            // ── Instantiate sub-controllers ──
            _boardView     = gameObject.AddComponent<BoardViewController>();
            _hudView       = gameObject.AddComponent<HudViewController>();
            _numpadView    = gameObject.AddComponent<NumpadViewController>();
            _rewardView    = gameObject.AddComponent<RewardViewController>();
            _shopView      = gameObject.AddComponent<ShopViewController>();
            _bossGateView  = gameObject.AddComponent<BossGateViewController>();
            _endScreenView = gameObject.AddComponent<EndScreenViewController>();

            // ── Configure sub-controllers ──
            _boardView.Configure(map, gridRoot);
            _boardView.BlurredPencilCells = _blurredPencilCells;
            _hudView.Configure(map, _sudokuPanel, hpText, pencilText);
            _numpadView.Configure(numpadRoot);
            _rewardView.Configure(map, _pathPanel);
            _shopView.Configure(map, _pathPanel,
                (item, onReplace, onAbort) => _rewardView.ShowBagSwapPanel(item, onReplace, onAbort));
            _bossGateView.Configure(map);
            _endScreenView.Configure(map, _gameOverPanel, goSummary, goDetails, goBack);
            _uiFlowCtrl = GetComponent<InRunUiFlowController>();
            _eventChoiceView = _uiFlowCtrl?.EventChoiceScreen;

            // ── Wire callbacks ──
            _boardView.OnCellClicked        = OnCellClicked;
            _numpadView.OnNumberPressed     = EnterNumber;
            _numpadView.OnPencilModeToggled = () =>
            {
                _pencilMode = _numpadView.PencilMode;
                _audio?.PlayPencilToggle();
            };

            _hudView.OnStatusChanged         = msg => SetStatus(msg);
            _hudView.OnItemEffectRequested   = ApplyItemEffect;
            _hudView.OnBagHighlightRequested = (cells, endTime) =>
            {
                _bagHighlightCells = cells;
                _bagHighlightEndTime = endTime;
            };
            _hudView.OnModifierTapped = rule =>
            {
                _boardView.HighlightGlobalConstraint(rule);
                RenderBoard();
            };

            _rewardView.OnStatusChanged   = msg => SetStatus(msg);
            _rewardView.OnPostRewardReady = OnPostRewardReady;
            _shopView.OnStatusChanged     = msg => SetStatus(msg);
            _shopView.OnShopClosed        = ShowPath;
            _bossGateView.OnModsConfirmed = OnBossModsConfirmed;
            _endScreenView.OnReturnToMenuPressed = SaveAndQuit;

            // ── Controller feedback overlays ──
            _uiRoot = uiRoot;
            BuildControllerIndicator();
            BuildVictoryFlashOverlay();

            ShowPath();
        }

        public void NotifyRunStarted()
        {
            _audio = FindFirstObjectByType<RunAudioController>();
            _resumeApplied     = false;
            _tutorialShown     = false;
            _completionHandled = false;
            _introCardShown    = false;   // #12
            _prevReachableIndices.Clear(); // #3
            _endScreenView.ResetShown();
            RefreshAccessibility();
            if (_inGameOptionsPanel != null) _inGameOptionsPanel.SetActive(false);
            _boardView.TutorialHighlightMap = null;
            _basicsTutorial = null;
            WireInGameButtons();
            ShowPath();
        }

        public void NotifyAccessibilityChanged()
        {
            RefreshAccessibility();
            if (_map?.Run?.CurrentBoard != null)
            {
                _boardView.MarkOverlayDirty();
                RebuildBoard();
            }
            FindFirstObjectByType<AmbientParticleController>()?.RefreshSettings();
        }

        private void RefreshAccessibility()
        {
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var opts = save.HasSaveFile() ? save.Load().PlayerProfile?.Options : null;
            _accessibility.Refresh(
                opts?.Accessibility ?? new AccessibilitySettings(),
                opts?.Graphics     ?? new GraphicsSettingsModel());
        }

        private void WireInGameButtons()
        {
            if (_sudokuPanel == null) return;
            var pr = _sudokuPanel.GetComponent<RectTransform>();
            if (pr == null) return;

            var sqBtn = pr.Find("BtnSudokuSaveQuit")?.GetComponent<Button>();
            if (sqBtn != null)
            {
                sqBtn.onClick.RemoveAllListeners();
                sqBtn.onClick.AddListener(SaveAndQuit);
            }

            var optBtn = pr.Find("BtnSudokuOptions")?.GetComponent<Button>();
            if (optBtn != null)
            {
                optBtn.onClick.RemoveAllListeners();
                optBtn.onClick.AddListener(ToggleOptionsPanel);
            }

            if (_inGameOptionsPanel == null && _sudokuPanel.transform.parent != null)
            {
                var parent = _sudokuPanel.transform.parent;
                var candidate = parent.Find("InGameOptionsPanel")?.gameObject
                             ?? parent.Find("InRunUI/InGameOptionsPanel")?.gameObject;
                if (candidate == null)
                    for (var i = 0; i < parent.childCount; i++)
                        if (parent.GetChild(i).name.Contains("Options") && parent.GetChild(i).name.Contains("Panel"))
                        { candidate = parent.GetChild(i).gameObject; break; }
                _inGameOptionsPanel = candidate;
            }

            // Wire the Back/close button inside the options panel
            if (_inGameOptionsPanel != null)
            {
                var closeBtn = _inGameOptionsPanel.transform.Find("BtnInGameOptionsClose")?.GetComponent<Button>();
                if (closeBtn != null)
                {
                    closeBtn.onClick.RemoveAllListeners();
                    closeBtn.onClick.AddListener(ToggleOptionsPanel);
                }
            }
        }

        private void ToggleOptionsPanel()
        {
            if (_inGameOptionsPanel == null) return;
            var nowActive = !_inGameOptionsPanel.activeSelf;
            _inGameOptionsPanel.SetActive(nowActive);
            if (nowActive)
                InRunUiFactory.SelectFirstInteractable(_inGameOptionsPanel);
        }

        public void SetInGameOptionsPanel(GameObject panel) => _inGameOptionsPanel = panel;
        public void SaveAndQuitPublic() => SaveAndQuit();

        public void SetHighlightConflictsLive(bool enabled)
        {
            if (_map?.Run?.CurrentBoard != null) RebuildBoard();
        }

        private void Update()
        {
            if (_map == null) return;
            var run = _map.Run;

            // Refresh controller type badge every 2 s
            _controllerIndicatorTimer -= Time.deltaTime;
            if (_controllerIndicatorTimer <= 0f)
            {
                _controllerIndicatorTimer = 2f;
                if (_controllerIndicatorText != null)
                    _controllerIndicatorText.text = ControllerGlyphService.GetIndicatorLabel();
            }

            if (run == null) return;

            if (!_resumeApplied) { ApplyResumeState(); return; }

            // Poll async boss level completion — finalize state and show puzzle once overlay is ready.
            if (_pendingBossLevel != null && run.TryCompleteAsyncLevel())
            {
                var level = _pendingBossLevel;
                _pendingBossLevel = null;
                HideBossAwakeningPanel();
                SetStatus($"Boss - {level.BoardSize}x{level.BoardSize} {level.Stars}*");
                ShowSudoku();
                RebuildBoard();
            }

            if (!_tutorialShown && run.State != null && run.State.TutorialMode)
            {
                _tutorialShown = true;
                ShowSudoku();
                RebuildBoard();

                // Sudoku Basics first-time tutorial (RunNumber == 0 marks the basics run)
                if (run.State.RunNumber == 0 && _basicsTutorial == null && _sudokuPanel != null)
                {
                    _basicsTutorial = gameObject.AddComponent<SudokuBasicsTutorialController>();
                    _basicsTutorial.GetBoard = () => _map?.Run?.CurrentBoard;
                    _basicsTutorial.SetHighlights = highlights =>
                    {
                        _boardView.TutorialHighlightMap = highlights;
                        RebuildBoard();
                    };
                    // Finish releases tutorial control; the player completes the puzzle freely.
                    _basicsTutorial.OnFinished = OnBasicsTutorialFinished;
                    // Auto-enable pencil mode on the numpad when a pencil-interaction step starts.
                    _basicsTutorial.OnPencilStepEntered = () =>
                    {
                        _pencilMode = true;
                        _numpadView?.SetPencilMode(true);
                    };
                    _basicsTutorial.StartTutorial(_sudokuPanel.transform);
                }
            }

            if (_sudokuPanel != null && _sudokuPanel.activeSelf)
            {
                HandleKeyboard();
                HandleCompletion();
                _hudView.Refresh(run.State, run.CurrentLevelConfig);
                CheckGameOver();
            }

            // Q shortcut works on path overview too (not just inside a puzzle)
            if (_pathPanel != null && _pathPanel.activeSelf && Input.GetKeyDown(KeyCode.Q))
                SaveAndQuit();
        }

        private void LateUpdate()
        {
            // Flush any keyboard digit queued this frame. Runs after EventSystem.Update(),
            // so _selectedRow/_selectedCol already reflects any same-frame cell click.
            if (_pendingKeyboardDigit <= 0) return;
            var digit = _pendingKeyboardDigit;
            _pendingKeyboardDigit = 0;
            if (_sudokuPanel != null && _sudokuPanel.activeSelf && _map?.Run?.CurrentBoard != null)
                EnterNumber(digit);
        }

        // ─────────────────────────── Screen transitions ───────────────────────────

        private void ShowPath()
        {
            var run = _map?.Run;
            if (run?.State != null && run.State.TutorialMode) { ShowSudoku(); return; }
            if (run?.State != null && run.State.IsSeasonalChallenge)
            {
                ShowSudoku();
                RebuildBoard();
                return;
            }
            // Accumulate this puzzle's elapsed time into the run total before stopping the timer
            if (run?.State != null)
                run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
            SetActive(_sudokuPanel, false);
            SetActive(_gameOverPanel, false);
            ShowPanelWithFade(_pathPanel, _pathCg);
            _audio?.SetContext(RunAudioController.Context.Path);
            _hudView?.StopPuzzleTimer();

            // Reset any puzzle-scoped item interactions / highlights so they can't leak into the next puzzle.
            CancelPendingItemInteraction(false);
            _bagHighlightCells.Clear();
            _bagHighlightEndTime = 0f;

            // #12 — Show intro card once on the very first ShowPath of a new (non-resume, non-tutorial) run
            if (!_introCardShown && !_resumeApplied && run?.State != null && !run.State.TutorialMode)
            {
                _introCardShown = true;
                ShowRunIntroCard(run);
                return; // intro card calls ShowPath() again on dismiss
            }

            RefreshPathOverview();
        }

        private void ShowSudoku()
        {
            SetActive(_pathPanel, false);
            SetActive(_gameOverPanel, false);
            ShowPanelWithFade(_sudokuPanel, _sudokuCg);
            var floor = _map?.Run?.State?.CurrentFloor ?? 0;
            _audio?.SetFloorIndex(floor);
            _audio?.SetContext(RunAudioController.Context.Puzzle);
            _hudView?.StartPuzzleTimer();
        }

        // #12 — Full-screen intro card shown once at the start of a new run
        private void ShowRunIntroCard(RunDirector run)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) { RefreshPathOverview(); return; }

            var card = new GameObject("RunIntroCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.09f, 0.96f);

            var def = ClassCatalog.GetDefinition(run.State.ClassId);

            // Class icon
            var iconSprite = Resources.Load<Sprite>("class/icon_" +
                ClassCatalog.GetIconName(run.State.ClassId));
            if (iconSprite != null)
            {
                var iconGo = new GameObject("ClassIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(card.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.37f, 0.55f);
                iconRt.anchorMax = new Vector2(0.63f, 0.90f);
                iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            var nameTxt = InRunUiFactory.CreateText(card.transform, "ClassName",
                LocalizationService.T(def.Name), 28, TextAnchor.MiddleCenter, GamePalette.AccentGold);
            nameTxt.rectTransform.anchorMin = new Vector2(0.1f, 0.44f);
            nameTxt.rectTransform.anchorMax = new Vector2(0.9f, 0.56f);
            nameTxt.rectTransform.offsetMin = nameTxt.rectTransform.offsetMax = Vector2.zero;
            nameTxt.fontStyle = FontStyle.Bold;

            var passiveTxt = InRunUiFactory.CreateText(card.transform, "Passive",
                def.PassiveDescription, 14, TextAnchor.UpperCenter,
                new Color(0.90f, 0.87f, 0.72f, 1f));
            passiveTxt.rectTransform.anchorMin = new Vector2(0.08f, 0.26f);
            passiveTxt.rectTransform.anchorMax = new Vector2(0.92f, 0.44f);
            passiveTxt.rectTransform.offsetMin = passiveTxt.rectTransform.offsetMax = Vector2.zero;
            passiveTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            var floorTxt = InRunUiFactory.CreateText(card.transform, "FloorLabel",
                "Floor 1", 18, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.72f, 0.58f, 1f));
            floorTxt.rectTransform.anchorMin = new Vector2(0.1f, 0.16f);
            floorTxt.rectTransform.anchorMax = new Vector2(0.9f, 0.26f);
            floorTxt.rectTransform.offsetMin = floorTxt.rectTransform.offsetMax = Vector2.zero;

            var dismissTxt = InRunUiFactory.CreateText(card.transform, "Dismiss",
                "Tap or press any button to begin", 11, TextAnchor.MiddleCenter,
                new Color(0.60f, 0.58f, 0.48f, 1f));
            dismissTxt.rectTransform.anchorMin = new Vector2(0.1f, 0.07f);
            dismissTxt.rectTransform.anchorMax = new Vector2(0.9f, 0.14f);
            dismissTxt.rectTransform.offsetMin = dismissTxt.rectTransform.offsetMax = Vector2.zero;

            // Tap-to-dismiss button (invisible, covers entire card)
            var tapBtn = card.AddComponent<Button>();
            tapBtn.transition = Selectable.Transition.None;
            tapBtn.onClick.AddListener(() =>
            {
                Destroy(card);
                RefreshPathOverview();
            });

            // Also auto-dismiss after 3 seconds
            _introCardDismissed = false;  // G-1
            tapBtn.onClick.AddListener(() =>
            {
                if (_introCardDismissed) return;  // G-1 guard
                _introCardDismissed = true;
                Destroy(card);
                RefreshPathOverview();
            });
            StartCoroutine(AutoDismissIntroCard(card, 3f));
        }

        private System.Collections.IEnumerator AutoDismissIntroCard(GameObject card, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_introCardDismissed) yield break;  // G-1 guard
            _introCardDismissed = true;
            if (card != null)
            {
                Destroy(card);
                RefreshPathOverview();
            }
        }

        private void ShowPanelWithFade(GameObject go, CanvasGroup cg)
        {
            if (go == null) return;
            go.SetActive(true);
            var dur = _accessibility.GetAnimationDuration(AnimationHelper.MenuPanelDuration);
            StartCoroutine(AnimationHelper.FadeIn(cg, dur));
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            if (go == null) return null;
            var cg = go.GetComponent<CanvasGroup>();
            return cg != null ? cg : go.AddComponent<CanvasGroup>();
        }

        // ── Boss Awakening overlay ──

        private void BuildBossAwakeningPanel(RectTransform uiRoot)
        {
            var go = new GameObject("BossAwakeningPanel");
            go.transform.SetParent(uiRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);

            var label = new GameObject("BossAwakeningLabel");
            label.transform.SetParent(go.transform, false);
            var labelRt = label.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.2f, 0.4f);
            labelRt.anchorMax = new Vector2(0.8f, 0.6f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var txt = label.AddComponent<Text>();
            txt.text = "Boss Awakening...";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 28;
            txt.color = new Color(1f, 0.85f, 0.4f, 1f);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _bossAwakeningCg = go.AddComponent<CanvasGroup>();
            _bossAwakeningCg.alpha = 0f;
            go.SetActive(false);
            _bossAwakeningPanel = go;
        }

        private void ShowBossAwakeningPanel()
        {
            if (_bossAwakeningPanel == null) return;
            _bossAwakeningPanel.SetActive(true);
            var dur = _accessibility.GetAnimationDuration(AnimationHelper.MenuPanelDuration);
            StartCoroutine(AnimationHelper.FadeIn(_bossAwakeningCg, dur));
        }

        private void HideBossAwakeningPanel()
        {
            if (_bossAwakeningPanel == null) return;
            _bossAwakeningPanel.SetActive(false);
            if (_bossAwakeningCg != null) _bossAwakeningCg.alpha = 0f;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        // ─────────────────────────── Board delegates ───────────────────────────

        private void RebuildBoard()
        {
            _boardView.RebuildBoard(_selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight),
                _bagHighlightCells, _fogDisabledMovesRemaining, _accessibility,
                _map?.Run?.GetLockedCells(), _blurredPencilCells, // [REQ: DEBUFF-HOOK-013] pass locked cells to renderer
                BuildPressureRenderData());

            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null) return;

            if (_sudokuPanel != null)
            {
                var infoText = _sudokuPanel.GetComponent<RectTransform>()
                    ?.Find("SudokuGameplayLevelInfo")?.GetComponent<Text>();
                if (infoText != null && run.State != null)
                {
                    var cfg   = run.CurrentLevelConfig;
                    var stars = cfg != null ? new string('★', cfg.Stars) : "";
                    infoText.text =
                        $"Floor {run.State.CurrentFloor + 1}  Depth {run.State.Depth}  {board.Size}×{board.Size}  {stars}";
                }
            }

            _numpadView.UpdateButtonStates(board.Size, board);
        }

        private void RenderBoard()
        {
            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null) return;
            _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight),
                _bagHighlightCells, _fogDisabledMovesRemaining,
                run.GetLockedCells(), _blurredPencilCells, BuildPressureRenderData()); // [REQ: DEBUFF-HOOK-013] pass locked cells to renderer
        }

        // [REQ: PRESSURE-UI-001] returns null for tutorial (RunNumber==0) — no overlays rendered
        // [REQ: PRESSURE-UI-002..009] populates PressureRenderData fields consumed by BoardViewController
        private PressureRenderData? BuildPressureRenderData()
        {
            var run = _map?.Run;
            // No pressure visuals during the basics tutorial (RunNumber = 0)
            if (run == null || run.State?.RunNumber == 0) return null;
            var ls = run.CurrentLevelState;
            if (ls == null) return null;
            if (ls.ActiveThreats.Count == 0 &&
                ls.HauntedCell == (-1, -1) &&
                ls.CrumblingCells.Count == 0 &&
                ls.ExpiryFlashCells.Count == 0 &&
                ls.ChainSuccessCells.Count == 0 &&
                ls.CrumbledFlashCells.Count == 0 &&
                !ls.WaveRipplePending)
                return null; // nothing active — skip allocation

            // Snapshot one-shot flash fields then clear them so they fire only once
            HashSet<(int, int)> expiryFlash   = ls.ExpiryFlashCells.Count   > 0 ? new HashSet<(int, int)>(ls.ExpiryFlashCells)   : null;
            HashSet<(int, int)> chainSuccess   = ls.ChainSuccessCells.Count  > 0 ? new HashSet<(int, int)>(ls.ChainSuccessCells)  : null;
            HashSet<(int, int)> crumbledFlash  = ls.CrumbledFlashCells.Count > 0 ? new HashSet<(int, int)>(ls.CrumbledFlashCells) : null;
            var waveRipple = ls.WaveRipplePending;
            ls.ExpiryFlashCells.Clear();
            ls.ChainSuccessCells.Clear();
            ls.CrumbledFlashCells.Clear();
            ls.WaveRipplePending = false;

            return new PressureRenderData
            {
                ActiveThreats         = ls.ActiveThreats,
                HauntedCell           = ls.HauntedCell,
                CrumblingCells        = ls.CrumblingCells,
                CrumblingMaxFragility = ls.CrumblingStartFragility,
                WaveRipplePending     = waveRipple,
                ChainSuccessCells     = chainSuccess,
                CrumbledFlashCells    = crumbledFlash,
                ExpiryFlashCells      = expiryFlash
            };
        }

        // ─────────────────────────── Path overview ───────────────────────────

        private void RefreshPathOverview()
        {
            var run = _map?.Run;
            if (run?.State == null || run.CurrentFloorGraph == null) return;

            var s         = run.State;
            var floorName = FloorThemeData.GetFloorName(s.CurrentFloor);
            var floorTint = FloorThemeData.GetFloorTint(s.CurrentFloor);

            if (_pathStatsBar != null)
            {
                var sb = new StringBuilder();
                var hpFrac = s.MaxHP > 0 ? (float)s.CurrentHP / s.MaxHP : 1f;
                var hpColor = hpFrac > 0.50f ? "#F5EDD1" : hpFrac > 0.25f ? "#FBD441" : "#BF3838";
                sb.Append($"{floorName}  ·  HP <color={hpColor}>{s.CurrentHP}/{s.MaxHP}</color>  ·  Pencil {s.CurrentPencil}/{s.MaxPencil}");
                if (s.HasRelic && (s.HeldRelics?.Count ?? 0) > 0)
                    sb.Append($"  ·  Relics {s.HeldRelics.Count}");
                var floorEffect = _map?.GetPositiveFloorEffectLabel();
                if (!string.IsNullOrEmpty(floorEffect))
                {
                    // Strip parenthetical detail " (+...)" so the bar stays single-line on narrow screens
                    var parenIdx = floorEffect.IndexOf(" (", System.StringComparison.Ordinal);
                    var shortEffect = parenIdx > 0 ? floorEffect.Substring(0, parenIdx) : floorEffect;
                    sb.Append($"  ·  {shortEffect}");
                }
                _pathStatsBar.text = sb.ToString();
            }

            if (_goldLabel != null) _goldLabel.text = s.CurrentGold.ToString();

            // P-1 — First-pick callout band: shown when no node has been chosen yet
            var isFirstPick = s.NodePath == null || s.NodePath.Count == 0;
            if (_pathPanel != null)
            {
                var existing = _pathPanel.transform.Find("FirstPickBand");
                if (existing != null) Destroy(existing.gameObject);
                _firstPickBand = null;

                if (isFirstPick)
                {
                    var bandGo = new GameObject("FirstPickBand", typeof(RectTransform), typeof(Image));
                    bandGo.transform.SetParent(_pathPanel.transform, false);
                    var bandRt = bandGo.GetComponent<RectTransform>();
                    // Fix D: use pixel offsets above legend strip so the band never overlaps it
                    bandRt.anchorMin = new Vector2(0.01f, 0f);
                    bandRt.anchorMax = new Vector2(0.99f, 0f);
                    bandRt.offsetMin = new Vector2(0f, LegendH);
                    bandRt.offsetMax = new Vector2(0f, LegendH + 36f);
                    bandGo.GetComponent<Image>().color = new Color(
                        GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.22f);
                    var bandTxt = InRunUiFactory.CreateText(bandGo.transform, "Msg",
                        "Select a Puzzle tile to begin your run.", 13,
                        TextAnchor.MiddleCenter, GamePalette.TextPrimary);
                    bandTxt.rectTransform.anchorMin = Vector2.zero;
                    bandTxt.rectTransform.anchorMax = Vector2.one;
                    bandTxt.rectTransform.offsetMin = bandTxt.rectTransform.offsetMax = Vector2.zero;
                    bandTxt.fontStyle = FontStyle.Bold;
                    bandTxt.raycastTarget = false;
                    _firstPickBand = bandGo;
                }
            }

            RebuildPathCanvas(run, floorTint);
        }

        private void RebuildPathCanvas(RunDirector run, Color floorTint)
        {
            EnsurePathOverlayRoot();
            if (_pathOverlayRoot == null) return;

            // Stop boss pulse loop — boss node button is about to be destroyed and rebuilt
            if (_bossRingPulse != null) { StopCoroutine(_bossRingPulse); _bossRingPulse = null; }
            _bossNodeTransform = null;

            InRunUiFactory.ClearChildren(_pathOverlayRoot);

            var graph = run.CurrentFloorGraph;
            if (graph == null || graph.Count == 0) return;

            var panelRect = _pathPanel?.GetComponent<RectTransform>()?.rect ?? Rect.zero;
            var viewW = (_pathViewport != null && _pathViewport.rect.width > 10f)
                ? _pathViewport.rect.width
                : Mathf.Max(400f, panelRect.width);
            var viewH = (_pathViewport != null && _pathViewport.rect.height > 10f)
                ? _pathViewport.rect.height
                : Mathf.Max(300f, panelRect.height - 52f);

            // Content wider than viewport when there are many nodes (gives comfortable spacing)
            var w = Mathf.Max(viewW, graph.Count * 65f + 120f);
            var h = viewH;

            // Update content width so ScrollRect can scroll
            if (_pathOverlayRoot != null)
                _pathOverlayRoot.sizeDelta = new Vector2(w, 0f);
            var reachable = run.GetReachableNodes();

            // Separate route nodes — Start and Boss sit outside the lane boxes
            var calmLane = new List<RunNode>();
            var riskLane = new List<RunNode>();
            for (var i = 0; i < graph.Count; i++)
            {
                var n = graph[i];
                if (n.Type == NodeType.Start || n.Type == NodeType.Boss) continue;
                if (n.Route == RouteType.CalmRoute) calmLane.Add(n);
                else riskLane.Add(n);
            }

            // Per-route lane background boxes — low-alpha fog bands, not hard containers
            // Calm: stone-green — garden path feeling, subtly brightened by floor green tint
            var calmBg      = new Color(0.13f + floorTint.g * 0.07f, 0.21f + floorTint.g * 0.06f, 0.13f, 0.14f);
            var calmOutline = new Color(0.28f, 0.60f + floorTint.g * 0.14f, 0.28f, 0.30f);
            // Risk: ember-amber — scorched earth, subtly brightened by floor red tint
            var riskBg      = new Color(0.25f + floorTint.r * 0.06f, 0.11f, 0.04f, 0.14f);
            var riskOutline = new Color(0.70f + floorTint.r * 0.10f, 0.32f, 0.08f, 0.30f);

            var calmRemaining = 0;
            for (var i = 0; i < calmLane.Count; i++) if (!calmLane[i].Visited) calmRemaining++;
            var riskRemaining = 0;
            for (var i = 0; i < riskLane.Count; i++) if (!riskLane[i].Visited) riskRemaining++;

            // Load the floor background PNG — cross-fade on floor advance
            var isFloorAdvance = _lastBuiltFloor >= 0 && run.State.CurrentFloor != _lastBuiltFloor;
            _lastBuiltFloor = run.State.CurrentFloor;
            if (_floorBgRaw != null)
            {
                var bgName = run.State.CurrentFloor switch
                {
                    0 => "background/bg_garden_grove",
                    1 => "background/bg_koi_garden",
                    2 => "background/bg_koi_terrace",
                    3 => "background/bg_shrine_garden",
                    4 => "background/bg_demon_summit",
                    _ => "background/bg_garden_grove",
                };
                var bgTex = Resources.Load<Texture2D>(bgName);
                if (isFloorAdvance && _floorBgPrev != null && _floorBgRaw.texture != null)
                {
                    _floorBgPrev.texture = _floorBgRaw.texture;
                    _floorBgPrev.uvRect  = new Rect(0f, 0f, 1f, 1f);
                    _floorBgPrev.color   = new Color(1f, 1f, 1f, 0.92f);
                    _floorBgRaw.texture  = bgTex;
                    _floorBgRaw.uvRect   = new Rect(0f, 0f, 1f, 1f);
                    _floorBgRaw.color    = new Color(1f, 1f, 1f, 0f);
                    StartCoroutine(CrossFadeFloorBackground(0.6f));
                }
                else
                {
                    _floorBgRaw.texture = bgTex;
                    _floorBgRaw.color   = new Color(1f, 1f, 1f, bgTex != null ? 0.92f : 0f);
                    _floorBgRaw.uvRect  = new Rect(0f, 0f, 1f, 1f);
                }
            }

            // Per-floor scrim alpha — ensures text/nodes are readable on each background art
            // Floor 3 Shrine Garden uses highest alpha: amber art risks low contrast with AccentGold text
            if (_bgScrimImg != null)
            {
                var scrimAlpha = run.State.CurrentFloor switch
                {
                    0 => 0.40f, // Garden Grove — bright green, moderate darkening
                    1 => 0.35f, // Koi Garden — darker tones, less needed
                    2 => 0.38f, // Koi Terrace — blue/teal, moderate
                    3 => 0.50f, // Shrine Garden — amber, highest to protect gold text contrast
                    4 => 0.30f, // Demon Summit — already dark volcanic art
                    _ => 0.40f,
                };
                _bgScrimImg.color = new Color(0f, 0f, 0f, scrimAlpha);
            }

            // Panel base image: nearly opaque dark base (scrim child handles the visible darkening)
            if (_pathPanel != null)
            {
                var panelImg = _pathPanel.GetComponent<Image>();
                if (panelImg != null)
                    panelImg.color = new Color(0.04f, 0.06f, 0.05f, 0.90f);
            }

            // Suggestion 3: seasonal ambient particles — (re)initialize when floor changes
            // DestroyImmediate required because Destroy is deferred: AddComponent would return null
            // on the same frame due to [DisallowMultipleComponent] still seeing the old instance.
            if (_pathPanel != null && _ambientParticlesFloor != run.State.CurrentFloor)
            {
                _ambientParticlesFloor = run.State.CurrentFloor;
                var existing = _pathPanel.GetComponent<PathAmbientParticles>();
                if (existing != null)
                    DestroyImmediate(existing);
                _ambientParticles = null;
                var panelRt2 = _pathPanel.GetComponent<RectTransform>();
                if (panelRt2 != null)
                {
                    _ambientParticles = _pathPanel.AddComponent<PathAmbientParticles>();
                    if (_ambientParticles != null)
                        _ambientParticles.Initialize(panelRt2, run.State.CurrentFloor);
                }
            }

            // Thematic 4: floor progress indicator dots at top of path panel
            RebuildFloorProgressStrip(run);

            // Suggestion 1: floor-themed lane names — situates the player inside the world
            var (calmLaneName, riskLaneName) = run.State.CurrentFloor switch
            {
                0 => ("Garden Walk",    "Briar Crossing"),
                1 => ("Koi Shore",      "Vine Path"),
                2 => ("Stone Bridge",   "Deep Current"),
                3 => ("Temple Walk",    "Demon Trail"),
                4 => ("Summit Path",    "Abyss Edge"),
                _ => ("Calm Path",      "Risk Path"),
            };

            // Bridge pairs: crossing a bridge gives access to the partner tile on the other lane,
            // so each lane's reachable tile count includes +1 per bridge pair.
            var bridgePairs = 0;
            for (var i = 0; i < calmLane.Count; i++) if (calmLane[i].IsCrossLink) bridgePairs++;
            var calmDisplayCount = calmRemaining + bridgePairs;
            var riskDisplayCount = riskRemaining + bridgePairs;

            if (calmLane.Count > 0)
                DrawRouteLane(_pathOverlayRoot, calmLane, w, h, calmBg, calmOutline, "CalmLane",
                    $"{calmLaneName}  ·  {calmDisplayCount} tile{(calmDisplayCount == 1 ? "" : "s")}");
            if (riskLane.Count > 0)
                DrawRouteLane(_pathOverlayRoot, riskLane, w, h, riskBg, riskOutline, "RiskLane",
                    $"{riskLaneName}  ·  {riskDisplayCount} tile{(riskDisplayCount == 1 ? "" : "s")}");

            var accentColor   = InRunUiFactory.AccentGold;
            var bossGateColor = GamePalette.BossGateNode;

            // Route-differentiated edge colors (Thematic 2)
            var calmEdgeColor     = new Color(floorTint.r * 0.55f, floorTint.g * 0.72f, floorTint.b * 0.90f, 0.60f);
            var riskEdgeColor     = new Color(Mathf.Min(floorTint.r * 0.90f + 0.15f, 1f), floorTint.g * 0.50f, floorTint.b * 0.40f, 0.70f);
            var crossLinkEdgeCol  = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);
            var goldTrailColor    = new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f);

            // Build set of actually-walked edges from NodePath sequence (A: adjacency fix)
            var walkedEdges = new HashSet<(int, int)>();
            var npList = run.State.NodePath;
            if (npList != null)
                for (var k = 0; k + 1 < npList.Count; k++)
                    walkedEdges.Add((npList[k], npList[k + 1]));

            // Thematic 2: walked-route ground trail — thick, very-low-alpha strip beneath edges
            // Drawn before lane boxes to appear behind the fog bands but above the garden background.
            // Because DrawRouteLane calls SetAsFirstSibling, these trails end up between the background
            // and the lane bands in the sibling order, giving a natural "worn groove in the earth" look.
            foreach (var edge in walkedEdges)
            {
                var src = edge.Item1; var dst = edge.Item2;
                if (src < 0 || src >= graph.Count || dst < 0 || dst >= graph.Count) continue;
                var nA = graph[src]; var nB = graph[dst];
                var isCrossTrail = nA.IsCrossLink && nB.IsCrossLink;
                var trailCol = isCrossTrail
                    ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.055f)
                    : new Color(
                        floorTint.r * 0.60f + 0.10f,
                        floorTint.g * 0.60f + 0.10f,
                        floorTint.b * 0.35f + 0.05f,
                        0.06f);
                DrawGroundTrail(_pathOverlayRoot, w, h, nA, nB, trailCol);
            }

            for (var i = 0; i < graph.Count; i++)
            {
                var nodeI = graph[i];
                for (var n = 0; n < nodeI.NextNodes.Count; n++)
                {
                    var next = nodeI.NextNodes[n];
                    if (next < 0 || next >= graph.Count) continue;
                    var nodeN = graph[next];

                    // Fix 1: use IsCrossLink flags for precision — avoids false positive on Start→Risk edges
                    var isCrossLinkEdge = nodeI.IsCrossLink && nodeN.IsCrossLink;
                    var isWalked = walkedEdges.Contains((i, next)) || walkedEdges.Contains((next, i));

                    if (isCrossLinkEdge)
                    {
                        // Walked bridge: brighter gold dots; unwalked: accent dots + glow aura
                        var dotColor = isWalked
                            ? new Color(goldTrailColor.r, goldTrailColor.g, goldTrailColor.b, 0.85f)
                            : crossLinkEdgeCol;
                        DrawDottedEdge(_pathOverlayRoot, w, h, nodeI, nodeN, dotColor);
                        DrawEdge(_pathOverlayRoot, w, h, nodeI, nodeN,
                            new Color(dotColor.r, dotColor.g, dotColor.b, 0.07f), 10f);
                    }
                    else
                    {
                        // Calm = thinner blue-tinted, Risk = thicker red-tinted (Thematic 2)
                        var isCalm  = nodeI.Route == RouteType.CalmRoute && nodeN.Route == RouteType.CalmRoute;
                        if (isWalked)
                        {
                            // Thematic 8: visited edges thinner than unwalked (worn-groove look)
                            DrawEdge(_pathOverlayRoot, w, h, nodeI, nodeN, goldTrailColor, isCalm ? 1.2f : 2.5f);
                        }
                        else
                        {
                            DrawEdge(_pathOverlayRoot, w, h, nodeI, nodeN,
                                isCalm ? calmEdgeColor : riskEdgeColor,
                                isCalm ? 2f : 4f);
                            // Directional chevron at 65% of each unwalked lane edge
                            DrawEdgeChevron(_pathOverlayRoot, w, h, nodeI, nodeN,
                                isCalm ? calmEdgeColor : riskEdgeColor);
                        }
                    }
                }
            }

            // Suggestion 6: build visit-order map from NodePath (node index → visit step, 1-based)
            var visitOrder = new Dictionary<int, int>();
            if (npList != null)
                for (var k = 0; k < npList.Count; k++)
                    if (!visitOrder.ContainsKey(npList[k]))
                        visitOrder[npList[k]] = k + 1;

            // Draw nodes
            // Collect reachable node buttons for controller navigation
            _gbReachableNodeIndices.Clear();
            _gbReachableNodeButtons.Clear();
            _gbPathCursor = 0;
            var allNodeButtons = new List<(Button btn, float x)>(); // F10: stagger-reveal collection

            for (var i = 0; i < graph.Count; i++)
            {
                var node      = graph[i];
                var pos       = new Vector2(node.CanvasX * w, (1f - node.CanvasY) * h);
                var isCurrent = i == run.State.CurrentNodeIndex;
                var canReach  = reachable != null && reachable.Contains(i);

                var isFirstPick = run.State.NodePath == null || run.State.NodePath.Count == 0;
                var isInvalidFirstPick = isFirstPick && !IsPuzzleNodeType(node.Type)
                    && node.Type != NodeType.Start;

                var color = node.Visited ? new Color(floorTint.r, floorTint.g, floorTint.b, 0.4f)
                    : isCurrent ? accentColor
                    : node.Type == NodeType.Boss ? bossGateColor
                    : node.Type == NodeType.CrossLink
                        // Suggestion 5: bridge color blends with floorTint so bridges feel native to each floor
                        ? Color.Lerp(
                            new Color(accentColor.r * 0.60f, accentColor.g * 0.50f, accentColor.b * 0.12f, 0.90f),
                            new Color(floorTint.r, floorTint.g, floorTint.b, 0.90f),
                            0.40f)
                    : isInvalidFirstPick ? new Color(0.25f, 0.25f, 0.28f, 0.55f)
                    : floorTint;

                var nodeConfig = IsPuzzleNodeType(node.Type) ? _map.GetFixedLevelConfig(node) : null;

                // Thematic 5: detect if this bridge node was used as a departure point this run
                var bridgeWasUsed = false;
                if (node.IsCrossLink && node.Visited)
                {
                    for (var nn = 0; nn < node.NextNodes.Count; nn++)
                    {
                        var ni2 = node.NextNodes[nn];
                        if (ni2 < 0 || ni2 >= graph.Count) continue;
                        if (graph[ni2].Route != node.Route &&
                            (walkedEdges.Contains((i, ni2)) || walkedEdges.Contains((ni2, i))))
                        {
                            bridgeWasUsed = true;
                            break;
                        }
                    }
                }

                var btn = CreateNodeButton(_pathOverlayRoot, node, pos, color, nodeConfig, floorTint, bridgeWasUsed);
                btn.interactable = canReach && !node.Visited && !isInvalidFirstPick;
                allNodeButtons.Add((btn, pos.x)); // F10

                // Thematic 6: track boss node transform for the reachable-pulse loop
                if (node.Type == NodeType.Boss) _bossNodeTransform = btn.transform;

                // Visual state decorators
                var btnRt = btn.GetComponent<RectTransform>();
                if (node.Type == NodeType.Start) AddStartHalo(btnRt);
                if (node.Type == NodeType.Boss)  AddBossRing(btnRt);
                if (canReach && !node.Visited && !isCurrent)
                {
                    if (node.IsCrossLink) AddBridgeRing(btnRt);
                    else                  AddAvailableRing(btnRt);
                }
                if (node.Visited)
                {
                    var visitStep = visitOrder.TryGetValue(i, out var step) ? step : 0;
                    AddVisitedMark(btnRt, node, visitStep);
                }

                // #9 — brief gold ring fade on the node just completed (only after reward screen dismissed)
                if (_completedRingArmed && i == _justCompletedNodeIndex && node.Visited)
                    StartCoroutine(FadeOutCompletedRing(btn.GetComponent<RectTransform>()));

                var capturedNode   = node;
                var capturedConfig = nodeConfig;
                var idx = i;
                var btnTransform = btn.transform;
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(AnimationHelper.PulseScale(btnTransform, 1f, 1.18f,
                        AnimationHelper.NumberPlaceDuration));
                    OnNodeClicked(idx);
                });

                // Hover tooltip via EventTrigger (P-2: invalid first-pick gets a dedicated message)
                var capturedIsInvalidFirst = isInvalidFirstPick;
                var et = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener(evtData =>
                {
                    if (capturedNode.Visited) return;  // node already completed — no tooltip needed
                    var pos = ((UnityEngine.EventSystems.PointerEventData)evtData).position;
                    if (capturedIsInvalidFirst)
                        ShowFirstPickBlockedTooltip(pos);
                    else
                        ShowNodeTooltip(capturedNode, capturedConfig, pos);
                });
                et.triggers.Add(enterEntry);
                var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exitEntry.callback.AddListener(_ => HideNodeTooltip());
                et.triggers.Add(exitEntry);

                if (canReach && !node.Visited)
                {
                    _gbReachableNodeIndices.Add(i);
                    _gbReachableNodeButtons.Add(btn);
                }
            }

            // #8 — On first pick, pulse all valid reachable puzzle nodes to guide the player
            var isFirstPickPost = run.State.NodePath == null || run.State.NodePath.Count == 0;
            if (isFirstPickPost)
            {
                for (var ni = 0; ni < _gbReachableNodeButtons.Count; ni++)
                {
                    var ni2 = _gbReachableNodeIndices[ni];
                    if (ni2 < graph.Count && IsPuzzleNodeType(graph[ni2].Type))
                        StartCoroutine(AnimationHelper.PulseScale(
                            _gbReachableNodeButtons[ni].transform, 1f, 1.15f, 0.45f));
                }
            }

            // #3 — Pulse nodes that became newly reachable since last refresh (skip on first pick — #8 handles it)
            for (var ni = 0; ni < _gbReachableNodeIndices.Count; ni++)
            {
                if (!isFirstPickPost && !_prevReachableIndices.Contains(_gbReachableNodeIndices[ni]))
                    StartCoroutine(AnimationHelper.PulseScale(
                        _gbReachableNodeButtons[ni].transform, 1f, 1.28f, 0.30f));
            }

            // F10 — Stagger-reveal: small scale pop on each node left-to-right after canvas rebuilds
            allNodeButtons.Sort((a, b) => a.x.CompareTo(b.x));
            StartCoroutine(StaggerRevealNodes(allNodeButtons));
            _prevReachableIndices.Clear();
            for (var ni = 0; ni < _gbReachableNodeIndices.Count; ni++)
                _prevReachableIndices.Add(_gbReachableNodeIndices[ni]);

            // Thematic 6: start a slow repeating pulse on the boss node when it is reachable
            if (_bossNodeTransform != null && reachable != null)
            {
                var bossIsReachable = false;
                for (var bi = 0; bi < graph.Count; bi++)
                    if (graph[bi].Type == NodeType.Boss && reachable.Contains(bi))
                    { bossIsReachable = true; break; }
                if (bossIsReachable)
                    _bossRingPulse = StartCoroutine(BossNodePulseLoop(_bossNodeTransform));
            }

            // Scroll to center the current node in the viewport
            ScrollToNode(run.State.CurrentNodeIndex, w, viewW);

            // Legend strip: fixed to _pathPanel (not scrollable root), rebuilt each refresh
            if (_pathPanel != null)
            {
                var existingLegend = _pathPanel.transform.Find("PathLegend");
                if (existingLegend != null)
                {
                    Destroy(existingLegend.gameObject);
                    if (_saveQuitResetCo != null) { StopCoroutine(_saveQuitResetCo); _saveQuitResetCo = null; }
                    _saveQuitPending = false;
                    _saveQuitImg = null;
                    _saveQuitLbl = null;
                }

                // Container spans the bottom strip — pixel-exact to match scroll area offset
                var legendGo = new GameObject("PathLegend", typeof(RectTransform), typeof(Image));
                legendGo.transform.SetParent(_pathPanel.transform, false);
                var legendRt = legendGo.GetComponent<RectTransform>();
                legendRt.anchorMin = new Vector2(0f, 0f);
                legendRt.anchorMax = new Vector2(1f, 0f);
                legendRt.offsetMin = Vector2.zero;
                legendRt.offsetMax = new Vector2(0f, LegendH);

                // Legend background bar (keeps icons readable over the garden background)
                var legendBg = legendGo.GetComponent<Image>();
                legendBg.color = new Color(0.06f, 0.07f, 0.10f, 0.92f);
                legendBg.raycastTarget = false;

                // Top separator line
                var sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
                sep.transform.SetParent(legendRt, false);
                var sepRt = sep.GetComponent<RectTransform>();
                sepRt.anchorMin = new Vector2(0f, 1f);
                sepRt.anchorMax = new Vector2(1f, 1f);
                sepRt.offsetMin = Vector2.zero;
                sepRt.offsetMax = new Vector2(0f, 1f);
                var sepImg = sep.GetComponent<Image>();
                sepImg.color = new Color(1f, 1f, 1f, 0.12f);
                sepImg.raycastTarget = false;

                // "Legend" title label (before icon list)
                const float legendTitleW = 0.10f;
                var title = InRunUiFactory.CreateText(legendRt, "LegendTitle", "Legend",
                    9, TextAnchor.MiddleLeft, new Color(0.82f, 0.80f, 0.70f, 0.90f));
                title.fontStyle = FontStyle.Bold;
                title.rectTransform.anchorMin = new Vector2(0f, 0f);
                title.rectTransform.anchorMax = new Vector2(legendTitleW, 1f);
                title.rectTransform.offsetMin = new Vector2(8f, 0f);
                title.rectTransform.offsetMax = new Vector2(-2f, 0f);
                title.raycastTarget = false;

                // Legend entries reflect what players actually encounter on the path.
                // Event nodes were removed in R36 so they are excluded here.
                var legendEntries = new (string Path, string Label)[]
                {
                    ("node/icon_puzzle_tile",     "Puzzle"),
                    ("node/icon_elite_mask",      "Elite"),
                    ("node/icon_preboss_gate",    "Pre-Boss"),
                    ("node/icon_demon_mask",      "Boss"),
                    ("node/icon_market_stall",    "Shop"),
                    ("node/icon_campfire_stones", "Rest"),
                    ("node/icon_relic_pedestal",  "Relic"),
                    ("node/icon_moss_trap",       "Cursed"),
                };

                var entryCount = legendEntries.Length;
                const float legendEntriesEnd = 0.80f; // right 20% reserved for Save & Quit button
                var legendColor = new Color(0.78f, 0.75f, 0.62f, 0.75f);
                for (var ei = 0; ei < entryCount; ei++)
                {
                    var (iconPath, label) = legendEntries[ei];
                    var xMin = legendTitleW + (float)ei / entryCount * (legendEntriesEnd - legendTitleW);
                    var xMax = legendTitleW + (float)(ei + 1) / entryCount * (legendEntriesEnd - legendTitleW);

                    var entryGo = new GameObject($"LegendEntry_{label}", typeof(RectTransform));
                    entryGo.transform.SetParent(legendRt, false);
                    var entryRt = entryGo.GetComponent<RectTransform>();
                    entryRt.anchorMin = new Vector2(xMin, 0f);
                    entryRt.anchorMax = new Vector2(xMax, 1f);
                    entryRt.offsetMin = Vector2.zero;
                    entryRt.offsetMax = Vector2.zero;

                    var sprite = Resources.Load<Sprite>(iconPath);
                    if (sprite != null)
                    {
                        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                        iconGo.transform.SetParent(entryRt, false);
                        var iconRt = iconGo.GetComponent<RectTransform>();
                        iconRt.anchorMin = new Vector2(0.04f, 0.12f);
                        iconRt.anchorMax = new Vector2(0.36f, 0.88f);
                        iconRt.offsetMin = Vector2.zero;
                        iconRt.offsetMax = Vector2.zero;
                        var iconImg = iconGo.GetComponent<Image>();
                        iconImg.sprite = sprite;
                        iconImg.preserveAspect = true;
                        iconImg.raycastTarget  = false;

                        var lbl = InRunUiFactory.CreateText(entryRt, "Label", label,
                            9, TextAnchor.MiddleLeft, legendColor);
                        lbl.rectTransform.anchorMin = new Vector2(0.38f, 0f);
                        lbl.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                        lbl.rectTransform.offsetMin = Vector2.zero;
                        lbl.rectTransform.offsetMax = Vector2.zero;
                        lbl.resizeTextForBestFit = true;
                        lbl.resizeTextMinSize   = 7;
                        lbl.resizeTextMaxSize   = 9;
                        lbl.raycastTarget = false;
                    }
                    else
                    {
                        var lbl = InRunUiFactory.CreateText(entryRt, "Label", label,
                            9, TextAnchor.MiddleCenter, legendColor);
                        InRunUiFactory.StretchFill(lbl.rectTransform);
                        lbl.resizeTextForBestFit = true;
                        lbl.resizeTextMinSize   = 7;
                        lbl.resizeTextMaxSize   = 9;
                        lbl.raycastTarget = false;
                    }
                }

                // Thin vertical divider between legend entries and Save & Quit button
                var divGo = new GameObject("LegendDivider", typeof(RectTransform), typeof(Image));
                divGo.transform.SetParent(legendRt, false);
                var divRt = divGo.GetComponent<RectTransform>();
                divRt.anchorMin = new Vector2(legendEntriesEnd, 0.08f);
                divRt.anchorMax = new Vector2(legendEntriesEnd, 0.92f);
                divRt.offsetMin = Vector2.zero;
                divRt.offsetMax = new Vector2(1f, 0f); // 1 px wide
                var divImg = divGo.GetComponent<Image>();
                divImg.color         = new Color(1f, 1f, 1f, 0.18f);
                divImg.raycastTarget = false;

                // Save & Quit button — right end of legend bar, always above map content
                var sqGo = new GameObject("BtnSaveQuit", typeof(RectTransform), typeof(Image), typeof(Button));
                sqGo.transform.SetParent(legendRt, false);
                var sqRt = sqGo.GetComponent<RectTransform>();
                sqRt.anchorMin = new Vector2(legendEntriesEnd + 0.01f, 0.08f);
                sqRt.anchorMax = new Vector2(0.99f, 0.92f);
                sqRt.offsetMin = sqRt.offsetMax = Vector2.zero;
                var sqImg = sqGo.GetComponent<Image>();
                sqImg.color = new Color(0.22f, 0.18f, 0.14f, 0.90f);
                var sqBtn = sqGo.GetComponent<Button>();
                var sqCb = new ColorBlock();
                sqCb.normalColor      = new Color(0.22f, 0.18f, 0.14f, 0.90f);
                sqCb.highlightedColor = new Color(0.38f, 0.30f, 0.22f, 1.00f);
                sqCb.pressedColor     = new Color(0.12f, 0.10f, 0.08f, 1.00f);
                sqCb.selectedColor    = sqCb.normalColor;
                sqCb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                sqCb.colorMultiplier  = 1f;
                sqCb.fadeDuration     = 0.1f;
                sqBtn.colors          = sqCb;
                sqBtn.targetGraphic   = sqImg;
                _saveQuitImg = sqImg;
                sqBtn.onClick.AddListener(() =>
                {
                    if (!_saveQuitPending)
                    {
                        _saveQuitPending = true;
                        if (_saveQuitLbl != null) _saveQuitLbl.text = "Confirm?";
                        if (_saveQuitImg != null) _saveQuitImg.color = new Color(0.55f, 0.18f, 0.10f, 0.95f);
                        if (_saveQuitResetCo != null) StopCoroutine(_saveQuitResetCo);
                        _saveQuitResetCo = StartCoroutine(ResetSaveQuitConfirm(3f));
                    }
                    else
                    {
                        if (_saveQuitResetCo != null) { StopCoroutine(_saveQuitResetCo); _saveQuitResetCo = null; }
                        _saveQuitPending = false;
                        SaveAndQuit();
                    }
                });
                var sqIconSprite = Resources.Load<Sprite>("ui/icon_ink_save");
                if (sqIconSprite != null)
                {
                    var sqIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    sqIconGo.transform.SetParent(sqRt, false);
                    var sqIconRt = sqIconGo.GetComponent<RectTransform>();
                    sqIconRt.anchorMin = new Vector2(0.04f, 0.10f);
                    sqIconRt.anchorMax = new Vector2(0.22f, 0.90f);
                    sqIconRt.offsetMin = sqIconRt.offsetMax = Vector2.zero;
                    var sqIconImg = sqIconGo.GetComponent<Image>();
                    sqIconImg.sprite       = sqIconSprite;
                    sqIconImg.preserveAspect = true;
                    sqIconImg.raycastTarget  = false;
                }
                var sqLbl = InRunUiFactory.CreateText(sqRt, "Label", "Save & Quit",
                    11, TextAnchor.MiddleLeft, new Color(0.92f, 0.88f, 0.78f, 1f));
                sqLbl.rectTransform.anchorMin = new Vector2(0.25f, 0f);
                sqLbl.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                sqLbl.rectTransform.offsetMin = sqLbl.rectTransform.offsetMax = Vector2.zero;
                sqLbl.resizeTextForBestFit = true;
                sqLbl.resizeTextMinSize   = 9;
                sqLbl.resizeTextMaxSize   = 11;
                sqLbl.raycastTarget = false;
                _saveQuitLbl = sqLbl;
            }

            // Floor transition: fade the path canvas in each rebuild
            var pathCg = _pathOverlayRoot.GetComponent<CanvasGroup>();
            if (pathCg != null) StartCoroutine(AnimationHelper.FadeIn(pathCg, 0.22f));
        }

        /// <summary>Small V-chevron at 65% of an edge, indicating travel direction.</summary>
        private void DrawEdgeChevron(RectTransform parent, float w, float h, RunNode a, RunNode b, Color color)
        {
            var pa = new Vector2(a.CanvasX * w, (1f - a.CanvasY) * h);
            var pb = new Vector2(b.CanvasX * w, (1f - b.CanvasY) * h);
            var d = pb - pa;
            var dist = d.magnitude;
            if (dist < 24f) return;
            var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            var tip = pa + d * 0.65f;
            var chevronColor = new Color(color.r, color.g, color.b, color.a * 0.55f);
            for (var side = -1; side <= 1; side += 2)
            {
                var seg = new GameObject("Chevron", typeof(RectTransform), typeof(Image));
                seg.transform.SetParent(parent, false);
                var segImg = seg.GetComponent<Image>();
                segImg.color = chevronColor;
                segImg.raycastTarget = false;
                var segRt = seg.GetComponent<RectTransform>();
                segRt.anchorMin = Vector2.zero;
                segRt.anchorMax = Vector2.zero;
                segRt.pivot = new Vector2(0f, 0.5f);
                segRt.sizeDelta = new Vector2(7f, 1.5f);
                segRt.anchoredPosition = tip;
                segRt.localEulerAngles = new Vector3(0f, 0f, angle + 180f + side * 32f);
            }
        }

        /// <summary>Gold double-ring for reachable bridge nodes — more prominent than the standard ring.</summary>
        private static void AddBridgeRing(RectTransform parent)
        {
            // Outer gold ring
            var outer = new GameObject("BridgeRingOuter", typeof(RectTransform), typeof(Image));
            outer.transform.SetParent(parent, false);
            outer.transform.SetAsFirstSibling();
            var outerRt = outer.GetComponent<RectTransform>();
            outerRt.anchorMin = new Vector2(-0.22f, -0.22f);
            outerRt.anchorMax = new Vector2(1.22f,  1.22f);
            outerRt.offsetMin = outerRt.offsetMax = Vector2.zero;
            var outerImg = outer.GetComponent<Image>();
            outerImg.color = new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.05f);
            outerImg.raycastTarget = false;
            var outerOl = outer.AddComponent<Outline>();
            outerOl.effectColor = new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.70f);
            outerOl.effectDistance = new Vector2(4f, -4f);
            // Inner ring (tighter)
            var inner = new GameObject("BridgeRingInner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(parent, false);
            inner.transform.SetAsFirstSibling();
            var innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(-0.11f, -0.11f);
            innerRt.anchorMax = new Vector2(1.11f,  1.11f);
            innerRt.offsetMin = innerRt.offsetMax = Vector2.zero;
            var innerImg = inner.GetComponent<Image>();
            innerImg.color = new Color(1f, 0.85f, 0.20f, 0.04f);
            innerImg.raycastTarget = false;
            var innerOl = inner.AddComponent<Outline>();
            innerOl.effectColor = new Color(1f, 0.85f, 0.20f, 0.55f);
            innerOl.effectDistance = new Vector2(2f, -2f);
        }

        /// <summary>Always-visible green halo for the Start node — marks the entry point.</summary>
        private static void AddStartHalo(RectTransform parent)
        {
            var go = new GameObject("StartHalo", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.20f, -0.20f);
            rt.anchorMax = new Vector2(1.20f,  1.20f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.85f, 0.30f, 0.05f);
            img.raycastTarget = false;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0.28f, 0.82f, 0.28f, 0.68f);
            ol.effectDistance = new Vector2(3f, -3f);
        }

        /// <summary>Always-visible red ring for the Boss gate node — marks the destination.</summary>
        private static void AddBossRing(RectTransform parent)
        {
            var go = new GameObject("BossRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.10f, -0.10f);
            rt.anchorMax = new Vector2(1.10f,  1.10f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(GamePalette.DangerRed.r, GamePalette.DangerRed.g, GamePalette.DangerRed.b, 0.06f);
            img.raycastTarget = false;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(GamePalette.DangerRed.r, GamePalette.DangerRed.g, GamePalette.DangerRed.b, 0.72f);
            ol.effectDistance = new Vector2(4f, -4f);
        }

        private void DrawRouteLane(RectTransform parent, List<RunNode> laneNodes, float w, float h,
            Color boxColor, Color outlineColor, string goName, string label)
        {
            const float padPx = 40f;

            var minPx = float.MaxValue; var maxPx = float.MinValue;
            var minPy = float.MaxValue; var maxPy = float.MinValue;
            for (var i = 0; i < laneNodes.Count; i++)
            {
                var px = laneNodes[i].CanvasX * w;
                var py = (1f - laneNodes[i].CanvasY) * h;
                if (px < minPx) minPx = px;
                if (px > maxPx) maxPx = px;
                if (py < minPy) minPy = py;
                if (py > maxPy) maxPy = py;
            }

            var boxW    = (maxPx - minPx) + padPx * 2f;
            var boxH    = (maxPy - minPy) + padPx * 2f;
            var centerX = (minPx + maxPx) * 0.5f;
            var centerY = (minPy + maxPy) * 0.5f;

            var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centerX, centerY);
            rt.sizeDelta = new Vector2(boxW, boxH);

            var img = go.GetComponent<Image>();
            img.color = boxColor;
            img.raycastTarget = false;

            // Fog-band: subtle top-edge separator line instead of hard outline
            var sep = new GameObject("LaneSep", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(go.transform, false);
            var sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 0.965f);
            sepRt.anchorMax = Vector2.one;
            sepRt.offsetMin = sepRt.offsetMax = Vector2.zero;
            var sepImg = sep.GetComponent<Image>();
            sepImg.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.40f);
            sepImg.raycastTarget = false;

            // Lane label: dark pill above the lane box ensures readability over all floor background art
            var lblPill = new GameObject("LaneLabelBg", typeof(RectTransform), typeof(Image));
            lblPill.transform.SetParent(go.transform, false);
            var lblPillRt = lblPill.GetComponent<RectTransform>();
            lblPillRt.anchorMin = new Vector2(0f, 1f);
            lblPillRt.anchorMax = new Vector2(0.85f, 1f);
            lblPillRt.offsetMin = new Vector2(4f, 2f);
            lblPillRt.offsetMax = new Vector2(-4f, 28f);
            lblPill.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var lbl = InRunUiFactory.CreateText(go.transform, "LaneLabel", label, 12,
                TextAnchor.UpperLeft, new Color(GamePalette.TextPrimary.r, GamePalette.TextPrimary.g, GamePalette.TextPrimary.b, 0.90f));
            lbl.rectTransform.anchorMin = new Vector2(0f, 1f);
            lbl.rectTransform.anchorMax = new Vector2(0.85f, 1f);
            lbl.rectTransform.offsetMin = new Vector2(6f, 4f);
            lbl.rectTransform.offsetMax = new Vector2(0f, 26f);
            lbl.raycastTarget = false;

            // #4 — Lane crossing rule tooltip: (?) button in top-right of lane box
            // tipPanel is reparented to _pathPanel so it uses canvas-space clamped positioning.
            var tipPanel = new GameObject("LaneTip", typeof(RectTransform), typeof(Image));
            tipPanel.transform.SetParent(_pathPanel.transform, false);
            var tipPanelRt2 = tipPanel.GetComponent<RectTransform>();
            tipPanelRt2.anchorMin = new Vector2(0.5f, 0.5f);
            tipPanelRt2.anchorMax = new Vector2(0.5f, 0.5f);
            tipPanelRt2.pivot     = new Vector2(0f, 1f);
            tipPanelRt2.sizeDelta = new Vector2(240f, 56f);
            tipPanel.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.95f);
            var tipCanvas = tipPanel.AddComponent<Canvas>();
            tipCanvas.overrideSorting = true;
            tipCanvas.sortingOrder = 50;
            tipPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var tipTxt = InRunUiFactory.CreateText(tipPanel.transform, "TipTxt",
                "You may cross lanes at the cost of lane balance — each route must have tiles remaining.",
                9, TextAnchor.MiddleLeft, new Color(0.90f, 0.87f, 0.72f, 1f));
            tipTxt.rectTransform.anchorMin = Vector2.zero;
            tipTxt.rectTransform.anchorMax = Vector2.one;
            tipTxt.rectTransform.offsetMin = new Vector2(4f, 2f);
            tipTxt.rectTransform.offsetMax = new Vector2(-4f, -2f);
            tipTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            tipTxt.verticalOverflow   = VerticalWrapMode.Overflow;
            tipTxt.raycastTarget = false;
            tipPanel.SetActive(false);

            var helpBtn = new GameObject("LaneHelpBtn", typeof(RectTransform), typeof(Image));
            helpBtn.transform.SetParent(go.transform, false);
            var helpRt = helpBtn.GetComponent<RectTransform>();
            helpRt.anchorMin = new Vector2(0.76f, 0.80f);
            helpRt.anchorMax = new Vector2(1.00f, 1.00f);
            helpRt.offsetMin = helpRt.offsetMax = Vector2.zero;
            helpBtn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var helpTxt = InRunUiFactory.CreateText(helpBtn.transform, "HelpLbl", "(?)", 9,
                TextAnchor.MiddleCenter,
                new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.70f));
            helpTxt.rectTransform.anchorMin = Vector2.zero;
            helpTxt.rectTransform.anchorMax = Vector2.one;
            helpTxt.rectTransform.offsetMin = helpTxt.rectTransform.offsetMax = Vector2.zero;
            helpTxt.raycastTarget = false;
            var helpBtnComp = helpBtn.AddComponent<Button>();
            helpBtnComp.transition = Selectable.Transition.None;
            var etHelp = helpBtn.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var panelRtCaptured = _pathPanel.GetComponent<RectTransform>();
            var hoverIn = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            hoverIn.callback.AddListener(evtData =>
            {
                var screenPos = ((UnityEngine.EventSystems.PointerEventData)evtData).position;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    panelRtCaptured, screenPos, null, out var local);
                tipPanelRt2.anchoredPosition = ClampedTooltipPosition(local, panelRtCaptured,
                    240f, 56f, 12f);
                tipPanel.SetActive(true);
            });
            etHelp.triggers.Add(hoverIn);
            var hoverOut = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            hoverOut.callback.AddListener(_ => tipPanel.SetActive(false));
            etHelp.triggers.Add(hoverOut);
            // P-6 — Also toggle on click so controller/keyboard can open the tip
            var tipVisible = new bool[1];
            helpBtnComp.onClick.AddListener(() =>
            {
                tipVisible[0] = !tipVisible[0];
                tipPanel.SetActive(tipVisible[0]);
            });
        }

        private void OnNodeClicked(int nodeIndex)
        {
            if (_rewardView.IsAwaitingReward || _bossGateView.IsAwaiting) return;
            var run = _map?.Run;
            if (run == null) return;

            var graph = run.CurrentFloorGraph;
            if (graph == null || nodeIndex < 0 || nodeIndex >= graph.Count) return;
            var node = graph[nodeIndex];

            // Fresh-run constraint: the first tile selected must be a puzzle tile
            var np = run.State.NodePath;
            if ((np == null || np.Count == 0) && run.CurrentBoard == null && !IsPuzzleNodeType(node.Type))
            {
                SetStatus(node.Type == NodeType.CrossLink
                    ? "Start on a puzzle tile first — bridges can be crossed after your first tile."
                    : "Your first tile must be a puzzle tile.");
                return;
            }

            // Lane-switch balance: enforce remaining-length rules when crossing between routes
            var isCrossRoute = false;
            var currentIdx = run.State.CurrentNodeIndex;
            if (currentIdx >= 0 && currentIdx < graph.Count)
            {
                var currentNode = graph[currentIdx];
                if (currentNode.Type != NodeType.Start &&
                    node.Type != NodeType.Boss && node.Type != NodeType.Start &&
                    currentNode.Route != node.Route)
                {
                    isCrossRoute = true;

                    // Phase 3: explicit bridge crossing — both ends are IsCrossLink and directly
                    // connected via NextNodes. Skip the lane-balance check entirely; the bridge
                    // itself is the designed crossing point and is always valid.
                    var isExplicitBridge = graph[currentIdx].IsCrossLink
                        && graph[currentIdx].NextNodes.Contains(nodeIndex)
                        && graph[nodeIndex].IsCrossLink;

                    if (!isExplicitBridge)
                    {
                    // Count remaining tiles on each route from this switch point (ignore earlier unvisited tiles).
                    int NextOnSameRouteOrBoss(int idx)
                    {
                        if (idx < 0 || idx >= graph.Count) return -1;
                        var n = graph[idx];
                        var best = -1;
                        for (var ni = 0; ni < n.NextNodes.Count; ni++)
                        {
                            var nextIdx = n.NextNodes[ni];
                            if (nextIdx < 0 || nextIdx >= graph.Count) continue;
                            var next = graph[nextIdx];
                            if (next.Type == NodeType.Boss) return nextIdx;
                            if (next.Route == n.Route) best = nextIdx;
                        }
                        return best;
                    }

                    int CountRouteTilesToBoss(int startIdx)
                    {
                        if (startIdx < 0 || startIdx >= graph.Count) return 0;
                        var count = 0;
                        var idx2 = startIdx;
                        var guard = 0;
                        while (idx2 >= 0 && idx2 < graph.Count && guard++ < graph.Count + 4)
                        {
                            var n2 = graph[idx2];
                            if (n2.Type == NodeType.Boss) break;
                            if (n2.Type != NodeType.Start) count++;
                            idx2 = NextOnSameRouteOrBoss(idx2);
                            if (idx2 < 0) break;
                        }
                        return count;
                    }

                    var fromRemaining = CountRouteTilesToBoss(NextOnSameRouteOrBoss(currentIdx));
                    var toRemaining   = CountRouteTilesToBoss(nodeIndex);

                    bool blocked;
                    string blockReason;
                    if (currentNode.Route == RouteType.RiskRoute)
                    {
                        // Risk → Calm: calm must have strictly more tiles remaining
                        blocked     = toRemaining <= fromRemaining;
                        blockReason = $"Switch blocked: Calm needs more tiles than Risk ({toRemaining} vs {fromRemaining}).";
                    }
                    else
                    {
                        // Calm → Risk: risk must have strictly fewer tiles remaining (and at least one)
                        blocked     = toRemaining >= fromRemaining || toRemaining <= 0;
                        blockReason = toRemaining <= 0
                            ? "No Risk path tiles remain."
                            : $"Switch blocked: Risk must have fewer tiles than Calm ({toRemaining} vs {fromRemaining}).";
                    }

                    if (blocked) { SetStatus(blockReason); return; }
                    } // end !isExplicitBridge
                }
            }

            // Boss gate requires modifier choice first (only if modifiers not yet chosen)
            if (node.Type == NodeType.Boss && (run.State.ChosenBossModifiers == null || run.State.ChosenBossModifiers.Count == 0))
            {
                _bossNodeIndex = nodeIndex;
                _bossGateView.ShowBossGateChoice();
                return;
            }

            if (!_map.TryAdvanceToNodeAndStartPuzzle(nodeIndex, out var arrivedNode, out var level, forced: isCrossRoute))
            {
                SetStatus("Cannot move there.");
                return;
            }

            _pathMessage       = string.Empty;
            _completionHandled = false;
            _boardView.MarkOverlayDirty();
            _selectedRow    = -1;
            _selectedCol    = -1;
            _highlightValue = 0;
            _blurredPencilCells.Clear();
            CancelPendingItemInteraction(false);
            _bagHighlightCells.Clear();
            _bagHighlightEndTime = 0f;
            _audio?.PlayPathAdvance();

            // Park narrative flavor text
            var flavorText = SudokuRoguelike.Run.ParkNarrativeService.GetNodeFlavor(
                arrivedNode.Type, _map.Run.State.CurrentFloor);
            if (!string.IsNullOrEmpty(flavorText))
            {
                _pathMessage = flavorText;
                StartCoroutine(ClearPathMessageAfterDelay(4f));
            }

            if (arrivedNode.Type == NodeType.Shop) { _shopView.ShowShop(); return; }
            if (arrivedNode.Type == NodeType.Rest) { _rewardView.ShowRestChoicePanel(); ShowPath(); return; }
            if (arrivedNode.Type == NodeType.Event)
            {
                ShowPath();
                // Use the proper event choice panel (built by InRunUiBlueprintBuilder)
                if (_uiFlowCtrl != null)
                    _uiFlowCtrl.OnNodeEntered(NodeType.Event);
                else
                    _rewardView.HandleEvent(); // fallback: auto-pick option 0
                return;
            }
            if (arrivedNode.Type == NodeType.Relic)
            {
                var run2    = _map.Run;
                var choices = run2.RollRelicChoices(3);
                if (choices != null && choices.Count > 0) _rewardView.ShowRelicChoicePanel(choices);
                ShowPath();
                return;
            }
            if (arrivedNode.Type == NodeType.Cursed)
            {
                ShowPath();
                var cursedNode = arrivedNode;
                _bossGateView.ShowCursedNodePanel(_pathPanel, cursedNode,
                    cfg =>
                    {
                        _map.Run.StartLevel(cfg);
                        _completionHandled = false; _boardView.MarkOverlayDirty();
                        _selectedRow = -1; _selectedCol = -1; _highlightValue = 0;
                        CancelPendingItemInteraction(false);
                        _bagHighlightCells.Clear();
                        _bagHighlightEndTime = 0f;
                        SetStatus($"Cursed {cfg.BoardSize}×{cfg.BoardSize} {cfg.Stars}★");
                        ShowSudoku(); RebuildBoard();
                    },
                    cfg =>
                    {
                        _map.Run.StartLevel(cfg);
                        _completionHandled = false; _boardView.MarkOverlayDirty();
                        _selectedRow = -1; _selectedCol = -1; _highlightValue = 0;
                        CancelPendingItemInteraction(false);
                        _bagHighlightCells.Clear();
                        _bagHighlightEndTime = 0f;
                        SetStatus($"{cfg.BoardSize}×{cfg.BoardSize} {cfg.Stars}★");
                        ShowSudoku(); RebuildBoard();
                    });
                return;
            }

            if (level == null) { ShowPath(); return; }

            SetStatus($"{arrivedNode.Type} - {level.BoardSize}x{level.BoardSize} {level.Stars}*");
            ShowSudoku();
            RebuildBoard();
        }

        // ─────────────────────────── Garden path background ───────────────────────────
        // Texture creation moved to GardenTextureFactory — see GardenTextureFactory.cs

        /// <summary>
        /// Draws a thick, very-low-alpha rectangle connecting two node positions.
        /// Used for the walked-route ground trail beneath edge lines (Thematic 2).
        /// </summary>
        private static void DrawGroundTrail(RectTransform parent, float w, float h,
            RunNode a, RunNode b, Color color)
        {
            var pa  = new Vector2(a.CanvasX * w, (1f - a.CanvasY) * h);
            var pb  = new Vector2(b.CanvasX * w, (1f - b.CanvasY) * h);
            var dir = pb - pa;
            var len = dir.magnitude;
            if (len < 1f) return;

            var go = new GameObject("GroundTrail", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(len, 10f);
            rt.anchoredPosition = (pa + pb) * 0.5f;
            rt.localRotation    = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            var img = go.GetComponent<Image>();
            img.color         = color;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Destroys and recreates the floor progress dot strip at the top of <c>_pathPanel</c>.
        /// Shows one dot per floor: filled/gold for completed, pulsing for current, dim for upcoming.
        /// </summary>
        private void RebuildFloorProgressStrip(RunDirector run)
        {
            if (_pathPanel == null) return;
            if (_floorProgressStrip != null) Destroy(_floorProgressStrip.gameObject);
            _floorProgressStrip = null;

            var state        = run.State;
            if (state == null) return;
            var totalFloors  = state.TotalFloors;
            var currentFloor = state.CurrentFloor;

            var stripGo = new GameObject("FloorProgressBar", typeof(RectTransform), typeof(Image));
            stripGo.transform.SetParent(_pathPanel.transform, false);
            var stripRt = stripGo.GetComponent<RectTransform>();
            // Anchored to top of path panel, 22 px tall
            stripRt.anchorMin = new Vector2(0f, 1f);
            stripRt.anchorMax = new Vector2(1f, 1f);
            stripRt.pivot     = new Vector2(0.5f, 1f);
            stripRt.offsetMin = new Vector2(0f, -22f);
            stripRt.offsetMax = Vector2.zero;
            var stripImg = stripGo.GetComponent<Image>();
            stripImg.color         = new Color(0f, 0f, 0f, 0.35f);
            stripImg.raycastTarget = false;

            for (var fi = 0; fi < totalFloors; fi++)
            {
                var dotGo = new GameObject($"FloorDot_{fi}", typeof(RectTransform), typeof(Image));
                dotGo.transform.SetParent(stripRt, false);
                var dotRt = dotGo.GetComponent<RectTransform>();
                var xMin  = (float)fi / totalFloors;
                var xMax  = (float)(fi + 1) / totalFloors;
                dotRt.anchorMin = new Vector2(xMin + 0.01f, 0.12f);
                dotRt.anchorMax = new Vector2(xMax - 0.01f, 0.88f);
                dotRt.offsetMin = dotRt.offsetMax = Vector2.zero;

                var dotImg = dotGo.GetComponent<Image>();
                dotImg.raycastTarget = false;

                if (fi < currentFloor)
                {
                    // Completed floor: solid gold
                    dotImg.color = new Color(
                        GamePalette.AccentGold.r, GamePalette.AccentGold.g,
                        GamePalette.AccentGold.b, 0.80f);
                }
                else if (fi == currentFloor)
                {
                    // Active floor: bright gold + single pulse
                    dotImg.color = GamePalette.AccentGold;
                    StartCoroutine(AnimationHelper.PulseScale(dotGo.transform, 1f, 1.22f, 0.55f));
                }
                else
                {
                    // Future floor: muted but visible
                    dotImg.color = new Color(0.50f, 0.48f, 0.38f, 0.50f);
                }

                // Floor number label
                var lbl = InRunUiFactory.CreateText(dotGo.transform, "FloorNum",
                    (fi + 1).ToString(), 8, TextAnchor.MiddleCenter,
                    fi == currentFloor
                        ? new Color(0.06f, 0.04f, 0.02f, 1f)
                        : new Color(GamePalette.TextPrimary.r, GamePalette.TextPrimary.g, GamePalette.TextPrimary.b, 0.70f));
                InRunUiFactory.StretchFill(lbl.rectTransform);
                lbl.raycastTarget = false;
            }

            _floorProgressStrip = stripRt;
        }

        /// <summary>Slowly pulses the boss node transform until it is destroyed or goes inactive.</summary>
        private IEnumerator BossNodePulseLoop(Transform bossT)
        {
            while (bossT != null && bossT.gameObject != null
                   && bossT.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(AnimationHelper.PulseScale(bossT, 1f, 1.12f, 0.80f));
                yield return new WaitForSeconds(0.40f);
            }
        }

        private void EnsurePathOverlayRoot()
        {
            if (_pathOverlayRoot != null || _pathPanel == null) return;
            var panelRt = _pathPanel.GetComponent<RectTransform>();

            // Previous-floor background — sits behind current, used for cross-fade on floor advance
            var bgPrevGo = new GameObject("FloorBackgroundPrev", typeof(RectTransform), typeof(RawImage));
            bgPrevGo.transform.SetParent(panelRt, false);
            bgPrevGo.transform.SetAsFirstSibling();
            var bgPrevRt = bgPrevGo.GetComponent<RectTransform>();
            bgPrevRt.anchorMin = Vector2.zero; bgPrevRt.anchorMax = Vector2.one;
            bgPrevRt.offsetMin = Vector2.zero; bgPrevRt.offsetMax = Vector2.zero;
            _floorBgPrev = bgPrevGo.GetComponent<RawImage>();
            _floorBgPrev.color         = new Color(1f, 1f, 1f, 0f); // starts transparent
            _floorBgPrev.raycastTarget = false;

            // Current floor background image — loads bg_Garden_Grove.png etc. in RebuildPathCanvas
            var bgGo = new GameObject("FloorBackground", typeof(RectTransform), typeof(RawImage));
            bgGo.transform.SetParent(panelRt, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            _floorBgRaw = bgGo.GetComponent<RawImage>();
            _floorBgRaw.color         = new Color(1f, 1f, 1f, 0.92f); // near-full opacity
            _floorBgRaw.raycastTarget = false;

            // Dark scrim between the background art and all UI content — alpha tuned per floor in RebuildPathCanvas
            var scrimGo = new GameObject("BackgroundScrim", typeof(RectTransform), typeof(Image));
            scrimGo.transform.SetParent(panelRt, false);
            scrimGo.transform.SetSiblingIndex(1); // above FloorBackground (0), below scroll area
            var scrimRt = scrimGo.GetComponent<RectTransform>();
            scrimRt.anchorMin = Vector2.zero; scrimRt.anchorMax = Vector2.one;
            scrimRt.offsetMin = scrimRt.offsetMax = Vector2.zero;
            _bgScrimImg = scrimGo.GetComponent<Image>();
            _bgScrimImg.color         = new Color(0f, 0f, 0f, 0.40f); // overridden per floor
            _bgScrimImg.raycastTarget = false;

            // Stats bar — 30 px tall, sits between FloorProgressBar (22 px + 4 gap = -26) and the map scroll area
            var statsBarGo = new GameObject("PathStatsBar", typeof(RectTransform), typeof(Image));
            statsBarGo.transform.SetParent(panelRt, false);
            var statsBarRt    = statsBarGo.GetComponent<RectTransform>();
            statsBarRt.anchorMin  = new Vector2(0f, 1f);
            statsBarRt.anchorMax  = new Vector2(1f, 1f);
            statsBarRt.pivot      = new Vector2(0.5f, 1f);
            statsBarRt.offsetMin  = new Vector2(2f,  -56f); // top of bar = 26px below panel top (below FloorProgressBar)
            statsBarRt.offsetMax  = new Vector2(-2f, -26f); // 30 px tall
            var statsBarImg = statsBarGo.GetComponent<Image>();
            statsBarImg.color         = new Color(0f, 0f, 0f, 0.45f);
            statsBarImg.raycastTarget = false;
            var statsText = InRunUiFactory.CreateText(statsBarGo.transform, "PathStatsText",
                "", 13, TextAnchor.MiddleLeft, new Color(0.88f, 0.86f, 0.76f, 0.92f));
            statsText.rectTransform.anchorMin = Vector2.zero;
            statsText.rectTransform.anchorMax = Vector2.one;
            statsText.rectTransform.offsetMin = new Vector2(10f, 0f);
            statsText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            statsText.resizeTextForBestFit = true;
            statsText.resizeTextMinSize   = 10;
            statsText.resizeTextMaxSize   = 13;
            statsText.raycastTarget = false;
            statsText.rectTransform.offsetMax = new Vector2(-88f, 0f); // leave 88px on right for gold display
            _pathStatsBar = statsText;

            // Gold coin icon — rightmost 26px of the stats bar
            var goldIconGo = new GameObject("GoldIcon", typeof(RectTransform), typeof(Image));
            goldIconGo.transform.SetParent(statsBarGo.transform, false);
            var goldIconRt = goldIconGo.GetComponent<RectTransform>();
            goldIconRt.anchorMin = new Vector2(1f, 0f); goldIconRt.anchorMax = new Vector2(1f, 1f);
            goldIconRt.pivot     = new Vector2(1f, 0.5f);
            goldIconRt.offsetMin = new Vector2(-84f, 3f);
            goldIconRt.offsetMax = new Vector2(-58f, -3f);
            _goldIcon = goldIconGo.GetComponent<Image>();
            _goldIcon.sprite        = Resources.Load<Sprite>("economy/icon_sakura_coin");
            _goldIcon.preserveAspect = true;
            _goldIcon.raycastTarget  = false;
            if (_goldIcon.sprite == null) _goldIcon.color = Color.clear;

            // Gold value label — to the right of icon
            var goldLabelText = InRunUiFactory.CreateText(statsBarGo.transform, "GoldLabel",
                "0", 12, TextAnchor.MiddleLeft, new Color(0.95f, 0.85f, 0.40f, 1f));
            goldLabelText.rectTransform.anchorMin = new Vector2(1f, 0f);
            goldLabelText.rectTransform.anchorMax = new Vector2(1f, 1f);
            goldLabelText.rectTransform.pivot     = new Vector2(1f, 0.5f);
            goldLabelText.rectTransform.offsetMin = new Vector2(-56f, 1f);
            goldLabelText.rectTransform.offsetMax = new Vector2(-4f,  -1f);
            goldLabelText.raycastTarget = false;
            _goldLabel = goldLabelText;

            // ScrollRect container — stops 56 px below the top (22 px strip + 4 gap + 30 px stats bar)
            var scrollGo = new GameObject("PathScrollArea", typeof(RectTransform));
            scrollGo.transform.SetParent(panelRt, false);
            var scrollRt        = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin  = Vector2.zero;
            scrollRt.anchorMax  = Vector2.one;
            scrollRt.offsetMin  = new Vector2(0f, LegendH);
            scrollRt.offsetMax  = new Vector2(0f, -56f); // 22 px FloorProgressBar + 4 px gap + 30 px stats bar
            _pathScrollRect                   = scrollGo.AddComponent<ScrollRect>();
            _pathScrollRect.horizontal        = true;
            _pathScrollRect.vertical          = false;
            _pathScrollRect.movementType      = ScrollRect.MovementType.Clamped;
            _pathScrollRect.scrollSensitivity = 20f;

            // Viewport — child of scroll area, clips the scrolling content
            var viewportGo = new GameObject("PathViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            _pathViewport           = viewportGo.GetComponent<RectTransform>();
            _pathViewport.anchorMin = Vector2.zero;
            _pathViewport.anchorMax = Vector2.one;
            _pathViewport.offsetMin = _pathViewport.offsetMax = Vector2.zero;
            _pathScrollRect.viewport = _pathViewport;

            // Content — left-anchored inside viewport; width set each rebuild
            var contentGo = new GameObject("PathOverlay", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _pathOverlayRoot            = contentGo.GetComponent<RectTransform>();
            _pathOverlayRoot.anchorMin  = new Vector2(0f, 0f);
            _pathOverlayRoot.anchorMax  = new Vector2(0f, 1f);
            _pathOverlayRoot.pivot      = new Vector2(0f, 0f);
            _pathOverlayRoot.offsetMin  = Vector2.zero;
            _pathOverlayRoot.offsetMax  = Vector2.zero;
            _pathScrollRect.content     = _pathOverlayRoot;
            contentGo.AddComponent<CanvasGroup>(); // used for floor transition fade-in

            // Fix C: BuildPathLegend removed — legend is rebuilt each RebuildPathCanvas call instead
            // Tooltip panel (floats above all path content, hidden by default)
            EnsureNodeTooltip();
        }

        private void EnsureNodeTooltip()
        {
            if (_nodeTooltipPanel != null || _pathPanel == null) return;
            var panelRt = _pathPanel.GetComponent<RectTransform>();
            _nodeTooltipPanel = new GameObject("NodeTooltip", typeof(RectTransform), typeof(Image));
            _nodeTooltipPanel.transform.SetParent(panelRt, false);
            var rt = _nodeTooltipPanel.GetComponent<RectTransform>();
            // Anchor at center so anchoredPosition uses the same origin as
            // RectTransformUtility.ScreenPointToLocalPointInRectangle (center-relative).
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(200f, 56f);
            _nodeTooltipPanel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.10f, 0.95f);
            var tooltipOutline = _nodeTooltipPanel.AddComponent<Outline>();
            tooltipOutline.effectColor    = new Color(1f, 1f, 1f, 0.12f);
            tooltipOutline.effectDistance = new Vector2(1f, -1f);
            _nodeTooltipText = InRunUiFactory.CreateText(_nodeTooltipPanel.transform, "TooltipText", "",
                11, TextAnchor.UpperLeft, InRunUiFactory.TextColor);
            var tooltipTextShadow = _nodeTooltipText.gameObject.AddComponent<Shadow>();
            tooltipTextShadow.effectColor    = new Color(0f, 0f, 0f, 0.50f);
            tooltipTextShadow.effectDistance = new Vector2(1f, -1f);
            var textRt = _nodeTooltipText.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(6f, 4f);
            textRt.offsetMax = new Vector2(-6f, -4f);
            _nodeTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _nodeTooltipText.verticalOverflow   = VerticalWrapMode.Overflow;
            _nodeTooltipPanel.SetActive(false);
        }

        // Returns anchoredPosition (pivot top-left) so the tooltip stays visible inside panelRt.
        // 'local' is center-relative, as returned by ScreenPointToLocalPointInRectangle.
        private Vector2 ClampedTooltipPosition(Vector2 local, RectTransform panelRt)
            => ClampedTooltipPosition(local, panelRt, 200f, 56f, 12f);

        private Vector2 ClampedTooltipPosition(Vector2 local, RectTransform panelRt,
            float w, float h, float offset)
        {
            var halfW = panelRt.rect.width  * 0.5f;
            var halfH = panelRt.rect.height * 0.5f;

            var x = local.x + offset;   // prefer top-right of cursor
            var y = local.y + offset;

            if (x + w >  halfW)  x = local.x - w - offset; // flip left
            if (y - h < -halfH)  y = local.y + h + offset; // flip up

            x = Mathf.Clamp(x, -halfW,          halfW - w);
            y = Mathf.Clamp(y, -halfH + h,       halfH);

            return new Vector2(x, y);
        }

        private void ShowNodeTooltip(RunNode node, LevelConfig config, Vector2 screenPos)
        {
            EnsureNodeTooltip();
            if (_nodeTooltipPanel == null) return;
            var sb = new StringBuilder();
            sb.Append($"<b>{GetNodeLabel(node.Type)}</b>");
            if (node.Route == RouteType.RiskRoute) sb.Append("  [Risk]");
            else if (node.Route == RouteType.CalmRoute) sb.Append("  [Calm]");
            if (node.IsCrossLink)
            {
                // Floor-themed bridge name
                var bridgeName = node.Floor switch
                {
                    0 => "Bamboo bridge",
                    1 => "Stone step crossing",
                    2 => "Stepping stones",
                    3 => "Garden bridge",
                    4 => "Summit crossing",
                    _ => "Stone bridge"
                };
                sb.Append($"\n⇄ {bridgeName} — cross to the other path");
            }
            if (config != null)
                sb.Append($"\n{config.BoardSize}×{config.BoardSize}  ★ {Mathf.Clamp(config.Stars, 1, 6)}");
            _nodeTooltipText.text = sb.ToString();
            var panelRt = _pathPanel.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRt, screenPos, null, out var local);
            var rt = _nodeTooltipPanel.GetComponent<RectTransform>();
            rt.anchoredPosition = ClampedTooltipPosition(local, panelRt);
            _nodeTooltipPanel.SetActive(true);
            _nodeTooltipPanel.transform.SetAsLastSibling();
        }

        private void HideNodeTooltip()
        {
            if (_nodeTooltipPanel != null) _nodeTooltipPanel.SetActive(false);
        }

        // P-2 — Show tooltip explaining why this node is disabled as the first pick
        private void ShowFirstPickBlockedTooltip(Vector2 screenPos)
        {
            EnsureNodeTooltip();
            if (_nodeTooltipPanel == null || _pathPanel == null) return;
            _nodeTooltipText.text = "First tile must be a puzzle tile.";
            var panelRt = _pathPanel.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRt, screenPos, null, out var local);
            var rt = _nodeTooltipPanel.GetComponent<RectTransform>();
            rt.anchoredPosition = ClampedTooltipPosition(local, panelRt);
            _nodeTooltipPanel.SetActive(true);
            _nodeTooltipPanel.transform.SetAsLastSibling();
        }

        private void BuildPathLegend(RectTransform parent, float legendH)
        {
            var legendTypes = new[]
            {
                (NodeType.Puzzle,      "Puzzle"),
                (NodeType.ElitePuzzle, "Elite"),
                (NodeType.PreBoss,     "Pre-Boss"),
                (NodeType.Boss,        "Boss Gate"),
                (NodeType.Shop,        "Shop"),
                (NodeType.Rest,        "Rest"),
                (NodeType.Relic,       "Relic"),
                (NodeType.Event,       "Event"),
                (NodeType.Cursed,      "Cursed"),
            };

            var go = new GameObject("PathLegend", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, legendH);
            go.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.10f, 0.94f);

            // Top separator line
            var sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(rt, false);
            var sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 1f);
            sepRt.anchorMax = new Vector2(1f, 1f);
            sepRt.offsetMin = Vector2.zero;
            sepRt.offsetMax = new Vector2(0f, 1f);
            sep.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            sep.GetComponent<Image>().raycastTarget = false;

            const float entryW = 86f;
            var totalW  = legendTypes.Length * entryW;
            var startX  = -totalW * 0.5f + entryW * 0.5f;

            for (var i = 0; i < legendTypes.Length; i++)
            {
                var type     = legendTypes[i].Item1;
                var label    = legendTypes[i].Item2;
                var iconPath = GetNodeIconPath(type);
                var sprite   = string.IsNullOrEmpty(iconPath) ? null
                    : Resources.Load<Sprite>(iconPath);

                var entry = new GameObject($"Legend_{type}", typeof(RectTransform));
                entry.transform.SetParent(rt, false);
                var entryRt = entry.GetComponent<RectTransform>();
                entryRt.anchorMin = entryRt.anchorMax = new Vector2(0.5f, 0.5f);
                entryRt.pivot     = new Vector2(0.5f, 0.5f);
                entryRt.sizeDelta = new Vector2(entryW - 6f, legendH - 10f);
                entryRt.anchoredPosition = new Vector2(startX + i * entryW, 0f);

                if (sprite != null)
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(entry.transform, false);
                    var iconRt = iconGo.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0.02f, 0.15f);
                    iconRt.anchorMax = new Vector2(0.40f, 0.90f);
                    iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
                    var iconImg = iconGo.GetComponent<Image>();
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget  = false;

                    var lbl = InRunUiFactory.CreateText(entry.transform, "LblLegend", label,
                        8, TextAnchor.MiddleLeft, new Color(0.80f, 0.80f, 0.80f, 0.90f));
                    lbl.rectTransform.anchorMin = new Vector2(0.42f, 0f);
                    lbl.rectTransform.anchorMax = new Vector2(1f, 1f);
                    lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
                    lbl.raycastTarget = false;
                }
                else
                {
                    var lbl = InRunUiFactory.CreateText(entry.transform, "LblLegend", label,
                        8, TextAnchor.MiddleCenter, new Color(0.80f, 0.80f, 0.80f, 0.90f));
                    InRunUiFactory.StretchFill(lbl.rectTransform);
                    lbl.raycastTarget = false;
                }
            }
        }

        private Coroutine _smoothScrollCo;

        /// <summary>Smoothly scrolls the path viewport to center the given node index horizontally.</summary>
        private void ScrollToNode(int nodeIndex, float contentW, float viewW)
        {
            if (_pathScrollRect == null) return;
            var graph = _map?.Run?.CurrentFloorGraph;
            if (graph == null || nodeIndex < 0 || nodeIndex >= graph.Count) return;
            var scrollRange = contentW - viewW;
            var target = scrollRange <= 0f ? 0f
                : Mathf.Clamp01((graph[nodeIndex].CanvasX * contentW - viewW * 0.5f) / scrollRange);
            if (_smoothScrollCo != null) StopCoroutine(_smoothScrollCo);
            _smoothScrollCo = StartCoroutine(SmoothScrollToTarget(target, 0.25f));
        }

        private System.Collections.IEnumerator SmoothScrollToTarget(float target, float duration)
        {
            var elapsed = 0f;
            var start = _pathScrollRect != null ? _pathScrollRect.normalizedPosition.x : 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                // Ease-out cubic
                var ease = 1f - (1f - t) * (1f - t) * (1f - t);
                if (_pathScrollRect != null)
                    _pathScrollRect.normalizedPosition = new Vector2(Mathf.Lerp(start, target, ease), 0f);
                yield return null;
            }
            if (_pathScrollRect != null)
                _pathScrollRect.normalizedPosition = new Vector2(target, 0f);
            _smoothScrollCo = null;
        }

        private static void AddAvailableRing(RectTransform parent)
        {
            var go = new GameObject("AvailableRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.13f, -0.13f);
            rt.anchorMax = new Vector2(1.13f,  1.13f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color         = new Color(1.0f, 0.85f, 0.20f, 0.05f); // near-transparent fill
            img.raycastTarget = false;
            // C: outline-only ring — bright gold border, minimal interior fill
            var ol = go.AddComponent<Outline>();
            ol.effectColor    = new Color(1.0f, 0.85f, 0.20f, 0.88f);
            ol.effectDistance = new Vector2(3f, -3f);
        }

        private void AddVisitedMark(RectTransform parent, RunNode node, int visitStep = 0)
        {
            // Ghost icon overlay — faint silhouette of what happened here
            // Bridge nodes use flow_ribbon to distinguish from Start (which also uses engraved_stone)
            var iconPath = node.Type == NodeType.CrossLink
                ? "ui/icon_flow_ribbon"
                : GetNodeIconPath(node.Type);
            if (!string.IsNullOrEmpty(iconPath))
            {
                var sprite = Resources.Load<Sprite>(iconPath);
                if (sprite != null)
                {
                    var ghostGo = new GameObject("VisitedGhost", typeof(RectTransform), typeof(Image));
                    ghostGo.transform.SetParent(parent, false);
                    var ghostRt = ghostGo.GetComponent<RectTransform>();
                    ghostRt.anchorMin = Vector2.zero;
                    ghostRt.anchorMax = Vector2.one;
                    ghostRt.offsetMin = ghostRt.offsetMax = Vector2.zero;
                    var ghostImg = ghostGo.GetComponent<Image>();
                    ghostImg.sprite = sprite;
                    ghostImg.preserveAspect = true;
                    ghostImg.color = new Color(1f, 1f, 1f, 0.12f);
                    ghostImg.raycastTarget = false;
                }
            }
            // D: checkmark in bottom-left — dark circle bg ensures visibility on all floor tints
            var chkBg = new GameObject("CheckBg", typeof(RectTransform), typeof(Image));
            chkBg.transform.SetParent(parent, false);
            var chkBgRt = chkBg.GetComponent<RectTransform>();
            chkBgRt.anchorMin = Vector2.zero;
            chkBgRt.anchorMax = new Vector2(0.36f, 0.36f);
            chkBgRt.offsetMin = new Vector2(2f, 2f);
            chkBgRt.offsetMax = Vector2.zero;
            chkBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var lbl = InRunUiFactory.CreateText(parent, "VisitedMark", "✓",
                11, TextAnchor.LowerLeft, new Color(0.50f, 0.90f, 0.50f, 0.80f));
            lbl.rectTransform.anchorMin = Vector2.zero;
            lbl.rectTransform.anchorMax = new Vector2(0.42f, 0.42f);
            lbl.rectTransform.offsetMin = new Vector2(2f, 2f);
            lbl.rectTransform.offsetMax = Vector2.zero;
            lbl.raycastTarget = false;

            // Suggestion 6: visit sequence number in bottom-right — lets the player retrace their route
            // Omit for step 1 (Start is step 1 but it's never drawn as visited) and step 0 (unknown).
            if (visitStep >= 2)
            {
                var seqLbl = InRunUiFactory.CreateText(parent, "VisitSeq", visitStep.ToString(),
                    8, TextAnchor.LowerRight,
                    new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g,
                              InRunUiFactory.AccentGold.b, 0.60f));
                seqLbl.rectTransform.anchorMin = new Vector2(0.55f, 0f);
                seqLbl.rectTransform.anchorMax = new Vector2(1f, 0.38f);
                seqLbl.rectTransform.offsetMin = Vector2.zero;
                seqLbl.rectTransform.offsetMax = new Vector2(-3f, 0f);
                seqLbl.raycastTarget = false;
            }
        }

        private void DrawEdge(RectTransform parent, float w, float h, RunNode a, RunNode b, Color color, float thickness = 3f)
        {
            var pa = new Vector2(a.CanvasX * w, (1f - a.CanvasY) * h);
            var pb = new Vector2(b.CanvasX * w, (1f - b.CanvasY) * h);
            var d  = pb - pa;
            var dist = d.magnitude;
            if (dist < 1f) return;

            var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.zero;
            r.pivot     = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(dist, thickness);
            r.anchoredPosition = pa + d * 0.5f;
            r.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        // Thematic 5: dotted edge for cross-lane connections
        private void DrawDottedEdge(RectTransform parent, float w, float h, RunNode a, RunNode b, Color color)
        {
            var pa   = new Vector2(a.CanvasX * w, (1f - a.CanvasY) * h);
            var pb   = new Vector2(b.CanvasX * w, (1f - b.CanvasY) * h);
            var d    = pb - pa;
            var dist = d.magnitude;
            if (dist < 1f) return;
            var dir  = d / dist;
            var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

            const float SegLen = 6f;
            const float Gap    = 5f;
            const float Step   = SegLen + Gap;
            var count = Mathf.FloorToInt((dist - Gap) / Step);
            for (var k = 0; k < count; k++)
            {
                var center = pa + dir * (Gap * 0.5f + k * Step + SegLen * 0.5f);
                var seg = new GameObject("EdgeDot", typeof(RectTransform), typeof(Image));
                seg.transform.SetParent(parent, false);
                seg.transform.SetAsFirstSibling();
                var segImg = seg.GetComponent<Image>();
                segImg.color = color;
                segImg.raycastTarget = false;
                var segRt = seg.GetComponent<RectTransform>();
                segRt.anchorMin = Vector2.zero;
                segRt.anchorMax = Vector2.zero;
                segRt.pivot     = new Vector2(0.5f, 0.5f);
                segRt.sizeDelta = new Vector2(SegLen, 2.5f);
                segRt.anchoredPosition  = center;
                segRt.localEulerAngles  = new Vector3(0, 0, angle);
            }
        }

        private static bool IsPuzzleNodeType(NodeType t) =>
            t == NodeType.Puzzle || t == NodeType.ElitePuzzle || t == NodeType.PreBoss || t == NodeType.Boss;

        private Button CreateNodeButton(RectTransform parent, RunNode node, Vector2 pos, Color color, LevelConfig config = null, Color floorTint = default, bool bridgeWasUsed = false)
        {
            var isBoss = node.Type == NodeType.Boss;
            // Thematic 1: per-type sizing — Boss gets a banner gate shape; Elite is visibly larger than standard
            var size = node.Type switch
            {
                NodeType.Boss        => new Vector2(116, 60),   // prominent banner gate (1.6× standard width)
                NodeType.ElitePuzzle => new Vector2(84, 84),    // ~1.17× standard — clearly distinct
                NodeType.PreBoss     => new Vector2(82, 82),
                NodeType.Relic       => new Vector2(68, 80),
                NodeType.Shop        => new Vector2(74, 60),
                NodeType.Rest        => new Vector2(72, 60),
                _                    => new Vector2(72, 72),
            };
            var go = new GameObject($"Node_{node.Type}_{node.Index}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = color;

            // Try to load icon
            var iconPath = GetNodeIconPath(node.Type);
            var sprite   = string.IsNullOrEmpty(iconPath) ? null : Resources.Load<Sprite>(iconPath);
            if (sprite != null)
            {
                // Icon left-center, label right
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.04f, 0.15f);
                iconRt.anchorMax = new Vector2(0.42f, 0.85f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = sprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget  = false;
                // Thematic 3: seasonal icon tint — floor-theme color blends into the icon
                if (floorTint.a > 0.01f)
                    iconImg.color = Color.Lerp(Color.white, floorTint, 0.30f);

                var lbl = InRunUiFactory.CreateText(go.transform, "Label", GetNodeLabelShort(node),
                    isBoss ? 11 : 10, TextAnchor.MiddleCenter, InRunUiFactory.TextColor);
                lbl.rectTransform.anchorMin = new Vector2(0.40f, 0f);
                lbl.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                lbl.rectTransform.offsetMin = Vector2.zero;
                lbl.rectTransform.offsetMax = Vector2.zero;
                lbl.verticalOverflow = VerticalWrapMode.Overflow;
                lbl.resizeTextForBestFit = true;
                lbl.resizeTextMinSize    = 7;
                lbl.resizeTextMaxSize    = isBoss ? 11 : 10;
                var lblShadow = lbl.gameObject.AddComponent<Shadow>();
                lblShadow.effectColor    = new Color(0f, 0f, 0f, 0.65f);
                lblShadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
            else
            {
                var lbl = InRunUiFactory.CreateText(go.transform, "Label", GetNodeLabelShort(node),
                    isBoss ? 13 : 11, TextAnchor.MiddleCenter, InRunUiFactory.TextColor);
                InRunUiFactory.StretchFill(lbl.rectTransform);
                lbl.resizeTextForBestFit = true;
                lbl.resizeTextMinSize    = 8;
                lbl.resizeTextMaxSize    = isBoss ? 13 : 11;
                var lblShadow = lbl.gameObject.AddComponent<Shadow>();
                lblShadow.effectColor    = new Color(0f, 0f, 0f, 0.65f);
                lblShadow.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // Cursed nodes get a danger-red outline to signal risk
            if (node.Type == NodeType.Cursed)
            {
                var cursedOutline = go.AddComponent<Outline>();
                cursedOutline.effectColor    = GamePalette.DangerRed;
                cursedOutline.effectDistance = new Vector2(2.5f, -2.5f);
            }

            // Cross-link badge: ⇄ when available, ↳ when already used to cross
            if (node.IsCrossLink)
            {
                var badgeChar  = bridgeWasUsed ? "↳" : "⇄";
                var badgeColor = bridgeWasUsed
                    ? new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g,
                        InRunUiFactory.AccentGold.b, 0.70f)
                    : new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g,
                        InRunUiFactory.AccentGold.b, 0.90f);
                var badge = InRunUiFactory.CreateText(go.transform, "CrossBadge", badgeChar, 9,
                    TextAnchor.UpperRight, badgeColor);
                badge.rectTransform.anchorMin = new Vector2(0.60f, 0.72f);
                badge.rectTransform.anchorMax = Vector2.one;
                badge.rectTransform.offsetMin = Vector2.zero;
                badge.rectTransform.offsetMax = new Vector2(-3f, -2f);
                badge.raycastTarget = false;
            }

            // Puzzle-type nodes: top-left size, bottom-right stars (stars hidden on visited nodes — visit-seq occupies same corner)
            if (config != null && IsPuzzleNodeType(node.Type))
            {
                var sizeLabel = InRunUiFactory.CreateText(go.transform, "SizeLabel",
                    $"{config.BoardSize}×{config.BoardSize}", 9, TextAnchor.UpperLeft,
                    new Color(InRunUiFactory.TextColor.r, InRunUiFactory.TextColor.g, InRunUiFactory.TextColor.b, 0.85f));
                // Anchored right of icon (icon ends at 0.42) to avoid overlap
                sizeLabel.rectTransform.anchorMin = new Vector2(0.44f, 0.72f);
                sizeLabel.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                sizeLabel.rectTransform.offsetMin = new Vector2(2f, 0f);
                sizeLabel.rectTransform.offsetMax = new Vector2(0f, -2f);
                sizeLabel.raycastTarget = false;
                var szShadow = sizeLabel.gameObject.AddComponent<Shadow>();
                szShadow.effectColor    = new Color(0f, 0f, 0f, 0.65f);
                szShadow.effectDistance = new Vector2(1f, -1f);

                if (!node.Visited)
                {
                    var stars    = Mathf.Clamp(config.Stars, 1, 6);
                    var starStr  = new string('★', stars);
                    var starLabel = InRunUiFactory.CreateText(go.transform, "StarLabel",
                        starStr, 9, TextAnchor.LowerRight, InRunUiFactory.AccentGold);
                    starLabel.rectTransform.anchorMin = new Vector2(0.44f, 0f);
                    starLabel.rectTransform.anchorMax = new Vector2(1f, 0.28f);
                    starLabel.rectTransform.offsetMin = new Vector2(0f, 2f);
                    starLabel.rectTransform.offsetMax = new Vector2(-3f, 0f);
                    starLabel.resizeTextForBestFit = true;
                    starLabel.resizeTextMinSize   = 7;
                    starLabel.resizeTextMaxSize   = 9;
                    starLabel.raycastTarget = false;
                }
            }

            return go.GetComponent<Button>();
        }

        private static string GetNodeIconPath(NodeType t) => t switch
        {
            NodeType.Start       => "node/icon_engraved_stone",
            NodeType.Puzzle      => "node/icon_puzzle_tile",
            NodeType.ElitePuzzle => "node/icon_elite_mask",
            NodeType.PreBoss     => "node/icon_preboss_gate",
            NodeType.Boss        => "node/icon_demon_mask",
            NodeType.Shop        => "node/icon_market_stall",
            NodeType.Rest        => "node/icon_campfire_stones",
            NodeType.Relic       => "node/icon_relic_pedestal",
            NodeType.Event       => "node/icon_event_scroll",
            NodeType.Cursed      => "node/icon_moss_trap",
            NodeType.CrossLink   => "ui/icon_flow_ribbon",
            _                    => ""
        };

        private static string GetNodeLabelShort(RunNode node) => node.Type switch
        {
            NodeType.Start       => "Start",
            NodeType.Puzzle      => "Puzzle",
            NodeType.ElitePuzzle => "Elite",
            NodeType.Shop        => "Shop",
            NodeType.Rest        => "Rest",
            NodeType.Relic       => "Relic",
            NodeType.Event       => "Event",
            NodeType.PreBoss     => "Pre-Boss",
            NodeType.Boss        => "Boss\nGate",
            NodeType.Cursed      => "Cursed",
            NodeType.CrossLink   => "Bridge",
            _                    => "?"
        };

        private static string GetNodeLabel(NodeType t) => t switch
        {
            NodeType.Start       => "Start",
            NodeType.Puzzle      => "Puzzle",
            NodeType.ElitePuzzle => "Elite",
            NodeType.Shop        => "Shop",
            NodeType.Rest        => "Rest",
            NodeType.Relic       => "Relic",
            NodeType.Event       => "Event",
            NodeType.PreBoss     => "Elite",
            NodeType.Boss        => "Boss Gate",
            NodeType.Cursed      => "Cursed",
            NodeType.CrossLink   => "Bridge",
            _                    => "?"
        };

        // ─────────────────────────── Input ───────────────────────────

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // If items focus is active, Esc exits it rather than cancelling item interactions
                if (_gbControllerInItemsFocus)
                {
                    _gbControllerInItemsFocus = false;
                    _numpadView?.HighlightControllerCursor(null);
                    SetStatus("Puzzle mode  (Tab for items)");
                    _inputRemap.Tick();
                    return;
                }
                if (_windChimeChoosingHouses || _windChimePending ||
                    _templeIncensePending || _koiReflectionPending || _swapMode || _patternScrollPending)
                    CancelPendingItemInteraction();
                return;
            }
            // House-choice phase for Wind Chime takes over all digit keys
            if (_windChimeChoosingHouses) { HandleWindChimeHouseChoice(); _inputRemap.Tick(); return; }
            HandleGamepadController();
            var sz = _map?.Run?.CurrentBoard?.Size ?? 9;
            if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W)) MoveSelection(-1, 0);
            if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S)) MoveSelection(1, 0);
            if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) MoveSelection(0, -1);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) MoveSelection(0, 1);

            // ── Bag navigation: Tab / Shift+Tab ──
            // When items focus is active Tab cycles slots; when puzzle focus is active Tab switches to items focus.
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (_gbControllerInItemsFocus)
                {
                    _hudView?.ControllerSelectBagSlot(shift ? -1 : +1);
                }
                else
                {
                    // Switch to items focus and select first slot
                    _gbControllerInItemsFocus = true;
                    _hudView?.ControllerSelectBagSlot(0);
                    SetStatus("Items focus  (Tab to cycle, Enter to use, Esc or Tab back)");
                }
                _inputRemap.Tick();
                return;
            }

            // ── Use selected item: Enter / Space while items-focus is active ──
            if (_gbControllerInItemsFocus &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                _hudView?.ControllerActivateBagSlot();
                _gbControllerInItemsFocus = false;
                _inputRemap.Tick();
                return;
            }

            // ── Inspect selected item: I ──
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (!_gbControllerInItemsFocus)
                {
                    // Auto-enter items focus first so there is something to inspect
                    _gbControllerInItemsFocus = true;
                    _hudView?.ControllerSelectBagSlot(0);
                }
                _hudView?.ControllerInspectBagSlot();
                _inputRemap.Tick();
                return;
            }

            for (var v = 1; v <= Mathf.Min(sz, 9); v++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + v) || Input.GetKeyDown(KeyCode.Keypad0 + v))
                {
                    // Queue the digit: flush in LateUpdate so any same-frame cell click
                    // (EventSystem) has already updated _selectedRow/_selectedCol first.
                    _pendingKeyboardDigit = v;
                    break;
                }

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                ClearCell();

            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
                TogglePencilMode();

            // Ctrl+Z: undo last correctly placed digit
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                Input.GetKeyDown(KeyCode.Z) && _lastPlacedRow >= 0)
            {
                var board = _map?.Run?.CurrentBoard;
                if (board != null && !board.GivenMask[_lastPlacedRow, _lastPlacedCol])
                {
                    board.PlaceValue(_lastPlacedRow, _lastPlacedCol, 0);
                    _selectedRow = _lastPlacedRow;
                    _selectedCol = _lastPlacedCol;
                    _lastPlacedRow = -1;
                    RenderBoard();
                    SetStatus("Undo: last digit cleared.");
                }
                _inputRemap.Tick();
                return;
            }

            // H: toggle digit highlight on the selected cell (mirrors R3 controller button)
            if (Input.GetKeyDown(KeyCode.H))
            {
                var board = _map?.Run?.CurrentBoard;
                if (board != null && _selectedRow >= 0 && _selectedCol >= 0)
                {
                    var val = board.Cells[_selectedRow, _selectedCol];
                    if (val > 0)
                    {
                        _highlightValue = (_highlightValue == val) ? 0 : val;
                        RenderBoard();
                        SetStatus(_highlightValue > 0 ? $"Highlighting {val}." : "Highlight cleared.");
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Q))
                SaveAndQuit();

            // Tick AFTER all WasActionPressed checks so _prevH/_prevV hold the
            // previous frame's values during this frame's edge detection.
            _inputRemap.Tick();
        }

        // Returns true when any overlay panel (shop / reward / rest / relic / bag-swap) is open.
        private bool TryGetActiveOverlay(out GameObject panel)
        {
            panel = null;
            var sp = _shopView?.ActivePanel;
            if (sp != null && sp.activeSelf) { panel = sp; return true; }
            var rp = _rewardView?.ActivePanel;
            if (rp != null && rp.activeSelf) { panel = rp; return true; }
            var bp = _bossGateView?.ActivePanel;
            if (bp != null && bp.activeSelf) { panel = bp; return true; }
            var ep = _eventChoiceView?.ActivePanel;
            if (ep != null && ep.activeSelf) { panel = ep; return true; }
            return false;
        }

        private void HandleGamepadOverlayFocus(GameObject panel)
        {
            // Populate reusable cache — avoids per-frame allocation
            _overlayBtnCache.Clear();
            var allBtns = panel.GetComponentsInChildren<Button>(false);
            for (var k = 0; k < allBtns.Length; k++)
                if (allBtns[k].IsInteractable()) _overlayBtnCache.Add(allBtns[k]);
            if (_overlayBtnCache.Count == 0) return;

            var curSel = UnityEngine.EventSystems.EventSystem.current
                ?.currentSelectedGameObject?.GetComponent<Button>();
            var curIdx = _overlayBtnCache.IndexOf(curSel);
            if (curIdx < 0) curIdx = 0;

            var moveNext = _inputRemap.WasActionPressed(InputAction.MoveRight)
                        || _inputRemap.WasActionPressed(InputAction.MoveDown);
            var movePrev = _inputRemap.WasActionPressed(InputAction.MoveLeft)
                        || _inputRemap.WasActionPressed(InputAction.MoveUp);

            if (moveNext) curIdx = (curIdx + 1) % _overlayBtnCache.Count;
            if (movePrev) curIdx = (curIdx + _overlayBtnCache.Count - 1) % _overlayBtnCache.Count;
            if (moveNext || movePrev)
                UnityEngine.EventSystems.EventSystem.current
                    ?.SetSelectedGameObject(_overlayBtnCache[curIdx].gameObject);

            // A (JoystickButton0) = confirm / invoke selected button
            if (Input.GetKeyDown(KeyCode.JoystickButton0))
                _overlayBtnCache[Mathf.Clamp(curIdx, 0, _overlayBtnCache.Count - 1)].onClick.Invoke();

            // B (JoystickButton1) = invoke skip / abort / cancel button if one exists
            if (Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                Button skip = null;
                for (var k = _overlayBtnCache.Count - 1; k >= 0; k--)
                {
                    var n = _overlayBtnCache[k].name;
                    if (n.IndexOf("skip",   StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("abort",  StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("leave",  StringComparison.OrdinalIgnoreCase) >= 0)
                    { skip = _overlayBtnCache[k]; break; }
                }
                skip?.onClick.Invoke();
            }
        }

        private void HandleGamepadTutorialFocus()
        {
            if (_basicsTutorial == null) return;
            var panel = _basicsTutorial.PanelRoot;
            if (panel == null || !panel.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                var tutBtns = panel.GetComponentsInChildren<Button>(false);
                for (var k = 0; k < tutBtns.Length; k++)
                {
                    if (tutBtns[k].IsInteractable() && tutBtns[k].gameObject.activeSelf)
                    { tutBtns[k].onClick.Invoke(); break; }
                }
            }
        }

        private void HandleGamepadController()
        {
            // Start (JoystickButton7): save & quit to menu (pause equivalent)
            if (Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                SaveAndQuit();
                return;
            }

            // ── Tutorial panel: A advances step, puzzle input suppressed ──────────
            if (_basicsTutorial != null && _basicsTutorial.IsActive)
            {
                HandleGamepadTutorialFocus();
                return;
            }

            // ── In-game options panel ─────────────────────────────────────────────
            if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf)
            {
                HandleGamepadOverlayFocus(_inGameOptionsPanel);
                if (Input.GetKeyDown(KeyCode.JoystickButton1))
                    ToggleOptionsPanel();
                return;
            }

            // ── Overlay panels (shop / reward / rest / relic / bag-swap / boss-gate / event) ──
            if (TryGetActiveOverlay(out var overlayPanel))
            {
                HandleGamepadOverlayFocus(overlayPanel);
                return;
            }

            // Path panel: left stick / d-pad navigate nodes, A confirm, B quit
            if (_pathPanel != null && _pathPanel.activeSelf)
            {
                HandleGamepadPathFocus();
                return;
            }

            // LB (JoystickButton4): toggle Puzzle ↔ Items focus
            if (Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                _gbControllerInItemsFocus = !_gbControllerInItemsFocus;
                if (_gbControllerInItemsFocus)
                {
                    _numpadView?.HighlightControllerCursor(null);
                    _hudView?.ControllerSelectBagSlot(0);
                }
                else
                {
                    _numpadView?.HighlightControllerCursor(_gbNumpadRow * 3 + _gbNumpadCol);
                    SetStatus("[Controller] Puzzle mode  (LB to switch)");
                }
            }

            if (_gbControllerInItemsFocus)
                HandleGamepadItemsFocus();
            else
                HandleGamepadPuzzleFocus();
        }

        private void HandleGamepadPathFocus()
        {
            if (_gbReachableNodeButtons.Count == 0) return;

            // Left stick / D-Pad: cycle through reachable nodes (right/down = next, left/up = prev)
            var moveNext = _inputRemap.WasActionPressed(InputAction.MoveRight) ||
                           _inputRemap.WasActionPressed(InputAction.MoveDown);
            var movePrev = _inputRemap.WasActionPressed(InputAction.MoveLeft) ||
                           _inputRemap.WasActionPressed(InputAction.MoveUp);

            if (moveNext) _gbPathCursor = (_gbPathCursor + 1) % _gbReachableNodeButtons.Count;
            if (movePrev) _gbPathCursor = (_gbPathCursor + _gbReachableNodeButtons.Count - 1) % _gbReachableNodeButtons.Count;

            if (moveNext || movePrev)
            {
                var selBtn = _gbReachableNodeButtons[_gbPathCursor];
                // Scroll the path viewport so the selected node is visible
                if (_pathScrollRect != null && selBtn != null)
                    ScrollToNode(_gbReachableNodeIndices[_gbPathCursor],
                        _pathOverlayRoot?.sizeDelta.x ?? 600f,
                        _pathViewport?.rect.width ?? 400f);
                // Highlight via EventSystem selection
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    selBtn != null ? selBtn.gameObject : null);
                SetStatus($"[Controller] Node {_gbPathCursor + 1}/{_gbReachableNodeButtons.Count}  —  A to enter");
            }

            // LT / RT: free-scroll the path map left/right
            float lt = 0f, rt = 0f;
            try { lt = Input.GetAxis(TriggerAxisL); } catch { }
            try { rt = Input.GetAxis(TriggerAxisR); } catch { }
            if (_pathScrollRect != null)
            {
                var scrollDelta = (rt - lt) * Time.unscaledDeltaTime * 1.5f;
                if (Mathf.Abs(scrollDelta) > 0.001f)
                    _pathScrollRect.horizontalNormalizedPosition =
                        Mathf.Clamp01(_pathScrollRect.horizontalNormalizedPosition + scrollDelta);
            }
            _triggerLPrev = lt;
            _triggerRPrev = rt;

            // A (JoystickButton0): enter the currently highlighted node
            if (Input.GetKeyDown(KeyCode.JoystickButton0) && _gbReachableNodeButtons.Count > 0)
            {
                var selBtn = _gbReachableNodeButtons[_gbPathCursor];
                if (selBtn != null && selBtn.interactable)
                    selBtn.onClick.Invoke();
            }

            // B (JoystickButton1): first press shows confirmation; second press (within 2s) saves & quits
            if (Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                if (_pathBConfirmTimer > 0f)
                {
                    _pathBConfirmTimer = 0f;
                    SaveAndQuit();
                }
                else
                {
                    _pathBConfirmTimer = 2f;
                    SetStatus("Press B again to save & quit.");
                }
            }
            if (_pathBConfirmTimer > 0f)
            {
                _pathBConfirmTimer -= Time.unscaledDeltaTime;
                if (_pathBConfirmTimer <= 0f)
                    SetStatus(""); // confirmation window expired
            }
        }

        private void HandleGamepadPuzzleFocus()
        {
            // D-Pad + left stick: move cell selection (gamepad-only — keyboard arrows are handled below
            // in the direct keyboard section to avoid double-firing the same MoveSelection call).
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveUp))    MoveSelection(-1, 0);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveDown))  MoveSelection(1, 0);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveLeft))  MoveSelection(0, -1);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveRight)) MoveSelection(0, 1);

            // A (JoystickButton0): place digit at numpad cursor position
            if (Input.GetKeyDown(KeyCode.JoystickButton0))
                _pendingKeyboardDigit = _gbNumpadRow * 3 + _gbNumpadCol + 1;

            // B (JoystickButton1): clear cell
            if (Input.GetKeyDown(KeyCode.JoystickButton1))
                ClearCell();

            // X (JoystickButton2) or RB (JoystickButton5): toggle pencil mode
            if (Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton5))
                TogglePencilMode();

            // Y (JoystickButton3): undo last correctly placed digit
            if (Input.GetKeyDown(KeyCode.JoystickButton3) && _lastPlacedRow >= 0)
            {
                var board = _map?.Run?.CurrentBoard;
                if (board != null && !board.GivenMask[_lastPlacedRow, _lastPlacedCol])
                {
                    _selectedRow = _lastPlacedRow;
                    _selectedCol = _lastPlacedCol;
                    ClearCell();
                    _lastPlacedRow = -1;
                    _lastPlacedCol = -1;
                    SetStatus("Undo: last digit cleared.");
                }
            }

            // R3 (JoystickButton9): toggle highlight for value at selected cell
            if (Input.GetKeyDown(KeyCode.JoystickButton9))
            {
                var board = _map?.Run?.CurrentBoard;
                if (board != null && _selectedRow >= 0 && _selectedCol >= 0)
                {
                    var val = board.Cells[_selectedRow, _selectedCol];
                    if (val > 0)
                    {
                        _highlightValue = (_highlightValue == val) ? 0 : val;
                        RenderBoard();
                        SetStatus(_highlightValue > 0 ? $"Highlighting {val}." : "Highlight cleared.");
                    }
                }
            }

            // Select/Back (JoystickButton6): advance end-screen / skip to next step
            if (Input.GetKeyDown(KeyCode.JoystickButton6))
            {
                // If the game-over screen is shown, pressing Select returns to menu
                if (_gameOverPanel != null && _gameOverPanel.activeSelf)
                    SaveAndQuit();
            }

            // Right stick: navigate 3×3 numpad cursor
            HandleRightStickNumpad();
        }

        private void HandleRightStickNumpad()
        {
            float h = 0f, v = 0f;
            try { h = Input.GetAxis(RightStickH); } catch { }
            try { v = Input.GetAxis(RightStickV); } catch { }

            var hRight = h >  RightStickThresh && _gbRStickPrevH <=  RightStickThresh;
            var hLeft  = h < -RightStickThresh && _gbRStickPrevH >= -RightStickThresh;
            var vUp    = v >  RightStickThresh && _gbRStickPrevV <=  RightStickThresh;
            var vDown  = v < -RightStickThresh && _gbRStickPrevV >= -RightStickThresh;

            if (hRight) _gbNumpadCol = Mathf.Min(_gbNumpadCol + 1, 2);
            if (hLeft)  _gbNumpadCol = Mathf.Max(_gbNumpadCol - 1, 0);
            if (vUp)    _gbNumpadRow = Mathf.Max(_gbNumpadRow - 1, 0);
            if (vDown)  _gbNumpadRow = Mathf.Min(_gbNumpadRow + 1, 2);

            _gbRStickPrevH = h;
            _gbRStickPrevV = v;

            // Only highlight the numpad cursor when the stick actually moves;
            // prevents the phantom "5" highlight for keyboard-only players.
            if (hRight || hLeft || vUp || vDown)
                _numpadView?.HighlightControllerCursor(_gbNumpadRow * 3 + _gbNumpadCol);
        }

        private void HandleGamepadItemsFocus()
        {
            // D-Pad up/down or LT/RT: navigate bag item slots
            if (Input.GetKeyDown(KeyCode.JoystickButton12)) _hudView?.ControllerSelectBagSlot(-1);
            if (Input.GetKeyDown(KeyCode.JoystickButton13)) _hudView?.ControllerSelectBagSlot(+1);

            float lt = 0f, rt = 0f;
            try { lt = Input.GetAxis(TriggerAxisL); } catch { }
            try { rt = Input.GetAxis(TriggerAxisR); } catch { }
            if (lt > TriggerThresh && _triggerLPrev <= TriggerThresh) _hudView?.ControllerSelectBagSlot(-1);
            if (rt > TriggerThresh && _triggerRPrev <= TriggerThresh) _hudView?.ControllerSelectBagSlot(+1);
            _triggerLPrev = lt;
            _triggerRPrev = rt;

            // A: activate selected item
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) _hudView?.ControllerActivateBagSlot();
        }

        private void CancelPendingItemInteraction(bool showStatus = true)
        {
            _windChimeChoosingHouses = false;
            _windChimePending        = false;
            _windChimeFillRow = _windChimeFillCol = _windChimeFillBox = false;
            _templeIncensePending    = false;
            _templeIncenseChargesLeft = 0;
            _koiReflectionPending    = false;
            _koiReflectionChargesLeft = 0;
            _patternScrollPending      = false;
            _patternScrollChargesLeft  = 0;
            _patternScrollMechanics    = null;
            _patternScrollSolvedIds    = null;
            _patternScrollStagedIndex  = -1;
            if (_patternScrollOverlay != null) { UnityEngine.Object.Destroy(_patternScrollOverlay); _patternScrollOverlay = null; }
            _swapMode = false;
            _swapRow1 = -1; _swapCol1 = -1;
            if (showStatus) SetStatus("Cancelled.");
        }

        // ── Pattern Scroll: build stable instance list from current overlay ──
        private static List<(string label, List<CellCoord> cells)> BuildPatternScrollMechanics(
            ModifierOverlayData ov)
        {
            var list = new List<(string, List<CellCoord>)>();
            var lineIdx = new Dictionary<LineType, int>();
            foreach (var line in ov.Lines)
            {
                if (!lineIdx.TryGetValue(line.Type, out var n)) n = 0;
                lineIdx[line.Type] = n + 1;
                list.Add(($"{line.Type} #{n + 1} ({line.Cells.Count} cells)",
                    new List<CellCoord>(line.Cells)));
            }
            for (var i = 0; i < ov.KropkiDots.Count; i++)
            {
                var d = ov.KropkiDots[i];
                list.Add(($"Kropki {(d.IsBlack ? "Black" : "White")} dot #{i + 1}",
                    new List<CellCoord> { d.CellA, d.CellB }));
            }
            for (var i = 0; i < ov.PairConstraints.Count; i++)
            {
                var p = ov.PairConstraints[i];
                list.Add(($"{p.Type} pair #{i + 1}",
                    new List<CellCoord> { p.CellA, p.CellB }));
            }
            for (var i = 0; i < ov.KillerCages.Count; i++)
            {
                var c = ov.KillerCages[i];
                list.Add(($"Killer Cage #{i + 1} (sum={c.Sum}, {c.Cells.Count} cells)",
                    new List<CellCoord>(c.Cells)));
            }
            for (var i = 0; i < ov.Arrows.Count; i++)
            {
                var a = ov.Arrows[i];
                var cells = new List<CellCoord> { a.CircleCell };
                cells.AddRange(a.ArrowCells);
                list.Add(($"Arrow #{i + 1} ({cells.Count} cells)", cells));
            }
            return list;
        }

        private void ShowPatternScrollOverlay()
        {
            if (_patternScrollOverlay != null) UnityEngine.Object.Destroy(_patternScrollOverlay);
            if (_sudokuPanel == null) return;

            var ov = _map?.Run?.CurrentOverlay;
            if (ov == null) return;

            // Build the mechanic list exactly once per scroll activation so indices
            // remain stable across selection rounds (Rare=2 picks, Epic=4 picks).
            if (_patternScrollMechanics == null)
            {
                _patternScrollMechanics  = BuildPatternScrollMechanics(ov);
                _patternScrollSolvedIds  = new HashSet<int>();
                _patternScrollStagedIndex = -1;
            }

            var overlay = new GameObject("PatternScrollOverlay", typeof(RectTransform));
            overlay.transform.SetParent(_sudokuPanel.transform, false);
            var rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.44f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            overlay.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.05f, 0.93f);
            _patternScrollOverlay = overlay;

            // ── Title (visible during Selecting state only — hidden in Idle/Applied) ──
            var titleGo = InRunUiFactory.CreateText(overlay.transform, "Title",
                $"Pattern Scroll  ({_patternScrollChargesLeft} remaining)\nSelect a mechanic instance — Press ESC to cancel",
                11, TextAnchor.UpperCenter, GamePalette.AccentGold);
            titleGo.rectTransform.anchorMin = new Vector2(0.02f, 0.91f);
            titleGo.rectTransform.anchorMax = new Vector2(0.98f, 1.00f);
            titleGo.rectTransform.offsetMin = titleGo.rectTransform.offsetMax = Vector2.zero;
            titleGo.lineSpacing = 1.15f;

            var mechanics = _patternScrollMechanics;
            if (mechanics.Count == 0)
            {
                var none = InRunUiFactory.CreateText(overlay.transform, "None",
                    "No mechanic instances found on this puzzle.", 11,
                    TextAnchor.MiddleCenter, GamePalette.TextPrimary);
                none.rectTransform.anchorMin = new Vector2(0.04f, 0.12f);
                none.rectTransform.anchorMax = new Vector2(0.96f, 0.88f);
                none.rectTransform.offsetMin = none.rectTransform.offsetMax = Vector2.zero;
                return;
            }

            // ── Mechanic buttons (built before confirmBtn so listeners can reference it) ──
            var count    = mechanics.Count;
            var slotH    = 0.84f / count;
            var mechBtns = new Button[count];
            for (var i = 0; i < count; i++)
            {
                var yMax = 0.89f - i * slotH;
                var yMin = yMax - slotH + 0.004f;
                mechBtns[i] = InRunUiFactory.CreatePanelButton(overlay.transform, $"MechBtn_{i}",
                    new Vector2(0.03f, yMin), new Vector2(0.97f, yMax), mechanics[i].label);
                if (_patternScrollSolvedIds.Contains(i))
                {
                    // Already resolved in this activation – show greyed out, non-interactive
                    mechBtns[i].interactable = false;
                    var dimg = mechBtns[i].GetComponent<Image>();
                    if (dimg != null) dimg.color = new Color(0.18f, 0.18f, 0.16f, 0.50f);
                }
            }

            // ── Confirm button (Confirmed state) – hidden until a mechanic is staged ──
            var confirmBtn = InRunUiFactory.CreatePanelButton(overlay.transform, "BtnConfirmScroll",
                new Vector2(0.10f, 0.01f), new Vector2(0.90f, 0.08f), "Confirm Selection");
            confirmBtn.GetComponent<Image>().color = new Color(0.25f, 0.60f, 0.28f, 0.93f);
            confirmBtn.gameObject.SetActive(_patternScrollStagedIndex >= 0);
            confirmBtn.onClick.AddListener(PatternScrollConfirmSelection);

            // ── Wire mechanic-button listeners (now that confirmBtn exists) ──
            for (var i = 0; i < count; i++)
            {
                if (_patternScrollSolvedIds.Contains(i)) continue;
                var idx = i;
                mechBtns[i].onClick.AddListener(() =>
                    PatternScrollStageSelection(idx, mechBtns, confirmBtn));
            }

            // Restore gold highlight on previously staged button (re-entering from a refresh)
            if (_patternScrollStagedIndex >= 0 && _patternScrollStagedIndex < count)
            {
                var img = mechBtns[_patternScrollStagedIndex].GetComponent<Image>();
                if (img != null) img.color = new Color(0.95f, 0.80f, 0.25f, 0.95f);
            }
        }

        // ── Selecting → Confirmed: stage a mechanic choice ──
        private void PatternScrollStageSelection(int index, Button[] mechBtns, Button confirmBtn)
        {
            // Un-highlight previously staged button
            if (_patternScrollStagedIndex >= 0 && _patternScrollStagedIndex < mechBtns.Length)
            {
                var prev = mechBtns[_patternScrollStagedIndex].GetComponent<Image>();
                if (prev != null) prev.color = InRunUiFactory.BtnColor;
            }

            _patternScrollStagedIndex = index;

            // Highlight newly staged button gold
            var img = mechBtns[index].GetComponent<Image>();
            if (img != null) img.color = new Color(0.95f, 0.80f, 0.25f, 0.95f);

            confirmBtn.gameObject.SetActive(true);
            SetStatus($"Pattern Scroll: '{_patternScrollMechanics[index].label}' staged. Click Confirm to apply — ESC cancels.");
        }

        // ── Confirmed → Applied: fill cells and advance or finish ──
        private void PatternScrollConfirmSelection()
        {
            if (_patternScrollStagedIndex < 0 || _patternScrollMechanics == null) return;
            if (_patternScrollStagedIndex >= _patternScrollMechanics.Count) return;

            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null) return;

            var (label, cells) = _patternScrollMechanics[_patternScrollStagedIndex];
            var filled = 0;
            foreach (var coord in cells)
            {
                if (coord.Row < 0 || coord.Row >= board.Size ||
                    coord.Col < 0 || coord.Col >= board.Size) continue;
                if (board.GivenMask[coord.Row, coord.Col]) continue;
                if (board.Cells[coord.Row, coord.Col] != 0) continue;
                var sol = board.Solution[coord.Row, coord.Col];
                if (sol > 0) { run.PlaceNumber(coord.Row, coord.Col, sol); filled++; }
            }

            // Mark this instance as solved — it will be greyed out in subsequent rounds
            _patternScrollSolvedIds.Add(_patternScrollStagedIndex);
            _patternScrollStagedIndex = -1;
            _patternScrollChargesLeft--;
            RebuildBoard();

            if (_patternScrollChargesLeft <= 0)
            {
                // Applied → Idle
                _patternScrollPending   = false;
                _patternScrollMechanics = null;
                _patternScrollSolvedIds = null;
                if (_patternScrollOverlay != null)
                { UnityEngine.Object.Destroy(_patternScrollOverlay); _patternScrollOverlay = null; }
                SetStatus($"Pattern Scroll: solved '{label}', filled {filled} cell(s). Done.");
            }
            else
            {
                // Back to Selecting with updated solved set
                SetStatus($"Pattern Scroll: solved '{label}' (+{filled} cells). {_patternScrollChargesLeft} selection(s) left — Press ESC to cancel.");
                ShowPatternScrollOverlay();
            }
        }

        /// <summary>
        /// Phase 1 of Wind Chime: player presses 1/2/3 to choose which house(s) to fill.
        /// Auto-advances to cell-click phase when enough houses are selected.
        /// </summary>
        private void HandleWindChimeHouseChoice()
        {
            // Numeric keys 1/2/3 toggle Row/Col/Box selection
            static int Count(bool a, bool b, bool c) => (a ? 1 : 0) + (b ? 1 : 0) + (c ? 1 : 0);

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) ||
                Input.GetKeyDown(KeyCode.JoystickButton3)) // Y button
                ToggleWindChimeHouseFlag(ref _windChimeFillRow);

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) ||
                Input.GetKeyDown(KeyCode.JoystickButton1)) // B button
                ToggleWindChimeHouseFlag(ref _windChimeFillCol);

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3) ||
                Input.GetKeyDown(KeyCode.JoystickButton2)) // X button
                ToggleWindChimeHouseFlag(ref _windChimeFillBox);

            var selected = Count(_windChimeFillRow, _windChimeFillCol, _windChimeFillBox);
            var r = _windChimeFillRow ? "[●Row]" : "[ Row]";
            var c = _windChimeFillCol ? "[●Col]" : "[ Col]";
            var b = _windChimeFillBox ? "[●Box]" : "[ Box]";
            var remain = _windChimeMaxHouses - selected;

            if (selected >= _windChimeMaxHouses && selected > 0)
            {
                // Enough houses selected → proceed to cell-click phase
                _windChimeChoosingHouses = false;
                _windChimePending        = true;
                var label = (selected == 3 ? "row + column + box" :
                             selected == 2 ? (_windChimeFillRow && _windChimeFillCol ? "row + column" :
                                              _windChimeFillRow && _windChimeFillBox ? "row + box" : "column + box") :
                             _windChimeFillRow ? "row" : _windChimeFillCol ? "column" : "box");
                SetStatus($"Wind Chime: click a cell to fill its {label}. ESC cancels.");
            }
            else
            {
                SetStatus($"Wind Chime: {r} {c} {b}  — select {remain} more (1=Row 2=Col 3=Box). ESC cancels.");
            }
        }

        private void ToggleWindChimeHouseFlag(ref bool flag)
        {
            if (flag)
            {
                flag = false; // deselect
                return;
            }
            var selected = (_windChimeFillRow ? 1 : 0) + (_windChimeFillCol ? 1 : 0) + (_windChimeFillBox ? 1 : 0);
            if (selected < _windChimeMaxHouses)
                flag = true;
        }

        private void MoveSelection(int dr, int dc)
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) return;
            if (_selectedRow < 0)
            {
                if (TryFindEditable(board, out var r, out var c)) { _selectedRow = r; _selectedCol = c; }
                _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                    HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                return;
            }
            _selectedRow = Mathf.Clamp(_selectedRow + dr, 0, board.Size - 1);
            _selectedCol = Mathf.Clamp(_selectedCol + dc, 0, board.Size - 1);
            _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
        }

        private void OnCellClicked(int r, int c)
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) return;

            // Ignore cell clicks while player is choosing houses for Wind Chime
            if (_windChimeChoosingHouses) return;

            if (_koiReflectionPending)
            {
                HandleKoiReflectionClick(r, c);
                return;
            }

            if (_windChimePending)
            {
                HandleWindChimeClick(r, c);
                return;
            }

            if (_templeIncensePending)
            {
                HandleTempleIncenseClick(r, c);
                return;
            }

            if (_swapMode)
            {
                HandleSwapClick(r, c);
                return;
            }
            var val = board.Cells[r, c];
            // Single click on a filled cell sets/clears highlight; clicking same value again clears it
            if (val > 0)
                _highlightValue = _highlightValue == val ? 0 : val;
            else
                _highlightValue = 0;
            // Stamp selection time when the active edit cell changes (input safety window)
            if (r != _selectedRow || c != _selectedCol)
                _cellSelectedTime = Time.realtimeSinceStartup;
            _selectedRow = r;
            _selectedCol = c;
            _audio?.PlayCellSelect();
            _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);

            // Anti-Knight: after selecting an empty cell, clear highlight so forbidden cells
            // show for the selected cell's neighbor values
            if (val == 0 && HasActiveModifier(BossModifierId.Antiknight))
                _highlightValue = 0;
        }

        private void EnterNumber(int value)
        {
            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (run == null || board == null) return;
            if (_selectedRow < 0 || _selectedCol < 0)
            {
                if (!TryFindEditable(board, out _selectedRow, out _selectedCol)) return;
            }
            // Safety window: ignore digit input for a brief period after cell selection
            // to prevent accidental placements on touch/fast keyboard usage.
            if (Time.realtimeSinceStartup - _cellSelectedTime < CellSelectInputDelay) return;
            if (board.GivenMask[_selectedRow, _selectedCol]) { SetStatus("Given cell."); return; }
            if (run.GetLockedCells().Contains((_selectedRow, _selectedCol))) { SetStatus("Cell is locked!"); return; } // [REQ: DEBUFF-HOOK-013] block digit input on locked cells
            if (value < 1 || value > board.Size) return;

            // Basics tutorial: lock input to the guided cell during step 6
            if (_basicsTutorial != null && _basicsTutorial.IsInputLockedToCell(_selectedRow, _selectedCol))
            { SetStatus("Try the highlighted cell!"); return; }

            if (_pencilMode)
            {
                if (board.Cells[_selectedRow, _selectedCol] != 0) { SetStatus("Clear cell first."); return; }
                if (!run.TryAddPencilMark(_selectedRow, _selectedCol, value))
                    SetStatus("No pencil charges.");
                else
                {
                    SetStatus($"Pencil: {value}");
                    run.GetAnalytics()?.RecordPencilPlaced();
                    _basicsTutorial?.OnPlayerAddedPencilMark(_selectedRow, _selectedCol, value);
                    if (SudokuRoguelike.Run.CurseService.IsActive(run.State, "blurred_sight")) // [REQ: CURSE-INT-009] blurred_sight: hide pencil marks 1.5 s after placement
                    {
                        var r = _selectedRow; var c = _selectedCol;
                        _blurredPencilCells.Add((r, c));
                        StartCoroutine(UnblurPencilCellAfterDelay(r, c, 1.5f));
                    }
                }
                RenderBoard();
                return;
            }

            // During a pencil-interaction tutorial step, block final number placement.
            if (_basicsTutorial != null && _basicsTutorial.IsOnPencilStep)
            { SetStatus("Use pencil mode for this step!"); return; }

            var result = run.PlaceNumber(_selectedRow, _selectedCol, value);
            // Notify basics tutorial of the placement
            _basicsTutorial?.OnPlayerPlacedNumber(_selectedRow, _selectedCol, value,
                result == PlaceResult.Correct);

            if (result == PlaceResult.Correct)
            {
                _lastPlacedRow = _selectedRow;
                _lastPlacedCol = _selectedCol;
                _audio?.PlayCorrectPlacement();
                _boardView?.PlayDigitPlaced(_selectedRow, _selectedCol);
                RumbleService.PulseCorrect(this);
                _map?.SaveNow();
                SetStatus($"Placed {value}.");
            }
            else if (result == PlaceResult.IsGiven)
                SetStatus("Given cell.");
            else if (result == PlaceResult.IsPresolved)
                SetStatus("Locked cell.");
            else
            {
                _lastMistakeRow = _selectedRow;
                _lastMistakeCol = _selectedCol;
                {
                    var shielded = run.State.MistakeShieldCharges > 0;
                    run.ApplyMistakePenalty(_selectedRow, _selectedCol); // handles shield + class passive + relic
                    if (shielded)
                        SetStatus($"Umbrella blocked the penalty! ({run.State.MistakeShieldCharges} left)");
                    else
                    {
                        _audio?.PlayWrongPlacement();
                        _audio?.PlayHpLoss();
                        RumbleService.PulseMistake(this);
                        SetStatus($"{value} conflicts! HP: {run.State.CurrentHP}");
                        if (!_accessibility.IsReduceMotion)
                            StartCoroutine(AnimationHelper.ScreenShake(
                                _sudokuPanel != null ? _sudokuPanel.transform : transform,
                                AnimationHelper.ShakeWrongPx, AnimationHelper.ShakeWrongDuration));
                    }
                }
            }
            _numpadView.UpdateButtonStates(board.Size, board);
            // [REQ: PRESSURE-UI-004,005,008,009] Trigger one-shot pressure animations then render with pressure data
            var pressure = BuildPressureRenderData();
            if (pressure.HasValue)
            {
                var pr = pressure.Value;
                if (pr.ExpiryFlashCells?.Count > 0)
                {
                    _boardView?.FlashCells(pr.ExpiryFlashCells, new Color(0.95f, 0.15f, 0.15f, 1f), 0.45f);
                    if (!_accessibility.IsReduceMotion)
                    {
                        StartCoroutine(AnimationHelper.ScreenShake(
                            _sudokuPanel != null ? _sudokuPanel.transform : transform,
                            AnimationHelper.ShakeWrongPx * 1.5f, 0.18f));
                        _boardView?.ShakeBadgeCells(pr.ExpiryFlashCells);
                    }
                }
                if (pr.ChainSuccessCells?.Count > 0)
                    _boardView?.FlashCells(pr.ChainSuccessCells, new Color(1f, 0.82f, 0.15f, 1f), 0.45f);
                if (pr.CrumbledFlashCells?.Count > 0)
                    _boardView?.FlashCells(pr.CrumbledFlashCells, new Color(0.20f, 0.80f, 0.30f, 1f), 0.45f);
                if (pr.WaveRipplePending && !_accessibility.IsReduceMotion)
                    _boardView?.TriggerWaveRipple();
            }
            _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining,
                run.GetLockedCells(), _blurredPencilCells, pressure); // [REQ: DEBUFF-HOOK-013] pass locked cells to renderer
            CheckGameOver();
        }

        private void ClearCell()
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null || _selectedRow < 0) return;
            if (board.GivenMask[_selectedRow, _selectedCol]) return;
            board.PlaceValue(_selectedRow, _selectedCol, 0);
            _highlightValue = 0;
            RenderBoard();
        }

        private void TogglePencilMode()
        {
            _pencilMode = !_pencilMode;
            _numpadView.SetPencilMode(_pencilMode);
            _audio?.PlayPencilToggle();
        }

        // ─────────────────────────── Completion / Game Over ───────────────────────────

        private void HandleCompletion()
        {
            var run = _map?.Run;
            if (run == null || !run.IsLevelComplete) { _completionHandled = false; return; }
            if (_completionHandled) return;
            _completionHandled = true;

            if (run.State.TutorialMode)
            {
                run.CompleteLevelAndGrantRewards();
                if (run.State.RunNumber == 0)
                {
                    // Basics tutorial: play stinger and return after a short pause
                    _audio?.SetContext(RunAudioController.Context.Path);
                    _audio?.PlayPuzzleSolved();
                    RumbleService.PulsePuzzleSolved(this);
                    _boardView?.FlashBoardSolved();
                    SetStatus("Puzzle solved! Well done!");
                    StartCoroutine(ReturnToMenuAfterDelay(2.5f));
                }
                else
                {
                    SetStatus("Tutorial solved!");
                    SaveAndQuit();
                }
                return;
            }

            if (run.State.IsSeasonalChallenge)
            {
                // Accumulate this puzzle's elapsed time into the run total before stopping the timer
                if (run.State != null)
                    run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
                _hudView?.StopPuzzleTimer();

                SetActive(_sudokuPanel, false);
                SetActive(_pathPanel, false);
                _audio?.SetContext(RunAudioController.Context.Path);
                _audio?.PlayPuzzleSolved();
                RumbleService.PulsePuzzleSolved(this);
                _boardView?.FlashBoardSolved();
                _endScreenView.ShowGameOver(run, true);
                return;
            }

            // _justCompletedNodeIndex is set in OnPostRewardReady after reward screen dismissed
            _justCompletedNodeIndex = -1;
            ShowPath();
            _audio?.PlayPuzzleSolved();
            RumbleService.PulsePuzzleSolved(this);
            _boardView?.FlashBoardSolved();
            StartCoroutine(DelayedShowReward());
        }

        private System.Collections.IEnumerator DelayedShowReward()
        {
            // Store completed node index now so OnPostRewardReady can fire the gold ring after dismissal
            _justCompletedNodeIndex = _map?.Run?.State?.CurrentNodeIndex ?? -1;
            yield return new WaitForSecondsRealtime(0.35f);

            var node = _map?.Run?.GetCurrentNode();
            var isBoss = node?.Type == NodeType.Boss;
            var runState = _map?.Run?.State;
            var isFinalBoss = isBoss && runState != null && runState.CurrentFloor >= runState.TotalFloors - 1;

            if (isFinalBoss)
            {
                // Final boss: skip reward — OnPostRewardReady handles victory sequence directly
                OnPostRewardReady();
                yield break;
            }

            if (isBoss)
            {
                // Non-final boss: offer 1-of-3 relic choice instead of item reward
                var choices = _map?.Run?.RollRelicChoices(3);
                if (choices != null && choices.Count > 0)
                {
                    _rewardView.ShowRelicChoicePanel(choices, OnPostRewardReady);
                    yield break;
                }
            }

            _rewardView.ShowRewardScreen();
        }

        private void CheckGameOver()
        {
            if (_gameOverPanel != null && _gameOverPanel.activeSelf) return;
            var run = _map?.Run;
            if (run == null || !run.IsPlayerDead) return;

            // Accumulate this puzzle's elapsed time into the run total before stopping the timer
            if (run.State != null)
                run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
            _hudView?.StopPuzzleTimer();

            SetActive(_sudokuPanel, false);
            SetActive(_pathPanel, false);
            _audio?.SetContext(RunAudioController.Context.Path); // stop puzzle loop before stinger
            _audio?.PlayGameOver();
            _endScreenView.ShowGameOver(run, false);
        }

        // ─────────────────────────── Post-reward callback ───────────────────────────

        private void OnPostRewardReady()
        {
            var node = _map?.Run?.GetCurrentNode();
            if (node != null && node.Type == NodeType.Boss)
            {
                var s = _map.Run.State;
                if (s.CurrentFloor >= s.TotalFloors - 1)
                {
                    SetActive(_sudokuPanel, false);
                    SetActive(_pathPanel, false);
                    // Fire run-won hooks
                    RelicService.OnRunVictory(s);
                    var run = _map.Run;
                    if (run.DailyGoals != null && !s.IsSeasonalChallenge)
                        DailyGoalService.EvaluateInRun(run.DailyGoals, s, run.CurrentLevelState, DailyGoalService.TriggerRunWon);
                    _audio?.SetContext(RunAudioController.Context.Path); // stop puzzle loop before stinger
                    _audio?.PlayRunVictoryFanfare();
                    RumbleService.PulseVictory(this);
                    _boardView?.TriggerDigitRain();
                    StartCoroutine(VictoryScreenFlash());
                    _endScreenView.ShowGameOver(run, true);
                    return;
                }
                _map.AdvanceToNextFloor();
                var newFloor = _map.Run.State.CurrentFloor;
                var floorFlavor = SudokuRoguelike.Run.ParkNarrativeService.GetFloorEntryFlavor(newFloor);
                _pathMessage = floorFlavor;
                StartCoroutine(ClearPathMessageAfterDelay(5f));
                SetStatus("Garden cleared! Next floor...");
                _justCompletedNodeIndex = -1; // floor advance: no ring (node is gone)
            }
            else
            {
                _completedRingArmed = true; // arm so RebuildPathCanvas fires the gold ring
            }
            ShowPath();
            _completedRingArmed = false;
        }

        // ─────────────────────────── Boss gate callback ───────────────────────────

        private void OnBossModsConfirmed(List<BossModifierId> mods)
        {
            var run = _map?.Run;
            if (run != null) run.ChooseBossModifiers(new List<BossModifierId>(mods));

            var idx = _bossNodeIndex;
            _bossNodeIndex = -1;
            if (idx < 0) return;

            // Advance the node and launch overlay generation asynchronously so the main thread
            // stays unblocked. ShowSudoku / RebuildBoard are called from Update once async is done.
            if (!_map.TryAdvanceToBossNodeAsync(idx, out _, out var level)) return;

            _pendingBossLevel  = level;
            _pathMessage       = string.Empty;
            _completionHandled = false;
            _boardView.MarkOverlayDirty();
            _selectedRow    = -1;
            _selectedCol    = -1;
            _highlightValue = 0;
            CancelPendingItemInteraction(false);
            _bagHighlightCells.Clear();
            _bagHighlightEndTime = 0f;

            ShowBossAwakeningPanel();
        }

        // ─────────────────────────── Save / Resume ───────────────────────────

        private void SaveAndQuit()
        {
            Time.timeScale = 1f;

            var runWasSeasonal = _map?.Run?.State?.IsSeasonalChallenge == true;

            // Avoid writing an autosave from the end screen — result screens already clear the active-run save.
            var onEndScreen = _gameOverPanel != null && _gameOverPanel.activeSelf;
            if (!onEndScreen)
            {
                // Accumulate current puzzle's elapsed time into the run total before saving,
                // so the run timer persists correctly across Save & Quit / Resume cycles.
                var runState = _map?.Run?.State;
                if (runState != null)
                    runState.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
                _map?.SaveNow();
            }

            var bootstrap = FindFirstObjectByType<Bootstrap.GameBootstrap>();
            if (bootstrap != null) bootstrap.ReturnToMenu();

            // Seasonal challenge should land back on the Monthly Walk panel with refreshed PB info.
            if (runWasSeasonal)
                FindFirstObjectByType<MainMenuController>()?.ShowMonthlyWalkPanel();
        }

        /// <summary>
        /// Called when the basics tutorial (RunNumber == 0) reaches its final Finish step.
        /// Releases tutorial state so the player can solve the rest of the puzzle freely.
        /// Does NOT return to menu — the player finishes on their own.
        /// </summary>
        private void OnBasicsTutorialFinished()
        {
            _boardView.TutorialHighlightMap = null;
            _basicsTutorial = null;
            _pencilMode = false;
            _numpadView?.SetPencilMode(false);
            RebuildBoard();
            SetStatus("Solve the remaining cells on your own!");
        }

        private System.Collections.IEnumerator ReturnToMenuAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            SaveAndQuit();
        }

        private void ApplyResumeState()
        {
            var run = _map?.Run;
            if (run?.State == null) return;
            _resumeApplied = true;

            if (run.State.TutorialMode) { ShowSudoku(); RebuildBoard(); return; }

            // Resume: already-active puzzle → show board immediately
            if (run.CurrentBoard != null && run.CurrentLevelState != null && !run.IsLevelComplete)
            {
                ShowSudoku(); RebuildBoard(); return;
            }

            // Fresh run: path panel is already visible (shown by NotifyRunStarted).
            // Do not auto-advance — wait for the player to select their first tile.
            var nodePath = run.State.NodePath;
            if ((nodePath == null || nodePath.Count == 0) && run.CurrentBoard == null)
            {
                _pathMessage = "Select your first tile to begin.";
                RefreshPathOverview();
                return;
            }

            var s = run.State;
            var floorName = FloorThemeData.GetFloorName(s.CurrentFloor);
            _pathMessage = $"Resumed — Floor {s.CurrentFloor + 1}: {floorName}";
            ShowPath();
        }

        // ─────────────────────────── Helpers ───────────────────────────

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        // ── Controller indicator badge (top-right of sudoku panel) ──
        private void BuildControllerIndicator()
        {
            if (_sudokuPanel == null) return;
            var go = new GameObject("ControllerGlyphIndicator", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_sudokuPanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-4f, -4f);
            rt.sizeDelta        = new Vector2(80f, 22f);
            var t               = go.GetComponent<Text>();
            t.font              = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize          = 11;
            t.color             = new Color(0.98f, 0.83f, 0.26f, 0.70f);
            t.alignment         = TextAnchor.UpperRight;
            t.text              = string.Empty;
            _controllerIndicatorText = t;
        }

        // ── Full-screen gold flash overlay for victory ──
        private void BuildVictoryFlashOverlay()
        {
            if (_uiRoot == null) return;
            var go  = new GameObject("VictoryFlashOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_uiRoot, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color         = new Color(0.98f, 0.83f, 0.26f, 0f);
            img.raycastTarget = false;
            go.SetActive(false);
            _victoryFlashOverlay = img;
        }

        private IEnumerator VictoryScreenFlash()
        {
            if (_victoryFlashOverlay == null) yield break;
            _victoryFlashOverlay.gameObject.SetActive(true);
            // Phase 1: fast fade in (0.25 s)
            var t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                _victoryFlashOverlay.color =
                    new Color(0.98f, 0.83f, 0.26f, Mathf.Lerp(0f, 0.55f, t / 0.25f));
                yield return null;
            }
            // Phase 2: hold (0.30 s)
            yield return new WaitForSecondsRealtime(0.30f);
            // Phase 3: slow fade out (0.60 s)
            t = 0f;
            while (t < 0.60f)
            {
                t += Time.deltaTime;
                _victoryFlashOverlay.color =
                    new Color(0.98f, 0.83f, 0.26f, Mathf.Lerp(0.55f, 0f, t / 0.60f));
                yield return null;
            }
            _victoryFlashOverlay.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator ClearPathMessageAfterDelay(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _pathMessage = string.Empty;
            RefreshPathOverview();
        }

        private System.Collections.IEnumerator ResetSaveQuitConfirm(float seconds)
        {
            var elapsed = 0f;
            var startAlpha = 0.95f;
            var endAlpha   = 0.30f;
            var baseColor  = new Color(0.55f, 0.18f, 0.10f, startAlpha);
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var a = Mathf.Lerp(startAlpha, endAlpha, t);
                if (_saveQuitImg != null)
                    _saveQuitImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            _saveQuitPending  = false;
            _saveQuitResetCo  = null;
            if (_saveQuitLbl != null) _saveQuitLbl.text  = "Save & Quit";
            if (_saveQuitImg != null) _saveQuitImg.color = new Color(0.22f, 0.18f, 0.14f, 0.90f);
        }

        private System.Collections.IEnumerator FadeOutCompletedRing(RectTransform nodeRt)
        {
            var ringGo = new GameObject("CompletedRing", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(nodeRt, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = new Vector2(-0.08f, -0.08f);
            ringRt.anchorMax = new Vector2(1.08f, 1.08f);
            ringRt.offsetMin = ringRt.offsetMax = Vector2.zero;
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.color         = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.55f);
            ringImg.raycastTarget = false;
            var outline = ringGo.AddComponent<Outline>();
            outline.effectColor    = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.55f);
            outline.effectDistance = new Vector2(3f, -3f);

            const float duration = 1.5f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                // ringGo may have been destroyed externally (e.g. panel teardown) — bail cleanly.
                if (ringGo == null) yield break;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var a = Mathf.Lerp(0.55f, 0f, t);
                ringImg.color = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, a);
                outline.effectColor = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, a);
                yield return null;
            }
            if (ringGo != null) Destroy(ringGo);
            _justCompletedNodeIndex = -1;
        }

        private System.Collections.IEnumerator StaggerRevealNodes(List<(Button btn, float x)> nodes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].btn != null)
                    StartCoroutine(AnimationHelper.PulseScale(nodes[i].btn.transform, 1f, 1.06f, 0.20f));
                yield return new WaitForSeconds(0.04f);
            }
        }

        private System.Collections.IEnumerator CrossFadeFloorBackground(float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (_floorBgRaw  != null) _floorBgRaw.color  = new Color(1f, 1f, 1f, Mathf.Lerp(0f,    0.92f, t));
                if (_floorBgPrev != null) _floorBgPrev.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.92f, 0f,    t));
                yield return null;
            }
            if (_floorBgPrev != null) { _floorBgPrev.texture = null; _floorBgPrev.color = new Color(1f, 1f, 1f, 0f); }
        }

        private System.Collections.IEnumerator UnblurPencilCellAfterDelay(int row, int col, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _blurredPencilCells.Remove((row, col));
            RenderBoard();
        }

        private static bool TryFindEditable(SudokuBoard board, out int r, out int c)
        {
            r = -1; c = -1;
            if (board == null) return false;
            for (var rr = 0; rr < board.Size; rr++)
            for (var cc = 0; cc < board.Size; cc++)
                if (!board.GivenMask[rr, cc]) { r = rr; c = cc; return true; }
            return false;
        }

        private bool HasActiveModifier(BossModifierId mod)
        {
            var cfg = _map?.Run?.CurrentLevelConfig;
            return cfg != null && cfg.ActiveModifiers.Contains(mod);
        }

        private static bool HasConflict(SudokuBoard board, int r, int c)
        {
            var val = board.Cells[r, c];
            if (val == 0) return false;
            var sz = board.Size;
            for (var i = 0; i < sz; i++)
            {
                if (i != c && board.Cells[r, i] == val) return true;
                if (i != r && board.Cells[i, c] == val) return true;
            }
            if (board.RegionMap == null) return false;
            var reg = board.RegionMap[r, c];
            for (var rr = 0; rr < sz; rr++)
            for (var cc = 0; cc < sz; cc++)
                if (board.RegionMap[rr, cc] == reg && (rr != r || cc != c) && board.Cells[rr, cc] == val) return true;
            return false;
        }

        // ─────────────────────────── Item effects ───────────────────────────

        private void ApplyItemEffect(int slotIndex, ItemInstance item)
        {
            var run   = _map?.Run;
            if (run == null) return;
            var board = run.CurrentBoard;
            var s     = run.State;

            switch (item.Type)
            {
                // ── Ink Well: restore pencil marks ──
                case ItemType.InkWell:
                {
                    var amount = ItemService.GetInkWellAmount(item.Rarity);
                    s.CurrentPencil = Math.Min(s.MaxPencil, s.CurrentPencil + amount);
                    run.TryUseItem(slotIndex);
                    SetStatus($"Ink Well: restored {amount} pencil marks.");
                    break;
                }

                // ── Meditation Stone: restore HP ──
                case ItemType.MeditationStone:
                {
                    var amount = ItemService.GetMeditationStoneAmount(item.Rarity);
                    s.CurrentHP = Math.Min(s.MaxHP, s.CurrentHP + amount);
                    run.TryUseItem(slotIndex);
                    SetStatus($"Meditation Stone: restored {amount} HP.");
                    break;
                }

                // ── Offering Bowl: sacrifice HP for gold ──
                case ItemType.OfferingBowl:
                {
                    if (s.CurrentHP <= 1) { SetStatus("Not enough HP to sacrifice."); return; }
                    s.CurrentHP -= 1;
                    s.CurrentGold += 30;
                    run.TryUseItem(slotIndex);
                    SetStatus("Offering Bowl: -1 HP, +30 gold.");
                    break;
                }

                // ── Solver: fill selected cell (+neighbours) ──
                case ItemType.Solver:
                {
                    if (board == null || _selectedRow < 0) { SetStatus("Select a cell first."); return; }
                    var neighbors = ItemService.GetSolverNeighborCount(item.Rarity);
                    var filled    = SolveCells(board, run, _selectedRow, _selectedCol, neighbors);
                    run.TryUseItem(slotIndex);
                    SetStatus($"Solver: filled {filled} cell(s).");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Finder: highlight cells matching selected digit in solution ──
                case ItemType.Finder:
                {
                    if (board == null || _highlightValue <= 0) { SetStatus("Select a number first."); return; }
                    var count = ItemService.GetFinderHighlightCount(item.Rarity);
                    var found = 0;
                    _bagHighlightCells.Clear();
                    for (var r = 0; r < board.Size && found < count; r++)
                    for (var c = 0; c < board.Size && found < count; c++)
                    {
                        if (board.GivenMask[r, c]) continue;
                        if (board.Cells[r, c] == 0 && board.Solution[r, c] == _highlightValue)
                        { _bagHighlightCells.Add((r, c)); found++; }
                    }
                    run.TryUseItem(slotIndex);
                    _bagHighlightEndTime = Time.time + 4f;
                    SetStatus($"Finder: highlighted {found} cell(s) for {_highlightValue}.");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Pattern Scroll: player selects N distinct mechanic instances to solve ──
                case ItemType.PatternScroll:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    run.TryUseItem(slotIndex);
                    _patternScrollPending     = true;
                    _patternScrollChargesLeft = ItemService.GetPatternScrollZones(item.Rarity);
                    _patternScrollMechanics   = null;   // will be built fresh in ShowPatternScrollOverlay
                    _patternScrollSolvedIds   = null;
                    _patternScrollStagedIndex = -1;
                    SetStatus($"Pattern Scroll: select {_patternScrollChargesLeft} mechanic(s) to solve. Press ESC to cancel.");
                    ShowPatternScrollOverlay();
                    break;
                }

                // ── Koi Reflection: player selects cells to reveal candidates for ──
                case ItemType.KoiReflection:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    var count = ItemService.GetKoiReflectionCells(item.Rarity);
                    run.TryUseItem(slotIndex);
                    _koiReflectionPending = true;
                    _koiReflectionChargesLeft = count;
                    SetStatus($"Koi Reflection: click {count} cell(s) to reveal candidates.");
                    break;
                }

                // ── Lantern of Clarity: disable fog for N moves ──
                case ItemType.LanternOfClarity:
                {
                    var moves = ItemService.GetLanternOfClarityMoves(item.Rarity);
                    run.State.FogDisabledMoves += moves;
                    run.TryUseItem(slotIndex);
                    SetStatus($"Lantern of Clarity: fog disabled for {run.State.FogDisabledMoves} moves.");
                    if (board != null)
                        _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                            HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Garden Rake: clear pencil marks in selected row + column ──
                case ItemType.GardenRake:
                {
                    if (board == null || _selectedRow < 0) { SetStatus("Select a cell first."); return; }
                    for (var i2 = 0; i2 < board.Size; i2++)
                    {
                        board.ClearPencilMarks(_selectedRow, i2);
                        board.ClearPencilMarks(i2, _selectedCol);
                    }
                    run.TryUseItem(slotIndex);
                    SetStatus($"Garden Rake: cleared row {_selectedRow + 1} and col {_selectedCol + 1}.");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Pruning Shears: remove one incorrect candidate from selected cell ──
                case ItemType.PruningShears:
                {
                    if (board == null || _selectedRow < 0) { SetStatus("Select a cell first."); return; }
                    if (board.Cells[_selectedRow, _selectedCol] != 0) { SetStatus("Cell is already filled."); return; }
                    var marks   = board.GetPencilMarks(_selectedRow, _selectedCol);
                    var correct = board.Solution[_selectedRow, _selectedCol];
                    var removed = false;
                    foreach (var v in marks)
                    {
                        if (v != correct) { board.RemovePencilMark(_selectedRow, _selectedCol, v); removed = true; break; }
                    }
                    if (!removed) { SetStatus("No incorrect candidates to prune."); return; }
                    run.TryUseItem(slotIndex);
                    SetStatus("Pruning Shears: removed one incorrect candidate.");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Zen Sand Sifter: highlight cells with exactly two candidates ──
                case ItemType.ZenSandSifter:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    _bagHighlightCells.Clear();
                    for (var r = 0; r < board.Size; r++)
                    for (var c = 0; c < board.Size; c++)
                    {
                        if (board.GivenMask[r, c] || board.Cells[r, c] != 0) continue;
                        var usedMask = 0;
                        for (var rr = 0; rr < board.Size; rr++) if (board.Cells[rr, c] > 0) usedMask |= 1 << board.Cells[rr, c];
                        for (var cc = 0; cc < board.Size; cc++) if (board.Cells[r, cc] > 0) usedMask |= 1 << board.Cells[r, cc];
                        var reg = board.RegionMap[r, c];
                        for (var rr = 0; rr < board.Size; rr++)
                        for (var cc = 0; cc < board.Size; cc++)
                            if (board.RegionMap[rr, cc] == reg && board.Cells[rr, cc] > 0) usedMask |= 1 << board.Cells[rr, cc];
                        var cands = 0;
                        for (var v = 1; v <= board.Size; v++) if ((usedMask & (1 << v)) == 0) cands++;
                        if (cands == 2) _bagHighlightCells.Add((r, c));
                    }
                    run.TryUseItem(slotIndex);
                    _bagHighlightEndTime = Time.time + 5f;
                    SetStatus($"Zen Sand Sifter: {_bagHighlightCells.Count} cells with exactly 2 candidates.");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Ginkgo Leaf: undo last mistake placement ──
                case ItemType.GinkgoLeaf:
                {
                    if (board == null || _lastMistakeRow < 0) { SetStatus("No mistake to undo."); return; }
                    board.PlaceValue(_lastMistakeRow, _lastMistakeCol, 0);
                    var hpRestore = Math.Max(1, s.LastMistakeHpLost);
                    s.CurrentHP = Math.Min(s.MaxHP, s.CurrentHP + hpRestore);
                    // Also restore the combo streak that was lost on the mistake
                    if (s.LastComboBeforeMistake > 0)
                        s.ComboStreak = s.LastComboBeforeMistake;
                    run.TryUseItem(slotIndex);
                    SetStatus($"Ginkgo Leaf: mistake undone, +{hpRestore} HP, combo restored.");
                    _lastMistakeRow = -1;
                    _lastMistakeCol = -1;
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Rice Paper Umbrella: block next 2 mistake penalties ──
                case ItemType.RicePaperUmbrella:
                {
                    run.State.MistakeShieldCharges += 2;
                    run.TryUseItem(slotIndex);
                    SetStatus($"Rice Paper Umbrella: next {run.State.MistakeShieldCharges} mistake(s) won't cost HP.");
                    break;
                }

                // ── Temple Incense: player clicks N cells to fill correct answer ──
                case ItemType.TempleIncense:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    var count = item.Rarity == ItemRarity.Epic ? 6 : item.Rarity == ItemRarity.Rare ? 4 : 2;
                    run.TryUseItem(slotIndex);
                    _templeIncensePending = true;
                    _templeIncenseChargesLeft = count;
                    SetStatus($"Temple Incense: click {count} empty cell(s) to fill their correct answer.");
                    break;
                }

                // ── Koi Dragon Scale: fill the most-complete unit ──
                case ItemType.KoiDragonScale:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    var count = FillMostCompleteUnit(board, run);
                    if (count == 0) { SetStatus("No incomplete unit found."); return; }
                    run.TryUseItem(slotIndex);
                    SetStatus($"Koi Dragon Scale: filled {count} cell(s) in the most-complete unit.");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Golden Kintsugi Jar: highlight all mistakes ──
                case ItemType.GoldenKintsugiJar:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    _bagHighlightCells.Clear();
                    for (var r = 0; r < board.Size; r++)
                    for (var c = 0; c < board.Size; c++)
                    {
                        if (board.Cells[r, c] == 0 || board.GivenMask[r, c]) continue;
                        if (board.Cells[r, c] != board.Solution[r, c])
                            _bagHighlightCells.Add((r, c));
                    }
                    run.TryUseItem(slotIndex);
                    _bagHighlightEndTime = Time.time + 6f;
                    SetStatus($"Golden Kintsugi Jar: found {_bagHighlightCells.Count} mistake(s).");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Silk Fan: swap two non-given cells ──
                case ItemType.SilkFan:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    _swapMode = true;
                    _swapRow1 = -1; _swapCol1 = -1;
                    run.TryUseItem(slotIndex);
                    SetStatus("Silk Fan: click first cell to swap...");
                    break;
                }

                // ── Wind Chime: player selects which house(s) to fill ──
                case ItemType.WindChime:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    run.TryUseItem(slotIndex);
                    _windChimeRarity = item.Rarity;
                    _windChimeFillRow = _windChimeFillCol = _windChimeFillBox = false;
                    if (item.Rarity == ItemRarity.Epic)
                    {
                        // Epic: all 3 houses — skip choice, go straight to cell-click
                        _windChimeFillRow = _windChimeFillCol = _windChimeFillBox = true;
                        _windChimePending = true;
                        SetStatus("Wind Chime: click any cell to fill its row + column + box. ESC cancels.");
                    }
                    else
                    {
                        _windChimeMaxHouses      = item.Rarity == ItemRarity.Rare ? 2 : 1;
                        _windChimeChoosingHouses = true;
                        SetStatus($"Wind Chime: press 1=Row 2=Col 3=Box to select {_windChimeMaxHouses} house(s). ESC cancels.");
                    }
                    break;
                }

                default:
                    SetStatus($"Used {ItemService.GetItemName(item.Type)}.");
                    run.TryUseItem(slotIndex);
                    break;
            }

            _hudView.Refresh(run?.State, run?.CurrentLevelConfig);
        }

        private int SolveCells(SudokuBoard board, RunDirector run, int row, int col, int neighborCount)
        {
            var filled = 0;
            if (!board.GivenMask[row, col] && board.Cells[row, col] != board.Solution[row, col])
            {
                run.PlaceNumber(row, col, board.Solution[row, col]);
                filled++;
            }
            if (neighborCount <= 0) return filled;
            var dirs  = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            var added = 0;
            foreach (var (dr, dc) in dirs)
            {
                var nr = row + dr; var nc = col + dc;
                if (nr < 0 || nr >= board.Size || nc < 0 || nc >= board.Size) continue;
                if (!board.GivenMask[nr, nc] && board.Cells[nr, nc] != board.Solution[nr, nc])
                {
                    run.PlaceNumber(nr, nc, board.Solution[nr, nc]);
                    filled++;
                    added++;
                    if (added >= neighborCount) break;
                }
            }
            return filled;
        }

        private static int FillMostCompleteUnit(SudokuBoard board, RunDirector run)
        {
            var best       = -1;
            var bestFilled = 0;
            var bestType   = 0; // 0=row, 1=col, 2=region

            // Check rows
            for (var r = 0; r < board.Size; r++)
            {
                var f = 0;
                for (var c = 0; c < board.Size; c++) if (board.Cells[r, c] != 0) f++;
                if (f > bestFilled && f < board.Size) { bestFilled = f; best = r; bestType = 0; }
            }
            // Check cols
            for (var c = 0; c < board.Size; c++)
            {
                var f = 0;
                for (var r = 0; r < board.Size; r++) if (board.Cells[r, c] != 0) f++;
                if (f > bestFilled && f < board.Size) { bestFilled = f; best = c; bestType = 1; }
            }
            // Check regions
            for (var reg = 0; reg < board.Size; reg++)
            {
                var f = 0;
                for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.RegionMap[r, c] == reg && board.Cells[r, c] != 0) f++;
                if (f > bestFilled && f < board.Size) { bestFilled = f; best = reg; bestType = 2; }
            }

            if (best < 0) return 0;
            var count = 0;
            if (bestType == 0)
            {
                for (var c = 0; c < board.Size; c++)
                    if (!board.GivenMask[best, c] && board.Cells[best, c] != board.Solution[best, c])
                    { run.PlaceNumber(best, c, board.Solution[best, c]); count++; }
            }
            else if (bestType == 1)
            {
                for (var r = 0; r < board.Size; r++)
                    if (!board.GivenMask[r, best] && board.Cells[r, best] != board.Solution[r, best])
                    { run.PlaceNumber(r, best, board.Solution[r, best]); count++; }
            }
            else
            {
                for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.RegionMap[r, c] == best && !board.GivenMask[r, c] && board.Cells[r, c] != board.Solution[r, c])
                    { run.PlaceNumber(r, c, board.Solution[r, c]); count++; }
            }
            return count;
        }

        private void HandleKoiReflectionClick(int r, int c)        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) { _koiReflectionPending = false; return; }
            if (board.GivenMask[r, c] || board.Cells[r, c] != 0)
            {
                SetStatus($"Koi Reflection: pick an empty cell ({_koiReflectionChargesLeft} remaining).");
                return;
            }
            var usedMask = 0;
            for (var rr = 0; rr < board.Size; rr++) if (board.Cells[rr, c] > 0) usedMask |= 1 << board.Cells[rr, c];
            for (var cc = 0; cc < board.Size; cc++) if (board.Cells[r, cc] > 0) usedMask |= 1 << board.Cells[r, cc];
            var reg = board.RegionMap[r, c];
            for (var rr = 0; rr < board.Size; rr++)
            for (var cc = 0; cc < board.Size; cc++)
                if (board.RegionMap[rr, cc] == reg && board.Cells[rr, cc] > 0) usedMask |= 1 << board.Cells[rr, cc];
            for (var v = 1; v <= board.Size; v++)
                if ((usedMask & (1 << v)) == 0) board.AddPencilMark(r, c, v);

            _koiReflectionChargesLeft--;
            if (_koiReflectionChargesLeft <= 0)
            {
                _koiReflectionPending = false;
                SetStatus("Koi Reflection: candidates revealed.");
            }
            else
            {
                SetStatus($"Koi Reflection: candidates revealed ({_koiReflectionChargesLeft} remaining).");
            }
            RenderBoard();
        }

        private void HandleWindChimeClick(int r, int c)
        {
            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null) { _windChimePending = false; return; }

            var filled = 0;
            if (_windChimeFillRow)
            {
                for (var cc = 0; cc < board.Size; cc++)
                    if (!board.GivenMask[r, cc] && board.Cells[r, cc] == 0)
                    { run.PlaceNumber(r, cc, board.Solution[r, cc]); filled++; }
            }
            if (_windChimeFillCol)
            {
                for (var rr = 0; rr < board.Size; rr++)
                    if (!board.GivenMask[rr, c] && board.Cells[rr, c] == 0)
                    { run.PlaceNumber(rr, c, board.Solution[rr, c]); filled++; }
            }
            if (_windChimeFillBox)
            {
                var reg = board.RegionMap[r, c];
                for (var rr = 0; rr < board.Size; rr++)
                for (var cc = 0; cc < board.Size; cc++)
                    if (board.RegionMap[rr, cc] == reg && !board.GivenMask[rr, cc] && board.Cells[rr, cc] == 0)
                    { run.PlaceNumber(rr, cc, board.Solution[rr, cc]); filled++; }
            }
            _windChimePending = false;
            _windChimeFillRow = _windChimeFillCol = _windChimeFillBox = false;
            _map?.SaveNow();
            SetStatus($"Wind Chime: filled {filled} cell(s).");
            RenderBoard();
        }

        private void HandleTempleIncenseClick(int r, int c)
        {
            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null) { _templeIncensePending = false; return; }
            if (board.GivenMask[r, c] || board.Cells[r, c] != 0)
            {
                SetStatus($"Temple Incense: pick an empty cell ({_templeIncenseChargesLeft} remaining).");
                return;
            }
            run.PlaceNumber(r, c, board.Solution[r, c]);
            _templeIncenseChargesLeft--;
            if (_templeIncenseChargesLeft <= 0)
            {
                _templeIncensePending = false;
                _map?.SaveNow();
                SetStatus("Temple Incense: done.");
            }
            else
            {
                SetStatus($"Temple Incense: filled ({_templeIncenseChargesLeft} remaining).");
            }
            RenderBoard();
        }

        private void HandleSwapClick(int r, int c)
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) { _swapMode = false; return; }
            if (board.GivenMask[r, c]) { SetStatus("Can't swap a given cell. Click another."); return; }

            if (_swapRow1 < 0)
            {
                _swapRow1 = r; _swapCol1 = c;
                SetStatus($"Silk Fan: first cell ({r + 1},{c + 1}) selected. Click second cell.");
            }
            else
            {
                if (r == _swapRow1 && c == _swapCol1) { SetStatus("Silk Fan: same cell. Pick a different one."); return; }
                var val1 = board.Cells[_swapRow1, _swapCol1];
                var val2 = board.Cells[r, c];
                board.PlaceValue(_swapRow1, _swapCol1, val2);
                board.PlaceValue(r, c, val1);
                _swapMode = false;
                _swapRow1 = -1; _swapCol1 = -1;
                SetStatus("Silk Fan: swapped cells.");
                RenderBoard();
            }
        }
    }
}
