using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class InRunUiBlueprintBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunMapController runMapController;

        [Header("Palette")]
        [SerializeField] private Color panelColor = new(0.08f, 0.10f, 0.13f, 0.88f);
        [SerializeField] private Color accentColor = new(0.30f, 0.60f, 0.55f, 1f);
        [SerializeField] private Color textColor = new(0.92f, 0.93f, 0.90f, 1f);
        [SerializeField] private Color buttonColor = new(0.20f, 0.26f, 0.31f, 1f);

        [Header("Typography")]
        [SerializeField] private int titleFontSize = 24;
        [SerializeField] private int bodyFontSize = 18;
        [SerializeField] private int smallFontSize = 15;

        [ContextMenu("Build In-Run UI Blueprint")]
        public void BuildBlueprint()
        {
            var canvas = EnsureCanvas();
            var root = EnsureRect("InRunUI", canvas.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(root.gameObject, new Color(0f, 0f, 0f, 0f));

            var flow = EnsureComponent<InRunUiFlowController>(root.gameObject);

            var eventPanel = BuildEventPanel(root);
            var cursePanel = BuildCursePanel(root);
            var heatPanel = BuildHeatGraphPanel(root);

            flow.Configure(
                runMapController,
                eventPanel.GetComponent<EventChoiceScreenController>(),
                cursePanel.GetComponent<CursePanelController>(),
                heatPanel.GetComponent<HeatCurveGraphController>());
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = textColor;

            return button;
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
