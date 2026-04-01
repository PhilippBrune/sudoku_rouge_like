using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Boss;
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
    /// In-run screen controller: path overview, sudoku gameplay, rewards, shop, game over.
    /// Procedurally builds all UI. Drives RunMapController / RunDirector.
    /// </summary>
    public sealed class PrototypeRunScreenController : MonoBehaviour
    {
        // ── Injected via Configure() ──
        private RunMapController _map;
        private GameObject _pathPanel;
        private GameObject _sudokuPanel;
        private GameObject _gameOverPanel;
        private Text _overviewText;
        private Text _statusText;
        private Text _hpText;
        private Text _pencilText;
        private Text _gameOverSummary;
        private Text _gameOverDetails;
        private Button _gameOverBackBtn;
        private RectTransform _gridRoot;
        private RectTransform _numpadRoot;

        // ── Runtime state ──
        private List<CellView> _cells;
        private List<Button> _numpadButtons;
        private int _boardSize;
        private int _selectedRow;
        private int _selectedCol;
        private int _highlightValue;
        private bool _completionHandled;
        private bool _gameOverShown;
        private bool _pencilMode;
        private Button _pencilModeButton;
        private bool _resumeApplied;
        private bool _tutorialShown;

        // ── Overlay ──
        private RectTransform _gridOverlayRoot;
        private RectTransform _gridNumberRoot;
        private List<GameObject> _overlayObjects;
        private bool _overlaysBuilt;
        private HashSet<long> _cageBorderEdges;
        private Sprite _circleSprite;
        private Sprite _ringSprite;

        // ── Reward / Shop ──
        private GameObject _rewardPanel;
        private Text _rewardSummary;
        private bool _awaitingReward;
        private List<ItemInstance> _rewardSlots;
        private GameObject _shopPanel;
        private Text _shopSummary;
        private List<ShopOffer> _shopOffers;

        // ── Boss gate ──
        private GameObject _bossGatePanel;
        private bool _awaitingBossGate;
        private List<BossModifierId> _bossGateOptions;
        private List<BossModifierId> _selectedBossMods;
        private int _bossPicksRequired;
        private Text _bossGateTitle;

        // ── Path overlay ──
        private RectTransform _pathOverlayRoot;
        private string _pathMessage;

        // ── Options panel ──
        private GameObject _inGameOptionsPanel;
        private int _bossNodeIndex = -1;

        // ── Audio ──
        private RunAudioController _audio;

        // ── Accessibility ──
        private AccessibilityService _accessibility = new AccessibilityService();

        // ── HUD bars ──
        private Image _hpBarFill;
        private Image _pencilBarFill;

        // ── Floor modifier display ──
        private GameObject _floorModBanner;
        private Text _floorModText;
        private GameObject _modifierInfoBox;
        private Text _modifierInfoText;

        // ── Shop selection ──
        private int _selectedShopOffer = -1;

        // ── Bag-full swap panel ──
        private GameObject _bagSwapPanel;
        private ItemInstance _pendingSwapItem;    // item waiting to be placed
        private System.Action<int> _onSwapSlotChosen; // callback(slotIndex) → do the replace
        private System.Action _onSwapAborted;

        // ── Bag panel ──
        private GameObject _bagPanel;
        private List<Button> _bagItemButtons;
        private Button _bagRelicButton;
        private HashSet<(int row, int col)> _bagHighlightCells;
        private float _bagHighlightEndTime;
        private int _selectedBagSlot = -1;
        private int _umbrellaCharges;
        private int _fogDisabledMovesRemaining;
        private bool _swapMode;
        private int _swapRow1 = -1, _swapCol1 = -1;
        private int _lastMistakeRow = -1, _lastMistakeCol = -1;

        // ── Colors ──
        private static readonly Color BgColor = new Color(0.07f, 0.11f, 0.14f);
        private static readonly Color PanelBg = new Color(0.08f, 0.12f, 0.16f, 0.94f);
        private static readonly Color BtnColor = new Color(0.15f, 0.22f, 0.29f, 0.94f);
        private static readonly Color AccentGold = new Color(0.98f, 0.83f, 0.26f);
        private static readonly Color TextColor = new Color(0.96f, 0.93f, 0.82f);
        private static readonly Color HpRed = new Color(0.75f, 0.22f, 0.22f);
        private static readonly Color PencilBlue = new Color(0.22f, 0.55f, 0.75f);
        private static readonly Color EmptyCellColor = new Color(0.12f, 0.18f, 0.14f, 1f);
        private static readonly Color GivenCellColor = new Color(0.20f, 0.29f, 0.20f, 1f);
        private static readonly Color SelectedColor = new Color(0.73f, 0.49f, 0.18f, 1f);
        private static readonly Color RowColHL = new Color(0.18f, 0.34f, 0.56f, 1f);
        private static readonly Color MatchColor = new Color(0.36f, 0.24f, 0.58f, 1f);
        private static readonly Color ConflictColor = new Color(0.72f, 0.18f, 0.18f, 1f);
        private static readonly Color FogColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        private static readonly Color AntiknightForbidColor = new Color(0.55f, 0.20f, 0.60f, 0.55f);
        private static readonly Color BagHighlightColor = new Color(0.30f, 0.65f, 0.85f, 0.55f);
        private static readonly Color RegionBorder = new Color(1f, 0.78f, 0.24f, 0.72f);
        private static readonly Color KillerBorder = new Color(0.85f, 0.15f, 0.12f, 0.90f);

        private static Font _font;
        private static Font GetFont()
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        // ────────────────────── Lifecycle ──────────────────────

        private void Awake()
        {
            _cells = new List<CellView>();
            _numpadButtons = new List<Button>();
            _overlayObjects = new List<GameObject>();
            _cageBorderEdges = new HashSet<long>();
            _rewardSlots = new List<ItemInstance>();
            _shopOffers = new List<ShopOffer>();
            _bossGateOptions = new List<BossModifierId>();
            _selectedBossMods = new List<BossModifierId>();
            _bagItemButtons = new List<Button>();
            _bagHighlightCells = new HashSet<(int, int)>();
            _selectedRow = -1;
            _selectedCol = -1;
            _pathMessage = string.Empty;
        }

        public void Configure(RunMapController map,
            GameObject pathPanel, GameObject sudokuPanel, GameObject gameOverPanel,
            Text overviewText, Text statusText, Text hpText, Text pencilText,
            Text goSummary, Text goDetails, Button goBack,
            RectTransform gridRoot, RectTransform numpadRoot)
        {
            _map = map;
            _pathPanel = pathPanel;
            _sudokuPanel = sudokuPanel;
            _gameOverPanel = gameOverPanel;
            _overviewText = overviewText;
            _statusText = statusText;
            _hpText = hpText;
            _pencilText = pencilText;
            _gameOverSummary = goSummary;
            _gameOverDetails = goDetails;
            _gameOverBackBtn = goBack;
            _gridRoot = gridRoot;
            _numpadRoot = numpadRoot;

            if (_gameOverBackBtn != null)
            {
                _gameOverBackBtn.onClick.RemoveAllListeners();
                _gameOverBackBtn.onClick.AddListener(SaveAndQuit);
            }

            BuildNumpad();
            ShowPath();
        }

        public void NotifyRunStarted()
        {
            _audio = FindFirstObjectByType<RunAudioController>();
            _resumeApplied = false;
            _tutorialShown = false;
            _completionHandled = false;
            _gameOverShown = false;
            RefreshAccessibility();
            WireInGameButtons();
            ShowPath();
        }

        public void NotifyAccessibilityChanged()
        {
            RefreshAccessibility();
            // Refresh board overlays if a puzzle is active
            if (_map?.Run?.CurrentBoard != null && _cells != null && _cells.Count > 0)
            {
                _overlaysBuilt = false; // force overlay rebuild with new accessibility settings
                RebuildBoard();
            }
            // Refresh particle controller
            FindFirstObjectByType<AmbientParticleController>()?.RefreshSettings();
        }

        private void RefreshAccessibility()
        {
            var save = new SaveFileService();
            var opts = save.HasSaveFile() ? save.Load().PlayerProfile?.Options : null;
            _accessibility.Refresh(
                opts?.Accessibility ?? new AccessibilitySettings(),
                opts?.Graphics ?? new GraphicsSettingsModel());
        }

        private void WireInGameButtons()
        {
            if (_sudokuPanel == null) return;
            var pr = _sudokuPanel.GetComponent<RectTransform>();
            if (pr == null) return;

            // Wire Save & Quit button (label becomes "Quit" in tutorial — no save occurs)
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

            // Wire Options button
            var optBtn = pr.Find("BtnSudokuOptions")?.GetComponent<Button>();
            if (optBtn != null)
            {
                optBtn.onClick.RemoveAllListeners();
                optBtn.onClick.AddListener(ToggleOptionsPanel);
            }

            // Find in-game options panel (sibling of sudokuPanel under same parent)
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

        public void SetInGameOptionsPanel(GameObject panel)
        {
            _inGameOptionsPanel = panel;
        }

        public void SaveAndQuitPublic() => SaveAndQuit();

        public void SetHighlightConflictsLive(bool enabled)
        {
            // Refresh board visuals when highlight-conflicts setting changes mid-game
            if (_map?.Run?.CurrentBoard != null && _cells != null && _cells.Count > 0)
                RebuildBoard();
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
                RefreshHud();
                CheckGameOver();
            }
        }

        // ────────────────────── Screen transitions ──────────────────────

        private void ShowPath()
        {
            var run = _map?.Run;
            if (run?.State != null && run.State.TutorialMode) { ShowSudoku(); return; }
            SetActive(_pathPanel, true);
            SetActive(_sudokuPanel, false);
            SetActive(_gameOverPanel, false);
            HidePanel(_rewardPanel);
            HidePanel(_shopPanel);
            _audio?.SetContext(RunAudioController.Context.Path);
            RefreshPathOverview();
        }

        private void ShowSudoku()
        {
            SetActive(_pathPanel, false);
            SetActive(_sudokuPanel, true);
            SetActive(_gameOverPanel, false);
            HidePanel(_rewardPanel);
            HidePanel(_shopPanel);
            var floor = _map?.Run?.State?.CurrentFloor ?? 0;
            _audio?.SetFloorIndex(floor);
            _audio?.SetContext(RunAudioController.Context.Puzzle);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private static void HidePanel(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
        }

        // ────────────────────── Path overview ──────────────────────

        private void RefreshPathOverview()
        {
            var run = _map?.Run;
            if (run?.State == null || run.CurrentFloorGraph == null) return;

            var s = run.State;
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
            ClearChildren(_pathOverlayRoot);

            var graph = run.CurrentFloorGraph;
            if (graph == null || graph.Count == 0) return;

            var w = Mathf.Max(400f, _pathOverlayRoot.rect.width);
            var h = Mathf.Max(300f, _pathOverlayRoot.rect.height);
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

            var pathColor    = new Color(floorTint.r * 0.75f, floorTint.g * 0.75f, floorTint.b * 0.75f, 0.70f);
            var accentColor  = AccentGold;
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
                var node = graph[i];
                var pos = new Vector2(node.CanvasX * w, (1f - node.CanvasY) * h);
                var isCurrent = i == run.State.CurrentNodeIndex;
                var canReach = reachable != null && reachable.Contains(i);

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
            ol.effectColor = outlineColor;
            ol.effectDistance = new Vector2(2f, -2f);

            // Lane label in top-left corner of the box
            var lbl = CreateText(go.transform, "LaneLabel", label, 10,
                TextAnchor.UpperLeft, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.85f));
            lbl.rectTransform.anchorMin = new Vector2(0f, 0.80f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 1f);
            lbl.rectTransform.offsetMin = new Vector2(6f, 0f);
            lbl.rectTransform.offsetMax = Vector2.zero;
            lbl.raycastTarget = false;
        }

        private void OnNodeClicked(int nodeIndex)
        {
            if (_awaitingReward || _awaitingBossGate) return;
            var run = _map?.Run;
            if (run == null) return;

            var graph = run.CurrentFloorGraph;
            if (graph == null || nodeIndex < 0 || nodeIndex >= graph.Count) return;
            var node = graph[nodeIndex];

            // Boss gate requires modifier choice first (only if modifiers not yet chosen)
            if (node.Type == NodeType.Boss && (run.State.ChosenBossModifiers == null || run.State.ChosenBossModifiers.Count == 0))
            {
                _bossNodeIndex = nodeIndex;
                ShowBossGateChoice();
                return;
            }

            if (!_map.TryAdvanceToNodeAndStartPuzzle(nodeIndex, out var arrivedNode, out var level))
            {
                SetStatus("Cannot move there.");
                return;
            }

            _pathMessage = string.Empty;
            _completionHandled = false;
            _gameOverShown = false;
            _overlaysBuilt = false;
            _selectedRow = -1;
            _selectedCol = -1;
            _highlightValue = 0;
            _selectedBagSlot = -1;

            if (arrivedNode.Type == NodeType.Shop) { HandleShop(); return; }
            if (arrivedNode.Type == NodeType.Rest) { HandleRest(); ShowPath(); return; }
            if (arrivedNode.Type == NodeType.Event) { HandleEvent(); return; }
            if (arrivedNode.Type == NodeType.Relic) { HandleRelic(); ShowPath(); return; }

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
            var d = pb - pa;
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
            var size = isBoss ? new Vector2(120, 60) : new Vector2(72, 72);
            var go = new GameObject($"Node_{node.Type}_{node.Index}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = color;

            // Try to load icon
            var iconName = GetNodeIconName(node.Type);
            var sprite = string.IsNullOrEmpty(iconName) ? null : Resources.Load<Sprite>("GeneratedIcons/icon_" + iconName);
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
                var img = iconGo.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;

                var lbl = CreateText(go.transform, "Label", GetNodeLabelShort(node),
                    isBoss ? 11 : 10, TextAnchor.MiddleCenter, TextColor);
                lbl.rectTransform.anchorMin = new Vector2(0.40f, 0f);
                lbl.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                lbl.rectTransform.offsetMin = Vector2.zero;
                lbl.rectTransform.offsetMax = Vector2.zero;
                lbl.verticalOverflow = VerticalWrapMode.Overflow;
            }
            else
            {
                var lbl = CreateText(go.transform, "Label", GetNodeLabelShort(node),
                    isBoss ? 13 : 11, TextAnchor.MiddleCenter, TextColor);
                StretchFill(lbl.rectTransform);
            }

            // Puzzle-type nodes: top-left size, bottom-right stars
            if (config != null && IsPuzzleNodeType(node.Type))
            {
                var sizeLabel = CreateText(go.transform, "SizeLabel",
                    $"{config.BoardSize}×{config.BoardSize}", 9, TextAnchor.UpperLeft,
                    new Color(TextColor.r, TextColor.g, TextColor.b, 0.85f));
                sizeLabel.rectTransform.anchorMin = new Vector2(0f, 0.72f);
                sizeLabel.rectTransform.anchorMax = new Vector2(0.55f, 1f);
                sizeLabel.rectTransform.offsetMin = new Vector2(3f, 0f);
                sizeLabel.rectTransform.offsetMax = new Vector2(0f, -2f);
                sizeLabel.raycastTarget = false;

                var stars = Mathf.Clamp(config.Stars, 1, 6);
                var starStr = new string('★', stars);
                var starLabel = CreateText(go.transform, "StarLabel",
                    starStr, 9, TextAnchor.LowerRight, AccentGold);
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
            _                    => ""
        };

        private static string GetNodeLabelShort(RunNode node)
        {
            return node.Type switch
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
        }

        private static string GetNodeLabel(NodeType t) => t switch
        {
            NodeType.Start => "Start", NodeType.Puzzle => "Puzzle", NodeType.ElitePuzzle => "Elite",
            NodeType.Shop => "Shop", NodeType.Rest => "Rest", NodeType.Relic => "Relic",
            NodeType.Event => "Event", NodeType.PreBoss => "Elite", NodeType.Boss => "Boss Gate",
            _ => "?"
        };

        // ────────────────────── Board building ──────────────────────

        private void RebuildBoard()
        {
            var run = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null || _gridRoot == null) return;

            if (_boardSize != board.Size || _cells.Count == 0)
            {
                BuildGrid(board.Size);
                _overlaysBuilt = false;
            }

            if (!_overlaysBuilt) { BuildOverlays(); _overlaysBuilt = true; }

            if (_selectedRow < 0 && TryFindEditable(board, out var r, out var c))
            {
                _selectedRow = r;
                _selectedCol = c;
            }

            // Update level/depth info label
            if (_sudokuPanel != null)
            {
                var infoText = _sudokuPanel.GetComponent<RectTransform>()?.Find("SudokuGameplayLevelInfo")?.GetComponent<Text>();
                if (infoText != null && run?.State != null)
                {
                    var cfg = run.CurrentLevelConfig;
                    var stars = cfg != null ? new string('★', cfg.Stars) : "";
                    infoText.text = $"Floor {run.State.CurrentFloor + 1}  Depth {run.State.Depth}  {board.Size}×{board.Size}  {stars}";
                }
            }

            UpdateNumpad(board.Size, board);
            RenderBoard(board);
        }

        private void BuildGrid(int size)
        {
            _boardSize = size;
            _cells.Clear();
            ClearChildren(_gridRoot);

            var grid = _gridRoot.GetComponent<GridLayoutGroup>() ?? _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = size;
            grid.spacing = new Vector2(2, 2);
            grid.cellSize = size <= 6 ? new Vector2(96, 96) : size <= 8 ? new Vector2(74, 74) : new Vector2(62, 62);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var totalW = grid.cellSize.x * size + grid.spacing.x * (size - 1);
            var totalH = grid.cellSize.y * size + grid.spacing.y * (size - 1);
            _gridRoot.anchorMin = new Vector2(0.5f, 0.42f);
            _gridRoot.anchorMax = new Vector2(0.5f, 0.42f);
            _gridRoot.pivot = new Vector2(0.5f, 0.5f);
            _gridRoot.anchoredPosition = Vector2.zero;
            _gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalW);
            _gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalH);

            EnsureNumberRoot(size, grid);
            EnsureOverlayRoot();

            for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
            {
                // Background cell
                var cellGo = new GameObject($"C_{row}_{col}", typeof(RectTransform), typeof(Image), typeof(Button));
                cellGo.transform.SetParent(_gridRoot, false);
                var img = cellGo.GetComponent<Image>();
                img.color = EmptyCellColor;
                var cr = row; var cc = col;
                cellGo.GetComponent<Button>().onClick.AddListener(() => OnCellClicked(cr, cc));

                var bt = CreateBorder(cellGo.transform, "BT", new Vector2(0,1), new Vector2(1,1), new Vector2(0,-3), Vector2.zero);
                var bb = CreateBorder(cellGo.transform, "BB", Vector2.zero, new Vector2(1,0), Vector2.zero, new Vector2(0,3));
                var bl = CreateBorder(cellGo.transform, "BL", Vector2.zero, new Vector2(0,1), Vector2.zero, new Vector2(3,0));
                var br = CreateBorder(cellGo.transform, "BR", new Vector2(1,0), Vector2.one, new Vector2(-3,0), Vector2.zero);

                // Number cell
                var numGo = new GameObject($"N_{row}_{col}", typeof(RectTransform));
                numGo.transform.SetParent(_gridNumberRoot, false);
                var fs = size <= 6 ? 30 : size <= 8 ? 24 : 20;
                var valText = CreateText(numGo.transform, "Val", "", fs, TextAnchor.MiddleCenter, Color.white);
                StretchFill(valText.rectTransform);
                valText.raycastTarget = false;
                var pencilFs = size <= 6 ? 16 : 14;
                var pencilText = CreateText(numGo.transform, "Pencil", "", pencilFs, TextAnchor.UpperLeft, new Color(0.84f, 0.86f, 0.82f, 0.95f));
                pencilText.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
                pencilText.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
                pencilText.rectTransform.offsetMin = Vector2.zero;
                pencilText.rectTransform.offsetMax = Vector2.zero;
                pencilText.supportRichText = true;
                pencilText.raycastTarget = false;

                _cells.Add(new CellView { Row = row, Col = col, Image = img, Label = valText,
                    PencilLabel = pencilText, BorderTop = bt, BorderBottom = bb, BorderLeft = bl, BorderRight = br });
            }
        }

        private static Image CreateBorder(Transform parent, string name, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax; rt.offsetMin = omin; rt.offsetMax = omax;
            var img = go.GetComponent<Image>();
            img.color = new Color(1, 0.78f, 0.26f, 0);
            img.raycastTarget = false;
            return img;
        }

        private void EnsureNumberRoot(int size, GridLayoutGroup srcGrid)
        {
            if (_gridNumberRoot == null)
            {
                var go = new GameObject("GridNumbers", typeof(RectTransform));
                go.transform.SetParent(_gridRoot.parent, false);
                _gridNumberRoot = go.GetComponent<RectTransform>();
                go.AddComponent<LayoutElement>().ignoreLayout = true;
                var cg = go.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            else ClearChildren(_gridNumberRoot);

            CopyRectTransform(_gridRoot, _gridNumberRoot);
            var ng = _gridNumberRoot.GetComponent<GridLayoutGroup>() ?? _gridNumberRoot.gameObject.AddComponent<GridLayoutGroup>();
            ng.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            ng.constraintCount = size;
            ng.spacing = srcGrid.spacing;
            ng.cellSize = srcGrid.cellSize;
            ng.childAlignment = TextAnchor.MiddleCenter;
            _gridNumberRoot.SetSiblingIndex(_gridRoot.GetSiblingIndex() + 1);
        }

        private void EnsureOverlayRoot()
        {
            if (_gridOverlayRoot != null) return;
            var go = new GameObject("GridOverlay", typeof(RectTransform));
            go.transform.SetParent(_gridRoot.parent, false);
            _gridOverlayRoot = go.GetComponent<RectTransform>();
            CopyRectTransform(_gridRoot, _gridOverlayRoot);
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            _gridOverlayRoot.SetSiblingIndex(_gridRoot.GetSiblingIndex() + 1);
            if (_gridNumberRoot != null)
                _gridNumberRoot.SetSiblingIndex(_gridOverlayRoot.GetSiblingIndex() + 1);
        }

        private static void CopyRectTransform(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, src.rect.width);
            dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, src.rect.height);
        }

        // ────────────────────── Board rendering ──────────────────────

        private void RenderBoard(SudokuBoard board)
        {
            var overlay = _map?.Run?.CurrentOverlay;
            var hasAntiknight = HasActiveModifier(BossModifierId.Antiknight);

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var val = board.Cells[cell.Row, cell.Col];
                var given = board.GivenMask[cell.Row, cell.Col];

                cell.Label.text = val == 0 ? "" : val.ToString();
                cell.Label.color = given ? new Color(0.04f, 0.04f, 0.04f) : Color.white;
                cell.PencilLabel.text = val == 0 ? BuildPencilText(board, cell.Row, cell.Col) : "";

                var c = ComputeCellColor(board, cell.Row, cell.Col, given);
                if (_selectedRow == cell.Row && _selectedCol == cell.Col)
                    c = SelectedColor;
                else if (_selectedRow == cell.Row || _selectedCol == cell.Col)
                    c = RowColHL;
                if (_highlightValue > 0 && val == _highlightValue)
                    c = MatchColor;

                // Anti-Knight: highlight cells that are a knight's move from the selected cell
                // and already contain the highlighted value (forbidden by rule)
                if (hasAntiknight && _selectedRow >= 0 && _highlightValue > 0)
                {
                    if (IsAntiknightForbiddenCell(board, cell.Row, cell.Col))
                        c = Color.Lerp(c, AntiknightForbidColor, 0.70f);
                }

                // Bag item highlights (Finder, Pattern Scroll, etc.)
                if (_bagHighlightCells != null && _bagHighlightCells.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, BagHighlightColor, 0.65f);

                // Fog (bypassed while LanternOfClarity is active)
                if (_fogDisabledMovesRemaining <= 0 && overlay != null && IsFogged(overlay, cell.Row, cell.Col))
                {
                    c = FogColor;
                    if (val == 0) cell.Label.text = "";
                    cell.PencilLabel.text = "";
                }

                // Conflicts
                if (!given && HasConflict(board, cell.Row, cell.Col))
                    c = ConflictColor;

                cell.Image.color = c;
                UpdateBorders(board, cell);
            }
        }

        private bool HasActiveModifier(BossModifierId mod)
        {
            var cfg = _map?.Run?.CurrentLevelConfig;
            return cfg != null && cfg.ActiveModifiers.Contains(mod);
        }

        private static readonly (int dr, int dc)[] KnightMoves =
        {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2), (1, 2), (2, -1), (2, 1)
        };

        // Returns true if this cell is a knight's move from the selected cell and the selected
        // cell has a non-zero value, making this cell forbidden for that value.
        private bool IsAntiknightForbiddenCell(SudokuBoard board, int r, int c)
        {
            if (_selectedRow < 0 || _selectedCol < 0) return false;
            // Check if (r,c) is a knight's move from selected cell
            var dr = r - _selectedRow;
            var dc = c - _selectedCol;
            var isKnightMove = false;
            for (var k = 0; k < KnightMoves.Length; k++)
                if (KnightMoves[k].dr == dr && KnightMoves[k].dc == dc) { isKnightMove = true; break; }
            if (!isKnightMove) return false;

            // Highlight if this cell has the same value as highlighted value OR
            // if the selected cell is empty and the highlighted value is > 0,
            // show all knight-move cells as forbidden destinations
            var selectedVal = board.Cells[_selectedRow, _selectedCol];
            if (_highlightValue > 0)
            {
                // Show forbidden cells: knight-move cells that contain the highlighted value
                return board.Cells[r, c] == _highlightValue;
            }
            // If nothing is highlighted but selected cell has a value, show forbidden cells for that value
            if (selectedVal > 0)
                return board.Cells[r, c] == selectedVal;
            return false;
        }

        private static Color ComputeCellColor(SudokuBoard board, int r, int c, bool given)
        {
            if (board.RegionMap != null)
            {
                var alt = (board.RegionMap[r, c] & 1) == 0;
                return given
                    ? (alt ? new Color(0.25f, 0.36f, 0.25f) : new Color(0.19f, 0.30f, 0.20f))
                    : (alt ? new Color(0.15f, 0.22f, 0.16f) : new Color(0.11f, 0.17f, 0.12f));
            }
            return given ? GivenCellColor : EmptyCellColor;
        }

        private void UpdateBorders(SudokuBoard board, CellView cell)
        {
            var map = board.RegionMap;
            if (map == null) return;
            var r = cell.Row; var c = cell.Col; var sz = board.Size;
            var region = map[r, c];
            var clear = new Color(RegionBorder.r, RegionBorder.g, RegionBorder.b, 0);

            cell.BorderTop.color = (r == 0 || map[r - 1, c] != region) ? RegionBorder : clear;
            cell.BorderBottom.color = (r == sz - 1 || map[r + 1, c] != region) ? RegionBorder : clear;
            cell.BorderLeft.color = (c == 0 || map[r, c - 1] != region) ? RegionBorder : clear;
            cell.BorderRight.color = (c == sz - 1 || map[r, c + 1] != region) ? RegionBorder : clear;

            // Killer cage borders override region borders
            if (_cageBorderEdges.Count > 0)
            {
                if (IsCageEdge(r, c, sz, 0)) cell.BorderTop.color = KillerBorder;
                if (IsCageEdge(r, c, sz, 1)) cell.BorderBottom.color = KillerBorder;
                if (IsCageEdge(r, c, sz, 2)) cell.BorderLeft.color = KillerBorder;
                if (IsCageEdge(r, c, sz, 3)) cell.BorderRight.color = KillerBorder;
            }
        }

        private static string BuildPencilText(SudokuBoard board, int r, int c)
        {
            var marks = board.GetPencilMarks(r, c);
            if (marks == null || marks.Count == 0) return "";
            var sb = new StringBuilder();
            for (var v = 1; v <= board.Size; v++)
                if (marks.Contains(v)) sb.Append(v).Append(' ');
            return sb.ToString().TrimEnd();
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
            if (board.RegionMap != null)
            {
                var reg = board.RegionMap[r, c];
                for (var rr = 0; rr < sz; rr++)
                for (var cc = 0; cc < sz; cc++)
                    if ((rr != r || cc != c) && board.RegionMap[rr, cc] == reg && board.Cells[rr, cc] == val)
                        return true;
            }
            return false;
        }

        private static bool IsFogged(ModifierOverlayData overlay, int r, int c)
        {
            if (overlay?.FogCells == null) return false;
            for (var i = 0; i < overlay.FogCells.Count; i++)
                if (overlay.FogCells[i].Row == r && overlay.FogCells[i].Col == c) return true;
            return false;
        }

        // ────────────────────── Input ──────────────────────

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) return;
            var sz = _map?.Run?.CurrentBoard?.Size ?? 9;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) MoveSelection(-1, 0);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) MoveSelection(1, 0);
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) MoveSelection(0, -1);
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
                RenderBoard(board);
                return;
            }
            _selectedRow = Mathf.Clamp(_selectedRow + dr, 0, board.Size - 1);
            _selectedCol = Mathf.Clamp(_selectedCol + dc, 0, board.Size - 1);
            RenderBoard(board);
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
            RenderBoard(board);

            // Anti-Knight: after selecting an empty cell, clear highlight so forbidden cells
            // show for the selected cell's neighbor values
            if (val == 0 && HasActiveModifier(BossModifierId.Antiknight))
                _highlightValue = 0;
        }

        private void EnterNumber(int value)
        {
            var run = _map?.Run;
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
                RenderBoard(board);
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
                    RefreshBag();
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
            UpdateNumpad(board.Size, board);
            RenderBoard(board);
            CheckGameOver();
        }

        private void ClearCell()
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null || _selectedRow < 0) return;
            if (board.GivenMask[_selectedRow, _selectedCol]) return;
            board.PlaceValue(_selectedRow, _selectedCol, 0);
            _highlightValue = 0;
            RenderBoard(board);
        }

        private void TogglePencilMode()
        {
            _pencilMode = !_pencilMode;
            if (_pencilModeButton != null)
            {
                var lbl = _pencilModeButton.GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = _pencilMode ? "Mode: PENCIL" : "Mode: SOLVE";
                var img = _pencilModeButton.GetComponent<Image>();
                if (img != null)
                    img.color = _pencilMode ? new Color(0.31f, 0.43f, 0.28f) : BtnColor;
            }
        }

        // ────────────────────── Numpad ──────────────────────

        private void BuildNumpad()
        {
            if (_numpadRoot == null) return;
            _numpadButtons.Clear();
            ClearChildren(_numpadRoot);

            var grid = _numpadRoot.GetComponent<GridLayoutGroup>() ?? _numpadRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(90, 56);
            grid.spacing = new Vector2(8, 8);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var v = 1; v <= 9; v++)
            {
                var go = new GameObject($"Num_{v}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_numpadRoot, false);
                go.GetComponent<Image>().color = new Color(0.19f, 0.30f, 0.20f);
                var val = v;
                go.GetComponent<Button>().onClick.AddListener(() => EnterNumber(val));
                var t = CreateText(go.transform, "L", v.ToString(), 24, TextAnchor.MiddleCenter, TextColor);
                StretchFill(t.rectTransform);
                _numpadButtons.Add(go.GetComponent<Button>());
            }

            // Pencil mode button
            var pmGo = new GameObject("PencilBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            pmGo.transform.SetParent(_numpadRoot, false);
            pmGo.AddComponent<LayoutElement>().ignoreLayout = true;
            var pmRect = pmGo.GetComponent<RectTransform>();
            pmRect.anchorMin = new Vector2(0.1f, 0.04f);
            pmRect.anchorMax = new Vector2(0.9f, 0.14f);
            pmRect.offsetMin = Vector2.zero;
            pmRect.offsetMax = Vector2.zero;
            pmGo.GetComponent<Image>().color = BtnColor;
            _pencilModeButton = pmGo.GetComponent<Button>();
            _pencilModeButton.onClick.AddListener(TogglePencilMode);
            var pmLbl = CreateText(pmGo.transform, "L", "Mode: SOLVE", 13, TextAnchor.MiddleCenter, TextColor);
            StretchFill(pmLbl.rectTransform);
        }

        private void UpdateNumpad(int boardSize, SudokuBoard board = null)
        {
            // Count how many of each digit are placed on the board
            var counts = new int[boardSize + 1];
            if (board != null)
                for (var r = 0; r < boardSize; r++)
                for (var c = 0; c < boardSize; c++)
                {
                    var v = board.Cells[r, c];
                    if (v >= 1 && v <= boardSize) counts[v]++;
                }

            for (var i = 0; i < _numpadButtons.Count; i++)
            {
                var btn = _numpadButtons[i];
                if (btn == null) continue;
                var digit = i + 1;
                var inRange = digit <= boardSize;
                var completed = inRange && board != null && counts[digit] >= boardSize;
                btn.interactable = inRange && !completed;
                var img = btn.GetComponent<Image>();
                if (img != null)
                    img.color = !inRange ? new Color(0.13f, 0.13f, 0.13f, 0.8f)
                        : completed  ? new Color(0.10f, 0.10f, 0.10f, 0.5f)
                        : new Color(0.19f, 0.30f, 0.20f);
            }
        }

        // ────────────────────── HUD ──────────────────────

        private void RefreshHud()
        {
            var s = _map?.Run?.State;
            if (s == null) return;
            if (_hpText != null) _hpText.text = $"HP: {s.CurrentHP}/{s.MaxHP}";
            if (_pencilText != null) _pencilText.text = $"Pencil: {s.CurrentPencil}/{s.MaxPencil}";

            if (_hpBarFill == null && _sudokuPanel != null)
            {
                var pr = _sudokuPanel.GetComponent<RectTransform>();
                var hpBg = pr?.Find("HpBarBg");
                if (hpBg != null) _hpBarFill = hpBg.Find("HpBarFill")?.GetComponent<Image>();
                var pBg = pr?.Find("PencilBarBg");
                if (pBg != null) _pencilBarFill = pBg.Find("PencilBarFill")?.GetComponent<Image>();
            }

            if (_hpBarFill != null)
            {
                EnsureBarSprite(_hpBarFill);
                _hpBarFill.fillAmount = s.MaxHP > 0 ? Mathf.Clamp01(s.CurrentHP / (float)s.MaxHP) : 0;
            }
            if (_pencilBarFill != null)
            {
                EnsureBarSprite(_pencilBarFill);
                _pencilBarFill.fillAmount = s.MaxPencil > 0 ? Mathf.Clamp01(s.CurrentPencil / (float)s.MaxPencil) : 0;
            }

            EnsureFloorModBanner();
            EnsureModifierInfoBox();
            EnsureBagPanel();
            RefreshBag();
            if (_bagHighlightEndTime > 0f && Time.time > _bagHighlightEndTime)
            {
                _bagHighlightCells.Clear();
                _bagHighlightEndTime = 0f;
                var brd = _map?.Run?.CurrentBoard;
                if (brd != null) RenderBoard(brd);
            }
        }

        private static void EnsureBarSprite(Image fill)
        {
            if (fill.sprite != null) return;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) tex.SetPixel(x, y, Color.white);
            tex.Apply();
            fill.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
        }

        // ────────────────────── Floor modifier display ──────────────────────

        private void EnsureFloorModBanner()
        {
            var run = _map?.Run;
            if (run?.State == null) return;
            var mods = run.State.ActiveFloorModifiers;

            if (mods == null || mods.Count == 0)
            {
                if (_floorModBanner != null) _floorModBanner.SetActive(false);
                return;
            }

            if (_floorModBanner == null && _sudokuPanel != null)
            {
                _floorModBanner = new GameObject("FloorModBanner", typeof(RectTransform), typeof(Image));
                _floorModBanner.transform.SetParent(_sudokuPanel.transform, false);
                var rt = _floorModBanner.GetComponent<RectTransform>();
                // Positioned below level info (0.87-0.92) and above status text (0.79-0.83)
                rt.anchorMin = new Vector2(0.27f, 0.835f);
                rt.anchorMax = new Vector2(0.73f, 0.865f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _floorModBanner.GetComponent<Image>().color = new Color(0.15f, 0.10f, 0.25f, 0.85f);

                _floorModText = CreateText(_floorModBanner.transform, "FloorModText", "",
                    12, TextAnchor.MiddleCenter, AccentGold);
                _floorModText.rectTransform.anchorMin = Vector2.zero;
                _floorModText.rectTransform.anchorMax = Vector2.one;
                _floorModText.rectTransform.offsetMin = new Vector2(4, 0);
                _floorModText.rectTransform.offsetMax = new Vector2(-4, 0);
            }

            if (_floorModBanner != null) _floorModBanner.SetActive(true);

            if (_floorModText != null)
            {
                var sb = new StringBuilder("Floor Modifiers: ");
                for (var i = 0; i < mods.Count; i++)
                {
                    if (i > 0) sb.Append(" | ");
                    sb.Append(FormatModName(mods[i]));
                }
                _floorModText.text = sb.ToString();
            }
        }

        private void EnsureModifierInfoBox()
        {
            var run = _map?.Run;
            if (run?.State == null || run.CurrentLevelConfig == null) return;
            var activeMods = run.CurrentLevelConfig.ActiveModifiers;

            if (activeMods == null || activeMods.Count == 0)
            {
                if (_modifierInfoBox != null) _modifierInfoBox.SetActive(false);
                return;
            }

            if (_modifierInfoBox == null && _sudokuPanel != null)
            {
                _modifierInfoBox = new GameObject("ModInfoBox", typeof(RectTransform), typeof(Image));
                _modifierInfoBox.transform.SetParent(_sudokuPanel.transform, false);
                var rt = _modifierInfoBox.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.74f, 0.08f);
                rt.anchorMax = new Vector2(0.97f, 0.27f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _modifierInfoBox.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.18f, 0.90f);

                _modifierInfoText = CreateText(_modifierInfoBox.transform, "ModInfoText", "",
                    10, TextAnchor.UpperLeft, TextColor);
                _modifierInfoText.rectTransform.anchorMin = Vector2.zero;
                _modifierInfoText.rectTransform.anchorMax = Vector2.one;
                _modifierInfoText.rectTransform.offsetMin = new Vector2(4, 4);
                _modifierInfoText.rectTransform.offsetMax = new Vector2(-4, -4);
            }

            if (_modifierInfoBox != null) _modifierInfoBox.SetActive(true);

            if (_modifierInfoText != null)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < activeMods.Count; i++)
                {
                    if (i > 0) sb.Append('\n');
                    sb.Append(FormatModName(activeMods[i]));
                    sb.Append(": ");
                    sb.Append(GetModDesc(activeMods[i]));
                }
                _modifierInfoText.text = sb.ToString();
            }
        }

        // ────────────────────── Completion / Game Over ──────────────────────

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

            ShowRewardScreen();
        }

        private void CheckGameOver()
        {
            if (_gameOverShown) return;
            var run = _map?.Run;
            if (run == null || !run.IsPlayerDead) return;
            _gameOverShown = true;
            ShowGameOver(run, false);
        }

        private void ShowGameOver(RunDirector run, bool victory)
        {
            SetActive(_sudokuPanel, false);
            SetActive(_pathPanel, false);
            SetActive(_gameOverPanel, true);

            // Dynamic title
            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = victory ? "Victory!" : "Defeat";
                _gameOverSummary.color = victory
                    ? new Color(0.85f, 0.75f, 0.20f, 1f)   // gold for victory
                    : new Color(0.90f, 0.30f, 0.20f, 1f);  // red for defeat
            }

            // Snapshot XP before persisting so we can show level-up
            var classId = run.State.ClassId;
            var snapshotSave = new SaveFileService();
            var preXp = snapshotSave.HasSaveFile()
                ? GetClassTotalXpFromSave(snapshotSave.Load(), classId) : 0;
            var preLevel = XpTable.DeriveLevel(preXp);

            var result = _map.BuildRunResult(victory, 0, 0);
            PersistResult(result);

            // Load updated XP after persist; fallback to in-memory calculation
            var postSave = new SaveFileService();
            var postXp = postSave.HasSaveFile()
                ? GetClassTotalXpFromSave(postSave.Load(), classId) : preXp + (result?.XpEarned ?? 0);
            var postLevel = XpTable.DeriveLevel(postXp);
            var postXpInto = postXp - XpTable.CumulativeXpForLevel(postLevel);
            var postXpToNext = postLevel < 40 ? XpTable.XpToNextLevel(postLevel) : 0;

            if (_gameOverDetails != null)
            {
                var sb = new StringBuilder();

                // Run summary
                sb.AppendLine($"Class: {classId}   Floor: {run.State.Depth}");
                sb.AppendLine($"HP: {run.State.CurrentHP}/{run.State.MaxHP}   Gold: {run.State.CurrentGold}");

                // XP earned this run
                var xpEarned = result?.XpEarned ?? 0;
                sb.AppendLine($"XP Earned This Run: +{xpEarned}");
                sb.AppendLine();

                // Level-up notification
                if (preLevel != postLevel)
                    sb.AppendLine($"  Level Up!  {preLevel}  →  {postLevel}");

                // XP progress bar toward next level
                if (postLevel < 40)
                {
                    var barLen = 16;
                    var filled = postXpToNext > 0
                        ? Math.Min(barLen, (int)(postXpInto * barLen / (float)postXpToNext))
                        : 0;
                    var bar = new string('=', filled) + new string('-', barLen - filled);
                    sb.AppendLine($"{classId}  Lv {postLevel}");
                    sb.AppendLine($"[{bar}]  {postXpInto} / {postXpToNext} XP");
                    sb.AppendLine($"(to Level {postLevel + 1})");
                }
                else
                {
                    sb.AppendLine($"{classId}  Level MAX  (Lv 40)");
                }

                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        private static int GetClassTotalXpFromSave(SaveFileEnvelope envelope, ClassId classId)
        {
            var entries = envelope?.MetaProgress?.GardenProgression?.ClassEntries;
            if (entries == null) return 0;
            for (var i = 0; i < entries.Count; i++)
                if (entries[i].ClassId == classId) return entries[i].TotalXp;
            return 0;
        }

        private void PersistResult(RunResult result)
        {
            if (result == null || result.TutorialMode) return;

            // Populate item/relic counts from the run state
            var run = _map?.Run;
            if (run != null)
            {
                // Use analytics total (accumulated across all puzzles in the run)
                result.ItemsUsedThisRun = run.GetAnalytics()?.TotalItemsUsed
                    ?? run.CurrentLevelState?.ItemsUsedThisLevel ?? 0;
                result.AcquiredRelic = run.State.HasRelic;
            }

            // ProfileService handles XP commit, class unlock evaluation, stats, and saving
            var profile = new ProfileService(new SaveFileService());
            profile.RecordRunAndGetNewUnlocks(result);

            // Clear active run state
            var save = new SaveFileService();
            if (save.HasSaveFile())
            {
                var envelope = save.Load();
                envelope.ActiveRunState = null;
                envelope.ActivePuzzle = null;
                save.Save(envelope);
            }
        }

        // ────────────────────── Rewards ──────────────────────

        private void ShowRewardScreen()
        {
            ShowPath();
            BuildRewardPanel();

            if (!_map.TryClaimCurrentPuzzleRewards(out var gold, out var slots))
            {
                _awaitingReward = false;
                SetStatus("No rewards.");
                HidePanel(_rewardPanel);
                return;
            }

            _awaitingReward = true;
            _rewardSlots.Clear();
            _rewardSlots.AddRange(slots);

            if (_rewardSummary != null)
                _rewardSummary.text = $"Puzzle complete! Gold +{gold}\nChoose a reward ({slots.Count} slots).";

            RebuildRewardButtons();
            if (_rewardPanel != null) _rewardPanel.SetActive(true);
        }

        private void BuildRewardPanel()
        {
            if (_rewardPanel != null || _pathPanel == null) return;
            _rewardPanel = CreateOverlayPanel(_pathPanel.transform, "RewardPanel", "Reward");
            _rewardSummary = _rewardPanel.transform.Find("Summary")?.GetComponent<Text>();
            _rewardPanel.SetActive(false);
        }

        private void RebuildRewardButtons()
        {
            if (_rewardPanel == null) return;
            ClearNamedChildren(_rewardPanel.transform, "Slot_");

            var cols = Mathf.Clamp(_rewardSlots.Count, 1, 3);
            for (var i = 0; i < _rewardSlots.Count; i++)
            {
                var item = _rewardSlots[i];
                var col = i % cols;
                var row = i / cols;
                var xMin = 0.08f + col * 0.29f;
                var yMax = 0.50f - row * 0.18f;

                var itemLabel = item != null
                    ? $"{ItemService.GetItemName(item.Type)}\n{item.Rarity}\n{ItemService.GetItemDescription(item.Type, item.Rarity)}"
                    : "Nothing";
                var btn = CreatePanelButton(_rewardPanel.transform, $"Slot_{i}",
                    new Vector2(xMin, yMax - 0.14f), new Vector2(xMin + 0.26f, yMax),
                    itemLabel);
                if (item != null)
                    SetButtonIcon(btn, ItemService.GetIconName(item.Type));
                var idx = i;
                btn.onClick.AddListener(() => ClaimReward(idx));
            }

            // Skip button
            var skip = CreatePanelButton(_rewardPanel.transform, "Slot_skip",
                new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.18f), "Skip");
            skip.onClick.AddListener(() =>
            {
                _awaitingReward = false;
                HidePanel(_rewardPanel);
                HandlePostReward();
            });
        }

        private void ClaimReward(int index)
        {
            var run = _map?.Run;
            if (run == null) return;

            if (index >= 0 && index < _rewardSlots.Count && _rewardSlots[index] != null)
            {
                var claimed = _rewardSlots[index];

                if (run.IsBagFull())
                {
                    // Bag is full — show replacement panel before finishing the reward
                    ShowBagSwapPanel(
                        claimed,
                        slotToReplace =>
                        {
                            run.ReplaceItemInInventory(slotToReplace, claimed);
                            new ProfileService(new SaveFileService()).RecordItemDiscovery(claimed.Type);
                            _awaitingReward = false;
                            _rewardSlots.Clear();
                            HidePanel(_rewardPanel);
                            HidePanel(_bagSwapPanel);
                            HandlePostReward();
                        },
                        () =>
                        {
                            // Abort — skip this reward slot
                            _awaitingReward = false;
                            _rewardSlots.Clear();
                            HidePanel(_rewardPanel);
                            HidePanel(_bagSwapPanel);
                            HandlePostReward();
                        });
                    return;
                }

                run.PickRewardItem(index);
                new ProfileService(new SaveFileService()).RecordItemDiscovery(claimed.Type);
            }

            _awaitingReward = false;
            _rewardSlots.Clear();
            HidePanel(_rewardPanel);
            HandlePostReward();
        }

        private void HandlePostReward()
        {
            var node = _map?.Run?.GetCurrentNode();
            if (node != null && node.Type == NodeType.Boss)
            {
                var s = _map.Run.State;
                if (s.CurrentFloor >= s.TotalFloors - 1)
                {
                    ShowGameOver(_map.Run, true);
                    return;
                }
                _map.AdvanceToNextFloor();
                SetStatus("Garden cleared! Next floor...");
            }
            ShowPath();
        }

        // ────────────────────── Bag-full swap panel ──────────────────────

        private void ShowBagSwapPanel(ItemInstance newItem, System.Action<int> onReplace, System.Action onAbort)
        {
            _pendingSwapItem = newItem;
            _onSwapSlotChosen = onReplace;
            _onSwapAborted = onAbort;

            // Lazily create the panel parented to path panel (or canvas root if needed)
            var parent = _pathPanel?.transform ?? FindFirstObjectByType<Canvas>()?.transform;
            if (parent == null) return;

            if (_bagSwapPanel != null) Destroy(_bagSwapPanel);
            _bagSwapPanel = new GameObject("BagSwapPanel", typeof(RectTransform), typeof(Image));
            _bagSwapPanel.transform.SetParent(parent, false);
            var rt = _bagSwapPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.25f);
            rt.anchorMax = new Vector2(0.75f, 0.75f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _bagSwapPanel.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.14f, 0.97f);

            var outline = _bagSwapPanel.AddComponent<Outline>();
            outline.effectColor = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            var title = CreateText(_bagSwapPanel.transform, "SwapTitle",
                $"Bag Full — Choose a slot to replace with:\n{ItemService.GetItemName(newItem.Type)} ({newItem.Rarity})\n{ItemService.GetItemDescription(newItem.Type, newItem.Rarity)}",
                13, TextAnchor.UpperCenter, AccentGold);
            title.rectTransform.anchorMin = new Vector2(0.04f, 0.72f);
            title.rectTransform.anchorMax = new Vector2(0.96f, 0.98f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            var run = _map?.Run;
            var heldItems = run?.State?.HeldItems;
            var slotCount = run?.State?.ItemSlots ?? 0;

            for (var i = 0; i < slotCount; i++)
            {
                var item = (heldItems != null && i < heldItems.Count) ? heldItems[i] : null;
                var col = i % 3;
                var row = i / 3;
                var xMin = 0.05f + col * 0.32f;
                var yMax = 0.68f - row * 0.25f;
                var label = item != null
                    ? $"{ItemService.GetItemName(item.Type)}\n{item.Rarity}"
                    : "Empty";
                var slotBtn = CreatePanelButton(_bagSwapPanel.transform, $"SwapSlot_{i}",
                    new Vector2(xMin, yMax - 0.20f), new Vector2(xMin + 0.29f, yMax), label);
                if (item != null)
                    SetButtonIcon(slotBtn, ItemService.GetIconName(item.Type));
                var capturedIdx = i;
                slotBtn.onClick.AddListener(() => _onSwapSlotChosen?.Invoke(capturedIdx));
            }

            var abortBtn = CreatePanelButton(_bagSwapPanel.transform, "SwapAbort",
                new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.14f), "Keep Bag (Abort)");
            abortBtn.onClick.AddListener(() => _onSwapAborted?.Invoke());

            _bagSwapPanel.SetActive(true);
        }

        // ────────────────────── Shop ──────────────────────

        private void HandleShop()
        {
            var run = _map?.Run;
            if (run == null) return;

            BuildShopPanel();
            _shopOffers.Clear();
            _shopOffers.AddRange(run.BuildShopOffers());
            RebuildShopButtons();
            if (_shopPanel != null) _shopPanel.SetActive(true);
        }

        private void BuildShopPanel()
        {
            if (_shopPanel != null || _pathPanel == null) return;
            _shopPanel = CreateOverlayPanel(_pathPanel.transform, "ShopPanel", "Shop");
            _shopSummary = _shopPanel.transform.Find("Summary")?.GetComponent<Text>();
            _shopPanel.SetActive(false);
        }

        private void RebuildShopButtons()
        {
            if (_shopPanel == null) return;
            ClearNamedChildren(_shopPanel.transform, "Offer_");
            _selectedShopOffer = -1;

            var s = _map?.Run?.State;
            if (_shopSummary != null && s != null)
                _shopSummary.text = $"Gold: {s.CurrentGold}  |  Click an item to preview, then press Buy.";

            for (var i = 0; i < Mathf.Min(3, _shopOffers.Count); i++)
            {
                var offer = _shopOffers[i];
                var label = offer.Item != null
                    ? $"{ItemService.GetItemName(offer.Item.Type)}\n{offer.Price}g"
                    : $"Offer {offer.Price}g";
                var btn = CreatePanelButton(_shopPanel.transform, $"Offer_{i}",
                    new Vector2(0.08f + i * 0.29f, 0.40f), new Vector2(0.08f + i * 0.29f + 0.26f, 0.70f), label);
                if (offer.Item != null)
                    SetButtonIcon(btn, ItemService.GetIconName(offer.Item.Type));
                var idx = i;
                btn.onClick.AddListener(() => SelectShopOffer(idx));
            }

            // Buy button (disabled until an offer is selected)
            var buyBtn = CreatePanelButton(_shopPanel.transform, "Offer_buy",
                new Vector2(0.35f, 0.26f), new Vector2(0.65f, 0.36f), "Buy");
            buyBtn.interactable = false;
            buyBtn.gameObject.name = "ShopBuyBtn";
            buyBtn.onClick.AddListener(TryBuySelectedOffer);

            var skip = CreatePanelButton(_shopPanel.transform, "Offer_skip",
                new Vector2(0.35f, 0.13f), new Vector2(0.65f, 0.23f), "Skip");
            skip.onClick.AddListener(() =>
            {
                _shopOffers.Clear();
                _selectedShopOffer = -1;
                HidePanel(_shopPanel);
                _pathMessage = "Shop skipped.";
            });
        }

        private void SelectShopOffer(int idx)
        {
            _selectedShopOffer = idx;
            var offer = (idx >= 0 && idx < _shopOffers.Count) ? _shopOffers[idx] : null;

            // Update summary with item description
            if (_shopSummary != null && offer != null)
            {
                var s = _map?.Run?.State;
                var goldTxt = s != null ? $"Gold: {s.CurrentGold}  |  " : "";
                var desc = offer.Item != null
                    ? $"{goldTxt}{ItemService.GetItemName(offer.Item.Type)} ({offer.Item.Rarity}) — {offer.Price}g\n{ItemService.GetItemDescription(offer.Item.Type, offer.Item.Rarity)}"
                    : $"{goldTxt}Offer — {offer.Price}g";
                _shopSummary.text = desc;
            }

            // Highlight selected, un-highlight others; enable Buy button
            for (var i = 0; i < Mathf.Min(3, _shopOffers.Count); i++)
            {
                var offerGo = _shopPanel?.transform.Find($"Offer_{i}");
                if (offerGo == null) continue;
                var btn = offerGo.GetComponent<Button>();
                if (btn == null) continue;
                var cols = btn.colors;
                cols.normalColor = (i == idx) ? new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.45f) : BtnColor;
                btn.colors = cols;
            }

            var buyGo = _shopPanel?.transform.Find("ShopBuyBtn");
            if (buyGo != null)
            {
                var buyBtn = buyGo.GetComponent<Button>();
                if (buyBtn != null) buyBtn.interactable = true;
            }
        }

        private void TryBuySelectedOffer()
        {
            var run = _map?.Run;
            if (run == null || _selectedShopOffer < 0 || _selectedShopOffer >= _shopOffers.Count) return;
            var offer = _shopOffers[_selectedShopOffer];
            if (offer == null || offer.Item == null) return;

            // Gold check before proceeding
            if (run.State.CurrentGold < offer.Price) { SetStatus("Not enough gold."); return; }

            if (offer.Item != null && run.IsBagFull())
            {
                // Bag is full — show swap panel; deduct gold only after confirmed
                ShowBagSwapPanel(
                    offer.Item,
                    slotToReplace =>
                    {
                        if (!run.TryPurchaseShopOffer(_selectedShopOffer))
                        {
                            SetStatus("Not enough gold.");
                            HidePanel(_bagSwapPanel);
                            return;
                        }
                        // AddItemToInventory inside TryPurchaseShopOffer silently fails (bag full),
                        // so manually replace the chosen slot with the purchased item.
                        run.ReplaceItemInInventory(slotToReplace, offer.Item);
                        new ProfileService(new SaveFileService()).RecordItemDiscovery(offer.Item.Type);
                        _shopOffers.Clear();
                        _selectedShopOffer = -1;
                        HidePanel(_shopPanel);
                        HidePanel(_bagSwapPanel);
                        _pathMessage = "Purchased!";
                        RefreshPathOverview();
                    },
                    () => HidePanel(_bagSwapPanel));
                return;
            }

            if (!run.TryPurchaseShopOffer(_selectedShopOffer)) { SetStatus("Not enough gold."); return; }
            var purchasedItem = offer.Item;
            _shopOffers.Clear();
            _selectedShopOffer = -1;
            HidePanel(_shopPanel);
            _pathMessage = "Purchased!";
            if (purchasedItem != null)
                new ProfileService(new SaveFileService()).RecordItemDiscovery(purchasedItem.Type);
            RefreshPathOverview();
        }

        // ────────────────────── Rest / Event / Relic nodes ──────────────────────

        private void HandleRest()
        {
            var s = _map?.Run?.State;
            if (s == null) return;
            var heal = Mathf.Max(1, Mathf.CeilToInt(s.MaxHP * 0.10f));
            var before = s.CurrentHP;
            s.CurrentHP = Mathf.Min(s.MaxHP, s.CurrentHP + heal);
            _pathMessage = $"Rested. HP {before} -> {s.CurrentHP}";
        }

        private void HandleEvent()
        {
            var evt = _map.OpenEventNode();
            if (evt == null || evt.Options.Count == 0)
            {
                _pathMessage = "Event: nothing happened.";
                return;
            }
            _map.ChooseEventOption(0);
            _pathMessage = $"Event: {evt.Title}\nChose: {evt.Options[0].Label}";
        }

        private void HandleRelic()
        {
            var run = _map?.Run;
            if (run == null) return;
            var relic = run.RollRelicReward();
            if (relic == null) return;
            run.AcceptRelic(relic);
            new ProfileService(new SaveFileService()).RecordRelicDiscovery(relic.Id);
            _pathMessage = $"Relic: {relic.Id}";
        }

        // ────────────────────── Boss gate ──────────────────────

        private void ShowBossGateChoice()
        {
            if (_awaitingBossGate) return;
            var run = _map?.Run;
            if (run == null) return;

            _bossGateOptions.Clear();
            _selectedBossMods.Clear();
            var choices = run.RollBossModifierChoices();
            if (choices != null) _bossGateOptions.AddRange(choices);
            if (_bossGateOptions.Count == 0) return;

            run.GetBossModifierCounts(out _, out _bossPicksRequired);
            _awaitingBossGate = true;
            BuildBossGatePanel();
        }

        private void BuildBossGatePanel()
        {
            if (_bossGatePanel != null) Destroy(_bossGatePanel);
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _bossGatePanel = new GameObject("BossGatePanel", typeof(RectTransform), typeof(Image));
            _bossGatePanel.transform.SetParent(canvas.transform, false);
            var pr = _bossGatePanel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.15f, 0.10f);
            pr.anchorMax = new Vector2(0.85f, 0.90f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            _bossGatePanel.GetComponent<Image>().color = PanelBg;

            _bossGateTitle = CreateText(_bossGatePanel.transform, "Title",
                $"Boss Gate — Pick {_bossPicksRequired} Modifiers (0/{_bossPicksRequired})",
                18, TextAnchor.MiddleCenter, AccentGold);
            _bossGateTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.90f);
            _bossGateTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            _bossGateTitle.rectTransform.offsetMin = Vector2.zero;
            _bossGateTitle.rectTransform.offsetMax = Vector2.zero;

            var run2 = _map?.Run;
            var seenMods = run2?.State?.SeenBossModifiers;
            for (var i = 0; i < _bossGateOptions.Count; i++)
            {
                var mod = _bossGateOptions[i];
                var seen = seenMods != null && seenMods.Contains(mod);
                var col = i % 3; var row = i / 3;
                var xMin = 0.05f + col * 0.31f;
                var yMax = 0.85f - row * 0.25f;
                var labelText = seen ? $"{FormatModName(mod)}\n{GetModDesc(mod)}" : "???";
                var btn = CreatePanelButton(_bossGatePanel.transform, $"Mod_{i}",
                    new Vector2(xMin, yMax - 0.22f), new Vector2(xMin + 0.28f, yMax),
                    labelText);
                SetButtonIcon(btn, seen ? BossService.GetIconName(mod) : null, !seen);
                var captured = mod;
                btn.onClick.AddListener(() => ToggleBossModSelection(captured, btn));
            }

            // Confirm button (disabled until enough picks)
            var confirmBtn = CreatePanelButton(_bossGatePanel.transform, "ConfirmBoss",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f),
                "Confirm");
            confirmBtn.interactable = false;
            confirmBtn.onClick.AddListener(ConfirmBossGateAll);
            confirmBtn.gameObject.name = "ConfirmBossBtn";
        }

        private void ToggleBossModSelection(BossModifierId mod, Button btn)
        {
            if (_selectedBossMods.Contains(mod))
            {
                _selectedBossMods.Remove(mod);
                var colors = btn.colors;
                colors.normalColor = BtnColor;
                btn.colors = colors;
            }
            else if (_selectedBossMods.Count < _bossPicksRequired)
            {
                _selectedBossMods.Add(mod);
                var colors = btn.colors;
                colors.normalColor = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.5f);
                btn.colors = colors;
            }

            // Update title
            if (_bossGateTitle != null)
                _bossGateTitle.text = $"Boss Gate — Pick {_bossPicksRequired} Modifiers ({_selectedBossMods.Count}/{_bossPicksRequired})";

            // Enable/disable confirm
            var confirmGo = _bossGatePanel?.transform.Find("ConfirmBossBtn");
            if (confirmGo != null)
            {
                var confirmBtn = confirmGo.GetComponent<Button>();
                if (confirmBtn != null)
                    confirmBtn.interactable = _selectedBossMods.Count >= _bossPicksRequired;
            }
        }

        private void ConfirmBossGateAll()
        {
            _awaitingBossGate = false;
            var run = _map?.Run;
            if (run != null) run.ChooseBossModifiers(new List<BossModifierId>(_selectedBossMods));
            if (_bossGatePanel != null) { Destroy(_bossGatePanel); _bossGatePanel = null; }

            // Directly advance to the boss node (skip boss gate check)
            var idx = _bossNodeIndex;
            _bossNodeIndex = -1;
            if (idx < 0) return;

            if (!_map.TryAdvanceToNodeAndStartPuzzle(idx, out _, out var level)) return;
            _pathMessage = string.Empty;
            _completionHandled = false;
            _gameOverShown = false;
            _overlaysBuilt = false;
            _selectedRow = -1;
            _selectedCol = -1;
            _highlightValue = 0;
            if (level == null) { ShowPath(); return; }
            SetStatus($"Boss - {level.BoardSize}x{level.BoardSize} {level.Stars}*");
            ShowSudoku();
            RebuildBoard();
        }

        // ────────────────────── Modifier overlay rendering ──────────────────────

        private void BuildOverlays()
        {
            ClearOverlays();
            var overlay = _map?.Run?.CurrentOverlay;
            if (overlay == null || _cells.Count == 0) return;
            EnsureOverlayRoot();
            CopyRectTransform(_gridRoot, _gridOverlayRoot);

            var grid = _gridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null) return;
            var cW = grid.cellSize.x; var cH = grid.cellSize.y;
            var sX = grid.spacing.x; var sY = grid.spacing.y;
            var tW = cW * _boardSize + sX * (_boardSize - 1);
            var tH = cH * _boardSize + sY * (_boardSize - 1);

            // Lines — draw thin connecting spine first, then circles around each cell
            var lineWidthMult = _accessibility.GetLineWidthMultiplier();
            for (var li = 0; li < overlay.Lines.Count; li++)
            {
                var line = overlay.Lines[li];
                var color = GetLineColor(line.Type);
                var spineAlpha = new Color(color.r, color.g, color.b,
                    _accessibility.GetOverlayAlpha(color.a * 0.50f));
                var spineW = 3f * lineWidthMult;

                // Spine (thin line between cell-edge midpoints so numbers stay readable)
                for (var ci = 0; ci < line.Cells.Count - 1; ci++)
                    DrawOverlayLine(line.Cells[ci], line.Cells[ci + 1], spineAlpha, spineW, cW, cH, sX, sY, tW, tH);

                // Circles per cell — hollow ring outline
                var ringOuter = Mathf.Min(cW, cH) * 0.44f;
                for (var ci = 0; ci < line.Cells.Count; ci++)
                    DrawRingDot(line.Cells[ci], color, ringOuter, cW, cH, sX, sY, tW, tH);

                // Thermo: filled bulb at start
                if (line.Type == LineType.Thermo && line.Cells.Count > 0)
                    DrawDot(line.Cells[0], color, ringOuter + 4f, cW, cH, sX, sY, tW, tH);

                // SlowThermo: open (ring) bulb at start — distinguishes from strict thermo
                if (line.Type == LineType.SlowThermo && line.Cells.Count > 0)
                    DrawRingDot(line.Cells[0], color, ringOuter + 5f, cW, cH, sX, sY, tW, tH);

                // BetweenLine: hollow dots at both ends (larger ring for emphasis)
                if (line.Type == LineType.BetweenLine && line.Cells.Count >= 2)
                {
                    var last = line.Cells[line.Cells.Count - 1];
                    DrawRingDot(line.Cells[0], color, ringOuter + 4f, cW, cH, sX, sY, tW, tH);
                    DrawRingDot(last, color, ringOuter + 4f, cW, cH, sX, sY, tW, tH);
                }

                // UniqueSetLine: small "U" ring at both endpoints
                if (line.Type == LineType.UniqueSetLine && line.Cells.Count >= 2)
                {
                    DrawRingDot(line.Cells[0], color, ringOuter + 3f, cW, cH, sX, sY, tW, tH);
                    DrawRingDot(line.Cells[line.Cells.Count - 1], color, ringOuter + 3f, cW, cH, sX, sY, tW, tH);
                }
            }

            // Kropki dots (white/black standard + sum-labelled dots)
            for (var di = 0; di < overlay.KropkiDots.Count; di++)
            {
                var dot = overlay.KropkiDots[di];
                var mr = (dot.CellA.Row + dot.CellB.Row) / 2f;
                var mc = (dot.CellA.Col + dot.CellB.Col) / 2f;
                var px = mc * (cW + sX) + cW * 0.5f - tW * 0.5f;
                var py = -(mr * (cH + sY) + cH * 0.5f - tH * 0.5f);
                if (dot.SumValue > 0)
                {
                    // Sum Kropki: grey circle with sum label
                    DrawCircle(px, py, 12f, new Color(0.45f, 0.45f, 0.50f, 0.90f));
                    DrawOverlayText(px, py, dot.SumValue.ToString(), 9, new Color(1f, 1f, 1f, 1f));
                }
                else if (dot.IsBlack)
                    DrawCircle(px, py, 10f, new Color(0.1f, 0.1f, 0.1f, 0.9f));
                else
                    DrawRingAtPos(px, py, 10f, new Color(0.95f, 0.95f, 0.95f, 0.85f));
            }

            // Pair constraints (Greater/Less Than and XV Pairs)
            if (overlay.PairConstraints != null)
            {
                for (var pi = 0; pi < overlay.PairConstraints.Count; pi++)
                {
                    var pair = overlay.PairConstraints[pi];
                    var mr = (pair.CellA.Row + pair.CellB.Row) / 2f;
                    var mc = (pair.CellA.Col + pair.CellB.Col) / 2f;
                    var px = mc * (cW + sX) + cW * 0.5f - tW * 0.5f;
                    var py = -(mr * (cH + sY) + cH * 0.5f - tH * 0.5f);
                    string label;
                    Color labelColor;
                    switch (pair.Type)
                    {
                        case PairConstraintType.GreaterThan:
                            label = ">"; labelColor = new Color(0.95f, 0.70f, 0.20f, 0.90f); break;
                        case PairConstraintType.LessThan:
                            label = "<"; labelColor = new Color(0.95f, 0.70f, 0.20f, 0.90f); break;
                        case PairConstraintType.SumX:
                            label = "X"; labelColor = new Color(0.20f, 0.80f, 0.90f, 0.90f); break;
                        case PairConstraintType.SumV:
                            label = "V"; labelColor = new Color(0.20f, 0.80f, 0.90f, 0.90f); break;
                        case PairConstraintType.SumK:
                            label = pair.Value.ToString(); labelColor = new Color(0.20f, 0.80f, 0.90f, 0.90f); break;
                        default:
                            label = "?"; labelColor = Color.white; break;
                    }
                    DrawOverlayText(px, py, label, 11, labelColor);
                }
            }

            // Killer cages
            _cageBorderEdges.Clear();
            for (var ci = 0; ci < overlay.KillerCages.Count; ci++)
            {
                var cage = overlay.KillerCages[ci];
                for (var c = 0; c < cage.Cells.Count; c++)
                    RecordCageEdges(cage, cage.Cells[c].Row, cage.Cells[c].Col);

                var tl = cage.Cells[0];
                for (var c = 1; c < cage.Cells.Count; c++)
                {
                    var cc = cage.Cells[c];
                    if (cc.Row < tl.Row || (cc.Row == tl.Row && cc.Col < tl.Col)) tl = cc;
                }
                var tlX = tl.Col * (cW + sX) - tW * 0.5f;
                var tlY = tH * 0.5f - tl.Row * (cH + sY);
                DrawCageSum(tlX, tlY, cage.Sum, cW, cH);
            }

            // Arrows
            for (var ai = 0; ai < overlay.Arrows.Count; ai++)
            {
                var arrow = overlay.Arrows[ai];
                var arrowColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
                DrawRingDot(arrow.CircleCell, arrowColor, 18f, cW, cH, sX, sY, tW, tH);
                var pathColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
                if (arrow.ArrowCells.Count > 0)
                    DrawOverlayLine(arrow.CircleCell, arrow.ArrowCells[0], pathColor, 3f, cW, cH, sX, sY, tW, tH);
                for (var pi = 0; pi < arrow.ArrowCells.Count - 1; pi++)
                    DrawOverlayLine(arrow.ArrowCells[pi], arrow.ArrowCells[pi + 1], pathColor, 3f, cW, cH, sX, sY, tW, tH);
            }

            // Cell markers
            for (var mi = 0; mi < overlay.CellMarkers.Count; mi++)
            {
                var m = overlay.CellMarkers[mi];
                var px = m.Cell.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
                var py = -(m.Cell.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
                if (m.Type == MarkerType.Even)
                {
                    var sq = new GameObject("Even", typeof(RectTransform), typeof(Image));
                    sq.transform.SetParent(_gridOverlayRoot, false);
                    var sr = sq.GetComponent<RectTransform>();
                    sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
                    sr.pivot = new Vector2(0.5f, 0.5f);
                    sr.anchoredPosition = new Vector2(px, py);
                    sr.sizeDelta = new Vector2(cW * 0.7f, cH * 0.7f);
                    sq.GetComponent<Image>().color = new Color(0.35f, 0.65f, 0.9f, 0.55f);
                    sq.GetComponent<Image>().raycastTarget = false;
                    _overlayObjects.Add(sq);
                }
                else if (m.Type == MarkerType.Odd)
                    DrawCircle(px, py, cW * 0.35f, new Color(0.9f, 0.55f, 0.2f, 0.55f));
                else if (m.Type == MarkerType.Prime)
                {
                    // Prime: gold pentagon-like ring + "P" label
                    DrawCircle(px, py, cW * 0.36f, new Color(0.85f, 0.75f, 0.15f, 0.50f));
                    DrawOverlayText(px, py, "P", 10, new Color(1f, 0.95f, 0.4f, 0.95f));
                }
                else if (m.Type == MarkerType.Fortress)
                {
                    // Fortress: grey shaded cell background
                    var sq = new GameObject("Fortress", typeof(RectTransform), typeof(Image));
                    sq.transform.SetParent(_gridOverlayRoot, false);
                    var sr = sq.GetComponent<RectTransform>();
                    sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
                    sr.pivot = new Vector2(0.5f, 0.5f);
                    sr.anchoredPosition = new Vector2(px, py);
                    sr.sizeDelta = new Vector2(cW * 0.88f, cH * 0.88f);
                    sq.GetComponent<Image>().color = new Color(0.45f, 0.45f, 0.50f, 0.45f);
                    sq.GetComponent<Image>().raycastTarget = false;
                    _overlayObjects.Add(sq);
                }
            }
        }

        private static Color GetLineColor(LineType t) => t switch
        {
            LineType.GermanWhispers  => new Color(0.20f, 0.72f, 0.30f, 0.55f),
            LineType.DutchWhispers   => new Color(0.10f, 0.95f, 0.78f, 0.75f),
            LineType.ParityLine      => new Color(0.10f, 0.88f, 0.80f, 0.70f),
            LineType.RenbanLine      => new Color(0.80f, 0.35f, 0.65f, 0.55f),
            LineType.Palindrome      => new Color(0.60f, 0.60f, 0.60f, 0.65f),
            LineType.Thermo          => new Color(0.85f, 0.45f, 0.10f, 0.70f),
            LineType.BetweenLine     => new Color(0.90f, 0.90f, 0.90f, 0.60f),
            LineType.ConsecutiveLine => new Color(0.95f, 0.65f, 0.15f, 0.70f),  // orange
            LineType.SlowThermo      => new Color(0.70f, 0.30f, 0.80f, 0.70f),  // purple (open bulb variant)
            LineType.UniqueSetLine   => new Color(0.20f, 0.70f, 0.95f, 0.65f),  // sky blue
            _ => new Color(0.20f, 0.72f, 0.30f, 0.55f)
        };

        private void DrawOverlayLine(CellCoord a, CellCoord b, Color color, float width,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var ax = a.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
            var ay = -(a.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
            var bx = b.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
            var by = -(b.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
            var dx = bx - ax; var dy = by - ay;
            var len = Mathf.Sqrt(dx * dx + dy * dy);

            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(ax, ay);
            rt.sizeDelta = new Vector2(len, width);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private void DrawDot(CellCoord c, Color color, float radius,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var px = c.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
            var py = -(c.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
            DrawCircle(px, py, radius, color);
        }

        private void DrawRingDot(CellCoord c, Color color, float radius,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var px = c.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
            var py = -(c.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
            DrawRingAtPos(px, py, radius, color);
        }

        private void DrawOverlayText(float x, float y, string text, int fontSize, Color color)
        {
            var go = new GameObject("OvTxt", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(26f, 18f);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private void DrawRingAtPos(float x, float y, float radius, Color color)
        {
            if (_ringSprite == null) _ringSprite = BuildRingSprite();
            var go = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(radius * 2, radius * 2);
            var img = go.GetComponent<Image>();
            img.sprite = _ringSprite;
            img.color = color;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private void DrawCircle(float x, float y, float radius, Color color)
        {
            if (_circleSprite == null) _circleSprite = BuildCircleSprite();
            var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(radius * 2, radius * 2);
            var img = go.GetComponent<Image>();
            img.sprite = _circleSprite;
            img.color = color;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private static Sprite BuildCircleSprite()
        {
            const int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var c = sz * 0.5f - 0.5f;
            for (var py = 0; py < sz; py++)
            for (var px = 0; px < sz; px++)
            {
                var d = Mathf.Sqrt((px - c) * (px - c) + (py - c) * (py - c));
                tex.SetPixel(px, py, new Color(1, 1, 1, Mathf.Clamp01(c - d + 0.5f)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
        }

        // Ring sprite: white ring, transparent inside (inner radius = 82% of outer = thin ring)
        private static Sprite BuildRingSprite()
        {
            const int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var c = sz * 0.5f - 0.5f;
            var outerR = c;
            var innerR = c * 0.82f;
            for (var py = 0; py < sz; py++)
            for (var px = 0; px < sz; px++)
            {
                var d = Mathf.Sqrt((px - c) * (px - c) + (py - c) * (py - c));
                var a = Mathf.Clamp01(outerR - d + 0.5f) * Mathf.Clamp01(d - innerR + 0.5f);
                tex.SetPixel(px, py, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
        }

        private void DrawCageSum(float x, float y, int sum, float cW, float cH)
        {
            var bg = new GameObject("CageSumBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_gridOverlayRoot, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0, 1);
            bgRt.anchoredPosition = new Vector2(x, y);
            bgRt.sizeDelta = new Vector2(cW * 0.46f, cH * 0.30f);
            bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            bg.GetComponent<Image>().raycastTarget = false;

            var txt = CreateText(bg.transform, "Sum", sum.ToString(), 11, TextAnchor.MiddleCenter, KillerBorder);
            StretchFill(txt.rectTransform);
            txt.raycastTarget = false;
            _overlayObjects.Add(bg);
        }

        private void RecordCageEdges(KillerCage cage, int r, int c)
        {
            CheckCageEdge(cage, r, c, -1, 0, 0);
            CheckCageEdge(cage, r, c, 1, 0, 1);
            CheckCageEdge(cage, r, c, 0, -1, 2);
            CheckCageEdge(cage, r, c, 0, 1, 3);
        }

        private void CheckCageEdge(KillerCage cage, int r, int c, int dr, int dc, int side)
        {
            var nr = r + dr; var nc = c + dc;
            for (var i = 0; i < cage.Cells.Count; i++)
                if (cage.Cells[i].Row == nr && cage.Cells[i].Col == nc) return;
            _cageBorderEdges.Add((long)r * _boardSize * 4 + c * 4 + side);
        }

        private bool IsCageEdge(int r, int c, int sz, int side)
            => _cageBorderEdges.Contains((long)r * sz * 4 + c * 4 + side);

        private void ClearOverlays()
        {
            for (var i = _overlayObjects.Count - 1; i >= 0; i--)
                if (_overlayObjects[i] != null) Destroy(_overlayObjects[i]);
            _overlayObjects.Clear();
        }

        // ────────────────────── Modifier descriptions ──────────────────────

        private static string FormatModName(BossModifierId m) => m switch
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
            // Extended pool
            BossModifierId.Antiking          => "Anti-King",
            BossModifierId.AntiBishop        => "Anti-Bishop",
            BossModifierId.NonconsecDiagonal => "Nonconsec. Diagonal",
            BossModifierId.DistanceGe2       => "Distance ≥ 2",
            BossModifierId.EntropyGlobal     => "Entropy",
            BossModifierId.ModularRegions    => "Modular Regions",
            BossModifierId.ConsecutiveLine   => "Consecutive Line",
            BossModifierId.SlowThermo        => "Slow Thermo",
            BossModifierId.UniqueSetLine     => "Unique Set Line",
            BossModifierId.FullKropki        => "Full Kropki",
            BossModifierId.SumKropki         => "Sum Dot",
            BossModifierId.GreaterLessThan   => "Greater/Less Than",
            BossModifierId.XVPairs           => "XV Pairs",
            BossModifierId.PrimeCells        => "Prime Cells",
            BossModifierId.FortressCells     => "Fortress Cells",
            _                                => m.ToString()
        };

        private static string GetModDesc(BossModifierId m) => m switch
        {
            BossModifierId.FogOfWar          => "Correct placements reveal hidden cells.",
            BossModifierId.ArrowSums         => "Arrow circles: digits on the arrow sum to the number in the circle.",
            BossModifierId.GermanWhispers    => "Green line: neighbours on the line must differ by 5 or more.",
            BossModifierId.DutchWhispers     => "Teal line: neighbours on the line must differ by 4 or more.",
            BossModifierId.ParityLines       => "Cyan line: adjacent cells must strictly alternate between odd and even.",
            BossModifierId.RenbanLines       => "Pink line: all digits on the line form a consecutive set (any order).",
            BossModifierId.KillerCages       => "Dashed cage: digits sum to the cage label; no repeats inside the cage.",
            BossModifierId.DifferenceKropki  => "White dot between cells: those two digits differ by exactly 1.",
            BossModifierId.RatioKropki       => "Black dot between cells: one digit is exactly double the other.",
            BossModifierId.Palindrome        => "Grey line: digits read the same from either end of the line.",
            BossModifierId.Thermo            => "Orange line with bulb: digits must strictly increase from bulb to tip.",
            BossModifierId.BetweenLines      => "White line: every digit on the line must fall strictly between the two endpoint values.",
            BossModifierId.EvenOdd           => "Blue square = even digit. Orange circle = odd digit.",
            BossModifierId.Nonconsecutive    => "Global: no two orthogonally adjacent cells may contain consecutive digits.",
            BossModifierId.Antiknight        => "Global: no two cells a chess knight's move apart may share the same digit.",
            // Extended pool
            BossModifierId.Antiking          => "Global: no two cells a king's move apart (including diagonal) may share a digit.",
            BossModifierId.AntiBishop        => "Global: no two cells on the same diagonal may share a digit.",
            BossModifierId.NonconsecDiagonal => "Global: diagonally adjacent cells cannot be consecutive (e.g. 4 cannot touch 3 or 5 diagonally).",
            BossModifierId.DistanceGe2       => "Global: equal digits must be at least 2 cells apart (no king-adjacent equal digits).",
            BossModifierId.EntropyGlobal     => "Every 3 consecutive row/col cells must contain one low (1–3), one mid (4–6), and one high (7–9) digit.",
            BossModifierId.ModularRegions    => "Every box region must contain at least one digit from {1–3}, {4–6}, and {7–9}.",
            BossModifierId.ConsecutiveLine   => "Orange line: adjacent cells on the line must differ by exactly 1 (e.g. 3-4-5).",
            BossModifierId.SlowThermo        => "Purple line with open bulb: digits must increase or stay equal from bulb to tip (e.g. 2-2-3-5).",
            BossModifierId.UniqueSetLine     => "Sky-blue line: no digit may repeat anywhere on the line.",
            BossModifierId.FullKropki        => "All diff-by-1 pairs show a white dot; all 1:2 ratio pairs show a black dot. No dot = neither.",
            BossModifierId.SumKropki         => "Labelled dot between two cells: those cells must sum to that value.",
            BossModifierId.GreaterLessThan   => "Orange chevrons (> / <) between cells indicate which digit must be larger.",
            BossModifierId.XVPairs           => "X between two cells: they sum to 10. V between two cells: they sum to 5.",
            BossModifierId.PrimeCells        => "Gold 'P' cells must contain a prime digit: 2, 3, 5, or 7.",
            BossModifierId.FortressCells     => "Grey shaded cells must be strictly greater than every orthogonally adjacent unshaded cell.",
            _                                => m.ToString()
        };

        // ────────────────────── Save / Resume ──────────────────────

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
                        _overlaysBuilt = false;
                        _selectedRow = -1;
                        _selectedCol = -1;
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

        // ────────────────────── UI Helpers ──────────────────────

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

        private static void ClearChildren(RectTransform root)
        {
            if (root == null) return;
            for (var i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private static void ClearNamedChildren(Transform parent, string prefix)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }
        }

        private static Text CreateText(Transform parent, string name, string content,
            int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = GetFont();
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            return t;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private GameObject CreateOverlayPanel(Transform parent, string name, string title)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.20f, 0.18f);
            pr.anchorMax = new Vector2(0.80f, 0.78f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = PanelBg;

            var t = CreateText(panel.transform, "Title", title, 22, TextAnchor.MiddleCenter, AccentGold);
            t.rectTransform.anchorMin = new Vector2(0.06f, 0.86f);
            t.rectTransform.anchorMax = new Vector2(0.94f, 0.97f);
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = Vector2.zero;

            var summary = CreateText(panel.transform, "Summary", "", 14, TextAnchor.UpperLeft, TextColor);
            summary.rectTransform.anchorMin = new Vector2(0.08f, 0.58f);
            summary.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            summary.rectTransform.offsetMin = Vector2.zero;
            summary.rectTransform.offsetMax = Vector2.zero;

            return panel;
        }

        private Button CreatePanelButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = BtnColor;

            var t = CreateText(go.transform, "Label", label, 12, TextAnchor.MiddleCenter, TextColor);
            StretchFill(t.rectTransform);
            t.verticalOverflow = VerticalWrapMode.Overflow;

            return go.GetComponent<Button>();
        }

        // Icon top 62%, label bottom 34% — used for reward/shop/boss-gate panel buttons.
        private static void SetButtonIcon(Button btn, string iconName, bool unknown = false)
        {
            var sprite = unknown ? null : (string.IsNullOrEmpty(iconName) ? null : Resources.Load<Sprite>("GeneratedIcons/icon_" + iconName));

            // Reposition label to bottom
            var label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.rectTransform.anchorMin = new Vector2(0.02f, 0.02f);
                label.rectTransform.anchorMax = new Vector2(0.98f, 0.36f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.fontSize = 10;
                label.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (sprite == null && !unknown) return;

            // Icon fills top 60% of button
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(btn.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.05f, 0.38f);
            iconRt.anchorMax = new Vector2(0.95f, 0.97f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var img = iconGo.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            else
            {
                // "???" placeholder
                img.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                var qText = new GameObject("UnknownLabel", typeof(RectTransform), typeof(Text));
                qText.transform.SetParent(iconGo.transform, false);
                var qt = qText.GetComponent<Text>();
                qt.text = "???";
                qt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                qt.fontSize = 22;
                qt.alignment = TextAnchor.MiddleCenter;
                qt.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                var qRt = qText.GetComponent<RectTransform>();
                qRt.anchorMin = Vector2.zero;
                qRt.anchorMax = Vector2.one;
                qRt.offsetMin = Vector2.zero;
                qRt.offsetMax = Vector2.zero;
            }
        }

        // ────────────────────── Bag panel ──────────────────────

        private void EnsureBagPanel()
        {
            if (_bagPanel != null || _sudokuPanel == null) return;
            var pr = _sudokuPanel.GetComponent<RectTransform>();
            if (pr == null) return;

            var bagGo = new GameObject("BagPanel", typeof(RectTransform), typeof(Image));
            bagGo.transform.SetParent(pr, false);
            var bagRt = bagGo.GetComponent<RectTransform>();
            bagRt.anchorMin = new Vector2(0.01f, 0.03f);
            bagRt.anchorMax = new Vector2(0.21f, 0.74f);
            bagRt.offsetMin = Vector2.zero;
            bagRt.offsetMax = Vector2.zero;
            bagGo.GetComponent<Image>().color = new Color(0.06f, 0.10f, 0.12f, 0.70f);
            _bagPanel = bagGo;

            var title = CreateText(bagGo.transform, "BagTitle", "BAG", 13, TextAnchor.MiddleCenter, AccentGold);
            title.rectTransform.anchorMin = new Vector2(0.02f, 0.93f);
            title.rectTransform.anchorMax = new Vector2(0.98f, 1.00f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            // Three item slots
            _bagItemButtons.Clear();
            var slotYs = new[] { (0.68f, 0.90f), (0.45f, 0.67f), (0.22f, 0.44f) };
            for (var i = 0; i < 3; i++)
            {
                var (yMin, yMax) = slotYs[i];
                var btn = CreateBagSlotButton(bagGo.transform, $"BagSlot_{i}",
                    new Vector2(0.03f, yMin), new Vector2(0.97f, yMax));
                _bagItemButtons.Add(btn);
                var idx = i;
                btn.onClick.AddListener(() => OnBagItemClicked(idx));
            }

            // Thin divider between items and relic
            var divGo = new GameObject("BagDivider", typeof(RectTransform), typeof(Image));
            divGo.transform.SetParent(bagGo.transform, false);
            var divRt = divGo.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.05f, 0.19f);
            divRt.anchorMax = new Vector2(0.95f, 0.21f);
            divRt.offsetMin = Vector2.zero;
            divRt.offsetMax = Vector2.zero;
            divGo.GetComponent<Image>().color = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.35f);

            // Relic slot
            _bagRelicButton = CreateBagSlotButton(bagGo.transform, "BagRelic",
                new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.17f));
            _bagRelicButton.onClick.RemoveAllListeners();
            _bagRelicButton.onClick.AddListener(OnBagRelicClicked);
        }

        private Button CreateBagSlotButton(Transform parent, string name, Vector2 ancMin, Vector2 ancMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin;
            rt.anchorMax = ancMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.16f, 0.20f, 0.80f);

            var btn = go.GetComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = new Color(0.10f, 0.16f, 0.20f, 0.80f);
            cols.highlightedColor = new Color(0.17f, 0.26f, 0.33f, 0.90f);
            cols.pressedColor = new Color(0.07f, 0.11f, 0.14f, 0.90f);
            cols.disabledColor = new Color(0.06f, 0.09f, 0.11f, 0.45f);
            btn.colors = cols;

            // Icon (left ~30%)
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.04f, 0.12f);
            iconRt.anchorMax = new Vector2(0.36f, 0.88f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            iconGo.GetComponent<Image>().raycastTarget = false;

            // Name text (right of icon)
            var nameText = CreateText(go.transform, "NameText", "", 10, TextAnchor.MiddleLeft, TextColor);
            nameText.rectTransform.anchorMin = new Vector2(0.38f, 0.04f);
            nameText.rectTransform.anchorMax = new Vector2(0.98f, 0.96f);
            nameText.rectTransform.offsetMin = new Vector2(2f, 1f);
            nameText.rectTransform.offsetMax = new Vector2(-2f, -1f);
            nameText.supportRichText = true;

            return btn;
        }

        private void RefreshBag()
        {
            if (_bagPanel == null) return;
            var state = _map?.Run?.State;

            for (var i = 0; i < _bagItemButtons.Count; i++)
            {
                var btn = _bagItemButtons[i];
                if (btn == null) continue;
                var iconImg = btn.transform.Find("Icon")?.GetComponent<Image>();
                var nameText = btn.transform.Find("NameText")?.GetComponent<Text>();
                var item = (state != null && i < state.HeldItems.Count) ? state.HeldItems[i] : null;

                if (item != null)
                {
                    btn.interactable = true;
                    if (iconImg != null)
                    {
                        var spr = Resources.Load<Sprite>("GeneratedIcons/icon_" + ItemService.GetIconName(item.Type));
                        iconImg.sprite = spr;
                        iconImg.color = spr != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                        iconImg.preserveAspect = true;
                    }
                    if (nameText != null)
                    {
                        var chargeTxt = item.Charges > 1 ? $" ×{item.Charges}" : "";
                        nameText.text = $"{ItemService.GetItemName(item.Type)}{chargeTxt}\n<size=8>{item.Rarity}</size>";
                        nameText.color = TextColor;
                    }
                }
                else
                {
                    btn.interactable = false;
                    if (iconImg != null) { iconImg.sprite = null; iconImg.color = new Color(1f, 1f, 1f, 0.08f); }
                    if (nameText != null)
                    {
                        nameText.text = (state != null && i < state.ItemSlots) ? "Empty" : "—";
                        nameText.color = new Color(0.45f, 0.45f, 0.45f, 0.55f);
                    }
                }
            }

            // Umbrella visual: if charges active, tint slot 0 (or whichever item it came from) — just show status text
            // (umbrella charges are tracked separately; visual is reflected in SetStatus calls)

            if (_bagRelicButton != null)
            {
                var iconImg = _bagRelicButton.transform.Find("Icon")?.GetComponent<Image>();
                var nameText = _bagRelicButton.transform.Find("NameText")?.GetComponent<Text>();
                if (state != null && state.HasRelic)
                {
                    var relic = state.HeldRelic;
                    _bagRelicButton.interactable = false; // passives not manually clickable; click shows description
                    if (iconImg != null)
                    {
                        var spr = Resources.Load<Sprite>("GeneratedIcons/icon_" + RelicService.GetIconName(relic.Id));
                        iconImg.sprite = spr;
                        iconImg.color = spr != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                        iconImg.preserveAspect = true;
                    }
                    if (nameText != null)
                    {
                        var useTxt = relic.UsesRemaining < 0 ? "Passive"
                            : relic.UsesRemaining == 0 ? "Spent" : $"×{relic.UsesRemaining}";
                        nameText.text = $"{RelicService.GetRelicName(relic.Id)}\n<size=8>{useTxt}</size>";
                        nameText.color = new Color(0.98f, 0.83f, 0.26f, 1f);
                    }
                    _bagRelicButton.interactable = true;
                }
                else
                {
                    _bagRelicButton.interactable = false;
                    if (iconImg != null) { iconImg.sprite = null; iconImg.color = new Color(1f, 1f, 1f, 0.08f); }
                    if (nameText != null) { nameText.text = "No Relic"; nameText.color = new Color(0.45f, 0.45f, 0.45f, 0.55f); }
                }
            }
        }

        private void OnBagItemClicked(int idx)
        {
            var run = _map?.Run;
            if (run == null || idx < 0 || idx >= run.State.HeldItems.Count) return;
            var item = run.State.HeldItems[idx];
            if (item == null) return;

            if (_selectedBagSlot == idx)
            {
                // Second click on same slot → use the item
                _selectedBagSlot = -1;
                RefreshBagHighlights();
                ApplyItemEffect(idx, item);
            }
            else
            {
                // First click → show description
                _selectedBagSlot = idx;
                RefreshBagHighlights();
                var desc = ItemService.GetItemDescription(item.Type, item.Rarity);
                SetStatus($"{ItemService.GetItemName(item.Type)}: {desc}  [click again to use]");
            }
        }

        private void RefreshBagHighlights()
        {
            for (var i = 0; i < _bagItemButtons.Count; i++)
            {
                var btn = _bagItemButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                if (img == null) continue;
                img.color = (i == _selectedBagSlot)
                    ? new Color(AccentGold.r * 0.5f, AccentGold.g * 0.5f, 0.10f, 0.90f)
                    : new Color(0.10f, 0.16f, 0.20f, 0.80f);
            }
        }

        private void OnBagRelicClicked()
        {
            var state = _map?.Run?.State;
            if (state == null || !state.HasRelic) return;
            var relic = state.HeldRelic;
            SetStatus($"{RelicService.GetRelicName(relic.Id)}: {RelicService.GetRelicDescription(relic.Id)}");
        }

        private void ApplyItemEffect(int slotIndex, ItemInstance item)
        {
            var run = _map?.Run;
            if (run == null) return;
            var board = run.CurrentBoard;
            var s = run.State;

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
                    var filled = SolveCells(board, run, _selectedRow, _selectedCol, neighbors);
                    run.TryUseItem(slotIndex);
                    SetStatus($"Solver: filled {filled} cell(s).");
                    RenderBoard(board);
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
                    RenderBoard(board);
                    break;
                }

                // ── Pattern Scroll: highlight conflict cells ──
                case ItemType.PatternScroll:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    _bagHighlightCells.Clear();
                    var zones = ItemService.GetPatternScrollZones(item.Rarity); // -1 = all
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
                    RenderBoard(board);
                    break;
                }

                // ── Koi Reflection: reveal candidates for N cells ──
                case ItemType.KoiReflection:
                {
                    if (board == null) { SetStatus("No active board."); return; }
                    var count = ItemService.GetKoiReflectionCells(item.Rarity);
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
                    RenderBoard(board);
                    break;
                }

                // ── Lantern of Clarity: disable fog for N moves ──
                case ItemType.LanternOfClarity:
                {
                    var moves = ItemService.GetLanternOfClarityMoves(item.Rarity);
                    _fogDisabledMovesRemaining += moves;
                    run.TryUseItem(slotIndex);
                    SetStatus($"Lantern of Clarity: fog disabled for {_fogDisabledMovesRemaining} moves.");
                    if (board != null) RenderBoard(board);
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
                    RenderBoard(board);
                    break;
                }

                // ── Pruning Shears: remove one incorrect candidate from selected cell ──
                case ItemType.PruningShears:
                {
                    if (board == null || _selectedRow < 0) { SetStatus("Select a cell first."); return; }
                    if (board.Cells[_selectedRow, _selectedCol] != 0) { SetStatus("Cell is already filled."); return; }
                    var marks = board.GetPencilMarks(_selectedRow, _selectedCol);
                    var correct = board.Solution[_selectedRow, _selectedCol];
                    var removed = false;
                    foreach (var v in marks)
                    {
                        if (v != correct) { board.RemovePencilMark(_selectedRow, _selectedCol, v); removed = true; break; }
                    }
                    if (!removed) { SetStatus("No incorrect candidates to prune."); return; }
                    run.TryUseItem(slotIndex);
                    SetStatus("Pruning Shears: removed one incorrect candidate.");
                    RenderBoard(board);
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
                    RenderBoard(board);
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
                    RenderBoard(board);
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
                    RenderBoard(board);
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
                    RenderBoard(board);
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
                    RenderBoard(board);
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
                    if (_awaitingReward)
                    {
                        var newSlots = run.BuildItemRewardSlots();
                        if (newSlots != null)
                        {
                            _rewardSlots.Clear();
                            _rewardSlots.AddRange(newSlots);
                            RebuildRewardButtons();
                        }
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

            RefreshBag();
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
            var dirs = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
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
            var best = -1;
            var bestFilled = 0;
            var bestType = 0; // 0=row, 1=col, 2=region

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
                SetStatus($"Silk Fan: swapped cells.");
                RenderBoard(board);
            }
        }

        // ────────────────────── Inner types ──────────────────────

        private sealed class CellView
        {
            public int Row;
            public int Col;
            public Image Image;
            public Text Label;
            public Text PencilLabel;
            public Image BorderTop;
            public Image BorderBottom;
            public Image BorderLeft;
            public Image BorderRight;
        }
    }
}
