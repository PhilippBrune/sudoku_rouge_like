using System;

namespace SudokuRoguelike.Core
{
    public static class StarDensityService
    {
        public static float MissingPercentForStars(int stars)
        {
            var clamped = Math.Clamp(stars, 1, 5);
            return Math.Clamp((clamped + 3) * 0.1f, 0.01f, 0.95f);
        }

        public static int MissingPercentLabelForStars(int stars)
        {
            return (int)MathF.Round(MissingPercentForStars(stars) * 100f);
        }
    }
}