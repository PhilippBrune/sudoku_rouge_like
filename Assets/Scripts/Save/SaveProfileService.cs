using System;
using System.IO;
using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Save
{
    /// <summary>
    /// Manages the three save-slot profile system.
    /// Each slot is a separate JSON file (save_profile_0/1/2.json).
    /// The active slot is persisted in PlayerPrefs so it survives across sessions.
    /// </summary>
    // [REQ: SAVE-SLOT-002] SaveProfileService: cross-slot manager - ActiveSlot stored in PlayerPrefs (key "ActiveProfileSlot"), 3 slots max
    public sealed class SaveProfileService
    {
        public const string ActiveSlotPrefKey = "ActiveProfileSlot";
        private static string SlotKey => TestSaves.PrefKey(ActiveSlotPrefKey);   // SAVE-TEST-001

        // ── Active slot ──────────────────────────────────────────────────────────

        public static int ActiveSlot
        {
            // Without a stored slot the most recently written profile is used and stored (EnsureActiveSlot), so every
            // reader agrees whichever runs first.
            get => PlayerPrefs.HasKey(SlotKey) ? PlayerPrefs.GetInt(SlotKey, 0) : EnsureActiveSlot();
            set
            {
                PlayerPrefs.SetInt(SlotKey, Math.Max(0, Math.Min(value, SaveFileService.MaxSlots - 1)));
                PlayerPrefs.Save();
            }
        }

        public SaveFileService GetActiveService() => new SaveFileService(ActiveSlot);

        /// <summary>
        /// Makes sure an active slot is stored and returns it. The active slot lives in PlayerPrefs (the registry on
        /// Windows) while the profiles are files, and Steam Cloud restores only the files (new PC, reinstall). Without a
        /// stored slot, the most recently written profile becomes active, so the player lands in their own profile
        /// instead of a new, empty slot 0. With no profile at all nothing is stored (a fresh install shows profile
        /// select).
        /// </summary>
        // [REQ: SAVE-SLOT-004] GAP-043: a missing active-slot pref selects the most recently written existing profile
        public static int EnsureActiveSlot()
        {
            if (PlayerPrefs.HasKey(SlotKey)) return PlayerPrefs.GetInt(SlotKey, 0);

            var newest = NewestExistingSlot();
            if (newest < 0) return 0;

            ActiveSlot = newest;
            Debug.Log($"[SaveProfile] No active slot stored; using slot {newest}, the most recently written profile.");
            return newest;
        }

        /// <summary>Slot whose profile file was written last, or -1 when no slot has a profile. Ties go to the lower slot.</summary>
        public static int NewestExistingSlot()
        {
            var newest = -1;
            var newestTime = DateTime.MinValue;
            for (var i = 0; i < SaveFileService.MaxSlots; i++)
            {
                var path = SaveFileService.GetSlotPath(i);
                if (!File.Exists(path)) continue;
                var written = File.GetLastWriteTimeUtc(path);
                if (newest >= 0 && written <= newestTime) continue;
                newest = i;
                newestTime = written;
            }
            return newest;
        }

        // ── Slot summaries ───────────────────────────────────────────────────────

        /// <summary>Returns one summary per slot (always MaxSlots entries).</summary>
        public ProfileSlotSummary[] GetAllSlotSummaries()
        {
            var summaries = new ProfileSlotSummary[SaveFileService.MaxSlots];
            for (var i = 0; i < SaveFileService.MaxSlots; i++)
                summaries[i] = BuildSummary(i);
            return summaries;
        }

        private static ProfileSlotSummary BuildSummary(int slot)
        {
            var svc = new SaveFileService(slot);

            if (!svc.HasSaveFile())
                return new ProfileSlotSummary { SlotIndex = slot, IsEmpty = true, DisplayName = $"Profile {slot + 1}" };

            var env = svc.Load();

            var lastClass = ClassId.NumberFreak;
            if (env.ActiveRunState != null)
                lastClass = env.ActiveRunState.ClassId;
            else if (env.MetaProgress?.GardenProgression?.ClassEntries != null
                     && env.MetaProgress.GardenProgression.ClassEntries.Count > 0)
                lastClass = env.MetaProgress.GardenProgression.ClassEntries[0].ClassId;

            return new ProfileSlotSummary
            {
                SlotIndex    = slot,
                IsEmpty      = false,
                DisplayName  = $"Profile {slot + 1}",
                LastPlayedClass    = lastClass,
                TotalRunsStarted   = env.Statistics?.TotalRunsStarted   ?? 0,
                TotalRunsCompleted = env.Statistics?.TotalRunsCompleted  ?? 0,
                TotalBossesDefeated= env.Statistics?.TotalBossesDefeated ?? 0,
                HasActiveRun = env.ActiveRunState != null && !env.ActiveRunState.TutorialMode,
                LastPlayedUtc= env.TimestampUtc ?? ""
            };
        }

        // ── Select / create ──────────────────────────────────────────────────────

        /// <summary>Switch the active slot. Creates an empty save file if the slot is new.</summary>
        public void SelectSlot(int slot)
        {
            ActiveSlot = slot;
            var svc = new SaveFileService(slot);
            if (!svc.HasSaveFile())
                svc.Save(new SaveFileEnvelope { TimestampUtc = DateTime.UtcNow.ToString("o") });
        }

        // ── Delete ───────────────────────────────────────────────────────────────

        public void DeleteSlot(int slot)
        {
            new SaveFileService(slot).DeleteSaveFile();

            // If we deleted the active slot, fall back to slot 0
            if (ActiveSlot == slot)
                ActiveSlot = 0;
        }

        // ── Any non-empty slot? ──────────────────────────────────────────────────

        public bool AnySlotExists()
        {
            for (var i = 0; i < SaveFileService.MaxSlots; i++)
                if (new SaveFileService(i).HasSaveFile()) return true;
            return false;
        }
    }
}
