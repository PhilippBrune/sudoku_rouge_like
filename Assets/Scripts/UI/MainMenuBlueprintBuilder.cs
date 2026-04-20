using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuBlueprintBuilder : MonoBehaviour
    {
        [SerializeField] private bool autoBuildOnStart = true;

        private static readonly Color BgColor     = new(0.06f, 0.09f, 0.08f, 1f);
        private static readonly Color PanelColor  = new(0.10f, 0.12f, 0.10f, 0.70f);
        private static readonly Color BtnColor    = new(0.08f, 0.06f, 0.04f, 0.78f);  // dark charcoal, 78% opaque
        private static readonly Color BtnTextColor = new(0.98f, 0.83f, 0.26f, 1f);    // gold
        private static readonly Color AccentColor = new(0.98f, 0.83f, 0.26f, 1f);
        private static readonly Color TxtColor    = new(0.96f, 0.93f, 0.82f, 1f);
        // ─ Over-background colours (used when a panel has an art background image) ─
        private static readonly Color BgPanelOverlay = new(0.06f, 0.08f, 0.06f, 0.88f);
        private static readonly Color BgBtnNormal    = new(0.08f, 0.06f, 0.04f, 0.78f);  // same dark charcoal for art-bg panels
        private static readonly Color BgBtnText      = new(0.98f, 0.83f, 0.26f, 1f);    // gold
        private static readonly Color TextPillColor    = new(1.00f, 0.97f, 0.93f, 0.15f);
        private static readonly Color ContentScrimColor = new(0f, 0f, 0f, 0.60f);

        private bool _built;

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

        [ContextMenu("Build Main Menu")]
        public void Build()
        {
            if (Application.isPlaying && _built) return;

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
            EnsureOrGetImage(root.gameObject, BgColor);
            // A — CanvasGroup for splash-to-menu fade-in (starts invisible; GameBootstrap fades it in)
            // Note: ?? bypasses Unity's null operator override — use EnsureComponent instead
            var rootCg = EnsureComponent<CanvasGroup>(root.gameObject);
            rootCg.alpha = 0f;

            // ── Title ────────────────────────────────────────────────────────
            var titleText = BuildText("TitleText", root, "Run of the Nine", 42, TextAnchor.MiddleCenter);
            SetRect(titleText.rectTransform,
                new Vector2(0.15f, 0.88f), new Vector2(0.85f, 0.96f), Vector2.zero, Vector2.zero);
            titleText.fontStyle = FontStyle.BoldAndItalic;
            titleText.color = TxtColor;
            var titleShadow = EnsureComponent<Shadow>(titleText.gameObject);
            titleShadow.effectColor = GamePalette.ShadowDark;
            titleShadow.effectDistance = new Vector2(2f, -2f);

            var subtitleText = BuildText("SubtitleText", root, "Sudoku Roguelike", 18, TextAnchor.MiddleCenter);
            SetRect(subtitleText.rectTransform,
                new Vector2(0.25f, 0.845f), new Vector2(0.75f, 0.885f), Vector2.zero, Vector2.zero);
            subtitleText.color = GamePalette.WithAlpha(TxtColor, 0.65f);

            // ── Menu Card ──────────────────────────────────────────────────────
            var card = EnsureRect("MenuCard", root,
                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.84f),
                Vector2.zero, Vector2.zero);
            // 3 — Stone tablet card: visible background + subtle gold border when no art bg
            EnsureOrGetImage(card.gameObject, new Color(0.12f, 0.14f, 0.11f, 0.85f));
            var cardOutline = EnsureComponent<Outline>(card.gameObject);
            cardOutline.effectColor = new Color(GamePalette.AccentGold.r, GamePalette.AccentGold.g, GamePalette.AccentGold.b, 0.28f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // ── Main Menu Buttons ──────────────────────────────────────────────
            // Primary group (Start / Resume) — taller and gold-accented
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

            // Secondary group — same height, slightly muted (M-1: Meta → Modes → Items → Custom Puzzle)
            var btnMeta     = BuildMenuButton(card, "BtnMeta",     T("Meta Progression"), new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.70f), mc.OpenMetaProgression);
            var btnModes    = BuildMenuButton(card, "BtnModes",    T("Game Modes"),       new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.62f), mc.OpenGameModes);
            var btnItems    = BuildMenuButton(card, "BtnItems",    "Items & Relic Codex",            new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.54f), mc.OpenItems);
            var btnTut      = BuildMenuButton(card, "BtnTutorial", T("Custom Puzzle"),   new Vector2(0.06f, 0.39f), new Vector2(0.94f, 0.46f), mc.OpenTutorial);

            // Thin separator before system group
            var sepGo2 = new GameObject("MenuGroupSeparator2", typeof(RectTransform), typeof(Image));
            sepGo2.transform.SetParent(card, false);
            var sepRt2 = sepGo2.GetComponent<RectTransform>();
            sepRt2.anchorMin = new Vector2(0.06f, 0.375f);
            sepRt2.anchorMax = new Vector2(0.94f, 0.377f);
            sepRt2.offsetMin = sepRt2.offsetMax = Vector2.zero;
            sepGo2.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

            // System group — visually muted (smaller text, lower alpha buttons handled via StyleSecondaryMenuButton)
            var btnProfiles = BuildMenuButton(card, "BtnProfiles", "Profiles",            new Vector2(0.06f, 0.31f), new Vector2(0.94f, 0.37f), mc.ShowProfileSelect);
            var btnOpt      = BuildMenuButton(card, "BtnOptions",  T("Options"),          new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.30f), mc.OpenOptions);
            var btnCred     = BuildMenuButton(card, "BtnCredits",  T("Credits"),          new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.22f), mc.OpenCredits);
            var btnQuit     = BuildMenuButton(card, "BtnQuit",     T("Quit"),             new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.14f), mc.ExitGame);

            StyleSecondaryMenuButton(btnProfiles);
            StyleSecondaryMenuButton(btnOpt);
            StyleSecondaryMenuButton(btnCred);
            StyleSecondaryMenuButton(btnQuit);

            ApplyMenuButtonIcon(btnStart,    "GeneratedIcons/icon_engraved_stone"); // 4 — engraved stone tablet fits the garden-gate theme
            ApplyMenuButtonIcon(btnResume,   "GeneratedIcons/icon_scroll_graph");
            ApplyMenuButtonIcon(btnTut,      "GeneratedIcons/icon_bamboo_scroll");
            ApplyMenuButtonIcon(btnMeta,     "GeneratedIcons/icon_golden_bloom");
            ApplyMenuButtonIcon(btnModes,    "GeneratedIcons/icon_garden_lantern");
            ApplyMenuButtonIcon(btnItems,    "GeneratedIcons/icon_triple_chest");
            ApplyMenuButtonIcon(btnProfiles, "GeneratedIcons/icon_scroll_stamp");
            ApplyMenuButtonIcon(btnOpt,      "GeneratedIcons/icon_stone_gear");
            ApplyMenuButtonIcon(btnCred,     "GeneratedIcons/icon_language_scroll");
            ApplyMenuButtonIcon(btnQuit,     "GeneratedIcons/icon_torii_lock");

            // Apply art background to main-menu card if the asset is available
            var rootBgImg = EnsureOrGetImage(root.gameObject, BgColor);
            var rootBgApplied = ApplyPanelBackground(rootBgImg, "GeneratedIcons/ui_main_menu");
            if (rootBgApplied)
            {
                // Art background overrides the stone tablet — revert card to transparent
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
            var debug = BuildToggle("DebugToggle", card, "Enable All (Debug)");
            SetRect(debug.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.01f), new Vector2(0.50f, 0.07f), Vector2.zero, Vector2.zero);
            debug.SetIsOnWithoutNotify(false);
            debug.onValueChanged.RemoveAllListeners();
            debug.onValueChanged.AddListener(mc.OnDebugEnableAllChanged);
