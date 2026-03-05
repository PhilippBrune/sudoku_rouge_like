using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private SaveConflictDecision defaultConflictDecision = SaveConflictDecision.KeepLocal;
        [SerializeField] private string gameplaySceneName = "Prototype";
        [SerializeField] private bool logOnlyForOptionsAndCredits = true;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject classSelectPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject tutorialSetupPanel;
        [SerializeField] private GameObject tutorialProgressPanel;
        [SerializeField] private GameObject metaProgressionPanel;
        [SerializeField] private GameObject gameModesPanel;
        [SerializeField] private GameObject itemsPanel;
        [SerializeField] private GameObject saveConflictPanel;
        [SerializeField] private GameObject confirmQuitPanel;
        [SerializeField] private GameObject confirmDeleteSavePanel;
        [SerializeField] private GameObject onboardingPanel;
        [SerializeField] private Text onboardingBodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text classSelectClassText;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Dropdown languageDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle highContrastToggle;
        [SerializeField] private Toggle highlightErrorsToggle;
        [SerializeField] private Toggle debugEnableAllToggle;
        [SerializeField] private OptionsController optionsController;
        [SerializeField] private TutorialMenuController tutorialMenuController;
        [SerializeField] private MetaProgressionPanelController metaProgressionController;
        [SerializeField] private GameModesPanelController gameModesController;
        [SerializeField] private ItemsMenuController itemsMenuController;
        [SerializeField] private ClassId selectedClass = ClassId.NumberFreak;

        private const string OnboardingSeenKey = "sr_onboarding_seen";
        private int _onboardingIndex;
        private SaveFileEnvelope _pendingConflictEnvelope;

        private readonly MenuFlowService _menu = new();
        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();
        private readonly SudokuRoguelike.Meta.ClassGardenProgressionService _classProgression = new();
        private readonly ICloudSaveProvider _cloud = new LocalCloudSaveProvider();
        private SaveConflictService _conflicts;
        private static bool _debugEnableAllFeatures;

        public MenuFlowService Menu => _menu;
        public bool DebugEnableAllFeatures => _debugEnableAllFeatures;

        private void Awake()
        {
            _conflicts = new SaveConflictService(_save, _cloud);

            if (optionsController == null)
            {
                optionsController = GetComponent<OptionsController>();
            }

            if (tutorialMenuController == null)
            {
                tutorialMenuController = GetComponent<TutorialMenuController>();
            }

            if (metaProgressionController == null)
            {
                metaProgressionController = GetComponent<MetaProgressionPanelController>();
            }

            if (gameModesController == null)
            {
                gameModesController = GetComponent<GameModesPanelController>();
            }

            if (itemsMenuController == null)
            {
                itemsMenuController = GetComponent<ItemsMenuController>();
            }
        }

        private void Start()
        {
            if (mainMenuPanel == null)
            {
                var builder = FindFirstObjectByType<MainMenuBlueprintBuilder>();
                if (builder != null)
                {
                    builder.Build();
                }
            }

            if (PlayerPrefs.GetInt(OnboardingSeenKey, 0) == 0)
            {
                OpenOnboarding();
                return;
            }

            ShowMainMenu();
            SetStatus("Ready.");
            SyncOptionsWidgetsFromProfile();
            ApplyLanguageToVisibleUi(optionsController != null ? optionsController.Options.Language : LanguageOption.English);
        }

        public void StartGame()
        {
            _menu.OnStartGame();
            ShowClassSelect();
            RefreshClassSelectUi();
            SetStatus("Select class, then continue.");
        }

        public void OpenTutorial()
        {
            _menu.OnTutorial();
            ShowTutorialSetup();
            SetStatus("Configure tutorial session.");
        }

        public void StartTutorialGame()
        {
            StartTutorialGame(new TutorialSetupConfig
            {
                BoardSize = 5,
                Stars = 1,
                ResourceMode = TutorialResourceMode.Simulation
            });
        }

        public void StartTutorialGame(TutorialSetupConfig setup)
        {
            Debug.Log($"MainMenuController: StartTutorialGame pressed. Target scene='{gameplaySceneName}'");
            SetStatus("Loading tutorial...");
            _menu.OnTutorial();
            _menu.ConfirmTutorialSetup(setup);
            LaunchRequestContext.Request(new LaunchRequest
            {
                Mode = GameMode.Tutorial,
                TutorialSetup = setup
            });
            LoadGameplayScene();
        }

        public void OpenTutorialProgress()
        {
            _menu.OpenTutorialProgress();
            tutorialMenuController?.RefreshProgressView();
            ShowTutorialProgress();
            SetStatus("Tutorial progress.");
        }

        public void OpenMetaProgression()
        {
            _menu.OpenMeta();
            metaProgressionController?.RefreshView();
            ShowMetaProgression();
            SetStatus("Meta progression.");
        }

        public void OpenGameModes()
        {
            _menu.OpenModes();
            gameModesController?.RefreshView();
            ShowGameModes();
            SetStatus("Select game mode.");
        }

        public void OpenItems()
        {
            itemsMenuController?.RefreshView();
            ShowItems();
            SetStatus("Items archive.");
        }

        public void StartMode(GameMode mode)
        {
            Debug.Log($"MainMenuController: StartMode pressed. Mode={mode}, Class={selectedClass}, scene='{gameplaySceneName}'");
            SetStatus($"Loading {mode}...");
            _menu.SetMode(mode);
            LaunchRequestContext.Request(new LaunchRequest
            {
                Mode = mode,
                ClassId = selectedClass
            });
            LoadGameplayScene();
        }

        public void SetSelectedClass(ClassId classId)
        {
            if (!IsClassUnlockedOrDebug(classId))
            {
                SetStatus($"{classId} is locked.");
                return;
            }

            selectedClass = classId;
            RefreshClassSelectUi();
            SetStatus($"Selected class: {selectedClass}");
        }

        public void SelectClassNumberFreak() => SetSelectedClass(ClassId.NumberFreak);
        public void SelectClassGardenMonk() => SetSelectedClass(ClassId.GardenMonk);
        public void SelectClassShrineArchivist() => SetSelectedClass(ClassId.ShrineArchivist);
        public void SelectClassKoiGambler() => SetSelectedClass(ClassId.KoiGambler);
        public void SelectClassStoneGardener() => SetSelectedClass(ClassId.StoneGardener);
        public void SelectClassLanternSeer() => SetSelectedClass(ClassId.LanternSeer);

        public void ConfirmClassAndStart()
        {
            StartMode(GameMode.GardenRun);
        }

        public void BackFromClassSelect()
        {
            BackToMainMenu();
        }

        public void SetStatusExternal(string message)
        {
            SetStatus(message);
        }

        public void ResumeGame()
        {
            var hasSave = false;
            SaveFileEnvelope envelope = null;

            if (_conflicts.HasRunConflict())
            {
                _pendingConflictEnvelope = null;
                OpenSaveConflictPanel();
                if (_conflicts.TryBuildRunConflictSummary(out var summary))
                {
                    SetStatus($"Save conflict detected. {summary} Choose Local, Cloud, or Cancel.");
                }
                else
                {
                    SetStatus("Save conflict detected. Choose Local, Cloud, or Cancel.");
                }
                return;
            }

            if (_conflicts.TryResolveRunConflict(defaultConflictDecision, out envelope))
            {
                hasSave = envelope?.ActiveRunState != null;
                if (hasSave && runMapController != null)
                {
                    hasSave = runMapController.ResumeFromEnvelope(envelope);
                }
            }

            if (!hasSave && _save.TryRestoreLatestRunBackup() && _save.TryLoadRun(out var restoredEnvelope))
            {
                envelope = restoredEnvelope;
                hasSave = envelope?.ActiveRunState != null;
                if (hasSave && runMapController != null)
                {
                    hasSave = runMapController.ResumeFromEnvelope(envelope);
                }

                if (hasSave)
                {
                    SetStatus("Run save was restored from latest backup.");
                }
            }

            _menu.Session.HasRunInProgress = hasSave;
            _menu.OnResumeGame(saveValid: hasSave);

            if (hasSave)
            {
                SetStatus(envelope != null && envelope.ActivePuzzle != null && envelope.ActivePuzzle.IsBoss
                    ? "Resuming mid-boss encounter..."
                    : "Resuming mid-run...");
                LoadGameplayScene();
            }
            else
            {
                Debug.LogWarning("MainMenuController: No valid run save found for Resume.");
                SetStatus("No valid run save found.");
            }
        }

        public void OpenOptions()
        {
            _menu.OpenOptions();
            ShowOptions();
            SyncOptionsWidgetsFromProfile();
            if (logOnlyForOptionsAndCredits)
            {
                Debug.Log("MainMenuController: Options selected.");
            }
        }

        public void OpenCredits()
        {
            _menu.OpenCredits();
            ShowCredits();
            if (logOnlyForOptionsAndCredits)
            {
                Debug.Log("MainMenuController: Credits selected.");
            }
        }

        public void BackToMainMenu()
        {
            ShowMainMenu();
            SetStatus("Ready.");
        }

        public void BackToOptions()
        {
            ShowOptions();
            SetStatus("Options.");
        }

        public void OnMasterVolumeChanged(float value)
        {
            if (optionsController == null)
            {
                optionsController = GetComponent<OptionsController>();
            }

            optionsController?.SetMasterVolume(value);
            SetStatus($"Master Volume: {Mathf.RoundToInt(value * 100f)}%");
        }

        public void OnMusicVolumeChanged(float value)
        {
            optionsController?.SetMusicVolume(value);
            SetStatus($"Music Volume: {Mathf.RoundToInt(value * 100f)}%");
        }

        public void OnSfxVolumeChanged(float value)
        {
            optionsController?.SetSfxVolume(value);
            SetStatus($"SFX Volume: {Mathf.RoundToInt(value * 100f)}%");
        }

        public void OnLanguageChanged(int index)
        {
            try
            {
                var lang = index == 1 ? LanguageOption.German : LanguageOption.English;
                optionsController?.SetLanguage(lang);
                ApplyLanguageToVisibleUi(lang);
                SetStatus(lang == LanguageOption.German
                    ? "Sprache auf Deutsch gestellt."
                    : "Language set to English.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MainMenuController: Language dropdown failed: {ex}");
                ShowOptions();
                SetStatus("Language change failed. Options kept open.");
            }
        }

        public void OnResolutionChanged(int index)
        {
            try
            {
                var width = 1920;
                var height = 1080;
                var fullscreen = true;

                switch (index)
                {
                    case 0:
                        width = 1280;
                        height = 720;
                        fullscreen = false;
                        break;
                    case 1:
                        width = 1600;
                        height = 900;
                        fullscreen = false;
                        break;
                    case 2:
                        width = 1920;
                        height = 1080;
                        fullscreen = true;
                        break;
                    case 3:
                        width = 2560;
                        height = 1440;
                        fullscreen = true;
                        break;
                }

                optionsController?.SetResolution(width, height, fullscreen);
                if (optionsController != null && optionsController.RequiresRestartForResolutionModeSwitch(fullscreen))
                {
                    SetStatus("Resolution applied. Restart recommended for fullscreen mode switch.");
                }
                else
                {
                    SetStatus($"Resolution changed to {width}x{height}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MainMenuController: Resolution dropdown failed: {ex}");
                ShowOptions();
                SetStatus("Resolution change failed. Options kept open.");
            }
        }

        public void OnHighContrastChanged(bool enabled)
        {
            optionsController?.SetHighContrast(enabled);
            SetStatus(enabled ? "High contrast enabled." : "High contrast disabled.");
        }

        public void OnHighlightErrorsChanged(bool enabled)
        {
            optionsController?.SetHighlightConflicts(enabled);
            SetStatus(enabled ? "Error highlighting enabled." : "Error highlighting disabled.");
        }

        public void OnDebugEnableAllChanged(bool enabled)
        {
            _debugEnableAllFeatures = enabled;
            if (enabled)
            {
                ApplyDebugUnlocks();
            }

            SetStatus(enabled ? "Debug: all progression locks disabled." : "Debug: progression locks enabled.");
            metaProgressionController?.RefreshView();
            gameModesController?.RefreshView();
            itemsMenuController?.RefreshView();
            RefreshClassSelectUi();
        }

        public void OpenQuitConfirmation()
        {
            ShowQuitConfirm();
            SetStatus("Confirm quit?");
        }

        public void ConfirmQuit()
        {
            ExitGame();
        }

        public void CancelQuit()
        {
            BackToMainMenu();
        }

        public void OpenDeleteSaveConfirmation()
        {
            ShowDeleteSaveConfirm();
            SetStatus("Delete run/profile save? This cannot be undone.");
        }

        public void ConfirmDeleteSave()
        {
            var deletedRun = _save.DeleteRunSave();
            var deletedProfile = _save.DeleteProfileSave();
            ShowMainMenu();
            SetStatus(deletedRun || deletedProfile ? "Save files deleted." : "No save files found to delete.");
        }

        public void CancelDeleteSave()
        {
            BackToOptions();
        }

        public void ResolveConflictKeepLocal() => ResolveConflictAndResume(SaveConflictDecision.KeepLocal);
        public void ResolveConflictKeepCloud() => ResolveConflictAndResume(SaveConflictDecision.KeepCloud);
        public void ResolveConflictCancel()
        {
            _pendingConflictEnvelope = null;
            BackToMainMenu();
            SetStatus("Resume canceled.");
        }

        public void OnboardingNext()
        {
            _onboardingIndex = Mathf.Clamp(_onboardingIndex + 1, 0, 2);
            RefreshOnboardingText();
            Debug.Log($"OnboardingNext clicked. Step={_onboardingIndex}");
        }

        public void OnboardingBack()
        {
            _onboardingIndex = Mathf.Clamp(_onboardingIndex - 1, 0, 2);
            RefreshOnboardingText();
            Debug.Log($"OnboardingBack clicked. Step={_onboardingIndex}");
        }

        public void OnboardingSkip()
        {
            Debug.Log("OnboardingSkip clicked.");
            CompleteOnboarding();
        }

        public void OnboardingComplete()
        {
            Debug.Log("OnboardingComplete clicked.");
            CompleteOnboarding();
        }

        public void ConfigureUi(
            GameObject menuPanel,
            GameObject classPanel,
            GameObject options,
            GameObject credits,
            Text status,
            Text classSelectText,
            Slider volumeSlider,
            OptionsController optionsService,
            GameObject tutorialSetup = null,
            GameObject tutorialProgress = null,
            TutorialMenuController tutorialController = null,
            GameObject metaPanel = null,
            GameObject modesPanel = null,
            GameObject itemsArchivePanel = null,
            MetaProgressionPanelController metaController = null,
            GameModesPanelController modesController = null,
            ItemsMenuController itemsController = null,
            GameObject conflictPanel = null,
            GameObject quitPanel = null,
            GameObject deletePanel = null,
            GameObject onboardPanel = null,
            Text onboardText = null,
            Slider musicSlider = null,
            Slider sfxSlider = null,
            Dropdown language = null,
            Dropdown resolution = null,
            Toggle highContrast = null,
            Toggle highlightErrors = null,
            Toggle debugEnableAll = null)
        {
            mainMenuPanel = menuPanel;
            classSelectPanel = classPanel;
            optionsPanel = options;
            creditsPanel = credits;
            tutorialSetupPanel = tutorialSetup;
            tutorialProgressPanel = tutorialProgress;
            metaProgressionPanel = metaPanel;
            gameModesPanel = modesPanel;
            itemsPanel = itemsArchivePanel;
            statusText = status;
            classSelectClassText = classSelectText;
            masterVolumeSlider = volumeSlider;
            optionsController = optionsService;
            tutorialMenuController = tutorialController;
            metaProgressionController = metaController;
            gameModesController = modesController;
            itemsMenuController = itemsController;
            saveConflictPanel = conflictPanel;
            confirmQuitPanel = quitPanel;
            confirmDeleteSavePanel = deletePanel;
            onboardingPanel = onboardPanel;
            onboardingBodyText = onboardText;
            musicVolumeSlider = musicSlider;
            sfxVolumeSlider = sfxSlider;
            languageDropdown = language;
            resolutionDropdown = resolution;
            highContrastToggle = highContrast;
            highlightErrorsToggle = highlightErrors;
            debugEnableAllToggle = debugEnableAll;

            if (debugEnableAllToggle != null)
            {
                debugEnableAllToggle.SetIsOnWithoutNotify(_debugEnableAllFeatures);
                debugEnableAllToggle.onValueChanged.RemoveAllListeners();
                debugEnableAllToggle.onValueChanged.AddListener(OnDebugEnableAllChanged);
            }

            RefreshClassSelectUi();
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadGameplayScene()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogError("MainMenuController: Gameplay scene name is empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                Debug.LogWarning($"MainMenuController: Scene '{gameplaySceneName}' is not loadable by name. Checking fallback index 1.");
                var sceneCount = SceneManager.sceneCountInBuildSettings;
                if (sceneCount > 1)
                {
                    Debug.Log("MainMenuController: Loading fallback scene at build index 1.");
                    SceneManager.LoadScene(1);
                    return;
                }

                Debug.LogError("MainMenuController: No fallback gameplay scene found. Add MainMenu + Prototype to Build Profiles scene list.");
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
        }

        private void ShowMainMenu()
        {
            SetPanelState(mainMenuPanel, true);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        private void ShowClassSelect()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, true);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
            RefreshClassSelectUi();
        }

        private void ShowOptions()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, true);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);

            if (optionsController != null && masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(optionsController.Options.Audio.MasterVolume);
            }
        }

        private void ShowCredits()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, true);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        private void ShowTutorialSetup()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, true);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
            tutorialMenuController?.RefreshSetupView();
        }

        private void ShowTutorialProgress()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, true);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        private void ShowMetaProgression()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, true);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        private void ShowGameModes()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, true);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        private void ShowItems()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, true);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        public ClassId SelectedClass => selectedClass;

        private void OpenSaveConflictPanel()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, true);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        private void ShowQuitConfirm()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, true);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, false);
        }

        private void ShowDeleteSaveConfirm()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, true);
            SetPanelState(onboardingPanel, false);
        }

        private void OpenOnboarding()
        {
            SetPanelState(mainMenuPanel, false);
            SetPanelState(classSelectPanel, false);
            SetPanelState(optionsPanel, false);
            SetPanelState(creditsPanel, false);
            SetPanelState(tutorialSetupPanel, false);
            SetPanelState(tutorialProgressPanel, false);
            SetPanelState(metaProgressionPanel, false);
            SetPanelState(gameModesPanel, false);
            SetPanelState(itemsPanel, false);
            SetPanelState(saveConflictPanel, false);
            SetPanelState(confirmQuitPanel, false);
            SetPanelState(confirmDeleteSavePanel, false);
            SetPanelState(onboardingPanel, true);
            _onboardingIndex = 0;
            RefreshOnboardingText();
            SetStatus("Welcome. Let's learn the garden flow.");
        }

        private void CompleteOnboarding()
        {
            PlayerPrefs.SetInt(OnboardingSeenKey, 1);
            PlayerPrefs.Save();
            ShowMainMenu();
            SetStatus("Onboarding complete. Ready.");
        }

        private bool IsClassUnlockedOrDebug(ClassId classId)
        {
            if (_debugEnableAllFeatures || classId == ClassId.NumberFreak)
            {
                return true;
            }

            if (_save.TryLoadProfile(out var envelope) && envelope?.MetaProgress != null)
            {
                _profile.ApplyEnvelope(envelope);
                return _profile.IsClassUnlocked(classId);
            }

            return false;
        }

        private void RefreshClassSelectUi()
        {
            if (classSelectClassText != null)
            {
                EnsureProfileLoaded();
                var snapshot = ClassCatalog.Build(selectedClass);
                var meta = ClassCatalog.GetMeta(selectedClass);
                var entry = GetClassProgressEntry(selectedClass);
                var xpToNext = _classProgression.XpToNextLevel(Mathf.Max(1, entry.Level));
                var unlocked = IsClassUnlockedOrDebug(selectedClass) ? "Unlocked" : "Locked";

                classSelectClassText.text =
                    $"Selected Class: {selectedClass} ({unlocked})\n" +
                    $"HP {snapshot.HP} | Pencil {snapshot.Pencil} | Slots {snapshot.ItemSlots} | Rerolls {snapshot.RerollTokens}\n" +
                    $"Tier {meta.Tier} | Complexity {meta.Complexity} | Skill {meta.SkillBand}\n" +
                    $"Level {entry.Level} | XP {entry.CurrentXp}/{xpToNext} | Prestige {entry.PrestigeTier}\n" +
                    $"Passive: {meta.PassiveDescription}";
            }

            SetClassButtonInteractable("BtnStartClassNumberFreak", IsClassUnlockedOrDebug(ClassId.NumberFreak));
            SetClassButtonInteractable("BtnStartClassGardenMonk", IsClassUnlockedOrDebug(ClassId.GardenMonk));
            SetClassButtonInteractable("BtnStartClassShrineArchivist", IsClassUnlockedOrDebug(ClassId.ShrineArchivist));
            SetClassButtonInteractable("BtnStartClassKoiGambler", IsClassUnlockedOrDebug(ClassId.KoiGambler));
            SetClassButtonInteractable("BtnStartClassStoneGardener", IsClassUnlockedOrDebug(ClassId.StoneGardener));
            SetClassButtonInteractable("BtnStartClassLanternSeer", IsClassUnlockedOrDebug(ClassId.LanternSeer));
        }

        private void ApplyDebugUnlocks()
        {
            EnsureProfileLoaded();

            var allClasses = (ClassId[])Enum.GetValues(typeof(ClassId));
            for (var i = 0; i < allClasses.Length; i++)
            {
                _profile.UnlockClass(allClasses[i]);
            }

            _profile.Meta.EndlessZenUnlocked = true;
            _profile.Meta.SpiritTrialsUnlocked = true;
            _profile.Meta.HiddenDualModifierBossUnlocked = true;
            _profile.Meta.ChaosMonkUnlocked = true;

            EnsureDefaultItemCodexEntries(_profile.Meta.ItemCodex.Entries);
            var discoveredOn = DateTime.UtcNow.ToString("yyyy-MM-dd");
            for (var i = 0; i < _profile.Meta.ItemCodex.Entries.Count; i++)
            {
                var entry = _profile.Meta.ItemCodex.Entries[i];
                entry.Discovered = true;
                entry.Mastered = true;
                entry.DiscoveredDate = string.IsNullOrWhiteSpace(entry.DiscoveredDate) ? discoveredOn : entry.DiscoveredDate;
            }

            SaveProfile();
        }

        private void EnsureProfileLoaded()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }
        }

        private ClassGardenProgressEntry GetClassProgressEntry(ClassId classId)
        {
            var progression = _profile.Meta.GardenProgression;
            if (progression == null)
            {
                return new ClassGardenProgressEntry { ClassId = classId, Level = 1, CurrentXp = 0, PrestigeTier = 0 };
            }

            for (var i = 0; i < progression.ClassEntries.Count; i++)
            {
                var entry = progression.ClassEntries[i];
                if (entry.ClassId == classId)
                {
                    return entry;
                }
            }

            return new ClassGardenProgressEntry { ClassId = classId, Level = 1, CurrentXp = 0, PrestigeTier = 0 };
        }

        private static void EnsureDefaultItemCodexEntries(List<ItemCodexEntry> entries)
        {
            AddCodexEntryIfMissing(entries, new ItemCodexEntry
            {
                ItemID = "relic_koi",
                Name = "Koi Reflection",
                Type = "Relic",
                RarityTier = "Rare",
                UnlockCondition = "Complete a Garden run.",
                Description = "Adds calm combo stability.",
                EffectFormula = "+1 combo grace",
                SynergyTags = "Class:GardenMonk"
            });
            AddCodexEntryIfMissing(entries, new ItemCodexEntry
            {
                ItemID = "consumable_tea",
                Name = "Tea of Focus",
                Type = "Consumable",
                RarityTier = "Common",
                UnlockCondition = "Use 3 consumables.",
                Description = "Boosts accuracy for one puzzle.",
                EffectFormula = "-1 mistake penalty for 5 moves",
                SynergyTags = "Utility"
            });
            AddCodexEntryIfMissing(entries, new ItemCodexEntry
            {
                ItemID = "curse_blind",
                Name = "Shrouded Lens",
                Type = "Cursed",
                RarityTier = "Epic",
                UnlockCondition = "Accept a trap event.",
                Description = "Power for clarity at a price.",
                EffectFormula = "+Gold, +Heat",
                SynergyTags = "Curse"
            });
            AddCodexEntryIfMissing(entries, new ItemCodexEntry
            {
                ItemID = "legendary_lantern",
                Name = "Lantern of Nine",
                Type = "Relic",
                RarityTier = "Legendary",
                UnlockCondition = "Defeat a Boss with Heat 5+.",
                Description = "A sacred relic of the garden.",
                EffectFormula = "+2 reroll tokens, +10% XP",
                SynergyTags = "Class:LanternSeer"
            });
            AddCodexEntryIfMissing(entries, new ItemCodexEntry
            {
                ItemID = "boss_reward_root",
                Name = "Ember Root",
                Type = "Boss Reward",
                RarityTier = "Epic",
                UnlockCondition = "Clear first Boss.",
                Description = "Reward from the guardian.",
                EffectFormula = "+1 passive tier",
                SynergyTags = "Boss"
            });
        }

        private static void AddCodexEntryIfMissing(List<ItemCodexEntry> entries, ItemCodexEntry seed)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].ItemID, seed.ItemID, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            entries.Add(seed);
        }

        private void SaveProfile()
        {
            var envelope = new SaveFileEnvelope
            {
                PlayerProfile = new ProfileSaveData { Options = _profile.Options },
                MetaProgress = _profile.Meta,
                TutorialProgress = _profile.TutorialProgress,
                Statistics = _profile.Stats,
                Mastery = _profile.Mastery,
                Completion = _profile.Completion
            };

            _save.SaveProfile(envelope);
        }

        private static void SetClassButtonInteractable(string buttonName, bool interactable)
        {
            var buttonTransform = FindSceneObject(buttonName);
            if (buttonTransform == null)
            {
                return;
            }

            var button = buttonTransform.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static Transform FindSceneObject(string objectName)
        {
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (var i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null || candidate.name != objectName)
                {
                    continue;
                }

                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private void RefreshOnboardingText()
        {
            if (onboardingBodyText == null)
            {
                return;
            }

            onboardingBodyText.text = _onboardingIndex switch
            {
                0 => "Welcome to Sudoku Roguelike. Start with Number Freak, keep HP safe, and learn puzzle pressure.",
                1 => "Tutorial mode gives safe practice: no progression rewards, configurable size/stars/modifiers.",
                _ => "Use Meta Progression to unlock classes and Game Modes to choose your run style. Good luck."
            };
        }

        private void ResolveConflictAndResume(SaveConflictDecision decision)
        {
            if (!_conflicts.TryResolveRunConflict(decision, out _pendingConflictEnvelope))
            {
                BackToMainMenu();
                SetStatus("Conflict canceled. No resume applied.");
                return;
            }

            if (_pendingConflictEnvelope == null || _pendingConflictEnvelope.ActiveRunState == null)
            {
                BackToMainMenu();
                SetStatus("Resolved save has no active run.");
                return;
            }

            var isBoss = _pendingConflictEnvelope.ActivePuzzle != null && _pendingConflictEnvelope.ActivePuzzle.IsBoss;
            SetStatus(isBoss ? "Resuming mid-boss encounter..." : "Resuming mid-run...");
            LoadGameplayScene();
        }

        private void SyncOptionsWidgetsFromProfile()
        {
            if (optionsController == null)
            {
                return;
            }

            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(optionsController.Options.Audio.MasterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(optionsController.Options.Audio.MusicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(optionsController.Options.Audio.SfxVolume);
            if (languageDropdown != null) languageDropdown.SetValueWithoutNotify(optionsController.Options.Language == LanguageOption.German ? 1 : 0);
            if (highContrastToggle != null) highContrastToggle.SetIsOnWithoutNotify(optionsController.Options.Accessibility.HighContrastMode);
            if (highlightErrorsToggle != null) highlightErrorsToggle.SetIsOnWithoutNotify(optionsController.Options.Gameplay.HighlightConflicts);
        }

        private void ApplyLanguageToVisibleUi(LanguageOption language)
        {
            var german = language == LanguageOption.German;

            SetTextByName("Subtitle", german ? "Sudoku-Roguelike" : "Sudoku Roguelike");
            SetTextByName("OptionsTitle", german ? "Optionen" : "Options");
            SetTextByName("AudioSectionTitle", german ? "Audio" : "Audio");
            SetTextByName("MasterVolumeLabel", german ? "Master-Lautstärke" : "Master Volume");
            SetTextByName("MusicVolumeLabel", german ? "Musik-Lautstärke" : "Music Volume");
            SetTextByName("SfxVolumeLabel", german ? "SFX-Lautstärke" : "SFX Volume");
            SetTextByName("DisplaySectionTitle", german ? "Anzeige" : "Display");
            SetTextByName("LanguageLabel", german ? "Sprache" : "Language");
            SetTextByName("ResolutionLabel", german ? "Auflösung" : "Resolution");
            SetTextByName("AccessibilitySectionTitle", german ? "Barrierefreiheit" : "Accessibility");
            SetTextByName("TutorialTitle", german ? "Tutorial-Setup" : "Tutorial Setup");
            SetTextByName("BoardSizeLabel", german ? "Spielfeldgröße" : "Board Size");
            SetTextByName("StarsLabel", german ? "Sterne" : "Star Difficulty");
            SetTextByName("ResourceModeLabel", german ? "Ressourcenmodus" : "Resource Mode");
            SetTextByName("ModifiersTitle", german ? "Boss-Mechaniken (0-2)" : "Boss Mechanics (0-2)");

            SetButtonLabel("BtnStart", german ? "Spiel starten" : "Start Game");
            SetButtonLabel("BtnResume", german ? "Fortsetzen" : "Resume Game");
            SetButtonLabel("BtnTutorial", german ? "Tutorial" : "Tutorial");
            SetButtonLabel("BtnMeta", german ? "Meta-Fortschritt" : "Meta Progression");
            SetButtonLabel("BtnModes", german ? "Spielmodi" : "Game Modes");
            SetButtonLabel("BtnItems", german ? "Items" : "Items");
            SetButtonLabel("BtnOptions", german ? "Optionen" : "Options");
            SetButtonLabel("BtnCredits", german ? "Credits" : "Credits");
            SetButtonLabel("BtnQuit", german ? "Beenden" : "Quit");
            SetButtonLabel("BtnOptionsBack", german ? "Zurück" : "Back");
            SetButtonLabel("BtnTutorialStart", german ? "Puzzle starten" : "Start Puzzle");
            SetButtonLabel("BtnModesBack", german ? "Zurück" : "Back");
            SetButtonLabel("BtnItemsBack", german ? "Zurück" : "Back");
            SetButtonLabel("BtnModeGardenRun", german ? "Gartenlauf starten" : "Start Garden Run");
            SetButtonLabel("BtnModeEndless", german ? "Endlos-Zen starten" : "Start Endless Zen");
            SetButtonLabel("BtnModeTrials", german ? "Spirit Trials starten" : "Start Spirit Trials");

            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                languageDropdown.AddOptions(german
                    ? new System.Collections.Generic.List<string> { "Englisch", "Deutsch" }
                    : new System.Collections.Generic.List<string> { "English", "German" });
                languageDropdown.SetValueWithoutNotify(german ? 1 : 0);
            }
        }

        private static void SetTextByName(string objectName, string value)
        {
            var text = FindByName<Text>(objectName);
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetButtonLabel(string buttonName, string value)
        {
            var button = FindByName<Button>(buttonName);
            var label = button != null ? button.transform.Find("Label")?.GetComponent<Text>() : null;
            if (label != null)
            {
                label.text = value;
            }
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

        private static void SetPanelState(GameObject panel, bool visible)
        {
            if (panel == null)
            {
                return;
            }

            var animator = panel.GetComponent<MenuPanelAnimator>();
            if (animator != null)
            {
                animator.Play(visible);
            }
            else
            {
                panel.SetActive(visible);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
