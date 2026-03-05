using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SudokuRoguelike.UI
{
    public sealed class InRunUiBlueprintBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunMapController runMapController;

        [Header("Palette")]
        [SerializeField] private Color panelColor = new(0.10f, 0.15f, 0.12f, 0.90f);
        [SerializeField] private Color accentColor = new(0.56f, 0.72f, 0.42f, 1f);
        [SerializeField] private Color textColor = new(0.92f, 0.96f, 0.89f, 1f);
        [SerializeField] private Color buttonColor = new(0.18f, 0.27f, 0.20f, 1f);

        [Header("Typography")]
        [SerializeField] private int titleFontSize = 24;
        [SerializeField] private int bodyFontSize = 18;
        [SerializeField] private int smallFontSize = 15;

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

            var canvas = EnsureCanvas();
            EnsureEventSystem();
            var root = EnsureRect("InRunUI", canvas.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(root.gameObject, new Color(0f, 0f, 0f, 0f));

            var flow = EnsureComponent<InRunUiFlowController>(root.gameObject);
            var debug = EnsureComponent<PrototypeUiDebugHotkeys>(gameObject);
            var tutorialBanner = EnsureComponent<TutorialRunBannerController>(root.gameObject);
            var boardPreview = EnsureComponent<SudokuBoardPreviewController>(root.gameObject);
            var runScreen = EnsureComponent<PrototypeRunScreenController>(root.gameObject);

            var eventPanel = BuildEventPanel(root);
            var cursePanel = BuildCursePanel(root);
            var heatPanel = BuildHeatGraphPanel(root);
            var debugPanel = BuildDebugPanel(root);
            var sudokuPanel = BuildSudokuBoardPanel(root, out var boardText, out var boardStatusText);
            var pathOverviewPanel = BuildPathOverviewPanel(root, out var pathOverviewText, out var laneAText, out var laneBText, out var laneAPathRoot, out var laneBPathRoot, out var chooseAButton, out var chooseBButton, out var saveQuitPathButton);
            var sudokuGameplayPanel = BuildSudokuGameplayPanel(root, out var sudokuGridRoot, out var numpadRoot, out var saveQuitSudokuButton, out var solveSudokuButton, out var sudokuGameplayStatusText, out var hpText, out var pencilText);
            var gameOverPanel = BuildGameOverPanel(root, out var gameOverSummaryText, out var gameOverDetailsText, out var gameOverBackButton);
            var tutorialLabel = BuildTutorialBanner(root);

            flow.Configure(
                runMapController,
                eventPanel.GetComponent<EventChoiceScreenController>(),
                cursePanel.GetComponent<CursePanelController>(),
                heatPanel.GetComponent<HeatCurveGraphController>());

            var pathPreviewText = debugPanel.transform.Find("PathPreviewText")?.GetComponent<Text>();
            debug.Configure(flow, runMapController, pathPreviewText);
            WireDebugButtons(debugPanel, debug);
            tutorialBanner.Configure(runMapController, tutorialLabel);
            boardPreview.Configure(runMapController, boardText, boardStatusText);
            runScreen.Configure(
                runMapController,
                pathOverviewPanel,
                sudokuGameplayPanel,
                pathOverviewText,
                laneAText,
                laneBText,
                laneAPathRoot,
                laneBPathRoot,
                chooseAButton,
                chooseBButton,
                saveQuitPathButton,
                saveQuitSudokuButton,
                sudokuGridRoot,
                numpadRoot,
                solveSudokuButton,
                sudokuGameplayStatusText,
                hpText,
                pencilText,
                gameOverPanel,
                gameOverSummaryText,
                gameOverDetailsText,
                gameOverBackButton);

            // Keep legacy prototype helper panels out of the main redesign flow.
            eventPanel.SetActive(false);
            cursePanel.SetActive(false);
            heatPanel.SetActive(false);
            debugPanel.SetActive(false);
            sudokuPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            tutorialLabel.gameObject.SetActive(false);

            if (runMapController == null)
            {
                Debug.LogWarning("InRunUiBlueprintBuilder: RunMapController was not found. Assign it on the builder or place one as a child.");
            }
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
            EnsureOrGetImage(panel, panelColor);

            var controller = EnsureComponent<EventChoiceScreenController>(panel);

            var title = BuildText("PromptText", panel.transform as RectTransform, "Event", titleFontSize, TextAnchor.UpperLeft);
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

            var result = BuildText("ResultText", panel.transform as RectTransform, string.Empty, smallFontSize, TextAnchor.LowerLeft);
            SetRect(result.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.75f, 0.18f), Vector2.zero, Vector2.zero);

            var closeButton = BuildButton("CloseButton", panel.transform as RectTransform, "Close", bodyFontSize);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.05f), new Vector2(0.96f, 0.18f), Vector2.zero, Vector2.zero);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(controller.CloseEvent);

            var buttonTemplate = BuildButton("OptionButtonTemplate", panel.transform as RectTransform, "Option Label — Tradeoff", bodyFontSize);
            SetRect(buttonTemplate.GetComponent<RectTransform>(), new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.30f), Vector2.zero, Vector2.zero);

            controller.Configure(panel, title, result, optionsArea, buttonTemplate);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildCursePanel(RectTransform root)
        {
            var panel = EnsureRect("CursePanel", root, new Vector2(0.02f, 0.58f), new Vector2(0.30f, 0.96f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);
            var controller = EnsureComponent<CursePanelController>(panel);

            var title = BuildText("TitleText", panel.transform as RectTransform, "Curses (0)", bodyFontSize, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);

            var list = BuildText("CurseListText", panel.transform as RectTransform, "No active curses.", smallFontSize, TextAnchor.UpperLeft);
            SetRect(list.rectTransform, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);

            var tension = BuildText("TensionText", panel.transform as RectTransform, "Heat pressure: 1.00", smallFontSize, TextAnchor.LowerLeft);
            SetRect(tension.rectTransform, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.20f), Vector2.zero, Vector2.zero);

            controller.Configure(title, list, tension);
            return panel;
        }

        private GameObject BuildHeatGraphPanel(RectTransform root)
        {
            var panel = EnsureRect("HeatGraphPanel", root, new Vector2(0.32f, 0.76f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);
            var controller = EnsureComponent<HeatCurveGraphController>(panel);

            var label = BuildText("YAxisLabel", panel.transform as RectTransform, "Heat 1.0 - 1.0", smallFontSize, TextAnchor.UpperLeft);
            SetRect(label.rectTransform, new Vector2(0.03f, 0.72f), new Vector2(0.30f, 0.95f), Vector2.zero, Vector2.zero);

            var graphRoot = EnsureRect("GraphRoot", panel.transform as RectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.97f, 0.68f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(graphRoot.gameObject, new Color(0f, 0f, 0f, 0.15f));

            var pointTemplate = BuildImageTemplate("PointTemplate", panel.transform as RectTransform, accentColor, new Vector2(10f, 10f));
            var segmentTemplate = BuildImageTemplate("SegmentTemplate", panel.transform as RectTransform, new Color(accentColor.r, accentColor.g, accentColor.b, 0.75f), new Vector2(32f, 3f));

            controller.Configure(graphRoot, pointTemplate, segmentTemplate, label);

            return panel;
        }

        private GameObject BuildDebugPanel(RectTransform root)
        {
            var panel = EnsureRect("DebugControlsPanel", root, new Vector2(0.32f, 0.58f), new Vector2(0.98f, 0.74f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("DebugTitle", panel.transform as RectTransform, "Garden Paths", bodyFontSize, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(0.02f, 0.66f), new Vector2(0.20f, 0.96f), Vector2.zero, Vector2.zero);

            var hint = BuildText("PathPreviewText", panel.transform as RectTransform, "Path A/B previews appear here.", smallFontSize, TextAnchor.UpperLeft);
            SetRect(hint.rectTransform, new Vector2(0.22f, 0.10f), new Vector2(0.64f, 0.92f), Vector2.zero, Vector2.zero);

            var calmPath = BuildButton("BtnPathA", panel.transform as RectTransform, "Path A (A)", smallFontSize);
            SetRect(calmPath.GetComponent<RectTransform>(), new Vector2(0.66f, 0.52f), new Vector2(0.79f, 0.88f), Vector2.zero, Vector2.zero);

            var riskyPath = BuildButton("BtnPathB", panel.transform as RectTransform, "Path B (B)", smallFontSize);
            SetRect(riskyPath.GetComponent<RectTransform>(), new Vector2(0.80f, 0.52f), new Vector2(0.93f, 0.88f), Vector2.zero, Vector2.zero);

            var quitAndSave = BuildButton("BtnQuitSave", panel.transform as RectTransform, "Save & Quit (Q)", smallFontSize);
            SetRect(quitAndSave.GetComponent<RectTransform>(), new Vector2(0.66f, 0.10f), new Vector2(0.93f, 0.46f), Vector2.zero, Vector2.zero);

            return panel;
        }

        private GameObject BuildSudokuBoardPanel(RectTransform root, out Text boardText, out Text statusText)
        {
            var panel = EnsureRect("SudokuBoardPanel", root, new Vector2(0.02f, 0.10f), new Vector2(0.30f, 0.56f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, panelColor);

            var title = BuildText("SudokuBoardTitle", panel.transform as RectTransform, "Sudoku Board", bodyFontSize, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            boardText = BuildText("SudokuBoardText", panel.transform as RectTransform, "", smallFontSize, TextAnchor.UpperLeft);
            SetRect(boardText.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            statusText = BuildText("SudokuBoardStatus", panel.transform as RectTransform, "", smallFontSize, TextAnchor.UpperLeft);
            SetRect(statusText.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero);

            return panel;
        }

        private GameObject BuildPathOverviewPanel(
            RectTransform root,
            out Text overviewText,
            out Text laneAText,
            out Text laneBText,
            out RectTransform laneAPathRoot,
            out RectTransform laneBPathRoot,
            out Button chooseA,
            out Button chooseB,
            out Button saveQuit)
        {
            var panel = EnsureRect("PathOverviewPanel", root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(panelColor.r, panelColor.g, panelColor.b, 0.97f));

            var title = BuildText("PathOverviewTitle", panel.transform as RectTransform, "Garden Path Overview", 34, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero);

            overviewText = BuildText("PathOverviewText", panel.transform as RectTransform, "", 20, TextAnchor.UpperCenter);
            SetRect(overviewText.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.91f), Vector2.zero, Vector2.zero);

            laneAText = BuildText("PathLaneAText", panel.transform as RectTransform, "", 18, TextAnchor.UpperLeft);
            SetRect(laneAText.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.45f, 0.80f), Vector2.zero, Vector2.zero);

            laneBText = BuildText("PathLaneBText", panel.transform as RectTransform, "", 18, TextAnchor.UpperLeft);
            SetRect(laneBText.rectTransform, new Vector2(0.55f, 0.72f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero);

            laneAPathRoot = BuildPathLaneScroll(
                "LaneAPathScroll",
                panel.transform as RectTransform,
                new Vector2(0.08f, 0.24f),
                new Vector2(0.45f, 0.70f),
                "LaneAPathRoot");

            laneBPathRoot = BuildPathLaneScroll(
                "LaneBPathScroll",
                panel.transform as RectTransform,
                new Vector2(0.55f, 0.24f),
                new Vector2(0.92f, 0.70f),
                "LaneBPathRoot");

            chooseA = null;
            chooseB = null;

            saveQuit = BuildButton("BtnPathOverviewSaveQuit", panel.transform as RectTransform, "Save & Quit (Q)", 20);
            SetRect(saveQuit.GetComponent<RectTransform>(), new Vector2(0.72f, 0.10f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);

            return panel;
        }

        private RectTransform BuildPathLaneScroll(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string contentName)
        {
            var scrollRoot = EnsureRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(scrollRoot.gameObject, new Color(0f, 0f, 0f, 0.12f));

            var viewport = EnsureRect("Viewport", scrollRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var viewportImage = EnsureOrGetImage(viewport.gameObject, new Color(1f, 1f, 1f, 0.02f));
            var mask = EnsureComponent<Mask>(viewport.gameObject);
            mask.showMaskGraphic = false;
            viewportImage.raycastTarget = true;

            var content = EnsureRect(contentName, viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(-8f, -8f));
            content.pivot = new Vector2(0.5f, 0.5f);

            var layout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var fitter = EnsureComponent<ContentSizeFitter>(content.gameObject);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = EnsureComponent<ScrollRect>(scrollRoot.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 22f;

            return content;
        }

        private GameObject BuildSudokuGameplayPanel(
            RectTransform root,
            out RectTransform gridRoot,
            out RectTransform numpadRoot,
            out Button saveQuit,
            out Button solveButton,
            out Text statusText,
            out Text hpText,
            out Text pencilText)
        {
            var panel = EnsureRect("SudokuGameplayPanel", root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(panelColor.r, panelColor.g, panelColor.b, 0.97f));

            var title = BuildText("SudokuGameplayTitle", panel.transform as RectTransform, "Sudoku Puzzle", 34, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero);

            var levelInfo = BuildText("SudokuGameplayLevelInfo", panel.transform as RectTransform, "Level: -  Depth: -", 18, TextAnchor.MiddleCenter);
            SetRect(levelInfo.rectTransform, new Vector2(0.30f, 0.87f), new Vector2(0.70f, 0.92f), Vector2.zero, Vector2.zero);

            statusText = BuildText("SudokuGameplayStatus", panel.transform as RectTransform, "Select a path and start solving.", 19, TextAnchor.MiddleLeft);
            SetRect(statusText.rectTransform, new Vector2(0.30f, 0.79f), new Vector2(0.72f, 0.86f), Vector2.zero, Vector2.zero);

            hpText = BuildText("SudokuGameplayHp", panel.transform as RectTransform, "HP: -", 18, TextAnchor.MiddleLeft);
            SetRect(hpText.rectTransform, new Vector2(0.03f, 0.83f), new Vector2(0.26f, 0.89f), Vector2.zero, Vector2.zero);

            pencilText = BuildText("SudokuGameplayPencil", panel.transform as RectTransform, "Pencil: -", 18, TextAnchor.MiddleLeft);
            SetRect(pencilText.rectTransform, new Vector2(0.03f, 0.76f), new Vector2(0.26f, 0.82f), Vector2.zero, Vector2.zero);

            saveQuit = BuildButton("BtnSudokuSaveQuit", panel.transform as RectTransform, "Save & Quit (Q)", 18);
            SetRect(saveQuit.GetComponent<RectTransform>(), new Vector2(0.80f, 0.84f), new Vector2(0.94f, 0.91f), Vector2.zero, Vector2.zero);

            solveButton = BuildButton("BtnSudokuSolve", panel.transform as RectTransform, "Solve", 18);
            SetRect(solveButton.GetComponent<RectTransform>(), new Vector2(0.73f, 0.84f), new Vector2(0.79f, 0.91f), Vector2.zero, Vector2.zero);

            gridRoot = EnsureRect("SudokuGameplayGridRoot", panel.transform as RectTransform, new Vector2(0.22f, 0.16f), new Vector2(0.70f, 0.80f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(gridRoot.gameObject, new Color(0f, 0f, 0f, 0.20f));

            numpadRoot = EnsureRect("SudokuGameplayNumpadRoot", panel.transform as RectTransform, new Vector2(0.74f, 0.28f), new Vector2(0.93f, 0.72f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(numpadRoot.gameObject, new Color(0f, 0f, 0f, 0.20f));

            var hint = BuildText("SudokuGameplayHint", panel.transform as RectTransform, "Use numpad buttons or keyboard 1-9.\nSingle click: select cell\nDouble click filled cell: highlight same numbers", 15, TextAnchor.UpperLeft);
            SetRect(hint.rectTransform, new Vector2(0.74f, 0.72f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);

            return panel;
        }

        private GameObject BuildGameOverPanel(RectTransform root, out Text summaryText, out Text detailsText, out Button backButton)
        {
            var panel = EnsureRect("GameOverPanel", root, new Vector2(0.22f, 0.20f), new Vector2(0.78f, 0.80f), Vector2.zero, Vector2.zero).gameObject;
            EnsureOrGetImage(panel, new Color(0.14f, 0.07f, 0.07f, 0.96f));

            var title = BuildText("GameOverTitle", panel.transform as RectTransform, "Game Over", 38, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.95f), Vector2.zero, Vector2.zero);

            summaryText = BuildText("GameOverSummary", panel.transform as RectTransform, string.Empty, 18, TextAnchor.UpperLeft);
            SetRect(summaryText.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero);

            detailsText = BuildText("GameOverDetails", panel.transform as RectTransform, string.Empty, 17, TextAnchor.UpperLeft);
            SetRect(detailsText.rectTransform, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.56f), Vector2.zero, Vector2.zero);

            backButton = BuildButton("BtnGameOverBack", panel.transform as RectTransform, "Back to Menu", 19);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.18f), Vector2.zero, Vector2.zero);

            return panel;
        }

        private Text BuildTutorialBanner(RectTransform root)
        {
            var label = BuildText("TutorialBanner", root, "TUTORIAL MODE\nNo Progression Rewards", bodyFontSize, TextAnchor.UpperRight);
            SetRect(label.rectTransform, new Vector2(0.66f, 0.92f), new Vector2(0.98f, 0.995f), Vector2.zero, Vector2.zero);
            label.color = accentColor;
            return label;
        }

        private static void WireDebugButtons(GameObject debugPanel, PrototypeUiDebugHotkeys debug)
        {
            if (debugPanel == null || debug == null)
            {
                return;
            }

            var calmPath = debugPanel.transform.Find("BtnPathA")?.GetComponent<Button>();
            var riskyPath = debugPanel.transform.Find("BtnPathB")?.GetComponent<Button>();
            var quit = debugPanel.transform.Find("BtnQuitSave")?.GetComponent<Button>();

            if (calmPath != null)
            {
                calmPath.onClick.RemoveAllListeners();
                calmPath.onClick.AddListener(debug.ChoosePathCalm);
            }

            if (riskyPath != null)
            {
                riskyPath.onClick.RemoveAllListeners();
                riskyPath.onClick.AddListener(debug.ChoosePathRisky);
            }

            if (quit != null)
            {
                quit.onClick.RemoveAllListeners();
                quit.onClick.AddListener(debug.QuitAndSave);
            }
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

        private static Font GetBuiltInFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
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
