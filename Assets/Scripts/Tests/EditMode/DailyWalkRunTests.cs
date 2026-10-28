using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using static SudokuRoguelike.Run.DailyGoalService;

namespace SudokuRoguelike.Tests
{
    // DailyGoalSystem 0.3: the Daily Walk as a run of two floors (owner 2026-10-26).
    [TestFixture]
    public sealed class DailyWalkRunTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private static readonly DateTime Monday = new DateTime(2026, 10, 26);

        [SetUp]
        [TearDown]
        public void CleanSlot()
        {
            SaveFileService.FlushSharedPendingWrites();
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }

        [Test]
        [Requirement("DAILY-WALK-001")]
        public void TheDaysSeed_IsTheSameAllDay_AndChangesWithTheDay()
        {
            Assert.AreEqual(WalkSeed(Monday), WalkSeed(Monday.AddHours(23)));
            Assert.AreNotEqual(WalkSeed(Monday), WalkSeed(Monday.AddDays(1)));
            Assert.That(WalkSeed(Monday), Is.InRange(100000, 999999));
        }

        [Test]
        [Requirement("DAILY-WALK-002")]
        public void TheNeutralWalker_StartsWithTenHpTenPencilTwoSlotsAndOneRelic()
        {
            var state = RunArchetypeService.CreateRunState((ClassId)0, WalkSeed(Monday));
            Assert.AreEqual(10, state.MaxHP);
            Assert.AreEqual(10, state.MaxPencil);
            Assert.AreEqual(2, state.ItemSlots);
            Assert.AreEqual(1, RunArchetypeService.GetStartingRelicCount((ClassId)0, 1));
            Assert.IsFalse(ClassCatalog.IsClass((ClassId)0));
            Assert.AreEqual(LocalizationService.T("DailyWalk.Walker"), ClassCatalog.DisplayName((ClassId)0));
        }

        [Test]
        [Requirement("DAILY-WALK-004")]
        public void ThePool_KeepsSevenGoalsPerTier_AndItsHardGoalsFitTwoFloors()
        {
            var goals = GetAllGoals();
            Assert.AreEqual(21, goals.Length, "tools/discord/walk_calendar.py expects 21");
            foreach (GoalTier tier in Enum.GetValues(typeof(GoalTier)))
                Assert.AreEqual(7, goals.Count(g => g.Tier == tier));
            var ids = goals.Select(g => g.Id).ToList();
            CollectionAssert.IsSubsetOf(new[] { "t3_finish_hp3", "t3_finish_no_item", "t3_finish_walk", "t3_three_perfect" }, ids);
            CollectionAssert.IsNotSubsetOf(new[] { "t3_boss_floor3" }, ids);
            CollectionAssert.IsNotSubsetOf(new[] { "t3_floor4" }, ids);
            CollectionAssert.IsNotSubsetOf(new[] { "t3_new_class" }, ids);
            Assert.AreEqual("+200 XP", goals.First(g => g.Id == "t3_finish_hp3").RewardDescription);
        }

        [Test]
        [Requirement("DAILY-WALK-004", "DAILY-WALK-006")]
        public void TheFinishingGoals_CompleteOnTheirTriggers_AndAllThreeBringAStamp()
        {
            var state = new DailyGoalState();
            RefreshIfNewDay(state, DateTime.Today);
            var ids = GetAllGoals().Select(g => g.Id).ToList();
            state.TodayGoalIds = new[] { ids.IndexOf("t1_any_puzzle"), ids.IndexOf("t2_floor2"), ids.IndexOf("t3_finish_hp3") };
            EvaluateInRun(state, new RunState(), null, TriggerPuzzleComplete);
            EvaluateInRun(state, new RunState(), null, TriggerReachedFloor2);
            Assert.AreEqual(0, state.TotalInkStamps, "two of three");
            EvaluateInRun(state, new RunState(), null, TriggerFinishedHp3);
            Assert.IsTrue(state.TodayGoalCompleted.All(done => done));
            Assert.AreEqual(1, state.TotalInkStamps, "all three: +1 ink stamp");
            CollectionAssert.AreEqual(new[] { GoalTier.Easy, GoalTier.Medium, GoalTier.Hard }, GoalsMet(state));
        }

