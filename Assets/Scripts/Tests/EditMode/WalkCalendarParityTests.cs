using System;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // The Discord posts (tools/discord/walk_calendar.py) compute the Daily Walk goals, the Seed of the week and the
    // Monthly Walk board without the game. These values are pinned on both sides (tools/discord/test_walk_calendar.py
    // asserts the same numbers), so the posts always match what players see in the game.
    [TestFixture]
    [Requirement("RUN-WEEK-001", "RUN-WEEK-002")]
    public sealed class WalkCalendarParityTests
    {
        [Test]
        public void SeededRandom_IsTheSequenceThePythonPortReproduces()
        {
            var rng = new Random(20261015);
            CollectionAssert.AreEqual(new[] { 853, 829, 927, 976, 551 }, Enumerable.Range(0, 5).Select(_ => rng.Next(1000)));
        }

        [TestCase(2026, 10, 15, 5, 12, 20)]
        [TestCase(2026, 1, 1, 2, 11, 14)]
        [TestCase(2027, 1, 1, 4, 12, 20)]
        public void DailyWalkGoals_ForADate_AreFixed(int y, int m, int d, int g1, int g2, int g3)
        {
            var state = new DailyGoalState();
            DailyGoalService.RefreshIfNewDay(state, new DateTime(y, m, d));
            CollectionAssert.AreEqual(new[] { g1, g2, g3 }, state.TodayGoalIds);
        }

        [TestCase(2026, 10, 15, 2026, 42, 390609)]
        [TestCase(2026, 1, 1, 2026, 1, 160412)]
        [TestCase(2027, 1, 1, 2026, 53, 444943)]
        public void SeedOfTheWeek_FollowsTheIsoWeek(int y, int m, int d, int isoYear, int isoWeek, int seed)
        {
            var date = new DateTime(y, m, d);
            Assert.AreEqual((isoYear, isoWeek), WeeklySeedService.IsoWeek(date));
            Assert.AreEqual(seed, WeeklySeedService.SeedFor(date));
            Assert.AreEqual(DayOfWeek.Monday, WeeklySeedService.WeekStart(date).DayOfWeek);
        }

        [Test]
        public void SeedOfTheWeek_IsTheSameAllWeek_AndChangesOnMonday()
        {
            var monday = new DateTime(2026, 10, 12);
            var seed = WeeklySeedService.SeedFor(monday);
            for (var i = 1; i < 7; i++)
                Assert.AreEqual(seed, WeeklySeedService.SeedFor(monday.AddDays(i)));
            Assert.AreNotEqual(seed, WeeklySeedService.SeedFor(monday.AddDays(7)));
            Assert.That(seed, Is.InRange(100000, 999999), "six digits, easy to type");
        }

        [TestCase(2026, 10, 2, BossModifierId.Antiknight)]
        [TestCase(2026, 12, 0, BossModifierId.Palindrome)]
        public void MonthlyWalkBoard_ForAMonth_IsFixed(int y, int m, int regionVariant, BossModifierId modifier)
        {
            var config = SeasonalChallengeService.BuildChallengeConfig(y, m);
            Assert.AreEqual(9, config.BoardSize);
            Assert.AreEqual(regionVariant, config.RegionVariant);
            CollectionAssert.AreEqual(new[] { modifier }, config.ActiveModifiers);
        }
    }
}
