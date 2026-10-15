using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Data
{
    public static class FloorThemeData
    {
        public static readonly string[] FloorNames =
        {
            "Bamboo Courtyard",
            "Moss Garden",
            "Koi Terrace",
            "Stone Lantern Walk",
            "Shrine Summit"
        };

        // The boss of each floor: its avatar (Resources/boss/icon_<key>) and its name (catalog key Floor.Boss.<key>).
        private static readonly string[] BossKeys = { "grove_spirit", "koi_warden", "shrine_oni", "stone_patriarch", "demon_mask" };
        private static readonly string[] BossNames = { "Grove Spirit", "Koi Warden", "Shrine Oni", "Stone Patriarch", "Demon Mask" };

        public static string GetBossKey(int floorIndex) =>
            BossKeys[Mathf.Clamp(floorIndex, 0, BossKeys.Length - 1)];

        public static string GetBossName(int floorIndex)
        {
            var i = Mathf.Clamp(floorIndex, 0, BossKeys.Length - 1);
            return LocalizationService.T("Floor.Boss." + BossKeys[i], BossNames[i]);
        }

        public static readonly int[] MinBoardSize = { 5, 6, 7, 8, 9 };
        public static readonly int[] MaxBoardSize = { 6, 7, 8, 9, 9 };

        public static string GetFloorName(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= FloorNames.Length)
                return FloorNames[0];
            return FloorNames[floorIndex];
        }

        public static int GetMinBoardSize(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= MinBoardSize.Length)
                return 5;
            return MinBoardSize[floorIndex];
        }

        public static int GetMaxBoardSize(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= MaxBoardSize.Length)
                return 9;
            return MaxBoardSize[floorIndex];
        }

        public static Color GetFloorTint(int floorIndex)
        {
            switch (floorIndex)
            {
                case 0: return new Color(0.30f, 0.55f, 0.25f, 1f);
                case 1: return new Color(0.20f, 0.45f, 0.30f, 1f);
                case 2: return new Color(0.25f, 0.40f, 0.60f, 1f);
                case 3: return new Color(0.50f, 0.40f, 0.30f, 1f);
                case 4: return new Color(0.60f, 0.30f, 0.25f, 1f);
                default: return Color.white;
            }
        }
    }
}
