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
        private const string OptionsUiFactoryPath = "Assets/Scripts/UI/OptionsUiFactory.cs";
        private const string OptionsUiFactoryPagesPath = "Assets/Scripts/UI/OptionsUiFactory.Pages.cs";
        private const string OptionsTabSetPath = "Assets/Scripts/UI/OptionsTabSet.cs";
        private const string GamePalettePath = "Assets/Scripts/UI/GamePalette.cs";
        private const string BossServicePath = "Assets/Scripts/Boss/BossService.cs";
        private const string BoardViewControllerPath = "Assets/Scripts/UI/BoardViewController.cs";
        private const string BossGateViewControllerPath = "Assets/Scripts/UI/BossGateViewController.cs";
        private const string HudViewControllerPath = "Assets/Scripts/UI/HudViewController.cs";
        private const string ShopViewControllerPath = "Assets/Scripts/UI/ShopViewController.cs";
        private const string RewardViewControllerPath = "Assets/Scripts/UI/RewardViewController.cs";
        private const string EndScreenViewControllerPath = "Assets/Scripts/UI/EndScreenViewController.cs";
        private const string EventChoiceScreenControllerPath = "Assets/Scripts/UI/EventChoiceScreenController.cs";
        private const string CustomPuzzleScreenPath = "Assets/Scripts/UI/CustomPuzzleScreen.cs";
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
            Button("ItemsCodex", MainMenuPath, "BtnCodexCurses", "CodexTabId.Curses"),
            Button("ItemsCodex", MainMenuPath, "BtnItemsBack", "mc.BackToMainMenu"),

            Button("ClassSelect", MainMenuPath, "BtnConfirmClass", "mc.BeginRunFromSelect"),
            Button("ClassSelect", MainMenuPath, "BtnClassBack", "mc.BackToMainMenu"),
            Toggle("ClassSelect", MainMenuPath, "TglAllowIrregular", "SetAllowIrregularPuzzles"),

            Button("CustomPuzzle", MainMenuPath, "BtnTutStart", "mc.StartTutorialGame"),
            Button("CustomPuzzle", MainMenuPath, "BtnTutBack", "mc.BackToMainMenu"),

            // [REQ: UI-OPT-001] Options tabs and controls: one factory for the main menu and the run.
            Button("Options", OptionsTabSetPath, "BuildButton(tabName", "Activate(index)"),
            Slider("Options", OptionsUiFactoryPath, "\"MasterVolume\"", "SetMasterVolume"),
            Slider("Options", OptionsUiFactoryPath, "\"MusicVolume\"", "SetMusicVolume"),
            Slider("Options", OptionsUiFactoryPath, "\"SfxVolume\"", "SetSfxVolume"),
            Slider("Options", OptionsUiFactoryPath, "\"UiVolume\"", "SetUiVolume"),
            Toggle("Options", OptionsUiFactoryPath, "\"MuteUnfocused\"", "SetMuteWhenUnfocused"),
            Toggle("Options", OptionsUiFactoryPath, "\"MuteAll\"", "SetMuteAll"),
            Dropdown("Options", OptionsUiFactoryPath, "MenuMusicStyleDropdown", "SetMusicStyleIndex"),
            Toggle("Options", OptionsUiFactoryPath, "\"Fullscreen\"", "SetFullscreen"),
            Toggle("Options", OptionsUiFactoryPath, "\"VSync\"", "SetVSync"),
            Toggle("Options", OptionsUiFactoryPath, "\"ScreenShake\"", "SetScreenShake"),
            Slider("Options", OptionsUiFactoryPath, "\"ParticleIntensity\"", "SetParticleIntensity"),
            Slider("Options", OptionsUiFactoryPath, "\"Brightness\"", "SetBrightness"),
            Slider("Options", OptionsUiFactoryPath, "\"Contrast\"", "SetContrast"),
            Dropdown("Options", OptionsUiFactoryPath, "ResolutionDropdown", "optCtrl.SetResolution"),
            Dropdown("Options", OptionsUiFactoryPath, "DisplayModeDropdown", "optCtrl.SetDisplayMode"),
            Dropdown("Options", OptionsUiFactoryPagesPath, "LanguageDropdown", "langConfirmRow.SetActive"),
            Button("Options", OptionsUiFactoryPath, "BtnLangApply", "optCtrl.SetLanguage"),
            Button("Options", OptionsUiFactoryPath, "BtnLangCancel", "SetValueWithoutNotify"),
            Toggle("Options", OptionsUiFactoryPath, "\"HighlightErrors\"", "SetHighlightConflicts"),
            Toggle("Options", OptionsUiFactoryPath, "\"AutoPencil\"", "SetAutoPencilCleanup"),
            Toggle("Options", OptionsUiFactoryPath, "\"CandidateCount\"", "SetShowCandidateCount"),
            Toggle("Options", OptionsUiFactoryPath, "\"CursorSnap\"", "SetCursorSnapOnSelection"),
            Toggle("Options", OptionsUiFactoryPath, "\"DoubleTap\"", "SetDoubleTapConfirm"),
            Toggle("Options", OptionsUiFactoryPath, "\"ControllerRumble\"", "SetControllerRumbleEnabled"),
            Toggle("Options", OptionsUiFactoryPath, "\"Colorblind\"", "SetColorblindMode"),
            Toggle("Options", OptionsUiFactoryPath, "\"HighContrast\"", "SetHighContrast"),
            Toggle("Options", OptionsUiFactoryPath, "\"ReduceMotion\"", "SetReduceMotion"),
            Toggle("Options", OptionsUiFactoryPath, "\"AltSymbols\"", "SetAltSymbols"),
            Toggle("Options", OptionsUiFactoryPath, "\"ScreenReader\"", "SetScreenReaderEnabled"),
            Button("Options", MainMenuPath, "BtnKeybindings", "mc.OpenKeybindings"),
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
            // [REQ: REQ-ECON-100] GAP-016 buy-pencil HUD button
            Button("InRun", InRunBlueprintPath, "BtnBuyPencil", "BuyPencil", InRunControllerPath),
            // [REQ: REQ-ITEM-100] GAP-015 reward reroll (built at runtime by RewardViewController)
            Button("InRunReward", RewardViewControllerPath, "Slot_reroll", "RerollRewardSlots", RewardViewControllerPath),
            Button("InRunOptions", InRunBlueprintPath, "BtnInGameOptionsClose", "ToggleOptionsPanel", InRunControllerPath),
            Button("InRunOptions", InRunBlueprintPath, "BtnInGameOptionsSaveQuit", "SaveAndQuitPublic"),
            Button("InRunOptions", InRunBlueprintPath, "BtnInGameOptionsModifiers", "ShowModifierList", "Assets/Scripts/UI/InRunController.RunMenu.cs"),
            Button("InRunOptions", InRunBlueprintPath, "BtnInGameOptionsAbandon", "OnAbandonPressed", "Assets/Scripts/UI/InRunController.RunMenu.cs")
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
        [Requirement("UI-STYLE-012")]
        public void CustomPuzzleModes_DisableUnavailableChoicesAndValidateBeforeLaunch()
        {
            var blueprintSource = ReadSource(MainMenuPath);
            var menuSource = ReadSource(MainMenuControllerPath);
            var customSource = ReadSource(TutorialMenuControllerPath);
            var screenSource = ReadSource(CustomPuzzleScreenPath);
            var serviceSource = ReadSource(TutorialModeServicePath);

            // The rule book lists every family's rules; the controller decides what can be chosen.
            StringAssert.Contains("EnsureComponent<CustomPuzzleScreen>(panel).Build(screen, tutCtrl, startBtn)", blueprintSource);
            StringAssert.Contains("foreach (var family in ModifierFamilies.Order)", screenSource);
            StringAssert.Contains("var available = _setup.CanSelect(tile.Id, out _);", screenSource);
            StringAssert.Contains("_rulesMessage.text = _setup.ValidationMessage ?? string.Empty;", screenSource);
            StringAssert.Contains("TutorialModeService.CanAddCustomModifier(_config.BoardSize, _config.SelectedModifiers, id, out reason)", customSource);
            StringAssert.Contains("DropRulesThatNoLongerFit();", customSource);
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
        [Requirement("UI-OPT-001")]
        public void OptionsAudioControls_ShowLivePercentageReadoutsForEveryAudioChannel()
        {
            var factorySource = ReadSource(OptionsUiFactoryPath);

            foreach (var id in new[] { "MasterVolume", "MusicVolume", "SfxVolume", "UiVolume" })
            {
                StringAssert.Contains($"BuildVolumeArrowRow(p + \"{id}\"", factorySource);
            }

            StringAssert.Contains("string FormatPercent(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + \"%\"", factorySource);
            StringAssert.Contains("string FormatVolumeLabel(float value) => label + \" \" + FormatPercent(value)", factorySource);
            StringAssert.Contains("id + \"Val\"", factorySource);
            StringAssert.Contains("lbl.text = FormatVolumeLabel(current)", factorySource);
            StringAssert.Contains("lbl.text = FormatVolumeLabel(v)", factorySource);
        }

        // [REQ: UI-OPT-001] Both Options panels are built by the same factory, so they cannot drift apart (GAP-029).
        [Test]
        [Requirement("UI-OPT-001", "UI-OPT-002")]
        public void OptionsPanels_MainMenuAndInRun_UseTheSharedFactory()
        {
            var mainMenuSource = ReadSource(MainMenuPath);
            var inRunSource = ReadSource(InRunBlueprintPath);

            StringAssert.Contains("OptionsUiFactory.BuildStandardTabs(tabs, optCtrl,", mainMenuSource);
            StringAssert.Contains("OptionsUiFactory.BuildStandardTabs(tabs, optCtrl,", inRunSource);
            StringAssert.Contains("Prefix = \"IG\"", inRunSource);
            // UI-STYLE-017: the Accessibility page is one of the shared pages built by BuildStandardTabs.
            StringAssert.Contains("BuildAccessibilitySection(tabs, accIdx, accSec, optCtrl", ReadSource(OptionsUiFactoryPath));
            StringAssert.Contains("tabs.Add(\"IGTabRun\", \"IGRunSection\"", inRunSource);
            Assert.That(inRunSource, Does.Not.Contain("BuildSlider("), "the in-run Options use the main menu's arrow rows");
        }

        [Test]
        public void CompactActionButtons_UseFullTextureIconsAndWideFit()
        {
            var factorySource = ReadSource(InRunUiFactoryPath);

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
        [Requirement("UI-STYLE-013", "CURSE-ACQUIRE-004", "CURSE-ACQUIRE-005")]
        public void RestScreen_IsATeaTable_WithOneSlipPerBowl()
        {
            var rewardSource = ReadSource(RewardViewControllerPath);

            StringAssert.Contains("ScrollUiKit.Scene(_pathPanel, \"RestChoicePanel\", \"bg_rest\"", rewardSource);
            StringAssert.Contains("ScrollUiKit.TeaBowl()", rewardSource);
            StringAssert.Contains("$\"RestSlip_{index}\"", rewardSource);
            StringAssert.Contains("\"BtnRestDrink\"", rewardSource);
            // Cleanse only while a curse is active; each slip shows the value after the choice.
            StringAssert.Contains("if (curses.Count > 0)", rewardSource);
            StringAssert.Contains("F(\"InRun.Rest.After.Hp\"", rewardSource);
            // Choosing a bowl lifts its slip; choosing it again drinks it, once.
            StringAssert.Contains("if (_selectedRest == index)", rewardSource);
            StringAssert.Contains("if (_restTaken || index < 0 || index >= _restChoices.Count) return;", rewardSource);
            // GAP-056: the scenes' number keys are read in Update before the board-only keyboard handler, and the rest is
            // asked first (the reward view also owns the rest scene).
            var inRunSource = ReadSource(InRunControllerPath);
            Assert.Less(inRunSource.IndexOf("&& TryHandleSceneDigit(k))", StringComparison.Ordinal),
                inRunSource.IndexOf("HandleKeyboard();", StringComparison.Ordinal), "scene keys are read outside HandleKeyboard");
            Assert.Less(inRunSource.IndexOf("_rewardView.SelectRestByIndex(index)", StringComparison.Ordinal),
                inRunSource.IndexOf("_rewardView.SelectRewardByIndex(index)", StringComparison.Ordinal));
        }

        [Test]
        [Requirement("UI-STYLE-014", "UI-STYLE-015", "UI-STYLE-016")]
        public void EventCursedAndRunEnd_AreScenesInTheInkAndPaperStyle()
        {
            var eventSource = ReadSource(EventChoiceScreenControllerPath);
            var gateSource = ReadSource(BossGateViewControllerPath);
            var endSource = ReadSource(EndScreenViewControllerPath);

            // GAP-052: the event is a scene drawn over the path, not the blueprint panel under it.
            StringAssert.Contains("ScrollUiKit.Scene(_pathPanel, \"EventScene\", \"bg_event\"", eventSource);
            StringAssert.Contains("ScrollUiKit.HandScroll(screen.transform, \"EventScroll\"", eventSource);
            StringAssert.Contains("F(\"InRun.Event.NeedGold\", option.GoldCost, state.CurrentGold)", eventSource);
            StringAssert.Contains("ScrollUiKit.Scene(pathPanel, \"CursedChoicePanel\", \"bg_curse\"", gateSource);
            StringAssert.Contains("\"BtnDeclineSkip\"", gateSource);
            StringAssert.Contains("ShowRunEndScroll(new RunEndSummary", endSource);
            StringAssert.Contains("run.State.Mode == GameMode.GardenRun && !run.State.TutorialMode", endSource);
            StringAssert.Contains("UseRunEndScroll(false);", endSource);
        }

        [Test]
        [Requirement("UI-STYLE-007", "REQ-ITEM-100")]
        public void RewardScreen_IsATallyScrollWithEmaPlaques()
        {
            var rewardSource = ReadSource(RewardViewControllerPath);

            // A scene beside the path panel (not inside it), so the path's buttons never draw over it.
            StringAssert.Contains("ScrollUiKit.Scene(_pathPanel, \"RewardPanel\", \"bg_reward\"", rewardSource);
            StringAssert.Contains("ScrollUiKit.HangingScroll(screen, \"RewardTally\"", rewardSource);
            StringAssert.Contains("ScrollUiKit.Ema()", rewardSource);
            StringAssert.Contains("$\"Slot_{index}\"", rewardSource);
            StringAssert.Contains("\"Slot_take\"", rewardSource);
            StringAssert.Contains("\"Slot_skip\"", rewardSource);
            // Choosing a plaque lifts it; choosing it again takes it.
            StringAssert.Contains("if (_selectedReward == index)", rewardSource);
            StringAssert.Contains("ClaimReward(index);", rewardSource);
            StringAssert.Contains("F(\"InRun.Reward.TakeNothing\", nothingGold)", rewardSource);
            // The bag-swap panel opens above the scene.
            StringAssert.Contains("_bagSwapPanel.transform.SetAsLastSibling();", rewardSource);
        }

        [Test]
        [Requirement("UI-STYLE-009")]
        public void ShopScreen_IsTheMarketStall_WithPriceTagsPurseAndSlip()
        {
            var shopSource = ReadSource(ShopViewControllerPath);

            StringAssert.Contains("ScrollUiKit.Scene(_pathPanel, \"ShopPanel\", \"bg_shop\"", shopSource);
            StringAssert.Contains("$\"Offer_{index}\"", shopSource);
            StringAssert.Contains("ScrollUiKit.PriceTag()", shopSource);
            StringAssert.Contains("\"ShopSlip\"", shopSource);
            StringAssert.Contains("\"ShopPurse\"", shopSource);
            StringAssert.Contains("\"ShopBuyBtn\"", shopSource);
            StringAssert.Contains("\"ShopRerollBtn\"", shopSource);
            StringAssert.Contains("\"Offer_skip\"", shopSource);
            // The cost is clear before buying; Buy stays off when the gold is short.
            StringAssert.Contains("F(\"InRun.Shop.GoldAfter\", state.CurrentGold, state.CurrentGold - offer.Price)", shopSource);
            StringAssert.Contains("_shopBuyButton.interactable = item != null && canAfford;", shopSource);
            // Pays and swaps in one step when the bag is full (GAP-036).
            StringAssert.Contains("run.TryPurchaseShopOffer(selectedOfferIdx, slotToReplace)", shopSource);
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

            // UI-STYLE-002: both headers are paper-coloured text with a shadow on the painting, in the same size.
            Assert.That(hudSource, Does.Match("(?s)\"BagTitle\", T\\(\"InRun\\.Hud\\.BagTitle\"\\), 16,\\s*TextAnchor\\.MiddleCenter, InRunUiFactory\\.BagHeaderText\\);\\s*AddHeaderShadow\\(title\\);"));
            Assert.That(hudSource, Does.Match("(?s)\"RelicTitle\", T\\(\"InRun\\.Hud\\.RelicsTitle\"\\), 16,\\s*TextAnchor\\.MiddleCenter, InRunUiFactory\\.BagHeaderText\\);\\s*AddHeaderShadow\\(relicTitle\\);"));
            StringAssert.Contains("relicTitle.rectTransform.anchorMin = new Vector2(0.02f, 0.41f)", hudSource);
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

            // UI-STYLE-002: the hint is light text with a dark shadow at the foot of the right column.
            StringAssert.Contains("SudokuGameplayHintBg", inRunSource);
            StringAssert.Contains("new Vector2(0.7716f, 0.02f), new Vector2(0.9710f, 0.105f)", inRunSource);
            StringAssert.Contains("SetRect(hint.rectTransform, new Vector2(0.7716f, 0.02f), new Vector2(0.9710f, 0.105f)", inRunSource);
            StringAssert.Contains("hint.color = new Color(0.90f, 0.86f, 0.76f, 0.95f)", inRunSource);
            StringAssert.Contains("hint.gameObject.AddComponent<Shadow>()", inRunSource);
            StringAssert.Contains("hintShadow.effectColor = new Color(0f, 0f, 0f, 0.65f)", inRunSource);
        }

        [Test]
        public void SudokuPuzzleScreen_SitsOnAHangingScroll_WithoutDarkBoxes()
        {
            var factorySource = ReadSource(InRunUiFactoryPath);
            var inRunSource = ReadSource(InRunBlueprintPath);
            var hudSource = ReadSource(HudViewControllerPath);

            // UI-STYLE-002: the board hangs on a scroll, the rules are paper slips, the bag holds wooden plaques;
            // the dark translucent boxes of the old layout are gone.
            StringAssert.Contains("ScrollUiKit.HangingScroll(panel.transform, \"PuzzleScroll\"", inRunSource);
            StringAssert.Contains("EnsureOrGetImage(gridRoot.gameObject, ScrollUiKit.Ink)", inRunSource);
            StringAssert.Contains("EnsureOrGetImage(numpadRoot.gameObject, new Color(0f, 0f, 0f, 0f))", inRunSource);
            StringAssert.Contains("EnsureOrGetImage(hintBg.gameObject, new Color(0f, 0f, 0f, 0f))", inRunSource);
            StringAssert.Contains("ScrollUiKit.MakeLantern(bgImg, fillImg, shade)", inRunSource);

            StringAssert.Contains("internal static readonly Color BagSlotBg            = new Color(0.80f, 0.60f, 0.38f, 0.96f)", factorySource);
            StringAssert.Contains("internal static readonly Color BagSlotEmptyBg", factorySource);
            Assert.That(factorySource, Does.Not.Contain("SudokuPuzzleBoxBg"), "the shared dark box colour is retired");

            StringAssert.Contains("ScrollUiKit.MakeSlip(_modifierInfoBox.GetComponent<Image>()", hudSource);
            StringAssert.Contains("_floorModBanner.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f)", hudSource);
            StringAssert.Contains("bagGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f)", hudSource);
            StringAssert.Contains("!hasItem ? InRunUiFactory.BagSlotEmptyBg", hudSource);
        }

        [Test]
        public void SudokuPassiveText_UsesCompactPanelWithAdaptiveFontSize()
        {
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("new GameObject(\"PassiveLabel\", typeof(RectTransform), typeof(Image))", hudSource);
            StringAssert.Contains("rt.anchorMax = new Vector2(0.25f, 0.095f)", hudSource);
            StringAssert.Contains("\"PassiveText\", \"\", 15", hudSource);
            StringAssert.Contains("_passiveText.resizeTextForBestFit = true", hudSource);
            StringAssert.Contains("_passiveText.resizeTextMinSize = 10", hudSource);
            StringAssert.Contains("_passiveText.resizeTextMaxSize = 15", hudSource);
            StringAssert.Contains("_passiveText.rectTransform.offsetMin = new Vector2(6f, 2f)", hudSource);
            StringAssert.Contains("_passiveText.verticalOverflow = VerticalWrapMode.Truncate", hudSource);
        }

        [Test]
        public void SudokuTimers_AreShadowedTextUnderThePortrait_WithoutBackgroundBox()
        {
            var hudSource = ReadSource(HudViewControllerPath);

            StringAssert.Contains("\"PuzzleTimer\"", hudSource);
            StringAssert.Contains("\"RunTimer\"", hudSource);
            StringAssert.Contains("\"\", 18, TextAnchor.MiddleLeft", hudSource);
            StringAssert.Contains("\"\", 15, TextAnchor.MiddleLeft", hudSource);
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
                "Timers should remain visible as shadowed text without a grey background box.");
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
        [Requirement("UI-STYLE-001")]
        public void PathOverviewSaveQuitButton_IsARoundInkSaveButton_WithTwoStepConfirm()
        {
            var inRunSource = ReadSource(InRunControllerPath);

            StringAssert.Contains("ScrollUiKit.RoundButton(panelRt, \"BtnSaveQuit\"", inRunSource);
            StringAssert.Contains("\"ui/icon_ink_save\"", inRunSource);
            StringAssert.Contains("T(\"InRun.Path.ConfirmQuit\")", inRunSource);
            StringAssert.Contains("saveQuit.onClick.AddListener(SaveAndQuit); // Custom Puzzle: one click, no confirm step", inRunSource);
            StringAssert.Contains("ScrollUiKit.RoundButton(panelRt, \"BtnPathOptions\"", inRunSource);
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
        // The banner was deliberately retired in 0826fec ("Custom Puzzle mode does not need a tutorial
        // overlay"); the test now guards that the whole backing stays hidden (GAP-010 triage).
        public void TutorialBannerPanel_KeepsWholeTopRightBackingHidden()
        {
            var blueprintSource = ReadSource(InRunBlueprintPath);
            var bannerSource = ReadSource(TutorialRunBannerControllerPath);

            StringAssert.Contains("var tutorialBanner = EnsureComponent<TutorialRunBannerController>(root.gameObject)", blueprintSource);
            StringAssert.Contains("tutorialBanner.Configure(runMapController, tutorialLabel)", blueprintSource);
            StringAssert.Contains("tutorialLabel.transform.parent.gameObject.SetActive(false)", blueprintSource);
            StringAssert.Contains("private GameObject _bannerPanel", bannerSource);
            StringAssert.Contains("_bannerPanel = _bannerText != null ? _bannerText.transform.parent?.gameObject : null", bannerSource);
            StringAssert.Contains("_bannerPanel.SetActive(false)", bannerSource);
            StringAssert.Contains("_bannerText.gameObject.SetActive(false)", bannerSource);
        }

        [Test]
        public void InGameOptionsPanel_UsesOpaqueFullscreenBackground()
        {
            var inRunSource = ReadSource(InRunBlueprintPath);

            StringAssert.Contains("EnsureRect(\"InGameOptionsPanel\", root, Vector2.zero, Vector2.one", inRunSource);
            StringAssert.Contains("new Color(0.04f, 0.035f, 0.03f, 1f)", inRunSource);
            // A rebuild clears the old background, card and scroll parts first (UI-STYLE-017).
            StringAssert.Contains("new[] { \"PanelBackground\", \"Content\", \"OptionsBanner\", \"OptionsScroll\"", inRunSource);
            StringAssert.Contains("ScrollUiKit.DestroyChildrenNow(panel.transform, part)", inRunSource);
            Assert.That(
                inRunSource,
                Does.Not.Contain("AddPanelBackground(panel.transform, \"bg_menu_pause\""),
                "In-puzzle Options must not use a transparent pause background over gameplay.");
        }

        [Test]
        [Requirement("UI-STYLE-001")]
        public void PathStats_AreAPaperTag_AndRouteLanesAreGone()
        {
            var inRunSource = ReadSource(InRunControllerPath);

            StringAssert.Contains("ScrollUiKit.Fill(panelRt, \"PathStatsBar\"", inRunSource);
            StringAssert.Contains("\"PathStatsText\"", inRunSource);
            StringAssert.Contains("ScrollUiKit.HandScroll(panelRt, \"PathScroll\"", inRunSource);
            Assert.That(inRunSource, Does.Not.Contain("DrawRouteLane("), "The scroll has no lane boxes; the route names are in the bottom line.");
            Assert.That(inRunSource, Does.Not.Contain("\"LaneTip\""), "Lane tips were parented to the panel and piled up on every rebuild.");
        }

        [Test]
        [Requirement("REQ-BOSS-107", "REQ-UI-107", "UI-STYLE-005")]
        public void BossDefeatedScreen_IsItsOwnViewBesideTheGameOverPanel_WithStatsAndOfferings()
        {
            var endSource = ReadSource(EndScreenViewControllerPath);

            // A sibling of the game-over panel, never the panel itself: an open game-over panel makes Save & Quit
            // clear the run (GAP-046).
            StringAssert.Contains("var root = _gameOverPanel != null ? _gameOverPanel.transform.parent : transform;", endSource);
            StringAssert.Contains("ScrollUiKit.PaperTag(screen, \"BossClearedStats\"", endSource);
            StringAssert.Contains("ScrollUiKit.Offering(screen.transform, i, relic, center, T(\"InRun.BossDefeated.Take\"))", endSource);
            StringAssert.Contains("\"BtnLeaveOfferings\"", endSource);
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
        [Requirement("UI-STYLE-008")]
        public void RelicShrine_UsesTheOfferingTable_AndLegendaryArtLoadsCompletely()
        {
            var rewardSource = ReadSource(RewardViewControllerPath);
            var kitSource = ReadSource("Assets/Scripts/UI/ScrollUiKit.cs");
            var factorySource = ReadSource(InRunUiFactoryPath);

            StringAssert.Contains("ScrollUiKit.Scene(_pathPanel, \"RelicChoicePanel\", \"bg_relic\"", rewardSource);
            StringAssert.Contains("ScrollUiKit.Offering(screen.transform, i, relic, centre, T(\"InRun.BossDefeated.Take\"))", rewardSource);
            StringAssert.Contains("\"RelicShrineRun\"", rewardSource);
            StringAssert.Contains("\"RelicShrineHeld\"", rewardSource);
            StringAssert.Contains("if (_relicChosen) return;", rewardSource);
            StringAssert.Contains("internal static Button Offering(Transform screen, int index, RelicInstance relic, float center, string takeLabel)", kitSource);
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

            StringAssert.Contains("CageOutline.Build(cage.Cells, _boardSize, cW, cH, sX, sY, inset)", boardSource);
            StringAssert.Contains("CageOutline.Dashes(length, dash, gap)", boardSource);
            StringAssert.Contains("const float dash = 8f", boardSource);
            StringAssert.Contains("txt.fontStyle = FontStyle.Bold", boardSource);
            StringAssert.Contains("KillerBorder = new(0.94f, 0.26f, 0.22f, 1.00f)", paletteSource);
        }

        [Test]
        [Requirement("REQ-BOSS-107", "REQ-UI-107")]
        public void VictoryScreen_ClearsBossInterstitialAndCommitsFinalBossRewards()
        {
            var endSource = ReadSource(EndScreenViewControllerPath);
            var runSource = ReadSource(InRunControllerPath);

            StringAssert.Contains("ClearBossClearedDynamicContent()", endSource);
            StringAssert.Contains("UnityEngine.Object.Destroy(_bossDefeatedScreen)", endSource);
            StringAssert.Contains("label.text = T(\"Back\")", endSource);
            StringAssert.Contains("_map.TryClaimCurrentPuzzleRewards(out _, out _, buildItemSlots: false)", runSource);
            StringAssert.Contains("run.GetAnalytics()?.RecordRunComplete()", runSource);
            Assert.That(runSource, Does.Not.Contain("ShowBossCleared(_map.Run, onClaim, \"Claim Reward"));
        }

        [Test]
        [Requirement("UI-STYLE-018")]
        public void ItemsCodex_ProvidesRelicRuleAndCurseTabsWithIcons()
        {
            var menuSource = ReadSource(MainMenuPath);
            var itemsSource = ReadSource(ItemsMenuControllerPath);

            StringAssert.Contains("BtnCodexRelics", menuSource);
            StringAssert.Contains("BtnCodexModifiers", menuSource);
            StringAssert.Contains("BtnCodexCurses", menuSource);
            StringAssert.Contains("itemsCtrl.SetTabButtons(tabItems, tabRelics, tabModifiers, tabCurses)", menuSource);
            StringAssert.Contains("itemsCtrl.ShowTab(CodexTabId.Relics)", menuSource);
            StringAssert.Contains("itemsCtrl.ShowTab(CodexTabId.Modes)", menuSource);
            StringAssert.Contains("itemsCtrl.ShowTab(CodexTabId.Curses)", menuSource);
            StringAssert.Contains("BossService.GetIconName(id)", itemsSource);
            StringAssert.Contains("BossService.GetIconFolder(id)", itemsSource);
            // Icons load through the scroll kit (which uses InRunUiFactory.LoadResourceSprite).
            StringAssert.Contains("ScrollUiKit.Icon(", itemsSource);
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
        public void RenbanLines_RenderPinkToMatchRuleText()
        {
            var paletteSource = ReadSource(GamePalettePath);
            // The rule text comes from BossService; the Custom Puzzle tooltip shows the same text (GAP-048).
            var bossSource = ReadSource(BossServicePath);
            var customSource = ReadSource(CustomPuzzleScreenPath);

            StringAssert.Contains("LineRenbanLine      = new(0.93f, 0.50f, 0.76f, 1f); // pink", paletteSource);
            StringAssert.Contains("Cells on pink lines form a consecutive set", bossSource);
            StringAssert.Contains("BossService.GetModifierDescription(id)", customSource);
            Assert.That(customSource, Does.Not.Contain("Orange line: all digits on the line form a consecutive set"));
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

            // UI-STYLE-002 scroll header, top to bottom: eyebrow 0.899–0.925, floor name 0.853–0.897, status 0.826–0.852.
            StringAssert.Contains("SetRect(levelInfo.rectTransform, new Vector2(0.3005f, 0.899f), new Vector2(0.6995f, 0.925f)", inRunSource);
            StringAssert.Contains("new Vector2(0.3005f, 0.853f), new Vector2(0.6995f, 0.897f)", inRunSource);
            StringAssert.Contains("SetRect(statusText.rectTransform, new Vector2(0.3005f, 0.826f), new Vector2(0.6995f, 0.852f)", inRunSource);
            // Floor modifiers and the combo share one lane under the board; a combo hides the floor line.
            StringAssert.Contains("rt.anchorMin = new Vector2(0.3105f, 0.135f)", hudSource);
            StringAssert.Contains("rt.anchorMax = new Vector2(0.6895f, 0.175f)", hudSource);
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

                // Factory-wired controls: the action is passed in the same builder call
                // (e.g. BuildMenuButton(card, "BtnStart", ..., mc.StartGame)), and the builder
                // registers it with onClick.AddListener. A nearby AddListener is not required.
                var statementEnd = source.IndexOf(';', index);
                if (statementEnd < 0) continue;
                var statementStart = source.LastIndexOf('\n', index) + 1;
                var statement = source.Substring(statementStart, statementEnd - statementStart);
                if (statement.Contains(control.ActionToken)
                    && GetBuilderTokens(control.ControlType).Any(statement.Contains))
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
                    // Runtime panels create their buttons through InRunUiFactory.
                    return new[] { "BuildButton", "BuildMenuButton", "CreateActionButton", "CreatePanelButton", "RoundButton", "RimButton", "PillButton",
                        "BuildBackPill", "RunButton" };
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
            return SourceText.ReadClass(path);
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
