using System;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // The first slice of the one-more-run concept (GameDesignSpec.md §14 "Between runs"): Walk again (RM-110), So close
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

        // [REQ: RUN-EARLY-001] RM-118 (owner's pick "behutsam öffnen"): a new profile's Garden Run holds back what its
        // finished runs have not opened; the Daily Walk and other modes hold nothing back.
        [Test]
        [Requirement("RUN-EARLY-001")]
        public void EarlyGarden_HoldsBackWhatTheRunsHaveNotOpened_InAGardenRunOnly()
        {
            var state = new RunState { Mode = GameMode.GardenRun, GardenOpened = 3 };
            Assert.IsFalse(EarlyGarden.IsHeld(state, EarlyGarden.Kind.Relic, nameof(RelicId.PhoenixFeather)), "opened by run 1");
            Assert.IsFalse(EarlyGarden.IsHeld(state, EarlyGarden.Kind.Rule, nameof(BossModifierId.Skyscrapers)), "opened by run 3");
            Assert.IsTrue(EarlyGarden.IsHeld(state, EarlyGarden.Kind.Relic, nameof(RelicId.ShiftingGarden)), "run 4's");
            Assert.IsTrue(EarlyGarden.IsHeld(state, EarlyGarden.Kind.Event, "tempting_shrine"));
            Assert.IsFalse(EarlyGarden.IsHeld(state, EarlyGarden.Kind.Relic, nameof(RelicId.GoldenRoot)), "never held");
            Assert.IsFalse(EarlyGarden.IsHeld(new RunState { Mode = GameMode.GardenRun, GardenOpened = 3, DailyWalk = true },
                EarlyGarden.Kind.Relic, nameof(RelicId.DragonsEye)), "the Daily Walk is the same for everyone");
            Assert.IsFalse(EarlyGarden.IsHeld(new RunState { Mode = GameMode.EndlessZen, GardenOpened = 0 },
                EarlyGarden.Kind.Rule, nameof(BossModifierId.Sandwich)));
            Assert.IsFalse(EarlyGarden.IsHeld(new RunState { Mode = GameMode.GardenRun }, EarlyGarden.Kind.Relic,
                nameof(RelicId.DragonsEye)), "an older save opens everything");
            Assert.AreEqual(10, EarlyGarden.Opened(37));
            CollectionAssert.AreEqual(new[] { EarlyGarden.Entries[3] }, EarlyGarden.NewSince(EarlyGarden.Opened(3), EarlyGarden.Opened(4)));
            Assert.IsEmpty(EarlyGarden.NewSince(EarlyGarden.Opened(30), EarlyGarden.Opened(31)), "nothing after the tenth run");
            foreach (var entry in EarlyGarden.Entries)
                Assert.IsNotEmpty(EarlyGarden.Name(entry), entry.Id);
        }

        // [REQ: RUN-EARLY-001] A first run's relic offerings, floor rules and events never show what it holds back.
        [Test]
        [Requirement("RUN-EARLY-001")]
        public void AFirstGardenRun_NeverOffersWhatItHoldsBack()
        {
            var heldRelics = new[] { RelicId.PhoenixFeather, RelicId.ShiftingGarden, RelicId.EternalLotus, RelicId.DragonsEye };
            var heldRules = new[] { BossModifierId.Skyscrapers, BossModifierId.Sandwich, BossModifierId.LittleKiller };
            var offeredRelics = new System.Collections.Generic.HashSet<RelicId>();
            for (var seed = 0; seed < 40; seed++)
            {
                var run = new RunDirector();
                run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, FiveFloors = true, GardenOpened = 0 }, 9100 + seed);
                for (var floor = 0; floor < 4; floor++)
                {
                    foreach (var relic in run.RollRelicChoices(3)) offeredRelics.Add(relic.Id);
                    run.AdvanceToNextFloor();
                    foreach (var rule in heldRules)
                        Assert.IsFalse(run.State.ActiveFloorModifiers.Contains(rule), $"seed {seed}, floor {floor + 2}: {rule}");
                }
            }
            Assert.IsFalse(offeredRelics.Overlaps(heldRelics), "a held relic was offered");
            Assert.IsTrue(offeredRelics.Contains(RelicId.GoldenRoot) || offeredRelics.Contains(RelicId.SilentGrid), "the other legendaries still come");

            var events = new RunEventService(77) { Held = key => key == "wishing_fountain" || key == "tempting_shrine" };
            for (var i = 0; i < 300; i++)
            {
                var e = events.BuildEvent(4, i);
                Assert.AreNotEqual("wishing_fountain", e.Key);
                Assert.AreNotEqual("tempting_shrine", e.Key);
            }
        }

        // [REQ: RUN-FIRST-001] RM-120: the tiles the player can pick first are baked first, the boss last.
        [Test]
        [Requirement("RUN-FIRST-001")]
        public void FloorBoards_BakeTheReachableTilesFirst_AndTheBossLast()
        {
            var graph = new System.Collections.Generic.List<RunNode>
            {
                new RunNode { Index = 0, Type = NodeType.Boss },
                new RunNode { Index = 1, Type = NodeType.Puzzle },
                new RunNode { Index = 2, Type = NodeType.Shop },
                new RunNode { Index = 3, Type = NodeType.Puzzle },
                new RunNode { Index = 4, Type = NodeType.ElitePuzzle },
            };
            CollectionAssert.AreEqual(new[] { 3, 4, 1, 2, 0 }, SudokuRoguelike.UI.RunMapController.PreBakeOrder(graph, new[] { 3, 4 }));

            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, FiveFloors = true }, 120);
            var order = SudokuRoguelike.UI.RunMapController.PreBakeOrder(run.CurrentFloorGraph, run.GetReachableNodes());
            CollectionAssert.AreEquivalent(Enumerable.Range(0, run.CurrentFloorGraph.Count), order, "every tile once");
            CollectionAssert.AreEquivalent(run.GetReachableNodes(), order.Take(run.GetReachableNodes().Count));
            Assert.AreEqual(NodeType.Boss, run.CurrentFloorGraph[order[order.Count - 1]].Type);
        }

        // [REQ: RUN-BALANCE-001] RM-108's balance check: gold left, shop visits with a purchase, items used.
        [Test]
        [Requirement("RUN-BALANCE-001")]
        public void RunHistory_KeepsTheBalanceCheck()
        {
            var state = new RunState { Mode = GameMode.GardenRun, CurrentGold = 63, ItemsUsedThisRun = 5 };
            RunLengthLog.OnShopPurchase(state);   // no shop visited: nothing counts
            RunLengthLog.OnShopVisit(state);
            RunLengthLog.OnShopPurchase(state);
            RunLengthLog.OnShopPurchase(state);   // a second purchase in the same visit
            RunLengthLog.OnShopVisit(state);      // a visit without a purchase
            RunLengthLog.OnShopVisit(state);
            RunLengthLog.OnShopPurchase(state);

            var entry = RunLengthLog.Entry(state, true, 5, new DateTime(2026, 11, 3, 20, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(63, entry.GoldLeft);
            Assert.AreEqual(3, entry.ShopVisits);
            Assert.AreEqual(2, entry.ShopVisitsWithPurchase);
            Assert.AreEqual(5, entry.ItemsUsed);
            Assert.AreEqual(-1, new RunHistoryEntry().GoldLeft, "an older entry reads as unknown");
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
            // A class without a puzzle passive (the Number Freak's +10 gold is a one-off the preview leaves out).
            run.StartRun(new LaunchRequest { ClassId = ClassId.StoneGardener, Mode = GameMode.GardenRun, FiveFloors = true, AllowIrregularPuzzles = false,
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
            Assert.AreEqual(RewardPreview.PuzzleXp(run.State, 6, 2) + XpTable.PerfectBonus, entry.TotalXp, "XP (a perfect solve adds its bonus)");
            Assert.Greater(RewardPreview.PuzzleGold(run.State, 6, 2, 1.5f), preview, "the cursed stake is higher");
            run.State.ThreeFloors = true;
            Assert.AreEqual((int)System.Math.Round(RewardPreview.PuzzleXp(null, 6, 2) * RunDirector.ThreeFloorXp, System.MidpointRounding.AwayFromZero),
                RewardPreview.PuzzleXp(run.State, 6, 2), "a three-floor run's XP (RUN-FLOORS-001)");
        }
    }
}
