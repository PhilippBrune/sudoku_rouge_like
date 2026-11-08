using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed partial class MainMenuBlueprintBuilder
    {
        // [REQ: DAILY-GOAL-004] [REQ: SEASON-UI-001] [REQ: UI-STYLE-032] SPEC-CONF-027 / GAP-028: the Daily and Monthly Walk as
        // paper tags at the bottom right of the main menu, children of the menu card so they show and hide with it.
        private void BuildHomeWidgets(RectTransform card, MainMenuController mc, Button firstTablet)
        {
            var (dMin, dMax) = OnCard(0.594f, 0.135f, 0.765f, 0.285f);
            var (mMin, mMax) = OnCard(0.792f, 0.135f, 0.963f, 0.285f);
            var column = ScrollUiKit.Area(card, "HomeWidgets", Vector2.zero, Vector2.one);
            var daily = BuildWalkTag(column, "DailyWidget", dMin, dMax, -1f, T("Daily Walk"), "BtnDailyWidget", mc.OpenDailyWalk);
            var monthly = BuildWalkTag(column, "MonthlyWidget", mMin, mMax, 1.2f, T("Monthly Walk"), "BtnMonthlyWidget", mc.ShowMonthlyWalkPanel);
            var (rMin, rMax) = OnCard(0.325f, 0.135f, 0.585f, 0.445f);
            RecentRunsSlip.Build(column, mc, rMin, rMax);   // [REQ: RUN-HISTORY-002]
            EnsureComponent<MainMenuHomeWidgetsController>(column.gameObject);
            daily.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = firstTablet, selectOnRight = monthly };
            monthly.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = daily };
        }

        private Button BuildWalkTag(RectTransform parent, string name, Vector2 min, Vector2 max, float angle, string title,
            string buttonName, UnityAction open)
        {
            var tag = ScrollUiKit.PaperTag(parent, name, min, max, angle, 30f);
            ScrollUiKit.Label(tag.transform, "WidgetTitle", title.ToUpperInvariant(), 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.07f, 0.74f), new Vector2(0.95f, 0.92f), 9);
            // Filled by MainMenuHomeWidgetsController whenever the main menu is shown.
            ScrollUiKit.Label(tag.transform, "WidgetInfo", string.Empty, 19, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.07f, 0.36f), new Vector2(0.95f, 0.72f), 11);
            var button = ScrollUiKit.PillButton(tag.transform, buttonName, new Vector2(0.07f, 0.08f), new Vector2(0.42f, 0.30f),
                T("MainMenu.Widget.Open"), 17, ScrollUiKit.Ink, TabletText, 16f);
            button.onClick.AddListener(open);
            return button;
        }
    }
}
