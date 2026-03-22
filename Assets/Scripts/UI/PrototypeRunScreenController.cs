using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SudokuRoguelike.UI
{
    public sealed class PrototypeRunScreenController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private GameObject pathOverviewPanel;
        [SerializeField] private GameObject sudokuPanel;
        [SerializeField] private Text pathOverviewText;
        [SerializeField] private Text laneAText;
        [SerializeField] private Text laneBText;
        [SerializeField] private RectTransform laneAPathRoot;
        [SerializeField] private RectTransform laneBPathRoot;
        [SerializeField] private Button choosePathAButton;
        [SerializeField] private Button choosePathBButton;
        [SerializeField] private Button saveQuitPathButton;
        [SerializeField] private Button saveQuitSudokuButton;
        [SerializeField] private RectTransform sudokuGridRoot;
        [SerializeField] private RectTransform numpadRoot;
        [SerializeField] private Button solveSudokuButton;
        [SerializeField] private Text sudokuStatusText;
        [SerializeField] private Text hpText;
        [SerializeField] private Text pencilText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverSummaryText;
        [SerializeField] private Text gameOverDetailsText;
        [SerializeField] private Button gameOverBackToMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private Button _optionsSudokuButton;
        private GameObject _inGameOptionsPanel;

        private readonly List<CellView> _cells = new();
        private readonly List<Button> _numpadButtons = new();

        private int _boardSize;
        private int _selectedRow = -1;
        private int _selectedCol = -1;
        private readonly HashSet<long> _multiSelectedCells = new();
        private int _highlightValue;
        private int _lastClickRow = -1;
        private int _lastClickCol = -1;
        private float _lastClickTime;
        private bool _completionHandled;
        private float _nextPathRefreshTime;
        private bool _buttonIconsApplied;
        private bool _fallbackRunInitAttempted;
        private bool _tutorialSudokuShown;
        private int _lastLaneRenderSignature = int.MinValue;
        private bool _gameOverShown;
        private bool _tutorialCompletionProcessed;
        private string _pathOverlayMessage = string.Empty;
        private bool _resumeScreenApplied;
        private bool _pencilMode;
        private Button _pencilModeButton;
        private RunAudioController _runAudio;
        private string _hoverInfo = string.Empty;
        private RectTransform _inventoryBadgeRoot;
        // Old _classToken/_classTokenTarget removed — replaced by PlayerIconController
        // _hasClassTokenTarget removed — replaced by PlayerIconController
        private GameObject _rewardPanel;
        private Text _rewardSummaryText;
        private Text _rewardHoverText;
        private readonly List<ItemRollSlot> _pendingRewardSlots = new();
        private bool _awaitingRewardChoice;
        private GameObject _shopPanel;
        private Text _shopSummaryText;
        private Text _shopHoverText;
        private readonly List<ShopOffer> _shopOffers = new();
        private string _pendingShopOfferId = string.Empty;
        private bool _awaitingShopReplacement;
        private RectTransform _puzzleItemBarRoot;
        private Text _puzzleItemHoverText;
        private Text _levelInfoText;
        private Text _modifiersLabel;
        private Image _hpBarFill;
        private Image _pencilBarFill;
        private RectTransform _pathOverlayRoot;
        // Old lane-end/cross-link tracking fields removed — replaced by canvas coordinate system
        private int _lastPuzzleItemSignature = int.MinValue;
        private readonly List<(int Row, int Col)> _finderHighlightCells = new();
        private float _finderHighlightUntil;
        private GameObject _bossGateChoicePanel;
        private bool _awaitingBossGateChoice;
        private bool _awaitingRelicChoice;
        private GameObject _relicChoicePanel;
        private RelicInstance _offeredRelic;
        private bool _awaitingRewardReplacement;
        private int _pendingRewardSlotIndex;
        private RectTransform _gridOverlayRoot;
        private RectTransform _gridNumberRoot;
        private readonly List<GameObject> _overlayObjects = new();
        private readonly Dictionary<BossModifierId, List<GameObject>> _overlayGroups = new();
        private BossModifierId _currentOverlayModifier;
        private bool _overlaysBuilt;
        private readonly HashSet<long> _cageBorderEdges = new();
        private Sprite _circleSprite;
        private Text _bossModifierDescText;
        private GameObject _legendPanel;
        private bool _legendExpanded;
        private Coroutine _isolateCoroutine;

        private const string ReturnTutorialProgressPrefKey = "sr_return_to_tutorial_progress";

        private static readonly Color EmptyColor = new(0.12f, 0.18f, 0.14f, 1f);
        private static readonly Color GivenColor = new(0.20f, 0.29f, 0.20f, 1f);
        private static readonly Color RowColHighlight = new(0.18f, 0.34f, 0.56f, 1f);
        private static readonly Color SelectedColor = new(0.73f, 0.49f, 0.18f, 1f);
        private static readonly Color MatchValueColor = new(0.36f, 0.24f, 0.58f, 1f);
        private static readonly Color FinderHintColor = new(0.22f, 0.45f, 0.30f, 1f);
        private static readonly Color ConflictColor = new(0.72f, 0.18f, 0.18f, 1f);
        private static readonly Color FogColor = new(0.06f, 0.06f, 0.08f, 1f);
        private static readonly Color GermanWhispersLineColor = new(0.20f, 0.72f, 0.30f, 0.55f);
        private static readonly Color DutchWhispersLineColor = new(0.10f, 0.95f, 0.78f, 0.75f); // bright teal
        private static readonly Color ParityLineColor = new(0.10f, 0.88f, 0.80f, 0.70f);
        private static readonly Color RenbanLineColor = new(0.80f, 0.35f, 0.65f, 0.55f);
        private static readonly Color PalindromeLineColor = new(0.60f, 0.60f, 0.60f, 0.65f); // gray
        private static readonly Color ThermoLineColor = new(0.85f, 0.45f, 0.10f, 0.70f);    // warm orange
        private static readonly Color BetweenLinesColor = new(0.90f, 0.90f, 0.90f, 0.60f); // near-white
        private static readonly Color EvenMarkerColor = new(0.35f, 0.65f, 0.90f, 0.55f);   // light blue square
        private static readonly Color OddMarkerColor = new(0.90f, 0.55f, 0.20f, 0.55f);    // warm orange circle
        private static readonly Color KillerCageBorder = new(0.85f, 0.15f, 0.12f, 0.90f);
        private static readonly Color WhiteDotColor = new(0.95f, 0.95f, 0.95f, 0.85f);
        private static readonly Color BlackDotColor = new(0.10f, 0.10f, 0.10f, 0.90f);
        private static readonly Color ArrowCircleColor = new(0.75f, 0.75f, 0.75f, 0.90f);
        private static readonly Color ArrowCircleInnerColor = new(0f, 0f, 0f, 0f); // transparent → true hollow outline
        private static readonly Color ArrowPathColor = new(0.55f, 0.55f, 0.55f, 0.45f);
        private static readonly Color KnightMoveHighlight = new(0.32f, 0.22f, 0.48f, 0.55f);

        private static readonly (int Dr, int Dc)[] KnightOffsets =
        {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2),  (1, 2),  (2, -1),  (2, 1)
        };

        private AccessibilityService _accessibility;

        // High contrast overrides
        private static readonly Color HcBackground = new(1f, 1f, 1f, 1f);
        private static readonly Color HcBorder = new(0f, 0f, 0f, 1f);
        private static readonly Color HcSelected = new(1f, 1f, 0f, 1f);
        private static readonly Color HcError = new(1f, 0f, 0f, 1f);
        private static readonly Color HcFog = new(0f, 0f, 0f, 1f);
        private static readonly Color HcPanelBg = new(0.02f, 0.02f, 0.04f, 0.95f);
        private static readonly Color HcPanelText = new(1f, 1f, 1f, 1f);

        private bool _highlightConflicts = true;

        public void SetHighlightConflictsLive(bool enabled)
        {
            _highlightConflicts = enabled;
            var board = runMapController?.Run?.CurrentBoard;
            if (board != null)
            {
                RenderBoard(board);
            }
        }

        public void Configure(
            RunMapController runMap,
            GameObject pathPanel,
            GameObject sudokuGamePanel,
            Text overviewText,
            Text laneA,
            Text laneB,
            RectTransform laneARoot,
            RectTransform laneBRoot,
            Button chooseA,
            Button chooseB,
            Button saveQuitPath,
            Button saveQuitSudoku,
            Button optionsSudoku,
            GameObject inGameOptions,
            RectTransform gridRoot,
            RectTransform numpad,
            Button solveButton,
            Text statusText,
            Text hp,
            Text pencil,
            GameObject gameOver,
            Text gameOverSummary,
            Text gameOverDetails,
            Button gameOverBack)
        {
            runMapController = runMap;
            pathOverviewPanel = pathPanel;
            sudokuPanel = sudokuGamePanel;
            pathOverviewText = overviewText;
            laneAText = laneA;
            laneBText = laneB;
            laneAPathRoot = laneARoot;
            laneBPathRoot = laneBRoot;
            choosePathAButton = chooseA;
            choosePathBButton = chooseB;
            saveQuitPathButton = saveQuitPath;
            saveQuitSudokuButton = saveQuitSudoku;
            _optionsSudokuButton = optionsSudoku;
            _inGameOptionsPanel = inGameOptions;
            sudokuGridRoot = gridRoot;
            numpadRoot = numpad;
            solveSudokuButton = solveButton;
            sudokuStatusText = statusText;
            hpText = hp;
            pencilText = pencil;
            gameOverPanel = gameOver;
            gameOverSummaryText = gameOverSummary;
            gameOverDetailsText = gameOverDetails;
            gameOverBackToMenuButton = gameOverBack;

            WireButtons();
            BuildNumpad();
            SquarePathActionButtons();
            ShowPathOverview();
            RefreshPathOverview();
        }

        private void Awake()
        {
            WireButtons();
            _accessibility = new AccessibilityService();
            _runAudio = GetComponent<RunAudioController>();
            if (_runAudio == null)
            {
                _runAudio = gameObject.AddComponent<RunAudioController>();
            }

            var save = new SaveFileService();
            var profile = new ProfileService();
            if (save.TryLoadProfile(out var envelope))
            {
                profile.ApplyEnvelope(envelope);
            }

            _highlightConflicts = profile.Options.Gameplay.HighlightConflicts;
        }

        private void Update()
        {
            if (runMapController == null)
            {
                runMapController = FindFirstObjectByType<RunMapController>();
                if (runMapController == null)
                {
                    return;
                }
            }

            if (runMapController.Run == null && !_fallbackRunInitAttempted)
            {
                _fallbackRunInitAttempted = true;
                runMapController.Initialize(ClassId.NumberFreak, new MetaProgressionState());
            }

            if (pathOverviewPanel != null && pathOverviewPanel.activeSelf && Time.unscaledTime >= _nextPathRefreshTime)
            {
                _nextPathRefreshTime = Time.unscaledTime + 0.50f;
                RefreshPathOverview();
            }

            if (pathOverviewPanel != null && pathOverviewPanel.activeSelf)
            {
                _playerIcon.Update();
            }

            if (!_buttonIconsApplied)
            {
                TryApplyButtonIcons();
            }

            if (!_tutorialSudokuShown && runMapController?.Run?.RunState != null && runMapController.Run.RunState.TutorialMode)
            {
                _tutorialSudokuShown = true;
                ShowSudoku();
                BuildOrRefreshSudokuBoard();
                SetStatus("Tutorial puzzle started.");
            }

            if (sudokuPanel != null && sudokuPanel.activeSelf)
            {
                _runAudio?.SetContext(RunAudioController.Context.Puzzle);
            }

            if (!_resumeScreenApplied)
            {
                ApplyResumeScreenState();
            }

            if (sudokuPanel != null && sudokuPanel.activeSelf)
            {
                HandleKeyboardInput();
                HandleCompletionState();
                RefreshHud();
                RefreshSolveButtonState();
                CheckForGameOver();
            }
        }

        private void WireButtons()
        {
            if (choosePathAButton != null)
            {
                choosePathAButton.gameObject.SetActive(false);
            }

            if (choosePathBButton != null)
            {
                choosePathBButton.gameObject.SetActive(false);
            }

            if (saveQuitPathButton != null)
            {
                saveQuitPathButton.onClick.RemoveAllListeners();
                saveQuitPathButton.onClick.AddListener(SaveAndQuit);
            }

            if (saveQuitSudokuButton != null)
            {
                saveQuitSudokuButton.onClick.RemoveAllListeners();
                saveQuitSudokuButton.onClick.AddListener(SaveAndQuit);
            }

            if (_optionsSudokuButton != null)
            {
                _optionsSudokuButton.onClick.RemoveAllListeners();
                _optionsSudokuButton.onClick.AddListener(() =>
                {
                    if (_inGameOptionsPanel != null) _inGameOptionsPanel.SetActive(true);
                });
            }

            if (solveSudokuButton != null)
            {
                solveSudokuButton.onClick.RemoveAllListeners();
                solveSudokuButton.onClick.AddListener(EvaluateCurrentSudoku);
                solveSudokuButton.gameObject.SetActive(false);
            }

            if (gameOverBackToMenuButton != null)
            {
                gameOverBackToMenuButton.onClick.RemoveAllListeners();
                gameOverBackToMenuButton.onClick.AddListener(SaveAndQuit);
            }
        }

        private static bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current != null &&
                   UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private void HandleKeyboardInput()
        {
            // Escape closes the top-most active panel
            if (WasEscapePressed())
            {
                if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf)
                {
                    _inGameOptionsPanel.SetActive(false);
                    return;
                }
                if (_shopPanel != null && _shopPanel.activeSelf)
                {
                    _shopPanel.SetActive(false);
                    ShowPathOverview();
                    return;
                }
                if (_rewardPanel != null && _rewardPanel.activeSelf)
                {
                    // Don't allow closing reward panel (must choose)
                }
                if (_bossGateChoicePanel != null && _bossGateChoicePanel.activeSelf)
                {
                    // Don't allow closing boss gate (must choose)
                }
            }

            var boardSize = runMapController?.Run?.CurrentBoard?.Size ?? 9;

            var moveRow = 0;
            var moveCol = 0;
            if (WasMoveUpPressed()) moveRow -= 1;
            if (WasMoveDownPressed()) moveRow += 1;
            if (WasMoveLeftPressed()) moveCol -= 1;
            if (WasMoveRightPressed()) moveCol += 1;
            if (moveRow != 0 || moveCol != 0)
            {
                MoveSelection(moveRow, moveCol);
            }

            for (var i = 1; i <= 9; i++)
            {
                if (i > boardSize)
                {
                    continue;
                }

                if (WasDigitPressed(i))
                {
                    EnterNumber(i);
                }
            }

            if (WasClearPressed())
            {
                ClearSelectedCell();
            }

            if (WasSaveQuitPressed())
            {
                SaveAndQuit();
            }

            if (WasTogglePencilModePressed())
            {
                TogglePencilMode();
            }

            // Multi-select shortcuts
            HandleMultiSelectShortcuts();
        }

        private void HandleMultiSelectShortcuts()
        {
            var board = runMapController?.Run?.CurrentBoard;
            if (board == null) return;

            var ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (!ctrlHeld) return;

            if (shiftHeld && Input.GetKeyDown(KeyCode.A))
            {
                // CTRL+SHIFT+A: deselect all
                _multiSelectedCells.Clear();
                _selectedRow = -1;
                _selectedCol = -1;
                RenderBoard(board);
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                // CTRL+A: select all empty cells
                _multiSelectedCells.Clear();
                for (var r = 0; r < board.Size; r++)
                {
                    for (var c = 0; c < board.Size; c++)
                    {
                        if (board.IsEmpty(r, c) && !board.IsGiven(r, c))
                            _multiSelectedCells.Add((long)r * 1000 + c);
                    }
                }
                RenderBoard(board);
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                // CTRL+I: invert selection
                var newSelection = new HashSet<long>();
                for (var r = 0; r < board.Size; r++)
                {
                    for (var c = 0; c < board.Size; c++)
                    {
                        if (board.IsGiven(r, c)) continue;
                        var key = (long)r * 1000 + c;
                        if (!_multiSelectedCells.Contains(key))
                            newSelection.Add(key);
                    }
                }
                _multiSelectedCells.Clear();
                foreach (var k in newSelection)
                    _multiSelectedCells.Add(k);
                RenderBoard(board);
            }
        }

        /// <summary>Check if a cell is in the multi-selection set.</summary>
        private bool IsCellMultiSelected(int row, int col)
        {
            return _multiSelectedCells.Contains((long)row * 1000 + col);
        }

        private void MoveSelection(int deltaRow, int deltaCol)
        {
            var board = runMapController?.Run?.CurrentBoard;
            if (board == null)
            {
                return;
            }

            if (_selectedRow < 0 || _selectedCol < 0)
            {
                if (TryFindFirstEditableCell(board, out var startRow, out var startCol))
                {
                    _selectedRow = startRow;
                    _selectedCol = startCol;
                    RenderBoard(board);
                }

                return;
            }

            _selectedRow = Mathf.Clamp(_selectedRow + deltaRow, 0, board.Size - 1);
            _selectedCol = Mathf.Clamp(_selectedCol + deltaCol, 0, board.Size - 1);
            RenderBoard(board);
        }

        private static bool WasDigitPressed(int value)
        {
            if (value < 1 || value > 9)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            return value switch
            {
                1 => Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame,
                2 => Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame,
                3 => Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame,
                4 => Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame,
                5 => Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame,
                6 => Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame,
                7 => Keyboard.current.digit7Key.wasPressedThisFrame || Keyboard.current.numpad7Key.wasPressedThisFrame,
                8 => Keyboard.current.digit8Key.wasPressedThisFrame || Keyboard.current.numpad8Key.wasPressedThisFrame,
                9 => Keyboard.current.digit9Key.wasPressedThisFrame || Keyboard.current.numpad9Key.wasPressedThisFrame,
                _ => false
            };
#else
            return Input.GetKeyDown(KeyCode.Alpha0 + value) || Input.GetKeyDown(KeyCode.Keypad0 + value);
#endif
        }

        private static bool WasSaveQuitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }

    private static bool WasMoveUpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
    }

    private static bool WasMoveDownPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
    }

    private static bool WasMoveLeftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#endif
    }

    private static bool WasMoveRightPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
    }

    private static bool WasTogglePencilModePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
        return false;
        }

        return Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.rightCtrlKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
