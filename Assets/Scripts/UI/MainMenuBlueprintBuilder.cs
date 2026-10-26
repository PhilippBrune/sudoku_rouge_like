using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    public sealed partial class MainMenuBlueprintBuilder : MonoBehaviour
    {
        [SerializeField] private bool autoBuildOnStart = true;

        // Shared with the in-run Options (OptionsUiStyle, UI-OPT-001).
        private static readonly Color BgColor      = OptionsUiStyle.Backdrop;
        private static readonly Color PanelColor   = OptionsUiStyle.Card;
        private static readonly Color BtnColor     = OptionsUiStyle.Button;
        private static readonly Color BtnTextColor = OptionsUiStyle.ButtonText;
        private static readonly Color AccentColor  = OptionsUiStyle.Accent;
        private static readonly Color TxtColor     = OptionsUiStyle.Text;
        // - Over-background colours (used when a panel has an art background image) -
        private static readonly Color BgPanelOverlay = new(0.06f, 0.08f, 0.06f, 0.88f);
        private static readonly Color BgBtnNormal    = new(0.08f, 0.06f, 0.04f, 0.78f);  // same dark charcoal for art-bg panels
        private static readonly Color BgBtnText      = new(0.98f, 0.83f, 0.26f, 1f);    // gold
        private static readonly Color TextPillColor    = new(1.00f, 0.97f, 0.93f, 0.15f);
        private static readonly Color ContentScrimColor = new(0f, 0f, 0f, 0.60f);

        private bool _built;
        private OptionsController _slotChangedOptionsController;
        private Action _keybindingSlotChangedHandler;
        private Action _optionsSlotChangedHandler;

        public int SlotChangedHandlerRegistrationCount =>
            (_keybindingSlotChangedHandler != null ? 1 : 0) +
            (_optionsSlotChangedHandler != null ? 1 : 0);

        private static string T(string key) => LocalizationService.T(key);

        private void Start()
        {
            if (!autoBuildOnStart || !Application.isPlaying || _built) return;
            Build();
            _built = true;
        }

        public void ForceRebuild()
        {
            _built = false;
            Build();
        }

        private void OnDestroy()
        {
            ClearSlotChangedHandlers();
        }

        [ContextMenu("Build Main Menu")]
        public void Build()
        {
            if (Application.isPlaying && _built) return;

            ClearSlotChangedHandlers();

            var mc          = EnsureComponent<MainMenuController>(gameObject);
            var optCtrl     = EnsureComponent<OptionsController>(gameObject);
            var tutCtrl     = EnsureComponent<TutorialMenuController>(gameObject);
            var metaCtrl    = EnsureComponent<MetaProgressionPanelController>(gameObject);
            var modesCtrl   = EnsureComponent<GameModesPanelController>(gameObject);
            var itemsCtrl   = EnsureComponent<ItemsMenuController>(gameObject);
            var keybindCtrl = EnsureComponent<KeybindingsPanelController>(gameObject);
            var bootstrap   = FindAnyObjectByType<GameBootstrap>();

            EnsureEventSystem();
            var canvas = EnsureCanvas();
            var root = EnsureRect("MainMenuRoot", canvas.transform as RectTransform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // A rebuild (e.g. after a language change) keeps the menu's current visibility; only the
            // first build starts invisible for the splash fade-in.
            var previousRootCg = root.GetComponent<CanvasGroup>();
            var previousAlpha = previousRootCg != null ? previousRootCg.alpha : 0f;
            ClearGeneratedRoot(root);
            EnsureOrGetImage(root.gameObject, BgColor);
            // A ? CanvasGroup for splash-to-menu fade-in (starts invisible; GameBootstrap fades it in)
            // Note: ?? bypasses Unity's null operator override ? use EnsureComponent instead
            var rootCg = EnsureComponent<CanvasGroup>(root.gameObject);
            rootCg.alpha = previousAlpha;

            // -- Title --------------------------------------------------------
            var titleText = BuildText("TitleText", root, "Run of the Nine", 42, TextAnchor.MiddleCenter);
            SetRect(titleText.rectTransform,
                new Vector2(0.15f, 0.88f), new Vector2(0.85f, 0.96f), Vector2.zero, Vector2.zero);
            titleText.fontStyle = FontStyle.BoldAndItalic;
            titleText.color = TxtColor;
            var titleShadow = EnsureComponent<Shadow>(titleText.gameObject);
            titleShadow.effectColor = GamePalette.ShadowDark;
            titleShadow.effectDistance = new Vector2(2f, -2f);

            var subtitleText = BuildText("SubtitleText", root, T("MainMenu.Subtitle"), 18, TextAnchor.MiddleCenter);
            SetRect(subtitleText.rectTransform,
                new Vector2(0.25f, 0.845f), new Vector2(0.75f, 0.885f), Vector2.zero, Vector2.zero);
            subtitleText.color = GamePalette.WithAlpha(TxtColor, 0.65f);

            // -- Menu Card ------------------------------------------------------
            var card = EnsureRect("MenuCard", root,
                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.84f),
                Vector2.zero, Vector2.zero);
            // 3 ? Stone tablet card: visible background + subtle gold border when no art bg
            EnsureOrGetImage(card.gameObject, new Color(0.12f, 0.14f, 0.11f, 0.85f));
            var cardOutline = EnsureComponent<Outline>(card.gameObject);
            cardOutline.effectColor = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.28f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // -- Main Menu Buttons ----------------------------------------------
            // Primary group (Start / Resume) ? taller and gold-accented
            var btnStart    = BuildMenuButton(card, "BtnStart",    T("Start Game"),       new Vector2(0.06f, 0.81f), new Vector2(0.94f, 0.89f), mc.StartGame);
            var btnResume   = BuildMenuButton(card, "BtnResume",   T("Resume Game"),      new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.80f), mc.ResumeGame);

            // Style primary buttons: larger text, gold label
            StylePrimaryMenuButton(btnStart);
            StylePrimaryMenuButton(btnResume);

            // Thin separator line between primary and secondary groups
            var sepGo = new GameObject("MenuGroupSeparator", typeof(RectTransform), typeof(Image));
            sepGo.transform.SetParent(card, false);
            var sepRt = sepGo.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0.06f, 0.705f);
            sepRt.anchorMax = new Vector2(0.94f, 0.707f);
            sepRt.offsetMin = sepRt.offsetMax = Vector2.zero;
            sepGo.GetComponent<Image>().color = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.30f);

            // Secondary group ? same height, slightly muted (M-1: Meta ? Modes ? Items ? Custom Puzzle)
            var btnMeta     = BuildMenuButton(card, "BtnMeta",     T("Meta Progression"), new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.70f), mc.OpenMetaProgression);
            var btnModes    = BuildMenuButton(card, "BtnModes",    T("Game Modes"),       new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.62f), mc.OpenGameModes);
            var btnItems    = BuildMenuButton(card, "BtnItems",    T("Codex.Title"), new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.54f), mc.OpenItems);
            var btnTut      = BuildMenuButton(card, "BtnTutorial", T("Custom Puzzle"),   new Vector2(0.06f, 0.39f), new Vector2(0.94f, 0.46f), mc.OpenTutorial);

            // Thin separator before system group
            var sepGo2 = new GameObject("MenuGroupSeparator2", typeof(RectTransform), typeof(Image));
            sepGo2.transform.SetParent(card, false);
            var sepRt2 = sepGo2.GetComponent<RectTransform>();
            sepRt2.anchorMin = new Vector2(0.06f, 0.375f);
            sepRt2.anchorMax = new Vector2(0.94f, 0.377f);
            sepRt2.offsetMin = sepRt2.offsetMax = Vector2.zero;
            sepGo2.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

            // System group ? visually muted (smaller text, lower alpha buttons handled via StyleSecondaryMenuButton)
            // GAP-029: all four system buttons share one height (0.06).
            var btnProfiles = BuildMenuButton(card, "BtnProfiles", T("Profiles"),        new Vector2(0.06f, 0.31f), new Vector2(0.94f, 0.37f), mc.ShowProfileSelect);
            var btnOpt      = BuildMenuButton(card, "BtnOptions",  T("Options"),          new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.30f), mc.OpenOptions);
            var btnCred     = BuildMenuButton(card, "BtnCredits",  T("Credits"),          new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.23f), mc.OpenCredits);
            var btnQuit     = BuildMenuButton(card, "BtnQuit",     T("Quit"),             new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.16f), mc.ExitGame);

            StyleSecondaryMenuButton(btnProfiles);
            StyleSecondaryMenuButton(btnOpt);
            StyleSecondaryMenuButton(btnCred);
            StyleSecondaryMenuButton(btnQuit);

            // MM-1: explicit vertical navigation chain for main-menu buttons
            var mainMenuButtons = new Button[] { btnStart, btnResume, btnMeta, btnModes, btnItems, btnTut, btnProfiles, btnOpt, btnCred, btnQuit };
            for (var i = 0; i < mainMenuButtons.Length; i++)
            {
                var nav = mainMenuButtons[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp   = mainMenuButtons[(i + mainMenuButtons.Length - 1) % mainMenuButtons.Length];
                nav.selectOnDown = mainMenuButtons[(i + 1) % mainMenuButtons.Length];
                mainMenuButtons[i].navigation = nav;
            }
            BuildHomeWidgets(card, mc, mainMenuButtons); // SPEC-CONF-027: Daily/Monthly widgets

            ApplyMenuButtonIcon(btnStart,    "ui/icon_start_game");               // start: dedicated torii gate icon
            ApplyMenuButtonIcon(btnResume,   "ui/icon_resume_scroll");             // F18: dedicated resume icon
            ApplyMenuButtonIcon(btnTut,      "ui/icon_tutorial_bamboo");           // F18: dedicated tutorial/custom-puzzle icon
            ApplyMenuButtonIcon(btnMeta,     "ui/icon_golden_bloom");
            ApplyMenuButtonIcon(btnModes,    "ui/icon_modes_lantern");             // F18: dedicated game-modes icon
            ApplyMenuButtonIcon(btnItems,    "ui/icon_items_codex");               // F18: dedicated items-codex icon
            ApplyMenuButtonIcon(btnProfiles, "ui/icon_profiles_mask");             // F18/F13: profiles icon; scroll_stamp reserved for Confirm
            ApplyMenuButtonIcon(btnOpt,      "ui/icon_stone_gear");
            ApplyMenuButtonIcon(btnCred,     "ui/icon_credits_scroll");               // F-QA: dedicated credits icon replaces language_scroll
            ApplyMenuButtonIcon(btnQuit,     "ui/icon_gate_close");                // F17: dedicated quit/exit icon

            // Apply art background to main-menu card if the asset is available
            var rootBgImg = EnsureOrGetImage(root.gameObject, BgColor);
            var rootBgApplied = ApplyPanelBackground(rootBgImg, "background/bg_main_menu");
            if (rootBgApplied)
            {
                // Art background overrides the stone tablet ? revert card to transparent
                EnsureOrGetImage(card.gameObject, Color.clear);
                cardOutline.effectColor = Color.clear;
                titleText.color    = new Color(0.06f, 0.04f, 0.02f, 1f);
                subtitleText.color = new Color(0.06f, 0.04f, 0.02f, 1f);
                AddTextPill(titleText.rectTransform);
                AddTextPill(subtitleText.rectTransform);
                foreach (var btn in new Button[] { btnStart, btnResume, btnTut, btnMeta, btnModes, btnItems, btnProfiles, btnOpt, btnCred, btnQuit })
                    StyleButtonForBackground(btn);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug toggle (hidden in release builds)
            var debug = BuildToggle("DebugToggle", card, T("Enable All (Debug)"));
            SetRect(debug.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.01f), new Vector2(0.50f, 0.07f), Vector2.zero, Vector2.zero);
            debug.SetIsOnWithoutNotify(false);
            debug.onValueChanged.RemoveAllListeners();
            debug.onValueChanged.AddListener(mc.OnDebugEnableAllChanged);
#endif

            // -- Status Bar -----------------------------------------------------
            var status = BuildText("StatusText", root, T("Ready."), 20, TextAnchor.MiddleCenter);
            SetRect(status.rectTransform,
                new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.07f), Vector2.zero, Vector2.zero);
            status.color = rootBgApplied
                ? new Color(0.06f, 0.04f, 0.02f, 0.70f)
                : GamePalette.WithAlpha(TxtColor, 0.6f);
            if (rootBgApplied) AddTextPill(status.rectTransform);
            var statusShadow = EnsureComponent<Shadow>(status.gameObject);
            statusShadow.effectColor = GamePalette.ShadowMid;
            statusShadow.effectDistance = new Vector2(1f, -1f);

            // [REQ: UI-MENU-VERSION-001] The game's version in the bottom-left corner (Player Settings' version, e.g. "0.2.0").
            var version = BuildText("VersionText", root, LocalizationService.Format("MainMenu.Version", "Version {0}", Application.version),
                16, TextAnchor.MiddleLeft);
            SetRect(version.rectTransform, new Vector2(0.012f, 0.012f), new Vector2(0.25f, 0.045f), Vector2.zero, Vector2.zero);
            version.color = GamePalette.WithAlpha(TxtColor, 0.75f);
            version.raycastTarget = false;
            var versionShadow = EnsureComponent<Shadow>(version.gameObject);
            versionShadow.effectColor = GamePalette.ShadowDark;
            versionShadow.effectDistance = new Vector2(1f, -1f);

            // -- Sub-panels -----------------------------------------------------
            var classPanel     = BuildClassSelectPanel(root, mc);
            var optPanel       = BuildOptionsPanel(root, mc, optCtrl);
            var credPanel      = BuildCreditsPanel(root, mc);
            var tutPanel       = BuildTutorialSetupPanel(root, mc, tutCtrl);
            var metaPanel      = BuildMetaProgressionPanel(root, mc, metaCtrl);
            var modesPanel     = BuildGameModesPanel(root, mc, modesCtrl);
            var itemsPanel     = BuildItemsPanel(root, mc, itemsCtrl);
            var profilePanel   = BuildProfileSelectPanel(root, mc);
            var keybindPanel   = BuildKeybindingsPanel(root, mc, keybindCtrl);
            var dailyPanel     = BuildDailyWalkPanel(root, mc);
            var monthlyPanel   = BuildMonthlyWalkPanel(root, mc);

            var trialsSelectCtrl  = EnsureComponent<SpiritTrialsTierSelectController>(gameObject);
            var trialsSelectPanel = BuildSpiritTrialsTierSelectPanel(root, mc, trialsSelectCtrl);

            var zenLeaderboardCtrl  = EnsureComponent<EndlessZenLeaderboardController>(gameObject);
            var zenLeaderboardPanel = BuildEndlessZenLeaderboardPanel(root, mc, zenLeaderboardCtrl);

            // -- Wire -----------------------------------------------------------
            mc.ConfigureUi(bootstrap,
                card.gameObject, classPanel, optPanel, credPanel,
                tutPanel, metaPanel, modesPanel, itemsPanel, profilePanel, keybindPanel,
                optCtrl, tutCtrl, metaCtrl, modesCtrl, itemsCtrl, keybindCtrl,
                status, btnResume, dailyPanel, monthlyPanel, null,   // no Accessibility panel: it is a page of the Options (UI-STYLE-017)
                trialsSelectPanel, zenLeaderboardPanel,
                trialsSelectCtrl, zenLeaderboardCtrl);

            // F ? give the controller a direct canvas reference for modal spawning (avoids FindFirstObjectByType)
            mc.SetMainCanvas(canvas);

            // Reload per-profile keybindings and refresh labels when the active slot changes.
            ReplaceSlotChangedHandler(optCtrl, ref _keybindingSlotChangedHandler, () =>
            {
                mc.InputRemap.ReloadForSlot(SaveProfileService.ActiveSlot);
                keybindCtrl.RefreshAllLabels();
            });

            Debug.Log("[MainMenuBlueprintBuilder] Menu built.");
            if (Application.isPlaying) _built = true;
        }

        private void ReplaceSlotChangedHandler(
            OptionsController optionsController,
            ref Action slotChangedHandler,
            Action nextHandler)
        {
            if (optionsController == null || nextHandler == null) return;

            if (_slotChangedOptionsController != null &&
                _slotChangedOptionsController != optionsController)
            {
                ClearSlotChangedHandlers();
            }

            _slotChangedOptionsController = optionsController;

            if (slotChangedHandler != null)
                optionsController.OnSlotChanged -= slotChangedHandler;

            slotChangedHandler = nextHandler;
            optionsController.OnSlotChanged += slotChangedHandler;
        }

        private void ReplaceOptionsChangedHandler(
            OptionsController optionsController,
            ref Action optionsChangedHandler,
            Action nextHandler)
        {
            if (optionsController == null || nextHandler == null) return;

            if (_slotChangedOptionsController != null &&
                _slotChangedOptionsController != optionsController)
            {
                ClearSlotChangedHandlers();
            }

            _slotChangedOptionsController = optionsController;

            if (optionsChangedHandler != null)
                optionsController.OnOptionsChanged -= optionsChangedHandler;

            optionsChangedHandler = nextHandler;
            optionsController.OnOptionsChanged += optionsChangedHandler;
        }

        private void ClearSlotChangedHandlers()
        {
            if (_slotChangedOptionsController != null)
            {
                if (_keybindingSlotChangedHandler != null)
                    _slotChangedOptionsController.OnSlotChanged -= _keybindingSlotChangedHandler;
                if (_optionsSlotChangedHandler != null)
                    _slotChangedOptionsController.OnOptionsChanged -= _optionsSlotChangedHandler;
            }

            _slotChangedOptionsController = null;
            _keybindingSlotChangedHandler = null;
            _optionsSlotChangedHandler = null;
        }

        private static void ClearGeneratedRoot(RectTransform root)
        {
            if (root == null || root.childCount == 0) return;

            var children = new List<GameObject>(root.childCount);
            for (var i = root.childCount - 1; i >= 0; i--)
                children.Add(root.GetChild(i).gameObject);

            foreach (var child in children)
            {
                child.transform.SetParent(null, false);
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        // Panel: Class Select is built in MainMenuBlueprintBuilder.ClassSelect.cs (UI-STYLE-024).

        // ------------------------------------------------------------------------
        // Panel: Options
        // ------------------------------------------------------------------------

        // [REQ: UI-STYLE-017] Options as a hand scroll with bookmarks (owner pick 2026-10-20): Audio, Display, Gameplay,
        // Accessibility (with Text Size and the preview board; the separate Accessibility panel is gone), Controls
        // (keybindings, the Sudoku Basics lesson) and Language; "Reset this page" and Back under the scroll.
        private GameObject BuildOptionsPanel(RectTransform root, MainMenuController mc,
            OptionsController optCtrl)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            var panel = EnsureRect("OptionsPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var backdrop = EnsureOrGetImage(panel, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_settings_menu"))
                backdrop.color = new Color(0.50f, 0.50f, 0.50f, 1f);   // the candle room, dimmed behind the scroll
            var pr = (RectTransform)panel.transform;
            foreach (var part in new[] { "Content", "OptionsBanner", "OptionsScroll", "OptionsKeyHint" })
                ScrollUiKit.DestroyChildrenNow(pr, part);

            var card = OptionsUiFactory.BuildOptionsScroll(pr, "Title");
            var tabs = OptionsTabSet.On(card);
            var setup = new OptionsUiFactory.Setup
            {
                OnLanguageApplied = mc.RefreshLanguage,
                AccessibilityPreview = true,
                AddPagesBeforeLanguage = t => BuildControlsPage(t, mc, optCtrl)
            };
            var refreshControls = OptionsUiFactory.BuildStandardTabs(tabs, optCtrl, setup);

            var back = OptionsUiFactory.BuildBackPill("BtnOptBack", card, T("Back"));
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            tabs.SetFooter(new Selectable[] { back });

            // -- Slot-change refresh (the Accessibility page's preview and text size are part of it) -------------
            ReplaceOptionsChangedHandler(optCtrl, ref _optionsSlotChangedHandler, refreshControls);

            tabs.Activate(0);
            panel.SetActive(false);
            return panel;
        }

        // The Controls page: keys and buttons, and the Sudoku Basics lesson.
        private void BuildControlsPage(OptionsTabSet tabs, MainMenuController mc, OptionsController optCtrl)
        {
            var index = tabs.Count;
            var section = tabs.Add("TabControls", "ControlsSection", T("Options.Tab.Controls"));
            OptionsUiFactory.SectionHeader("ControlsHdr", section, T("Options.Tab.Controls"));
            OptionsUiFactory.PageSubtitle("ControlsSub", section, T("Options.Sub.Controls"));

            OptionsUiFactory.LineLabel("KeybindingsLabel", section, 0, T("Options.KeysAndButtons"));
            var btnKeybinds = OptionsUiFactory.BuildButton("BtnKeybindings", section, T("Keybindings..."), 19);
            OptionsUiFactory.SetRect((RectTransform)btnKeybinds.transform, OptionsUiFactory.LineMin(0, 0.38f),
                OptionsUiFactory.LineMax(0, 0.82f));
            btnKeybinds.onClick.RemoveAllListeners();
            btnKeybinds.onClick.AddListener(mc.OpenKeybindings);

            OptionsUiFactory.LineLabel("BasicsLabel", section, 1, T("Options.SudokuBasics"));
            var btnBasicsTut = OptionsUiFactory.BuildButton("BtnReplayBasicsTutorial", section, T("Sudoku Tutorial"), 19);
            OptionsUiFactory.SetRect((RectTransform)btnBasicsTut.transform, OptionsUiFactory.LineMin(1, 0.38f),
                OptionsUiFactory.LineMax(1, 0.82f));
            btnBasicsTut.onClick.RemoveAllListeners();
            btnBasicsTut.onClick.AddListener(mc.LaunchSudokuBasicsTutorial);
            // [REQ: INPUT-PAD-002] X-input or D-input: Automatic follows the pad that is connected.
            OptionsUiFactory.LineLabel("ControllerLayoutLabel", section, 2, T("Options.Controller"));
            var btnLayout = OptionsUiFactory.BuildButton("BtnControllerLayout", section, ControllerLayoutLabel(), 19);
            OptionsUiFactory.SetRect((RectTransform)btnLayout.transform, OptionsUiFactory.LineMin(2, 0.38f),
                OptionsUiFactory.LineMax(2, 0.82f));
            btnLayout.onClick.RemoveAllListeners();
            btnLayout.onClick.AddListener(() =>
            {
                optCtrl.SetControllerLayout((ControllerLayoutMode)(((int)PadButtons.Mode + 1) % 3));
                btnLayout.GetComponentInChildren<Text>().text = ControllerLayoutLabel();
            });

            OptionsUiFactory.LineRule(section, 0);
            OptionsUiFactory.LineRule(section, 1);
            OptionsUiFactory.LineRule(section, 2);
            OptionsUiFactory.PageSlip("ControlsSlip", section, T("Options.Slip.Tip"), T("Options.Tip.Controls"));

            tabs.SetRows(index, new Selectable[] { btnKeybinds }, new Selectable[] { btnBasicsTut }, new Selectable[] { btnLayout });
        }

        private static string ControllerLayoutLabel() => PadButtons.Mode switch
        {
            ControllerLayoutMode.XInput => T("Options.Controller.XInput"),
            ControllerLayoutMode.DInput => T("Options.Controller.DInput"),
            _ => T("Options.Controller.Automatic")
        };

        // ------------------------------------------------------------------------
        // Panel: Credits
        // ------------------------------------------------------------------------

        private GameObject BuildCreditsPanel(RectTransform root, MainMenuController mc)
        {
            // F3: expanded panel matching MetaProgression footprint
            var panel = MakePanel("CreditsPanel", root,
                new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.92f));
            var pr = GetPanelContent(panel);

            // F9: localisation-wrapped title, consistent with all other panels
            var title = BuildText("Title", pr, T("Credits"), 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);

            // F2: ScrollRect ? also enables controller/keyboard axis scroll via ScrollActivePanelIfNeeded()
            var scrollContent = BuildScrollArea("CreditsScroll", pr,
                new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.88f));

            // -- Local helpers --------------------------------------------------
            // Unique counter prevents EnsureRect(name) collisions in the scroll content
            var n = 0;

            void AddSeparator()
            {
                var sepGo = new GameObject("CredSep" + n++, typeof(RectTransform), typeof(Image));
                sepGo.transform.SetParent(scrollContent, false);
                sepGo.GetComponent<Image>().color = new Color(
                    GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.35f);
                var le = sepGo.AddComponent<LayoutElement>();
                le.preferredHeight = 2f;
                le.flexibleWidth   = 1f;
            }

            void AddSpacer(float height = 8f)
            {
                var spGo = new GameObject("CredSpace" + n++, typeof(RectTransform));
                spGo.transform.SetParent(scrollContent, false);
                var le = spGo.AddComponent<LayoutElement>();
                le.preferredHeight = height;
                le.flexibleWidth   = 1f;
            }

            // F6: three distinct visual tiers ? section header, role title, body
            Text AddHeader(string text)
            {
                var t = BuildText("CredH" + n++, scrollContent, text, 19, TextAnchor.MiddleLeft);
                t.color = GamePalette.AccentGold;
                return t;
            }

            Text AddRole(string text)
            {
                var t = BuildText("CredR" + n++, scrollContent, text, 14, TextAnchor.UpperLeft);
                t.fontStyle = FontStyle.Bold;
                return t;
            }

            Text AddBody(string text)
            {
                var t = BuildText("CredB" + n++, scrollContent, text, 13, TextAnchor.UpperLeft);
                t.color = GamePalette.WithAlpha(TxtColor, 0.85f);
                return t;
            }

            // -- Section 1: Game Design & Concept ------------------------------
            AddHeader(T("Credits.Section.GameDesign"));
            AddSeparator();
            AddSpacer(4f);
            AddRole(T("Credits.Role.GameDesign"));
            AddBody(T("Credits.Body.GameDesign"));
            AddSpacer(10f);

            // -- Section 2: Development ----------------------------------------
            AddHeader(T("Credits.Section.Development"));
            AddSeparator();
            AddSpacer(4f);
            AddRole(T("Credits.Role.Programming"));
            AddBody(T("Credits.Body.Programming"));
            AddSpacer(10f);

            // -- Section 3: Art & Assets ---------------------------------------
            AddHeader(T("Credits.Section.ArtAssets"));
            AddSeparator();
            AddSpacer(4f);
            AddRole(T("Credits.Role.VisualArt"));
            AddBody(T("Credits.Body.VisualArt"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.IconArt"));
            AddBody(T("Credits.Body.IconArt"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.MusicAudio"));
            AddBody(T("Credits.Body.MusicAudio"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.Font"));
            AddBody(T("Credits.Body.Font"));
            AddSpacer(10f);

            // -- Section 4: Engine & Technology -------------------------------
            AddHeader(T("Credits.Section.EngineTech"));
            AddSeparator();
            AddSpacer(4f);
            AddRole(T("Credits.Role.GameEngine"));
            AddBody(T("Credits.Body.GameEngine"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.UnityPackages"));
            AddBody(T("Credits.Body.UnityPackages"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.ThirdPartyDevelopmentTools"));
            AddBody(T("Credits.Body.GoogleResolver"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.PlatformServices"));
            AddBody(T("Credits.Body.PlatformServices"));
            AddSpacer(10f);

            // -- Section 5: AI Assistance --------------------------------------
            AddHeader(T("Credits.Section.AIAssistance"));
            AddSeparator();
            AddSpacer(4f);
            AddBody(T("Credits.Body.AIAssistance"));
            AddSpacer(10f);

            // -- Community: playtesters and translators (CREDITS-COMMUNITY-001) --
            var community = CommunityCredits.Load();
            AddHeader(T("Credits.Section.Community"));
            AddSeparator();
            AddSpacer(4f);
            if (community.playtesters.Count > 0)
            {
                AddRole(T("Credits.Role.Playtesters"));
                AddBody(community.PlaytesterLine());
                AddSpacer(6f);
            }
            if (community.translators.Count > 0)
            {
                AddRole(T("Credits.Role.Translators"));
                AddBody(community.TranslatorLine());
                AddSpacer(6f);
            }
            if (!community.HasNames)
                AddBody(T("Credits.Body.CommunityEmpty"));
            if (!string.IsNullOrEmpty(community.discord))
                AddBody(LocalizationService.Format("Credits.Body.Discord", "Join us on Discord: {0}", community.discord));
            AddSpacer(10f);

            // -- Section 6: Special Thanks -------------------------------------
            AddHeader(T("Credits.Section.SpecialThanks"));
            AddSeparator();
            AddSpacer(4f);
            AddBody(T("Credits.Body.SpecialThanks"));
            AddSpacer(10f);

            // -- Section 7: Legal Notices --------------------------------------
            AddHeader(T("Credits.Section.LegalNotices"));
            AddSeparator();
            AddSpacer(4f);
            AddRole(T("Credits.Role.Copyright"));
            AddBody(T("Credits.Body.Copyright"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.AIGeneratedDisclaimer"));
            AddBody(T("Credits.Body.AIGeneratedDisclaimer"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.Trademarks"));
            AddBody(T("Credits.Body.Trademarks"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.PrivacyAnalyticsNotice"));
            AddBody(T("Credits.Body.PrivacyAnalyticsNotice"));
            AddSpacer(6f);
            AddRole(T("Credits.Role.AssetLicenses"));
            AddBody(T("Credits.Body.AssetLicenses"));
            AddSpacer(10f);

            // -- Section 8: Licence texts [REQ: LEGAL-LICENSE-001] (GAP-068) -------
            // The full texts the font's and Steamworks.NET's licences require; legal texts stay in English.
            AddHeader(T("Credits.Section.Licenses"));
            AddSeparator();
            AddSpacer(4f);
            AddBody(T("Credits.Body.LicensesIntro"));
            foreach (var license in ThirdPartyLicenses.All)
            {
                AddSpacer(6f);
                AddRole(T(license.RoleKey));
                AddBody(ThirdPartyLicenses.Load(license)).fontSize = 11;
            }
            AddSpacer(16f);

            // F4: no icon ? navigation back, not a save action
            var back = BuildButton("BtnCredBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.38f, 0.01f), new Vector2(0.62f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_menu_credits"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }

            panel.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------------
        // Panel: Meta Progression
        // ------------------------------------------------------------------------

        // [REQ: UI-STYLE-019] Meta Progression as the garden of classes (owner pick 2026-10-20): the class-select painting
        // dimmed, the banner, the plaque area (cords and ema plaques, filled by MetaProgressionPanelController), the hanging
        // scroll with the chosen class, the totals along the bottom, Back and the key hint.
        private GameObject BuildMetaProgressionPanel(RectTransform root, MainMenuController mc,
            MetaProgressionPanelController metaCtrl)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            var panel = EnsureRect("MetaProgressionPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var backdrop = EnsureOrGetImage(panel, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_class_select"))
                backdrop.color = new Color(0.45f, 0.45f, 0.45f, 1f);   // the garden, dimmed behind the plaques
            var pr = (RectTransform)panel.transform;
            foreach (var part in new[] { "Content", "MetaBanner", "ClassGarden", "ClassScroll", "MetaTotals", "MetaKeyHint", "BtnMetaBack" })
                ScrollUiKit.DestroyChildrenNow(pr, part);

            ScrollUiKit.Banner(pr, "MetaBanner", new Vector2(0.156f, 0.891f), new Vector2(0.448f, 0.983f),
                "MetaEyebrow", T("MetaProgression.Eyebrow"), "Title", T("Meta Progression"), 46);
            var garden = ScrollUiKit.Area(pr, "ClassGarden", Vector2.zero, Vector2.one);
            var scroll = ScrollUiKit.HangingScroll(pr, "ClassScroll", new Vector2(0.578f, 0.150f), new Vector2(0.9375f, 0.862f));
            var totals = ScrollUiKit.Area(pr, "MetaTotals", new Vector2(0.047f, 0.135f), new Vector2(0.536f, 0.205f));

            var back = OptionsUiFactory.BuildBackPill("BtnMetaBack", pr, T("Back"));
            SetRect((RectTransform)back.transform, new Vector2(0.232f, 0.050f), new Vector2(0.352f, 0.110f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            var hint = OptionsUiFactory.BuildText("MetaKeyHint", pr, T("MetaProgression.KeyHint"), 17, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.03f, 0.008f), new Vector2(0.553f, 0.045f), Vector2.zero, Vector2.zero);
            hint.color = new Color(0.86f, 0.82f, 0.72f, 1f);

            metaCtrl.Configure(garden, scroll, totals, back);
            panel.SetActive(false);
            return panel;
        }

        // Panel: Game Modes is built in MainMenuBlueprintBuilder.GameModes.cs (UI-STYLE-025).
        // ------------------------------------------------------------------------
        // Panel: Items Codex
        // ------------------------------------------------------------------------

        // [REQ: UI-STYLE-018] The Codex as a collector's book (owner pick 2026-10-20): the banner, a wooden book with two
        // paper pages, bookmarks Items / Relics / Rules / Curses with their counts, the left page's filter and tile grid,
        // the right page with the chosen entry (ItemsMenuController), Back under the book.
        private GameObject BuildItemsPanel(RectTransform root, MainMenuController mc,
            ItemsMenuController itemsCtrl)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            var panel = EnsureRect("ItemsPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var backdrop = EnsureOrGetImage(panel, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_items_menu"))
                backdrop.color = new Color(0.42f, 0.42f, 0.42f, 1f);   // the market, dimmed behind the book
            var pr = (RectTransform)panel.transform;
            foreach (var part in new[] { "Content", "CodexBanner", "CodexBook", "CodexKeyHint", "BtnCodex", "BtnItemsBack" })
                ScrollUiKit.DestroyChildrenNow(pr, part);

            ScrollUiKit.Banner(pr, "CodexBanner", new Vector2(0.344f, 0.891f), new Vector2(0.656f, 0.983f),
                "CodexEyebrow", T("Codex.Eyebrow"), "Title", T("Codex.Title"), 46);
            var book = ScrollUiKit.Fill(pr, "CodexBook", new Vector2(0.068f, 0.155f), new Vector2(0.932f, 0.835f),
                new Color(0.33f, 0.21f, 0.11f, 1f), ScrollUiKit.Rounded(14f));
            var bookShadow = book.gameObject.AddComponent<Shadow>();
            bookShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            bookShadow.effectDistance = new Vector2(0f, -12f);
            var paperColor = new Color(0.94f, 0.90f, 0.81f, 1f);
            var left = ScrollUiKit.Fill(book.transform, "LeftPage", new Vector2(0.012f, 0.022f), new Vector2(0.495f, 0.978f), paperColor);
            var right = ScrollUiKit.Fill(book.transform, "RightPage", new Vector2(0.505f, 0.022f), new Vector2(0.988f, 0.978f), paperColor);
            ScrollUiKit.Fill(book.transform, "Spine", new Vector2(0.495f, 0.022f), new Vector2(0.505f, 0.978f),
                new Color(0.22f, 0.14f, 0.07f, 1f));

            // Bookmarks above the book; their labels (with the counts) are written by the controller.
            Button Bookmark(string name, int index)
            {
                var tab = OptionsUiFactory.BuildButton(name, pr, "", 21);
                var x = 0.078f + index * 0.118f;
                SetRect((RectTransform)tab.transform, new Vector2(x, 0.835f), new Vector2(x + 0.110f, 0.888f), Vector2.zero, Vector2.zero);
                var img = tab.GetComponent<Image>();
                img.sprite = ScrollUiKit.Rounded(10f);
                img.type = Image.Type.Sliced;
                var label = tab.GetComponentInChildren<Text>();
                if (label != null)
                {
                    // "Gegenstände 18/31", "Malédictions 6/17": long names shrink to the bookmark.
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 13;
                    label.resizeTextMaxSize = 21;
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                }
                return tab;
            }
            var tabItems = Bookmark("BtnCodexItems", 0);
            var tabRelics = Bookmark("BtnCodexRelics", 1);
            var tabModifiers = Bookmark("BtnCodexModifiers", 2);
            var tabCurses = Bookmark("BtnCodexCurses", 3);

            // Filter on the left page: All · Found · Not yet found (Y switches).
            Button FilterButton(string name, string label, float x0, float x1)
            {
                var button = OptionsUiFactory.BuildButton(name, left.rectTransform, label, 19);
                SetRect((RectTransform)button.transform, new Vector2(x0, 0.905f), new Vector2(x1, 0.975f), Vector2.zero, Vector2.zero);
                var colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0f);
                colors.highlightedColor = new Color(0.97f, 0.83f, 0.52f, 0.35f);
                button.colors = colors;
                button.GetComponent<Image>().color = Color.white;
                var outline = button.GetComponent<Outline>();
                if (outline != null) outline.effectColor = Color.clear;
                return button;
            }
            var filterAll = FilterButton("BtnCodexFilterAll", T("Codex.Filter.All"), 0.03f, 0.18f);
            var filterFound = FilterButton("BtnCodexFilterFound", T("Codex.Filter.Found"), 0.19f, 0.38f);
            var filterUnknown = FilterButton("BtnCodexFilterUnknown", T("Codex.Filter.Unknown"), 0.39f, 0.70f);

            // The tile grid scrolls inside the left page.
            var view = EnsureRect("CodexGridView", left.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.89f),
                Vector2.zero, Vector2.zero);
            view.gameObject.AddComponent<RectMask2D>();
            var grid = EnsureRect("CodexGrid", view, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            grid.pivot = new Vector2(0.5f, 1f);
            var layout = EnsureComponent<GridLayoutGroup>(grid.gameObject);
            layout.cellSize = new Vector2(138f, 138f);
            layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.padding = new RectOffset(4, 4, 4, 4);
            EnsureComponent<ContentSizeFitter>(grid.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = EnsureComponent<ScrollRect>(view.gameObject);
            scroll.viewport = view;
            scroll.content = grid;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var detail = EnsureRect("CodexDetail", right.rectTransform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f),
                Vector2.zero, Vector2.zero);

            itemsCtrl.Configure(grid);
            itemsCtrl.ConfigureDetail(detail);
            itemsCtrl.SetTabButtons(tabItems, tabRelics, tabModifiers, tabCurses);
            itemsCtrl.SetFilterButtons(filterAll, filterFound, filterUnknown);
            tabItems.onClick.AddListener(() => itemsCtrl.ShowTab(CodexTabId.Items));
            tabRelics.onClick.AddListener(() => itemsCtrl.ShowTab(CodexTabId.Relics));
            tabModifiers.onClick.AddListener(() => itemsCtrl.ShowTab(CodexTabId.Modes));
            tabCurses.onClick.AddListener(() => itemsCtrl.ShowTab(CodexTabId.Curses));
            // [REQ: META-CODEX-010] The Items page also filters by rarity (Every rarity · Normal · Rare · Epic).
            itemsCtrl.SetRarityButton(FilterButton("BtnCodexRarity", T("Codex.Rarity.Every"), 0.71f, 0.99f));

            var back = OptionsUiFactory.BuildBackPill("BtnItemsBack", pr, T("Back"));
            SetRect((RectTransform)back.transform, new Vector2(0.44f, 0.075f), new Vector2(0.56f, 0.135f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            itemsCtrl.SetBackButton(back);
            var hint = OptionsUiFactory.BuildText("CodexKeyHint", pr, T("Codex.KeyHint"), 17, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.2f, 0.03f), new Vector2(0.8f, 0.065f), Vector2.zero, Vector2.zero);
            hint.color = new Color(0.86f, 0.82f, 0.72f, 1f);

            panel.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------------
        // Panel: Keybindings
        // ------------------------------------------------------------------------

        private static readonly InputAction[] RebindableActions =
        {
            InputAction.MoveUp,
            InputAction.MoveDown,
            InputAction.MoveLeft,
            InputAction.MoveRight,
            InputAction.TogglePencil,
            InputAction.ClearCell,
            InputAction.HighlightDigit,
            InputAction.BagNext,
            InputAction.BagPrevious,
            InputAction.UseItem,
            InputAction.InspectItem,
            InputAction.Confirm,
            InputAction.Cancel,
            InputAction.SkipAnimation,
            InputAction.TabNext,
            InputAction.TabPrevious,
        };

        private GameObject BuildKeybindingsPanel(RectTransform root, MainMenuController mc,
            KeybindingsPanelController keybindCtrl)
        {
            var panel = MakePanel("KeybindingsPanel", root,
                new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.92f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Keybindings.Title"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            title.color = TxtColor;

            var hint = BuildText("Hint", pr, T("Keybindings.Hint"), 11, TextAnchor.UpperCenter);
            SetRect(hint.rectTransform,
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.91f), Vector2.zero, Vector2.zero);
            hint.color = GamePalette.TextMuted;

            // Column headers
            var hdrAction = BuildText("HdrAction", pr, T("Keybindings.Header.Action"), 14, TextAnchor.MiddleLeft);
            SetRect(hdrAction.rectTransform,
                new Vector2(0.05f, 0.83f), new Vector2(0.38f, 0.88f), Vector2.zero, Vector2.zero);
            hdrAction.color = GamePalette.AccentGold;

            var hdrKey = BuildText("HdrKey", pr, T("Keybindings.Header.Key"), 14, TextAnchor.MiddleCenter);
            SetRect(hdrKey.rectTransform,
                new Vector2(0.40f, 0.83f), new Vector2(0.62f, 0.88f), Vector2.zero, Vector2.zero);
            hdrKey.color = GamePalette.AccentGold;

            var hdrPad = BuildText("HdrPad", pr, T("Keybindings.Header.Gamepad"), 14, TextAnchor.MiddleCenter);
            SetRect(hdrPad.rectTransform,
                new Vector2(0.64f, 0.83f), new Vector2(0.82f, 0.88f), Vector2.zero, Vector2.zero);
            hdrPad.color = GamePalette.AccentGold;

            // Rows ? inside a scroll area so overflow is handled gracefully
            var rowCount = RebindableActions.Length;

            // Detect connected controller type for friendly gamepad labels
            var ctrlPlatform = ControllerGlyphService.Detect();
            bool isPs = ctrlPlatform == ControllerPlatform.PlayStation;

            string PadLabel(KeyCode key)
            {
                if (key == KeyCode.None) return "?";
                return key switch
                {
                    KeyCode.JoystickButton0  => isPs ? "Cross"     : "A",
                    KeyCode.JoystickButton1  => isPs ? "Circle"    : "B (ctx)",
                    KeyCode.JoystickButton2  => isPs ? "Square"    : "X",
                    KeyCode.JoystickButton3  => isPs ? "Triangle"  : "Y",
                    KeyCode.JoystickButton4  => isPs ? "L1"        : "LB",
                    KeyCode.JoystickButton5  => isPs ? "R1"        : "RB",
                    KeyCode.JoystickButton6  => isPs ? "Select"    : "Back",
                    KeyCode.JoystickButton9  => isPs ? "R3"        : "R3",
                    KeyCode.JoystickButton12 => "D-Up",
                    KeyCode.JoystickButton13 => "D-Down",
                    KeyCode.JoystickButton14 => "D-Left",
                    KeyCode.JoystickButton15 => "D-Right",
                    _                        => key.ToString().Replace("JoystickButton", "Btn")
                };
            }

            bool IsUiOnly(InputAction a) =>
                a == InputAction.BagNext || a == InputAction.BagPrevious;

            const float rowHeightPx = 44f;
            var contentHeightPx = rowCount * rowHeightPx;

            var scrollContainer = EnsureRect("KeybindScroll", pr,
                new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(scrollContainer.gameObject, GamePalette.ScrollBg);

            var kbViewport = EnsureRect("Viewport", scrollContainer,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureComponent<RectMask2D>(kbViewport.gameObject);

            var kbContent = EnsureRect("Content", kbViewport,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            kbContent.pivot     = new Vector2(0.5f, 1f);
            kbContent.sizeDelta = new Vector2(0f, contentHeightPx);

            var kbScrollRect = EnsureComponent<ScrollRect>(scrollContainer.gameObject);
            kbScrollRect.viewport         = kbViewport;
            kbScrollRect.content          = kbContent;
            kbScrollRect.horizontal       = false;
            kbScrollRect.vertical         = true;
            kbScrollRect.movementType     = ScrollRect.MovementType.Clamped;
            kbScrollRect.scrollSensitivity = 30f;

            var rebindButtons = new System.Collections.Generic.List<Button>();
            for (var i = 0; i < rowCount; i++)
            {
                var action = RebindableActions[i];
                var topOffset    = i * rowHeightPx;
                var bottomOffset = (i + 1) * rowHeightPx;

                // Row container (parent for all cells in this row)
                var rowBgColor = i % 2 == 0 ? GamePalette.ScrollBgDark : GamePalette.ScrollBgFaint;
                var rowBg = EnsureRect($"RowBg_{i}", kbContent,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(2f,  -bottomOffset),
                    new Vector2(-2f, -topOffset));
                var rowBgImg = EnsureOrGetImage(rowBg.gameObject, rowBgColor);
                keybindCtrl.RegisterRowBackground(action, rowBgImg, rowBgColor);

                // Action label
                var actionLbl = BuildText($"Action_{i}", rowBg, KeybindingsPanelController.GetActionLabel(action), 13, TextAnchor.MiddleLeft);
                SetRect(actionLbl.rectTransform,
                    new Vector2(0.02f, 0.05f), new Vector2(0.38f, 0.95f), Vector2.zero, Vector2.zero);
                actionLbl.color = TxtColor;

                // Current key label
                var defaultKey = InputRemapService.GetDefault(action);
                var keyDisplay = IsUiOnly(action) ? T("Keybindings.UiOnly") : InputRemapService.FormatKeyCode(defaultKey);
                var keyLbl = BuildText($"Key_{i}", rowBg, keyDisplay, 13, TextAnchor.MiddleCenter);
                SetRect(keyLbl.rectTransform,
                    new Vector2(0.40f, 0.05f), new Vector2(0.62f, 0.95f), Vector2.zero, Vector2.zero);
                keyLbl.color = IsUiOnly(action) ? GamePalette.TextMuted : GamePalette.TextPrimary;
                keybindCtrl.RegisterLabel(action, keyLbl);

                // Gamepad default label (read-only, friendly name)
                var padDefault = InputRemapService.GetGamepadDefault(action);
                var padLbl = BuildText($"Pad_{i}", rowBg, PadLabel(padDefault), 11, TextAnchor.MiddleCenter);
                SetRect(padLbl.rectTransform,
                    new Vector2(0.64f, 0.05f), new Vector2(0.82f, 0.95f), Vector2.zero, Vector2.zero);
                padLbl.color = GamePalette.TextMuted;

                // Rebind button (hidden for UI-only actions)
                var capturedAction = action;
                var btnRebind = BuildButton($"BtnRebind_{i}", rowBg, T("Rebind"), 11);
                SetRect(btnRebind.GetComponent<RectTransform>(),
                    new Vector2(0.84f, 0.08f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);
                btnRebind.interactable = !IsUiOnly(action);
                btnRebind.onClick.RemoveAllListeners();
                btnRebind.onClick.AddListener(() => keybindCtrl.StartListening(capturedAction));
                AddButtonGlyphBadge(btnRebind, "KEY", new Vector2(0.04f, 0.18f), new Vector2(0.34f, 0.82f), 0.38f);
                if (!IsUiOnly(action)) rebindButtons.Add(btnRebind);
            }

            // Chain explicit vertical nav between all interactable rebind buttons
            for (var i = 0; i < rebindButtons.Count - 1; i++)
                LinkSelectableNav(rebindButtons[i], rebindButtons[i + 1]);

            // Status text
            var statusLbl = BuildText("KeybindStatus", pr, "", 12, TextAnchor.MiddleCenter);
            SetRect(statusLbl.rectTransform,
                new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.15f), Vector2.zero, Vector2.zero);
            statusLbl.color = GamePalette.TextMuted;

            keybindCtrl.Configure(mc.InputRemap, statusLbl);

            // Reset all
            var btnReset = BuildButton("BtnResetAll", pr, T("Reset All"), 14);
            SetRect(btnReset.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.01f), new Vector2(0.30f, 0.09f), Vector2.zero, Vector2.zero);
            btnReset.onClick.RemoveAllListeners();
            btnReset.onClick.AddListener(keybindCtrl.ResetAll);
            ApplyMenuButtonIcon(btnReset, "ui/icon_temple_bell");           // F-QA: reset/clear action icon (was unused)

            // Back
            var back = BuildButton("BtnKeybindBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackFromPanel);
            ApplyMenuButtonIcon(back, "ui/icon_back_leaf"); // F16: back/close uses dedicated back_leaf icon

            ApplySettingsInRunBackground(panel, pr); // F-QA: shared settings background helper

            panel.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------------
        // Panel: Spirit Trials — Tier Select ? Tier Select
        // ------------------------------------------------------------------------

        private GameObject BuildSpiritTrialsTierSelectPanel(RectTransform root, MainMenuController mc,
            SpiritTrialsTierSelectController trialsCtrl)
        {
            var panel = MakePanel("SpiritTrialsTierSelectPanel", root,
                new Vector2(0.24f, 0.10f), new Vector2(0.76f, 0.90f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Spirit Trials"), 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);

            var subtitle = BuildText("Subtitle", pr, T("SpiritTrials.TierSelectSubtitle"), 15, TextAnchor.UpperCenter);
            SetRect(subtitle.rectTransform,
                new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);
            subtitle.color = GamePalette.TextMuted;

            // Four tier rows: Apprentice, Adept, Master, Grandmaster
            var tiers = new[]
            {
                (SpiritTrialsTier.Apprentice,  "SpiritTrials.Tier.Apprentice",  "???  ",  5, 15, "5:00"),
                (SpiritTrialsTier.Adept,       "SpiritTrials.Tier.Adept",       "???  ",  4, 12, "7:00"),
                (SpiritTrialsTier.Master,      "SpiritTrials.Tier.Master",      "???? ",  3, 10, "9:00"),
                (SpiritTrialsTier.Grandmaster, "SpiritTrials.Tier.Grandmaster", "?????", 2,  8, "12:00"),
            };

            float[] rowBases = { 0.62f, 0.49f, 0.36f, 0.23f };
            var bestLabels = new Text[tiers.Length];

            for (var i = 0; i < tiers.Length; i++)
            {
                var (tier, nameKey, stars, hp, pencil, parTime) = tiers[i];
                var rowBase    = rowBases[i];
                var capturedTier = tier;

                // Row background
                var rowBg = EnsureRect($"TierRow_{i}", pr,
                    new Vector2(0.04f, rowBase), new Vector2(0.96f, rowBase + 0.11f),
                    Vector2.zero, Vector2.zero);
                EnsureOrGetImage(rowBg.gameObject, GamePalette.MenuInfoBg);
                var rowOutline = EnsureComponent<Outline>(rowBg.gameObject);
                rowOutline.effectColor = GamePalette.AccentGoldFaint;
                rowOutline.effectDistance = new Vector2(1f, -1f);

                // Tier name
                var nameLbl = BuildText($"TierName_{i}", rowBg, T(nameKey), 20, TextAnchor.MiddleLeft);
                SetRect(nameLbl.rectTransform,
                    new Vector2(0.04f, 0.52f), new Vector2(0.55f, 0.96f), Vector2.zero, Vector2.zero);
                nameLbl.color = GamePalette.AccentGold;

                // Stars
                var starsLbl = BuildText($"TierStars_{i}", rowBg, stars, 13, TextAnchor.MiddleLeft);
                SetRect(starsLbl.rectTransform,
                    new Vector2(0.04f, 0.06f), new Vector2(0.55f, 0.50f), Vector2.zero, Vector2.zero);
                starsLbl.color = GamePalette.WinGold;

                // Stats (right-top)
                var statsLbl = BuildText(
                    $"TierStats_{i}",
                    rowBg,
                    LocalizationService.Format("SpiritTrials.TierStats", "HP {0} - Pencil {1} - Par {2}", hp, pencil, parTime),
                    11,
                    TextAnchor.MiddleRight);
                SetRect(statsLbl.rectTransform,
                    new Vector2(0.44f, 0.52f), new Vector2(0.86f, 0.96f), Vector2.zero, Vector2.zero);

                // Personal best (right-bottom)
                var bestLbl = BuildText($"TierBest_{i}", rowBg, T("SpiritTrials.BestPlaceholder"), 11, TextAnchor.MiddleRight);
                SetRect(bestLbl.rectTransform,
                    new Vector2(0.44f, 0.06f), new Vector2(0.86f, 0.50f), Vector2.zero, Vector2.zero);
                bestLbl.color = GamePalette.TextMuted;
                bestLabels[i] = bestLbl;

                // Transparent click button covering the entire row
                var rowBtn = BuildButton($"BtnTierStart_{i}", pr, "", 14);
                SetRect(rowBtn.GetComponent<RectTransform>(),
                    new Vector2(0.04f, rowBase), new Vector2(0.96f, rowBase + 0.11f),
                    Vector2.zero, Vector2.zero);
                var btnImg = rowBtn.GetComponent<Image>();
                if (btnImg != null) btnImg.color = Color.clear;
                var btnLbl = rowBtn.GetComponentInChildren<Text>(true);
                if (btnLbl != null) btnLbl.color = Color.clear;
                rowBtn.onClick.RemoveAllListeners();
                rowBtn.onClick.AddListener(() => mc.StartSpiritTrials(capturedTier));
                AddButtonGlyphBadge(rowBtn, ">", new Vector2(0.91f, 0.24f), new Vector2(0.98f, 0.76f), -1f);
            }

            var back = BuildButton("BtnTrialsSelectBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackFromPanel);
            ApplyMenuButtonIcon(back, "ui/icon_back_leaf"); // F16: back/close uses dedicated back_leaf icon;

            trialsCtrl.Configure(bestLabels[0], bestLabels[1], bestLabels[2], bestLabels[3]);

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_trials_menu"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }

            panel.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------------
        // Panel: Endless Zen ? Leaderboard / Records
        // ------------------------------------------------------------------------

        private GameObject BuildEndlessZenLeaderboardPanel(RectTransform root, MainMenuController mc,
            EndlessZenLeaderboardController zenCtrl)
        {
            var panel = MakePanel("EndlessZenLeaderboardPanel", root,
                new Vector2(0.28f, 0.14f), new Vector2(0.72f, 0.86f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("EndlessZen.Title"), 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);

            var subtitle = BuildText("Subtitle", pr, T("EndlessZen.Subtitle"), 14, TextAnchor.UpperCenter);
            SetRect(subtitle.rectTransform,
                new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);
            subtitle.color = GamePalette.TextMuted;

            // Personal best depth
            var bestDepthLbl = BuildText("BestDepth", pr, T("EndlessZen.PersonalBestPlaceholder"), 22, TextAnchor.MiddleCenter);
            SetRect(bestDepthLbl.rectTransform,
                new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.74f), Vector2.zero, Vector2.zero);
            bestDepthLbl.color = GamePalette.AccentGold;

            // Total sessions
            var sessionLbl = BuildText(
                "TotalSessions",
                pr,
                LocalizationService.Format("EndlessZen.TotalSessions", "Total Sessions: {0}", 0),
                16,
                TextAnchor.MiddleCenter);
            SetRect(sessionLbl.rectTransform,
                new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.63f), Vector2.zero, Vector2.zero);
            sessionLbl.color = GamePalette.TextMuted;

            // Start button
            var startBtn = BuildButton("BtnZenStart", pr, T("Start Endless Zen"), 18);
            SetRect(startBtn.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.40f), Vector2.zero, Vector2.zero);
            startBtn.onClick.RemoveAllListeners();
            startBtn.onClick.AddListener(mc.StartEndlessZen);
            ApplyMenuButtonIcon(startBtn, "ui/icon_zen_mode");             // F-QA: zen mode icon; eternal_lotus reserved for legendary relic

            var back = BuildButton("BtnZenLeaderboardBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackFromPanel);
            ApplyMenuButtonIcon(back, "ui/icon_back_leaf"); // F16: back/close uses dedicated back_leaf icon;

            zenCtrl.Configure(bestDepthLbl, sessionLbl);

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_zen_menu"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }

            panel.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------------
        // Panel: Profile Select (3 save slots)
        // ------------------------------------------------------------------------

        private GameObject BuildProfileSelectPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("ProfileSelectPanel", root,
                new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("ProfileSelect.Title"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);
            title.color = TxtColor;

            var subtitle = BuildText("Subtitle", pr, T("ProfileSelect.Subtitle"), 14, TextAnchor.UpperCenter);
            SetRect(subtitle.rectTransform,
                new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero);
            subtitle.color = GamePalette.WithAlpha(TxtColor, 0.65f);

            // 3 slot cards side by side
            const float cardW = 0.28f;
            const float gap   = 0.04f;
            const float startX = 0.05f;
            for (var i = 0; i < 3; i++)
            {
                var xMin = startX + i * (cardW + gap);
                var xMax = xMin + cardW;
                BuildSlotCard(pr, mc, i, xMin, xMax);
            }

            // Explicit horizontal nav between Load buttons and between Delete buttons across cards
            var loadBtns = new Button[3];
            var delBtns  = new Button[3];
            for (var i = 0; i < 3; i++)
            {
                var card = pr.Find($"SlotCard_{i}");
                if (card == null) continue;
                loadBtns[i] = card.Find($"BtnLoad_{i}")?.GetComponent<Button>();
                delBtns[i]  = card.Find($"BtnDelete_{i}")?.GetComponent<Button>();
            }
            for (var i = 0; i < 2; i++)
            {
                if (loadBtns[i] != null && loadBtns[i + 1] != null)
                    LinkHorizontalNav(loadBtns[i], loadBtns[i + 1]);
                if (delBtns[i] != null && delBtns[i + 1] != null)
                    LinkHorizontalNav(delBtns[i], delBtns[i + 1]);
            }
            // Load ? Delete vertical within each card
            for (var i = 0; i < 3; i++)
                if (loadBtns[i] != null && delBtns[i] != null)
                    LinkSelectableNav(loadBtns[i], delBtns[i]);

            // Back
            var back = BuildButton("BtnProfileBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "ui/icon_back_leaf"); // F16: back/close uses dedicated back_leaf icon;

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_menu_profile"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }

            panel.SetActive(false);
            return panel;
        }

        private void BuildSlotCard(RectTransform parent, MainMenuController mc, int slotIndex,
            float xMin, float xMax)
        {
            var card = EnsureRect($"SlotCard_{slotIndex}", parent,
                new Vector2(xMin, 0.18f), new Vector2(xMax, 0.80f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(card.gameObject, GamePalette.ListItemBg);
            var outline = EnsureComponent<Outline>(card.gameObject);
            outline.effectColor = GamePalette.ButtonOutline;
            outline.effectDistance = new Vector2(1f, -1f);

            // Slot label
            var slotLabel = BuildText(
                $"SlotLabel_{slotIndex}",
                card,
                LocalizationService.Format("ProfileSelect.SlotLabel", "Profile {0}", slotIndex + 1),
                18,
                TextAnchor.UpperCenter);
            SetRect(slotLabel.rectTransform,
                new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            slotLabel.color = TxtColor;

            // Info text (class / runs / last played ? populated at runtime by mc.RefreshProfileSelect)
            var info = BuildText($"SlotInfo_{slotIndex}", card, T("ProfileSelect.Loading"), 12, TextAnchor.UpperCenter);
            SetRect(info.rectTransform,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            info.color = GamePalette.TextMuted;

            // Load / New button
            var btnLoad = BuildButton($"BtnLoad_{slotIndex}", card, T("Load"), 14);
            SetRect(btnLoad.GetComponent<RectTransform>(),
                new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.40f), Vector2.zero, Vector2.zero);
            var capturedSlot = slotIndex;
            btnLoad.onClick.RemoveAllListeners();
            btnLoad.onClick.AddListener(() => mc.SelectProfileSlot(capturedSlot));

            // Delete button
            var btnDelete = BuildButton($"BtnDelete_{slotIndex}", card, T("Delete"), 12);
            SetRect(btnDelete.GetComponent<RectTransform>(),
                new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.21f), Vector2.zero, Vector2.zero);
            btnDelete.onClick.RemoveAllListeners();
            btnDelete.onClick.AddListener(() => mc.DeleteProfileSlot(capturedSlot));

            // Style delete button as danger
            var delImg = btnDelete.GetComponent<Image>();
            if (delImg != null) delImg.color = GamePalette.WithAlpha(GamePalette.DangerRed, 0.80f);
            var delTxt = btnDelete.GetComponentInChildren<Text>();
            if (delTxt != null) delTxt.color = Color.white;
        }

        // ------------------------------------------------------------------------
        // Helpers: Composite Builders
        // ------------------------------------------------------------------------

        private GameObject MakePanel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            // Full-screen opaque overlay so submenus are not transparent over the main menu
            var go = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(go, BgColor);

            // Inner card ? same proportions as the main menu card
            var inner = EnsureRect("Content", go.transform as RectTransform,
                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.92f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(inner.gameObject, PanelColor);
            var outline = EnsureComponent<Outline>(inner.gameObject);
            outline.effectColor = GamePalette.CardOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return go;
        }

        // Returns the inner card rect of a panel created by MakePanel
        private static RectTransform GetPanelContent(GameObject panel)
        {
            var t = panel.transform.Find("Content");
            return t != null ? t as RectTransform : panel.transform as RectTransform;
        }

        private RectTransform BuildScrollArea(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var scrollRect = EnsureRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(scrollRect.gameObject, GamePalette.ScrollBg);

            var viewport = EnsureRect("Viewport", scrollRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(viewport.gameObject, GamePalette.ScrollBgFaint);
            var mask = EnsureComponent<Mask>(viewport.gameObject);
            mask.showMaskGraphic = false;

            var content = EnsureRect("Content", viewport,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);

            var fitter = EnsureComponent<ContentSizeFitter>(content.gameObject);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var layout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 4f;

            var scroll = EnsureComponent<ScrollRect>(scrollRect.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            return content;
        }

        private Slider BuildLabelAndSlider(RectTransform parent, string id, string label,
            Vector2 areaMin, Vector2 areaMax, float min, float max, float value,
            UnityEngine.Events.UnityAction<float> onChanged)
        {
            var midY = (areaMin.y + areaMax.y) * 0.5f;

            var lbl = BuildText(id + "Label", parent, label, 16, TextAnchor.MiddleLeft);
            SetRect(lbl.rectTransform,
                new Vector2(areaMin.x, midY), areaMax, Vector2.zero, Vector2.zero);

            var slider = BuildSlider(id + "Slider", parent);
            SetRect(slider.GetComponent<RectTransform>(),
                areaMin, new Vector2(areaMax.x, midY), Vector2.zero, Vector2.zero);
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        private Toggle BuildOptionToggle(RectTransform parent, string id, string label,
            Vector2 anchorMin, Vector2 anchorMax, bool value,
            UnityEngine.Events.UnityAction<bool> onChanged) =>
            OptionsUiFactory.BuildOptionToggle(parent, id, label, anchorMin, anchorMax, value, onChanged);

        // ------------------------------------------------------------------------
        // Helpers: Menu Button
        // ------------------------------------------------------------------------

        private Button BuildMenuButton(RectTransform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
        {
            var rect = EnsureRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var img = EnsureOrGetImage(rect.gameObject, BtnColor);
            img.raycastTarget = true;

            var btn = EnsureComponent<Button>(rect.gameObject);

            // Warm outline to match the mockup
            var outline = EnsureComponent<Outline>(rect.gameObject);
            outline.effectColor = GamePalette.ButtonOutline;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var colors = btn.colors;
            colors.normalColor      = BtnColor;
            colors.highlightedColor = GamePalette.ButtonHighlight;
            colors.pressedColor     = GamePalette.ButtonPressed;
            colors.selectedColor    = new Color(0.98f, 0.83f, 0.26f, 0.70f); // gold tint when controller-focused
            colors.disabledColor    = GamePalette.ButtonDisabled;
            colors.fadeDuration     = 0.08f;
            colors.colorMultiplier  = 1f;
            btn.colors = colors;

            // Enable automatic navigation so EventSystem can traverse with stick/d-pad
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // Dark text on cream button
            var lblRect = EnsureRect("Label", rect, Vector2.zero, Vector2.one,
                new Vector2(10f, 4f), new Vector2(-10f, -4f));
            var text = EnsureComponent<Text>(lblRect.gameObject);
            text.text = label;
            text.font = GetBuiltInFont();
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = BtnTextColor;
            lblRect.SetAsLastSibling();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);

            return btn;
        }

        private void ApplyMenuButtonIcon(Button button, string resourcePath)
        {
            if (button == null) return;
            var icon = LoadSpriteFromResources(resourcePath);
            if (icon == null) return;

            var iconRect = EnsureRect("Icon", button.transform as RectTransform,
                new Vector2(0.02f, 0.16f), new Vector2(0.13f, 0.84f), Vector2.zero, Vector2.zero);
            var iconImg = EnsureOrGetImage(iconRect.gameObject, Color.white);
            iconImg.sprite = icon;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;

            var label = button.transform.Find("Label") as RectTransform;
            if (label != null)
            {
                SetRect(label, new Vector2(0.16f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);
                label.SetAsLastSibling();
            }
        }

        private void AddButtonGlyphBadge(Button button, string glyph, Vector2 anchorMin, Vector2 anchorMax, float labelMinX)
        {
            if (button == null) return;
            var root = button.transform as RectTransform;
            if (root == null) return;

            var badge = EnsureRect("AffordanceGlyph", root, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var badgeImg = EnsureOrGetImage(badge.gameObject, new Color(0.98f, 0.83f, 0.26f, 0.16f));
            badgeImg.raycastTarget = false;

            var textRect = EnsureRect("GlyphText", badge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var txt = EnsureComponent<Text>(textRect.gameObject);
            txt.text = glyph;
            txt.font = GetBuiltInFont();
            txt.fontSize = glyph.Length > 1 ? 9 : 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.98f, 0.83f, 0.26f, 0.92f);
            txt.raycastTarget = false;

            if (labelMinX >= 0f)
            {
                var label = button.transform.Find("Label") as RectTransform;
                if (label != null)
                    SetRect(label, new Vector2(labelMinX, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);
            }
        }

        // ------------------------------------------------------------------------
        // Helpers: Panel Backgrounds
        // ------------------------------------------------------------------------

        private void ApplySettingsInRunBackground(GameObject panel, RectTransform pr)
        {
            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_settings_inrun"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }
        }

        private static bool ApplyPanelBackground(Image bgImg, string resourcePath)
        {
            var sprite = LoadSpriteFromResources(resourcePath);
            if (sprite == null) return false;
            bgImg.sprite = sprite;
            bgImg.type   = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.color  = Color.white;
            return true;
        }

        private static void StyleButtonForBackground(Button btn)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (btn.name.StartsWith("BtnTierStart_", StringComparison.Ordinal))
            {
                if (img != null) img.color = Color.clear;
                var transparentColors = btn.colors;
                transparentColors.normalColor = Color.clear;
                transparentColors.highlightedColor = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.14f);
                transparentColors.pressedColor = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.22f);
                transparentColors.selectedColor = transparentColors.highlightedColor;
                transparentColors.colorMultiplier = 1f;
                btn.colors = transparentColors;
                var hiddenLabel = btn.transform.Find("Label")?.GetComponent<Text>();
                if (hiddenLabel != null) hiddenLabel.color = Color.clear;
                return;
            }
            if (img != null) img.color = BgBtnNormal;
            var colors = btn.colors;
            colors.normalColor      = BgBtnNormal;
            colors.highlightedColor = new Color(BgBtnNormal.r + 0.10f, BgBtnNormal.g + 0.10f, BgBtnNormal.b + 0.09f, 0.30f);
            colors.pressedColor     = new Color(BgBtnNormal.r + 0.03f, BgBtnNormal.g + 0.03f, BgBtnNormal.b + 0.03f, 0.48f);
            colors.selectedColor    = colors.highlightedColor;
            colors.colorMultiplier  = 1f;
            btn.colors = colors;
            var label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.color = BgBtnText;
        }

        /// <summary>Applies gold label and larger font to a primary main-menu button (Start/Resume).</summary>
        private static void StylePrimaryMenuButton(Button btn)
        {
            if (btn == null) return;
            var label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label == null) return;
            label.fontSize = 26;
            label.color    = GamePalette.AccentGold;
        }

        /// <summary>Applies muted label to system-tier buttons (Options/Credits/Profiles/Quit).</summary>
        private static void StyleSecondaryMenuButton(Button btn)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = new Color(BtnColor.r * 0.80f, BtnColor.g * 0.80f, BtnColor.b * 0.80f, 0.60f);
            var label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label == null) return;
            label.fontSize = 18;
            label.color    = new Color(BtnTextColor.r, BtnTextColor.g, BtnTextColor.b, 0.62f);
        }

        /// <summary>
        /// Turns all non-button Text children black and adds a very-transparent pill background
        /// so they read clearly against art backgrounds.
        /// Call after the panel is fully built; restore any text that sits on a dark sub-background afterwards.
        /// </summary>
        private static void DarkenNonButtonText(RectTransform root)
        {
            var dark = new Color(0.06f, 0.04f, 0.02f, 1f);
            foreach (var t in root.GetComponentsInChildren<Text>(true))
            {
                if (t.GetComponentInParent<Button>() != null) continue;
                t.color = dark;
                AddTextPill(t.rectTransform);
            }
        }

        /// <summary>Inserts a very-transparent cream Image behind the given Text rect.</summary>
        private static void AddTextPill(RectTransform textRt)
        {
            var parentRt = textRt.parent as RectTransform;
            if (parentRt == null) return;
            var bgName = textRt.gameObject.name + "_Pill";
            if (parentRt.Find(bgName) != null) return;
            var pill   = new GameObject(bgName, typeof(RectTransform));
            pill.transform.SetParent(parentRt, false);
            var pillRt        = pill.GetComponent<RectTransform>();
            pillRt.anchorMin  = textRt.anchorMin;
            pillRt.anchorMax  = textRt.anchorMax;
            pillRt.offsetMin  = textRt.offsetMin - new Vector2(3f, 1f);
            pillRt.offsetMax  = textRt.offsetMax + new Vector2(3f, 1f);
            pillRt.pivot      = textRt.pivot;
            pill.AddComponent<Image>().color = TextPillColor;
            pill.transform.SetSiblingIndex(textRt.GetSiblingIndex());
        }

        // ------------------------------------------------------------------------
        // Helpers: UI Primitives
        // ------------------------------------------------------------------------

        private Text BuildText(string name, RectTransform parent, string value,
            int size, TextAnchor anchor) => OptionsUiFactory.BuildText(name, parent, value, size, anchor);

        private Button BuildButton(string name, RectTransform parent, string label, int size) =>
            OptionsUiFactory.BuildButton(name, parent, label, size);

        private Dropdown BuildDropdown(string name, RectTransform parent) =>
            OptionsUiFactory.BuildDropdown(name, parent);

        /// Links explicit down/up navigation between two Selectable widgets (Toggle, Slider, Dropdown, Button).
        private static void LinkSelectableNav(Selectable from, Selectable to)
        {
            var n = from.navigation;
            n.mode = Navigation.Mode.Explicit;
            n.selectOnDown = to;
            from.navigation = n;
            var nu = to.navigation;
            nu.mode = Navigation.Mode.Explicit;
            nu.selectOnUp = from;
            to.navigation = nu;
        }

        /// Links down/up navigation from a Slider to the next Selectable.
        private static void LinkSliderNav(Slider from, Selectable to) =>
            LinkSelectableNav(from, to);

        /// Links right/left navigation between two Selectables (horizontal sibling layout).
        private static void LinkHorizontalNav(Selectable from, Selectable to)
        {
            var n = from.navigation;
            n.mode = Navigation.Mode.Explicit;
            n.selectOnRight = to;
            from.navigation = n;
            var nu = to.navigation;
            nu.mode = Navigation.Mode.Explicit;
            nu.selectOnLeft = from;
            to.navigation = nu;
        }

        /// <summary>
        /// Resizes the dropdown template so all items are visible without scrolling.
        /// Call this after AddOptions(). Capped at 300px to stay on screen.
        /// </summary>
        private static void FitDropdownToItems(Dropdown dd) => OptionsUiFactory.FitDropdownToItems(dd);

        // ------------------------------------------------------------------------
        // Helpers: Infrastructure
        // ------------------------------------------------------------------------

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private Canvas EnsureCanvas()
        {
            var existing = GetComponentInParent<Canvas>();
            if (existing != null) return existing;

            // Reuse the canvas this builder created on an earlier Build. Without this, ForceRebuild
            // (e.g. after a language change) stacked a second canvas and menu over the old one.
            var own = transform.Find("Canvas");
            if (own != null && own.TryGetComponent<Canvas>(out var ownCanvas)) return ownCanvas;

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        // ------------------------------------------------------------------------
        // Helpers: Core
        // ------------------------------------------------------------------------

        private static RectTransform EnsureRect(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
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

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
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
            var img = EnsureComponent<Image>(go);
            img.color = color;
            return img;
        }

        // V2: delegates to InRunUiFactory.GetFont() ? single font-load path for all builders.
        private static Font GetBuiltInFont() => InRunUiFactory.GetFont();

        private static Sprite LoadSpriteFromResources(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);

            return null;
        }
    }
}
