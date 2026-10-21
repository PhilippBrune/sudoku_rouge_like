using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Builds the Options controls. The main-menu Options (MainMenuBlueprintBuilder) and the in-run Options
    /// (InRunUiBlueprintBuilder) both call <see cref="BuildStandardTabs"/>, so they have the same tabs, controls, look and
    /// D-pad navigation (GAP-029, FUTURE-015). The widget builders also serve the main menu's other panels.
    /// </summary>
    // [REQ: UI-OPT-001]
    public static partial class OptionsUiFactory
    {
        public sealed class Setup
        {
            /// <summary>Prefix of every object name: "" in the main menu, "IG" in a run.</summary>
            public string Prefix = "";

            /// <summary>Runs after a new language is applied. Null: the Display tab shows a hint instead of the
            /// language row (the in-run UI cannot rebuild itself in the middle of a puzzle).</summary>
            public Action OnLanguageApplied;

            /// <summary>Extra reaction to Highlight Errors, e.g. repainting the board during a run.</summary>
            public Action<bool> OnHighlightConflictsChanged;

            /// <summary>The Accessibility page also has Text Size and the preview board (main menu).</summary>
            public bool AccessibilityPreview;

            /// <summary>Adds pages between Accessibility and Language (the main menu's Controls page).</summary>
            public Action<OptionsTabSet> AddPagesBeforeLanguage;

            /// <summary>Set by BuildStandardTabs: the Accessibility page's refresh (text size, preview).</summary>
            public Action AccessibilityRefresh;
        }

        public readonly struct ArrowRow
        {
            public ArrowRow(Button dec, Button inc, Text value, Action<float> refresh)
            {
                Dec = dec;
                Inc = inc;
                Value = value;
                Refresh = refresh;
            }

            public Button Dec { get; }
            public Button Inc { get; }
            public Text Value { get; }
            public Action<float> Refresh { get; }
        }

        private const float DropdownItemHeight = 32f;

        private static string T(string key) => LocalizationService.T(key);

        private static Selectable[] Row(params Selectable[] items) => items;

        // ------------------------------------------------------------------------
        // Options pages: Audio, Display, Gameplay, Accessibility, [extra pages], Language
        // ------------------------------------------------------------------------

        /// <summary>
        /// Adds the Audio, Display, Gameplay and Accessibility pages, then <see cref="Setup.AddPagesBeforeLanguage"/>, then
        /// Language, each with its controls, "Reset this page", slip and navigation rows (UI-STYLE-017: one setting per
        /// line on the scroll). Returns the refresh that re-reads all controls from <paramref name="optCtrl"/>; register it
        /// with <c>OptionsController.OnOptionsChanged</c> (resets and profile switches raise it). The Accessibility page's
        /// own refresh is also returned in <see cref="Setup.AccessibilityRefresh"/>.
        /// </summary>
        public static Action BuildStandardTabs(OptionsTabSet tabs, OptionsController optCtrl, Setup setup)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            setup ??= new Setup();
            var p = setup.Prefix ?? "";
            var refreshAudio = BuildAudioPage(tabs, optCtrl, p);
            var refreshDisplay = BuildDisplayPage(tabs, optCtrl, p);
            var refreshGameplay = BuildGameplayPage(tabs, optCtrl, setup, p);

            var accIdx = tabs.Count;
            var accSec = tabs.Add(p + "TabAccessibility", p + "AccessibilitySection", T("Accessibility"));
            setup.AccessibilityRefresh = BuildAccessibilitySection(tabs, accIdx, accSec, optCtrl, p, setup.AccessibilityPreview);
            setup.AddPagesBeforeLanguage?.Invoke(tabs);
            var refreshLanguage = BuildLanguagePage(tabs, optCtrl, setup, p);

            var refreshAccessibility = setup.AccessibilityRefresh;
            return () =>
            {
                refreshAudio();
                refreshDisplay();
                refreshGameplay();
                refreshAccessibility?.Invoke();
                refreshLanguage();
            };
        }

        private static int ResolutionIndex(Resolution[] resolutions, GraphicsSettingsModel graphics)
        {
            for (var i = 0; i < resolutions.Length; i++)
                if (resolutions[i].width == graphics.ResolutionWidth && resolutions[i].height == graphics.ResolutionHeight)
                    return i;
            return 0;
        }

        public static Text SectionHeader(string name, RectTransform section, string label)
        {
            var header = BuildText(name, section, label, OptionsUiStyle.HeaderSize, TextAnchor.MiddleLeft);
            if (OptionsUiStyle.Paper) SetRect(header.rectTransform, new Vector2(0f, 0.89f), new Vector2(1f, 1.00f));
            else SetRect(header.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.75f, 1.00f));
            return header;
        }

        private static Button ResetButton(string name, RectTransform section, OptionsController optCtrl, Action reset)
        {
            var btn = BuildButton(name, section, T("Reset"), 11);
            SetRect(btn.GetComponent<RectTransform>(), new Vector2(0.78f, 0.90f), new Vector2(0.96f, 0.99f));
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                reset();
                optCtrl.NotifyOptionsChangedForUiRefresh();
            });
            return btn;
        }

        // OPT-1: [<] [value%] [>] button row instead of a Unity Slider (controller-friendly).
        public static ArrowRow BuildVolumeArrowRow(string id, string label,
            RectTransform parent, Vector2 aMin, Vector2 aMax, float initValue, Action<float> setter)
        {
            if (OptionsUiStyle.Paper)
                return BuildPaperValueRow(id, label, parent, aMin, aMax, initValue, 0f, 1f, 0.05f, setter);
            var current = initValue;

            string FormatPercent(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
            string FormatVolumeLabel(float value) => label + " " + FormatPercent(value);

            var lbl = BuildText(id + "Label", parent, FormatVolumeLabel(initValue), 14, TextAnchor.MiddleLeft);
            SetRect(lbl.rectTransform, new Vector2(aMin.x, aMin.y), new Vector2(aMin.x + 0.28f, aMax.y));

            var btnDec = BuildButton(id + "Dec", parent, "<", 16);
            SetRect(btnDec.GetComponent<RectTransform>(),
                new Vector2(aMin.x + 0.29f, aMin.y), new Vector2(aMin.x + 0.35f, aMax.y));

            var valTxt = BuildText(id + "Val", parent, FormatPercent(initValue), 13, TextAnchor.MiddleCenter);
            SetRect(valTxt.rectTransform, new Vector2(aMin.x + 0.35f, aMin.y), new Vector2(aMax.x - 0.06f, aMax.y));
            valTxt.color = GamePalette.TextMuted;

            var btnInc = BuildButton(id + "Inc", parent, ">", 16);
            SetRect(btnInc.GetComponent<RectTransform>(), new Vector2(aMax.x - 0.06f, aMin.y), aMax);

            void Step(float delta)
            {
                current = Mathf.Clamp(Mathf.Round((current + delta) * 20f) / 20f, 0f, 1f);
                setter(current);
                lbl.text = FormatVolumeLabel(current);
                valTxt.text = FormatPercent(current);
            }

            btnDec.onClick.RemoveAllListeners();
            btnDec.onClick.AddListener(() => Step(-0.05f));
            btnInc.onClick.RemoveAllListeners();
            btnInc.onClick.AddListener(() => Step(0.05f));

            return new ArrowRow(btnDec, btnInc, valTxt, v =>
            {
                current = v;
                lbl.text = FormatVolumeLabel(v);
                valTxt.text = FormatPercent(v);
            });
        }

        // ------------------------------------------------------------------------
        // Widgets (also used by the main menu's other panels)
        // ------------------------------------------------------------------------

        public static Text BuildText(string name, RectTransform parent, string value, int size, TextAnchor anchor)
        {
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = EnsureComponent<Text>(rect.gameObject);
            text.text = value;
            text.font = InRunUiFactory.GetFont();
            text.fontSize = size;
            text.alignment = anchor;
            text.color = OptionsUiStyle.Text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button BuildButton(string name, RectTransform parent, string label, int size)
        {
            var btnColor = OptionsUiStyle.Button;
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = EnsureImage(rect.gameObject, btnColor);
            img.raycastTarget = true;

            var btn = EnsureComponent<Button>(rect.gameObject);
            var outline = EnsureComponent<Outline>(rect.gameObject);
            outline.effectColor = OptionsUiStyle.Paper ? new Color(0.13f, 0.11f, 0.09f, 0.35f) : GamePalette.AccentGoldGlow;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var colors = btn.colors;
            colors.normalColor      = btnColor;
            colors.highlightedColor = new Color(btnColor.r, btnColor.g, btnColor.b, 0.30f);
            colors.pressedColor     = new Color(btnColor.r - 0.05f, btnColor.g - 0.05f, btnColor.b - 0.05f, 0.48f);
            colors.selectedColor    = OptionsUiStyle.Paper ? OptionsUiStyle.PaperFocus
                : new Color(0.98f, 0.83f, 0.26f, 0.70f); // gold tint when controller-focused
            colors.disabledColor    = GamePalette.DisabledGray;
            colors.fadeDuration     = 0.08f;
            colors.colorMultiplier  = 1f;
            btn.colors = colors;

            // Enable automatic navigation so EventSystem can traverse with stick/d-pad
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            var lblRect = EnsureRect("Label", rect, Vector2.zero, Vector2.one,
                new Vector2(10f, 8f), new Vector2(-10f, -8f));
            var text = EnsureComponent<Text>(lblRect.gameObject);
            text.text = label;
            text.font = InRunUiFactory.GetFont();
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = OptionsUiStyle.ButtonText;

            lblRect.SetAsLastSibling();
            return btn;
        }

        public static Toggle BuildToggle(string name, RectTransform parent, string label)
        {
            var row = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.sizeDelta = new Vector2(260f, 32f);

            var bg = EnsureImage(row.gameObject, GamePalette.WithAlpha(OptionsUiStyle.Button, 0.85f));
            var toggle = EnsureComponent<Toggle>(row.gameObject);

            // [REQ: REQ-UI-103] GAP-029: an always-visible box, so "off" reads as an empty box and "on" as a filled one.
            var box = EnsureRect("Box", row,
                new Vector2(0.02f, 0.12f), new Vector2(0.10f, 0.88f), Vector2.zero, Vector2.zero);
            EnsureImage(box.gameObject, new Color(0.02f, 0.02f, 0.02f, 0.90f)).raycastTarget = false;
            var boxOutline = EnsureComponent<Outline>(box.gameObject);
            boxOutline.effectColor = OptionsUiStyle.Accent;
            boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var check = EnsureRect("Checkmark", box,
                new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
            var checkImg = EnsureImage(check.gameObject, OptionsUiStyle.Accent);
            checkImg.raycastTarget = false;

            var text = BuildText("Label", row, label, 15, TextAnchor.MiddleLeft);
            text.color = OptionsUiStyle.ButtonText;
            SetRect(text.rectTransform, new Vector2(0.14f, 0f), new Vector2(0.98f, 1f));

            toggle.targetGraphic = bg;
            toggle.graphic = checkImg;
            if (OptionsUiStyle.Paper)
                PaintPaperToggle(row, toggle, bg, box.GetComponent<Image>(), checkImg, text);
            return toggle;
        }

        public static Toggle BuildOptionToggle(RectTransform parent, string id, string label,
            Vector2 anchorMin, Vector2 anchorMax, bool value, UnityAction<bool> onChanged, string note = null)
        {
            var tgl = BuildToggle(id + "Toggle", parent, label);
            SetRect(tgl.GetComponent<RectTransform>(), anchorMin, anchorMax);
            tgl.SetIsOnWithoutNotify(value);
            tgl.onValueChanged.RemoveAllListeners();
            tgl.onValueChanged.AddListener(onChanged);
            if (OptionsUiStyle.Paper) ToggleNote(tgl, note);
            return tgl;
        }

        public static Dropdown BuildDropdown(string name, RectTransform parent)
        {
            var btnColor = OptionsUiStyle.Button;
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureImage(rect.gameObject, btnColor);
            var dropdown = EnsureComponent<Dropdown>(rect.gameObject);

            var label = BuildText("Label", rect, T("GeneratedUi.Dropdown.Select"), OptionsUiStyle.Paper ? 20 : 16, TextAnchor.MiddleLeft);
            label.color = OptionsUiStyle.ButtonText;
            SetRect(label.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.76f, 0.9f));

            var arrowRect = EnsureRect("Arrow", rect,
                new Vector2(0.82f, 0.15f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
            var arrowText = EnsureComponent<Text>(arrowRect.gameObject);
            arrowText.font = InRunUiFactory.GetFont();
            arrowText.text = "▼";
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = OptionsUiStyle.ButtonText;
            arrowText.fontSize = 16;

            // Template
            var template = EnsureRect("Template", rect,
                Vector2.zero, new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 180f);
            // Opaque: the options underneath showed through the open list (1.1 display-mode check).
            EnsureImage(template.gameObject, new Color(btnColor.r, btnColor.g, btnColor.b, 1f));
            template.gameObject.SetActive(false);

            var viewport = EnsureRect("Viewport", template,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureImage(viewport.gameObject, OptionsUiStyle.Paper ? new Color(0.94f, 0.90f, 0.81f, 1f) : GamePalette.ScrollBgDark);
            EnsureComponent<Mask>(viewport.gameObject).showMaskGraphic = false;

            var content = EnsureRect("Content", viewport,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            // One item tall: Unity measures the item's offsets against this rect when it opens the list; at 0 px
            // it subtracted one item height and the last option was hidden.
            content.sizeDelta = new Vector2(0f, DropdownItemHeight);
            var cl = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            cl.childAlignment = TextAnchor.UpperCenter;
            cl.childControlHeight = true;
            cl.childControlWidth = true;
            cl.childForceExpandHeight = false;
            cl.childForceExpandWidth = true;
            // Unity sizes the list as item height x item count, so the layout adds no spacing or padding;
            // before, the list was shorter than its items and the last option was cut off.
            cl.spacing = 0f;
            cl.padding = new RectOffset(0, 0, 0, 0);
            var cf = EnsureComponent<ContentSizeFitter>(content.gameObject);
            cf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Item template
            var item = EnsureRect("Item", content,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -DropdownItemHeight), Vector2.zero);
            var itemLayout = EnsureComponent<LayoutElement>(item.gameObject);
            itemLayout.minHeight = DropdownItemHeight;
            itemLayout.preferredHeight = DropdownItemHeight;
            var itemBg = EnsureImage(item.gameObject, OptionsUiStyle.Paper ? new Color(0.94f, 0.90f, 0.81f, 1f) : GamePalette.ListItemBg);
            var itemTgl = EnsureComponent<Toggle>(item.gameObject);
            itemTgl.targetGraphic = itemBg;

            var itemCheck = EnsureRect("Item Checkmark", item,
                new Vector2(0.02f, 0.18f), new Vector2(0.08f, 0.82f), Vector2.zero, Vector2.zero);
            var itemCheckImg = EnsureImage(itemCheck.gameObject, OptionsUiStyle.Accent);
            itemTgl.graphic = itemCheckImg;

            var itemLabel = BuildText("Item Label", item, T("GeneratedUi.Dropdown.Option"), 16, TextAnchor.MiddleLeft);
            itemLabel.color = OptionsUiStyle.ButtonText;
            SetRect(itemLabel.rectTransform, new Vector2(0.10f, 0f), new Vector2(0.98f, 1f));

            AttachDropdownItemCompat(item.gameObject, item, itemLabel, itemBg, itemTgl);

            var scroll = EnsureComponent<ScrollRect>(template.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            dropdown.captionText = label;
            dropdown.template = template;
            dropdown.itemText = itemLabel;
            dropdown.itemImage = null;
            dropdown.targetGraphic = EnsureImage(rect.gameObject, btnColor);
            dropdown.captionText.raycastTarget = false;
            dropdown.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            return dropdown;
        }

        /// <summary>
        /// Resizes the dropdown template so all items are visible without scrolling (GAP-042).
        /// Call this after AddOptions(). Capped at 300px to stay on screen.
        /// </summary>
        public static void FitDropdownToItems(Dropdown dd)
        {
            if (dd == null || dd.template == null || dd.options.Count == 0) return;
            var h = Mathf.Min(dd.options.Count * DropdownItemHeight, 300f);
            var sd = dd.template.sizeDelta;
            dd.template.sizeDelta = new Vector2(sd.x, h);
        }

        // ------------------------------------------------------------------------
        // Core helpers
        // ------------------------------------------------------------------------

        /// <summary>Finds or creates the child <paramref name="name"/>, so a rebuild reuses the existing objects.</summary>
        public static RectTransform EnsureRect(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                child = go.GetComponent<RectTransform>();
            }
            child.anchorMin = anchorMin;
            child.anchorMax = anchorMax;
            child.offsetMin = offsetMin;
            child.offsetMax = offsetMax;
            return child;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static TComponent EnsureComponent<TComponent>(GameObject go) where TComponent : Component
        {
            var existing = go.GetComponent<TComponent>();
            return existing != null ? existing : go.AddComponent<TComponent>();
        }

        public static Image EnsureImage(GameObject go, Color color)
        {
            var img = EnsureComponent<Image>(go);
            img.color = color;
            return img;
        }

        // ------------------------------------------------------------------------
        // Helpers: Dropdown Compatibility
        // ------------------------------------------------------------------------
    }
}
