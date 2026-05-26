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
        // Unified dark box color used across the Sudoku Puzzle screen.
        private static readonly Color ScrimColor = InRunUiFactory.SudokuPuzzleBoxBg;

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

            var eventPanel = BuildEventPanel(root);
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
                eventPanel.GetComponent<EventChoiceScreenController>(),
                cursePanel.GetComponent<CursePanelController>());

            // Keep legacy prototype helper panels out of the main redesign flow.
            eventPanel.SetActive(false);
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

        private GameObject BuildEventPanel(RectTransform root)
        {
            var panel = EnsureRect("EventPanel", root, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.78f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(panelColor.r, panelColor.g, panelColor.b, 0.40f)); // scrim: bg_event.png shows through
            InRunUiFactory.AddPanelBackground(panel.transform, "bg_event");

            var controller = EnsureComponent<EventChoiceScreenController>(panel);

            var title = BuildText("PromptText", panel.transform as RectTransform,
                T("InRun.Blueprint.EventTitle", "Event"), titleFontSize, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);

            var optionsArea = EnsureRect("OptionsRoot", panel.transform as RectTransform, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.70f), Vector2.zero, Vector2.zero);
            var layout = EnsureComponent<VerticalLayoutGroup>(optionsArea.gameObject);
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(8, 8, 8, 8);
            EnsureComponent<ContentSizeFitter>(optionsArea.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EnsureComponent<RectMask2D>(optionsArea.gameObject); // clips options that exceed the anchored region

            var result = BuildText("ResultText", panel.transform as RectTransform, string.Empty, smallFontSize, TextAnchor.LowerLeft);
            SetRect(result.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.75f, 0.18f), Vector2.zero, Vector2.zero);

            var closeButton = BuildButton("CloseButton", panel.transform as RectTransform, T("Back", "Back"), bodyFontSize);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(closeButton, "back_leaf", "ui"); // F16: back/close uses dedicated back_leaf icon

            controller.Configure(panel, title, result, optionsArea);

            panel.SetActive(false);
            return panel;
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

        private void BuildSudokuGameplayPanel(RectTransform root)
        {
            RectTransform gridRoot, numpadRoot;
            Button saveQuit, optionsButton;
            Text statusText, hpText, pencilText;
            var panel = EnsureRect("SudokuGameplayPanel", root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(panelColor.r, panelColor.g, panelColor.b, 0.40f)); // scrim: bg_puzzle_board.png shows through
            InRunUiFactory.AddPanelBackground(panel.transform, "bg_puzzle_board");

            // Dark scrim behind the center-top info band — created before the title so it renders beneath it.
            var infoScrim = EnsureRect("InfoScrimBg", panel.transform as RectTransform,
                new Vector2(0.22f, 0.79f), new Vector2(0.70f, 1.00f), Vector2.zero, Vector2.zero);
            var infoScrimImg = EnsureOrGetImage(infoScrim.gameObject, ScrimColor);
            infoScrimImg.raycastTarget = false;

            // Title is a child of InfoScrimBg so it always renders above the scrim and can't drift out of it.
            // Anchors within the scrim: top 67–100% maps to panel y=0.93–1.00 within scrim y=0.79–1.00.
            var title = BuildText("SudokuGameplayTitle", infoScrim,
                T("InRun.Blueprint.SudokuGameplayTitle", "Sudoku Puzzle"), 34, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.03f, 0.66f), new Vector2(0.97f, 1.00f), Vector2.zero, Vector2.zero);

            var levelInfo = BuildText("SudokuGameplayLevelInfo", panel.transform as RectTransform,
                T("InRun.Blueprint.LevelDepthPlaceholder", "Level: -  Depth: -"), 18, TextAnchor.MiddleCenter);
            SetRect(levelInfo.rectTransform, new Vector2(0.22f, 0.83f), new Vector2(0.70f, 0.875f), Vector2.zero, Vector2.zero);

            statusText = BuildText("SudokuGameplayStatus", panel.transform as RectTransform,
                T("InRun.Blueprint.SelectPathStatus", "Select a path and start solving."), 19, TextAnchor.MiddleLeft);
            SetRect(statusText.rectTransform, new Vector2(0.22f, 0.79f), new Vector2(0.70f, 0.83f), Vector2.zero, Vector2.zero);

            // Unified scrim covering class badge + HP + Pencil, all within BAG column width
            var hpPencilScrim = EnsureRect("HpPencilScrim", panel.transform as RectTransform,
                new Vector2(0.01f, 0.75f), new Vector2(0.21f, 1.00f), Vector2.zero, Vector2.zero);
            var hpPencilScrimImg = EnsureOrGetImage(hpPencilScrim.gameObject, ScrimColor);
            hpPencilScrimImg.raycastTarget = false;
            // Remove any stray Outline component that produces a double-border effect
            var scrimOutline = hpPencilScrim.GetComponent<UnityEngine.UI.Outline>();
            if (scrimOutline != null) UnityEngine.Object.Destroy(scrimOutline);

            hpText = BuildText("SudokuGameplayHp", panel.transform as RectTransform,
                T("InRun.Blueprint.HpPlaceholder", "HP: -"), 18, TextAnchor.MiddleLeft);
            SetRect(hpText.rectTransform, new Vector2(0.02f, 0.85f), new Vector2(0.09f, 0.89f), Vector2.zero, Vector2.zero);

            var hpBarBgRect = EnsureRect("HpBarBg", panel.transform as RectTransform, new Vector2(0.09f, 0.847f), new Vector2(0.20f, 0.878f), Vector2.zero, Vector2.zero);
            var hpBarBgImg = EnsureOrGetImage(hpBarBgRect.gameObject, new Color(0.08f, 0.06f, 0.06f, 0.85f));
            hpBarBgImg.raycastTarget = false;
            var hpBarFillGo = new GameObject("HpBarFill", typeof(RectTransform), typeof(Image));
            hpBarFillGo.transform.SetParent(hpBarBgRect, false);
            var hpBarFillRect = hpBarFillGo.GetComponent<RectTransform>();
            hpBarFillRect.anchorMin = Vector2.zero;
            hpBarFillRect.anchorMax = Vector2.one;
            hpBarFillRect.offsetMin = Vector2.zero;
            hpBarFillRect.offsetMax = Vector2.zero;
            var hpBarFillImg = hpBarFillGo.GetComponent<Image>();
            hpBarFillImg.sprite = BuildWhiteSprite();
            hpBarFillImg.color = new Color(0.75f, 0.22f, 0.22f, 0.90f);
            hpBarFillImg.type = Image.Type.Filled;
            hpBarFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpBarFillImg.fillOrigin = 0;
            hpBarFillImg.fillAmount = 1f;
            hpBarFillImg.raycastTarget = false;

            pencilText = BuildText("SudokuGameplayPencil", panel.transform as RectTransform,
                T("InRun.Blueprint.PencilPlaceholder", "Pencil: -"), 18, TextAnchor.MiddleLeft);
            SetRect(pencilText.rectTransform, new Vector2(0.02f, 0.79f), new Vector2(0.09f, 0.83f), Vector2.zero, Vector2.zero);

            var pencilBarBgRect = EnsureRect("PencilBarBg", panel.transform as RectTransform, new Vector2(0.09f, 0.777f), new Vector2(0.20f, 0.808f), Vector2.zero, Vector2.zero);
            var pencilBarBgImg = EnsureOrGetImage(pencilBarBgRect.gameObject, new Color(0.06f, 0.06f, 0.08f, 0.85f));
            pencilBarBgImg.raycastTarget = false;
            var pencilBarFillGo = new GameObject("PencilBarFill", typeof(RectTransform), typeof(Image));
            pencilBarFillGo.transform.SetParent(pencilBarBgRect, false);
            var pencilBarFillRect = pencilBarFillGo.GetComponent<RectTransform>();
            pencilBarFillRect.anchorMin = Vector2.zero;
            pencilBarFillRect.anchorMax = Vector2.one;
            pencilBarFillRect.offsetMin = Vector2.zero;
            pencilBarFillRect.offsetMax = Vector2.zero;
            var pencilBarFillImg = pencilBarFillGo.GetComponent<Image>();
            pencilBarFillImg.sprite = BuildWhiteSprite();
            pencilBarFillImg.color = new Color(0.22f, 0.55f, 0.80f, 0.90f);
            pencilBarFillImg.type = Image.Type.Filled;
            pencilBarFillImg.fillMethod = Image.FillMethod.Horizontal;
            pencilBarFillImg.fillOrigin = 0;
            pencilBarFillImg.fillAmount = 1f;
            pencilBarFillImg.raycastTarget = false;

            saveQuit = BuildButton("BtnSudokuSaveQuit", panel.transform as RectTransform,
                T("InRun.Path.SaveAndQuit", "Save & Quit"), 18);
            SetRect(saveQuit.GetComponent<RectTransform>(), new Vector2(0.74f, 0.01f), new Vector2(0.93f, 0.08f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(saveQuit, "ink_save", "ui");
            var sqShortcut = BuildText("ShortcutLabel", saveQuit.GetComponent<RectTransform>(), "(Q)", 10, TextAnchor.LowerRight);
            SetRect(sqShortcut.rectTransform, new Vector2(0.60f, 0.05f), new Vector2(0.98f, 0.48f), Vector2.zero, Vector2.zero);
            sqShortcut.color = GamePalette.TextMuted;

            optionsButton = BuildButton("BtnSudokuOptions", panel.transform as RectTransform, T("Options", "Options"), 18);
            SetRect(optionsButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.34f), new Vector2(0.93f, 0.41f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(optionsButton, "stone_gear", "ui");
            var optShortcut = BuildText("ShortcutLabel", optionsButton.GetComponent<RectTransform>(), "(O)", 10, TextAnchor.LowerRight);
            SetRect(optShortcut.rectTransform, new Vector2(0.60f, 0.05f), new Vector2(0.98f, 0.48f), Vector2.zero, Vector2.zero);
            optShortcut.color = GamePalette.TextMuted;

            // Keyboard nav: Save&Quit ↔ Options (vertical pair)
            InRunUiFactory.ApplyFocusColors(saveQuit);
            InRunUiFactory.ApplyFocusColors(optionsButton);
            var sqNav = saveQuit.navigation;
            sqNav.mode = Navigation.Mode.Explicit;
            sqNav.selectOnUp = optionsButton;
            saveQuit.navigation = sqNav;
            var optNav = optionsButton.navigation;
            optNav.mode = Navigation.Mode.Explicit;
            optNav.selectOnDown = saveQuit;
            optionsButton.navigation = optNav;

            gridRoot = EnsureRect("SudokuGameplayGridRoot", panel.transform as RectTransform, new Vector2(0.22f, 0.16f), new Vector2(0.70f, 0.76f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(gridRoot.gameObject, InRunUiFactory.SudokuPuzzleBoxBg);

            numpadRoot = EnsureRect("SudokuGameplayNumpadRoot", panel.transform as RectTransform, new Vector2(0.74f, 0.42f), new Vector2(0.93f, 0.72f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(numpadRoot.gameObject, InRunUiFactory.SudokuPuzzleBoxBg);

            var hintBg = EnsureRect("SudokuGameplayHintBg", panel.transform as RectTransform,
                new Vector2(0.74f, 0.725f), new Vector2(0.93f, 0.845f), Vector2.zero, Vector2.zero);
            var hintBgImg = EnsureOrGetImage(hintBg.gameObject, InRunUiFactory.SudokuPuzzleBoxBg);
            hintBgImg.raycastTarget = false;

            var hint = BuildText("SudokuGameplayHint", panel.transform as RectTransform,
                T("InRun.Blueprint.PuzzleHint", "Use numpad buttons or keyboard 1-9.\nSingle click: select cell\nDouble click filled cell: highlight same numbers"),
                18, TextAnchor.UpperLeft);
            SetRect(hint.rectTransform, new Vector2(0.745f, 0.73f), new Vector2(0.925f, 0.84f), Vector2.zero, Vector2.zero);
            hint.color = GamePalette.TextPrimary;
            var hintShadow = hint.gameObject.AddComponent<Shadow>();
            hintShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            hintShadow.effectDistance = new Vector2(1f, -1f);

            var bossDesc = BuildText("SudokuGameplayBossDesc", panel.transform as RectTransform, string.Empty, 20, TextAnchor.UpperLeft);
            SetRect(bossDesc.rectTransform, new Vector2(0.74f, 0.09f), new Vector2(0.93f, 0.33f), Vector2.zero, Vector2.zero);
            bossDesc.color = new Color(1f, 0.65f, 0.30f, 1f);
            bossDesc.gameObject.SetActive(false);
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
                new Vector2(0.06f, 0.14f), new Vector2(0.60f, 0.84f), Vector2.zero, Vector2.zero);
            var detailsScrimImg = EnsureOrGetImage(detailsScrim.gameObject, new Color(0.03f, 0.025f, 0.02f, 0.68f));
            detailsScrimImg.raycastTarget = false;

            detailsText = BuildText("GameOverDetails", panel.transform as RectTransform, string.Empty, 14, TextAnchor.UpperLeft);
            SetRect(detailsText.rectTransform, new Vector2(0.08f, 0.17f), new Vector2(0.58f, 0.81f), Vector2.zero, Vector2.zero);
            detailsText.color = InRunUiFactory.WarmIvory;
            detailsText.lineSpacing = 1.18f;
            var detailsShadow = EnsureComponent<Shadow>(detailsText.gameObject);
            detailsShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            detailsShadow.effectDistance = new Vector2(1f, -1f);

            backButton = BuildButton("BtnGameOverBack", panel.transform as RectTransform, T("Back", "Back"), 18);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.66f, 0.06f), new Vector2(0.93f, 0.15f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(backButton, "back_leaf", "ui"); // F16: back/close uses dedicated back_leaf icon
        }

        private GameObject BuildInGameOptionsPanel(RectTransform root)
        {
            var optCtrl = EnsureComponent<OptionsController>(gameObject);

            var panel = EnsureRect("InGameOptionsPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(0.055f, 0.07f, 0.065f, 1f));
            InRunUiFactory.ClearNamedChildren(panel.transform, "PanelBackground");

            var pr = panel.transform as RectTransform;
            var opts = optCtrl.GetOptions();

            var title = BuildText("InGameOptionsTitle", pr, T("Options", "Options"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.93f), new Vector2(0.92f, 0.99f), Vector2.zero, Vector2.zero);

            // ── Audio ──
            var audioHdr = BuildText("IGAudioHdr", pr, T("Audio", "Audio"), 17, TextAnchor.MiddleLeft);
            audioHdr.color = new Color(0.75f, 0.72f, 0.55f, 1f);
            SetRect(audioHdr.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);

            string FormatPercent(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
            string FormatAudioLabel(string label, float value) => label + " " + FormatPercent(value);

            var masterText = T("Master Volume", "Master Volume");
            var masterLabel = BuildText("IGMasterLabel", pr, FormatAudioLabel(masterText, opts.Audio.MasterVolume), 15, TextAnchor.MiddleLeft);
            SetRect(masterLabel.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.48f, 0.88f), Vector2.zero, Vector2.zero);
            var masterSlider = BuildSlider("IGMasterSlider", pr);
            SetRect(masterSlider.GetComponent<RectTransform>(), new Vector2(0.05f, 0.80f), new Vector2(0.48f, 0.84f), Vector2.zero, Vector2.zero);
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.SetValueWithoutNotify(opts.Audio.MasterVolume);
            masterSlider.onValueChanged.AddListener(v => optCtrl.SetMasterVolume(v));
            var masterPct = BuildText("IGMasterPct", pr, FormatPercent(opts.Audio.MasterVolume), 13, TextAnchor.MiddleRight);
            SetRect(masterPct.rectTransform, new Vector2(0.38f, 0.84f), new Vector2(0.48f, 0.88f), Vector2.zero, Vector2.zero);
            masterPct.color = GamePalette.TextMuted;
            masterPct.raycastTarget = false;
            masterSlider.onValueChanged.AddListener(v =>
            {
                masterLabel.text = FormatAudioLabel(masterText, v);
                masterPct.text = FormatPercent(v);
            });

            var musicText = T("Music Volume", "Music Volume");
            var musicLabel = BuildText("IGMusicLabel", pr, FormatAudioLabel(musicText, opts.Audio.MusicVolume), 15, TextAnchor.MiddleLeft);
            SetRect(musicLabel.rectTransform, new Vector2(0.52f, 0.84f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
            var musicSlider = BuildSlider("IGMusicSlider", pr);
            SetRect(musicSlider.GetComponent<RectTransform>(), new Vector2(0.52f, 0.80f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero);
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.SetValueWithoutNotify(opts.Audio.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => optCtrl.SetMusicVolume(v));
            var musicPct = BuildText("IGMusicPct", pr, FormatPercent(opts.Audio.MusicVolume), 13, TextAnchor.MiddleRight);
            SetRect(musicPct.rectTransform, new Vector2(0.85f, 0.84f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
            musicPct.color = GamePalette.TextMuted;
            musicPct.raycastTarget = false;
            musicSlider.onValueChanged.AddListener(v =>
            {
                musicLabel.text = FormatAudioLabel(musicText, v);
                musicPct.text = FormatPercent(v);
            });

            var sfxText = T("SFX Volume", "SFX Volume");
            var sfxLabel = BuildText("IGSfxLabel", pr, FormatAudioLabel(sfxText, opts.Audio.SfxVolume), 15, TextAnchor.MiddleLeft);
            SetRect(sfxLabel.rectTransform, new Vector2(0.05f, 0.75f), new Vector2(0.48f, 0.79f), Vector2.zero, Vector2.zero);
            var sfxSlider = BuildSlider("IGSfxSlider", pr);
            SetRect(sfxSlider.GetComponent<RectTransform>(), new Vector2(0.05f, 0.71f), new Vector2(0.48f, 0.75f), Vector2.zero, Vector2.zero);
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(opts.Audio.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => optCtrl.SetSfxVolume(v));
            var sfxPct = BuildText("IGSfxPct", pr, FormatPercent(opts.Audio.SfxVolume), 13, TextAnchor.MiddleRight);
            SetRect(sfxPct.rectTransform, new Vector2(0.38f, 0.75f), new Vector2(0.48f, 0.79f), Vector2.zero, Vector2.zero);
            sfxPct.color = GamePalette.TextMuted;
            sfxPct.raycastTarget = false;
            sfxSlider.onValueChanged.AddListener(v =>
            {
                sfxLabel.text = FormatAudioLabel(sfxText, v);
                sfxPct.text = FormatPercent(v);
            });

            var uiText = T("UI Volume", "UI Volume");
            var uiLabel = BuildText("IGUiLabel", pr, FormatAudioLabel(uiText, opts.Audio.UiVolume), 15, TextAnchor.MiddleLeft);
            SetRect(uiLabel.rectTransform, new Vector2(0.52f, 0.75f), new Vector2(0.95f, 0.79f), Vector2.zero, Vector2.zero);
            var uiSlider = BuildSlider("IGUiSlider", pr);
            SetRect(uiSlider.GetComponent<RectTransform>(), new Vector2(0.52f, 0.71f), new Vector2(0.95f, 0.75f), Vector2.zero, Vector2.zero);
            uiSlider.minValue = 0f;
            uiSlider.maxValue = 1f;
            uiSlider.SetValueWithoutNotify(opts.Audio.UiVolume);
            uiSlider.onValueChanged.AddListener(v => optCtrl.SetUiVolume(v));
            var uiPct = BuildText("IGUiPct", pr, FormatPercent(opts.Audio.UiVolume), 13, TextAnchor.MiddleRight);
            SetRect(uiPct.rectTransform, new Vector2(0.85f, 0.75f), new Vector2(0.95f, 0.79f), Vector2.zero, Vector2.zero);
            uiPct.color = GamePalette.TextMuted;
            uiPct.raycastTarget = false;
            uiSlider.onValueChanged.AddListener(v =>
            {
                uiLabel.text = FormatAudioLabel(uiText, v);
                uiPct.text = FormatPercent(v);
            });

            var muteTgl = BuildToggle("IGMuteWhenUnfocusedToggle", pr,
                T("InRun.Options.MuteWhenUnfocused", "Mute When Unfocused"));
            SetRect(muteTgl.GetComponent<RectTransform>(), new Vector2(0.05f, 0.66f), new Vector2(0.50f, 0.70f), Vector2.zero, Vector2.zero);
            muteTgl.SetIsOnWithoutNotify(opts.Audio.MuteWhenUnfocused);
            muteTgl.onValueChanged.AddListener(v => optCtrl.SetMuteWhenUnfocused(v));

            // ── Display ──
            var displayHdr = BuildText("IGDisplayHdr", pr, T("Display", "Display"), 17, TextAnchor.MiddleLeft);
            displayHdr.color = new Color(0.75f, 0.72f, 0.55f, 1f);
            SetRect(displayHdr.rectTransform, new Vector2(0.05f, 0.61f), new Vector2(0.95f, 0.65f), Vector2.zero, Vector2.zero);

            var fullscreenTgl = BuildToggle("IGFullscreenToggle", pr, T("Fullscreen", "Fullscreen"));
            SetRect(fullscreenTgl.GetComponent<RectTransform>(), new Vector2(0.05f, 0.56f), new Vector2(0.50f, 0.60f), Vector2.zero, Vector2.zero);
            fullscreenTgl.SetIsOnWithoutNotify(opts.Graphics.Fullscreen);
            fullscreenTgl.onValueChanged.AddListener(v => optCtrl.SetFullscreen(v));

            // ── Gameplay ──
            var gameHdr = BuildText("IGGameHdr", pr, T("Gameplay", "Gameplay"), 17, TextAnchor.MiddleLeft);
            gameHdr.color = new Color(0.75f, 0.72f, 0.55f, 1f);
            SetRect(gameHdr.rectTransform, new Vector2(0.05f, 0.51f), new Vector2(0.95f, 0.55f), Vector2.zero, Vector2.zero);

            var highlightToggle = BuildToggle("IGHighlightToggle", pr, T("Highlight Errors", "Highlight Errors"));
            SetRect(highlightToggle.GetComponent<RectTransform>(), new Vector2(0.05f, 0.46f), new Vector2(0.50f, 0.50f), Vector2.zero, Vector2.zero);
            highlightToggle.SetIsOnWithoutNotify(opts.Gameplay.HighlightConflicts);
            highlightToggle.onValueChanged.AddListener(v =>
            {
                optCtrl.SetHighlightConflicts(v);
                var rs = root.GetComponent<InRunController>();
                if (rs != null) rs.SetHighlightConflictsLive(v);
            });

            // ── Accessibility ──
            var accHdr = BuildText("IGAccHdr", pr, T("Accessibility", "Accessibility"), 17, TextAnchor.MiddleLeft);
            accHdr.color = new Color(0.75f, 0.72f, 0.55f, 1f);
            SetRect(accHdr.rectTransform, new Vector2(0.05f, 0.41f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero);

            var colorblindTgl = BuildToggle("IGColorblindToggle", pr, T("Colorblind Mode", "Colorblind Mode"));
            SetRect(colorblindTgl.GetComponent<RectTransform>(), new Vector2(0.05f, 0.36f), new Vector2(0.50f, 0.40f), Vector2.zero, Vector2.zero);
            colorblindTgl.SetIsOnWithoutNotify(opts.Accessibility.ColorblindMode);
            colorblindTgl.onValueChanged.AddListener(v => optCtrl.SetColorblindMode(v));

            var highContrastTgl = BuildToggle("IGHighContrastToggle", pr, T("High Contrast", "High Contrast"));
            SetRect(highContrastTgl.GetComponent<RectTransform>(), new Vector2(0.52f, 0.36f), new Vector2(0.97f, 0.40f), Vector2.zero, Vector2.zero);
            highContrastTgl.SetIsOnWithoutNotify(opts.Accessibility.HighContrastMode);
            highContrastTgl.onValueChanged.AddListener(v => optCtrl.SetHighContrast(v));

            var reduceMotionTgl = BuildToggle("IGReduceMotionToggle", pr, T("Reduce Motion", "Reduce Motion"));
            SetRect(reduceMotionTgl.GetComponent<RectTransform>(), new Vector2(0.05f, 0.30f), new Vector2(0.50f, 0.34f), Vector2.zero, Vector2.zero);
            reduceMotionTgl.SetIsOnWithoutNotify(opts.Accessibility.ReduceMotion);
            reduceMotionTgl.onValueChanged.AddListener(v => optCtrl.SetReduceMotion(v));

            var altSymbolsTgl = BuildToggle("IGAltSymbolsToggle", pr,
                T("InRun.Options.AltSymbols", "Alt Symbols"));
            SetRect(altSymbolsTgl.GetComponent<RectTransform>(), new Vector2(0.52f, 0.30f), new Vector2(0.97f, 0.34f), Vector2.zero, Vector2.zero);
            altSymbolsTgl.SetIsOnWithoutNotify(opts.Accessibility.AlternativeConstraintSymbols);
            altSymbolsTgl.onValueChanged.AddListener(v => optCtrl.SetAltSymbols(v));

            var closeBtn = BuildButton("BtnInGameOptionsClose", pr, T("Back", "Back"), 18);
            SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            ApplyButtonIcon(closeBtn, "back_leaf", "ui"); // F16: back/close uses dedicated back_leaf icon

            // Apply gold focus highlight to all interactive elements
            InRunUiFactory.ApplyFocusColors(masterSlider);
            InRunUiFactory.ApplyFocusColors(musicSlider);
            InRunUiFactory.ApplyFocusColors(sfxSlider);
            InRunUiFactory.ApplyFocusColors(uiSlider);
            InRunUiFactory.ApplyFocusColors(muteTgl);
            InRunUiFactory.ApplyFocusColors(fullscreenTgl);
            InRunUiFactory.ApplyFocusColors(highlightToggle);
            InRunUiFactory.ApplyFocusColors(colorblindTgl);
            InRunUiFactory.ApplyFocusColors(highContrastTgl);
            InRunUiFactory.ApplyFocusColors(reduceMotionTgl);
            InRunUiFactory.ApplyFocusColors(altSymbolsTgl);

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
