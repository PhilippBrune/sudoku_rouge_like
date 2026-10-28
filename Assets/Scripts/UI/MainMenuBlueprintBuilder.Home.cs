using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-032] Main menu B · The garden path (owner's pick, 0.3.0 playtest, RM-217): the painting stays bright and
    // free; the title stands top left over a soft shade; the saved Garden Run is a paper card; the menu walks down the
    // left as wooden tablets (the chosen one slides out on paper); the Daily and Monthly Walk hang bottom right; the
    // profile sits top right. The object names (MenuCard, BtnStart, BtnResume, ...) stay for the controller and tests.
    public sealed partial class MainMenuBlueprintBuilder
    {
        // The menu column on the screen; everything that hides with the main menu is a child of it (MenuCard).
        private static readonly Vector2 CardMin = new Vector2(0.044f, 0.10f), CardMax = new Vector2(0.31f, 0.635f);
        private static readonly Color TabletWood = new Color(0.23f, 0.14f, 0.08f, 0.92f);
        private static readonly Color TabletText = new Color(0.95f, 0.91f, 0.82f, 1f);
        private static readonly Color TitleCream = new Color(0.95f, 0.87f, 0.65f, 1f);

        /// <summary>A screen rect as anchors on MenuCard (whose children may sit outside it).</summary>
        private static (Vector2 min, Vector2 max) OnCard(float xMin, float yMin, float xMax, float yMax)
        {
            var size = CardMax - CardMin;
            return (new Vector2((xMin - CardMin.x) / size.x, (yMin - CardMin.y) / size.y),
                    new Vector2((xMax - CardMin.x) / size.x, (yMax - CardMin.y) / size.y));
        }

        private void BuildHomeTitle(RectTransform root)
        {
            var shade = ScrollUiKit.Fill(root, "HomeShade", Vector2.zero, new Vector2(0.47f, 1f), new Color(0.05f, 0.035f, 0.02f, 0.86f));
            shade.sprite = ScrollUiKit.FadeRight();
            shade.raycastTarget = false;

            var title = ScrollUiKit.Label(root, "TitleText", "Run of the Nine", 68, TextAnchor.MiddleLeft, TitleCream,
                new Vector2(0.044f, 0.862f), new Vector2(0.42f, 0.955f), 36);
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            var subtitle = ScrollUiKit.Label(root, "SubtitleText", T("MainMenu.Subtitle").ToUpperInvariant(), 19, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.72f, 0.45f, 1f), new Vector2(0.045f, 0.826f), new Vector2(0.42f, 0.862f), 12);
            subtitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.7f);
            ScrollUiKit.Fill(root, "TitleStroke", new Vector2(0.044f, 0.812f), new Vector2(0.19f, 0.818f), ScrollUiKit.Vermilion,
                ScrollUiKit.Rounded(3f)).raycastTarget = false;
        }

        /// <summary>The saved Garden Run as a paper card; MainMenuController fills it, and hides it without a saved run.</summary>
        private Button BuildRunCard(RectTransform card, string name, UnityAction resume)
        {
            var (min, max) = OnCard(0.044f, 0.655f, 0.315f, 0.778f);
            var paper = ScrollUiKit.Fill(card, name, min, max, ScrollUiKit.Paper);
            paper.raycastTarget = true;
            paper.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);
            ScrollUiKit.Fill(paper.transform, "Edge", Vector2.zero, new Vector2(0.027f, 1f), ScrollUiKit.Vermilion).raycastTarget = false;
            var button = paper.gameObject.AddComponent<Button>();
            button.targetGraphic = paper;
            var colors = ScrollUiKit.StateColors(ScrollUiKit.Paper);
            colors.selectedColor = colors.highlightedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(resume);

            var disc = ScrollUiKit.Spot(paper.transform, "ClassDisc", new Vector2(0.15f, 0.5f), new Vector2(96f, 96f),
                new Color(0.16f, 0.10f, 0.06f, 1f), ScrollUiKit.Disc());
            disc.raycastTarget = false;
            ScrollUiKit.Icon(disc.transform, "ClassIcon", null, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Color.white);
            ScrollUiKit.Label(paper.transform, "Label", T("Resume"), 34, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                new Vector2(0.26f, 0.56f), new Vector2(0.80f, 0.92f), 18);
            ScrollUiKit.Label(paper.transform, "RunLine", string.Empty, 18, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.26f, 0.33f), new Vector2(0.97f, 0.56f), 11);
            ScrollUiKit.Label(paper.transform, "RunChips", string.Empty, 16, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.26f, 0.08f), new Vector2(0.97f, 0.32f), 10);
            var seal = ScrollUiKit.Fill(paper.transform, "Seal", new Vector2(0.82f, 0.70f), new Vector2(0.975f, 0.90f), ScrollUiKit.Vermilion);
            seal.raycastTarget = false;
            ScrollUiKit.Label(seal.transform, "Text", T("MainMenu.Saved").ToUpperInvariant(), 13, TextAnchor.MiddleCenter,
                new Color(0.98f, 0.93f, 0.88f, 1f), Vector2.zero, Vector2.one, 8);
            return button;
        }

        /// <summary>One wooden tablet of the menu column: icon and label; on focus it turns to paper and slides out.</summary>
        private Button BuildTablet(RectTransform card, string name, string label, int index, int gapsBefore, string icon, UnityAction click)
        {
            const float height = 0.090f, step = 0.1038f, gap = 0.0242f;
            var top = 1f - index * step - gapsBefore * gap;
            var tablet = ScrollUiKit.Fill(card, name, new Vector2(0f, top - height), new Vector2(0.92f, top), TabletWood);
            tablet.raycastTarget = true;
            ScrollUiKit.Fill(tablet.transform, "Edge", Vector2.zero, new Vector2(0.008f, 1f), new Color(0.54f, 0.42f, 0.23f, 1f)).raycastTarget = false;
            ScrollUiKit.Icon(tablet.transform, "Icon", icon, new Vector2(0.03f, 0.12f), new Vector2(0.12f, 0.88f), Color.white);
            var text = ScrollUiKit.Label(tablet.transform, "Label", label, 26, TextAnchor.MiddleLeft, TabletText,
                new Vector2(0.16f, 0f), new Vector2(0.98f, 1f), 15);
            var arrow = ScrollUiKit.Fill(tablet.transform, "Arrow", new Vector2(1f, 0.28f), new Vector2(1.05f, 0.72f), ScrollUiKit.Paper,
                ScrollUiKit.ArrowRight());
            arrow.raycastTarget = false;
            arrow.gameObject.SetActive(false);
            var button = tablet.gameObject.AddComponent<Button>();
            button.targetGraphic = tablet;
            button.transition = Selectable.Transition.None;   // HomeTablet paints the focus
            button.onClick.AddListener(click);
            tablet.gameObject.AddComponent<HomeTablet>().Configure(tablet, text, arrow.gameObject);
            return button;
        }

        /// <summary>[REQ: CUSTOM-SAVE-001] A small "Continue" pill at the right end of a tablet; MainMenuController shows it
        /// with the saved Custom Puzzle's board.</summary>
        private static void BuildContinuePill(Button tablet, string name, UnityAction click)
        {
            var pill = ScrollUiKit.PillButton(tablet.transform, name, new Vector2(0.56f, 0.16f), new Vector2(0.985f, 0.84f), string.Empty,
                15, ScrollUiKit.Vermilion, Color.white, 16f);
            pill.onClick.AddListener(click);
            pill.gameObject.SetActive(false);
        }

        /// <summary>The active profile top right (MainMenuController.RefreshHomeProfile fills it).</summary>
        private static void BuildProfileTag(RectTransform card)
        {
            var (min, max) = OnCard(0.80f, 0.915f, 0.976f, 0.963f);
            var tag = ScrollUiKit.Fill(card, "HomeProfile", min, max, new Color(0.08f, 0.06f, 0.04f, 0.72f), ScrollUiKit.Rounded(10f));
            tag.raycastTarget = false;
            ScrollUiKit.InkFrame(tag.transform, ScrollUiKit.Gold, 1.5f);
            ScrollUiKit.Label(tag.transform, "HomeProfileText", string.Empty, 18, TextAnchor.MiddleCenter, TabletText,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 11);
        }

        private static readonly string[] HomeChain =
            { "BtnResume", "BtnStart", "BtnModes", "BtnTutorial", "BtnMeta", "BtnItems", "BtnProfiles", "BtnOptions", "BtnCredits", "BtnQuit" };

        /// <summary>[REQ: INPUT-NAV-001] Up and down along the shown entries (the run card only with a saved run), right to the
        /// Continue pill from Custom Puzzle and to the Daily Walk from the others.</summary>
        internal static void WireHomeNavigation(Transform card)
        {
            var chain = new System.Collections.Generic.List<Button>();
            foreach (var name in HomeChain)
            {
                var button = card.Find(name)?.GetComponent<Button>();
                if (button != null && button.gameObject.activeSelf) chain.Add(button);
            }
            var daily = card.Find("HomeWidgets/DailyWidget/BtnDailyWidget")?.GetComponent<Button>();
            var pill = card.Find("BtnTutorial/BtnHomeContinueCustom")?.GetComponent<Button>();
            var pillShown = pill != null && pill.gameObject.activeSelf;
            for (var i = 0; i < chain.Count; i++)
            {
                var isTutorial = chain[i].name == "BtnTutorial";
                chain[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = chain[(i + chain.Count - 1) % chain.Count],
                    selectOnDown = chain[(i + 1) % chain.Count],
                    selectOnRight = isTutorial && pillShown ? pill : daily,
                };
                if (isTutorial && pillShown)
                    pill.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = chain[i], selectOnRight = daily };
            }
        }
    }

    /// <summary>[REQ: UI-STYLE-032] A main menu tablet: wood at rest, paper with dark ink and slid out when chosen or under
    /// the pointer.</summary>
    public sealed class HomeTablet : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float SlideOut = 26f;
        private static readonly Color Wood = new Color(0.23f, 0.14f, 0.08f, 0.92f);
        private static readonly Color Light = new Color(0.95f, 0.91f, 0.82f, 1f);

        private Image _tablet;
        private Text _label;
        private GameObject _arrow;
        private bool _selected, _hovered;

        public void Configure(Image tablet, Text label, GameObject arrow)
        {
            _tablet = tablet;
            _label = label;
            _arrow = arrow;
            Paint();
        }

        public void OnSelect(BaseEventData _) { _selected = true; Paint(); }
        public void OnDeselect(BaseEventData _) { _selected = false; Paint(); }
        public void OnPointerEnter(PointerEventData _) { _hovered = true; Paint(); }
        public void OnPointerExit(PointerEventData _) { _hovered = false; Paint(); }

        private void OnDisable()
        {
            _selected = _hovered = false;
            Paint();
        }

        private void Paint()
        {
            if (_tablet == null) return;
            var on = _selected || _hovered;
            _tablet.color = on ? ScrollUiKit.Paper : Wood;
            _label.color = on ? ScrollUiKit.Ink : Light;
            _arrow.SetActive(on);
            var rt = _tablet.rectTransform;
            rt.anchoredPosition = new Vector2(on ? SlideOut : 0f, rt.anchoredPosition.y);
        }
    }
}
