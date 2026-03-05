using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class GameModesPanelController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private Text modesSummaryText;

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        private void Awake()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }
        }

        public void Configure(MainMenuController controller, Text summary)
        {
            mainMenuController = controller;
            modesSummaryText = summary;
            RefreshView();
        }

        public void RefreshView()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }

            if (modesSummaryText != null)
            {
                var debugAll = mainMenuController != null && mainMenuController.DebugEnableAllFeatures;
                var endless = (_profile.Meta.EndlessZenUnlocked || debugAll) ? "Unlocked" : "Locked";
                var trials = (_profile.Meta.SpiritTrialsUnlocked || debugAll) ? "Unlocked" : "Locked";
                var german = _profile.Options.Language == LanguageOption.German;
                if (german)
                {
                    endless = _profile.Meta.EndlessZenUnlocked ? "Freigeschaltet" : "Gesperrt";
                    trials = _profile.Meta.SpiritTrialsUnlocked ? "Freigeschaltet" : "Gesperrt";
                    modesSummaryText.text =
                        $"Gartenlauf: Freigeschaltet\n" +
                        $"Endlos-Zen: {endless}\n" +
                        $"Spirit Trials: {trials}\n\n" +
                        "Meta-Fortschritt für Demo-Freischaltungen nutzen.";
                }
                else
                {
                    modesSummaryText.text =
                        $"Garden Run: Unlocked\n" +
                        $"Endless Zen: {endless}\n" +
                        $"Spirit Trials: {trials}\n\n" +
                        (debugAll ? "Debug Enable-All active." : "Use Meta Progression panel for demo unlocks.");
                }
            }
        }

        public void StartGardenRun()
        {
            if (mainMenuController == null)
            {
                mainMenuController = FindFirstObjectByType<MainMenuController>();
            }

            mainMenuController?.StartMode(GameMode.GardenRun);
        }

        public void StartEndlessZen()
        {
            if (mainMenuController == null)
            {
                mainMenuController = FindFirstObjectByType<MainMenuController>();
            }

            RefreshView();
            if (!_profile.Meta.EndlessZenUnlocked && (mainMenuController == null || !mainMenuController.DebugEnableAllFeatures))
            {
                mainMenuController?.SetStatusExternal(_profile.Options.Language == LanguageOption.German
                    ? "Endlos-Zen ist gesperrt."
                    : "Endless Zen is locked.");
                return;
            }

            mainMenuController?.StartMode(GameMode.EndlessZen);
        }

        public void StartSpiritTrials()
        {
            if (mainMenuController == null)
            {
                mainMenuController = FindFirstObjectByType<MainMenuController>();
            }

            RefreshView();
            if (!_profile.Meta.SpiritTrialsUnlocked && (mainMenuController == null || !mainMenuController.DebugEnableAllFeatures))
            {
                mainMenuController?.SetStatusExternal(_profile.Options.Language == LanguageOption.German
                    ? "Spirit Trials ist gesperrt."
                    : "Spirit Trials is locked.");
                return;
            }

            mainMenuController?.StartMode(GameMode.SpiritTrials);
        }
    }
}
