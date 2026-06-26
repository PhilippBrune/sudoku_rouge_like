using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed partial class MainMenuBlueprintBuilder
    {
        private GameObject BuildDailyWalkPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("DailyWalkPanel", root,
                new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Challenge.Daily.Title"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            var streakLabel = BuildText("StreakLabel", pr, T("Challenge.Daily.StreakPlaceholder"), 18, TextAnchor.MiddleCenter);
            SetRect(streakLabel.rectTransform,
                new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero);
            streakLabel.name = "StreakLabel";

            var goal0 = BuildText("Goal0", pr, T("Challenge.Daily.Goal1Placeholder"), 15, TextAnchor.MiddleLeft);
            SetRect(goal0.rectTransform,
                new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.69f), Vector2.zero, Vector2.zero);

            var goal1 = BuildText("Goal1", pr, T("Challenge.Daily.Goal2Placeholder"), 15, TextAnchor.MiddleLeft);
            SetRect(goal1.rectTransform,
                new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.61f), Vector2.zero, Vector2.zero);

            var goal2 = BuildText("Goal2", pr, T("Challenge.Daily.Goal3Placeholder"), 15, TextAnchor.MiddleLeft);
            SetRect(goal2.rectTransform,
                new Vector2(0.06f, 0.46f), new Vector2(0.94f, 0.53f), Vector2.zero, Vector2.zero);

            var stamps = BuildText("StampsLabel", pr, T("Challenge.Daily.InkStampsPlaceholder"), 16, TextAnchor.MiddleCenter);
            SetRect(stamps.rectTransform,
                new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.44f), Vector2.zero, Vector2.zero);
            stamps.name = "StampsLabel";

            var back = BuildButton("BtnDailyBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.ShowGameModes);
            ApplyMenuButtonIcon(back, "ui/icon_back_leaf");

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_daily_walk"))
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

        private GameObject BuildMonthlyWalkPanel(RectTransform root, MainMenuController mc)
        {
            var panel = MakePanel("MonthlyWalkPanel", root,
                new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Challenge.Monthly.Title"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            var themeLabel = BuildText("ThemeLabel", pr, T("Challenge.Monthly.LoadingTheme"), 20, TextAnchor.MiddleCenter);
            SetRect(themeLabel.rectTransform,
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
            themeLabel.name = "MonthlyThemeLabel";

            var subtitleLabel = BuildText("SubtitleLabel", pr, "", 15, TextAnchor.MiddleCenter);
            SetRect(subtitleLabel.rectTransform,
                new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.77f), Vector2.zero, Vector2.zero);
            subtitleLabel.name = "MonthlySubtitleLabel";

            var bestLabel = BuildText("BestLabel", pr, T("Challenge.Monthly.PersonalBestPlaceholder"), 16, TextAnchor.MiddleCenter);
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

            var begin = BuildButton("BtnBeginMonthly", pr, T("Begin Monthly Walk"), 18);
            SetRect(begin.GetComponent<RectTransform>(),
                new Vector2(0.16f, 0.28f), new Vector2(0.84f, 0.38f), Vector2.zero, Vector2.zero);
            begin.onClick.RemoveAllListeners();
            begin.onClick.AddListener(mc.LaunchSeasonalChallenge);
            ApplyMenuButtonIcon(begin, "ui/icon_resume_scroll");
            begin.name = "BtnBeginMonthly";

            var back = BuildButton("BtnMonthlyBack", pr, T("Back"), 18);
            SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.ShowGameModes);
            ApplyMenuButtonIcon(back, "ui/icon_back_leaf");

            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_menu_challenge"))
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
    }
}
