using UnityEngine;

namespace SudokuRoguelike.Core
{
    public static class StarDensityService
    {
        // [REQ: GEN-REMOVE-002] MissingPercentForStars: linear formula (stars+3)*0.1 — 1★=40%, 4★=70%, 7★=100%
        // [REQ: GEN-REMOVE-003] 7★ returns 1.0 (100% removed = empty grid); SudokuGenerator bypasses totalCells−1 clamp for this value
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
