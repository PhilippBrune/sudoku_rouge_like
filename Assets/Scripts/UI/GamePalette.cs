using SudokuRoguelike.Core;
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
        // [REQ: UI-STYLE-002] The board is ink on paper (hanging scroll). Digits reach >= 4.5:1 on the plain cell states
        // (empty, given, selected, row/column, match, jigsaw): givens ink 11-15:1, placed blue ink 5.8-7.8:1; on a
        // conflict cell the digit turns ink (5.5:1), on a fog blot it turns paper. Blended tints (bag highlight, locks,
        // tutorial focus) are not measured.
        public static readonly Color CellEmpty    = new(0.96f, 0.93f, 0.85f, 1f);
        public static readonly Color CellGiven    = new(0.91f, 0.87f, 0.77f, 1f);
        public static readonly Color CellSelected = new(0.95f, 0.79f, 0.42f, 1f);
        public static readonly Color CellRowCol   = new(0.86f, 0.82f, 0.68f, 1f);
        public static readonly Color CellMatch    = new(0.76f, 0.85f, 0.68f, 1f);
        public static readonly Color CellConflict = new(0.90f, 0.42f, 0.36f, 1f); // still "strong red" for high contrast
        public static readonly Color CellFog      = new(0.24f, 0.21f, 0.17f, 1f); // an ink blot

        // Jigsaw checker pattern (alternating region tints, two paper tones)
        public static readonly Color CellJigsawGivenAlt  = new(0.87f, 0.83f, 0.72f, 1f);
        public static readonly Color CellJigsawGivenMain = new(0.91f, 0.87f, 0.77f, 1f);
        public static readonly Color CellJigsawEmptyAlt  = new(0.92f, 0.88f, 0.79f, 1f);
        public static readonly Color CellJigsawEmptyMain = new(0.96f, 0.93f, 0.85f, 1f);

        // Cell number labels
        // [REQ: REQ-UI-103] Givens and player digits must be told apart at a glance (GAP-029): givens are bold ivory,
        // player digits blue ink. Both keep >= 5:1 contrast on the cell and highlight backgrounds.
        public static readonly Color CellNumberGiven  = new(0.11f, 0.10f, 0.08f, 1f);
        public static readonly Color CellNumberPlaced = new(0.16f, 0.27f, 0.55f, 1f);
        // Digits on the selected (amber) cell: dark ink, 5.3:1 against CellSelected.
        public static readonly Color CellNumberOnSelected = new(0.10f, 0.08f, 0.05f, 1f);
        // A given digit under fog (the dark ink blot) is written in paper colour, ~10:1 against CellFog.
        public static readonly Color CellNumberOnFog = new(0.95f, 0.91f, 0.82f, 1f);
        public static readonly Color PencilMarkColor  = new(0.36f, 0.31f, 0.24f, 0.95f);

        // Cell highlight overlays
        public static readonly Color AntiknightForbid    = new(0.55f, 0.20f, 0.60f, 0.55f);
        public static readonly Color BagHighlightOverlay = new(0.30f, 0.65f, 0.85f, 0.55f);
        public static readonly Color CellLocked          = new(0.65f, 0.15f, 0.60f, 0.70f); // deep magenta lock tint

        // ── Board borders ────────────────────────────────────────────────────────
        public static readonly Color RegionBorder = new(0.13f, 0.11f, 0.09f, 0.95f); // ink region borders
        public static readonly Color KillerBorder = new(0.94f, 0.26f, 0.22f, 1.00f);

        // ── Modifier line colors ─────────────────────────────────────────────────
        // [REQ: BOSS-MOD-002] The colours puzzle setters use (Logic Masters, GM Puzzles, Cracking the Cryptic): green German
        // Whispers, orange Dutch Whispers, pink Renban, grey Palindrome, grey thermometers (GAP-048). Lines are opaque
        // bands through the cell centres, so overlaps never darken; ink digits keep >= 5:1 on every band.
        public static readonly Color LineGermanWhispers  = new(0.42f, 0.72f, 0.38f, 1f); // green
        public static readonly Color LineDutchWhispers   = new(0.96f, 0.55f, 0.25f, 1f); // orange
        public static readonly Color LineParityLine      = new(0.45f, 0.58f, 0.96f, 1f); // blue
        public static readonly Color LineRenbanLine      = new(0.93f, 0.50f, 0.76f, 1f); // pink
        public static readonly Color LinePalindrome      = new(0.56f, 0.56f, 0.58f, 1f); // grey
        public static readonly Color LineThermo          = new(0.72f, 0.72f, 0.72f, 1f); // light grey, with a bulb
        public static readonly Color LineBetweenLine     = new(0.76f, 0.60f, 0.42f, 1f); // taupe, with a circle at each end
        public static readonly Color LineConsecutiveLine = new(0.86f, 0.70f, 0.10f, 1f); // gold
        public static readonly Color LineSlowThermo      = new(0.70f, 0.50f, 0.92f, 1f); // violet, with an open bulb
        public static readonly Color LineUniqueSetLine   = new(0.34f, 0.78f, 0.90f, 1f); // sky blue, with rings at the ends
        // Catalog batch B1 (modifier-catalog-plan.md): zipper lavender, entropic peach and modular teal as setters use them.
        public static readonly Color LineNonconsecLine   = new(0.62f, 0.80f, 0.22f, 1f); // lime, ink crosses on each step
        public static readonly Color LineHighLow         = new(0.52f, 0.82f, 0.64f, 1f); // mint, paper stripe in the middle
        public static readonly Color LineNabner          = new(0.90f, 0.36f, 0.42f, 1f); // red
        public static readonly Color LineEntropic        = new(0.99f, 0.72f, 0.55f, 1f); // peach
        public static readonly Color LineModular         = new(0.20f, 0.62f, 0.56f, 1f); // teal
        public static readonly Color LineZipper          = new(0.80f, 0.72f, 0.98f, 1f); // lavender, ink mark in the middle
        public static readonly Color LineRegionSum       = new(0.24f, 0.32f, 0.66f, 1f); // navy, ink ticks at region borders
        public static readonly Color LineNLine           = new(0.70f, 0.38f, 0.20f, 1f); // sienna, number tag at the start
        public static readonly Color LineSumLine         = new(0.58f, 0.60f, 0.16f, 1f); // olive, total tag at the end
        public static readonly Color LineWhisperK        = new(0.66f, 0.28f, 0.62f, 1f); // magenta, "≥K" tag at the start

        // ── Modifier overlay markers ─────────────────────────────────────────────
        public static readonly Color OverlayKropkiSum    = new(0.45f, 0.45f, 0.50f, 0.90f);
        // Catalog batch B2: Difference ≥ 3 bar, ×3 ring, mirror-pair badge.
        public static readonly Color OverlayDifferenceK  = new(0.45f, 0.25f, 0.55f, 1.00f); // plum
        public static readonly Color OverlayMultiple     = new(0.60f, 0.42f, 0.12f, 1.00f); // ochre
        public static readonly Color OverlayMirror       = new(0.30f, 0.30f, 0.65f, 1.00f); // indigo
        // Cage outlines and labels by kind (a plain killer cage keeps KillerBorder).
        public static readonly Color OverlayCageProduct    = new(0.08f, 0.50f, 0.48f, 1.00f); // teal
        public static readonly Color OverlayCageDifference = new(0.18f, 0.40f, 0.80f, 1.00f); // blue
        public static readonly Color OverlayCageRatio      = new(0.22f, 0.18f, 0.15f, 1.00f); // ink
        public static readonly Color OverlayCageRenban     = new(0.84f, 0.32f, 0.64f, 1.00f); // pink, like Renban lines
        // Batch B6 marks
        public static readonly Color OverlayDistancePair   = new(0.56f, 0.34f, 0.18f, 1.00f); // brown link and badge
        public static readonly Color OverlayOppositeSum    = new(0.72f, 0.16f, 0.30f, 1.00f); // crimson badge
        public static readonly Color OverlayCountingCircle = new(0.92f, 0.48f, 0.08f, 1.00f); // orange ring
        public static readonly Color OverlayDistanceCell   = new(0.16f, 0.24f, 0.56f, 1.00f); // navy ring and dots
        public static readonly Color OverlayNegationZone   = new(0.42f, 0.72f, 0.38f, 0.20f); // green wash under the zone
        public static readonly Color OverlayKropkiBlack  = new(0.10f, 0.10f, 0.10f, 0.90f);
        public static readonly Color OverlayKropkiWhite  = new(1.00f, 1.00f, 1.00f, 1.00f);
        public static readonly Color OverlayKropkiWhiteRim = new(0.13f, 0.11f, 0.09f, 1.00f); // ink rim: a white dot on paper
        public static readonly Color OverlayArrow        = new(0.38f, 0.36f, 0.33f, 0.90f);
        public static readonly Color OverlayArrowPath    = new(0.40f, 0.38f, 0.34f, 0.55f);
        public static readonly Color OverlayGreaterLess  = new(0.78f, 0.42f, 0.04f, 0.95f);
        public static readonly Color OverlayXVPair       = new(0.04f, 0.46f, 0.58f, 0.95f);
        public static readonly Color OverlayEvenMarker   = new(0.35f, 0.65f, 0.90f, 0.55f);
        public static readonly Color OverlayOddMarker    = new(0.90f, 0.55f, 0.20f, 0.55f);
        public static readonly Color OverlayPrimeCircle  = new(0.85f, 0.75f, 0.15f, 0.50f);
        public static readonly Color OverlayPrimeLabel   = new(0.52f, 0.38f, 0.02f, 0.95f);
        public static readonly Color OverlayFortress     = new(0.45f, 0.45f, 0.50f, 0.45f);
        public static readonly Color OverlayCageFogBg    = new(0.96f, 0.93f, 0.85f, 0.92f); // paper behind the cage sum

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

        // ── Per-class signature colors (F10/F26) ──────────────────────────────────
        // Used to tint class-exclusive item/relic icons and class badge overlays.
        // Apply as a 30% opacity tint in the bottom-left corner of affected icons.
        public static readonly Color ClassColorNumberFreak       = new(0.30f, 0.62f, 0.95f, 0.30f); // cool blue
        public static readonly Color ClassColorGardenMonk        = new(0.35f, 0.72f, 0.40f, 0.30f); // calm green
        public static readonly Color ClassColorShrineArchivist   = new(0.85f, 0.75f, 0.35f, 0.30f); // scroll gold
        public static readonly Color ClassColorKoiGambler        = new(0.90f, 0.45f, 0.18f, 0.30f); // koi orange
        public static readonly Color ClassColorStoneGardener     = new(0.55f, 0.55f, 0.56f, 0.30f); // stone gray
        public static readonly Color ClassColorLanternSeer       = new(0.95f, 0.85f, 0.20f, 0.30f); // lantern gold
        public static readonly Color ClassColorReedDuelist       = new(0.30f, 0.82f, 0.70f, 0.30f); // reed teal
        public static readonly Color ClassColorQuietCartographer = new(0.70f, 0.42f, 0.82f, 0.30f); // map violet

        // ── Modifier subgroup accent colors (F27) ─────────────────────────────────
        // Applied as a thin border tint when rendering modifier icons so similar
        // icons are distinguishable at small sizes (24-32 px).
        public static readonly Color ModGroupAntiMove  = new(0.88f, 0.25f, 0.25f, 0.55f); // red  — antiknight/antiking/antibishop
        public static readonly Color ModGroupLine      = new(0.25f, 0.80f, 0.55f, 0.55f); // legend group colour for all line modifiers (whispers, parity, renban, palindrome, consecutive, thermo)
        public static readonly Color ModGroupDot       = new(0.85f, 0.85f, 0.85f, 0.55f); // white — kropki/white_dot/black_dot/sum_dot
        public static readonly Color ModGroupCage      = new(0.85f, 0.20f, 0.15f, 0.45f); // crimson — killer/arrow
        public static readonly Color ModGroupCell      = new(0.35f, 0.65f, 0.92f, 0.55f); // blue  — even_odd/prime/fortress/fog
        public static readonly Color ModGroupGlobal    = new(0.65f, 0.40f, 0.85f, 0.55f); // violet — nonconsecutive/distance/modular/entropy

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

        // ── Class color lookup (F10/F26) ──────────────────────────────────────────
        /// <summary>
        /// Returns the per-class tint color for <paramref name="classId"/>.
        /// Apply at 30% opacity to the bottom-left quadrant of class-exclusive item/relic icons.
        /// </summary>
        public static Color GetClassColor(ClassId classId) => classId switch
        {
            ClassId.NumberFreak       => ClassColorNumberFreak,
            ClassId.GardenMonk        => ClassColorGardenMonk,
            ClassId.ShrineArchivist   => ClassColorShrineArchivist,
            ClassId.KoiGambler        => ClassColorKoiGambler,
            ClassId.StoneGardener     => ClassColorStoneGardener,
            ClassId.LanternSeer       => ClassColorLanternSeer,
            ClassId.ReedDuelist       => ClassColorReedDuelist,
            ClassId.QuietCartographer => ClassColorQuietCartographer,
            _                         => Color.clear
        };

        // ── Modifier group lookup (F27) ───────────────────────────────────────────
        /// <summary>
        /// Returns the subgroup accent color for a modifier icon name, used as a thin border tint
        /// when rendering modifier icons so similar icons are distinguishable at 24-32 px.
        /// </summary>
        public static Color GetModifierGroupColor(string iconName)
        {
            if (iconName == null) return Color.clear;
            if (iconName.StartsWith("antiknight") || iconName.StartsWith("antiking") || iconName.StartsWith("antibishop") || iconName.StartsWith("nonconsec"))
                return ModGroupAntiMove;
            if (iconName.StartsWith("german") || iconName.StartsWith("dutch") || iconName.StartsWith("parity") ||
                iconName.StartsWith("renban") || iconName.StartsWith("palindrome") || iconName.StartsWith("between") ||
                iconName.StartsWith("consecutive") || iconName.StartsWith("thermo") || iconName.StartsWith("slow_thermo") ||
                iconName.StartsWith("unique_set") || iconName.StartsWith("arrow"))
                return ModGroupLine;
            if (iconName.StartsWith("white_dot") || iconName.StartsWith("black_dot") || iconName.StartsWith("full_kropki") || iconName.StartsWith("sum_dot"))
                return ModGroupDot;
            if (iconName.StartsWith("killer") || iconName.StartsWith("arrow_sum"))
                return ModGroupCage;
            if (iconName.StartsWith("even_odd") || iconName.StartsWith("prime") || iconName.StartsWith("fortress") || iconName.StartsWith("fog"))
                return ModGroupCell;
            if (iconName.StartsWith("nonconsecutive") || iconName.StartsWith("distance") || iconName.StartsWith("modular") ||
                iconName.StartsWith("tricolor") || iconName.StartsWith("greater"))
                return ModGroupGlobal;
            return Color.clear;
        }
    }
}
