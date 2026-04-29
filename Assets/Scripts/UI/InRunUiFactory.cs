using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    internal enum UiAction
    {
        Back,
        Confirm,
        Accept,
        Cancel,
        Decline,
        Skip,
        Leave,
        Start,
        Begin,
        SaveQuit,
        Reroll,
        Buy,
        Heal,
        Pencil,
        Cleanse,
        WardingFlame,
        Next,
        Finish
    }

    /// <summary>
    /// Shared static factory helpers for all in-run sub-controllers.
    /// </summary>
    internal static class InRunUiFactory
    {
        // ── In-run panel/button color scheme ──
        internal static readonly Color PanelBg           = new Color(0.08f, 0.12f, 0.16f, 0.94f);
        internal static readonly Color BtnColor          = new Color(0.15f, 0.22f, 0.29f, 0.94f);
        internal static readonly Color AccentGold        = GamePalette.AccentGold;
        internal static readonly Color TextColor         = GamePalette.TextPrimary;

        // Cursed / boss gate panels
        internal static readonly Color CursedPanelBg    = new Color(0.18f, 0.06f, 0.06f, 0.96f);
        internal static readonly Color CursedTitleRed   = new Color(0.90f, 0.30f, 0.20f, 1f);
        internal static readonly Color WarmIvory        = new Color(0.95f, 0.85f, 0.75f, 1f);

        // HUD modifier panels
        internal static readonly Color ModBannerBg      = new Color(0.15f, 0.10f, 0.25f, 0.85f);
        internal static readonly Color ModInfoBoxBg     = new Color(0.10f, 0.14f, 0.18f, 0.90f);

        // Bag panel
        internal static readonly Color BagPanelBg          = new Color(0.06f, 0.10f, 0.12f, 0.70f);
        internal static readonly Color BagSlotBg            = new Color(0.10f, 0.16f, 0.20f, 0.80f);
        internal static readonly Color BagSlotHighlight     = new Color(0.17f, 0.26f, 0.33f, 0.90f);
        internal static readonly Color BagSlotPressed       = new Color(0.07f, 0.11f, 0.14f, 0.90f);
        internal static readonly Color BagSlotDisabled      = new Color(0.06f, 0.09f, 0.11f, 0.45f);
        internal static readonly Color BagHighlightSelected = new Color(
            GamePalette.AccentGold.r * 0.5f, GamePalette.AccentGold.g * 0.5f, 0.10f, 0.90f);
        internal static readonly Color PassiveLabelColor    = new Color(0.78f, 0.85f, 0.75f, 0.80f);

        // Reward / rest / relic / swap panels
        internal static readonly Color SwapPanelBg         = new Color(0.07f, 0.10f, 0.14f, 0.97f);
        internal static readonly Color RestRelicPanelBg    = new Color(0.08f, 0.12f, 0.10f, 0.95f);

        // Icon / slot states
        internal static readonly Color IconNoSprite  = new Color(1f, 1f, 1f, 0.20f);
        /// <summary>
        /// F12: <c>icon_empty_slot</c> (economy/icon_empty_slot.png) is displayed in a bag panel slot
        /// when no item currently occupies that slot. It is a passive placeholder — never interactive.
        /// This color (near-transparent white) tints it down so it doesn't compete with filled slots.
        /// </summary>
        internal static readonly Color IconEmpty     = new Color(1f, 1f, 1f, 0.08f);
        internal static readonly Color SlotEmptyText = new Color(0.45f, 0.45f, 0.45f, 0.55f);

        // Unknown boss-mod icon placeholder
        internal static readonly Color PlaceholderIconBg  = new Color(0.30f, 0.30f, 0.30f, 0.40f);
        internal static readonly Color UnknownIconText    = new Color(0.70f, 0.70f, 0.70f, 0.90f);

        // Numpad button states
        internal static readonly Color NumpadDisabled    = new Color(0.13f, 0.13f, 0.13f, 0.80f);
        internal static readonly Color NumpadCompleted   = new Color(0.10f, 0.10f, 0.10f, 0.50f);
        internal static readonly Color NumpadActive      = new Color(0.19f, 0.30f, 0.20f, 1.00f);
        internal static readonly Color NumpadPencilActive = new Color(0.31f, 0.43f, 0.28f, 1.00f);

        // Scroll lane backgrounds (used in InRunUiBlueprintBuilder)
        internal static readonly Color LaneScrollBg   = new Color(0f, 0f, 0f, 0.12f);
        internal static readonly Color LaneViewportBg = new Color(1f, 1f, 1f, 0.02f);

        // Button interaction states — used in CreatePanelButton and all overlay controllers
        internal static readonly Color BtnHighlighted  = new Color(0.55f, 0.50f, 0.38f, 1f);
        internal static readonly Color BtnSelected     = new Color(0.98f, 0.83f, 0.26f, 0.70f);
        internal static readonly Color BtnPressed      = new Color(0.30f, 0.26f, 0.18f, 1f);
        internal static readonly Color BtnDisabled     = new Color(0.30f, 0.30f, 0.35f, 0.90f);
        internal static readonly Color RerollFreeColor = new Color(0.15f, 0.55f, 0.40f, 0.95f);

        // ── Font ──
        // V2: place a .ttf at Assets/Resources/Fonts/MainFont.ttf to use a custom font.
        // Falls back to the Unity built-in LegacyRuntime/Arial if absent.
        private const string CustomFontPath = "Fonts/MainFont";
        private static Font _font;
        internal static Font GetFont()
        {
            if (_font == null)
                _font = Resources.Load<Font>(CustomFontPath)
                     ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        // ── Text ──
        internal static Text CreateText(Transform parent, string name, string content,
            int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = GetFont();
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            return t;
        }

        // ── Layout helpers ──
        internal static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void ClearChildren(RectTransform root)
        {
            if (root == null) return;
            for (var i = root.childCount - 1; i >= 0; i--)
                Object.Destroy(root.GetChild(i).gameObject);
        }

        internal static void ClearNamedChildren(Transform parent, string prefix)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                    Object.Destroy(child.gameObject);
            }
        }

        internal static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        internal static void HidePanel(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
        }

        // ── Bar sprite ──
        internal static void EnsureBarSprite(Image fill)
        {
            if (fill.sprite != null) return;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) tex.SetPixel(x, y, Color.white);
            tex.Apply();
            fill.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
        }

        // ── Border helper ──
        internal static Image CreateBorder(Transform parent, string name,
            Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax; rt.offsetMin = omin; rt.offsetMax = omax;
            var img = go.GetComponent<Image>();
            img.color = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0);
            img.raycastTarget = false;
            return img;
        }

        // ── RectTransform copy ──
        internal static void CopyRectTransform(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, src.rect.width);
            dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, src.rect.height);
        }

        // ── Panel button ──
        internal static Button CreatePanelButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = BtnColor;

            var t = CreateText(go.transform, "Label", label, 12, TextAnchor.MiddleCenter, TextColor);
            StretchFill(t.rectTransform);
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var btn = go.GetComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var cols = btn.colors;
            cols.highlightedColor = BtnHighlighted;
            cols.selectedColor    = BtnSelected;
            cols.pressedColor     = BtnPressed;
            btn.colors = cols;
            return btn;
        }

        internal static Button CreateActionButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label, UiAction action, bool compact = false)
        {
            var btn = CreatePanelButton(parent, name, anchorMin, anchorMax, label);
            ApplyActionIcon(btn, action, compact);
            return btn;
        }

        internal static void ApplyActionIcon(Button btn, UiAction action, bool compact = false)
        {
            var iconRef = GetActionIcon(action);
            SetButtonIcon(btn, iconRef.Name, false, iconRef.Subfolder, compact);
        }

        private static ActionIconRef GetActionIcon(UiAction action)
        {
            switch (action)
            {
                case UiAction.Back:
                    return new ActionIconRef("back_leaf", "ui");
                case UiAction.Confirm:
                case UiAction.Accept:
                    return new ActionIconRef("confirm_seal", "ui");
                case UiAction.Cancel:
                case UiAction.Decline:
                    return new ActionIconRef("gate_close", "ui");
                case UiAction.Skip:
                case UiAction.Leave:
                    return new ActionIconRef("skip_stone", "ui");
                case UiAction.Start:
                case UiAction.Begin:
                    return new ActionIconRef("start_game", "ui");
                case UiAction.SaveQuit:
                    return new ActionIconRef("ink_save", "ui");
                case UiAction.Reroll:
                    return new ActionIconRef("reroll_arrow", "ui");
                case UiAction.Buy:
                    return new ActionIconRef("trade_coin", "ui");
                case UiAction.Heal:
                    return new ActionIconRef("petal_orb", "ui");
                case UiAction.Pencil:
                    return new ActionIconRef("ink_save", "ui");
                case UiAction.Cleanse:
                    return new ActionIconRef("flame_stone", "ui");
                case UiAction.WardingFlame:
                    return new ActionIconRef("warding_flame", "relic");
                case UiAction.Next:
                    return new ActionIconRef("resume_scroll", "ui");
                case UiAction.Finish:
                    return new ActionIconRef("start_game", "ui");
                default:
                    return new ActionIconRef("confirm_seal", "ui");
            }
        }

        private struct ActionIconRef
        {
            internal readonly string Name;
            internal readonly string Subfolder;

            internal ActionIconRef(string name, string subfolder)
            {
                Name = name;
                Subfolder = subfolder;
            }
        }

        /// <summary>
        /// Sets EventSystem focus to the first interactable, active button in <paramref name="panel"/>.
        /// Safe to call with a null panel or when EventSystem is absent.
        /// </summary>
        internal static void SelectFirstInteractable(GameObject panel)
        {
            if (panel == null) return;
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;
            var buttons = panel.GetComponentsInChildren<Button>(false);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].IsInteractable())
                {
                    es.SetSelectedGameObject(buttons[i].gameObject);
                    return;
                }
            }
        }

        // ── Overlay panel (reward / shop) ──
        internal static GameObject CreateOverlayPanel(Transform parent, string name, string title)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.20f, 0.18f);
            pr.anchorMax = new Vector2(0.80f, 0.78f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = PanelBg;

            var t = CreateText(panel.transform, "Title", title, 22, TextAnchor.MiddleCenter, AccentGold);
            t.rectTransform.anchorMin = new Vector2(0.06f, 0.86f);
            t.rectTransform.anchorMax = new Vector2(0.94f, 0.97f);
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = Vector2.zero;

            var summary = CreateText(panel.transform, "Summary", "", 14, TextAnchor.UpperLeft, TextColor);
            summary.rectTransform.anchorMin = new Vector2(0.08f, 0.58f);
            summary.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            summary.rectTransform.offsetMin = Vector2.zero;
            summary.rectTransform.offsetMax = Vector2.zero;

            return panel;
        }

        // ── Panel background art ──
        /// <summary>
        /// Inserts a RawImage background PNG as the first child of a panel (renders below all UI content).
        /// Pass an empty <paramref name="bgTextureName"/> to create a placeholder whose texture is set at runtime.
        /// </summary>
        internal static RawImage AddPanelBackground(Transform panelTransform, string bgTextureName, float alpha = 0.85f)
        {
            var bgGo = new GameObject("PanelBackground", typeof(RectTransform), typeof(RawImage));
            bgGo.transform.SetParent(panelTransform, false);
            bgGo.transform.SetAsFirstSibling();
            var rt = bgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var raw = bgGo.GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(bgTextureName))
            {
                var tex = Resources.Load<Texture2D>("background/" + bgTextureName);
                if (tex == null)
                    Debug.LogWarning($"[InRunUiFactory] Missing panel background: background/{bgTextureName}");
                raw.texture = tex;
                raw.color   = new Color(1f, 1f, 1f, tex != null ? alpha : 0f);
            }
            else
            {
                raw.color = Color.clear; // caller sets texture + color at runtime
            }
            raw.raycastTarget = false;
            return raw;
        }

        // ── Button icon ──
        internal sealed class ThemedModalShell
        {
            internal GameObject Root;
            internal RectTransform Panel;
        }

        internal static ThemedModalShell CreateThemedModalShell(Transform parent, string name,
            Vector2 panelAnchorMin, Vector2 panelAnchorMax, string backgroundName = null, float backgroundAlpha = 0.18f)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            StretchFill(rootRt);
            var scrim = root.GetComponent<Image>();
            scrim.color = new Color(0.02f, 0.025f, 0.03f, 0.76f);

            var panelGo = new GameObject("ThemedPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelGo.transform.SetParent(root.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = panelAnchorMin;
            panelRt.anchorMax = panelAnchorMax;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            StyleThemedPanel(panelGo, backgroundName, backgroundAlpha);

            return new ThemedModalShell { Root = root, Panel = panelRt };
        }

        internal static void StyleThemedPanel(GameObject panel, string backgroundName = null, float backgroundAlpha = 0.16f)
        {
            if (panel == null) return;
            var img = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
            img.color = new Color(0.15f, 0.13f, 0.10f, 0.96f);

            var outline = panel.GetComponent<Outline>() ?? panel.AddComponent<Outline>();
            outline.effectColor = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.58f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (!string.IsNullOrEmpty(backgroundName))
                AddPanelBackground(panel.transform, backgroundName, backgroundAlpha);
        }

        // compact=true: side-by-side layout (icon left, label right) for small/short buttons.
        // compact=false: stacked layout (icon top, label bottom) for tall standalone buttons.
        internal static void SetButtonIcon(Button btn, string iconName, bool unknown = false,
            string subfolder = "GeneratedIcons", bool compact = false)
        {
            if (btn == null) return;
            ClearNamedChildren(btn.transform, "Icon");

            var spritePath = (!unknown && !string.IsNullOrEmpty(iconName))
                ? subfolder + "/icon_" + iconName
                : null;
            var sprite = spritePath != null ? Resources.Load<Sprite>(spritePath) : null;
            if (sprite == null && !unknown && !string.IsNullOrEmpty(iconName))
                Debug.LogWarning($"[InRunUiFactory] Missing button icon: {spritePath}");

            var label = btn.transform.Find("Label")?.GetComponent<Text>();

            if (compact)
            {
                // Side-by-side: icon on left (anchor-based height), label fills the rest.
                if (label != null)
                {
                    label.rectTransform.anchorMin = new Vector2(0.25f, 0.05f);
                    label.rectTransform.anchorMax = new Vector2(0.98f, 0.95f);
                    label.rectTransform.offsetMin = Vector2.zero;
                    label.rectTransform.offsetMax = Vector2.zero;
                    label.fontSize = 11;
                    label.verticalOverflow = VerticalWrapMode.Overflow;
                }

                if (sprite == null && !unknown) return;

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(btn.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                // Anchor-based height (80% of button) + fixed width — scales with button.
                iconRt.anchorMin = new Vector2(0f, 0.10f);
                iconRt.anchorMax = new Vector2(0f, 0.90f);
                iconRt.pivot     = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(6f, 0f);
                iconRt.sizeDelta = new Vector2(36f, 0f);
                var img = iconGo.GetComponent<Image>();
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.preserveAspect = true;
                }
                else
                {
                    img.color = PlaceholderIconBg;
                    var qText = new GameObject("UnknownLabel", typeof(RectTransform), typeof(Text));
                    qText.transform.SetParent(iconGo.transform, false);
                    var qt = qText.GetComponent<Text>();
                    qt.text = "???";
                    qt.font = GetFont();
                    qt.fontSize = 16;
                    qt.alignment = TextAnchor.MiddleCenter;
                    qt.color = UnknownIconText;
                    var qRt = qText.GetComponent<RectTransform>();
                    qRt.anchorMin = Vector2.zero;
                    qRt.anchorMax = Vector2.one;
                    qRt.offsetMin = Vector2.zero;
                    qRt.offsetMax = Vector2.zero;
                }
                return;
            }

            // Stacked layout: reposition label to bottom, icon fills top portion.
            if (label != null)
            {
                label.rectTransform.anchorMin = new Vector2(0.02f, 0.02f);
                label.rectTransform.anchorMax = new Vector2(0.98f, 0.36f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.fontSize = 10;
                label.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (sprite == null && !unknown) return;

            var iconGoS = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGoS.transform.SetParent(btn.transform, false);
            var iconRtS = iconGoS.GetComponent<RectTransform>();
            iconRtS.anchorMin = new Vector2(0.08f, 0.40f);
            iconRtS.anchorMax = new Vector2(0.92f, 0.94f);
            iconRtS.offsetMin = Vector2.zero;
            iconRtS.offsetMax = Vector2.zero;
            var imgS = iconGoS.GetComponent<Image>();
            if (sprite != null)
            {
                imgS.sprite = sprite;
                imgS.preserveAspect = true;
            }
            else
            {
                imgS.color = PlaceholderIconBg;
                var qText = new GameObject("UnknownLabel", typeof(RectTransform), typeof(Text));
                qText.transform.SetParent(iconGoS.transform, false);
                var qt = qText.GetComponent<Text>();
                qt.text = "???";
                qt.font = GetFont();
                qt.fontSize = 22;
                qt.alignment = TextAnchor.MiddleCenter;
                qt.color = UnknownIconText;
                var qRt = qText.GetComponent<RectTransform>();
                qRt.anchorMin = Vector2.zero;
                qRt.anchorMax = Vector2.one;
                qRt.offsetMin = Vector2.zero;
                qRt.offsetMax = Vector2.zero;
            }

            // A-9B: LayoutElement ensures consistent minimum icon size across all button sizes
            var le = iconGoS.AddComponent<LayoutElement>();
            le.minWidth      = 32f;
            le.minHeight     = 32f;
            le.preferredWidth  = 48f;
            le.preferredHeight = 48f;
        }

        // ── Bag slot button ──
        internal static Button CreateBagSlotButton(Transform parent, string name, Vector2 ancMin, Vector2 ancMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin;
            rt.anchorMax = ancMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = BagSlotBg;

            var btn = go.GetComponent<Button>();
            var cols = btn.colors;
            cols.normalColor    = BagSlotBg;
            cols.highlightedColor = BagSlotHighlight;
            cols.pressedColor   = BagSlotPressed;
            cols.disabledColor  = BagSlotDisabled;
            btn.colors = cols;

            // Icon (left ~30%)
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.04f, 0.12f);
            iconRt.anchorMax = new Vector2(0.36f, 0.88f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var iconBtnImg = iconGo.GetComponent<Image>();
            iconBtnImg.raycastTarget = false;
            iconBtnImg.preserveAspect = true;  // L8: ensures empty-slot icon is never cut off

            // Name text (right of icon)
            var nameText = CreateText(go.transform, "NameText", "", 10, TextAnchor.MiddleLeft, TextColor);
            nameText.rectTransform.anchorMin = new Vector2(0.38f, 0.04f);
            nameText.rectTransform.anchorMax = new Vector2(0.98f, 0.96f);
            nameText.rectTransform.offsetMin = new Vector2(2f, 1f);
            nameText.rectTransform.offsetMax = new Vector2(-2f, -1f);
            nameText.supportRichText = true;

            return btn;
        }

        // ── Modifier name/description ──
        internal static string FormatModName(BossModifierId m) => m switch
        {
            BossModifierId.FogOfWar          => "Fog of War",
            BossModifierId.ArrowSums         => "Arrow Sums",
            BossModifierId.GermanWhispers    => "German Whispers",
            BossModifierId.DutchWhispers     => "Dutch Whispers",
            BossModifierId.ParityLines       => "Parity Lines",
            BossModifierId.RenbanLines       => "Renban Lines",
            BossModifierId.KillerCages       => "Killer Cages",
            BossModifierId.DifferenceKropki  => "Difference Kropki",
            BossModifierId.RatioKropki       => "Ratio Kropki",
            BossModifierId.Palindrome        => "Palindrome",
            BossModifierId.Thermo            => "Thermo",
            BossModifierId.BetweenLines      => "Between Lines",
            BossModifierId.EvenOdd           => "Even/Odd",
            BossModifierId.Nonconsecutive    => "Nonconsecutive",
            BossModifierId.Antiknight        => "Antiknight",
            BossModifierId.Antiking          => "Anti-King",
            BossModifierId.AntiBishop        => "Anti-Bishop",
            BossModifierId.NonconsecDiagonal => "Nonconsec. Diagonal",
            BossModifierId.DistanceGe2       => "Distance \u2265 2",
            BossModifierId.EntropyGlobal     => "Entropy",
            BossModifierId.ModularRegions    => "Modular Regions",
            BossModifierId.ConsecutiveLine   => "Consecutive Line",
            BossModifierId.SlowThermo        => "Slow Thermo",
            BossModifierId.UniqueSetLine     => "Unique Set Line",
            BossModifierId.FullKropki        => "Full Kropki",
            BossModifierId.SumKropki         => "Sum Dot",
            BossModifierId.GreaterLessThan   => "Greater/Less Than",
            BossModifierId.XVPairs           => "XV Pairs",
            BossModifierId.PrimeCells        => "Prime Cells",
            BossModifierId.FortressCells     => "Fortress Cells",
            BossModifierId.RowWipe           => "Row Wipe",
            BossModifierId.ColWipe           => "Column Wipe",
            BossModifierId.DoublePenalty     => "Double Penalty",
            BossModifierId.CellLock          => "Cell Lock",
            BossModifierId.PencilBlind       => "Pencil Blind",
            BossModifierId.BoxWipe           => "Box Wipe",
            BossModifierId.CrossWipe         => "Cross Wipe",
            BossModifierId.PencilDrain       => "Pencil Drain",
            BossModifierId.GoldFine          => "Gold Fine",
            BossModifierId.CountdownFill     => "Countdown Fill",
            BossModifierId.HauntedCell       => "Haunted Cell",
            BossModifierId.CrumblingRegion   => "Crumbling Region",
            BossModifierId.PressureWave      => "Pressure Wave",
            _                                => m.ToString()
        };

        internal static string GetModDesc(BossModifierId m) => m switch
        {
            BossModifierId.FogOfWar          => "Correct placements reveal hidden cells.",
            BossModifierId.ArrowSums         => "Arrow circles: digits on the arrow sum to the number in the circle.",
            BossModifierId.GermanWhispers    => "Green line: neighbours on the line must differ by 5 or more.",
            BossModifierId.DutchWhispers     => "Teal line: neighbours on the line must differ by 4 or more.",
            BossModifierId.ParityLines       => "Cyan line: adjacent cells must strictly alternate between odd and even.",
            BossModifierId.RenbanLines       => "Pink line: all digits on the line form a consecutive set (any order).",
            BossModifierId.KillerCages       => "Dashed cage: digits sum to the cage label; no repeats inside the cage.",
            BossModifierId.DifferenceKropki  => "White dot between cells: those two digits differ by exactly 1.",
            BossModifierId.RatioKropki       => "Black dot between cells: one digit is exactly double the other.",
            BossModifierId.Palindrome        => "Purple line: digits read the same from either end of the line.",
            BossModifierId.Thermo            => "Orange line with bulb: digits must strictly increase from bulb to tip.",
            BossModifierId.BetweenLines      => "White line: every digit on the line must fall strictly between the two endpoint values.",
            BossModifierId.EvenOdd           => "Blue square = even digit. Orange circle = odd digit.",
            BossModifierId.Nonconsecutive    => "Global: no two orthogonally adjacent cells may contain consecutive digits.",
            BossModifierId.Antiknight        => "Global: no two cells a chess knight's move apart may share the same digit.",
            BossModifierId.Antiking          => "Global: no two cells a king's move apart (including diagonal) may share a digit.",
            BossModifierId.AntiBishop        => "Global: no two cells on the same diagonal may share a digit.",
            BossModifierId.NonconsecDiagonal => "Global: diagonally adjacent cells cannot be consecutive (e.g. 4 cannot touch 3 or 5 diagonally).",
            BossModifierId.DistanceGe2       => "Global: equal digits must be at least 2 cells apart (no king-adjacent equal digits).",
            BossModifierId.EntropyGlobal     => "Every 3 consecutive row/col cells must contain one low (1\u20133), one mid (4\u20136), and one high (7\u20139) digit.",
            BossModifierId.ModularRegions    => "Every box region must contain at least one digit from {1\u20133}, {4\u20136}, and {7\u20139}.",
            BossModifierId.ConsecutiveLine   => "Orange line: adjacent cells on the line must differ by exactly 1 (e.g. 3-4-5).",
            BossModifierId.SlowThermo        => "Purple line with open bulb: digits must increase or stay equal from bulb to tip (e.g. 2-2-3-5).",
            BossModifierId.UniqueSetLine     => "Sky-blue line: no digit may repeat anywhere on the line.",
            BossModifierId.FullKropki        => "All diff-by-1 pairs show a white dot; all 1:2 ratio pairs show a black dot. No dot = neither.",
            BossModifierId.SumKropki         => "Labelled dot between two cells: those cells must sum to that value.",
            BossModifierId.GreaterLessThan   => "Orange chevrons (> / <) between cells indicate which digit must be larger.",
            BossModifierId.XVPairs           => "X between two cells: they sum to 10. V between two cells: they sum to 5.",
            BossModifierId.PrimeCells        => "Gold 'P' cells must contain a prime digit: 2, 3, 5, or 7.",
            BossModifierId.FortressCells     => "Grey shaded cells must be strictly greater than every orthogonally adjacent unshaded cell.",

            // Boss debuffs
            BossModifierId.RowWipe       => "Mistake: all your digits in that row are erased.",
            BossModifierId.ColWipe       => "Mistake: all your digits in that column are erased.",
            BossModifierId.DoublePenalty => "Mistakes deal 2 HP damage instead of 1.",
            BossModifierId.CellLock      => "Mistake: that cell locks for 3 correct placements before you can edit it.",
            BossModifierId.PencilBlind   => "Mistake: all pencil marks in that row and column are cleared.",
            BossModifierId.BoxWipe       => "Mistake: all your digits in that box are erased.",
            BossModifierId.CrossWipe     => "Mistake: all your digits in that row AND column are erased.",
            BossModifierId.PencilDrain   => "Mistake: lose 3 pencil charges.",
            BossModifierId.GoldFine      => "Mistake: lose 5 gold.",

            // Pressure mechanics
            BossModifierId.CountdownFill => "A cell counts down each turn — fill it before it reaches zero or lose HP.",
            BossModifierId.HauntedCell   => "One cell is haunted. Every mistake you make elsewhere costs +1 extra HP until you correctly fill the haunted cell.",
            BossModifierId.CrumblingRegion => "A region crumbles over time — digits placed there are erased if the region fully collapses.",
            BossModifierId.PressureWave  => "Periodic waves sweep the board, briefly locking cells you haven't solved yet.",

            _                            => m.ToString()
        };

        // ── Rarity pip overlay (F11/F25) ──────────────────────────────────────────
        /// <summary>
        /// Adds a small colored pip in the top-right corner of <paramref name="itemIconTransform"/>
        /// to signal item rarity at a glance in the bag panel and reward screen.
        /// <list type="bullet">
        ///   <item><term>Normal</term>  <description>No pip (returns null).</description></item>
        ///   <item><term>Rare</term>    <description>Teal dot (AccentGold family, desaturated teal).</description></item>
        ///   <item><term>Legendary</term><description>Gold dot (GamePalette.AccentGold).</description></item>
        /// </list>
        /// The pip is a <c>raycastTarget=false</c> Image so it never intercepts pointer events.
        /// </summary>
        internal static Image AddRarityPip(Transform itemIconTransform, ItemRarity rarity)
        {
            if (rarity == ItemRarity.Normal) return null;

            var pip = new GameObject("RarityPip", typeof(RectTransform), typeof(Image));
            pip.transform.SetParent(itemIconTransform, false);
            var rt = pip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.62f, 0.62f);
            rt.anchorMax = new Vector2(0.97f, 0.97f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = pip.GetComponent<Image>();
            img.color = rarity switch
            {
                ItemRarity.Rare => new Color(0.30f, 0.82f, 0.72f, 0.90f), // teal
                ItemRarity.Epic => new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, 0.15f, 0.95f), // gold
                _                                          => Color.clear
            };
            img.raycastTarget = false;
            return img;
        }

        // ── Confirm button icon reservation (F13) ──────────────────────────────────
        internal static Image AddRelicTierPip(Transform relicIconTransform, RelicTier tier)
        {
            if (relicIconTransform == null) return null;

            ClearNamedChildren(relicIconTransform, "RelicTier");

            var border = new GameObject("RelicTierFrame", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(relicIconTransform, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(1f, 1f);
            borderRt.offsetMax = new Vector2(-1f, -1f);
            var borderImg = border.GetComponent<Image>();
            borderImg.color = GetRelicTierBorderColor(tier);
            borderImg.raycastTarget = false;

            var pip = new GameObject("RelicTierPip", typeof(RectTransform), typeof(Image));
            pip.transform.SetParent(relicIconTransform, false);
            var pipRt = pip.GetComponent<RectTransform>();
            pipRt.anchorMin = new Vector2(0.62f, 0.62f);
            pipRt.anchorMax = new Vector2(0.97f, 0.97f);
            pipRt.offsetMin = Vector2.zero;
            pipRt.offsetMax = Vector2.zero;
            var pipImg = pip.GetComponent<Image>();
            pipImg.color = GetRelicTierPipColor(tier);
            pipImg.raycastTarget = false;

            if (tier == RelicTier.Legendary)
            {
                var inner = new GameObject("RelicTierPipInner", typeof(RectTransform), typeof(Image));
                inner.transform.SetParent(pip.transform, false);
                var innerRt = inner.GetComponent<RectTransform>();
                innerRt.anchorMin = new Vector2(0.26f, 0.26f);
                innerRt.anchorMax = new Vector2(0.74f, 0.74f);
                innerRt.offsetMin = Vector2.zero;
                innerRt.offsetMax = Vector2.zero;
                var innerImg = inner.GetComponent<Image>();
                innerImg.color = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, 0.12f, 0.98f);
                innerImg.raycastTarget = false;
            }

            return pipImg;
        }

        private static Color GetRelicTierBorderColor(RelicTier tier)
        {
            switch (tier)
            {
                case RelicTier.Tier2:
                    return new Color(0.42f, 0.58f, 0.66f, 0.34f);
                case RelicTier.Tier3:
                    return new Color(0.23f, 0.78f, 0.72f, 0.42f);
                case RelicTier.Tier4:
                    return new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, 0.14f, 0.52f);
                case RelicTier.Legendary:
                    return new Color(0.88f, 0.20f, 0.16f, 0.62f);
                default:
                    return new Color(0.48f, 0.44f, 0.36f, 0.24f);
            }
        }

        private static Color GetRelicTierPipColor(RelicTier tier)
        {
            switch (tier)
            {
                case RelicTier.Tier2:
                    return new Color(0.46f, 0.65f, 0.76f, 0.88f);
                case RelicTier.Tier3:
                    return new Color(0.30f, 0.82f, 0.72f, 0.92f);
                case RelicTier.Tier4:
                    return new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, 0.15f, 0.96f);
                case RelicTier.Legendary:
                    return new Color(0.92f, 0.18f, 0.16f, 0.96f);
                default:
                    return new Color(0.58f, 0.54f, 0.46f, 0.78f);
            }
        }

        // UiAction centralizes action icon semantics. Confirm/Accept use confirm_seal;
        // scroll_stamp remains reserved for future stamped-scroll interactions.

        // ── Modifier subgroup border (F27) ──────────────────────────────────────────
        /// <summary>
        /// Adds a thin colored stripe at the bottom of a modifier button's icon area to indicate
        /// which subgroup the modifier belongs to (line, dot, cage, cell, anti-move, global).
        /// This ensures visually similar modifier icons are distinguishable at 24-32 px.
        /// Call after <see cref="SetButtonIcon"/> so the Icon child already exists.
        /// </summary>
        internal static void AddModifierGroupMarker(Button btn, string modIconName)
        {
            if (btn == null || string.IsNullOrEmpty(modIconName)) return;
            var groupColor = GamePalette.GetModifierGroupColor(modIconName);
            if (groupColor == Color.clear) return;
            var iconChild = btn.transform.Find("Icon");
            if (iconChild == null) return;

            // Bottom stripe — increased height (0.08→0.12) for better visibility
            var stripe = new GameObject("GroupStripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(iconChild, false);
            var rt = stripe.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0.12f); // F16: increased from 0.08 for readability
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = stripe.GetComponent<Image>();
            img.color = groupColor;
            img.raycastTarget = false;

            // F16: left-edge stripe — adds a second colour anchor along the left side
            var leftStripe = new GameObject("GroupStripeLeft", typeof(RectTransform), typeof(Image));
            leftStripe.transform.SetParent(iconChild, false);
            var lRt = leftStripe.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = new Vector2(0.06f, 1f);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
            var lImg = leftStripe.GetComponent<Image>();
            lImg.color = groupColor;
            lImg.raycastTarget = false;
        }
    }
}
