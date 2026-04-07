using UnityEngine;

namespace SudokuRoguelike.UI
{
    public static class GamePalette
    {
        // Shared
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

        // Board-specific
        public static readonly Color CellEmpty    = new(0.12f, 0.18f, 0.14f, 1f);
        public static readonly Color CellGiven    = new(0.20f, 0.29f, 0.20f, 1f);
        public static readonly Color CellSelected = new(0.73f, 0.49f, 0.18f, 1f);
        public static readonly Color CellRowCol   = new(0.18f, 0.34f, 0.56f, 1f);
        public static readonly Color CellMatch    = new(0.36f, 0.24f, 0.58f, 1f);
        public static readonly Color CellConflict = new(0.72f, 0.18f, 0.18f, 1f);
        public static readonly Color CellFog      = new(0.06f, 0.06f, 0.08f, 1f);
        public static readonly Color RegionBorder = new(1f,    0.78f, 0.24f, 0.72f);
        public static readonly Color KillerBorder = new(0.85f, 0.15f, 0.12f, 0.90f);
    }
}
