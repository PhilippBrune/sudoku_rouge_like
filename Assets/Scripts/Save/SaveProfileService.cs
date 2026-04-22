using System;
using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Save
{
    /// <summary>
    /// Manages the three save-slot profile system.
    /// Each slot is a separate JSON file (save_profile_0/1/2.json).
    /// The active slot is persisted in PlayerPrefs so it survives across sessions.
    /// </summary>
    public sealed class SaveProfileService
    {
        private const string ActiveSlotPrefKey = "ActiveProfileSlot";

        // ── Active slot ──────────────────────────────────────────────────────────

        public static int ActiveSlot
        {
            get => PlayerPrefs.GetInt(ActiveSlotPrefKey, 0);
            set
            {
                PlayerPrefs.SetInt(ActiveSlotPrefKey, Math.Max(0, Math.Min(value, SaveFileService.MaxSlots - 1)));
                PlayerPrefs.Save();
            }
        }

        public SaveFileService GetActiveService() => new SaveFileService(ActiveSlot);

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
