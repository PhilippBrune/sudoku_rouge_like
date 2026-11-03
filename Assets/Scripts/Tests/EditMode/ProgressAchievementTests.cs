using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.Tests
{
    // The achievements beyond Harmony (Achievements.md): every one is earned from data the game records at a run's end.
    [TestFixture]
    public sealed class ProgressAchievementTests
    {
        private RecordingAchievementBridge _bridge;

        [SetUp]
        public void SetUp()
        {
            _bridge = new RecordingAchievementBridge();
            SteamAchievementService.SetPlatformBridgeForTests(_bridge);
        }

        [TearDown]
        public void TearDown() => SteamAchievementService.ResetPlatformBridgeForTests();

        private static RunResult Run(bool victory, int floor = 1) => new RunResult
        {
            Mode = GameMode.GardenRun,
            Victory = victory,
            FloorReached = floor,
            PlayedClassId = ClassId.GardenMonk,
            MistakesMade = 3,
            ItemsUsedThisRun = 2
        };

        private static List<string> Evaluate(RunResult result, ProfileStats stats = null, MetaProgressionState meta = null,
            int finalHp = 5, int maxClassLevel = 1, int dailyStreak = 0)
        {
            var mastery = new MasteryAchievementState();
            SteamAchievementService.EvaluateProgressAchievements(meta ?? new MetaProgressionState(),
                stats ?? new ProfileStats(), mastery, result, finalHp, maxClassLevel, dailyStreak);
            return mastery.UnlockedAchievements;
        }

        [Test]
        [Requirement("ACHIEVE-RUN-001")]
        public void AFirstLostRun_GivesTheFirstPuzzleAndBoss_ButNoWin()
        {
            var got = Evaluate(Run(victory: false, floor: 2), new ProfileStats { TotalPuzzlesSolved = 4, TotalBossesDefeated = 1 });

            CollectionAssert.Contains(got, "first_puzzle");
            CollectionAssert.Contains(got, "first_boss");
            CollectionAssert.DoesNotContain(got, "first_win");
            CollectionAssert.DoesNotContain(got, "floor_5");
            CollectionAssert.DoesNotContain(got, "perfect_run", "a lost run is never perfect");
            CollectionAssert.Contains(_bridge.UnlockedIds, "first_boss", "forwarded to Steam");
        }

        [Test]
        [Requirement("ACHIEVE-RUN-001")]
        public void AWinWithNoMistakeNoItemAndOneHp_GivesEachOfItsAchievements()
        {
            var run = Run(victory: true, floor: 5);
            run.MistakesMade = 0;
            run.ItemsUsedThisRun = 0;
            run.CursedPuzzlesAccepted = 3;
            run.TileXpEntries.Add(new TileXpEntry { BoardSize = 9, Stars = 5 });

            var got = Evaluate(run, finalHp: 1);

            CollectionAssert.IsSubsetOf(new[] { "first_win", "floor_5", "perfect_run", "no_items_win", "one_hp_win",
                "cursed_three", "nine_by_nine", "five_stars" }, got);
        }

        [Test]
        [Requirement("ACHIEVE-RUN-001")]
        public void AnOrdinaryWin_GivesOnlyTheWin()
        {
            var run = Run(victory: true, floor: 5);
            run.TileXpEntries.Add(new TileXpEntry { BoardSize = 8, Stars = 4 });

            var got = Evaluate(run, finalHp: 4);

            CollectionAssert.Contains(got, "first_win");
            foreach (var id in new[] { "perfect_run", "no_items_win", "one_hp_win", "cursed_three", "nine_by_nine", "five_stars" })
                CollectionAssert.DoesNotContain(got, id);
        }

        [Test]
        [Requirement("ACHIEVE-CLASS-001")]
        public void Classes_UnlocksLevelsAndAWinWithEveryClass()
        {
            var meta = new MetaProgressionState();
            var got = Evaluate(Run(victory: false), meta: meta, maxClassLevel: 9);
            CollectionAssert.DoesNotContain(got, "class_level_10");
            CollectionAssert.DoesNotContain(got, "class_second", "a new profile has only the Number Freak");

            got = Evaluate(Run(victory: false), meta: meta, maxClassLevel: 40);
            CollectionAssert.IsSubsetOf(new[] { "class_level_10", "class_level_40" }, got);

            foreach (ClassId id in System.Enum.GetValues(typeof(ClassId)))
            {
                var win = Run(victory: true, floor: 5);
                win.PlayedClassId = id;
                got = Evaluate(win, meta: meta);
            }
            CollectionAssert.Contains(got, "win_every_class");
            Assert.AreEqual(8, meta.ClassesWithAWin.Count);
        }

        [Test]
        [Requirement("ACHIEVE-CLASS-001")]
        public void WinEveryClass_NeedsAllEight()
        {
            var meta = new MetaProgressionState();
            var got = Evaluate(Run(victory: true, floor: 5), meta: meta);
            CollectionAssert.DoesNotContain(got, "win_every_class");
            CollectionAssert.AreEqual(new[] { ClassId.GardenMonk }, meta.ClassesWithAWin);
        }

        [Test]
        [Requirement("ACHIEVE-COLLECT-001")]
        public void ASevenDayStreak_GivesSevenDawns()
        {
            CollectionAssert.DoesNotContain(Evaluate(Run(false), dailyStreak: 6), "daily_streak_7");
            CollectionAssert.Contains(Evaluate(Run(false), dailyStreak: 7), "daily_streak_7");
        }

        [Test]
        [Requirement("ACHIEVE-RUN-001")]
        public void TutorialsAndOtherModes_GiveNoRunAchievements()
        {
            var tutorial = Run(victory: true, floor: 5);
            tutorial.TutorialMode = true;
            CollectionAssert.DoesNotContain(Evaluate(tutorial), "first_win");

            var trial = Run(victory: true, floor: 5);
            trial.Mode = GameMode.SpiritTrials;
            CollectionAssert.DoesNotContain(Evaluate(trial), "first_win");
        }

        [Test]
        [Requirement("ACHIEVE-MODE-001")]
        public void ModeResults_GiveTheirAchievements()
        {
            var mastery = new MasteryAchievementState();
            SteamAchievementService.EvaluateModeAchievements(mastery, GameMode.EndlessZen, completed: false, endlessDepth: 24);
            CollectionAssert.IsEmpty(mastery.UnlockedAchievements);

            SteamAchievementService.EvaluateModeAchievements(mastery, GameMode.EndlessZen, completed: false, endlessDepth: 25);
            SteamAchievementService.EvaluateModeAchievements(mastery, GameMode.SpiritTrials, completed: true, tier: SpiritTrialsTier.Master);
            SteamAchievementService.EvaluateModeAchievements(mastery, GameMode.SpiritTrials, completed: true, tier: SpiritTrialsTier.Grandmaster);
            SteamAchievementService.EvaluateModeAchievements(mastery, GameMode.SeasonalChallenge, completed: false);
            CollectionAssert.AreEquivalent(new[] { "endless_25", "trials_grandmaster" }, mastery.UnlockedAchievements);

            SteamAchievementService.EvaluateModeAchievements(mastery, GameMode.SeasonalChallenge, completed: true);
            CollectionAssert.Contains(mastery.UnlockedAchievements, "monthly_walk");
        }

        [Test]
        [Requirement("ACHIEVE-COLLECT-001")]
        public void Codex_HalfAndAll()
        {
            Assert.IsFalse(SteamAchievementService.CodexHalf(49, 100));
            Assert.IsTrue(SteamAchievementService.CodexHalf(50, 100));
            Assert.IsFalse(SteamAchievementService.CodexHalf(0, 0), "an empty Codex counts nothing");
        }

        [Test]
        [Requirement("ACHIEVE-STORY-001")]
        public void TheStory_GivesANewPage_AKeepersWholeTale_AndTheNinthKeeper()
        {
            var meta = new MetaProgressionState
            {
                StoryChapters = new List<StoryChapterEntry>
                {
                    new StoryChapterEntry { ClassId = ClassId.GardenMonk, Chapter = 2 },
                    new StoryChapterEntry { ClassId = ClassId.ReedDuelist, Chapter = 5 },
                }
            };

            var got = Evaluate(Run(victory: false), meta: meta);

            CollectionAssert.Contains(got, "story_new_page");
            CollectionAssert.Contains(got, "story_tale_reed_duelist");
            CollectionAssert.DoesNotContain(got, "story_tale_garden_monk", "chapter 2 of 5 is not the whole tale");
            CollectionAssert.DoesNotContain(got, "story_ninth_keeper");
            meta.StoryNinthOpened = true;
            CollectionAssert.Contains(Evaluate(Run(victory: false), meta: meta), "story_ninth_keeper");
            CollectionAssert.IsSubsetOf(new[] { "story_new_page", "story_tale_reed_duelist", "story_ninth_keeper" },
                SteamAchievementService.AllAchievementIds);
        }

        [Test]
        public void AFreshProfile_HasNoStoryAchievement()
        {
            var got = Evaluate(Run(victory: false));

            CollectionAssert.IsEmpty(got.FindAll(id => id.StartsWith("story_")), "chapter 1 is open from the start");
        }

        [Test]
        public void EveryAchievementId_HasAnEntryInTheSpec()
        {
            var spec = System.IO.File.ReadAllText("docs/Achievements.md");
            foreach (var id in SteamAchievementService.AllAchievementIds)
                StringAssert.Contains("`" + id + "`", spec, id);
        }

        private sealed class RecordingAchievementBridge : IPlatformAchievementBridge
        {
            public readonly List<string> UnlockedIds = new List<string>();

            public string ProviderName => "Test";
            public bool IsAvailable => true;

            public bool TryUnlockAchievement(string achievementId, out string error)
            {
                error = null;
                UnlockedIds.Add(achievementId);
                return true;
            }
        }

        // [REQ: CLASS-CHALLENGE-001] [REQ: CLASS-CHALLENGE-002] RM-116: a Garden Run with the class meets its challenges once,
        // keeps the day, and each met one grants its own achievement; the Daily Walk meets none.
        [Test]
        [Requirement("CLASS-CHALLENGE-001", "CLASS-CHALLENGE-002")]
        public void ClassChallenges_AreMetOnce_KeptWithTheDay_AndGrantTheirAchievements()
        {
            var meta = new MetaProgressionState();
            var state = new RunState { ActiveSeconds = 35 * 60f, MostBossRulesCleared = 3, CleanFloors = 1 };
            for (var i = 0; i < 7; i++) state.RouteHistory.Add((int)RouteType.RiskRoute);
            state.RouteHistory.Add((int)RouteType.CalmRoute);
            var win = new RunResult { PlayedClassId = ClassId.QuietCartographer, Mode = GameMode.GardenRun, Victory = true, HarmonyLevel = 6 };
            var met = SudokuRoguelike.Meta.ClassChallenges.Record(meta, win, state, new System.DateTime(2026, 11, 3));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, met, "six risk tiles, under 40 minutes, Harmony VI");
            Assert.AreEqual("2026-11-03", SudokuRoguelike.Meta.ClassChallenges.MetOn(meta, ClassId.QuietCartographer, 1));
            Assert.IsEmpty(SudokuRoguelike.Meta.ClassChallenges.Record(meta, win, state, new System.DateTime(2026, 11, 4)), "once");
            Assert.AreEqual("2026-11-03", SudokuRoguelike.Meta.ClassChallenges.MetOn(meta, ClassId.QuietCartographer, 1), "the first day stays");

            var slow = new RunState { ActiveSeconds = 41 * 60f };
            Assert.IsFalse(SudokuRoguelike.Meta.ClassChallenges.Meets(ClassId.GardenMonk, 1,
                new RunResult { PlayedClassId = ClassId.GardenMonk, Mode = GameMode.GardenRun, Victory = true }, slow), "41 minutes");
            Assert.IsFalse(SudokuRoguelike.Meta.ClassChallenges.Meets(ClassId.GardenMonk, 0,
                new RunResult { PlayedClassId = ClassId.GardenMonk, Mode = GameMode.GardenRun, Victory = true, DailyWalk = true }, state),
                "the Daily Walk meets no challenge");
            Assert.IsTrue(SudokuRoguelike.Meta.ClassChallenges.Meets(ClassId.LanternSeer, 0,
                new RunResult { PlayedClassId = ClassId.LanternSeer, Mode = GameMode.GardenRun }, state), "a boss with three rules, won or not");
            Assert.IsFalse(SudokuRoguelike.Meta.ClassChallenges.Meets(ClassId.LanternSeer, 0,
                new RunResult { PlayedClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun }, state), "only with the class");

            Assert.IsFalse(SudokuRoguelike.Meta.ClassChallenges.Meets(ClassId.QuietCartographer, 0, win,
                new RunState { RouteHistory = { (int)RouteType.RiskRoute, (int)RouteType.CalmRoute } }), "one risk tile is not six");

            // A Quiet Floor: a pencil mark or HP lost on any puzzle of the floor spoils it, a later heal does not undo it.
            var floor = new RunState();
            var boss = new LevelConfig { IsBoss = true };
            SudokuRoguelike.Meta.ClassChallenges.OnPuzzleSolved(floor, new LevelState { HpLost = 1 }, new LevelConfig());
            SudokuRoguelike.Meta.ClassChallenges.OnPuzzleSolved(floor, new LevelState(), boss);
            Assert.AreEqual(0, floor.CleanFloors, "HP lost on the floor");
            SudokuRoguelike.Meta.ClassChallenges.OnPuzzleSolved(floor, new LevelState(), new LevelConfig());
            SudokuRoguelike.Meta.ClassChallenges.OnPuzzleSolved(floor, new LevelState(), boss);
            Assert.AreEqual(1, floor.CleanFloors, "the next floor starts clean");

            var mastery = new MasteryAchievementState();
            SteamAchievementService.EvaluateProgressAchievements(meta, new ProfileStats(), mastery, win, 5, 1, 0);
            for (var i = 0; i < 3; i++)
                CollectionAssert.Contains(mastery.UnlockedAchievements, "challenge_quiet_cartographer_" + (i + 1));
            CollectionAssert.DoesNotContain(mastery.UnlockedAchievements, "challenge_number_freak_1");
            CollectionAssert.IsSubsetOf(SudokuRoguelike.Meta.ClassChallenges.AllAchievementIds().ToList(), SteamAchievementService.AllAchievementIds);
        }
    }
}
