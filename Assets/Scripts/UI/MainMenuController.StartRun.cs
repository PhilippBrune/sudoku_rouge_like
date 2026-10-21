using UnityEngine;
using UnityEngine.UI;
using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    // Starting a run from Select Class: the optional seed (GAP-020), the warning before a saved run is abandoned (GAP-032).
    public sealed partial class MainMenuController
    {
        private InputField _seedInput;
        private ClassSelectScreen _classSelectScreen;
        private bool _hasPendingSeed;
        private int _pendingSeed;

        public void SetClassSelectScreen(ClassSelectScreen screen)
        {
            _classSelectScreen = screen;
            if (screen == null) return;
            screen.HarmonyStep = StepHarmonyClassLevel;
            screen.PerkCycle = CyclePerkOnClassSelect;
        }

        /// <summary>[REQ: REQ-RUN-100] The optional seed on the class scroll (an empty field means a random seed).</summary>
        public void SetSeedInput(InputField input) => _seedInput = input;

        // Select Class opens with an empty seed field: a seed typed and left behind with Back would start the next run (the
        // confirmation window used to start empty every time).
        private void ClearSeed()
        {
            if (_seedInput != null) _seedInput.text = string.Empty;
        }

        /// <summary>The class chosen on Select Class (kept when a language change rebuilds the menu).</summary>
        public ClassId SelectedClass => _selectedClass;

        // [REQ: UI-STYLE-024] Begin on Select Class starts the run: the scroll shows what the confirmation window showed
        // (owner 2026-10-21). [REQ: REQ-RUN-101] With a saved run, Begin reads "Abandon & Begin" and the scroll warns.
        public void BeginRunFromSelect()
        {
            if (_gameBootstrap == null) return;
            if (!IsClassUnlockedOrDebug(_selectedClass, Profile.LoadMetaProgress()))
            {
                SetStatus(LocalizationService.T("MainMenu.Status.ClassLocked", "Class not yet unlocked."));
                return;
            }
            _hasPendingSeed = TryParseSeed(_seedInput != null ? _seedInput.text : null, out _pendingSeed);
            if (_seedInput != null) _seedInput.text = string.Empty;
            if (_gameBootstrap.HasResumableRun()) _gameBootstrap.AbandonSavedRun();
            ConfirmClassAndStart();
        }

        /// <summary>The perk button on Select Class (and Y): the next perk of the chosen Harmony level.</summary>
        public void CyclePerkOnClassSelect()
        {
            if (_gameModesController == null) return;
            _gameModesController.CycleHarmonyPerk(+1);
            RefreshHarmonyReadout();
        }

        private void ApplyPendingSeed(LaunchRequest request)
        {
            request.HasSeed = _hasPendingSeed;
            request.Seed = _pendingSeed;
            _hasPendingSeed = false;
        }

        /// <summary>Digits only, up to 9 of them; anything else falls back to a random seed.</summary>
        public static bool TryParseSeed(string text, out int seed)
        {
            seed = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            if (text.Length > 9) return false;
            foreach (var ch in text)
                if (ch < '0' || ch > '9') return false;
            return int.TryParse(text, out seed);
        }

        // GAP-029: modal action labels were 12 pt. They are enlarged per modal; the shared button factory keeps its
        // compact size (same approach as RewardViewController.ConfigureRestActionLabel).
        internal static void ConfigureModalActionLabel(Button button)
        {
            var label = button != null ? button.transform.Find("Label")?.GetComponent<Text>() : null;
            if (label == null) return;
            label.fontSize = 16;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 16;
        }
    }
}