#endif
    }

        private static bool WasClearPressed()
        {
    #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
            return false;
            }

            return Keyboard.current.backspaceKey.wasPressedThisFrame ||
               Keyboard.current.deleteKey.wasPressedThisFrame ||
               Keyboard.current.numpad0Key.wasPressedThisFrame;
    #else
            return Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Keypad0);
    #endif
        }

        private void HandleCompletionState()
        {
            var levelState = runMapController?.Run?.CurrentLevelState;
            if (levelState == null)
            {
                return;
            }

            if (!levelState.PuzzleComplete)
            {
                _completionHandled = false;
                return;
            }

            if (_completionHandled)
            {
                return;
            }

            _completionHandled = true;
            if (runMapController?.Run?.RunState != null && runMapController.Run.RunState.TutorialMode)
            {
                SetStatus("Tutorial Sudoku solved.");
                TryCompleteTutorialAndReturn();
                return;
            }

            // PreBoss requires 2 puzzles solved sequentially.
            var runState = runMapController?.Run?.RunState;
            var currentNode = GetCurrentNode();
            if (currentNode != null && currentNode.Type == NodeType.PreBoss && runState != null && runState.PreBossPuzzlesCompleted < 1)
            {
                runState.PreBossPuzzlesCompleted++;
                SetStatus($"Pre-Boss puzzle {runState.PreBossPuzzlesCompleted}/2 complete. Starting next puzzle...");
                StartNextPreBossPuzzle();
                return;
            }

            if (currentNode != null && currentNode.Type == NodeType.PreBoss && runState != null)
            {
                runState.PreBossPuzzlesCompleted = 0;
            }

            ShowRewardScreen();
        }

        private RunNode GetCurrentNode()
        {
            var run = runMapController?.Run;
            if (run?.CurrentRunGraph == null || run.RunState == null) return null;
            var idx = run.RunState.CurrentNodeIndex;
            if (idx < 0 || idx >= run.CurrentRunGraph.Count) return null;
            return run.CurrentRunGraph[idx];
        }

        private void StartNextPreBossPuzzle()
        {
            var run = runMapController?.Run;
            if (run == null) return;

            // Build a new level config for the second pre-boss puzzle (slightly harder).
            var node = GetCurrentNode();
            if (node == null) return;

            var config = runMapController.GetFixedLevelConfig(node);
            if (config == null) return;

            config.Difficulty = (DifficultyTier)Mathf.Min((int)config.Difficulty + 1, (int)DifficultyTier.Diff5);
            config.Stars = Mathf.Min(5, config.Stars + 1);

            run.StartLevel(config);
            _completionHandled = false;
            _selectedRow = -1;
            _selectedCol = -1;
            _highlightValue = 0;
            BuildOrRefreshSudokuBoard();
            RefreshHud();
        }

        private void ShowPathOverview()
        {
            if (runMapController?.Run?.RunState != null && runMapController.Run.RunState.TutorialMode)
            {
                ShowSudoku();
                return;
            }

            if (pathOverviewPanel != null)
            {
                pathOverviewPanel.SetActive(true);
            }

            if (sudokuPanel != null)
            {
                sudokuPanel.SetActive(false);
            }

            if (_rewardPanel != null && !_awaitingRewardChoice)
            {
                _rewardPanel.SetActive(false);
            }

            UpdateQuitButtonLabels();

            _runAudio?.SetContext(RunAudioController.Context.Path);
        }

        private void ShowSudoku()
        {
            if (pathOverviewPanel != null)
            {
                pathOverviewPanel.SetActive(false);
            }

            if (sudokuPanel != null)
            {
                sudokuPanel.SetActive(true);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            HideRewardPanel();

            if (_shopPanel != null)
            {
                _shopPanel.SetActive(false);
            }

            UpdateQuitButtonLabels();

            _runAudio?.SetContext(RunAudioController.Context.Puzzle);
        }

        private void ChoosePath(bool risk)
        {
            if (_shopPanel != null && _shopPanel.activeSelf)
            {
                SetStatus("Resolve shop choice first.");
                return;
            }

            if (_awaitingRewardChoice)
            {
                SetStatus("Choose a reward first.");
                return;
            }

            if (_awaitingBossGateChoice)
            {
                SetStatus("Choose a boss modifier first.");
                return;
            }

            if (_awaitingRelicChoice)
            {
                SetStatus("Choose a relic first.");
                return;
            }

            if (runMapController == null)
            {
                runMapController = FindFirstObjectByType<RunMapController>();
            }

            if (runMapController == null)
            {
                SetStatus("RunMapController missing.");
                return;
            }

            if (!runMapController.TryAdvancePathAndStartNextPuzzle(risk, out var node, out var level, out var failureReason))
            {
                SetStatus(string.IsNullOrWhiteSpace(failureReason) ? "Path is unavailable." : failureReason);
                RefreshPathOverview();
                return;
            }

            _runAudio?.PlayPathAdvance();

            _selectedRow = -1;
            _selectedCol = -1;
            _highlightValue = 0;
            _completionHandled = false;
            _gameOverShown = false;

            if (node.Type == NodeType.Shop)
            {
                HandleShopNode();
                _runAudio?.SetContext(RunAudioController.Context.Shop);
                ShowPathOverview();
                RefreshPathOverview();
                return;
            }

            if (node.Type == NodeType.Rest)
            {
                HandleRestNode();
                _runAudio?.SetContext(RunAudioController.Context.Rest);
                ShowPathOverview();
                RefreshPathOverview();
                return;
            }

            if (node.Type == NodeType.Event)
            {
                HandleEventNode();
                ShowPathOverview();
                RefreshPathOverview();
                return;
            }

            if (node.Type == NodeType.Relic)
            {
                HandleRelicNode();
                ShowPathOverview();
                RefreshPathOverview();
                return;
            }

            // Cross-link tiles now function as their underlying type (puzzle, shop, etc.)
            // with the additional ability to switch lanes after completion.

            _pathOverlayMessage = string.Empty;

            if (level == null)
            {
                SetStatus("Node has no puzzle level configured.");
                ShowPathOverview();
                RefreshPathOverview();
                return;
            }

            SetStatus($"Route selected: {node.Type}, {level.BoardSize}x{level.BoardSize}, {level.Stars}★");
            ShowSudoku();
            BuildOrRefreshSudokuBoard();
            RefreshPathOverview();
        }

        private readonly PlayerIconController _playerIcon = new();

        private void RefreshPathOverview()
        {
            if (runMapController == null) return;

            var run = runMapController.Run;
            if (run == null || run.CurrentRunGraph == null || run.CurrentRunGraph.Count == 0)
            {
                if (pathOverviewText != null) pathOverviewText.text = "No active run graph.";
                return;
            }

            var runState = run.RunState;
            var floor = runState.CurrentFloor;
            var theme = Data.FloorThemeData.Get(floor);

            // Apply floor theme background
            if (pathOverviewPanel != null)
            {
                var bgImage = pathOverviewPanel.GetComponent<Image>();
                if (bgImage != null) bgImage.color = theme.BgColor;
            }

            // Meta HUD
            if (pathOverviewText != null)
            {
                var classPassive = Classes.ClassCatalog.GetMeta(runState.ClassId).PassiveDescription;
                var overview =
                    $"{theme.Name} ({theme.JapaneseName})    Garden {floor + 1} / {runState.TotalFloors}\n" +
                    $"HP: {runState.CurrentHP}/{runState.MaxHP}    Gold: {runState.CurrentGold}    Pencil: {runState.CurrentPencil}/{runState.MaxPencil}\n" +
                    $"Items: {runState.Inventory.Count}    Relic: {(runState.HasRelic ? Economy.RelicService.GetName(runState.HeldRelic.Id) : "None")}    Passive: {classPassive}";

                if (!string.IsNullOrWhiteSpace(_pathOverlayMessage))
                    overview += "\n" + _pathOverlayMessage;
                if (!string.IsNullOrWhiteSpace(_hoverInfo))
                    overview += "\n" + _hoverInfo;

                pathOverviewText.text = overview;
            }

            if (laneAText != null) laneAText.text = "Calm Route";
            if (laneBText != null) laneBText.text = "Risk Route";

            // Hide per-lane backgrounds — we now render everything on the overlay root
            if (laneAPathRoot != null)
            {
                var img = laneAPathRoot.GetComponent<Image>();
                if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
            }
            if (laneBPathRoot != null)
            {
                var img = laneBPathRoot.GetComponent<Image>();
                if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
            }

            var previewA = runMapController.BuildPathChoicePreview(false);
            var previewB = runMapController.BuildPathChoicePreview(true);
            var lockValue = previewA.LockedPath ?? previewB.LockedPath;

            var nextSignature = BuildLaneRenderSignature(previewA, previewB, lockValue);
            if (nextSignature != _lastLaneRenderSignature)
            {
                _lastLaneRenderSignature = nextSignature;
                RebuildGardenCanvas(lockValue, theme);
                RebuildInventoryBadges();
            }
        }

        private void RebuildGardenCanvas(bool? lockValue, Data.FloorTheme theme)
        {
            EnsurePathOverlayRoot();
            if (_pathOverlayRoot == null) return;

            // Clear previous renders from lane roots and overlay
            ClearChildren(laneAPathRoot);
            ClearChildren(laneBPathRoot);
            for (var i = _pathOverlayRoot.childCount - 1; i >= 0; i--)
                Destroy(_pathOverlayRoot.GetChild(i).gameObject);

            var run = runMapController?.Run;
            var graph = run?.CurrentRunGraph;
            if (graph == null || graph.Count == 0) return;

            var canvasRect = _pathOverlayRoot;
            var canvasW = Mathf.Max(400f, canvasRect.rect.width);
            var canvasH = Mathf.Max(300f, canvasRect.rect.height);

            // Build adjacency: connect sequential same-lane nodes + start→first of each lane + last→boss
            var calmNodes = new List<RunNode>();
            var riskNodes = new List<RunNode>();
            RunNode startNode = null;
            RunNode bossNode = null;

            for (var i = 0; i < graph.Count; i++)
            {
                var node = graph[i];
                if (node.Type == NodeType.Start) startNode = node;
                else if (node.Type == NodeType.Boss) bossNode = node;
                else if (!node.IsRiskPath) calmNodes.Add(node);
                else riskNodes.Add(node);
            }

            // Sort by depth
            calmNodes.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            riskNodes.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            // Draw edges first (behind tiles)
            var edgeColor = theme.PathColor;
            if (startNode != null)
            {
                if (calmNodes.Count > 0) DrawCanvasEdge(canvasRect, canvasW, canvasH, startNode, calmNodes[0], edgeColor);
                if (riskNodes.Count > 0) DrawCanvasEdge(canvasRect, canvasW, canvasH, startNode, riskNodes[0], edgeColor);
            }

            for (var i = 1; i < calmNodes.Count; i++)
                DrawCanvasEdge(canvasRect, canvasW, canvasH, calmNodes[i - 1], calmNodes[i], edgeColor);
            for (var i = 1; i < riskNodes.Count; i++)
                DrawCanvasEdge(canvasRect, canvasW, canvasH, riskNodes[i - 1], riskNodes[i], edgeColor);

            if (bossNode != null)
            {
                if (calmNodes.Count > 0) DrawCanvasEdge(canvasRect, canvasW, canvasH, calmNodes[calmNodes.Count - 1], bossNode, edgeColor);
                if (riskNodes.Count > 0) DrawCanvasEdge(canvasRect, canvasW, canvasH, riskNodes[riskNodes.Count - 1], bossNode, edgeColor);
            }

            // Draw cross-edge bezier
            RunNode crossCalm = null, crossRisk = null;
            for (var i = 0; i < graph.Count; i++)
            {
                if (graph[i].IsCrossLink)
                {
                    if (!graph[i].IsRiskPath) crossCalm = graph[i];
                    else crossRisk = graph[i];
                }
            }

            if (crossCalm != null && crossRisk != null)
            {
                DrawCrossEdgeBezier(canvasRect, canvasW, canvasH, crossCalm, crossRisk);
            }

            // Draw all node tiles
            var currentNodeIndex = run.RunState.CurrentNodeIndex;
            for (var i = 0; i < graph.Count; i++)
            {
                var node = graph[i];
                var pos = CanvasToPixel(node.CanvasX, node.CanvasY, canvasW, canvasH);
                var isCurrentNode = i == currentNodeIndex;
                var nodeButton = CreateGardenNodeTile(_pathOverlayRoot, node, pos, lockValue, theme, isCurrentNode);
                nodeButton.onClick.RemoveAllListeners();

                if (node.Type == NodeType.Boss)
                {
                    nodeButton.onClick.AddListener(() => ShowBossGateChoice());
                    nodeButton.interactable = IsNextChoiceNode(node, false) || IsNextChoiceNode(node, true);
                }
                else if (node.Type != NodeType.Start)
                {
                    var risk = node.IsRiskPath;
                    nodeButton.onClick.AddListener(() => ChoosePath(risk));
                    nodeButton.interactable = IsNextChoiceNode(node, risk);
                }
                else
                {
                    nodeButton.interactable = false;
                }

                // Track player icon position
                if (isCurrentNode)
                {
                    _playerIcon.EnsureCreated(canvasRect);
                    _playerIcon.SetPosition(new Vector2(node.CanvasX, node.CanvasY), canvasRect);
                    _playerIcon.BringToFront();
                }
            }
        }

        private static Vector2 CanvasToPixel(float cx, float cy, float w, float h)
        {
            // Left-to-right layout: X maps directly, Y inverted (canvas 0=top, RectTransform 0=bottom)
            return new Vector2(cx * w, (1f - cy) * h);
        }

        private void DrawCanvasEdge(RectTransform parent, float w, float h, RunNode a, RunNode b, Color color)
        {
            var pa = CanvasToPixel(a.CanvasX, a.CanvasY, w, h);
            var pb = CanvasToPixel(b.CanvasX, b.CanvasY, w, h);

            var lineGo = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(parent, false);
            lineGo.transform.SetAsFirstSibling();
            var img = lineGo.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rect = lineGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;

            var delta = pb - pa;
            var dist = delta.magnitude;
            if (dist < 1f) return;
            rect.sizeDelta = new Vector2(dist, 3f);
            rect.anchoredPosition = pa + delta * 0.5f;
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void DrawCrossEdgeBezier(RectTransform parent, float w, float h, RunNode a, RunNode b)
        {
            var pa = CanvasToPixel(a.CanvasX, a.CanvasY, w, h);
            var pb = CanvasToPixel(b.CanvasX, b.CanvasY, w, h);
            var centerX = w * 0.5f;

            // Control points offset toward center
            var cpA = new Vector2(Mathf.Lerp(pa.x, centerX, 0.5f), Mathf.Lerp(pa.y, pb.y, 0.33f));
            var cpB = new Vector2(Mathf.Lerp(pb.x, centerX, 0.5f), Mathf.Lerp(pa.y, pb.y, 0.67f));

            var crossColor = new Color(0.85f, 0.72f, 0.18f, 0.70f);
            const int segments = 12;
            var prev = pa;
            for (var s = 1; s <= segments; s++)
            {
                var t = (float)s / segments;
                var pt = CubicBezier(pa, cpA, cpB, pb, t);
                var segGo = new GameObject("BezierSeg", typeof(RectTransform), typeof(Image));
                segGo.transform.SetParent(parent, false);
                segGo.transform.SetAsFirstSibling();
                var img = segGo.GetComponent<Image>();
                img.color = crossColor;
                img.raycastTarget = false;
                var rect = segGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                var d = pt - prev;
                var segLen = d.magnitude;
                if (segLen < 0.5f) { prev = pt; continue; }
                rect.sizeDelta = new Vector2(segLen, 3f);
                rect.anchoredPosition = prev + d * 0.5f;
                rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                prev = pt;
            }
        }

        private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        private Button CreateGardenNodeTile(RectTransform parent, RunNode node, Vector2 pos,
            bool? lockValue, Data.FloorTheme theme, bool isCurrent)
        {
            var go = new GameObject($"Tile_{node.Type}_{node.Depth}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var goRect = go.GetComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.zero;
            goRect.pivot = new Vector2(0.5f, 0.5f);
            goRect.sizeDelta = node.Type == NodeType.Boss ? new Vector2(120f, 60f) : new Vector2(72f, 72f);
            goRect.anchoredPosition = pos;

            var currentNodeIdx = runMapController?.Run?.RunState?.CurrentNodeIndex ?? 0;
            var nodeIdx = -1;
            var graph = runMapController?.Run?.CurrentRunGraph;
            if (graph != null)
            {
                for (var i = 0; i < graph.Count; i++)
                    if (ReferenceEquals(graph[i], node)) { nodeIdx = i; break; }
            }

            // Determine tile visual state
            Color nodeColor;
            var isVisited = nodeIdx >= 0 && nodeIdx < currentNodeIdx;
            var isUnreachable = false;

            // Unreachable: on the opposite locked lane and not a boss/start/crosslink
            if (lockValue.HasValue && node.Type != NodeType.Boss && node.Type != NodeType.Start
                && !node.IsCrossLink)
            {
                isUnreachable = node.IsRiskPath != lockValue.Value;
            }

            if (node.Type == NodeType.Boss)
                nodeColor = theme.BossGateColor;
            else if (node.Type == NodeType.Start)
                nodeColor = theme.TileColor;
            else if (isVisited)
                nodeColor = new Color(theme.TileColor.r, theme.TileColor.g, theme.TileColor.b, 0.40f);
            else if (isUnreachable)
                nodeColor = new Color(0.25f, 0.25f, 0.25f, 0.35f);
            else if (node.IsCrossLink)
                nodeColor = new Color(0.65f, 0.42f, 0.08f, 1f);
            else if (isCurrent)
                nodeColor = new Color(theme.AccentColor.r, theme.AccentColor.g, theme.AccentColor.b, 1f);
            else
                nodeColor = theme.TileColor;

            var image = go.GetComponent<Image>();
            image.color = nodeColor;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.colorMultiplier = 1.25f;
            colors.fadeDuration = 0.07f;
            button.colors = colors;

            // Node type label
            var labelGo = new GameObject("TypeLabel", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.30f);
            labelRect.anchorMax = new Vector2(1f, 0.70f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = GetTileLabel(node.Type);
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = node.Type == NodeType.Boss ? 14 : 11;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.95f, 0.95f, 0.90f, isVisited ? 0.50f : 0.95f);
            labelText.raycastTarget = false;

            // Size and star labels for puzzle nodes
            var showLabels = node.Type == NodeType.Puzzle || node.Type == NodeType.ElitePuzzle
                || node.Type == NodeType.Boss || node.Type == NodeType.PreBoss;
            if (showLabels)
            {
                runMapController.TryGetFixedLevelForNode(node, out var config);
                var boardSize = config != null ? config.BoardSize : Mathf.Clamp(4 + node.Depth / 3, 4, 9);
                var starCount = config != null ? config.Stars : Mathf.Clamp(1 + node.Depth / 4, 1, 5);

                var sizeGo = new GameObject("SzLbl", typeof(RectTransform));
                sizeGo.transform.SetParent(go.transform, false);
                var sizeRect = sizeGo.GetComponent<RectTransform>();
                sizeRect.anchorMin = new Vector2(0f, 0.72f);
                sizeRect.anchorMax = new Vector2(0.50f, 1f);
                sizeRect.offsetMin = new Vector2(3f, 0f);
                sizeRect.offsetMax = new Vector2(0f, -2f);
                var sizeText = sizeGo.AddComponent<Text>();
                sizeText.text = $"{boardSize}x{boardSize}";
                sizeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                sizeText.fontSize = 9;
                sizeText.alignment = TextAnchor.UpperLeft;
                sizeText.color = new Color(0.95f, 0.95f, 0.85f, 0.85f);
                sizeText.raycastTarget = false;

                var starGo = new GameObject("StarLbl", typeof(RectTransform));
                starGo.transform.SetParent(go.transform, false);
                var starRect = starGo.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.50f, 0f);
                starRect.anchorMax = new Vector2(1f, 0.30f);
                starRect.offsetMin = new Vector2(0f, 2f);
                starRect.offsetMax = new Vector2(-3f, 0f);
                var starText = starGo.AddComponent<Text>();
                starText.text = new string('\u2605', starCount);
                starText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                starText.fontSize = 9;
                starText.alignment = TextAnchor.LowerRight;
                starText.color = new Color(0.98f, 0.83f, 0.26f, 0.85f);
                starText.raycastTarget = false;
            }

            // Visited checkmark
            if (isVisited)
            {
                var checkGo = new GameObject("Check", typeof(RectTransform));
                checkGo.transform.SetParent(go.transform, false);
                var checkRect = checkGo.GetComponent<RectTransform>();
                checkRect.anchorMin = new Vector2(0.7f, 0.7f);
                checkRect.anchorMax = new Vector2(1f, 1f);
                checkRect.offsetMin = Vector2.zero;
                checkRect.offsetMax = Vector2.zero;
                var checkText = checkGo.AddComponent<Text>();
                checkText.text = "\u2713";
                checkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                checkText.fontSize = 16;
                checkText.alignment = TextAnchor.MiddleCenter;
                checkText.color = new Color(0.40f, 0.80f, 0.42f, 0.70f);
                checkText.raycastTarget = false;
            }

            // Cross-link badge: small ⇄ indicator in top-left corner
            if (node.IsCrossLink)
            {
                var crossGo = new GameObject("CrossBadge", typeof(RectTransform));
                crossGo.transform.SetParent(go.transform, false);
                var crossRect = crossGo.GetComponent<RectTransform>();
                crossRect.anchorMin = new Vector2(0f, 0.75f);
                crossRect.anchorMax = new Vector2(0.30f, 1f);
                crossRect.offsetMin = Vector2.zero;
                crossRect.offsetMax = Vector2.zero;
                var crossText = crossGo.AddComponent<Text>();
                crossText.text = "\u21c4";
                crossText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                crossText.fontSize = 12;
                crossText.alignment = TextAnchor.MiddleCenter;
                crossText.color = new Color(1f, 0.92f, 0.60f, 0.90f);
                crossText.raycastTarget = false;
            }

            return button;
        }

        private static string GetTileLabel(NodeType type)
        {
            return type switch
            {
                NodeType.Start => "Start",
                NodeType.Puzzle => "Puzzle",
                NodeType.ElitePuzzle => "Elite",
                NodeType.Shop => "Shop",
                NodeType.Rest => "Rest",
                NodeType.Relic => "Relic",
                NodeType.Event => "Event",
                NodeType.PreBoss => "Elite",
                NodeType.Boss => "Boss Gate",
                _ => "?"
            };
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null) return;
            for (var i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private static void PrepareLaneRootForFreePlacement(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = new Vector2(8f, 8f);
            root.offsetMax = new Vector2(-8f, -8f);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
            }

            var fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            var viewport = root.parent as RectTransform;
            if (viewport != null)
            {
                var mask = viewport.GetComponent<Mask>();
                if (mask != null)
                {
                    mask.showMaskGraphic = false;
                }
            }
        }

        private void EnsurePathOverlayRoot()
        {
            if (_pathOverlayRoot != null || pathOverviewPanel == null)
            {
                return;
            }

            var panelRect = pathOverviewPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            var overlayGo = new GameObject("PathOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(panelRect, false);
            _pathOverlayRoot = overlayGo.GetComponent<RectTransform>();
            _pathOverlayRoot.anchorMin = Vector2.zero;
            _pathOverlayRoot.anchorMax = Vector2.one;
            _pathOverlayRoot.offsetMin = Vector2.zero;
            _pathOverlayRoot.offsetMax = Vector2.zero;
            _pathOverlayRoot.SetAsLastSibling();
        }

        // RebuildSharedBossGate removed — replaced by RebuildGardenCanvas

        private int BuildLaneRenderSignature(RunMapController.PathChoicePreview previewA, RunMapController.PathChoicePreview previewB, bool? lockValue)
        {
            unchecked
            {
                var hash = 17;
                var run = runMapController?.Run;
                hash = hash * 31 + (run?.RunState?.CurrentNodeIndex ?? -1);
                hash = hash * 31 + (lockValue.HasValue ? (lockValue.Value ? 1 : 2) : 0);
                hash = hash * 31 + BuildPreviewSignature(previewA);
                hash = hash * 31 + BuildPreviewSignature(previewB);

                var state = run?.RunState;
                if (state != null)
                {
                    hash = hash * 31 + state.Inventory.Count;
                    hash = hash * 31 + state.CurrentGold;
                    for (var i = 0; i < state.Inventory.Count; i++)
                    {
                        var item = state.Inventory[i];
                        hash = hash * 31 + (item?.Id?.GetHashCode() ?? 0);
                        hash = hash * 31 + (item?.Charges ?? 0);
                    }
                }

                var graph = run?.CurrentRunGraph;
                if (graph != null)
                {
                    hash = hash * 31 + graph.Count;
                    for (var i = 0; i < graph.Count; i++)
                    {
                        var node = graph[i];
                        hash = hash * 31 + node.Depth;
                        hash = hash * 31 + (int)node.Type;
                        hash = hash * 31 + (node.IsRiskPath ? 1 : 0);
                        hash = hash * 31 + (node.IsRevealed ? 1 : 0);
                    }
                }

                return hash;
            }
        }

        private static int BuildPreviewSignature(RunMapController.PathChoicePreview preview)
        {
            unchecked
            {
                var hash = 13;
                hash = hash * 31 + (preview.Available ? 1 : 0);
                hash = hash * 31 + (preview.RiskPath ? 1 : 0);
                hash = hash * 31 + preview.Depth;
                hash = hash * 31 + preview.BoardSize;
                hash = hash * 31 + preview.Stars;
                hash = hash * 31 + (int)preview.NodeType;
                return hash;
            }
        }

        // Old lane-based methods removed: RebuildLaneNodeButtons, ResolveLaneOverlaps,
        // CaptureLaneEndForSharedBoss, StoreCrossLinkPosition, CreateCrossLinkConnectorLine,
        // ComputeLaneNodePosition, CreatePathConnectionLine, IsPuzzleNodeType, CreatePathNodeButton
        // All replaced by RebuildGardenCanvas with pre-computed canvas positions.

        private bool IsNextChoiceNode(RunNode node, bool risk)
        {
            var preview = runMapController.BuildPathChoicePreview(risk);
            return preview.Available && preview.Depth == node.Depth && preview.NodeType == node.Type;
        }

        private void TryApplyButtonIcons()
        {
            var quit = Resources.Load<Sprite>("GeneratedIcons/icon_ink_save");

            if (quit == null)
            {
                return;
            }

            ApplyButtonIcon(saveQuitPathButton, quit);
            ApplyButtonIcon(saveQuitSudokuButton, quit);
            _buttonIconsApplied = true;
        }

        private static void ApplyButtonIcon(Button button, Sprite sprite)
        {
            if (button == null || sprite == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private void BuildOrRefreshSudokuBoard()
        {
            var board = runMapController?.Run?.CurrentBoard;
            if (board == null || sudokuGridRoot == null)
            {
                return;
            }

            if (_boardSize != board.Size || _cells.Count == 0)
            {
                BuildBoardGrid(board.Size);
                _overlaysBuilt = false;

                // Sync overlay and number root sizes with the resized grid
                if (_gridOverlayRoot != null)
                {
                    _gridOverlayRoot.anchoredPosition = sudokuGridRoot.anchoredPosition;
                    _gridOverlayRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sudokuGridRoot.rect.width);
                    _gridOverlayRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sudokuGridRoot.rect.height);
                }
            }

            if (!_overlaysBuilt)
            {
                BuildModifierOverlays();
                _overlaysBuilt = true;
            }

            // Boss modifier description — refresh every time the board is built/refreshed
            RefreshBossModifierDescription();

            if ((_selectedRow < 0 || _selectedCol < 0) && TryFindFirstEditableCell(board, out var row, out var col))
            {
                _selectedRow = row;
                _selectedCol = col;
            }

            UpdateNumpadAvailability(board.Size);
            RenderBoard(board);
            RebuildPuzzleItemBar();
        }

        private void BuildBoardGrid(int size)
        {
            _boardSize = size;
            _cells.Clear();

            for (var i = sudokuGridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(sudokuGridRoot.GetChild(i).gameObject);
            }

            var grid = sudokuGridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = sudokuGridRoot.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = size;
            grid.spacing = new Vector2(2f, 2f);
            grid.cellSize = size <= 6 ? new Vector2(96f, 96f) : size <= 8 ? new Vector2(74f, 74f) : new Vector2(62f, 62f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var totalW = (grid.cellSize.x * size) + (grid.spacing.x * (size - 1));
            var totalH = (grid.cellSize.y * size) + (grid.spacing.y * (size - 1));
            sudokuGridRoot.anchorMin = new Vector2(0.5f, 0.42f);
            sudokuGridRoot.anchorMax = new Vector2(0.5f, 0.42f);
            sudokuGridRoot.pivot = new Vector2(0.5f, 0.5f);
            sudokuGridRoot.anchoredPosition = Vector2.zero;
            sudokuGridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalW);
            sudokuGridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalH);

            // Ensure the number-text layer exists and is placed just after the grid root (overlay
            // will be inserted between them when EnsureGridOverlayRoot is called later, giving
            // the correct order: Grid → Overlay → Numbers).
            if (_gridNumberRoot == null)
            {
                var numGo = new GameObject("GridNumbers", typeof(RectTransform));
                numGo.transform.SetParent(sudokuGridRoot.parent, false);
                _gridNumberRoot = numGo.GetComponent<RectTransform>();
                var leNum = numGo.AddComponent<UnityEngine.UI.LayoutElement>();
                leNum.ignoreLayout = true;
                var cgNum = numGo.AddComponent<CanvasGroup>();
                cgNum.blocksRaycasts = false;
                cgNum.interactable = false;
                _gridNumberRoot.SetSiblingIndex(sudokuGridRoot.GetSiblingIndex() + 1);
            }
            else
            {
                for (var i = _gridNumberRoot.childCount - 1; i >= 0; i--)
                    Destroy(_gridNumberRoot.GetChild(i).gameObject);
            }

            // Keep number root size/position in sync with grid root
            _gridNumberRoot.anchorMin = sudokuGridRoot.anchorMin;
            _gridNumberRoot.anchorMax = sudokuGridRoot.anchorMax;
            _gridNumberRoot.pivot = sudokuGridRoot.pivot;
            _gridNumberRoot.anchoredPosition = sudokuGridRoot.anchoredPosition;
            _gridNumberRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalW);
            _gridNumberRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalH);

            // Set up the same GridLayoutGroup on the number root so children line up with grid cells
            var numGrid = _gridNumberRoot.GetComponent<GridLayoutGroup>() ?? _gridNumberRoot.gameObject.AddComponent<GridLayoutGroup>();
            numGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            numGrid.constraintCount = size;
            numGrid.spacing = grid.spacing;
            numGrid.cellSize = grid.cellSize;
            numGrid.childAlignment = TextAnchor.MiddleCenter;

            var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    // Background cell in sudokuGridRoot — image + button + borders only
                    var cellGo = new GameObject($"Cell_{row}_{col}", typeof(RectTransform), typeof(Image), typeof(Button));
                    cellGo.transform.SetParent(sudokuGridRoot, false);

                    var image = cellGo.GetComponent<Image>();
                    image.color = EmptyColor;

                    var button = cellGo.GetComponent<Button>();
                    var capturedRow = row;
                    var capturedCol = col;
                    button.onClick.AddListener(() => OnCellClicked(capturedRow, capturedCol));

                    var borderTop = CreateCellBorder(cellGo.transform, "BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 0f));
                    var borderBottom = CreateCellBorder(cellGo.transform, "BorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 3f));
                    var borderLeft = CreateCellBorder(cellGo.transform, "BorderLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(3f, 0f));
                    var borderRight = CreateCellBorder(cellGo.transform, "BorderRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(0f, 0f));

                    // Text cell in _gridNumberRoot — value and pencil text only (no image blocker)
                    var numCellGo = new GameObject($"NumCell_{row}_{col}", typeof(RectTransform));
                    numCellGo.transform.SetParent(_gridNumberRoot, false);

                    var textGo = new GameObject("Value", typeof(RectTransform), typeof(Text));
                    textGo.transform.SetParent(numCellGo.transform, false);
                    var textRect = textGo.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    var text = textGo.GetComponent<Text>();
                    text.font = builtinFont;
                    var baseFontSize = size <= 6 ? 30 : size <= 8 ? 24 : 20;
                    text.fontSize = _accessibility?.ScaleFont(baseFontSize) ?? baseFontSize;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = new Color(0.93f, 0.96f, 0.90f, 1f);
                    text.raycastTarget = false;

                    var pencilGo = new GameObject("Pencil", typeof(RectTransform), typeof(Text));
                    pencilGo.transform.SetParent(numCellGo.transform, false);
                    var pencilRect = pencilGo.GetComponent<RectTransform>();
                    pencilRect.anchorMin = new Vector2(0.08f, 0.08f);
                    pencilRect.anchorMax = new Vector2(0.92f, 0.92f);
                    pencilRect.offsetMin = Vector2.zero;
                    pencilRect.offsetMax = Vector2.zero;
                    var pencilText = pencilGo.GetComponent<Text>();
                    pencilText.font = builtinFont;
                    var basePencilSize = size <= 6 ? 16 : 14;
                    pencilText.fontSize = _accessibility?.ScaleFont(basePencilSize) ?? basePencilSize;
                    pencilText.alignment = TextAnchor.UpperLeft;
                    pencilText.color = new Color(0.84f, 0.86f, 0.82f, 0.95f);
                    pencilText.supportRichText = true;
                    pencilText.raycastTarget = false;

                    _cells.Add(new CellView
                    {
                        Row = row,
                        Col = col,
                        Root = cellGo.GetComponent<RectTransform>(),
                        Image = image,
                        Label = text,
                        PencilLabel = pencilText,
                        BorderTop = borderTop,
                        BorderBottom = borderBottom,
                        BorderLeft = borderLeft,
                        BorderRight = borderRight,
                        Button = button
                    });
                }
            }
        }

        private static Image CreateCellBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.98f, 0.76f, 0.26f, 0.0f);
            image.raycastTarget = false;
            return image;
        }

        private void BuildNumpad()
        {
            if (numpadRoot == null)
            {
                return;
            }

            _numpadButtons.Clear();
            for (var i = numpadRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(numpadRoot.GetChild(i).gameObject);
            }

            var grid = numpadRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = numpadRoot.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(90f, 56f);
            grid.spacing = new Vector2(8f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var value = 1; value <= 9; value++)
            {
                var btn = CreateNumpadButton(value);
                _numpadButtons.Add(btn);
            }

            EnsurePencilToggleButton();
        }

        private void EnsurePencilToggleButton()
        {
            if (numpadRoot == null)
            {
                return;
            }

            if (_pencilModeButton != null)
            {
                return;
            }

            // Place Mode button inside the numpad box, below the 7-8-9 row.
            var btnGo = new GameObject("BtnPencilMode", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(numpadRoot, false);

            var le = btnGo.AddComponent<UnityEngine.UI.LayoutElement>();
            le.ignoreLayout = true;

            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.10f, 0.04f);
            rect.anchorMax = new Vector2(0.90f, 0.14f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = btnGo.GetComponent<Image>();
            image.color = new Color(0.20f, 0.26f, 0.31f, 0.95f);

            var button = btnGo.GetComponent<Button>();
            button.onClick.AddListener(TogglePencilMode);
            _pencilModeButton = button;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = _accessibility?.ScaleFont(13) ?? 13;
            text.color = new Color(0.92f, 0.95f, 0.90f, 1f);
            text.text = "Mode: SOLVE";
        }

        private void TogglePencilMode()
        {
            _pencilMode = !_pencilMode;
            _runAudio?.PlayPencilToggle();
            if (_pencilModeButton != null)
            {
                var label = _pencilModeButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = _pencilMode ? "Mode: PENCIL" : "Mode: SOLVE";
                }

                var image = _pencilModeButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = _pencilMode ? new Color(0.31f, 0.43f, 0.28f, 0.98f) : new Color(0.20f, 0.26f, 0.31f, 0.95f);
                }
            }
        }

        private Button CreateNumpadButton(int value)
        {
            var btnGo = new GameObject($"Num_{value}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(numpadRoot, false);

            var image = btnGo.GetComponent<Image>();
            image.color = new Color(0.19f, 0.30f, 0.20f, 1f);

            var button = btnGo.GetComponent<Button>();
            var colors = button.colors;
            colors.colorMultiplier = 1.30f;
            colors.fadeDuration = 0.07f;
            colors.highlightedColor = new Color(0.27f, 0.40f, 0.28f, 1f);
            colors.pressedColor = new Color(0.13f, 0.21f, 0.14f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => EnterNumber(value));

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(btnGo.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = value.ToString();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = _accessibility?.ScaleFont(24) ?? 24;
            text.color = new Color(0.93f, 0.96f, 0.90f, 1f);

            return button;
        }

        private void UpdateNumpadAvailability(int boardSize)
        {
            for (var i = 0; i < _numpadButtons.Count; i++)
            {
                var value = i + 1;
                var button = _numpadButtons[i];
                if (button == null)
                {
                    continue;
                }

                var enabled = value <= boardSize;
                button.interactable = enabled;

                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = enabled ? new Color(0.19f, 0.30f, 0.20f, 1f) : new Color(0.13f, 0.13f, 0.13f, 0.8f);
                }
            }
        }

        private void OnCellClicked(int row, int col)
        {
            var run = runMapController?.Run;
            var board = run?.CurrentBoard;
            if (board == null)
            {
                return;
            }

            // SilkFan phase 2: complete the swap on second cell click
            if (run.IsSilkFanPending)
            {
                if (run.TryCompleteSilkFanSwap(row, col, out var swapMsg))
                {
                    _runAudio?.PlayItemUse();
                    RenderBoard(board);
                    RefreshHud();
                    _lastPuzzleItemSignature = int.MinValue;
                    RebuildPuzzleItemBar();
                }
                SetStatus(swapMsg);
                return;
            }

            var now = Time.unscaledTime;
            var value = board.GetCell(row, col);
            var isDoubleClick = _lastClickRow == row && _lastClickCol == col && now - _lastClickTime <= 0.28f;

            _selectedRow = row;
            _selectedCol = col;
            _runAudio?.PlayCellSelect();

            if (isDoubleClick && value > 0)
            {
                _highlightValue = value;
            }
            else if (_highlightValue > 0 && _highlightValue == value)
            {
                _highlightValue = 0;
            }

            _lastClickRow = row;
            _lastClickCol = col;
            _lastClickTime = now;

            RenderBoard(board);
        }

        private void EnterNumber(int value)
        {
            var run = runMapController?.Run;
            var board = run?.CurrentBoard;
            if (run == null || board == null)
            {
                return;
            }

            // Cancel any pending SilkFan swap when entering a number
            if (run.IsSilkFanPending)
            {
                run.CancelSilkFan();
                SetStatus("Silk Fan cancelled.");
            }

            if (_selectedRow < 0 || _selectedCol < 0)
            {
                if (TryFindFirstEditableCell(board, out var autoRow, out var autoCol))
                {
                    _selectedRow = autoRow;
                    _selectedCol = autoCol;
                }

                if (_selectedRow < 0 || _selectedCol < 0)
                {
                    SetStatus("No editable cell available.");
                    return;
                }

                SetStatus("Select a cell first.");
            }

            if (board.IsGiven(_selectedRow, _selectedCol))
            {
                SetStatus("Given cells cannot be changed.");
                return;
            }

            var fogOverlay = run.CurrentOverlayData;
            var isFoggedCell = fogOverlay != null && fogOverlay.IsFogged(_selectedRow, _selectedCol);

            if (value < 1 || value > board.Size)
            {
                SetStatus($"Value must be between 1 and {board.Size}.");
                return;
            }

            if (_pencilMode)
            {
                if (!board.IsEmpty(_selectedRow, _selectedCol))
                {
                    SetStatus("Clear the cell before adding pencil marks.");
                    return;
                }

                var pencil = board.GetPencilSet(_selectedRow, _selectedCol);
                if (pencil.Contains(value))
                {
                    pencil.Remove(value);
                    SetStatus($"Pencil: removed {value}.");
                }
                else
                {
                    if (!run.TryAddPencilMark(_selectedRow, _selectedCol, value))
                    {
                        SetStatus("No pencil charges left.");
                        return;
                    }

                    SetStatus($"Pencil: added {value}.");
                }

                RefreshHud();
                RenderBoard(board);
                return;
            }

            var ok = run.PlaceNumber(_selectedRow, _selectedCol, value);
            if (isFoggedCell)
            {
                SetStatus($"Placed {value} in fog \u2014 will validate on reveal.");
                _runAudio?.PlayCorrectPlacement();
            }
            else
            {
                SetStatus(ok ? $"Placed {value}." : $"{value} is incorrect. HP now {run.RunState.CurrentHP}.");
                if (!ok)
                {
                    _runAudio?.PlayWrongPlacement();
                    TriggerScreenShake(AnimationHelper.ShakeWrongPx, AnimationHelper.ShakeWrongDuration);
                    if (run.RunState.CurrentHP <= 2)
                    {
                        _runAudio?.PlayHpCritical();
                    }
                }
                else _runAudio?.PlayCorrectPlacement();
            }
            RefreshHud();
            RenderBoard(board);
            RefreshSolveButtonState();
            CheckForGameOver();
        }

        private void EvaluateCurrentSudoku()
        {
            var run = runMapController?.Run;
            var board = run?.CurrentBoard;
            var state = run?.RunState;
            var levelState = run?.CurrentLevelState;
            if (run == null || board == null || state == null || levelState == null)
            {
                return;
            }

            if (!board.IsComplete())
            {
                SetStatus("Fill all cells before pressing Solve.");
                return;
            }

            if (IsBoardSolved(board))
            {
                levelState.PuzzleComplete = true;
                SetStatus("Sudoku solved. Choose next path tile.");
                _runAudio?.PlayPuzzleSolved();
                HandleCompletionState();
                return;
            }

            levelState.Mistakes++;
            state.CurrentHP = Math.Max(0, state.CurrentHP - 1);
            SetStatus($"Sudoku has errors. HP now {state.CurrentHP}.");
            _runAudio?.PlayWrongPlacement();
            TriggerScreenShake(AnimationHelper.ShakeWrongPx, AnimationHelper.ShakeWrongDuration);
            if (state.CurrentHP <= 2) _runAudio?.PlayHpCritical();
            RefreshHud();
            CheckForGameOver();
        }

        private static bool IsBoardSolved(SudokuBoard board)
        {
            for (var row = 0; row < board.Size; row++)
            {
                for (var col = 0; col < board.Size; col++)
                {
                    if (board.GetCell(row, col) != board.Solution[row, col])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryFindFirstEditableCell(SudokuBoard board, out int row, out int col)
        {
            row = -1;
            col = -1;
            if (board == null)
            {
                return false;
            }

            for (var r = 0; r < board.Size; r++)
            {
                for (var c = 0; c < board.Size; c++)
                {
                    if (!board.IsGiven(r, c))
                    {
                        row = r;
                        col = c;
                        return true;
                    }
                }
            }

            return false;
        }

        private void RenderBoard(SudokuBoard board)
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var value = board.GetCell(cell.Row, cell.Col);
                var given = board.IsGiven(cell.Row, cell.Col);

                cell.Label.text = value == 0 ? string.Empty : value.ToString();
                cell.Label.color = given ? new Color(0.04f, 0.04f, 0.04f, 1f) : new Color(0.98f, 0.98f, 0.98f, 1f);
                if (cell.PencilLabel != null)
                {
                    cell.PencilLabel.text = value == 0 ? BuildPencilMarkup(board, cell.Row, cell.Col) : string.Empty;
                }

                var color = ComputeBaseCellColor(board, cell.Row, cell.Col, given);

                if (_highlightConflicts && !given && HasConflict(board, cell.Row, cell.Col))
                {
                    color = ConflictColor;
                }

                if (_selectedRow == cell.Row && _selectedCol == cell.Col)
                {
                    color = SelectedColor;
                }
                else if (_selectedRow == cell.Row || _selectedCol == cell.Col)
                {
                    color = RowColHighlight;
                }
                else if (IsAntiknightActive() && IsKnightMoveFrom(_selectedRow, _selectedCol, cell.Row, cell.Col))
                {
                    color = KnightMoveHighlight;
                }

                if (_highlightValue > 0 && value == _highlightValue)
                {
                    color = MatchValueColor;
                }

                if (Time.unscaledTime <= _finderHighlightUntil && ContainsFinderHighlight(cell.Row, cell.Col))
                {
                    color = FinderHintColor;
                }

                var overlay = runMapController?.Run?.CurrentOverlayData;
                if (overlay != null && overlay.IsFogged(cell.Row, cell.Col))
                {
                    color = FogColor;
                    // Keep placed numbers visible in fog; only hide empty cells
                    if (value == 0)
                    {
                        cell.Label.text = string.Empty;
                    }
                    if (cell.PencilLabel != null) cell.PencilLabel.text = string.Empty;
                }

                // High contrast overrides
                if (_accessibility != null && _accessibility.IsHighContrast)
                {
                    if (color == ConflictColor)
                        color = HcError;
                    else if (color == SelectedColor || (_selectedRow == cell.Row && _selectedCol == cell.Col))
                        color = HcSelected;
                    else if (color == FogColor)
                        color = HcFog;
                    else
                        color = HcBackground;

                    cell.Label.color = new Color(0.05f, 0.05f, 0.05f, 1f);
                }

                // Fog: add "?" text for accessibility
                if (overlay != null && overlay.IsFogged(cell.Row, cell.Col) && value == 0)
                {
                    var fogLabel = _accessibility?.GetFogCellLabel();
                    if (fogLabel != null)
                    {
                        cell.Label.text = fogLabel;
                        cell.Label.color = _accessibility.IsHighContrast
                            ? Color.white
                            : new Color(0.5f, 0.5f, 0.6f, 0.5f);
                    }
                }

                cell.Image.color = color;
                UpdateCellBorders(board, cell);
            }

            UpdateNumpadSolvedState(board);
        }

        private void UpdateCellBorders(SudokuBoard board, CellView cell)
        {
            var map = board.RegionMap;
            if (map == null)
            {
                return;
            }

            var row = cell.Row;
            var col = cell.Col;
            var region = map[row, col];
            var size = board.Size;
            var borderColor = new Color(1f, 0.78f, 0.24f, 0.72f);

            var top = row == 0 || map[row - 1, col] != region;
            var bottom = row == size - 1 || map[row + 1, col] != region;
            var left = col == 0 || map[row, col - 1] != region;
            var right = col == size - 1 || map[row, col + 1] != region;

            if (cell.BorderTop != null) cell.BorderTop.color = top ? borderColor : new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
            if (cell.BorderBottom != null) cell.BorderBottom.color = bottom ? borderColor : new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
            if (cell.BorderLeft != null) cell.BorderLeft.color = left ? borderColor : new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
            if (cell.BorderRight != null) cell.BorderRight.color = right ? borderColor : new Color(borderColor.r, borderColor.g, borderColor.b, 0f);

            // Apply killer cage border color on top of region borders
            if (_cageBorderEdges.Count > 0)
            {
                if (cell.BorderTop != null && IsCageBorderEdge(row, col, size, 0)) cell.BorderTop.color = KillerCageBorder;
                if (cell.BorderBottom != null && IsCageBorderEdge(row, col, size, 1)) cell.BorderBottom.color = KillerCageBorder;
                if (cell.BorderLeft != null && IsCageBorderEdge(row, col, size, 2)) cell.BorderLeft.color = KillerCageBorder;
                if (cell.BorderRight != null && IsCageBorderEdge(row, col, size, 3)) cell.BorderRight.color = KillerCageBorder;
            }
        }

        private string BuildPencilMarkup(SudokuBoard board, int row, int col)
        {
            var set = board.GetPencilSet(row, col);
            if (set == null || set.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (var value = 1; value <= board.Size; value++)
            {
                if (!set.Contains(value))
                {
                    continue;
                }

                var valid = IsPencilMarkValid(board, row, col, value);
                if (!valid)
                {
                    sb.Append("<color=#FF6A6A>");
                }

                sb.Append(value);
                sb.Append(' ');

                if (!valid)
                {
                    sb.Append("</color>");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static bool IsPencilMarkValid(SudokuBoard board, int row, int col, int value)
        {
            return SudokuValidator.IsMoveValid(board, row, col, value);
        }

        private static Color GetLineColor(LineType type)
        {
            return type switch
            {
                LineType.GermanWhispers => GermanWhispersLineColor,
                LineType.DutchWhispers => DutchWhispersLineColor,
                LineType.Parity => ParityLineColor,
                LineType.Renban => RenbanLineColor,
                LineType.Palindrome => PalindromeLineColor,
                LineType.Thermo => ThermoLineColor,
                LineType.BetweenLines => BetweenLinesColor,
                _ => GermanWhispersLineColor
            };
        }

        private void UpdateNumpadSolvedState(SudokuBoard board)
        {
            if (board == null)
            {
                return;
            }

            var size = board.Size;
            var counts = new int[size + 1];
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var value = board.GetCell(row, col);
                    if (value >= 1 && value <= size)
                    {
                        counts[value]++;
                    }
                }
            }

            for (var i = 0; i < _numpadButtons.Count; i++)
            {
                var value = i + 1;
                if (value > size)
                {
                    continue;
                }

                var btn = _numpadButtons[i];
                if (btn == null)
                {
                    continue;
                }

                var solved = counts[value] >= size;
                btn.interactable = !solved;
                var image = btn.GetComponent<Image>();
                if (image != null)
                {
                    image.color = solved ? new Color(0.28f, 0.28f, 0.28f, 0.85f) : new Color(0.19f, 0.30f, 0.20f, 1f);
                }
            }
        }

        private static Color ComputeBaseCellColor(SudokuBoard board, int row, int col, bool given)
        {
            var regionMap = board.RegionMap;
            if (regionMap != null)
            {
                var regionId = regionMap[row, col];
                var alternate = (regionId & 1) == 0;
                if (given)
                {
                    return alternate ? new Color(0.25f, 0.36f, 0.25f, 1f) : new Color(0.19f, 0.30f, 0.20f, 1f);
                }

                return alternate ? new Color(0.15f, 0.22f, 0.16f, 1f) : new Color(0.11f, 0.17f, 0.12f, 1f);
            }

            return given ? GivenColor : EmptyColor;
        }

        private static bool HasConflict(SudokuBoard board, int row, int col)
        {
            var value = board.GetCell(row, col);
            if (value == 0)
            {
                return false;
            }

            var size = board.Size;

            for (var c = 0; c < size; c++)
            {
                if (c != col && board.GetCell(row, c) == value)
                {
                    return true;
                }
            }

            for (var r = 0; r < size; r++)
            {
                if (r != row && board.GetCell(r, col) == value)
                {
                    return true;
                }
            }

            var regionMap = board.RegionMap;
            if (regionMap != null)
            {
                var region = regionMap[row, col];
                for (var r = 0; r < size; r++)
                {
                    for (var c = 0; c < size; c++)
                    {
                        if ((r != row || c != col) && regionMap[r, c] == region && board.GetCell(r, c) == value)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsAntiknightActive()
        {
            var mods = runMapController?.Run?.CurrentLevelConfig?.ActiveModifiers;
            if (mods == null) return false;
            for (var i = 0; i < mods.Count; i++)
                if (mods[i] == BossModifierId.Antiknight) return true;
            return false;
        }

        private static bool IsKnightMoveFrom(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (fromRow < 0 || fromCol < 0) return false;
            for (var i = 0; i < KnightOffsets.Length; i++)
            {
                if (fromRow + KnightOffsets[i].Dr == toRow && fromCol + KnightOffsets[i].Dc == toCol)
                    return true;
            }
            return false;
        }

        private void SaveAndQuit()
        {
            var tutorialMode = runMapController?.Run?.RunState?.TutorialMode == true;
            if (!tutorialMode)
            {
                Debug.Log("PrototypeRunScreenController: Save & Quit triggered.");
                runMapController?.Run?.OnQuitRequested();
                Debug.Log("PrototypeRunScreenController: Auto-save requested via RunDirector.OnQuitRequested().");
            }
            else
            {
                Debug.Log("PrototypeRunScreenController: Tutorial quit triggered (no save).");
                PlayerPrefs.SetInt(ReturnTutorialProgressPrefKey, 1);
                PlayerPrefs.Save();
            }

            if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }

        private void ApplyResumeScreenState()
        {
            var run = runMapController?.Run;
            if (run?.RunState == null)
            {
                return;
            }

            _resumeScreenApplied = true;

            if (run.RunState.TutorialMode)
            {
                ShowSudoku();
                BuildOrRefreshSudokuBoard();
                return;
            }

            if (run.RunState.CurrentNodeIndex <= 0)
            {
                ShowPathOverview();
                RefreshPathOverview();
                return;
            }

            if (run.CurrentBoard != null && run.CurrentLevelState != null && !run.CurrentLevelState.PuzzleComplete)
            {
                ShowSudoku();
                BuildOrRefreshSudokuBoard();
                return;
            }

            ShowPathOverview();
            RefreshPathOverview();
        }

        private void TryCompleteTutorialAndReturn()
        {
            if (_tutorialCompletionProcessed)
            {
                return;
            }

            _tutorialCompletionProcessed = true;

            var run = runMapController?.Run;
            if (run == null)
            {
                SaveAndQuit();
                return;
            }

            run.CompleteLevelAndGrantRewards();

            if (run.TryConsumeLastCompletedTutorialSetup(out var completedSetup))
            {
                PersistTutorialCompletion(completedSetup);
            }
            else if (run.ActiveTutorialSetup != null)
            {
                PersistTutorialCompletion(run.ActiveTutorialSetup);
            }
            else
            {
                var fallback = BuildSolvedTutorialSetupFromRun(run);
                if (fallback != null)
                {
                    PersistTutorialCompletion(fallback);
                }
            }

            PlayerPrefs.SetInt(ReturnTutorialProgressPrefKey, 1);
            PlayerPrefs.Save();
            SaveAndQuit();
        }

        private void UpdateQuitButtonLabels()
        {
            var tutorialMode = runMapController?.Run?.RunState?.TutorialMode == true;
            var label = tutorialMode ? "Quit (No Save)" : "Save & Quit (Q)";
            SetButtonLabel(saveQuitPathButton, label);
            SetButtonLabel(saveQuitSudokuButton, label);
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private void PersistTutorialCompletion(TutorialSetupConfig completedSetup)
        {
            if (completedSetup == null)
            {
                return;
            }

            var save = new SaveFileService();
            var profile = new ProfileService();
            if (save.TryLoadProfile(out var envelope))
            {
                profile.ApplyEnvelope(envelope);
            }

            var tutorialProgress = new TutorialProgressService(profile.TutorialProgress);
            tutorialProgress.MarkCompleted(completedSetup);

            var updated = new SaveFileEnvelope
            {
                PlayerProfile = new ProfileSaveData { Options = profile.Options },
                MetaProgress = profile.Meta,
                TutorialProgress = profile.TutorialProgress,
                Statistics = profile.Stats,
                Mastery = profile.Mastery,
                Completion = profile.Completion
            };

            save.SaveProfile(updated);
        }

        private void PersistRunResult(RunResult result)
        {
            if (result == null || result.TutorialMode) return;
            var save = new SaveFileService();
            var profile = new ProfileService();
            if (save.TryLoadProfile(out var envelope))
            {
                profile.ApplyEnvelope(envelope);
            }
            profile.RecordRunAndGetNewUnlocks(result);

            // Sync item discoveries from this run's inventory into the ItemCodex
            var inventory = runMapController?.Run?.RunState?.Inventory;
            if (inventory != null && profile.Meta?.ItemCodex != null)
            {
                var today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
                for (var inv = 0; inv < inventory.Count; inv++)
                {
                    var itemId = inventory[inv].Id;
                    if (string.IsNullOrWhiteSpace(itemId)) continue;

                    ItemCodexEntry codexEntry = null;
                    for (var ci = 0; ci < profile.Meta.ItemCodex.Entries.Count; ci++)
                    {
                        if (profile.Meta.ItemCodex.Entries[ci].ItemID == itemId)
                        {
                            codexEntry = profile.Meta.ItemCodex.Entries[ci];
                            break;
                        }
                    }

                    if (codexEntry == null) continue;

                    if (!codexEntry.Discovered)
                    {
                        codexEntry.Discovered = true;
                        codexEntry.DiscoveredDate = today;
                        UnityEngine.Debug.Log($"[ItemCodex] Discovered: {itemId} ({codexEntry.Name})");
                    }

                    codexEntry.TimesPicked++;
                    if (result.Victory) codexEntry.TimesWon++;
                    if (result.GardenDepthReached > codexEntry.BestRunDepth)
                        codexEntry.BestRunDepth = result.GardenDepthReached;
                }
            }

            var updated = new SaveFileEnvelope
            {
                PlayerProfile = new ProfileSaveData { Options = profile.Options },
                MetaProgress = profile.Meta,
                TutorialProgress = profile.TutorialProgress,
                Statistics = profile.Stats,
                Mastery = profile.Mastery,
                Completion = profile.Completion
            };
            save.SaveProfile(updated);
        }

        private static TutorialSetupConfig BuildSolvedTutorialSetupFromRun(RunDirector run)
        {
            if (run?.CurrentLevelConfig == null || run.RunState == null)
            {
                return null;
            }

            var setup = new TutorialSetupConfig
            {
                BoardSize = run.CurrentLevelConfig.BoardSize,
                Stars = run.CurrentLevelConfig.Stars,
                ResourceMode = run.RunState.TutorialResourceMode
            };

            var modifiers = run.CurrentLevelConfig.ActiveModifiers;
            for (var i = 0; i < modifiers.Count; i++)
            {
                setup.SelectedModifiers.Add(modifiers[i]);
            }

            return setup;
        }

        private void HandleShopNode()
        {
            var run = runMapController?.Run;
            if (run?.RunState == null)
            {
                return;
            }

            BuildShopPanel();
            _shopOffers.Clear();
            _shopOffers.AddRange(run.BuildShopOffers());
            _pendingShopOfferId = string.Empty;
            _awaitingShopReplacement = false;
            RefreshShopSummaryText();

            if (_shopHoverText != null)
            {
                _shopHoverText.text = "Hover an offer to inspect details.";
            }

            RebuildShopButtons();
            if (_shopPanel != null)
            {
                _shopPanel.SetActive(true);
            }

            SetStatus("Shop opened.");
        }

        private void BuildShopPanel()
        {
            if (_shopPanel != null || pathOverviewPanel == null)
            {
                return;
            }

            var panelRect = pathOverviewPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            _shopPanel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            _shopPanel.transform.SetParent(panelRect, false);
            var rect = _shopPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.18f, 0.16f);
            rect.anchorMax = new Vector2(0.82f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = _shopPanel.GetComponent<Image>();
            image.color = new Color(0.08f, 0.10f, 0.12f, 0.96f);

            var title = new GameObject("ShopTitle", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(_shopPanel.transform, false);
            title.rectTransform.anchorMin = new Vector2(0.06f, 0.88f);
            title.rectTransform.anchorMax = new Vector2(0.94f, 0.98f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 22;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.90f, 0.62f, 1f);
            title.text = "Garden Shop";

            _shopSummaryText = new GameObject("ShopSummary", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            _shopSummaryText.transform.SetParent(_shopPanel.transform, false);
            _shopSummaryText.rectTransform.anchorMin = new Vector2(0.06f, 0.69f);
            _shopSummaryText.rectTransform.anchorMax = new Vector2(0.94f, 0.86f);
            _shopSummaryText.rectTransform.offsetMin = Vector2.zero;
            _shopSummaryText.rectTransform.offsetMax = Vector2.zero;
            _shopSummaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _shopSummaryText.fontSize = 14;
            _shopSummaryText.alignment = TextAnchor.UpperLeft;
            _shopSummaryText.color = new Color(0.92f, 0.95f, 0.96f, 1f);

            _shopHoverText = new GameObject("ShopHover", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            _shopHoverText.transform.SetParent(_shopPanel.transform, false);
            _shopHoverText.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
            _shopHoverText.rectTransform.anchorMax = new Vector2(0.94f, 0.22f);
            _shopHoverText.rectTransform.offsetMin = Vector2.zero;
            _shopHoverText.rectTransform.offsetMax = Vector2.zero;
            _shopHoverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _shopHoverText.fontSize = 13;
            _shopHoverText.alignment = TextAnchor.UpperLeft;
            _shopHoverText.color = new Color(0.95f, 0.93f, 0.85f, 1f);

            // High contrast overrides
            if (_accessibility is { IsHighContrast: true })
            {
                image.color = HcPanelBg;
                title.color = HcPanelText;
                title.fontStyle = FontStyle.Bold;
                _shopSummaryText.color = HcPanelText;
                _shopSummaryText.fontStyle = FontStyle.Bold;
                _shopHoverText.color = HcPanelText;
            }

            _shopPanel.SetActive(false);
        }

        private void RebuildShopButtons()
        {
            if (_shopPanel == null)
            {
                return;
            }

            for (var i = _shopPanel.transform.childCount - 1; i >= 0; i--)
            {
                var child = _shopPanel.transform.GetChild(i);
                if (child.name.StartsWith("ShopChoice_", StringComparison.Ordinal) ||
                    child.name.StartsWith("ShopReplace_", StringComparison.Ordinal) ||
                    child.name == "ShopSkip" ||
                    child.name == "ShopReroll")
                {
                    Destroy(child.gameObject);
                }
            }

            if (_awaitingShopReplacement)
            {
                var inventory = runMapController?.Run?.RunState?.Inventory;
                if (inventory == null)
                {
                    return;
                }

                if (_shopSummaryText != null)
                {
                    _shopSummaryText.text = "Inventory is full. Choose a slot to replace, or cancel.";
                }

                for (var i = 0; i < inventory.Count; i++)
                {
                    var replaceButton = BuildPanelButton(_shopPanel.transform, $"ShopReplace_{i}", new Vector2(0.05f + ((i % 3) * 0.32f), 0.48f - ((i / 3) * 0.18f)), new Vector2(0.26f, 0.14f), new Color(0.28f, 0.22f, 0.18f, 0.95f), anchorBased: true);
                    var idx = i;
                    replaceButton.onClick.AddListener(() => PurchaseOfferReplacing(idx));

                    var label = replaceButton.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.alignment = TextAnchor.MiddleCenter;
                        label.fontSize = 13;
                        label.text = $"Replace {DescribeItemShort(inventory[i])}";
                    }
                }

                var cancel = BuildPanelButton(_shopPanel.transform, "ShopSkip", new Vector2(0.33f, 0.24f), new Vector2(0.34f, 0.12f), new Color(0.25f, 0.26f, 0.28f, 0.95f), anchorBased: true);
                cancel.onClick.AddListener(() =>
                {
                    _awaitingShopReplacement = false;
                    _pendingShopOfferId = string.Empty;
                    RebuildShopButtons();
                });

                var cancelLabel = cancel.GetComponentInChildren<Text>();
                if (cancelLabel != null)
                {
                    cancelLabel.text = "Cancel";
                }

                return;
            }

            var cardCount = Mathf.Min(3, _shopOffers.Count);
            for (var i = 0; i < cardCount; i++)
            {
                var offer = _shopOffers[i];
                var btn = BuildPanelButton(_shopPanel.transform, $"ShopChoice_{i}", new Vector2(0.08f + (i * 0.29f), 0.38f), new Vector2(0.26f, 0.30f), new Color(0.17f, 0.26f, 0.32f, 0.95f), anchorBased: true);
                var idx = i;
                btn.onClick.AddListener(() => TryBuyShopOffer(idx));

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(btn.transform, false);
                icon.rectTransform.anchorMin = new Vector2(0.06f, 0.40f);
                icon.rectTransform.anchorMax = new Vector2(0.38f, 0.92f);
                icon.rectTransform.offsetMin = Vector2.zero;
                icon.rectTransform.offsetMax = Vector2.zero;
                icon.sprite = GetItemSprite(offer.Item);
                icon.preserveAspect = true;
                icon.color = Color.white;

                var label = btn.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.rectTransform.anchorMin = new Vector2(0.40f, 0.08f);
                    label.rectTransform.anchorMax = new Vector2(0.96f, 0.94f);
                    label.rectTransform.offsetMin = new Vector2(2f, 2f);
                    label.rectTransform.offsetMax = new Vector2(-2f, -2f);
                    label.alignment = TextAnchor.UpperLeft;
                    label.fontSize = 12;
                    label.text = $"{DescribeItemShort(offer.Item)}\n{offer.Price}g";
                }

                var trigger = btn.gameObject.AddComponent<EventTrigger>();
                trigger.triggers = new List<EventTrigger.Entry>();

                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ =>
                {
                    if (_shopHoverText != null)
                    {
                        _shopHoverText.text = DescribeShopOffer(offer);
                    }
                });
                trigger.triggers.Add(enter);

                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ =>
                {
                    if (_shopHoverText != null)
                    {
                        _shopHoverText.text = "Hover an offer to inspect details.";
                    }
                });
                trigger.triggers.Add(exit);
            }

            var skip = BuildPanelButton(_shopPanel.transform, "ShopSkip", new Vector2(0.33f, 0.24f), new Vector2(0.34f, 0.12f), new Color(0.25f, 0.26f, 0.28f, 0.95f), anchorBased: true);
            skip.onClick.AddListener(() => CloseShopPanel(false));
            var skipLabel = skip.GetComponentInChildren<Text>();
            if (skipLabel != null)
            {
                skipLabel.text = "Take Nothing";
            }

            var reroll = BuildPanelButton(_shopPanel.transform, "ShopReroll", new Vector2(0.08f, 0.24f), new Vector2(0.22f, 0.12f), new Color(0.24f, 0.22f, 0.31f, 0.95f), anchorBased: true);
            reroll.onClick.AddListener(TryRerollShopOffers);
            var rerollLabel = reroll.GetComponentInChildren<Text>();
            if (rerollLabel != null)
            {
                rerollLabel.fontSize = 11;
                rerollLabel.text = BuildShopRerollLabel();
            }
        }

        private void TryBuyShopOffer(int offerIndex)
        {
            var run = runMapController?.Run;
            var state = run?.RunState;
            if (run == null || state == null || offerIndex < 0 || offerIndex >= _shopOffers.Count)
            {
                return;
            }

            var offer = _shopOffers[offerIndex];
            if (offer == null)
            {
                return;
            }

            if (!offer.IsRelic && offer.Item != null && state.Inventory.Count >= state.ItemSlots)
            {
                _awaitingShopReplacement = true;
                _pendingShopOfferId = offer.OfferId;
                RebuildShopButtons();
                return;
            }

            var purchased = run.TryPurchaseShopOffer(offer.OfferId);
            if (!purchased)
            {
                SetStatus("Cannot buy offer.");
                return;
            }

            SetStatus($"Purchased {DescribeItemShort(offer.Item)}.");
            _runAudio?.PlayShopPurchase();
            CloseShopPanel(true);
        }

        private void PurchaseOfferReplacing(int replaceIndex)
        {
            var run = runMapController?.Run;
            if (run == null || string.IsNullOrWhiteSpace(_pendingShopOfferId))
            {
                return;
            }

            if (!run.TryPurchaseShopOfferReplacingSlot(_pendingShopOfferId, replaceIndex))
            {
                SetStatus("Replacement purchase failed.");
                return;
            }

            SetStatus("Purchased by replacing an inventory slot.");
            _runAudio?.PlayShopPurchase();
            CloseShopPanel(true);
        }

        private void TryRerollShopOffers()
        {
            var run = runMapController?.Run;
            if (run == null)
            {
                return;
            }

            if (!run.TryRerollShopOffers(out var spentGold, out var usedToken))
            {
                SetStatus("Cannot reroll shop offers.");
                return;
            }

            _shopOffers.Clear();
            _shopOffers.AddRange(run.CurrentShopOffers);
            _runAudio?.PlayShopReroll();
            SetStatus(usedToken ? "Shop rerolled using a reroll token." : $"Shop rerolled for {spentGold}g.");
            RefreshShopSummaryText();
            RebuildShopButtons();
            RefreshHud();
        }

        private void RefreshShopSummaryText()
        {
            var state = runMapController?.Run?.RunState;
            if (_shopSummaryText == null || state == null)
            {
                return;
            }

            _shopSummaryText.text =
                "Shop Node\n" +
                $"Gold: {state.CurrentGold}  |  Reroll Tokens: {state.RerollTokens}\n" +
                $"{BuildShopRerollLabel()}  |  Choose one offer or skip.";
        }

        private string BuildShopRerollLabel()
        {
            var run = runMapController?.Run;
            if (run == null)
            {
                return "Reroll";
            }

            return run.HasShopRerollTokenAvailable()
                ? "Reroll (1 token)"
                : $"Reroll ({run.GetShopRerollGoldCostPreview()}g)";
        }

        private void CloseShopPanel(bool purchased)
        {
            _awaitingShopReplacement = false;
            _pendingShopOfferId = string.Empty;
            _shopOffers.Clear();

            if (_shopPanel != null)
            {
                _shopPanel.SetActive(false);
            }

            if (purchased)
            {
                _pathOverlayMessage = "Shop purchase complete.";
            }
            else
            {
                _pathOverlayMessage = "Shop skipped.";
                SetStatus("Skipped shop.");
                _runAudio?.PlayPathAdvance();
            }

            RefreshPathOverview();
        }

        private void HandleRestNode()
        {
            var state = runMapController?.Run?.RunState;
            if (state == null)
            {
                return;
            }

            var healAmount = Mathf.Max(1, Mathf.CeilToInt(state.MaxHP * 0.10f));
            var before = state.CurrentHP;
            state.CurrentHP = Mathf.Min(state.MaxHP, state.CurrentHP + healAmount);
            var recovered = Mathf.Max(0, state.CurrentHP - before);

            _runAudio?.PlayRestHeal();
            _pathOverlayMessage = $"Rest Node\nRecovered {recovered} HP ({before} -> {state.CurrentHP}).";
            SetStatus("Rested and recovered HP.");
        }

        private void HandleEventNode()
        {
            var run = runMapController?.Run;
            if (run == null)
            {
                return;
            }

            var runEvent = runMapController.OpenEventNode();
            if (runEvent == null || runEvent.Options.Count == 0)
            {
                _pathOverlayMessage = "Event Node\nNo scripted event found. (Design docs define richer event logic.)";
                SetStatus("Event node has no active scripted payload yet.");
                return;
            }

            var option = runEvent.Options[0];
            var resolved = runMapController.ChooseEventOption(option.OptionId);
            _pathOverlayMessage =
                $"Event Node\n{runEvent.Prompt}\n" +
                $"Chosen: {option.Label} ({option.Tradeoff})\n" +
                (resolved ? "Outcome applied." : "Outcome failed requirements.");
            SetStatus(resolved ? "Event resolved." : "Event option failed.");
        }

        private void HandleRelicNode()
        {
            var state = runMapController?.Run?.RunState;
            if (state == null)
            {
                return;
            }

            // Roll relic via RunDirector (single relic slot)
            var run = runMapController?.Run;
            if (run != null)
            {
                var node = run.GetCurrentNode();
                var isRisk = node != null && node.IsRiskPath;
                var (offered, needsChoice) = run.RollRelicNodeReward(isRisk);
                if (!needsChoice)
                {
                    _runAudio?.PlayRelicPickup();
                    _pathOverlayMessage = $"Relic: {Economy.RelicService.GetName(offered.Id)}\n{Economy.RelicService.GetDescription(offered.Id)}";
                    SetStatus("Relic acquired.");
                }
                else
                {
                    _offeredRelic = offered;
                    _awaitingRelicChoice = true;
                    BuildRelicChoicePanel(state, offered);
                    SetStatus("Choose which relic to keep.");
                }
            }
        }

        private void BuildRelicChoicePanel(RunState state, RelicInstance offered)
        {
            if (_relicChoicePanel != null)
                Destroy(_relicChoicePanel);

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _relicChoicePanel = new GameObject("RelicChoicePanel", typeof(RectTransform), typeof(Image));
            _relicChoicePanel.transform.SetParent(canvas.transform, false);
            var panelRect = _relicChoicePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.20f);
            panelRect.anchorMax = new Vector2(0.85f, 0.80f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImg = _relicChoicePanel.GetComponent<Image>();
            panelImg.color = new Color(0.10f, 0.08f, 0.06f, 0.96f);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelRect, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.85f);
            titleRect.anchorMax = new Vector2(0.95f, 0.97f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = "Relic Found — Choose One to Keep";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.90f, 0.80f, 0.55f, 1f);

            // Current relic card (left)
            var current = state.HeldRelic;
            BuildRelicCard(panelRect, "CurrentRelic",
                new Vector2(0.04f, 0.18f), new Vector2(0.48f, 0.82f),
                "CURRENT",
                Economy.RelicService.GetName(current.Id),
                $"Tier {(int)current.Tier + 1}\n{Economy.RelicService.GetDescription(current.Id)}",
                new Color(0.22f, 0.30f, 0.22f, 1f),
                () => ConfirmRelicChoice(current));

            // Offered relic card (right)
            BuildRelicCard(panelRect, "OfferedRelic",
                new Vector2(0.52f, 0.18f), new Vector2(0.96f, 0.82f),
                "NEW",
                Economy.RelicService.GetName(offered.Id),
                $"Tier {(int)offered.Tier + 1}\n{Economy.RelicService.GetDescription(offered.Id)}",
                new Color(0.30f, 0.22f, 0.15f, 1f),
                () => ConfirmRelicChoice(offered));

            // Hint text
            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            hintGo.transform.SetParent(panelRect, false);
            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.10f, 0.03f);
            hintRect.anchorMax = new Vector2(0.90f, 0.15f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            var hintText = hintGo.GetComponent<Text>();
            hintText.text = "Click a relic to keep it. The other will be discarded.";
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintText.fontSize = 13;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.70f, 0.70f, 0.70f, 1f);
        }

        private void BuildRelicCard(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            string badge, string relicName, string description,
            Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var cardGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            cardGo.transform.SetParent(parent, false);
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = anchorMin;
            cardRect.anchorMax = anchorMax;
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            var cardImg = cardGo.GetComponent<Image>();
            cardImg.color = bgColor;

            // Badge (CURRENT / NEW)
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Text));
            badgeGo.transform.SetParent(cardRect, false);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.05f, 0.85f);
            badgeRect.anchorMax = new Vector2(0.95f, 0.98f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;
            var badgeText = badgeGo.GetComponent<Text>();
            badgeText.text = badge;
            badgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            badgeText.fontSize = 11;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = new Color(0.65f, 0.65f, 0.55f, 1f);
            badgeText.fontStyle = FontStyle.Bold;

            // Relic name
            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(cardRect, false);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.60f);
            nameRect.anchorMax = new Vector2(0.95f, 0.85f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameText = nameGo.GetComponent<Text>();
            nameText.text = relicName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 16;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = new Color(0.95f, 0.88f, 0.65f, 1f);

            // Description
            var descGo = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            descGo.transform.SetParent(cardRect, false);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0.05f);
            descRect.anchorMax = new Vector2(0.95f, 0.58f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;
            var descText = descGo.GetComponent<Text>();
            descText.text = description;
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 12;
            descText.alignment = TextAnchor.UpperCenter;
            descText.color = Color.white;
            descText.verticalOverflow = VerticalWrapMode.Overflow;

            var btn = cardGo.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
        }

        private void ConfirmRelicChoice(RelicInstance chosen)
        {
            _awaitingRelicChoice = false;

            var run = runMapController?.Run;
            if (run != null)
            {
                run.AcceptRelicChoice(chosen);
                _runAudio?.PlayRelicPickup();
                _pathOverlayMessage = $"Kept relic: {Economy.RelicService.GetName(chosen.Id)}\n{Economy.RelicService.GetDescription(chosen.Id)}";
                SetStatus("Relic chosen.");
            }

            if (_relicChoicePanel != null)
            {
                Destroy(_relicChoicePanel);
                _relicChoicePanel = null;
            }

            _offeredRelic = null;
        }

        private readonly List<BossModifierId> _bossGateSelected = new();
        private List<BossModifierId> _bossGateOptions;
        private int _bossGateRequiredChoices;

        private void ShowBossGateChoice()
        {
            if (_awaitingBossGateChoice) return;

            var run = runMapController?.Run;
            if (run == null) return;

            var state = run.RunState;
            var floor = state.CurrentFloor;

            RunGraphService.GetBossModifierCounts(floor, out var optionsShown, out var playerChooses);

            // Roll fresh options excluding any already chosen this run
            var bossService = run.BossServiceInstance;
            _bossGateOptions = bossService != null
                ? bossService.RollBossOptions(optionsShown, state.ChosenBossModifiers)
                : new List<BossModifierId>();

            if (_bossGateOptions.Count == 0)
            {
                ChoosePath(false);
                return;
            }

            _bossGateRequiredChoices = Mathf.Min(playerChooses, _bossGateOptions.Count);
            _bossGateSelected.Clear();
            _awaitingBossGateChoice = true;

            BuildBossGatePanel(state);
        }

        private void BuildBossGatePanel(RunState state)
        {
            if (_bossGateChoicePanel != null)
                Destroy(_bossGateChoicePanel);

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _bossGateChoicePanel = new GameObject("BossGateChoicePanel", typeof(RectTransform), typeof(Image));
            _bossGateChoicePanel.transform.SetParent(canvas.transform, false);
            var panelRect = _bossGateChoicePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.10f);
            panelRect.anchorMax = new Vector2(0.85f, 0.90f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImg = _bossGateChoicePanel.GetComponent<Image>();
            var hc = _accessibility is { IsHighContrast: true };
            panelImg.color = hc ? HcPanelBg : new Color(0.10f, 0.08f, 0.06f, 0.96f);

            var floor = state.CurrentFloor;
            var theme = Data.FloorThemeData.Get(floor);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelRect, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = $"Boss Gate — {theme.Name}\nChoose {_bossGateRequiredChoices} Modifier(s)   [{_bossGateSelected.Count}/{_bossGateRequiredChoices} selected]";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = hc ? HcPanelText : new Color(0.90f, 0.80f, 0.55f, 1f);
            if (hc) titleText.fontStyle = FontStyle.Bold;

            // Option cards
            var count = _bossGateOptions.Count;
            var columns = Mathf.Min(count, 3);
            var rows = Mathf.CeilToInt((float)count / columns);

            for (var i = 0; i < count; i++)
            {
                var modifier = _bossGateOptions[i];
                var seen = state.SeenBossModifiers.Contains(modifier);
                var modName = seen ? FormatModifierName(modifier) : "???";
                var modDesc = seen ? GetBossModifierDescription(modifier) : "Unknown modifier";
                var isSelected = _bossGateSelected.Contains(modifier);

                var col = i % columns;
                var row = i / columns;
                var xMin = 0.03f + col * (0.94f / columns);
                var xMax = xMin + (0.94f / columns) - 0.02f;
                var yMax = 0.85f - row * (0.72f / rows);
                var yMin = yMax - (0.72f / rows) + 0.02f;

                var btnGo = new GameObject($"BossChoice_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(panelRect, false);
                var btnRect = btnGo.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(xMin, yMin);
                btnRect.anchorMax = new Vector2(xMax, yMax);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = isSelected
                    ? new Color(0.40f, 0.55f, 0.25f, 1f)
                    : new Color(0.25f, 0.18f, 0.12f, 1f);

                var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                lblGo.transform.SetParent(btnRect, false);
                var lblRect = lblGo.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.offsetMin = new Vector2(4f, 4f);
                lblRect.offsetMax = new Vector2(-4f, -4f);
                var lblText = lblGo.GetComponent<Text>();
                lblText.text = isSelected ? $"\u2713 {modName}\n{modDesc}" : $"{modName}\n{modDesc}";
                lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lblText.fontSize = 12;
                lblText.alignment = TextAnchor.MiddleCenter;
                lblText.color = Color.white;
                lblText.verticalOverflow = VerticalWrapMode.Overflow;

                var captured = modifier;
                var btn = btnGo.GetComponent<Button>();
                btn.onClick.AddListener(() => ToggleBossGateModifier(captured, state));
            }

            // Confirm button (only active when enough selected)
            var confirmGo = new GameObject("ConfirmBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmGo.transform.SetParent(panelRect, false);
            var confirmRect = confirmGo.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.30f, 0.02f);
            confirmRect.anchorMax = new Vector2(0.70f, 0.10f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            var confirmImg = confirmGo.GetComponent<Image>();
            var canConfirm = _bossGateSelected.Count >= _bossGateRequiredChoices;
            confirmImg.color = canConfirm ? new Color(0.45f, 0.65f, 0.25f, 1f) : new Color(0.30f, 0.30f, 0.30f, 0.60f);

            var confirmLblGo = new GameObject("ConfirmLbl", typeof(RectTransform), typeof(Text));
            confirmLblGo.transform.SetParent(confirmRect, false);
            var confirmLblRect = confirmLblGo.GetComponent<RectTransform>();
            confirmLblRect.anchorMin = Vector2.zero;
            confirmLblRect.anchorMax = Vector2.one;
            confirmLblRect.offsetMin = Vector2.zero;
            confirmLblRect.offsetMax = Vector2.zero;
            var confirmLblText = confirmLblGo.GetComponent<Text>();
            confirmLblText.text = canConfirm ? "Confirm Selection" : $"Select {_bossGateRequiredChoices - _bossGateSelected.Count} more";
            confirmLblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            confirmLblText.fontSize = 16;
            confirmLblText.alignment = TextAnchor.MiddleCenter;
            confirmLblText.color = Color.white;

            var confirmBtn = confirmGo.GetComponent<Button>();
            confirmBtn.interactable = canConfirm;
            confirmBtn.onClick.AddListener(() => ConfirmBossGateSelection(state));
        }

        private void ToggleBossGateModifier(BossModifierId modifier, RunState state)
        {
            if (_bossGateSelected.Contains(modifier))
            {
                _bossGateSelected.Remove(modifier);
            }
            else if (_bossGateSelected.Count < _bossGateRequiredChoices)
            {
                _bossGateSelected.Add(modifier);
            }

            // Rebuild panel to reflect selection state
            BuildBossGatePanel(state);
        }

        private void ConfirmBossGateSelection(RunState state)
        {
            _awaitingBossGateChoice = false;

            if (state != null)
            {
                // Apply all selected modifiers
                for (var i = 0; i < _bossGateSelected.Count; i++)
                {
                    state.ChosenBossModifiers.Add(_bossGateSelected[i]);
                    state.SeenBossModifiers.Add(_bossGateSelected[i]);
                }

                // Keep legacy single-modifier field for backwards compat
                if (_bossGateSelected.Count > 0)
                    state.ChosenBossModifier = _bossGateSelected[0];
            }

            if (_bossGateChoicePanel != null)
            {
                Destroy(_bossGateChoicePanel);
                _bossGateChoicePanel = null;
            }

            ChoosePath(false);
        }

        private bool HandleBossVictoryOrAdvance()
        {
            var run = runMapController?.Run;
            if (run == null) return false;

            var state = run.RunState;
            var isLastFloor = state.CurrentFloor >= state.TotalFloors - 1;

            if (isLastFloor)
            {
                // Final boss defeated — trigger victory
                ShowGameOver(run);
                return true;
            }

            // Advance to next floor
            runMapController.AdvanceToNextGarden();
            var theme = Data.FloorThemeData.Get(state.CurrentFloor);
            SetStatus($"Garden Cleared! Entering {theme.Name}...");
            _lastLaneRenderSignature = int.MinValue;
            ShowPathOverview();
            RefreshPathOverview();
            return true;
        }

        private static string FormatOffer(ShopOffer offer)
        {
            if (offer == null) return "Unknown offer";

            if (offer.IsRelic && offer.RelicOffer != null)
                return $"{Economy.RelicService.GetName(offer.RelicOffer.Id)} - {offer.Price}g";

            if (offer.Item != null)
                return $"{Items.ItemService.GetItemName(offer.Item.Type)} ({offer.Item.Rarity}) - {offer.Price}g";

            return $"Offer - {offer.Price}g";
        }

        private void RebuildInventoryBadges()
        {
            var state = runMapController?.Run?.RunState;
            if (state == null || pathOverviewPanel == null)
            {
                return;
            }

            var panelRect = pathOverviewPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            if (_inventoryBadgeRoot == null)
            {
                var rootGo = new GameObject("InventoryBadgeRoot", typeof(RectTransform));
                rootGo.transform.SetParent(panelRect, false);
                _inventoryBadgeRoot = rootGo.GetComponent<RectTransform>();
                _inventoryBadgeRoot.anchorMin = new Vector2(0.02f, 0.82f);
                _inventoryBadgeRoot.anchorMax = new Vector2(0.98f, 0.91f);
                _inventoryBadgeRoot.offsetMin = Vector2.zero;
                _inventoryBadgeRoot.offsetMax = Vector2.zero;
            }

            for (var i = _inventoryBadgeRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_inventoryBadgeRoot.GetChild(i).gameObject);
            }

            var slot = 0;
            for (var i = 0; i < state.Inventory.Count; i++)
            {
                var item = state.Inventory[i];
                CreateHoverBadge(
                    DescribeItemShort(item),
                    DescribeItem(item),
                    slot++,
                    new Color(0.19f, 0.35f, 0.24f, 0.92f),
                    GetItemSprite(item));
            }

            if (state.HasRelic && state.HeldRelic != null)
            {
                var relicName = Economy.RelicService.GetName(state.HeldRelic.Id);
                var relicDesc = Economy.RelicService.GetDescription(state.HeldRelic.Id);
                CreateHoverBadge(
                    relicName,
                    relicDesc,
                    slot++,
                    new Color(0.30f, 0.24f, 0.15f, 0.92f),
                    Resources.Load<Sprite>("GeneratedIcons/icon_relic_pedestal") ?? GetFallbackSprite());
            }
        }

        private void CreateHoverBadge(string label, string description, int slot, Color color, Sprite icon)
        {
            if (_inventoryBadgeRoot == null)
            {
                return;
            }

            var go = new GameObject($"Badge_{slot}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
            go.transform.SetParent(_inventoryBadgeRoot, false);
            var rect = go.GetComponent<RectTransform>();

            var w = 108f;
            var h = 24f;
            var cols = 8;
            var row = slot / cols;
            var col = slot % cols;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(col * (w + 6f), -row * (h + 4f));

            var image = go.GetComponent<Image>();
            image.color = color;

            var iconImage = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            iconImage.transform.SetParent(go.transform, false);
            iconImage.rectTransform.anchorMin = new Vector2(0.02f, 0.12f);
            iconImage.rectTransform.anchorMax = new Vector2(0.24f, 0.88f);
            iconImage.rectTransform.offsetMin = Vector2.zero;
            iconImage.rectTransform.offsetMax = Vector2.zero;
            iconImage.sprite = icon ?? GetFallbackSprite();
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.26f, 0f);
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(2f, 1f);
            textRect.offsetMax = new Vector2(-4f, -1f);

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.95f, 0.96f, 0.92f, 1f);
            text.text = label;

            var trigger = go.GetComponent<EventTrigger>();
            trigger.triggers = new List<EventTrigger.Entry>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                _hoverInfo = description;
                RefreshPathOverview();
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                _hoverInfo = string.Empty;
                RefreshPathOverview();
            });
            trigger.triggers.Add(exit);
        }

        private static string DescribeItem(ItemInstance item)
        {
            if (item == null)
            {
                return "Unknown item";
            }

            var name = Items.ItemService.GetItemName(item.Type);
            var effect = Items.ItemService.GetItemDescription(item.Type, item.Rarity);
            var charges = item.IsInfinite ? "Infinite" : item.Charges.ToString();
            return $"{name} ({item.Rarity})\nCharges: {charges}\n{effect}";
        }

        // DescribeRelic and ShortRelicName archived — use RelicService.GetName/GetDescription directly

        // EnsureClassToken, TrySetClassTokenTargetForNode, UpdateClassTokenPosition removed
        // Player position is now managed by PlayerIconController

        private void ShowRewardScreen()
        {
            if (runMapController == null)
            {
                return;
            }

            var currentMode = runMapController.Run?.RunState?.Mode ?? GameMode.GardenRun;
            if (currentMode == GameMode.EndlessZen || currentMode == GameMode.SpiritTrials)
            {
                ShowPathOverview();
                _awaitingRewardChoice = false;
                SetStatus("Puzzle cleared. Continue to next level.");
                RefreshPathOverview();
                return;
            }

            ShowPathOverview();
            BuildRewardPanel();
            _pendingRewardSlots.Clear();

            if (!runMapController.TryClaimCurrentPuzzleRewards(out var goldEarned, out var slots, out var reason))
            {
                _awaitingRewardChoice = false;
                SetStatus(string.IsNullOrWhiteSpace(reason) ? "Rewards unavailable." : reason);
                HideRewardPanel();
                RefreshPathOverview();
                return;
            }

            _awaitingRewardChoice = true;
            _pendingRewardSlots.AddRange(slots);

            var runRef = runMapController?.Run;
            var cfg = runRef?.CurrentLevelConfig;
            var lvlState = runRef?.CurrentLevelState;
            var xpBreak = XpService.CalculateTile(
                cfg?.BoardSize ?? 9,
                cfg?.Stars ?? 1,
                cfg?.ActiveModifiers?.Count ?? 0,
                cfg?.IsBoss ?? false,
                (lvlState?.Mistakes ?? 0) == 0);

            var summary = new StringBuilder();
            summary.AppendLine("Puzzle Complete!");
            summary.AppendLine($"Gold gained: +{goldEarned}");
            summary.AppendLine(string.Empty);
            summary.AppendLine($"XP Gained:    +{xpBreak.TotalXp}");
            if (xpBreak.IsBoss)
                summary.AppendLine("  Boss ×2");
            if (xpBreak.PerfectBonus > 0)
                summary.AppendLine($"  Perfect Solve:   +{xpBreak.PerfectBonus}");
            summary.AppendLine(string.Empty);
            summary.AppendLine($"Item slots rolled: {_pendingRewardSlots.Count}");
            summary.AppendLine("Choose one slot reward.");
            if (_rewardSummaryText != null)
            {
                _rewardSummaryText.text = summary.ToString().TrimEnd();
            }

            if (_pendingRewardSlots.Count == 0)
            {
                _awaitingRewardChoice = false;
                HideRewardPanel();
                SetStatus("Rewards granted. Choose next path tile.");
                RefreshPathOverview();
                return;
            }

            RebuildRewardButtons();
            SetStatus("Choose your reward.");
            RefreshPathOverview();
        }

        private void BuildRewardPanel()
        {
            if (_rewardPanel != null || pathOverviewPanel == null)
            {
                return;
            }

            var panelRect = pathOverviewPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            _rewardPanel = new GameObject("RewardPanel", typeof(RectTransform), typeof(Image));
            _rewardPanel.transform.SetParent(panelRect, false);
            var rect = _rewardPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.24f, 0.22f);
            rect.anchorMax = new Vector2(0.76f, 0.72f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = _rewardPanel.GetComponent<Image>();
            image.color = new Color(0.06f, 0.10f, 0.08f, 0.96f);

            var title = new GameObject("RewardTitle", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(_rewardPanel.transform, false);
            title.rectTransform.anchorMin = new Vector2(0.06f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(0.94f, 0.96f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 22;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.96f, 0.88f, 0.56f, 1f);
            title.text = "Reward";

            _rewardSummaryText = new GameObject("RewardSummary", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            _rewardSummaryText.transform.SetParent(_rewardPanel.transform, false);
            _rewardSummaryText.rectTransform.anchorMin = new Vector2(0.08f, 0.56f);
            _rewardSummaryText.rectTransform.anchorMax = new Vector2(0.92f, 0.80f);
            _rewardSummaryText.rectTransform.offsetMin = Vector2.zero;
            _rewardSummaryText.rectTransform.offsetMax = Vector2.zero;
            _rewardSummaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _rewardSummaryText.fontSize = 14;
            _rewardSummaryText.alignment = TextAnchor.UpperLeft;
            _rewardSummaryText.color = new Color(0.94f, 0.95f, 0.92f, 1f);

            _rewardHoverText = new GameObject("RewardHover", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            _rewardHoverText.transform.SetParent(_rewardPanel.transform, false);
            _rewardHoverText.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
            _rewardHoverText.rectTransform.anchorMax = new Vector2(0.92f, 0.18f);
            _rewardHoverText.rectTransform.offsetMin = Vector2.zero;
            _rewardHoverText.rectTransform.offsetMax = Vector2.zero;
            _rewardHoverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _rewardHoverText.fontSize = 13;
            _rewardHoverText.alignment = TextAnchor.UpperLeft;
            _rewardHoverText.color = new Color(0.95f, 0.93f, 0.85f, 1f);
            _rewardHoverText.text = "Hover a reward to inspect details.";

            // High contrast overrides
            if (_accessibility is { IsHighContrast: true })
            {
                image.color = HcPanelBg;
                title.color = HcPanelText;
                title.fontStyle = FontStyle.Bold;
                _rewardSummaryText.color = HcPanelText;
                _rewardSummaryText.fontStyle = FontStyle.Bold;
                _rewardHoverText.color = HcPanelText;
            }

            _rewardPanel.SetActive(false);
        }

        private void RebuildRewardButtons()
        {
            if (_rewardPanel == null)
            {
                return;
            }

            for (var i = _rewardPanel.transform.childCount - 1; i >= 0; i--)
            {
                var child = _rewardPanel.transform.GetChild(i);
                if (child.name.StartsWith("RewardChoice_", StringComparison.Ordinal))
                {
                    Destroy(child.gameObject);
                }
            }

            if (_awaitingRewardReplacement)
            {
                var inventory = runMapController?.Run?.RunState?.Inventory;
                if (inventory == null) return;

                if (_rewardSummaryText != null)
                {
                    _rewardSummaryText.text = "Inventory is full. Choose an item to replace, or cancel.";
                }

                var cols = Mathf.Clamp(inventory.Count, 1, 3);
                for (var i = 0; i < inventory.Count; i++)
                {
                    var col = i % cols;
                    var row = i / cols;
                    var xMin = 0.08f + (col * 0.29f);
                    var yMax = 0.50f - (row * 0.18f);

                    var btnGo = new GameObject($"RewardChoice_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGo.transform.SetParent(_rewardPanel.transform, false);
                    var rect = btnGo.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(xMin, yMax - 0.14f);
                    rect.anchorMax = new Vector2(xMin + 0.26f, yMax);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    btnGo.GetComponent<Image>().color = new Color(0.28f, 0.22f, 0.18f, 0.95f);

                    var idx = i;
                    btnGo.GetComponent<Button>().onClick.AddListener(() => ClaimRewardReplacing(idx));

                    var lbl = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    lbl.transform.SetParent(btnGo.transform, false);
                    lbl.rectTransform.anchorMin = Vector2.zero;
                    lbl.rectTransform.anchorMax = Vector2.one;
                    lbl.rectTransform.offsetMin = Vector2.zero;
                    lbl.rectTransform.offsetMax = Vector2.zero;
                    lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    lbl.fontSize = 12;
                    lbl.alignment = TextAnchor.MiddleCenter;
                    lbl.color = Color.white;
                    lbl.text = $"Replace {DescribeItemShort(inventory[i])}";
                }

                var cancelGo = new GameObject("RewardChoice_cancel", typeof(RectTransform), typeof(Image), typeof(Button));
                cancelGo.transform.SetParent(_rewardPanel.transform, false);
                var cancelRect = cancelGo.GetComponent<RectTransform>();
                cancelRect.anchorMin = new Vector2(0.33f, 0.08f);
                cancelRect.anchorMax = new Vector2(0.67f, 0.20f);
                cancelRect.offsetMin = Vector2.zero;
                cancelRect.offsetMax = Vector2.zero;
                cancelGo.GetComponent<Image>().color = new Color(0.25f, 0.26f, 0.28f, 0.95f);
                cancelGo.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _awaitingRewardReplacement = false;
                    RebuildRewardButtons();
                });
                var cancelLbl = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                cancelLbl.transform.SetParent(cancelGo.transform, false);
                cancelLbl.rectTransform.anchorMin = Vector2.zero;
                cancelLbl.rectTransform.anchorMax = Vector2.one;
                cancelLbl.rectTransform.offsetMin = Vector2.zero;
                cancelLbl.rectTransform.offsetMax = Vector2.zero;
                cancelLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                cancelLbl.fontSize = 13;
                cancelLbl.alignment = TextAnchor.MiddleCenter;
                cancelLbl.color = Color.white;
                cancelLbl.text = "Cancel";

                _rewardPanel.SetActive(true);
                return;
            }

            var columns = Mathf.Clamp(_pendingRewardSlots.Count, 1, 3);
            var rows = Mathf.CeilToInt(_pendingRewardSlots.Count / (float)columns);
            var spacingX = 0.03f;
            var spacingY = 0.04f;
            var availableWidth = 0.84f;
            var availableHeight = 0.28f;
            var cellWidth = (availableWidth - ((columns - 1) * spacingX)) / columns;
            var cellHeight = (availableHeight - ((Mathf.Max(1, rows) - 1) * spacingY)) / Mathf.Max(1, rows);

            for (var i = 0; i < _pendingRewardSlots.Count; i++)
            {
                var slot = _pendingRewardSlots[i];
                var button = new GameObject($"RewardChoice_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(_rewardPanel.transform, false);

                var rect = button.GetComponent<RectTransform>();
                var col = i % columns;
                var row = i / columns;
                var xMin = 0.08f + (col * (cellWidth + spacingX));
                var yMax = 0.50f - (row * (cellHeight + spacingY));
                var yMin = yMax - cellHeight;

                rect.anchorMin = new Vector2(xMin, yMin);
                rect.anchorMax = new Vector2(xMin + cellWidth, yMax);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var image = button.GetComponent<Image>();
                image.color = new Color(0.18f, 0.29f, 0.22f, 0.95f);

                var btn = button.GetComponent<Button>();
                var index = i;
                btn.onClick.AddListener(() => ClaimReward(index));

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button.transform, false);
                icon.rectTransform.anchorMin = new Vector2(0.06f, 0.18f);
                icon.rectTransform.anchorMax = new Vector2(0.34f, 0.88f);
                icon.rectTransform.offsetMin = Vector2.zero;
                icon.rectTransform.offsetMax = Vector2.zero;
                icon.sprite = GetRewardSlotSprite(slot);
                icon.preserveAspect = true;
                icon.color = Color.white;

                var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                label.transform.SetParent(button.transform, false);
                label.rectTransform.anchorMin = new Vector2(0.36f, 0.10f);
                label.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
                label.rectTransform.offsetMin = new Vector2(2f, 2f);
                label.rectTransform.offsetMax = new Vector2(-2f, -2f);
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.fontSize = 12;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.text = DescribeRollSlotShort(slot);

                var trigger = button.AddComponent<EventTrigger>();
                trigger.triggers = new List<EventTrigger.Entry>();

                var startScale = Vector3.one * 0.78f;
                var hoverScale = Vector3.one * 1.06f;
                button.transform.localScale = startScale;
                StartCoroutine(AnimateRewardSlotScale(button.transform as RectTransform, 0.06f * i, startScale, Vector3.one));

                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ =>
                {
                    button.transform.localScale = hoverScale;
                    if (_rewardHoverText != null)
                    {
                        _rewardHoverText.text = DescribeRollSlot(slot);
                    }
                });
                trigger.triggers.Add(enter);

                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ =>
                {
                    button.transform.localScale = Vector3.one;
                    if (_rewardHoverText != null)
                    {
                        _rewardHoverText.text = "Hover a reward to inspect details.";
                    }
                });
                trigger.triggers.Add(exit);
            }

            var run = runMapController?.Run;
            if (run != null && !run.RunState.TutorialMode)
            {
                var rerollCost = FormulaService.RerollCost(run.RunState.RerollsThisRun);
                var canAfford = run.RunState.CurrentGold >= rerollCost;

                var rerollGo = new GameObject("RewardChoice_reroll", typeof(RectTransform), typeof(Image), typeof(Button));
                rerollGo.transform.SetParent(_rewardPanel.transform, false);
                var rerollRect = rerollGo.GetComponent<RectTransform>();
                rerollRect.anchorMin = new Vector2(0.08f, 0.08f);
                rerollRect.anchorMax = new Vector2(0.42f, 0.18f);
                rerollRect.offsetMin = Vector2.zero;
                rerollRect.offsetMax = Vector2.zero;
                rerollGo.GetComponent<Image>().color = canAfford
                    ? new Color(0.22f, 0.36f, 0.28f, 0.95f)
                    : new Color(0.30f, 0.22f, 0.22f, 0.60f);

                var rerollBtn = rerollGo.GetComponent<Button>();
                rerollBtn.interactable = canAfford;
                rerollBtn.onClick.AddListener(RerollRewards);

                var rerollLbl = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                rerollLbl.transform.SetParent(rerollGo.transform, false);
                rerollLbl.rectTransform.anchorMin = Vector2.zero;
                rerollLbl.rectTransform.anchorMax = Vector2.one;
                rerollLbl.rectTransform.offsetMin = Vector2.zero;
                rerollLbl.rectTransform.offsetMax = Vector2.zero;
                rerollLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                rerollLbl.fontSize = 14;
                rerollLbl.alignment = TextAnchor.MiddleCenter;
                rerollLbl.color = Color.white;
                rerollLbl.text = $"Reroll ({rerollCost}g)";
            }

            // Skip button
            var skipGo = new GameObject("RewardChoice_skip", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(_rewardPanel.transform, false);
            var skipRect = skipGo.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.58f, 0.08f);
            skipRect.anchorMax = new Vector2(0.92f, 0.18f);
            skipRect.offsetMin = Vector2.zero;
            skipRect.offsetMax = Vector2.zero;
            skipGo.GetComponent<Image>().color = new Color(0.25f, 0.26f, 0.28f, 0.95f);
            skipGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                _pendingRewardSlots.Clear();
                _awaitingRewardChoice = false;
                _awaitingRewardReplacement = false;
                HideRewardPanel();

                var currentNode = GetCurrentNode();
                if (currentNode != null && currentNode.Type == NodeType.Boss)
                {
                    if (HandleBossVictoryOrAdvance()) return;
                }

                SetStatus("Reward skipped. Choose next path tile.");
                _lastLaneRenderSignature = int.MinValue;
                ShowPathOverview();
                RefreshPathOverview();
            });
            var skipLbl = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            skipLbl.transform.SetParent(skipGo.transform, false);
            skipLbl.rectTransform.anchorMin = Vector2.zero;
            skipLbl.rectTransform.anchorMax = Vector2.one;
            skipLbl.rectTransform.offsetMin = Vector2.zero;
            skipLbl.rectTransform.offsetMax = Vector2.zero;
            skipLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipLbl.fontSize = 14;
            skipLbl.alignment = TextAnchor.MiddleCenter;
            skipLbl.color = Color.white;
            skipLbl.text = "Skip";

            _rewardPanel.SetActive(true);
        }

        private void RerollRewards()
        {
            var run = runMapController?.Run;
            if (run == null) return;

            if (!run.TryRerollItemSlots(_pendingRewardSlots))
            {
                SetStatus("Not enough gold to reroll.");
                return;
            }

            _runAudio?.PlayShopPurchase();
            RebuildRewardButtons();
            RefreshPathOverview();
        }

        private void ClaimReward(int slotIndex)
        {
            var run = runMapController?.Run;
            if (run == null)
            {
                return;
            }

            if (_pendingRewardSlots.Count > 0 && slotIndex >= 0 && slotIndex < _pendingRewardSlots.Count)
            {
                var slot = _pendingRewardSlots[slotIndex];
                var state = run.RunState;
                if (!slot.IsNothing && slot.RolledItem != null && state.Inventory.Count >= state.ItemSlots)
                {
                    _awaitingRewardReplacement = true;
                    _pendingRewardSlotIndex = slotIndex;
                    RebuildRewardButtons();
                    return;
                }

                run.PickRolledSlot(_pendingRewardSlots, slotIndex);
            }

            _pendingRewardSlots.Clear();
            _awaitingRewardChoice = false;
            HideRewardPanel();
            _runAudio?.PlayRewardClaim();

            // After boss: advance to next garden or trigger victory.
            var currentNode = GetCurrentNode();
            if (currentNode != null && currentNode.Type == NodeType.Boss)
            {
                if (HandleBossVictoryOrAdvance()) return;
            }

            SetStatus("Reward claimed. Choose next path tile.");
            _lastLaneRenderSignature = int.MinValue;
            RefreshPathOverview();
        }

        private void ClaimRewardReplacing(int replaceIndex)
        {
            var run = runMapController?.Run;
            if (run == null) return;

            run.PickRolledSlotReplacingIndex(_pendingRewardSlots, _pendingRewardSlotIndex, replaceIndex);

            _awaitingRewardReplacement = false;
            _pendingRewardSlots.Clear();
            _awaitingRewardChoice = false;
            HideRewardPanel();
            _runAudio?.PlayRewardClaim();

            var currentNode = GetCurrentNode();
            if (currentNode != null && currentNode.Type == NodeType.Boss)
            {
                if (HandleBossVictoryOrAdvance()) return;
            }

            SetStatus("Reward claimed. Choose next path tile.");
            _lastLaneRenderSignature = int.MinValue;
            RefreshPathOverview();
        }

        private void HideRewardPanel()
        {
            if (_rewardPanel != null)
            {
                _rewardPanel.SetActive(false);
            }
        }

        private static string DescribeRollSlot(ItemRollSlot slot)
        {
            if (slot == null)
            {
                return "Unknown";
            }

            if (slot.IsNothing)
            {
                return "Nothing\nSkip this reward. No gold, no item.";
            }

            if (slot.RolledItem != null)
            {
                var itemName = Items.ItemService.GetItemName(slot.RolledItem.Type);
                var effect = Items.ItemService.GetItemDescription(slot.RolledItem.Type, slot.RolledItem.Rarity);
                return $"{itemName} ({slot.RolledItem.Rarity})\n{effect}";
            }

            return "Locked";
        }

        private static string DescribeRollSlotShort(ItemRollSlot slot)
        {
            if (slot == null)
            {
                return "Unknown";
            }

            if (slot.IsNothing)
            {
                return "Nothing";
            }

            if (slot.RolledItem != null)
            {
                return $"{slot.RolledItem.Type}\n{slot.RolledItem.Rarity}";
            }

            return "Locked";
        }

        private static string DescribeItemShort(ItemInstance item)
        {
            if (item == null)
            {
                return "Unknown";
            }

            return $"{item.Type} ({item.Rarity})";
        }

        private static string DescribeShopOffer(ShopOffer offer)
        {
            if (offer == null)
                return "Unknown offer";

            if (offer.IsRelic && offer.RelicOffer != null)
            {
                var name = Economy.RelicService.GetName(offer.RelicOffer.Id);
                var desc = Economy.RelicService.GetDescription(offer.RelicOffer.Id);
                return $"{name} ({offer.RelicOffer.Tier})\n{desc}\nPrice: {offer.Price}g";
            }

            var item = offer.Item;
            if (item == null)
                return $"Offer price: {offer.Price}g";

            var itemName = Items.ItemService.GetItemName(item.Type);
            var effect = Items.ItemService.GetItemDescription(item.Type, item.Rarity);
            return $"{itemName} ({item.Rarity})\n{effect}\nPrice: {offer.Price}g";
        }

        private Button BuildPanelButton(Transform parent, string name, Vector2 anchorMin, Vector2 size, Color color, bool anchorBased = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            if (anchorBased)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = new Vector2(anchorMin.x + size.x, anchorMin.y + size.y);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMin;
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            var image = go.GetComponent<Image>();
            image.color = color;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.colorMultiplier = 1.2f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(go.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6f, 4f);
            label.rectTransform.offsetMax = new Vector2(-6f, -4f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 12;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.95f, 0.95f, 0.92f, 1f);
            label.text = name;

            return button;
        }

        private void EnsurePuzzleItemBar()
        {
            if (_puzzleItemBarRoot != null || sudokuPanel == null)
            {
                return;
            }

            var panelRect = sudokuPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            var root = new GameObject("PuzzleItemBar", typeof(RectTransform));
            root.transform.SetParent(panelRect, false);
            _puzzleItemBarRoot = root.GetComponent<RectTransform>();
            _puzzleItemBarRoot.anchorMin = new Vector2(0.03f, 0.14f);
            _puzzleItemBarRoot.anchorMax = new Vector2(0.20f, 0.46f);
            _puzzleItemBarRoot.offsetMin = Vector2.zero;
            _puzzleItemBarRoot.offsetMax = Vector2.zero;

            var hoverText = new GameObject("PuzzleItemHover", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            hoverText.transform.SetParent(panelRect, false);
            hoverText.rectTransform.anchorMin = new Vector2(0.03f, 0.03f);
            hoverText.rectTransform.anchorMax = new Vector2(0.28f, 0.12f);
            hoverText.rectTransform.offsetMin = Vector2.zero;
            hoverText.rectTransform.offsetMax = Vector2.zero;
            hoverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            hoverText.fontSize = 12;
            hoverText.alignment = TextAnchor.UpperLeft;
            hoverText.color = new Color(0.90f, 0.92f, 0.85f, 1f);
            hoverText.text = "Bag\nHover an item for details.";
            _puzzleItemHoverText = hoverText;
        }

        private int BuildPuzzleItemSignature()
        {
            unchecked
            {
                var state = runMapController?.Run?.RunState;
                if (state == null)
                {
                    return -1;
                }

                var hash = 29;
                hash = hash * 31 + state.Inventory.Count;
                hash = hash * 31 + state.ItemSlots;
                for (var i = 0; i < state.Inventory.Count; i++)
                {
                    var item = state.Inventory[i];
                    hash = hash * 31 + (item?.Id?.GetHashCode() ?? 0);
                    hash = hash * 31 + (item?.Charges ?? 0);
                    hash = hash * 31 + (int)(item?.Type ?? ItemType.Solver);
                    hash = hash * 31 + (int)(item?.Rarity ?? ItemRarity.Normal);
                }

                return hash;
            }
        }

        private void RebuildPuzzleItemBar()
        {
            if (sudokuPanel == null || !sudokuPanel.activeSelf)
            {
                return;
            }

            EnsurePuzzleItemBar();
            var state = runMapController?.Run?.RunState;
            if (_puzzleItemBarRoot == null || state == null)
            {
                return;
            }

            if (state.TutorialMode)
            {
                _puzzleItemBarRoot.gameObject.SetActive(false);
                return;
            }
            _puzzleItemBarRoot.gameObject.SetActive(true);

            var signature = BuildPuzzleItemSignature();
            if (signature == _lastPuzzleItemSignature)
            {
                return;
            }

            _lastPuzzleItemSignature = signature;

            for (var i = _puzzleItemBarRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_puzzleItemBarRoot.GetChild(i).gameObject);
            }

            for (var i = 0; i < state.Inventory.Count; i++)
            {
                var item = state.Inventory[i];
                var btn = BuildPanelButton(_puzzleItemBarRoot, $"PuzzleItem_{i}", new Vector2(0f, 1f), new Vector2(150f, 44f), new Color(0.18f, 0.26f, 0.20f, 0.95f));
                var btnRect = btn.GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    btnRect.anchoredPosition = new Vector2(0f, -i * 48f);
                }

                var idx = i;
                btn.onClick.AddListener(() => TryUsePuzzleItem(idx));

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(btn.transform, false);
                icon.rectTransform.anchorMin = new Vector2(0.04f, 0.14f);
                icon.rectTransform.anchorMax = new Vector2(0.30f, 0.86f);
                icon.rectTransform.offsetMin = Vector2.zero;
                icon.rectTransform.offsetMax = Vector2.zero;
                icon.sprite = GetItemSprite(item);
                icon.preserveAspect = true;
                icon.color = Color.white;

                var label = btn.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.alignment = TextAnchor.MiddleLeft;
                    label.fontSize = 11;
                    label.text = $"{item.Type}\n{item.Rarity} x{item.Charges}";
                    label.rectTransform.anchorMin = new Vector2(0.34f, 0.06f);
                    label.rectTransform.anchorMax = new Vector2(0.98f, 0.94f);
                    label.rectTransform.offsetMin = Vector2.zero;
                    label.rectTransform.offsetMax = Vector2.zero;
                }

                var trigger = btn.gameObject.AddComponent<EventTrigger>();
                trigger.triggers = new List<EventTrigger.Entry>();

                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ =>
                {
                    if (_puzzleItemHoverText != null)
                    {
                        _puzzleItemHoverText.text = DescribeItem(item);
                    }
                });
                trigger.triggers.Add(enter);

                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ =>
                {
                    if (_puzzleItemHoverText != null)
                    {
                        _puzzleItemHoverText.text = "Bag\nHover an item for details.";
                    }
                });
                trigger.triggers.Add(exit);
            }
        }

        private void TryUsePuzzleItem(int index)
        {
            var run = runMapController?.Run;
            var board = run?.CurrentBoard;
            if (run == null || board == null)
            {
                return;
            }

            if (_selectedRow < 0 || _selectedCol < 0)
            {
                if (!TryFindFirstEditableCell(board, out _selectedRow, out _selectedCol))
                {
                    SetStatus("Select a cell before using an item.");
                    return;
                }
            }

            // Cancel pending SilkFan if using a different item
            if (run.IsSilkFanPending)
            {
                run.CancelSilkFan();
                SetStatus("Silk Fan cancelled.");
            }

            var usedType = run.RunState != null && index >= 0 && index < run.RunState.Inventory.Count && run.RunState.Inventory[index] != null
                ? run.RunState.Inventory[index].Type
                : ItemType.Solver;

            if (!run.TryUseInventoryItemAt(index, _selectedRow, _selectedCol, out var message))
            {
                // SilkFan phase 1: pending state was set even though used=false
                if (run.IsSilkFanPending)
                {
                    SetStatus(message);
                    return;
                }
                SetStatus(string.IsNullOrWhiteSpace(message) ? "Item usage failed." : message);
                return;
            }

            if (usedType == ItemType.Finder)
            {
                CaptureFinderHighlights(run);
            }

            SetStatus(message);
            _runAudio?.PlayItemUse();
            RenderBoard(board);
            RefreshHud();
            RefreshSolveButtonState();
            _lastPuzzleItemSignature = int.MinValue;
            RebuildPuzzleItemBar();
        }

        private IEnumerator AnimateRewardSlotScale(RectTransform rect, float delay, Vector3 from, Vector3 to)
        {
            if (rect == null)
            {
                yield break;
            }

            // Reduce Motion: skip animation entirely
            if (_accessibility != null && _accessibility.IsReduceMotion)
            {
                rect.localScale = to;
                yield break;
            }

            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            var duration = AnimationHelper.RewardSlotDuration;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rect.localScale = Vector3.LerpUnclamped(from, to, AnimationHelper.Ease(t, AnimationHelper.EaseType.EaseOut));
                yield return null;
            }

            if (rect != null)
            {
                rect.localScale = to;
            }
        }

        private void CaptureFinderHighlights(RunDirector run)
        {
            _finderHighlightCells.Clear();
            if (run?.LastFinderHints == null)
            {
                return;
            }

            for (var i = 0; i < run.LastFinderHints.Count; i++)
            {
                _finderHighlightCells.Add(run.LastFinderHints[i]);
            }

            if (_finderHighlightCells.Count > 0)
            {
                _finderHighlightUntil = Time.unscaledTime + 2.2f;
            }
        }

        private bool ContainsFinderHighlight(int row, int col)
        {
            for (var i = 0; i < _finderHighlightCells.Count; i++)
            {
                if (_finderHighlightCells[i].Row == row && _finderHighlightCells[i].Col == col)
                {
                    return true;
                }
            }

            return false;
        }

        private static string ItemTypeToIconName(ItemType type)
        {
            return type switch
            {
                ItemType.Solver => "icon_scroll_graph",
                ItemType.Finder => "icon_compass_of_order",
                ItemType.InkWell => "icon_ink_save",
                ItemType.MeditationStone => "icon_stone_altar",
                ItemType.WindChime => "icon_wind_bell",
                ItemType.PatternScroll => "icon_language_scroll",
                ItemType.KoiReflection => "icon_golden_koi",
                ItemType.LanternOfClarity => "icon_garden_lantern",
                ItemType.GardenRake => "icon_pebble",
                ItemType.OfferingBowl => "icon_rice_bowl",
                ItemType.PruningShears => "icon_pebble",
                ItemType.ZenSandSifter => "icon_pebble",
                ItemType.GinkgoLeaf => "icon_golden_bloom",
                ItemType.RicePaperUmbrella => "icon_pebble",
                ItemType.TempleIncense => "icon_sacred_bell",
                ItemType.KoiDragonScale => "icon_golden_koi",
                ItemType.GoldenKintsugiJar => "icon_broken_mask",
                ItemType.SilkFan => "icon_pebble",
                _ => "icon_pebble"
            };
        }

        private static Sprite GetFallbackSprite()
        {
            return Resources.Load<Sprite>("GeneratedIcons/icon_pebble");
        }

        private static Sprite GetItemSprite(ItemInstance item)
        {
            if (item == null)
            {
                return GetFallbackSprite();
            }

            var iconPath = "GeneratedIcons/" + ItemTypeToIconName(item.Type);
            var sprite = Resources.Load<Sprite>(iconPath);
            return sprite ?? GetFallbackSprite();
        }

        private static Sprite GetRewardSlotSprite(ItemRollSlot slot)
        {
            if (slot == null)
            {
                return GetFallbackSprite();
            }

            if (slot.IsNothing)
            {
                return Resources.Load<Sprite>("GeneratedIcons/icon_coin_sakura") ?? GetFallbackSprite();
            }

            return GetItemSprite(slot.RolledItem);
        }

        private void ClearSelectedCell()
        {
            var board = runMapController?.Run?.CurrentBoard;
            if (board == null)
            {
                return;
            }

            if (_selectedRow < 0 || _selectedCol < 0)
            {
                SetStatus("Select a cell first.");
                return;
            }

            if (board.IsGiven(_selectedRow, _selectedCol))
            {
                SetStatus("Given cells cannot be cleared.");
                return;
            }

            board.ClearCell(_selectedRow, _selectedCol);
            _highlightValue = 0;
            SetStatus("Cleared selected cell.");
            RenderBoard(board);
            RefreshSolveButtonState();
        }

        private void SetStatus(string message)
        {
            if (sudokuStatusText != null)
            {
                sudokuStatusText.text = message;
            }
        }

        private void TriggerScreenShake(float px, float ms)
        {
            if (_accessibility != null && (!_accessibility.ScreenShakeEnabled || _accessibility.IsReduceMotion))
                return;
            var target = sudokuPanel != null ? sudokuPanel.transform : transform;
            StartCoroutine(AnimationHelper.ScreenShake(target, px, ms));
        }

        private void SquarePathActionButtons()
        {
            SetSquareButton(saveQuitPathButton);
        }

        private static void SetSquareButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            var height = Mathf.Max(44f, rect.rect.height);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, height);

            var colors = button.colors;
            colors.colorMultiplier = 1.45f;
            colors.fadeDuration = 0.07f;
            button.colors = colors;
        }

        private void RefreshHud()
        {
            var state = runMapController?.Run?.RunState;
            if (state == null)
            {
                return;
            }

            UpdateQuitButtonLabels();

            if (hpText != null)
            {
                hpText.text = $"HP: {state.CurrentHP}/{state.MaxHP}";
            }

            if (pencilText != null)
            {
                pencilText.text = $"Pencil: {state.CurrentPencil}/{state.MaxPencil}";
            }

            if (_hpBarFill == null || _pencilBarFill == null)
            {
                var panelRect = sudokuPanel != null ? sudokuPanel.GetComponent<RectTransform>() : null;
                if (panelRect != null)
                {
                    var hpBg = panelRect.Find("HpBarBg");
                    if (hpBg != null) _hpBarFill = hpBg.Find("HpBarFill")?.GetComponent<Image>();
                    var pencilBg = panelRect.Find("PencilBarBg");
                    if (pencilBg != null) _pencilBarFill = pencilBg.Find("PencilBarFill")?.GetComponent<Image>();
                }
            }

            if (_hpBarFill != null)
            {
                EnsureBarFillSprite(_hpBarFill);
                _hpBarFill.fillAmount = state.MaxHP > 0 ? Mathf.Clamp01(state.CurrentHP / (float)state.MaxHP) : 0f;

                // Colorblind: numeric HP overlay on bar
                if (_accessibility != null && _accessibility.IsColorblind)
                {
                    var hpOverlay = _hpBarFill.transform.Find("HpNumericOverlay");
                    if (hpOverlay == null)
                    {
                        var ovGo = new GameObject("HpNumericOverlay", typeof(RectTransform), typeof(Text));
                        ovGo.transform.SetParent(_hpBarFill.transform, false);
                        var ovRect = ovGo.GetComponent<RectTransform>();
                        ovRect.anchorMin = Vector2.zero;
                        ovRect.anchorMax = Vector2.one;
                        ovRect.offsetMin = Vector2.zero;
                        ovRect.offsetMax = Vector2.zero;
                        hpOverlay = ovGo.transform;
                    }
                    var ovTxt = hpOverlay.GetComponent<Text>();
                    if (ovTxt != null)
                    {
                        ovTxt.text = $"{state.CurrentHP}/{state.MaxHP}";
                        ovTxt.fontSize = _accessibility.ScaleFont(12);
                        ovTxt.alignment = TextAnchor.MiddleCenter;
                        ovTxt.color = Color.white;
                        ovTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        ovTxt.fontStyle = FontStyle.Bold;
                        ovTxt.raycastTarget = false;
                    }
                }
            }

            if (_pencilBarFill != null)
            {
                EnsureBarFillSprite(_pencilBarFill);
                _pencilBarFill.fillAmount = state.MaxPencil > 0 ? Mathf.Clamp01(state.CurrentPencil / (float)state.MaxPencil) : 0f;
            }

            if (_levelInfoText == null)
            {
                var panelRect = sudokuPanel != null ? sudokuPanel.GetComponent<RectTransform>() : null;
                _levelInfoText = panelRect != null
                    ? panelRect.Find("SudokuGameplayLevelInfo")?.GetComponent<Text>()
                    : null;
            }

            if (_levelInfoText != null)
            {
                var isTutorial = runMapController?.Run?.RunState != null && runMapController.Run.RunState.TutorialMode;
                _levelInfoText.text = isTutorial ? string.Empty : $"Depth: {state.Depth}  Floor: {state.CurrentFloor + 1}";
            }

            if (_modifiersLabel == null)
            {
                var panelRect2 = sudokuPanel != null ? sudokuPanel.GetComponent<RectTransform>() : null;
                _modifiersLabel = panelRect2 != null
                    ? panelRect2.Find("SudokuGameplayModifiers")?.GetComponent<Text>()
                    : null;
            }

            if (_modifiersLabel != null)
            {
                var mods = runMapController?.Run?.CurrentLevelConfig?.ActiveModifiers;
                if (mods != null && mods.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("Modifiers: ");
                    for (var m = 0; m < mods.Count; m++)
                    {
                        if (m > 0) sb.Append(", ");
                        sb.Append(mods[m]);
                    }
                    _modifiersLabel.text = sb.ToString();
                }
                else
                {
                    _modifiersLabel.text = string.Empty;
                }
            }

            RebuildPuzzleItemBar();
        }

        private void RefreshSolveButtonState()
        {
            if (solveSudokuButton == null)
            {
                return;
            }

            var board = runMapController?.Run?.CurrentBoard;
            var levelState = runMapController?.Run?.CurrentLevelState;
            var canEvaluate = board != null && board.IsComplete() && (levelState == null || !levelState.PuzzleComplete);
            solveSudokuButton.interactable = canEvaluate;
        }

        private void CheckForGameOver()
        {
            if (_gameOverShown)
            {
                return;
            }

            var run = runMapController?.Run;
            var state = run?.RunState;
            if (state == null || state.CurrentHP > 0)
            {
                return;
            }

            _gameOverShown = true;
            ShowGameOver(run);
        }

        private void ShowGameOver(RunDirector run)
        {
            _runAudio?.PlayGameOver();
            if (sudokuPanel != null)
            {
                sudokuPanel.SetActive(false);
            }

            if (pathOverviewPanel != null)
            {
                pathOverviewPanel.SetActive(false);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            var result = run.BuildRunResult(victory: false, bossPhaseReached: 0, secondsPlayed: 0);
            PersistRunResult(result);
            var presenter = new EndScreenPresenter();
            if (gameOverSummaryText != null)
            {
                gameOverSummaryText.text = presenter.BuildRunOverSummary(result);
            }

            if (gameOverDetailsText != null)
            {
                var goDetails = new StringBuilder();
                goDetails.AppendLine($"Class: {run.RunState.ClassId}");
                goDetails.AppendLine($"Depth reached: {run.RunState.Depth}");
                goDetails.AppendLine($"HP: {run.RunState.CurrentHP}/{run.RunState.MaxHP}");
                goDetails.AppendLine($"Pencil: {run.RunState.CurrentPencil}/{run.RunState.MaxPencil}");
                goDetails.AppendLine(string.Empty);

                // XP breakdown (new formula)
                var cfg = run.CurrentLevelConfig;
                var lvlState = run.CurrentLevelState;
                if (cfg != null)
                {
                    var xpBreak = XpService.CalculateTile(
                        cfg.BoardSize, cfg.Stars, cfg.ActiveModifiers?.Count ?? 0,
                        cfg.IsBoss, (lvlState?.Mistakes ?? 0) == 0);
                    goDetails.AppendLine("— XP Breakdown —");
                    goDetails.AppendLine($"Base: {xpBreak.BaseXp} × {xpBreak.StarMult:F1} = {xpBreak.TileXp}");
                    if (xpBreak.IsBoss) goDetails.AppendLine($"Boss ×1.5");
                    if (xpBreak.ModifierBonus > 0) goDetails.AppendLine($"Modifier Bonus: +{xpBreak.ModifierBonus}");
                    if (xpBreak.PerfectBonus > 0) goDetails.AppendLine($"Perfect Bonus: +{xpBreak.PerfectBonus}");
                    goDetails.AppendLine($"Total XP: +{xpBreak.TotalXp}");
                    goDetails.AppendLine(string.Empty);
                }
                else
                {
                    goDetails.AppendLine($"XP Gained: +{result.XpEarned}");
                    goDetails.AppendLine(string.Empty);
                }

                // Class-level progress after XP was applied
                var gSave = new SaveFileService();
                var gProfile = new ProfileService();
                if (gSave.TryLoadProfile(out var gEnv))
                {
                    gProfile.ApplyEnvelope(gEnv);
                    var prog   = gProfile.Meta.GardenProgression;

                    // Per-class level
                    ClassGardenProgressEntry classEntry = null;
                    if (prog != null)
                    {
                        for (var ci = 0; ci < prog.ClassEntries.Count; ci++)
                        {
                            if (prog.ClassEntries[ci].ClassId == run.RunState.ClassId)
                            {
                                classEntry = prog.ClassEntries[ci];
                                break;
                            }
                        }
                    }

                    if (classEntry != null)
                    {
                        var (classLevel, classProgressXp, classXpToNext) = XpService.DeriveLevel(classEntry.TotalXp);
                        var pctClass = classXpToNext > 0 ? Mathf.Clamp01((float)classProgressXp / classXpToNext) : 1f;
                        goDetails.AppendLine($"Class Level {classLevel}  ({Mathf.RoundToInt(pctClass * 100f)}%)");
                        goDetails.AppendLine($"XP {classProgressXp}/{classXpToNext}");
                        var nextUnlock = ClassGardenProgressionService.GetNextUnlock(run.RunState.ClassId, classLevel);
                        if (nextUnlock != "Max Level")
                            goDetails.AppendLine($"Next: {nextUnlock}");
                        else
                            goDetails.AppendLine("Class Max Level");
                    }
                }

                gameOverDetailsText.text = goDetails.ToString().TrimEnd();
            }

            SetStatus("Game Over");
        }

        private void BuildModifierOverlays()
        {
            ClearOverlayObjects();
            var run = runMapController?.Run;
            var overlay = run?.CurrentOverlayData;
            if (overlay == null || _cells.Count == 0) return;

            EnsureGridOverlayRoot();
            // Sync overlay position/size with grid in case board size changed
            _gridOverlayRoot.anchoredPosition = sudokuGridRoot.anchoredPosition;
            _gridOverlayRoot.sizeDelta = sudokuGridRoot.sizeDelta;

            var size = _boardSize;
            var grid = sudokuGridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null) return;

            var cellW = grid.cellSize.x;
            var cellH = grid.cellSize.y;
            var spaceX = grid.spacing.x;
            var spaceY = grid.spacing.y;
            var totalW = (cellW * size) + (spaceX * (size - 1));
            var totalH = (cellH * size) + (spaceY * (size - 1));

            // Refresh accessibility state for this render pass
            _accessibility?.Refresh();

            // Lines (whispers, parity, renban, palindrome, thermo, between lines)
            for (var li = 0; li < overlay.Lines.Count; li++)
            {
                var line = overlay.Lines[li];
                var lineColor = GetLineColor(line.Type);
                var dashPattern = _accessibility?.GetLineDashPattern(line.Type);
                for (var ci = 0; ci < line.Cells.Count - 1; ci++)
                {
                    var a = line.Cells[ci];
                    var b = line.Cells[ci + 1];
                    DrawLineBetweenCells(a, b, lineColor, 4f, cellW, cellH, spaceX, spaceY, totalW, totalH, dashPattern);
                }

                for (var ci = 0; ci < line.Cells.Count; ci++)
                {
                    var c = line.Cells[ci];
                    DrawCellDot(c, lineColor, 8f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                }

                // Thermo: large bulb circle at start cell
                if (line.Type == LineType.Thermo && line.Cells.Count > 0)
                {
                    DrawCellDot(line.Cells[0], lineColor, 18f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                    var thermoLabel = _accessibility?.GetThermoBulbLabel();
                    if (thermoLabel != null)
                        DrawOverlayLabel(line.Cells[0], thermoLabel, 14, cellW, cellH, spaceX, spaceY, totalW, totalH);
                }

                // Between Lines: hollow circles at both endpoints
                if (line.Type == LineType.BetweenLines && line.Cells.Count >= 2)
                {
                    DrawCellDot(line.Cells[0], lineColor, 14f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                    DrawCellDot(line.Cells[0], new Color(0f, 0f, 0f, 0f), 9f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                    DrawCellDot(line.Cells[line.Cells.Count - 1], lineColor, 14f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                    DrawCellDot(line.Cells[line.Cells.Count - 1], new Color(0f, 0f, 0f, 0f), 9f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                    var betweenLabel = _accessibility?.GetBetweenEndpointLabel();
                    if (betweenLabel != null)
                    {
                        DrawOverlayLabel(line.Cells[0], betweenLabel, 12, cellW, cellH, spaceX, spaceY, totalW, totalH);
                        DrawOverlayLabel(line.Cells[line.Cells.Count - 1], betweenLabel, 12, cellW, cellH, spaceX, spaceY, totalW, totalH);
                    }
                }
            }

            // Kropki dots — white = hollow white circle, black = filled black circle
            for (var di = 0; di < overlay.Dots.Count; di++)
            {
                var dot = overlay.Dots[di];
                var mr = (dot.CellA.Row + dot.CellB.Row) / 2f;
                var mc = (dot.CellA.Col + dot.CellB.Col) / 2f;
                var posX = mc * (cellW + spaceX) + cellW * 0.5f - totalW * 0.5f;
                var posY = -(mr * (cellH + spaceY) + cellH * 0.5f - totalH * 0.5f);
                if (dot.Type == DotType.White)
                {
                    // Hollow white circle: outer white ring + transparent inner
                    DrawOverlayCircle(posX, posY, 10f, WhiteDotColor);
                    DrawOverlayCircle(posX, posY,  6f, new Color(0f, 0f, 0f, 0f)); // punch out centre
                }
                else
                {
                    // Filled black circle
                    DrawOverlayCircle(posX, posY, 10f, BlackDotColor);
                }

                // Accessibility: add text label on dot
                var dotLabel = _accessibility?.GetDotLabel(dot.Type);
                if (dotLabel != null)
                    DrawOverlayTextAt(posX, posY, dotLabel, 11, Color.white);
            }

            // Killer cages — record cage edges and draw sum text
            _cageBorderEdges.Clear();
            for (var ci = 0; ci < overlay.Cages.Count; ci++)
            {
                var cage = overlay.Cages[ci];
                for (var c = 0; c < cage.Cells.Count; c++)
                {
                    var cell = cage.Cells[c];
                    RecordCageEdges(cage, cell.Row, cell.Col, size);
                }

                // Sum label on first cell (top-left of cage)
                var topLeft = cage.Cells[0];
                for (var c = 1; c < cage.Cells.Count; c++)
                {
                    var cc = cage.Cells[c];
                    if (cc.Row < topLeft.Row || (cc.Row == topLeft.Row && cc.Col < topLeft.Col))
                        topLeft = cc;
                }

                // Place cage sum: background Image parent + Text child (avoids multiple Graphic on same object)
                var tlX = topLeft.Col * (cellW + spaceX) - totalW * 0.5f;
                var tlY = totalH * 0.5f - topLeft.Row * (cellH + spaceY);

                var sumBgGo = new GameObject("CageSumBg", typeof(RectTransform), typeof(Image));
                sumBgGo.transform.SetParent(_gridOverlayRoot, false);
                var sumBgRect = sumBgGo.GetComponent<RectTransform>();
                sumBgRect.anchorMin = new Vector2(0.5f, 0.5f);
                sumBgRect.anchorMax = new Vector2(0.5f, 0.5f);
                sumBgRect.pivot = new Vector2(0f, 1f);
                sumBgRect.anchoredPosition = new Vector2(tlX, tlY);
                sumBgRect.sizeDelta = new Vector2(cellW * 0.46f, cellH * 0.30f);
                var sumBgImg = sumBgGo.GetComponent<Image>();
                sumBgImg.color = new Color(0f, 0f, 0f, 0.65f);
                sumBgImg.raycastTarget = false;

                var sumTextGo = new GameObject("CageSumText", typeof(RectTransform), typeof(Text));
                sumTextGo.transform.SetParent(sumBgGo.transform, false);
                var sumTextRect = sumTextGo.GetComponent<RectTransform>();
                sumTextRect.anchorMin = Vector2.zero;
                sumTextRect.anchorMax = Vector2.one;
                sumTextRect.offsetMin = Vector2.zero;
                sumTextRect.offsetMax = Vector2.zero;
                var sumText = sumTextGo.GetComponent<Text>();
                sumText.text = cage.Sum.ToString();
                sumText.fontSize = _accessibility?.ScaleFont(11) ?? 11;
                sumText.alignment = TextAnchor.MiddleCenter;
                sumText.color = new Color(0.90f, 0.15f, 0.12f, 1f);
                sumText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                sumText.raycastTarget = false;

                _overlayObjects.Add(sumBgGo);
            }

            // Red board border strips when Killer Cage is active
            if (overlay.Cages.Count > 0)
            {
                const float bt = 4f; // border thickness
                var redBorder = new Color(0.85f, 0.15f, 0.12f, 0.85f);
                // Top
                DrawBorderStrip("KCBorderTop",    _gridOverlayRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, bt), redBorder);
                // Bottom
                DrawBorderStrip("KCBorderBottom", _gridOverlayRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -bt), new Vector2(0f, 0f), redBorder);
                // Left
                DrawBorderStrip("KCBorderLeft",   _gridOverlayRoot, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-bt, 0f), new Vector2(0f, 0f), redBorder);
                // Right
                DrawBorderStrip("KCBorderRight",  _gridOverlayRoot, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(bt, 0f), redBorder);
            }

            // Arrows
            for (var ai = 0; ai < overlay.Arrows.Count; ai++)
            {
                var arrow = overlay.Arrows[ai];
                // Hollow circle: outer ring + inner cutout
                DrawCellDot(arrow.Circle, ArrowCircleColor, 18f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                DrawCellDot(arrow.Circle, ArrowCircleInnerColor, 11f, cellW, cellH, spaceX, spaceY, totalW, totalH);

                var arrowLabel = _accessibility?.GetArrowCircleLabel();
                if (arrowLabel != null)
                    DrawOverlayLabel(arrow.Circle, arrowLabel, 12, cellW, cellH, spaceX, spaceY, totalW, totalH);

                if (arrow.Path.Count > 0)
                    DrawLineBetweenCells(arrow.Circle, arrow.Path[0], ArrowPathColor, 3f, cellW, cellH, spaceX, spaceY, totalW, totalH);

                for (var pi = 0; pi < arrow.Path.Count - 1; pi++)
                    DrawLineBetweenCells(arrow.Path[pi], arrow.Path[pi + 1], ArrowPathColor, 3f, cellW, cellH, spaceX, spaceY, totalW, totalH);

                if (arrow.Path.Count > 0)
                {
                    var last = arrow.Path[arrow.Path.Count - 1];
                    DrawCellDot(last, ArrowPathColor, 6f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                }
            }

            // Even/Odd cell markers — square for even, circle for odd
            for (var mi = 0; mi < overlay.CellMarkers.Count; mi++)
            {
                var marker = overlay.CellMarkers[mi];
                var posX = marker.Cell.Col * (cellW + spaceX) + cellW * 0.5f - totalW * 0.5f;
                var posY = -(marker.Cell.Row * (cellH + spaceY) + cellH * 0.5f - totalH * 0.5f);
                if (marker.Type == MarkerType.Even)
                {
                    // Blue square
                    var sq = new GameObject("EvenMarker", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    sq.transform.SetParent(_gridOverlayRoot, false);
                    var sqRect = sq.GetComponent<RectTransform>();
                    sqRect.anchorMin = new Vector2(0.5f, 0.5f);
                    sqRect.anchorMax = new Vector2(0.5f, 0.5f);
                    sqRect.pivot = new Vector2(0.5f, 0.5f);
                    sqRect.anchoredPosition = new Vector2(posX, posY);
                    sqRect.sizeDelta = new Vector2(cellW * 0.70f, cellH * 0.70f);
                    var sqImg = sq.GetComponent<UnityEngine.UI.Image>();
                    sqImg.color = EvenMarkerColor;
                    sqImg.raycastTarget = false;
                    _overlayObjects.Add(sq);
                }
                else
                {
                    // Orange circle
                    DrawOverlayCircle(posX, posY, cellW * 0.35f, OddMarkerColor);
                }

                // Accessibility: add E/O text label on marker
                var markerLabel = _accessibility?.GetMarkerLabel(marker.Type);
                if (markerLabel != null)
                    DrawOverlayTextAt(posX, posY, markerLabel, 14, Color.white);
            }

            // Build modifier legend panel for multi-modifier readability
            var modifiers = run?.CurrentLevelConfig?.ActiveModifiers;
            if (modifiers != null && modifiers.Count > 0)
            {
                BuildModifierLegend(modifiers);
            }
        }

        private void RefreshBossModifierDescription()
        {
            if (_bossModifierDescText == null && sudokuPanel != null)
            {
                _bossModifierDescText = sudokuPanel.transform.Find("SudokuGameplayBossDesc")?.GetComponent<Text>();
            }

            if (_bossModifierDescText == null) return;

            var run = runMapController?.Run;
            var modifiers = run?.CurrentLevelConfig?.ActiveModifiers;
            if (modifiers != null && modifiers.Count > 0)
            {
                var descSb = new System.Text.StringBuilder();
                for (var mi = 0; mi < modifiers.Count; mi++)
                {
                    if (mi > 0) descSb.AppendLine();
                    descSb.Append(GetBossModifierDescription(modifiers[mi]));
                }
                _bossModifierDescText.text = descSb.ToString();
                _bossModifierDescText.gameObject.SetActive(true);
            }
            else
            {
                _bossModifierDescText.text = string.Empty;
                _bossModifierDescText.gameObject.SetActive(false);
            }
        }

        private static string FormatModifierName(BossModifierId modifier) => modifier switch
        {
            BossModifierId.FogOfWar          => "Fog of War",
            BossModifierId.ArrowSums         => "Arrow Sums",
            BossModifierId.GermanWhispers    => "German Whispers",
            BossModifierId.DutchWhispers     => "Dutch Whispers",
            BossModifierId.ParityLines       => "Parity Lines",
            BossModifierId.RenbanLines       => "Renban Lines",
            BossModifierId.KillerCages       => "Killer Cages",
            BossModifierId.DifferenceKropki  => "Difference Kropki",
            BossModifierId.RatioKropki       => "Ratio Kropki",
            BossModifierId.Palindrome        => "Palindrome",
            BossModifierId.Thermo            => "Thermo",
            BossModifierId.BetweenLines      => "Between Lines",
            BossModifierId.EvenOdd           => "Even/Odd",
            BossModifierId.Nonconsecutive    => "Nonconsecutive",
            BossModifierId.Antiknight        => "Antiknight",
            _                                => modifier.ToString()
        };

        private static string GetBossModifierDescription(BossModifierId modifier)
        {
            switch (modifier)
            {
                case BossModifierId.FogOfWar:
                    return "Fog of War: Correct placements reveal hidden cells.\nExample: placing 5 in a fogged cell clears nearby fog.";
                case BossModifierId.ArrowSums:
                    return "Arrow Sums (grey arrow): Digits along the arrow sum to the circled digit.\nExample: circle=6, path=[2,4] \u2192 2+4=6 \u2713";
                case BossModifierId.GermanWhispers:
                    return "German Whispers (green line): Neighbours differ by \u22655.\nExample: 1-7-2-8-3 \u2713  |  1-5-2 \u2717 (5-2=3 < 5)";
                case BossModifierId.DutchWhispers:
                    return "Dutch Whispers (teal line): Neighbours differ by \u22654.\nExample: 1-6-2-7-3 \u2713  |  1-4-1 \u2717 (4-1=3 < 4)";
                case BossModifierId.ParityLines:
                    return "Parity Lines (teal line): Adjacent cells alternate odd/even.\nExample: 1-4-3-8-5 \u2713  |  2-4-6 \u2717 (even-even)";
                case BossModifierId.RenbanLines:
                    return "Renban (pink/purple line): Line digits form a consecutive set (any order).\nExample: 3-5-4 \u2713  |  1-3-5 \u2717 (gap)";
                case BossModifierId.KillerCages:
                    return "Killer Cages (dashed border): Cage digits sum to label; no repeats.\nExample: cage=7, cells=[3,4] \u2713  |  [4,4] \u2717 (repeat)";
                case BossModifierId.DifferenceKropki:
                    return "White Dots (white circle): Adjacent cells differ by exactly 1.\nExample: 4\u25e65 \u2713  |  4\u25e66 \u2717";
                case BossModifierId.RatioKropki:
                    return "Black Dots (black circle): One adjacent cell is double the other.\nExample: 3\u25cf6 \u2713  |  3\u25cf5 \u2717";
                case BossModifierId.Palindrome:
                    return "Palindrome (grey line): Digits read the same forward and backward.\nExample: 3-7-5-7-3 \u2713  |  3-7-5-8-3 \u2717";
                case BossModifierId.Thermo:
                    return "Thermo (orange bulb\u2192tip): Digits strictly increase from bulb to tip.\nExample: bulb=2, path=[4,7] \u2713  |  bulb=2, path=[5,3] \u2717";
                case BossModifierId.BetweenLines:
                    return "Between Lines (white circles at ends): All path digits must lie strictly between the endpoint values.\nExample: ends=2&8, path=[5,4] \u2713  |  path=[1,5] \u2717";
                case BossModifierId.EvenOdd:
                    return "Even/Odd (blue square=even, orange circle=odd): Marked cells must have the correct parity.\nExample: square cell must be 2,4,6,8; circle cell must be 1,3,5,7,9.";
                case BossModifierId.Nonconsecutive:
                    return "Nonconsecutive (global): No two orthogonally adjacent cells may contain consecutive digits.\nExample: 3 adjacent to 4 is \u2717; 3 adjacent to 5 \u2713";
                case BossModifierId.Antiknight:
                    return "Antiknight (global): No two cells a chess knight\u2019s move apart may share a digit.\nExample: if 5 is at r1c1, r2c3 and r3c2 cannot be 5.";
                default:
                    return modifier.ToString();
            }
        }

        private void EnsureGridOverlayRoot()
        {
            if (_gridOverlayRoot != null) return;
            var go = new GameObject("GridOverlay", typeof(RectTransform));
            go.transform.SetParent(sudokuGridRoot.parent, false);
            _gridOverlayRoot = go.GetComponent<RectTransform>();
            _gridOverlayRoot.anchorMin = sudokuGridRoot.anchorMin;
            _gridOverlayRoot.anchorMax = sudokuGridRoot.anchorMax;
            _gridOverlayRoot.pivot = sudokuGridRoot.pivot;
            _gridOverlayRoot.anchoredPosition = sudokuGridRoot.anchoredPosition;
            _gridOverlayRoot.sizeDelta = sudokuGridRoot.sizeDelta;

            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.ignoreLayout = true;

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Final sibling order: Grid (back) → Overlay (middle) → Numbers (front)
            // Insert overlay right after grid; _gridNumberRoot (if present) shifts to after overlay.
            var gridIndex = sudokuGridRoot.GetSiblingIndex();
            _gridOverlayRoot.SetSiblingIndex(gridIndex + 1);

            // Ensure number root is placed after overlay
            if (_gridNumberRoot != null)
            {
                _gridNumberRoot.SetSiblingIndex(gridIndex + 2);
            }
        }

        private void ClearOverlayObjects()
        {
            for (var i = _overlayObjects.Count - 1; i >= 0; i--)
            {
                if (_overlayObjects[i] != null) Destroy(_overlayObjects[i]);
            }
            _overlayObjects.Clear();
            _overlayGroups.Clear();
            if (_legendPanel != null)
            {
                Destroy(_legendPanel);
                _legendPanel = null;
            }
            _legendExpanded = false;
            if (_isolateCoroutine != null)
            {
                StopCoroutine(_isolateCoroutine);
                _isolateCoroutine = null;
            }
        }

        /// <summary>Register an overlay GameObject under a modifier ID for tap-to-isolate grouping.</summary>
        private void RegisterOverlayForModifier(BossModifierId modId, GameObject go)
        {
            if (!_overlayGroups.TryGetValue(modId, out var list))
            {
                list = new List<GameObject>();
                _overlayGroups[modId] = list;
            }
            list.Add(go);
        }

        /// <summary>Build collapsible modifier legend panel when 2+ modifiers are active.</summary>
        private void BuildModifierLegend(List<BossModifierId> activeModifiers)
        {
            if (activeModifiers == null || activeModifiers.Count < 1) return;
            if (_gridOverlayRoot == null) return;

            _legendPanel = new GameObject("ModifierLegend", typeof(RectTransform), typeof(CanvasGroup));
            _legendPanel.transform.SetParent(_gridOverlayRoot.parent, false);
            var panelRect = _legendPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-5f, -5f);
            panelRect.sizeDelta = new Vector2(130f, 26f + activeModifiers.Count * 28f);

            var panelBg = _legendPanel.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            panelBg.raycastTarget = true;

            // Title
            var titleGo = new GameObject("LegendTitle", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(_legendPanel.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 22f);
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = "Modifiers";
            titleText.fontSize = 11;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.raycastTarget = false;

            // Entries
            for (var i = 0; i < activeModifiers.Count; i++)
            {
                var modId = activeModifiers[i];
                var yOffset = -(24f + i * 28f);

                // Colour swatch
                var swatchGo = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                swatchGo.transform.SetParent(_legendPanel.transform, false);
                var swatchRect = swatchGo.GetComponent<RectTransform>();
                swatchRect.anchorMin = new Vector2(0f, 1f);
                swatchRect.anchorMax = new Vector2(0f, 1f);
                swatchRect.pivot = new Vector2(0f, 1f);
                swatchRect.anchoredPosition = new Vector2(6f, yOffset);
                swatchRect.sizeDelta = new Vector2(16f, 16f);
                var swatchImg = swatchGo.GetComponent<Image>();
                swatchImg.color = GetModifierSwatchColor(modId);
                swatchImg.raycastTarget = false;

                // Label
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(_legendPanel.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(0f, 1f);
                labelRect.anchoredPosition = new Vector2(26f, yOffset);
                labelRect.sizeDelta = new Vector2(-32f, 20f);
                var labelText = labelGo.GetComponent<Text>();
                labelText.text = modId.ToString();
                labelText.fontSize = 10;
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.color = Color.white;
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.raycastTarget = false;

                // Tap-to-isolate button
                var btnGo = new GameObject("IsolateBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(_legendPanel.transform, false);
                var btnRect = btnGo.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0f, 1f);
                btnRect.anchorMax = new Vector2(1f, 1f);
                btnRect.pivot = new Vector2(0f, 1f);
                btnRect.anchoredPosition = new Vector2(0f, yOffset);
                btnRect.sizeDelta = new Vector2(0f, 24f);
                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = new Color(0f, 0f, 0f, 0f);
                var btn = btnGo.GetComponent<Button>();
                var capturedMod = modId;
                btn.onClick.AddListener(() => OnLegendEntryTapped(capturedMod));
            }

            // Start collapsed if only 1 modifier
            if (activeModifiers.Count < 2)
            {
                _legendPanel.SetActive(false);
                _legendExpanded = false;
            }
            else
            {
                _legendExpanded = true;
            }
        }

        private void OnLegendEntryTapped(BossModifierId selectedMod)
        {
            if (_isolateCoroutine != null)
            {
                StopCoroutine(_isolateCoroutine);
                RestoreAllOverlayOpacity();
            }
            _isolateCoroutine = StartCoroutine(IsolateModifierCoroutine(selectedMod));
        }

        private System.Collections.IEnumerator IsolateModifierCoroutine(BossModifierId selectedMod)
        {
            // Dim all non-selected modifier groups to 15% opacity
            foreach (var kvp in _overlayGroups)
            {
                var alpha = kvp.Key == selectedMod ? 1f : 0.15f;
                for (var i = 0; i < kvp.Value.Count; i++)
                {
                    var go = kvp.Value[i];
                    if (go == null) continue;
                    var cg = go.GetComponent<CanvasGroup>();
                    if (cg == null) cg = go.AddComponent<CanvasGroup>();
                    cg.alpha = alpha;
                }
            }

            yield return new WaitForSeconds(3f);

            RestoreAllOverlayOpacity();
            _isolateCoroutine = null;
        }

        private void RestoreAllOverlayOpacity()
        {
            foreach (var kvp in _overlayGroups)
            {
                for (var i = 0; i < kvp.Value.Count; i++)
                {
                    var go = kvp.Value[i];
                    if (go == null) continue;
                    var cg = go.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 1f;
                }
            }
        }

        private static Color GetModifierSwatchColor(BossModifierId modId)
        {
            return modId switch
            {
                BossModifierId.GermanWhispers => new Color(0.20f, 0.72f, 0.30f, 1f),
                BossModifierId.DutchWhispers => new Color(0.10f, 0.95f, 0.78f, 1f),
                BossModifierId.ParityLines => new Color(0.30f, 0.40f, 0.85f, 1f),
                BossModifierId.RenbanLines => new Color(0.80f, 0.35f, 0.65f, 1f),
                BossModifierId.Palindrome => new Color(0.60f, 0.60f, 0.60f, 1f),
                BossModifierId.Thermo => new Color(0.75f, 0.45f, 0.15f, 1f),
                BossModifierId.BetweenLines => new Color(0.90f, 0.90f, 0.90f, 1f),
                BossModifierId.DifferenceKropki => new Color(1f, 1f, 1f, 1f),
                BossModifierId.RatioKropki => new Color(0.15f, 0.15f, 0.15f, 1f),
                BossModifierId.KillerCages => new Color(0.85f, 0.15f, 0.12f, 1f),
                BossModifierId.ArrowSums => new Color(0.55f, 0.55f, 0.55f, 1f),
                BossModifierId.EvenOdd => new Color(0.35f, 0.65f, 0.90f, 1f),
                BossModifierId.Nonconsecutive => Color.white,
                BossModifierId.Antiknight => Color.white,
                BossModifierId.FogOfWar => new Color(0.40f, 0.40f, 0.50f, 1f),
                _ => Color.white
            };
        }

        private void DrawLineBetweenCells(CellCoord a, CellCoord b, Color color, float width,
            float cellW, float cellH, float spX, float spY, float totalW, float totalH,
            LineDashPattern pattern = null)
        {
            var ax = a.Col * (cellW + spX) + cellW * 0.5f - totalW * 0.5f;
            var ay = -(a.Row * (cellH + spY) + cellH * 0.5f - totalH * 0.5f);
            var bx = b.Col * (cellW + spX) + cellW * 0.5f - totalW * 0.5f;
            var by = -(b.Row * (cellH + spY) + cellH * 0.5f - totalH * 0.5f);

            var dx = bx - ax;
            var dy = by - ay;
            var len = Mathf.Sqrt(dx * dx + dy * dy);
            var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            var lineWidth = width * (_accessibility?.GetLineWidthMultiplier() ?? 1f);
            if (pattern != null && pattern.Style != LineDashStyle.Solid && pattern.Style != LineDashStyle.SolidThick)
                lineWidth = pattern.Width * (_accessibility?.GetLineWidthMultiplier() ?? 1f);
            if (pattern != null && pattern.Style == LineDashStyle.SolidThick)
                lineWidth = pattern.Width * (_accessibility?.GetLineWidthMultiplier() ?? 1f);

            var overlayAlpha = _accessibility?.GetOverlayAlpha(color.a) ?? color.a;
            var drawColor = new Color(color.r, color.g, color.b, overlayAlpha);

            // Dashed / dotted / dash-dot patterns: draw multiple segments
            if (pattern != null && pattern.SegmentLength > 0f &&
                (pattern.Style == LineDashStyle.Dashed || pattern.Style == LineDashStyle.Dotted || pattern.Style == LineDashStyle.DashDot))
            {
                var segLen = pattern.SegmentLength;
                var gapLen = pattern.GapLength;
                var pos = 0f;
                var isDash = true;
                while (pos < len)
                {
                    var currentLen = isDash ? segLen : (pattern.Style == LineDashStyle.DashDot && !isDash ? 3f : gapLen);
                    if (isDash)
                    {
                        var end = Mathf.Min(pos + currentLen, len);
                        var segStart = pos / len;
                        var segEnd = end / len;
                        var sx = ax + dx * segStart;
                        var sy = ay + dy * segStart;
                        DrawSegment(sx, sy, end - pos, lineWidth, angle, drawColor);
                    }
                    pos += currentLen;
                    isDash = !isDash;
                }
                return;
            }

            // Double line pattern: two parallel lines offset by width
            if (pattern != null && pattern.Style == LineDashStyle.DoubleLine)
            {
                var perpX = -dy / len * lineWidth * 0.8f;
                var perpY = dx / len * lineWidth * 0.8f;
                DrawSegment(ax + perpX, ay + perpY, len, lineWidth * 0.6f, angle, drawColor);
                DrawSegment(ax - perpX, ay - perpY, len, lineWidth * 0.6f, angle, drawColor);
                return;
            }

            // Wavy pattern: sine wave offset segments
            if (pattern != null && pattern.Style == LineDashStyle.Wavy)
            {
                const int segments = 10;
                var segLen = len / segments;
                for (var si = 0; si < segments; si++)
                {
                    var t0 = si / (float)segments;
                    var t1 = (si + 1f) / segments;
                    var waveAmp = lineWidth * 1.5f;
                    var perpX = -dy / len;
                    var perpY = dx / len;
                    var off0 = Mathf.Sin(t0 * Mathf.PI * 3f) * waveAmp;
                    var off1 = Mathf.Sin(t1 * Mathf.PI * 3f) * waveAmp;
                    var sx = ax + dx * t0 + perpX * off0;
                    var sy = ay + dy * t0 + perpY * off0;
                    var ex = ax + dx * t1 + perpX * off1;
                    var ey = ay + dy * t1 + perpY * off1;
                    var sdx = ex - sx;
                    var sdy = ey - sy;
                    var sLen = Mathf.Sqrt(sdx * sdx + sdy * sdy);
                    var sAngle = Mathf.Atan2(sdy, sdx) * Mathf.Rad2Deg;
                    DrawSegment(sx, sy, sLen, lineWidth * 0.7f, sAngle, drawColor);
                }
                return;
            }

            // Default: solid line
            DrawSegment(ax, ay, len, lineWidth, angle, drawColor);
        }

        private void DrawSegment(float x, float y, float length, float width, float angle, Color color)
        {
            var go = new GameObject("OverlaySeg", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(length, width);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private void DrawCellDot(CellCoord c, Color color, float radius,
            float cellW, float cellH, float spX, float spY, float totalW, float totalH)
        {
            var posX = c.Col * (cellW + spX) + cellW * 0.5f - totalW * 0.5f;
            var posY = -(c.Row * (cellH + spY) + cellH * 0.5f - totalH * 0.5f);
            DrawOverlayCircle(posX, posY, radius, color);
        }

        private void DrawOverlayLabel(CellCoord c, string label, int fontSize,
            float cellW, float cellH, float spX, float spY, float totalW, float totalH)
        {
            var posX = c.Col * (cellW + spX) + cellW * 0.5f - totalW * 0.5f;
            var posY = -(c.Row * (cellH + spY) + cellH * 0.5f - totalH * 0.5f);
            DrawOverlayTextAt(posX, posY, label, fontSize, Color.white);
        }

        private void DrawOverlayTextAt(float x, float y, string label, int fontSize, Color color)
        {
            var scaledSize = _accessibility?.ScaleFont(fontSize) ?? fontSize;
            var go = new GameObject("OverlayLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(24f, 20f);
            var txt = go.GetComponent<Text>();
            txt.text = label;
            txt.fontSize = scaledSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private static void EnsureBarFillSprite(Image fill)
        {
            if (fill.sprite != null) return;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var white = Color.white;
            for (var py = 0; py < 4; py++)
                for (var px = 0; px < 4; px++)
                    tex.SetPixel(px, py, white);
            tex.Apply();
            fill.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
        }

        private static Sprite BuildCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var center = size * 0.5f - 0.5f;
            var r = center;
            for (var py = 0; py < size; py++)
            {
                for (var px = 0; px < size; px++)
                {
                    var dx = px - center;
                    var dy = py - center;
                    var dist = UnityEngine.Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = UnityEngine.Mathf.Clamp01((r - dist) + 0.5f);
                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void DrawOverlayCircle(float x, float y, float radius, Color color)
        {
            if (_circleSprite == null)
                _circleSprite = BuildCircleSprite();

            var go = new GameObject("OverlayDot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);

            var img = go.GetComponent<Image>();
            img.sprite = _circleSprite;
            img.color = color;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private void DrawBorderStrip(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private static void TintBorderIfCageEdge(Image border, KillerCage cage, int row, int col, int dr, int dc)
        {
            var nr = row + dr;
            var nc = col + dc;
            var inCage = false;
            for (var i = 0; i < cage.Cells.Count; i++)
            {
                if (cage.Cells[i].Row == nr && cage.Cells[i].Col == nc)
                {
                    inCage = true;
                    break;
                }
            }

            if (!inCage)
            {
                border.color = KillerCageBorder;
            }
        }

        private void RecordCageEdges(KillerCage cage, int row, int col, int boardSize)
        {
            // Check each of the four directions; if the neighbor is NOT in the cage, mark that edge
            CheckAndRecordEdge(cage, row, col, -1, 0, boardSize); // top
            CheckAndRecordEdge(cage, row, col, 1, 0, boardSize);  // bottom
            CheckAndRecordEdge(cage, row, col, 0, -1, boardSize); // left
            CheckAndRecordEdge(cage, row, col, 0, 1, boardSize);  // right
        }

        private void CheckAndRecordEdge(KillerCage cage, int row, int col, int dr, int dc, int boardSize)
        {
            var nr = row + dr;
            var nc = col + dc;
            var inCage = false;
            for (var i = 0; i < cage.Cells.Count; i++)
            {
                if (cage.Cells[i].Row == nr && cage.Cells[i].Col == nc)
                {
                    inCage = true;
                    break;
                }
            }

            if (!inCage)
            {
                // Encode: row * boardSize * 4 + col * 4 + sideIndex (top=0, bottom=1, left=2, right=3)
                var side = dr == -1 ? 0 : dr == 1 ? 1 : dc == -1 ? 2 : 3;
                _cageBorderEdges.Add((long)row * boardSize * 4 + col * 4 + side);
            }
        }

        private bool IsCageBorderEdge(int row, int col, int boardSize, int side)
        {
            return _cageBorderEdges.Contains((long)row * boardSize * 4 + col * 4 + side);
        }

        private sealed class CellView
        {
            public int Row;
            public int Col;
            public RectTransform Root;
            public Image Image;
            public Text Label;
            public Text PencilLabel;
            public Image BorderTop;
            public Image BorderBottom;
            public Image BorderLeft;
            public Image BorderRight;
            public Button Button;
        }
    }
}
