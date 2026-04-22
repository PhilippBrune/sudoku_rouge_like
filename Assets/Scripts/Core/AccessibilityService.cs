using SudokuRoguelike.Core;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Core
{
    /// <summary>
    /// Single source of truth for all accessibility settings.
    /// Call Refresh() whenever the player changes options so all consumers
    /// (board rendering, animations, particles) see updated values.
    /// </summary>
    // [REQ: ACCESS-COLOR-001] Colorblind mode, high contrast, reduce motion, alt symbols
    public sealed class AccessibilityService
    {
        // ── Runtime settings ──────────────────────────────────────────────────────

        private AccessibilitySettings _settings;
        private GraphicsSettingsModel _graphics;

        public bool IsColorblind    => _settings?.ColorblindMode                ?? false;
        public bool IsHighContrast  => _settings?.HighContrastMode              ?? false;
        public bool IsReduceMotion  => _settings?.ReduceMotion                  ?? false;
        public bool UseAltSymbols   => _settings?.AlternativeConstraintSymbols  ?? false;
        public float FontScale      => _settings?.FontScale                     ?? 1f;
        public bool ScreenShakeEnabled => _graphics?.ScreenShake               ?? true;
        public float ParticleIntensity => _graphics?.ParticleIntensity          ?? 1f;

        public void Refresh(AccessibilitySettings accessibility, GraphicsSettingsModel graphics)
        {
            _settings = accessibility ?? new AccessibilitySettings();
            _graphics = graphics      ?? new GraphicsSettingsModel();

            // Sync ColorMode from settings flags so the board renderer stays consistent.
            if (_settings.HighContrastMode)
                ActiveColorMode = ColorMode.HighContrast;
            else if (_settings.ColorblindMode)
                ActiveColorMode = ColorMode.Protanopia;   // default CB mode until per-type setting added
            else
                ActiveColorMode = ColorMode.Normal;
        }

        public int ScaleFont(int baseSize) => (int)(baseSize * FontScale);

        public float GetAnimationDuration(float baseMs)
            => IsReduceMotion ? baseMs * 0.3f : baseMs;

        /// <summary>Returns the overlay alpha scaled by high-contrast boost.</summary>
        public float GetOverlayAlpha(float baseAlpha)
            => Mathf.Clamp01(baseAlpha * (IsHighContrast ? 1.4f : 1f));

        /// <summary>Returns the line-width multiplier for modifier overlay lines.</summary>
        public float GetLineWidthMultiplier() => IsHighContrast ? 1.5f : 1.0f;

        // ── Colour-blind / high-contrast mode ────────────────────────────────────

        public enum ColorMode
        {
            Normal,
            Protanopia,      // red-green (red-weak)
            Deuteranopia,    // red-green (green-weak)
            HighContrast
        }

        /// <summary>
        /// Active colour-blind mode.  Set directly or via Refresh() from settings.
        /// Board renderers must route every cell background colour through GetCellColor().
        /// </summary>
        public ColorMode ActiveColorMode { get; set; } = ColorMode.Normal;

        /// <summary>
        /// Adjusts <paramref name="baseColor"/> for the active colour-blind mode.
        /// Returns the colour unchanged in Normal mode.
        /// </summary>
        public Color GetCellColor(Color baseColor)
        {
            return ActiveColorMode switch
            {
                ColorMode.Protanopia   => SimulateProtanopia(baseColor),
                ColorMode.Deuteranopia => SimulateDeuteranopia(baseColor),
                ColorMode.HighContrast => ToHighContrast(baseColor),
                _                      => baseColor
            };
        }

        // ── Colour-blind simulation (simple linear approximation) ─────────────────

        private static Color SimulateProtanopia(Color c)
        {
            var r = 0.567f * c.r + 0.433f * c.g;
            var g = 0.558f * c.r + 0.442f * c.g;
            var b = 0.242f * c.g + 0.758f * c.b;
            return new Color(r, g, b, c.a);
        }

        private static Color SimulateDeuteranopia(Color c)
        {
            var r = 0.625f * c.r + 0.375f * c.g;
            var g = 0.700f * c.r + 0.300f * c.g;
            var b = 0.300f * c.g + 0.700f * c.b;
            return new Color(r, g, b, c.a);
        }

        private static Color ToHighContrast(Color c)
        {
            // Detect strongly red colours (conflict/error) and map to high-vis yellow.
            // The game palette is dark-dominant, so distinguish given (green-grey, lum ~0.26)
            // from empty (very dark, lum ~0.15) using a 0.20 luminance threshold.
            var isHighRed = c.r > 0.40f && c.r > c.g * 2f && c.r > c.b * 2f;
            if (isHighRed) return GamePalette.HcCellConflict;

            var lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            return lum < 0.20f ? GamePalette.HcCellEmpty : GamePalette.HcCellGiven;
        }
    }
}
