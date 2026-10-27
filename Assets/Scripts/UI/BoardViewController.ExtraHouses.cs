using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: GEN-MOD-002] Extra houses of batch B5 (BossMechanicsSystem.md "Modifier catalog"), drawn under the lines: X-Sudoku as two
    // thin indigo diagonals, Hyper windows as teal-washed squares with a dashed outline, Disjoint groups as a small pip
    // in every cell at the cell's place in its box, extra regions as a violet wash with a dashed outline, the magic
    // square as a gold box with "15" in its corner.
    public sealed partial class BoardViewController
    {
        private const string XSudokuKey = "Houses.X";
        private const string HyperKey = "Houses.Hyper";
        private const string DisjointKey = "Houses.Disjoint";
        private const string ExtraRegionKey = "Houses.Extra";
        private const string MagicSquareKey = "Houses.Magic";

        private void DrawExtraHouses(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (overlay.IsXSudoku)
            {
                UseOverlayGroup(XSudokuKey);
                DrawOverlaySegment(new Vector2(-tW * 0.5f, tH * 0.5f), new Vector2(tW * 0.5f, -tH * 0.5f), GamePalette.OverlayXDiagonal, 3f);
                DrawOverlaySegment(new Vector2(tW * 0.5f, tH * 0.5f), new Vector2(-tW * 0.5f, -tH * 0.5f), GamePalette.OverlayXDiagonal, 3f);
            }

            if (overlay.IsHyperSudoku && _boardSize == 9)
            {
                UseOverlayGroup(HyperKey);
                foreach (var top in new[] { 1, 5 })
                foreach (var left in new[] { 1, 5 })
                {
                    var window = new KillerCage();
                    for (var i = 0; i < 9; i++) window.Cells.Add(new CellCoord(top + i / 3, left + i % 3));
                    DrawWash(window, GamePalette.OverlayHyperWash, cW, cH, sX, sY, tW, tH);
                    DrawCageOutline(window, GamePalette.OverlayHyper, cW, cH, sX, sY, tW, tH);
                }
            }

            if (overlay.IsDisjointGroups && _boardSize == 9)
            {
                UseOverlayGroup(DisjointKey);
                for (var r = 0; r < 9; r++)
                for (var c = 0; c < 9; c++)
                {
                    var p = CellCenter(new CellCoord(r, c), cW, cH, sX, sY, tW, tH)
                            + new Vector2((c % 3 - 1) * cW * 0.36f, (1 - r % 3) * cH * 0.36f);
                    DrawCircle(p.x, p.y, Mathf.Max(2.5f, cW * 0.045f), GamePalette.OverlayDisjointPip);
                }
            }

            foreach (var region in overlay.ExtraRegions)
            {
                UseOverlayGroup(ExtraRegionKey);
                var outline = new KillerCage();
                outline.Cells.AddRange(region.Cells);
                DrawWash(outline, GamePalette.OverlayExtraRegionWash, cW, cH, sX, sY, tW, tH);
                DrawCageOutline(outline, GamePalette.OverlayExtraRegion, cW, cH, sX, sY, tW, tH);
            }

            foreach (var cage in overlay.KillerCages)
            {
                if (cage.CageType != KillerCageType.MagicSquare || cage.Cells.Count == 0) continue;
                UseOverlayGroup(MagicSquareKey);
                DrawWash(cage, GamePalette.OverlayMagicWash, cW, cH, sX, sY, tW, tH);
                DrawCageOutline(cage, GamePalette.OverlayMagic, cW, cH, sX, sY, tW, tH);
                var tl = cage.Cells[0];
                DrawCageSum(tl.Col * (cW + sX) - tW * 0.5f, tH * 0.5f - tl.Row * (cH + sY), MagicSquareRule.LineSum.ToString(),
                    GamePalette.OverlayMagic, cW, cH);
                _cageLabelCells.Add((tl.Row, tl.Col));
            }
        }

        // A translucent fill over each cell of the group (the gaps between neighbouring cells included).
        private void DrawWash(KillerCage group, Color color, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            foreach (var cell in group.Cells)
            {
                var go = new GameObject("Wash", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(OverlayParent, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = CellCenter(cell, cW, cH, sX, sY, tW, tH);
                rt.sizeDelta = new Vector2(cW + sX, cH + sY);
                var image = go.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                _overlayObjects.Add(go);
            }
        }
    }
}
