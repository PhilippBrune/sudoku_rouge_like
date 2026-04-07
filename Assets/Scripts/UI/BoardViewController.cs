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
    /// <summary>
    /// Owns the sudoku grid: cell building, board rendering, and modifier overlay rendering.
    /// </summary>
    public sealed class BoardViewController : MonoBehaviour
    {
        // ── Injected ──
        private RunMapController _map;
        private RectTransform _gridRoot;

        // ── Board cells ──
        private List<CellView> _cells;
        private int _boardSize;

        // ── Overlay roots ──
        private RectTransform _gridOverlayRoot;
        private RectTransform _gridNumberRoot;
        private List<GameObject> _overlayObjects;
        private bool _overlaysBuilt;

        // ── Cage edges ──
        private HashSet<long> _cageBorderEdges;

        // ── Sprites ──
        private Sprite _circleSprite;
        private Sprite _ringSprite;

        // ── Colors (board-specific) ──
        private static readonly Color EmptyCellColor        = new Color(0.12f, 0.18f, 0.14f, 1f);
        private static readonly Color GivenCellColor        = new Color(0.20f, 0.29f, 0.20f, 1f);
        private static readonly Color SelectedColor         = new Color(0.73f, 0.49f, 0.18f, 1f);
        private static readonly Color RowColHL              = new Color(0.18f, 0.34f, 0.56f, 1f);
        private static readonly Color MatchColor            = new Color(0.36f, 0.24f, 0.58f, 1f);
        private static readonly Color ConflictColor         = new Color(0.72f, 0.18f, 0.18f, 1f);
        private static readonly Color FogColor              = new Color(0.06f, 0.06f, 0.08f, 1f);
        private static readonly Color AntiknightForbidColor = new Color(0.55f, 0.20f, 0.60f, 0.55f);
        private static readonly Color BagHighlightColor     = new Color(0.30f, 0.65f, 0.85f, 0.55f);
        private static readonly Color RegionBorder          = new Color(1f, 0.78f, 0.24f, 0.72f);
        private static readonly Color KillerBorder          = new Color(0.85f, 0.15f, 0.12f, 0.90f);

        // ── Callbacks ──
        public Action<int, int> OnCellClicked;

        public HashSet<long> CageBorderEdges => _cageBorderEdges;

        public void Configure(RunMapController map, RectTransform gridRoot)
        {
            _map = map;
            _gridRoot = gridRoot;
            _cells = new List<CellView>();
            _overlayObjects = new List<GameObject>();
            _cageBorderEdges = new HashSet<long>();
        }

        public void MarkOverlayDirty() => _overlaysBuilt = false;

        public void RebuildBoard(int selectedRow, int selectedCol, int highlightValue,
            bool hasAntiknight, HashSet<(int, int)> bagHighlightCells, int fogDisabledMovesRemaining,
            AccessibilityService accessibility)
        {
            var run = _map?.Run;
            var board = run?.CurrentBoard;
            if (board == null || _gridRoot == null) return;

            if (_boardSize != board.Size || _cells.Count == 0)
            {
                BuildGrid(board.Size);
                _overlaysBuilt = false;
            }

            if (!_overlaysBuilt)
            {
                BuildOverlays(accessibility);
                _overlaysBuilt = true;
            }

            RenderBoard(board, selectedRow, selectedCol, highlightValue,
                hasAntiknight, bagHighlightCells, fogDisabledMovesRemaining);
        }

        public void RenderBoard(SudokuBoard board, int selectedRow, int selectedCol, int highlightValue,
            bool hasAntiknight, HashSet<(int, int)> bagHighlightCells, int fogDisabledMovesRemaining)
        {
            var overlay = _map?.Run?.CurrentOverlay;

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var val = board.Cells[cell.Row, cell.Col];
                var given = board.GivenMask[cell.Row, cell.Col];

                cell.Label.text = val == 0 ? "" : val.ToString();
                cell.Label.color = given ? new Color(0.04f, 0.04f, 0.04f) : Color.white;
                cell.PencilLabel.text = val == 0 ? BuildPencilText(board, cell.Row, cell.Col) : "";

                var c = ComputeCellColor(board, cell.Row, cell.Col, given);
                if (selectedRow == cell.Row && selectedCol == cell.Col)
                    c = SelectedColor;
                else if (selectedRow == cell.Row || selectedCol == cell.Col)
                    c = RowColHL;
                if (highlightValue > 0 && val == highlightValue)
                    c = MatchColor;

                if (hasAntiknight && selectedRow >= 0 && highlightValue > 0)
                {
                    if (IsAntiknightForbiddenCell(board, cell.Row, cell.Col, selectedRow, selectedCol, highlightValue))
                        c = Color.Lerp(c, AntiknightForbidColor, 0.70f);
                }

                if (bagHighlightCells != null && bagHighlightCells.Contains((cell.Row, cell.Col)))
                    c = Color.Lerp(c, BagHighlightColor, 0.65f);

                if (fogDisabledMovesRemaining <= 0 && overlay != null && IsFogged(overlay, cell.Row, cell.Col))
                {
                    c = FogColor;
                    if (val == 0) cell.Label.text = "";
                    cell.PencilLabel.text = "";
                }

                if (!given && HasConflict(board, cell.Row, cell.Col))
                    c = ConflictColor;

                cell.Image.color = c;
                UpdateBorders(board, cell);
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
                var pencilText = InRunUiFactory.CreateText(numGo.transform, "Pencil", "", pencilFs, TextAnchor.UpperLeft, new Color(0.84f, 0.86f, 0.82f, 0.95f));
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

        private static readonly (int dr, int dc)[] KnightMoves =
        {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2), (1, 2), (2, -1), (2, 1)
        };

        private static bool IsAntiknightForbiddenCell(SudokuBoard board, int r, int c,
            int selectedRow, int selectedCol, int highlightValue)
        {
            if (selectedRow < 0 || selectedCol < 0) return false;
            var dr = r - selectedRow;
            var dc = c - selectedCol;
            var isKnightMove = false;
            for (var k = 0; k < KnightMoves.Length; k++)
                if (KnightMoves[k].dr == dr && KnightMoves[k].dc == dc) { isKnightMove = true; break; }
            if (!isKnightMove) return false;

            var selectedVal = board.Cells[selectedRow, selectedCol];
            if (highlightValue > 0)
                return board.Cells[r, c] == highlightValue;
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

            cell.BorderTop.color    = (r == 0        || map[r - 1, c] != region) ? RegionBorder : clear;
            cell.BorderBottom.color = (r == sz - 1   || map[r + 1, c] != region) ? RegionBorder : clear;
            cell.BorderLeft.color   = (c == 0        || map[r, c - 1] != region) ? RegionBorder : clear;
            cell.BorderRight.color  = (c == sz - 1   || map[r, c + 1] != region) ? RegionBorder : clear;

            if (_cageBorderEdges.Count > 0)
            {
                if (IsCageEdge(r, c, sz, 0)) cell.BorderTop.color    = KillerBorder;
                if (IsCageEdge(r, c, sz, 1)) cell.BorderBottom.color = KillerBorder;
                if (IsCageEdge(r, c, sz, 2)) cell.BorderLeft.color   = KillerBorder;
                if (IsCageEdge(r, c, sz, 3)) cell.BorderRight.color  = KillerBorder;
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
                    DrawCircle(px, py, cW * 0.36f, new Color(0.85f, 0.75f, 0.15f, 0.50f));
                    DrawOverlayText(px, py, "P", 10, new Color(1f, 0.95f, 0.4f, 0.95f));
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
            LineType.ConsecutiveLine => new Color(0.95f, 0.65f, 0.15f, 0.70f),
            LineType.SlowThermo      => new Color(0.70f, 0.30f, 0.80f, 0.70f),
            LineType.UniqueSetLine   => new Color(0.20f, 0.70f, 0.95f, 0.65f),
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

            var txt = InRunUiFactory.CreateText(bg.transform, "Sum", sum.ToString(), 11, TextAnchor.MiddleCenter, KillerBorder);
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
