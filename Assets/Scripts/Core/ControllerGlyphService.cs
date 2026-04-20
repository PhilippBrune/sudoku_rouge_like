using UnityEngine;

namespace SudokuRoguelike.Core
{
    public enum ControllerPlatform { None, Xbox, PlayStation, Generic }

    /// <summary>
    /// Detects which gamepad platform is connected by examining <see cref="Input.GetJoystickNames"/>.
    /// Result is cached for <see cref="CheckInterval"/> seconds to avoid per-frame overhead.
    /// </summary>
    public static class ControllerGlyphService
    {
        private static ControllerPlatform _cached = ControllerPlatform.None;
        private static float _lastCheck = -99f;
        private const float CheckInterval = 2f;

        public static ControllerPlatform Detect()
        {
            if (Time.unscaledTime - _lastCheck < CheckInterval) return _cached;
            _lastCheck = Time.unscaledTime;
            _cached    = DetectNow();
            return _cached;
        }

        private static ControllerPlatform DetectNow()
        {
            var names     = Input.GetJoystickNames();
            var anyGeneric = false;
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n)) continue;
                var low = n.ToLowerInvariant();
                if (low.Contains("xbox")   || low.Contains("xinput") || low.Contains("045e"))
                    return ControllerPlatform.Xbox;
                if (low.Contains("dualshock") || low.Contains("dual shock") ||
                    low.Contains("dualsense") || low.Contains("dual sense") ||
                    low.Contains("wireless controller") ||
                    low.Contains("054c") || low.Contains("sony") || low.Contains("playstation"))
                    return ControllerPlatform.PlayStation;
                anyGeneric = true;
            }
            return anyGeneric ? ControllerPlatform.Generic : ControllerPlatform.None;
        }

        /// <summary>Small badge shown in the HUD: "[Xbox]", "[PS]", "[Ctrl]", or empty when no controller.</summary>
        public static string GetIndicatorLabel()
            => Detect() switch
            {
                ControllerPlatform.Xbox        => "[Xbox]",
                ControllerPlatform.PlayStation => "[PS]",
                ControllerPlatform.Generic     => "[Ctrl]",
                _                               => string.Empty,
            };

        /// <summary>
        /// Human-readable face button names for the four primary actions.
        /// Returns (confirm, back, pencil, undo).
        /// </summary>
        public static (string confirm, string back, string pencil, string undo) GetFaceLabels()
            => Detect() switch
            {
                ControllerPlatform.PlayStation => ("Cross", "Circle", "Square", "Triangle"),
                _                               => ("A",     "B",      "X",      "Y"),   // Xbox / Generic
            };

        /// <summary>Label for shoulder buttons (LB/L1, RB/R1).</summary>
        public static (string lb, string rb) GetShoulderLabels()
            => Detect() == ControllerPlatform.PlayStation ? ("L1", "R1") : ("LB", "RB");
    }
}
