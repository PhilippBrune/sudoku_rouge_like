using System;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Fills the Daily Walk and Monthly Walk widgets on the main menu each time the menu is shown (GAP-028).
    /// </summary>
    // [REQ: DAILY-GOAL-004] [REQ: SEASON-UI-001] SPEC-CONF-027
    public sealed class MainMenuHomeWidgetsController : MonoBehaviour
    {
        private void OnEnable() => Refresh();

        public void Refresh()
        {
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            SetText("DailyWidget/WidgetInfo", BuildDailyLine(envelope.DailyGoals ?? new DailyGoalState(), DateTime.Today));
            SetText("MonthlyWidget/WidgetInfo", BuildMonthlyLine(DateTime.Today));
        }

        /// <summary>Today's completed goals and the streak, as the Daily Walk panel would show them.</summary>
        public static string BuildDailyLine(DailyGoalState goals, DateTime today)
        {
            DailyGoalService.RefreshIfNewDay(goals, today);
            var done = 0;
            foreach (var completed in goals.TodayGoalCompleted)
                if (completed) done++;
            return LocalizationService.Format("MainMenu.Widget.Daily", "{0}/3 goals today  -  streak {1}",
                done, goals.CurrentStreak);
        }

        /// <summary>This month's theme and the countdown to the next Monthly Walk.</summary>
        public static string BuildMonthlyLine(DateTime today) =>
            SeasonalChallengeService.GetThemeName(today.Year, today.Month) + "\n" +
            SeasonalChallengeService.GetCountdownLabel(today);

        private void SetText(string path, string value)
        {
            var text = transform.Find(path)?.GetComponent<Text>();
            if (text != null) text.text = value;
        }
    }
}
