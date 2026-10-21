using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// The look of the Options panels: colours, text sizes, the card and the section area. Shared by the main-menu
    /// Options (MainMenuBlueprintBuilder) and the in-run Options (InRunUiBlueprintBuilder); OptionsUiFactory builds the
    /// controls with it (GAP-029, FUTURE-015). Since UI-STYLE-017 the Options are a hand scroll: inside a
    /// <see cref="UsePaper"/> scope the shared widgets are built in ink on paper; the other menu panels keep the dark look.
    /// </summary>
    // [REQ: UI-OPT-001] [REQ: UI-STYLE-017]
    public static class OptionsUiStyle
    {
        public static readonly Color Backdrop   = new Color(0.06f, 0.09f, 0.08f, 1f);
        public static readonly Color Card       = new Color(0.10f, 0.12f, 0.10f, 0.70f);

        private static readonly Color DarkButton     = new Color(0.08f, 0.06f, 0.04f, 0.78f);  // dark charcoal
        private static readonly Color DarkButtonText = new Color(0.98f, 0.83f, 0.26f, 1f);     // gold
        private static readonly Color DarkText       = new Color(0.96f, 0.93f, 0.82f, 1f);

        // Ink on paper (UI-STYLE-017).
        public static readonly Color PaperInk       = new Color(0.13f, 0.11f, 0.09f, 1f);
        public static readonly Color PaperInkSoft   = new Color(0.33f, 0.29f, 0.24f, 1f);
        public static readonly Color PaperButton    = new Color(0.83f, 0.77f, 0.65f, 0.75f);
        public static readonly Color PaperVermilion = new Color(0.70f, 0.20f, 0.15f, 1f);
        public static readonly Color PaperFocus     = new Color(0.97f, 0.83f, 0.52f, 0.95f);

        /// <summary>True while the Options scroll is being built.</summary>
        public static bool Paper { get; private set; }

        /// <summary>Builds the shared widgets in ink on paper until the scope is disposed.</summary>
        public static PaperScope UsePaper()
        {
            var scope = new PaperScope(Paper);
            Paper = true;
            return scope;
        }

        public readonly struct PaperScope : IDisposable
        {
            private readonly bool _previous;
            public PaperScope(bool previous) => _previous = previous;
            public void Dispose() => Paper = _previous;
        }

        public static Color Button     => Paper ? PaperButton : DarkButton;
        public static Color ButtonText => Paper ? PaperInk : DarkButtonText;
        public static Color Accent     => Paper ? PaperVermilion : DarkButtonText;
        public static Color Text       => Paper ? PaperInk : DarkText;

        public const int TitleSize = 28;
        public static int HeaderSize => Paper ? 36 : 18;
        public static int TabLabelSize => Paper ? 20 : 14;

        /// <summary>Card rect of an Options panel inside its full-screen backdrop (same as the main menu card).</summary>
        public static readonly Vector2 CardMin = new Vector2(0.28f, 0.08f);
        public static readonly Vector2 CardMax = new Vector2(0.72f, 0.92f);

        /// <summary>
        /// Area of a tab's section inside the card: below the tab bar, above the bottom buttons. On the scroll it is the
        /// left part of the paper; the right part holds the page's slip.
        /// </summary>
        public static Vector2 SectionMin => Paper ? new Vector2(0.04f, 0.06f) : new Vector2(0f, 0.10f);
        public static Vector2 SectionMax => Paper ? new Vector2(0.64f, 0.93f) : new Vector2(1f, 0.88f);

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
