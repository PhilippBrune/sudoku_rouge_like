using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class GeneratedUiWiringContractTests
    {
        private const string MainMenuPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.cs";
        private const string MainMenuControllerPath = "Assets/Scripts/UI/MainMenuController.cs";
        private const string TutorialMenuControllerPath = "Assets/Scripts/UI/TutorialMenuController.cs";
        private const string TutorialModeServicePath = "Assets/Scripts/Tutorial/TutorialModeService.cs";
        private const string ChallengePanelsPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.ChallengePanels.cs";
        private const string InRunBlueprintPath = "Assets/Scripts/UI/InRunUiBlueprintBuilder.cs";
        private const string InRunControllerPath = "Assets/Scripts/UI/InRunController.cs";
        private const string RunMapControllerPath = "Assets/Scripts/UI/RunMapController.cs";
        private const string InRunUiFactoryPath = "Assets/Scripts/UI/InRunUiFactory.cs";
        private const string GamePalettePath = "Assets/Scripts/UI/GamePalette.cs";
        private const string BoardViewControllerPath = "Assets/Scripts/UI/BoardViewController.cs";
        private const string BossGateViewControllerPath = "Assets/Scripts/UI/BossGateViewController.cs";
        private const string HudViewControllerPath = "Assets/Scripts/UI/HudViewController.cs";
        private const string ShopViewControllerPath = "Assets/Scripts/UI/ShopViewController.cs";
        private const string RewardViewControllerPath = "Assets/Scripts/UI/RewardViewController.cs";
        private const string EndScreenViewControllerPath = "Assets/Scripts/UI/EndScreenViewController.cs";
        private const string ItemsMenuControllerPath = "Assets/Scripts/UI/ItemsMenuController.cs";
        private const string TutorialRunBannerControllerPath = "Assets/Scripts/UI/TutorialRunBannerController.cs";

        private static readonly SourceControlExpectation[] RequiredControls =
        {
            Button("MainMenu", MainMenuPath, "BtnStart", "mc.StartGame"),
            Button("MainMenu", MainMenuPath, "BtnResume", "mc.ResumeGame"),
            Button("MainMenu", MainMenuPath, "BtnMeta", "mc.OpenMetaProgression"),
            Button("MainMenu", MainMenuPath, "BtnModes", "mc.OpenGameModes"),
            Button("MainMenu", MainMenuPath, "BtnItems", "mc.OpenItems"),
            Button("MainMenu", MainMenuPath, "BtnTutorial", "mc.OpenTutorial"),
            Button("MainMenu", MainMenuPath, "BtnProfiles", "mc.ShowProfileSelect"),
            Button("MainMenu", MainMenuPath, "BtnOptions", "mc.OpenOptions"),
            Button("MainMenu", MainMenuPath, "BtnCredits", "mc.OpenCredits"),
            Button("MainMenu", MainMenuPath, "BtnQuit", "mc.ExitGame"),
            Button("ItemsCodex", MainMenuPath, "BtnCodexItems", "CodexTabId.Items"),
            Button("ItemsCodex", MainMenuPath, "BtnCodexRelics", "CodexTabId.Relics"),
            Button("ItemsCodex", MainMenuPath, "BtnCodexModifiers", "CodexTabId.Modes"),
            Button("ItemsCodex", MainMenuPath, "BtnItemsBack", "mc.BackToMainMenu"),

            Button("ClassSelect", MainMenuPath, "BtnConfirmClass", "mc.ShowStartRunModal"),
            Button("ClassSelect", MainMenuPath, "BtnClassBack", "mc.BackToMainMenu"),
            Toggle("ClassSelect", MainMenuPath, "TglAllowIrregular", "SetAllowIrregularPuzzles"),

            Button("CustomPuzzle", MainMenuPath, "BtnTutStart", "mc.StartTutorialGame"),
            Button("CustomPuzzle", MainMenuPath, "BtnTutBack", "mc.BackToMainMenu"),

            Button("Options", MainMenuPath, "TabAudio", "ActivateTab(0)"),
            Button("Options", MainMenuPath, "TabDisplay", "ActivateTab(1)"),
            Button("Options", MainMenuPath, "TabGame", "ActivateTab(2)"),
            Dropdown("Options", MainMenuPath, "MenuMusicStyleDropdown", "SetMusicStyleIndex"),
            Dropdown("Options", MainMenuPath, "ResolutionDropdown", "optCtrl.SetResolution"),
            Dropdown("Options", MainMenuPath, "LanguageDropdown", "langConfirmRow.SetActive"),
            Button("Options", MainMenuPath, "BtnLangApply", "optCtrl.SetLanguage"),
            Button("Options", MainMenuPath, "BtnLangCancel", "SetValueWithoutNotify"),
            Button("Options", MainMenuPath, "BtnKeybindings", "mc.OpenKeybindings"),
            Button("Options", MainMenuPath, "BtnAccessibility", "mc.OpenAccessibility"),
            Button("Options", MainMenuPath, "BtnOptBack", "mc.BackToMainMenu"),
            Button("Options", MainMenuPath, "BtnReplayBasicsTutorial", "mc.LaunchSudokuBasicsTutorial"),

            Button("Credits", MainMenuPath, "BtnCredBack", "mc.BackToMainMenu"),
            Button("DailyWalk", ChallengePanelsPath, "BtnDailyBack", "mc.ShowGameModes"),
            Button("MonthlyWalk", ChallengePanelsPath, "BtnBeginMonthly", "mc.LaunchSeasonalChallenge"),
            Button("MonthlyWalk", ChallengePanelsPath, "BtnMonthlyBack", "mc.ShowGameModes"),
            Button("SpiritTrials", MainMenuPath, "BtnTierStart_", "mc.StartSpiritTrials"),
            Button("SpiritTrials", MainMenuPath, "BtnTrialsSelectBack", "mc.BackFromPanel"),
            Button("EndlessZen", MainMenuPath, "BtnZenStart", "mc.StartEndlessZen"),
            Button("EndlessZen", MainMenuPath, "BtnZenLeaderboardBack", "mc.BackFromPanel"),
            Button("ProfileSelect", MainMenuPath, "BtnProfileBack", "mc.BackToMainMenu"),
            Button("ProfileSelect", MainMenuPath, "BtnLoad_", "mc.SelectProfileSlot"),
            Button("ProfileSelect", MainMenuPath, "BtnDelete_", "mc.DeleteProfileSlot"),

            Button("InRun", InRunBlueprintPath, "BtnSudokuSaveQuit", "SaveAndQuit", InRunControllerPath),
            Button("InRun", InRunBlueprintPath, "BtnSudokuOptions", "ToggleOptionsPanel", InRunControllerPath),
            Button("InRunOptions", InRunBlueprintPath, "BtnInGameOptionsClose", "ToggleOptionsPanel", InRunControllerPath),
            Toggle("InRunOptions", InRunBlueprintPath, "IGMuteWhenUnfocusedToggle", "SetMuteWhenUnfocused"),
            Slider("InRunOptions", InRunBlueprintPath, "IGMasterSlider", "SetMasterVolume"),
            Slider("InRunOptions", InRunBlueprintPath, "IGMusicSlider", "SetMusicVolume"),
            Slider("InRunOptions", InRunBlueprintPath, "IGSfxSlider", "SetSfxVolume"),
            Slider("InRunOptions", InRunBlueprintPath, "IGUiSlider", "SetUiVolume"),
            Toggle("InRunOptions", InRunBlueprintPath, "IGFullscreenToggle", "SetFullscreen"),
            Toggle("InRunOptions", InRunBlueprintPath, "IGHighlightToggle", "SetHighlightConflicts"),
            Toggle("InRunOptions", InRunBlueprintPath, "IGColorblindToggle", "SetColorblind"),
            Toggle("InRunOptions", InRunBlueprintPath, "IGHighContrastToggle", "SetHighContrast"),
            Toggle("InRunOptions", InRunBlueprintPath, "IGReduceMotionToggle", "SetReduceMotion"),
            Toggle("InRunOptions", InRunBlueprintPath, "IGAltSymbolsToggle", "SetAltSymbols")
        };

        [Test]
        public void RequiredGeneratedControls_AreDeclaredInBuilderSources()
        {
            var missing = RequiredControls
                .Where(control => !HasCreationEvidence(control))
                .Select(control => $"{control.CreationPath}: missing {control.ControlType} {control.ScreenId}/{control.ControlName}")
                .ToArray();

            Assert.IsEmpty(missing);
        }

        [Test]
        public void RequiredGeneratedControls_HaveWiringEvidence()
        {
            var missing = RequiredControls
                .Where(control => !HasWiringEvidence(control))
                .Select(control =>
                    $"{control.WiringPath}: missing wiring token '{control.ActionToken}' for {control.ScreenId}/{control.ControlName}")
                .ToArray();

            Assert.IsEmpty(missing);
        }

        [Test]
        public void CustomPuzzleModes_DisableUnavailableChoicesAndValidateBeforeLaunch()
        {
            var blueprintSource = ReadSource(MainMenuPath);
            var menuSource = ReadSource(MainMenuControllerPath);
            var customSource = ReadSource(TutorialMenuControllerPath);
            var serviceSource = ReadSource(TutorialModeServicePath);

            StringAssert.Contains("T(\"Sudoku Modes (max 5)\")", blueprintSource);
            StringAssert.Contains("new[] { \"5x5\", \"6x6\", \"7x7\", \"8x8\", \"9x9\" }", blueprintSource);
            StringAssert.Contains("CustomSelectionSummaryBg", blueprintSource);
            StringAssert.Contains("ModTooltipBg", blueprintSource);
            StringAssert.Contains("tutCtrl.SetSelectionSummaryText(selectionSummary)", blueprintSource);
            StringAssert.Contains("SetSelectionSummaryText(Text text)", customSource);
            StringAssert.Contains("RefreshModifierAvailability()", customSource);
            StringAssert.Contains("toggle.interactable = canSelect", customSource);
            StringAssert.Contains("CustomModifierLimit = 5", serviceSource);
            StringAssert.Contains("TryValidateCustomSetup(setup", menuSource);
        }

        [Test]
        public void OptionsAndDebugControls_UseDistinctMusicFallbacksAndScopedDebugHotkeys()
        {
            var blueprintSource = ReadSource(MainMenuPath);
            var musicSource = ReadSource("Assets/Scripts/UI/MenuMusicController.cs");
            var previewSource = ReadSource("Assets/Scripts/UI/AccessibilityPreviewController.cs");
            var optionsSource = ReadSource("Assets/Scripts/UI/OptionsController.cs");
            var hotkeysSource = ReadSource("Assets/Scripts/Bootstrap/DebugHotkeys.cs");
            var bootstrapSource = ReadSource("Assets/Scripts/Bootstrap/GameBootstrap.cs");
            var menuSource = ReadSource(MainMenuControllerPath);

            StringAssert.Contains("() => BuildFallbackLoop(normalized)", musicSource);
            StringAssert.Contains("ProceduralSfxLibrary.BuildPuzzleLoop()", musicSource);
            StringAssert.Contains("ProceduralSfxLibrary.BuildRestLoop()", musicSource);
            StringAssert.DoesNotContain("lbl.fontSize = 20", blueprintSource);
            StringAssert.Contains("UpdateScale(acc?.FontScale ?? 1f)", previewSource);
            StringAssert.Contains("Screen reader captions enabled.", optionsSource);
            StringAssert.Contains("RuntimeDebugEnabled", hotkeysSource);
            StringAssert.Contains("if (!RuntimeDebugEnabled) return;", hotkeysSource);
            StringAssert.Contains("#if UNITY_EDITOR || DEVELOPMENT_BUILD", bootstrapSource);
            StringAssert.Contains("DebugHotkeys.SetRuntimeDebugEnabled(isOn)", menuSource);
        }

        [Test]
        public void OptionsAudioControls_ShowLivePercentageReadoutsForEveryAudioChannel()
        {
            var mainMenuSource = ReadSource(MainMenuPath);
            var inRunSource = ReadSource(InRunBlueprintPath);

            foreach (var id in new[] { "MasterVolume", "MusicVolume", "SfxVolume", "UiVolume" })
            {
                StringAssert.Contains($"BuildVolumeArrowRow(\"{id}\"", mainMenuSource);
            }

            StringAssert.Contains("string FormatPercent(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + \"%\"", mainMenuSource);
            StringAssert.Contains("string FormatVolumeLabel(float value) => label + \" \" + FormatPercent(value)", mainMenuSource);
            StringAssert.Contains("id + \"Val\"", mainMenuSource);
            StringAssert.Contains("lbl.text = FormatVolumeLabel(current)", mainMenuSource);
            StringAssert.Contains("lbl.text = FormatVolumeLabel(v)", mainMenuSource);

            foreach (var id in new[] { "IGMasterPct", "IGMusicPct", "IGSfxPct", "IGUiPct" })
            {
                StringAssert.Contains(id, inRunSource);
            }

            StringAssert.Contains("string FormatAudioLabel(string label, float value) => label + \" \" + FormatPercent(value)", inRunSource);
            StringAssert.Contains("FormatAudioLabel(masterText, opts.Audio.MasterVolume)", inRunSource);
            StringAssert.Contains("masterLabel.text = FormatAudioLabel(masterText, v)", inRunSource);
            StringAssert.Contains("musicLabel.text = FormatAudioLabel(musicText, v)", inRunSource);
            StringAssert.Contains("sfxLabel.text = FormatAudioLabel(sfxText, v)", inRunSource);
            StringAssert.Contains("uiLabel.text = FormatAudioLabel(uiText, v)", inRunSource);
            StringAssert.Contains("IGUiSlider", inRunSource);
            StringAssert.Contains("optCtrl.SetUiVolume", inRunSource);
        }

        [Test]
        public void ShopAndRestActionButtons_UseFullTextureIconsAndWideCompactFit()
        {
            var shopSource = ReadSource(ShopViewControllerPath);
            var rewardSource = ReadSource(RewardViewControllerPath);
            var factorySource = ReadSource(InRunUiFactoryPath);

            StringAssert.Contains("UiAction.Buy, compact: true", shopSource);
            StringAssert.Contains("UiAction.Reroll, compact: true", shopSource);
            StringAssert.Contains("BtnReroll", rewardSource);
            StringAssert.Contains("UiAction.Reroll, compact: true", rewardSource);
            StringAssert.Contains("LoadResourceSprite(spritePath)", factorySource);
            StringAssert.Contains("Resources.Load<Texture2D>(spritePath)", factorySource);
            StringAssert.Contains("Sprite.Create(", factorySource);
            StringAssert.Contains("Resources.LoadAll<Sprite>(spritePath)", factorySource);
            StringAssert.Contains("if (compact)", factorySource);
            StringAssert.Contains("ApplyWideCompactActionIconFit(btn)", factorySource);
            StringAssert.Contains("label.rectTransform.anchorMin = new Vector2(0.28f, 0.05f)", factorySource);
            StringAssert.Contains("iconRt.anchorMin = new Vector2(0.035f, 0.10f)", factorySource);
            StringAssert.Contains("iconRt.anchorMax = new Vector2(0.245f, 0.90f)", factorySource);
            StringAssert.Contains("img.preserveAspect = true", factorySource);
        }

        [Test]
        public void RestPanel_UsesLocalLargerActionLabelText()
        {
            var rewardSource = ReadSource(RewardViewControllerPath);
            var factorySource = ReadSource(InRunUiFactoryPath);

            StringAssert.Contains("CreateText(panel.transform, \"Title\", T(\"InRun.Rest.Title\"), 22", rewardSource);
            StringAssert.Contains("ConfigureRestActionLabel(btnHeal)", rewardSource);
            StringAssert.Contains("ConfigureRestActionLabel(btnPencil)", rewardSource);
            StringAssert.Contains("ConfigureRestActionLabel(btnReroll)", rewardSource);
            StringAssert.Contains("ConfigureRestActionLabel(btnCleanse)", rewardSource);
            StringAssert.Contains("private static void ConfigureRestActionLabel(Button button)", rewardSource);
            StringAssert.Contains("label.fontSize = 18", rewardSource);
            StringAssert.Contains("label.resizeTextForBestFit = true", rewardSource);
            StringAssert.Contains("label.resizeTextMinSize = 14", rewardSource);
            StringAssert.Contains("label.resizeTextMaxSize = 18", rewardSource);
            StringAssert.Contains("label.horizontalOverflow = HorizontalWrapMode.Overflow", rewardSource);
            Assert.That(
                factorySource,
                Does.Not.Contain("resizeTextMaxSize = 18"),
                "Rest readability must be applied locally in RewardViewController, not by changing shared compact action buttons.");
        }

        [Test]
        public void RewardScreen_UsesExpandedFourColumnLayoutAndReadableTitle()
        {
            var rewardSource = ReadSource(RewardViewControllerPath);

            StringAssert.Contains("panelRt.anchorMin = new Vector2(0.06f, 0.12f)", rewardSource);
            StringAssert.Contains("panelRt.anchorMax = new Vector2(0.94f, 0.84f)", rewardSource);
            StringAssert.Contains("title.color = new Color(0.08f, 0.11f, 0.07f, 1f)", rewardSource);
            StringAssert.Contains("title.fontStyle = FontStyle.Bold", rewardSource);
            StringAssert.Contains("title.gameObject.AddComponent<Outline>()", rewardSource);
            StringAssert.Contains("titleOutline.effectColor = new Color(GamePalette.TextPrimary.r", rewardSource);
            StringAssert.Contains("Mathf.Clamp(_rewardSlots.Count, 1, 4)", rewardSource);
            StringAssert.Contains("const float left = 0.04f", rewardSource);
            StringAssert.Contains("const float right = 0.96f", rewardSource);
            StringAssert.Contains("new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.16f)", rewardSource);
            Assert.That(rewardSource, Does.Not.Contain("Mathf.Clamp(_rewardSlots.Count, 1, 3)"));
        }

        [Test]
        public void BagEmptySlots_UseSlicedSpriteFallbackAndCompactEmptyIconLayout()
        {
            var hudSource = ReadSource(HudViewControllerPath);
            var factorySource = ReadSource(InRunUiFactoryPath);

            StringAssert.Contains("internal static Sprite LoadResourceSprite(string spritePath)", factorySource);
            StringAssert.Contains("Resources.LoadAll<Sprite>(spritePath)", factorySource);
            StringAssert.Contains("InRunUiFactory.LoadResourceSprite(path)", hudSource);
            StringAssert.Contains("GetFullTextureBagIcon(\"economy/icon_empty_slot\")", hudSource);
            StringAssert.Contains("Resources.Load<Texture2D>(path)", hudSource);
            StringAssert.Contains("Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height)", hudSource);
            StringAssert.Contains("SetBagItemIconLayout(iconImg, true)", hudSource);
            StringAssert.Contains("rt.anchorMin = new Vector2(0.05f, 0.16f)", hudSource);
            StringAssert.Contains("rt.anchorMax = new Vector2(0.31f, 0.84f)", hudSource);
            StringAssert.Contains("SetBagItemIconLayout(iconImg, false)", hudSource);
        }

        [Test]
        public void SudokuBagAndRelicsHeaders_UseMatchingFontSizeAndColor()
        {
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("\"BagTitle\", T(\"InRun.Hud.BagTitle\"), 13", hudSource);
            StringAssert.Contains("\"RelicTitle\", T(\"InRun.Hud.RelicsTitle\"), 13", hudSource);
            Assert.That(hudSource, Does.Match("(?s)\"BagTitle\", T\\(\"InRun\\.Hud\\.BagTitle\"\\), 13,\\s*TextAnchor\\.MiddleCenter, InRunUiFactory\\.AccentGold\\);"));
            Assert.That(hudSource, Does.Match("(?s)\"RelicTitle\", T\\(\"InRun\\.Hud\\.RelicsTitle\"\\), 13,\\s*TextAnchor\\.MiddleCenter, InRunUiFactory\\.AccentGold\\);"));
            StringAssert.Contains("relicTitle.rectTransform.anchorMin = new Vector2(0.02f, 0.43f)", hudSource);
        }

        [Test]
        public void SudokuRelicSlots_AreSquareUsingCurrentWidthAsHeight()
        {
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("var go = new GameObject($\"RelicSlot_{i}\", typeof(RectTransform), typeof(Image), typeof(Button))", hudSource);
            StringAssert.Contains("var aspect = go.AddComponent<AspectRatioFitter>()", hudSource);
            StringAssert.Contains("aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight", hudSource);
            StringAssert.Contains("aspect.aspectRatio = 1f", hudSource);
        }

        [Test]
        public void SudokuClassBadge_UsesSharedHpPencilScrimWithoutOwnGreyBacking()
        {
            var hudSource = ReadSource(HudViewControllerPath);
            var inRunSource = ReadSource(InRunBlueprintPath);

            StringAssert.Contains("HpPencilScrim", inRunSource);
            StringAssert.Contains("new GameObject(\"ClassBadge\", typeof(RectTransform))", hudSource);
            Assert.That(
                hudSource,
                Does.Not.Contain("new GameObject(\"ClassBadge\", typeof(RectTransform), typeof(Image))"),
                "Class badge must not add a second grey background over HpPencilScrim.");
        }

        [Test]
        public void SudokuHpAndPencilBars_UseIndependentDirtyCachesFromTextLabels()
        {
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("_cachedHpBarHp = -1, _cachedHpBarMax = -1", hudSource);
            StringAssert.Contains("_cachedPencilBarPencil = -1, _cachedPencilBarMax = -1", hudSource);
            StringAssert.Contains("state.CurrentHP != _cachedHpBarHp || state.MaxHP != _cachedHpBarMax", hudSource);
            StringAssert.Contains("_cachedHpBarHp = state.CurrentHP", hudSource);
            StringAssert.Contains("_hpBarFill.fillAmount = state.MaxHP > 0 ? Mathf.Clamp01(state.CurrentHP / (float)state.MaxHP) : 0", hudSource);
            StringAssert.Contains("state.CurrentPencil != _cachedPencilBarPencil || state.MaxPencil != _cachedPencilBarMax", hudSource);
            StringAssert.Contains("_cachedPencilBarPencil = state.CurrentPencil", hudSource);
        }

        [Test]
        public void SudokuPuzzleHint_UsesReadableContrastTreatment()
        {
            var inRunSource = ReadSource(InRunBlueprintPath);

            StringAssert.Contains("SudokuGameplayHintBg", inRunSource);
            StringAssert.Contains("new Vector2(0.74f, 0.725f), new Vector2(0.93f, 0.845f)", inRunSource);
            StringAssert.Contains("SetRect(hint.rectTransform, new Vector2(0.745f, 0.73f), new Vector2(0.925f, 0.84f)", inRunSource);
            StringAssert.Contains("EnsureOrGetImage(hintBg.gameObject, InRunUiFactory.SudokuPuzzleBoxBg)", inRunSource);
            StringAssert.Contains("hint.color = GamePalette.TextPrimary", inRunSource);
            StringAssert.Contains("hint.gameObject.AddComponent<Shadow>()", inRunSource);
            StringAssert.Contains("hintShadow.effectColor = new Color(0f, 0f, 0f, 0.65f)", inRunSource);
        }

        [Test]
        public void SudokuPuzzleBoxes_UseSharedColorAndTransparency()
        {
            var factorySource = ReadSource(InRunUiFactoryPath);
            var inRunSource = ReadSource(InRunBlueprintPath);
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("internal static readonly Color SudokuPuzzleBoxBg = new Color(0.04f, 0.05f, 0.08f, 0.50f)", factorySource);
            StringAssert.Contains("internal static readonly Color ModBannerBg      = SudokuPuzzleBoxBg", factorySource);
            StringAssert.Contains("internal static readonly Color ModInfoBoxBg     = SudokuPuzzleBoxBg", factorySource);
            StringAssert.Contains("internal static readonly Color BagPanelBg          = SudokuPuzzleBoxBg", factorySource);
            StringAssert.Contains("internal static readonly Color BagSlotBg            = SudokuPuzzleBoxBg", factorySource);

            StringAssert.Contains("private static readonly Color ScrimColor = InRunUiFactory.SudokuPuzzleBoxBg", inRunSource);
            StringAssert.Contains("EnsureOrGetImage(gridRoot.gameObject, InRunUiFactory.SudokuPuzzleBoxBg)", inRunSource);
            StringAssert.Contains("EnsureOrGetImage(numpadRoot.gameObject, InRunUiFactory.SudokuPuzzleBoxBg)", inRunSource);
            StringAssert.Contains("EnsureOrGetImage(hintBg.gameObject, InRunUiFactory.SudokuPuzzleBoxBg)", inRunSource);

            StringAssert.Contains("_floorModBanner.GetComponent<Image>().color = InRunUiFactory.ModBannerBg", hudSource);
            StringAssert.Contains("_modifierInfoBox.GetComponent<Image>().color = InRunUiFactory.ModInfoBoxBg", hudSource);
            StringAssert.Contains("bagGo.GetComponent<Image>().color = InRunUiFactory.BagPanelBg", hudSource);
            StringAssert.Contains("go.GetComponent<Image>().color = InRunUiFactory.SudokuPuzzleBoxBg", hudSource);
        }

        [Test]
        public void SudokuPassiveText_UsesCompactPanelWithAdaptiveFontSize()
        {
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("new GameObject(\"PassiveLabel\", typeof(RectTransform), typeof(Image))", hudSource);
            StringAssert.Contains("rt.anchorMax = new Vector2(0.21f, 0.09f)", hudSource);
            StringAssert.Contains("go.GetComponent<Image>().color = InRunUiFactory.SudokuPuzzleBoxBg", hudSource);
            StringAssert.Contains("\"PassiveText\", \"\", 11", hudSource);
            StringAssert.Contains("_passiveText.resizeTextForBestFit = true", hudSource);
            StringAssert.Contains("_passiveText.resizeTextMinSize = 8", hudSource);
            StringAssert.Contains("_passiveText.resizeTextMaxSize = 11", hudSource);
            StringAssert.Contains("_passiveText.rectTransform.offsetMin = new Vector2(6f, 2f)", hudSource);
            StringAssert.Contains("_passiveText.verticalOverflow = VerticalWrapMode.Truncate", hudSource);
        }

        [Test]
        public void SudokuTimers_UseTextOnlyTopRightLabelsWithoutLargeBackgroundBox()
        {
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("\"PuzzleTimer\"", hudSource);
            StringAssert.Contains("\"RunTimer\"", hudSource);
            StringAssert.Contains("\"\", 15, TextAnchor.MiddleRight", hudSource);
            StringAssert.Contains("\"\", 12, TextAnchor.MiddleRight", hudSource);
            StringAssert.Contains("_timerText.fontStyle = FontStyle.Bold", hudSource);
            StringAssert.Contains("_runTimerText.fontStyle = FontStyle.Bold", hudSource);
            StringAssert.Contains("gameObject.AddComponent<Shadow>()", hudSource);
            StringAssert.Contains("shadow.effectColor = new Color(0f, 0f, 0f, 0.92f)", hudSource);
            StringAssert.Contains("gameObject.AddComponent<Outline>()", hudSource);
            StringAssert.Contains("outline.effectColor = new Color(0f, 0f, 0f, 0.90f)", hudSource);
            StringAssert.Contains("private void RemoveLegacyTimerBacking()", hudSource);
            StringAssert.Contains("_sudokuPanel.transform.Find(\"TimerBg\")", hudSource);
            StringAssert.Contains("RemoveLegacyTimerBacking();", hudSource);
            StringAssert.Contains("Destroy(timerBg.gameObject)", hudSource);
            Assert.That(
                hudSource,
                Does.Not.Contain("new GameObject(\"TimerBg\""),
                "Timers should remain visible as shadowed text without the large top-right grey background box.");
        }

        [Test]
        public void PuzzleNodeGeneration_UsesAsyncPathAndRollbackInsteadOfSynchronousStart()
        {
            var mapSource = ReadSource(RunMapControllerPath);
            var inRunSource = ReadSource(InRunControllerPath);

            StringAssert.Contains("_run.StartLevelAsync(nextLevel)", mapSource);
            StringAssert.Contains("StorePendingGenerationRollback(rollback)", mapSource);
            StringAssert.Contains("RollbackPendingGeneration()", mapSource);
            Assert.That(mapSource, Does.Not.Contain("_run.StartLevel(nextLevel)"));
            Assert.That(inRunSource, Does.Not.Contain("_map.Run.StartLevel(cfg)"));
            StringAssert.Contains("_map.StartPuzzleAsync(cfg)", inRunSource);
            StringAssert.Contains("Creating puzzle...", inRunSource);
        }

        [Test]
        public void PathOverviewSaveQuitButton_UsesInkSaveIcon()
        {
            var inRunSource = ReadSource(InRunControllerPath);

            StringAssert.Contains("new GameObject(\"BtnSaveQuit\"", inRunSource);
            StringAssert.Contains("InRunUiFactory.LoadResourceSprite(\"ui/icon_ink_save\")", inRunSource);
            StringAssert.Contains("new GameObject(\"Icon\", typeof(RectTransform), typeof(Image))", inRunSource);
            StringAssert.Contains("sqIconImg.preserveAspect = true", inRunSource);
            StringAssert.Contains("sqLbl.rectTransform.anchorMin = new Vector2(0.25f, 0f)", inRunSource);
        }

        [Test]
        public void PuzzlePreBake_IsBoundedAndExceptionSafe()
        {
            var mapSource = ReadSource(RunMapControllerPath);
            var runSource = ReadSource("Assets/Scripts/Run/RunDirector.cs");

            StringAssert.Contains("new SemaphoreSlim(2, 2)", mapSource);
            StringAssert.Contains("QueuePreBake(", mapSource);
            StringAssert.Contains("_preBakeGate.Wait(token)", mapSource);
            StringAssert.Contains("CancelPreBakeForInteractiveGeneration()", mapSource);
            StringAssert.Contains("catch (Exception ex)", mapSource);
            StringAssert.Contains("BakeBossBoard(LevelConfig config, CancellationToken cancel = default)", runSource);
            StringAssert.Contains("CreatePuzzleForConfigWithBudgetedRetries(", runSource);
            StringAssert.Contains("BakeNodePuzzle: board generation failed", runSource);
        }

        [Test]
        public void TutorialBannerPanel_HidesWholeTopRightBackingOutsideTutorialRuns()
        {
            var blueprintSource = ReadSource(InRunBlueprintPath);
            var bannerSource = ReadSource(TutorialRunBannerControllerPath);

            StringAssert.Contains("var tutorialBanner = EnsureComponent<TutorialRunBannerController>(root.gameObject)", blueprintSource);
            StringAssert.Contains("tutorialBanner.Configure(runMapController, tutorialLabel)", blueprintSource);
            StringAssert.Contains("tutorialLabel.transform.parent.gameObject.SetActive(false)", blueprintSource);
            StringAssert.Contains("private GameObject _bannerPanel", bannerSource);
            StringAssert.Contains("_bannerPanel = _bannerText != null ? _bannerText.transform.parent?.gameObject : null", bannerSource);
            StringAssert.Contains("_bannerPanel.SetActive(isTutorial)", bannerSource);
            StringAssert.Contains("_bannerText.gameObject.SetActive(isTutorial)", bannerSource);
        }

        [Test]
        public void InGameOptionsPanel_UsesOpaqueFullscreenBackground()
        {
            var inRunSource = ReadSource(InRunBlueprintPath);

            StringAssert.Contains("EnsureRect(\"InGameOptionsPanel\", root, Vector2.zero, Vector2.one", inRunSource);
            StringAssert.Contains("new Color(0.055f, 0.07f, 0.065f, 1f)", inRunSource);
            StringAssert.Contains("InRunUiFactory.ClearNamedChildren(panel.transform, \"PanelBackground\")", inRunSource);
            Assert.That(
                inRunSource,
                Does.Not.Contain("AddPanelBackground(panel.transform, \"bg_menu_pause\""),
                "In-puzzle Options must not use a transparent pause background over gameplay.");
        }

        [Test]
        public void PathStatsBarKeepsBacking_AndRouteLanesDoNotDrawLargeFilledBoxes()
        {
            var inRunSource = ReadSource(InRunControllerPath);

            StringAssert.Contains("new GameObject(\"PathStatsBar\", typeof(RectTransform), typeof(Image))", inRunSource);
            StringAssert.Contains("statsBarImg.color = new Color(0.04f, 0.05f, 0.08f, 0.50f)", inRunSource);
            StringAssert.Contains("statsBarImg.raycastTarget = false", inRunSource);
            StringAssert.Contains("statsText.rectTransform.anchorMax = new Vector2(0.62f, 1f)", inRunSource);
            StringAssert.Contains("statsText.gameObject.AddComponent<Shadow>()", inRunSource);
            StringAssert.Contains("new GameObject(goName, typeof(RectTransform))", inRunSource);
            Assert.That(
                inRunSource,
                Does.Not.Contain("RemoveLegacyPathStatsBacking"),
                "Path stats backing should not be stripped from the HUD row.");
            Assert.That(
                inRunSource,
                Does.Not.Contain("new GameObject(goName, typeof(RectTransform), typeof(Image))"),
                "Route lane containers should not draw large filled grey rectangles into the empty top-right path area.");
        }

        [Test]
        public void BossClearedRewardScreen_HidesDetailsScrimAndCentersStats()
        {
            var endSource = ReadSource(EndScreenViewControllerPath);

            StringAssert.Contains("SetGameOverDetailsScrimActive(false)", endSource);
            StringAssert.Contains("statsRt.anchorMin = new Vector2(0.30f, 0.36f)", endSource);
            StringAssert.Contains("statsRt.anchorMax = new Vector2(0.70f, 0.58f)", endSource);
            StringAssert.Contains("statsTxt.fontSize = 22", endSource);
            StringAssert.Contains("statsGo.AddComponent<Shadow>()", endSource);
            StringAssert.Contains("SetGameOverDetailsScrimActive(true)", endSource);
        }

        [Test]
        public void ItemAndRelicIcons_DoNotAddTopRightColorOverlays()
        {
            var factorySource = ReadSource(InRunUiFactoryPath);

            StringAssert.Contains("internal static Image AddRarityPip", factorySource);
            StringAssert.Contains("ClearNamedChildren(itemIconTransform, \"RarityPip\")", factorySource);
            StringAssert.Contains("internal static Image AddRelicTierPip", factorySource);
            StringAssert.Contains("ClearNamedChildren(relicIconTransform, \"RelicTier\")", factorySource);
            Assert.That(factorySource, Does.Not.Contain("new GameObject(\"RarityPip\""));
            Assert.That(factorySource, Does.Not.Contain("new GameObject(\"RelicTierPip\""));
            Assert.That(factorySource, Does.Not.Contain("new GameObject(\"RelicTierFrame\""));
        }

        [Test]
        public void RelicChoiceCards_RenderCompleteLegendaryTexturesAndKeepIconSpacing()
        {
            var rewardSource = ReadSource(RewardViewControllerPath);
            var factorySource = ReadSource(InRunUiFactoryPath);

            StringAssert.Contains("ConfigureRelicChoiceButtonLayout(btn)", rewardSource);
            StringAssert.Contains("label.rectTransform.anchorMin = new Vector2(0.08f, 0.10f)", rewardSource);
            StringAssert.Contains("label.rectTransform.anchorMax = new Vector2(0.92f, 0.35f)", rewardSource);
            StringAssert.Contains("iconRt.anchorMin = new Vector2(0.16f, 0.43f)", rewardSource);
            StringAssert.Contains("iconRt.anchorMax = new Vector2(0.84f, 0.86f)", rewardSource);
            StringAssert.Contains("ShouldUseFullTextureComposition(spritePath, sprites.Length)", factorySource);
            StringAssert.Contains("spriteCount > 1", factorySource);
            StringAssert.Contains("spritePath.StartsWith(\"legendary/\", System.StringComparison.OrdinalIgnoreCase)", factorySource);

            var loadAllIndex = factorySource.IndexOf("Resources.LoadAll<Sprite>(spritePath)", StringComparison.Ordinal);
            var textureIndex = factorySource.IndexOf("Resources.Load<Texture2D>(spritePath)", StringComparison.Ordinal);
            Assert.That(loadAllIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(textureIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(loadAllIndex, Is.LessThan(textureIndex),
                "Imported sprites are inspected first, then composite and legendary art can use full textures.");
        }

        [Test]
        public void KillerCages_UseDashedRedOutlinesAndReadableSums()
        {
            var boardSource = ReadSource(BoardViewControllerPath);
            var paletteSource = ReadSource(GamePalettePath);

            StringAssert.Contains("DrawDashedCageHorizontal", boardSource);
            StringAssert.Contains("DrawDashedCageVertical", boardSource);
            StringAssert.Contains("const float dash = 8f", boardSource);
            StringAssert.Contains("txt.fontStyle = FontStyle.Bold", boardSource);
            StringAssert.Contains("KillerBorder = new(0.94f, 0.26f, 0.22f, 1.00f)", paletteSource);
        }

        [Test]
        public void VictoryScreen_ClearsBossInterstitialAndCommitsFinalBossRewards()
        {
            var endSource = ReadSource(EndScreenViewControllerPath);
            var runSource = ReadSource(InRunControllerPath);

            StringAssert.Contains("ClearBossClearedDynamicContent()", endSource);
            StringAssert.Contains("RemoveDynamicChild(\"BossClearedContinueBtn\")", endSource);
            StringAssert.Contains("label.text = T(\"Back\")", endSource);
            StringAssert.Contains("_map.TryClaimCurrentPuzzleRewards(out _, out _, buildItemSlots: false)", runSource);
            StringAssert.Contains("run.GetAnalytics()?.RecordRunComplete()", runSource);
            Assert.That(runSource, Does.Not.Contain("ShowBossCleared(_map.Run, onClaim, \"Claim Reward"));
        }

        [Test]
        public void ItemsCodex_ProvidesRelicAndBossModifierTabsWithIcons()
        {
            var menuSource = ReadSource(MainMenuPath);
            var itemsSource = ReadSource(ItemsMenuControllerPath);

            StringAssert.Contains("BtnCodexRelics", menuSource);
            StringAssert.Contains("BtnCodexModifiers", menuSource);
            StringAssert.Contains("itemsCtrl.SetTabButtons(tabItems, tabRelics, tabModifiers)", menuSource);
            StringAssert.Contains("itemsCtrl.ShowTab(CodexTabId.Relics)", menuSource);
            StringAssert.Contains("itemsCtrl.ShowTab(CodexTabId.Modes)", menuSource);
            StringAssert.Contains("BossService.GetIconName(id)", itemsSource);
            StringAssert.Contains("BossService.GetIconFolder(id)", itemsSource);
            StringAssert.Contains("InRunUiFactory.LoadResourceSprite", itemsSource);
        }

        [Test]
        public void BossGateModifierButtons_DoNotAddColorGroupStripes()
        {
            var bossGateSource = ReadSource(BossGateViewControllerPath);
            var factorySource = ReadSource(InRunUiFactoryPath);

            StringAssert.Contains("internal static void AddModifierGroupMarker", factorySource);
            Assert.That(
                bossGateSource,
                Does.Not.Contain("AddModifierGroupMarker"),
                "Boss gate modifier cards should show icons/text only; no colored group stripe overlays.");
        }

        [Test]
        public void RenbanLines_RenderOrangeToMatchRuleText()
        {
            var paletteSource = ReadSource(GamePalettePath);
            var factorySource = ReadSource(InRunUiFactoryPath);
            var mainMenuSource = ReadSource(MainMenuPath);

            StringAssert.Contains("LineRenbanLine      = new(0.95f, 0.52f, 0.12f, 0.75f)", paletteSource);
            StringAssert.Contains("Orange line: all digits on the line form a consecutive set", factorySource);
            StringAssert.Contains("Orange line: all digits on the line form a consecutive set", mainMenuSource);
            Assert.That(factorySource, Does.Not.Contain("Pink line: all digits on the line form a consecutive set"));
            Assert.That(mainMenuSource, Does.Not.Contain("Pink line: all digits on the line form a consecutive set"));
        }

        [Test]
        public void GameOverDetails_UseReadableScrimAndShadow()
        {
            var inRunSource = ReadSource(InRunBlueprintPath);

            StringAssert.Contains("GameOverDetailsScrim", inRunSource);
            StringAssert.Contains("new Color(0.03f, 0.025f, 0.02f, 0.68f)", inRunSource);
            StringAssert.Contains("detailsText.color = InRunUiFactory.WarmIvory", inRunSource);
            StringAssert.Contains("detailsShadow.effectColor = new Color(0f, 0f, 0f, 0.85f)", inRunSource);
            StringAssert.Contains("titleShadow.effectColor = new Color(0f, 0f, 0f, 0.80f)", inRunSource);
        }

        [Test]
        public void SudokuTopCenterInfoBoxes_UseNonOverlappingVerticalLanes()
        {
            var hudSource = ReadSource(HudViewControllerPath);
            var inRunSource = ReadSource(InRunBlueprintPath);

            StringAssert.Contains("SetRect(levelInfo.rectTransform, new Vector2(0.22f, 0.83f), new Vector2(0.70f, 0.875f)", inRunSource);
            StringAssert.Contains("SetRect(statusText.rectTransform, new Vector2(0.22f, 0.79f), new Vector2(0.70f, 0.83f)", inRunSource);
            StringAssert.Contains("rt.anchorMin = new Vector2(0.24f, 0.885f)", hudSource);
            StringAssert.Contains("rt.anchorMax = new Vector2(0.68f, 0.920f)", hudSource);
            StringAssert.Contains("private GameObject _comboBox", hudSource);
            StringAssert.Contains("var go = new GameObject(\"ComboCounter\", typeof(RectTransform), typeof(Image))", hudSource);
            StringAssert.Contains("_comboBox = go", hudSource);
            StringAssert.Contains("if (_comboBox != null) _comboBox.SetActive(true)", hudSource);
            StringAssert.Contains("if (_comboBox != null) _comboBox.SetActive(false)", hudSource);
            StringAssert.Contains("if (_floorModBanner != null) _floorModBanner.SetActive(false)", hudSource);
            StringAssert.Contains("isActiveAndEnabled && gameObject.activeInHierarchy", hudSource);
        }

        private static SourceControlExpectation Button(
            string screenId,
            string creationPath,
            string controlName,
            string actionToken,
            string wiringPath = null)
        {
            return new SourceControlExpectation(
                screenId,
                controlName,
                GeneratedUiControlType.Button,
                creationPath,
                wiringPath ?? creationPath,
                actionToken);
        }

        private static SourceControlExpectation Toggle(
            string screenId,
            string creationPath,
            string controlName,
            string actionToken)
        {
            return new SourceControlExpectation(
                screenId,
                controlName,
                GeneratedUiControlType.Toggle,
                creationPath,
                creationPath,
                actionToken);
        }

        private static SourceControlExpectation Dropdown(
            string screenId,
            string creationPath,
            string controlName,
            string actionToken)
        {
            return new SourceControlExpectation(
                screenId,
                controlName,
                GeneratedUiControlType.Dropdown,
                creationPath,
                creationPath,
                actionToken);
        }

        private static SourceControlExpectation Slider(
            string screenId,
            string creationPath,
            string controlName,
            string actionToken)
        {
            return new SourceControlExpectation(
                screenId,
                controlName,
                GeneratedUiControlType.Slider,
                creationPath,
                creationPath,
                actionToken);
        }

        private static bool HasCreationEvidence(SourceControlExpectation control)
        {
            var source = ReadSource(control.CreationPath);
            foreach (var index in AllIndexesOf(source, control.ControlName))
            {
                var window = WindowAround(source, index, 220, 520);
                if (GetBuilderTokens(control.ControlType).Any(token => window.Contains(token)))
                    return true;
            }

            return false;
        }

        private static bool HasWiringEvidence(SourceControlExpectation control)
        {
            var source = ReadSource(control.WiringPath);
            foreach (var index in AllIndexesOf(source, control.ControlName))
            {
                var window = WindowAround(source, index, 350, 3500);
                if (window.Contains(control.ActionToken) &&
                    (window.Contains("AddListener") || window.Contains("onValueChanged")))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<string> GetBuilderTokens(GeneratedUiControlType controlType)
        {
            switch (controlType)
            {
                case GeneratedUiControlType.Button:
                    return new[] { "BuildButton", "BuildMenuButton" };
                case GeneratedUiControlType.Toggle:
                    return new[] { "BuildToggle", "BuildOptionToggle" };
                case GeneratedUiControlType.Dropdown:
                    return new[] { "BuildDropdown" };
                case GeneratedUiControlType.Slider:
                    return new[] { "BuildSlider", "BuildVolumeArrowRow" };
                default:
                    return Array.Empty<string>();
            }
        }

        private static string ReadSource(string path)
        {
            return File.ReadAllText(path);
        }

        private static IEnumerable<int> AllIndexesOf(string source, string token)
        {
            var index = 0;
            while (index < source.Length)
            {
                index = source.IndexOf(token, index, StringComparison.Ordinal);
                if (index < 0)
                    yield break;

                yield return index;
                index += token.Length;
            }
        }

        private static string WindowAround(string source, int index, int before, int after)
        {
            var start = Math.Max(0, index - before);
            var end = Math.Min(source.Length, index + after);
            return source.Substring(start, end - start);
        }

        private readonly struct SourceControlExpectation
        {
            public SourceControlExpectation(
                string screenId,
                string controlName,
                GeneratedUiControlType controlType,
                string creationPath,
                string wiringPath,
                string actionToken)
            {
                ScreenId = screenId;
                ControlName = controlName;
                ControlType = controlType;
                CreationPath = creationPath;
                WiringPath = wiringPath;
                ActionToken = actionToken;
            }

            public string ScreenId { get; }
            public string ControlName { get; }
            public GeneratedUiControlType ControlType { get; }
            public string CreationPath { get; }
            public string WiringPath { get; }
            public string ActionToken { get; }
        }
    }
}
