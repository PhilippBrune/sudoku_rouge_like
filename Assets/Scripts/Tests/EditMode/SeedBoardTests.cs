using System;
using NUnit.Framework;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // RM-101 (owner 2026-11-04, "week and month"): the seed of the week and the Monthly Walk get boards.
    [TestFixture]
    public sealed class SeedBoardTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 11, 2);

        [Test]
        [Requirement("SEED-BOARD-001")]
        public void WeeklySeedBoard_IsTheSeedsWeek_AlsoWhenTheRunEndsInTheNextWeek()
        {
            var seed = WeeklySeedService.SeedFor(Monday);
            Assert.AreEqual("WeeklySeed_2026-W45", SteamLeaderboardService.WeeklySeedBoard(seed, Monday.AddDays(6)));
            Assert.AreEqual("WeeklySeed_2026-W45", SteamLeaderboardService.WeeklySeedBoard(seed, Monday.AddDays(7)),
                "started on Sunday night, ended on Monday");
            Assert.IsNull(SteamLeaderboardService.WeeklySeedBoard(seed, Monday.AddDays(14)), "two weeks on it is an old seed");
            Assert.IsNull(SteamLeaderboardService.WeeklySeedBoard(1, Monday), "a typed seed has no board");
        }

        [Test]
        [Requirement("SEED-BOARD-002")]
        public void MonthlyWalkBoard_IsTheMonth()
        {
            Assert.AreEqual("MonthlyWalk_2026-11", SteamLeaderboardService.MonthlyWalkBoard(new DateTime(2026, 11, 30)));
            Assert.AreEqual("MonthlyWalk_2027-01", SteamLeaderboardService.MonthlyWalkBoard(new DateTime(2027, 1, 1)));
        }
    }
}
