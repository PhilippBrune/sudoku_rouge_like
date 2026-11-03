using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed partial class MainMenuBlueprintBuilder
    {
        // [REQ: UI-STYLE-033] [REQ: SEASON-UI-001] Monthly Walk · Under the old tree (owner's pick, RM-217): the month's painting,
        // a banner with the month and the garden; a slip with the board, its rules and how the score is counted; the month's
        // best on a dark panel with its grade; the days of the month as ink marks; Back and Begin. MainMenuController
        // (RefreshMonthlyWalkPanel) fills it each time it opens; the label names are the ones it looks for.
        private GameObject BuildMonthlyWalkPanel(RectTransform root, MainMenuController mc)
        {
            var pr = EnsureRect("MonthlyWalkPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ScrollUiKit.DestroyChildrenNow(pr);   // a rebuild (language change) starts from an empty panel
            var backdrop = EnsureOrGetImage(pr.gameObject, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_monthly_walk"))
                backdrop.color = new Color(0.55f, 0.55f, 0.55f, 1f);

            ScrollUiKit.Banner(pr, "MonthlyBanner", new Vector2(0.318f, 0.844f), new Vector2(0.682f, 0.957f),
                "MonthlyEyebrow", string.Empty, "MonthlyThemeLabel", string.Empty, 46);

            // The board, its rules and the score (left).
            var slip = ScrollUiKit.PaperTag(pr, "MonthlySlip", new Vector2(0.130f, 0.287f), new Vector2(0.4635f, 0.769f), -0.6f).transform;
            ScrollUiKit.Fill(slip, "Band", new Vector2(0f, 0.977f), Vector2.one, ScrollUiKit.Moss).raycastTarget = false;
            ScrollUiKit.Label(slip, "BoardEyebrow", T("Monthly.Board").ToUpperInvariant(), 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.054f, 0.89f), new Vector2(0.95f, 0.95f), 9);
            ScrollUiKit.Label(slip, "MonthlySubtitleLabel", string.Empty, 21, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.054f, 0.80f), new Vector2(0.95f, 0.885f), 12);
            ScrollUiKit.Area(slip, "MonthlyRules", new Vector2(0.054f, 0.40f), new Vector2(0.95f, 0.79f));
            ScrollUiKit.Fill(slip, "Rule", new Vector2(0.054f, 0.395f), new Vector2(0.95f, 0.398f),
                new Color(0.24f, 0.18f, 0.12f, 0.2f)).raycastTarget = false;
            var rows = new (string Key, string Value)[]
            {
                ("Monthly.Score.Cells", Number(8100)), ("Monthly.Score.Mistake", "−" + Number(300)),
                ("Monthly.Score.Pencil", F("Monthly.Score.UpTo", "up to {0}", "+" + Number(500))), ("Monthly.Score.Perfect", "+" + Number(1000)),
            };
            for (var i = 0; i < rows.Length; i++)
            {
                var top = 0.385f - i * 0.062f;
                ScrollUiKit.ValueRow(slip, "Score_" + i, new Vector2(0.054f, top - 0.058f), new Vector2(0.95f, top), T(rows[i].Key),
                    rows[i].Value, 18, ScrollUiKit.Vermilion);
            }
            ScrollUiKit.Label(slip, "MonthlyNote", T("Monthly.Note"), 15, TextAnchor.LowerLeft, ScrollUiKit.InkSoft,
                new Vector2(0.054f, 0.03f), new Vector2(0.95f, 0.13f), 10);

            // The month's best and its grade (right, top).
            var best = ScrollUiKit.Fill(pr, "MonthlyBestPanel", new Vector2(0.5208f, 0.537f), new Vector2(0.854f, 0.769f),
                new Color(0.086f, 0.07f, 0.047f, 0.9f));
            ScrollUiKit.InkFrame(best.transform, ScrollUiKit.Gold, 2f);
            ScrollUiKit.Label(best.transform, "BestEyebrow", T("Monthly.Best").ToUpperInvariant(), 15, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.72f, 0.45f, 1f), new Vector2(0.05f, 0.80f), new Vector2(0.70f, 0.93f), 9);
            ScrollUiKit.Label(best.transform, "MonthlyBestLabel", string.Empty, 74, TextAnchor.MiddleLeft, ScrollUiKit.PaperText,
                new Vector2(0.05f, 0.40f), new Vector2(0.72f, 0.82f), 30);
            ScrollUiKit.Label(best.transform, "MonthlyBestBreakdownLabel", string.Empty, 18, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.79f, 0.66f, 1f), new Vector2(0.05f, 0.22f), new Vector2(0.75f, 0.39f), 11);
            ScrollUiKit.Label(best.transform, "MonthlyLadder", F("Monthly.Ladder", "S {0} · A {1} · B {2} · C {3}",
                    Number(8500), Number(6500), Number(4500), Number(2500)), 15, TextAnchor.MiddleLeft,
                new Color(0.66f, 0.61f, 0.50f, 1f), new Vector2(0.05f, 0.06f), new Vector2(0.75f, 0.21f), 10);
            var ring = ScrollUiKit.Spot(best.transform, "GradeRing", new Vector2(0.86f, 0.55f), new Vector2(132f, 132f),
                ScrollUiKit.Vermilion, ScrollUiKit.Enso());
            ring.raycastTarget = false;
            ScrollUiKit.Label(ring.transform, "MonthlyGrade", string.Empty, 64, TextAnchor.MiddleCenter, new Color(0.88f, 0.35f, 0.28f, 1f),
                Vector2.zero, Vector2.one);

            // The days of the month (right, bottom).
            var days = ScrollUiKit.PaperTag(pr, "MonthlyDaysPaper", new Vector2(0.5208f, 0.287f), new Vector2(0.854f, 0.509f), 0.5f).transform;
            ScrollUiKit.Label(days, "MonthName", string.Empty, 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.93f), 9);
            ScrollUiKit.Area(days, "MonthlyDays", new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.76f));
            ScrollUiKit.Label(days, "MonthlyCountdownLabel", string.Empty, 20, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.38f), 12).supportRichText = true;

            var back = ScrollUiKit.RimButton(pr, "BtnMonthlyBack", new Vector2(0.406f, 0.206f), new Vector2(0.468f, 0.259f),
                T("Back"), 22, ScrollUiKit.Gold, new Color(0.45f, 0.33f, 0.14f, 0.95f), ScrollUiKit.PaperText);
            back.onClick.AddListener(mc.BackFromPanel);   // MENU-BACK-001: the screen it was opened from
            var begin = ScrollUiKit.PillButton(pr, "BtnBeginMonthly", new Vector2(0.483f, 0.206f), new Vector2(0.594f, 0.259f),
                T("Monthly.Begin"), 22, ScrollUiKit.Vermilion, Color.white, 26f);
            begin.onClick.AddListener(mc.LaunchSeasonalChallenge);
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = begin };
            begin.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = back };
            var hint = ScrollUiKit.Label(pr, "MonthlyKeyHint", string.Empty, 18, TextAnchor.MiddleCenter, new Color(0.85f, 0.79f, 0.66f, 1f),
                new Vector2(0.30f, 0.135f), new Vector2(0.70f, 0.175f), 11);
            DeviceHint.Attach(hint, "Monthly.KeyHint");   // INPUT-HINT-001

            pr.gameObject.SetActive(false);
            return pr.gameObject;
        }

        private static string Number(int value) => value.ToString("N0", SudokuRoguelike.Core.LocalizationService.Culture);

        private static string F(string key, string fallback, params object[] args) =>
            SudokuRoguelike.Core.LocalizationService.Format(key, fallback, args);
    }
}