#endif

            // ── Status Bar ─────────────────────────────────────────────────────
            var status = BuildText("StatusText", root, "Ready.", 20, TextAnchor.MiddleCenter);
            SetRect(status.rectTransform,
                new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.07f), Vector2.zero, Vector2.zero);
            status.color = rootBgApplied
                ? new Color(0.06f, 0.04f, 0.02f, 0.70f)
                : GamePalette.WithAlpha(TxtColor, 0.6f);
            if (rootBgApplied) AddTextPill(status.rectTransform);
            var statusShadow = EnsureComponent<Shadow>(status.gameObject);
            statusShadow.effectColor = GamePalette.ShadowMid;
            statusShadow.effectDistance = new Vector2(1f, -1f);

            // ── Sub-panels ─────────────────────────────────────────────────────
            var classPanel     = BuildClassSelectPanel(root, mc);
            var optPanel       = BuildOptionsPanel(root, mc, optCtrl);
            var credPanel      = BuildCreditsPanel(root, mc);
            var tutPanel       = BuildTutorialSetupPanel(root, mc, tutCtrl);
            var metaPanel      = BuildMetaProgressionPanel(root, mc, metaCtrl);
            var modesPanel     = BuildGameModesPanel(root, mc);
            var itemsPanel     = BuildItemsPanel(root, mc, itemsCtrl);
            var profilePanel   = BuildProfileSelectPanel(root, mc);
            var keybindPanel   = BuildKeybindingsPanel(root, mc, keybindCtrl);
            var accessPanel    = BuildAccessibilityPanel(root, mc, optCtrl);
            var dailyPanel     = BuildDailyWalkPanel(root, mc);
            var monthlyPanel   = BuildMonthlyWalkPanel(root, mc);

            // ── Wire ───────────────────────────────────────────────────────────
            mc.ConfigureUi(bootstrap,
                card.gameObject, classPanel, optPanel, credPanel,
                tutPanel, metaPanel, modesPanel, itemsPanel, profilePanel, keybindPanel,
                optCtrl, tutCtrl, metaCtrl, modesCtrl, itemsCtrl, keybindCtrl,
                status, btnResume, dailyPanel, monthlyPanel, accessPanel);

            // F — give the controller a direct canvas reference for modal spawning (avoids FindFirstObjectByType)
            mc.SetMainCanvas(canvas);

            // Reload per-profile keybindings and refresh labels when the active slot changes.
            optCtrl.OnSlotChanged += () =>
            {
                mc.InputRemap.ReloadForSlot(SaveProfileService.ActiveSlot);
                keybindCtrl.RefreshAllLabels();
            };

            Debug.Log("[MainMenuBlueprintBuilder] Menu built.");
            if (Application.isPlaying) _built = true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Class Select
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildClassSelectPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("ClassSelectPanel", root,
                new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Select Class"), 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            // 8 class buttons in 2×4 grid
            var classes = new (ClassId id, string label, string icon)[]
            {
                (ClassId.NumberFreak,       "Number Freak",       "icon_number_freak"),
                (ClassId.GardenMonk,        "Garden Monk",        "icon_garden_monk"),
                (ClassId.ShrineArchivist,   "Shrine Archivist",   "icon_shrine_archivist"),
                (ClassId.KoiGambler,        "Koi Gambler",        "icon_koi_gambler"),
                (ClassId.StoneGardener,     "Stone Gardener",     "icon_stone_gardener"),
                (ClassId.LanternSeer,       "Lantern Seer",       "icon_lantern_seer"),
                (ClassId.ReedDuelist,       "Reed Duelist",       "icon_reed_duelist"),
                (ClassId.QuietCartographer, "Quiet Cartographer", "icon_quiet_cartographer"),
            };

            for (var i = 0; i < classes.Length; i++)
            {
                var (classId, label, icon) = classes[i];
                var col = i % 2;
                var row = i / 2;
                var xMin = col == 0 ? 0.10f : 0.54f;
                var xMax = col == 0 ? 0.46f : 0.90f;
                // 0.08-tall buttons with 0.10 spacing: rows 0-3 occupy y=0.49-0.87
                var yMax = 0.87f - row * 0.10f;
                var yMin = yMax - 0.08f;

                var btn = BuildButton($"BtnClass{classId}", pr, T(label), 16);
                SetRect(btn.GetComponent<RectTransform>(),
                    new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);
                btn.onClick.RemoveAllListeners();
                var captured = classId;
                btn.onClick.AddListener(() => mc.SetSelectedClass(captured));
                ApplyMenuButtonIcon(btn, "class/" + icon);
                mc.RegisterClassButton(captured, btn);

                // #5 — Mini-summary: passive one-liner below the class name icon
                var def = ClassCatalog.GetDefinition(classId);
                var summaryTxt = InRunUiFactory.CreateText(btn.transform, "Summary",
                    def.PassiveDescription, 9, TextAnchor.LowerLeft,
                    new Color(0.82f, 0.80f, 0.68f, 0.70f));
                summaryTxt.rectTransform.anchorMin = new Vector2(0.16f, 0f);
                summaryTxt.rectTransform.anchorMax = new Vector2(1f, 0.38f);
                summaryTxt.rectTransform.offsetMin = new Vector2(2f, 2f);
                summaryTxt.rectTransform.offsetMax = new Vector2(-2f, 0f);
                summaryTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                summaryTxt.verticalOverflow   = VerticalWrapMode.Truncate;
                summaryTxt.raycastTarget = false;

                // #6 — Lock icon: top-right corner, visible only for locked classes
                var lockIconGo = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
                lockIconGo.transform.SetParent(btn.transform, false);
                var lockRt = lockIconGo.GetComponent<RectTransform>();
                lockRt.anchorMin = new Vector2(0.80f, 0.60f);
                lockRt.anchorMax = new Vector2(1.00f, 1.00f);
                lockRt.offsetMin = lockRt.offsetMax = Vector2.zero;
                var lockSprite = Resources.Load<Sprite>("GeneratedIcons/icon_iron_latch"); // 5 — iron latch fits "locked gate" better than torii
                var lockImg = lockIconGo.GetComponent<Image>();
                lockImg.sprite = lockSprite;
                lockImg.color = new Color(0.90f, 0.50f, 0.15f, 0.85f);
                lockImg.preserveAspect = true;
                lockImg.raycastTarget = false;
                mc.RegisterLockOverlay(captured, lockIconGo);

                // C-3 — EventTrigger: show unlock-condition tooltip on hover OR controller focus
                var capturedForHover = classId;
                var capturedBtn = btn;
                var etClass = EnsureComponent<UnityEngine.EventSystems.EventTrigger>(btn.gameObject);
                var lockEnter = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                lockEnter.callback.AddListener(evtData =>
                    mc.ShowClassLockTooltip(capturedForHover,
                        ((UnityEngine.EventSystems.PointerEventData)evtData).position));
                etClass.triggers.Add(lockEnter);
                var lockExit = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                lockExit.callback.AddListener(_ => mc.HideClassLockTooltip());
                etClass.triggers.Add(lockExit);
                // Controller: Select/Deselect mirror the pointer hover behaviour
                var lockSelect = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.Select };
                lockSelect.callback.AddListener(_ =>
                {
                    var rt = capturedBtn.GetComponent<RectTransform>();
                    var screenPos = RectTransformUtility.WorldToScreenPoint(null, rt.position);
                    mc.ShowClassLockTooltip(capturedForHover, screenPos);
                });
                etClass.triggers.Add(lockSelect);
                var lockDeselect = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.Deselect };
                lockDeselect.callback.AddListener(_ => mc.HideClassLockTooltip());
                etClass.triggers.Add(lockDeselect);
            }

            // Class info panel: y=0.19-0.48 (height=0.29), below row3 (bottom≈0.49), above controls (top≈0.18)
            var infoBg = EnsureRect("ClassInfoBg", pr,
                new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(infoBg.gameObject, GamePalette.MenuInfoBg);
            var infoOutline = EnsureComponent<Outline>(infoBg.gameObject);
            infoOutline.effectColor = GamePalette.AccentGoldFaint;
            infoOutline.effectDistance = new Vector2(1f, -1f);
            // Clip text to box so it never bleeds over buttons
            EnsureComponent<UnityEngine.UI.RectMask2D>(infoBg.gameObject);

            var classInfo = BuildText("ClassInfoText", infoBg, "", 11, TextAnchor.UpperLeft);
            SetRect(classInfo.rectTransform,
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
            classInfo.color = TxtColor;
            classInfo.lineSpacing = 1.1f;
            mc.SetClassInfoText(classInfo);

            // Select first class by default and show lock states
            mc.SetSelectedClass(ClassId.NumberFreak);
            mc.RefreshClassLockStates();

            // Allow Irregular toggle — SAME width (0.36) and height (0.08) as class buttons
            var irregular = BuildToggle("TglAllowIrregular", pr, T("Allow Irregular Puzzles"));
            SetRect(irregular.GetComponent<RectTransform>(),
                new Vector2(0.10f, 0.10f), new Vector2(0.46f, 0.18f), Vector2.zero, Vector2.zero);
            irregular.isOn = false; // default: irregular puzzles off
            irregular.onValueChanged.RemoveAllListeners();
            irregular.onValueChanged.AddListener(mc.SetAllowIrregularPuzzles);

            // N-3 — Tooltip on the irregular toggle
            {
                var tipPanel = new GameObject("IrregularTip", typeof(RectTransform), typeof(Image));
                tipPanel.transform.SetParent(pr, false);
                var tipRt = tipPanel.GetComponent<RectTransform>();
                tipRt.anchorMin = new Vector2(0.10f, 0.19f);
                tipRt.anchorMax = new Vector2(0.46f, 0.26f);
                tipRt.offsetMin = tipRt.offsetMax = Vector2.zero;
                tipPanel.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.95f);
                var tipTxt = InRunUiFactory.CreateText(tipPanel.transform, "Tip",
                    "Regions may be non-rectangular (jigsaw-shaped). Harder to visualise.",
                    9, TextAnchor.MiddleLeft, new Color(0.90f, 0.87f, 0.72f, 1f));
                tipTxt.rectTransform.anchorMin = Vector2.zero;
                tipTxt.rectTransform.anchorMax = Vector2.one;
                tipTxt.rectTransform.offsetMin = new Vector2(4f, 2f);
                tipTxt.rectTransform.offsetMax = new Vector2(-4f, -2f);
                tipTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                tipTxt.verticalOverflow   = VerticalWrapMode.Overflow;
                tipTxt.raycastTarget = false;
                tipPanel.SetActive(false);
                var et = EnsureComponent<UnityEngine.EventSystems.EventTrigger>(irregular.gameObject);
                var enterE = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enterE.callback.AddListener(_ => tipPanel.SetActive(true));
                et.triggers.Add(enterE);
                var exitE = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exitE.callback.AddListener(_ => tipPanel.SetActive(false));
                et.triggers.Add(exitE);
                // Controller: Select/Deselect mirror the pointer hover behaviour
                var selE = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.Select };
                selE.callback.AddListener(_ => tipPanel.SetActive(true));
                et.triggers.Add(selE);
                var deselE = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.Deselect };
                deselE.callback.AddListener(_ => tipPanel.SetActive(false));
                et.triggers.Add(deselE);
            }

            // #8/C-5 — Start Run: AccentGold text, routes through modal
            var confirm = BuildButton("BtnConfirmClass", pr, T("Start Run"), 20);
            SetRect(confirm.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.10f), new Vector2(0.90f, 0.18f), Vector2.zero, Vector2.zero);
            confirm.onClick.RemoveAllListeners();
            confirm.onClick.AddListener(mc.ShowStartRunModal);
            ApplyMenuButtonIcon(confirm, "GeneratedIcons/icon_bud");
            var confirmLbl = confirm.transform.Find("Label")?.GetComponent<Text>();
            if (confirmLbl != null) confirmLbl.color = GamePalette.AccentGold;

            // Back — same height as class buttons (0.08)
            var back = BuildButton("BtnClassBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            // Apply art background if the asset is available
            if (ApplyPanelBackground(panel.GetComponent<Image>(), "GeneratedIcons/ui_class_select"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;

                classInfo.color = TxtColor; // classInfo sits on dark MenuInfoBg — keep light
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }

            panel.SetActive(false);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Tutorial Setup
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildTutorialSetupPanel(RectTransform root, MainMenuController mc,
            TutorialMenuController tutCtrl)
        {
            var panel = MakePanel("TutorialSetupPanel", root,
                new Vector2(0.22f, 0.05f), new Vector2(0.78f, 0.95f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Custom Puzzle"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            // Board Size
            var bsLabel = BuildText("BoardSizeLabel", pr, T("Board Size"), 15, TextAnchor.MiddleLeft);
            SetRect(bsLabel.rectTransform,
                new Vector2(0.04f, 0.84f), new Vector2(0.28f, 0.89f), Vector2.zero, Vector2.zero);
            var boardDrop = BuildDropdown("BoardSizeDropdown", pr);
            SetRect(boardDrop.GetComponent<RectTransform>(),
                new Vector2(0.30f, 0.84f), new Vector2(0.96f, 0.89f), Vector2.zero, Vector2.zero);
            boardDrop.ClearOptions();
            boardDrop.AddOptions(new List<string> { "5x5", "6x6", "7x7", "8x8", "9x9" });
            FitDropdownToItems(boardDrop);

            // Stars
            var stLabel = BuildText("StarsLabel", pr, T("Difficulty"), 15, TextAnchor.MiddleLeft);
            SetRect(stLabel.rectTransform,
                new Vector2(0.04f, 0.77f), new Vector2(0.28f, 0.82f), Vector2.zero, Vector2.zero);
            var starsDrop = BuildDropdown("StarsDropdown", pr);
            SetRect(starsDrop.GetComponent<RectTransform>(),
                new Vector2(0.30f, 0.77f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);
            starsDrop.ClearOptions();
            starsDrop.AddOptions(new List<string>
                { "1 Star (40%)", "2 Stars (50%)", "3 Stars (60%)", "4 Stars (70%)", "5 Stars (80%)", "6 Stars (90%)", "7 Stars (100%)" });
            FitDropdownToItems(starsDrop);

            // Region Layout
            var rgLabel = BuildText("RegionLabel", pr, T("Region Layout"), 15, TextAnchor.MiddleLeft);
            SetRect(rgLabel.rectTransform,
                new Vector2(0.04f, 0.70f), new Vector2(0.28f, 0.75f), Vector2.zero, Vector2.zero);
            var regionDrop = BuildDropdown("RegionDropdown", pr);
            SetRect(regionDrop.GetComponent<RectTransform>(),
                new Vector2(0.30f, 0.70f), new Vector2(0.96f, 0.75f), Vector2.zero, Vector2.zero);
            regionDrop.ClearOptions();
            regionDrop.AddOptions(new List<string> { "Standard", "Alt Rectangular", "Jigsaw" });
            FitDropdownToItems(regionDrop);

            // ── Resource Mode (dropdown) ──
            var rmLabel = BuildText("ResourceModeLabel", pr, T("Resource Mode"), 15, TextAnchor.MiddleLeft);
            rmLabel.fontStyle = FontStyle.Bold;
            // Same color as Board Size label (TxtColor, not AccentColor)
            rmLabel.color = TxtColor;
            SetRect(rmLabel.rectTransform,
                new Vector2(0.04f, 0.63f), new Vector2(0.28f, 0.68f), Vector2.zero, Vector2.zero);

            var resourceModeDrop = BuildDropdown("ResourceModeDropdown", pr);
            SetRect(resourceModeDrop.GetComponent<RectTransform>(),
                new Vector2(0.30f, 0.63f), new Vector2(0.96f, 0.68f), Vector2.zero, Vector2.zero);
            resourceModeDrop.ClearOptions();
            resourceModeDrop.AddOptions(new List<string> { T("Free (∞ HP/Pencil)"), T("Class-Based") });
            FitDropdownToItems(resourceModeDrop);

            // ── Class row (hidden by default in Free mode) ──
            var classRow = EnsureRect("ClassRow", pr,
                new Vector2(0.04f, 0.57f), new Vector2(0.96f, 0.62f), Vector2.zero, Vector2.zero);
            var classLabel = BuildText("ClassLabel", classRow, T("Class:"), 15, TextAnchor.MiddleLeft);
            classLabel.color = TxtColor;
            SetRect(classLabel.rectTransform,
                new Vector2(0f, 0f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero);
            var classDrop = BuildDropdown("ClassDropdown", classRow);
            SetRect(classDrop.GetComponent<RectTransform>(),
                new Vector2(0.30f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            classDrop.ClearOptions();
            classDrop.AddOptions(new List<string>
            {
                T("Number Freak"), T("Garden Monk"), T("Shrine Archivist"),
                T("Koi Gambler"), T("Stone Gardener"), T("Lantern Seer"),
                T("Reed Duelist"), T("Quiet Cartographer")
            });
            FitDropdownToItems(classDrop);
            classRow.gameObject.SetActive(false);

            // Wire resource mode dropdown to controller
            tutCtrl.ConfigureResourceModeDropdown(resourceModeDrop, classDrop, classRow);

            // ── Sudoku Modes label ──
            var modsTitle = BuildText("ModsTitle", pr, T("Sudoku Modes"), 15, TextAnchor.MiddleLeft);
            modsTitle.fontStyle = FontStyle.Bold;
            modsTitle.color = AccentColor;
            SetRect(modsTitle.rectTransform,
                new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.56f), Vector2.zero, Vector2.zero);

            // ── 6 modifier columns (30 total: 15 original + 15 extended) ──
            var colW = 0.94f / 6f;

            RectTransform MakeCol(string colName, int idx)
            {
                var xMin = 0.02f + idx * colW;
                var xMax = xMin + colW - 0.005f;
                var col = EnsureRect(colName, pr,
                    new Vector2(xMin, 0.10f), new Vector2(xMax, 0.51f), Vector2.zero, Vector2.zero);
                var vl = EnsureComponent<VerticalLayoutGroup>(col.gameObject);
                vl.childAlignment = TextAnchor.UpperCenter;
                vl.spacing = 1f;
                vl.childControlWidth = true;
                vl.childControlHeight = false;
                vl.childForceExpandWidth = true;
                vl.childForceExpandHeight = false;
                vl.padding = new RectOffset(1, 1, 1, 1);
                return col;
            }

            void AddHeader(RectTransform col, string goName, string text)
            {
                var h = EnsureRect(goName, col, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var t = EnsureComponent<Text>(h.gameObject);
                t.text = text;
                t.font = GetBuiltInFont();
                t.fontSize = 10;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = AccentColor;
                t.fontStyle = FontStyle.Bold;
                EnsureComponent<LayoutElement>(h.gameObject).preferredHeight = 14f;
            }

            Toggle AddTgl(RectTransform col, string goName, string text)
            {
                var tgl = BuildToggle(goName, col, text);
                EnsureComponent<LayoutElement>(tgl.gameObject).preferredHeight = 16f;
                return tgl;
            }

            // Col 0: Global / Negative (original 2 + 6 extended = 8)
            var c0 = MakeCol("ColGlobal", 0);
            AddHeader(c0, "HdrGlobal", "Global/Neg");
            var noncons        = AddTgl(c0, "TglNoncons",        "Nonconsec.");
            var antiknight     = AddTgl(c0, "TglAntiknight",     "Anti-Knight");
            var antiking       = AddTgl(c0, "TglAntiking",       "Anti-King");
            var antibishop     = AddTgl(c0, "TglAntiBishop",     "Anti-Bishop");
            var nonconsecDiag  = AddTgl(c0, "TglNonconsecDiag",  "NcDiagonal");
            var distGe2        = AddTgl(c0, "TglDistGe2",        "Dist ≥ 2");
            var entropy        = AddTgl(c0, "TglEntropy",        "Entropy");
            var modular        = AddTgl(c0, "TglModular",        "Modular Rgn");

            // Col 1: Line (original 6 + 3 extended = 9)
            var c1 = MakeCol("ColLine", 1);
            AddHeader(c1, "HdrLine", "Line");
            var german     = AddTgl(c1, "TglGerman",     "German");
            var dutch      = AddTgl(c1, "TglDutch",      "Dutch");
            var parity     = AddTgl(c1, "TglParity",     "Parity");
            var renban     = AddTgl(c1, "TglRenban",     "Renban");
            var palindrome = AddTgl(c1, "TglPalindrome", "Palindrome");
            var between    = AddTgl(c1, "TglBetween",    "Between");
            var consecLine = AddTgl(c1, "TglConsecLine", "Consecutive");
            var slowThermo = AddTgl(c1, "TglSlowThermo", "SlowThermo");
            var uniqueSet  = AddTgl(c1, "TglUniqueSet",  "UniqueSet");

            // Col 2: Thermo + Arrow
            var c2 = MakeCol("ColThermoArrow", 2);
            AddHeader(c2, "HdrThermoArrow", "Thermo/Arrow");
            var thermo = AddTgl(c2, "TglThermo", "Thermo");
            var arrow  = AddTgl(c2, "TglArrow",  "Arrow Sums");

            // Col 3: Dots (original 2 + 2 extended = 4)
            var c3 = MakeCol("ColDots", 3);
            AddHeader(c3, "HdrDots", "Dots");
            var diff       = AddTgl(c3, "TglDiff",       "Diff Kropki");
            var ratio      = AddTgl(c3, "TglRatio",      "Ratio Kropki");
            var fullKropki = AddTgl(c3, "TglFullKropki", "Full Kropki");
            var sumKropki  = AddTgl(c3, "TglSumKropki",  "Sum Dot");

            // Col 4: Pairs + Cages (original 1 + 2 extended = 3 + Killer)
            var c4 = MakeCol("ColPairs", 4);
            AddHeader(c4, "HdrPairs", "Pairs/Cage");
            var killer       = AddTgl(c4, "TglKiller",       "Killer");
            var greaterLess  = AddTgl(c4, "TglGreaterLess",  "Gt/LessThan");
            var xvPairs      = AddTgl(c4, "TglXVPairs",      "XV Pairs");

            // Col 5: Cell / Fog (original 2 + 2 extended = 4)
            var c5 = MakeCol("ColCell", 5);
            AddHeader(c5, "HdrCell", "Cell / Fog");
            var evenOdd      = AddTgl(c5, "TglEvenOdd",      "Even/Odd");
            var fog          = AddTgl(c5, "TglFog",          "Fog of War");
            var primeCells   = AddTgl(c5, "TglPrimeCells",   "Prime");
            var fortressCells= AddTgl(c5, "TglFortress",     "Fortress");

            // Bottom controls
            var startBtn = BuildButton("BtnTutStart", pr, T("Start Puzzle"), 15);
            SetRect(startBtn.GetComponent<RectTransform>(),
                new Vector2(0.10f, 0.01f), new Vector2(0.46f, 0.09f), Vector2.zero, Vector2.zero);
            startBtn.onClick.RemoveAllListeners();
            startBtn.onClick.AddListener(mc.StartTutorialGame);
            ApplyMenuButtonIcon(startBtn, "GeneratedIcons/icon_bud");

            var backBtn = BuildButton("BtnTutBack", pr, T("Back"), 18);
            SetRect(backBtn.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(backBtn, "GeneratedIcons/icon_torii_lock");

            // ── Modifier hover tooltip ──
            var ttBg = EnsureRect("ModTooltipBg", pr,
                new Vector2(0.04f, 0.01f), new Vector2(0.59f, 0.09f), Vector2.zero, Vector2.zero);
            var ttImg = EnsureComponent<Image>(ttBg.gameObject);
            ttImg.color = GamePalette.ShadowDark;
            var ttTextRect = EnsureRect("ModTooltipText", ttBg,
                new Vector2(0.01f, 0f), new Vector2(0.99f, 1f), Vector2.zero, Vector2.zero);
            var ttText = EnsureComponent<Text>(ttTextRect.gameObject);
            ttText.font = GetBuiltInFont();
            ttText.fontSize = 11;
            ttText.color = TxtColor;
            ttText.alignment = TextAnchor.MiddleLeft;
            ttText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ttText.verticalOverflow = VerticalWrapMode.Overflow;
            ttBg.gameObject.SetActive(false);

            void WireTip(Toggle tgl, string desc)
            {
                var et = EnsureComponent<EventTrigger>(tgl.gameObject);
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => { ttText.text = desc; ttBg.gameObject.SetActive(true); });
                et.triggers.Add(enter);
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => ttBg.gameObject.SetActive(false));
                et.triggers.Add(exit);
            }

            WireTip(german,        "Green line: neighbours on the line must differ by 5 or more.");
            WireTip(dutch,         "Teal line: neighbours on the line must differ by 4 or more.");
            WireTip(parity,        "Cyan line: adjacent cells must strictly alternate between odd and even.");
            WireTip(renban,        "Pink line: all digits on the line form a consecutive set (any order).");
            WireTip(diff,          "White dot between cells: those two digits differ by exactly 1.");
            WireTip(ratio,         "Black dot between cells: one digit is exactly double the other.");
            WireTip(killer,        "Dashed cage: digits sum to the cage label; no repeats inside the cage.");
            WireTip(arrow,         "Arrow circles: digits on the arrow sum to the number in the circle.");
            WireTip(fog,           "Correct placements reveal hidden cells.");
            WireTip(palindrome,    "Purple line: digits read the same from either end of the line.");
            WireTip(thermo,        "Orange line with bulb: digits must strictly increase from bulb to tip.");
            WireTip(between,       "White line: every digit on the line must fall strictly between the two endpoint values.");
            WireTip(evenOdd,       "Blue square = even digit. Orange circle = odd digit.");
            WireTip(noncons,       "Global: no two orthogonally adjacent cells may contain consecutive digits.");
            WireTip(antiknight,    "Global: no two cells a chess knight's move apart may share the same digit.");
            WireTip(antiking,      "Global: no two cells a king's move apart (including diagonal) may share a digit.");
            WireTip(antibishop,    "Global: no two cells on the same diagonal may share a digit.");
            WireTip(nonconsecDiag, "Global: diagonally adjacent cells cannot be consecutive (e.g. 4 cannot touch 3 or 5 diagonally).");
            WireTip(distGe2,       "Global: equal digits must be at least 2 cells apart (no king-adjacent equal digits).");
            WireTip(entropy,       "Every 3 consecutive row/col cells must contain one low (1–3), one mid (4–6), and one high (7–9) digit.");
            WireTip(modular,       "Every box region must contain at least one digit from {1–3}, {4–6}, and {7–9}.");
            WireTip(consecLine,    "Orange line: adjacent cells on the line must differ by exactly 1 (e.g. 3-4-5).");
            WireTip(slowThermo,    "Purple line with open bulb: digits must increase or stay equal from bulb to tip (e.g. 2-2-3-5).");
            WireTip(uniqueSet,     "Sky-blue line: no digit may repeat anywhere on the line.");
            WireTip(fullKropki,    "All diff-by-1 pairs show a white dot; all 1:2 ratio pairs show a black dot. No dot = neither.");
            WireTip(sumKropki,     "Labelled dot between two cells: those cells must sum to that value.");
            WireTip(greaterLess,   "Orange chevrons (> / <) between cells indicate which digit must be larger.");
            WireTip(xvPairs,       "X between two cells: they sum to 10. V between two cells: they sum to 5.");
            WireTip(primeCells,    "Gold 'P' cells must contain a prime digit: 2, 3, 5, or 7.");
            WireTip(fortressCells, "Grey shaded cells must be strictly greater than every orthogonally adjacent unshaded cell.");

            // Wire tutorial controller — order matches TutorialMenuController.GetConfig() allModifiers (30 total)
            tutCtrl.Configure(boardDrop, starsDrop, regionDrop, startBtn,
                // Original 15: german, dutch, parity, renban, diff, ratio, killer, arrow, fog,
                //               palindrome, thermo, between, evenOdd, noncons, antiknight
                german, dutch, parity, renban, diff, ratio, killer, arrow, fog,
                palindrome, thermo, between, evenOdd, noncons, antiknight,
                // Extended 15
                antiking, antibishop, nonconsecDiag, distGe2, entropy, modular,
                consecLine, slowThermo, uniqueSet, fullKropki, sumKropki,
                greaterLess, xvPairs, primeCells, fortressCells);

            // Apply art background if the asset is available
            if (ApplyPanelBackground(panel.GetComponent<Image>(), "GeneratedIcons/ui_tutorial_setup"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;

                ttText.color = TxtColor; // tooltip sits on ShadowDark bg — keep light
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }

            panel.SetActive(false);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Options
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildOptionsPanel(RectTransform root, MainMenuController mc,
            OptionsController optCtrl)
        {
            var panel = MakePanel("OptionsPanel", root,
                new Vector2(0.18f, 0.04f), new Vector2(0.82f, 0.96f));
            var pr = GetPanelContent(panel);

            var opts = optCtrl.GetOptions();

            // ── Title ──────────────────────────────────────────────────────────
            var title = BuildText("Title", pr, T("Options"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.94f), new Vector2(0.92f, 0.99f), Vector2.zero, Vector2.zero);

            // ── Tab buttons ─────────────────────────────────────────────────────
            var tabAudio   = BuildButton("TabAudio",   pr, T("Audio"),    14);
            var tabDisplay = BuildButton("TabDisplay", pr, T("Display"),  14);
            var tabGame    = BuildButton("TabGame",    pr, T("Gameplay"), 14);
            SetRect(tabAudio.GetComponent<RectTransform>(),
                new Vector2(0.02f, 0.88f), new Vector2(0.34f, 0.94f), Vector2.zero, Vector2.zero);
            SetRect(tabDisplay.GetComponent<RectTransform>(),
                new Vector2(0.35f, 0.88f), new Vector2(0.65f, 0.94f), Vector2.zero, Vector2.zero);
            SetRect(tabGame.GetComponent<RectTransform>(),
                new Vector2(0.66f, 0.88f), new Vector2(0.98f, 0.94f), Vector2.zero, Vector2.zero);

            // ── Section containers (same rect, one visible at a time) ───────────
            RectTransform MakeSec(string name)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(pr, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.10f);
                rt.anchorMax = new Vector2(1f, 0.88f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return rt;
            }
            var audioSec   = MakeSec("AudioSection");
            var displaySec = MakeSec("DisplaySection");
            var gameSec    = MakeSec("GameSection");

            // ── Tab activation ───────────────────────────────────────────────────
            var tabs     = new Button[]         { tabAudio, tabDisplay, tabGame };
            var sections = new RectTransform[]  { audioSec, displaySec, gameSec };

            void ActivateTab(int idx)
            {
                for (var i = 0; i < 3; i++)
                {
                    sections[i].gameObject.SetActive(i == idx);
                    var tabImg = tabs[i].GetComponent<Image>();
                    var tabLbl = tabs[i].GetComponentInChildren<Text>();
                    if (tabImg != null) tabImg.color = i == idx
                        ? GamePalette.WithAlpha(GamePalette.AccentGold, 0.40f)
                        : GamePalette.WithAlpha(GamePalette.Panel, 0.80f);
                    if (tabLbl != null) tabLbl.color = i == idx
                        ? GamePalette.AccentGold
                        : GamePalette.TextMuted;
                }
            }
            tabAudio.onClick.RemoveAllListeners();
            tabAudio.onClick.AddListener(() => ActivateTab(0));
            tabDisplay.onClick.RemoveAllListeners();
            tabDisplay.onClick.AddListener(() => ActivateTab(1));
            tabGame.onClick.RemoveAllListeners();
            tabGame.onClick.AddListener(() => ActivateTab(2));

            // ── Helpers ─────────────────────────────────────────────────────────
            Button MakeResetBtn(string id, RectTransform parent, System.Action action)
            {
                var btn = BuildButton(id, parent, T("Reset"), 11);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => action());
                return btn;
            }

            (Slider slider, Text readout) BuildVolumeSlider(string id, string label,
                RectTransform parent, Vector2 aMin, Vector2 aMax, float value,
                System.Action<float> setter)
            {
                var s = BuildLabelAndSlider(parent, id, label, aMin, aMax, 0f, 1f, value,
                    v => setter(v));
                var pct = BuildText(id + "Pct", parent,
                    Mathf.RoundToInt(value * 100f) + "%", 12, TextAnchor.MiddleRight);
                SetRect(pct.rectTransform,
                    new Vector2(aMax.x - 0.09f, aMin.y), new Vector2(aMax.x, aMax.y),
                    Vector2.zero, Vector2.zero);
                pct.color = GamePalette.TextMuted;
                s.onValueChanged.AddListener(v =>
                    pct.text = Mathf.RoundToInt(v * 100f) + "%");
                return (s, pct);
            }

            // ════════════════════════════════════════════════════════════════════
            // AUDIO SECTION
            // ════════════════════════════════════════════════════════════════════
            var audioHdr = BuildText("AudioHdr", audioSec, T("Audio"), 18, TextAnchor.MiddleLeft);
            SetRect(audioHdr.rectTransform,
                new Vector2(0.05f, 0.88f), new Vector2(0.75f, 1.00f), Vector2.zero, Vector2.zero);
            var btnResetAudio = MakeResetBtn("BtnResetAudio", audioSec, () =>
            {
                optCtrl.ResetAudioToDefaults();
                mc.RefreshOptionsPanel();
            });
            SetRect(btnResetAudio.GetComponent<RectTransform>(),
                new Vector2(0.78f, 0.90f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);

            var (sMaster, _) = BuildVolumeSlider("MasterVolume", T("Master Volume"), audioSec,
                new Vector2(0.05f, 0.70f), new Vector2(0.45f, 0.88f),
                opts.Audio.MasterVolume, optCtrl.SetMasterVolume);

            var (sMusic, _) = BuildVolumeSlider("MusicVolume", T("Music Volume"), audioSec,
                new Vector2(0.52f, 0.70f), new Vector2(0.92f, 0.88f),
                opts.Audio.MusicVolume, optCtrl.SetMusicVolume);

            var (sSfx, _) = BuildVolumeSlider("SfxVolume", T("SFX Volume"), audioSec,
                new Vector2(0.05f, 0.52f), new Vector2(0.45f, 0.70f),
                opts.Audio.SfxVolume, optCtrl.SetSfxVolume);

            var (sUi, _) = BuildVolumeSlider("UiVolume", T("UI Volume"), audioSec,
                new Vector2(0.52f, 0.52f), new Vector2(0.92f, 0.70f),
                opts.Audio.UiVolume, optCtrl.SetUiVolume);

            var tMute = BuildOptionToggle(audioSec, "MuteUnfocused", T("Mute when unfocused"),
                new Vector2(0.05f, 0.37f), new Vector2(0.45f, 0.50f),
                opts.Audio.MuteWhenUnfocused, optCtrl.SetMuteWhenUnfocused);

            var tMuteAll = BuildOptionToggle(audioSec, "MuteAll", T("Mute all"),
                new Vector2(0.52f, 0.37f), new Vector2(0.92f, 0.50f),
                opts.Audio.MuteAll, optCtrl.SetMuteAll);

            // Explicit D-pad nav: Master → SFX → MuteUnfocused; Music → UI → MuteAll
            LinkSliderNav(sMaster, sSfx);
            LinkSliderNav(sSfx, tMute);
            LinkSliderNav(sMusic, sUi);
            LinkSliderNav(sUi, tMuteAll);

            // ════════════════════════════════════════════════════════════════════
            // DISPLAY SECTION
            // ════════════════════════════════════════════════════════════════════
            var displayHdr = BuildText("DisplayHdr", displaySec, T("Display"), 18, TextAnchor.MiddleLeft);
            SetRect(displayHdr.rectTransform,
                new Vector2(0.05f, 0.88f), new Vector2(0.75f, 1.00f), Vector2.zero, Vector2.zero);
            var btnResetDisplay = MakeResetBtn("BtnResetDisplay", displaySec, () =>
            {
                optCtrl.ResetGraphicsToDefaults();
                mc.RefreshOptionsPanel();
            });
            SetRect(btnResetDisplay.GetComponent<RectTransform>(),
                new Vector2(0.78f, 0.90f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);

            var tFullscreen = BuildOptionToggle(displaySec, "Fullscreen", T("Fullscreen"),
                new Vector2(0.05f, 0.74f), new Vector2(0.45f, 0.87f),
                opts.Graphics.Fullscreen, optCtrl.SetFullscreen);

            var tVSync = BuildOptionToggle(displaySec, "VSync", T("VSync"),
                new Vector2(0.52f, 0.74f), new Vector2(0.92f, 0.87f),
                opts.Graphics.VSync, optCtrl.SetVSync);

            var tScreenShake = BuildOptionToggle(displaySec, "ScreenShake", T("Screen Shake"),
                new Vector2(0.05f, 0.59f), new Vector2(0.45f, 0.72f),
                opts.Graphics.ScreenShake, optCtrl.SetScreenShake);

            var (sParticle, _) = BuildVolumeSlider("ParticleIntensity", T("Particles"), displaySec,
                new Vector2(0.52f, 0.59f), new Vector2(0.92f, 0.72f),
                opts.Graphics.ParticleIntensity, optCtrl.SetParticleIntensity);

            // Resolution row
            var resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
            {
                resolutions = new[]
                {
                    new Resolution { width = 1280, height = 720  },
                    new Resolution { width = 1920, height = 1080 },
                    new Resolution { width = 2560, height = 1440 },
                    new Resolution { width = 3840, height = 2160 },
                };
            }
            var resLabel = BuildText("ResLabel", displaySec, T("Resolution"), 14, TextAnchor.MiddleLeft);
            SetRect(resLabel.rectTransform,
                new Vector2(0.05f, 0.47f), new Vector2(0.22f, 0.57f), Vector2.zero, Vector2.zero);
            var resDrop = BuildDropdown("ResolutionDropdown", displaySec);
            SetRect(resDrop.GetComponent<RectTransform>(),
                new Vector2(0.23f, 0.45f), new Vector2(0.96f, 0.57f), Vector2.zero, Vector2.zero);
            resDrop.ClearOptions();
            var resOpts   = new System.Collections.Generic.List<string>();
            var curW      = optCtrl.GetOptions().Graphics.ResolutionWidth;
            var curH      = optCtrl.GetOptions().Graphics.ResolutionHeight;
            var curResIdx = 0;
            for (var i = 0; i < resolutions.Length; i++)
            {
                var r = resolutions[i];
                resOpts.Add($"{r.width} × {r.height}");
                if (r.width == curW && r.height == curH) curResIdx = i;
            }
            resDrop.AddOptions(resOpts);
            resDrop.SetValueWithoutNotify(curResIdx);
            resDrop.onValueChanged.RemoveAllListeners();
            resDrop.onValueChanged.AddListener(idx =>
            {
                var r = resolutions[idx];
                optCtrl.SetResolution(r.width, r.height);
            });

            // Language row
            var langLabel = BuildText("LangLabel", displaySec, T("Language"), 14, TextAnchor.MiddleLeft);
            SetRect(langLabel.rectTransform,
                new Vector2(0.05f, 0.31f), new Vector2(0.22f, 0.43f), Vector2.zero, Vector2.zero);
            var langDrop = BuildDropdown("LanguageDropdown", displaySec);
            SetRect(langDrop.GetComponent<RectTransform>(),
                new Vector2(0.23f, 0.29f), new Vector2(0.65f, 0.43f), Vector2.zero, Vector2.zero);
            langDrop.ClearOptions();
            langDrop.AddOptions(new System.Collections.Generic.List<string> { "English", "Deutsch" });
            FitDropdownToItems(langDrop);
            var langItem = langDrop.template?.Find("Viewport/Content/Item");
            if (langItem != null)
            {
                var le = langItem.GetComponent<LayoutElement>();
                if (le != null) le.minHeight = 50f;
                var lbl = langItem.Find("Item Label")?.GetComponent<Text>();
                if (lbl != null) lbl.fontSize = 20;
            }
            langDrop.template.sizeDelta = new Vector2(langDrop.template.sizeDelta.x,
                Mathf.Min(langDrop.options.Count * 54f + 8f, 300f));
            langDrop.SetValueWithoutNotify((int)opts.Language);

            // Explicit D-pad nav: Fullscreen → ScreenShake → resDrop → langDrop
            //                     VSync → ParticleIntensity (right column, partial chain)
            LinkSelectableNav(tFullscreen, tScreenShake);
            LinkSelectableNav(tScreenShake, resDrop);
            LinkSelectableNav(resDrop, langDrop);
            LinkSelectableNav(tVSync, sParticle);

            // Language confirmation row (initially hidden)
            var langConfirmRow = new GameObject("LangConfirmRow", typeof(RectTransform));
            langConfirmRow.transform.SetParent(displaySec, false);
            var langConfirmRt = langConfirmRow.GetComponent<RectTransform>();
            langConfirmRt.anchorMin = new Vector2(0.05f, 0.14f);
            langConfirmRt.anchorMax = new Vector2(0.96f, 0.27f);
            langConfirmRt.offsetMin = Vector2.zero;
            langConfirmRt.offsetMax = Vector2.zero;

            var langWarnText = BuildText("LangWarnText", langConfirmRt,
                "Apply language change? Menu will rebuild.", 12, TextAnchor.MiddleLeft);
            SetRect(langWarnText.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f),
                Vector2.zero, Vector2.zero);
            langWarnText.color = GamePalette.AccentGold;

            var btnLangApply = BuildButton("BtnLangApply", langConfirmRt, T("Apply"), 12);
            SetRect(btnLangApply.GetComponent<RectTransform>(),
                new Vector2(0.57f, 0.05f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero);
            var btnLangCancel = BuildButton("BtnLangCancel", langConfirmRt, T("Cancel"), 12);
            SetRect(btnLangCancel.GetComponent<RectTransform>(),
                new Vector2(0.77f, 0.05f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);

            langConfirmRow.SetActive(false);
            var pendingLangIdx = (int)opts.Language;

            langDrop.onValueChanged.RemoveAllListeners();
            langDrop.onValueChanged.AddListener(idx =>
            {
                pendingLangIdx = idx;
                langConfirmRow.SetActive(idx != (int)optCtrl.GetOptions().Language);
            });
            btnLangApply.onClick.RemoveAllListeners();
            btnLangApply.onClick.AddListener(() =>
            {
                optCtrl.SetLanguage((LanguageOption)pendingLangIdx);
                langConfirmRow.SetActive(false);
                mc.RefreshLanguage();
            });
            btnLangCancel.onClick.RemoveAllListeners();
            btnLangCancel.onClick.AddListener(() =>
            {
                langDrop.SetValueWithoutNotify((int)optCtrl.GetOptions().Language);
                langConfirmRow.SetActive(false);
            });

            // ════════════════════════════════════════════════════════════════════
            // GAMEPLAY SECTION
            // ════════════════════════════════════════════════════════════════════
            var gameHdr = BuildText("GameHdr", gameSec, T("Gameplay"), 18, TextAnchor.MiddleLeft);
            SetRect(gameHdr.rectTransform,
                new Vector2(0.05f, 0.88f), new Vector2(0.75f, 1.00f), Vector2.zero, Vector2.zero);
            var btnResetGame = MakeResetBtn("BtnResetGameplay", gameSec, () =>
            {
                optCtrl.ResetGameplayToDefaults();
                mc.RefreshOptionsPanel();
            });
            SetRect(btnResetGame.GetComponent<RectTransform>(),
                new Vector2(0.78f, 0.90f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);

            var tHighlight = BuildOptionToggle(gameSec, "HighlightErrors", T("Highlight Errors"),
                new Vector2(0.05f, 0.74f), new Vector2(0.45f, 0.87f),
                opts.Gameplay.HighlightConflicts, optCtrl.SetHighlightConflicts);

            var tConfirmWrong = BuildOptionToggle(gameSec, "ConfirmWrong", T("Confirm Wrong Placement"),
                new Vector2(0.52f, 0.74f), new Vector2(0.96f, 0.87f),
                opts.Gameplay.ConfirmBeforeWrongPlacement, optCtrl.SetConfirmBeforeWrongPlacement);

            var tAutoPencil = BuildOptionToggle(gameSec, "AutoPencil", T("Auto Pencil Cleanup"),
                new Vector2(0.05f, 0.58f), new Vector2(0.45f, 0.71f),
                opts.Gameplay.AutoPencilCleanup, optCtrl.SetAutoPencilCleanup);

            var tCandidates = BuildOptionToggle(gameSec, "CandidateCount", T("Show Candidate Count"),
                new Vector2(0.52f, 0.58f), new Vector2(0.96f, 0.71f),
                opts.Gameplay.ShowCandidateCount, optCtrl.SetShowCandidateCount);

            var tCursorSnap = BuildOptionToggle(gameSec, "CursorSnap", T("Cursor Snap"),
                new Vector2(0.05f, 0.42f), new Vector2(0.45f, 0.55f),
                opts.Gameplay.CursorSnapOnSelection, optCtrl.SetCursorSnapOnSelection);

            var tDoubleTap = BuildOptionToggle(gameSec, "DoubleTap", T("Double-Tap to Confirm"),
                new Vector2(0.52f, 0.42f), new Vector2(0.96f, 0.55f),
                opts.Gameplay.DoubleTapConfirmNumberEntry, optCtrl.SetDoubleTapConfirm);

            var tRumble = BuildOptionToggle(gameSec, "ControllerRumble", T("Controller Rumble"),
                new Vector2(0.05f, 0.26f), new Vector2(0.45f, 0.39f),
                opts.Gameplay.ControllerRumbleEnabled, optCtrl.SetControllerRumbleEnabled);

            // Explicit D-pad nav left col: HighlightErrors → AutoPencil → CursorSnap → ControllerRumble
            // Explicit D-pad nav right col: ConfirmWrong → CandidateCount → DoubleTap
            LinkSelectableNav(tHighlight, tAutoPencil);
            LinkSelectableNav(tAutoPencil, tCursorSnap);
            LinkSelectableNav(tCursorSnap, tRumble);
            LinkSelectableNav(tConfirmWrong, tCandidates);
            LinkSelectableNav(tCandidates, tDoubleTap);

            // ── Bottom row: Back, Tutorial, Controls & Accessibility ────────────
            var ctrlHdr = BuildText("CtrlHdr", pr, T("Controls & Accessibility"), 14, TextAnchor.MiddleLeft);
            SetRect(ctrlHdr.rectTransform,
                new Vector2(0.05f, 0.05f), new Vector2(0.40f, 0.09f), Vector2.zero, Vector2.zero);
            ctrlHdr.color = GamePalette.TextMuted;

            var btnKeybinds = BuildButton("BtnKeybindings", pr, T("Keybindings..."), 13);
            SetRect(btnKeybinds.GetComponent<RectTransform>(),
                new Vector2(0.43f, 0.05f), new Vector2(0.65f, 0.09f), Vector2.zero, Vector2.zero);
            btnKeybinds.onClick.RemoveAllListeners();
            btnKeybinds.onClick.AddListener(mc.OpenKeybindings);

            var btnAccess = BuildButton("BtnAccessibility", pr, T("Accessibility..."), 13);
            SetRect(btnAccess.GetComponent<RectTransform>(),
                new Vector2(0.67f, 0.05f), new Vector2(0.96f, 0.09f), Vector2.zero, Vector2.zero);
            btnAccess.onClick.RemoveAllListeners();
            btnAccess.onClick.AddListener(mc.OpenAccessibility);

            var back = BuildButton("BtnOptBack", pr, T("Back"), 16);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.05f, 0.01f), new Vector2(0.35f, 0.05f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            var btnBasicsTut = BuildButton("BtnReplayBasicsTutorial", pr, T("Sudoku Tutorial"), 13);
            SetRect(btnBasicsTut.GetComponent<RectTransform>(),
                new Vector2(0.37f, 0.01f), new Vector2(0.65f, 0.05f), Vector2.zero, Vector2.zero);
            btnBasicsTut.onClick.RemoveAllListeners();
            btnBasicsTut.onClick.AddListener(mc.LaunchSudokuBasicsTutorial);

            // ── Slot-change refresh ──────────────────────────────────────────────
            optCtrl.OnSlotChanged += () =>
            {
                var o = optCtrl.GetOptions();
                sMaster.SetValueWithoutNotify(o.Audio.MasterVolume);
                sMusic.SetValueWithoutNotify(o.Audio.MusicVolume);
                sSfx.SetValueWithoutNotify(o.Audio.SfxVolume);
                sUi.SetValueWithoutNotify(o.Audio.UiVolume);
                tMute.SetIsOnWithoutNotify(o.Audio.MuteWhenUnfocused);
                tMuteAll.SetIsOnWithoutNotify(o.Audio.MuteAll);
                tFullscreen.SetIsOnWithoutNotify(o.Graphics.Fullscreen);
                tVSync.SetIsOnWithoutNotify(o.Graphics.VSync);
                tScreenShake.SetIsOnWithoutNotify(o.Graphics.ScreenShake);
                sParticle.SetValueWithoutNotify(o.Graphics.ParticleIntensity);
                var newResIdx = 0;
                for (var i = 0; i < resolutions.Length; i++)
                    if (resolutions[i].width == o.Graphics.ResolutionWidth && resolutions[i].height == o.Graphics.ResolutionHeight) { newResIdx = i; break; }
                resDrop.SetValueWithoutNotify(newResIdx);
                langDrop.SetValueWithoutNotify((int)o.Language);
                langConfirmRow.SetActive(false);
                tHighlight.SetIsOnWithoutNotify(o.Gameplay.HighlightConflicts);
                tConfirmWrong.SetIsOnWithoutNotify(o.Gameplay.ConfirmBeforeWrongPlacement);
                tAutoPencil.SetIsOnWithoutNotify(o.Gameplay.AutoPencilCleanup);
                tCandidates.SetIsOnWithoutNotify(o.Gameplay.ShowCandidateCount);
                tCursorSnap.SetIsOnWithoutNotify(o.Gameplay.CursorSnapOnSelection);
                tDoubleTap.SetIsOnWithoutNotify(o.Gameplay.DoubleTapConfirmNumberEntry);
                tRumble.SetIsOnWithoutNotify(o.Gameplay.ControllerRumbleEnabled);
            };

            // Start on Audio tab
            ActivateTab(0);

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "GeneratedIcons/ui_options"))
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

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Credits
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildCreditsPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("CreditsPanel", root,
                new Vector2(0.33f, 0.22f), new Vector2(0.67f, 0.78f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Credits", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.1f, 0.80f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

            var body = BuildText("Body", pr,
                "Run of the Nine\nDesign + Systems + UI Wiring", 18, TextAnchor.MiddleCenter);
            SetRect(body.rectTransform,
                new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.74f), Vector2.zero, Vector2.zero);

            var back = BuildButton("BtnCredBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            panel.SetActive(false);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Meta Progression
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildMetaProgressionPanel(RectTransform root, MainMenuController mc,
            MetaProgressionPanelController metaCtrl)
        {
            var panel = MakePanel("MetaProgressionPanel", root,
                new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Meta Progression", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var summary = BuildText("SummaryText", pr, "", 17, TextAnchor.UpperLeft);
            SetRect(summary.rectTransform,
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            var scrollContent = BuildScrollArea("MetaScroll", pr,
                new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.80f));

            var back = BuildButton("BtnMetaBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            metaCtrl.Configure(scrollContent, summary);

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "GeneratedIcons/ui_meta_progression"))
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

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Game Modes
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildGameModesPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("GameModesPanel", root,
                new Vector2(0.28f, 0.16f), new Vector2(0.72f, 0.84f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Game Modes", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            var desc = BuildText("Desc", pr,
                "Garden Run: Standard roguelike with shops & bosses.\n\n" +
                "Endless Zen: Relaxed mode, infinite puzzles, no HP.\n\n" +
                "Spirit Trials: Challenge tiers with ranked modifiers.\n\n" +
                "Daily Walk: Three goals per day for rewards.\n\n" +
                "Monthly Walk: One shared puzzle for the whole month.",
                15, TextAnchor.UpperLeft);
            SetRect(desc.rectTransform,
                new Vector2(0.10f, 0.44f), new Vector2(0.90f, 0.82f), Vector2.zero, Vector2.zero);

            var garden = BuildButton("BtnGarden", pr, "Start Garden Run", 18);
            SetRect(garden.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.35f), new Vector2(0.88f, 0.43f), Vector2.zero, Vector2.zero);
            garden.onClick.RemoveAllListeners();
            garden.onClick.AddListener(mc.StartGame);
            ApplyMenuButtonIcon(garden, "GeneratedIcons/icon_bud");

            var endless = BuildButton("BtnEndless", pr, "Start Endless Zen", 18);
            SetRect(endless.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.26f), new Vector2(0.88f, 0.34f), Vector2.zero, Vector2.zero);
            endless.onClick.RemoveAllListeners();
            endless.onClick.AddListener(mc.StartEndlessZen);
            ApplyMenuButtonIcon(endless, "GeneratedIcons/icon_infinite_lotus");

            var trials = BuildButton("BtnTrials", pr, "Start Spirit Trials", 18);
            SetRect(trials.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.17f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
            trials.onClick.RemoveAllListeners();
            trials.onClick.AddListener(mc.StartSpiritTrials);
            ApplyMenuButtonIcon(trials, "GeneratedIcons/icon_temple_seal");

            var daily = BuildButton("BtnDailyWalk", pr, "Daily Walk", 18);
            SetRect(daily.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.09f), new Vector2(0.49f, 0.16f), Vector2.zero, Vector2.zero);
            daily.onClick.RemoveAllListeners();
            daily.onClick.AddListener(mc.ShowDailyWalkPanel);
            ApplyMenuButtonIcon(daily, "GeneratedIcons/icon_bud");

            var monthly = BuildButton("BtnMonthlyWalk", pr, "Monthly Walk", 18);
            SetRect(monthly.GetComponent<RectTransform>(),
                new Vector2(0.51f, 0.09f), new Vector2(0.88f, 0.16f), Vector2.zero, Vector2.zero);
            monthly.onClick.RemoveAllListeners();
            monthly.onClick.AddListener(mc.ShowMonthlyWalkPanel);
            ApplyMenuButtonIcon(monthly, "GeneratedIcons/icon_lantern_of_clarity");

            var back = BuildButton("BtnModesBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            // Apply art background if the asset is available
            if (ApplyPanelBackground(panel.GetComponent<Image>(), "GeneratedIcons/ui_game_modes"))
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

        private GameObject BuildDailyWalkPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("DailyWalkPanel", root,
                new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Daily Walk", 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            var streakLabel = BuildText("StreakLabel", pr, "Streak: —", 18, TextAnchor.MiddleCenter);
            SetRect(streakLabel.rectTransform,
                new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero);
            streakLabel.name = "StreakLabel";

            var goal0 = BuildText("Goal0", pr, "Goal 1: —", 15, TextAnchor.MiddleLeft);
            SetRect(goal0.rectTransform,
                new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.69f), Vector2.zero, Vector2.zero);

            var goal1 = BuildText("Goal1", pr, "Goal 2: —", 15, TextAnchor.MiddleLeft);
            SetRect(goal1.rectTransform,
                new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.61f), Vector2.zero, Vector2.zero);

            var goal2 = BuildText("Goal2", pr, "Goal 3: —", 15, TextAnchor.MiddleLeft);
            SetRect(goal2.rectTransform,
                new Vector2(0.06f, 0.46f), new Vector2(0.94f, 0.53f), Vector2.zero, Vector2.zero);

            var stamps = BuildText("StampsLabel", pr, "Ink Stamps: 0", 16, TextAnchor.MiddleCenter);
            SetRect(stamps.rectTransform,
                new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.44f), Vector2.zero, Vector2.zero);
            stamps.name = "StampsLabel";

            var back = BuildButton("BtnDailyBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.ShowGameModes);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildMonthlyWalkPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("MonthlyWalkPanel", root,
                new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Monthly Walk", 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            var themeLabel = BuildText("ThemeLabel", pr, "Loading theme…", 20, TextAnchor.MiddleCenter);
            SetRect(themeLabel.rectTransform,
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
            themeLabel.name = "MonthlyThemeLabel";

            var subtitleLabel = BuildText("SubtitleLabel", pr, "", 15, TextAnchor.MiddleCenter);
            SetRect(subtitleLabel.rectTransform,
                new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.77f), Vector2.zero, Vector2.zero);
            subtitleLabel.name = "MonthlySubtitleLabel";

            var bestLabel = BuildText("BestLabel", pr, "Personal Best: —", 16, TextAnchor.MiddleCenter);
            SetRect(bestLabel.rectTransform,
                new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.69f), Vector2.zero, Vector2.zero);
            bestLabel.name = "MonthlyBestLabel";

            var bestBreakdownLabel = BuildText("BestBreakdownLabel", pr, "", 14, TextAnchor.MiddleCenter);
            SetRect(bestBreakdownLabel.rectTransform,
                new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
            bestBreakdownLabel.name = "MonthlyBestBreakdownLabel";

            var countdownLabel = BuildText("CountdownLabel", pr, "", 15, TextAnchor.MiddleCenter);
            SetRect(countdownLabel.rectTransform,
                new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.54f), Vector2.zero, Vector2.zero);
            countdownLabel.name = "MonthlyCountdownLabel";

            var begin = BuildButton("BtnBeginMonthly", pr, "Begin Monthly Walk", 18);
            SetRect(begin.GetComponent<RectTransform>(),
                new Vector2(0.16f, 0.28f), new Vector2(0.84f, 0.38f), Vector2.zero, Vector2.zero);
            begin.onClick.RemoveAllListeners();
            begin.onClick.AddListener(mc.LaunchSeasonalChallenge);
            ApplyMenuButtonIcon(begin, "GeneratedIcons/icon_lantern_of_clarity");
            begin.name = "BtnBeginMonthly";

            var back = BuildButton("BtnMonthlyBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.ShowGameModes);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            panel.SetActive(false);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Items Codex
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildItemsPanel(RectTransform root, MainMenuController mc,
            ItemsMenuController itemsCtrl)
        {
            var panel = MakePanel("ItemsPanel", root,
                new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Items & Relic Codex", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var scrollContent = BuildScrollArea("ItemsScroll", pr,
                new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.88f));

            var back = BuildButton("BtnItemsBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            itemsCtrl.Configure(scrollContent);

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "GeneratedIcons/ui_items_menu"))
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

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Keybindings
        // ════════════════════════════════════════════════════════════════════════

        private static readonly (InputAction Action, string Label)[] RebindableActions =
        {
            (InputAction.MoveUp,         "Move Up"),
            (InputAction.MoveDown,       "Move Down"),
            (InputAction.MoveLeft,       "Move Left"),
            (InputAction.MoveRight,      "Move Right"),
            (InputAction.TogglePencil,   "Toggle Pencil"),
            (InputAction.ClearCell,      "Clear Cell"),
            (InputAction.UndoLastDigit,  "Undo Last Digit"),
            (InputAction.HighlightDigit, "Highlight Digit"),
            (InputAction.BagNext,        "Bag Next Slot"),
            (InputAction.BagPrevious,    "Bag Prev Slot"),
            (InputAction.UseItem,        "Use Item"),
            (InputAction.InspectItem,    "Inspect Item"),
            (InputAction.Confirm,        "Confirm"),
            (InputAction.Cancel,         "Cancel"),
            (InputAction.SkipAnimation,  "Skip Animation"),
            (InputAction.TabNext,        "Tab Next"),
            (InputAction.TabPrevious,    "Tab Previous"),
        };

        private GameObject BuildKeybindingsPanel(RectTransform root, MainMenuController mc,
            KeybindingsPanelController keybindCtrl)
        {
            var panel = MakePanel("KeybindingsPanel", root,
                new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.92f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Keybindings", 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            title.color = TxtColor;

            var hint = BuildText("Hint", pr,
                "Click Rebind, then press any key. Gamepad supported. Esc to cancel. " +
                "Digit keys 1\u20139 are fixed (use on-screen numpad for gamepad).",
                11, TextAnchor.UpperCenter);
            SetRect(hint.rectTransform,
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.91f), Vector2.zero, Vector2.zero);
            hint.color = GamePalette.TextMuted;

            // Column headers
            var hdrAction = BuildText("HdrAction", pr, "Action", 14, TextAnchor.MiddleLeft);
            SetRect(hdrAction.rectTransform,
                new Vector2(0.05f, 0.83f), new Vector2(0.38f, 0.88f), Vector2.zero, Vector2.zero);
            hdrAction.color = GamePalette.AccentGold;

            var hdrKey = BuildText("HdrKey", pr, "Key", 14, TextAnchor.MiddleCenter);
            SetRect(hdrKey.rectTransform,
                new Vector2(0.40f, 0.83f), new Vector2(0.62f, 0.88f), Vector2.zero, Vector2.zero);
            hdrKey.color = GamePalette.AccentGold;

            var hdrPad = BuildText("HdrPad", pr, "Gamepad", 14, TextAnchor.MiddleCenter);
            SetRect(hdrPad.rectTransform,
                new Vector2(0.64f, 0.83f), new Vector2(0.82f, 0.88f), Vector2.zero, Vector2.zero);
            hdrPad.color = GamePalette.AccentGold;

            // Rows — inside a scroll area so overflow is handled gracefully
            var rowCount = RebindableActions.Length;

            // Detect connected controller type for friendly gamepad labels
            var ctrlPlatform = ControllerGlyphService.Detect();
            bool isPs = ctrlPlatform == ControllerPlatform.PlayStation;

            string PadLabel(KeyCode key)
            {
                if (key == KeyCode.None) return "—";
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
                var (action, label) = RebindableActions[i];
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
                var actionLbl = BuildText($"Action_{i}", rowBg, label, 13, TextAnchor.MiddleLeft);
                SetRect(actionLbl.rectTransform,
                    new Vector2(0.02f, 0.05f), new Vector2(0.38f, 0.95f), Vector2.zero, Vector2.zero);
                actionLbl.color = TxtColor;

                // Current key label
                var defaultKey = InputRemapService.GetDefault(action);
                var keyDisplay = IsUiOnly(action) ? "(UI only)" : InputRemapService.FormatKeyCode(defaultKey);
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
                var btnRebind = BuildButton($"BtnRebind_{i}", rowBg, "Rebind", 11);
                SetRect(btnRebind.GetComponent<RectTransform>(),
                    new Vector2(0.84f, 0.08f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);
                btnRebind.interactable = !IsUiOnly(action);
                btnRebind.onClick.RemoveAllListeners();
                btnRebind.onClick.AddListener(() => keybindCtrl.StartListening(capturedAction));
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
            var btnReset = BuildButton("BtnResetAll", pr, "Reset All", 14);
            SetRect(btnReset.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.01f), new Vector2(0.30f, 0.09f), Vector2.zero, Vector2.zero);
            btnReset.onClick.RemoveAllListeners();
            btnReset.onClick.AddListener(keybindCtrl.ResetAll);

            // Back
            var back = BuildButton("BtnKeybindBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackFromPanel);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            panel.SetActive(false);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Accessibility (descriptions + live preview board)
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildAccessibilityPanel(RectTransform root, MainMenuController mc,
            OptionsController optCtrl)
        {
            var panel = MakePanel("AccessibilityPanel", root,
                new Vector2(0.18f, 0.04f), new Vector2(0.82f, 0.96f));
            var pr = GetPanelContent(panel);
            var opts = optCtrl.GetOptions();

            // ── Title ──
            var title = BuildText("Title", pr, T("Accessibility"), 26, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);

            // ── Left column: toggles with description labels ──────────────────

            // Helper: description label beneath toggle
            Text MakeDesc(RectTransform parent, string name, string text,
                Vector2 min, Vector2 max)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = min; rt.anchorMax = max;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var t = go.GetComponent<Text>();
                t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize  = 11;
                t.alignment = TextAnchor.UpperLeft;
                t.color     = new Color(0.80f, 0.78f, 0.68f, 0.85f);
                t.text      = text;
                return t;
            }

            // Pair 1 — Colorblind Mode
            var tColorblind = BuildOptionToggle(pr, "Colorblind", T("Colorblind Mode"),
                new Vector2(0.04f, 0.82f), new Vector2(0.48f, 0.87f),
                opts.Accessibility.ColorblindMode, optCtrl.SetColorblindMode);
            MakeDesc(pr, "DescColorblind",
                "Recolours conflict cells to cyan\nfor red-green colour blindness.",
                new Vector2(0.06f, 0.76f), new Vector2(0.47f, 0.82f));

            // Pair 2 — High Contrast
            var tHighContrast = BuildOptionToggle(pr, "HighContrast", T("High Contrast"),
                new Vector2(0.04f, 0.68f), new Vector2(0.48f, 0.73f),
                opts.Accessibility.HighContrastMode, optCtrl.SetHighContrast);
            MakeDesc(pr, "DescHighContrast",
                "Brightens given cells and conflict\ncolours for dim or low-contrast displays.",
                new Vector2(0.06f, 0.62f), new Vector2(0.47f, 0.68f));

            // Pair 3 — Reduce Motion
            var tReduceMotion = BuildOptionToggle(pr, "ReduceMotion", T("Reduce Motion"),
                new Vector2(0.04f, 0.54f), new Vector2(0.48f, 0.59f),
                opts.Accessibility.ReduceMotion, optCtrl.SetReduceMotion);
            MakeDesc(pr, "DescReduceMotion",
                "Disables all UI animations\nand overlay transitions.",
                new Vector2(0.06f, 0.48f), new Vector2(0.47f, 0.54f));

            // Pair 4 — Alt Constraint Symbols
            var tAltSymbols = BuildOptionToggle(pr, "AltSymbols", T("Alt Constraint Symbols"),
                new Vector2(0.04f, 0.40f), new Vector2(0.48f, 0.45f),
                opts.Accessibility.AlternativeConstraintSymbols, optCtrl.SetAltSymbols);
            MakeDesc(pr, "DescAltSymbols",
                "Shows text labels for constraint\ntypes instead of graphical icons.",
                new Vector2(0.06f, 0.34f), new Vector2(0.47f, 0.40f));

            // Pair 5 — Text Size (FontScale) slider
            var fontScaleLbl = BuildText("FontScaleLabel", pr, T("Text Size"), 15, TextAnchor.MiddleLeft);
            SetRect(fontScaleLbl.rectTransform,
                new Vector2(0.06f, 0.29f), new Vector2(0.47f, 0.34f), Vector2.zero, Vector2.zero);
            var fontScaleSlider = BuildSlider("FontScaleSlider", pr);
            SetRect(fontScaleSlider.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.24f), new Vector2(0.42f, 0.29f), Vector2.zero, Vector2.zero);
            fontScaleSlider.minValue = 0.8f;
            fontScaleSlider.maxValue = 1.5f;
            fontScaleSlider.SetValueWithoutNotify(opts.Accessibility.FontScale);
            fontScaleSlider.onValueChanged.RemoveAllListeners();
            fontScaleSlider.onValueChanged.AddListener(optCtrl.SetFontScale);
            var fontScalePct = BuildText("FontScalePct", pr,
                Mathf.RoundToInt(opts.Accessibility.FontScale * 100f) + "%",
                12, TextAnchor.MiddleLeft);
            SetRect(fontScalePct.rectTransform,
                new Vector2(0.43f, 0.24f), new Vector2(0.48f, 0.29f), Vector2.zero, Vector2.zero);
            fontScalePct.color = GamePalette.TextMuted;
            fontScaleSlider.onValueChanged.AddListener(v =>
                fontScalePct.text = Mathf.RoundToInt(v * 100f) + "%");

            // ── Right column: preview board ────────────────────────────────────

            var previewLabel = BuildText("PreviewLabel", pr, "Preview", 13, TextAnchor.MiddleCenter);
            SetRect(previewLabel.rectTransform,
                new Vector2(0.53f, 0.89f), new Vector2(0.96f, 0.93f), Vector2.zero, Vector2.zero);
            previewLabel.color = new Color(0.80f, 0.78f, 0.68f, 0.75f);

            var previewGo = new GameObject("AccessPreview", typeof(RectTransform));
            previewGo.transform.SetParent(pr, false);
            var previewRt = previewGo.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0.53f, 0.22f);
            previewRt.anchorMax = new Vector2(0.96f, 0.88f);
            previewRt.offsetMin = Vector2.zero;
            previewRt.offsetMax = Vector2.zero;
            var previewCtrl = previewGo.AddComponent<AccessibilityPreviewController>();
            previewCtrl.Configure(optCtrl);

            // ── Explicit nav order (left column, top-to-bottom loop) ───────────
            void LinkNav(Toggle from, Toggle to)
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
            LinkNav(tColorblind, tHighContrast);
            LinkNav(tHighContrast, tReduceMotion);
            LinkNav(tReduceMotion, tAltSymbols);

            // ── Sync on slot change ────────────────────────────────────────────
            optCtrl.OnSlotChanged += () =>
            {
                var o = optCtrl.GetOptions();
                tColorblind.SetIsOnWithoutNotify(o.Accessibility.ColorblindMode);
                tHighContrast.SetIsOnWithoutNotify(o.Accessibility.HighContrastMode);
                tReduceMotion.SetIsOnWithoutNotify(o.Accessibility.ReduceMotion);
                tAltSymbols.SetIsOnWithoutNotify(o.Accessibility.AlternativeConstraintSymbols);
                fontScaleSlider.SetValueWithoutNotify(o.Accessibility.FontScale);
                fontScalePct.text = Mathf.RoundToInt(o.Accessibility.FontScale * 100f) + "%";
            };

            // ── Screen Reader stub (non-interactive — backend not yet implemented) ──
            var tScreenReader = BuildOptionToggle(pr, "ScreenReader", "Screen Reader",
                new Vector2(0.04f, 0.18f), new Vector2(0.48f, 0.23f),
                opts.Accessibility.ScreenReaderEnabled, _ => { });
            tScreenReader.interactable = false;
            MakeDesc(pr, "DescScreenReader",
                "Narrates UI elements aloud. (Coming soon.)",
                new Vector2(0.06f, 0.12f), new Vector2(0.47f, 0.18f));

            // ── Reset to Defaults ──────────────────────────────────────────────
            var btnResetAcc = BuildButton("BtnResetAcc", pr, T("Reset to Defaults"), 13);
            SetRect(btnResetAcc.GetComponent<RectTransform>(),
                new Vector2(0.04f, 0.01f), new Vector2(0.48f, 0.09f), Vector2.zero, Vector2.zero);
            btnResetAcc.onClick.RemoveAllListeners();
            btnResetAcc.onClick.AddListener(() =>
            {
                optCtrl.ResetAccessibilityToDefaults();
                mc.RefreshOptionsPanel();
            });

            // ── Back ──────────────────────────────────────────────────────────
            var back = BuildButton("BtnAccBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackFromPanel);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            panel.SetActive(false);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Panel: Profile Select (3 save slots)
        // ════════════════════════════════════════════════════════════════════════

        private GameObject BuildProfileSelectPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("ProfileSelectPanel", root,
                new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, "Select Profile", 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);
            title.color = TxtColor;

            var subtitle = BuildText("Subtitle", pr, "Choose a profile to play. Up to 3 profiles supported.", 14, TextAnchor.UpperCenter);
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
            // Load → Delete vertical within each card
            for (var i = 0; i < 3; i++)
                if (loadBtns[i] != null && delBtns[i] != null)
                    LinkSelectableNav(loadBtns[i], delBtns[i]);

            // Back
            var back = BuildButton("BtnProfileBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

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
            var slotLabel = BuildText($"SlotLabel_{slotIndex}", card, $"Profile {slotIndex + 1}", 18, TextAnchor.UpperCenter);
            SetRect(slotLabel.rectTransform,
                new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            slotLabel.color = TxtColor;

            // Info text (class / runs / last played — populated at runtime by mc.RefreshProfileSelect)
            var info = BuildText($"SlotInfo_{slotIndex}", card, "Loading...", 12, TextAnchor.UpperCenter);
            SetRect(info.rectTransform,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            info.color = GamePalette.TextMuted;

            // Load / New button
            var btnLoad = BuildButton($"BtnLoad_{slotIndex}", card, "Load", 14);
            SetRect(btnLoad.GetComponent<RectTransform>(),
                new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.40f), Vector2.zero, Vector2.zero);
            var capturedSlot = slotIndex;
            btnLoad.onClick.RemoveAllListeners();
            btnLoad.onClick.AddListener(() => mc.SelectProfileSlot(capturedSlot));

            // Delete button
            var btnDelete = BuildButton($"BtnDelete_{slotIndex}", card, "Delete", 12);
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

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: Composite Builders
        // ════════════════════════════════════════════════════════════════════════

        private GameObject MakePanel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            // Full-screen opaque overlay so submenus are not transparent over the main menu
            var go = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(go, BgColor);

            // Inner card — same proportions as the main menu card
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
            UnityEngine.Events.UnityAction<bool> onChanged)
        {
            var tgl = BuildToggle(id + "Toggle", parent, label);
            SetRect(tgl.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            tgl.SetIsOnWithoutNotify(value);
            tgl.onValueChanged.RemoveAllListeners();
            tgl.onValueChanged.AddListener(onChanged);
            return tgl;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: Menu Button
        // ════════════════════════════════════════════════════════════════════════

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
            colors.selectedColor    = new Color(0.98f, 0.83f, 0.26f, 0.30f); // gold tint when controller-focused
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

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: Panel Backgrounds
        // ════════════════════════════════════════════════════════════════════════

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

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: UI Primitives
        // ════════════════════════════════════════════════════════════════════════

        private Text BuildText(string name, RectTransform parent, string value,
            int size, TextAnchor anchor)
        {
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = EnsureComponent<Text>(rect.gameObject);
            text.text = value;
            text.font = GetBuiltInFont();
            text.fontSize = size;
            text.alignment = anchor;
            text.color = TxtColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button BuildButton(string name, RectTransform parent, string label, int size)
        {
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = EnsureOrGetImage(rect.gameObject, BtnColor);
            img.raycastTarget = true;

            var btn = EnsureComponent<Button>(rect.gameObject);
            var outline = EnsureComponent<Outline>(rect.gameObject);
            outline.effectColor = GamePalette.AccentGoldGlow;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var colors = btn.colors;
            colors.normalColor      = BtnColor;
            colors.highlightedColor = new Color(BtnColor.r, BtnColor.g, BtnColor.b, 0.30f);
            colors.pressedColor     = new Color(BtnColor.r - 0.05f, BtnColor.g - 0.05f, BtnColor.b - 0.05f, 0.48f);
            colors.selectedColor    = new Color(0.98f, 0.83f, 0.26f, 0.30f); // gold tint when controller-focused
            colors.disabledColor    = GamePalette.DisabledGray;
            colors.fadeDuration     = 0.08f;
            colors.colorMultiplier  = 1f;
            btn.colors = colors;

            // Enable automatic navigation so EventSystem can traverse with stick/d-pad
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            var lblRect = EnsureRect("Label", rect, Vector2.zero, Vector2.one,
                new Vector2(10f, 8f), new Vector2(-10f, -8f));
            var text = EnsureComponent<Text>(lblRect.gameObject);
            text.text = label;
            text.font = GetBuiltInFont();
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = BtnTextColor;

            lblRect.SetAsLastSibling();

            return btn;
        }

        private Toggle BuildToggle(string name, RectTransform parent, string label)
        {
            var row = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.sizeDelta = new Vector2(260f, 32f);

            var bg = EnsureOrGetImage(row.gameObject, GamePalette.WithAlpha(BtnColor, 0.85f));
            var toggle = EnsureComponent<Toggle>(row.gameObject);

            var check = EnsureRect("Checkmark", row,
                new Vector2(0.02f, 0.15f), new Vector2(0.10f, 0.85f), Vector2.zero, Vector2.zero);
            var checkImg = EnsureOrGetImage(check.gameObject, AccentColor);

            var text = BuildText("Label", row, label, 15, TextAnchor.MiddleLeft);
            text.color = BtnTextColor; // dark text on cream background
            SetRect(text.rectTransform,
                new Vector2(0.14f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            toggle.targetGraphic = bg;
            toggle.graphic = checkImg;
            return toggle;
        }

        private Slider BuildSlider(string name, RectTransform parent)
        {
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var slider = EnsureComponent<Slider>(rect.gameObject);

            var bg = EnsureRect("Background", rect,
                new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(bg.gameObject, GamePalette.ShadowMid);

            var fillArea = EnsureRect("Fill Area", rect,
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var fill = EnsureRect("Fill", fillArea,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = EnsureOrGetImage(fill.gameObject, GamePalette.ToggleFill);
            fillImg.raycastTarget = false;

            var handleArea = EnsureRect("Handle Slide Area", rect,
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var handle = EnsureRect("Handle", handleArea,
                Vector2.zero, new Vector2(0f, 1f), new Vector2(-10f, 0f), new Vector2(10f, 0f));
            var handleImg = EnsureOrGetImage(handle.gameObject, GamePalette.ToggleHandle);

            slider.targetGraphic = handleImg;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            return slider;
        }

        private Dropdown BuildDropdown(string name, RectTransform parent)
        {
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(rect.gameObject, BtnColor);
            var dropdown = EnsureComponent<Dropdown>(rect.gameObject);

            var label = BuildText("Label", rect, "Select", 16, TextAnchor.MiddleLeft);
            label.color = BtnTextColor; // dark text on cream/light background
            SetRect(label.rectTransform,
                new Vector2(0.04f, 0.1f), new Vector2(0.76f, 0.9f), Vector2.zero, Vector2.zero);

            var arrowRect = EnsureRect("Arrow", rect,
                new Vector2(0.82f, 0.15f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
            var arrowText = EnsureComponent<Text>(arrowRect.gameObject);
            arrowText.font = GetBuiltInFont();
            arrowText.text = "\u25BC";
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = BtnTextColor; // dark
            arrowText.fontSize = 16;

            // Template
            var template = EnsureRect("Template", rect,
                Vector2.zero, new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 180f);
            EnsureOrGetImage(template.gameObject, BtnColor);
            template.gameObject.SetActive(false);

            var viewport = EnsureRect("Viewport", template,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(viewport.gameObject, GamePalette.ScrollBgDark);
            EnsureComponent<Mask>(viewport.gameObject).showMaskGraphic = false;

            var content = EnsureRect("Content", viewport,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var cl = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            cl.childAlignment = TextAnchor.UpperCenter;
            cl.childControlHeight = true;
            cl.childControlWidth = true;
            cl.childForceExpandHeight = false;
            cl.childForceExpandWidth = true;
            cl.spacing = 2f;
            cl.padding = new RectOffset(2, 2, 2, 2);
            var cf = EnsureComponent<ContentSizeFitter>(content.gameObject);
            cf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Item template
            var item = EnsureRect("Item", content,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), Vector2.zero);
            EnsureComponent<LayoutElement>(item.gameObject).minHeight = 30f;
            var itemBg = EnsureOrGetImage(item.gameObject, GamePalette.ListItemBg);
            var itemTgl = EnsureComponent<Toggle>(item.gameObject);
            itemTgl.targetGraphic = itemBg;

            var itemCheck = EnsureRect("Item Checkmark", item,
                new Vector2(0.02f, 0.18f), new Vector2(0.08f, 0.82f), Vector2.zero, Vector2.zero);
            var itemCheckImg = EnsureOrGetImage(itemCheck.gameObject, AccentColor);
            itemTgl.graphic = itemCheckImg;

            var itemLabel = BuildText("Item Label", item, "Option", 16, TextAnchor.MiddleLeft);
            itemLabel.color = BtnTextColor; // dark text for readability in dropdown list
            SetRect(itemLabel.rectTransform,
                new Vector2(0.10f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            AttachDropdownItemCompat(item.gameObject, item, itemLabel, itemBg, itemTgl);

            var scroll = EnsureComponent<ScrollRect>(template.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            dropdown.captionText = label;
            dropdown.template = template;
            dropdown.itemText = itemLabel;
            dropdown.itemImage = null;
            dropdown.targetGraphic = EnsureOrGetImage(rect.gameObject, BtnColor);
            dropdown.captionText.raycastTarget = false;
            dropdown.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            return dropdown;
        }

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
        private static void FitDropdownToItems(Dropdown dd)
        {
            if (dd == null || dd.template == null || dd.options.Count == 0) return;
            var h = Mathf.Min(dd.options.Count * 32f + 8f, 300f);
            var sd = dd.template.sizeDelta;
            dd.template.sizeDelta = new Vector2(sd.x, h);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: Infrastructure
        // ════════════════════════════════════════════════════════════════════════

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

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: Core
        // ════════════════════════════════════════════════════════════════════════

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

        // V2: delegates to InRunUiFactory.GetFont() — single font-load path for all builders.
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

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: Dropdown Compatibility
        // ════════════════════════════════════════════════════════════════════════

        private static void AttachDropdownItemCompat(GameObject itemGo, RectTransform itemRect,
            Text itemLabel, Image itemBg, Toggle itemTgl)
        {
            var diType = typeof(Dropdown).GetNestedType("DropdownItem",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (diType == null || !typeof(Component).IsAssignableFrom(diType))
            {
                Debug.LogWarning("[MainMenuBlueprintBuilder] DropdownItem type not found — dropdown may not work.");
                return;
            }

            var di = itemGo.GetComponent(diType) ?? itemGo.AddComponent(diType);
            SetMember(diType, di, "text", itemLabel, "m_Text");
            SetMember(diType, di, "image", itemBg, "m_Image");
            SetMember(diType, di, "toggle", itemTgl, "m_Toggle");
            SetMember(diType, di, "rectTransform", itemRect, "m_RectTransform");
        }

        private static void SetMember(Type type, Component target,
            string propName, object value, string fieldFallback)
        {
            var prop = type.GetProperty(propName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value, null);
                return;
            }

            var field = type.GetField(fieldFallback,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
