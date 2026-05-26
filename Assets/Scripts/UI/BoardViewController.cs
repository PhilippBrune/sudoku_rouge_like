using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>Per-cell highlight types used by the tutorial system.</summary>
    public enum TutorialCellHighlight { Focus, Target, Conflict }

    /// <summary>
    /// Owns the sudoku grid: cell building, board rendering, and modifier overlay rendering.
    /// </summary>
    public sealed class BoardViewController : MonoBehaviour
    {
        // ── Injected ──
        private RunMapController _map;
        private RectTransform _gridRoot;

        /// <summary>Cells whose pencil marks should be hidden (blurred_sight curse). Set by InRunController.</summary>
        public HashSet<(int, int)> BlurredPencilCells;

        /// <summary>Per-cell tutorial highlights (row/column/box focus, target cell, conflict cell).
        /// Set by SudokuBasicsTutorialController via InRunController. Null = no highlights.</summary>
        public Dictionary<(int, int), TutorialCellHighlight> TutorialHighlightMap;

        // ── Board cells ──
        private List<CellView> _cells;
        private int _boardSize;

        // ── Overlay roots ──
        private RectTransform _gridOverlayRoot;
        private RectTransform _gridNumberRoot;
        private List<GameObject> _overlayObjects;
        private List<GameObject> _pressureOverlayObjects;  // dynamic threat overlays cleared each render
        private bool _overlaysBuilt;

        // ── Cage edges ──
        private HashSet<long> _cageBorderEdges;

        // ── Constraint conflict preview cache ──
        // Invalidated whenever the selected cell or highlight value changes.
        private HashSet<(int, int)> _conflictPreviewCells = new HashSet<(int, int)>();
        private int _prevConflictRow = -2, _prevConflictCol = -2, _prevConflictValue = -1;

        // ── Per-cell conflict cache (HasConflict) ──
        // Rebuilt only when the board state changes (digit placed/removed), not on every render.
        // Change detected via Knuth multiplicative checksum over all placed cell values.
        private readonly HashSet<(int, int)> _conflictCellsCache = new HashSet<(int, int)>();
        private uint _conflictCacheChecksum = uint.MaxValue; // sentinel: forces first build

        // ── Sprites ──
        private Sprite _circleSprite;
        private Sprite _ringSprite;

        // ── Colors (board-specific) — all sourced from GamePalette ──
        private static readonly Color EmptyCellColor        = GamePalette.CellEmpty;
        private static readonly Color GivenCellColor        = GamePalette.CellGiven;
        private static readonly Color SelectedColor         = GamePalette.CellSelected;
        private static readonly Color RowColHL              = GamePalette.CellRowCol;
        private static readonly Color MatchColor            = GamePalette.CellMatch;
        private static readonly Color ConflictColor         = GamePalette.CellConflict;
        private static readonly Color FogColor              = GamePalette.CellFog;
        private static readonly Color AntiknightForbidColor = GamePalette.AntiknightForbid;
        private static readonly Color BagHighlightColor     = GamePalette.BagHighlightOverlay;
        private static readonly Color RegionBorder          = GamePalette.RegionBorder;
        private static readonly Color KillerBorder          = GamePalette.KillerBorder;

        // ── Accessibility ──
        private AccessibilityService _accessibility;

        // ── Global constraint highlight ──
        private HashSet<(int, int)> _globalConstraintHighlight = new HashSet<(int, int)>();
        private static readonly Color GlobalConstraintHighlightColor = new Color(0.90f, 0.70f, 0.10f, 0.35f);

        // ── Tutorial highlight colors ──
        private static readonly Color TutorialFocusColor    = new Color(0.20f, 0.85f, 0.70f, 0.45f); // teal  — row/col/box
        private static readonly Color TutorialTargetColor   = new Color(0.95f, 0.85f, 0.15f, 0.75f); // gold  — guided cell
        private static readonly Color TutorialConflictColor = new Color(0.90f, 0.30f, 0.25f, 0.45f); // red   — blocking cell

        // ── Callbacks ──
        public Action<int, int> OnCellClicked;

        public HashSet<long> CageBorderEdges => _cageBorderEdges;

        public void Configure(RunMapController map, RectTransform gridRoot)
        {
            _map = map;
            _gridRoot = gridRoot;
            _cells = new List<CellView>();
            _overlayObjects = new List<GameObject>();
            _pressureOverlayObjects = new List<GameObject>();
            _cageBorderEdges = new HashSet<long>();
        }

        public void MarkOverlayDirty()
        {
            _overlaysBuilt = false;
            _conflictCacheChecksum = uint.MaxValue; // overlay affects HasConstraintConflict
        }

        public void HighlightGlobalConstraint(BossModifierId rule)
        {
            _globalConstraintHighlight.Clear();
            var board = _map?.Run?.CurrentBoard;
            if (board == null) return;
            var size = board.Size;

            if (rule == BossModifierId.Nonconsecutive)
            {
                // Highlight all orthogonally adjacent cell pairs
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                {
                    if (c + 1 < size) { _globalConstraintHighlight.Add((r, c)); _globalConstraintHighlight.Add((r, c + 1)); }
                    if (r + 1 < size) { _globalConstraintHighlight.Add((r, c)); _globalConstraintHighlight.Add((r + 1, c)); }
                }
            }
            else if (rule == BossModifierId.Antiknight)
            {
                // Highlight all knight-move pairs
                int[] dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
                int[] dc = { -1, 1, -2, 2, -2, 2, -1, 1 };
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                for (var k = 0; k < 8; k++)
                {
                    var nr = r + dr[k]; var nc = c + dc[k];
                    if (nr >= 0 && nr < size && nc >= 0 && nc < size)
                    { _globalConstraintHighlight.Add((r, c)); _globalConstraintHighlight.Add((nr, nc)); }
                }
            }
        }

        public void ClearGlobalConstraintHighlight() => _globalConstraintHighlight.Clear();

        /// <summary>V6: Plays a brief scale-punch on the cell at (row, col) when a digit is placed.</summary>
        public void PlayDigitPlaced(int row, int col)
        {
            var cell = _cells?.Find(c => c.Row == row && c.Col == col);
            if (cell == null) return;
            StartCoroutine(AnimationHelper.PulseScale(cell.Image.transform,
                1f, 1f + 0.15f, AnimationHelper.NumberPlaceDuration));
        }

        public void RebuildBoard(int selectedRow, int selectedCol, int highlightValue,
            bool hasAntiknight, HashSet<(int, int)> bagHighlightCells, int fogDisabledMovesRemaining,
            AccessibilityService accessibility, HashSet<(int, int)> lockedCells = null,
            HashSet<(int, int)> blurredPencilCells = null, PressureRenderData? pressure = null)
        {
            var run = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null || _gridRoot == null) return;

            if (_boardSize != board.Size || _cells.Count == 0)
            {
                BuildGrid(board.Size);
                UpdateAllBorders(board); // once per board — RegionMap never changes during a puzzle
                _overlaysBuilt = false;
            }

            _accessibility = accessibility;

            if (!_overlaysBuilt)
            {
                BuildOverlays(accessibility);
                _overlaysBuilt = true;
            }

            RenderBoard(board, selectedRow, selectedCol, highlightValue,
                hasAntiknight, bagHighlightCells, fogDisabledMovesRemaining,
                lockedCells, blurredPencilCells, pressure);
        }

        public void RenderBoard(SudokuBoard board, int selectedRow, int selectedCol, int highlightValue,
            bool hasAntiknight, HashSet<(int, int)> bagHighlightCells, int fogDisabledMovesRemaining,
            HashSet<(int, int)> lockedCells = null, HashSet<(int, int)> blurredPencilCells = null,
            PressureRenderData? pressure = null)
        {
            var overlay = _map?.Run?.CurrentOverlay;
            var engine  = _map?.Run?.ConstraintEngine;

            // Rebuild per-cell conflict cache only when the board state changed (digit placed/removed).
            // On selection-only renders the checksum matches and the cache is reused as-is.
            var boardChecksum = ComputeBoardChecksum(board);
            if (boardChecksum != _conflictCacheChecksum)
            {
                BuildConflictCache(board, overlay);
                _conflictCacheChecksum = boardChecksum;
            }

            // Build a HashSet of fogged cells once per render — O(1) lookup replaces O(n) scan per cell.
            HashSet<(int, int)> fogSet = null;
            if (fogDisabledMovesRemaining <= 0 && overlay?.FogCells?.Count > 0)
            {
                fogSet = new HashSet<(int, int)>(overlay.FogCells.Count);
                for (var f = 0; f < overlay.FogCells.Count; f++)
                    fogSet.Add((overlay.FogCells[f].Row, overlay.FogCells[f].Col));
            }

            // Recompute which placed cells conflict with the current selection + highlight value.
            // Uses the engine's full rule set so every active modifier is covered.
            if (engine != null && engine.RuleCount > 0 && selectedRow >= 0 && highlightValue > 0)
            {
                if (selectedRow != _prevConflictRow || selectedCol != _prevConflictCol
                    || highlightValue != _prevConflictValue)
                {
                    _conflictPreviewCells = engine.ComputeConflictCells(
                        board, selectedRow, selectedCol, highlightValue, overlay);
                    _prevConflictRow   = selectedRow;
                    _prevConflictCol   = selectedCol;
                    _prevConflictValue = highlightValue;
                }
            }
            else
            {
                if (_conflictPreviewCells.Count > 0) _conflictPreviewCells.Clear();
                _prevConflictRow = _prevConflictCol = -2; _prevConflictValue = -1;
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var val = board.Cells[cell.Row, cell.Col];
                var given = board.GivenMask[cell.Row, cell.Col];

                var labelText = val == 0 ? "" : val.ToString();
                if (labelText != cell.LastLabelText) { cell.Label.text = labelText; cell.LastLabelText = labelText; }
                cell.Label.color = given ? GamePalette.CellNumberGiven : Color.white;
                var pencilBlurred = (BlurredPencilCells != null && BlurredPencilCells.Contains((cell.Row, cell.Col)))
                                 || (blurredPencilCells != null && blurredPencilCells.Contains((cell.Row, cell.Col)));
                var pencilText = (val == 0 && !pencilBlurred) ? BuildPencilText(board, cell.Row, cell.Col) : "";
                if (pencilText != cell.LastPencilText) { cell.PencilLabel.text = pencilText; cell.LastPencilText = pencilText; }

                var c = _accessibility != null
                    ? _accessibility.GetCellColor(ComputeCellColor(board, cell.Row, cell.Col, given))
                    : ComputeCellColor(board, cell.Row, cell.Col, given);
                if (selectedRow == cell.Row && selectedCol == cell.Col)
                    c = SelectedColor;
                else if (selectedRow == cell.Row || selectedCol == cell.Col)
                    c = RowColHL;
                if (highlightValue > 0 && val == highlightValue)
                    c = MatchColor;

                // Constraint conflict preview: highlight cells that would be forbidden
                // by ANY active rule (row/col/region/Antiknight/Nonconsecutive/Kropki/etc.)
                // if highlightValue were placed at the selected cell.
                if (_conflictPreviewCells.Count > 0 && _conflictPreviewCells.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, AntiknightForbidColor, 0.70f);

                if (bagHighlightCells != null && bagHighlightCells.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, BagHighlightColor, 0.65f);

                if (_globalConstraintHighlight.Count > 0 && _globalConstraintHighlight.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, GlobalConstraintHighlightColor, 0.55f);

                if (fogSet != null && fogSet.Contains((cell.Row, cell.Col)))
                {
                    c = FogColor;
                    if (val == 0 && cell.LastLabelText != "") { cell.Label.text = ""; cell.LastLabelText = ""; }
                    if (cell.LastPencilText != "") { cell.PencilLabel.text = ""; cell.LastPencilText = ""; }
                }

                if (!given && _conflictCellsCache.Contains((cell.Row, cell.Col)))
                    c = ConflictColor;

                if (lockedCells != null && lockedCells.Contains((cell.Row, cell.Col))) // [REQ: DEBUFF-HOOK-012] CellLock: tint locked cells with GamePalette.CellLocked
                    c = Color.Lerp(c, GamePalette.CellLocked, 0.75f);

                if (TutorialHighlightMap != null &&
                    TutorialHighlightMap.TryGetValue((cell.Row, cell.Col), out var tutHL))
                {
                    switch (tutHL)
                    {
                        case TutorialCellHighlight.Focus:
                            c = Color.Lerp(c, TutorialFocusColor, 0.65f); break;
                        case TutorialCellHighlight.Target:
                            c = Color.Lerp(c, TutorialTargetColor, 0.75f); break;
                        case TutorialCellHighlight.Conflict:
                            c = Color.Lerp(c, TutorialConflictColor, 0.50f); break;
                    }
                }

                // ── Pressure mechanic overlays ──────────────────────────────────
                // [REQ: PRESSURE-UI-001] null pressure = tutorial — no overlays rendered
                // [REQ: PRESSURE-UI-006] haunted pulse  [PRESSURE-UI-007] crumbling tint
                // [REQ: PRESSURE-UI-002] badge text     [PRESSURE-UI-003] badge colour ratio ramp
                var badgeText = "";
                var badgeColor = Color.white;
                if (pressure.HasValue)
                {
                    var pr  = pressure.Value;
                    var key = cell.Row * 100 + cell.Col;

                    // Haunted cell: pulsing purple vignette while unfilled  [REQ: PRESSURE-UI-006]
                    if (val == 0 && pr.HauntedCell == (cell.Row, cell.Col))
                    {
                        var pulse      = (UnityEngine.Mathf.Sin(UnityEngine.Time.time * 2f) + 1f) * 0.5f;
                        var pulseAlpha = UnityEngine.Mathf.Lerp(0.25f, 0.65f, pulse);
                        c = Color.Lerp(c, new Color(0.45f, 0.10f, 0.60f, 1f), pulseAlpha);
                    }

                    // Crumbling cells: crack-brown tint intensifies as fragility drops  [REQ: PRESSURE-UI-007]
                    if (pr.CrumblingCells != null && pr.CrumblingCells.TryGetValue(key, out var frag))
                    {
                        var maxFrag       = pr.CrumblingMaxFragility > 0 ? pr.CrumblingMaxFragility : 4;
                        var crackIntensity = 1f - (float)frag / maxFrag;
                        c = Color.Lerp(c, new Color(0.60f, 0.35f, 0.15f, 1f), crackIntensity * 0.65f);
                    }

                    // Countdown fill badges: show RemainingPlacements in top-right corner  [REQ: PRESSURE-UI-002]
                    // [REQ: PRESSURE-UI-003] badge colour ramps by ratio: >50% grey, 25-50% amber, <25% red
                    if (pr.ActiveThreats != null)
                    {
                        foreach (var threat in pr.ActiveThreats)
                        {
                            if (threat.Cells == null || !threat.Cells.Contains((cell.Row, cell.Col)))
                                continue;
                            badgeText = threat.RemainingPlacements.ToString();
                            var ratio = threat.InitialPlacements > 0
                                ? threat.RemainingPlacements / (float)threat.InitialPlacements
                                : 0f;
                            if (ratio > 0.5f)
                                badgeColor = new Color(0.70f, 0.70f, 0.70f, 0.90f); // grey   (>50%)
                            else if (ratio > 0.25f)
                                badgeColor = new Color(1.00f, 0.65f, 0.10f, 0.90f); // amber (25–50%)
                            else
                                badgeColor = new Color(0.95f, 0.20f, 0.20f, 0.90f); // red   (<25%)
                            break;
                        }
                    }
                }

                cell.Image.color = c;
                if (cell.BadgeLabel != null)
                {
                    if (badgeText != cell.LastBadgeText) { cell.BadgeLabel.text = badgeText; cell.LastBadgeText = badgeText; }
                    cell.BadgeLabel.color = badgeColor;
                }
            }

            // [REQ: PRESSURE-UI-010,011] Dynamic pressure overlays (region strips, chain lines)
            if (pressure.HasValue)
                RenderPressureThreatOverlays(pressure.Value);
            else
            {
                // Clear any leftover pressure overlays when no pressure is active
                for (var i = _pressureOverlayObjects.Count - 1; i >= 0; i--)
                    if (_pressureOverlayObjects[i] != null) Destroy(_pressureOverlayObjects[i]);
                _pressureOverlayObjects.Clear();
            }
        }

        // ────────────────────── Grid building ──────────────────────

        private void BuildGrid(int size)
        {
            _boardSize = size;
            _cells.Clear();
            InRunUiFactory.ClearChildren(_gridRoot);

            var grid = _gridRoot.GetComponent<GridLayoutGroup>() ?? _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = size;
            const float TargetBoardPx = 620f;
            const float SpacingPx    = 2f;
            var cellPx = Mathf.Clamp(
                Mathf.Floor((TargetBoardPx - SpacingPx * (size - 1)) / size),
                48f, 128f);
            grid.spacing  = new Vector2(SpacingPx, SpacingPx);
            grid.cellSize = new Vector2(cellPx, cellPx);
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
                cellGo.GetComponent<Button>().onClick.AddListener(() => OnCellClicked?.Invoke(cr, cc));

                var bt = InRunUiFactory.CreateBorder(cellGo.transform, "BT", new Vector2(0,1), new Vector2(1,1), new Vector2(0,-3), Vector2.zero);
                var bb = InRunUiFactory.CreateBorder(cellGo.transform, "BB", Vector2.zero, new Vector2(1,0), Vector2.zero, new Vector2(0,3));
                var bl = InRunUiFactory.CreateBorder(cellGo.transform, "BL", Vector2.zero, new Vector2(0,1), Vector2.zero, new Vector2(3,0));
                var br = InRunUiFactory.CreateBorder(cellGo.transform, "BR", new Vector2(1,0), Vector2.one, new Vector2(-3,0), Vector2.zero);

                // Number cell
                var numGo = new GameObject($"N_{row}_{col}", typeof(RectTransform));
                numGo.transform.SetParent(_gridNumberRoot, false);
                var fs = size <= 6 ? 30 : size <= 8 ? 24 : 20;
                var valText = InRunUiFactory.CreateText(numGo.transform, "Val", "", fs, TextAnchor.MiddleCenter, Color.white);
                InRunUiFactory.StretchFill(valText.rectTransform);
                valText.raycastTarget = false;
                var pencilFs = size <= 6 ? 16 : 14;
                var pencilText = InRunUiFactory.CreateText(numGo.transform, "Pencil", "", pencilFs, TextAnchor.UpperLeft, GamePalette.PencilMarkColor);
                pencilText.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
                pencilText.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
                pencilText.rectTransform.offsetMin = Vector2.zero;
                pencilText.rectTransform.offsetMax = Vector2.zero;
                pencilText.supportRichText = true;
                pencilText.raycastTarget = false;

                // Pressure mechanic badge — top-right corner count-down number
                var badgeFs = size <= 6 ? 11 : 9;
                var badgeText = InRunUiFactory.CreateText(numGo.transform, "Badge", "",
                    badgeFs, TextAnchor.UpperRight, Color.white);
                badgeText.rectTransform.anchorMin = new Vector2(0.50f, 0.52f);
                badgeText.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);
                badgeText.rectTransform.offsetMin = Vector2.zero;
                badgeText.rectTransform.offsetMax = Vector2.zero;
                badgeText.fontStyle    = FontStyle.Bold;
                badgeText.raycastTarget = false;

                _cells.Add(new CellView { Row = row, Col = col, Image = img, Label = valText,
                    PencilLabel = pencilText, BadgeLabel = badgeText,
                    BorderTop = bt, BorderBottom = bb, BorderLeft = bl, BorderRight = br });
            }
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
            else InRunUiFactory.ClearChildren(_gridNumberRoot);

            InRunUiFactory.CopyRectTransform(_gridRoot, _gridNumberRoot);
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
            InRunUiFactory.CopyRectTransform(_gridRoot, _gridOverlayRoot);
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            _gridOverlayRoot.SetSiblingIndex(_gridRoot.GetSiblingIndex() + 1);
            if (_gridNumberRoot != null)
                _gridNumberRoot.SetSiblingIndex(_gridOverlayRoot.GetSiblingIndex() + 1);
        }

        // ────────────────────── Board rendering helpers ──────────────────────

        private static Color ComputeCellColor(SudokuBoard board, int r, int c, bool given)
        {
            if (board.RegionMap != null)
            {
                var alt = (board.RegionMap[r, c] & 1) == 0;
                return given
                    ? (alt ? GamePalette.CellJigsawGivenAlt  : GamePalette.CellJigsawGivenMain)
                    : (alt ? GamePalette.CellJigsawEmptyAlt  : GamePalette.CellJigsawEmptyMain);
            }
            return given ? GivenCellColor : EmptyCellColor;
        }

        private void UpdateAllBorders(SudokuBoard board)
        {
            for (var i = 0; i < _cells.Count; i++)
                UpdateBorders(board, _cells[i]);
        }

        private void UpdateBorders(SudokuBoard board, CellView cell)
        {
            var map = board.RegionMap;
            if (map == null) return;
            var r = cell.Row; var c = cell.Col; var sz = board.Size;
            var region = map[r, c];
            var clear = new Color(RegionBorder.r, RegionBorder.g, RegionBorder.b, 0);

            cell.BorderTop.color    = (r == 0        || map[r - 1, c] != region) ? RegionBorder : clear;
            cell.BorderBottom.color = (r == sz - 1   || map[r + 1, c] != region) ? RegionBorder : clear;
            cell.BorderLeft.color   = (c == 0        || map[r, c - 1] != region) ? RegionBorder : clear;
            cell.BorderRight.color  = (c == sz - 1   || map[r, c + 1] != region) ? RegionBorder : clear;

            // Cage borders are drawn in BuildOverlays as inset overlay strips—do not tint cell borders here.
        }

        private static readonly StringBuilder _pencilSb = new StringBuilder(32);

        private static string BuildPencilText(SudokuBoard board, int r, int c)
        {
            var marks = board.GetPencilMarks(r, c);
            if (marks == null || marks.Count == 0) return "";
            _pencilSb.Clear();
            for (var v = 1; v <= board.Size; v++)
                if (marks.Contains(v)) _pencilSb.Append(v).Append(' ');
            if (_pencilSb.Length > 0) _pencilSb.Length--; // trim trailing space
            return _pencilSb.ToString();
        }

        private static uint ComputeBoardChecksum(SudokuBoard board)
        {
            var size = board.Size;
            var h = (uint)size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                h = h * 2654435761u ^ (uint)(board.Cells[r, c] * (r * 17 + c * 31 + 1));
            return h;
        }

        private void BuildConflictCache(SudokuBoard board, ModifierOverlayData overlay)
        {
            _conflictCellsCache.Clear();
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (board.GivenMask[r, c] || board.Cells[r, c] == 0) continue;
                if (HasConflict(board, r, c))
                    _conflictCellsCache.Add((r, c));
            }
        }

        private bool HasConflict(SudokuBoard board, int r, int c)
        {
            var val = board.Cells[r, c];
            if (val == 0) return false;
            // When the constraint engine has active rules, delegate entirely to it so that
            // modifier violations (Antiknight, Nonconsecutive, Kropki, etc.) are also shown in red.
            var engine  = _map?.Run?.ConstraintEngine;
            var overlay = _map?.Run?.CurrentOverlay;
            if (engine != null && engine.RuleCount > 0)
                return engine.HasConstraintConflict(board, r, c, overlay);
            // Fallback for tutorial runs (no engine) — standard row/col/region check only.
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

        private bool IsCageEdge(int r, int c, int sz, int side)
            => _cageBorderEdges.Contains((long)r * sz * 4 + c * 4 + side);

        // ────────────────────── Modifier overlay rendering ──────────────────────

        private void BuildOverlays(AccessibilityService accessibility)
        {
            ClearOverlays();
            var overlay = _map?.Run?.CurrentOverlay;
            if (overlay == null || _cells.Count == 0) return;
            EnsureOverlayRoot();
            InRunUiFactory.CopyRectTransform(_gridRoot, _gridOverlayRoot);

            var grid = _gridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null) return;
            var cW = grid.cellSize.x; var cH = grid.cellSize.y;
            var sX = grid.spacing.x; var sY = grid.spacing.y;
            var tW = cW * _boardSize + sX * (_boardSize - 1);
            var tH = cH * _boardSize + sY * (_boardSize - 1);

            // Lines
            // [REQ: ACCESS-OVERLAY-001] Overlay rendering applies accessibility multipliers: GetLineWidthMultiplier ×1.5 (wide lines), GetOverlayAlpha ×1.4 (stronger tints)
            var lineWidthMult = accessibility.GetLineWidthMultiplier();
            for (var li = 0; li < overlay.Lines.Count; li++)
            {
                var line = overlay.Lines[li];
                var color = GetLineColor(line.Type);
                var spineAlpha = new Color(color.r, color.g, color.b,
                    accessibility.GetOverlayAlpha(color.a * 0.50f));
                var spineW = 3f * lineWidthMult;

                for (var ci = 0; ci < line.Cells.Count - 1; ci++)
                    DrawOverlayLine(line.Cells[ci], line.Cells[ci + 1], spineAlpha, spineW, cW, cH, sX, sY, tW, tH);

                var ringOuter = Mathf.Min(cW, cH) * 0.44f;
                for (var ci = 0; ci < line.Cells.Count; ci++)
                    DrawRingDot(line.Cells[ci], color, ringOuter, cW, cH, sX, sY, tW, tH);

                if (line.Type == LineType.Thermo && line.Cells.Count > 0)
                    DrawDot(line.Cells[0], color, ringOuter + 4f, cW, cH, sX, sY, tW, tH);

                if (line.Type == LineType.SlowThermo && line.Cells.Count > 0)
                    DrawRingDot(line.Cells[0], color, ringOuter + 5f, cW, cH, sX, sY, tW, tH);

                if (line.Type == LineType.BetweenLine && line.Cells.Count >= 2)
                {
                    var last = line.Cells[line.Cells.Count - 1];
                    DrawRingDot(line.Cells[0], color, ringOuter + 4f, cW, cH, sX, sY, tW, tH);
                    DrawRingDot(last, color, ringOuter + 4f, cW, cH, sX, sY, tW, tH);
                }

                if (line.Type == LineType.UniqueSetLine && line.Cells.Count >= 2)
                {
                    DrawRingDot(line.Cells[0], color, ringOuter + 3f, cW, cH, sX, sY, tW, tH);
                    DrawRingDot(line.Cells[line.Cells.Count - 1], color, ringOuter + 3f, cW, cH, sX, sY, tW, tH);
                }

                if (accessibility != null && accessibility.UseAltSymbols && line.Cells.Count > 0)
                {
                    var fc = line.Cells[0];
                    var abbrX = fc.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
                    var abbrY = -(fc.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
                    DrawOverlayText(abbrX, abbrY, BossService.GetLineTypeAbbreviation(line.Type), 8, Color.white);
                }
            }

            // Kropki dots
            for (var di = 0; di < overlay.KropkiDots.Count; di++)
            {
                var dot = overlay.KropkiDots[di];
                var mr = (dot.CellA.Row + dot.CellB.Row) / 2f;
                var mc = (dot.CellA.Col + dot.CellB.Col) / 2f;
                var px = mc * (cW + sX) + cW * 0.5f - tW * 0.5f;
                var py = -(mr * (cH + sY) + cH * 0.5f - tH * 0.5f);
                if (dot.SumValue > 0)
                {
                    DrawCircle(px, py, 12f, GamePalette.OverlayKropkiSum);
                    DrawOverlayText(px, py, dot.SumValue.ToString(), 9, Color.white);
                }
                else if (dot.IsBlack)
                {
                    DrawCircle(px, py, 10f, GamePalette.OverlayKropkiBlack);
                    if (accessibility != null && accessibility.UseAltSymbols)
                        DrawOverlayText(px, py, "KR", 7, Color.white);
                }
                else
                {
                    DrawRingAtPos(px, py, 10f, GamePalette.OverlayKropkiWhite);
                    if (accessibility != null && accessibility.UseAltSymbols)
                        DrawOverlayText(px, py, "KC", 7, Color.white);
                }
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
                    int labelSize;
                    switch (pair.Type)
                    {
                        case PairConstraintType.GreaterThan:
                            label = ">"; labelColor = GamePalette.OverlayGreaterLess; labelSize = 22; break;
                        case PairConstraintType.LessThan:
                            label = "<"; labelColor = GamePalette.OverlayGreaterLess; labelSize = 22; break;
                        case PairConstraintType.SumX:
                            label = "X"; labelColor = GamePalette.OverlayXVPair; labelSize = 11; break;
                        case PairConstraintType.SumV:
                            label = "V"; labelColor = GamePalette.OverlayXVPair; labelSize = 11; break;
                        case PairConstraintType.SumK:
                            label = pair.Value.ToString(); labelColor = GamePalette.OverlayXVPair; labelSize = 11; break;
                        default:
                            label = "?"; labelColor = Color.white; labelSize = 11; break;
                    }
                    DrawOverlayText(px, py, label, labelSize, labelColor);
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

            // Killer cage borders — drawn as inset strips so they are not
            // occluded by the 3px thick box-border Images on cell views.
            // Inset of 4px clears the 3px cell border on all sides.
            if (_cageBorderEdges.Count > 0)
            {
                const float inset = 4f;
                const float thick = 2f;
                for (var r = 0; r < _boardSize; r++)
                for (var c = 0; c < _boardSize; c++)
                {
                    var cellLeft   =  c * (cW + sX) - tW * 0.5f;
                    var cellTop    =  tH * 0.5f - r * (cH + sY);
                    var cellBottom =  cellTop  - cH;
                    var cellRight  =  cellLeft + cW;
                    if (IsCageEdge(r, c, _boardSize, 0)) // TOP
                        DrawDashedCageHorizontal(cellLeft + inset, cellTop - inset - thick, cW - inset * 2f, thick);
                    if (IsCageEdge(r, c, _boardSize, 1)) // BOTTOM
                        DrawDashedCageHorizontal(cellLeft + inset, cellBottom + inset, cW - inset * 2f, thick);
                    if (IsCageEdge(r, c, _boardSize, 2)) // LEFT
                        DrawDashedCageVertical(cellLeft + inset, cellBottom + inset, thick, cH - inset * 2f);
                    if (IsCageEdge(r, c, _boardSize, 3)) // RIGHT
                        DrawDashedCageVertical(cellRight - inset - thick, cellBottom + inset, thick, cH - inset * 2f);
                }
            }

            // Arrows
            for (var ai = 0; ai < overlay.Arrows.Count; ai++)
            {
                var arrow = overlay.Arrows[ai];
                var arrowColor = GamePalette.OverlayArrow;
                DrawRingDot(arrow.CircleCell, arrowColor, 18f, cW, cH, sX, sY, tW, tH);
                var pathColor = GamePalette.OverlayArrowPath;
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
                    sq.GetComponent<Image>().color = GamePalette.OverlayEvenMarker;
                    sq.GetComponent<Image>().raycastTarget = false;
                    _overlayObjects.Add(sq);
                    if (accessibility != null && accessibility.UseAltSymbols)
                        DrawOverlayText(px, py, "EV", 8, Color.white);
                }
                else if (m.Type == MarkerType.Odd)
                {
                    DrawCircle(px, py, cW * 0.35f, GamePalette.OverlayOddMarker);
                    if (accessibility != null && accessibility.UseAltSymbols)
                        DrawOverlayText(px, py, "OD", 8, Color.white);
                }
                else if (m.Type == MarkerType.Prime)
                {
                    DrawCircle(px, py, cW * 0.36f, GamePalette.OverlayPrimeCircle);
                    DrawOverlayText(px, py, "P", 10, GamePalette.OverlayPrimeLabel);
                }
                else if (m.Type == MarkerType.Fortress)
                {
                    var sq = new GameObject("Fortress", typeof(RectTransform), typeof(Image));
                    sq.transform.SetParent(_gridOverlayRoot, false);
                    var sr = sq.GetComponent<RectTransform>();
                    sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
                    sr.pivot = new Vector2(0.5f, 0.5f);
                    sr.anchoredPosition = new Vector2(px, py);
                    sr.sizeDelta = new Vector2(cW * 0.88f, cH * 0.88f);
                    sq.GetComponent<Image>().color = GamePalette.OverlayFortress;
                    sq.GetComponent<Image>().raycastTarget = false;
                    _overlayObjects.Add(sq);
                    if (accessibility != null && accessibility.UseAltSymbols)
                        DrawOverlayText(px, py, "FC", 8, Color.white);
                }
            }
        }

        private static Color GetLineColor(LineType t) => t switch
        {
            LineType.GermanWhispers  => GamePalette.LineGermanWhispers,
            LineType.DutchWhispers   => GamePalette.LineDutchWhispers,
            LineType.ParityLine      => GamePalette.LineParityLine,
            LineType.RenbanLine      => GamePalette.LineRenbanLine,
            LineType.Palindrome      => GamePalette.LinePalindrome,
            LineType.Thermo          => GamePalette.LineThermo,
            LineType.BetweenLine     => GamePalette.LineBetweenLine,
            LineType.ConsecutiveLine => GamePalette.LineConsecutiveLine,
            LineType.SlowThermo      => GamePalette.LineSlowThermo,
            LineType.UniqueSetLine   => GamePalette.LineUniqueSetLine,
            _                        => GamePalette.LineGermanWhispers
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
            // Size the box generously so large font sizes are never clipped
            var box = Mathf.Max(30f, fontSize * 1.8f);
            rt.sizeDelta = new Vector2(box, box);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = InRunUiFactory.GetFont();
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
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

        private void DrawCageStrip(float left, float bottom, float w, float h)
        {
            var go = new GameObject("CageEdge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(left, bottom);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.color = KillerBorder;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        private void DrawDashedCageHorizontal(float left, float bottom, float width, float thickness)
        {
            const float dash = 8f;
            const float gap = 5f;
            for (var x = 0f; x < width; x += dash + gap)
                DrawCageStrip(left + x, bottom, Mathf.Min(dash, width - x), thickness);
        }

        private void DrawDashedCageVertical(float left, float bottom, float thickness, float height)
        {
            const float dash = 8f;
            const float gap = 5f;
            for (var y = 0f; y < height; y += dash + gap)
                DrawCageStrip(left, bottom + y, thickness, Mathf.Min(dash, height - y));
        }

        private void DrawCageSum(float x, float y, int sum, float cW, float cH)
        {
            var bg = new GameObject("CageSumBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_gridOverlayRoot, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0, 1);
            bgRt.anchoredPosition = new Vector2(x, y);
            bgRt.sizeDelta = new Vector2(cW * 0.50f, cH * 0.30f);
            bg.GetComponent<Image>().color = GamePalette.OverlayCageFogBg;
            bg.GetComponent<Image>().raycastTarget = false;

            var txt = InRunUiFactory.CreateText(bg.transform, "Sum", sum.ToString(), 12, TextAnchor.MiddleCenter, KillerBorder);
            txt.fontStyle = FontStyle.Bold;
            InRunUiFactory.StretchFill(txt.rectTransform);
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

        private void ClearOverlays()
        {
            for (var i = _overlayObjects.Count - 1; i >= 0; i--)
                if (_overlayObjects[i] != null) Destroy(_overlayObjects[i]);
            _overlayObjects.Clear();
        }

        // ────────────────────── Pressure mechanic animations ──────────────────────

        /// <summary>
        /// Briefly overlays the given cells with a coloured flash that fades out over <paramref name="duration"/> seconds.
        /// Safe to call with a null/empty set.
        /// </summary>
        // [REQ: PRESSURE-UI-004] expiry red-flash  [PRESSURE-UI-005] chain gold-flash  [PRESSURE-UI-008] auto-fill green-flash
        public void FlashCells(HashSet<(int row, int col)> cells, Color flashColor, float duration)
        {
            if (cells == null || cells.Count == 0 || _gridRoot == null) return;
            StartCoroutine(FlashCellsCoroutine(cells, flashColor, duration));
        }

        // [REQ: PRESSURE-UI-004] badge shake on threat expiry — call site guards reduce-motion
        public void ShakeBadgeCells(HashSet<(int row, int col)> cells)
        {
            if (cells == null || cells.Count == 0) return;
            foreach (var cell in _cells)
            {
                if (cell.BadgeLabel == null) continue;
                if (!cells.Contains((cell.Row, cell.Col))) continue;
                StartCoroutine(ShakeBadgeCoroutine(cell.BadgeLabel.rectTransform));
            }
        }

        private System.Collections.IEnumerator ShakeBadgeCoroutine(RectTransform rt)
        {
            const float duration  = 0.30f;
            const float amplitude = 3f;
            const float cycles    = 3f;
            float elapsed = 0f;
            Vector2 origin = rt.anchoredPosition;
            while (elapsed < duration)
            {
                float t      = elapsed / duration;
                float offset = Mathf.Sin(t * cycles * Mathf.PI * 2f) * amplitude * (1f - t);
                rt.anchoredPosition = origin + new Vector2(offset, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt.anchoredPosition = origin;
        }

        private System.Collections.IEnumerator FlashCellsCoroutine(
            HashSet<(int row, int col)> cells, Color flashColor, float duration)
        {
            EnsureOverlayRoot();
            var grid = _gridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null) yield break;

            var cW = grid.cellSize.x; var cH = grid.cellSize.y;
            var sX = grid.spacing.x;  var sY = grid.spacing.y;

            // Create a flash image for each targeted cell
            var flashImages = new List<Image>(cells.Count);
            foreach (var (row, col) in cells)
            {
                var go = new GameObject("PressureFlash", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_gridOverlayRoot, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot     = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(col * (cW + sX), -(row * (cH + sY)));
                rt.sizeDelta        = new Vector2(cW, cH);
                var img = go.GetComponent<Image>();
                img.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0.82f);
                img.raycastTarget = false;
                flashImages.Add(img);
            }

            // Fade out over duration
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var alpha = Mathf.Lerp(0.82f, 0f, elapsed / duration);
                for (var i = 0; i < flashImages.Count; i++)
                    if (flashImages[i] != null)
                        flashImages[i].color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }

            for (var i = 0; i < flashImages.Count; i++)
                if (flashImages[i] != null) Destroy(flashImages[i].gameObject);
        }

        /// <summary>
        /// Plays a single expanding-ring ripple animation centred on the board, used when a wave threat spawns.
        /// </summary>
        // [REQ: PRESSURE-UI-009] wave ripple animation
        public void TriggerWaveRipple()
        {
            if (_gridRoot == null) return;
            StartCoroutine(WaveRippleCoroutine());
        }

        /// <summary>
        /// Flashes every filled cell gold then triggers the wave ripple — called on puzzle solved.
        /// </summary>
        public void FlashBoardSolved()
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) return;
            var cells = new HashSet<(int row, int col)>();
            for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.Cells[r, c] > 0)
                        cells.Add((r, c));
            // Gold flash across all filled cells
            FlashCells(cells, new Color(0.98f, 0.83f, 0.26f, 1f), 0.55f);
            TriggerWaveRipple();
        }

        /// <summary>
        /// Briefly cycles random digits in every cell before restoring the solved board —
        /// called once after the final boss is defeated.
        /// </summary>
        public void TriggerDigitRain()
        {
            if (_map?.Run?.CurrentBoard == null) return;
            StartCoroutine(DigitRainCoroutine());
        }

        private System.Collections.IEnumerator DigitRainCoroutine()
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null) yield break;
            var rng    = new System.Random();
            var size   = board.Size;
            // Phase 1: scramble for 1.2 s at ~16 fps of changes
            const float scrambleDuration = 1.20f;
            const float stepInterval     = 0.06f;
            var elapsed = 0f;
            while (elapsed < scrambleDuration)
            {
                // Set a random digit on every cell label
                for (var r = 0; r < size; r++)
                    for (var c = 0; c < size; c++)
                    {
                        var cell = _cells?.Find(v => v.Row == r && v.Col == c);
                        if (cell == null) continue;
                        cell.Label.text = ((rng.Next() % size) + 1).ToString();
                        var fade = elapsed / scrambleDuration;
                        cell.Label.color = new Color(0.98f, 0.83f, 0.26f,
                            Mathf.Lerp(0.85f, 0.30f, fade));
                    }
                elapsed += stepInterval;
                yield return new WaitForSeconds(stepInterval);
            }
            // Phase 2: settle — restore each cell one by one in reading order with a tiny pop
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    var cell = _cells?.Find(v => v.Row == r && v.Col == c);
                    if (cell == null) continue;
                    var val = board.Cells[r, c];
                    cell.Label.text  = val > 0 ? val.ToString() : "";
                    cell.Label.color = board.GivenMask[r, c] ? GamePalette.CellNumberGiven : Color.white;
                    StartCoroutine(AnimationHelper.PulseScale(cell.Label.transform,
                        1f, 1.25f, 0.18f));
                }
                yield return new WaitForSeconds(0.025f); // row-by-row settle
            }
        }

        private System.Collections.IEnumerator WaveRippleCoroutine()
        {
            EnsureOverlayRoot();
            var go = new GameObject("WaveRipple", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var rippleColor = new Color(0.40f, 0.60f, 1.00f, 0.70f); // blue-white wave tint
            var img = go.GetComponent<Image>();
            img.color = rippleColor;
            img.raycastTarget = false;

            var boardPx = _gridRoot.rect.width;
            const float Duration = 0.55f;
            var elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / Duration;
                rt.sizeDelta = new Vector2(boardPx * t, boardPx * t);
                img.color = new Color(rippleColor.r, rippleColor.g, rippleColor.b,
                    Mathf.Lerp(0.70f, 0f, t));
                yield return null;
            }
            Destroy(go);
        }

        /// <summary>
        /// Draws dynamic pressure-threat overlays (region border strip, chain line) for all active threats.
        /// Called at the end of each <see cref="RenderBoard"/> pass; destroys and recreates overlay objects.
        /// </summary>
        // [REQ: PRESSURE-UI-010] region border strip  [REQ: PRESSURE-UI-011] chain animated line
        private void RenderPressureThreatOverlays(PressureRenderData pr)
        {
            // Destroy previous dynamic pressure overlays
            for (var i = _pressureOverlayObjects.Count - 1; i >= 0; i--)
                if (_pressureOverlayObjects[i] != null) Destroy(_pressureOverlayObjects[i]);
            _pressureOverlayObjects.Clear();

            if (pr.ActiveThreats == null || pr.ActiveThreats.Count == 0 || _gridRoot == null) return;
            EnsureOverlayRoot();

            var grid = _gridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null) return;
            var cW = grid.cellSize.x; var cH = grid.cellSize.y;
            var sX = grid.spacing.x;  var sY = grid.spacing.y;
            var tW = cW * _boardSize + sX * (_boardSize - 1);
            var tH = cH * _boardSize + sY * (_boardSize - 1);

            // Badge colour ramp reused as threat colour (red at low ratio)
            foreach (var threat in pr.ActiveThreats)
            {
                if (threat.Cells == null || threat.Cells.Count == 0) continue;

                var ratio = threat.InitialPlacements > 0
                    ? threat.RemainingPlacements / (float)threat.InitialPlacements : 0f;
                var threatColor = ratio > 0.5f
                    ? new Color(0.60f, 0.60f, 1.00f, 0.55f)  // blue-grey
                    : ratio > 0.25f
                        ? new Color(1.00f, 0.60f, 0.10f, 0.55f)  // amber
                        : new Color(0.95f, 0.15f, 0.15f, 0.55f); // red

                // [REQ: PRESSURE-UI-011] Chain scope: draw animated connecting lines between cells
                if (threat.Scope == ThreatScope.Chain || threat.IsChain)
                {
                    var lineAlpha = (Mathf.Sin(Time.time * 3f) + 1f) * 0.15f + 0.40f; // 0.40–0.70 pulse
                    var lineColor = new Color(threatColor.r, threatColor.g, threatColor.b, lineAlpha);
                    for (var ci = 0; ci < threat.Cells.Count - 1; ci++)
                        DrawPressureLine(threat.Cells[ci], threat.Cells[ci + 1], lineColor, 4f,
                            cW, cH, sX, sY, tW, tH);
                    continue; // chain scope — no border strip
                }

                // [REQ: PRESSURE-UI-010] Row/Col/Box scope: draw a coloured strip on the region boundary
                if (threat.Scope == ThreatScope.Row || threat.Scope == ThreatScope.Column)
                {
                    // Infer the row or column index from the first cell
                    var first = threat.Cells[0];
                    if (threat.Scope == ThreatScope.Row)
                    {
                        // Vertical strip on the left edge of the row
                        var py = -(first.row * (cH + sY));
                        DrawPressureStrip(-tW * 0.5f - 5f, py - tH * 0.5f, 4f, cH, threatColor);
                    }
                    else
                    {
                        // Horizontal strip on the top edge of the column
                        var px = first.col * (cW + sX) - tW * 0.5f;
                        DrawPressureStrip(px, tH * 0.5f + 5f, cW, 4f, threatColor);
                    }
                }
                else if (threat.Scope == ThreatScope.Box)
                {
                    // Draw thin strips on all four edges of the bounding box of threat cells
                    var minR = int.MaxValue; var maxR = int.MinValue;
                    var minC = int.MaxValue; var maxC = int.MinValue;
                    foreach (var cell in threat.Cells)
                    {
                        if (cell.row < minR) minR = cell.row;
                        if (cell.row > maxR) maxR = cell.row;
                        if (cell.col < minC) minC = cell.col;
                        if (cell.col > maxC) maxC = cell.col;
                    }
                    var left   = minC * (cW + sX) - tW * 0.5f - 3f;
                    var right  = maxC * (cW + sX) + cW - tW * 0.5f + 3f;
                    var top    =  tH * 0.5f - minR * (cH + sY) + 3f;
                    var bottom =  tH * 0.5f - (maxR * (cH + sY) + cH) - 3f;
                    var boxW   = right - left;
                    var boxH   = top - bottom;
                    // Top strip
                    DrawPressureStripAbs(left, top, boxW, 4f, threatColor);
                    // Bottom strip
                    DrawPressureStripAbs(left, bottom - 4f, boxW, 4f, threatColor);
                    // Left strip
                    DrawPressureStripAbs(left - 4f, bottom, 4f, boxH, threatColor);
                    // Right strip
                    DrawPressureStripAbs(right, bottom, 4f, boxH, threatColor);
                }
            }
        }

        private void DrawPressureLine((int row, int col) a, (int row, int col) b, Color color, float width,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var ax = a.col * (cW + sX) + cW * 0.5f - tW * 0.5f;
            var ay = -(a.row * (cH + sY) + cH * 0.5f - tH * 0.5f);
            var bx = b.col * (cW + sX) + cW * 0.5f - tW * 0.5f;
            var by = -(b.row * (cH + sY) + cH * 0.5f - tH * 0.5f);
            var dx = bx - ax; var dy = by - ay;
            var len = Mathf.Sqrt(dx * dx + dy * dy);

            var go = new GameObject("PressureLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(ax, ay);
            rt.sizeDelta = new Vector2(len, width);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _pressureOverlayObjects.Add(go);
        }

        // Draws a strip using overlay-root-relative coordinates (anchored at centre = (0,0))
        private void DrawPressureStrip(float x, float y, float w, float h, Color color)
        {
            var go = new GameObject("PressureStrip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _pressureOverlayObjects.Add(go);
        }

        // Same as above but pivot = bottom-left with absolute coordinates relative to overlay centre
        private void DrawPressureStripAbs(float left, float bottom, float w, float h, Color color)
        {
            var go = new GameObject("PressureStrip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_gridOverlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(left, bottom);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _pressureOverlayObjects.Add(go);
        }

        // ────────────────────── Inner types ──────────────────────

        private sealed class CellView
        {
            public int Row;
            public int Col;
            public Image Image;
            public Text Label;
            public Text PencilLabel;
            /// <summary>Top-right corner badge for pressure mechanic countdowns. Null = no pressure UI built.</summary>
            public Text BadgeLabel;
            public Image BorderTop;
            public Image BorderBottom;
            public Image BorderLeft;
            public Image BorderRight;

            // Cached last-assigned text values — Unity triggers a full mesh rebuild even when
            // the same string is re-assigned, so we skip the assignment when nothing changed.
            public string LastLabelText   = "\0"; // sentinel so first real assignment always fires
            public string LastPencilText  = "\0";
            public string LastBadgeText   = "\0";
        }
    }
}
