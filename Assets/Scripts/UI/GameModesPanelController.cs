using System;
using System.Collections.Generic;
using UnityEngine;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    public sealed class GameModesPanelController : MonoBehaviour
    {
        public SpiritTrialsTier SelectedTrialsTier { get; set; } = SpiritTrialsTier.Apprentice;
        public int EndlessZenSize { get; set; } = 9;
        public int EndlessZenStars { get; set; } = 2;

        // [HARMONY-UI-001] Harmony difficulty selection for GardenRun.
        public int SelectedHarmonyLevel { get; set; } = 0;
        public HarmonyPerkId SelectedHarmonyPerk { get; set; } = HarmonyPerkId.None;

        // [L5-B] Save-callback and save-file service injected by the owning screen.
        private SaveFileService _saveFile;

        /// <summary>
        /// [L5-B] Wire up the harmony node row so selection changes are persisted immediately.
        /// Call once after the panel is shown and meta is loaded.
        /// </summary>
        public void BindHarmonyNodeRow(HarmonyNodeRowController nodeRow, SaveFileService saveFile)
        {
            if (nodeRow == null) return;
            _saveFile = saveFile;
            nodeRow.OnNodeSelected -= OnHarmonyNodeSelected; // guard against double-bind
            nodeRow.OnNodeSelected += OnHarmonyNodeSelected;
        }

        private void OnHarmonyNodeSelected(int level)
        {
            SelectedHarmonyLevel = level;
            if (_saveFile == null) return;
            var envelope = _saveFile.Load();
            if (envelope.MetaProgress == null) envelope.MetaProgress = new MetaProgressionState();
            envelope.MetaProgress.LastSelectedHarmonyLevel = level;
            _saveFile.Save(envelope);
        }

        public void SelectTrialsTier(int tierIndex)
        {
            SelectedTrialsTier = (SpiritTrialsTier)tierIndex;
        }

        public void SetEndlessZenSize(int size)
        {
            EndlessZenSize = Mathf.Clamp(size, 5, 9);
        }

        public void SetEndlessZenStars(int stars)
        {
            EndlessZenStars = Mathf.Clamp(stars, 1, 6);
        }

        /// <summary>Sets the active Harmony level; clamped to [0, maxUnlocked].</summary>
        public void SetHarmonyLevel(int level, int maxUnlocked)
        {
            SelectedHarmonyLevel = Mathf.Clamp(level, 0, maxUnlocked);
        }

        /// <summary>Sets the active Harmony perk for the upcoming GardenRun.</summary>
        public void SetHarmonyPerk(int perkIndex)
        {
            SelectedHarmonyPerk = (HarmonyPerkId)perkIndex;
        }

        public void LoadFromMeta(MetaProgressionState meta)
        {
            var maxUnlocked = Mathf.Clamp(meta?.MaxUnlockedHarmonyLevel ?? 0, 0, 10);
            SelectedHarmonyLevel = Mathf.Clamp(meta?.LastSelectedHarmonyLevel ?? 0, 0, maxUnlocked);
            if (!HarmonyDifficultyService.IsPerkAvailable(SelectedHarmonyPerk, SelectedHarmonyLevel))
                SelectedHarmonyPerk = HarmonyPerkId.None;
        }

        public int GetMaxUnlockedHarmonyLevel(MetaProgressionState meta)
        {
            return Mathf.Clamp(meta?.MaxUnlockedHarmonyLevel ?? 0, 0, 10);
        }

        public void StepHarmonyLevel(int delta, MetaProgressionState meta)
        {
            var maxUnlocked = GetMaxUnlockedHarmonyLevel(meta);
            SelectedHarmonyLevel = Mathf.Clamp(SelectedHarmonyLevel + delta, 0, maxUnlocked);
            PersistHarmonyLevel(SelectedHarmonyLevel);
            if (!HarmonyDifficultyService.IsPerkAvailable(SelectedHarmonyPerk, SelectedHarmonyLevel))
                SelectedHarmonyPerk = HarmonyPerkId.None;
        }

        public void CycleHarmonyPerk(int delta)
        {
            var available = new List<HarmonyPerkId> { HarmonyPerkId.None };
            foreach (HarmonyPerkId perk in Enum.GetValues(typeof(HarmonyPerkId)))
            {
                if (perk == HarmonyPerkId.None) continue;
                if (HarmonyDifficultyService.IsPerkAvailable(perk, SelectedHarmonyLevel))
                    available.Add(perk);
            }

            if (available.Count == 0)
            {
                SelectedHarmonyPerk = HarmonyPerkId.None;
                return;
            }

            var currentIndex = available.IndexOf(SelectedHarmonyPerk);
            if (currentIndex < 0) currentIndex = 0;
            currentIndex = (currentIndex + delta) % available.Count;
            if (currentIndex < 0) currentIndex += available.Count;
            SelectedHarmonyPerk = available[currentIndex];
        }

        private void PersistHarmonyLevel(int level)
        {
            if (_saveFile == null)
                _saveFile = new SaveFileService(SaveProfileService.ActiveSlot);

            var envelope = _saveFile.Load();
            if (envelope.MetaProgress == null) envelope.MetaProgress = new MetaProgressionState();
            envelope.MetaProgress.LastSelectedHarmonyLevel = level;
            _saveFile.Save(envelope);
        }
    }
}
