using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;
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
        private RectTransform _pathOverlayRoot;
        private Vector2 _laneAEndPanelLocal;
        private Vector2 _laneBEndPanelLocal;
        private bool _hasLaneAEnd;
        private bool _hasLaneBEnd;
        private int _lastPuzzleItemSignature = int.MinValue;

        private const string ReturnTutorialProgressPrefKey = "sr_return_to_tutorial_progress";

        private static readonly Color EmptyColor = new(0.12f, 0.18f, 0.14f, 1f);
        private static readonly Color GivenColor = new(0.20f, 0.29f, 0.20f, 1f);
        private static readonly Color RowColHighlight = new(0.18f, 0.34f, 0.56f, 1f);
        private static readonly Color SelectedColor = new(0.73f, 0.49f, 0.18f, 1f);
        private static readonly Color MatchValueColor = new(0.36f, 0.24f, 0.58f, 1f);

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

            ShowRewardScreen();
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
                var overview =
                    "Garden Overview\n" +
                    $"HP: {runState.CurrentHP}/{runState.MaxHP}    Gold: {runState.CurrentGold}    Pencil: {runState.CurrentPencil}/{runState.MaxPencil}\n" +
                    $"Items: {runState.Inventory.Count}    Relics: {runState.RelicIds.Count}";
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

            if (boss == null || !_hasLaneAEnd || !_hasLaneBEnd)
            {
                return;
            }

            var width = Mathf.Max(320f, _pathOverlayRoot.rect.width);
            var height = Mathf.Max(220f, _pathOverlayRoot.rect.height);
            var bossPos = new Vector2(width * 0.5f, Mathf.Clamp(height * 0.14f, 56f, height - 42f));

            CreatePathConnectionLine(_pathOverlayRoot, _laneAEndPanelLocal, bossPos);
            CreatePathConnectionLine(_pathOverlayRoot, _laneBEndPanelLocal, bossPos);

            var bossButton = CreatePathNodeButton(_pathOverlayRoot, boss, risk: false, bossPos);
            bossButton.onClick.RemoveAllListeners();
            bossButton.onClick.AddListener(() => ChoosePath(false));
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
            const float minDist = 90f;

            for (var pass = 0; pass < 8; pass++)
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
            var t = count <= 1 ? 0.5f : index / (float)(count - 1);

            var seedA = Mathf.Abs((node.Depth * 73856093) ^ (index * 29791) ^ (risk ? 19349663 : 83492791));
            var seedB = Mathf.Abs((node.Depth * 83492791) ^ (index * 17389) ^ (risk ? 923521 : 289133));

            var jitterX = (((seedA % 1000) / 999f) - 0.5f) * (width * 0.24f);
            var jitterY = (((seedB % 1000) / 999f) - 0.5f) * (height * 0.10f);

            var centerX = width * (risk ? 0.58f : 0.42f);
            var baseX = centerX + jitterX + Mathf.Sin((node.Depth + (risk ? 3 : 0)) * 1.35f) * (width * 0.08f);
            var baseY = Mathf.Lerp(height - 52f, 52f, t) + jitterY;

            return new Vector2(Mathf.Clamp(baseX, 46f, width - 46f), Mathf.Clamp(baseY, 46f, height - 46f));
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
            goRect.sizeDelta = new Vector2(78f, 78f);
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

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(go.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.08f, 0.20f);
            titleRect.anchorMax = new Vector2(0.92f, 0.80f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var title = titleGo.GetComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 15;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.94f, 0.95f, 0.87f, 1f);

            var topLeftGo = new GameObject("BoardSize", typeof(RectTransform), typeof(Text));
            topLeftGo.transform.SetParent(go.transform, false);
            var topLeftRect = topLeftGo.GetComponent<RectTransform>();
            topLeftRect.anchorMin = new Vector2(0.04f, 0.60f);
            topLeftRect.anchorMax = new Vector2(0.36f, 0.95f);
            topLeftRect.offsetMin = Vector2.zero;
            topLeftRect.offsetMax = Vector2.zero;

            var topLeft = topLeftGo.GetComponent<Text>();
            topLeft.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            topLeft.fontSize = 13;
            topLeft.alignment = TextAnchor.UpperLeft;
            topLeft.color = new Color(1f, 0.90f, 0.64f, 1f);

            var bottomRightGo = new GameObject("Stars", typeof(RectTransform), typeof(Text));
            bottomRightGo.transform.SetParent(go.transform, false);
            var bottomRightRect = bottomRightGo.GetComponent<RectTransform>();
            bottomRightRect.anchorMin = new Vector2(0.58f, 0.04f);
            bottomRightRect.anchorMax = new Vector2(0.96f, 0.40f);
            bottomRightRect.offsetMin = Vector2.zero;
            bottomRightRect.offsetMax = Vector2.zero;

            var bottomRight = bottomRightGo.GetComponent<Text>();
            bottomRight.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            bottomRight.fontSize = 13;
            bottomRight.alignment = TextAnchor.LowerRight;
            bottomRight.color = new Color(0.99f, 0.85f, 0.46f, 1f);

            if (runMapController.TryGetFixedLevelForNode(node, out var config))
            {
                title.text = node.Type == NodeType.Boss ? "Boss Gate" : node.Type.ToString();
                var puzzleNode = IsPuzzleNodeType(node.Type);
                topLeft.text = puzzleNode && node.Type != NodeType.Boss ? $"{config.BoardSize}x{config.BoardSize}" : string.Empty;
                bottomRight.text = puzzleNode ? (node.Type == NodeType.Boss ? "Final" : $"{config.Stars}*") : string.Empty;
            }
            else
            {
                title.text = node.Type == NodeType.Boss ? "Boss Gate" : node.Type.ToString();
                topLeft.text = string.Empty;
                bottomRight.text = string.Empty;
            }

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
            sudokuGridRoot.anchorMin = new Vector2(0.5f, 0.5f);
            sudokuGridRoot.anchorMax = new Vector2(0.5f, 0.5f);
            sudokuGridRoot.pivot = new Vector2(0.5f, 0.5f);
            sudokuGridRoot.anchoredPosition = Vector2.zero;
            sudokuGridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalW);
            sudokuGridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalH);

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var cellGo = new GameObject($"Cell_{row}_{col}", typeof(RectTransform), typeof(Image), typeof(Button));
                    cellGo.transform.SetParent(sudokuGridRoot, false);

                    var image = cellGo.GetComponent<Image>();
                    image.color = EmptyColor;

                    var button = cellGo.GetComponent<Button>();
                    var capturedRow = row;
                    var capturedCol = col;
                    button.onClick.AddListener(() => OnCellClicked(capturedRow, capturedCol));

                    var textGo = new GameObject("Value", typeof(RectTransform), typeof(Text));
                    textGo.transform.SetParent(cellGo.transform, false);
                    var textRect = textGo.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;

                    var text = textGo.GetComponent<Text>();
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    text.fontSize = size <= 6 ? 30 : size <= 8 ? 24 : 20;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = new Color(0.93f, 0.96f, 0.90f, 1f);

                    var pencilGo = new GameObject("Pencil", typeof(RectTransform), typeof(Text));
                    pencilGo.transform.SetParent(cellGo.transform, false);
                    var pencilRect = pencilGo.GetComponent<RectTransform>();
                    pencilRect.anchorMin = new Vector2(0.08f, 0.08f);
                    pencilRect.anchorMax = new Vector2(0.92f, 0.92f);
                    pencilRect.offsetMin = Vector2.zero;
                    pencilRect.offsetMax = Vector2.zero;

                    var pencilText = pencilGo.GetComponent<Text>();
                    pencilText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    pencilText.fontSize = size <= 6 ? 16 : 14;
                    pencilText.alignment = TextAnchor.UpperLeft;
                    pencilText.color = new Color(0.84f, 0.86f, 0.82f, 0.95f);
                    pencilText.supportRichText = true;

                    var borderTop = CreateCellBorder(cellGo.transform, "BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 0f));
                    var borderBottom = CreateCellBorder(cellGo.transform, "BorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 3f));
                    var borderLeft = CreateCellBorder(cellGo.transform, "BorderLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(3f, 0f));
                    var borderRight = CreateCellBorder(cellGo.transform, "BorderRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(0f, 0f));

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

            var btnGo = new GameObject("BtnPencilMode", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(numpadRoot.parent != null ? numpadRoot.parent : numpadRoot, false);

            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.93f, 0.26f);
            rect.anchorMax = new Vector2(0.93f, 0.26f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(120f, 32f);
            rect.anchoredPosition = Vector2.zero;

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

                cell.Image.color = color;
                UpdateCellBorders(board, cell);
            }

            UpdateNumpadSolvedState(board);
        }

        private static void UpdateCellBorders(SudokuBoard board, CellView cell)
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

            if (_shopSummaryText != null)
            {
                _shopSummaryText.text =
                    "Shop Node\n" +
                    $"Gold: {run.RunState.CurrentGold}\n" +
                    "Choose one offer or skip.";
            }

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
                    child.name == "ShopSkip")
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
                    var replaceButton = BuildPanelButton(_shopPanel.transform, $"ShopReplace_{i}", new Vector2(0.08f + ((i % 3) * 0.29f), 0.48f - ((i / 3) * 0.18f)), new Vector2(0.34f, 0.14f), new Color(0.28f, 0.22f, 0.18f, 0.95f));
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

                var cancel = BuildPanelButton(_shopPanel.transform, "ShopSkip", new Vector2(0.33f, 0.24f), new Vector2(0.34f, 0.12f), new Color(0.25f, 0.26f, 0.28f, 0.95f));
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
                var btn = BuildPanelButton(_shopPanel.transform, $"ShopChoice_{i}", new Vector2(0.08f + (i * 0.29f), 0.42f), new Vector2(0.26f, 0.26f), new Color(0.17f, 0.26f, 0.32f, 0.95f));
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

            var skip = BuildPanelButton(_shopPanel.transform, "ShopSkip", new Vector2(0.33f, 0.24f), new Vector2(0.34f, 0.12f), new Color(0.25f, 0.26f, 0.28f, 0.95f));
            skip.onClick.AddListener(() => CloseShopPanel(false));
            var skipLabel = skip.GetComponentInChildren<Text>();
            if (skipLabel != null)
            {
                skipLabel.text = "Take Nothing";
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
            CloseShopPanel(true);
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

            _pathOverlayMessage = $"Relic Node\nAcquired relic: {relicId}\nEffect: +1 Max Pencil.";
            SetStatus("Relic acquired.");
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
                _inventoryBadgeRoot.anchorMin = new Vector2(0.02f, 0.89f);
                _inventoryBadgeRoot.anchorMax = new Vector2(0.98f, 0.99f);
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

            if (relicId.Contains("gold", StringComparison.OrdinalIgnoreCase)) return $"{relicId}: improves gold economy.";
            if (relicId.Contains("silent", StringComparison.OrdinalIgnoreCase)) return $"{relicId}: mistake protection effect.";
            if (relicId.Contains("garden", StringComparison.OrdinalIgnoreCase)) return $"{relicId}: route and pressure mutation.";
            return $"{relicId}: passive relic effect active.";
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

                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ =>
                {
                    if (_rewardHoverText != null)
                    {
                        _rewardHoverText.text = DescribeRollSlot(slot);
                    }
                });
                trigger.triggers.Add(enter);

                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ =>
                {
                    if (_rewardHoverText != null)
                    {
                        _rewardHoverText.text = "Hover a reward to inspect details.";
                    }
                });
                trigger.triggers.Add(exit);
            }

            _rewardPanel.SetActive(true);
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
                run.PickRolledSlot(_pendingRewardSlots, slotIndex);
            }

            _pendingRewardSlots.Clear();
            _awaitingRewardChoice = false;
            HideRewardPanel();
            SetStatus("Reward claimed. Choose next path tile.");
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
                return $"Gold +{slot.NothingGoldBonus}";
            }

            if (slot.RolledItem != null)
            {
                return $"{slot.RolledItem.Type}\n{slot.RolledItem.Rarity}";
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

        private Button BuildPanelButton(Transform parent, string name, Vector2 anchorMin, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

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

            if (!run.TryUseInventoryItemAt(index, _selectedRow, _selectedCol, out var message))
            {
                SetStatus(string.IsNullOrWhiteSpace(message) ? "Item usage failed." : message);
                return;
            }

            SetStatus(message);
            RenderBoard(board);
            RefreshHud();
            RefreshSolveButtonState();
            _lastPuzzleItemSignature = int.MinValue;
            RebuildPuzzleItemBar();
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
                _levelInfoText.text = $"Level: {state.Level}  Depth: {state.Depth}";
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
