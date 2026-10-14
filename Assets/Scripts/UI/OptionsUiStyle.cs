using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// The look of the Options panels: colours, text sizes, the card and the section area. Shared by the main-menu
    /// Options (MainMenuBlueprintBuilder) and the in-run Options (InRunUiBlueprintBuilder); OptionsUiFactory builds the
    /// controls with it (GAP-029, FUTURE-015).
    /// </summary>
    // [REQ: UI-OPT-001]
    public static class OptionsUiStyle
    {
        public static readonly Color Backdrop   = new Color(0.06f, 0.09f, 0.08f, 1f);
        public static readonly Color Card       = new Color(0.10f, 0.12f, 0.10f, 0.70f);
        public static readonly Color Button     = new Color(0.08f, 0.06f, 0.04f, 0.78f);  // dark charcoal
        public static readonly Color ButtonText = new Color(0.98f, 0.83f, 0.26f, 1f);     // gold
        public static readonly Color Accent     = new Color(0.98f, 0.83f, 0.26f, 1f);
        public static readonly Color Text       = new Color(0.96f, 0.93f, 0.82f, 1f);

        public const int TitleSize = 28;
        public const int HeaderSize = 18;
        public const int TabLabelSize = 14;

        /// <summary>Card rect of an Options panel inside its full-screen backdrop (same as the main menu card).</summary>
        public static readonly Vector2 CardMin = new Vector2(0.28f, 0.08f);
        public static readonly Vector2 CardMax = new Vector2(0.72f, 0.92f);

        /// <summary>Area of a tab's section inside the card: below the tab bar, above the bottom buttons.</summary>
        public static readonly Vector2 SectionMin = new Vector2(0f, 0.10f);
        public static readonly Vector2 SectionMax = new Vector2(1f, 0.88f);

        public static RectTransform MakeCard(RectTransform backdrop)
        {
            var go = new GameObject("Content", typeof(RectTransform), typeof(Image), typeof(Outline));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(backdrop, false);
            rt.anchorMin = CardMin;
            rt.anchorMax = CardMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = Card;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = GamePalette.CardOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return rt;
        }
    }
}
