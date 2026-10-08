// TEST INFRASTRUCTURE — temporary diagnostic created for the 2026-10-01 full game review
// (docs/review/*), batch 3. Records observed behaviour to TestResults/review-2026-10-01/probe3.txt.
// Assertions encode the CURRENT SPEC; failures are review findings. Delete or promote after review.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.TestTools;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    [Explicit("Review diagnostics (2026-10-01): assertions encode the spec and fail by design; run on demand only.")]
    [Category("ReviewDiagnostics")]
    public sealed class ReviewDiagnostics3Tests
    {
        private static readonly string ProbePath =
            Path.Combine("TestResults", "review-2026-10-01", "probe3.txt");

        private readonly List<string> _errors = new List<string>();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += OnLog;
            Probe("");
            Probe("=== " + TestContext.CurrentContext.Test.Name + " ===");
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            if (_errors.Count > 0)
            {
                Probe($"  [unity error logs during test: {_errors.Count}]");
                foreach (var e in _errors.Distinct().Take(6)) Probe("    " + e);
            }
            _errors.Clear();
            LogAssert.ignoreFailingMessages = false;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add(type + ": " + message.Split('\n')[0]);
        }

        private static void Probe(string line)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProbePath));
            File.AppendAllText(ProbePath, line + "\n");
            TestContext.WriteLine(line);
        }

        private static bool HasRule(BossModifierId id) =>
            ModifierFactory.BuildRules(new List<BossModifierId> { id }).Count > 0;

        private static string Classify(BossModifierId id)
        {
            var v = (int)id;
            if (v >= 103 && v <= 106) return "pressure";
            if (v >= 94) return "debuff";
            return HasRule(id) ? "rule" : "INERT";
        }

        // ───────── D22 Endless Zen ─────────

        [Test]
        public void D22_EndlessZen_LevelsGenerateWithFunctionalModifiers()
        {
            var failures = new List<string>();
            var classes = new Dictionary<string, int>();
            foreach (var seed in Enumerable.Range(0, 8).Select(i => 41000 + i))
            {
                var run = new RunDirector();
                var sw = Stopwatch.StartNew();
                run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.EndlessZen, AllowIrregularPuzzles = true }, seed);
                sw.Stop();
                var zen = new EndlessZenService(seed);
                var line = $"  seed {seed}: depth0 board={(run.CurrentBoard == null ? "NULL" : run.CurrentBoard.Size + "x" + run.CurrentBoard.Size)} {sw.ElapsedMilliseconds}ms HP={run.State.CurrentHP} slots={run.State.ItemSlots} mods=[{string.Join(",", run.CurrentLevelConfig?.ActiveModifiers ?? new List<BossModifierId>())}]";
                if (run.CurrentBoard == null) failures.Add($"seed {seed}: depth 0 board not generated");
                foreach (var depth in new[] { 5, 12, 25 })
                {
                    var cfg = zen.BuildNextLevel(depth, true);
                    foreach (var m in cfg.ActiveModifiers)
                    {
                        var k = Classify(m);
                        classes[k] = classes.TryGetValue(k, out var c) ? c + 1 : 1;
                    }
                    var sw2 = Stopwatch.StartNew();
                    run.StartLevel(cfg);
                    sw2.Stop();
                    line += $" | d{depth}: {cfg.Stars}★ v{cfg.RegionVariant} [{string.Join(",", cfg.ActiveModifiers.Select(m => m + ":" + Classify(m)))}] board={(run.CurrentBoard == null ? "NULL" : "ok")} {sw2.ElapsedMilliseconds}ms";
                    if (run.CurrentBoard == null) failures.Add($"seed {seed} depth {depth}: board not generated");
                }
                Probe(line);
            }
            Probe("  modifier draw classes: " + string.Join(", ", classes.Select(kv => kv.Key + "=" + kv.Value)));
            if (classes.TryGetValue("INERT", out var inert) && inert > 0)
                failures.Add($"{inert} drawn modifiers have no constraint rule or geometry (extended library IDs)");
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────── D23 Spirit Trials ─────────

        [Test]
        public void D23_SpiritTrials_EachTierGeneratesWithFunctionalModifiers()
        {
            var failures = new List<string>();
            foreach (SpiritTrialsTier tier in Enum.GetValues(typeof(SpiritTrialsTier)))
            {
                foreach (var seed in Enumerable.Range(0, 5).Select(i => 52000 + i))
                {
                    var run = new RunDirector();
                    var sw = Stopwatch.StartNew();
                    run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.SpiritTrials, SelectedTier = tier, AllowIrregularPuzzles = true }, seed);
                    sw.Stop();
                    var cfg = run.CurrentLevelConfig;
                    var mods = cfg?.ActiveModifiers ?? new List<BossModifierId>();
                    Probe($"  {tier} seed {seed}: board={(run.CurrentBoard == null ? "NULL" : run.CurrentBoard.Size + "x" + run.CurrentBoard.Size)} {sw.ElapsedMilliseconds}ms stars={cfg?.Stars} v{cfg?.RegionVariant} HP={run.State.CurrentHP} pencil={run.State.CurrentPencil} unique={cfg?.RequireUniqueModifierSolution}/{run.LastGenerationMetrics?.UniqueSolutionVerified} mods=[{string.Join(",", mods.Select(m => m + ":" + Classify(m)))}]");
                    if (run.CurrentBoard == null) failures.Add($"{tier} seed {seed}: no board ({sw.ElapsedMilliseconds}ms)");
                    if (mods.Any(m => Classify(m) != "rule")) failures.Add($"{tier} seed {seed}: non-functional modifier drawn {string.Join(",", mods.Where(m => Classify(m) != "rule"))}");
                }
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────── D24 Monthly Walk (Seasonal) ─────────

        [Test]
        public void D24_MonthlyWalk_TwoYearsOfChallengesGenerate()
        {
            var failures = new List<string>();
            for (var year = 2026; year <= 2027; year++)
            for (var month = 1; month <= 12; month++)
            {
                var cfg = SeasonalChallengeService.BuildChallengeConfig(year, month);
                var run = new RunDirector();
                run.RestoreState(SeasonalChallengeService.CreateChallengeRunState(year, month));
                var sw = Stopwatch.StartNew();
                run.StartSeasonalChallenge(cfg);
                sw.Stop();
                Probe($"  {year}-{month:D2} '{SeasonalChallengeService.GetThemeName(year, month)}': v{cfg.RegionVariant} mods=[{string.Join(",", cfg.ActiveModifiers.Select(m => m + ":" + Classify(m)))}] board={(run.CurrentBoard == null ? "NULL" : "ok")} {sw.ElapsedMilliseconds}ms uniqueVerified={run.LastGenerationMetrics?.UniqueSolutionVerified} HP={run.State.CurrentHP} pencil={run.State.CurrentPencil}");
                if (run.CurrentBoard == null) failures.Add($"{year}-{month:D2}: monthly puzzle not generated ({sw.ElapsedMilliseconds}ms)");
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────── D25 economy & node flows ─────────

        [Test]
        public void D25_ShopRestRelicCursedFlows_ChangeStateAsSpecified()
        {
            var failures = new List<string>();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 7331);
            var s = run.State;

            // Shop
            s.CurrentGold = 500;
            var offers = run.BuildShopOffers();
            var price = offers[0].Price;
            var itemsBefore = s.HeldItems.Count;
            var bought = run.TryPurchaseShopOffer(0);
            Probe($"  shop: offers={offers.Count} [{string.Join(",", offers.Select(o => $"{o.Item.Type}/{o.Item.Rarity}/{o.Price}"))}] buy#0 ok={bought} gold 500->{s.CurrentGold} (price {price}) items {itemsBefore}->{s.HeldItems.Count}");
            if (!bought || s.CurrentGold != 500 - price || s.HeldItems.Count != itemsBefore + 1) failures.Add("shop purchase did not deduct gold / add item");
            while (s.HeldItems.Count < s.ItemSlots) s.HeldItems.Add(new ItemInstance { Type = ItemType.InkWell, Rarity = ItemRarity.Normal, Charges = 1 });
            var goldFull = s.CurrentGold;
            var boughtFull = run.TryPurchaseShopOffer(1);
            Probe($"  shop with full bag: buy ok={boughtFull} gold {goldFull}->{s.CurrentGold} items={s.HeldItems.Count}/{s.ItemSlots}");
            if (boughtFull && s.HeldItems.Count > s.ItemSlots) failures.Add("shop allowed buying beyond bag capacity");
            if (boughtFull && s.CurrentGold < goldFull && s.HeldItems.Count == s.ItemSlots) failures.Add("shop charged gold for an item that could not be stored (bag full)");
            s.RerollTokens = 0;
            var gBefore = s.CurrentGold;
            var rerolled = run.RerollShop();
            Probe($"  shop reroll: ok={rerolled} gold {gBefore}->{s.CurrentGold} next cost={run.GetShopRerollCost()}");

            // Rest
            s.CurrentHP = 2;
            run.AcceptRestHeal();
            var hpAfter = s.CurrentHP;
            s.CurrentPencil = 0;
            run.AcceptRestPencilBoost();
            Probe($"  rest: heal 2->{hpAfter} (MaxHP {s.MaxHP}, GDS +2); pencil 0->{s.CurrentPencil} (GDS +3)");
            if (hpAfter != Math.Min(s.MaxHP, 4)) failures.Add($"rest heal gives {hpAfter - 2} HP (GDS: +2)");
            if (s.CurrentPencil != 3) failures.Add($"rest pencil gives {s.CurrentPencil} (GDS: +3)");

            // Relic choice (GDS v1.5: 3-of-3 relic choice panel)
            var relicsBefore = s.HeldRelics.Count;
            var choices = run.RollRelicChoices(3);
            run.AcceptRelicChoice(0);
            Probe($"  relic choice: offered={choices.Count} [{string.Join(",", choices.Select(r => $"{r.Id}/{r.Tier}"))}] held {relicsBefore}->{s.HeldRelics.Count}");
            if (choices.Count != 3 || s.HeldRelics.Count != relicsBefore + 1) failures.Add("relic choice flow did not offer 3 / add 1 relic");

            // Cursed node
            var normal = run.BuildLevelConfig(false, false, 2);
            var cursed = run.BuildCursedLevelConfig(2);
            run.OnCursedNodeAccepted();
            Probe($"  cursed: normal mods=[{string.Join(",", normal.ActiveModifiers)}] size={normal.BoardSize} | cursed mods=[{string.Join(",", cursed.ActiveModifiers)}] size={cursed.BoardSize} goldMult={cursed.CursedGoldMult} xpMult={cursed.CursedXpMult} floorRange={SudokuRoguelike.Data.FloorThemeData.GetMinBoardSize(0)}-{SudokuRoguelike.Data.FloorThemeData.GetMaxBoardSize(0)}");
            if (cursed.ActiveModifiers.Count != normal.ActiveModifiers.Count + 1) failures.Add("cursed node did not add exactly one modifier");
            if (Math.Abs(cursed.CursedGoldMult - 1.5f) > 0.01f || Math.Abs(cursed.CursedXpMult - 1.5f) > 0.01f) failures.Add("cursed multipliers not x1.5");

            // Emergency heal
            s.CurrentGold = 200;
            s.CurrentHP = 1;
            var healed = run.TryBuyEmergencyHeal();
            Probe($"  emergency heal: ok={healed} HP 1->{s.CurrentHP} gold 200->{s.CurrentGold}");

            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }
    }
}
