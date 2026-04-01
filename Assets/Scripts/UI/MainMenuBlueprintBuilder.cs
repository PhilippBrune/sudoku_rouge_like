using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuBlueprintBuilder : MonoBehaviour
    {
        [SerializeField] private bool autoBuildOnStart = true;

        private static readonly Color BgColor     = new(0.06f, 0.09f, 0.08f, 1f);
        private static readonly Color PanelColor  = new(0.10f, 0.12f, 0.10f, 0.70f);
        private static readonly Color BtnColor    = new(0.82f, 0.75f, 0.62f, 0.92f);
        private static readonly Color BtnTextColor = new(0.12f, 0.10f, 0.08f, 1f);
        private static readonly Color AccentColor = new(0.98f, 0.83f, 0.26f, 1f);
        private static readonly Color TxtColor    = new(0.96f, 0.93f, 0.82f, 1f);

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

            var mc        = EnsureComponent<MainMenuController>(gameObject);
            var optCtrl   = EnsureComponent<OptionsController>(gameObject);
            var tutCtrl   = EnsureComponent<TutorialMenuController>(gameObject);
            var metaCtrl  = EnsureComponent<MetaProgressionPanelController>(gameObject);
            var modesCtrl = EnsureComponent<GameModesPanelController>(gameObject);
            var itemsCtrl = EnsureComponent<ItemsMenuController>(gameObject);
            var bootstrap = FindAnyObjectByType<GameBootstrap>();

            EnsureEventSystem();
            var canvas = EnsureCanvas();
            var root = EnsureRect("MainMenuRoot", canvas.transform as RectTransform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(root.gameObject, BgColor);

            // ── Title ────────────────────────────────────────────────────────
            var titleText = BuildText("TitleText", root, "Run of the Nine", 42, TextAnchor.MiddleCenter);
            SetRect(titleText.rectTransform,
                new Vector2(0.15f, 0.88f), new Vector2(0.85f, 0.96f), Vector2.zero, Vector2.zero);
            titleText.fontStyle = FontStyle.BoldAndItalic;
            titleText.color = TxtColor;
            var titleShadow = EnsureComponent<Shadow>(titleText.gameObject);
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            titleShadow.effectDistance = new Vector2(2f, -2f);

            var subtitleText = BuildText("SubtitleText", root, "Sudoku Roguelike", 18, TextAnchor.MiddleCenter);
            SetRect(subtitleText.rectTransform,
                new Vector2(0.25f, 0.845f), new Vector2(0.75f, 0.885f), Vector2.zero, Vector2.zero);
            subtitleText.color = new Color(TxtColor.r, TxtColor.g, TxtColor.b, 0.65f);

            // ── Menu Card ──────────────────────────────────────────────────────
            var card = EnsureRect("MenuCard", root,
                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.84f),
                Vector2.zero, Vector2.zero);
            EnsureOrGetImage(card.gameObject, PanelColor);
            var cardOutline = EnsureComponent<Outline>(card.gameObject);
            cardOutline.effectColor = new Color(0.55f, 0.45f, 0.30f, 0.35f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // ── Main Menu Buttons ──────────────────────────────────────────────
            // 9 buttons evenly spaced within card (0.88 top to 0.08 bottom)
            var btnStart  = BuildMenuButton(card, "BtnStart",   T("Start Game"),       new Vector2(0.06f, 0.81f), new Vector2(0.94f, 0.88f), mc.StartGame);
            var btnResume = BuildMenuButton(card, "BtnResume",  T("Resume Game"),      new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.79f), mc.ResumeGame);
            var btnTut    = BuildMenuButton(card, "BtnTutorial",T("Tutorial"),         new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.70f), mc.OpenTutorial);
            var btnMeta   = BuildMenuButton(card, "BtnMeta",    T("Meta Progression"), new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.61f), mc.OpenMetaProgression);
            var btnModes  = BuildMenuButton(card, "BtnModes",   T("Game Modes"),       new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.52f), mc.OpenGameModes);
            var btnItems  = BuildMenuButton(card, "BtnItems",   T("Items"),            new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.43f), mc.OpenItems);
            var btnOpt    = BuildMenuButton(card, "BtnOptions", T("Options"),          new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.34f), mc.OpenOptions);
            var btnCred   = BuildMenuButton(card, "BtnCredits", T("Credits"),          new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.25f), mc.OpenCredits);
            var btnQuit   = BuildMenuButton(card, "BtnQuit",    T("Quit"),             new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.16f), mc.ExitGame);

            ApplyMenuButtonIcon(btnStart,  "GeneratedIcons/icon_bud");
            ApplyMenuButtonIcon(btnResume, "GeneratedIcons/icon_scroll_graph");
            ApplyMenuButtonIcon(btnTut,    "GeneratedIcons/icon_bamboo_scroll");
            ApplyMenuButtonIcon(btnMeta,   "GeneratedIcons/icon_golden_bloom");
            ApplyMenuButtonIcon(btnModes,  "GeneratedIcons/icon_garden_lantern");
            ApplyMenuButtonIcon(btnItems,  "GeneratedIcons/icon_triple_chest");
            ApplyMenuButtonIcon(btnOpt,    "GeneratedIcons/icon_stone_gear");
            ApplyMenuButtonIcon(btnCred,   "GeneratedIcons/icon_language_scroll");
            ApplyMenuButtonIcon(btnQuit,   "GeneratedIcons/icon_torii_lock");

            // Debug toggle
            var debug = BuildToggle("DebugToggle", card, "Enable All (Debug)");
            SetRect(debug.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.01f), new Vector2(0.50f, 0.07f), Vector2.zero, Vector2.zero);
            debug.SetIsOnWithoutNotify(false);
            debug.onValueChanged.RemoveAllListeners();
            debug.onValueChanged.AddListener(mc.OnDebugEnableAllChanged);

            // ── Status Bar ─────────────────────────────────────────────────────
            var status = BuildText("StatusText", root, "Ready.", 20, TextAnchor.MiddleCenter);
            SetRect(status.rectTransform,
                new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.07f), Vector2.zero, Vector2.zero);
            status.color = new Color(TxtColor.r, TxtColor.g, TxtColor.b, 0.6f);
            var statusShadow = EnsureComponent<Shadow>(status.gameObject);
            statusShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            statusShadow.effectDistance = new Vector2(1f, -1f);

            // ── Sub-panels ─────────────────────────────────────────────────────
            var classPanel = BuildClassSelectPanel(root, mc);
            var optPanel   = BuildOptionsPanel(root, mc, optCtrl);
            var credPanel  = BuildCreditsPanel(root, mc);
            var tutPanel   = BuildTutorialSetupPanel(root, mc, tutCtrl);
            var metaPanel  = BuildMetaProgressionPanel(root, mc, metaCtrl);
            var modesPanel = BuildGameModesPanel(root, mc);
            var itemsPanel = BuildItemsPanel(root, mc, itemsCtrl);

            // ── Wire ───────────────────────────────────────────────────────────
            mc.ConfigureUi(bootstrap,
                card.gameObject, classPanel, optPanel, credPanel,
                tutPanel, metaPanel, modesPanel, itemsPanel,
                optCtrl, tutCtrl, metaCtrl, modesCtrl, itemsCtrl,
                status, btnResume);

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
                (ClassId.NumberFreak,       "Number Freak",       "icon_spin_coin"),
                (ClassId.GardenMonk,        "Garden Monk",        "icon_enlightenment_tree"),
                (ClassId.ShrineArchivist,   "Shrine Archivist",   "icon_scroll_graph"),
                (ClassId.KoiGambler,        "Koi Gambler",        "icon_golden_koi"),
                (ClassId.StoneGardener,     "Stone Gardener",     "icon_moss_stone"),
                (ClassId.LanternSeer,       "Lantern Seer",       "icon_garden_lantern"),
                (ClassId.ReedDuelist,       "Reed Duelist",       "icon_bamboo_scroll"),
                (ClassId.QuietCartographer, "Quiet Cartographer", "icon_compass_of_order"),
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
                ApplyMenuButtonIcon(btn, "GeneratedIcons/" + icon);
                mc.RegisterClassButton(captured, btn);
            }

            // Class info panel: y=0.19-0.48 (height=0.29), below row3 (bottom≈0.49), above controls (top≈0.18)
            var infoBg = EnsureRect("ClassInfoBg", pr,
                new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(infoBg.gameObject, new Color(0.08f, 0.10f, 0.09f, 0.85f));
            var infoOutline = EnsureComponent<Outline>(infoBg.gameObject);
            infoOutline.effectColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.30f);
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

            // Confirm — same height as class buttons (0.08)
            var confirm = BuildButton("BtnConfirmClass", pr, T("Start Run"), 20);
            SetRect(confirm.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.10f), new Vector2(0.90f, 0.18f), Vector2.zero, Vector2.zero);
            confirm.onClick.RemoveAllListeners();
            confirm.onClick.AddListener(mc.ConfirmClassAndStart);
            ApplyMenuButtonIcon(confirm, "GeneratedIcons/icon_bud");

            // Back — same height as class buttons (0.08)
            var back = BuildButton("BtnClassBack", pr, "Back", 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

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

            var title = BuildText("Title", pr, T("Tutorial Setup"), 28, TextAnchor.UpperCenter);
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
                new Vector2(0.62f, 0.02f), new Vector2(0.80f, 0.09f), Vector2.zero, Vector2.zero);
            startBtn.onClick.RemoveAllListeners();
            startBtn.onClick.AddListener(mc.StartTutorialGame);
            ApplyMenuButtonIcon(startBtn, "GeneratedIcons/icon_bud");

            var backBtn = BuildButton("BtnTutBack", pr, T("Back"), 15);
            SetRect(backBtn.GetComponent<RectTransform>(),
                new Vector2(0.82f, 0.02f), new Vector2(0.96f, 0.09f), Vector2.zero, Vector2.zero);
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(backBtn, "GeneratedIcons/icon_torii_lock");

            // ── Modifier hover tooltip ──
            var ttBg = EnsureRect("ModTooltipBg", pr,
                new Vector2(0.04f, 0.01f), new Vector2(0.59f, 0.09f), Vector2.zero, Vector2.zero);
            var ttImg = EnsureComponent<Image>(ttBg.gameObject);
            ttImg.color = new Color(0f, 0f, 0f, 0.80f);
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
            WireTip(palindrome,    "Grey line: digits read the same from either end of the line.");
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
                new Vector2(0.24f, 0.12f), new Vector2(0.76f, 0.88f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Options"), 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.91f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            // ── Audio ──
            var audioHdr = BuildText("AudioHdr", pr, T("Audio"), 20, TextAnchor.MiddleLeft);
            SetRect(audioHdr.rectTransform,
                new Vector2(0.10f, 0.86f), new Vector2(0.90f, 0.91f), Vector2.zero, Vector2.zero);

            var opts = optCtrl.GetOptions();

            BuildLabelAndSlider(pr, "MasterVolume", T("Master Volume"),
                new Vector2(0.10f, 0.78f), new Vector2(0.48f, 0.86f),
                0f, 1f, opts.Audio.MasterVolume, optCtrl.SetMasterVolume);

            BuildLabelAndSlider(pr, "MusicVolume", T("Music Volume"),
                new Vector2(0.52f, 0.78f), new Vector2(0.90f, 0.86f),
                0f, 1f, opts.Audio.MusicVolume, optCtrl.SetMusicVolume);

            BuildLabelAndSlider(pr, "SfxVolume", T("SFX Volume"),
                new Vector2(0.10f, 0.70f), new Vector2(0.48f, 0.78f),
                0f, 1f, opts.Audio.SfxVolume, optCtrl.SetSfxVolume);

            BuildLabelAndSlider(pr, "UiVolume", T("UI Volume"),
                new Vector2(0.52f, 0.70f), new Vector2(0.90f, 0.78f),
                0f, 1f, opts.Audio.UiVolume, optCtrl.SetUiVolume);

            BuildOptionToggle(pr, "MuteUnfocused", T("Mute when unfocused"),
                new Vector2(0.10f, 0.65f), new Vector2(0.48f, 0.69f),
                opts.Audio.MuteWhenUnfocused, optCtrl.SetMuteWhenUnfocused);

            // ── Display ──
            var displayHdr = BuildText("DisplayHdr", pr, T("Display"), 20, TextAnchor.MiddleLeft);
            SetRect(displayHdr.rectTransform,
                new Vector2(0.10f, 0.59f), new Vector2(0.90f, 0.64f), Vector2.zero, Vector2.zero);

            BuildOptionToggle(pr, "Fullscreen", T("Fullscreen"),
                new Vector2(0.10f, 0.53f), new Vector2(0.48f, 0.57f),
                opts.Graphics.Fullscreen, optCtrl.SetFullscreen);

            // Language
            var langLabel = BuildText("LangLabel", pr, T("Language"), 16, TextAnchor.MiddleLeft);
            SetRect(langLabel.rectTransform,
                new Vector2(0.52f, 0.54f), new Vector2(0.66f, 0.58f), Vector2.zero, Vector2.zero);
            var langDrop = BuildDropdown("LanguageDropdown", pr);
            SetRect(langDrop.GetComponent<RectTransform>(),
                new Vector2(0.68f, 0.53f), new Vector2(0.90f, 0.58f), Vector2.zero, Vector2.zero);
            langDrop.ClearOptions();
            langDrop.AddOptions(new System.Collections.Generic.List<string> { "English", "Deutsch" });
            FitDropdownToItems(langDrop);
            langDrop.SetValueWithoutNotify((int)opts.Language);
            langDrop.onValueChanged.RemoveAllListeners();
            langDrop.onValueChanged.AddListener(idx =>
            {
                optCtrl.SetLanguage((LanguageOption)idx);
                mc.RefreshLanguage();
            });

            // ── Gameplay ──
            var gameHdr = BuildText("GameHdr", pr, T("Gameplay"), 20, TextAnchor.MiddleLeft);
            SetRect(gameHdr.rectTransform,
                new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.51f), Vector2.zero, Vector2.zero);

            BuildOptionToggle(pr, "HighlightErrors", T("Highlight Errors"),
                new Vector2(0.10f, 0.40f), new Vector2(0.48f, 0.44f),
                opts.Gameplay.HighlightConflicts, optCtrl.SetHighlightConflicts);

            // ── Accessibility ──
            var accHdr = BuildText("AccessHdr", pr, T("Accessibility"), 20, TextAnchor.MiddleLeft);
            SetRect(accHdr.rectTransform,
                new Vector2(0.10f, 0.33f), new Vector2(0.90f, 0.38f), Vector2.zero, Vector2.zero);

            BuildOptionToggle(pr, "Colorblind", T("Colorblind Mode"),
                new Vector2(0.10f, 0.27f), new Vector2(0.48f, 0.31f),
                opts.Accessibility.ColorblindMode, optCtrl.SetColorblindMode);

            BuildOptionToggle(pr, "HighContrast", T("High Contrast"),
                new Vector2(0.52f, 0.27f), new Vector2(0.90f, 0.31f),
                opts.Accessibility.HighContrastMode, optCtrl.SetHighContrast);

            BuildOptionToggle(pr, "ReduceMotion", T("Reduce Motion"),
                new Vector2(0.10f, 0.21f), new Vector2(0.48f, 0.25f),
                opts.Accessibility.ReduceMotion, optCtrl.SetReduceMotion);

            BuildOptionToggle(pr, "AltSymbols", T("Alt Constraint Symbols"),
                new Vector2(0.52f, 0.21f), new Vector2(0.90f, 0.25f),
                opts.Accessibility.AlternativeConstraintSymbols, optCtrl.SetAltSymbols);

            // ── Controls ──
            var ctrlHdr = BuildText("CtrlHdr", pr, T("Controls"), 20, TextAnchor.MiddleLeft);
            SetRect(ctrlHdr.rectTransform,
                new Vector2(0.10f, 0.14f), new Vector2(0.90f, 0.19f), Vector2.zero, Vector2.zero);

            var keybindNote = BuildText("KeybindList", pr,
                "1-9: Place digit    P: Pencil mode    Q: Save & Quit    Esc: Pause", 12, TextAnchor.MiddleLeft);
            SetRect(keybindNote.rectTransform,
                new Vector2(0.10f, 0.09f), new Vector2(0.90f, 0.14f), Vector2.zero, Vector2.zero);
            keybindNote.color = new Color(TxtColor.r, TxtColor.g, TxtColor.b, 0.7f);

            // Back
            var back = BuildButton("BtnOptBack", pr, "Back", 20);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.74f, 0.02f), new Vector2(0.90f, 0.08f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

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

            var back = BuildButton("BtnCredBack", pr, "Back", 20);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.30f, 0.10f), new Vector2(0.70f, 0.24f), Vector2.zero, Vector2.zero);
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

            var back = BuildButton("BtnMetaBack", pr, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.80f, 0.04f), new Vector2(0.92f, 0.10f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(back, "GeneratedIcons/icon_torii_lock");

            metaCtrl.Configure(scrollContent, summary);

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
                "Spirit Trials: Challenge tiers with ranked modifiers.",
                16, TextAnchor.UpperLeft);
            SetRect(desc.rectTransform,
                new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.82f), Vector2.zero, Vector2.zero);

            var garden = BuildButton("BtnGarden", pr, "Start Garden Run", 18);
            SetRect(garden.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.45f), Vector2.zero, Vector2.zero);
            garden.onClick.RemoveAllListeners();
            garden.onClick.AddListener(mc.StartGame);
            ApplyMenuButtonIcon(garden, "GeneratedIcons/icon_bud");

            var endless = BuildButton("BtnEndless", pr, "Start Endless Zen", 18);
            SetRect(endless.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.34f), Vector2.zero, Vector2.zero);
            endless.onClick.RemoveAllListeners();
            endless.onClick.AddListener(mc.StartEndlessZen);
            ApplyMenuButtonIcon(endless, "GeneratedIcons/icon_infinite_lotus");

            var trials = BuildButton("BtnTrials", pr, "Start Spirit Trials", 18);
            SetRect(trials.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.23f), Vector2.zero, Vector2.zero);
            trials.onClick.RemoveAllListeners();
            trials.onClick.AddListener(mc.StartSpiritTrials);
            ApplyMenuButtonIcon(trials, "GeneratedIcons/icon_temple_seal");

            var back = BuildButton("BtnModesBack", pr, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.76f, 0.05f), new Vector2(0.88f, 0.11f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
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

            var title = BuildText("Title", pr, "Items Codex", 30, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var scrollContent = BuildScrollArea("ItemsScroll", pr,
                new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.88f));

            var back = BuildButton("BtnItemsBack", pr, "Back", 16);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.82f, 0.04f), new Vector2(0.94f, 0.10f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);

            itemsCtrl.Configure(scrollContent);

            panel.SetActive(false);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Helpers: Composite Builders
        // ════════════════════════════════════════════════════════════════════════

        private GameObject MakePanel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            // Full-screen opaque overlay so submenus are not transparent over the main menu
            var go = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(go, new Color(BgColor.r, BgColor.g, BgColor.b, 1f));

            // Inner card — same proportions as the main menu card
            var inner = EnsureRect("Content", go.transform as RectTransform,
                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.92f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(inner.gameObject, PanelColor);
            var outline = EnsureComponent<Outline>(inner.gameObject);
            outline.effectColor = new Color(0.55f, 0.45f, 0.30f, 0.35f);
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
            EnsureOrGetImage(scrollRect.gameObject, new Color(0f, 0f, 0f, 0.12f));

            var viewport = EnsureRect("Viewport", scrollRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(viewport.gameObject, new Color(1f, 1f, 1f, 0.02f));
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
            layout.childControlHeight = false;
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

        private void BuildLabelAndSlider(RectTransform parent, string id, string label,
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
        }

        private void BuildOptionToggle(RectTransform parent, string id, string label,
            Vector2 anchorMin, Vector2 anchorMax, bool value,
            UnityEngine.Events.UnityAction<bool> onChanged)
        {
            var tgl = BuildToggle(id + "Toggle", parent, label);
            SetRect(tgl.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            tgl.SetIsOnWithoutNotify(value);
            tgl.onValueChanged.RemoveAllListeners();
            tgl.onValueChanged.AddListener(onChanged);
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
            outline.effectColor = new Color(0.45f, 0.38f, 0.25f, 0.7f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var colors = btn.colors;
            colors.normalColor      = BtnColor;
            colors.highlightedColor = new Color(0.90f, 0.83f, 0.70f, 0.95f);
            colors.pressedColor     = new Color(0.72f, 0.65f, 0.52f, 0.95f);
            colors.selectedColor    = colors.highlightedColor;
            colors.disabledColor    = new Color(0.45f, 0.42f, 0.38f, 0.7f);
            colors.fadeDuration     = 0.08f;
            colors.colorMultiplier  = 1f;
            btn.colors = colors;

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
            outline.effectColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.38f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var colors = btn.colors;
            colors.normalColor      = BtnColor;
            colors.highlightedColor = new Color(BtnColor.r + 0.05f, BtnColor.g + 0.05f, BtnColor.b + 0.05f, 1f);
            colors.pressedColor     = new Color(BtnColor.r - 0.03f, BtnColor.g - 0.03f, BtnColor.b - 0.03f, 1f);
            colors.selectedColor    = colors.highlightedColor;
            colors.disabledColor    = new Color(0.22f, 0.22f, 0.22f, 0.7f);
            colors.fadeDuration     = 0.08f;
            colors.colorMultiplier  = 1.35f;
            btn.colors = colors;

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

            var bg = EnsureOrGetImage(row.gameObject,
                new Color(BtnColor.r, BtnColor.g, BtnColor.b, 0.65f));
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
            EnsureOrGetImage(bg.gameObject, new Color(0f, 0f, 0f, 0.35f));

            var fillArea = EnsureRect("Fill Area", rect,
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var fill = EnsureRect("Fill", fillArea,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = EnsureOrGetImage(fill.gameObject, new Color(0.25f, 0.65f, 0.55f, 1f));
            fillImg.raycastTarget = false;

            var handleArea = EnsureRect("Handle Slide Area", rect,
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var handle = EnsureRect("Handle", handleArea,
                Vector2.zero, new Vector2(0f, 1f), new Vector2(-10f, 0f), new Vector2(10f, 0f));
            var handleImg = EnsureOrGetImage(handle.gameObject, new Color(0.90f, 0.95f, 0.92f, 1f));

            slider.targetGraphic = handleImg;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.direction = Slider.Direction.LeftToRight;

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
            EnsureOrGetImage(viewport.gameObject, new Color(0f, 0f, 0f, 0.1f));
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
            var itemBg = EnsureOrGetImage(item.gameObject, new Color(0f, 0f, 0f, 0.2f));
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

            return dropdown;
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

        private static Font GetBuiltInFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

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
