using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Save
{
    /// <summary>
    /// [REQ: DISPLAY-MODE-008] The display mode and size belong to the PC, not to the profile: the profile travels with
    /// Steam Cloud, so a Legion Go's exclusive 1920×1200 reached a 3440×1440 desktop (GAP-074). They are kept in this PC's
    /// PlayerPrefs; the profile's fields only show them.
    /// </summary>
    public static class DisplayPrefs
    {
        private static string ModeKey => TestSaves.PrefKey("Display.Mode");
        private static string WidthKey => TestSaves.PrefKey("Display.Width");
        private static string HeightKey => TestSaves.PrefKey("Display.Height");

        public static bool TryLoad(out DisplayModeOption mode, out int width, out int height)
        {
            mode = DisplayModeOption.Borderless;
            width = height = 0;
            if (!PlayerPrefs.HasKey(ModeKey)) return false;
            mode = (DisplayModeOption)PlayerPrefs.GetInt(ModeKey);
            width = PlayerPrefs.GetInt(WidthKey, 0);
            height = PlayerPrefs.GetInt(HeightKey, 0);
            return true;
        }

        public static void Store(GraphicsSettingsModel graphics)
        {
            PlayerPrefs.SetInt(ModeKey, (int)graphics.DisplayMode);
            PlayerPrefs.SetInt(WidthKey, graphics.ResolutionWidth);
            PlayerPrefs.SetInt(HeightKey, graphics.ResolutionHeight);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(ModeKey);
            PlayerPrefs.DeleteKey(WidthKey);
            PlayerPrefs.DeleteKey(HeightKey);
        }
    }
}
