using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed partial class MainMenuBlueprintBuilder
    {
        private static readonly Color HomeWidgetBg = new Color(0.10f, 0.12f, 0.09f, 0.90f);

        // [REQ: DAILY-GOAL-004] [REQ: SEASON-UI-001] SPEC-CONF-027 / GAP-028: Daily Walk and Monthly Walk widgets on the
        // main menu, right of the menu card. They are children of the card, so they show and hide with the main menu.
        private void BuildHomeWidgets(RectTransform card, MainMenuController mc, Button[] menuButtons)
        {
            var column = EnsureRect("HomeWidgets", card,
                new Vector2(1.05f, 0.47f), new Vector2(1.55f, 0.89f), Vector2.zero, Vector2.zero);

            var daily = BuildHomeWidget(column, "DailyWidget", new Vector2(0f, 0.52f), new Vector2(1f, 1f),
                T("Daily Walk"), "BtnDailyWidget", mc.ShowDailyWalkPanel);
            var monthly = BuildHomeWidget(column, "MonthlyWidget", new Vector2(0f, 0f), new Vector2(1f, 0.48f),
                T("Monthly Walk"), "BtnMonthlyWidget", mc.ShowMonthlyWalkPanel);
            EnsureComponent<MainMenuHomeWidgetsController>(column.gameObject);

            // Gamepad: right from the menu column reaches the widgets, left returns to Start Game.
            foreach (var button in menuButtons)
            {
                var nav = button.navigation;
                nav.selectOnRight = daily;
                button.navigation = nav;
            }
            daily.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit, selectOnLeft = menuButtons[0], selectOnDown = monthly, selectOnUp = monthly
            };
            monthly.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit, selectOnLeft = menuButtons[0], selectOnUp = daily, selectOnDown = daily
            };
        }

        private Button BuildHomeWidget(RectTransform parent, string name, Vector2 min, Vector2 max,
            string title, string buttonName, UnityAction open)
        {
            var widget = EnsureRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            EnsureOrGetImage(widget.gameObject, HomeWidgetBg);
            var outline = EnsureComponent<Outline>(widget.gameObject);
            outline.effectColor = GamePalette.AccentGoldGlow;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var titleText = BuildText("WidgetTitle", widget, title, 18, TextAnchor.MiddleCenter);
            SetRect(titleText.rectTransform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero);
            titleText.color = GamePalette.AccentGold;

            // Filled by MainMenuHomeWidgetsController whenever the main menu is shown.
            var info = BuildText("WidgetInfo", widget, "", 14, TextAnchor.MiddleCenter);
            SetRect(info.rectTransform, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.74f), Vector2.zero, Vector2.zero);

            var button = BuildButton(buttonName, widget, T("MainMenu.Widget.Open"), 15);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.32f), Vector2.zero, Vector2.zero);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(open);
            return button;
        }
    }
}
