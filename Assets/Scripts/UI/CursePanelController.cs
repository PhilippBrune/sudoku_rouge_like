using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;
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

        // [REQ: CURSE-INT-011] RefreshPanel: renders all active curse names + icons; called at node arrival, cleanse, and run-state transitions
        public void RefreshPanel()
        {
            var state = _runMap?.Run?.State;
            ClearIcons();

            if (state == null)
            {
                if (_titleText != null)    _titleText.text    = F("InRun.Curse.Title", 0);
                if (_curseListText != null) _curseListText.text = T("InRun.Curse.None");
                return;
            }

            var curses = CurseService.GetActiveCurses(state);
            if (_titleText != null)
                _titleText.text = F("InRun.Curse.Title", curses.Count);

            if (_curseListText == null) return;

            if (curses.Count == 0)
            {
                _curseListText.text = T("InRun.Curse.None");
                return;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < curses.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(F(
                    "InRun.Curse.Line",
                    curses[i].Name,
                    curses[i].Description));
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
                AddFallbackBadge(iconGo.transform, curses[i].Id);
                _iconPool.Add(img);
            }
        }

        private static void AddFallbackBadge(Transform iconRoot, string curseId)
        {
            if (iconRoot == null) return;

            Color badgeColor;
            string glyph;
            switch (curseId)
            {
                case "blurred_sight":
                    badgeColor = new Color(0.42f, 0.66f, 0.86f, 0.92f);
                    glyph = "B";
                    break;
                case "fraying_thread":
                    badgeColor = new Color(0.78f, 0.54f, 0.30f, 0.92f);
                    glyph = "F";
                    break;
                case "counting_shadow":
                    badgeColor = new Color(0.42f, 0.28f, 0.70f, 0.92f);
                    glyph = "C";
                    break;
                case "double_or_nothing":
                    badgeColor = new Color(0.90f, 0.24f, 0.18f, 0.92f);
                    glyph = "2";
                    break;
                default:
                    return;
            }

            var frameGo = new GameObject("CurseFallbackFrame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(iconRoot, false);
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = Vector2.zero;
            frameRt.offsetMax = Vector2.zero;
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.color = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.18f);
            frameImg.raycastTarget = false;

            var badgeGo = new GameObject("CurseFallbackBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(iconRoot, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.62f, 0.00f);
            badgeRt.anchorMax = new Vector2(1.00f, 0.38f);
            badgeRt.offsetMin = Vector2.zero;
            badgeRt.offsetMax = Vector2.zero;
            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.color = badgeColor;
            badgeImg.raycastTarget = false;

            var labelGo = new GameObject("BadgeLabel", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(badgeGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = glyph;
            label.font = InRunUiFactory.GetFont();
            label.fontSize = 10;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private void ClearIcons()
        {
            for (var i = _iconPool.Count - 1; i >= 0; i--)
                if (_iconPool[i] != null) Destroy(_iconPool[i].gameObject);
            _iconPool.Clear();
        }

        private static string T(string key) => LocalizationService.T(key);

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);
    }
}
