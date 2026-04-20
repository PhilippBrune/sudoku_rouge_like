using UnityEngine;

namespace SudokuRoguelike.UI
{
    public static class GamePalette
    {
        // ── Shared UI ────────────────────────────────────────────────────────────
        public static readonly Color Background   = new(0.06f, 0.09f, 0.10f, 1f);
        public static readonly Color Panel        = new(0.10f, 0.14f, 0.12f, 0.90f);
        public static readonly Color Button       = new(0.82f, 0.75f, 0.62f, 0.92f);
        public static readonly Color ButtonText   = new(0.12f, 0.10f, 0.08f, 1f);
        public static readonly Color AccentGold   = new(0.98f, 0.83f, 0.26f, 1f);
        public static readonly Color TextPrimary  = new(0.96f, 0.93f, 0.82f, 1f);
        public static readonly Color TextMuted    = new(0.60f, 0.57f, 0.50f, 1f);
        public static readonly Color HpRed        = new(0.75f, 0.22f, 0.22f, 1f);
        public static readonly Color PencilBlue   = new(0.22f, 0.55f, 0.75f, 1f);
        public static readonly Color DangerRed    = new(0.72f, 0.18f, 0.18f, 1f);
        public static readonly Color SuccessGreen = new(0.30f, 0.72f, 0.40f, 1f);
        public static readonly Color WinGold      = new(0.85f, 0.75f, 0.20f, 1f);

        // ── Board cells ──────────────────────────────────────────────────────────
        public static readonly Color CellEmpty    = new(0.12f, 0.18f, 0.14f, 1f);
        public static readonly Color CellGiven    = new(0.20f, 0.29f, 0.20f, 1f);
        public static readonly Color CellSelected = new(0.73f, 0.49f, 0.18f, 1f);
        public static readonly Color CellRowCol   = new(0.18f, 0.34f, 0.56f, 1f);
        public static readonly Color CellMatch    = new(0.36f, 0.24f, 0.58f, 1f);
        public static readonly Color CellConflict = new(0.72f, 0.18f, 0.18f, 1f);
        public static readonly Color CellFog      = new(0.06f, 0.06f, 0.08f, 1f);

        // Jigsaw checker pattern (alternating region tints)
        public static readonly Color CellJigsawGivenAlt  = new(0.25f, 0.36f, 0.25f, 1f);
        public static readonly Color CellJigsawGivenMain = new(0.19f, 0.30f, 0.20f, 1f);
        public static readonly Color CellJigsawEmptyAlt  = new(0.15f, 0.22f, 0.16f, 1f);
        public static readonly Color CellJigsawEmptyMain = new(0.11f, 0.17f, 0.12f, 1f);

        // Cell number labels
        public static readonly Color CellNumberGiven  = new(0.04f, 0.04f, 0.04f, 1f);
        public static readonly Color PencilMarkColor  = new(0.84f, 0.86f, 0.82f, 0.95f);

        // Cell highlight overlays
        public static readonly Color AntiknightForbid    = new(0.55f, 0.20f, 0.60f, 0.55f);
        public static readonly Color BagHighlightOverlay = new(0.30f, 0.65f, 0.85f, 0.55f);
        public static readonly Color CellLocked          = new(0.65f, 0.15f, 0.60f, 0.70f); // deep magenta lock tint

        // ── Board borders ────────────────────────────────────────────────────────
        public static readonly Color RegionBorder = new(1f,    0.78f, 0.24f, 0.72f);
        public static readonly Color KillerBorder = new(0.85f, 0.15f, 0.12f, 0.90f);

        // ── Modifier line colors ─────────────────────────────────────────────────
        public static readonly Color LineGermanWhispers  = new(0.20f, 0.72f, 0.30f, 0.55f);
        public static readonly Color LineDutchWhispers   = new(0.10f, 0.95f, 0.78f, 0.75f);
        public static readonly Color LineParityLine      = new(0.10f, 0.88f, 0.80f, 0.70f);
        public static readonly Color LineRenbanLine      = new(0.80f, 0.35f, 0.65f, 0.55f);
        public static readonly Color LinePalindrome      = new(0.55f, 0.30f, 0.85f, 1.0f);
        public static readonly Color LineThermo          = new(0.85f, 0.45f, 0.10f, 0.70f);
        public static readonly Color LineBetweenLine     = new(0.90f, 0.90f, 0.90f, 0.60f);
        public static readonly Color LineConsecutiveLine = new(0.95f, 0.65f, 0.15f, 0.70f);
        public static readonly Color LineSlowThermo      = new(0.70f, 0.30f, 0.80f, 0.70f);
        public static readonly Color LineUniqueSetLine   = new(0.20f, 0.70f, 0.95f, 0.65f);

