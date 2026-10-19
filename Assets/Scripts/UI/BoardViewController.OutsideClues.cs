using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-010] Clues outside the grid: on a board with outside clues the grid is drawn about 12 % smaller
    // (owner decision 2026-10-19) and the clues stand in a ring round it, one beside each row or column; a Little Killer
    // clue sits outside a corner of its diagonal with a short arrow along it.
    // [REQ: UI-STYLE-011] Quadruples: a white circle with an ink rim on the corner the four cells share, with its digits.
    public sealed partial class BoardViewController
    {
        /// <summary>Share of the normal board size used when outside clues need the ring round the grid.</summary>
        public const float ClueRingBoardScale = 0.88f;

        private const string QuadruplesKey = "Corner.Quadruples";

        private static string OutsideClueKey(OutsideClueType type) => "Outside." + type;

        public static bool HasClueRing(ModifierOverlayData overlay) =>
            overlay?.OutsideClues != null && overlay.OutsideClues.Count > 0;

        private void DrawOutsideClues(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (!HasClueRing(overlay)) return;
            var font = Mathf.Clamp(Mathf.RoundToInt(cH * 0.36f), 14, 30);
            var gap = cH * 0.42f;
            var ink = GamePalette.OverlayOutsideClue;
            foreach (var clue in overlay.OutsideClues)
            {
                UseOverlayGroup(OutsideClueKey(clue.Type));
                var text = clue.Value.ToString();
                switch (clue.Edge)
                {
                    case OutsideClueEdge.Top:
                        DrawOverlayText(CellCenter(new CellCoord(0, clue.Index), cW, cH, sX, sY, tW, tH).x, tH * 0.5f + gap, text, font, ink);
                        break;
                    case OutsideClueEdge.Bottom:
                        DrawOverlayText(CellCenter(new CellCoord(0, clue.Index), cW, cH, sX, sY, tW, tH).x, -tH * 0.5f - gap, text, font, ink);
                        break;
                    case OutsideClueEdge.Left:
                        DrawOverlayText(-tW * 0.5f - gap, CellCenter(new CellCoord(clue.Index, 0), cW, cH, sX, sY, tW, tH).y, text, font, ink);
                        break;
                    case OutsideClueEdge.Right:
                        DrawOverlayText(tW * 0.5f + gap, CellCenter(new CellCoord(clue.Index, 0), cW, cH, sX, sY, tW, tH).y, text, font, ink);
                        break;
                    default:
                        DrawLittleKiller(clue, text, font, ink, cW, cH, sX, sY, tW, tH);
                        break;
                }
            }
        }

        // The clue stands where the diagonal would continue outside the grid; a short ink arrow points along it.
        private void DrawLittleKiller(OutsideClue clue, string text, int font, Color ink,
            float cW, float cH, float sX, float sY, float tW, float tH)
        {
            var cells = OutsideClueGeometry.Cells(clue, _boardSize);
            if (cells.Count == 0) return;
            var start = CellCenter(cells[0], cW, cH, sX, sY, tW, tH);
            var step = new Vector2(clue.Edge == OutsideClueEdge.DiagonalDownRight ? cW + sX : -(cW + sX), -(cH + sY));
            var at = start - step * 0.95f;
            DrawOverlayText(at.x, at.y, text, Mathf.RoundToInt(font * 0.8f), ink);
            var dir = step.normalized;
            var tip = start - step * 0.42f;
            DrawOverlaySegment(at + dir * (cH * 0.30f), tip, ink, 2.5f);
            var side = new Vector2(-dir.y, dir.x) * 5f;
            DrawOverlaySegment(tip, tip - dir * 7f + side, ink, 2.5f);
            DrawOverlaySegment(tip, tip - dir * 7f - side, ink, 2.5f);
        }

        private void DrawQuadruples(ModifierOverlayData overlay, float cW, float cH, float sX, float sY, float tW, float tH)
        {
            if (overlay.Quadruples == null || overlay.Quadruples.Count == 0) return;
            UseOverlayGroup(QuadruplesKey);
            var r = Mathf.Min(cW, cH) * 0.40f;
            foreach (var quad in overlay.Quadruples)
            {
                // Two digits share one row and get a larger size; three or four sit in two rows.
                var font = Mathf.Clamp(Mathf.RoundToInt(cH * (quad.Digits.Count <= 2 ? 0.36f : 0.27f)), 12, 26);
                var corner = (CellCenter(quad.TopLeft, cW, cH, sX, sY, tW, tH)
                              + CellCenter(new CellCoord(quad.TopLeft.Row + 1, quad.TopLeft.Col + 1), cW, cH, sX, sY, tW, tH)) * 0.5f;
                DrawCircle(corner.x, corner.y, r + 2f, GamePalette.OverlayOutsideClue);
                DrawCircle(corner.x, corner.y, r, GamePalette.CellEmpty);
                var n = quad.Digits.Count;
                for (var i = 0; i < n; i++)
                {
                    // One row for two digits, two rows for three or four.
                    var columns = n <= 2 ? n : 2;
                    var rows = n <= 2 ? 1 : 2;
                    var x = (i % columns - (columns - 1) * 0.5f) * r * 0.90f;
                    var y = ((rows - 1) * 0.5f - i / columns) * r * 0.86f;
                    DrawOverlayText(corner.x + x, corner.y + y, quad.Digits[i].ToString(), font, GamePalette.OverlayOutsideClue);
                }
            }
        }
    }
}
