using UnityEngine;

namespace SudokuRoguelike.Core
{
    public static class StarDensityService
    {
        public static float MissingPercentForStars(int stars)
        {
            stars = Mathf.Clamp(stars, 1, 7);
            return (stars + 3) * 0.1f;
        }

        public static int GetStars(DifficultyTier tier)
        {
            return (int)tier;
        }
    }
}