        // ── Modifier overlay markers ─────────────────────────────────────────────
        public static readonly Color OverlayKropkiSum    = new(0.45f, 0.45f, 0.50f, 0.90f);
        public static readonly Color OverlayKropkiBlack  = new(0.10f, 0.10f, 0.10f, 0.90f);
        public static readonly Color OverlayKropkiWhite  = new(0.95f, 0.95f, 0.95f, 0.85f);
        public static readonly Color OverlayArrow        = new(0.75f, 0.75f, 0.75f, 0.90f);
        public static readonly Color OverlayArrowPath    = new(0.55f, 0.55f, 0.55f, 0.45f);
        public static readonly Color OverlayGreaterLess  = new(0.95f, 0.70f, 0.20f, 0.90f);
        public static readonly Color OverlayXVPair       = new(0.20f, 0.80f, 0.90f, 0.90f);
        public static readonly Color OverlayEvenMarker   = new(0.35f, 0.65f, 0.90f, 0.55f);
        public static readonly Color OverlayOddMarker    = new(0.90f, 0.55f, 0.20f, 0.55f);
        public static readonly Color OverlayPrimeCircle  = new(0.85f, 0.75f, 0.15f, 0.50f);
        public static readonly Color OverlayPrimeLabel   = new(1.00f, 0.95f, 0.40f, 0.95f);
        public static readonly Color OverlayFortress     = new(0.45f, 0.45f, 0.50f, 0.45f);
        public static readonly Color OverlayCageFogBg    = new(0.00f, 0.00f, 0.00f, 0.65f);

        // ── Run-map path overlay ──────────────────────────────────────────────────
        public static readonly Color RiskRouteOutline = new(0.88f, 0.40f, 0.18f, 0.75f);
        public static readonly Color BossGateNode     = new(0.70f, 0.20f, 0.20f, 1.00f);

        // ── AccentGold alpha variants ─────────────────────────────────────────────
        public static readonly Color AccentGoldFaint  = new(0.98f, 0.83f, 0.26f, 0.30f);
        public static readonly Color AccentGoldSubtle = new(0.98f, 0.83f, 0.26f, 0.35f);
        public static readonly Color AccentGoldGlow   = new(0.98f, 0.83f, 0.26f, 0.38f);
        public static readonly Color AccentGoldDim    = new(0.98f, 0.83f, 0.26f, 0.45f);
        public static readonly Color AccentGoldHalf   = new(0.98f, 0.83f, 0.26f, 0.50f);
        public static readonly Color AccentGoldMid    = new(0.98f, 0.83f, 0.26f, 0.60f);

        // ── Shadow / overlay blacks ───────────────────────────────────────────────
        public static readonly Color ShadowDark  = new(0f, 0f, 0f, 0.80f);
        public static readonly Color ShadowMid   = new(0f, 0f, 0f, 0.60f);
        public static readonly Color ShadowLight = new(0f, 0f, 0f, 0.35f);

        // ── Scroll and list backgrounds ───────────────────────────────────────────
        public static readonly Color ScrollBg      = new(0f, 0f, 0f, 0.12f);
        public static readonly Color ScrollBgDark  = new(0f, 0f, 0f, 0.10f);
        public static readonly Color ScrollBgFaint = new(1f, 1f, 1f, 0.02f);
        public static readonly Color ListItemBg    = new(0f, 0f, 0f, 0.20f);

        // ── Button state colors (for ColorBlock) ──────────────────────────────────
        public static readonly Color ButtonOutline   = new(0.45f, 0.38f, 0.25f, 0.70f);
        public static readonly Color ButtonHighlight = new(0.90f, 0.83f, 0.70f, 0.95f);
        public static readonly Color ButtonPressed   = new(0.72f, 0.65f, 0.52f, 0.95f);
        public static readonly Color ButtonDisabled  = new(0.45f, 0.42f, 0.38f, 0.70f);
        public static readonly Color DisabledGray    = new(0.22f, 0.22f, 0.22f, 0.70f);

        // ── Menu-specific ─────────────────────────────────────────────────────────
        public static readonly Color CardOutline  = new(0.55f, 0.45f, 0.30f, 0.35f);
        public static readonly Color MenuInfoBg   = new(0.08f, 0.10f, 0.09f, 0.85f);
        public static readonly Color ToggleFill   = new(0.25f, 0.65f, 0.55f, 1f);
        public static readonly Color ToggleHandle = new(0.90f, 0.95f, 0.92f, 1f);

        // ── High-contrast semantic palette ───────────────────────────────────────
        // These are used directly by AccessibilityService.ToHighContrast() and
        // AccessibilityPreviewController to guarantee readable cell combinations.
        public static readonly Color HcCellGiven    = new(0.92f, 0.90f, 0.85f, 1f); // near-white given
        public static readonly Color HcCellEmpty    = new(0.07f, 0.09f, 0.08f, 1f); // near-black empty
        public static readonly Color HcCellConflict = new(0.96f, 0.88f, 0.08f, 1f); // high-vis yellow
        public static readonly Color HcTextOnLight  = new(0.04f, 0.04f, 0.04f, 1f); // dark text for light bg
        public static readonly Color HcTextOnDark   = new(0.95f, 0.93f, 0.88f, 1f); // light text for dark bg
        public static readonly Color HcPencilMark   = new(0.60f, 0.60f, 0.60f, 1f); // mid-grey pencil

        // ── V5 sprite-swap flag ───────────────────────────────────────────────────
        /// <summary>
        /// When true, BoardViewController uses Sprite assets for cell markers (Killer, Kropki,
        /// Arrow, EvenOdd) instead of procedural textures.  Flip to true once finished art is
        /// imported and verified at all board sizes (5–9) and variants (Standard, Jigsaw).
        /// See VisualRedesignPlan Phase V5.
        /// </summary>
        public static bool UseSpriteCellMarkers = false;

        // ── Alpha helper ──────────────────────────────────────────────────────────
        /// <summary>Returns <paramref name="c"/> with its alpha replaced by <paramref name="a"/>.</summary>
        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
