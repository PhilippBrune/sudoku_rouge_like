using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class CursePanelController : MonoBehaviour
    {
        private Text _titleText;
        private Text _curseListText;
        private RectTransform _iconColumn;
        private RunMapController _runMap;

        // Pooled icon Images — rebuilt each RefreshPanel call
        private readonly List<Image> _iconPool = new List<Image>();

        public void Bind(RunMapController runMap)
        {
            _runMap = runMap;
        }

        /// <param name="iconColumn">RectTransform of the left icon column; may be null for text-only mode.</param>
        public void Configure(Text title, Text list, RectTransform iconColumn = null)
        {
            _titleText    = title;
            _curseListText = list;
            _iconColumn   = iconColumn;
        }

        public void RefreshPanel()
        {
            var state = _runMap?.Run?.State;
            ClearIcons();

            if (state == null)
            {
                if (_titleText != null)    _titleText.text    = "Curses (0)";
                if (_curseListText != null) _curseListText.text = "No active curses.";
                return;
            }

            var curses = CurseService.GetActiveCurses(state);
            if (_titleText != null)
                _titleText.text = curses.Count > 0 ? $"Curses ({curses.Count})" : "Curses (0)";

            if (_curseListText == null) return;

            if (curses.Count == 0)
            {
                _curseListText.text = "No active curses.";
                return;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < curses.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append($"{curses[i].Name} \u2014 {curses[i].Description}");
            }
            _curseListText.text = sb.ToString();

            if (_iconColumn == null) return;

            // Stack one icon per curse in the icon column, evenly distributed
            var rowH = 1f / curses.Count;
            for (var i = 0; i < curses.Count; i++)
            {
                var iconGo = new GameObject($"CurseIcon_{i}", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(_iconColumn, false);
                var rt = iconGo.GetComponent<RectTransform>();
                var yMax = 1f - i * rowH;
                var yMin = yMax - rowH;
                rt.anchorMin = new Vector2(0.10f, yMin + 0.04f);
                rt.anchorMax = new Vector2(0.90f, yMax - 0.04f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = iconGo.GetComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                var iconName = "cursed/icon_" + CurseService.GetIconName(curses[i].Id);
                var sprite = Resources.Load<Sprite>(iconName);
                if (sprite != null)
                {
                    img.sprite = sprite;
                }
                else
                {
                    Debug.LogWarning($"[CursePanelController] Missing curse icon: {iconName}");
                    img.color = new Color(0.55f, 0.15f, 0.15f, 0.45f); // muted red placeholder
                }
                _iconPool.Add(img);
            }
        }

        private void ClearIcons()
        {
            for (var i = _iconPool.Count - 1; i >= 0; i--)
                if (_iconPool[i] != null) Destroy(_iconPool[i].gameObject);
            _iconPool.Clear();
        }
    }
}
