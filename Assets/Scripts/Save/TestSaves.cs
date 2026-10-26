using System;
using System.IO;
using UnityEngine;

namespace SudokuRoguelike.Save
{
    /// <summary>
    /// [REQ: SAVE-TEST-001] The Editor and development builds keep their profiles in a folder of their own ("test-saves"
    /// inside the real save folder) and start from an empty one at every start, so testing never touches a player's
    /// saves and always begins as a new game. A file named ".keep" in that folder keeps its saves for the next start.
    /// Release builds are not affected (SAVE-FOLDER-001).
    /// </summary>
    public static class TestSaves
    {
        public const string FolderName = "test-saves";
        public const string KeepMarker = ".keep";

        public static bool Active => Application.isEditor || Debug.isDebugBuild;
        public static string Folder => Path.Combine(Application.persistentDataPath, FolderName);

        /// <summary>A PlayerPrefs key of its own for test starts: a development build shares the player's registry key.</summary>
        public static string PrefKey(string key) => Active ? "test." + key : key;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StartFresh()
        {
            if (Active && string.IsNullOrEmpty(SaveFileService.RootDirectoryOverride)) Clear(Folder);
        }

        /// <summary>Deletes the test profiles and the test PlayerPrefs, unless the folder holds the keep marker.</summary>
        public static void Clear(string folder)
        {
            try
            {
                if (File.Exists(Path.Combine(folder, KeepMarker))) return;
                if (Directory.Exists(folder))
                    foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"[TestSaves] Could not empty {folder}: {ex.Message}");
            }
            PlayerPrefs.DeleteKey(PrefKey(SaveProfileService.ActiveSlotPrefKey));
            for (var slot = 0; slot < SaveFileService.MaxSlots; slot++)
                PlayerPrefs.DeleteKey(PrefKey(Core.InputRemapService.PrefsKeyFor(slot)));
            PlayerPrefs.Save();
        }
    }
}
