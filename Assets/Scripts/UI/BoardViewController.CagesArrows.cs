using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: GEN-MOD-002] Cages and arrows of every kind (BossMechanicsSystem.md "Modifier catalog", batches B3 and B6). A cage is one
    // dashed outline (UI-STYLE-006) in its kind's colour with a label in the upper-left corner: the sum, "?" (sum
    // hidden), "×24" (product), "−3" (difference), "1:3" (ratio) or "R" (Renban). An arrow is a ring with its path;
    // an average arrow carries "Ø", a double arrow a ring at both ends, a pill arrow a two-cell pill. Each kind draws
    // into its own legend layer.
    public sealed partial class BoardViewController
    {
        private const string HiddenCagesKey = "Cages.Hidden";
        private const string ProductCagesKey = "Cages.Product";
        private const string DifferenceCagesKey = "Cages.Difference";
        private const string RatioCagesKey = "Cages.Ratio";
        private const string RenbanCagesKey = "Cages.Renban";
        private const string AverageArrowsKey = "Arrows.Average";
        private const string DoubleArrowsKey = "Arrows.Double";
        private const string PillArrowsKey = "Arrows.Pill";

        private const float ArrowRingRadius = 18f;

        private static string CageGroupKey(KillerCageType type) => type switch
        {
            KillerCageType.HiddenSum => HiddenCagesKey,
            KillerCageType.Product => ProductCagesKey,
            KillerCageType.Difference => DifferenceCagesKey,
            KillerCageType.Ratio => RatioCagesKey,
            KillerCageType.RenbanSet => RenbanCagesKey,
            _ => CagesKey
        };

        internal static Color CageColor(KillerCageType type) => type switch
        {
            KillerCageType.Product => GamePalette.OverlayCageProduct,
            KillerCageType.Difference => GamePalette.OverlayCageDifference,
            KillerCageType.Ratio => GamePalette.OverlayCageRatio,
            KillerCageType.RenbanSet => GamePalette.OverlayCageRenban,
            _ => KillerBorder
        };

        /// <summary>[REQ: RELIC-KEY-002] Cage Whisper: a hidden sum shows its last digit ("?7").</summary>
        public static bool WhisperHiddenSums;

        /// <summary>The label in a cage's corner. A hidden sum shows "?", or its last digit with Cage Whisper.</summary>
        public static string CageLabel(KillerCage cage) => cage.CageType switch
        {
            KillerCageType.HiddenSum => WhisperHiddenSums ? "?" + (cage.Sum % 10) : "?",
            KillerCageType.Product => "×" + cage.Sum,
            KillerCageType.Difference => "−" + cage.Sum,
            KillerCageType.Ratio => "1:" + cage.Sum,
            KillerCageType.RenbanSet => "R",
            _ => cage.Sum.ToString()
        };

        private static string ArrowGroupKey(ArrowType type) => type switch
        {
            ArrowType.Average => AverageArrowsKey,
            ArrowType.DoubleEndpoint => DoubleArrowsKey,
            ArrowType.Pill => PillArrowsKey,
            _ => ArrowsKey
        };

        // Killer cages of every kind: one dashed outline per cage, inset into its cells so the thick box borders on the
        // cell views do not hide it, and joined across the cells so the cage reads as one shape (UI-STYLE-006).
        private void DrawCages(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            for (var ci = 0; ci < overlay.KillerCages.Count; ci++)
            {
                var cage = overlay.KillerCages[ci];
                if (cage.Cells.Count == 0 || cage.CageType == KillerCageType.MagicSquare) continue;   // drawn with the houses
                UseOverlayGroup(CageGroupKey(cage.CageType));
                var color = CageColor(cage.CageType);
                DrawCageOutline(cage, color, cW, cH, sX, sY, tW, tH);

                var tl = cage.Cells[0];
                for (var c = 1; c < cage.Cells.Count; c++)
                {
                    var cc = cage.Cells[c];
                    if (cc.Row < tl.Row || (cc.Row == tl.Row && cc.Col < tl.Col)) tl = cc;
                }
                var tlX = tl.Col * (cW + sX) - tW * 0.5f;
                var tlY = tH * 0.5f - tl.Row * (cH + sY);
                DrawCageSum(tlX, tlY, CageLabel(cage), color, cW, cH);
                _cageLabelCells.Add((tl.Row, tl.Col));
            }
        }

        private void DrawArrows(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var ring = GamePalette.OverlayArrow;
            var pathColor = GamePalette.OverlayArrowPath;
            for (var ai = 0; ai < overlay.Arrows.Count; ai++)
            {
                var arrow = overlay.Arrows[ai];
                UseOverlayGroup(ArrowGroupKey(arrow.ArrowType));
                DrawRingDot(arrow.CircleCell, ring, ArrowRingRadius, cW, cH, sX, sY, tW, tH);

                // The path leaves the circle (a pill: its second cell) and runs through the arrow cells; a double arrow
                // ends in its second circle.
                var from = arrow.ArrowType == ArrowType.Pill ? arrow.SecondCircle : arrow.CircleCell;
                for (var pi = 0; pi < arrow.ArrowCells.Count; pi++)
                {
                    DrawOverlayLine(from, arrow.ArrowCells[pi], pathColor, 3f, cW, cH, sX, sY, tW, tH);
                    from = arrow.ArrowCells[pi];
                }

                switch (arrow.ArrowType)
                {
                    case ArrowType.Average:
                    {
                        var p = CellCenter(arrow.CircleCell, cW, cH, sX, sY, tW, tH);
                        DrawOverlayText(p.x + cW * 0.30f, p.y + cH * 0.30f, "Ø", Mathf.Clamp(Mathf.RoundToInt(cH * 0.22f), 11, 16), ring);
                        break;
                    }
                    case ArrowType.DoubleEndpoint:
                        DrawOverlayLine(from, arrow.SecondCircle, pathColor, 3f, cW, cH, sX, sY, tW, tH);
                        DrawRingDot(arrow.SecondCircle, ring, ArrowRingRadius, cW, cH, sX, sY, tW, tH);
                        break;
                    case ArrowType.Pill:
                        DrawPill(arrow.CircleCell, arrow.SecondCircle, ring, cW, cH, sX, sY, tW, tH);
                        break;
                }
            }
        }

        // A pill: a ring on each cell and the two sides joining them.
        private void DrawPill(CellCoord a, CellCoord b, Color color, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            DrawRingDot(b, color, ArrowRingRadius, cW, cH, sX, sY, tW, tH);
            var p = CellCenter(a, cW, cH, sX, sY, tW, tH);
            var q = CellCenter(b, cW, cH, sX, sY, tW, tH);
            var side = new Vector2(-(q - p).normalized.y, (q - p).normalized.x) * (ArrowRingRadius - 1f);
            DrawOverlaySegment(p + side, q + side, color, 2f);
            DrawOverlaySegment(p - side, q - side, color, 2f);
        }

        // A small tag in a line's colour in the lower-right corner of a cell: an N-line's number, a sum line's total.
        private void DrawLineTag(CellCoord cell, string text, Color color, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var p = CellCenter(cell, cW, cH, sX, sY, tW, tH) + new Vector2(cW * 0.26f, -cH * 0.30f);
            var tag = new GameObject("LineTag", typeof(RectTransform), typeof(Image));
            tag.transform.SetParent(OverlayParent, false);
            var rt = (RectTransform)tag.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = p;
            rt.sizeDelta = new Vector2(Mathf.Max(26f, cW * 0.44f), Mathf.Max(16f, cH * 0.26f));
            var image = tag.GetComponent<Image>();
            image.sprite = ScrollUiKit.Rounded(8f);
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            _overlayObjects.Add(tag);
            DrawOverlayText(p.x, p.y, text, Mathf.Clamp(Mathf.RoundToInt(cH * 0.2f), 10, 15), Color.white);
        }

        // Where a region-sum line or an N-line crosses a region border, an ink tick across the band marks the cut.
        private void DrawRegionTicks(ModifierLine line, float bandW, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var map = _map?.Run?.CurrentBoard?.RegionMap;
            if (map == null) return;
            var cells = line.Cells;
            for (var i = 0; i < cells.Count - 1; i++)
            {
                if (map[cells[i].Row, cells[i].Col] == map[cells[i + 1].Row, cells[i + 1].Col]) continue;
                var a = CellCenter(cells[i], cW, cH, sX, sY, tW, tH);
                var b = CellCenter(cells[i + 1], cW, cH, sX, sY, tW, tH);
                var mid = (a + b) * 0.5f;
                var dir = (b - a).normalized;
                var across = new Vector2(-dir.y, dir.x) * bandW * 1.1f;
                DrawOverlaySegment(mid - across, mid + across, GamePalette.RegionBorder, 3f);
            }
        }
    }
}
