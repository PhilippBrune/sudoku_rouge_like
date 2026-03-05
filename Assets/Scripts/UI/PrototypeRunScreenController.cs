using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using UnityEngine;
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

        private static readonly Color EmptyColor = new(0.12f, 0.18f, 0.14f, 1f);
        private static readonly Color GivenColor = new(0.20f, 0.29f, 0.20f, 1f);
        private static readonly Color RowColHighlight = new(0.25f, 0.38f, 0.26f, 1f);
        private static readonly Color SelectedColor = new(0.50f, 0.66f, 0.42f, 1f);
        private static readonly Color MatchValueColor = new(0.32f, 0.52f, 0.35f, 1f);

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
                choosePathAButton.onClick.RemoveAllListeners();
                choosePathAButton.onClick.AddListener(() => ChoosePath(false));
            }

            if (choosePathBButton != null)
            {
                choosePathBButton.onClick.RemoveAllListeners();
                choosePathBButton.onClick.AddListener(() => ChoosePath(true));
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
                return;
            }

            SetStatus("Sudoku solved. Choose next path tile.");
            ShowPathOverview();
            RefreshPathOverview();
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
        }

        private void ChoosePath(bool risk)
        {
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

            SetStatus($"Path {(risk ? "B" : "A")} selected: {node.Type}, {level.BoardSize}x{level.BoardSize}, {level.Stars}★");
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
                pathOverviewText.text = "Garden Overview\nChoose your first tile, then your lane is locked until the boss.";
            }

            if (laneAText != null)
            {
                laneAText.text = "Path A (Calm)";
            }

            if (laneBText != null)
            {
                laneBText.text = "Path B (Risk)";
            }

            var previewA = runMapController.BuildPathChoicePreview(false);
            var previewB = runMapController.BuildPathChoicePreview(true);
            var lockValue = previewA.LockedPath ?? previewB.LockedPath;

            if (choosePathAButton != null)
            {
                choosePathAButton.interactable = previewA.Available && (!lockValue.HasValue || lockValue.Value == false);
            }

            if (choosePathBButton != null)
            {
                choosePathBButton.interactable = previewB.Available && (!lockValue.HasValue || lockValue.Value == true);
            }

            var nextSignature = BuildLaneRenderSignature(previewA, previewB, lockValue);
            if (nextSignature != _lastLaneRenderSignature)
            {
                _lastLaneRenderSignature = nextSignature;
                RebuildLaneNodeButtons(false, laneAPathRoot, previewA.Available && (!lockValue.HasValue || lockValue.Value == false));
                RebuildLaneNodeButtons(true, laneBPathRoot, previewB.Available && (!lockValue.HasValue || lockValue.Value == true));
            }
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

            var graph = runMapController.Run.CurrentRunGraph;
            for (var i = 0; i < graph.Count; i++)
            {
                var node = graph[i];
                if (node == null || node.Depth <= 1)
                {
                    continue;
                }

                if (node.Type != NodeType.Boss && node.IsRiskPath != risk)
                {
                    continue;
                }

                var button = CreatePathNodeButton(root, node, risk);
                button.interactable = laneIsAvailable && IsNextChoiceNode(node, risk);
            }
        }

        private Button CreatePathNodeButton(RectTransform parent, RunNode node, bool risk)
        {
            var go = new GameObject($"PathNode_{(risk ? "B" : "A")}_{node.Depth}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = node.Type == NodeType.Boss ? new Color(0.38f, 0.16f, 0.10f, 1f) : new Color(0.19f, 0.28f, 0.20f, 1f);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 56f;
            layout.preferredHeight = 62f;

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
                topLeft.text = node.Type == NodeType.Boss ? string.Empty : $"{config.BoardSize}x{config.BoardSize}";
                bottomRight.text = node.Type == NodeType.Boss ? "Final" : $"{config.Stars}*";
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
            var calm = Resources.Load<Sprite>("GeneratedIcons/icon_relic_pedestal");
            var risk = Resources.Load<Sprite>("GeneratedIcons/icon_moss_trap");
            var quit = Resources.Load<Sprite>("GeneratedIcons/icon_ink_save");

            if (calm == null || risk == null || quit == null)
            {
                return;
            }

            ApplyButtonIcon(choosePathAButton, calm);
            ApplyButtonIcon(choosePathBButton, risk);
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

            UpdateNumpadAvailability(board.Size);
            RenderBoard(board);

            if ((_selectedRow < 0 || _selectedCol < 0) && TryFindFirstEditableCell(board, out var row, out var col))
            {
                _selectedRow = row;
                _selectedCol = col;
            }
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
            grid.cellSize = size <= 6 ? new Vector2(88f, 88f) : size <= 8 ? new Vector2(66f, 66f) : new Vector2(56f, 56f);

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

                    _cells.Add(new CellView
                    {
                        Row = row,
                        Col = col,
                        Root = cellGo.GetComponent<RectTransform>(),
                        Image = image,
                        Label = text,
                        Button = button
                    });
                }
            }
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

            for (var value = 1; value <= 9; value++)
            {
                var btn = CreateNumpadButton(value);
                _numpadButtons.Add(btn);
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

            if (runMapController?.Run?.RunState?.TutorialMode == true && board.IsGiven(row, col))
            {
                if (TryFindFirstEditableCell(board, out var fallbackRow, out var fallbackCol))
                {
                    row = fallbackRow;
                    col = fallbackCol;
                }
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

            var ok = run.PlaceNumber(_selectedRow, _selectedCol, value);
            SetStatus(ok ? $"Placed {value}." : $"{value} is incorrect. HP now {run.RunState.CurrentHP}.");
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
                cell.Label.color = given ? new Color(1f, 0.95f, 0.78f, 1f) : new Color(0.93f, 0.96f, 0.90f, 1f);

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
            Debug.Log("PrototypeRunScreenController: Save & Quit triggered.");
            runMapController?.Run?.OnQuitRequested();
            Debug.Log("PrototypeRunScreenController: Auto-save requested via RunDirector.OnQuitRequested().");

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
            SetSquareButton(choosePathAButton);
            SetSquareButton(choosePathBButton);
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

            if (hpText != null)
            {
                hpText.text = $"HP: {state.CurrentHP}/{state.MaxHP}";
            }

            if (pencilText != null)
            {
                pencilText.text = $"Pencil: {state.CurrentPencil}/{state.MaxPencil}";
            }
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
                    $"Pencil: {run.RunState.CurrentPencil}/{run.RunState.MaxPencil}\n" +
                    $"Heat: {run.RunState.CurrentHeatScore:0.00}";
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
            public Button Button;
        }
    }
}
