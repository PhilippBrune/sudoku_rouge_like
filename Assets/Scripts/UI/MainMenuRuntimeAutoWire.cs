using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuRuntimeAutoWire : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;

        private void Start()
        {
            if (mainMenuController == null)
            {
                mainMenuController = FindFirstObjectByType<MainMenuController>();
            }

            if (mainMenuController == null)
            {
                Debug.LogError("MainMenuRuntimeAutoWire: MainMenuController not found.");
                return;
            }

            WireButton("BtnStart", mainMenuController.StartGame, required: true);
            WireButton("BtnResume", mainMenuController.ResumeGame, required: true);
            WireButton("BtnTutorial", mainMenuController.OpenTutorial, required: true);
            WireButton("BtnClassSelectContinue", mainMenuController.ConfirmClassAndStart, required: false);
            WireButton("BtnClassSelectBack", mainMenuController.BackFromClassSelect, required: false);
            WireButton("BtnStartClassNumberFreak", mainMenuController.SelectClassNumberFreak, required: false);
            WireButton("BtnStartClassGardenMonk", mainMenuController.SelectClassGardenMonk, required: false);
            WireButton("BtnStartClassShrineArchivist", mainMenuController.SelectClassShrineArchivist, required: false);
            WireButton("BtnStartClassKoiGambler", mainMenuController.SelectClassKoiGambler, required: false);
            WireButton("BtnStartClassStoneGardener", mainMenuController.SelectClassStoneGardener, required: false);
            WireButton("BtnStartClassLanternSeer", mainMenuController.SelectClassLanternSeer, required: false);
            WireButton("BtnMeta", mainMenuController.OpenMetaProgression, required: false);
            WireButton("BtnModes", mainMenuController.OpenGameModes, required: false);
            WireButton("BtnItems", mainMenuController.OpenItems, required: false);
            WireButton("BtnOptions", mainMenuController.OpenOptions, required: true);
            WireButton("BtnCredits", mainMenuController.OpenCredits, required: true);
            WireButton("BtnQuit", mainMenuController.OpenQuitConfirmation, required: true);
            WireButton("BtnOptionsBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnCreditsBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnTutorialBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnTutorialProgress", mainMenuController.OpenTutorialProgress, required: false);
            WireButton("BtnProgressBackToSetup", mainMenuController.OpenTutorial, required: false);
            WireButton("BtnTutorialProgressBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnMetaBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnModesBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnModeGardenRun", () => FindFirstObjectByType<GameModesPanelController>()?.StartGardenRun(), required: false);
            WireButton("BtnModeEndless", () => FindFirstObjectByType<GameModesPanelController>()?.StartEndlessZen(), required: false);
            WireButton("BtnModeTrials", () => FindFirstObjectByType<GameModesPanelController>()?.StartSpiritTrials(), required: false);
            WireButton("BtnModesRefresh", () => FindFirstObjectByType<GameModesPanelController>()?.RefreshView(), required: false);
            WireButton("BtnItemsBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnConflictBack", mainMenuController.BackToMainMenu, required: false);
            WireButton("BtnConflictKeepLocal", mainMenuController.ResolveConflictKeepLocal, required: false);
            WireButton("BtnConflictKeepCloud", mainMenuController.ResolveConflictKeepCloud, required: false);
            WireButton("BtnConflictCancel", mainMenuController.ResolveConflictCancel, required: false);
            WireButton("BtnConfirmQuit", mainMenuController.ConfirmQuit, required: false);
            WireButton("BtnCancelQuit", mainMenuController.CancelQuit, required: false);
            WireButton("BtnDeleteSave", mainMenuController.OpenDeleteSaveConfirmation, required: false);
            WireButton("BtnConfirmDeleteSave", mainMenuController.ConfirmDeleteSave, required: false);
            WireButton("BtnCancelDeleteSave", mainMenuController.CancelDeleteSave, required: false);
            WireButton("BtnDeleteSaveBack", mainMenuController.BackToOptions, required: false);
            WireButton("BtnOnboardingNext", mainMenuController.OnboardingNext, required: false);
            WireButton("BtnOnboardingBack", mainMenuController.OnboardingBack, required: false);
            WireButton("BtnOnboardingSkip", mainMenuController.OnboardingSkip, required: false);
            WireButton("BtnOnboardingStart", mainMenuController.OnboardingComplete, required: false);
            WireSlider("MasterVolumeSlider", mainMenuController.OnMasterVolumeChanged, required: false);
            WireSlider("MusicVolumeSlider", mainMenuController.OnMusicVolumeChanged, required: false);
            WireSlider("SfxVolumeSlider", mainMenuController.OnSfxVolumeChanged, required: false);
            WireDropdown("MusicStyleDropdown", mainMenuController.OnMenuMusicStyleChanged, required: false);
            WireDropdown("LanguageDropdown", mainMenuController.OnLanguageChanged, required: false);
            WireDropdown("ResolutionDropdown", mainMenuController.OnResolutionChanged, required: false);
            WireToggle("HighlightErrorsToggle", mainMenuController.OnHighlightErrorsChanged, required: false);
            WireToggle("DebugEnableAllToggle", mainMenuController.OnDebugEnableAllChanged, required: false);

            Debug.Log("MainMenuRuntimeAutoWire: Menu buttons wired at runtime.");
        }

        public void Configure(MainMenuController controller)
        {
            mainMenuController = controller;
        }

        private static void WireButton(string buttonName, UnityEngine.Events.UnityAction callback, bool required)
        {
            var button = FindByName<Button>(buttonName);
            if (button == null)
            {
                if (required)
                {
                    Debug.LogWarning($"MainMenuRuntimeAutoWire: '{buttonName}' not found.");
                }

                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }

        private static void WireSlider(string sliderName, UnityEngine.Events.UnityAction<float> callback, bool required)
        {
            var slider = FindByName<Slider>(sliderName);
            if (slider == null)
            {
                if (required)
                {
                    Debug.LogWarning($"MainMenuRuntimeAutoWire: '{sliderName}' not found.");
                }

                return;
            }

            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(callback);
        }

        private static void WireDropdown(string dropdownName, UnityEngine.Events.UnityAction<int> callback, bool required)
        {
            var dropdown = FindByName<Dropdown>(dropdownName);
            if (dropdown == null)
            {
                if (required)
                {
                    Debug.LogWarning($"MainMenuRuntimeAutoWire: '{dropdownName}' not found.");
                }

                return;
            }

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(callback);
        }

        private static void WireToggle(string toggleName, UnityEngine.Events.UnityAction<bool> callback, bool required)
        {
            var toggle = FindByName<Toggle>(toggleName);
            if (toggle == null)
            {
                if (required)
                {
                    Debug.LogWarning($"MainMenuRuntimeAutoWire: '{toggleName}' not found.");
                }

                return;
            }

            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(callback);
        }

        private static T FindByName<T>(string name) where T : Component
        {
            var all = Resources.FindObjectsOfTypeAll<T>();
            for (var i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                var go = candidate.gameObject;
                if (go == null || go.name != name)
                {
                    continue;
                }

                var scene = go.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
