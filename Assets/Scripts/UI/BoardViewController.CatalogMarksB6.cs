using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: GEN-MOD-002] Board marks for batch B6 of the modifier catalog (modifier-catalog-plan.md): the 1:3 dot, distance
    // links, opposite-sum badges, counting circles, distance cells and the shaded inverted zone. Colours in GamePalette;
    // each mark draws into its modifier's legend layer.
    public sealed partial class BoardViewController
    {
        private const string RatioNKey = "Pairs.RatioN";
        private const string DistancePairKey = "CellPairs.Distance";
        private const string OppositeSumKey = "CellPairs.OppositeSum";
        private const string CountingKey = "Markers.Counting";
        private const string DistanceCellKey = "Markers.DistanceCell";

        private static int SmallFont(float cH) => Mathf.Clamp(Mathf.RoundToInt(cH * 0.2f), 10, 15);

        // A black dot like a ratio Kropki dot, with the ratio (3) on it.
        private void DrawRatioNPair(AdjacentPairConstraint pair, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            UseOverlayGroup(RatioNKey);
            var mid = (CellCenter(pair.CellA, cW, cH, sX, sY, tW, tH) + CellCenter(pair.CellB, cW, cH, sX, sY, tW, tH)) * 0.5f;
            DrawCircle(mid.x, mid.y, 11f, GamePalette.OverlayKropkiBlack);
            DrawOverlayText(mid.x, mid.y, RatioNPairRule.N(pair).ToString(), 10, Color.white);
        }

        // Distance links (brown, from the edge of one cell to the edge of the other, the steps on a badge where they cross)
        // and opposite sums (a crimson badge with the total in the upper-right corner of both cells).
        private void DrawValuePairs(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (overlay.CellPairs == null) return;
            foreach (var pair in overlay.CellPairs)
            {
                if (pair.PairType == CellPairType.Distance)
                {
                    UseOverlayGroup(DistancePairKey);
                    var a = CellCenter(pair.CellA, cW, cH, sX, sY, tW, tH);
                    var b = CellCenter(pair.CellB, cW, cH, sX, sY, tW, tH);
                    var dir = (b - a).normalized;
                    var trim = Mathf.Min(cW, cH) * 0.32f;
                    DrawOverlaySegment(a + dir * trim, b - dir * trim, GamePalette.OverlayDistancePair, 3f);
                    var mid = (a + b) * 0.5f;
                    DrawCircle(mid.x, mid.y, Mathf.Max(8f, Mathf.Min(cW, cH) * 0.15f), GamePalette.OverlayDistancePair);
                    DrawOverlayText(mid.x, mid.y, pair.Value.ToString(), 10, Color.white);
                }
                else if (pair.PairType == CellPairType.OppositeSum)
                {
                    UseOverlayGroup(OppositeSumKey);
                    foreach (var cell in new[] { pair.CellA, pair.CellB })
                    {
                        var p = CellCenter(cell, cW, cH, sX, sY, tW, tH) + new Vector2(cW * 0.29f, cH * 0.29f);
                        DrawCircle(p.x, p.y, Mathf.Max(9f, Mathf.Min(cW, cH) * 0.17f), GamePalette.OverlayOppositeSum);
                        DrawOverlayText(p.x, p.y, pair.Value.ToString(), 10, Color.white);
                    }
                }
            }
        }

        // Counting circles: an orange ring round the digit. Distance cells: the navy ring on the target, a small navy dot in
        // the upper-right corner of each cell that counts its steps to it. False for other markers.
        private bool DrawCatalogMarker(CellMarker marker, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var p = CellCenter(marker.Cell, cW, cH, sX, sY, tW, tH);
            var r = Mathf.Min(cW, cH);
            switch (marker.Type)
            {
                case MarkerType.CountingCircle:
                    UseOverlayGroup(CountingKey);
                    DrawRingAtPos(p.x, p.y, r * 0.40f, GamePalette.OverlayCountingCircle);
                    return true;
                case MarkerType.DistanceCellTarget:
                    UseOverlayGroup(DistanceCellKey);
                    DrawRingAtPos(p.x, p.y, r * 0.42f, GamePalette.OverlayDistanceCell);
                    DrawRingAtPos(p.x, p.y, r * 0.36f, GamePalette.OverlayDistanceCell);
                    return true;
                case MarkerType.DistanceCellSource:
                    UseOverlayGroup(DistanceCellKey);
                    DrawCircle(p.x + cW * 0.30f, p.y + cH * 0.30f, Mathf.Max(5f, r * 0.09f), GamePalette.OverlayDistanceCell);
                    return true;
                default:
                    return false;
            }
        }

        // The inverted zone: a green wash under the lines and a dashed green outline round the 3×3 area.
        private void DrawNegationZone(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (overlay.NegationZone == null || overlay.NegationZone.Count == 0) return;
            UseOverlayGroup(LineKey(LineType.ZoneWhisper));
            int top = int.MaxValue, left = int.MaxValue, bottom = int.MinValue, right = int.MinValue;
            foreach (var c in overlay.NegationZone)
            {
                top = Mathf.Min(top, c.Row); bottom = Mathf.Max(bottom, c.Row);
                left = Mathf.Min(left, c.Col); right = Mathf.Max(right, c.Col);
            }
            var a = CellCenter(new CellCoord(top, left), cW, cH, sX, sY, tW, tH);
            var b = CellCenter(new CellCoord(bottom, right), cW, cH, sX, sY, tW, tH);
            var wash = new GameObject("NegationZone", typeof(RectTransform), typeof(Image));
            wash.transform.SetParent(OverlayParent, false);
            var rt = (RectTransform)wash.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x) + cW, Mathf.Abs(b.y - a.y) + cH);
            var image = wash.GetComponent<Image>();
            image.color = GamePalette.OverlayNegationZone;
            image.raycastTarget = false;
            _overlayObjects.Add(wash);
            var outline = new KillerCage();
            outline.Cells.AddRange(overlay.NegationZone);
            DrawCageOutline(outline, GamePalette.LineGermanWhispers, cW, cH, sX, sY, tW, tH);
        }
    }
}
