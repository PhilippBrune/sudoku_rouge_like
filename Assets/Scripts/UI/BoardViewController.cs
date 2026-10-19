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
    public sealed partial class BoardViewController : MonoBehaviour
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

        // ── Constraint conflict preview cache ──
        // Invalidated whenever the selected cell or highlight value changes.
        private HashSet<(int, int)> _conflictPreviewCells = new HashSet<(int, int)>();
        private int _prevConflictRow = -2, _prevConflictCol = -2, _prevConflictValue = -1;

        // ── Forbidden-empty-cell cache (digit-based structural constraint preview) ──
        // Shows empty cells where the highlighted digit cannot be placed.
        private readonly HashSet<(int, int)> _forbiddenEmptyCells = new HashSet<(int, int)>();
        private int _prevForbiddenValue = -1;
        private uint _prevForbiddenChecksum = uint.MaxValue;

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

        // Cells that carry a cage sum in their top-left corner; their pencil marks sit at the bottom instead.
        private readonly HashSet<(int row, int col)> _cageLabelCells = new HashSet<(int row, int col)>();

        public void Configure(RunMapController map, RectTransform gridRoot)
        {
            _map = map;
            _gridRoot = gridRoot;
            _cells = new List<CellView>();
            _overlayObjects = new List<GameObject>();
            _pressureOverlayObjects = new List<GameObject>();
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

        private void RebuildForbiddenEmptyCells(SudokuBoard board, int value)
        {
            _forbiddenEmptyCells.Clear();
            if (value <= 0) return;
            var cfg = _map?.Run?.CurrentLevelConfig;
            if (cfg == null) return;
            var mods = cfg.ActiveModifiers;
            var antiknight   = mods.Contains(BossModifierId.Antiknight)   || mods.Contains(BossModifierId.KnightKingCombined);
            var antiking     = mods.Contains(BossModifierId.Antiking)     || mods.Contains(BossModifierId.KnightKingCombined);
            var antibishop   = mods.Contains(BossModifierId.AntiBishop);
            var nonconsec    = mods.Contains(BossModifierId.Nonconsecutive);
            var nonconsecDiag = mods.Contains(BossModifierId.NonconsecDiagonal);
            var distGe2      = mods.Contains(BossModifierId.DistanceGe2);
            if (!antiknight && !antiking && !antibishop && !nonconsec && !nonconsecDiag && !distGe2) return;
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (board.Cells[r, c] != 0) continue;
                if (IsForbiddenByStructural(board, r, c, value, size,
                        antiknight, antiking, antibishop, nonconsec, nonconsecDiag, distGe2))
                    _forbiddenEmptyCells.Add((r, c));
            }
        }

        private static bool IsForbiddenByStructural(SudokuBoard board, int row, int col, int value, int size,
            bool antiknight, bool antiking, bool antibishop, bool nonconsec, bool nonconsecDiag, bool distGe2)
        {
            if (antiknight)
            {
                int[] dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
                int[] dc = { -1,  1, -2,  2, -2, 2, -1, 1 };
                for (var i = 0; i < 8; i++)
                {
                    var nr = row + dr[i]; var nc = col + dc[i];
                    if (nr >= 0 && nr < size && nc >= 0 && nc < size && board.Cells[nr, nc] == value) return true;
                }
            }
            if (antiking || distGe2)
            {
                for (var dr = -1; dr <= 1; dr++)
                for (var dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    var nr = row + dr; var nc = col + dc;
                    if (nr >= 0 && nr < size && nc >= 0 && nc < size && board.Cells[nr, nc] == value) return true;
                }
            }
            if (antibishop)
            {
                int[] drs = { -1, -1,  1, 1 };
                int[] dcs = { -1,  1, -1, 1 };
                for (var d = 0; d < 4; d++)
                for (var step = 1; step <= SudokuRoguelike.Sudoku.AntiBishopRule.DiagonalRange; step++)
                {
                    var nr = row + drs[d] * step; var nc = col + dcs[d] * step;
                    if (nr < 0 || nr >= size || nc < 0 || nc >= size) break;
                    if (board.Cells[nr, nc] == value) return true;
                }
            }
            if (nonconsec)
            {
                int[] dr = { -1, 1,  0, 0 };
                int[] dc = {  0, 0, -1, 1 };
                for (var i = 0; i < 4; i++)
                {
                    var nr = row + dr[i]; var nc = col + dc[i];
                    if (nr >= 0 && nr < size && nc >= 0 && nc < size)
                    {
                        var v = board.Cells[nr, nc];
                        if (v != 0 && Math.Abs(v - value) == 1) return true;
                    }
                }
            }
            if (nonconsecDiag)
            {
                int[] dr = { -1, -1, 1,  1 };
                int[] dc = { -1,  1, -1, 1 };
                for (var i = 0; i < 4; i++)
                {
                    var nr = row + dr[i]; var nc = col + dc[i];
                    if (nr >= 0 && nr < size && nc >= 0 && nc < size)
                    {
                        var v = board.Cells[nr, nc];
                        if (v != 0 && Math.Abs(v - value) == 1) return true;
                    }
                }
            }
            return false;
        }

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

            // [REQ: UI-STYLE-010] A board with outside clues is drawn smaller, so the clue ring fits round it.
            var clueRing = HasClueRing(run.CurrentOverlay);
            if (_boardSize != board.Size || _cells.Count == 0 || clueRing != _gridHasClueRing)
            {
                BuildGrid(board.Size, clueRing);
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

            SyncRegionBorders(board);

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

            // Rebuild forbidden empty cells when digit or board state changes.
            if (highlightValue != _prevForbiddenValue || boardChecksum != _prevForbiddenChecksum)
            {
                RebuildForbiddenEmptyCells(board, highlightValue);
                _prevForbiddenValue    = highlightValue;
                _prevForbiddenChecksum = boardChecksum;
            }

            // [REQ: DEBUFF-DEF-011] CellBlur (boss debuff): cells whose labels are hidden until the timer runs out.
            var digitBlur = _map?.Run?.CurrentLevelState?.CellBlurCells;
            if (digitBlur != null && digitBlur.Count == 0) digitBlur = null;

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var val = board.Cells[cell.Row, cell.Col];
                var given = board.GivenMask[cell.Row, cell.Col];

                var labelText = val == 0 ? "" : val.ToString();
                if (labelText != cell.LastLabelText) { cell.Label.text = labelText; cell.LastLabelText = labelText; }
                cell.Label.color = given ? GamePalette.CellNumberGiven : GamePalette.CellNumberPlaced;
                var digitStyle = given ? FontStyle.Bold : FontStyle.Normal;
                if (cell.Label.fontStyle != digitStyle) cell.Label.fontStyle = digitStyle;
                var pencilBlurred = (BlurredPencilCells != null && BlurredPencilCells.Contains((cell.Row, cell.Col)))
                                 || (blurredPencilCells != null && blurredPencilCells.Contains((cell.Row, cell.Col)));
                var pencilText = (val == 0 && !pencilBlurred) ? BuildPencilText(board, cell.Row, cell.Col) : "";
                if (pencilText != cell.LastPencilText) { cell.PencilLabel.text = pencilText; cell.LastPencilText = pencilText; }
                var pencilAnchor = _cageLabelCells.Contains((cell.Row, cell.Col)) ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
                if (cell.PencilLabel.alignment != pencilAnchor) cell.PencilLabel.alignment = pencilAnchor;

                var c = _accessibility != null
                    ? _accessibility.GetCellColor(ComputeCellColor(board, cell.Row, cell.Col, given))
                    : ComputeCellColor(board, cell.Row, cell.Col, given);
                if (selectedRow == cell.Row && selectedCol == cell.Col)
                {
                    c = SelectedColor;
                    if (val != 0) cell.Label.color = GamePalette.CellNumberOnSelected; // GAP-029 contrast on amber
                }
                else if (selectedRow == cell.Row || selectedCol == cell.Col)
                    c = RowColHL;
                if (highlightValue > 0 && val == highlightValue)
                    c = MatchColor;

                // Constraint conflict preview: highlight cells that would be forbidden
                // by ANY active rule (row/col/region/Antiknight/Nonconsecutive/Kropki/etc.)
                // if highlightValue were placed at the selected cell.
                if (_conflictPreviewCells.Count > 0 && _conflictPreviewCells.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, AntiknightForbidColor, 0.70f);

                // Forbidden empty cells: empty cells where the highlighted digit cannot be placed
                // due to active structural constraints (Anti-Knight/King/Bishop, Nonconsecutive, etc.).
                // Uses the same colour as the row/column highlight — both indicate "digit can't go here".
                if (val == 0 && _forbiddenEmptyCells.Count > 0 && _forbiddenEmptyCells.Contains((cell.Row, cell.Col)))
                    c = RowColHL;

                if (bagHighlightCells != null && bagHighlightCells.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, BagHighlightColor, 0.65f);

                if (_globalConstraintHighlight.Count > 0 && _globalConstraintHighlight.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, GlobalConstraintHighlightColor, 0.55f);

                if (fogSet != null && fogSet.Contains((cell.Row, cell.Col)))
                {
                    c = FogColor;
                    if (val == 0 && cell.LastLabelText != "") { cell.Label.text = ""; cell.LastLabelText = ""; }
                    if (val != 0) cell.Label.color = GamePalette.CellNumberOnFog; // ink would vanish on the blot
                    if (cell.LastPencilText != "") { cell.PencilLabel.text = ""; cell.LastPencilText = ""; }
                }

                if (!given && _conflictCellsCache.Contains((cell.Row, cell.Col)))
                {
                    c = ConflictColor;
                    cell.Label.color = GamePalette.CellNumberGiven;
                }

                if (lockedCells != null && lockedCells.Contains((cell.Row, cell.Col))) // [REQ: DEBUFF-HOOK-012] CellLock: tint locked cells with GamePalette.CellLocked
                    c = Color.Lerp(c, GamePalette.CellLocked, 0.75f);

                // Blurred cells keep their values; only the digit and pencil labels are hidden.
                if (digitBlur != null && digitBlur.Contains((cell.Row, cell.Col)))
                {
                    c = Color.Lerp(c, FogColor, 0.35f);
                    if (cell.LastLabelText != "") { cell.Label.text = ""; cell.LastLabelText = ""; }
                    if (cell.LastPencilText != "") { cell.PencilLabel.text = ""; cell.LastPencilText = ""; }
                }

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

        private bool _gridHasClueRing;

        private void BuildGrid(int size, bool clueRing = false)
        {
            _boardSize = size;
            _gridHasClueRing = clueRing;
            _cells.Clear();
            _bordersBoard = null; // new cells: the next render draws the borders
            _bordersMap = null;
            InRunUiFactory.ClearChildren(_gridRoot);

            var grid = _gridRoot.GetComponent<GridLayoutGroup>() ?? _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = size;
            var targetBoardPx = clueRing ? 620f * ClueRingBoardScale : 620f;
            const float SpacingPx    = 2f;
            var cellPx = Mathf.Clamp(
                Mathf.Floor((targetBoardPx - SpacingPx * (size - 1)) / size),
                48f, 128f);
            grid.spacing  = new Vector2(SpacingPx, SpacingPx);
            grid.cellSize = new Vector2(cellPx, cellPx);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var totalW = grid.cellSize.x * size + grid.spacing.x * (size - 1);
            var totalH = grid.cellSize.y * size + grid.spacing.y * (size - 1);
            _gridRoot.anchorMin = new Vector2(0.5f, 0.515f); // UI-STYLE-002: centred on the hanging scroll
            _gridRoot.anchorMax = new Vector2(0.5f, 0.515f);
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
                var fs = Mathf.Clamp(Mathf.RoundToInt(cellPx * 0.50f), 18, 56);
                var valText = InRunUiFactory.CreateText(numGo.transform, "Val", "", fs, TextAnchor.MiddleCenter, Color.white);
                InRunUiFactory.StretchFill(valText.rectTransform);
                valText.raycastTarget = false;
                var pencilFs = Mathf.Clamp(Mathf.RoundToInt(cellPx * 0.19f), 11, 21);
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
                // Pressure counters keep their exact colours; an ink outline keeps the light grey one readable on paper.
                var badgeOutline = badgeText.gameObject.AddComponent<Outline>();
                badgeOutline.effectColor = new Color(0.10f, 0.08f, 0.06f, 0.85f);
                badgeOutline.effectDistance = new Vector2(1f, -1f);

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
                ScrollUiKit.SnapToPixels(go); // UI-STYLE-002: crisp digits and pencil marks
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
            ScrollUiKit.SnapToPixels(go); // UI-STYLE-002: crisp cage sums and clue labels
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

        /// <summary>Region map the gold region borders were last drawn for (read by tests).</summary>
        public int[,] RegionMapShownByBorders => _bordersMap;

        private SudokuBoard _bordersBoard;
        private int[,] _bordersMap;

        // [REQ: GEN-REGION-004] GAP-044: the borders belong to one board. They used to be drawn only when the grid was
        // rebuilt for a new size, so the next puzzle of the same size showed the previous puzzle's regions while the
        // digits and the rules used its own (seen as "two 3s in one box" on a 5×5 board).
        private void SyncRegionBorders(SudokuBoard board)
        {
            if (board == null || _cells == null || _cells.Count != board.Size * board.Size) return;
            if (ReferenceEquals(_bordersBoard, board) && ReferenceEquals(_bordersMap, board.RegionMap)) return;
            _bordersBoard = board;
            _bordersMap = board.RegionMap;
            UpdateAllBorders(board);
        }

        private void UpdateAllBorders(SudokuBoard board)
        {
            for (var i = 0; i < _cells.Count; i++)
                UpdateBorders(board, _cells[i]);
        }

        private void UpdateBorders(SudokuBoard board, CellView cell)
        {
            var map = board.RegionMap;
            var clear = new Color(RegionBorder.r, RegionBorder.g, RegionBorder.b, 0);
            if (map == null)
            {
                cell.BorderTop.color = cell.BorderBottom.color = cell.BorderLeft.color = cell.BorderRight.color = clear;
                return;
            }
            var r = cell.Row; var c = cell.Col; var sz = board.Size;
            var region = map[r, c];

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

            // Lines (each modifier draws into its own layer, see BoardViewController.OverlayGroups)
            // [REQ: ACCESS-OVERLAY-001] High contrast widens the line bands (GetLineWidthMultiplier ×1.5); the bands are
            // opaque, so GetOverlayAlpha only applies to the translucent overlays below. Alt symbols add a text cue per line.
            var lineWidthMult = accessibility.GetLineWidthMultiplier();
            // [REQ: BOSS-MOD-002] A line is a band a fifth of a cell wide through the cell centres, with round joints;
            // thermometers get a bulb (filled; open for Slow Thermo), Between lines a circle at both ends (GAP-048).
            var bandW = Mathf.Min(cW, cH) * 0.20f * lineWidthMult;
            var bulbR = Mathf.Min(cW, cH) * 0.34f;
            DrawNegationZone(overlay, cW, cH, sX, sY, tW, tH);   // under the lines that cross it
            DrawExtraHouses(overlay, cW, cH, sX, sY, tW, tH);    // B5 houses, also under the lines
            for (var li = 0; li < overlay.Lines.Count; li++)
            {
                var line = overlay.Lines[li];
                UseOverlayGroup(LineKey(line.Type));
                var color = GetLineColor(line.Type);
                // Hollow ends (Slow Thermo bulb, Between and Unique Set ends) are rings; the band stops at the ring,
                // so the cell shows through the hole whatever its colour.
                var openStart = line.Type == LineType.SlowThermo || line.Type == LineType.BetweenLine
                                || line.Type == LineType.UniqueSetLine;
                var openEnd = line.Cells.Count >= 2
                              && (line.Type == LineType.BetweenLine || line.Type == LineType.UniqueSetLine);
                var trim = bulbR * OpenRingInner;
                DrawLineBand(line.Cells, color, bandW, openStart ? trim : 0f, openEnd ? trim : 0f, cW, cH, sX, sY, tW, tH);

                if (line.Type == LineType.Thermo && line.Cells.Count > 0)
                    DrawDot(line.Cells[0], color, bulbR, cW, cH, sX, sY, tW, tH);
                if (openStart && line.Cells.Count > 0)
                    DrawOpenRing(line.Cells[0], color, bulbR, cW, cH, sX, sY, tW, tH);
                if (openEnd)
                    DrawOpenRing(line.Cells[line.Cells.Count - 1], color, bulbR, cW, cH, sX, sY, tW, tH);
                DrawCatalogLineMarks(line, color, bandW, bulbR, cW, cH, sX, sY, tW, tH);

                if (accessibility != null && accessibility.UseAltSymbols && line.Cells.Count > 0)
                {
                    var fc = line.Cells[0];
                    var abbrX = fc.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
                    var abbrY = -(fc.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
                    DrawOverlayText(abbrX, abbrY, BossService.GetLineTypeAbbreviation(line.Type), 8, GamePalette.CellNumberGiven);
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
                UseOverlayGroup(dot.SumValue > 0 ? KropkiSumKey : dot.IsBlack ? KropkiBlackKey : KropkiWhiteKey);
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
                    DrawCircle(px, py, 11f, GamePalette.OverlayKropkiWhiteRim);
                    DrawCircle(px, py, 8.5f, GamePalette.OverlayKropkiWhite);
                    if (accessibility != null && accessibility.UseAltSymbols)
                        DrawOverlayText(px, py, "KC", 7, GamePalette.OverlayKropkiWhiteRim);
                }
            }

            // Pair constraints (Greater/Less Than and XV Pairs)
            if (overlay.PairConstraints != null)
            {
                for (var pi = 0; pi < overlay.PairConstraints.Count; pi++)
                {
                    var pair = overlay.PairConstraints[pi];
                    if (pair.Type == PairConstraintType.DifferenceK)
                    {
                        DrawDifferenceKPair(pair, overlay, cW, cH, sX, sY, tW, tH);
                        continue;
                    }
                    if (pair.Type == PairConstraintType.RatioN)
                    {
                        DrawRatioNPair(pair, cW, cH, sX, sY, tW, tH);
                        continue;
                    }
                    var mr = (pair.CellA.Row + pair.CellB.Row) / 2f;
                    var mc = (pair.CellA.Col + pair.CellB.Col) / 2f;
                    var px = mc * (cW + sX) + cW * 0.5f - tW * 0.5f;
                    var py = -(mr * (cH + sY) + cH * 0.5f - tH * 0.5f);
                    string label;
                    Color labelColor;
                    int labelSize;
                    UseOverlayGroup(pair.Type == PairConstraintType.GreaterThan || pair.Type == PairConstraintType.LessThan
                        ? GreaterLessKey : XvKey);
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

            // Cages and arrows of every kind (BoardViewController.CagesArrows.cs)
            DrawCages(overlay, cW, cH, sX, sY, tW, tH);
            DrawArrows(overlay, cW, cH, sX, sY, tW, tH);

            // Outside clues and corner circles (BoardViewController.OutsideClues.cs)
            DrawOutsideClues(overlay, cW, cH, sX, sY, tW, tH);
            DrawQuadruples(overlay, cW, cH, sX, sY, tW, tH);

            // Cell markers
            for (var mi = 0; mi < overlay.CellMarkers.Count; mi++)
            {
                var m = overlay.CellMarkers[mi];
                if (m.Type == MarkerType.Multiple)
                {
                    DrawMultipleMarker(m, cW, cH, sX, sY, tW, tH);
                    continue;
                }
                if (DrawCatalogMarker(m, cW, cH, sX, sY, tW, tH)) continue;
                UseOverlayGroup(m.Type == MarkerType.Prime ? PrimeKey : m.Type == MarkerType.Fortress ? FortressKey : EvenOddKey);
                var px = m.Cell.Col * (cW + sX) + cW * 0.5f - tW * 0.5f;
                var py = -(m.Cell.Row * (cH + sY) + cH * 0.5f - tH * 0.5f);
                if (m.Type == MarkerType.Even)
                {
                    var sq = new GameObject("Even", typeof(RectTransform), typeof(Image));
                    sq.transform.SetParent(OverlayParent, false);
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
                    sq.transform.SetParent(OverlayParent, false);
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
            DrawMirrorPairs(overlay, cW, cH, sX, sY, tW, tH);
            DrawValuePairs(overlay, cW, cH, sX, sY, tW, tH);
            UseOverlayGroup(null);
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
            LineType.NonconsecLine      => GamePalette.LineNonconsecLine,
            LineType.HighLowAlternating => GamePalette.LineHighLow,
            LineType.NabnerLine         => GamePalette.LineNabner,
            LineType.EntropicLine       => GamePalette.LineEntropic,
            LineType.ModularLine        => GamePalette.LineModular,
            LineType.ZipperLine         => GamePalette.LineZipper,
            LineType.FastThermo         => GamePalette.LineThermo,
            LineType.AmbiguousThermo    => GamePalette.LineThermo,
            LineType.RegionSumLine      => GamePalette.LineRegionSum,
            LineType.NLine              => GamePalette.LineNLine,
            LineType.SumLine            => GamePalette.LineSumLine,
            LineType.WhisperGeneralized => GamePalette.LineWhisperK,
            LineType.ZoneWhisper        => GamePalette.LineGermanWhispers,
            _                        => GamePalette.LineGermanWhispers
        };

        private void DrawOverlayLine(CellCoord a, CellCoord b, Color color, float width,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            DrawOverlaySegment(CellCenter(a, cW, cH, sX, sY, tW, tH), CellCenter(b, cW, cH, sX, sY, tW, tH), color, width);
        }

        private void DrawOverlaySegment(Vector2 from, Vector2 to, Color color, float width)
        {
            var ax = from.x; var ay = from.y;
            var dx = to.x - ax; var dy = to.y - ay;
            var len = Mathf.Sqrt(dx * dx + dy * dy);

            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(OverlayParent, false);
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

        // Inner radius of the ring drawn at hollow line ends, as a share of its outer radius.
        private const float OpenRingInner = 0.66f;
        private static Sprite _openRingSprite;

        // One line as an opaque band: a segment between each pair of cells and a round joint on every cell. A trim
        // shortens the first or last segment (it then starts at a ring instead of a round end).
        private void DrawLineBand(IReadOnlyList<CellCoord> cells, Color color, float width, float trimStart, float trimEnd,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            for (var ci = 0; ci < cells.Count - 1; ci++)
            {
                var a = CellCenter(cells[ci], cW, cH, sX, sY, tW, tH);
                var b = CellCenter(cells[ci + 1], cW, cH, sX, sY, tW, tH);
                var dir = (b - a).normalized;
                if (ci == 0 && trimStart > 0f) a += dir * trimStart;
                if (ci == cells.Count - 2 && trimEnd > 0f) b -= dir * trimEnd;
                DrawOverlaySegment(a, b, color, width);
            }
            for (var ci = 0; ci < cells.Count; ci++)
            {
                if ((ci == 0 && trimStart > 0f) || (ci == cells.Count - 1 && trimEnd > 0f)) continue;
                DrawDot(cells[ci], color, width * 0.5f, cW, cH, sX, sY, tW, tH);
            }
        }

        private static Vector2 CellCenter(CellCoord c, float cW, float cH, float sX, float sY, float tW, float tH) =>
            new Vector2(c.Col * (cW + sX) + cW * 0.5f - tW * 0.5f, -(c.Row * (cH + sY) + cH * 0.5f - tH * 0.5f));

        // A thick ring in the line colour; the cell shows through its hole (Slow Thermo bulb, Between and Unique Set ends).
        private void DrawOpenRing(CellCoord c, Color color, float radius,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (_openRingSprite == null) _openRingSprite = BuildRingSprite(OpenRingInner);
            var p = CellCenter(c, cW, cH, sX, sY, tW, tH);
            var go = new GameObject("OpenRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(OverlayParent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = p;
            rt.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            var img = go.GetComponent<Image>();
            img.sprite = _openRingSprite;
            img.color = color;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
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
            go.transform.SetParent(OverlayParent, false);
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
            go.transform.SetParent(OverlayParent, false);
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
            go.transform.SetParent(OverlayParent, false);
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

        private static Sprite BuildRingSprite(float innerRatio = 0.82f)
        {
            const int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var c = sz * 0.5f - 0.5f;
            var outerR = c;
            var innerR = c * innerRatio;
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

        private void DrawCageStrip(float left, float bottom, float w, float h, Color color)
        {
            var go = new GameObject("CageEdge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(OverlayParent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(left, bottom);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _overlayObjects.Add(go);
        }

        // [REQ: UI-STYLE-006] Runs from CageOutline (board space, y down) mapped onto the overlay (centre origin, y up).
        // Each run is a centred strip extended by half its thickness, so the corners of the outline close.
        private void DrawCageOutline(KillerCage cage, Color color, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            const float inset = 5f;
            const float thick = 2f;
            const float dash = 8f;
            const float gap = 5f;
            var runs = CageOutline.Build(cage.Cells, _boardSize, cW, cH, sX, sY, inset);
            for (var i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                var from = new Vector2(Mathf.Min(run.From.x, run.To.x), Mathf.Min(run.From.y, run.To.y));
                var length = run.Length + thick;
                var dashes = CageOutline.Dashes(length, dash, gap);
                for (var d = 0; d < dashes.Count; d++)
                {
                    var (offset, dashLength) = dashes[d];
                    if (run.Horizontal)
                        DrawCageStrip(from.x - thick * 0.5f + offset - tW * 0.5f, tH * 0.5f - from.y - thick * 0.5f,
                            dashLength, thick, color);
                    else
                        DrawCageStrip(from.x - thick * 0.5f - tW * 0.5f,
                            tH * 0.5f - (from.y - thick * 0.5f + offset + dashLength), thick, dashLength, color);
                }
            }
        }

        private void DrawCageSum(float x, float y, string label, Color color, float cW, float cH)
        {
            var bg = new GameObject("CageSumBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(OverlayParent, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0, 1);
            bgRt.anchoredPosition = new Vector2(x, y);
            bgRt.sizeDelta = new Vector2(cW * 0.50f, cH * 0.30f);
            bg.GetComponent<Image>().color = GamePalette.OverlayCageFogBg;
            bg.GetComponent<Image>().raycastTarget = false;

            // The sum grows with the cell so it stays readable on 9×9 and on large screens alike.
            var fontSize = Mathf.Max(12, Mathf.RoundToInt(cH * 0.22f));
            var txt = InRunUiFactory.CreateText(bg.transform, "Sum", label, fontSize, TextAnchor.MiddleCenter, color);
            txt.fontStyle = FontStyle.Bold;
            InRunUiFactory.StretchFill(txt.rectTransform);
            txt.raycastTarget = false;
            _overlayObjects.Add(bg);
        }

        private void ClearOverlays()
        {
            for (var i = _overlayObjects.Count - 1; i >= 0; i--)
                if (_overlayObjects[i] != null) Destroy(_overlayObjects[i]);
            _overlayObjects.Clear();
            _cageLabelCells.Clear();
            ClearOverlayGroups();
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
                    cell.Label.color = board.GivenMask[r, c] ? GamePalette.CellNumberGiven : GamePalette.CellNumberPlaced;
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
