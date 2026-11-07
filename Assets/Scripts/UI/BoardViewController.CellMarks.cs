using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed partial class BoardViewController
    {
        private static readonly Color LanternGlow = new Color(1f, 0.78f, 0.25f, 1f);
        private RectTransform _selectionFrame;
        private float _selectionFramePx;
        private readonly List<Image> _lanternMarks = new List<Image>();

        // [REQ: UI-CELL-FOCUS-001] Paul's playtest of 0.4.2 (owner's pick FB5): the chosen cell wears a thick vermilion ink
        // frame above every highlight; a chosen 6 among highlighted 6s looked like the others.
        // [REQ: CLASS-PASSIVE-003] The Lantern Seer's lights glow in their cells.
        private void RenderCellMarks(int selectedRow, int selectedCol)
        {
            var selected = FindCellView(selectedRow, selectedCol);
            if (selected == null)
            {
                if (_selectionFrame != null) _selectionFrame.gameObject.SetActive(false);
            }
            else
            {
                var px = selected.Image.rectTransform.rect.width;
                if (_selectionFrame != null && Mathf.Abs(px - _selectionFramePx) > 1f)
                {
                    ScrollUiKit.DestroyNow(_selectionFrame.gameObject);   // built before the layout ran: the line was too thin
                    _selectionFrame = null;
                }
                if (_selectionFrame == null)
                {
                    _selectionFrame = (RectTransform)new GameObject("SelectionFrame", typeof(RectTransform)).transform;
                    ScrollUiKit.InkFrame(_selectionFrame, ScrollUiKit.Vermilion, Mathf.Max(3f, Mathf.Round(px * 0.075f)));
                    _selectionFramePx = px;
                }
                _selectionFrame.SetParent(selected.Label.transform.parent, false);
                InRunUiFactory.StretchFill(_selectionFrame);
                _selectionFrame.SetAsLastSibling();
                _selectionFrame.gameObject.SetActive(true);
            }

            var lights = _map?.Run?.LanternLightCells();
            var count = lights?.Count ?? 0;
            for (var i = 0; i < Mathf.Max(count, _lanternMarks.Count); i++)
            {
                if (i >= count)
                {
                    if (_lanternMarks[i] != null) _lanternMarks[i].gameObject.SetActive(false);
                    continue;
                }
                while (_lanternMarks.Count <= i) _lanternMarks.Add(null);
                var cell = FindCellView(lights[i].row, lights[i].col);
                if (cell == null) continue;
                if (_lanternMarks[i] == null)
                {
                    _lanternMarks[i] = ScrollUiKit.Fill(cell.Label.transform.parent, "LanternLight", Vector2.zero, Vector2.one,
                        LanternGlow, ScrollUiKit.Glow());
                    _lanternMarks[i].raycastTarget = false;
                    var core = ScrollUiKit.Fill(_lanternMarks[i].transform, "Core", new Vector2(0.36f, 0.36f), new Vector2(0.64f, 0.64f),
                        new Color(1f, 0.95f, 0.75f, 1f), ScrollUiKit.Disc());
                    core.raycastTarget = false;
                }
                var mark = _lanternMarks[i].rectTransform;
                mark.SetParent(cell.Label.transform.parent, false);
                mark.anchorMin = new Vector2(-0.15f, -0.15f);   // follows the cell's size, laid out or not; the glow spills a little
                mark.anchorMax = new Vector2(1.15f, 1.15f);
                mark.offsetMin = mark.offsetMax = Vector2.zero;
                mark.SetAsFirstSibling();   // under the notes
                mark.gameObject.SetActive(true);
            }
        }

        private CellView FindCellView(int row, int col)
        {
            if (row < 0 || col < 0) return null;
            foreach (var cell in _cells)
                if (cell.Row == row && cell.Col == col) return cell;
            return null;
        }
    }
}
