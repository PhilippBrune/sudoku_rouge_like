using System.Collections.Generic;
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
    }
}
