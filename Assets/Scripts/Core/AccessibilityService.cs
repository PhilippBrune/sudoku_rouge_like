using SudokuRoguelike.Core;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Core
{
    public sealed class AccessibilityService
    {
        private AccessibilitySettings _settings;
        private GraphicsSettingsModel _graphics;

        public bool IsColorblind => _settings?.ColorblindMode ?? false;
        public bool IsHighContrast => _settings?.HighContrastMode ?? false;
        public bool IsReduceMotion => _settings?.ReduceMotion ?? false;
        public bool UseAltSymbols => _settings?.AlternativeConstraintSymbols ?? false;
        public float FontScale => _settings?.FontScale ?? 1f;
        public bool ScreenShakeEnabled => _graphics?.ScreenShake ?? true;
        public float ParticleIntensity => _graphics?.ParticleIntensity ?? 1f;

        public void Refresh(AccessibilitySettings accessibility, GraphicsSettingsModel graphics)
        {
            _settings = accessibility ?? new AccessibilitySettings();
            _graphics = graphics ?? new GraphicsSettingsModel();
        }

        public int ScaleFont(int baseSize)
        {
            return (int)(baseSize * FontScale);
        }

        public float GetAnimationDuration(float baseMs)
        {
            return IsReduceMotion ? baseMs * 0.3f : baseMs;
        }

        public float GetOverlayAlpha(float baseAlpha)
        {
            return IsHighContrast ? baseAlpha * 1.4f : baseAlpha;
        }

        public float GetLineWidthMultiplier()
        {
            return IsHighContrast ? 1.5f : 1.0f;
        }
    }
}
