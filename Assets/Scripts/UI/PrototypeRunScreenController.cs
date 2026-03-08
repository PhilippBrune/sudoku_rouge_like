using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
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
        private Image _classToken;
        private Vector2 _classTokenTarget;
        private bool _hasClassTokenTarget;
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
        private RectTransform _pathOverlayRoot;
        private Vector2 _laneAEndPanelLocal;
        private Vector2 _laneBEndPanelLocal;
        private bool _hasLaneAEnd;
        private bool _hasLaneBEnd;
        private int _lastPuzzleItemSignature = int.MinValue;
        private readonly List<(int Row, int Col)> _finderHighlightCells = new();
        private float _finderHighlightUntil;
        private GameObject _bossGateChoicePanel;
        private bool _awaitingBossGateChoice;
        private bool _awaitingRewardReplacement;
        private int _pendingRewardSlotIndex;
        private RectTransform _gridOverlayRoot;
        private RectTransform _gridNumberRoot;
        private readonly List<GameObject> _overlayObjects = new();
        private bool _overlaysBuilt;
        private readonly HashSet<long> _cageBorderEdges = new();

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
        private static readonly Color DutchWhispersLineColor = new(0.90f, 0.55f, 0.15f, 0.55f);
        private static readonly Color ParityLineColor = new(0.30f, 0.40f, 0.85f, 0.55f);
        private static readonly Color RenbanLineColor = new(0.80f, 0.35f, 0.65f, 0.55f);
        private static readonly Color KillerCageBorder = new(0.85f, 0.15f, 0.12f, 0.90f);
        private static readonly Color WhiteDotColor = new(0.95f, 0.95f, 0.95f, 0.85f);
        private static readonly Color BlackDotColor = new(0.10f, 0.10f, 0.10f, 0.90f);
        private static readonly Color ArrowCircleColor = new(0.70f, 0.70f, 0.70f, 0.55f);
        private static readonly Color ArrowPathColor = new(0.55f, 0.55f, 0.55f, 0.45f);

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
                UpdateClassTokenPosition();
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

        private void HandleKeyboardInput()
        {
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

        private void RefreshPathOverview()
        {
            if (runMapController == null)
            {
                return;
            }

            PrepareLaneRootForFreePlacement(laneAPathRoot);
            PrepareLaneRootForFreePlacement(laneBPathRoot);

            EnsureClassToken();

            var run = runMapController.Run;
            if (run == null || run.CurrentRunGraph == null || run.CurrentRunGraph.Count == 0)
            {
                if (pathOverviewText != null)
                {
                    pathOverviewText.text = "No active run graph.";
                }

                return;
            }

            if (pathOverviewText != null)
            {
                var runState = run.RunState;
                var gardenName = GetGardenName(runState.Depth);
                var classPassive = Classes.ClassCatalog.GetMeta(runState.ClassId).PassiveDescription;
                var overview =
                    $"{gardenName}\n" +
                    $"HP: {runState.CurrentHP}/{runState.MaxHP}    Gold: {runState.CurrentGold}    Pencil: {runState.CurrentPencil}/{runState.MaxPencil}\n" +
                    $"Items: {runState.Inventory.Count}    Relics: {runState.RelicIds.Count}\n" +
                    $"Passive: {classPassive}";

                if (runState.RelicIds.Count > 0)
                {
                    overview += "\nRelics:";
                    for (var r = 0; r < runState.RelicIds.Count; r++)
                    {
                        overview += $"\n  - {DescribeRelic(runState.RelicIds[r])}";
                    }
                }

                if (!string.IsNullOrWhiteSpace(_pathOverlayMessage))
                {
                    overview += "\n\n" + _pathOverlayMessage;
                }

                if (!string.IsNullOrWhiteSpace(_hoverInfo))
                {
                    overview += "\n\n" + _hoverInfo;
                }

                pathOverviewText.text = overview;
            }

            if (laneAText != null)
            {
                laneAText.text = "Calm Route";
            }

            if (laneBText != null)
            {
                laneBText.text = "Risk Route";
            }

            var previewA = runMapController.BuildPathChoicePreview(false);
            var previewB = runMapController.BuildPathChoicePreview(true);
            var lockValue = previewA.LockedPath ?? previewB.LockedPath;

            var nextSignature = BuildLaneRenderSignature(previewA, previewB, lockValue);
            if (nextSignature != _lastLaneRenderSignature)
            {
                _lastLaneRenderSignature = nextSignature;
                RebuildLaneNodeButtons(false, laneAPathRoot, previewA.Available && (!lockValue.HasValue || lockValue.Value == false));
                RebuildLaneNodeButtons(true, laneBPathRoot, previewB.Available && (!lockValue.HasValue || lockValue.Value == true));
                RebuildInventoryBadges();
            }

            RebuildSharedBossGate();

            if (laneAPathRoot != null)
            {
                var image = laneAPathRoot.GetComponent<Image>();
                if (image != null)
                {
                    var dim = lockValue.HasValue && lockValue.Value;
                    image.color = dim ? new Color(0f, 0f, 0f, 0.30f) : new Color(0f, 0f, 0f, 0.12f);
                }
            }

            if (laneBPathRoot != null)
            {
                var image = laneBPathRoot.GetComponent<Image>();
                if (image != null)
                {
                    var dim = lockValue.HasValue && !lockValue.Value;
                    image.color = dim ? new Color(0f, 0f, 0f, 0.30f) : new Color(0f, 0f, 0f, 0.12f);
                }
            }
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

        private void RebuildSharedBossGate()
        {
            EnsurePathOverlayRoot();
            if (_pathOverlayRoot == null)
            {
                return;
            }

            for (var i = _pathOverlayRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_pathOverlayRoot.GetChild(i).gameObject);
            }

            var run = runMapController?.Run;
            var graph = run?.CurrentRunGraph;
            if (graph == null)
            {
                return;
            }

            RunNode boss = null;
            for (var i = 0; i < graph.Count; i++)
            {
                if (graph[i] != null && graph[i].Type == NodeType.Boss)
                {
                    boss = graph[i];
                    break;
                }
            }

            if (boss == null || (!_hasLaneAEnd && !_hasLaneBEnd))
            {
                return;
            }

            var width = Mathf.Max(320f, _pathOverlayRoot.rect.width);
            var height = Mathf.Max(220f, _pathOverlayRoot.rect.height);
            var bossPos = new Vector2(width * 0.5f, Mathf.Clamp(height * 0.14f, 56f, height - 42f));

            if (_hasLaneAEnd) CreatePathConnectionLine(_pathOverlayRoot, _laneAEndPanelLocal, bossPos);
            if (_hasLaneBEnd) CreatePathConnectionLine(_pathOverlayRoot, _laneBEndPanelLocal, bossPos);

            var bossButton = CreatePathNodeButton(_pathOverlayRoot, boss, risk: false, bossPos);
            bossButton.onClick.RemoveAllListeners();
            bossButton.onClick.AddListener(() => ShowBossGateChoice());
            bossButton.interactable = IsNextChoiceNode(boss, false) || IsNextChoiceNode(boss, true);
        }

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

        private void RebuildLaneNodeButtons(bool risk, RectTransform root, bool laneIsAvailable)
        {
            if (root == null || runMapController?.Run?.CurrentRunGraph == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }

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

            var graph = runMapController.Run.CurrentRunGraph;
            var laneNodes = new List<RunNode>();
            for (var i = 0; i < graph.Count; i++)
            {
                var node = graph[i];
                if (node == null || node.Depth <= 1)
                {
                    continue;
                }

                if (node.Type == NodeType.Boss)
                {
                    continue;
                }

                if (node.IsRiskPath != risk)
                {
                    continue;
                }

                laneNodes.Add(node);
            }

            // Fallback so path lanes are never empty if risk markers are missing/misaligned.
            if (laneNodes.Count == 0)
            {
                for (var i = 0; i < graph.Count; i++)
                {
                    var node = graph[i];
                    if (node != null && node.Depth > 1)
                    {
                        laneNodes.Add(node);
                    }
                }
            }

            var desiredPositions = new List<Vector2>(laneNodes.Count);
            for (var i = 0; i < laneNodes.Count; i++)
            {
                desiredPositions.Add(ComputeLaneNodePosition(root, i, laneNodes.Count, laneNodes[i], risk));
            }

            ResolveLaneOverlaps(desiredPositions, root);

            var previousNodePos = Vector2.zero;
            var hasPrevious = false;
            var laneEnd = new Vector2(Mathf.Max(24f, root.rect.width * 0.5f), Mathf.Max(24f, root.rect.height * 0.5f));
            var hasLaneEnd = false;
            for (var i = 0; i < laneNodes.Count; i++)
            {
                var node = laneNodes[i];
                var pos = desiredPositions[i];
                var button = CreatePathNodeButton(root, node, risk, pos);
                button.interactable = laneIsAvailable && IsNextChoiceNode(node, risk);
                TrySetClassTokenTargetForNode(node, risk, pos);

                if (hasPrevious)
                {
                    CreatePathConnectionLine(root, previousNodePos, pos);
                }

                hasPrevious = true;
                previousNodePos = pos;
                laneEnd = pos;
                hasLaneEnd = true;
            }

            CaptureLaneEndForSharedBoss(risk, root, laneEnd, hasLaneEnd);
        }

        private static void ResolveLaneOverlaps(List<Vector2> positions, RectTransform root)
        {
            if (positions == null || positions.Count <= 1 || root == null)
            {
                return;
            }

            var width = Mathf.Max(180f, root.rect.width);
            var height = Mathf.Max(140f, root.rect.height);
            const float minDist = 112f;

            for (var pass = 0; pass < 16; pass++)
            {
                for (var i = 0; i < positions.Count; i++)
                {
                    for (var j = i + 1; j < positions.Count; j++)
                    {
                        var delta = positions[j] - positions[i];
                        var dist = Mathf.Max(0.001f, delta.magnitude);
                        if (dist >= minDist)
                        {
                            continue;
                        }

                        var push = (minDist - dist) * 0.5f;
                        var dir = delta / dist;
                        positions[i] -= dir * push;
                        positions[j] += dir * push;
                    }

                    var p = positions[i];
                    p.x = Mathf.Clamp(p.x, 40f, width - 40f);
                    p.y = Mathf.Clamp(p.y, 44f, height - 44f);
                    positions[i] = p;
                }
            }
        }

        private void CaptureLaneEndForSharedBoss(bool risk, RectTransform laneRoot, Vector2 laneLocalPoint, bool hasPoint)
        {
            if (!hasPoint || laneRoot == null || pathOverviewPanel == null)
            {
                if (risk)
                {
                    _hasLaneBEnd = false;
                }
                else
                {
                    _hasLaneAEnd = false;
                }

                return;
            }

            var panelRect = pathOverviewPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            var world = laneRoot.TransformPoint(laneLocalPoint);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, RectTransformUtility.WorldToScreenPoint(null, world), null, out var panelPos))
            {
                return;
            }

            panelPos.x += panelRect.rect.width * 0.5f;
            panelPos.y += panelRect.rect.height * 0.5f;

            if (risk)
            {
                _laneBEndPanelLocal = panelPos;
                _hasLaneBEnd = true;
            }
            else
            {
                _laneAEndPanelLocal = panelPos;
                _hasLaneAEnd = true;
            }
        }

        private static Vector2 ComputeLaneNodePosition(RectTransform root, int index, int count, RunNode node, bool risk)
        {
            var width = Mathf.Max(320f, root.rect.width);
            var height = Mathf.Max(220f, root.rect.height);

            if (count <= 1)
                return new Vector2(width * 0.5f, height * 0.5f);

            // Snake layout: arrange nodes in a zigzag grid.
            // Determine how many columns fit (3 columns for a clear snake).
            const int cols = 3;
            var rows = Mathf.CeilToInt((float)count / cols);

            var row = index / cols;
            var colInRow = index % cols;

            // Alternate row direction for snake pattern.
            // Left lane (risk=false): even rows go left→right, odd rows right→left — starts top-left.
            // Right lane (risk=true): mirrored — even rows go right→left, odd rows left→right — starts top-right.
            bool reverseRow;
            if (risk)
                reverseRow = row % 2 == 0; // even rows right→left for risk
            else
                reverseRow = row % 2 == 1; // odd rows right→left for safe

            if (reverseRow)
                colInRow = cols - 1 - colInRow;

            var margin = 52f;
            var usableW = width - margin * 2f;
            var usableH = height - margin * 2f;

            var cellW = cols > 1 ? usableW / (cols - 1) : 0f;
            var cellH = rows > 1 ? usableH / (rows - 1) : 0f;

            var x = margin + colInRow * cellW;
            var y = height - margin - row * cellH; // top to bottom

            return new Vector2(Mathf.Clamp(x, 46f, width - 46f), Mathf.Clamp(y, 46f, height - 46f));
        }

        private static void CreatePathConnectionLine(RectTransform parent, Vector2 a, Vector2 b)
        {
            var lineGo = new GameObject("PathLine", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(parent, false);
            lineGo.transform.SetAsFirstSibling();

            var image = lineGo.GetComponent<Image>();
            image.color = new Color(0.95f, 0.51f, 0.17f, 0.85f);
            image.raycastTarget = false;

            var rect = lineGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);

            var delta = b - a;
            var dist = delta.magnitude;
            if (dist < 0.001f)
            {
                return;
            }

            var dir = delta / dist;
            var halfTile = 39f;
            var start = a + (dir * halfTile);
            var end = b - (dir * halfTile);
            var edgeDelta = end - start;

            rect.sizeDelta = new Vector2(Mathf.Max(2f, edgeDelta.magnitude), 4f);
            rect.anchoredPosition = start + (edgeDelta * 0.5f);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(edgeDelta.y, edgeDelta.x) * Mathf.Rad2Deg);
        }

        private static bool IsPuzzleNodeType(NodeType type)
        {
            return type == NodeType.Puzzle || type == NodeType.ElitePuzzle || type == NodeType.Boss;
        }

        private Button CreatePathNodeButton(RectTransform parent, RunNode node, bool risk, Vector2 anchoredPosition)
        {
            var go = new GameObject($"PathNode_{(risk ? "B" : "A")}_{node.Depth}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var goRect = go.GetComponent<RectTransform>();
            goRect.anchorMin = new Vector2(0f, 0f);
            goRect.anchorMax = new Vector2(0f, 0f);
            goRect.pivot = new Vector2(0.5f, 0.5f);
            goRect.sizeDelta = node.Type == NodeType.Boss ? new Vector2(156f, 78f) : new Vector2(78f, 78f);
            goRect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = node.Type == NodeType.Boss ? new Color(0.38f, 0.16f, 0.10f, 1f) : new Color(0.19f, 0.28f, 0.20f, 1f);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.colorMultiplier = 1.35f;
            colors.fadeDuration = 0.07f;
            colors.highlightedColor = new Color(0.30f, 0.43f, 0.30f, 1f);
            colors.pressedColor = new Color(0.15f, 0.22f, 0.16f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => ChoosePath(risk));

            // Add pixel-art icon for node type (added first so labels render on top)
            var iconSprite = PathNodeIconFactory.GetIcon(node.Type.ToString());
            if (iconSprite != null)
            {
                var iconGo = new GameObject("NodeIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.10f, 0.10f);
                iconRect.anchorMax = new Vector2(0.90f, 0.90f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Size label (top-left corner) — shown on all node types
            runMapController.TryGetFixedLevelForNode(node, out var config);
            var boardSize = config != null ? config.BoardSize : Mathf.Clamp(4 + node.Depth / 3, 4, 9);
            var starCount = config != null ? config.Stars : Mathf.Clamp(1 + node.Depth / 4, 1, 5);

            var sizeGo = new GameObject("SizeLabel", typeof(RectTransform), typeof(Text));
            sizeGo.transform.SetParent(go.transform, false);
            var sizeRect = sizeGo.GetComponent<RectTransform>();
            sizeRect.anchorMin = new Vector2(0f, 0.72f);
            sizeRect.anchorMax = new Vector2(0.50f, 1f);
            sizeRect.offsetMin = new Vector2(3f, 0f);
            sizeRect.offsetMax = new Vector2(0f, -2f);
            var sizeText = sizeGo.GetComponent<Text>();
            sizeText.text = $"{boardSize}x{boardSize}";
            sizeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            sizeText.fontSize = 10;
            sizeText.alignment = TextAnchor.UpperLeft;
            sizeText.color = new Color(0.95f, 0.95f, 0.85f, 0.90f);
            sizeText.raycastTarget = false;

            // Difficulty label (bottom-right corner) — shown on all node types
            var diffGo = new GameObject("DiffLabel", typeof(RectTransform), typeof(Text));
            diffGo.transform.SetParent(go.transform, false);
            var diffRect = diffGo.GetComponent<RectTransform>();
            diffRect.anchorMin = new Vector2(0.50f, 0f);
            diffRect.anchorMax = new Vector2(1f, 0.30f);
            diffRect.offsetMin = new Vector2(0f, 2f);
            diffRect.offsetMax = new Vector2(-3f, 0f);
            var diffText = diffGo.GetComponent<Text>();
            diffText.text = new string('\u2605', starCount);
            diffText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            diffText.fontSize = 10;
            diffText.alignment = TextAnchor.LowerRight;
            diffText.color = new Color(0.98f, 0.83f, 0.26f, 0.90f);
            diffText.raycastTarget = false;

            return button;
        }

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
                    text.fontSize = size <= 6 ? 30 : size <= 8 ? 24 : 20;
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
                    pencilText.fontSize = size <= 6 ? 16 : 14;
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
            text.fontSize = 13;
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
            text.fontSize = 24;
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
            var board = runMapController?.Run?.CurrentBoard;
            if (board == null)
            {
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
            if (fogOverlay != null && fogOverlay.IsFogged(_selectedRow, _selectedCol))
            {
                SetStatus("This cell is hidden by fog.");
                return;
            }

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
            SetStatus(ok ? $"Placed {value}." : $"{value} is incorrect. HP now {run.RunState.CurrentHP}.");
            if (!ok)
            {
                _runAudio?.PlayWrongPlacement();
            }
            else
            {
                _runAudio?.PlayCorrectPlacement();
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
                    cell.Label.text = string.Empty;
                    if (cell.PencilLabel != null) cell.PencilLabel.text = string.Empty;
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
                    var replaceButton = BuildPanelButton(_shopPanel.transform, $"ShopReplace_{i}", new Vector2(0.08f + ((i % 3) * 0.29f), 0.48f - ((i / 3) * 0.18f)), new Vector2(0.34f, 0.14f), new Color(0.28f, 0.22f, 0.18f, 0.95f), anchorBased: true);
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

            var relicId = $"relic_node_{state.Depth}_{DateTime.UtcNow:HHmmss}";
            state.RelicIds.Add(relicId);
            state.MaxPencil = Mathf.Min(99, state.MaxPencil + 1);
            state.CurrentPencil = Mathf.Min(state.MaxPencil, state.CurrentPencil + 1);

            _runAudio?.PlayRelicPickup();
            _pathOverlayMessage = $"Relic Node\nEffect: +1 Max Pencil.";
            SetStatus("Relic acquired.");
        }

        private void ShowBossGateChoice()
        {
            if (_awaitingBossGateChoice) return;

            var run = runMapController?.Run;
            if (run == null) return;

            var state = run.RunState;
            var bossDepth = 0;
            var graph = run.CurrentRunGraph;
            if (graph != null)
            {
                for (var i = 0; i < graph.Count; i++)
                {
                    if (graph[i] != null && graph[i].Type == NodeType.Boss)
                    {
                        bossDepth = graph[i].Depth;
                        break;
                    }
                }
            }

            var choices = run.GetBossChoicesForDepth(bossDepth);
            if (choices == null || choices.Count < 2)
            {
                ChoosePath(false);
                return;
            }

            _awaitingBossGateChoice = true;

            if (_bossGateChoicePanel != null)
            {
                Destroy(_bossGateChoicePanel);
            }

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _bossGateChoicePanel = new GameObject("BossGateChoicePanel", typeof(RectTransform), typeof(Image));
            _bossGateChoicePanel.transform.SetParent(canvas.transform, false);
            var panelRect = _bossGateChoicePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.25f);
            panelRect.anchorMax = new Vector2(0.75f, 0.75f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImg = _bossGateChoicePanel.GetComponent<Image>();
            panelImg.color = new Color(0.10f, 0.08f, 0.06f, 0.96f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelRect, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.80f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = "Boss Gate — Choose a Modifier";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.90f, 0.80f, 0.55f, 1f);

            for (var i = 0; i < 2 && i < choices.Count; i++)
            {
                var modifier = choices[i];
                var seen = state.SeenBossModifiers.Contains(modifier);
                var label = seen ? modifier.ToString() : "???";
                var yMin = i == 0 ? 0.42f : 0.10f;
                var yMax = i == 0 ? 0.72f : 0.40f;

                var btnGo = new GameObject($"BossChoice_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(panelRect, false);
                var btnRect = btnGo.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.10f, yMin);
                btnRect.anchorMax = new Vector2(0.90f, yMax);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = new Color(0.25f, 0.18f, 0.12f, 1f);

                var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                lblGo.transform.SetParent(btnRect, false);
                var lblRect = lblGo.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.offsetMin = Vector2.zero;
                lblRect.offsetMax = Vector2.zero;
                var lblText = lblGo.GetComponent<Text>();
                lblText.text = label;
                lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lblText.fontSize = 18;
                lblText.alignment = TextAnchor.MiddleCenter;
                lblText.color = Color.white;

                var captured = modifier;
                var btn = btnGo.GetComponent<Button>();
                btn.onClick.AddListener(() => HandleBossGateSelection(captured));
            }
        }

        private void HandleBossGateSelection(BossModifierId chosen)
        {
            _awaitingBossGateChoice = false;

            var state = runMapController?.Run?.RunState;
            if (state != null)
            {
                state.ChosenBossModifier = chosen;
                state.SeenBossModifiers.Add(chosen);
            }

            if (_bossGateChoicePanel != null)
            {
                Destroy(_bossGateChoicePanel);
                _bossGateChoicePanel = null;
            }

            ChoosePath(false);
        }

        private static string FormatOffer(ShopOffer offer)
        {
            if (offer == null)
            {
                return "Unknown offer";
            }

            if (offer.Item != null)
            {
                return $"{offer.Item.Type} ({offer.Item.Rarity}) - {offer.Price}g";
            }

            if (!string.IsNullOrWhiteSpace(offer.RelicId))
            {
                return $"{offer.RelicId} - {offer.Price}g";
            }

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

            for (var i = 0; i < state.RelicIds.Count; i++)
            {
                var relicId = state.RelicIds[i];
                CreateHoverBadge(
                    $"Relic {ShortRelicName(relicId)}",
                    DescribeRelic(relicId),
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

        private static string GetGardenName(int depth)
        {
            return depth switch
            {
                <= 3 => "Outer Gate",
                <= 6 => "Bamboo Shade",
                <= 9 => "Stone Court",
                _ => "Temple Ascent"
            };
        }

        private static string DescribeItem(ItemInstance item)
        {
            if (item == null)
            {
                return "Unknown item";
            }

            var effect = item.Type switch
            {
                ItemType.Solver => "Fill the selected empty cell with the correct value.",
                ItemType.Finder => "Adds pencil hints to empty cells that match the selected value.",
                ItemType.InkWell => "Restore pencil marks resource.",
                ItemType.MeditationStone => "Recover HP.",
                ItemType.WindChime => "Clean candidate marks from the selected row and column.",
                ItemType.PatternScroll => "Write legal candidate marks into the selected empty cell.",
                ItemType.KoiReflection => "Recover both HP and pencil resources.",
                ItemType.LanternOfClarity => "Reveal one correct value in an empty cell.",
                ItemType.TeaOfFocus => "Negate mistake damage for upcoming placements.",
                ItemType.CherryBlossomPact => "Increase max pencil and refill it immediately.",
                ItemType.FortuneEnvelope => "Gain bonus gold instantly.",
                ItemType.StoneShift => "Clear the selected non-given cell.",
                ItemType.HarmonyCharm => "Gain mistake shield charges.",
                ItemType.CompassOfOrder => "Reveal one clear candidate in the selected cell.",
                _ => "Use the item for a tactical boost."
            };

            return $"{item.Type} ({item.Rarity})\nCharges: {item.Charges}\n{effect}";
        }

        private static string DescribeRelic(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                return "Unknown relic";
            }

            // Legendary relics.
            if (relicId == "relic_legend_shifting_garden") return "Shifting Garden: Corrupts garden path, mutating route pressure.";
            if (relicId == "relic_legend_silent_grid") return "Silent Grid: +2 Mistake Shield charges.";
            if (relicId == "relic_legend_golden_root") return "Golden Root: Enables gold interest carry between stages.";

            // Named relics.
            if (relicId == "relic_combo_t2_monk_charm") return "Monk Charm: Passive combo synergy relic.";
            if (relicId == "relic_cursed_t4_transmuted") return "Transmuted Burden: Cursed relic. +8 Gold on acquire.";
            if (relicId == "relic_utility_t4_transmuted") return "Transmuted Sigil: +1 Max HP, +3 Gold.";

            // Dynamic relics by category.
            if (relicId.Contains("sur") || relicId.Contains("hp")) return $"{ShortRelicName(relicId)}: +1 Max HP, +1 Current HP.";
            if (relicId.Contains("eco") || relicId.Contains("gold")) return $"{ShortRelicName(relicId)}: +5 Gold on acquire.";
            if (relicId.Contains("util")) return $"{ShortRelicName(relicId)}: +1 Max HP, +3 Gold.";
            if (relicId.Contains("pencil")) return $"{ShortRelicName(relicId)}: +2 Max Pencil, +2 Current Pencil.";
            if (relicId.Contains("chaos") || relicId.Contains("cursed")) return $"{ShortRelicName(relicId)}: +8 Gold on acquire.";
            if (relicId.Contains("mod")) return $"{ShortRelicName(relicId)}: Synergy relic. Reduces boss modifier difficulty.";
            if (relicId.Contains("combo")) return $"{ShortRelicName(relicId)}: Synergy relic. Stacks gold multiplier.";

            return $"{relicId}: Passive relic effect active.";
        }

        private static string ShortRelicName(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                return "Relic";
            }

            return relicId.Length <= 10 ? relicId : relicId.Substring(0, 10);
        }

        private void EnsureClassToken()
        {
            if (_classToken != null || pathOverviewPanel == null)
            {
                return;
            }

            var panelRect = pathOverviewPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            var go = new GameObject("ClassToken", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panelRect, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(18f, 18f);

            _classToken = go.GetComponent<Image>();
            _classToken.color = new Color(0.95f, 0.26f, 0.20f, 0.95f);
            _classToken.raycastTarget = false;
        }

        private void TrySetClassTokenTargetForNode(RunNode node, bool risk, Vector2 localPos)
        {
            if (_classToken == null || runMapController?.Run?.CurrentRunGraph == null || node == null)
            {
                return;
            }

            var graph = runMapController.Run.CurrentRunGraph;
            var currentIndex = Mathf.Clamp(runMapController.Run.RunState.CurrentNodeIndex, 0, graph.Count - 1);
            var currentNode = graph[currentIndex];
            if (currentNode == null || currentNode.Depth != node.Depth)
            {
                return;
            }

            if (currentNode.Type != NodeType.Boss && currentNode.IsRiskPath != risk)
            {
                return;
            }

            var laneRoot = risk ? laneBPathRoot : laneAPathRoot;
            if (laneRoot == null)
            {
                return;
            }

            var world = laneRoot.TransformPoint(localPos);
            var panelRect = pathOverviewPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, RectTransformUtility.WorldToScreenPoint(null, world), null, out var panelPos))
            {
                return;
            }

            _classTokenTarget = panelPos;
            _hasClassTokenTarget = true;
        }

        private void UpdateClassTokenPosition()
        {
            if (_classToken == null || !_hasClassTokenTarget)
            {
                return;
            }

            var rect = _classToken.rectTransform;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, _classTokenTarget, 0.25f);
        }

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

            var summary = new StringBuilder();
            summary.AppendLine("Puzzle cleared");
            summary.AppendLine($"Gold gained: +{goldEarned}");
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
                    runMapController.AdvanceToNextGarden();
                    var gardenName = GetGardenName(runMapController.Run.RunState.Depth);
                    SetStatus($"Boss defeated! Entering {gardenName}...");
                    _lastLaneRenderSignature = int.MinValue;
                    ShowPathOverview();
                    RefreshPathOverview();
                    return;
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

            // After boss: advance to next garden instead of ending the run.
            var currentNode = GetCurrentNode();
            if (currentNode != null && currentNode.Type == NodeType.Boss)
            {
                runMapController.AdvanceToNextGarden();
                var gardenName = GetGardenName(run.RunState.Depth);
                SetStatus($"Boss defeated! Entering {gardenName}...");
                _lastLaneRenderSignature = int.MinValue;
                RefreshPathOverview();
                return;
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
                runMapController.AdvanceToNextGarden();
                var gardenName = GetGardenName(run.RunState.Depth);
                SetStatus($"Boss defeated! Entering {gardenName}...");
                _lastLaneRenderSignature = int.MinValue;
                RefreshPathOverview();
                return;
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
                return $"Gold +{slot.NothingGoldBonus}\nReceive bonus gold instead of an item.";
            }

            if (slot.RolledItem != null)
            {
                var effect = slot.RolledItem.Type switch
                {
                    ItemType.Solver => "Fill the selected empty cell with the correct value.",
                    ItemType.Finder => "Adds pencil hints to empty cells that match the selected value.",
                    ItemType.InkWell => "Restore pencil marks resource.",
                    ItemType.MeditationStone => "Recover HP.",
                    ItemType.WindChime => "Clean candidate marks from the selected row and column.",
                    ItemType.PatternScroll => "Write legal candidate marks into the selected empty cell.",
                    ItemType.KoiReflection => "Recover both HP and pencil resources.",
                    ItemType.LanternOfClarity => "Reveal one correct value in an empty cell.",
                    ItemType.TeaOfFocus => "Negate mistake damage for upcoming placements.",
                    ItemType.CherryBlossomPact => "Increase max pencil and refill it immediately.",
                    ItemType.FortuneEnvelope => "Gain bonus gold instantly.",
                    ItemType.StoneShift => "Clear the selected non-given cell.",
                    ItemType.HarmonyCharm => "Gain mistake shield charges.",
                    ItemType.CompassOfOrder => "Reveal one clear candidate in the selected cell.",
                    _ => "Use the item for a tactical boost."
                };
                return $"{slot.RolledItem.Type} ({slot.RolledItem.Rarity})\n{effect}";
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
                return $"Gold +{slot.NothingGoldBonus}";
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
            {
                return "Unknown offer";
            }

            if (offer.IsRelic)
            {
                return $"Relic {offer.RelicId}\nPrice: {offer.Price}g";
            }

            var item = offer.Item;
            if (item == null)
            {
                return $"Offer price: {offer.Price}g";
            }

            return
                $"{item.Type} ({item.Rarity})\n" +
                $"Charges: {item.Charges}\n" +
                $"Price: {offer.Price}g";
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

            var usedType = run.RunState != null && index >= 0 && index < run.RunState.Inventory.Count && run.RunState.Inventory[index] != null
                ? run.RunState.Inventory[index].Type
                : ItemType.Solver;

            if (!run.TryUseInventoryItemAt(index, _selectedRow, _selectedCol, out var message))
            {
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

            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            const float duration = 0.16f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rect.localScale = Vector3.LerpUnclamped(from, to, 1f - Mathf.Pow(1f - t, 3f));
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
                ItemType.TeaOfFocus => "icon_tea_cup",
                ItemType.CherryBlossomPact => "icon_golden_bloom",
                ItemType.FortuneEnvelope => "icon_sakura_coin",
                ItemType.StoneShift => "icon_stone_gear",
                ItemType.HarmonyCharm => "icon_jade_amulet",
                ItemType.CompassOfOrder => "icon_temple_seal",
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
                _levelInfoText.text = isTutorial ? string.Empty : $"Level: {state.Level}  Depth: {state.Depth}";
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
                gameOverDetailsText.text =
                    $"Class: {run.RunState.ClassId}\n" +
                    $"Depth reached: {run.RunState.Depth}\n" +
                    $"HP: {run.RunState.CurrentHP}/{run.RunState.MaxHP}\n" +
                    $"Pencil: {run.RunState.CurrentPencil}/{run.RunState.MaxPencil}";
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

            // Lines (whispers, parity, renban)
            for (var li = 0; li < overlay.Lines.Count; li++)
            {
                var line = overlay.Lines[li];
                var lineColor = GetLineColor(line.Type);
                for (var ci = 0; ci < line.Cells.Count - 1; ci++)
                {
                    var a = line.Cells[ci];
                    var b = line.Cells[ci + 1];
                    DrawLineBetweenCells(a, b, lineColor, 4f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                }

                for (var ci = 0; ci < line.Cells.Count; ci++)
                {
                    var c = line.Cells[ci];
                    DrawCellDot(c, lineColor, 8f, cellW, cellH, spaceX, spaceY, totalW, totalH);
                }
            }

            // Kropki dots
            for (var di = 0; di < overlay.Dots.Count; di++)
            {
                var dot = overlay.Dots[di];
                var dotColor = dot.Type == DotType.White ? WhiteDotColor : BlackDotColor;
                var mid = new CellCoord(0, 0);
                var mr = (dot.CellA.Row + dot.CellB.Row) / 2f;
                var mc = (dot.CellA.Col + dot.CellB.Col) / 2f;
                var posX = mc * (cellW + spaceX) + cellW * 0.5f - totalW * 0.5f;
                var posY = -(mr * (cellH + spaceY) + cellH * 0.5f - totalH * 0.5f);
                DrawOverlayCircle(posX, posY, 10f, dotColor);
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

                var sumIdx = topLeft.Row * size + topLeft.Col;
                if (sumIdx >= 0 && sumIdx < _cells.Count)
                {
                    // Parent to the number root so the sum renders above overlay lines
                    Transform sumParent = (_gridNumberRoot != null && sumIdx < _gridNumberRoot.childCount)
                        ? _gridNumberRoot.GetChild(sumIdx)
                        : _cells[sumIdx].Root;
                    var sumGo = new GameObject("CageSum", typeof(RectTransform), typeof(Image), typeof(Text));
                    sumGo.transform.SetParent(sumParent, false);
                    var sumRect = sumGo.GetComponent<RectTransform>();
                    sumRect.anchorMin = new Vector2(0f, 0.68f);
                    sumRect.anchorMax = new Vector2(0.48f, 1f);
                    sumRect.offsetMin = Vector2.zero;
                    sumRect.offsetMax = Vector2.zero;
                    var sumBg = sumGo.GetComponent<Image>();
                    sumBg.color = new Color(0f, 0f, 0f, 0.55f);
                    sumBg.raycastTarget = false;
                    var sumText = sumGo.GetComponent<Text>();
                    sumText.text = cage.Sum.ToString();
                    sumText.fontSize = 11;
                    sumText.alignment = TextAnchor.MiddleCenter;
                    sumText.color = new Color(0.90f, 0.15f, 0.12f, 1f);
                    sumText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    sumText.raycastTarget = false;
                    _overlayObjects.Add(sumGo);
                }
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
                DrawCellDot(arrow.Circle, ArrowCircleColor, 18f, cellW, cellH, spaceX, spaceY, totalW, totalH);

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
        }

        private void DrawLineBetweenCells(CellCoord a, CellCoord b, Color color, float width,
            float cellW, float cellH, float spX, float spY, float totalW, float totalH)
        {
            var ax = a.Col * (cellW + spX) + cellW * 0.5f - totalW * 0.5f;
            var ay = -(a.Row * (cellH + spY) + cellH * 0.5f - totalH * 0.5f);
            var bx = b.Col * (cellW + spX) + cellW * 0.5f - totalW * 0.5f;
            var by = -(b.Row * (cellH + spY) + cellH * 0.5f - totalH * 0.5f);

            var go = new GameObject("OverlayLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rect = go.GetComponent<RectTransform>();

            var dx = bx - ax;
            var dy = by - ay;
            var len = Mathf.Sqrt(dx * dx + dy * dy);
            var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(ax, ay);
            rect.sizeDelta = new Vector2(len, width);
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

        private void DrawOverlayCircle(float x, float y, float radius, Color color)
        {
            var go = new GameObject("OverlayDot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);

            var img = go.GetComponent<Image>();
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
