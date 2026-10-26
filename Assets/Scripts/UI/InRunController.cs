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
    public sealed partial class InRunController : MonoBehaviour
    {
        private static readonly WaitForSeconds _bossPulseWait = new WaitForSeconds(0.40f);

        // ── Injected ──
        private RunMapController _map;
        private GameObject _pathPanel;
        private GameObject _sudokuPanel;
        private GameObject _gameOverPanel;
        private Text _pathStatsBar;
        private Text _goldLabel;
        private int _shownGold = int.MinValue;
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
        /// <summary>The digit whose cells are highlighted (0 = none).</summary>
        public int HighlightValue => _highlightValue;
        private bool _pencilMode;
        private bool _completionHandled;
        private int  _pendingKeyboardDigit;
        private bool _resumeApplied;
        private bool _tutorialShown;
        private bool _spiritTrialsTimerStarted;

        // Input safety: block digit entry for a short window after cell selection
        // to prevent accidental placements on touch/tap. GameDesignSpec 4.2: 0.25 s (halved after the 0.2.0 playtest).
        private float _cellSelectedTime = -1f;
        public const float CellSelectInputDelay = GameplaySettings.DefaultInputLockSeconds;   // the input lock's default (OPT-GAME-006)

        // ── Sudoku Basics Tutorial ──
        private SudokuBasicsTutorialController _basicsTutorial;

        // ── Blurred Sight curse ──
        private readonly HashSet<(int, int)> _blurredPencilCells = new HashSet<(int, int)>();
        private readonly List<Coroutine> _unblurCoroutines = new List<Coroutine>();

        // ── Gamepad overlay focus — reused each frame to avoid per-frame allocation ──
        private readonly List<Button> _overlayBtnCache = new List<Button>();
        private readonly List<Selectable> _optionsFocusCache = new List<Selectable>();

        // ── Swap state (Silk Fan item) ──
        private bool _swapMode;
        private int _swapRow1 = -1, _swapCol1 = -1;
        private int _lastMistakeRow = -1, _lastMistakeCol = -1;

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
        private const float  TriggerThresh     = 0.3f;
        private float _triggerLPrev, _triggerRPrev;
        private float _pathBConfirmTimer;   // retained but unused after MAP-2 rework
        private float _bButtonHoldTime;      // PS-2: tracks how long B is held in puzzle focus
        private float _selectButtonHoldTime; // tracks how long Select is held for hold-to-save shortcut
        private bool  _pathQuitModalActive;  // MAP-2: true while the path quit-confirm modal is shown

        // ── Item state ──
        private HashSet<(int row, int col)> _bagHighlightCells;

        // Fog moves are stored in RunState.FogDisabledMoves (persisted); read via this accessor
        private int _fogDisabledMovesRemaining =>
            (_gardenLanternUnfogUntil > 0f && Time.realtimeSinceStartup < _gardenLanternUnfogUntil)
                ? int.MaxValue
                : (_map?.Run?.State?.FogDisabledMoves ?? 0);

        // Garden Lantern: real-time timestamp (Time.realtimeSinceStartup) at which the 15-second
        // fog reveal expires. 0 when no lantern is active. Not persisted — save+quit re-fogs the board.
        private float _gardenLanternUnfogUntil;
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
        private RectTransform _floorProgressStrip;      // Thematic 4: floor progress dot strip
        private int           _lastBuiltFloorForStrip = -1; // skip rebuild when floor unchanged
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
        private readonly List<(Button btn, float x)> _allNodeButtons = new List<(Button btn, float x)>();

        // ── Options / Boss ──
        private GameObject _inGameOptionsPanel;
        private int _bossNodeIndex = -1;

        // ── Boss Awakening async loading ──
        private LevelConfig _pendingAsyncLevel;
        private GameObject _bossAwakeningPanel;
        private CanvasGroup _bossAwakeningCg;
        private Text _bossAwakeningLabel;
        private Coroutine _bossAwakeningDotsCoroutine;
        private string _asyncGenerationBaseText = "Creating Puzzle";

        // ── Controller feedback ──
        private RectTransform _uiRoot;
        private Text  _controllerIndicatorText;
        private Image _victoryFlashOverlay;

        // ── Systems ──
        private RunAudioController _audio;
        private AccessibilityService _accessibility = new AccessibilityService();
        private readonly ControllerIndicatorPoller _controllerIndicatorPoller = new ControllerIndicatorPoller();
        private readonly AsyncLevelCompletionPresenter _asyncLevelCompletionPresenter = new AsyncLevelCompletionPresenter();
        private readonly TimedVisualStateController _timedVisualStateController = new TimedVisualStateController();

        // ── Icon cache: all node/legend sprites preloaded once at Awake ──
        private readonly Dictionary<string, Sprite> _nodeIconCache = new Dictionary<string, Sprite>();

        // ── Cached delegates for NodeHoverHandler — created once, reused across all nodes ──
        private Action<RunNode, LevelConfig, Vector2> _showNodeTooltipAction;
        private Action                                _hideNodeTooltipAction;
        private Action<Vector2>                       _showBlockedTooltipAction;

        // ── RebuildPathCanvas reusable allocations ──
        private readonly Dictionary<int, int> _visitOrder = new Dictionary<int, int>();
        private readonly HashSet<(int, int)> _walkedEdges = new HashSet<(int, int)>();

        // ─────────────────────────── Lifecycle ───────────────────────────

        private void Awake()
        {
            _bagHighlightCells = new HashSet<(int, int)>();
            PreloadNodeIcons();
            _showNodeTooltipAction  = ShowNodeTooltip;
            _hideNodeTooltipAction  = HideNodeTooltip;
            _showBlockedTooltipAction = ShowFirstPickBlockedTooltip;
        }

        private void PreloadNodeIcons()
        {
            // All paths used by CreateNodeButton (via GetNodeIconPath) and the legend strip.
            var paths = new[]
            {
                "node/icon_engraved_stone",
                "node/icon_puzzle_tile",
                "node/icon_elite_mask",
                "node/icon_preboss_gate",
                "node/icon_demon_mask",
                "node/icon_market_stall",
                "node/icon_campfire_stones",
                "node/icon_relic_pedestal",
                "node/icon_event_scroll",
                "node/icon_moss_trap",
                "node/icon_crosslink_bridge",
                "ui/icon_flow_ribbon",
            };
            foreach (var p in paths)
            {
                var s = Resources.Load<Sprite>(p);
                if (s != null) _nodeIconCache[p] = s;
            }
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
            // Share the blurred-cell set with RunDirector so ExportPuzzleSaveState can capture it
            if (map?.Run != null) map.Run.BlurredPencilCells = _blurredPencilCells;
            _hudView.Configure(map, _sudokuPanel, hpText, pencilText);
            _numpadView.Configure(numpadRoot);
            _rewardView.Configure(map, _pathPanel);
            _shopView.Configure(map, _pathPanel,
                (item, onReplace, onAbort) => _rewardView.ShowBagSwapPanel(item, onReplace, onAbort));
            _bossGateView.Configure(map, uiRoot);
            _endScreenView.Configure(map, _gameOverPanel, goSummary, goDetails, goBack);
            _uiFlowCtrl = GetComponent<InRunUiFlowController>();
            _eventChoiceView = _uiFlowCtrl?.EventChoiceScreen;
            _eventChoiceView?.SetSceneHost(_pathPanel);   // UI-STYLE-014: the event scene covers the path (GAP-052)

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
            _rewardView.PuzzleSeconds     = () => _hudView != null ? _hudView.LastPuzzleSeconds : 0f; // the reward tally's time (GAP-055)
            _shopView.OnStatusChanged     = msg => SetStatus(msg);
            _shopView.OnShopClosed        = ShowPath;
            _bossGateView.OnModsConfirmed = OnBossModsConfirmed;
            _endScreenView.OnReturnToMenuPressed = SaveAndQuit;
            _endScreenView.OnWalkAgainPressed = WalkAgain;

            // ── Controller feedback overlays ──
            _uiRoot = uiRoot;
            BuildControllerIndicator();
            BuildVictoryFlashOverlay();

            // Wire Options / Save & Quit as soon as the panels are known (also re-done per run start).
            WireInGameButtons();
            ShowPath();
        }

        public void NotifyRunStarted()
        {
            _audio = FindFirstObjectByType<RunAudioController>();
            _resumeApplied     = false;
            _tutorialShown     = false;
            _completionHandled = false;
            _spiritTrialsTimerStarted = false;
            _introCardShown    = false;   // #12
            _floorEntryShownFor = -1;     // UI-STYLE-021
            DismissFloorEntry(false);
            _prevReachableIndices.Clear(); // #3
            _endScreenView.ResetShown();
            RefreshAccessibility();
            if (_inGameOptionsPanel != null) _inGameOptionsPanel.SetActive(false);
            _boardView.TutorialHighlightMap = null;
            _basicsTutorial = null;
            _boardView?.MarkOverlayDirty();
            WireInGameButtons();
            ShowPath();
        }

        // [REQ: ACCESS-COLOR-002] NotifyAccessibilityChanged: called by OptionsController on SetColorblindMode/SetHighContrast; rebuilds board overlay and refreshes particles
        public void NotifyAccessibilityChanged()
        {
            RefreshAccessibility();
            if (_map?.Run?.CurrentBoard != null)
            {
                _boardView.MarkOverlayDirty();
                RebuildBoard();
            }
            // [REQ: ACCESS-PARTICLE-001] Particles (ambient seasonal FX) are refreshed on every accessibility change; AmbientParticleController.RefreshSettings stops them when ReduceMotion=true
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

            WireBuyPencilButton(pr);

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
                var closeBtn = FindInPanel<Button>(_inGameOptionsPanel.transform, "BtnInGameOptionsClose");
                if (closeBtn != null)
                {
                    closeBtn.onClick.RemoveAllListeners();
                    closeBtn.onClick.AddListener(ToggleOptionsPanel);
                }
                WireRunMenu();
            }
        }

        private void ToggleOptionsPanel()
        {
            if (_inGameOptionsPanel == null) return;
            var nowActive = !_inGameOptionsPanel.activeSelf;
            _inGameOptionsPanel.SetActive(nowActive);
            if (nowActive)
            {
                RefreshRunMenu();
                RebuildOptionsFocusCache();
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (_optionsFocusCache.Count > 0)
                    es?.SetSelectedGameObject(_optionsFocusCache[0].gameObject);
                else
                    InRunUiFactory.SelectFirstInteractable(_inGameOptionsPanel);
            }
        }

        private void RebuildOptionsFocusCache()
        {
            _optionsFocusCache.Clear();
            if (_inGameOptionsPanel == null) return;
            foreach (var sel in _inGameOptionsPanel.GetComponentsInChildren<Selectable>(false))
            {
                if (sel.IsInteractable() && sel.gameObject.activeInHierarchy)
                    _optionsFocusCache.Add(sel);
            }
        }

        private InRunCancelContext BuildInRunCancelContext()
        {
            var overlayActive = TryGetActiveOverlay(out _);
            return new InRunCancelContext(
                IsActive(_inGameOptionsPanel),
                overlayActive,
                IsActive(_pathPanel),
                _pathQuitModalActive,
                _gbControllerInItemsFocus,
                HasPendingItemInteraction);
        }

        private bool HasPendingItemInteraction =>
            _windChimeChoosingHouses || _windChimePending ||
            _templeIncensePending || _koiReflectionPending ||
            _swapMode || _patternScrollPending;

        private void ExecuteInRunCancelRoute(CancelRouteAction route)
        {
            switch (route)
            {
                case CancelRouteAction.CloseInRunOptions:
                    if (IsActive(_inGameOptionsPanel))
                        ToggleOptionsPanel();
                    _inputRemap.Tick();
                    break;
                case CancelRouteAction.ExitItemsFocus:
                    ExitItemsFocus();
                    _inputRemap.Tick();
                    break;
                case CancelRouteAction.CancelPendingItemInteraction:
                    CancelPendingItemInteraction();
                    _inputRemap.Tick();
                    break;
                case CancelRouteAction.ShowPathQuitConfirmation:
                    ShowPathQuitConfirmModal();
                    _inputRemap.Tick();
                    break;
            }
        }

        private void ExitItemsFocus()
        {
            _gbControllerInItemsFocus = false;
            _numpadView?.HighlightControllerCursor(null);
            SetStatus(T("Puzzle mode  (Tab for items)"));
        }

        private static bool IsActive(GameObject go) => go != null && go.activeSelf;

        public void SetInGameOptionsPanel(GameObject panel) => _inGameOptionsPanel = panel;
        public void SaveAndQuitPublic() => SaveAndQuit();

        private void Update()
        {
            InputRemapService.UpdateCursorVisibility();

            if (_map == null) return;
            var run = _map.Run;
            if (_gameOverPanel == null || !_gameOverPanel.activeSelf) RunLengthLog.Tick(run?.State, Time.unscaledDeltaTime);

            // GAP-029: gold granted by rewards, events or the shop shows at once, not only on the next path refresh.
            if (_goldLabel != null && run?.State != null && _shownGold != run.State.CurrentGold)
            {
                _shownGold = run.State.CurrentGold;
                _goldLabel.text = _shownGold.ToString();
            }

            if (_timedVisualStateController.HasExpired(_gardenLanternUnfogUntil, Time.realtimeSinceStartup))
            {
                _gardenLanternUnfogUntil = 0f;
                RenderBoard();
            }

            if (_controllerIndicatorPoller.Tick(Time.deltaTime, out var controllerIndicatorLabel) &&
                _controllerIndicatorText != null)
            {
                _controllerIndicatorText.text = controllerIndicatorLabel;
            }

            if (run == null) return;

            if (!_resumeApplied) { ApplyResumeState(); return; }

            // UI-STYLE-021: a click or any key skips the floor's painting; nothing behind it reacts meanwhile.
            if (IsFloorEntryShowing)
            {
                if (FloorEntrySkipPressed()) DismissFloorEntry(true);
                _inputRemap.Tick();
                return;
            }

            // Poll async puzzle generation completion; finalize state and show puzzle once ready.
            if (_pendingAsyncLevel != null && run.TryCompleteAsyncLevel())
            {
                var completionView = _asyncLevelCompletionPresenter.BuildCompletedView(
                    _pendingAsyncLevel,
                    run.CurrentBoard != null);
                _pendingAsyncLevel = null;
                HideBossAwakeningPanel();
                if (completionView.State == AsyncLevelCompletionViewState.Failed)
                {
                    _map.RollbackPendingGeneration();
                    SetStatus(completionView.LocalizeStatus ? T(completionView.StatusText) : completionView.StatusText);
                    ShowPath();
                    return;
                }

                _map.ClearPendingGenerationRollback();
                SetStatus(completionView.LocalizeStatus ? T(completionView.StatusText) : completionView.StatusText);
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

            // Number keys on the scenes over the path (reward, rest, shop, event). HandleKeyboard below runs only while
            // the board is open, and these scenes open after it closes, so their keys never arrived (GAP-056).
            for (var k = 0; k < 9; k++)
                if ((Input.GetKeyDown(KeyCode.Alpha1 + k) || Input.GetKeyDown(KeyCode.Keypad1 + k)) && TryHandleSceneDigit(k))
                {
                    _inputRemap.Tick();
                    return;
                }

            if (_sudokuPanel != null && _sudokuPanel.activeSelf)
            {
                HandleKeyboard();
                HandleCompletion();
                _hudView.Refresh(run.State, run.CurrentLevelConfig);
                RefreshBuyPencilButton(run);
                CheckGameOver();
                if (Input.GetKeyDown(KeyCode.Q)) SaveAndQuit();
                if (Input.GetKeyDown(KeyCode.O)) ToggleOptionsPanel();
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
                // GAP-029: the Monthly Walk has no path, so the blueprint's "Select a path" status is replaced.
                SetStatus(T("InRun.Status.MonthlyWalk"));
                return;
            }
            if (run?.State?.Mode == GameMode.EndlessZen || run?.State?.Mode == GameMode.SpiritTrials)
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
                return; // the intro card refreshes the path and enters the floor on dismiss
            }

            RefreshPathOverview();
            ShowFloorEntryIfNew(run?.State);
        }

        private void ShowSudoku()
        {
            DismissFloorEntry(false);
            MarkFloorEntered(_map?.Run?.State);   // UI-STYLE-021
            SetActive(_pathPanel, false);
            SetActive(_gameOverPanel, false);
            ShowPanelWithFade(_sudokuPanel, _sudokuCg);
            var floor = _map?.Run?.State?.CurrentFloor ?? 0;
            var state = _map?.Run?.State;
            _audio?.SetMode(state?.Mode ?? GameMode.GardenRun);
            _audio?.SetFloorIndex(floor);
            _audio?.SetContext(RunAudioController.Context.Puzzle);
            // [REQ: AUDIO-MUSIC-003] GAP-045: a boss puzzle plays the floor's boss track.
            if (_map?.Run?.CurrentLevelConfig?.IsBoss == true)
                _audio?.StartBossLayer();
            else
                _audio?.StopBossLayer();
            if (state?.Mode == GameMode.SpiritTrials)
            {
                _spiritTrialsTimerStarted = false;
                _hudView?.StopPuzzleTimer();
                return;
            }

            _hudView?.StartPuzzleTimer();
        }

        // [REQ: TRIAL-SESSION-002] Timer starts on first cell interaction via EnsureSpiritTrialsTimerStarted
        private void EnsureSpiritTrialsTimerStarted()
        {
            var state = _map?.Run?.State;
            if (state?.Mode != GameMode.SpiritTrials || _spiritTrialsTimerStarted) return;
            _spiritTrialsTimerStarted = true;
            _hudView?.StartPuzzleTimer();
        }

        // #12 — Full-screen intro card shown once at the start of a new run
        private void ShowRunIntroCard(RunDirector run)
        {
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas ?? FindFirstObjectByType<Canvas>()?.rootCanvas;
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
            nameTxt.rectTransform.anchorMin = new Vector2(0.1f, 0.49f);
            nameTxt.rectTransform.anchorMax = new Vector2(0.9f, 0.58f);
            nameTxt.rectTransform.offsetMin = nameTxt.rectTransform.offsetMax = Vector2.zero;
            nameTxt.fontStyle = FontStyle.Bold;

            var narrativeTxt = InRunUiFactory.CreateText(card.transform, "ClassNarrative",
                ParkNarrativeService.GetClassIntroFlavor(run.State.ClassId), 13, TextAnchor.UpperCenter,
                new Color(0.82f, 0.80f, 0.68f, 1f));
            narrativeTxt.rectTransform.anchorMin = new Vector2(0.08f, 0.34f);
            narrativeTxt.rectTransform.anchorMax = new Vector2(0.92f, 0.48f);
            narrativeTxt.rectTransform.offsetMin = narrativeTxt.rectTransform.offsetMax = Vector2.zero;
            narrativeTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            var passiveTxt = InRunUiFactory.CreateText(card.transform, "Passive",
                LocalizationService.T($"Class.Passive.{def.Id}", def.PassiveDescription), 14, TextAnchor.UpperCenter,
                new Color(0.90f, 0.87f, 0.72f, 1f));
            passiveTxt.rectTransform.anchorMin = new Vector2(0.08f, 0.20f);
            passiveTxt.rectTransform.anchorMax = new Vector2(0.92f, 0.34f);
            passiveTxt.rectTransform.offsetMin = passiveTxt.rectTransform.offsetMax = Vector2.zero;
            passiveTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            var floorTxt = InRunUiFactory.CreateText(card.transform, "FloorLabel",
                LocalizationService.Format("InRun.ClassIntro.Floor", "Floor {0}", 1), 18, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.72f, 0.58f, 1f));
            floorTxt.rectTransform.anchorMin = new Vector2(0.1f, 0.13f);
            floorTxt.rectTransform.anchorMax = new Vector2(0.9f, 0.20f);
            floorTxt.rectTransform.offsetMin = floorTxt.rectTransform.offsetMax = Vector2.zero;

            var dismissTxt = InRunUiFactory.CreateText(card.transform, "Dismiss",
                T("InRun.ClassIntro.BeginHint"), 11, TextAnchor.MiddleCenter,
                new Color(0.60f, 0.58f, 0.48f, 1f));
            dismissTxt.rectTransform.anchorMin = new Vector2(0.1f, 0.07f);
            dismissTxt.rectTransform.anchorMax = new Vector2(0.9f, 0.14f);
            dismissTxt.rectTransform.offsetMin = dismissTxt.rectTransform.offsetMax = Vector2.zero;

            // Tap-to-dismiss button (invisible, covers entire card)
            var tapBtn = card.AddComponent<Button>();
            tapBtn.transition = Selectable.Transition.None;
            // Tap, or the auto-dismiss after 3 seconds, closes the card once (G-1).
            _introCardDismissed = false;
            tapBtn.onClick.AddListener(() =>
            {
                if (_introCardDismissed) return;  // G-1 guard
                _introCardDismissed = true;
                Destroy(card);
                RefreshPathOverview();
                ShowFloorEntryIfNew(_map?.Run?.State);
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
                ShowFloorEntryIfNew(_map?.Run?.State);
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
            txt.text = "Creating Puzzle...";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 28;
            txt.color = new Color(1f, 0.85f, 0.4f, 1f);
            txt.font = FontAssetService.GetFont();
            _bossAwakeningLabel = txt;

            _bossAwakeningCg = go.AddComponent<CanvasGroup>();
            _bossAwakeningCg.alpha = 0f;
            go.SetActive(false);
            _bossAwakeningPanel = go;
        }

        private void ShowBossAwakeningPanel(string label = "Creating Puzzle")
        {
            if (_bossAwakeningPanel == null) return;
            _bossAwakeningPanel.SetActive(true);
            if (_bossAwakeningLabel != null)
                _bossAwakeningLabel.text = label + "...";
            _asyncGenerationBaseText = label;
            var dur = _accessibility.GetAnimationDuration(AnimationHelper.MenuPanelDuration);
            StartCoroutine(AnimationHelper.FadeIn(_bossAwakeningCg, dur));
            if (_bossAwakeningDotsCoroutine != null) StopCoroutine(_bossAwakeningDotsCoroutine);
            _bossAwakeningDotsCoroutine = StartCoroutine(AnimateBossAwakeningDots());
        }

        private void HideBossAwakeningPanel()
        {
            if (_bossAwakeningPanel == null) return;
            if (_bossAwakeningDotsCoroutine != null)
            {
                StopCoroutine(_bossAwakeningDotsCoroutine);
                _bossAwakeningDotsCoroutine = null;
            }
            _bossAwakeningPanel.SetActive(false);
            if (_bossAwakeningCg != null) _bossAwakeningCg.alpha = 0f;
        }

        private System.Collections.IEnumerator AnimateBossAwakeningDots()
        {
            var wait = new WaitForSeconds(0.45f);
            var dots = new[] { ".", "..", "..." };
            var i = 0;
            while (true)
            {
                if (_bossAwakeningLabel != null)
                    _bossAwakeningLabel.text = _asyncGenerationBaseText + dots[i % dots.Length];
                i++;
                yield return wait;
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        // ─────────────────────────── Board delegates ───────────────────────────

        private void RebuildBoard()
        {
            if (_map?.Run != null) _map.Run.AutoPencilCleanup = PencilCleanupOn;   // OPT-GAME-002 for every placement path
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
                var panelRt = _sudokuPanel.GetComponent<RectTransform>();
                var infoText = panelRt?.Find("SudokuGameplayLevelInfo")?.GetComponent<Text>();
                if (infoText != null && run.State != null)
                {
                    // [REQ: REQ-UI-101] Localized header with the shared "7×7 3★" notation (GAP-019, GAP-029).
                    var cfg = run.CurrentLevelConfig;
                    infoText.text = LocalizationService.Format("InRun.LevelInfo",
                        "Floor {0}  Depth {1}  {2}×{2}  {3}★",
                        run.State.CurrentFloor + 1, run.State.Depth, board.Size, cfg?.Stars ?? 0);
                }

                ApplyPuzzleScrollFloor(panelRt, run.State);

                var hintText = panelRt?.Find("SudokuGameplayHint")?.GetComponent<Text>();
                if (hintText != null)
                    hintText.text = LocalizationService.Format("InRun.BoardHint",
                        "Use numpad buttons or keyboard 1-{0}.\nSingle click: select cell\nDouble click filled cell: highlight same numbers",
                        board.Size);
            }

            _numpadView.UpdateButtonStates(board.Size, board);
        }

        // Every board refresh goes through here, so locked cells, blurred pencil marks and pressure badges/strips stay
        // visible after a cell click or an item use (they used to vanish until the next placement).
        private void RenderBoard()
        {
            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null) return;
            BoardViewController.WhisperHiddenSums = RelicService.WhispersHiddenSums(run.State);   // [REQ: RELIC-KEY-002]
            _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight),
                _bagHighlightCells, _fogDisabledMovesRemaining,
                run.GetLockedCells(), _blurredPencilCells, BuildPressureRenderData()); // [REQ: DEBUFF-HOOK-013] pass locked cells to renderer
        }

        // [REQ: PRESSURE-UI-001] returns null for tutorial (RunNumber==0) — no overlays rendered
        // [REQ: PRESSURE-UI-002..009] populates PressureRenderData fields consumed by BoardViewController
        // Only the placement path plays the one-shot flashes (consumeOneShots); a plain refresh leaves them pending.
        private PressureRenderData? BuildPressureRenderData(bool consumeOneShots = false)
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

            if (!consumeOneShots)
                return new PressureRenderData
                {
                    ActiveThreats         = ls.ActiveThreats,
                    HauntedCell           = ls.HauntedCell,
                    CrumblingCells        = ls.CrumblingCells,
                    CrumblingMaxFragility = ls.CrumblingStartFragility
                };

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

            if (_pathViewport != null && _pathViewport.rect.width <= 10f) Canvas.ForceUpdateCanvases();
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


            var isFloorAdvance = _lastBuiltFloor >= 0 && run.State.CurrentFloor != _lastBuiltFloor;
            _lastBuiltFloor = run.State.CurrentFloor;
            ApplyPathFloorArt(run.State.CurrentFloor, isFloorAdvance);
            if (_pathPanel != null && _pathPanel.GetComponent<Image>() is { } panelImg)
                panelImg.color = new Color(0.04f, 0.06f, 0.05f, 0.90f);

            // Seasonal ambient particles — (re)initialize when floor changes.
            // Reinitialize() reuses the existing component to avoid DestroyImmediate mid-frame.
            if (_pathPanel != null && _ambientParticlesFloor != run.State.CurrentFloor)
            {
                _ambientParticlesFloor = run.State.CurrentFloor;
                var panelRt2 = _pathPanel.GetComponent<RectTransform>();
                if (panelRt2 != null)
                {
                    if (_ambientParticles == null)
                        _ambientParticles = _pathPanel.AddComponent<PathAmbientParticles>();
                    _ambientParticles.Reinitialize(panelRt2, run.State.CurrentFloor);
                }
            }

            // Suggestion 1: floor-themed lane names — situates the player inside the world
            var (calmLaneNameKey, riskLaneNameKey) = run.State.CurrentFloor switch
            {
                0 => ("InRun.Path.Lane.GardenWalk",  "InRun.Path.Lane.BriarCrossing"),
                1 => ("InRun.Path.Lane.KoiShore",    "InRun.Path.Lane.VinePath"),
                2 => ("InRun.Path.Lane.StoneBridge", "InRun.Path.Lane.DeepCurrent"),
                3 => ("InRun.Path.Lane.TempleWalk",  "InRun.Path.Lane.DemonTrail"),
                4 => ("InRun.Path.Lane.SummitPath",  "InRun.Path.Lane.AbyssEdge"),
                _ => ("InRun.Path.Lane.CalmPath",    "InRun.Path.Lane.RiskPath"),
            };

            RebuildPathChrome(run, T(calmLaneNameKey), T(riskLaneNameKey));

            var accentColor   = InRunUiFactory.AccentGold;
            var bossGateColor = GamePalette.BossGateNode;

            // Build set of actually-walked edges from NodePath sequence (A: adjacency fix)
            _walkedEdges.Clear();
            var walkedEdges = _walkedEdges;
            var npList = run.State.NodePath;
            if (npList != null)
                for (var k = 0; k + 1 < npList.Count; k++)
                    walkedEdges.Add((npList[k], npList[k + 1]));

            // All edges/dots/chevrons go into a shared container so we only need one
            // SetAsFirstSibling() call to place the whole group behind lane boxes and nodes.
            var edgesGoRoot = new GameObject("EdgesRoot", typeof(RectTransform));
            edgesGoRoot.transform.SetParent(_pathOverlayRoot, false);
            var edgesRoot = edgesGoRoot.GetComponent<RectTransform>();
            InRunUiFactory.StretchFill(edgesRoot);

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
                        DrawDottedEdge(edgesRoot, w, h, nodeI, nodeN,
                            new Color(PathInk.r, PathInk.g, PathInk.b, isWalked ? 0.95f : 0.70f));
                        continue;
                    }
                    // The colour follows the route; the Start's edges take the colour of the route they open.
                    var routeNode = nodeI.Type == NodeType.Start ? nodeN : nodeI;
                    var isCalm = routeNode.Route == RouteType.CalmRoute;
                    DrawEdge(edgesRoot, w, h, nodeI, nodeN, PathEdgeColor(isCalm, isWalked),
                        isCalm ? (isWalked ? 6f : 5f) : (isWalked ? 8f : 7f));
                }
            }
            // One reorder places the entire edge group behind lane boxes and node buttons.
            edgesGoRoot.transform.SetAsFirstSibling();

            // Build visit-order map from NodePath (node index → visit step, 1-based)
            _visitOrder.Clear();
            var visitOrder = _visitOrder;
            if (npList != null)
                for (var k = 0; k < npList.Count; k++)
                    if (!visitOrder.ContainsKey(npList[k]))
                        visitOrder[npList[k]] = k + 1;

            // Draw nodes
            // Collect reachable node buttons for controller navigation
            _gbReachableNodeIndices.Clear();
            _gbReachableNodeButtons.Clear();
            _gbPathCursor = 0;
            _allNodeButtons.Clear(); // F10: stagger-reveal collection (reused field, no allocation)

            for (var i = 0; i < graph.Count; i++)
            {
                var node      = graph[i];
                var pos       = PathNodePos(node, w, h);
                var isCurrent = i == run.State.CurrentNodeIndex;
                var canReach  = reachable != null && reachable.Contains(i);

                var isFirstPick = run.State.NodePath == null || run.State.NodePath.Count == 0;
                var isInvalidFirstPick = isFirstPick && !IsPuzzleNodeType(node.Type)
                    && node.Type != NodeType.Start;

                // [REQ: MAP-ROUTE-001] [REQ: MAP-ROUTE-002] Every tile type is visible from floor entry (SPEC-CONF-030).
                // Puzzle details (board size, stars) show for reachable and visited tiles; Survey Notes and the
                // Accurate Map relic (AllNodesRevealed) show them for every tile of the floor.
                var detailsHidden = !node.Visited && !canReach && !run.State.AllNodesRevealed;

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

                var nodeConfig = !detailsHidden && IsPuzzleNodeType(node.Type) ? _map.GetFixedLevelConfig(node) : null;

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
                _allNodeButtons.Add((btn, pos.x)); // F10

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
                // [REQ: MAP-CROSS-001] ⇄ badge on both cross-link tiles; [REQ: REQ-PATH-100] marker on the player's tile.
                if (node.IsCrossLink) AddCrossLinkBadge(btnRt);
                if (isCurrent) AddPlayerMarker(btnRt, run.State.ClassId);
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

                // Hover/select tooltip — NodeHoverHandler implements the pointer interfaces
                // directly, avoiding EventTrigger.Entry + lambda allocations per node.
                var hover = btn.gameObject.AddComponent<NodeHoverHandler>();
                hover.Setup(capturedNode, capturedConfig, isInvalidFirstPick, false,
                    _showNodeTooltipAction, _hideNodeTooltipAction, _showBlockedTooltipAction);

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

            // P1: set initial controller focus on first reachable node so D-pad navigation works immediately
            if (_gbReachableNodeButtons.Count > 0)
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    _gbReachableNodeButtons[0].gameObject);

            // #3 — Pulse nodes that became newly reachable since last refresh (skip on first pick — #8 handles it)
            for (var ni = 0; ni < _gbReachableNodeIndices.Count; ni++)
            {
                if (!isFirstPickPost && !_prevReachableIndices.Contains(_gbReachableNodeIndices[ni]))
                    StartCoroutine(AnimationHelper.PulseScale(
                        _gbReachableNodeButtons[ni].transform, 1f, 1.28f, 0.30f));
            }

            // F10 — Stagger-reveal: small scale pop on each node left-to-right after canvas rebuilds
            _allNodeButtons.Sort((a, b) => a.x.CompareTo(b.x));
            StartCoroutine(StaggerRevealNodes(_allNodeButtons));
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

            // Floor transition: fade the path canvas in each rebuild
            var pathCg = _pathOverlayRoot.GetComponent<CanvasGroup>();
            if (pathCg != null) StartCoroutine(AnimationHelper.FadeIn(pathCg, 0.22f));
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
                SetStatus(T("Cannot move there."));
                return;
            }

            // level is null for non-puzzle nodes (Shop, Rest, Relic, Cursed, Event, async Boss).
            // Guard here before any downstream code can accidentally access it.
            var hasPuzzleLevel = level != null;

            _pathMessage       = string.Empty;
            _completionHandled = false;
            _boardView.MarkOverlayDirty();
            _selectedRow    = -1;
            _selectedCol    = -1;
            _highlightValue = 0;
            foreach (var c in _unblurCoroutines) if (c != null) StopCoroutine(c);
            _unblurCoroutines.Clear();
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

            if (hasPuzzleLevel && _map.Run.IsAsyncLevelPending)
            {
                _pendingAsyncLevel = level;
                SetStatus(T("Creating puzzle..."));
                ShowPath();
                ShowBossAwakeningPanel("Creating Puzzle");
                return;
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
                        _map.StartPuzzleAsync(cfg);
                        _map.Run.OnCursedNodeAccepted();
                        _completionHandled = false; _boardView.MarkOverlayDirty();
                        _selectedRow = -1; _selectedCol = -1; _highlightValue = 0;
                        CancelPendingItemInteraction(false);
                        _bagHighlightCells.Clear();
                        _bagHighlightEndTime = 0f;
                        SetStatus(LocalizationService.Format(
                            "InRun.Status.CursedPuzzle",
                            "Cursed {0}\u00d7{0} {1}\u2605",
                            cfg.BoardSize,
                            cfg.Stars));
                        _pendingAsyncLevel = cfg;
                        ShowPath();
                        ShowBossAwakeningPanel("Creating Puzzle");
                    },
                    cfg =>
                    {
                        _map.StartPuzzleAsync(cfg);
                        _completionHandled = false; _boardView.MarkOverlayDirty();
                        _selectedRow = -1; _selectedCol = -1; _highlightValue = 0;
                        CancelPendingItemInteraction(false);
                        _bagHighlightCells.Clear();
                        _bagHighlightEndTime = 0f;
                        SetStatus(LocalizationService.Format(
                            "InRun.Status.PuzzleNode",
                            "{0}\u00d7{0} {1}\u2605",
                            cfg.BoardSize,
                            cfg.Stars));
                        _pendingAsyncLevel = cfg;
                        ShowPath();
                        ShowBossAwakeningPanel("Creating Puzzle");
                    });
                return;
            }

            if (!hasPuzzleLevel) { ShowPath(); return; }

            SetStatus(LocalizationService.Format(
                "InRun.Status.ArrivedPuzzle",
                "{0} - {1}×{1} {2}★",
                GetNodeLabel(arrivedNode.Type),
                level.BoardSize,
                level.Stars));
            ShowSudoku();
            RebuildBoard();
        }

        /// <summary>Slowly pulses the boss node transform until it is destroyed or goes inactive.</summary>
        private IEnumerator BossNodePulseLoop(Transform bossT)
        {
            while (bossT != null && bossT.gameObject != null
                   && bossT.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(AnimationHelper.PulseScale(bossT, 1f, 1.12f, 0.80f));
                yield return _bossPulseWait;
            }
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
            // P4: do not show tooltip after puzzle has been completed
            if (_completionHandled) return;
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
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
            Canvas.ForceUpdateCanvases();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRt, screenPos, null, out var local);
            var rt = _nodeTooltipPanel.GetComponent<RectTransform>();
            rt.anchoredPosition = ClampedTooltipPosition(local, panelRt, rt.sizeDelta.x, rt.sizeDelta.y, 12f);
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

        private Coroutine _smoothScrollCo;

        /// <summary>Smoothly scrolls the path viewport to center the given node index horizontally.</summary>
        private void ScrollToNode(int nodeIndex, float contentW, float viewW)
        {
            if (_pathScrollRect == null) return;
            var graph = _map?.Run?.CurrentFloorGraph;
            if (graph == null || nodeIndex < 0 || nodeIndex >= graph.Count) return;
            var scrollRange = contentW - viewW;
            var target = scrollRange <= 0f ? 0f
                : Mathf.Clamp01((PathNodePos(graph[nodeIndex], contentW, 1f).x - viewW * 0.5f) / scrollRange);
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

        // UI-STYLE-001: a visited tile keeps its faded icon and gets a vermilion tick, plus the step number of the walk.
        private void AddVisitedMark(RectTransform parent, RunNode node, int visitStep = 0)
        {
            ScrollUiKit.Fill(parent, "VisitedMark", new Vector2(0.58f, -0.08f), new Vector2(1.08f, 0.42f),
                PathVermilion, ScrollUiKit.Tick());
            if (visitStep >= 2)
                ScrollUiKit.Label(parent, "VisitSeq", visitStep.ToString(), 14, TextAnchor.UpperRight,
                    new Color(PathInk.r, PathInk.g, PathInk.b, 0.70f), new Vector2(0.60f, 0.70f), new Vector2(1.05f, 1.05f));
        }

        private static bool IsPuzzleNodeType(NodeType t) =>
            t == NodeType.Puzzle || t == NodeType.ElitePuzzle || t == NodeType.PreBoss || t == NodeType.Boss;

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
            NodeType.Start       => T("InRun.Path.Node.Start"),
            NodeType.Puzzle      => T("InRun.Path.Node.Puzzle"),
            NodeType.ElitePuzzle => T("InRun.Path.Node.Elite"),
            NodeType.Shop        => T("InRun.Path.Node.Shop"),
            NodeType.Rest        => T("InRun.Path.Node.Rest"),
            NodeType.Relic       => T("InRun.Path.Node.Relic"),
            NodeType.Event       => T("InRun.Path.Node.Event"),
            NodeType.PreBoss     => T("InRun.Path.Node.PreBoss"),
            NodeType.Boss        => T("InRun.Path.Node.BossGateShort"),
            NodeType.Cursed      => T("InRun.Path.Node.Cursed"),
            NodeType.CrossLink   => T("InRun.Path.Node.Bridge"),
            _                    => "?"
        };

        private static string GetNodeLabel(NodeType t) => t switch
        {
            NodeType.Start       => T("InRun.Path.Node.Start"),
            NodeType.Puzzle      => T("InRun.Path.Node.Puzzle"),
            NodeType.ElitePuzzle => T("InRun.Path.Node.Elite"),
            NodeType.Shop        => T("InRun.Path.Node.Shop"),
            NodeType.Rest        => T("InRun.Path.Node.Rest"),
            NodeType.Relic       => T("InRun.Path.Node.Relic"),
            NodeType.Event       => T("InRun.Path.Node.Event"),
            NodeType.PreBoss     => T("InRun.Path.Node.Elite"),
            NodeType.Boss        => T("InRun.Path.Node.BossGate"),
            NodeType.Cursed      => T("InRun.Path.Node.Cursed"),
            NodeType.CrossLink   => T("InRun.Path.Node.Bridge"),
            _                    => "?"
        };

        // ─────────────────────────── Input ───────────────────────────

        private void HandleKeyboard()
        {
            // OPT-2: F1 toggles in-game options panel from keyboard
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleOptionsPanel();
                _inputRemap.Tick();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExecuteInRunCancelRoute(CancelRouteContract.ResolveInRunKeyboardCancel(BuildInRunCancelContext()));
                return;
            }

            // PS-3: Enter/A confirms pending cell-select item interactions
            bool anyPendingInteraction = _koiReflectionPending || _templeIncensePending ||
                _patternScrollPending || _swapMode || _windChimePending;
            if (anyPendingInteraction && _selectedRow >= 0 && _selectedCol >= 0
                && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0))))
            {
                OnCellClicked(_selectedRow, _selectedCol);
                _inputRemap.Tick();
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
                    SetStatus(T("Items focus  (Tab to cycle, Enter to use, Esc or Tab back)"));
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
                        SetStatus(_highlightValue > 0 ? Tf("Highlighting {0}.", val) : T("Highlight cleared."));
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
            if (_rewardView != null && _rewardView.IsSwapOpen) { panel = _rewardView.ActivePanel; return panel != null; }
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
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)))
                _overlayBtnCache[Mathf.Clamp(curIdx, 0, _overlayBtnCache.Count - 1)].SafeInvoke("OverlayGamepad-A");

            // B (JoystickButton1) = invoke skip / abort / cancel button if one exists
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton1)))
            {
                Button skip = null;
                for (var k = _overlayBtnCache.Count - 1; k >= 0; k--)
                {
                    if (CancelRouteContract.IsOverlayCancelControlName(_overlayBtnCache[k].name))
                    { skip = _overlayBtnCache[k]; break; }
                }
                skip?.onClick.Invoke();
            }
        }

        private void HandleGamepadOptionsFocus()
        {
            if (_optionsFocusCache.Count == 0 || OptionsPageChanged()) RebuildOptionsFocusCache();
            if (_optionsFocusCache.Count == 0) return;

            var es = UnityEngine.EventSystems.EventSystem.current;
            var curGo = es?.currentSelectedGameObject;
            var curSel = curGo != null ? curGo.GetComponent<Selectable>() : null;
            var idx = curSel != null ? _optionsFocusCache.IndexOf(curSel) : 0;
            if (idx < 0) idx = 0;

            // Up/Down cycles elements; Left/Right adjusts sliders
            var moveDown = _inputRemap.WasActionPressed(InputAction.MoveDown);
            var moveUp   = _inputRemap.WasActionPressed(InputAction.MoveUp);
            if (moveDown) idx = (idx + 1) % _optionsFocusCache.Count;
            if (moveUp)   idx = (idx + _optionsFocusCache.Count - 1) % _optionsFocusCache.Count;
            if (moveDown || moveUp)
                es?.SetSelectedGameObject(_optionsFocusCache[idx].gameObject);

            var moveRight = _inputRemap.WasActionPressed(InputAction.MoveRight);
            var moveLeft  = _inputRemap.WasActionPressed(InputAction.MoveLeft);
            if ((moveRight || moveLeft) && _optionsFocusCache[idx] is Slider sl)
                sl.value = Mathf.Clamp(sl.value + (moveRight ? 0.1f : -0.1f) * (sl.maxValue - sl.minValue),
                    sl.minValue, sl.maxValue);

            // A: invoke button or flip toggle
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)))
            {
                var sel = _optionsFocusCache[idx];
                if (sel is Button btn) btn.onClick.Invoke();
                else if (sel is Toggle tgl) tgl.isOn = !tgl.isOn;
                else if (sel is Dropdown dd) dd.Show();
            }

            // B / Start: close options panel
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton1)) || Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton7)))
                ToggleOptionsPanel();
        }

        private void HandleGamepadTutorialFocus()
        {
            if (_basicsTutorial == null) return;
            var panel = _basicsTutorial.PanelRoot;
            if (panel == null || !panel.activeSelf) return;
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)))
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
            _inputRemap.Tick();

            // Start (JoystickButton7): open/close in-game options panel (save & quit lives inside)
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton7)))
            {
                ToggleOptionsPanel();
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
                HandleGamepadOptionsFocus();
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
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton4)))
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
                    SetStatus(T("[Controller] Puzzle mode  (LB to switch)"));
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
                SetStatus(Tf("[Controller] Node {0}/{1}  —  A to enter", _gbPathCursor + 1, _gbReachableNodeButtons.Count));
            }

            // LT / RT: free-scroll the path map left/right
            var lt = InputRemapService.ReadTrigger(left: true);
            var rt = InputRemapService.ReadTrigger(left: false);
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
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)) && _gbReachableNodeButtons.Count > 0)
            {
                var selBtn = _gbReachableNodeButtons[_gbPathCursor];
                if (selBtn != null && selBtn.interactable)
                    selBtn.onClick.Invoke();
            }

            // Start (JoystickButton7): open options panel from path screen
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton7)))
                ToggleOptionsPanel();

            // MAP-2: B (JoystickButton1) shows a quit-confirm modal instead of double-press
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton1)) && !_pathQuitModalActive)
                ExecuteInRunCancelRoute(CancelRouteContract.ResolveInRunGamepadCancel(BuildInRunCancelContext()));
        }

        private void HandleGamepadPuzzleFocus()
        {
            // Start (JoystickButton7): toggle options panel
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton7)))
            { ToggleOptionsPanel(); return; }

            // D-Pad + left stick: move cell selection (gamepad-only — keyboard arrows are handled below
            // in the direct keyboard section to avoid double-firing the same MoveSelection call).
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveUp))    MoveSelection(-1, 0);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveDown))  MoveSelection(1, 0);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveLeft))  MoveSelection(0, -1);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveRight)) MoveSelection(0, 1);

            // PS-5: A (JoystickButton0) exits game-over screen before placing digits
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0)))
            {
                if (_gameOverPanel != null && _gameOverPanel.activeSelf)
                { SaveAndQuit(); return; }
                // PS-3: A also confirms pending cell-select interactions
                bool anyPending = _koiReflectionPending || _templeIncensePending ||
                    _patternScrollPending || _swapMode || _windChimePending;
                if (anyPending && _selectedRow >= 0 && _selectedCol >= 0)
                { OnCellClicked(_selectedRow, _selectedCol); return; }
                // Normal: place digit at numpad cursor position
                _pendingKeyboardDigit = _gbNumpadRow * 3 + _gbNumpadCol + 1;
                _numpadView?.HighlightControllerCursor(_gbNumpadRow * 3 + _gbNumpadCol);
            }

            // B (JoystickButton1): immediate exit when defeat screen is shown (no long-press)
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton1)) && _gameOverPanel != null && _gameOverPanel.activeSelf)
            { SaveAndQuit(); return; }

            // PS-2: B (JoystickButton1) long-press (≥0.4 s) clears cell; short press acts as cancel only
            if (Input.GetKey(PadButtons.Map(KeyCode.JoystickButton1)))
                _bButtonHoldTime += Time.unscaledDeltaTime;
            else
                _bButtonHoldTime = 0f;
            if (_bButtonHoldTime >= 0.4f)
            {
                _bButtonHoldTime = -1f; // prevent re-trigger while still held
                ClearCell();
            }

            // X (JoystickButton2) or RB (JoystickButton5): toggle pencil mode
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton2)) || Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton5)))
                TogglePencilMode();

            // R3 (JoystickButton9): toggle highlight for value at selected cell
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton9)))
            {
                var board = _map?.Run?.CurrentBoard;
                if (board != null && _selectedRow >= 0 && _selectedCol >= 0)
                {
                    var val = board.Cells[_selectedRow, _selectedCol];
                    if (val > 0)
                    {
                        _highlightValue = (_highlightValue == val) ? 0 : val;
                        RenderBoard();
                        SetStatus(_highlightValue > 0 ? Tf("Highlighting {0}.", val) : T("Highlight cleared."));
                    }
                }
            }

            // Select/Back (JoystickButton6): immediate exit from game-over; hold ≥1.5 s to save & quit
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton6)) && _gameOverPanel != null && _gameOverPanel.activeSelf)
            {
                _selectButtonHoldTime = 0f;
                SaveAndQuit();
                return;
            }
            if (Input.GetKey(PadButtons.Map(KeyCode.JoystickButton6)))
                _selectButtonHoldTime += Time.unscaledDeltaTime;
            else
                _selectButtonHoldTime = 0f;
            if (_selectButtonHoldTime >= 1.5f)
            {
                _selectButtonHoldTime = -999f;
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
            if (InputRemapService.WasDpadPressed(InputAction.MoveUp)) _hudView?.ControllerSelectBagSlot(-1);
            if (InputRemapService.WasDpadPressed(InputAction.MoveDown)) _hudView?.ControllerSelectBagSlot(+1);

            var lt = InputRemapService.ReadTrigger(left: true);
            var rt = InputRemapService.ReadTrigger(left: false);
            if (lt > TriggerThresh && _triggerLPrev <= TriggerThresh) _hudView?.ControllerSelectBagSlot(-1);
            if (rt > TriggerThresh && _triggerRPrev <= TriggerThresh) _hudView?.ControllerSelectBagSlot(+1);
            _triggerLPrev = lt;
            _triggerRPrev = rt;

            // A: activate selected item
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton0))) _hudView?.ControllerActivateBagSlot();

            // PS-4: L3 (JoystickButton8) inspects the currently selected bag slot
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton8))) _hudView?.ControllerInspectBagSlot();
        }

        // MAP-2: Shows a 2-button modal (Quit / Stay) on the path panel canvas.
        private void ShowPathQuitConfirmModal()
        {
            if (_pathQuitModalActive) return;
            _pathQuitModalActive = true;

            var canvas = GetComponentInParent<Canvas>()?.rootCanvas ?? FindFirstObjectByType<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                _pathQuitModalActive = false;
                return;
            }
            var canvasRt = canvas.GetComponent<RectTransform>();

            var shell = InRunUiFactory.CreateThemedModalShell(canvasRt, "PathQuitModal",
                new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.65f), "bg_menu_pause", 0.12f);
            var modal = shell.Root;
            var modalRt = shell.Panel;

            var txtGo = new GameObject("Prompt", typeof(RectTransform));
            txtGo.transform.SetParent(modalRt, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.05f, 0.55f);
            txtRt.anchorMax = new Vector2(0.95f, 0.95f);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<UnityEngine.UI.Text>();
            txt.text = T("InRun.Path.QuitPrompt");
            txt.font = FontAssetService.GetFont();
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            Button MakeModalBtn(string name, string label, Vector2 aMin, Vector2 aMax, UiAction action)
            {
                var btn = InRunUiFactory.CreateActionButton(modalRt, name, aMin, aMax, label, action);
                var img = btn.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = new Color(0.18f, 0.14f, 0.09f, 1f);
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.98f, 0.83f, 0.26f, 0.30f);
                colors.selectedColor = colors.highlightedColor;
                btn.colors = colors;
                return btn;
            }

            var quitBtn = MakeModalBtn("BtnQuit", T("InRun.Path.Quit"), new Vector2(0.06f, 0.07f), new Vector2(0.46f, 0.50f), UiAction.SaveQuit);
            var stayBtn = MakeModalBtn("BtnStay", T("InRun.Path.Stay"), new Vector2(0.54f, 0.07f), new Vector2(0.94f, 0.50f), UiAction.Cancel);

            // D-pad nav between the two buttons
            var qNav = quitBtn.navigation;
            qNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
            qNav.selectOnRight = stayBtn;
            quitBtn.navigation = qNav;
            var sNav = stayBtn.navigation;
            sNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
            sNav.selectOnLeft = quitBtn;
            stayBtn.navigation = sNav;

            quitBtn.onClick.AddListener(() => { Destroy(modal); _pathQuitModalActive = false; SaveAndQuit(); });
            stayBtn.onClick.AddListener(() => { Destroy(modal); _pathQuitModalActive = false; });

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(stayBtn.gameObject);
        }

        // RW-3: invoke an event choice button by index
        /// <summary>
        /// Key 1–9 (index 0–8) on a scene over the path: chooses on the open rest, event, shop or reward; the same key
        /// again takes it. Returns false when no such scene is open, so the key reaches the board.
        /// </summary>
        public bool TryHandleSceneDigit(int index)
        {
            // The in-run Options sit on top of every scene; their keys win.
            if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf) return false;
            // The bag-swap layer is modal over the reward and the shop: its choice is made by click or controller.
            if (_rewardView != null && _rewardView.IsSwapOpen) return true;
            if (_rewardView != null && _rewardView.IsRestOpen) { _rewardView.SelectRestByIndex(index); return true; }
            var ep = _eventChoiceView?.ActivePanel;
            if (ep != null && ep.activeSelf) { _eventChoiceView.SelectChoiceByIndex(index); return true; }
            var sp = _shopView?.ActivePanel;
            if (sp != null && sp.activeSelf) { _shopView.SelectSlotByIndex(index); return true; }
            if (_rewardView != null && _rewardView.IsAwaitingReward) { _rewardView.SelectRewardByIndex(index); return true; }
            return false;
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
            if (showStatus) SetStatus(T("Cancelled."));
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
                // Other cage kinds show the label drawn on the board, so a hidden sum stays hidden.
                var title = c.CageType == KillerCageType.Sum
                    ? $"Killer Cage #{i + 1} (sum={c.Sum}, {c.Cells.Count} cells)"
                    : $"{BossService.GetModifierName(CageModifier(c.CageType))} #{i + 1} ({BoardViewController.CageLabel(c)}, {c.Cells.Count} cells)";
                list.Add((title, new List<CellCoord>(c.Cells)));
            }
            for (var i = 0; i < ov.Arrows.Count; i++)
            {
                var a = ov.Arrows[i];
                var cells = new List<CellCoord> { a.CircleCell };
                if (a.ArrowType == ArrowType.DoubleEndpoint || a.ArrowType == ArrowType.Pill) cells.Add(a.SecondCircle);
                cells.AddRange(a.ArrowCells);
                list.Add(($"Arrow #{i + 1} ({cells.Count} cells)", cells));
            }
            return list;
        }

        private static BossModifierId CageModifier(KillerCageType type) => type switch
        {
            KillerCageType.HiddenSum => BossModifierId.KillerHiddenSum,
            KillerCageType.Product => BossModifierId.CageProduct,
            KillerCageType.Difference => BossModifierId.CageDifference,
            KillerCageType.Ratio => BossModifierId.CageRatio,
            KillerCageType.RenbanSet => BossModifierId.RenbanCage,
            KillerCageType.MagicSquare => BossModifierId.MagicSquareRegion,
            _ => BossModifierId.KillerCages
        };

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
                LocalizationService.Format(
                    "InRun.PatternScroll.Title",
                    "Pattern Scroll  ({0} remaining)\nSelect a mechanic instance \u2014 Press ESC to cancel",
                    _patternScrollChargesLeft),
                11, TextAnchor.UpperCenter, GamePalette.AccentGold);
            titleGo.rectTransform.anchorMin = new Vector2(0.02f, 0.91f);
            titleGo.rectTransform.anchorMax = new Vector2(0.98f, 1.00f);
            titleGo.rectTransform.offsetMin = titleGo.rectTransform.offsetMax = Vector2.zero;
            titleGo.lineSpacing = 1.15f;

            var mechanics = _patternScrollMechanics;
            if (mechanics.Count == 0)
            {
                var none = InRunUiFactory.CreateText(overlay.transform, "None",
                    T("InRun.PatternScroll.Empty"), 11,
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
                InRunUiFactory.SetButtonIcon(mechBtns[i], "pattern_scroll", false, "items");
                if (_patternScrollSolvedIds.Contains(i))
                {
                    // Already resolved in this activation – show greyed out, non-interactive
                    mechBtns[i].interactable = false;
                    var dimg = mechBtns[i].GetComponent<Image>();
                    if (dimg != null) dimg.color = new Color(0.18f, 0.18f, 0.16f, 0.50f);
                }
            }

            // ── Confirm button (Confirmed state) – hidden until a mechanic is staged ──
            var confirmBtn = InRunUiFactory.CreateActionButton(overlay.transform, "BtnConfirmScroll",
                new Vector2(0.10f, 0.01f), new Vector2(0.90f, 0.08f), T("InRun.PatternScroll.ConfirmSelection"), UiAction.Confirm);
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
            SetStatus(Tf("Pattern Scroll: '{0}' staged. Click Confirm to apply — ESC cancels.", _patternScrollMechanics[index].label));
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
                SetStatus(Tf("Pattern Scroll: solved '{0}', filled {1} cell(s). Done.", label, filled));
            }
            else
            {
                // Back to Selecting with updated solved set
                SetStatus(Tf("Pattern Scroll: solved '{0}' (+{1} cells). {2} selection(s) left — Press ESC to cancel.", label, filled, _patternScrollChargesLeft));
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
                Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton3))) // Y button
                ToggleWindChimeHouseFlag(ref _windChimeFillRow);

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) ||
                Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton1))) // B button
                ToggleWindChimeHouseFlag(ref _windChimeFillCol);

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3) ||
                Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton2))) // X button
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
                SetStatus(Tf("Wind Chime: click a cell to fill its {0}. ESC cancels.", label));
            }
            else
            {
                SetStatus(Tf("Wind Chime: {0} {1} {2}  — select {3} more (1=Row 2=Col 3=Box). ESC cancels.", r, c, b, remain));
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
                RenderBoard();
                return;
            }
            _selectedRow = Mathf.Clamp(_selectedRow + dr, 0, board.Size - 1);
            _selectedCol = Mathf.Clamp(_selectedCol + dc, 0, board.Size - 1);
            RenderBoard();
        }

        private void OnCellClicked(int r, int c)
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) return;
            EnsureSpiritTrialsTimerStarted();

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
            RenderBoard();

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
            EnsureSpiritTrialsTimerStarted();
            if (_selectedRow < 0 || _selectedCol < 0)
            {
                if (!TryFindEditable(board, out _selectedRow, out _selectedCol)) return;
            }
            // [REQ: OPT-GAME-006] The input lock: ignore digits for the set time after a cell is chosen (none when off),
            // to prevent accidental placements on touch/fast keyboard usage.
            if (Time.realtimeSinceStartup - _cellSelectedTime < Gameplay.InputLockDelay) return;
            if (board.GivenMask[_selectedRow, _selectedCol]) { SetStatus(T("Given cell.")); return; }
            if (run.GetLockedCells().Contains((_selectedRow, _selectedCol))) { SetStatus(T("Cell is locked!")); return; } // [REQ: DEBUFF-HOOK-013] block digit input on locked cells
            if (value < 1 || value > board.Size) return;

            // Basics tutorial: lock input to the guided cell during step 6
            if (_basicsTutorial != null && _basicsTutorial.IsInputLockedToCell(_selectedRow, _selectedCol))
            { SetStatus(T("Try the highlighted cell!")); return; }

            if (_pencilMode)
            {
                if (board.Cells[_selectedRow, _selectedCol] != 0) { SetStatus(T("Clear cell first.")); return; }
                if (!run.TryAddPencilMark(_selectedRow, _selectedCol, value))
                    SetStatus(T("No pencil charges."));
                else
                {
                    SetStatus(Tf("Pencil: {0}", value));
                    run.GetAnalytics()?.RecordPencilPlaced();
                    _basicsTutorial?.OnPlayerAddedPencilMark(_selectedRow, _selectedCol, value);
                    if (SudokuRoguelike.Run.CurseService.IsActive(run.State, "blurred_sight")) // [REQ: CURSE-INT-009] blurred_sight: hide pencil marks 1.5 s after placement
                    {
                        var r = _selectedRow; var c = _selectedCol;
                        _blurredPencilCells.Add((r, c));
                        _unblurCoroutines.Add(StartCoroutine(UnblurPencilCellAfterDelay(r, c, 1.5f)));
                    }
                    InkStoneAfterPencil(value);   // [REQ: RELIC-SIG-002]
                }
                RenderBoard();
                return;
            }

            // During a pencil-interaction tutorial step, block final number placement.
            if (_basicsTutorial != null && _basicsTutorial.IsOnPencilStep)
            { SetStatus(T("Use pencil mode for this step!")); return; }

            if (!ConfirmDigitEntry(value)) return;   // OPT-GAME-005, after the tutorial guards
            var umbrellaChargesBefore = run.State.MistakeShieldCharges;
            // RunDirector rejects a wrong digit and applies its penalty (HP, shields, relics, debuffs).
            var result = run.PlaceNumber(_selectedRow, _selectedCol, value);
            // Notify basics tutorial of the placement
            _basicsTutorial?.OnPlayerPlacedNumber(_selectedRow, _selectedCol, value,
                result == PlaceResult.Correct);

            if (result == PlaceResult.Correct)
            {
                // GameDesignSpec 4.1: a placed digit highlights every cell with that digit, as clicking a filled cell does.
                _highlightValue = value;
                _audio?.PlayCorrectPlacement();
                _boardView?.PlayDigitPlaced(_selectedRow, _selectedCol);
                RumbleService.PulseCorrect(this);
                _map?.SaveNow();
                SetStatus(Tf("Placed {0}.", value));
                JumpToNextEmptyCellIfOn(board);   // OPT-GAME-004
            }
            else if (result == PlaceResult.Unchanged || result == PlaceResult.TakenBack)
                SetStatus(PlacementNote(result, value));
            else if (result == PlaceResult.IsGiven)
                SetStatus(T("Given cell."));
            else if (result == PlaceResult.IsPresolved)
                SetStatus(T("Locked cell."));
            else
            {
                _lastMistakeRow = _selectedRow;
                _lastMistakeCol = _selectedCol;
                if (HighlightErrorsOn) _boardView?.FlashCells(new HashSet<(int row, int col)> { (_selectedRow, _selectedCol) },
                    GamePalette.CellConflict, 0.45f);
                if (run.State.MistakeShieldCharges < umbrellaChargesBefore)
                    SetStatus(Tf("Umbrella blocked the penalty! ({0} left)", run.State.MistakeShieldCharges));
                else
                {
                    _audio?.PlayWrongPlacement();
                    _audio?.PlayHpLoss();
                    RumbleService.PulseMistake(this);
                    SetStatus(Tf("{0} is wrong here. HP: {1}", value, run.State.CurrentHP));
                    // [REQ: ACCESS-MOTION-001] [REQ: OPT-GFX-001] No shake with Reduce Motion or with Screen Shake off
                    if (ScreenShakeAllowed)
                        StartCoroutine(AnimationHelper.ScreenShake(
                            _sudokuPanel != null ? _sudokuPanel.transform : transform,
                            AnimationHelper.ShakeWrongPx, AnimationHelper.ShakeWrongDuration));
                }
                // [REQ: REQ-SAVE-101] Persist the HP loss right away, so quitting cannot undo a mistake (GAP-031).
                _map?.SaveNow();
            }
            _numpadView.UpdateButtonStates(board.Size, board);
            // [REQ: PRESSURE-UI-004,005,008,009] Trigger one-shot pressure animations then render with pressure data
            var pressure = BuildPressureRenderData(consumeOneShots: true);
            if (pressure.HasValue)
            {
                var pr = pressure.Value;
                if (pr.ExpiryFlashCells?.Count > 0)
                {
                    _boardView?.FlashCells(pr.ExpiryFlashCells, new Color(0.95f, 0.15f, 0.15f, 1f), 0.45f);
                    if (ScreenShakeAllowed)
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
            EnsureSpiritTrialsTimerStarted();
            if (board.GivenMask[_selectedRow, _selectedCol]) return;
            board.PlaceValue(_selectedRow, _selectedCol, 0);
            _highlightValue = 0;
            RenderBoard();
        }

        private void TogglePencilMode()
        {
            EnsureSpiritTrialsTimerStarted();
            _pencilMode = !_pencilMode;
            _numpadView.SetPencilMode(_pencilMode);
            _audio?.PlayPencilToggle();
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

            _pendingAsyncLevel = level;
            _pathMessage       = string.Empty;
            _completionHandled = false;
            _boardView.MarkOverlayDirty();
            _selectedRow    = -1;
            _selectedCol    = -1;
            _highlightValue = 0;
            CancelPendingItemInteraction(false);
            _bagHighlightCells.Clear();
            _bagHighlightEndTime = 0f;

            ShowBossAwakeningPanel("Boss Awakening");
        }

        // ─────────────────────────── Save / Resume ───────────────────────────

        private bool TrySaveAndQuit()
        {
            Time.timeScale = 1f;

            // Guard: if the puzzle panel is visible but the board hasn't finished loading yet
            // (async generation still in flight), saving would capture an incomplete state.
            var inPuzzle = _sudokuPanel != null && _sudokuPanel.activeSelf;
            var run = _map?.Run;
            var onEndScreen = _gameOverPanel != null && _gameOverPanel.activeSelf;
            var decision = InRunSaveQuitPolicy.Resolve(new InRunSaveQuitContext(
                inPuzzle,
                run?.CurrentBoard != null,
                run?.CurrentLevelState != null,
                onEndScreen,
                run?.State));

            if (decision.ShouldBlockReturn)
            {
                SetStatus(T(decision.BlockedStatusKey));
                Time.timeScale = 1f; // ensure timeScale stays unblocked
                return false;
            }

            if (decision.PersistenceAction == InRunSaveQuitPersistenceAction.SaveResumeState)
            {
                // Accumulate current puzzle's elapsed time into the run total before saving,
                // so the run timer persists correctly across Save & Quit / Resume cycles.
                var runState = run?.State;
                if (runState != null)
                    runState.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
                _map?.SaveNow();
            }
            else if (decision.PersistenceAction == InRunSaveQuitPersistenceAction.ClearNonProgressionAutosave)
            {
                var safeSave = new Save.SaveFileService(Save.SaveProfileService.ActiveSlot);
                var safeEnv = safeSave.Load();
                if (safeEnv.ActiveRunState != null && safeEnv.ActiveRunState.DisableProgressionRewards)
                {
                    safeEnv.ActiveRunState = null;
                    safeEnv.ActivePuzzle = null;
                    safeSave.Save(safeEnv);
                }
            }
            else if (decision.PersistenceAction == InRunSaveQuitPersistenceAction.ClearFinishedGardenRun)
            {
                // [BUG-FIX] Safety net: the end screen calls RecordRunAndGetNewUnlocks (async save) to
                // clear ActiveRunState. In rare edge cases (exception in PersistResult, quick close, etc.)
                // the save might not have committed. Force-clear here before returning to main menu so
                // the profile card never shows "(run in progress)" for a finished run.
                var safeSave = new Save.SaveFileService(Save.SaveProfileService.ActiveSlot);
                var safeEnv = safeSave.Load();
                if (safeEnv.ActiveRunState != null && !safeEnv.ActiveRunState.TutorialMode)
                {
                    safeEnv.ActiveRunState = null;
                    safeEnv.ActivePuzzle = null;
                    safeSave.Save(safeEnv);
                }
            }

            var bootstrap = FindFirstObjectByType<Bootstrap.GameBootstrap>();
            if (bootstrap != null) bootstrap.ReturnToMenu();

            // Navigate to the appropriate panel after returning to menu.
            var mc = FindFirstObjectByType<MainMenuController>();
            if (mc != null)
            {
                var runMode = _map?.Run?.State?.Mode;
                if (runMode == GameMode.SpiritTrials)
                    mc.ShowSpiritTrialsTierSelect();
                else if (runMode == GameMode.EndlessZen && (_gameOverPanel?.activeSelf ?? false))
                    mc.BackToMainMenu();
                else
                    mc.ShowMainMenu(); // refreshes the Resume button so saved GardenRuns are immediately resumable
            }

            // Seasonal challenge should land back on the Monthly Walk panel with refreshed PB info.
            if (decision.ShouldShowMonthlyWalkPanel)
                FindFirstObjectByType<MainMenuController>()?.ShowMonthlyWalkPanel();
            return true;
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
            SetStatus(T("Solve the remaining cells on your own!"));
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
            _floorEntryShownFor = s.CurrentFloor;   // UI-STYLE-021: a resumed floor was entered before
            _pathMessage = $"Resumed — Floor {s.CurrentFloor + 1}: {floorName}";
            ShowPath();
        }

        // ─────────────────────────── Helpers ───────────────────────────

        private static string T(string key) => LocalizationService.T(key);
        private static string Tf(string key, params object[] args) => string.Format(LocalizationService.T(key), args);

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
            if (OptionsController.LiveAccessibility.ScreenReaderEnabled)
                AccessibilityAnnouncementOverlay.Announce(msg);
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
            t.font              = FontAssetService.GetFont();
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
            if (_saveQuitLbl != null) _saveQuitLbl.text  = T("InRun.Path.SaveAndQuit");
            if (_saveQuitImg != null) _saveQuitImg.color = Color.white;
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

        // Single coroutine drives all node reveal pulses — avoids spawning N sub-coroutines
        // and N WaitForSeconds allocations.
        private System.Collections.IEnumerator StaggerRevealNodes(List<(Button btn, float x)> nodes)
        {
            const float Stagger = 0.04f; // seconds between each node starting its pop
            const float Dur     = 0.20f;
            const float Peak    = 1.06f;
            const float Half    = Dur * 0.5f;

            var count      = nodes.Count;
            if (count == 0 || AnimationHelper.MotionReduced) yield break;   // ACCESS-MOTION-002

            var startTimes = new float[count];
            for (var i = 0; i < count; i++) startTimes[i] = float.MaxValue; // unstarted

            var launched   = 0;
            var lastLaunch = Time.unscaledTime;

            while (true)
            {
                var now = Time.unscaledTime;

                // Launch the next node once the stagger interval has elapsed
                if (launched < count && (launched == 0 || now - lastLaunch >= Stagger))
                {
                    startTimes[launched] = now;
                    lastLaunch = now;
                    launched++;
                }

                var allDone = (launched == count); // starts true only once all are launched
                for (var i = 0; i < launched; i++)
                {
                    var btn = nodes[i].btn;
                    var age = now - startTimes[i];
                    if (age >= Dur)
                    {
                        if (btn != null) btn.transform.localScale = Vector3.one;
                        continue; // this node is settled — don't flip allDone
                    }
                    allDone = false; // at least one animation still running
                    if (btn == null) continue;
                    var s = age < Half
                        ? Mathf.Lerp(1f, Peak, age / Half)
                        : Mathf.Lerp(Peak, 1f, (age - Half) / Half);
                    btn.transform.localScale = new Vector3(s, s, 1f);
                }

                if (allDone) yield break;
                yield return null;
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
            if (_map?.Run?.CurrentBoard != null)
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

            // [REQ: DEBUFF-DEF-015] Item Block (RevealCost) blocks the whole use, not only the item's consumption.
            if (run.ItemUseBlockedMoves > 0)
            {
                SetStatus(Tf("Items are blocked for {0} more correct placement(s).", run.ItemUseBlockedMoves));
                return;
            }

            switch (item.Type)
            {
                // ── Ink Well: restore pencil marks ──
                case ItemType.InkWell:
                {
                    var amount = ItemService.GetInkWellAmount(item.Rarity);
                    s.CurrentPencil = Math.Min(s.MaxPencil, s.CurrentPencil + amount);
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Ink Well: restored {0} pencil marks.", amount));
                    break;
                }

                // ── Meditation Stone: restore HP ──
                case ItemType.MeditationStone:
                {
                    var amount = ItemService.GetMeditationStoneAmount(item.Rarity);
                    s.CurrentHP = Math.Min(s.MaxHP, s.CurrentHP + amount);
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Meditation Stone: restored {0} HP.", amount));
                    break;
                }

                // ── Offering Bowl: sacrifice HP for gold ──
                case ItemType.OfferingBowl:
                {
                    if (s.CurrentHP <= 1) { SetStatus(T("Not enough HP to sacrifice.")); return; }
                    s.CurrentHP -= 1;
                    s.GainGold(30);
                    run.TryUseItem(slotIndex);
                    SetStatus(T("Offering Bowl: -1 HP, +30 gold."));
                    break;
                }

                // ── Solver: fill selected cell (+neighbours) ──
                case ItemType.Solver:
                {
                    if (board == null || _selectedRow < 0) { SetStatus(T("Select a cell first.")); return; }
                    var neighbors = ItemService.GetSolverNeighborCount(item.Rarity);
                    var filled    = SolveCells(board, run, _selectedRow, _selectedCol, neighbors);
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Solver: filled {0} cell(s).", filled));
                    RenderBoard();
                    break;
                }

                // ── Finder: highlight cells matching selected digit in solution ──
                case ItemType.Finder:
                {
                    if (board == null || _highlightValue <= 0) { SetStatus(T("Select a number first.")); return; }
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
                    SetStatus(Tf("Finder: highlighted {0} cell(s) for {1}.", found, _highlightValue));
                    RenderBoard();
                    break;
                }

                // ── Pattern Scroll: player selects N distinct mechanic instances to solve ──
                case ItemType.PatternScroll:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
                    run.TryUseItem(slotIndex);
                    _patternScrollPending     = true;
                    _patternScrollChargesLeft = ItemService.GetPatternScrollZones(item.Rarity);
                    _patternScrollMechanics   = null;   // will be built fresh in ShowPatternScrollOverlay
                    _patternScrollSolvedIds   = null;
                    _patternScrollStagedIndex = -1;
                    SetStatus(Tf("Pattern Scroll: select {0} mechanic(s) to solve. Press ESC to cancel.", _patternScrollChargesLeft));
                    ShowPatternScrollOverlay();
                    break;
                }

                // ── Koi Reflection: player selects cells to reveal candidates for ──
                case ItemType.KoiReflection:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
                    var count = ItemService.GetKoiReflectionCells(item.Rarity);
                    run.TryUseItem(slotIndex);
                    _koiReflectionPending = true;
                    _koiReflectionChargesLeft = count;
                    SetStatus(Tf("Koi Reflection: click {0} cell(s) to reveal candidates.", count));
                    break;
                }

                // ── Lantern of Clarity: disable fog for N moves ──
                case ItemType.LanternOfClarity:
                {
                    var moves = ItemService.GetLanternOfClarityMoves(item.Rarity);
                    run.State.FogDisabledMoves += moves;
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Lantern of Clarity: fog disabled for {0} moves.", run.State.FogDisabledMoves));
                    if (board != null)
                        RenderBoard();
                    break;
                }

                // ── Garden Rake: clear pencil marks in selected row + column ──
                case ItemType.GardenRake:
                {
                    if (board == null || _selectedRow < 0) { SetStatus(T("Select a cell first.")); return; }
                    for (var i2 = 0; i2 < board.Size; i2++)
                    {
                        board.ClearPencilMarks(_selectedRow, i2);
                        board.ClearPencilMarks(i2, _selectedCol);
                    }
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Garden Rake: cleared row {0} and col {1}.", _selectedRow + 1, _selectedCol + 1));
                    RenderBoard();
                    break;
                }

                // ── Pruning Shears: remove one incorrect candidate from selected cell ──
                case ItemType.PruningShears:
                {
                    if (board == null || _selectedRow < 0) { SetStatus(T("Select a cell first.")); return; }
                    if (board.Cells[_selectedRow, _selectedCol] != 0) { SetStatus(T("Cell is already filled.")); return; }
                    var marks   = board.GetPencilMarks(_selectedRow, _selectedCol);
                    var correct = board.Solution[_selectedRow, _selectedCol];
                    var removed = false;
                    foreach (var v in marks)
                    {
                        if (v != correct) { board.RemovePencilMark(_selectedRow, _selectedCol, v); removed = true; break; }
                    }
                    if (!removed) { SetStatus(T("No incorrect candidates to prune.")); return; }
                    run.TryUseItem(slotIndex);
                    SetStatus(T("Pruning Shears: removed one incorrect candidate."));
                    RenderBoard();
                    break;
                }

                // ── Zen Sand Sifter: highlight cells with exactly two candidates ──
                case ItemType.ZenSandSifter:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
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
                    SetStatus(Tf("Zen Sand Sifter: {0} cells with exactly 2 candidates.", _bagHighlightCells.Count));
                    RenderBoard();
                    break;
                }

                // ── Ginkgo Leaf: undo last mistake placement ──
                case ItemType.GinkgoLeaf:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
                    if (s.LastMistakeRow < 0 && _lastMistakeRow >= 0)
                    {
                        s.LastMistakeRow = _lastMistakeRow;
                        s.LastMistakeCol = _lastMistakeCol;
                    }
                    var effect = run.ApplyItemEffect(item);
                    if (effect.Kind != ItemEffectResult.ResultKind.BoardChanged)
                    {
                        SetStatus(T(effect.Text));
                        return;
                    }
                    run.TryUseItem(slotIndex);
                    SetStatus(T(effect.Text));
                    _lastMistakeRow = -1;
                    _lastMistakeCol = -1;
                    RenderBoard();
                    break;
                }

                // ── Rice Paper Umbrella: block next 2 mistake penalties ──
                case ItemType.RicePaperUmbrella:
                {
                    run.State.MistakeShieldCharges += 2;
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Rice Paper Umbrella: next {0} mistake(s) won't cost HP.", run.State.MistakeShieldCharges));
                    break;
                }

                // ── Temple Incense: player clicks N cells to fill correct answer ──
                case ItemType.TempleIncense:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
                    var count = item.Rarity == ItemRarity.Epic ? 6 : item.Rarity == ItemRarity.Rare ? 4 : 2;
                    run.TryUseItem(slotIndex);
                    _templeIncensePending = true;
                    _templeIncenseChargesLeft = count;
                    SetStatus(Tf("Temple Incense: click {0} empty cell(s) to fill their correct answer.", count));
                    break;
                }

                // ── Koi Dragon Scale: fill the most-complete unit ──
                case ItemType.KoiDragonScale:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
                    var count = FillMostCompleteUnit(board, run);
                    if (count == 0) { SetStatus(T("No incomplete unit found.")); return; }
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Koi Dragon Scale: filled {0} cell(s) in the most-complete unit.", count));
                    RenderBoard();
                    break;
                }

                // ── Golden Kintsugi Jar: highlight all mistakes ──
                case ItemType.GoldenKintsugiJar:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
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
                    SetStatus(Tf("Golden Kintsugi Jar: found {0} mistake(s).", _bagHighlightCells.Count));
                    RenderBoard();
                    break;
                }

                // ── Silk Fan: generate Shield ──
                case ItemType.SilkFan:
                {
                    var amount = ItemService.GetSilkFanShieldAmount();
                    s.ShieldPoints += amount;
                    run.TryUseItem(slotIndex);
                    SetStatus(Tf("Silk Fan: +{0} Shield. Mistakes won't break your combo while shielded.", amount));
                    _hudView?.Refresh(s, run.CurrentLevelConfig);
                    break;
                }

                // ── Bamboo Scroll: reveal star ratings of reachable next nodes ──
                case ItemType.BambooScroll:
                {
                    var reachableIdx = run.GetReachableNodes();
                    if (reachableIdx.Count == 0) { SetStatus(T("Bamboo Scroll: no upcoming nodes found.")); break; }
                    var parts = new System.Text.StringBuilder("Bamboo Scroll — next nodes: ");
                    var graph = run.CurrentFloorGraph;
                    for (var i = 0; i < reachableIdx.Count; i++)
                    {
                        var node = graph[reachableIdx[i]];
                        var cfg = _map.GetFixedLevelConfig(node);
                        var stars = cfg != null ? cfg.Stars : 0;
                        if (i > 0) parts.Append(" | ");
                        parts.Append(stars > 0 ? new string('★', stars) : node.Type.ToString());
                    }
                    run.TryUseItem(slotIndex);
                    SetStatus(parts.ToString());
                    break;
                }

                // ── Garden Lantern: illuminate all fogged cells for 15 seconds ──
                case ItemType.GardenLantern:
                {
                    _gardenLanternUnfogUntil = Time.realtimeSinceStartup + 15f;
                    run.TryUseItem(slotIndex);
                    SetStatus(T("Garden Lantern: fog lifted for 15 seconds."));
                    RenderBoard();
                    break;
                }

                // ── Golden Koi: +10g per star at puzzle completion ──
                case ItemType.GoldenKoi:
                {
                    s.GoldenKoiActive = true;
                    run.TryUseItem(slotIndex);
                    var stars = run.CurrentLevelConfig?.Stars ?? 0;
                    SetStatus(Tf("Golden Koi: active — +{0}g when this puzzle is completed.", stars * 10));
                    break;
                }

                // ── Temple Stamp: lock HP this puzzle (mistakes cost 0 HP) ──
                case ItemType.TempleStamp:
                {
                    s.TempleSealActive = true;
                    run.TryUseItem(slotIndex);
                    SetStatus(T("Temple Stamp: HP locked this puzzle — mistakes deal no damage."));
                    break;
                }

                // ── Wind Chime: player selects which house(s) to fill ──
                case ItemType.WindChime:
                {
                    if (board == null) { SetStatus(T("No active board.")); return; }
                    run.TryUseItem(slotIndex);
                    _windChimeRarity = item.Rarity;
                    _windChimeFillRow = _windChimeFillCol = _windChimeFillBox = false;
                    if (item.Rarity == ItemRarity.Epic)
                    {
                        // Epic: all 3 houses — skip choice, go straight to cell-click
                        _windChimeFillRow = _windChimeFillCol = _windChimeFillBox = true;
                        _windChimePending = true;
                        SetStatus(T("Wind Chime: click any cell to fill its row + column + box. ESC cancels."));
                    }
                    else
                    {
                        _windChimeMaxHouses      = item.Rarity == ItemRarity.Rare ? 2 : 1;
                        _windChimeChoosingHouses = true;
                        SetStatus(Tf("Wind Chime: press 1=Row 2=Col 3=Box to select {0} house(s). ESC cancels.", _windChimeMaxHouses));
                    }
                    break;
                }

                case var _ when CounterItems.IsCounterItem(item.Type):   // ITEM-COUNTER-001..006 (InRunController.CounterItems.cs)
                    UseCounterItem(slotIndex, item);
                    break;

                default:
                    SetStatus(Tf("Used {0}.", ItemService.GetItemName(item.Type)));
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
                SetStatus(Tf("Koi Reflection: pick an empty cell ({0} remaining).", _koiReflectionChargesLeft));
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
                SetStatus(T("Koi Reflection: candidates revealed."));
            }
            else
            {
                SetStatus(Tf("Koi Reflection: candidates revealed ({0} remaining).", _koiReflectionChargesLeft));
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
            SetStatus(Tf("Wind Chime: filled {0} cell(s).", filled));
            RenderBoard();
        }

        private void HandleTempleIncenseClick(int r, int c)
        {
            var run   = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null) { _templeIncensePending = false; return; }
            if (board.GivenMask[r, c] || board.Cells[r, c] != 0)
            {
                SetStatus(Tf("Temple Incense: pick an empty cell ({0} remaining).", _templeIncenseChargesLeft));
                return;
            }
            run.PlaceNumber(r, c, board.Solution[r, c]);
            _templeIncenseChargesLeft--;
            if (_templeIncenseChargesLeft <= 0)
            {
                _templeIncensePending = false;
                _map?.SaveNow();
                SetStatus(T("Temple Incense: done."));
            }
            else
            {
                SetStatus(Tf("Temple Incense: filled ({0} remaining).", _templeIncenseChargesLeft));
            }
            RenderBoard();
        }

        private void HandleSwapClick(int r, int c)
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) { _swapMode = false; return; }
            if (board.GivenMask[r, c]) { SetStatus(T("Can't swap a given cell. Click another.")); return; }

            if (_swapRow1 < 0)
            {
                _swapRow1 = r; _swapCol1 = c;
                SetStatus(Tf("Silk Fan: first cell ({0},{1}) selected. Click second cell.", r + 1, c + 1));
            }
            else
            {
                if (r == _swapRow1 && c == _swapCol1) { SetStatus(T("Silk Fan: same cell. Pick a different one.")); return; }
                var val1 = board.Cells[_swapRow1, _swapCol1];
                var val2 = board.Cells[r, c];
                board.PlaceValue(_swapRow1, _swapCol1, val2);
                board.PlaceValue(r, c, val1);
                _swapMode = false;
                _swapRow1 = -1; _swapCol1 = -1;
                SetStatus(T("Silk Fan: swapped cells."));
                RenderBoard();
            }
        }

        private void OnDestroy()
        {
            foreach (var btn in _overlayBtnCache) btn?.onClick.RemoveAllListeners();
            _overlayBtnCache.Clear();
            foreach (var btn in _gbReachableNodeButtons) btn?.onClick.RemoveAllListeners();
            _gbReachableNodeButtons.Clear();
            foreach (var entry in _allNodeButtons) entry.btn?.onClick.RemoveAllListeners();
            _allNodeButtons.Clear();
        }
    }
}