        [Test]
        [Requirement("DAILY-WALK-005")]
        public void OnlyTheDaysFirstWalk_IsScored()
        {
            var state = new DailyGoalState();
            Assert.IsTrue(TryStartScoredWalk(state, Monday));
            Assert.IsFalse(TryStartScoredWalk(state, Monday), "a second walk the same day");
            Assert.IsTrue(TryStartScoredWalk(state, Monday.AddDays(1)));
        }

        [Test]
        [Requirement("DAILY-WALK-006")]
        public void TheSeventhStampEarnedInAWeek_BringsTheWeeklyScroll_Once()
        {
            var state = new DailyGoalState();
            for (var day = 0; day < 6; day++) EarnStamps(state, 1, Monday.AddDays(day));
            Assert.AreEqual(0, state.PendingWalkXp);
            EarnStamps(state, 1, Monday.AddDays(6));
            Assert.AreEqual(WeeklyScrollXp, state.PendingWalkXp, "the seventh, on Sunday");
            EarnStamps(state, 3, Monday.AddDays(6));
            Assert.AreEqual(WeeklyScrollXp, state.PendingWalkXp, "once a week");
            EarnStamps(state, 1, Monday.AddDays(7));
            Assert.AreEqual(1, state.StampsThisWeek, "a new week counts from 0");
            Assert.AreEqual(11, state.TotalInkStamps);
        }

        [Test]
        [Requirement("DAILY-WALK-003")]
        public void TheWalksXp_WaitsForAClass_AndGoesOnlyToAnUnlockedOne()
        {
            var save = new SaveFileService(TestSlot);
            var profile = new ProfileService(save);
            profile.RecordRunAndGetNewUnlocks(new RunResult
            {
                PlayedClassId = 0, Mode = GameMode.GardenRun, DailyWalk = true, XpEarned = 400, FloorReached = 2
            });
            var garden = new ClassGardenProgressionService();
            var meta = save.Load().MetaProgress;
            Assert.AreEqual(0, garden.GetTotalXp(meta, ClassId.NumberFreak), "no class gets the walk's XP at its end");
            Assert.AreEqual(0, meta.GardenProgression?.ClassEntries?.Count(e => e.ClassId == 0) ?? 0);

            var goals = new DailyGoalState { CurrentStreak = 3 };
            RefreshIfNewDay(goals, DateTime.Today);
            Assert.AreEqual(660, profile.SettleDailyWalk(goals, 400), "400 ×1.5, streak day 4: +10 %");
            Assert.AreEqual(660, save.Load().DailyGoals.PendingWalkXp);

            Assert.IsFalse(profile.GiveWaitingXp(ClassId.QuietCartographer), "a locked class");
            Assert.IsTrue(profile.GiveWaitingXp(ClassId.NumberFreak));
            var after = save.Load();
            Assert.AreEqual(660, garden.GetTotalXp(after.MetaProgress, ClassId.NumberFreak));
            Assert.AreEqual(0, after.DailyGoals.PendingWalkXp);
            Assert.IsFalse(profile.GiveWaitingXp(ClassId.NumberFreak), "nothing waits now");
        }

