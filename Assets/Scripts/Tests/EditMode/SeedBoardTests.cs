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
        [Requirement("SEED-BOARD-003")]
        public void BoardSlip_TheFriendsSide_ListsTheFriends_WithThePlayersRow()
        {
            var standing = new BoardStanding { Entries = 128, Rank = 12, Friends = 3, FriendRank = 2 };
            standing.Top.Add(("Hanako", 9640));
            standing.FriendTop.Add(("Moritz", 9180));
            standing.FriendTop.Add((null, 8000));
            standing.FriendTop.Add(("Lea", 7240));
            var friends = SudokuRoguelike.UI.BoardSlip.FriendsText(standing);
            StringAssert.Contains("Moritz", friends);
            StringAssert.Contains("Lea", friends);
            StringAssert.Contains("<color=#9E2A2B>2. " + Core.LocalizationService.T("DailyWalk.Board.You"), friends);
            StringAssert.DoesNotContain("Hanako", friends, "the friends side shows only friends");
            var all = SudokuRoguelike.UI.BoardSlip.AllText(standing, "Board.Header.Month");
            StringAssert.Contains("Hanako", all);
            StringAssert.Contains("12. " + Core.LocalizationService.T("DailyWalk.Board.You"), all);
            StringAssert.Contains(Core.LocalizationService.T("Board.Friends.None"), SudokuRoguelike.UI.BoardSlip.FriendsText(new BoardStanding()));
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
