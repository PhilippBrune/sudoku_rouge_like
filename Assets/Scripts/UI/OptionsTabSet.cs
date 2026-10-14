using System.Collections.Generic;
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
