using System;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Core
{
    public enum LineDashStyle
    {
        Solid,
        SolidThick,
        Dashed,
        Dotted,
        DashDot,
        DoubleLine,
        Wavy
    }

    public sealed class LineDashPattern
    {
        public readonly LineDashStyle Style;
        public readonly float SegmentLength;
        public readonly float GapLength;
        public readonly float Width;

        public LineDashPattern(LineDashStyle style, float segmentLength, float gapLength, float width)
        {
            Style = style;
            SegmentLength = segmentLength;
            GapLength = gapLength;
            Width = width;
        }

        public static readonly LineDashPattern SolidDefault = new(LineDashStyle.Solid, 0f, 0f, 4f);
    }

    public sealed class AccessibilityService
    {
        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        private AccessibilitySettings _settings;
        private GraphicsSettingsModel _graphics;

        public bool IsColorblind => _settings?.ColorblindMode ?? false;
        public bool IsHighContrast => _settings?.HighContrastMode ?? false;
        public bool IsReduceMotion => _settings?.ReduceMotion ?? false;
        public bool UseAltSymbols => _settings?.AlternativeConstraintSymbols ?? false;
        public float FontScale => _settings?.FontScale ?? 1f;

        public bool ScreenShakeEnabled => (_graphics?.ScreenShake ?? true) && !IsReduceMotion;
        public float ParticleIntensity => IsReduceMotion ? 0f : (_graphics?.ParticleIntensity ?? 1f);

        public bool ShouldShowConstraintLabels => IsColorblind || UseAltSymbols;

        public AccessibilityService()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }
            _settings = _profile.Options?.Accessibility ?? new AccessibilitySettings();
            _graphics = _profile.Options?.Graphics ?? new GraphicsSettingsModel();
        }

        public int ScaleFont(int baseSize)
        {
            var scaled = (int)(baseSize * FontScale + 0.5f);
            return Math.Max(scaled, 8);
        }

        public float GetAnimationDuration(float baseMs)
        {
            return IsReduceMotion ? 0f : baseMs;
        }

        public LineDashPattern GetLineDashPattern(LineType lineType)
        {
            if (!IsColorblind) return LineDashPattern.SolidDefault;

            return lineType switch
            {
                LineType.GermanWhispers => new LineDashPattern(LineDashStyle.SolidThick, 0f, 0f, 6f),
                LineType.DutchWhispers => new LineDashPattern(LineDashStyle.Dashed, 12f, 6f, 4f),
                LineType.Parity => new LineDashPattern(LineDashStyle.Dotted, 4f, 4f, 4f),
                LineType.Renban => new LineDashPattern(LineDashStyle.DashDot, 10f, 4f, 4f),
                LineType.Palindrome => new LineDashPattern(LineDashStyle.DoubleLine, 0f, 0f, 3f),
                LineType.Thermo => new LineDashPattern(LineDashStyle.Solid, 0f, 0f, 4f),
                LineType.BetweenLines => new LineDashPattern(LineDashStyle.Wavy, 6f, 0f, 4f),
                _ => LineDashPattern.SolidDefault
            };
        }

        public string GetDotLabel(DotType dotType)
        {
            if (!ShouldShowConstraintLabels) return null;
            return dotType == DotType.White ? "1" : "\u00d7";
        }

        public string GetMarkerLabel(MarkerType markerType)
        {
            if (!ShouldShowConstraintLabels) return null;
            return markerType == MarkerType.Even ? "E" : "O";
        }

        public string GetConstraintLabel(BossModifierId modifier)
        {
            if (!ShouldShowConstraintLabels) return null;
            return modifier switch
            {
                BossModifierId.ArrowSums => "\u03a3",
                BossModifierId.DifferenceKropki => "1",
                BossModifierId.RatioKropki => "\u00d7",
                BossModifierId.EvenOdd => "E/O",
                BossModifierId.FogOfWar => "?",
                _ => null
            };
        }

        public string GetThermoBulbLabel()
        {
            return ShouldShowConstraintLabels ? "\u25b2" : null;
        }

        public string GetBetweenEndpointLabel()
        {
            return ShouldShowConstraintLabels ? "\u2195" : null;
        }

        public string GetArrowCircleLabel()
        {
            return ShouldShowConstraintLabels ? "\u03a3" : null;
        }

        public string GetFogCellLabel()
        {
            return ShouldShowConstraintLabels ? "?" : null;
        }

        public float GetOverlayAlpha(float baseAlpha)
        {
            return IsHighContrast ? 0.85f : baseAlpha;
        }

        public float GetLineWidthMultiplier()
        {
            return IsHighContrast ? 2f : 1f;
        }
    }
}
