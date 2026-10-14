using System;
using System.IO;
using UnityEngine;

namespace SudokuRoguelike.Save
{
    /// <summary>
    /// One-time copy of save files from folders that earlier builds used. Unity derives the save folder
    /// (LocalLow/&lt;company&gt;/&lt;product&gt;) from the Player Settings names; the released builds use
    /// Kratossos / sudoku_rouge_like (SAVE-FOLDER-001), while pre-release Steam builds (June/early July 2026) used
    /// DefaultCompany / RunOfTheNine. Originals are never deleted or overwritten.
    /// </summary>
    // [REQ: SAVE-FOLDER-001] the save folder is LocalLow/Kratossos/sudoku_rouge_like (Player Settings names of the
    // released game); it is never moved, only earlier folders are copied into it.
    // [REQ: SAVE-MIGRATE-001] saves from LocalLow/DefaultCompany/RunOfTheNine are copied when the current save folder
    // has no profile; the newest copied profile becomes the active slot.
    public static class LegacySaveMigration
    {
        /// <summary>Earlier save folders, relative to LocalLow, newest naming first.</summary>
        public static readonly string[] LegacyFolders =
        {
            Path.Combine("DefaultCompany", "RunOfTheNine")
        };

        private const string ProfilePattern = "save_profile_*.json";

#if !UNITY_EDITOR
        // Player only: in the Editor (and in test runs) the real save folders are left alone.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunOnStartup()
        {
            try
            {
                if (!string.IsNullOrEmpty(SaveFileService.RootDirectoryOverride)) return;
                var saveRoot = Application.persistentDataPath;
                var localLow = Directory.GetParent(saveRoot)?.Parent?.FullName;
                var slot = Run(saveRoot, localLow);
                if (slot >= 0) SaveProfileService.ActiveSlot = slot;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveMigration] Skipped: {ex.Message}");
            }
        }
#endif

        /// <summary>
        /// Copies the profile files (and their .bak copies) of the first legacy folder that has any profile into
        /// <paramref name="saveRoot"/>, if <paramref name="saveRoot"/> has none. Returns the slot of the most recently
        /// written copied profile, or -1 when nothing was copied.
        /// </summary>
        public static int Run(string saveRoot, string localLowRoot)
        {
            if (string.IsNullOrEmpty(saveRoot) || string.IsNullOrEmpty(localLowRoot)) return -1;
            if (HasProfiles(saveRoot)) return -1;

            foreach (var relative in LegacyFolders)
            {
                var source = Path.Combine(localLowRoot, relative);
                if (SamePath(source, saveRoot) || !HasProfiles(source)) continue;

                Directory.CreateDirectory(saveRoot);
                var newestSlot = -1;
                var newestTime = DateTime.MinValue;
                foreach (var file in Directory.GetFiles(source, "save_profile_*"))
                {
                    var name = Path.GetFileName(file);
                    if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                        && !name.EndsWith(".json.bak", StringComparison.OrdinalIgnoreCase)) continue;
                    var target = Path.Combine(saveRoot, name);
                    if (!File.Exists(target)) File.Copy(file, target);

                    if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                    var written = File.GetLastWriteTimeUtc(file);
                    var slot = SlotOf(name);
                    if (slot >= 0 && written >= newestTime)
                    {
                        newestTime = written;
                        newestSlot = slot;
                    }
                }

                Debug.Log($"[SaveMigration] Copied profiles from {source} to {saveRoot}; active slot {newestSlot}.");
                return newestSlot;
            }

            return -1;
        }

        private static bool HasProfiles(string folder) =>
            Directory.Exists(folder) && Directory.GetFiles(folder, ProfilePattern).Length > 0;

        private static int SlotOf(string fileName)
        {
            var digits = fileName.Substring("save_profile_".Length).Split('.')[0];
            return int.TryParse(digits, out var slot) && slot >= 0 && slot < SaveFileService.MaxSlots ? slot : -1;
        }

        private static bool SamePath(string a, string b) =>
            string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'),
                StringComparison.OrdinalIgnoreCase);
    }
}
