using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SudokuRoguelike.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SudokuRoguelike.UI
{
    public sealed class InRunUiBlueprintBuilder : MonoBehaviour
    {
        private OptionsController _optionsRefreshSource;
        private System.Action _optionsRefresh;

        private RunMapController runMapController;

        private Color panelColor;
        private Color accentColor;
        private Color textColor;
        private Color buttonColor;

        private int titleFontSize;
        private int bodyFontSize;
        private int smallFontSize;

        private static string T(string key, string englishFallback = null) =>
            LocalizationService.T(key, englishFallback);

        private void Awake()
        {
            panelColor  = GamePalette.Panel;
            accentColor = GamePalette.SuccessGreen;
            textColor   = GamePalette.TextPrimary;
            buttonColor = GamePalette.Button;

            titleFontSize = 24;
            bodyFontSize = 18;
            smallFontSize = 15;
        }

        [ContextMenu("Build In-Run UI Blueprint")]
        public void BuildBlueprint()
        {
            if (runMapController == null)
            {
                runMapController = GetComponentInChildren<RunMapController>(true);
            }

            if (runMapController == null)
            {
                runMapController = FindFirstObjectByType<RunMapController>();
            }

            EnsureMainCamera();
            var canvas = EnsureCanvas();
            EnsureEventSystem();
            var root = EnsureRect("InRunUI", canvas.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(root.gameObject, new Color(0f, 0f, 0f, 0f));

            var flow = EnsureComponent<InRunUiFlowController>(root.gameObject);
            var runScreen = EnsureComponent<InRunController>(root.gameObject);

            var eventScreen = BuildEventScreenHost(root);
            var cursePanel = BuildCursePanel(root);
            var debugPanel = BuildDebugPanel(root);
            var sudokuPanel = BuildSudokuBoardPanel(root);
            BuildPathOverviewPanel(root);
            BuildSudokuGameplayPanel(root);
            BuildGameOverPanel(root);
            var inGameOptionsPanel = BuildInGameOptionsPanel(root);
            var tutorialLabel = BuildTutorialBanner(root);

            flow.Configure(
                runMapController,
                eventScreen,
                cursePanel.GetComponent<CursePanelController>());

            // Keep legacy prototype helper panels out of the main redesign flow.
            cursePanel.SetActive(false);
            debugPanel.SetActive(false);
            sudokuPanel.SetActive(false);
            inGameOptionsPanel.SetActive(false);
            tutorialLabel.gameObject.SetActive(false);
            if (tutorialLabel.transform.parent != null)
                tutorialLabel.transform.parent.gameObject.SetActive(false);

            if (runMapController == null)
                runMapController = EnsureComponent<RunMapController>(root.gameObject);

            var tutorialBanner = EnsureComponent<TutorialRunBannerController>(root.gameObject);
            tutorialBanner.Configure(runMapController, tutorialLabel);

            runScreen.Configure(runMapController, root);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
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

        // [REQ: UI-STYLE-014] The event screen is a scene built over the path when an event opens
        // (EventChoiceScreenController); here only its empty host, no hidden panel (GAP-052).
        private EventChoiceScreenController BuildEventScreenHost(RectTransform root)
        {
            var host = EnsureRect("EventScreen", root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject;
            return EnsureComponent<EventChoiceScreenController>(host);
        }

        private GameObject BuildCursePanel(RectTransform root)
        {
            var panel = EnsureRect("CursePanel", root, new Vector2(0.02f, 0.58f), new Vector2(0.30f, 0.96f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);
            var controller = EnsureComponent<CursePanelController>(panel);

            var title = BuildText("TitleText", panel.transform as RectTransform,
                LocalizationService.Format("InRun.Curse.Title", "Curses ({0})", 0), bodyFontSize, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);

            // Icon column on the left — CursePanelController populates it dynamically per curse
            var iconColGo = new GameObject("CurseIconColumn", typeof(RectTransform));
            iconColGo.transform.SetParent(panel.transform, false);
            var iconColRt = iconColGo.GetComponent<RectTransform>();
            iconColRt.anchorMin = new Vector2(0.02f, 0.22f);
            iconColRt.anchorMax = new Vector2(0.20f, 0.78f);
            iconColRt.offsetMin = iconColRt.offsetMax = Vector2.zero;

            // List text shifted right to leave room for icons
            var list = BuildText("CurseListText", panel.transform as RectTransform,
                T("InRun.Curse.None", "No active curses."), smallFontSize, TextAnchor.UpperLeft);
            SetRect(list.rectTransform, new Vector2(0.22f, 0.22f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);

            controller.Configure(title, list, iconColRt);
            return panel;
        }

        private GameObject BuildDebugPanel(RectTransform root)
        {
            var panel = EnsureRect("DebugControlsPanel", root, new Vector2(0.32f, 0.58f), new Vector2(0.98f, 0.74f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("DebugTitle", panel.transform as RectTransform,
                T("InRun.Blueprint.PathDebugTitle", "Garden Paths"), bodyFontSize, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(0.02f, 0.66f), new Vector2(0.20f, 0.96f), Vector2.zero, Vector2.zero);

            var hint = BuildText("PathPreviewText", panel.transform as RectTransform,
                T("InRun.Blueprint.PathPreviewHint", "Path A/B previews appear here."), smallFontSize, TextAnchor.UpperLeft);
            SetRect(hint.rectTransform, new Vector2(0.22f, 0.10f), new Vector2(0.64f, 0.92f), Vector2.zero, Vector2.zero);

            var calmPath = BuildButton("BtnPathA", panel.transform as RectTransform,
                T("InRun.Blueprint.PathA", "Path A (A)"), smallFontSize);
            SetRect(calmPath.GetComponent<RectTransform>(), new Vector2(0.66f, 0.52f), new Vector2(0.79f, 0.88f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(calmPath, "bud", "meta");

            var riskyPath = BuildButton("BtnPathB", panel.transform as RectTransform,
                T("InRun.Blueprint.PathB", "Path B (B)"), smallFontSize);
            SetRect(riskyPath.GetComponent<RectTransform>(), new Vector2(0.80f, 0.52f), new Vector2(0.93f, 0.88f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(riskyPath, "golden_koi", "items");

            var quitAndSave = BuildButton("BtnQuitSave", panel.transform as RectTransform,
                T("InRun.Path.SaveAndQuit", "Save & Quit"), smallFontSize);
            SetRect(quitAndSave.GetComponent<RectTransform>(), new Vector2(0.66f, 0.10f), new Vector2(0.93f, 0.46f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(quitAndSave, "ink_save", "ui");

            return panel;
        }

        private GameObject BuildSudokuBoardPanel(RectTransform root)
        {
            Text boardText, statusText;
            var panel = EnsureRect("SudokuBoardPanel", root, new Vector2(0.02f, 0.10f), new Vector2(0.30f, 0.56f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("SudokuBoardTitle", panel.transform as RectTransform,
                T("InRun.Blueprint.SudokuBoardTitle", "Sudoku Board"), bodyFontSize, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            boardText = BuildText("SudokuBoardText", panel.transform as RectTransform, "", smallFontSize, TextAnchor.UpperLeft);
            SetRect(boardText.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            statusText = BuildText("SudokuBoardStatus", panel.transform as RectTransform, "", smallFontSize, TextAnchor.UpperLeft);
            SetRect(statusText.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero);

            return panel;
        }

        private void BuildPathOverviewPanel(RectTransform root)
        {
            var panel = EnsureRect("PathOverviewPanel", root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(panelColor.r, panelColor.g, panelColor.b, 0.97f));
            // Save & Quit button is built dynamically inside PathLegend by InRunController.RebuildPathCanvas
        }

        // [REQ: UI-STYLE-002] The puzzle screen as a hanging scroll (layout-proposals.md, B): the floor's painting behind,
        // the board in ink on a hanging scroll in the middle, HP and Pencil as paper lanterns on the left with the bag
        // and relics below, the number pad as wooden tiles and the rules as paper slips on the right, Save & Quit and
        // Options as round buttons. Names stay as before; InRunController and the HUD find the parts by name.
        // Panel anchors from the 1920×1080 mockup: a = (px / 1920 − 0.03) / 0.94 (the panel spans 3–97 %).
        private void BuildSudokuGameplayPanel(RectTransform root)
        {
            RectTransform gridRoot, numpadRoot;
            Button saveQuit, optionsButton;
            Text statusText, hpText, pencilText;
            var panel = EnsureRect("SudokuGameplayPanel", root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(0f, 0f, 0f, 0f)); // invisible, still keeps clicks from the path below
            var painting = InRunUiFactory.AddPanelBackground(panel.transform, "bg_puzzle_board", 1f);
            painting.rectTransform.anchorMin = new Vector2(-0.035f, -0.035f);
            painting.rectTransform.anchorMax = new Vector2(1.035f, 1.035f);
            var shade = ScrollUiKit.Bleed(panel.transform, "PuzzleShade").gameObject.AddComponent<Image>();
            shade.color = new Color(0.10f, 0.06f, 0.03f, 0.55f);
            shade.raycastTarget = false;
            shade.transform.SetSiblingIndex(1);

            ScrollUiKit.HangingScroll(panel.transform, "PuzzleScroll", new Vector2(0.3005f, 0.0291f), new Vector2(0.6995f, 0.9748f));

            // Header on the scroll: eyebrow (floor, depth, size, stars), the floor name, the status line in italics.
            var infoScrim = EnsureRect("InfoScrimBg", panel.transform as RectTransform,
                new Vector2(0.3005f, 0.853f), new Vector2(0.6995f, 0.897f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(infoScrim.gameObject, new Color(0f, 0f, 0f, 0f)).raycastTarget = false;
            var title = BuildText("SudokuGameplayTitle", infoScrim,
                T("InRun.Blueprint.SudokuGameplayTitle", "Sudoku Puzzle"), 34, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            title.color = ScrollUiKit.Ink;

            var levelInfo = BuildText("SudokuGameplayLevelInfo", panel.transform as RectTransform,
                T("InRun.Blueprint.LevelDepthPlaceholder", "Level: -  Depth: -"), 18, TextAnchor.MiddleCenter);
            SetRect(levelInfo.rectTransform, new Vector2(0.3005f, 0.899f), new Vector2(0.6995f, 0.925f), Vector2.zero, Vector2.zero);
            levelInfo.color = ScrollUiKit.InkSoft;

            statusText = BuildText("SudokuGameplayStatus", panel.transform as RectTransform,
                T("InRun.Blueprint.SelectPathStatus", "Select a path and start solving."), 18, TextAnchor.MiddleCenter);
            SetRect(statusText.rectTransform, new Vector2(0.3005f, 0.826f), new Vector2(0.6995f, 0.852f), Vector2.zero, Vector2.zero);
            statusText.color = ScrollUiKit.InkSoft;
            statusText.fontStyle = FontStyle.Italic;

            // Left column: HP and Pencil as paper lanterns (the bars keep their names; the HUD sets the fill).
            var hpPencilScrim = EnsureRect("HpPencilScrim", panel.transform as RectTransform,
                new Vector2(0.0346f, 0.60f), new Vector2(0.2230f, 0.83f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(hpPencilScrim.gameObject, new Color(0f, 0f, 0f, 0f)).raycastTarget = false;
            hpText = BuildLantern(panel.transform as RectTransform, "Hp", new Vector2(0.0346f, 0.6182f), new Vector2(0.1177f, 0.8152f),
                new Color(0.42f, 0.14f, 0.07f, 1f), new Color(0.96f, 0.45f, 0.22f, 1f),
                T("InRun.Blueprint.HpPlaceholder", "HP: -"));
            pencilText = BuildLantern(panel.transform as RectTransform, "Pencil", new Vector2(0.1398f, 0.6182f), new Vector2(0.2230f, 0.8152f),
                new Color(0.12f, 0.24f, 0.36f, 1f), new Color(0.62f, 0.82f, 0.95f, 1f),
                T("InRun.Blueprint.PencilPlaceholder", "Pencil: -"));

            // [REQ: REQ-ECON-100] Buy pencil during a puzzle (GAP-016). InRunController sets the label (cost) and state.
            // A small wooden tile under the Pencil lantern.
            var buyPencil = BuildButton("BtnBuyPencil", panel.transform as RectTransform,
                T("InRun.BuyPencil.Label", "+3 Pencil"), 15);
            SetRect(buyPencil.GetComponent<RectTransform>(), new Vector2(0.1398f, 0.563f), new Vector2(0.2230f, 0.603f), Vector2.zero, Vector2.zero);
            ScrollUiKit.MakeWoodTile(buyPencil, buyPencil.GetComponent<Image>());
            var buyLabel = buyPencil.GetComponentInChildren<Text>(true);
            SetRect(buyLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 1f), new Vector2(-4f, -1f));
            buyLabel.alignment = TextAnchor.MiddleCenter;
            buyLabel.color = ScrollUiKit.WoodTileText;
            buyLabel.resizeTextForBestFit = true;
            buyLabel.resizeTextMinSize = 10;
            buyLabel.resizeTextMaxSize = 15;

            // Right column: round Save & Quit and Options at the top, the wooden number pad, the hint at the bottom.
            saveQuit = ScrollUiKit.RoundButton(panel.transform, "BtnSudokuSaveQuit", new Vector2(0.8960f, 0.9462f), 60f, "ui/icon_ink_save");
            BuildRoundButtonCaption(saveQuit, T("InRun.Path.SaveAndQuit"), "Q");
            optionsButton = ScrollUiKit.RoundButton(panel.transform, "BtnSudokuOptions", new Vector2(0.9532f, 0.9462f), 60f, "ui/icon_stone_gear");
            BuildRoundButtonCaption(optionsButton, T("Options", "Options"), "O");
            // Keyboard nav: Save & Quit ↔ Options (horizontal pair)
            saveQuit.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = optionsButton };
            optionsButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = saveQuit };

            gridRoot = EnsureRect("SudokuGameplayGridRoot", panel.transform as RectTransform, new Vector2(0.3327f, 0.217f), new Vector2(0.6673f, 0.812f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(gridRoot.gameObject, ScrollUiKit.Ink); // the 2 px spacing between cells shows as ink lines

            numpadRoot = EnsureRect("SudokuGameplayNumpadRoot", panel.transform as RectTransform, new Vector2(0.7716f, 0.4704f), new Vector2(0.9710f, 0.7857f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(numpadRoot.gameObject, new Color(0f, 0f, 0f, 0f));

            var hintBg = EnsureRect("SudokuGameplayHintBg", panel.transform as RectTransform,
                new Vector2(0.7716f, 0.02f), new Vector2(0.9710f, 0.105f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(hintBg.gameObject, new Color(0f, 0f, 0f, 0f)).raycastTarget = false;

            var hint = BuildText("SudokuGameplayHint", panel.transform as RectTransform,
                T("InRun.Blueprint.PuzzleHint", "Use numpad buttons or keyboard 1-9.\nSingle click: select cell\nDouble click filled cell: highlight same numbers"),
                14, TextAnchor.UpperLeft);
            SetRect(hint.rectTransform, new Vector2(0.7716f, 0.02f), new Vector2(0.9710f, 0.105f), Vector2.zero, Vector2.zero);
            hint.color = new Color(0.90f, 0.86f, 0.76f, 0.95f);
            var hintShadow = hint.gameObject.AddComponent<Shadow>();
            hintShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            hintShadow.effectDistance = new Vector2(1f, -1f);

            // The boss's rule note sits on the scroll under the board, in vermilion ink.
            var bossDesc = BuildText("SudokuGameplayBossDesc", panel.transform as RectTransform, string.Empty, 18, TextAnchor.UpperCenter);
            SetRect(bossDesc.rectTransform, new Vector2(0.3105f, 0.050f), new Vector2(0.6895f, 0.128f), Vector2.zero, Vector2.zero);
            bossDesc.color = ScrollUiKit.Vermilion;
            bossDesc.gameObject.SetActive(false);
        }

        // The caption under a round button: name, then the key. 100 px wide, the gap to the neighbouring button, so
        // two captions never overlap; long translations wrap onto another line.
        private static void BuildRoundButtonCaption(Button button, string name, string key)
        {
            var caption = ScrollUiKit.Label(button.transform, "ShortcutLabel", name + "\n(" + key + ")", 15, TextAnchor.UpperCenter,
                new Color(0.95f, 0.91f, 0.82f, 1f), new Vector2(-0.33f, -1.1f), new Vector2(1.33f, -0.06f));
            caption.lineSpacing = 0.95f;
            ScrollUiKit.SnapToPixels(caption.gameObject);
            var shadow = caption.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        // A bar turned into a paper lantern: "<id>BarBg" with its "<id>BarFill" child (the HUD sets the fill amount and
        // colour) and the "SudokuGameplay<id>" label inside. Returns the label.
        private Text BuildLantern(RectTransform panel, string id, Vector2 min, Vector2 max, Color shade, Color glow, string initialText)
        {
            var bg = EnsureRect(id + "BarBg", panel, min, max, Vector2.zero, Vector2.zero);
            var bgImg = EnsureOrGetImage(bg.gameObject, shade);
            bgImg.raycastTarget = false;
            var fillGo = new GameObject(id + "BarFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bg, false);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = glow;
            fillImg.raycastTarget = false;
            fillImg.fillAmount = 1f;
            ScrollUiKit.MakeLantern(bgImg, fillImg, shade);

            var label = BuildText("SudokuGameplay" + id, panel, initialText, 24, TextAnchor.MiddleCenter);
            SetRect(label.rectTransform, new Vector2(min.x, Mathf.Lerp(min.y, max.y, 0.30f)), new Vector2(max.x, Mathf.Lerp(min.y, max.y, 0.70f)),
                Vector2.zero, Vector2.zero);
            label.color = new Color(0.18f, 0.08f, 0.03f, 1f);
            label.fontSize = 32;               // the value; the HUD writes the name above it in a smaller size
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 0.9f;
            return label;
        }

        private void BuildGameOverPanel(RectTransform root)
        {
            Text summaryText, detailsText;
            Button backButton;
            var panel = EnsureRect("GameOverPanel", root, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(0.10f, 0.06f, 0.06f, 0.40f)); // scrim: bg_victory/bg_defeat loaded at runtime
            InRunUiFactory.AddPanelBackground(panel.transform, ""); // texture set in EndScreenViewController.ShowGameOver
            var panelOutline = EnsureComponent<Outline>(panel);
            panelOutline.effectColor = new Color(0.70f, 0.20f, 0.15f, 0.55f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            // Dynamic title — "Game Over" or "Victory!" set at runtime
            summaryText = BuildText("GameOverTitle", panel.transform as RectTransform,
                T("InRun.Blueprint.GameOverTitle", "Game Over"), 34, TextAnchor.UpperCenter);
            summaryText.fontStyle = FontStyle.Bold;
            SetRect(summaryText.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
            var titleShadow = EnsureComponent<Shadow>(summaryText.gameObject);
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.80f);
            titleShadow.effectDistance = new Vector2(1f, -1f);

            // Details: runs, XP, level progress — larger area for readability
            var detailsScrim = EnsureRect("GameOverDetailsScrim", panel.transform as RectTransform,
                new Vector2(0.06f, 0.06f), new Vector2(0.60f, 0.84f), Vector2.zero, Vector2.zero);
            var detailsScrimImg = EnsureOrGetImage(detailsScrim.gameObject, new Color(0.03f, 0.025f, 0.02f, 0.68f));
            detailsScrimImg.raycastTarget = false;

            detailsText = BuildText("GameOverDetails", panel.transform as RectTransform, string.Empty, 14, TextAnchor.UpperLeft);
            SetRect(detailsText.rectTransform, new Vector2(0.08f, 0.09f), new Vector2(0.58f, 0.81f), Vector2.zero, Vector2.zero);
            detailsText.color = InRunUiFactory.WarmIvory;
            detailsText.lineSpacing = 1.18f;
            var detailsShadow = EnsureComponent<Shadow>(detailsText.gameObject);
            detailsShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            detailsShadow.effectDistance = new Vector2(1f, -1f);

            backButton = BuildButton("BtnGameOverBack", panel.transform as RectTransform, T("Back", "Back"), 18);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.66f, 0.06f), new Vector2(0.93f, 0.15f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(backButton, "back_leaf", "ui"); // F16: back/close uses dedicated back_leaf icon
        }

        // [REQ: UI-OPT-001] [REQ: UI-OPT-002] [REQ: UI-OPT-003] GAP-029 / FUTURE-015: the main-menu Options tabs and
        // controls (OptionsUiFactory), plus Accessibility and Run tabs. Object names carry the "IG" prefix.
        private GameObject BuildInGameOptionsPanel(RectTransform root)
        {
            var optCtrl = EnsureComponent<OptionsController>(gameObject);

            var panel = EnsureRect("InGameOptionsPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(0.055f, 0.07f, 0.065f, 1f));
            InRunUiFactory.ClearNamedChildren(panel.transform, "PanelBackground");
            InRunUiFactory.ClearNamedChildren(panel.transform, "Content");

            var pr = OptionsUiStyle.MakeCard(panel.transform as RectTransform);
            var runScreen = root.GetComponent<InRunController>() ?? root.GetComponentInParent<InRunController>();

            var title = OptionsUiFactory.BuildText("InGameOptionsTitle", pr, T("Options", "Options"),
                OptionsUiStyle.TitleSize, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.94f), new Vector2(0.92f, 0.99f), Vector2.zero, Vector2.zero);

            var tabs = OptionsTabSet.On(pr);
            var refreshStandard = OptionsUiFactory.BuildStandardTabs(tabs, optCtrl, new OptionsUiFactory.Setup
            {
                Prefix = "IG",
                OnHighlightConflictsChanged = v => { if (runScreen != null) runScreen.SetHighlightConflictsLive(v); }
            });

            var accIndex = tabs.Count;
            var accSec = tabs.Add("IGTabAccessibility", "IGAccessibilitySection", T("Accessibility", "Accessibility"));
            var refreshAccessibility = OptionsUiFactory.BuildAccessibilitySection(tabs, accIndex, accSec, optCtrl, "IG");

            // ── Run ── [REQ: REQ-RUN-101] seed, View Modifiers, Abandon Run (GAP-020); wired in InRunController.RunMenu.
            var runIndex = tabs.Count;
            var runSec = tabs.Add("IGTabRun", "IGRunSection", T("InRun.Options.RunTab", "Run"));
            OptionsUiFactory.SectionHeader("IGRunHdr", runSec, T("InRun.Options.RunTab", "Run"));
            var seedTxt = OptionsUiFactory.BuildText("IGRunSeed", runSec, "", 15, TextAnchor.MiddleLeft);
            SetRect(seedTxt.rectTransform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero);
            var modsBtn = OptionsUiFactory.BuildButton("BtnInGameOptionsModifiers", runSec, T("View Modifiers", "View Modifiers"), 16);
            SetRect(modsBtn.GetComponent<RectTransform>(), new Vector2(0.20f, 0.56f), new Vector2(0.80f, 0.68f), Vector2.zero, Vector2.zero);
            var saveQuitBtn = OptionsUiFactory.BuildButton("BtnInGameOptionsSaveQuit", runSec, T("InRun.Options.SaveQuit", "Save & Quit"), 16);
            SetRect(saveQuitBtn.GetComponent<RectTransform>(), new Vector2(0.20f, 0.40f), new Vector2(0.80f, 0.52f), Vector2.zero, Vector2.zero);
            if (runScreen != null)
                saveQuitBtn.onClick.AddListener(runScreen.SaveAndQuitPublic);
            var abandonBtn = OptionsUiFactory.BuildButton("BtnInGameOptionsAbandon", runSec, T("Abandon Run", "Abandon Run"), 16);
            SetRect(abandonBtn.GetComponent<RectTransform>(), new Vector2(0.20f, 0.18f), new Vector2(0.80f, 0.30f), Vector2.zero, Vector2.zero);
            tabs.SetRows(runIndex, new Selectable[] { modsBtn }, new Selectable[] { saveQuitBtn }, new Selectable[] { abandonBtn });

            // ── Back, always visible ──
            var closeBtn = OptionsUiFactory.BuildButton("BtnInGameOptionsClose", pr, T("Back", "Back"), 18);
            SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.30f, 0.015f), new Vector2(0.70f, 0.085f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(closeBtn, "back_leaf", "ui"); // F16: back/close uses dedicated back_leaf icon
            tabs.SetFooter(new Selectable[] { closeBtn });
            tabs.Activate(0);

            // Resets and profile switches raise OnOptionsChanged; replace the handler of an earlier build.
            if (_optionsRefreshSource != null && _optionsRefresh != null)
                _optionsRefreshSource.OnOptionsChanged -= _optionsRefresh;
            _optionsRefresh = () =>
            {
                refreshStandard();
                refreshAccessibility();
            };
            _optionsRefreshSource = optCtrl;
            optCtrl.OnOptionsChanged += _optionsRefresh;

            return panel;
        }

        private Text BuildTutorialBanner(RectTransform root)
        {
            var bannerPanel = EnsureRect("TutorialBannerPanel", root,
                new Vector2(0.66f, 0.835f), new Vector2(0.98f, 0.920f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(bannerPanel, new Color(0.04f, 0.06f, 0.04f, 0.72f));
            // bg_tutorial_progress removed — the dark tinted Image above is sufficient background.

            var label = BuildText("TutorialBanner", bannerPanel.transform as RectTransform,
                T("InRun.Blueprint.TutorialBanner", "TUTORIAL MODE\nNo Progression Rewards"),
                bodyFontSize, TextAnchor.UpperRight);
            SetRect(label.rectTransform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);
            label.color = accentColor;
            return label;
        }

        private static void EnsureMainCamera()
        {
            if (Object.FindFirstObjectByType<Camera>() != null)
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

        private RectTransform EnsureRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
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

        private static void ApplyButtonIcon(Button btn, string iconName, string subfolder = "GeneratedIcons")
        {
            var sprite = Resources.Load<Sprite>(subfolder + "/icon_" + iconName);
            if (sprite == null) return;

            // Find or destroy existing Icon child to avoid duplicates on rebuild
            var existingIcon = btn.transform.Find("Icon");
            if (existingIcon != null) DestroyImmediate(existingIcon.gameObject);

            // Shift label text right to make room for icon
            var label = btn.transform.Find("Label")?.GetComponent<RectTransform>();
            if (label != null)
                label.offsetMin = new Vector2(50f, label.offsetMin.y);

            // Anchor-based height (80% of button) + fixed width so the icon
            // scales with the button and never overflows a small rect.
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(btn.transform, false);
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0f, 0.10f);
            ir.anchorMax = new Vector2(0f, 0.90f);
            ir.pivot     = new Vector2(0f, 0.5f);
            ir.anchoredPosition = new Vector2(6f, 0f);
            ir.sizeDelta = new Vector2(36f, 0f); // height from anchors; width fixed at 36px
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        private Button BuildButton(string name, RectTransform parent, string label, int size)
        {
            var rect = EnsureRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var image = EnsureOrGetImage(rect.gameObject, buttonColor);
            image.raycastTarget = true;

            var button = EnsureComponent<Button>(rect.gameObject);
            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = new Color(buttonColor.r + 0.05f, buttonColor.g + 0.05f, buttonColor.b + 0.05f, buttonColor.a);
            colors.pressedColor = new Color(buttonColor.r - 0.04f, buttonColor.g - 0.04f, buttonColor.b - 0.04f, buttonColor.a);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
            button.colors = colors;

            var labelRect = EnsureRect("Label", rect, Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -8f));
            var text = EnsureComponent<Text>(labelRect.gameObject);
            text.text = label;
            text.font = GetBuiltInFont();
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = textColor;

            return button;
        }

        // V2: delegates to InRunUiFactory.GetFont() so both builders use the same
        //     Resources/Fonts/MainFont.ttf path, falling back to the Unity built-in.
        private static Font GetBuiltInFont() => FontAssetService.GetFont();

        private static Sprite BuildWhiteSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var white = Color.white;
            for (var py = 0; py < 4; py++)
                for (var px = 0; px < 4; px++)
                    tex.SetPixel(px, py, white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        private Image BuildImageTemplate(string name, RectTransform parent, Color color, Vector2 size)
        {
            var rect = EnsureRect(name, parent, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(-9999f, -9999f);
            var image = EnsureOrGetImage(rect.gameObject, color);
            return image;
        }
    }
}
