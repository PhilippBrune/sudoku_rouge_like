using System;

namespace SudokuRoguelike.Core
{
    public static class StarDensityService
    {
        public static float MissingPercentForStars(int stars)
        {
            // 7★ = 100% missing (0 givens, tutorial only)
            if (stars >= 7) return 1.0f;
            var clamped = Math.Clamp(stars, 1, 6);
            return Math.Clamp((clamped + 3) * 0.1f, 0.01f, 0.95f);
        }

        public static int MissingPercentLabelForStars(int stars)
        {
            return (int)MathF.Round(MissingPercentForStars(stars) * 100f);
        }
    }
}