using System;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // The first slice of the one-more-run concept (docs/design/one-more-run-concept.md §5): Walk again (RM-110), So close
    // (RM-111), What this run moved (RM-112), the stakes in numbers (RM-115) and the run-length measures (§6).
    [TestFixture]
    public sealed class OneMoreRunTests
    {
        [TearDown]
        public void TearDown() => LocalizationService.SetLanguage(LanguageOption.English);

        // ── RM-110 Walk again ──

        [Test]
        [Requirement("RUN-AGAIN-001")]
        public void WalkAgain_KeepsClassModeHarmonyAndPerk()
        {
            var state = RunArchetypeService.CreateRunState(ClassId.GardenMonk, 11, allowIrregular: false, harmonyLevel: 4,
                harmonyPerk: HarmonyPerkId.MoonshadeOffering);
            state.Mode = GameMode.GardenRun;

            var request = RunAgain.Request(state);

            Assert.AreEqual(RunAgain.Kind.Run, RunAgain.KindOf(state));
            Assert.AreEqual(ClassId.GardenMonk, request.ClassId);
            Assert.AreEqual(GameMode.GardenRun, request.Mode);
            Assert.AreEqual(4, request.HarmonyLevel);
            Assert.AreEqual(HarmonyPerkId.MoonshadeOffering, request.HarmonyPerk);
            Assert.IsFalse(request.AllowIrregularPuzzles);
            Assert.IsFalse(request.HasSeed, "a new seed");
        }

        [Test]
        [Requirement("RUN-AGAIN-001")]
        public void WalkAgain_KeepsTheTrialsTier_MonthlyWalkStartsTheMonth_TutorialHasNone()
        {
            var trials = new RunState { ClassId = ClassId.NumberFreak, Mode = GameMode.SpiritTrials, SpiritTrialsTier = SpiritTrialsTier.Master };
            Assert.AreEqual(SpiritTrialsTier.Master, RunAgain.Request(trials).SelectedTier);
            Assert.AreEqual(0, RunAgain.Request(trials).HarmonyLevel, "Harmony belongs to the Garden Run");

            var realTrials = SpiritTrialsService.CreateTrialRunState(ClassId.NumberFreak, SpiritTrialsTier.Adept, 5, false);
            Assert.AreEqual(RunAgain.Kind.Run, RunAgain.KindOf(realTrials), "Spirit Trials turn progression off, and still walk again");
            Assert.AreEqual(SpiritTrialsTier.Adept, RunAgain.Request(realTrials).SelectedTier);
            Assert.AreEqual(RunAgain.Kind.MonthlyWalk, RunAgain.KindOf(new RunState { IsSeasonalChallenge = true }));
            Assert.IsNull(RunAgain.Request(new RunState { IsSeasonalChallenge = true }));
            Assert.AreEqual(RunAgain.Kind.None, RunAgain.KindOf(new RunState { TutorialMode = true }));
            Assert.AreEqual(RunAgain.Kind.None, RunAgain.KindOf(null));
        }

        // ── §6 Run length ──

        [Test]
        [Requirement("RUN-LENGTH-001")]
        public void RunClock_CountsTheRunsTime_AndStampsTheFirstChoiceOnce()
        {
            var state = new RunState();
            RunLengthLog.OnRunStarted(state, 100f);
            RunLengthLog.Tick(state, 0.25f);
            RunLengthLog.Tick(state, 30f);   // a hitch counts at most half a second
            Assert.AreEqual(0.75f, state.ActiveSeconds, 1e-4);

            RunLengthLog.OnPathChoice(state);
            RunLengthLog.Tick(state, 0.5f);
            RunLengthLog.OnPathChoice(state);
            Assert.AreEqual(0.75f, state.SecondsToFirstChoice, 1e-4, "the first choice only");
        }

        [Test]
        [Requirement("RUN-LENGTH-001")]
        public void RunHistory_KeepsTheLastRuns_WithTheirLengths()
        {
            var state = new RunState { ClassId = ClassId.KoiGambler, Mode = GameMode.GardenRun, HarmonyLevel = 2,
                ActiveSeconds = 2465.4f, TotalRunSeconds = 1800f, SecondsToFirstChoice = 12.2f, SecondsSincePreviousRun = 20f };
            var entry = RunLengthLog.Entry(state, true, 5, new DateTime(2026, 10, 21, 20, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(2465, entry.ActiveSeconds);
            Assert.AreEqual(1800, entry.PuzzleSeconds);
            Assert.AreEqual(12, entry.SecondsToFirstChoice);
            Assert.AreEqual(20, entry.SecondsSincePreviousRun);
            Assert.AreEqual(5, entry.FloorReached);
            Assert.IsTrue(entry.Victory);
            Assert.AreEqual(RunLengthLog.SessionId, entry.SessionId);

            var envelope = new SaveFileEnvelope();
            for (var i = 0; i < RunLengthLog.MaxEntries + 5; i++)
                RunLengthLog.Append(envelope, new RunHistoryEntry { FloorReached = i });
            Assert.AreEqual(RunLengthLog.MaxEntries, envelope.RunHistory.Count);
            Assert.AreEqual(5, envelope.RunHistory[0].FloorReached, "the oldest go first");
        }

        [Test]
        [Requirement("RUN-LENGTH-001")]
        public void RunLength_ReadsInMinutesOrHours()
        {
            Assert.AreEqual("41 min", RunLengthLog.FormatDuration(41 * 60 + 10));
            Assert.AreEqual("1 h 05 min", RunLengthLog.FormatDuration(65 * 60));
        }

        // ── RM-111 So close ──

        [Test]
        [Requirement("RUN-END-NEAR-001")]
        public void SoClose_NamesWhereTheRunEnded_AndTheBestWithTheClass()
        {
            var lines = RunEndInsights.SoClose(false, 3, true, 9, 4, "Number Freak");
            Assert.AreEqual("So close: the Floor 3 boss, 9 cells from the end.", lines[0]);
            Assert.AreEqual("Your best with the Number Freak: Floor 4.", lines[1]);

            Assert.AreEqual("A new best floor with the Number Freak.", RunEndInsights.SoClose(false, 4, false, 1, 3, "Number Freak")[1]);
            StringAssert.Contains("1 cell from", RunEndInsights.SoClose(false, 2, false, 1, 0, "Number Freak")[0]);
            Assert.IsEmpty(RunEndInsights.SoClose(true, 5, true, 0, 4, "Number Freak"), "a win needs no near miss");
        }

        // ── RM-112 What this run moved ──

        [Test]
        [Requirement("RUN-END-MOVED-001")]
        public void Moved_ListsTheUnlocksBroughtCloser_ClosestFirst_AndTheNextHarmony()
        {
            var before = new MetaProgressionState { ClassUnlocks = new ClassUnlockProgress { BossesDefeated = 1, RelicsCollected = 2 } };
            var after = new MetaProgressionState { ClassUnlocks = new ClassUnlockProgress { BossesDefeated = 1, RelicsCollected = 9 } };
            var run = new RunState { Mode = GameMode.GardenRun, HarmonyLevel = 0 };

            var lines = RunEndInsights.Moved(before, after, run, false);

            var koi = LocalizationService.T(ClassCatalog.GetDefinition(ClassId.KoiGambler).Name, "Koi Gambler");
            Assert.AreEqual($"{koi}: 2 → 9 of 10", lines[0], "the relics brought the Koi Gambler closest");
            Assert.AreEqual("Harmony I: win a run on Garden Path.", lines.Last());
            Assert.LessOrEqual(lines.Count, RunEndInsights.MaxMoved);
        }

        [Test]
        [Requirement("RUN-END-MOVED-001")]
        public void Moved_PutsANewlyOpenedHarmonyFirst()
        {
            var before = new MetaProgressionState { MaxUnlockedHarmonyLevel = 3 };
            var after = new MetaProgressionState { MaxUnlockedHarmonyLevel = 4 };
            var lines = RunEndInsights.Moved(before, after, new RunState { Mode = GameMode.GardenRun, HarmonyLevel = 3 }, true);
            Assert.AreEqual("Harmony IV is open.", lines[0]);
        }

        // ── RM-115 Stakes ──

        [Test]
        [Requirement("STAKES-001")]
        public void RewardPreview_IsWhatThePuzzlePays()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false,
                ClassLevel = 1, HarmonyLevel = 3 }, 4120);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 2, Seed = 7, MissingPercent = StarDensityService.MissingPercentForStars(2) });
            var preview = RewardPreview.PuzzleGold(run.State, 6, 2);
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0)
                    run.PlaceNumber(r, c, board.Solution[r, c]);
            var gold = run.State.CurrentGold;

            var entry = run.CompleteLevelAndGrantRewards();

            Assert.AreEqual(preview, run.State.CurrentGold - gold, "gold");
            Assert.AreEqual(RewardPreview.PuzzleXp(6, 2) + XpTable.PerfectBonus, entry.TotalXp, "XP (a perfect solve adds its bonus)");
            Assert.Greater(RewardPreview.PuzzleGold(run.State, 6, 2, 1.5f), preview, "the cursed stake is higher");
        }
    }
}