        [Test]
        [Requirement("DAILY-WALK-003")]
        public void TwoFloorsWalked_AreNoGardenRunWin()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = 0, Mode = GameMode.GardenRun, DailyWalk = true }, WalkSeed(Monday));
            run.State.DailyWalk = true;
            run.State.TotalFloors = WalkFloors;
            var result = RunResultFactory.Build(run, victory: true, bossPhaseReached: 0, secondsPlayed: 60);
            Assert.IsTrue(result.DailyWalk);
            Assert.IsFalse(result.Victory);
        }

        // [REQ: DAILY-WALK-009] RM-216, way B (owner): the walk on one path over the gardens of Floors 2 and 4; the player
        // counts its floors 1 and 2; one event tile per floor for the goal that replaced "Take the Risk route".
        [Test]
        [Requirement("DAILY-WALK-009")]
        public void TheWalk_GoesOnOnePathOverFloorsTwoAndFour()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = 0, Mode = GameMode.GardenRun, DailyWalk = true }, WalkSeed(Monday));
            Assert.AreEqual(1, run.State.CurrentFloor, "the Moss Garden first");
            Assert.AreEqual(1, run.State.WalkFloor, "the walk's first floor");
            Assert.IsFalse(run.State.OnLastFloor);
            AssertOnePathWithAnEvent(run);

            run.AdvanceToNextFloor();
            Assert.AreEqual(3, run.State.CurrentFloor, "then the Stone Lantern Walk");
            Assert.AreEqual(2, run.State.WalkFloor);
            Assert.IsTrue(run.State.OnLastFloor, "its boss ends the walk");
            AssertOnePathWithAnEvent(run);
            Assert.IsTrue(SaveFileService.IsResumableRunState(run.State), "Floor 4 of a two-floor walk can be resumed");

            var garden = new RunDirector();
            garden.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, WalkSeed(Monday));
            Assert.AreEqual(0, garden.State.CurrentFloor, "a Garden Run is untouched");
            Assert.IsTrue(garden.CurrentFloorGraph.Any(n => n.Route == RouteType.RiskRoute), "and keeps both routes");
        }

        private static void AssertOnePathWithAnEvent(RunDirector run)
        {
            var graph = run.CurrentFloorGraph;
            Assert.IsFalse(graph.Any(n => n.Route == RouteType.RiskRoute), "no Risk route");
            Assert.IsFalse(graph.Any(n => n.IsCrossLink), "no cross-link");
            Assert.AreEqual(1, graph.Count(n => n.Type == NodeType.Event), "one event tile");
            Assert.IsTrue(graph.Where(n => n.Type != NodeType.Boss).All(n => n.NextNodes.Count == 1), "one way on from every tile");
        }

        // [REQ: DAILY-WALK-009] A walk saved before RM-216 resumes on its old Floors 1 and 2 with both routes.
        [Test]
        [Requirement("DAILY-WALK-009")]
        public void AWalkSavedBefore_KeepsItsFloorsAndRoutes()
        {
            var old = JsonUtility.FromJson<RunState>("{\"DailyWalk\":true,\"TotalFloors\":2,\"CurrentFloor\":1}");
            Assert.IsFalse(old.DailyWalkPath);
            Assert.AreEqual(2, old.WalkFloor);
            Assert.IsTrue(old.OnLastFloor);
            Assert.AreEqual(1, old.FloorStep);
        }

        // [REQ: DAILY-WALK-009] "Read an event on the path" replaced "Take the Risk route" in the pool's easy tier.
        [Test]
        [Requirement("DAILY-WALK-009")]
        public void ReadingAnEvent_CompletesTheEasyGoal()
        {
            var ids = GetAllGoals().Select(g => g.Id).ToList();
            CollectionAssert.DoesNotContain(ids, "t1_choose_risk");
            var state = new DailyGoalState();
            RefreshIfNewDay(state, DateTime.Today);
            state.TodayGoalIds = new[] { ids.IndexOf("t1_read_event"), ids.IndexOf("t2_floor2"), ids.IndexOf("t3_finish_hp3") };
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = 0, Mode = GameMode.GardenRun, DailyWalk = true }, WalkSeed(Monday));
            run.DailyGoals = state;
            run.BuildCurrentEvent();
            run.ResolveCurrentEventChoice(0);
            Assert.IsTrue(state.TodayGoalCompleted[0], "reading the event completes the goal");
        }

        [Test]
        [Requirement("DAILY-WALK-008")]
        public void TheBoardText_NamesThePlaceTheFriendsAndTheTopThree_AndSaysWhenSteamIsAway()
        {
            Assert.AreEqual("DailyWalk_2026-10-26", SteamLeaderboardService.DailyWalkBoard(Monday));
            StringAssert.Contains(LocalizationService.T("DailyWalk.Board.Offline"), UI.EndScreenViewController.DailyBoardText(null));
            var standing = new BoardStanding { Entries = 132, Rank = 17, FriendRank = 3, Friends = 5 };
            standing.Top.Add(("Mara", 1912));
            standing.Top.Add(("kenji_t", 1880));
            standing.Top.Add(("Rook", 1844));
            var text = UI.EndScreenViewController.DailyBoardText(standing);
            StringAssert.Contains("Mara", text);
            StringAssert.Contains("17", text);
            StringAssert.Contains("132", text);
            StringAssert.Contains("17. " + LocalizationService.T("DailyWalk.Board.You"), text);
            Assert.AreEqual("a‹b›", SteamLeaderboardService.PlainName("a<b>"), "no rich text from a player name");
        }
    }
}
