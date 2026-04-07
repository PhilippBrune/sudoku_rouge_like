using UnityEngine;

namespace SudokuRoguelike.Core
{
    public static class HardeningConstants
    {
        public const int MinBoardSize = 5;
        public const int MaxBoardSize = 9;
        public const int MinStars = 1;
        public const int MaxStars = 6;
        public const int TutorialMaxStars = 7;
        public const int MaxFloors = 5;
        public const int MaxPrestigeTier = 9;
        public const int MaxLevel = 40;
        public const int MaxItemSlots = 5;
        public const float MinFontScale = 0.8f;
        public const float MaxFontScale = 1.5f;
        public const float MinVolume = 0f;
        public const float MaxVolume = 1f;
        public const int MaxModifiersPerBoss = 3;
        public const int MaxRewardSlots = 5;

        public static int ClampBoardSize(int size) => Mathf.Clamp(size, MinBoardSize, MaxBoardSize);
        public static int ClampStars(int stars) => Mathf.Clamp(stars, MinStars, MaxStars);
        public static int ClampTutorialStars(int stars) => Mathf.Clamp(stars, MinStars, TutorialMaxStars);
        public static float ClampVolume(float vol) => Mathf.Clamp01(vol);
        public static float ClampFontScale(float scale) => Mathf.Clamp(scale, MinFontScale, MaxFontScale);
    }
}
