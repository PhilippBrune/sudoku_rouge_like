using System;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: DAILY-GOAL-003] GAP-028: the streak counts consecutive days with all three goals done. A day without
    // play completes no goals, so opening the game after a skipped day resets the streak.
    [TestFixture]
    [Requirement("DAILY-GOAL-003")]
    public sealed class DailyStreakTests
    {
        private static readonly DateTime Day1 = new DateTime(2026, 10, 1);

        [Test]
        public void CompletedConsecutiveDays_BuildTheStreak()
        {
            var state = new DailyGoalState();
            DailyGoalService.RefreshIfNewDay(state, Day1);
            CompleteAll(state);
            DailyGoalService.RefreshIfNewDay(state, Day1.AddDays(1));
            Assert.AreEqual(1, state.CurrentStreak);

            CompleteAll(state);
            DailyGoalService.RefreshIfNewDay(state, Day1.AddDays(2));
            Assert.AreEqual(2, state.CurrentStreak);
        }

        [Test]
        public void CompleteDayOne_SkipDayTwo_OpenDayThree_ResetsTheStreak()
        {
            var state = new DailyGoalState { CurrentStreak = 4 };
            DailyGoalService.RefreshIfNewDay(state, Day1);
            CompleteAll(state);

            DailyGoalService.RefreshIfNewDay(state, Day1.AddDays(2));

            Assert.AreEqual(0, state.CurrentStreak);
            Assert.AreEqual("2026-10-03", state.LastGoalDate);
        }

        [Test]
        public void IncompleteDay_ResetsTheStreak()
        {
            var state = new DailyGoalState { CurrentStreak = 3 };
            DailyGoalService.RefreshIfNewDay(state, Day1);
            state.TodayGoalCompleted[0] = true;

            DailyGoalService.RefreshIfNewDay(state, Day1.AddDays(1));

            Assert.AreEqual(0, state.CurrentStreak);
        }

        [Test]
        public void SameDay_ChangesNothing()
        {
            var state = new DailyGoalState { CurrentStreak = 2 };
            DailyGoalService.RefreshIfNewDay(state, Day1);
            CompleteAll(state);
            var streak = state.CurrentStreak;

            DailyGoalService.RefreshIfNewDay(state, Day1);

            Assert.AreEqual(streak, state.CurrentStreak);
            Assert.IsTrue(state.TodayGoalCompleted[0] && state.TodayGoalCompleted[1] && state.TodayGoalCompleted[2]);
        }

        private static void CompleteAll(DailyGoalState state)
        {
            for (var i = 0; i < state.TodayGoalCompleted.Length; i++)
                state.TodayGoalCompleted[i] = true;
        }
    }
}
