using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: GEN-MOD-002] Board marks for catalog batches B1 and B2 (docs/BossMechanicsSystem.md "Modifier catalog"): the extra
    // marks on the new line types, the Difference ≥ 3 bar, the ×3 marker and the mirror-pair badges. Colours in
    // GamePalette; each mark draws into its modifier's legend layer.
    public sealed partial class BoardViewController
    {
        private const string DifferenceKKey = "Pairs.DifferenceK";
        private const string MultipleKey = "Markers.Multiple";
        private const string MirrorKey = "CellPairs.Mirror";

        // Extra marks on top of a line's band; the band itself is drawn by BuildOverlays.
        private void DrawCatalogLineMarks(ModifierLine line, Color color, float bandW, float bulbR,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var cells = line.Cells;
            if (cells.Count == 0) return;
            var ink = GamePalette.RegionBorder;
            switch (line.Type)
            {
                case LineType.NonconsecLine:
                    // A small ink cross on every step: "these two are not consecutive".
                    for (var i = 0; i < cells.Count - 1; i++)
                    {
                        var mid = (CellCenter(cells[i], cW, cH, sX, sY, tW, tH) + CellCenter(cells[i + 1], cW, cH, sX, sY, tW, tH)) * 0.5f;
                        var arm = new Vector2(bandW * 0.7f, bandW * 0.7f);
                        DrawOverlaySegment(mid - arm, mid + arm, ink, 2.5f);
                        DrawOverlaySegment(mid + new Vector2(-arm.x, arm.y), mid + new Vector2(arm.x, -arm.y), ink, 2.5f);
                    }
                    break;
                case LineType.HighLowAlternating:
                    // A paper stripe down the middle: the double rail of a high-low line.
                    for (var i = 0; i < cells.Count - 1; i++)
                        DrawOverlaySegment(CellCenter(cells[i], cW, cH, sX, sY, tW, tH), CellCenter(cells[i + 1], cW, cH, sX, sY, tW, tH),
                            GamePalette.CellEmpty, bandW * 0.3f);
                    break;
                case LineType.ZipperLine:
                    // The middle of the zipper: a ring on the middle cell, or a dot between the two middle cells.
                    if (cells.Count % 2 == 1)
                        DrawRingDot(cells[cells.Count / 2], ink, bandW * 1.15f, cW, cH, sX, sY, tW, tH);
                    else
                    {
                        var mid = (CellCenter(cells[cells.Count / 2 - 1], cW, cH, sX, sY, tW, tH)
                                   + CellCenter(cells[cells.Count / 2], cW, cH, sX, sY, tW, tH)) * 0.5f;
                        DrawCircle(mid.x, mid.y, bandW * 0.45f, ink);
                    }
                    break;
                case LineType.FastThermo:
                    // A thermometer with a double bulb: digits climb by 2 or more.
                    DrawDot(cells[0], color, bulbR, cW, cH, sX, sY, tW, tH);
                    DrawRingDot(cells[0], color, bulbR + 6f, cW, cH, sX, sY, tW, tH);
                    break;
                case LineType.RegionSumLine:
                    DrawRegionTicks(line, bandW, cW, cH, sX, sY, tW, tH);
                    break;
                case LineType.NLine:
                    DrawRegionTicks(line, bandW, cW, cH, sX, sY, tW, tH);
                    DrawLineTag(cells[0], line.LineValue.ToString(), color, cW, cH, sX, sY, tW, tH);
                    break;
                case LineType.SumLine:
                    DrawLineTag(cells[cells.Count - 1], line.LineValue.ToString(), color, cW, cH, sX, sY, tW, tH);
                    break;
                case LineType.WhisperGeneralized:
                    DrawLineTag(cells[0], "≥" + line.LineValue, color, cW, cH, sX, sY, tW, tH);
                    break;
                case LineType.AmbiguousThermo:
                    // No bulb; an ink bar across each end says "a thermometer, direction unknown".
                    DrawEndBar(cells, 0, 1, bandW, ink, cW, cH, sX, sY, tW, tH);
                    DrawEndBar(cells, cells.Count - 1, cells.Count - 2, bandW, ink, cW, cH, sX, sY, tW, tH);
                    break;
            }
        }

        private void DrawEndBar(System.Collections.Generic.List<CellCoord> cells, int end, int inner, float bandW, Color ink,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (inner < 0 || inner >= cells.Count) return;
            var p = CellCenter(cells[end], cW, cH, sX, sY, tW, tH);
            var q = CellCenter(cells[inner], cW, cH, sX, sY, tW, tH);
            var dir = (p - q).normalized;
            var across = new Vector2(-dir.y, dir.x) * bandW * 0.9f;
            DrawOverlaySegment(p - across, p + across, ink, 3f);
        }

        // Difference ≥ K: a plum tag on the edge the two cells share, reading "≥3" (always upright).
        private void DrawDifferenceKPair(AdjacentPairConstraint pair, ModifierOverlayData overlay,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            UseOverlayGroup(DifferenceKKey);
            var mid = (CellCenter(pair.CellA, cW, cH, sX, sY, tW, tH) + CellCenter(pair.CellB, cW, cH, sX, sY, tW, tH)) * 0.5f;
            var tag = new GameObject("DifferenceK", typeof(RectTransform), typeof(Image));
            tag.transform.SetParent(OverlayParent, false);
            var rt = (RectTransform)tag.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(Mathf.Max(30f, cW * 0.46f), Mathf.Max(18f, cH * 0.28f));
            var image = tag.GetComponent<Image>();
            image.sprite = ScrollUiKit.Rounded(8f);
            image.type = Image.Type.Sliced;
            image.color = GamePalette.OverlayDifferenceK;
            image.raycastTarget = false;
            _overlayObjects.Add(tag);
            DrawOverlayText(mid.x, mid.y, "≥" + DifferenceKPairRule.K(pair, overlay), Mathf.Clamp(Mathf.RoundToInt(cH * 0.2f), 11, 16), Color.white);
        }

        // Multiples: an ochre ring with "×3" in the lower left (the upper left belongs to cage sums).
        private void DrawMultipleMarker(CellMarker marker, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            UseOverlayGroup(MultipleKey);
            var p = CellCenter(marker.Cell, cW, cH, sX, sY, tW, tH);
            DrawRingAtPos(p.x, p.y, Mathf.Min(cW, cH) * 0.34f, GamePalette.OverlayMultiple);
            DrawOverlayText(p.x - cW * 0.32f, p.y - cH * 0.33f, "×" + MultipleCellsRule.Divisor,
                Mathf.Clamp(Mathf.RoundToInt(cH * 0.2f), 11, 16), GamePalette.OverlayMultiple);
        }

        // Mirror cells: both cells of a pair carry the same letter in an indigo badge in the upper right.
        private void DrawMirrorPairs(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (overlay.CellPairs == null) return;
            var letter = 0;
            for (var i = 0; i < overlay.CellPairs.Count; i++)
            {
                var pair = overlay.CellPairs[i];
                if (pair.PairType != CellPairType.Mirror) continue;
                UseOverlayGroup(MirrorKey);
                var label = ((char)('A' + letter % 26)).ToString();
                letter++;
                foreach (var cell in new[] { pair.CellA, pair.CellB })
                {
                    var p = CellCenter(cell, cW, cH, sX, sY, tW, tH) + new Vector2(cW * 0.30f, cH * 0.30f);
                    DrawCircle(p.x, p.y, Mathf.Max(8f, Mathf.Min(cW, cH) * 0.15f), GamePalette.OverlayMirror);
                    DrawOverlayText(p.x, p.y, label, 10, Color.white);
                }
            }
        }
    }
}
