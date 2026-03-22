using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using SudokuRoguelike.Bootstrap;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private SaveConflictDecision defaultConflictDecision = SaveConflictDecision.KeepLocal;
        [SerializeField] private GameBootstrap gameBootstrap;
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
        private Text _classUnlockTableText;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Dropdown musicStyleDropdown;
        [SerializeField] private Dropdown languageDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle highlightErrorsToggle;
        [SerializeField] private Toggle debugEnableAllToggle;
        [SerializeField] private OptionsController optionsController;
        [SerializeField] private TutorialMenuController tutorialMenuController;
        [SerializeField] private MetaProgressionPanelController metaProgressionController;
        [SerializeField] private GameModesPanelController gameModesController;
        [SerializeField] private ItemsMenuController itemsMenuController;
        [SerializeField] private ClassId selectedClass = ClassId.NumberFreak;
        private bool _allowIrregularPuzzles = true;

        private const string OnboardingSeenKey = "sr_onboarding_seen";
        private const string ReturnTutorialProgressPrefKey = "sr_return_to_tutorial_progress";
        private int _onboardingIndex;
        private SaveFileEnvelope _pendingConflictEnvelope;

        private GameObject _controlsPanel;
        private readonly InputRemapService _inputRemap = new();

        private readonly MenuFlowService _menu = new();
        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();
        // ClassGardenProgressionService methods are now static — no instance needed
        private readonly ClassUnlockService _classUnlockService = new();
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

            ResolveOptionalWidgets();

            if (PlayerPrefs.GetInt(OnboardingSeenKey, 0) == 0)
            {
                OpenOnboarding();
                return;
            }

            ShowMainMenu();
            SetStatus("Ready.");
            SyncOptionsWidgetsFromProfile();
            ApplyLanguageToVisibleUi(optionsController != null ? optionsController.Options.Language : LanguageOption.English);

            if (PlayerPrefs.GetInt(ReturnTutorialProgressPrefKey, 0) == 1)
            {
                PlayerPrefs.SetInt(ReturnTutorialProgressPrefKey, 0);
                PlayerPrefs.Save();
                OpenTutorial();
                SetStatus("Tutorial complete. Returned to setup.");
            }
        }

        private void ResolveOptionalWidgets()
        {
            if (musicStyleDropdown == null)
            {
                musicStyleDropdown = FindDropdownByName("MusicStyleDropdown");
            }

            if (languageDropdown == null)
            {
                languageDropdown = FindDropdownByName("LanguageDropdown");
            }

            if (resolutionDropdown == null)
            {
                resolutionDropdown = FindDropdownByName("ResolutionDropdown");
            }
        }

        private static Dropdown FindDropdownByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var all = Resources.FindObjectsOfTypeAll<Dropdown>();
            for (var i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null || candidate.gameObject == null)
                {
                    continue;
                }

                if (candidate.gameObject.name != name)
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
            Debug.Log("MainMenuController: StartTutorialGame pressed.");
            SetStatus("Loading tutorial...");
            _menu.OnTutorial();
            _menu.ConfirmTutorialSetup(setup);
            var bootstrap = gameBootstrap != null ? gameBootstrap : FindFirstObjectByType<GameBootstrap>();
            bootstrap.LaunchTutorial(setup);
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
            Debug.Log($"MainMenuController: StartMode pressed. Mode={mode}, Class={selectedClass}");
            SetStatus($"Loading {mode}...");
            _menu.SetMode(mode);
            var bootstrap = gameBootstrap != null ? gameBootstrap : FindFirstObjectByType<GameBootstrap>();
            bootstrap.LaunchRun(new LaunchRequest
            {
                Mode = mode,
                ClassId = selectedClass,
                StartFresh = true,
                ResumeFromSave = false,
                AllowIrregularPuzzles = _allowIrregularPuzzles
            });
        }

        public void SetClassUnlockTableText(Text text)
        {
            _classUnlockTableText = text;
        }

        public void SetSelectedClass(ClassId classId)
        {
            selectedClass = classId;
            RefreshClassSelectUi();

            if (!IsClassUnlockedOrDebug(classId))
            {
                SetStatus($"{classId} is locked. See unlock requirements.");
                return;
            }

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
            if (!IsClassUnlockedOrDebug(selectedClass))
            {
                SetStatus($"{selectedClass} is locked. Choose an unlocked class.");
                return;
            }
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

        public void SetupResumeButton(Button resumeBtn)
        {
            if (resumeBtn == null) return;
            var hasRun = _save.TryLoadRun(out var envelope) && envelope?.ActiveRunState != null;
            resumeBtn.interactable = hasRun;
            var colors = resumeBtn.colors;
            colors.disabledColor = new Color(0.40f, 0.40f, 0.40f, 0.45f);
            resumeBtn.colors = colors;
            var label = resumeBtn.GetComponentInChildren<Text>();
            if (label != null) label.color = hasRun ? new Color(0.96f, 0.93f, 0.82f, 1f) : new Color(0.50f, 0.50f, 0.50f, 0.60f);
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
                var bootstrap = gameBootstrap != null ? gameBootstrap : FindFirstObjectByType<GameBootstrap>();
                bootstrap.LaunchResume();
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
            if (IsDropdownInteractionActive())
            {
                return;
            }

            ShowMainMenu();
            SetStatus("Ready.");
        }

        public void BackToOptions()
        {
            if (IsDropdownInteractionActive())
            {
                return;
            }

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

        public void OnMenuMusicStyleChanged(int index)
        {
            optionsController?.SetMenuMusicStyle(index);
            SetStatus(index == 0 ? "Menu music: 8-bit chill" : "Menu music: 16-bit chill");
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
                var mode = FullScreenMode.ExclusiveFullScreen;

                switch (index)
                {
                    case 0:
                        width = 1280;
                        height = 720;
                        fullscreen = false;
                        mode = FullScreenMode.Windowed;
                        break;
                    case 1:
                        width = 1600;
                        height = 900;
                        fullscreen = false;
                        mode = FullScreenMode.Windowed;
                        break;
                    case 2:
                        width = 1920;
                        height = 1080;
                        fullscreen = true;
                        mode = FullScreenMode.ExclusiveFullScreen;
                        break;
                    case 3:
                        width = 2560;
                        height = 1440;
                        fullscreen = true;
                        mode = FullScreenMode.ExclusiveFullScreen;
                        break;
                }

                optionsController?.SetResolution(width, height, fullscreen, mode);
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

        public void OnHighlightErrorsChanged(bool enabled)
        {
            optionsController?.SetHighlightConflicts(enabled);
            SetStatus(enabled ? "Error highlighting enabled." : "Error highlighting disabled.");
        }

        // ── Accessibility callbacks ──────────────────────────────────────

        public void OnColorblindModeChanged(bool enabled)
        {
            optionsController?.SetColorblindMode(enabled);
            SetStatus(enabled ? "Colorblind mode enabled." : "Colorblind mode disabled.");
        }

        public void OnHighContrastModeChanged(bool enabled)
        {
            optionsController?.SetHighContrastMode(enabled);
            SetStatus(enabled ? "High contrast mode enabled." : "High contrast mode disabled.");
        }

        public void OnReduceMotionChanged(bool enabled)
        {
            optionsController?.SetReduceMotion(enabled);
            SetStatus(enabled ? "Reduce motion enabled." : "Reduce motion disabled.");
        }

        public void OnAltSymbolsChanged(bool enabled)
        {
            optionsController?.SetAlternativeConstraintSymbols(enabled);
            SetStatus(enabled ? "Alternative symbols enabled." : "Alternative symbols disabled.");
        }

        public void OnFontScaleChanged(float value)
        {
            optionsController?.SetFontScale(value);
            SetStatus($"Font scale: {value:F1}x");
        }

        public void OnUiVolumeChanged(float value)
        {
            optionsController?.SetUiVolume(value);
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

        public void OpenControlsPanel()
        {
            if (_controlsPanel != null) Destroy(_controlsPanel);

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _controlsPanel = new GameObject("ControlsPanel", typeof(RectTransform), typeof(Image));
            _controlsPanel.transform.SetParent(canvas.transform, false);
            var rect = _controlsPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.08f);
            rect.anchorMax = new Vector2(0.85f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var bg = _controlsPanel.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.10f, 0.97f);

            var title = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(rect, false);
            title.rectTransform.anchorMin = new Vector2(0.05f, 0.92f);
            title.rectTransform.anchorMax = new Vector2(0.95f, 0.99f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 22;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.90f, 0.62f, 1f);
            title.text = "Keyboard Controls";

            // Build rows for each category / action
            var yPos = 0.88f;
            var categories = InputRemapService.Categories;
            foreach (var cat in categories)
            {
                // Category header
                var catLabel = new GameObject($"Cat_{cat}", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                catLabel.transform.SetParent(rect, false);
                catLabel.rectTransform.anchorMin = new Vector2(0.05f, yPos - 0.03f);
                catLabel.rectTransform.anchorMax = new Vector2(0.95f, yPos);
                catLabel.rectTransform.offsetMin = Vector2.zero;
                catLabel.rectTransform.offsetMax = Vector2.zero;
                catLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                catLabel.fontSize = 14;
                catLabel.alignment = TextAnchor.MiddleLeft;
                catLabel.color = new Color(0.80f, 0.75f, 0.55f, 1f);
                catLabel.text = cat;
                catLabel.fontStyle = FontStyle.Bold;
                yPos -= 0.035f;

                var actions = InputRemapService.GetActionsInCategory(cat);
                foreach (var action in actions)
                {
                    if (yPos < 0.08f) break;

                    // Action label
                    var actionLabel = new GameObject($"Act_{action}", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    actionLabel.transform.SetParent(rect, false);
                    actionLabel.rectTransform.anchorMin = new Vector2(0.08f, yPos - 0.025f);
                    actionLabel.rectTransform.anchorMax = new Vector2(0.45f, yPos);
                    actionLabel.rectTransform.offsetMin = Vector2.zero;
                    actionLabel.rectTransform.offsetMax = Vector2.zero;
                    actionLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    actionLabel.fontSize = 12;
                    actionLabel.alignment = TextAnchor.MiddleLeft;
                    actionLabel.color = Color.white;
                    actionLabel.text = FormatActionName(action);

                    // Current binding display
                    var bindingLabel = new GameObject($"Bind_{action}", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    bindingLabel.transform.SetParent(rect, false);
                    bindingLabel.rectTransform.anchorMin = new Vector2(0.48f, yPos - 0.025f);
                    bindingLabel.rectTransform.anchorMax = new Vector2(0.70f, yPos);
                    bindingLabel.rectTransform.offsetMin = Vector2.zero;
                    bindingLabel.rectTransform.offsetMax = Vector2.zero;
                    bindingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    bindingLabel.fontSize = 12;
                    bindingLabel.alignment = TextAnchor.MiddleCenter;
                    bindingLabel.color = new Color(0.70f, 0.85f, 0.70f, 1f);
                    bindingLabel.text = $"[ {_inputRemap.GetDisplayName(action)} ]";

                    yPos -= 0.028f;
                }
                yPos -= 0.01f;
            }

            // Reset All button
            var resetGo = new GameObject("ResetAllBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            resetGo.transform.SetParent(rect, false);
            var resetRect = resetGo.GetComponent<RectTransform>();
            resetRect.anchorMin = new Vector2(0.10f, 0.02f);
            resetRect.anchorMax = new Vector2(0.40f, 0.06f);
            resetRect.offsetMin = Vector2.zero;
            resetRect.offsetMax = Vector2.zero;
            resetGo.GetComponent<Image>().color = new Color(0.35f, 0.20f, 0.15f, 1f);
            var resetBtn = resetGo.GetComponent<Button>();
            resetBtn.onClick.AddListener(() =>
            {
                _inputRemap.ResetAllBindings();
                OpenControlsPanel(); // Rebuild to refresh display
            });
            var resetLbl = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            resetLbl.transform.SetParent(resetRect, false);
            resetLbl.rectTransform.anchorMin = Vector2.zero;
            resetLbl.rectTransform.anchorMax = Vector2.one;
            resetLbl.rectTransform.offsetMin = Vector2.zero;
            resetLbl.rectTransform.offsetMax = Vector2.zero;
            resetLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resetLbl.fontSize = 13;
            resetLbl.alignment = TextAnchor.MiddleCenter;
            resetLbl.color = Color.white;
            resetLbl.text = "Reset All to Defaults";

            // Back button
            var backGo = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(rect, false);
            var backRect = backGo.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.70f, 0.02f);
            backRect.anchorMax = new Vector2(0.90f, 0.06f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            backGo.GetComponent<Image>().color = new Color(0.20f, 0.28f, 0.32f, 1f);
            var backBtn = backGo.GetComponent<Button>();
            backBtn.onClick.AddListener(CloseControlsPanel);
            var backLbl = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            backLbl.transform.SetParent(backRect, false);
            backLbl.rectTransform.anchorMin = Vector2.zero;
            backLbl.rectTransform.anchorMax = Vector2.one;
            backLbl.rectTransform.offsetMin = Vector2.zero;
            backLbl.rectTransform.offsetMax = Vector2.zero;
            backLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            backLbl.fontSize = 14;
            backLbl.alignment = TextAnchor.MiddleCenter;
            backLbl.color = Color.white;
            backLbl.text = "Back";

            SetStatus("View and customize keyboard controls.");
        }

        public void CloseControlsPanel()
        {
            if (_controlsPanel != null)
            {
                Destroy(_controlsPanel);
                _controlsPanel = null;
            }
        }

        private static string FormatActionName(InputRemapService.InputAction action)
        {
            var name = action.ToString();
            // Insert spaces before capital letters
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                    result.Append(' ');
                result.Append(name[i]);
            }
            return result.ToString();
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
            musicStyleDropdown = null;
            languageDropdown = language;
            resolutionDropdown = resolution;
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

        // LoadGameplayScene removed — single-scene architecture.
        // GameBootstrap.LaunchRun / LaunchTutorial / LaunchResume handle transitions via ScreenManager.

        private void ShowMainMenu()
        {
            CloseControlsPanel();
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
            ResolveOptionalWidgets();

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

            if (musicStyleDropdown != null)
            {
                musicStyleDropdown.onValueChanged.RemoveListener(OnMenuMusicStyleChanged);
                musicStyleDropdown.onValueChanged.AddListener(OnMenuMusicStyleChanged);
                musicStyleDropdown.SetValueWithoutNotify(Mathf.Clamp(optionsController != null ? optionsController.Options.Audio.MenuMusicStyleIndex : 0, 0, 1));
            }

            SyncAccessibilityWidgets();
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

        public void SetAllowIrregularPuzzles(bool allow)
        {
            _allowIrregularPuzzles = allow;
        }

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
                var totalXp = entry.TotalXp;
                var (classLevel, progressXp, xpToNext) = SudokuRoguelike.Meta.ClassGardenProgressionService.DeriveLevel(totalXp);
                var isUnlocked = IsClassUnlockedOrDebug(selectedClass);
                var unlocked = isUnlocked ? "Unlocked" : "Locked";
                var unlockHint = isUnlocked ? string.Empty : _classUnlockService.GetUnlockRequirementText(selectedClass);

                var bonuses = SudokuRoguelike.Meta.ClassGardenProgressionService.GetStatBonuses(selectedClass, classLevel);
                var nextUnlock = SudokuRoguelike.Meta.ClassGardenProgressionService.GetNextUnlock(selectedClass, classLevel);
                var hpTotal     = snapshot.HP      + bonuses.HpBonus;
                var pencilTotal = snapshot.Pencil  + bonuses.PencilBonus;
                var slotsTotal  = snapshot.ItemSlots + bonuses.SlotBonus;
                var rerollTotal = snapshot.RerollTokens + bonuses.RerollBonus;

                var hpStr     = bonuses.HpBonus     > 0 ? $"HP {hpTotal} (+{bonuses.HpBonus})"         : $"HP {hpTotal}";
                var pencilStr = bonuses.PencilBonus > 0 ? $"Pencil {pencilTotal} (+{bonuses.PencilBonus})" : $"Pencil {pencilTotal}";
                var slotsStr  = bonuses.SlotBonus   > 0 ? $"Slots {slotsTotal} (+{bonuses.SlotBonus})"  : $"Slots {slotsTotal}";
                var rerollStr = bonuses.RerollBonus > 0 ? $"Rerolls {rerollTotal} (+{bonuses.RerollBonus})" : $"Rerolls {rerollTotal}";
                var xpDisplay = xpToNext > 0 ? $"{progressXp}/{xpToNext}" : "MAX";

                UnityEngine.Debug.Log($"[XP] RefreshClassSelectUi — {selectedClass} Lv{classLevel} XP:{xpDisplay}");

                classSelectClassText.text =
                    $"Selected Class: {selectedClass} ({unlocked})\n" +
                    $"{hpStr} | {pencilStr} | {slotsStr} | {rerollStr}\n" +
                    $"Tier {meta.Tier} | Complexity {meta.Complexity} | Skill {meta.SkillBand}\n" +
                    $"Level {classLevel} | XP {xpDisplay} | Prestige {entry.PrestigeTier}\n" +
                    $"Next: {nextUnlock}\n" +
                    $"Passive: {meta.PassiveDescription}" +
                    (string.IsNullOrWhiteSpace(unlockHint) ? string.Empty : $"\n<color=#FF4444>Unlock: {unlockHint}</color>");
            }

            SetClassButtonInteractable("BtnStartClassNumberFreak", true);
            SetClassButtonInteractable("BtnStartClassGardenMonk", true);
            SetClassButtonInteractable("BtnStartClassShrineArchivist", true);
            SetClassButtonInteractable("BtnStartClassKoiGambler", true);
            SetClassButtonInteractable("BtnStartClassStoneGardener", true);
            SetClassButtonInteractable("BtnStartClassLanternSeer", true);
            SetClassButtonInteractable("BtnStartClassReedDuelist", true);
            SetClassButtonInteractable("BtnStartClassQuietCartographer", true);

            HighlightClassButton("BtnStartClassNumberFreak", selectedClass == ClassId.NumberFreak);
            HighlightClassButton("BtnStartClassGardenMonk", selectedClass == ClassId.GardenMonk);
            HighlightClassButton("BtnStartClassShrineArchivist", selectedClass == ClassId.ShrineArchivist);
            HighlightClassButton("BtnStartClassKoiGambler", selectedClass == ClassId.KoiGambler);
            HighlightClassButton("BtnStartClassStoneGardener", selectedClass == ClassId.StoneGardener);
            HighlightClassButton("BtnStartClassLanternSeer", selectedClass == ClassId.LanternSeer);
            HighlightClassButton("BtnStartClassReedDuelist", selectedClass == ClassId.ReedDuelist);
            HighlightClassButton("BtnStartClassQuietCartographer", selectedClass == ClassId.QuietCartographer);

            if (_classUnlockTableText != null)
            {
                var allUnlocks = SudokuRoguelike.Meta.ClassGardenProgressionService.GetUnlocksInRange(selectedClass, 0, 40);
                var sb = new System.Text.StringBuilder("Level Rewards:\n");
                for (var ui = 0; ui < allUnlocks.Count; ui++)
                    sb.AppendLine(allUnlocks[ui]);
                _classUnlockTableText.text = sb.ToString().TrimEnd();
            }
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
                return new ClassGardenProgressEntry { ClassId = classId };

            for (var i = 0; i < progression.ClassEntries.Count; i++)
            {
                if (progression.ClassEntries[i].ClassId == classId)
                    return progression.ClassEntries[i];
            }

            return new ClassGardenProgressEntry { ClassId = classId };
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
                EffectFormula = "+Gold, +Risk",
                SynergyTags = "Curse"
            });
            AddCodexEntryIfMissing(entries, new ItemCodexEntry
            {
                ItemID = "legendary_lantern",
                Name = "Lantern of Nine",
                Type = "Relic",
                RarityTier = "Legendary",
                UnlockCondition = "Defeat a Boss with max difficulty.",
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

        private static readonly Color ClassButtonDefault = new(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color ClassButtonSelected = new(0.56f, 0.72f, 0.42f, 1f);

        private static void HighlightClassButton(string buttonName, bool selected)
        {
            var buttonTransform = FindSceneObject(buttonName);
            if (buttonTransform == null)
            {
                return;
            }

            var image = buttonTransform.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? ClassButtonSelected : ClassButtonDefault;
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
            var bootstrap = gameBootstrap != null ? gameBootstrap : FindFirstObjectByType<GameBootstrap>();
            bootstrap.LaunchResume();
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
            if (musicStyleDropdown != null) musicStyleDropdown.SetValueWithoutNotify(Mathf.Clamp(optionsController.Options.Audio.MenuMusicStyleIndex, 0, 1));
            if (languageDropdown != null) languageDropdown.SetValueWithoutNotify(optionsController.Options.Language == LanguageOption.German ? 1 : 0);
            if (highlightErrorsToggle != null) highlightErrorsToggle.SetIsOnWithoutNotify(optionsController.Options.Gameplay.HighlightConflicts);
            if (resolutionDropdown != null) resolutionDropdown.SetValueWithoutNotify(ResolveResolutionDropdownIndex(optionsController.Options));
        }

        private void SyncAccessibilityWidgets()
        {
            if (optionsController == null || optionsPanel == null) return;
            var a = optionsController.Options.Accessibility;
            var panelTr = optionsPanel.transform;

            var cb = panelTr.Find("ColorblindToggle")?.GetComponent<Toggle>();
            if (cb != null) cb.SetIsOnWithoutNotify(a.ColorblindMode);

            var hc = panelTr.Find("HighContrastToggle")?.GetComponent<Toggle>();
            if (hc != null) hc.SetIsOnWithoutNotify(a.HighContrastMode);

            var rm = panelTr.Find("ReduceMotionToggle")?.GetComponent<Toggle>();
            if (rm != null) rm.SetIsOnWithoutNotify(a.ReduceMotion);

            var alt = panelTr.Find("AltSymbolsToggle")?.GetComponent<Toggle>();
            if (alt != null) alt.SetIsOnWithoutNotify(a.AlternativeConstraintSymbols);

            var fs = panelTr.Find("FontScaleSlider")?.GetComponent<Slider>();
            if (fs != null) fs.SetValueWithoutNotify(a.FontScale);

            var uiVol = panelTr.Find("UiVolumeSlider")?.GetComponent<Slider>();
            if (uiVol != null) uiVol.SetValueWithoutNotify(optionsController.Options.Audio.UiVolume);
        }

        private static int ResolveResolutionDropdownIndex(OptionsState options)
        {
            var w = options.Graphics.Width;
            var h = options.Graphics.Height;
            var fs = options.Graphics.Fullscreen;
            if (w == 1280 && h == 720 && !fs) return 0;
            if (w == 1600 && h == 900 && !fs) return 1;
            if (w == 2560 && h == 1440 && fs) return 3;
            return 2; // 1920x1080 default
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
            SetTextByName("UiVolumeLabel", german ? "UI-Lautstärke" : "UI Volume");
            SetTextByName("FontScaleLabel", german ? "Schriftgröße" : "Font Scale");
            SetTextByName("TutorialTitle", german ? "Tutorial-Setup" : "Tutorial Setup");
            SetTextByName("BoardSizeLabel", german ? "Spielfeldgröße" : "Board Size");
            SetTextByName("StarsLabel", german ? "Sterne" : "Star Difficulty");
            SetTextByName("ResourceModeLabel", german ? "Ressourcenmodus" : "Resource Mode");
            SetTextByName("ModifiersTitle", german ? "Sudoku-Modi" : "Sudoku Modes");

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

        private static bool IsDropdownInteractionActive()
        {
            if (DropdownAutoSizeController.HasRecentGlobalInteraction)
            {
                return true;
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (BelongsToDropdown(eventSystem.currentSelectedGameObject))
            {
                return true;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null)
            {
                var selectedName = selected.name ?? string.Empty;
                if (selectedName.Contains("Dropdown List", StringComparison.OrdinalIgnoreCase) ||
                    selectedName.Contains("Blocker", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var dropdownList = FindSceneObject("Dropdown List");
            if (dropdownList != null && dropdownList.gameObject.activeInHierarchy)
            {
                return true;
            }

            if (HasActiveSceneObjectNamePart("Dropdown List"))
            {
                return true;
            }

            var blocker = FindSceneObject("Blocker");
            if (blocker != null && blocker.gameObject.activeInHierarchy)
            {
                return true;
            }

            if (HasActiveSceneObjectNamePart("Blocker"))
            {
                return true;
            }

            return false;
        }

        private static bool BelongsToDropdown(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.GetComponentInParent<Dropdown>() != null)
            {
                return true;
            }

            var name = target.name ?? string.Empty;
            if (name == "Dropdown List" || name == "Blocker" ||
                name.Contains("Dropdown List", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Blocker", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool HasActiveSceneObjectNamePart(string namePart)
        {
            if (string.IsNullOrWhiteSpace(namePart))
            {
                return false;
            }

            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (var i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null || candidate.gameObject == null)
                {
                    continue;
                }

                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (candidate.name != null && candidate.name.Contains(namePart, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
