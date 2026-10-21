using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-017] The Options as a hand scroll with bookmarks (layout-proposals.md, owner pick 2026-10-20): a banner,
    // cloth bookmarks for the pages above the paper (LB/RB turn them), one setting per line with the label on the left,
    // values as brush strokes with a stone, switches as hanko stamps, the page's slip on the right, "Reset this page" and
    // Back under the scroll. Main menu and run build it with the same parts.
    public static partial class OptionsUiFactory
    {
        // Lines of a page, in section coordinates: the header and subtitle above, eight lines below.
        private const float LineTop = 0.80f, LineStep = 0.093f, LineHeight = 0.083f;
        // Columns of a line: the label, then the control.
        private const float LabelEnd = 0.36f, ControlStart = 0.38f;

        internal static Vector2 LineMin(int line, float x = 0f) => new Vector2(x, LineTop - line * LineStep - LineHeight);
        internal static Vector2 LineMax(int line, float x = 1f) => new Vector2(x, LineTop - line * LineStep);

        // The scroll on a full-screen backdrop (mockup px: paper 255–1660 × 225–875, banner 610–1310 × 24–128).
        private static readonly Vector2 PaperMin = new Vector2(0.133f, 0.19f), PaperMax = new Vector2(0.865f, 0.79f);
        // "Reset this page" and Back under the scroll, in paper coordinates.
        internal static readonly Vector2 FooterBackMin = new Vector2(0.545f, -0.178f), FooterBackMax = new Vector2(0.66f, -0.085f);
        private static readonly Vector2 ResetScreenMin = new Vector2(0.395f, 0.083f), ResetScreenMax = new Vector2(0.522f, 0.139f);

        /// <summary>
        /// The banner, the paper with moss brocade and rollers, the LB/RB marks and the key hint on a full-screen
        /// <paramref name="backdrop"/>. Returns the paper: the card for <see cref="OptionsTabSet"/>.
        /// </summary>
        public static RectTransform BuildOptionsScroll(RectTransform backdrop, string titleName)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            ScrollUiKit.Banner(backdrop, "OptionsBanner", new Vector2(0.318f, 0.882f), new Vector2(0.682f, 0.978f),
                "OptionsEyebrow", T("Options.Eyebrow"), titleName, T("Options"), 46);
            var paper = ScrollUiKit.HandScroll(backdrop, "OptionsScroll", PaperMin, PaperMax, null, true);
            foreach (var (name, text, x) in new[] { ("TabHintLb", "LB", 0.045f), ("TabHintRb", "RB", 0.955f) })
            {
                var mark = BuildText(name, paper, text, 16, TextAnchor.MiddleCenter);
                SetRect(mark.rectTransform, new Vector2(x - 0.03f, 1.01f), new Vector2(x + 0.03f, 1.07f));
                mark.color = new Color(0.86f, 0.82f, 0.72f, 1f);
            }
            var hint = BuildText("OptionsKeyHint", backdrop, T("Options.KeyHint"), 17, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.2f, 0.035f), new Vector2(0.8f, 0.07f));
            hint.color = new Color(0.86f, 0.82f, 0.72f, 1f);
            return paper;
        }

        /// <summary>The vermilion Back pill under the scroll.</summary>
        public static Button BuildBackPill(string name, RectTransform paper, string label)
        {
            var back = BuildButton(name, paper, label, 24);
            SetRect((RectTransform)back.transform, FooterBackMin, FooterBackMax);
            PaintPill(back, OptionsUiStyle.PaperVermilion, new Color(0.99f, 0.95f, 0.88f, 1f));
            return back;
        }

        private static void PaintPill(Button button, Color fill, Color text)
        {
            var img = button.GetComponent<Image>();
            img.sprite = ScrollUiKit.Rounded(26f);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = Color.Lerp(fill, Color.white, 0.15f);
            colors.selectedColor = Color.Lerp(fill, OptionsUiStyle.PaperFocus, 0.55f);
            colors.pressedColor = Color.Lerp(fill, Color.black, 0.15f);
            button.colors = colors;
            var outline = button.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
            var lbl = button.GetComponentInChildren<Text>();
            if (lbl != null) lbl.color = text;
        }

        // The page's title and what it is about, above its lines.
        internal static void PageSubtitle(string name, RectTransform section, string text)
        {
            var sub = BuildText(name, section, text, 17, TextAnchor.UpperLeft);
            SetRect(sub.rectTransform, new Vector2(0f, 0.835f), new Vector2(1f, 0.885f));
            sub.color = OptionsUiStyle.PaperInkSoft;
        }

        /// <summary>The pinned slip right of a page: a small heading and a note, or room for a preview.</summary>
        internal static RectTransform PageSlip(string name, RectTransform section, string heading, string body,
            float bottom = 0.45f)
        {
            var slip = ScrollUiKit.PaperTag(section, name, new Vector2(1.07f, bottom), new Vector2(1.55f, 0.92f), 0.8f);
            slip.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            var head = BuildText(name + "Heading", slip.rectTransform, heading.ToUpperInvariant(), 15, TextAnchor.UpperLeft);
            SetRect(head.rectTransform, new Vector2(0.07f, 0.84f), new Vector2(0.95f, 0.95f));
            head.color = OptionsUiStyle.PaperInkSoft;
            if (!string.IsNullOrEmpty(body))
            {
                var note = BuildText(name + "Body", slip.rectTransform, body, 18, TextAnchor.UpperLeft);
                SetRect(note.rectTransform, new Vector2(0.07f, 0.06f), new Vector2(0.95f, 0.82f));
                note.resizeTextForBestFit = true;
                note.resizeTextMinSize = 12;
                note.resizeTextMaxSize = 18;
                note.verticalOverflow = VerticalWrapMode.Truncate;
            }
            return slip.rectTransform;
        }

        /// <summary>A label on the left of a line.</summary>
        internal static Text LineLabel(string name, RectTransform section, int line, string text)
        {
            var label = BuildText(name, section, text, 21, TextAnchor.MiddleLeft);
            SetRect(label.rectTransform, LineMin(line), LineMax(line, LabelEnd));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 21;
            return label;
        }

        // A thin ink rule under a line, like the mockup's ledger.
        internal static void LineRule(RectTransform section, int line)
        {
            var y = LineTop - line * LineStep - LineHeight - (LineStep - LineHeight) * 0.5f;
            ScrollUiKit.Line(section, "Rule_" + line, new Vector2(0f, y), new Vector2(1f, y),
                new Color(0.33f, 0.29f, 0.24f, 0.18f), 1.5f);
        }

        // [REQ: UI-STYLE-017] A value as a brush stroke with a stone: label | ‹ | stroke | › | value in vermilion.
        private static ArrowRow BuildPaperValueRow(string id, string label, RectTransform parent, Vector2 aMin, Vector2 aMax,
            float initValue, float min, float max, float step, Action<float> setter)
        {
            float X(float t) => aMin.x + t * (aMax.x - aMin.x);
            var current = initValue;
            string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";

            var lbl = BuildText(id + "Label", parent, label, 21, TextAnchor.MiddleLeft);
            SetRect(lbl.rectTransform, new Vector2(X(0f), aMin.y), new Vector2(X(LabelEnd), aMax.y));
            lbl.resizeTextForBestFit = true;
            lbl.resizeTextMinSize = 13;
            lbl.resizeTextMaxSize = 21;

            var dec = BuildButton(id + "Dec", parent, "‹", 30);
            SetRect((RectTransform)dec.transform, new Vector2(X(ControlStart), aMin.y), new Vector2(X(0.44f), aMax.y));
            var inc = BuildButton(id + "Inc", parent, "›", 30);
            SetRect((RectTransform)inc.transform, new Vector2(X(0.83f), aMin.y), new Vector2(X(0.89f), aMax.y));
            foreach (var arrow in new[] { dec, inc }) MakeQuiet(arrow);

            var bar = EnsureRect(id + "Bar", parent, new Vector2(X(0.45f), aMin.y), new Vector2(X(0.82f), aMax.y),
                Vector2.zero, Vector2.zero);
            ScrollUiKit.Fill(bar, "Track", new Vector2(0f, 0.47f), new Vector2(1f, 0.53f), new Color(0.33f, 0.29f, 0.24f, 0.28f));
            var stroke = ScrollUiKit.Fill(bar, "Stroke", new Vector2(0f, 0.36f), new Vector2(0.5f, 0.64f), OptionsUiStyle.PaperInk,
                ScrollUiKit.Rounded(6f));
            var stone = ScrollUiKit.Spot(bar, "Stone", new Vector2(0.5f, 0.5f), new Vector2(30f, 30f), new Color(0.72f, 0.71f, 0.67f, 1f),
                ScrollUiKit.Disc());
            var stoneShadow = stone.gameObject.AddComponent<Shadow>();
            stoneShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            stoneShadow.effectDistance = new Vector2(1.5f, -2.5f);

            var val = BuildText(id + "Val", parent, Percent(initValue), 21, TextAnchor.MiddleRight);
            SetRect(val.rectTransform, new Vector2(X(0.89f), aMin.y), new Vector2(X(1f), aMax.y));
            val.color = OptionsUiStyle.PaperVermilion;

            void Show(float v)
            {
                var t = Mathf.Approximately(max, min) ? 0f : Mathf.Clamp01((v - min) / (max - min));
                stroke.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, t), 0.64f);
                stone.rectTransform.anchorMin = stone.rectTransform.anchorMax = new Vector2(t, 0.5f);
                val.text = Percent(v);
            }

            void Step(float delta)
            {
                current = Mathf.Clamp(Mathf.Round((current + delta) / step) * step, min, max);
                setter(current);
                Show(current);
            }

            dec.onClick.RemoveAllListeners();
            dec.onClick.AddListener(() => Step(-step));
            inc.onClick.RemoveAllListeners();
            inc.onClick.AddListener(() => Step(step));
            Show(initValue);
            return new ArrowRow(dec, inc, val, v =>
            {
                current = v;
                Show(v);
            });
        }

        // A chevron without a fill; the focus still shows (controller).
        private static void MakeQuiet(Button button)
        {
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(0.97f, 0.83f, 0.52f, 0.35f);
            colors.selectedColor = OptionsUiStyle.PaperFocus;
            button.colors = colors;
            button.GetComponent<Image>().color = Color.white;
            var outline = button.GetComponent<Outline>();
            if (outline != null) outline.effectColor = Color.clear;
            // The chevron uses the whole button; the padding of a normal button clipped it.
            var label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.fontSize = 34;
            label.color = OptionsUiStyle.PaperInk;
        }

        // [REQ: UI-STYLE-017] A switch as a hanko stamp: label | an ink-framed square that is stamped vermilion when on.
        private static void PaintPaperToggle(RectTransform row, Toggle toggle, Image bg, Image box, Image check, Text label)
        {
            bg.color = Color.white;
            var colors = toggle.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(0.97f, 0.83f, 0.52f, 0.25f);
            colors.selectedColor = new Color(0.97f, 0.83f, 0.52f, 0.45f);
            colors.pressedColor = new Color(0.97f, 0.83f, 0.52f, 0.55f);
            toggle.colors = colors;

            var boxRt = box.rectTransform;
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(ControlStart, 0.5f);
            boxRt.pivot = new Vector2(0f, 0.5f);
            boxRt.sizeDelta = new Vector2(44f, 44f);
            boxRt.anchoredPosition = Vector2.zero;
            box.color = new Color(1f, 1f, 1f, 0f);
            var oldOutline = box.GetComponent<Outline>();
            if (oldOutline != null) oldOutline.effectColor = Color.clear;
            ScrollUiKit.InkFrame(boxRt, OptionsUiStyle.PaperVermilion, 2.5f);

            check.sprite = ScrollUiKit.Rounded(6f);
            check.type = Image.Type.Sliced;
            check.color = OptionsUiStyle.PaperVermilion;
            var checkRt = check.rectTransform;
            checkRt.anchorMin = new Vector2(0.08f, 0.08f);
            checkRt.anchorMax = new Vector2(0.92f, 0.92f);

            label.color = OptionsUiStyle.PaperInk;
            label.fontSize = 21;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 21;
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(LabelEnd, 1f));
            row.SetAsLastSibling();
        }

        /// <summary>A short note right of a switch (what it does).</summary>
        private static void ToggleNote(Toggle toggle, string note)
        {
            if (string.IsNullOrEmpty(note)) return;
            var text = BuildText("Note", (RectTransform)toggle.transform, note, 16, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, new Vector2(0.46f, 0f), new Vector2(1f, 1f));
            text.color = OptionsUiStyle.PaperInkSoft;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = 16;
        }

        /// <summary>"Reset this page" for a page, placed under the scroll beside Back (only the shown page's is visible).</summary>
        private static Button PaperResetButton(string name, RectTransform section, OptionsController optCtrl, Action reset)
        {
            var btn = ResetButton(name, section, optCtrl, reset);
            var label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = T("Options.ResetPage");
                label.fontSize = 22;
            }
            // Screen → section coordinates (the section is SectionMin..SectionMax of the paper).
            Vector2 ToSection(Vector2 screen)
            {
                var paper = new Vector2((screen.x - PaperMin.x) / (PaperMax.x - PaperMin.x),
                    (screen.y - PaperMin.y) / (PaperMax.y - PaperMin.y));
                var sMin = OptionsUiStyle.SectionMin;
                var sMax = OptionsUiStyle.SectionMax;
                return new Vector2((paper.x - sMin.x) / (sMax.x - sMin.x), (paper.y - sMin.y) / (sMax.y - sMin.y));
            }
            SetRect((RectTransform)btn.transform, ToSection(ResetScreenMin), ToSection(ResetScreenMax));
            PaintPill(btn, new Color(0.17f, 0.13f, 0.09f, 0.92f), new Color(0.97f, 0.92f, 0.80f, 1f));
            return btn;
        }
    }
}
