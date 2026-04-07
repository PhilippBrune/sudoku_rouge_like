using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
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
        private Text _overviewText;
        private Text _statusText;

        // ── Sub-controllers ──
        private BoardViewController _boardView;
        private HudViewController _hudView;
        private NumpadViewController _numpadView;
        private RewardViewController _rewardView;
        private ShopViewController _shopView;
        private BossGateViewController _bossGateView;
        private EndScreenViewController _endScreenView;

        // ── Game state ──
        private int _selectedRow = -1;
        private int _selectedCol = -1;
        private int _highlightValue;
        private bool _pencilMode;
        private bool _completionHandled;
        private bool _resumeApplied;
        private bool _tutorialShown;

        // ── Swap state (Silk Fan item) ──
        private bool _swapMode;
        private int _swapRow1 = -1, _swapCol1 = -1;
        private int _lastMistakeRow = -1, _lastMistakeCol = -1;

        // ── Item state ──
        private int _umbrellaCharges;
        private int _fogDisabledMovesRemaining;
        private HashSet<(int row, int col)> _bagHighlightCells;
        private float _bagHighlightEndTime;

        // ── Path overlay ──
        private RectTransform _pathOverlayRoot;
        private string _pathMessage = string.Empty;

        // ── Options / Boss ──
        private GameObject _inGameOptionsPanel;
        private int _bossNodeIndex = -1;

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
            _overviewText  = _pathPanel?.transform.Find("PathOverviewText")?.GetComponent<Text>();
            _statusText    = _sudokuPanel?.transform.Find("SudokuGameplayStatus")?.GetComponent<Text>();
            _inGameOptionsPanel = uiRoot.Find("InGameOptionsPanel")?.gameObject;

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
            _hudView.Configure(map, _sudokuPanel, hpText, pencilText);
            _numpadView.Configure(numpadRoot);
            _rewardView.Configure(map, _pathPanel);
            _shopView.Configure(map, _pathPanel,
                (item, onReplace, onAbort) => _rewardView.ShowBagSwapPanel(item, onReplace, onAbort));
            _bossGateView.Configure(map);
            _endScreenView.Configure(map, _gameOverPanel, goSummary, goDetails, goBack);

            // ── Wire callbacks ──
            _boardView.OnCellClicked        = OnCellClicked;
            _numpadView.OnNumberPressed     = EnterNumber;
            _numpadView.OnPencilModeToggled = () => _pencilMode = _numpadView.PencilMode;

            _hudView.OnStatusChanged         = msg => SetStatus(msg);
            _hudView.OnItemEffectRequested   = ApplyItemEffect;
            _hudView.OnBagHighlightRequested = (cells, endTime) =>
            {
                _bagHighlightCells = cells;
                _bagHighlightEndTime = endTime;
            };

            _rewardView.OnStatusChanged   = msg => SetStatus(msg);
            _rewardView.OnPostRewardReady = OnPostRewardReady;
            _shopView.OnStatusChanged     = msg => SetStatus(msg);
            _shopView.OnShopClosed        = ShowPath;
            _bossGateView.OnModsConfirmed = OnBossModsConfirmed;
            _endScreenView.OnReturnToMenuPressed = SaveAndQuit;

            ShowPath();
        }

        public void NotifyRunStarted()
        {
            _audio = FindFirstObjectByType<RunAudioController>();
            _resumeApplied     = false;
            _tutorialShown     = false;
            _completionHandled = false;
            _endScreenView.ResetShown();
            RefreshAccessibility();
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
            var save = new SaveFileService();
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
                if (_map?.Run?.State != null && _map.Run.State.TutorialMode)
                {
                    var sqLabel = sqBtn.GetComponentInChildren<Text>();
                    if (sqLabel != null) sqLabel.text = "Quit (Q)";
                }
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
        }

        private void ToggleOptionsPanel()
        {
            if (_inGameOptionsPanel != null)
                _inGameOptionsPanel.SetActive(!_inGameOptionsPanel.activeSelf);
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
            if (run == null) return;

            if (!_resumeApplied) { ApplyResumeState(); return; }

            if (!_tutorialShown && run.State != null && run.State.TutorialMode)
            {
                _tutorialShown = true;
                ShowSudoku();
                RebuildBoard();
            }

            if (_sudokuPanel != null && _sudokuPanel.activeSelf)
            {
                HandleKeyboard();
                HandleCompletion();
                _hudView.Refresh(run.State, run.CurrentLevelConfig);
                CheckGameOver();
            }
        }

        // ─────────────────────────── Screen transitions ───────────────────────────

        private void ShowPath()
        {
            var run = _map?.Run;
            if (run?.State != null && run.State.TutorialMode) { ShowSudoku(); return; }
            SetActive(_pathPanel, true);
            SetActive(_sudokuPanel, false);
            SetActive(_gameOverPanel, false);
            _audio?.SetContext(RunAudioController.Context.Path);
            RefreshPathOverview();
        }

        private void ShowSudoku()
        {
            SetActive(_pathPanel, false);
            SetActive(_sudokuPanel, true);
            SetActive(_gameOverPanel, false);
            var floor = _map?.Run?.State?.CurrentFloor ?? 0;
            _audio?.SetFloorIndex(floor);
            _audio?.SetContext(RunAudioController.Context.Puzzle);
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
                _bagHighlightCells, _fogDisabledMovesRemaining, _accessibility);

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
            var board = _map?.Run?.CurrentBoard;
            if (board == null) return;
            _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight),
                _bagHighlightCells, _fogDisabledMovesRemaining);
        }

        // ─────────────────────────── Path overview ───────────────────────────

        private void RefreshPathOverview()
        {
            var run = _map?.Run;
            if (run?.State == null || run.CurrentFloorGraph == null) return;

            var s         = run.State;
            var floorName = FloorThemeData.GetFloorName(s.CurrentFloor);
            var floorTint = FloorThemeData.GetFloorTint(s.CurrentFloor);

            if (_overviewText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{floorName} - Garden {s.CurrentFloor + 1}/{s.TotalFloors}");
                sb.AppendLine($"HP: {s.CurrentHP}/{s.MaxHP}  Gold: {s.CurrentGold}  Pencil: {s.CurrentPencil}/{s.MaxPencil}");
                if (s.HasRelic)
                    sb.AppendLine($"Relic: {s.HeldRelic.Id}");
                if (!string.IsNullOrEmpty(_pathMessage))
                    sb.AppendLine(_pathMessage);
                _overviewText.text = sb.ToString().TrimEnd();
            }

            RebuildPathCanvas(run, floorTint);
        }

        private void RebuildPathCanvas(RunDirector run, Color floorTint)
        {
            EnsurePathOverlayRoot();
            if (_pathOverlayRoot == null) return;
            InRunUiFactory.ClearChildren(_pathOverlayRoot);

            var graph = run.CurrentFloorGraph;
            if (graph == null || graph.Count == 0) return;

            var w         = Mathf.Max(400f, _pathOverlayRoot.rect.width);
            var h         = Mathf.Max(300f, _pathOverlayRoot.rect.height);
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

            // Per-route lane background boxes (drawn first = behind everything)
            var calmBg      = new Color(floorTint.r * 0.22f, floorTint.g * 0.26f, floorTint.b * 0.32f, 0.88f);
            var calmOutline = new Color(floorTint.r * 0.60f, floorTint.g * 0.78f, floorTint.b * 0.92f, 0.75f);
            var riskBg      = new Color(floorTint.r * 0.26f + 0.12f, floorTint.g * 0.16f, floorTint.b * 0.12f, 0.88f);
            var riskOutline = new Color(0.88f, 0.40f, 0.18f, 0.75f);

            if (calmLane.Count > 0)
                DrawRouteLane(_pathOverlayRoot, calmLane, w, h, calmBg, calmOutline, "CalmLane", "Calm Path");
            if (riskLane.Count > 0)
                DrawRouteLane(_pathOverlayRoot, riskLane, w, h, riskBg, riskOutline, "RiskLane", "Risk Path");

            var pathColor     = new Color(floorTint.r * 0.75f, floorTint.g * 0.75f, floorTint.b * 0.75f, 0.70f);
            var accentColor   = InRunUiFactory.AccentGold;
            var bossGateColor = new Color(0.7f, 0.2f, 0.2f, 1f);

            // Draw edges
            for (var i = 0; i < graph.Count; i++)
            {
                var node = graph[i];
                for (var n = 0; n < node.NextNodes.Count; n++)
                {
                    var next = node.NextNodes[n];
                    if (next >= 0 && next < graph.Count)
                        DrawEdge(_pathOverlayRoot, w, h, graph[i], graph[next], pathColor);
                }
            }

            // Draw nodes
            for (var i = 0; i < graph.Count; i++)
            {
                var node      = graph[i];
                var pos       = new Vector2(node.CanvasX * w, (1f - node.CanvasY) * h);
                var isCurrent = i == run.State.CurrentNodeIndex;
                var canReach  = reachable != null && reachable.Contains(i);

                var color = node.Visited ? new Color(floorTint.r, floorTint.g, floorTint.b, 0.4f)
                    : isCurrent ? accentColor
                    : node.Type == NodeType.Boss ? bossGateColor
                    : floorTint;

                var nodeConfig = IsPuzzleNodeType(node.Type) ? _map.GetFixedLevelConfig(node) : null;
                var btn = CreateNodeButton(_pathOverlayRoot, node, pos, color, nodeConfig);
                btn.interactable = canReach && !node.Visited;
                var idx = i;
                btn.onClick.AddListener(() => OnNodeClicked(idx));
            }
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

            var ol = go.AddComponent<Outline>();
            ol.effectColor    = outlineColor;
            ol.effectDistance = new Vector2(2f, -2f);

            // Lane label in top-left corner of the box
            var lbl = InRunUiFactory.CreateText(go.transform, "LaneLabel", label, 10,
                TextAnchor.UpperLeft, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.85f));
            lbl.rectTransform.anchorMin = new Vector2(0f, 0.80f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 1f);
            lbl.rectTransform.offsetMin = new Vector2(6f, 0f);
            lbl.rectTransform.offsetMax = Vector2.zero;
            lbl.raycastTarget = false;
        }

        private void OnNodeClicked(int nodeIndex)
        {
            if (_rewardView.IsAwaitingReward || _bossGateView.IsAwaiting) return;
            var run = _map?.Run;
            if (run == null) return;

            var graph = run.CurrentFloorGraph;
            if (graph == null || nodeIndex < 0 || nodeIndex >= graph.Count) return;
            var node = graph[nodeIndex];

            // Boss gate requires modifier choice first (only if modifiers not yet chosen)
            if (node.Type == NodeType.Boss && (run.State.ChosenBossModifiers == null || run.State.ChosenBossModifiers.Count == 0))
            {
                _bossNodeIndex = nodeIndex;
                _bossGateView.ShowBossGateChoice();
                return;
            }

            if (!_map.TryAdvanceToNodeAndStartPuzzle(nodeIndex, out var arrivedNode, out var level))
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

            if (arrivedNode.Type == NodeType.Shop) { _shopView.ShowShop(); return; }
            if (arrivedNode.Type == NodeType.Rest) { _rewardView.ShowRestChoicePanel(); ShowPath(); return; }
            if (arrivedNode.Type == NodeType.Event) { _rewardView.HandleEvent(); ShowPath(); return; }
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
                        SetStatus($"Cursed {cfg.BoardSize}×{cfg.BoardSize} {cfg.Stars}★");
                        ShowSudoku(); RebuildBoard();
                    },
                    cfg =>
                    {
                        _map.Run.StartLevel(cfg);
                        _completionHandled = false; _boardView.MarkOverlayDirty();
                        _selectedRow = -1; _selectedCol = -1; _highlightValue = 0;
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

        private void EnsurePathOverlayRoot()
        {
            if (_pathOverlayRoot != null || _pathPanel == null) return;
            var go = new GameObject("PathOverlay", typeof(RectTransform));
            go.transform.SetParent(_pathPanel.GetComponent<RectTransform>(), false);
            _pathOverlayRoot = go.GetComponent<RectTransform>();
            _pathOverlayRoot.anchorMin = Vector2.zero;
            _pathOverlayRoot.anchorMax = Vector2.one;
            _pathOverlayRoot.offsetMin = Vector2.zero;
            _pathOverlayRoot.offsetMax = Vector2.zero;
        }

        private void DrawEdge(RectTransform parent, float w, float h, RunNode a, RunNode b, Color color)
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
            r.sizeDelta = new Vector2(dist, 3f);
            r.anchoredPosition = pa + d * 0.5f;
            r.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        private static bool IsPuzzleNodeType(NodeType t) =>
            t == NodeType.Puzzle || t == NodeType.ElitePuzzle || t == NodeType.PreBoss || t == NodeType.Boss;

        private Button CreateNodeButton(RectTransform parent, RunNode node, Vector2 pos, Color color, LevelConfig config = null)
        {
            var isBoss = node.Type == NodeType.Boss;
            var size   = isBoss ? new Vector2(120, 60) : new Vector2(72, 72);
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
            var iconName = GetNodeIconName(node.Type);
            var sprite   = string.IsNullOrEmpty(iconName) ? null : Resources.Load<Sprite>("GeneratedIcons/icon_" + iconName);
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

                var lbl = InRunUiFactory.CreateText(go.transform, "Label", GetNodeLabelShort(node),
                    isBoss ? 11 : 10, TextAnchor.MiddleCenter, InRunUiFactory.TextColor);
                lbl.rectTransform.anchorMin = new Vector2(0.40f, 0f);
                lbl.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                lbl.rectTransform.offsetMin = Vector2.zero;
                lbl.rectTransform.offsetMax = Vector2.zero;
                lbl.verticalOverflow = VerticalWrapMode.Overflow;
            }
            else
            {
                var lbl = InRunUiFactory.CreateText(go.transform, "Label", GetNodeLabelShort(node),
                    isBoss ? 13 : 11, TextAnchor.MiddleCenter, InRunUiFactory.TextColor);
                InRunUiFactory.StretchFill(lbl.rectTransform);
            }

            // Puzzle-type nodes: top-left size, bottom-right stars
            if (config != null && IsPuzzleNodeType(node.Type))
            {
                var sizeLabel = InRunUiFactory.CreateText(go.transform, "SizeLabel",
                    $"{config.BoardSize}×{config.BoardSize}", 9, TextAnchor.UpperLeft,
                    new Color(InRunUiFactory.TextColor.r, InRunUiFactory.TextColor.g, InRunUiFactory.TextColor.b, 0.85f));
                sizeLabel.rectTransform.anchorMin = new Vector2(0f, 0.72f);
                sizeLabel.rectTransform.anchorMax = new Vector2(0.55f, 1f);
                sizeLabel.rectTransform.offsetMin = new Vector2(3f, 0f);
                sizeLabel.rectTransform.offsetMax = new Vector2(0f, -2f);
                sizeLabel.raycastTarget = false;

                var stars    = Mathf.Clamp(config.Stars, 1, 6);
                var starStr  = new string('★', stars);
                var starLabel = InRunUiFactory.CreateText(go.transform, "StarLabel",
                    starStr, 9, TextAnchor.LowerRight, InRunUiFactory.AccentGold);
                starLabel.rectTransform.anchorMin = new Vector2(0.45f, 0f);
                starLabel.rectTransform.anchorMax = new Vector2(1f, 0.28f);
                starLabel.rectTransform.offsetMin = new Vector2(0f, 2f);
                starLabel.rectTransform.offsetMax = new Vector2(-3f, 0f);
                starLabel.raycastTarget = false;
            }

            return go.GetComponent<Button>();
        }

        private static string GetNodeIconName(NodeType t) => t switch
        {
            NodeType.Puzzle      => "bud",
            NodeType.ElitePuzzle => "elite_mask",
            NodeType.PreBoss     => "elite_mask",
            NodeType.Boss        => "demon_mask",
            NodeType.Shop        => "market_stall",
            NodeType.Rest        => "campfire_stones",
            NodeType.Relic       => "relic_pedestal",
            NodeType.Event       => "stone_altar",
            NodeType.Cursed      => "porcelain_mask",
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
            _                    => "?"
        };

        // ─────────────────────────── Input ───────────────────────────

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) return;
            var sz = _map?.Run?.CurrentBoard?.Size ?? 9;
            if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W)) MoveSelection(-1, 0);
            if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S)) MoveSelection(1, 0);
            if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) MoveSelection(0, -1);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) MoveSelection(0, 1);

            for (var v = 1; v <= Mathf.Min(sz, 9); v++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + v) || Input.GetKeyDown(KeyCode.Keypad0 + v))
                    EnterNumber(v);

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                ClearCell();

            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
                TogglePencilMode();

            if (Input.GetKeyDown(KeyCode.Q))
                SaveAndQuit();
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
            _selectedRow = r;
            _selectedCol = c;
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
            if (board.GivenMask[_selectedRow, _selectedCol]) { SetStatus("Given cell."); return; }
            if (value < 1 || value > board.Size) return;

            if (_pencilMode)
            {
                if (board.Cells[_selectedRow, _selectedCol] != 0) { SetStatus("Clear cell first."); return; }
                if (!run.TryAddPencilMark(_selectedRow, _selectedCol, value))
                    SetStatus("No pencil charges.");
                else
                    SetStatus($"Pencil: {value}");
                _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                    HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                return;
            }

            var result = run.PlaceNumber(_selectedRow, _selectedCol, value);
            if (result == PlaceResult.Correct)
            {
                _audio?.PlayCorrectPlacement();
                SetStatus($"Placed {value}.");
                if (_fogDisabledMovesRemaining > 0) _fogDisabledMovesRemaining--;
            }
            else if (result == PlaceResult.IsGiven)
                SetStatus("Given cell.");
            else
            {
                _lastMistakeRow = _selectedRow;
                _lastMistakeCol = _selectedCol;
                if (_umbrellaCharges > 0)
                {
                    _umbrellaCharges--;
                    SetStatus($"Umbrella blocked the penalty! ({_umbrellaCharges} left)");
                }
                else
                {
                    _audio?.PlayWrongPlacement();
                    _audio?.PlayHpLoss();
                    run.ApplyMistakePenalty();
                    SetStatus($"{value} conflicts! HP: {run.State.CurrentHP}");
                    if (!_accessibility.IsReduceMotion)
                        StartCoroutine(AnimationHelper.ScreenShake(
                            _sudokuPanel != null ? _sudokuPanel.transform : transform,
                            AnimationHelper.ShakeWrongPx, AnimationHelper.ShakeWrongDuration));
                }
            }
            _numpadView.UpdateButtonStates(board.Size, board);
            _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
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
                SetStatus("Tutorial solved!");
                SaveAndQuit();
                return;
            }

            ShowPath();
            _rewardView.ShowRewardScreen();
        }

        private void CheckGameOver()
        {
            if (_endScreenView.IsShown) return;
            var run = _map?.Run;
            if (run == null || !run.IsPlayerDead) return;
            SetActive(_sudokuPanel, false);
            SetActive(_pathPanel, false);
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
                    _endScreenView.ShowGameOver(_map.Run, true);
                    return;
                }
                _map.AdvanceToNextFloor();
                SetStatus("Garden cleared! Next floor...");
            }
            ShowPath();
        }

        // ─────────────────────────── Boss gate callback ───────────────────────────

        private void OnBossModsConfirmed(List<BossModifierId> mods)
        {
            var run = _map?.Run;
            if (run != null) run.ChooseBossModifiers(new List<BossModifierId>(mods));

            var idx = _bossNodeIndex;
            _bossNodeIndex = -1;
            if (idx < 0) return;

            if (!_map.TryAdvanceToNodeAndStartPuzzle(idx, out _, out var level)) return;
            _pathMessage       = string.Empty;
            _completionHandled = false;
            _boardView.MarkOverlayDirty();
            _selectedRow    = -1;
            _selectedCol    = -1;
            _highlightValue = 0;
            if (level == null) { ShowPath(); return; }
            SetStatus($"Boss - {level.BoardSize}x{level.BoardSize} {level.Stars}*");
            ShowSudoku();
            RebuildBoard();
        }

        // ─────────────────────────── Save / Resume ───────────────────────────

        private void SaveAndQuit()
        {
            Time.timeScale = 1f;
            var bootstrap = FindFirstObjectByType<Bootstrap.GameBootstrap>();
            if (bootstrap != null) bootstrap.ReturnToMenu();
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

            // Fresh run (no nodes visited yet): auto-start the first available puzzle
            var nodePath = run.State.NodePath;
            var freshRun = (nodePath == null || nodePath.Count == 0) && run.CurrentBoard == null;
            if (freshRun)
            {
                var reachable = run.GetReachableNodes();
                if (reachable != null && reachable.Count > 0)
                {
                    var firstIdx = reachable[0];
                    if (_map.TryAdvanceToNodeAndStartPuzzle(firstIdx, out var arrivedNode, out var level))
                    {
                        _completionHandled = false;
                        _boardView.MarkOverlayDirty();
                        _selectedRow    = -1;
                        _selectedCol    = -1;
                        _highlightValue = 0;
                        if (level != null && arrivedNode != null &&
                            arrivedNode.Type != NodeType.Shop && arrivedNode.Type != NodeType.Rest &&
                            arrivedNode.Type != NodeType.Event && arrivedNode.Type != NodeType.Relic)
                        {
                            ShowSudoku(); RebuildBoard(); return;
                        }
                    }
                }
            }

            ShowPath();
        }

        // ─────────────────────────── Helpers ───────────────────────────

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
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

                // ── Pattern Scroll: highlight conflict cells ──
                case ItemType.PatternScroll:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    _bagHighlightCells.Clear();
                    var zones     = ItemService.GetPatternScrollZones(item.Rarity); // -1 = all
                    var zoneCount = 0;
                    for (var r = 0; r < board.Size; r++)
                    for (var c = 0; c < board.Size; c++)
                    {
                        if (board.Cells[r, c] == 0) continue;
                        if (HasConflict(board, r, c))
                        {
                            if (zones < 0 || zoneCount < zones)
                            { _bagHighlightCells.Add((r, c)); zoneCount++; }
                        }
                    }
                    run.TryUseItem(slotIndex);
                    _bagHighlightEndTime = Time.time + 5f;
                    SetStatus($"Pattern Scroll: highlighted {zoneCount} conflict(s).");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Koi Reflection: reveal candidates for N cells ──
                case ItemType.KoiReflection:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    var count    = ItemService.GetKoiReflectionCells(item.Rarity);
                    var revealed = 0;
                    for (var r = 0; r < board.Size && revealed < count; r++)
                    for (var c = 0; c < board.Size && revealed < count; c++)
                    {
                        if (board.GivenMask[r, c] || board.Cells[r, c] != 0) continue;
                        if (board.GetPencilMarks(r, c).Count > 0) continue;
                        // Add all valid candidates
                        var usedMask = 0;
                        for (var rr = 0; rr < board.Size; rr++) if (board.Cells[rr, c] > 0) usedMask |= 1 << board.Cells[rr, c];
                        for (var cc = 0; cc < board.Size; cc++) if (board.Cells[r, cc] > 0) usedMask |= 1 << board.Cells[r, cc];
                        var reg = board.RegionMap[r, c];
                        for (var rr = 0; rr < board.Size; rr++)
                        for (var cc = 0; cc < board.Size; cc++)
                            if (board.RegionMap[rr, cc] == reg && board.Cells[rr, cc] > 0) usedMask |= 1 << board.Cells[rr, cc];
                        var added = false;
                        for (var v = 1; v <= board.Size; v++)
                            if ((usedMask & (1 << v)) == 0) { board.AddPencilMark(r, c, v); added = true; }
                        if (added) revealed++;
                    }
                    run.TryUseItem(slotIndex);
                    SetStatus($"Koi Reflection: revealed candidates for {revealed} cell(s).");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Lantern of Clarity: disable fog for N moves ──
                case ItemType.LanternOfClarity:
                {
                    var moves = ItemService.GetLanternOfClarityMoves(item.Rarity);
                    _fogDisabledMovesRemaining += moves;
                    run.TryUseItem(slotIndex);
                    SetStatus($"Lantern of Clarity: fog disabled for {_fogDisabledMovesRemaining} moves.");
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
                    s.CurrentHP = Math.Min(s.MaxHP, s.CurrentHP + 1); // restore lost HP
                    run.TryUseItem(slotIndex);
                    SetStatus($"Ginkgo Leaf: mistake at ({_lastMistakeRow + 1},{_lastMistakeCol + 1}) undone, +1 HP.");
                    _lastMistakeRow = -1;
                    _lastMistakeCol = -1;
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
                    break;
                }

                // ── Rice Paper Umbrella: block next 2 mistake penalties ──
                case ItemType.RicePaperUmbrella:
                {
                    _umbrellaCharges += 2;
                    run.TryUseItem(slotIndex);
                    SetStatus($"Rice Paper Umbrella: next {_umbrellaCharges} mistake(s) won't cost HP.");
                    break;
                }

                // ── Temple Incense: fill a naked-single cell ──
                case ItemType.TempleIncense:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    var filled = false;
                    for (var r = 0; r < board.Size && !filled; r++)
                    for (var c = 0; c < board.Size && !filled; c++)
                    {
                        if (board.GivenMask[r, c] || board.Cells[r, c] != 0) continue;
                        var usedMask = 0;
                        for (var rr = 0; rr < board.Size; rr++) if (board.Cells[rr, c] > 0) usedMask |= 1 << board.Cells[rr, c];
                        for (var cc = 0; cc < board.Size; cc++) if (board.Cells[r, cc] > 0) usedMask |= 1 << board.Cells[r, cc];
                        var reg = board.RegionMap[r, c];
                        for (var rr = 0; rr < board.Size; rr++)
                        for (var cc = 0; cc < board.Size; cc++)
                            if (board.RegionMap[rr, cc] == reg && board.Cells[rr, cc] > 0) usedMask |= 1 << board.Cells[rr, cc];
                        var singles = new List<int>();
                        for (var v = 1; v <= board.Size; v++) if ((usedMask & (1 << v)) == 0) singles.Add(v);
                        if (singles.Count == 1)
                        {
                            run.PlaceNumber(r, c, singles[0]);
                            filled = true;
                            _selectedRow = r; _selectedCol = c;
                        }
                    }
                    if (!filled) { SetStatus("No naked single found."); return; }
                    run.TryUseItem(slotIndex);
                    SetStatus("Temple Incense: filled a naked-single cell.");
                    _boardView.RenderBoard(board, _selectedRow, _selectedCol, _highlightValue,
                        HasActiveModifier(BossModifierId.Antiknight), _bagHighlightCells, _fogDisabledMovesRemaining);
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

                // ── Wind Chime: reroll item reward slots ──
                case ItemType.WindChime:
                {
                    if (_rewardView.IsAwaitingReward)
                    {
                        var newSlots = run.BuildItemRewardSlots();
                        if (newSlots != null)
                            _rewardView.RerollRewardSlots(newSlots);
                    }
                    run.TryUseItem(slotIndex);
                    SetStatus("Wind Chime: reward slots rerolled.");
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
