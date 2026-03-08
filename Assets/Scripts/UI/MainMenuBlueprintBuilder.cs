using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.IO;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuBlueprintBuilder : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private bool autoBuildOnStart = true;

        [SerializeField] private Color backgroundColor = new(0.07f, 0.11f, 0.14f, 1f);
        [SerializeField] private Color panelColor = new(0.08f, 0.12f, 0.16f, 0.24f);
        [SerializeField] private Color buttonColor = new(0.15f, 0.22f, 0.29f, 0.94f);
        [SerializeField] private Color accentColor = new(0.98f, 0.83f, 0.26f, 1f);
        [SerializeField] private Color textColor = new(0.96f, 0.93f, 0.82f, 1f);

        private bool _builtAtRuntime;

        private void Start()
        {
            if (!autoBuildOnStart || !Application.isPlaying || _builtAtRuntime)
            {
                return;
            }

            Build();
            _builtAtRuntime = true;
        }

        [ContextMenu("Build Minimal Main Menu")]
        public void Build()
        {
            if (Application.isPlaying && _builtAtRuntime)
            {
                return;
            }

            if (mainMenuController == null)
            {
                mainMenuController = GetComponent<MainMenuController>();
                if (mainMenuController == null)
                {
                    mainMenuController = gameObject.AddComponent<MainMenuController>();
                }
            }

            var autoWire = EnsureComponent<MainMenuRuntimeAutoWire>(gameObject);
            autoWire.Configure(mainMenuController);
            var optionsController = EnsureComponent<OptionsController>(gameObject);
            var tutorialController = EnsureComponent<TutorialMenuController>(gameObject);
            var metaController = EnsureComponent<MetaProgressionPanelController>(gameObject);
            var modesController = EnsureComponent<GameModesPanelController>(gameObject);
            var itemsController = EnsureComponent<ItemsMenuController>(gameObject);

            EnsureEventSystem();
            EnsureMainCamera();
            var canvas = EnsureCanvas();
            var root = EnsureRect("MainMenuRoot", canvas.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rootImage = EnsureOrGetImage(root.gameObject, backgroundColor);
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = false;
            rootImage.sprite = null;
            var pngApplied = false;

            var menuMusic = EnsureComponent<MenuMusicController>(root.gameObject);
            var audioSource = EnsureComponent<AudioSource>(root.gameObject);
            audioSource.playOnAwake = false;
            audioSource.loop = true;

            var atmosphere = EnsureComponent<MainMenuAtmosphereController>(root.gameObject);
            var atmosphereFar = EnsureRect("AtmosphereFar", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var atmosphereMid = EnsureRect("AtmosphereMid", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var atmosphereNear = EnsureRect("AtmosphereNear", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(atmosphereFar.gameObject, new Color(0.06f, 0.12f, 0.16f, 0.60f));
            EnsureOrGetImage(atmosphereMid.gameObject, new Color(0.12f, 0.20f, 0.26f, 0.24f));
            EnsureOrGetImage(atmosphereNear.gameObject, new Color(0.96f, 0.78f, 0.20f, 0.05f));
            if (pngApplied)
            {
                var far = atmosphereFar.GetComponent<Image>();
                var mid = atmosphereMid.GetComponent<Image>();
                var near = atmosphereNear.GetComponent<Image>();
                if (far != null) far.color = new Color(0f, 0f, 0f, 0f);
                if (mid != null) mid.color = new Color(0f, 0f, 0f, 0f);
                if (near != null) near.color = new Color(0f, 0f, 0f, 0f);
            }

            var petalRoot = EnsureRect("PetalLayer", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var mistRoot = EnsureRect("MistLayer", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            atmosphere.Configure(atmosphereFar, atmosphereMid, atmosphereNear, petalRoot, mistRoot);
            if (pngApplied)
            {
                petalRoot.gameObject.SetActive(false);
                mistRoot.gameObject.SetActive(false);
            }

            var card = EnsureRect("MenuCard", root, new Vector2(0.33f, 0.08f), new Vector2(0.67f, 0.90f), Vector2.zero, Vector2.zero);
            var cardImage = EnsureOrGetImage(card.gameObject, panelColor);
            pngApplied = TryApplyFullBackgroundSprite(cardImage);

            var btnStart = BuildMenuButton(card, "BtnStart", "Start Game", new Vector2(0.08f, 0.686f), new Vector2(0.92f, 0.726f), mainMenuController.StartGame);
            var btnResume = BuildMenuButton(card, "BtnResume", "Resume Game", new Vector2(0.08f, 0.634f), new Vector2(0.92f, 0.674f), mainMenuController.ResumeGame);
            var btnTutorial = BuildMenuButton(card, "BtnTutorial", "Tutorial", new Vector2(0.08f, 0.579f), new Vector2(0.92f, 0.619f), mainMenuController.OpenTutorial);
            var btnMeta = BuildMenuButton(card, "BtnMeta", "Meta Progression", new Vector2(0.08f, 0.527f), new Vector2(0.92f, 0.567f), mainMenuController.OpenMetaProgression);
            var btnModes = BuildMenuButton(card, "BtnModes", "Game Modes", new Vector2(0.08f, 0.478f), new Vector2(0.92f, 0.518f), mainMenuController.OpenGameModes);
            var btnItems = BuildMenuButton(card, "BtnItems", "Items", new Vector2(0.08f, 0.428f), new Vector2(0.92f, 0.468f), mainMenuController.OpenItems);
            var btnOptions = BuildMenuButton(card, "BtnOptions", "Options", new Vector2(0.08f, 0.379f), new Vector2(0.92f, 0.419f), mainMenuController.OpenOptions);
            var btnCredits = BuildMenuButton(card, "BtnCredits", "Credits", new Vector2(0.08f, 0.329f), new Vector2(0.92f, 0.369f), mainMenuController.OpenCredits);
            var btnQuit = BuildMenuButton(card, "BtnQuit", "Quit", new Vector2(0.08f, 0.280f), new Vector2(0.92f, 0.320f), mainMenuController.ExitGame);

            TryApplyMainMenuButtonSlice(btnStart, new Vector2(0.08f, 0.686f), new Vector2(0.92f, 0.726f));
            TryApplyMainMenuButtonSlice(btnResume, new Vector2(0.08f, 0.634f), new Vector2(0.92f, 0.674f));
            TryApplyMainMenuButtonSlice(btnTutorial, new Vector2(0.08f, 0.579f), new Vector2(0.92f, 0.619f));
            TryApplyMainMenuButtonSlice(btnMeta, new Vector2(0.08f, 0.527f), new Vector2(0.92f, 0.567f));
            TryApplyMainMenuButtonSlice(btnModes, new Vector2(0.08f, 0.478f), new Vector2(0.92f, 0.518f));
            TryApplyMainMenuButtonSlice(btnItems, new Vector2(0.08f, 0.428f), new Vector2(0.92f, 0.468f));
            TryApplyMainMenuButtonSlice(btnOptions, new Vector2(0.08f, 0.379f), new Vector2(0.92f, 0.419f));
            TryApplyMainMenuButtonSlice(btnCredits, new Vector2(0.08f, 0.329f), new Vector2(0.92f, 0.369f));
            TryApplyMainMenuButtonSlice(btnQuit, new Vector2(0.08f, 0.280f), new Vector2(0.92f, 0.320f));

            ApplyMenuButtonIcon(btnStart, "GeneratedIcons/icon_bud");
            ApplyMenuButtonIcon(btnResume, "GeneratedIcons/icon_scroll_graph");
            ApplyMenuButtonIcon(btnTutorial, "GeneratedIcons/icon_bamboo_scroll");
            ApplyMenuButtonIcon(btnMeta, "GeneratedIcons/icon_golden_bloom");
            ApplyMenuButtonIcon(btnModes, "GeneratedIcons/icon_garden_lantern");
            ApplyMenuButtonIcon(btnItems, "GeneratedIcons/icon_triple_chest");
            ApplyMenuButtonIcon(btnOptions, "GeneratedIcons/icon_stone_gear");
            ApplyMenuButtonIcon(btnCredits, "GeneratedIcons/icon_language_scroll");
            ApplyMenuButtonIcon(btnQuit, "GeneratedIcons/icon_torii_lock");

            var debugToggle = BuildToggle("DebugEnableAllToggle", card, "Enable All (Debug)");
            SetRect(debugToggle.GetComponent<RectTransform>(), new Vector2(0.04f, 0.01f), new Vector2(0.36f, 0.05f), Vector2.zero, Vector2.zero);
            debugToggle.SetIsOnWithoutNotify(false);
            debugToggle.onValueChanged.RemoveAllListeners();
            debugToggle.onValueChanged.AddListener(mainMenuController.OnDebugEnableAllChanged);

            var statusPlate = EnsureRect("StatusPlate", root, new Vector2(0.34f, 0.015f), new Vector2(0.66f, 0.075f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(statusPlate.gameObject, new Color(0.15f, 0.19f, 0.20f, 0.78f));
            var statusPlateOutline = EnsureComponent<Outline>(statusPlate.gameObject);
            statusPlateOutline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f);
            statusPlateOutline.effectDistance = new Vector2(1.2f, -1.2f);

            var status = BuildText("StatusText", statusPlate, "Ready.", 28, TextAnchor.MiddleCenter);
            SetRect(status.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(6f, 2f), new Vector2(-6f, -2f));

            var classSelectPanel = BuildClassSelectPanel(root, mainMenuController, out var classSelectText);
            var optionsPanel = BuildOptionsPanel(root, mainMenuController, optionsController);
            var creditsPanel = BuildCreditsPanel(root, mainMenuController);
            var tutorialSetupPanel = BuildTutorialSetupPanel(root, mainMenuController, tutorialController);
            var tutorialProgressPanel = BuildTutorialProgressPanel(root, mainMenuController, tutorialController);
            var metaPanel = BuildMetaProgressionPanel(root, mainMenuController, metaController);
            var modesPanel = BuildGameModesPanel(root, mainMenuController, modesController);
            var itemsPanel = BuildItemsPanel(root, mainMenuController, itemsController);
            var conflictPanel = BuildSaveConflictPanel(root, mainMenuController);
            var quitPanel = BuildQuitConfirmPanel(root, mainMenuController);
            var deletePanel = BuildDeleteSaveConfirmPanel(root, mainMenuController);
            var onboardingPanel = BuildOnboardingPanel(root, mainMenuController, out var onboardingBody);

            EnsureComponent<MenuPanelAnimator>(card.gameObject);
            EnsureComponent<MenuPanelAnimator>(classSelectPanel);
            EnsureComponent<MenuPanelAnimator>(optionsPanel);
            EnsureComponent<MenuPanelAnimator>(creditsPanel);
            EnsureComponent<MenuPanelAnimator>(tutorialSetupPanel);
            EnsureComponent<MenuPanelAnimator>(tutorialProgressPanel);
            EnsureComponent<MenuPanelAnimator>(metaPanel);
            EnsureComponent<MenuPanelAnimator>(modesPanel);
            EnsureComponent<MenuPanelAnimator>(itemsPanel);
            EnsureComponent<MenuPanelAnimator>(conflictPanel);
            EnsureComponent<MenuPanelAnimator>(quitPanel);
            EnsureComponent<MenuPanelAnimator>(deletePanel);
            EnsureComponent<MenuPanelAnimator>(onboardingPanel);

            mainMenuController.ConfigureUi(
                card.gameObject,
                classSelectPanel,
                optionsPanel,
                creditsPanel,
                status,
                classSelectText,
                optionsPanel.transform.Find("MasterVolumeSlider")?.GetComponent<Slider>(),
                optionsController,
                tutorialSetupPanel,
                tutorialProgressPanel,
                tutorialController,
                metaPanel,
                modesPanel,
                itemsPanel,
                metaController,
                modesController,
                itemsController,
                conflictPanel,
                quitPanel,
                deletePanel,
                onboardingPanel,
                onboardingBody,
                optionsPanel.transform.Find("MusicVolumeSlider")?.GetComponent<Slider>(),
                optionsPanel.transform.Find("SfxVolumeSlider")?.GetComponent<Slider>(),
                optionsPanel.transform.Find("LanguageDropdown")?.GetComponent<Dropdown>(),
                optionsPanel.transform.Find("ResolutionDropdown")?.GetComponent<Dropdown>(),
                optionsPanel.transform.Find("HighlightErrorsToggle")?.GetComponent<Toggle>(),
                debugToggle);

            Debug.Log("MainMenuBlueprintBuilder: Minimal Main Menu built.");

            if (Application.isPlaying)
            {
                _builtAtRuntime = true;
            }
        }

        private GameObject BuildClassSelectPanel(RectTransform root, MainMenuController controller, out Text selectedClassText)
        {
            var panel = EnsureRect("ClassSelectPanel", root, new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("ClassSelectTitle", panel.transform as RectTransform, "Select Class", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            selectedClassText = BuildText("ClassSelectCurrentText", panel.transform as RectTransform, "Selected Class: NumberFreak", 13, TextAnchor.UpperLeft);
            SetRect(selectedClassText.rectTransform, new Vector2(0.04f, 0.20f), new Vector2(0.48f, 0.42f), Vector2.zero, Vector2.zero);

            var unlockTableText = BuildText("ClassUnlockTableText", panel.transform as RectTransform, "", 11, TextAnchor.UpperLeft);
            SetRect(unlockTableText.rectTransform, new Vector2(0.52f, 0.20f), new Vector2(0.96f, 0.42f), Vector2.zero, Vector2.zero);
            controller.SetClassUnlockTableText(unlockTableText);

            var btnNF = BuildButton("BtnStartClassNumberFreak", panel.transform as RectTransform, "Number Freak", 16);
            SetRect(btnNF.GetComponent<RectTransform>(), new Vector2(0.10f, 0.66f), new Vector2(0.46f, 0.74f), Vector2.zero, Vector2.zero);
            btnNF.onClick.RemoveAllListeners();
            btnNF.onClick.AddListener(controller.SelectClassNumberFreak);

            var btnGM = BuildButton("BtnStartClassGardenMonk", panel.transform as RectTransform, "Garden Monk", 16);
            SetRect(btnGM.GetComponent<RectTransform>(), new Vector2(0.54f, 0.66f), new Vector2(0.90f, 0.74f), Vector2.zero, Vector2.zero);
            btnGM.onClick.RemoveAllListeners();
            btnGM.onClick.AddListener(controller.SelectClassGardenMonk);

            var btnSA = BuildButton("BtnStartClassShrineArchivist", panel.transform as RectTransform, "Shrine Archivist", 16);
            SetRect(btnSA.GetComponent<RectTransform>(), new Vector2(0.10f, 0.55f), new Vector2(0.46f, 0.63f), Vector2.zero, Vector2.zero);
            btnSA.onClick.RemoveAllListeners();
            btnSA.onClick.AddListener(controller.SelectClassShrineArchivist);

            var btnKG = BuildButton("BtnStartClassKoiGambler", panel.transform as RectTransform, "Koi Gambler", 16);
            SetRect(btnKG.GetComponent<RectTransform>(), new Vector2(0.54f, 0.55f), new Vector2(0.90f, 0.63f), Vector2.zero, Vector2.zero);
            btnKG.onClick.RemoveAllListeners();
            btnKG.onClick.AddListener(controller.SelectClassKoiGambler);

            var btnSG = BuildButton("BtnStartClassStoneGardener", panel.transform as RectTransform, "Stone Gardener", 16);
            SetRect(btnSG.GetComponent<RectTransform>(), new Vector2(0.10f, 0.44f), new Vector2(0.46f, 0.52f), Vector2.zero, Vector2.zero);
            btnSG.onClick.RemoveAllListeners();
            btnSG.onClick.AddListener(controller.SelectClassStoneGardener);

            var btnLS = BuildButton("BtnStartClassLanternSeer", panel.transform as RectTransform, "Lantern Seer", 16);
            SetRect(btnLS.GetComponent<RectTransform>(), new Vector2(0.54f, 0.44f), new Vector2(0.90f, 0.52f), Vector2.zero, Vector2.zero);
            btnLS.onClick.RemoveAllListeners();
            btnLS.onClick.AddListener(controller.SelectClassLanternSeer);

            ApplyMenuButtonIcon(btnNF, "GeneratedIcons/icon_spin_coin");
            ApplyMenuButtonIcon(btnGM, "GeneratedIcons/icon_enlightenment_tree");
            ApplyMenuButtonIcon(btnSA, "GeneratedIcons/icon_scroll_graph");
            ApplyMenuButtonIcon(btnKG, "GeneratedIcons/icon_golden_koi");
            ApplyMenuButtonIcon(btnSG, "GeneratedIcons/icon_moss_stone");
            ApplyMenuButtonIcon(btnLS, "GeneratedIcons/icon_garden_lantern");

            var continueButton = BuildButton("BtnClassSelectContinue", panel.transform as RectTransform, "Continue", 20);
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.56f, 0.08f), new Vector2(0.90f, 0.18f), Vector2.zero, Vector2.zero);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(controller.ConfirmClassAndStart);

            var backButton = BuildButton("BtnClassSelectBack", panel.transform as RectTransform, "Back", 18);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.10f, 0.08f), new Vector2(0.34f, 0.18f), Vector2.zero, Vector2.zero);
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(controller.BackFromClassSelect);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildOptionsPanel(RectTransform root, MainMenuController controller, OptionsController optionsController)
        {
            var panel = EnsureRect("OptionsPanel", root, new Vector2(0.24f, 0.12f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("OptionsTitle", panel.transform as RectTransform, "Options", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.91f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var audioTitle = BuildText("AudioSectionTitle", panel.transform as RectTransform, "Audio", 20, TextAnchor.MiddleLeft);
            SetRect(audioTitle.rectTransform, new Vector2(0.10f, 0.83f), new Vector2(0.90f, 0.89f), Vector2.zero, Vector2.zero);

            var volumeLabel = BuildText("MasterVolumeLabel", panel.transform as RectTransform, "Master Volume", 18, TextAnchor.MiddleLeft);
            SetRect(volumeLabel.rectTransform, new Vector2(0.10f, 0.76f), new Vector2(0.90f, 0.82f), Vector2.zero, Vector2.zero);

            var slider = BuildSlider("MasterVolumeSlider", panel.transform as RectTransform);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.10f, 0.71f), new Vector2(0.90f, 0.76f), Vector2.zero, Vector2.zero);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(optionsController.Options.Audio.MasterVolume);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(controller.OnMasterVolumeChanged);

            var musicLabel = BuildText("MusicVolumeLabel", panel.transform as RectTransform, "Music Volume", 18, TextAnchor.MiddleLeft);
            SetRect(musicLabel.rectTransform, new Vector2(0.10f, 0.65f), new Vector2(0.90f, 0.71f), Vector2.zero, Vector2.zero);

            var musicSlider = BuildSlider("MusicVolumeSlider", panel.transform as RectTransform);
            SetRect(musicSlider.GetComponent<RectTransform>(), new Vector2(0.10f, 0.60f), new Vector2(0.90f, 0.65f), Vector2.zero, Vector2.zero);
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.SetValueWithoutNotify(optionsController.Options.Audio.MusicVolume);
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(controller.OnMusicVolumeChanged);

            var sfxLabel = BuildText("SfxVolumeLabel", panel.transform as RectTransform, "SFX Volume", 18, TextAnchor.MiddleLeft);
            SetRect(sfxLabel.rectTransform, new Vector2(0.10f, 0.54f), new Vector2(0.90f, 0.60f), Vector2.zero, Vector2.zero);

            var sfxSlider = BuildSlider("SfxVolumeSlider", panel.transform as RectTransform);
            SetRect(sfxSlider.GetComponent<RectTransform>(), new Vector2(0.10f, 0.49f), new Vector2(0.90f, 0.54f), Vector2.zero, Vector2.zero);
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(optionsController.Options.Audio.SfxVolume);
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(controller.OnSfxVolumeChanged);

            var musicStyleLabel = BuildText("MusicStyleLabel", panel.transform as RectTransform, "Menu Music Style", 18, TextAnchor.MiddleLeft);
            SetRect(musicStyleLabel.rectTransform, new Vector2(0.10f, 0.43f), new Vector2(0.44f, 0.48f), Vector2.zero, Vector2.zero);
            var musicStyle = BuildDropdown("MusicStyleDropdown", panel.transform as RectTransform);
            SetRect(musicStyle.GetComponent<RectTransform>(), new Vector2(0.44f, 0.43f), new Vector2(0.90f, 0.48f), Vector2.zero, Vector2.zero);
            musicStyle.ClearOptions();
            musicStyle.AddOptions(new System.Collections.Generic.List<string> { "8-bit Chill", "16-bit Chill" });
            musicStyle.SetValueWithoutNotify(Mathf.Clamp(optionsController.Options.Audio.MenuMusicStyleIndex, 0, 1));
            musicStyle.onValueChanged.RemoveAllListeners();
            musicStyle.onValueChanged.AddListener(controller.OnMenuMusicStyleChanged);

            var displayTitle = BuildText("DisplaySectionTitle", panel.transform as RectTransform, "Display", 20, TextAnchor.MiddleLeft);
            SetRect(displayTitle.rectTransform, new Vector2(0.10f, 0.36f), new Vector2(0.90f, 0.42f), Vector2.zero, Vector2.zero);

            var languageLabel = BuildText("LanguageLabel", panel.transform as RectTransform, "Language", 18, TextAnchor.MiddleLeft);
            SetRect(languageLabel.rectTransform, new Vector2(0.10f, 0.30f), new Vector2(0.44f, 0.35f), Vector2.zero, Vector2.zero);
            var language = BuildDropdown("LanguageDropdown", panel.transform as RectTransform);
            SetRect(language.GetComponent<RectTransform>(), new Vector2(0.44f, 0.30f), new Vector2(0.90f, 0.35f), Vector2.zero, Vector2.zero);
            language.ClearOptions();
            language.AddOptions(new System.Collections.Generic.List<string> { "English", "German" });
            language.SetValueWithoutNotify(optionsController.Options.Language == SudokuRoguelike.Core.LanguageOption.German ? 1 : 0);
            language.onValueChanged.RemoveAllListeners();
            language.onValueChanged.AddListener(controller.OnLanguageChanged);

            var resolutionLabel = BuildText("ResolutionLabel", panel.transform as RectTransform, "Resolution", 18, TextAnchor.MiddleLeft);
            SetRect(resolutionLabel.rectTransform, new Vector2(0.10f, 0.24f), new Vector2(0.44f, 0.29f), Vector2.zero, Vector2.zero);
            var resolution = BuildDropdown("ResolutionDropdown", panel.transform as RectTransform);
            SetRect(resolution.GetComponent<RectTransform>(), new Vector2(0.44f, 0.24f), new Vector2(0.90f, 0.29f), Vector2.zero, Vector2.zero);
            resolution.ClearOptions();
            resolution.AddOptions(new System.Collections.Generic.List<string> { "1280x720 Windowed", "1600x900 Windowed", "1920x1080 Fullscreen", "2560x1440 Fullscreen" });
            resolution.SetValueWithoutNotify(2);
            resolution.onValueChanged.RemoveAllListeners();
            resolution.onValueChanged.AddListener(controller.OnResolutionChanged);

            var accessibilityTitle = BuildText("AccessibilitySectionTitle", panel.transform as RectTransform, "Accessibility", 20, TextAnchor.MiddleLeft);
            SetRect(accessibilityTitle.rectTransform, new Vector2(0.10f, 0.17f), new Vector2(0.90f, 0.23f), Vector2.zero, Vector2.zero);

            var highlightErrors = BuildToggle("HighlightErrorsToggle", panel.transform as RectTransform, "Highlight Errors");
            SetRect(highlightErrors.GetComponent<RectTransform>(), new Vector2(0.10f, 0.11f), new Vector2(0.90f, 0.16f), Vector2.zero, Vector2.zero);
            highlightErrors.SetIsOnWithoutNotify(optionsController.Options.Gameplay.HighlightConflicts);
            highlightErrors.onValueChanged.RemoveAllListeners();
            highlightErrors.onValueChanged.AddListener(controller.OnHighlightErrorsChanged);

            var deleteSave = BuildButton("BtnDeleteSave", panel.transform as RectTransform, "Delete Save", 16);
            SetRect(deleteSave.GetComponent<RectTransform>(), new Vector2(0.10f, 0.03f), new Vector2(0.36f, 0.09f), Vector2.zero, Vector2.zero);
            deleteSave.onClick.RemoveAllListeners();
            deleteSave.onClick.AddListener(controller.OpenDeleteSaveConfirmation);

            var backButton = BuildButton("BtnOptionsBack", panel.transform as RectTransform, "Back", 20);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.03f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(controller.BackToMainMenu);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildSaveConflictPanel(RectTransform root, MainMenuController controller)
        {
            var panel = EnsureRect("SaveConflictPanel", root, new Vector2(0.30f, 0.24f), new Vector2(0.70f, 0.76f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("ConflictTitle", panel.transform as RectTransform, "Save Conflict", 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
            var body = BuildText("ConflictBody", panel.transform as RectTransform, "Local and cloud run saves differ. Choose which one to resume.", 17, TextAnchor.MiddleCenter);
            SetRect(body.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);

            var keepLocal = BuildButton("BtnConflictKeepLocal", panel.transform as RectTransform, "Use Local", 18);
            SetRect(keepLocal.GetComponent<RectTransform>(), new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.40f), Vector2.zero, Vector2.zero);
            keepLocal.onClick.RemoveAllListeners();
            keepLocal.onClick.AddListener(controller.ResolveConflictKeepLocal);

            var keepCloud = BuildButton("BtnConflictKeepCloud", panel.transform as RectTransform, "Use Cloud", 18);
            SetRect(keepCloud.GetComponent<RectTransform>(), new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.28f), Vector2.zero, Vector2.zero);
            keepCloud.onClick.RemoveAllListeners();
            keepCloud.onClick.AddListener(controller.ResolveConflictKeepCloud);

            var cancel = BuildButton("BtnConflictCancel", panel.transform as RectTransform, "Cancel", 16);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.62f, 0.06f), new Vector2(0.88f, 0.14f), Vector2.zero, Vector2.zero);
            cancel.onClick.RemoveAllListeners();
            cancel.onClick.AddListener(controller.ResolveConflictCancel);

            var back = BuildButton("BtnConflictBack", panel.transform as RectTransform, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.36f, 0.06f), new Vector2(0.60f, 0.14f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(controller.BackToMainMenu);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildQuitConfirmPanel(RectTransform root, MainMenuController controller)
        {
            var panel = EnsureRect("ConfirmQuitPanel", root, new Vector2(0.34f, 0.30f), new Vector2(0.66f, 0.70f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("QuitTitle", panel.transform as RectTransform, "Quit Game", 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            var body = BuildText("QuitBody", panel.transform as RectTransform, "Are you sure you want to quit?", 18, TextAnchor.MiddleCenter);
            SetRect(body.rectTransform, new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.70f), Vector2.zero, Vector2.zero);

            var confirm = BuildButton("BtnConfirmQuit", panel.transform as RectTransform, "Quit", 18);
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.18f, 0.20f), new Vector2(0.48f, 0.34f), Vector2.zero, Vector2.zero);
            confirm.onClick.RemoveAllListeners();
            confirm.onClick.AddListener(controller.ConfirmQuit);

            var cancel = BuildButton("BtnCancelQuit", panel.transform as RectTransform, "Back", 18);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.52f, 0.20f), new Vector2(0.82f, 0.34f), Vector2.zero, Vector2.zero);
            cancel.onClick.RemoveAllListeners();
            cancel.onClick.AddListener(controller.CancelQuit);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildDeleteSaveConfirmPanel(RectTransform root, MainMenuController controller)
        {
            var panel = EnsureRect("ConfirmDeleteSavePanel", root, new Vector2(0.32f, 0.28f), new Vector2(0.68f, 0.72f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("DeleteSaveTitle", panel.transform as RectTransform, "Delete Save", 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            var body = BuildText("DeleteSaveBody", panel.transform as RectTransform, "Delete profile and run save files? This cannot be undone.", 17, TextAnchor.MiddleCenter);
            SetRect(body.rectTransform, new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.72f), Vector2.zero, Vector2.zero);

            var confirm = BuildButton("BtnConfirmDeleteSave", panel.transform as RectTransform, "Delete", 17);
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.10f, 0.20f), new Vector2(0.40f, 0.33f), Vector2.zero, Vector2.zero);
            confirm.onClick.RemoveAllListeners();
            confirm.onClick.AddListener(controller.ConfirmDeleteSave);

            var cancel = BuildButton("BtnCancelDeleteSave", panel.transform as RectTransform, "Cancel", 17);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.42f, 0.20f), new Vector2(0.70f, 0.33f), Vector2.zero, Vector2.zero);
            cancel.onClick.RemoveAllListeners();
            cancel.onClick.AddListener(controller.CancelDeleteSave);

            var back = BuildButton("BtnDeleteSaveBack", panel.transform as RectTransform, "Back", 17);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.72f, 0.20f), new Vector2(0.90f, 0.33f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(controller.BackToOptions);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildOnboardingPanel(RectTransform root, MainMenuController controller, out Text bodyText)
        {
            var panel = EnsureRect("OnboardingPanel", root, new Vector2(0.24f, 0.16f), new Vector2(0.76f, 0.84f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("OnboardingTitle", panel.transform as RectTransform, "Welcome to the Garden", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

            bodyText = BuildText("OnboardingBody", panel.transform as RectTransform, "", 18, TextAnchor.MiddleCenter);
            SetRect(bodyText.rectTransform, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.80f), Vector2.zero, Vector2.zero);

            var back = BuildButton("BtnOnboardingBack", panel.transform as RectTransform, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.10f, 0.10f), new Vector2(0.26f, 0.18f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(controller.OnboardingBack);

            var next = BuildButton("BtnOnboardingNext", panel.transform as RectTransform, "Next", 16);
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.28f, 0.10f), new Vector2(0.44f, 0.18f), Vector2.zero, Vector2.zero);
            next.onClick.RemoveAllListeners();
            next.onClick.AddListener(controller.OnboardingNext);

            var skip = BuildButton("BtnOnboardingSkip", panel.transform as RectTransform, "Skip", 16);
            SetRect(skip.GetComponent<RectTransform>(), new Vector2(0.62f, 0.10f), new Vector2(0.76f, 0.18f), Vector2.zero, Vector2.zero);
            skip.onClick.RemoveAllListeners();
            skip.onClick.AddListener(controller.OnboardingSkip);

            var start = BuildButton("BtnOnboardingStart", panel.transform as RectTransform, "Start", 16);
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0.78f, 0.10f), new Vector2(0.90f, 0.18f), Vector2.zero, Vector2.zero);
            start.onClick.RemoveAllListeners();
            start.onClick.AddListener(controller.OnboardingComplete);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildCreditsPanel(RectTransform root, MainMenuController controller)
        {
            var panel = EnsureRect("CreditsPanel", root, new Vector2(0.33f, 0.22f), new Vector2(0.67f, 0.78f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("CreditsTitle", panel.transform as RectTransform, "Credits", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.1f, 0.80f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

            var body = BuildText("CreditsBody", panel.transform as RectTransform, "Run of the Nine\nDesign + Systems + UI Wiring", 18, TextAnchor.MiddleCenter);
            SetRect(body.rectTransform, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.74f), Vector2.zero, Vector2.zero);

            var backButton = BuildButton("BtnCreditsBack", panel.transform as RectTransform, "Back", 20);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.30f, 0.10f), new Vector2(0.70f, 0.24f), Vector2.zero, Vector2.zero);
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(controller.BackToMainMenu);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildTutorialSetupPanel(RectTransform root, MainMenuController controller, TutorialMenuController tutorialController)
        {
            var panel = EnsureRect("TutorialSetupPanel", root, new Vector2(0.22f, 0.10f), new Vector2(0.78f, 0.90f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("TutorialTitle", panel.transform as RectTransform, "Tutorial Setup", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var boardLabel = BuildText("BoardSizeLabel", panel.transform as RectTransform, "Board Size", 18, TextAnchor.MiddleLeft);
            SetRect(boardLabel.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.32f, 0.87f), Vector2.zero, Vector2.zero);
            var boardDropdown = BuildDropdown("BoardSizeDropdown", panel.transform as RectTransform);
            SetRect(boardDropdown.GetComponent<RectTransform>(), new Vector2(0.34f, 0.82f), new Vector2(0.88f, 0.87f), Vector2.zero, Vector2.zero);

            var starsLabel = BuildText("StarsLabel", panel.transform as RectTransform, "Star Difficulty", 18, TextAnchor.MiddleLeft);
            SetRect(starsLabel.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.32f, 0.79f), Vector2.zero, Vector2.zero);
            var starsDropdown = BuildDropdown("StarsDropdown", panel.transform as RectTransform);
            SetRect(starsDropdown.GetComponent<RectTransform>(), new Vector2(0.34f, 0.74f), new Vector2(0.88f, 0.79f), Vector2.zero, Vector2.zero);

            var resourceLabel = BuildText("ResourceModeLabel", panel.transform as RectTransform, "Resource Mode", 18, TextAnchor.MiddleLeft);
            SetRect(resourceLabel.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.32f, 0.71f), Vector2.zero, Vector2.zero);
            var resourceDropdown = BuildDropdown("ResourceModeDropdown", panel.transform as RectTransform);
            SetRect(resourceDropdown.GetComponent<RectTransform>(), new Vector2(0.34f, 0.66f), new Vector2(0.88f, 0.71f), Vector2.zero, Vector2.zero);

            var regionLabel = BuildText("RegionLayoutLabel", panel.transform as RectTransform, "Region Layout", 18, TextAnchor.MiddleLeft);
            SetRect(regionLabel.rectTransform, new Vector2(0.08f, 0.59f), new Vector2(0.32f, 0.64f), Vector2.zero, Vector2.zero);
            var regionDropdown = BuildDropdown("RegionLayoutDropdown", panel.transform as RectTransform);
            SetRect(regionDropdown.GetComponent<RectTransform>(), new Vector2(0.34f, 0.59f), new Vector2(0.88f, 0.64f), Vector2.zero, Vector2.zero);

            var modifiersTitle = BuildText("ModifiersTitle", panel.transform as RectTransform, "Boss Mechanics (0-2)", 18, TextAnchor.MiddleLeft);
            SetRect(modifiersTitle.rectTransform, new Vector2(0.08f, 0.51f), new Vector2(0.92f, 0.57f), Vector2.zero, Vector2.zero);

            var modRoot = EnsureRect("ModifierToggles", panel.transform as RectTransform, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.51f), Vector2.zero, Vector2.zero);
            var modLayout = EnsureComponent<GridLayoutGroup>(modRoot.gameObject);
            modLayout.cellSize = new Vector2(260f, 32f);
            modLayout.spacing = new Vector2(8f, 8f);
            modLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            modLayout.constraintCount = 2;

            var fog = BuildToggle("TglModifierFog", modRoot, "Fog of War");
            var arrow = BuildToggle("TglModifierArrow", modRoot, "Arrow Sums");
            var german = BuildToggle("TglModifierGerman", modRoot, "German Whispers");
            var dutch = BuildToggle("TglModifierDutch", modRoot, "Dutch Whispers");
            var parity = BuildToggle("TglModifierParity", modRoot, "Parity Lines");
            var renban = BuildToggle("TglModifierRenban", modRoot, "Renban Lines");
            var killer = BuildToggle("TglModifierKiller", modRoot, "Killer Cages");
            var difference = BuildToggle("TglModifierDifference", modRoot, "Difference Kropki");
            var ratio = BuildToggle("TglModifierRatio", modRoot, "Ratio Kropki");

            var validation = BuildText("TutorialValidationText", panel.transform as RectTransform, "Ready.", 16, TextAnchor.MiddleLeft);
            SetRect(validation.rectTransform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.25f), Vector2.zero, Vector2.zero);

            var completionHint = BuildText("TutorialCompletionHint", panel.transform as RectTransform, "Current configuration: ✖ Not completed", 16, TextAnchor.MiddleLeft);
            SetRect(completionHint.rectTransform, new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero);

            var modifierDescription = BuildText("ModifierDescriptionText", panel.transform as RectTransform, "Select a modifier to see its rule.", 15, TextAnchor.UpperLeft);
            SetRect(modifierDescription.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.62f, 0.14f), Vector2.zero, Vector2.zero);

            var startButton = BuildButton("BtnTutorialStart", panel.transform as RectTransform, "Start Puzzle", 18);
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0.05f), new Vector2(0.82f, 0.14f), Vector2.zero, Vector2.zero);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(tutorialController.StartTutorialFromSetup);

            var progressButton = BuildButton("BtnTutorialProgress", panel.transform as RectTransform, "Progress", 18);
            SetRect(progressButton.GetComponent<RectTransform>(), new Vector2(0.83f, 0.05f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero);
            progressButton.onClick.RemoveAllListeners();
            progressButton.onClick.AddListener(controller.OpenTutorialProgress);

            var backButton = BuildButton("BtnTutorialBack", panel.transform as RectTransform, "Back", 18);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.93f, 0.05f), new Vector2(0.99f, 0.14f), Vector2.zero, Vector2.zero);
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(controller.BackToMainMenu);

            tutorialController.Configure(
                controller,
                boardDropdown,
                starsDropdown,
                resourceDropdown,
                regionDropdown,
                validation,
                completionHint,
                modifierDescription,
                startButton,
                fog,
                arrow,
                german,
                dutch,
                parity,
                renban,
                killer,
                difference,
                ratio,
                null,
                null,
                null);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildTutorialProgressPanel(RectTransform root, MainMenuController controller, TutorialMenuController tutorialController)
        {
            var panel = EnsureRect("TutorialProgressPanel", root, new Vector2(0.22f, 0.10f), new Vector2(0.78f, 0.90f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("TutorialProgressTitle", panel.transform as RectTransform, "Tutorial Progress", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var boardProgress = BuildText("BoardProgressText", panel.transform as RectTransform, "", 16, TextAnchor.UpperLeft);
            SetRect(boardProgress.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.45f, 0.88f), Vector2.zero, Vector2.zero);

            var modifierProgress = BuildText("ModifierProgressText", panel.transform as RectTransform, "", 16, TextAnchor.UpperLeft);
            SetRect(modifierProgress.rectTransform, new Vector2(0.50f, 0.30f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            var completionPercent = BuildText("CompletionPercentText", panel.transform as RectTransform, "Completion: 0%", 18, TextAnchor.MiddleLeft);
            SetRect(completionPercent.rectTransform, new Vector2(0.50f, 0.22f), new Vector2(0.92f, 0.28f), Vector2.zero, Vector2.zero);

            var markButton = BuildButton("BtnMarkCurrentComplete", panel.transform as RectTransform, "Mark Current Config ✔", 16);
            SetRect(markButton.GetComponent<RectTransform>(), new Vector2(0.50f, 0.14f), new Vector2(0.78f, 0.20f), Vector2.zero, Vector2.zero);
            markButton.onClick.RemoveAllListeners();
            markButton.onClick.AddListener(tutorialController.MarkCurrentConfigurationSolvedForPrototype);

            var backToSetup = BuildButton("BtnProgressBackToSetup", panel.transform as RectTransform, "Back to Setup", 16);
            SetRect(backToSetup.GetComponent<RectTransform>(), new Vector2(0.79f, 0.14f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero);
            backToSetup.onClick.RemoveAllListeners();
            backToSetup.onClick.AddListener(controller.OpenTutorial);

            var backMain = BuildButton("BtnTutorialProgressBack", panel.transform as RectTransform, "Main Menu", 16);
            SetRect(backMain.GetComponent<RectTransform>(), new Vector2(0.83f, 0.90f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);
            backMain.onClick.RemoveAllListeners();
            backMain.onClick.AddListener(controller.BackToMainMenu);

            tutorialController.Configure(
                controller,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                boardProgress,
                modifierProgress,
                completionPercent);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildMetaProgressionPanel(RectTransform root, MainMenuController controller, MetaProgressionPanelController metaController)
        {
            var panel = EnsureRect("MetaProgressionPanel", root, new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("MetaTitle", panel.transform as RectTransform, "Meta Progression", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var summary = BuildText("MetaSummaryText", panel.transform as RectTransform, "", 17, TextAnchor.UpperLeft);
            SetRect(summary.rectTransform, new Vector2(0.08f, 0.70f), new Vector2(0.45f, 0.88f), Vector2.zero, Vector2.zero);

            var selectedClass = BuildText("SelectedClassText", panel.transform as RectTransform, "Selected Class: NumberFreak", 17, TextAnchor.MiddleLeft);
            SetRect(selectedClass.rectTransform, new Vector2(0.50f, 0.78f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            var classesTitle = BuildText("UnlockedClassesTitle", panel.transform as RectTransform, "Available Classes", 18, TextAnchor.MiddleLeft);
            SetRect(classesTitle.rectTransform, new Vector2(0.50f, 0.72f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);

            var classProgress = BuildText("ClassProgressText", panel.transform as RectTransform, "", 15, TextAnchor.UpperLeft);
            SetRect(classProgress.rectTransform, new Vector2(0.08f, 0.26f), new Vector2(0.46f, 0.68f), Vector2.zero, Vector2.zero);

            var btnNF = BuildButton("BtnClassNumberFreak", panel.transform as RectTransform, "Number Freak", 15);
            SetRect(btnNF.GetComponent<RectTransform>(), new Vector2(0.08f, 0.14f), new Vector2(0.22f, 0.20f), Vector2.zero, Vector2.zero);
            btnNF.onClick.RemoveAllListeners();
            btnNF.onClick.AddListener(metaController.SelectClassNumberFreak);

            var btnGM = BuildButton("BtnClassGardenMonk", panel.transform as RectTransform, "Garden Monk", 15);
            SetRect(btnGM.GetComponent<RectTransform>(), new Vector2(0.23f, 0.14f), new Vector2(0.37f, 0.20f), Vector2.zero, Vector2.zero);
            btnGM.onClick.RemoveAllListeners();
            btnGM.onClick.AddListener(metaController.SelectClassGardenMonk);

            var btnSA = BuildButton("BtnClassShrineArchivist", panel.transform as RectTransform, "Shrine Archivist", 15);
            SetRect(btnSA.GetComponent<RectTransform>(), new Vector2(0.38f, 0.14f), new Vector2(0.56f, 0.20f), Vector2.zero, Vector2.zero);
            btnSA.onClick.RemoveAllListeners();
            btnSA.onClick.AddListener(metaController.SelectClassShrineArchivist);

            var btnKG = BuildButton("BtnClassKoiGambler", panel.transform as RectTransform, "Koi Gambler", 15);
            SetRect(btnKG.GetComponent<RectTransform>(), new Vector2(0.57f, 0.14f), new Vector2(0.71f, 0.20f), Vector2.zero, Vector2.zero);
            btnKG.onClick.RemoveAllListeners();
            btnKG.onClick.AddListener(metaController.SelectClassKoiGambler);

            var btnSG = BuildButton("BtnClassStoneGardener", panel.transform as RectTransform, "Stone Gardener", 15);
            SetRect(btnSG.GetComponent<RectTransform>(), new Vector2(0.72f, 0.14f), new Vector2(0.86f, 0.20f), Vector2.zero, Vector2.zero);
            btnSG.onClick.RemoveAllListeners();
            btnSG.onClick.AddListener(metaController.SelectClassStoneGardener);

            var btnLS = BuildButton("BtnClassLanternSeer", panel.transform as RectTransform, "Lantern Seer", 15);
            SetRect(btnLS.GetComponent<RectTransform>(), new Vector2(0.72f, 0.58f), new Vector2(0.92f, 0.65f), Vector2.zero, Vector2.zero);
            btnLS.onClick.RemoveAllListeners();
            btnLS.onClick.AddListener(metaController.SelectClassLanternSeer);

            SetRect(btnNF.GetComponent<RectTransform>(), new Vector2(0.50f, 0.66f), new Vector2(0.70f, 0.73f), Vector2.zero, Vector2.zero);
            SetRect(btnGM.GetComponent<RectTransform>(), new Vector2(0.72f, 0.66f), new Vector2(0.92f, 0.73f), Vector2.zero, Vector2.zero);
            SetRect(btnSA.GetComponent<RectTransform>(), new Vector2(0.50f, 0.58f), new Vector2(0.70f, 0.65f), Vector2.zero, Vector2.zero);
            SetRect(btnKG.GetComponent<RectTransform>(), new Vector2(0.72f, 0.50f), new Vector2(0.92f, 0.57f), Vector2.zero, Vector2.zero);
            SetRect(btnSG.GetComponent<RectTransform>(), new Vector2(0.50f, 0.50f), new Vector2(0.70f, 0.57f), Vector2.zero, Vector2.zero);

            var unlockDemo = BuildButton("BtnMetaUnlockDemo", panel.transform as RectTransform, "Unlock Demo Content", 15);
            SetRect(unlockDemo.GetComponent<RectTransform>(), new Vector2(0.50f, 0.22f), new Vector2(0.92f, 0.29f), Vector2.zero, Vector2.zero);
            unlockDemo.onClick.RemoveAllListeners();
            unlockDemo.onClick.AddListener(metaController.UnlockDemoContent);

            var refresh = BuildButton("BtnMetaRefresh", panel.transform as RectTransform, "Refresh", 15);
            SetRect(refresh.GetComponent<RectTransform>(), new Vector2(0.50f, 0.14f), new Vector2(0.70f, 0.20f), Vector2.zero, Vector2.zero);
            refresh.onClick.RemoveAllListeners();
            refresh.onClick.AddListener(metaController.RefreshView);

            var back = BuildButton("BtnMetaBack", panel.transform as RectTransform, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.87f, 0.90f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(controller.BackToMainMenu);

            metaController.Configure(controller, summary, classProgress, selectedClass);
            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildGameModesPanel(RectTransform root, MainMenuController controller, GameModesPanelController modesController)
        {
            var panel = EnsureRect("GameModesPanel", root, new Vector2(0.28f, 0.16f), new Vector2(0.72f, 0.84f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("ModesTitle", panel.transform as RectTransform, "Game Modes", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            var summary = BuildText("ModesSummaryText", panel.transform as RectTransform, "", 17, TextAnchor.UpperLeft);
            SetRect(summary.rectTransform, new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.82f), Vector2.zero, Vector2.zero);

            var garden = BuildButton("BtnModeGardenRun", panel.transform as RectTransform, "Start Garden Run", 18);
            SetRect(garden.GetComponent<RectTransform>(), new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.45f), Vector2.zero, Vector2.zero);
            garden.onClick.RemoveAllListeners();
            garden.onClick.AddListener(modesController.StartGardenRun);

            var endless = BuildButton("BtnModeEndless", panel.transform as RectTransform, "Start Endless Zen", 18);
            SetRect(endless.GetComponent<RectTransform>(), new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.34f), Vector2.zero, Vector2.zero);
            endless.onClick.RemoveAllListeners();
            endless.onClick.AddListener(modesController.StartEndlessZen);

            var trials = BuildButton("BtnModeTrials", panel.transform as RectTransform, "Start Spirit Trials", 18);
            SetRect(trials.GetComponent<RectTransform>(), new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.23f), Vector2.zero, Vector2.zero);
            trials.onClick.RemoveAllListeners();
            trials.onClick.AddListener(modesController.StartSpiritTrials);

            ApplyMenuButtonIcon(garden, "GeneratedIcons/icon_bud");
            ApplyMenuButtonIcon(endless, "GeneratedIcons/icon_infinite_lotus");
            ApplyMenuButtonIcon(trials, "GeneratedIcons/icon_temple_seal");

            var refresh = BuildButton("BtnModesRefresh", panel.transform as RectTransform, "Refresh", 16);
            SetRect(refresh.GetComponent<RectTransform>(), new Vector2(0.12f, 0.05f), new Vector2(0.35f, 0.11f), Vector2.zero, Vector2.zero);
            refresh.onClick.RemoveAllListeners();
            refresh.onClick.AddListener(modesController.RefreshView);

            var back = BuildButton("BtnModesBack", panel.transform as RectTransform, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.76f, 0.05f), new Vector2(0.88f, 0.11f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(controller.BackToMainMenu);

            modesController.Configure(controller, summary);
            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildItemsPanel(RectTransform root, MainMenuController controller, ItemsMenuController itemsController)
        {
            var panel = EnsureRect("ItemsPanel", root, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("ItemsTitle", panel.transform as RectTransform, "Items Archive", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var completion = BuildText("ItemsCompletionText", panel.transform as RectTransform, "Completion: 0 / 0", 17, TextAnchor.MiddleLeft);
            SetRect(completion.rectTransform, new Vector2(0.06f, 0.84f), new Vector2(0.50f, 0.90f), Vector2.zero, Vector2.zero);

            var filter = BuildText("ItemsFilterText", panel.transform as RectTransform, "Filter: All", 16, TextAnchor.MiddleLeft);
            SetRect(filter.rectTransform, new Vector2(0.52f, 0.84f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);

            var all = BuildButton("BtnItemsAll", panel.transform as RectTransform, "All", 14);
            SetRect(all.GetComponent<RectTransform>(), new Vector2(0.06f, 0.76f), new Vector2(0.16f, 0.82f), Vector2.zero, Vector2.zero);
            all.onClick.RemoveAllListeners();
            all.onClick.AddListener(itemsController.FilterAll);

            var relics = BuildButton("BtnItemsRelics", panel.transform as RectTransform, "Relics", 14);
            SetRect(relics.GetComponent<RectTransform>(), new Vector2(0.17f, 0.76f), new Vector2(0.27f, 0.82f), Vector2.zero, Vector2.zero);
            relics.onClick.RemoveAllListeners();
            relics.onClick.AddListener(itemsController.FilterRelics);

            var consumables = BuildButton("BtnItemsConsumables", panel.transform as RectTransform, "Consumables", 14);
            SetRect(consumables.GetComponent<RectTransform>(), new Vector2(0.28f, 0.76f), new Vector2(0.42f, 0.82f), Vector2.zero, Vector2.zero);
            consumables.onClick.RemoveAllListeners();
            consumables.onClick.AddListener(itemsController.FilterConsumables);

            var cursed = BuildButton("BtnItemsCursed", panel.transform as RectTransform, "Cursed", 14);
            SetRect(cursed.GetComponent<RectTransform>(), new Vector2(0.43f, 0.76f), new Vector2(0.53f, 0.82f), Vector2.zero, Vector2.zero);
            cursed.onClick.RemoveAllListeners();
            cursed.onClick.AddListener(itemsController.FilterCursed);

            var legendary = BuildButton("BtnItemsLegendary", panel.transform as RectTransform, "Legendary", 14);
            SetRect(legendary.GetComponent<RectTransform>(), new Vector2(0.54f, 0.76f), new Vector2(0.66f, 0.82f), Vector2.zero, Vector2.zero);
            legendary.onClick.RemoveAllListeners();
            legendary.onClick.AddListener(itemsController.FilterLegendary);

            var boss = BuildButton("BtnItemsBoss", panel.transform as RectTransform, "Boss Rewards", 14);
            SetRect(boss.GetComponent<RectTransform>(), new Vector2(0.67f, 0.76f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
            boss.onClick.RemoveAllListeners();
            boss.onClick.AddListener(itemsController.FilterBossRewards);

            var classSpecific = BuildButton("BtnItemsClass", panel.transform as RectTransform, "Class-Specific", 14);
            SetRect(classSpecific.GetComponent<RectTransform>(), new Vector2(0.83f, 0.76f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);
            classSpecific.onClick.RemoveAllListeners();
            classSpecific.onClick.AddListener(itemsController.FilterClassSpecific);

            var unseen = BuildButton("BtnItemsUnseen", panel.transform as RectTransform, "Unseen", 14);
            SetRect(unseen.GetComponent<RectTransform>(), new Vector2(0.06f, 0.69f), new Vector2(0.16f, 0.75f), Vector2.zero, Vector2.zero);
            unseen.onClick.RemoveAllListeners();
            unseen.onClick.AddListener(itemsController.FilterUnseen);

            var sortRarity = BuildButton("BtnItemsSortRarity", panel.transform as RectTransform, "Sort: Rarity", 13);
            SetRect(sortRarity.GetComponent<RectTransform>(), new Vector2(0.17f, 0.69f), new Vector2(0.31f, 0.75f), Vector2.zero, Vector2.zero);
            sortRarity.onClick.RemoveAllListeners();
            sortRarity.onClick.AddListener(itemsController.SortByRarity);

            var sortUsed = BuildButton("BtnItemsSortUsed", panel.transform as RectTransform, "Sort: Used", 13);
            SetRect(sortUsed.GetComponent<RectTransform>(), new Vector2(0.32f, 0.69f), new Vector2(0.46f, 0.75f), Vector2.zero, Vector2.zero);
            sortUsed.onClick.RemoveAllListeners();
            sortUsed.onClick.AddListener(itemsController.SortByMostUsed);

            var sortWinRate = BuildButton("BtnItemsSortWinRate", panel.transform as RectTransform, "Sort: Win%", 13);
            SetRect(sortWinRate.GetComponent<RectTransform>(), new Vector2(0.47f, 0.69f), new Vector2(0.61f, 0.75f), Vector2.zero, Vector2.zero);
            sortWinRate.onClick.RemoveAllListeners();
            sortWinRate.onClick.AddListener(itemsController.SortByWinRate);

            var reset = BuildButton("BtnItemsReset", panel.transform as RectTransform, "Reset", 13);
            SetRect(reset.GetComponent<RectTransform>(), new Vector2(0.62f, 0.69f), new Vector2(0.72f, 0.75f), Vector2.zero, Vector2.zero);
            reset.onClick.RemoveAllListeners();
            reset.onClick.AddListener(itemsController.ResetFiltersAndSort);

            var iconStrip = EnsureRect("ItemsIconStrip", panel.transform as RectTransform, new Vector2(0.52f, 0.22f), new Vector2(0.94f, 0.34f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(iconStrip.gameObject, new Color(0f, 0f, 0f, 0.14f));
            var iconLayout = EnsureComponent<GridLayoutGroup>(iconStrip.gameObject);
            iconLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            iconLayout.constraintCount = 5;
            iconLayout.cellSize = new Vector2(74f, 86f);
            iconLayout.spacing = new Vector2(8f, 0f);
            iconLayout.childAlignment = TextAnchor.MiddleCenter;
            iconLayout.padding = new RectOffset(6, 6, 6, 6);

            var iconSlots = new Image[5];
            var iconLabels = new Text[5];
            for (var i = 0; i < iconSlots.Length; i++)
            {
                var slot = EnsureRect($"IconSlot_{i}", iconStrip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                slot.sizeDelta = new Vector2(74f, 86f);

                var slotBg = EnsureOrGetImage(slot.gameObject, new Color(0f, 0f, 0f, 0.22f));
                slotBg.type = Image.Type.Sliced;

                var iconRect = EnsureRect("IconImage", slot, new Vector2(0.10f, 0.38f), new Vector2(0.90f, 0.95f), Vector2.zero, Vector2.zero);
                var slotImage = EnsureOrGetImage(iconRect.gameObject, new Color(0f, 0f, 0f, 0.30f));
                iconSlots[i] = slotImage;

                var label = BuildText("Label", slot, "", 10, TextAnchor.MiddleCenter);
                SetRect(label.rectTransform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.34f), Vector2.zero, Vector2.zero);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                iconLabels[i] = label;
            }

            var legacyGrid = panel.transform.Find("ItemsGridText");
            if (legacyGrid != null)
            {
                DestroyImmediate(legacyGrid.gameObject);
            }

            var itemsListScroll = EnsureRect("ItemsListScroll", panel.transform as RectTransform, new Vector2(0.06f, 0.22f), new Vector2(0.48f, 0.68f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(itemsListScroll.gameObject, new Color(0f, 0f, 0f, 0.12f));

            var itemsListViewport = EnsureRect("Viewport", itemsListScroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var itemsListViewportImage = EnsureOrGetImage(itemsListViewport.gameObject, new Color(1f, 1f, 1f, 0.02f));
            var itemsListMask = EnsureComponent<Mask>(itemsListViewport.gameObject);
            itemsListMask.showMaskGraphic = false;
            itemsListViewportImage.raycastTarget = true;

            var itemsListContent = EnsureRect("Content", itemsListViewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -10f), new Vector2(-10f, -10f));
            itemsListContent.pivot = new Vector2(0.5f, 1f);
            var listFitter = EnsureComponent<ContentSizeFitter>(itemsListContent.gameObject);
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var listLayout = EnsureComponent<VerticalLayoutGroup>(itemsListContent.gameObject);
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.spacing = 6f;
            listLayout.padding = new RectOffset(0, 0, 0, 0);

            var itemsListScrollRect = EnsureComponent<ScrollRect>(itemsListScroll.gameObject);
            itemsListScrollRect.viewport = itemsListViewport;
            itemsListScrollRect.content = itemsListContent;
            itemsListScrollRect.horizontal = false;
            itemsListScrollRect.vertical = true;
            itemsListScrollRect.movementType = ScrollRect.MovementType.Clamped;
            itemsListScrollRect.scrollSensitivity = 24f;

            var grid = BuildText("ItemsGridText", itemsListContent, "", 15, TextAnchor.UpperLeft);
            SetRect(grid.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            grid.horizontalOverflow = HorizontalWrapMode.Wrap;
            grid.verticalOverflow = VerticalWrapMode.Overflow;
            grid.gameObject.SetActive(false);

            var details = BuildText("ItemsDetailText", panel.transform as RectTransform, "", 15, TextAnchor.UpperLeft);
            SetRect(details.rectTransform, new Vector2(0.52f, 0.36f), new Vector2(0.94f, 0.68f), Vector2.zero, Vector2.zero);

            var tooltip = BuildText("ItemsTooltipText", panel.transform as RectTransform, "", 14, TextAnchor.UpperLeft);
            SetRect(tooltip.rectTransform, new Vector2(0.52f, 0.10f), new Vector2(0.94f, 0.22f), Vector2.zero, Vector2.zero);

            var prev = BuildButton("BtnItemsPrev", panel.transform as RectTransform, "Prev", 15);
            SetRect(prev.GetComponent<RectTransform>(), new Vector2(0.06f, 0.10f), new Vector2(0.16f, 0.16f), Vector2.zero, Vector2.zero);
            prev.onClick.RemoveAllListeners();
            prev.onClick.AddListener(itemsController.SelectPrev);

            var next = BuildButton("BtnItemsNext", panel.transform as RectTransform, "Next", 15);
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.17f, 0.10f), new Vector2(0.27f, 0.16f), Vector2.zero, Vector2.zero);
            next.onClick.RemoveAllListeners();
            next.onClick.AddListener(itemsController.SelectNext);

            var discover = BuildButton("BtnItemsDiscoverPrototype", panel.transform as RectTransform, "Discover (Prototype)", 14);
            SetRect(discover.GetComponent<RectTransform>(), new Vector2(0.29f, 0.10f), new Vector2(0.48f, 0.16f), Vector2.zero, Vector2.zero);
            discover.onClick.RemoveAllListeners();
            discover.onClick.AddListener(itemsController.MarkRandomDiscoveredForPrototype);

            var back = BuildButton("BtnItemsBack", panel.transform as RectTransform, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.82f, 0.04f), new Vector2(0.94f, 0.10f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(controller.BackToMainMenu);

            itemsController.Configure(controller, completion, grid, details, tooltip, filter, iconSlots, iconLabels, itemsListContent, itemsListScrollRect);
            panel.SetActive(false);
            return panel;
        }

        private Button BuildMenuButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
        {
            var button = BuildButton(name, parent, label, 20);
            SetRect(button.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            return button;
        }

        private bool TryApplyFullBackgroundSprite(Image rootImage)
        {
            if (rootImage == null)
            {
                return false;
            }

            var source = TryLoadBackgroundTextureFromResources(
                "GeneratedIcons/main_menue",
                "main_menue",
                "GeneratedIcons/main_menu",
                "main_menu");

            if (source == null)
            {
                source = TryLoadBackgroundTextureFromDocs("main_menue.png", "main_menu.png");
            }

            if (source == null)
            {
                Debug.LogWarning("MainMenuBlueprintBuilder: main_menue.png not found in Resources or docs/. Menu art not applied.");
                rootImage.sprite = null;
                return false;
            }

            var sprite = Sprite.Create(source, new Rect(0, 0, source.width, source.height), new Vector2(0.5f, 0.5f), 100f);
            rootImage.sprite = sprite;
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = false;
            rootImage.color = Color.white;
            return true;
        }

        private static Texture2D TryLoadBackgroundTextureFromDocs(params string[] fileNames)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                projectRoot = Directory.GetCurrentDirectory();
            }

            var searchDirs = new[] { "docs", Path.Combine("docs", "icons") };

            for (var d = 0; d < searchDirs.Length; d++)
            {
                for (var f = 0; f < fileNames.Length; f++)
                {
                    var path = Path.Combine(projectRoot, searchDirs[d], fileNames[f]);
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var bytes = File.ReadAllBytes(path);
                    if (bytes == null || bytes.Length == 0)
                    {
                        continue;
                    }

                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes, markNonReadable: false))
                    {
                        return tex;
                    }
                }
            }

            return null;
        }

        private void TryApplyMainMenuButtonSlice(Button button, Vector2 cardAnchorMin, Vector2 cardAnchorMax)
        {
            if (button == null)
            {
                return;
            }

            var source = TryLoadBackgroundTextureFromResources(
                "GeneratedIcons/main_menue",
                "main_menue",
                "GeneratedIcons/main_menu",
                "main_menu");
            if (source == null)
            {
                return;
            }

            // Image is now directly on the card, so button anchors map directly to image UV.
            var pxMin = Mathf.Clamp(Mathf.RoundToInt(cardAnchorMin.x * source.width), 0, source.width - 1);
            var pxMax = Mathf.Clamp(Mathf.RoundToInt(cardAnchorMax.x * source.width), pxMin + 1, source.width);
            var pyMin = Mathf.Clamp(Mathf.RoundToInt(cardAnchorMin.y * source.height), 0, source.height - 1);
            var pyMax = Mathf.Clamp(Mathf.RoundToInt(cardAnchorMax.y * source.height), pyMin + 1, source.height);

            var rect = new Rect(pxMin, pyMin, pxMax - pxMin, pyMax - pyMin);
            var sprite = Sprite.Create(source, rect, new Vector2(0.5f, 0.5f), 100f);

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private void TryApplyMainMenuCardSlice(RectTransform card)
        {
            if (card == null)
            {
                return;
            }

            var image = card.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var source = TryLoadBackgroundTextureFromResources(
                "GeneratedIcons/main_menue",
                "main_menue",
                "GeneratedIcons/main_menu",
                "main_menu");
            if (source == null)
            {
                return;
            }

            // Mirror the exact card viewport from the full menu art.
            var xMin = Mathf.Min(card.anchorMin.x, card.anchorMax.x);
            var xMax = Mathf.Max(card.anchorMin.x, card.anchorMax.x);
            var yMin = Mathf.Min(card.anchorMin.y, card.anchorMax.y);
            var yMax = Mathf.Max(card.anchorMin.y, card.anchorMax.y);

            var pxMin = Mathf.Clamp(Mathf.RoundToInt(xMin * source.width), 0, source.width - 1);
            var pxMax = Mathf.Clamp(Mathf.RoundToInt(xMax * source.width), pxMin + 1, source.width);
            var pyMin = Mathf.Clamp(Mathf.RoundToInt(yMin * source.height), 0, source.height - 1);
            var pyMax = Mathf.Clamp(Mathf.RoundToInt(yMax * source.height), pyMin + 1, source.height);

            var rect = new Rect(pxMin, pyMin, pxMax - pxMin, pyMax - pyMin);
            var sprite = Sprite.Create(source, rect, new Vector2(0.5f, 0.5f), 100f);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private void ApplyMenuButtonIcon(Button button, string resourcePath)
        {
            if (button == null)
            {
                return;
            }

            var icon = LoadSpriteFromResources(resourcePath);
            if (icon == null)
            {
                return;
            }

            var iconRect = EnsureRect("Icon", button.transform as RectTransform, new Vector2(0.02f, 0.16f), new Vector2(0.13f, 0.84f), Vector2.zero, Vector2.zero);
            var iconImage = EnsureOrGetImage(iconRect.gameObject, Color.white);
            iconImage.sprite = icon;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;

            var label = button.transform.Find("Label") as RectTransform;
            if (label != null)
            {
                SetRect(label, new Vector2(0.16f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);
                label.SetAsLastSibling();
            }
        }

        private static Sprite LoadSpriteFromResources(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            var normalized = resourcePath.StartsWith("GeneratedIcons/")
                ? resourcePath
                : "GeneratedIcons/" + resourcePath;

            sprite = Resources.Load<Sprite>(normalized);
            if (sprite != null)
            {
                return sprite;
            }

            tex = Resources.Load<Texture2D>(normalized);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return null;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void EnsureMainCamera()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
            camera.orthographic = true;
        }

        private Canvas EnsureCanvas()
        {
            var existing = GetComponentInParent<Canvas>();
            if (existing != null)
            {
                return existing;
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RectTransform EnsureRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                child = go.GetComponent<RectTransform>();
            }

            child.anchorMin = anchorMin;
            child.anchorMax = anchorMax;
            child.offsetMin = offsetMin;
            child.offsetMax = offsetMax;
            return child;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private Image EnsureOrGetImage(GameObject go, Color color)
        {
            var image = EnsureComponent<Image>(go);
            image.color = color;
            return image;
        }

        private Text BuildText(string name, RectTransform parent, string value, int size, TextAnchor anchor)
        {
            var rect = EnsureRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var text = EnsureComponent<Text>(rect.gameObject);
            text.text = value;
            text.font = GetBuiltInFont();
            text.fontSize = size;
            text.alignment = anchor;
            text.color = textColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button BuildButton(string name, RectTransform parent, string label, int size)
        {
            var rect = EnsureRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var image = EnsureOrGetImage(rect.gameObject, buttonColor);
            image.raycastTarget = true;

            var button = EnsureComponent<Button>(rect.gameObject);
            var outline = EnsureComponent<Outline>(rect.gameObject);
            outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.38f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = new Color(buttonColor.r + 0.05f, buttonColor.g + 0.05f, buttonColor.b + 0.05f, 1f);
            colors.pressedColor = new Color(buttonColor.r - 0.03f, buttonColor.g - 0.03f, buttonColor.b - 0.03f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.22f, 0.22f, 0.22f, 0.7f);
            colors.fadeDuration = 0.08f;
            colors.colorMultiplier = 1.35f;
            button.colors = colors;

            var labelRect = EnsureRect("Label", rect, Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -8f));
            var text = EnsureComponent<Text>(labelRect.gameObject);
            text.text = label;
            text.font = GetBuiltInFont();
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;

            var shadow = EnsureComponent<Shadow>(labelRect.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(1f, -1f);

            labelRect.SetAsLastSibling();

            return button;
        }

        private Dropdown BuildDropdown(string name, RectTransform parent)
        {
            var rect = EnsureRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(rect.gameObject, buttonColor);

            var dropdown = EnsureComponent<Dropdown>(rect.gameObject);
            var label = BuildText("Label", rect, "Select", 16, TextAnchor.MiddleLeft);
            SetRect(label.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.76f, 0.9f), Vector2.zero, Vector2.zero);

            var arrowRect = EnsureRect("Arrow", rect, new Vector2(0.82f, 0.15f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
            var arrowText = EnsureComponent<Text>(arrowRect.gameObject);
            arrowText.font = GetBuiltInFont();
            arrowText.text = "▼";
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = textColor;
            arrowText.fontSize = 16;

            var template = EnsureRect("Template", rect, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 180f);
            EnsureOrGetImage(template.gameObject, panelColor);
            template.gameObject.SetActive(false);

            var viewport = EnsureRect("Viewport", template, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(viewport.gameObject, new Color(0f, 0f, 0f, 0.1f));
            EnsureComponent<Mask>(viewport.gameObject).showMaskGraphic = false;

            var content = EnsureRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var contentLayout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.spacing = 2f;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            var contentFitter = EnsureComponent<ContentSizeFitter>(content.gameObject);
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = EnsureRect("Item", content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), new Vector2(0f, 0f));
            var itemLayout = EnsureComponent<LayoutElement>(item.gameObject);
            itemLayout.minHeight = 30f;
            var itemBackground = EnsureOrGetImage(item.gameObject, new Color(0f, 0f, 0f, 0.2f));
            var itemToggle = EnsureComponent<Toggle>(item.gameObject);
            itemToggle.targetGraphic = itemBackground;

            var itemCheckmark = EnsureRect("Item Checkmark", item, new Vector2(0.02f, 0.18f), new Vector2(0.08f, 0.82f), Vector2.zero, Vector2.zero);
            var itemCheckmarkImage = EnsureOrGetImage(itemCheckmark.gameObject, accentColor);
            itemToggle.graphic = itemCheckmarkImage;

            var itemLabel = BuildText("Item Label", item, "Option", 16, TextAnchor.MiddleLeft);
            SetRect(itemLabel.rectTransform, new Vector2(0.10f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            // Newer Unity versions hide DropdownItem type; bind it via reflection for compatibility.
            AttachDropdownItemCompat(item.gameObject, item, itemLabel, itemBackground, itemToggle);

            var scrollRect = EnsureComponent<ScrollRect>(template.gameObject);
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            dropdown.captionText = label;
            dropdown.template = template;
            dropdown.itemText = itemLabel;
            dropdown.itemImage = null;
            dropdown.targetGraphic = EnsureOrGetImage(rect.gameObject, buttonColor);
            dropdown.captionText.raycastTarget = false;

            EnsureComponent<DropdownAutoSizeController>(rect.gameObject);

            return dropdown;
        }

        private static bool TryApplyMainMenuPngBackground(RectTransform root)
        {
            if (root == null)
            {
                return false;
            }

            var image = root.GetComponent<Image>();
            if (image == null)
            {
                return false;
            }

            // Prefer a Resources-based sprite so the same asset is used in editor and builds.
            var resourceSprite = TryLoadBackgroundSpriteFromResources(
                "GeneratedIcons/main_menue",
                "main_menue",
                "GeneratedIcons/main_menu",
                "main_menu");
            if (resourceSprite != null)
            {
                image.sprite = resourceSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
                return true;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                projectRoot = Directory.GetCurrentDirectory();
            }

            var docsPath = Path.Combine(projectRoot, "docs", "main_menue.png");
            if (!File.Exists(docsPath))
            {
                return false;
            }

            var bytes = File.ReadAllBytes(docsPath);
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes, markNonReadable: false))
            {
                return false;
            }

            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            return true;
        }

        private static Sprite TryLoadBackgroundSpriteFromResources(params string[] resourcePaths)
        {
            if (resourcePaths == null || resourcePaths.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < resourcePaths.Length; i++)
            {
                var path = resourcePaths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }

                var tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                {
                    return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            return null;
        }

        private static Texture2D TryLoadBackgroundTextureFromResources(params string[] resourcePaths)
        {
            if (resourcePaths == null || resourcePaths.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < resourcePaths.Length; i++)
            {
                var path = resourcePaths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                {
                    return tex;
                }

                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null && sprite.texture != null)
                {
                    return sprite.texture;
                }
            }

            return null;
        }

        private static void AttachDropdownItemCompat(GameObject itemGo, RectTransform itemRect, Text itemLabel, Image itemBackground, Toggle itemToggle)
        {
            var dropdownItemType = typeof(Dropdown).GetNestedType("DropdownItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (dropdownItemType == null || !typeof(Component).IsAssignableFrom(dropdownItemType))
            {
                Debug.LogWarning("DropdownItem type was not found. Dropdown template item may not function in this Unity version.");
                return;
            }

            var dropdownItem = itemGo.GetComponent(dropdownItemType) ?? itemGo.AddComponent(dropdownItemType);
            SetDropdownItemMember(dropdownItemType, dropdownItem, "text", itemLabel, "m_Text");
            SetDropdownItemMember(dropdownItemType, dropdownItem, "image", itemBackground, "m_Image");
            SetDropdownItemMember(dropdownItemType, dropdownItem, "toggle", itemToggle, "m_Toggle");
            SetDropdownItemMember(dropdownItemType, dropdownItem, "rectTransform", itemRect, "m_RectTransform");
        }

        private static void SetDropdownItemMember(Type dropdownItemType, Component dropdownItem, string propertyName, object value, string fallbackFieldName)
        {
            var property = dropdownItemType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                property.SetValue(dropdownItem, value, null);
                return;
            }

            var field = dropdownItemType.GetField(fallbackFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(dropdownItem, value);
            }
        }

        private Toggle BuildToggle(string name, RectTransform parent, string label)
        {
            var row = EnsureRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            row.sizeDelta = new Vector2(260f, 32f);

            var bg = EnsureOrGetImage(row.gameObject, new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.65f));
            var toggle = EnsureComponent<Toggle>(row.gameObject);

            var check = EnsureRect("Checkmark", row, new Vector2(0.02f, 0.15f), new Vector2(0.10f, 0.85f), Vector2.zero, Vector2.zero);
            var checkImage = EnsureOrGetImage(check.gameObject, accentColor);

            var text = BuildText("Label", row, label, 15, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, new Vector2(0.14f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            toggle.targetGraphic = bg;
            toggle.graphic = checkImage;
            return toggle;
        }

        private Slider BuildSlider(string name, RectTransform parent)
        {
            var sliderRect = EnsureRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var slider = EnsureComponent<Slider>(sliderRect.gameObject);

            var background = EnsureRect("Background", sliderRect, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(background.gameObject, new Color(0f, 0f, 0f, 0.35f));

            var fillArea = EnsureRect("Fill Area", sliderRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var fill = EnsureRect("Fill", fillArea, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var fillImage = EnsureOrGetImage(fill.gameObject, new Color(0.25f, 0.65f, 0.55f, 1f));

            var handleArea = EnsureRect("Handle Slide Area", sliderRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var handle = EnsureRect("Handle", handleArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-10f, 0f), new Vector2(10f, 0f));
            var handleImage = EnsureOrGetImage(handle.gameObject, new Color(0.90f, 0.95f, 0.92f, 1f));

            slider.targetGraphic = handleImage;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.direction = Slider.Direction.LeftToRight;

            fillImage.raycastTarget = false;
            return slider;
        }

        private static Font GetBuiltInFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
