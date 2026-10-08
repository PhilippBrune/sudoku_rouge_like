using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.TestTools;

namespace SudokuRoguelike.Tests
{
    // GAP-003 / GAP-004 / GAP-005 / GAP-007: puzzle generation never blocks progress, player-chosen
    // boss modifiers stay active, and the special modes only use modifiers that generate.
    [TestFixture]
    public sealed class GenerationReliabilityTests
    {
        [SetUp]
        public void SetUp()
        {
            // Fallback steps log warnings by design; generation failures would log errors and fail the assertions below.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        // [REQ: REQ-MODE-101] [REQ: TRIAL-TIER-002] [REQ: ZENMODE-DEPTH-004]
        [Test]
        [Requirement("REQ-MODE-101", "TRIAL-TIER-002", "ZENMODE-DEPTH-004")]
        public void SpecialModes_DrawOnlyModifiersThatHaveRulesAndGenerate()
        {
            var playable = BossService.GetGeneratablePlayerModifiers(9);
            Assert.IsNotEmpty(playable);
            foreach (var mod in playable)
            {
                Assert.Less((int)mod, 94, $"{mod} is a debuff or pressure mechanic");
                Assert.IsTrue(BossService.IsSupportedForGeneratedRun(mod), $"{mod} cannot be generated");
                Assert.GreaterOrEqual(ModifierFactory.BuildRules(new List<BossModifierId> { mod }).Count, 1, $"{mod} has no constraint rule");
            }
            CollectionAssert.DoesNotContain(playable, BossModifierId.ModularRegions);

            for (var seed = 0; seed < 40; seed++)
            {
                var zen = new EndlessZenService(41000 + seed);
                foreach (var depth in new[] { 0, 12, 25 })
                    foreach (var mod in zen.BuildNextLevel(depth, true).ActiveModifiers)
                        CollectionAssert.Contains(playable, mod, $"Endless Zen depth {depth} drew {mod}");

                var trials = new SpiritTrialsService(52000 + seed);
                foreach (SpiritTrialsTier tier in Enum.GetValues(typeof(SpiritTrialsTier)))
                    foreach (var mod in trials.BuildTrialLevel(tier, true).ActiveModifiers)
                        CollectionAssert.Contains(playable, mod, $"Spirit Trials {tier} drew {mod}");
            }
        }

        // [REQ: REQ-SUD-101] [REQ: BOSS-MOD-002] Anti-Bishop (diagonal range 2, SPEC-CONF-038) generates on every
        // supported size, its solution obeys the rule, and it is never paired with a blocked partner.
        [Test]
        [Requirement("REQ-SUD-101", "BOSS-MOD-002")]
        public void AntiBishop_GeneratesOnSupportedSizesAndKeepsItsRule()
        {
            Assert.IsTrue(BossService.IsSupportedForGeneratedRun(BossModifierId.AntiBishop));
            Assert.AreEqual(6, BossService.GetMinimumBoardSize(BossModifierId.AntiBishop));
            var rule = new AntiBishopRule();
            foreach (var size in new[] { 6, 8, 9 })
            for (var seed = 0; seed < 3; seed++)
            {
                var run = StartGardenRun(9100 + size * 10 + seed);
                var config = new LevelConfig { BoardSize = size, Stars = 3, Seed = 500 + seed,
                    MissingPercent = StarDensityService.MissingPercentForStars(3) };
                config.ActiveModifiers.Add(BossModifierId.AntiBishop);
                var sw = Stopwatch.StartNew();

                run.StartLevel(config);

                Assert.NotNull(run.CurrentBoard, $"{size}x{size} Anti-Bishop puzzle (seed {seed}) did not generate");
                Assert.Less(sw.ElapsedMilliseconds, 6000, "generation budget");
                CollectionAssert.Contains(run.CurrentLevelConfig.ActiveModifiers, BossModifierId.AntiBishop);
                var solved = new SudokuBoard(size, run.CurrentBoard.Solution, run.CurrentBoard.Solution, run.CurrentBoard.RegionMap);
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    Assert.IsTrue(rule.IsValid(solved, r, c, solved.Cells[r, c], null), $"rule broken at {r},{c}");
            }

            foreach (var partner in new[] { BossModifierId.Antiknight, BossModifierId.Nonconsecutive, BossModifierId.EntropyGlobal })
                Assert.IsFalse(BossService.CanCombineModifiers(new List<BossModifierId> { BossModifierId.AntiBishop }, partner),
                    $"Anti-Bishop + {partner} cannot be generated and must stay blocked");
        }

        [Test]
        public void AntiBishopRule_CoversTwoDiagonalStepsOnly()
        {
            var cells = new int[9, 9];
            var board = new SudokuBoard(9, new int[9, 9], cells, new int[9, 9]);
            cells[4, 4] = 5;
            var rule = new AntiBishopRule();
            Assert.IsFalse(rule.IsValid(board, 3, 3, 5, null), "one diagonal step");
            Assert.IsFalse(rule.IsValid(board, 6, 2, 5, null), "two diagonal steps");
            Assert.IsTrue(rule.IsValid(board, 7, 7, 5, null), "three diagonal steps are allowed");
            Assert.IsTrue(rule.IsValid(board, 3, 3, 6, null), "other digits are allowed");
        }

        // [REQ: REQ-SUD-101] Global structural rules never run on the irregular templates.
        [Test]
        [Requirement("REQ-SUD-101")]
        public void StructuralModifiers_UseRectangularRegions()
        {
            var run = StartGardenRun(9001);
            var config = new LevelConfig { BoardSize = 9, RegionVariant = 3, Stars = 2, Seed = 77,
                MissingPercent = StarDensityService.MissingPercentForStars(2) };
            config.ActiveModifiers.Add(BossModifierId.Antiking);

            run.StartLevel(config);

            Assert.NotNull(run.CurrentBoard, "structural 9×9 puzzle must generate");
            Assert.AreEqual(1, run.CurrentLevelConfig.RegionVariant);
            CollectionAssert.Contains(run.CurrentLevelConfig.ActiveModifiers, BossModifierId.Antiking);
        }

        // [REQ: REQ-SUD-101] A failed level is retried with a different seed instead of the same failing one.
        [Test]
        [Requirement("REQ-SUD-101")]
        public void FailedLevel_IsRetriedWithSaltedSeed()
        {
            var run = StartGardenRun(9002);
            var config = new LevelConfig { BoardSize = 6, Stars = 1, Seed = 4242,
                MissingPercent = StarDensityService.MissingPercentForStars(1) };
            run.State.FailedGenerationSeeds.Add(4242);

            run.StartLevel(config.Clone());

            Assert.NotNull(run.CurrentBoard);
            Assert.AreNotEqual(4242, run.CurrentLevelConfig.Seed, "the retry must not reuse the failing seed");

            var again = StartGardenRun(9002);
            again.State.FailedGenerationSeeds.Add(4242);
            again.StartLevel(config.Clone());
            Assert.AreEqual(run.CurrentLevelConfig.Seed, again.CurrentLevelConfig.Seed, "the salted seed is deterministic");
        }

        // [REQ: REQ-BOSS-101] [REQ: REQ-SUD-101] [REQ: REQ-PERF-100] Promoted from review diagnostic D18:
        // every boss generates through the async UI path with all chosen modifiers active.
        [Test]
        [Requirement("REQ-BOSS-101", "REQ-SUD-101", "REQ-PERF-100")]
        public void BossPuzzles_AllFloors_GenerateWithChosenModifiers()
        {
            var failures = new List<string>();
            foreach (var seed in new[] { 1001, 3003, 5005 })
            {
                var run = StartGardenRun(seed);
                for (var floor = 0; floor < 5; floor++)
                {
                    if (floor > 0) run.AdvanceToNextFloor();
                    var choices = run.RollBossModifierChoices();
                    run.GetBossModifierCounts(out _, out var picks);
                    var chosen = choices.Take(picks).ToList();
                    run.ChooseBossModifiers(chosen);
                    var boss = run.CurrentFloorGraph.First(n => n.Type == NodeType.Boss);
                    run.TryAdvanceToNode(boss.Index, forced: true);
                    var config = run.BuildLevelConfig(true, false, boss.Index);

                    if (!RunAsync(run, config, 30000, out var elapsed))
                    {
                        failures.Add($"seed {seed} F{floor + 1}: no board after {elapsed} ms");
                        continue;
                    }
                    foreach (var mod in run.State.ChosenBossModifiers)
                    {
                        if (!run.CurrentLevelConfig.ActiveModifiers.Contains(mod))
                            failures.Add($"seed {seed} F{floor + 1}: chosen {mod} dropped");
                        if (run.LastGenerationMissingModifiers.Contains(mod))
                            failures.Add($"seed {seed} F{floor + 1}: chosen {mod} has no overlay");
                    }
                    if (elapsed > 15000)
                        failures.Add($"seed {seed} F{floor + 1}: took {elapsed} ms");
                }
            }
            Assert.IsEmpty(failures);
        }

        // [REQ: SEASON-PUZZLE-001] Promoted from review diagnostic D24: every month generates,
        // including the five months that failed before the fix (2026-02/03/07/10, 2027-03).
        [Test]
        [Requirement("SEASON-PUZZLE-001")]
        public void MonthlyWalk_TwoYearsOfMonthsGenerate()
        {
            var failures = new List<string>();
            for (var year = 2026; year <= 2027; year++)
            for (var month = 1; month <= 12; month++)
            {
                var run = new RunDirector();
                run.RestoreState(SeasonalChallengeService.CreateChallengeRunState(year, month));
                var sw = Stopwatch.StartNew();
                run.StartSeasonalChallenge(SeasonalChallengeService.BuildChallengeConfig(year, month));
                if (run.CurrentBoard == null)
                    failures.Add($"{year}-{month:D2}: not generated ({sw.ElapsedMilliseconds} ms)");
                else if (run.CurrentBoard.Size != 9)
                    failures.Add($"{year}-{month:D2}: board size {run.CurrentBoard.Size}");
            }
            Assert.IsEmpty(failures);
        }

        // [REQ: SEASON-PUZZLE-001] The same month always produces the same puzzle.
        [Test]
        [Requirement("SEASON-PUZZLE-001")]
        public void MonthlyWalk_IsDeterministicPerMonth()
        {
            var a = new RunDirector();
            a.RestoreState(SeasonalChallengeService.CreateChallengeRunState(2026, 10));
            a.StartSeasonalChallenge(SeasonalChallengeService.BuildChallengeConfig(2026, 10));
            var b = new RunDirector();
            b.RestoreState(SeasonalChallengeService.CreateChallengeRunState(2026, 10));
            b.StartSeasonalChallenge(SeasonalChallengeService.BuildChallengeConfig(2026, 10));

            Assert.NotNull(a.CurrentBoard);
            CollectionAssert.AreEqual(a.CurrentBoard.Solution, b.CurrentBoard.Solution);
            CollectionAssert.AreEqual(a.CurrentBoard.Cells, b.CurrentBoard.Cells);
        }

        // [REQ: REQ-MODE-101] [REQ: ZENMODE-SESSION-001] [REQ: TRIAL-SESSION-001]
        // Promoted from review diagnostics D22/D23: the special modes start with a board.
        [Test]
        [Requirement("REQ-MODE-101", "ZENMODE-SESSION-001", "TRIAL-SESSION-001")]
        public void EndlessZenAndSpiritTrials_StartWithABoard()
        {
            var failures = new List<string>();
            for (var seed = 0; seed < 6; seed++)
            {
                var zen = new RunDirector();
                zen.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.EndlessZen, AllowIrregularPuzzles = true }, 41000 + seed);
                if (zen.CurrentBoard == null) failures.Add($"Endless Zen seed {41000 + seed}: no board at depth 0");
                for (var depth = 1; depth <= 3 && zen.CurrentBoard != null; depth++)
                {
                    zen.AdvanceEndlessZenLevel();
                    if (zen.CurrentBoard == null) failures.Add($"Endless Zen seed {41000 + seed}: no board at depth {depth}");
                }

                foreach (SpiritTrialsTier tier in Enum.GetValues(typeof(SpiritTrialsTier)))
                {
                    var trial = new RunDirector();
                    trial.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.SpiritTrials, SelectedTier = tier, AllowIrregularPuzzles = true }, 52000 + seed);
                    if (trial.CurrentBoard == null) failures.Add($"Spirit Trials {tier} seed {52000 + seed}: no board");
                }
            }
            Assert.IsEmpty(failures);
        }

        private static RunDirector StartGardenRun(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = true,
                ClassLevel = 1
            }, seed);
            return run;
        }

        private static bool RunAsync(RunDirector run, LevelConfig config, int timeoutMs, out long elapsed)
        {
            var sw = Stopwatch.StartNew();
            run.StartLevelAsync(config);
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (run.TryCompleteAsyncLevel())
                {
                    elapsed = sw.ElapsedMilliseconds;
                    return run.CurrentBoard != null;
                }
                Thread.Sleep(20);
            }
            elapsed = sw.ElapsedMilliseconds;
            return false;
        }
    }
}
