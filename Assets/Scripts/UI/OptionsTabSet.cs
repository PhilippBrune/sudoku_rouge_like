using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Tab bar and sections of an Options card (main menu and in-run). One section is visible at a time. Each section
    /// registers its controls in rows, top to bottom and left to right; the D-pad moves within a row, to the nearest
    /// control of the next row, from the first row up to the active tab, and from the last row down to the footer
    /// buttons. The links are rebuilt when a control is shown or hidden (language confirmation, Abandon Run in tutorials).
    /// </summary>
    // [REQ: UI-OPT-003]
    public sealed class OptionsTabSet : MonoBehaviour
    {
        private static readonly Selectable[][] NoRows = new Selectable[0][];

        private readonly List<Button> _tabs = new List<Button>();
        private readonly List<RectTransform> _sections = new List<RectTransform>();
        private readonly List<Selectable[][]> _rows = new List<Selectable[][]>();
        private Selectable[][] _footer = NoRows;
        private RectTransform _card;
        private int _linkedSignature;
        private bool _paper;   // UI-STYLE-017: bookmarks above the scroll's paper

        public int ActiveIndex { get; private set; }
        public int Count => _tabs.Count;
        public IReadOnlyList<Button> Tabs => _tabs;
        public IReadOnlyList<RectTransform> Sections => _sections;

        /// <summary>The tab set of <paramref name="card"/>, emptied for a fresh build.</summary>
        public static OptionsTabSet On(RectTransform card)
        {
            var set = OptionsUiFactory.EnsureComponent<OptionsTabSet>(card.gameObject);
            set._card = card;
            set._tabs.Clear();
            set._sections.Clear();
            set._rows.Clear();
            set._footer = NoRows;
            set.ActiveIndex = 0;
            set._paper = OptionsUiStyle.Paper;
            return set;
        }

        /// <summary>Adds a tab button to the bar and returns its (empty) section.</summary>
        public RectTransform Add(string tabName, string sectionName, string label)
        {
            var tab = OptionsUiFactory.BuildButton(tabName, _card, label, OptionsUiStyle.TabLabelSize);
            var section = OptionsUiFactory.EnsureRect(sectionName, _card,
                OptionsUiStyle.SectionMin, OptionsUiStyle.SectionMax, Vector2.zero, Vector2.zero);
            var index = _tabs.Count;
            tab.onClick.RemoveAllListeners();
            tab.onClick.AddListener(() => Activate(index));
            _tabs.Add(tab);
            _sections.Add(section);
            _rows.Add(NoRows);

            if (_paper)
            {
                // [REQ: UI-STYLE-017] Cloth bookmarks hanging over the top of the paper, between the LB and RB marks.
                tab.GetComponent<Image>().sprite = ScrollUiKit.Rounded(10f);
                tab.GetComponent<Image>().type = Image.Type.Sliced;
                var tabLabel = tab.GetComponentInChildren<Text>();
                if (tabLabel != null)
                {
                    // Long translations ("Barrierefreiheit", "Acessibilidade") shrink to the bookmark.
                    tabLabel.resizeTextForBestFit = true;
                    tabLabel.resizeTextMinSize = 12;
                    tabLabel.resizeTextMaxSize = OptionsUiStyle.TabLabelSize;
                    tabLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    tabLabel.verticalOverflow = VerticalWrapMode.Truncate;
                }
                var w = 0.82f / _tabs.Count;
                for (var i = 0; i < _tabs.Count; i++)
                    OptionsUiFactory.SetRect((RectTransform)_tabs[i].transform,
                        new Vector2(0.09f + i * w, 1.0f), new Vector2(0.09f + (i + 1) * w - 0.008f, 1.075f));
                return section;
            }
            var width = 0.96f / _tabs.Count;
            for (var i = 0; i < _tabs.Count; i++)
                OptionsUiFactory.SetRect((RectTransform)_tabs[i].transform,
                    new Vector2(0.02f + i * width, 0.88f), new Vector2(0.02f + (i + 1) * width - 0.01f, 0.94f));
            return section;
        }

        public void SetRows(int index, params Selectable[][] rows)
        {
            _rows[index] = rows ?? NoRows;
            Relink();
        }

        /// <summary>Buttons below the sections (Back, ...), reachable from the last row of every section.</summary>
        public void SetFooter(params Selectable[][] rows)
        {
            _footer = rows ?? NoRows;
            Relink();
        }

        public void Activate(int index)
        {
            ActiveIndex = index;
            for (var i = 0; i < _tabs.Count; i++)
            {
                _sections[i].gameObject.SetActive(i == index);
                var img = _tabs[i].GetComponent<Image>();
                var lbl = _tabs[i].GetComponentInChildren<Text>();
                if (_paper)
                {
                    // The open page's bookmark is paper and sits a little higher; the others are brown cloth.
                    var colors = _tabs[i].colors;
                    colors.normalColor = i == index ? new Color(0.94f, 0.90f, 0.81f, 1f) : new Color(0.47f, 0.31f, 0.17f, 1f);
                    colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.12f);
                    colors.selectedColor = Color.Lerp(colors.normalColor, OptionsUiStyle.PaperFocus, 0.6f);
                    _tabs[i].colors = colors;
                    if (img != null) img.color = Color.white;
                    if (lbl != null) lbl.color = i == index ? OptionsUiStyle.PaperInk : new Color(0.97f, 0.92f, 0.80f, 1f);
                    var rt = (RectTransform)_tabs[i].transform;
                    rt.anchoredPosition = new Vector2(0f, i == index ? 6f : 0f);
                    continue;
                }
                if (img != null) img.color = i == index
                    ? GamePalette.WithAlpha(GamePalette.AccentGold, 0.40f)
                    : GamePalette.WithAlpha(GamePalette.Panel, 0.80f);
                if (lbl != null) lbl.color = i == index ? GamePalette.AccentGold : GamePalette.TextMuted;
            }
            Relink();
        }

        private void Update()
        {
            if (Signature() != _linkedSignature) Relink();
            // [REQ: UI-STYLE-017] LB/RB (PageUp/PageDown on a keyboard) turn the page; the focus moves to its bookmark.
            if (_tabs.Count < 2) return;
            var step = Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton5)) || Input.GetKeyDown(KeyCode.PageDown) ? 1
                : Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton4)) || Input.GetKeyDown(KeyCode.PageUp) ? -1 : 0;
            if (step != 0) TurnPage(step);
        }

        /// <summary>Opens the next (+1) or previous (-1) page, wrapping round, and selects its bookmark.</summary>
        public void TurnPage(int step)
        {
            var next = ((ActiveIndex + step) % _tabs.Count + _tabs.Count) % _tabs.Count;
            Activate(next);
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(_tabs[next].gameObject);
        }

        public void Relink()
        {
            if (_tabs.Count == 0 || ActiveIndex >= _tabs.Count) return;
            var rows = new List<Selectable[]>();
            AddShown(rows, _rows[ActiveIndex]);
            AddShown(rows, _footer);

            for (var i = 0; i < _tabs.Count; i++)
                SetNav(_tabs[i],
                    i > 0 ? _tabs[i - 1] : null,
                    i < _tabs.Count - 1 ? _tabs[i + 1] : null,
                    null,
                    rows.Count > 0 ? Nearest(rows[0], CenterX(_tabs[i])) : null);

            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (var i = 0; i < row.Length; i++)
                {
                    var x = CenterX(row[i]);
                    SetNav(row[i],
                        i > 0 ? row[i - 1] : null,
                        i < row.Length - 1 ? row[i + 1] : null,
                        r > 0 ? Nearest(rows[r - 1], x) : _tabs[ActiveIndex],
                        r < rows.Count - 1 ? Nearest(rows[r + 1], x) : null);
                }
            }
            _linkedSignature = Signature();
        }

        private int Signature()
        {
            unchecked
            {
                var sig = ActiveIndex + 17;
                if (ActiveIndex < _rows.Count)
                    foreach (var row in _rows[ActiveIndex])
                        if (row != null)
                            foreach (var s in row) sig = sig * 31 + (IsShown(s) ? 1 : 2);
                foreach (var row in _footer)
                    if (row != null)
                        foreach (var s in row) sig = sig * 31 + (IsShown(s) ? 1 : 2);
                return sig;
            }
        }

        private void AddShown(List<Selectable[]> into, Selectable[][] rows)
        {
            foreach (var row in rows)
            {
                if (row == null) continue;
                var shown = new List<Selectable>(row.Length);
                foreach (var s in row)
                    if (IsShown(s)) shown.Add(s);
                if (shown.Count > 0) into.Add(shown.ToArray());
            }
        }

        // Active itself and up to its section or the card; the section and card may still be inactive while building.
        private bool IsShown(Selectable s)
        {
            if (s == null) return false;
            for (var t = s.transform; t != null && t != _card; t = t.parent)
            {
                if (t is RectTransform rt && _sections.Contains(rt)) return true;
                if (!t.gameObject.activeSelf) return false;
            }
            return true;
        }

        // Horizontal centre in card coordinates, from the anchors (the layout may not have run yet).
        private float CenterX(Selectable s)
        {
            var rt = (RectTransform)s.transform;
            var x = (rt.anchorMin.x + rt.anchorMax.x) * 0.5f;
            for (var t = rt.parent as RectTransform; t != null && t != _card; t = t.parent as RectTransform)
                x = t.anchorMin.x + x * (t.anchorMax.x - t.anchorMin.x);
            return x;
        }

        private Selectable Nearest(Selectable[] row, float x)
        {
            Selectable best = null;
            var bestDistance = float.MaxValue;
            foreach (var s in row)
            {
                var d = Mathf.Abs(CenterX(s) - x);
                if (d < bestDistance) { bestDistance = d; best = s; }
            }
            return best;
        }

        private static void SetNav(Selectable s, Selectable left, Selectable right, Selectable up, Selectable down)
        {
            s.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = left,
                selectOnRight = right,
                selectOnUp = up,
                selectOnDown = down
            };
        }
    }
}
