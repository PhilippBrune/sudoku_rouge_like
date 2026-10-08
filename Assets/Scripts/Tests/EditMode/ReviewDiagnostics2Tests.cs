// TEST INFRASTRUCTURE — temporary diagnostic created for the 2026-10-01 full game review
// (docs/review/*), batch 2. Records observed behaviour to TestResults/review-2026-10-01/probe2.txt.
// Assertions encode the CURRENT SPEC; failures are review findings. Delete or promote after review.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.TestTools;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    [Explicit("Review diagnostics (2026-10-01): assertions encode the spec and fail by design; run on demand only.")]
    [Category("ReviewDiagnostics")]
    public sealed class ReviewDiagnostics2Tests
    {
        private static readonly string ProbePath =
            Path.Combine("TestResults", "review-2026-10-01", "probe2.txt");

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

        private static RunDirector StartRun(int seed, int harmony = 0)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = true,
                ClassLevel = 1,
                HarmonyLevel = harmony
            }, seed);
            return run;
        }

        private static bool IsPuzzle(NodeType t) =>
            t == NodeType.Puzzle || t == NodeType.ElitePuzzle || t == NodeType.PreBoss || t == NodeType.Boss;

        private static RunNode FirstReachablePuzzle(RunDirector run)
        {
            foreach (var idx in run.GetReachableNodes())
                if (IsPuzzle(run.CurrentFloorGraph[idx].Type)) return run.CurrentFloorGraph[idx];
            return null;
        }

        /// <summary>Starts the level through the async path the UI uses and polls until it settles.</summary>
        private static bool RunAsync(RunDirector run, LevelConfig cfg, int timeoutMs, out long elapsed)
        {
            var sw = Stopwatch.StartNew();
            run.StartLevelAsync(cfg);
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (run.TryCompleteAsyncLevel()) { elapsed = sw.ElapsedMilliseconds; return run.CurrentBoard != null; }
                Thread.Sleep(20);
            }
            elapsed = sw.ElapsedMilliseconds;
            return false;
        }

        // ───────── D17 resume mid-puzzle then keep playing (GameBootstrap.LaunchResume sequence) ─────────

        [Test]
        public void D17_ResumeMidPuzzle_CanContinuePlacingAndCompleteLevel()
        {
            const int slot = 2;
            var path = Path.Combine(SaveFileService.SaveRoot, $"save_profile_{slot}.json");
            var backup = new Dictionary<string, string>();
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                backup[p] = File.Exists(p) ? File.ReadAllText(p) : null;
            var failures = new List<string>();
            try
            {
                foreach (var p in backup.Keys) if (File.Exists(p)) File.Delete(p);
                var save = new SaveFileService(slot);
                foreach (var floorsToAdvance in new[] { 0, 1 })
                {
                    var run = StartRun(6161 + floorsToAdvance);
                    for (var i = 0; i < floorsToAdvance; i++) run.AdvanceToNextFloor();
                    var node = FirstReachablePuzzle(run);
                    run.TryAdvanceToNode(node.Index);
                    run.StartLevel(run.BuildLevelConfig(node.Type == NodeType.Boss, node.Type == NodeType.ElitePuzzle, node.Index, node.Type == NodeType.PreBoss));
                    var b = run.CurrentBoard;
                    var placed = 0;
                    for (var r = 0; r < b.Size && placed < 2; r++)
                    for (var c = 0; c < b.Size && placed < 2; c++)
                        if (b.Cells[r, c] == 0 && run.PlaceNumber(r, c, b.Solution[r, c]) == PlaceResult.Correct) placed++;
                    new RunAutoSaveCoordinator(save).Save(run.State, run.ExportPuzzleSaveState());
                    save.FlushPendingWrites();

                    new RunResumeService(save).TryResumeFromSave(out var rs, out var ps);
                    var resumed = new RunDirector();
                    resumed.StartRun(new LaunchRequest { ClassId = rs.ClassId, Mode = rs.Mode, AllowIrregularPuzzles = rs.AllowIrregularPuzzles, HarmonyLevel = rs.HarmonyLevel }, rs.Seed);
                    resumed.RestoreState(rs);
                    var idx = rs.CurrentNodeIndex;
                    resumed.RebuildFloorGraph();
                    resumed.State.CurrentNodeIndex = idx;
                    resumed.TryRestorePuzzleSaveState(ps);

                    Probe($"  floor {floorsToAdvance + 1}: after resume config={(resumed.CurrentLevelConfig == null ? "NULL" : "ok")} engine={(resumed.ConstraintEngine == null ? "NULL" : "ok")} overlay={(resumed.CurrentOverlay == null ? "NULL" : "ok")} floorMods=[{string.Join(",", resumed.State.ActiveFloorModifiers)}]");
                    string placeOutcome;
                    try
                    {
                        var rb = resumed.CurrentBoard;
                        var done = false;
                        placeOutcome = "no empty cell";
                        for (var r = 0; r < rb.Size && !done; r++)
                        for (var c = 0; c < rb.Size && !done; c++)
                            if (rb.Cells[r, c] == 0)
                            {
                                placeOutcome = resumed.PlaceNumber(r, c, rb.Solution[r, c]).ToString();
                                done = true;
                            }
                    }
                    catch (Exception ex) { placeOutcome = "EXCEPTION " + ex.GetType().Name + ": " + ex.Message; }
                    Probe($"    first placement after resume: {placeOutcome}");
                    if (placeOutcome.StartsWith("EXCEPTION")) failures.Add($"floor {floorsToAdvance + 1}: placing a digit after resume throws ({placeOutcome})");

                    string completeOutcome;
                    try
                    {
                        var rb = resumed.CurrentBoard;
                        for (var r = 0; r < rb.Size; r++)
                        for (var c = 0; c < rb.Size; c++)
                            if (rb.Cells[r, c] == 0) rb.PlaceValue(r, c, rb.Solution[r, c]);
                        var entry = resumed.CompleteLevelAndGrantRewards();
                        completeOutcome = $"ok xp={entry?.TotalXp} gold={resumed.State.CurrentGold}";
                    }
                    catch (Exception ex) { completeOutcome = "EXCEPTION " + ex.GetType().Name + ": " + ex.Message; }
                    Probe($"    complete level after resume: {completeOutcome}");
                    if (completeOutcome.StartsWith("EXCEPTION")) failures.Add($"floor {floorsToAdvance + 1}: completing level after resume throws ({completeOutcome})");
                }
            }
            finally
            {
                SaveFileService.FlushSharedPendingWrites();
                foreach (var kv in backup)
                {
                    if (kv.Value == null) { if (File.Exists(kv.Key)) File.Delete(kv.Key); }
                    else File.WriteAllText(kv.Key, kv.Value);
                }
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────── D18 boss puzzles through the async UI path (TryAdvanceToBossNodeAsync emulation) ─────────

        [Test]
        public void D18_BossAsyncPath_AllFloors_GeneratesWithChosenModifiers()
        {
            var failures = new List<string>();
            var stats = new Dictionary<int, (int ok, int fail, int dropped, int missing)>();
            foreach (var seed in new[] { 1001, 2002, 3003, 4004, 5005, 6006, 7007, 8008 })
            {
                var run = StartRun(seed);
                for (var f = 0; f < 5; f++)
                {
                    if (f > 0) run.AdvanceToNextFloor();
                    var boss = run.CurrentFloorGraph.First(n => n.Type == NodeType.Boss);
                    run.TryAdvanceToNode(boss.Index, forced: true);
                    var choices = run.RollBossModifierChoices();
                    run.GetBossModifierCounts(out _, out var picks);
                    var chosen = choices.Take(picks).ToList();
                    run.ChooseBossModifiers(chosen);
                    var cfg = run.BuildLevelConfig(true, false, boss.Index);
                    cfg.BoardSize = Mathf.Max(cfg.BoardSize, 8); // same as RunMapController.TryAdvanceToBossNodeAsync
                    var requested = cfg.ActiveModifiers.ToList();
                    var ok = RunAsync(run, cfg, 30000, out var ms);
                    var active = run.CurrentLevelConfig?.ActiveModifiers ?? new List<BossModifierId>();
                    var dropped = requested.Where(m => !active.Contains(m)).ToList();
                    var missing = run.LastGenerationMissingModifiers.ToList();
                    Probe($"  seed {seed} floor {f + 1}: ok={ok} {ms}ms size={run.CurrentBoard?.Size} stars={cfg.Stars} requested=[{string.Join(",", requested)}] dropped=[{string.Join(",", dropped)}] overlayMissing=[{string.Join(",", missing)}]");
                    (int ok, int fail, int dropped, int missing) s = stats.TryGetValue(f, out var cur) ? cur : (0, 0, 0, 0);
                    stats[f] = (s.ok + (ok ? 1 : 0), s.fail + (ok ? 0 : 1), s.dropped + (dropped.Count > 0 ? 1 : 0), s.missing + (missing.Count > 0 ? 1 : 0));
                    if (!ok)
                    {
                        failures.Add($"seed {seed} F{f + 1}: boss generation failed ({ms}ms)");
                        // Retry exactly like the player would after the UI rolls back to the map.
                        var retryCfg = run.BuildLevelConfig(true, false, boss.Index);
                        retryCfg.BoardSize = Mathf.Max(retryCfg.BoardSize, 8);
                        var retryOk = RunAsync(run, retryCfg, 30000, out var ms2);
                        Probe($"    retry same node: seedEqual={retryCfg.Seed == cfg.Seed} ok={retryOk} {ms2}ms");
                        if (!retryOk) failures.Add($"seed {seed} F{f + 1}: retry also fails -> player blocked at boss gate");
                        if (!retryOk) break;
                    }
                    else
                    {
                        if (chosen.Any(m => !active.Contains(m))) failures.Add($"seed {seed} F{f + 1}: chosen modifier dropped {string.Join(",", chosen.Where(m => !active.Contains(m)))}");
                        if (missing.Count > 0) failures.Add($"seed {seed} F{f + 1}: overlay missing for {string.Join(",", missing)}");
                        var b = run.CurrentBoard;
                        for (var r = 0; r < b.Size; r++)
                        for (var c = 0; c < b.Size; c++)
                            if (b.Cells[r, c] == 0) b.PlaceValue(r, c, b.Solution[r, c]);
                        run.CompleteLevelAndGrantRewards();
                    }
                }
            }
            foreach (var kv in stats.OrderBy(k => k.Key))
                Probe($"  floor {kv.Key + 1} summary: ok={kv.Value.ok} fail={kv.Value.fail} withDroppedMods={kv.Value.dropped} withMissingOverlay={kv.Value.missing}");
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────── D19 placement correctness semantics ─────────

        [Test]
        public void D19_Placement_NonSolutionDigitThatFitsRulesIsAccepted()
        {
            var accepted = 0;
            var tried = 0;
            foreach (var seed in Enumerable.Range(0, 30).Select(i => 300 + i))
            {
                var run = StartRun(seed);
                var node = FirstReachablePuzzle(run);
                run.TryAdvanceToNode(node.Index);
                run.StartLevel(run.BuildLevelConfig(false, false, node.Index));
                var b = run.CurrentBoard;
                var found = false;
                for (var r = 0; r < b.Size && !found; r++)
                for (var c = 0; c < b.Size && !found; c++)
                {
                    if (b.Cells[r, c] != 0) continue;
                    for (var v = 1; v <= b.Size && !found; v++)
                    {
                        if (v == b.Solution[r, c]) continue;
                        if (!run.ConstraintEngine.ValidateAll(b, r, c, v, run.CurrentOverlay)) continue;
                        tried++;
                        var res = run.PlaceNumber(r, c, v);
                        if (res == PlaceResult.Correct) accepted++;
                        found = true;
                    }
                }
            }
            Probe($"  non-solution digits that fit current rules: tried={tried} accepted as Correct={accepted} (no HP loss, combo+1)");
            Assert.AreEqual(0, accepted, "Spec (GDS 5.1, BossMechanics 'PlaceNumber checks Solution') treats any non-solution digit as a wrong placement");
        }

        // ───────── D20 non-boss generation reliability on late floors ─────────

        [Test]
        public void D20_LateFloorPuzzles_AsyncGenerationReliability()
        {
            var failures = new List<string>();
            var results = new Dictionary<string, (int ok, int fail, long maxMs)>();
            foreach (var seed in Enumerable.Range(0, 12).Select(i => 9100 + i * 7))
            {
                var run = StartRun(seed);
                for (var f = 0; f < 5; f++)
                {
                    if (f > 0) run.AdvanceToNextFloor();
                    if (f < 3) continue;
                    foreach (var node in run.CurrentFloorGraph.Where(n => n.Type == NodeType.Puzzle || n.Type == NodeType.ElitePuzzle || n.Type == NodeType.PreBoss).Take(3))
                    {
                        var cfg = run.BuildLevelConfig(false, node.Type == NodeType.ElitePuzzle, node.Index, node.Type == NodeType.PreBoss);
                        var ok = RunAsync(run, cfg, 20000, out var ms);
                        var key = $"F{f + 1} {cfg.BoardSize}x{cfg.BoardSize} v{cfg.RegionVariant}";
                        (int ok, int fail, long maxMs) cur = results.TryGetValue(key, out var x) ? x : (0, 0, 0L);
                        results[key] = (cur.ok + (ok ? 1 : 0), cur.fail + (ok ? 0 : 1), Math.Max(cur.maxMs, ms));
                        if (!ok) failures.Add($"seed {seed} F{f + 1} node {node.Index} {key} mods=[{string.Join(",", cfg.ActiveModifiers)}] failed after {ms}ms");
                    }
                }
            }
            foreach (var kv in results.OrderBy(k => k.Key))
                Probe($"  {kv.Key}: ok={kv.Value.ok} fail={kv.Value.fail} maxMs={kv.Value.maxMs}");
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────── D21 per-rule semantic enforcement on a solved board ─────────

        [Test]
        public void D21_ModifierRules_RejectWrongDigitsAtConstrainedCells()
        {
            var failures = new List<string>();
            var mods = ((BossModifierId[])Enum.GetValues(typeof(BossModifierId)))
                .Where(BossService.IsImplementedModifier).Distinct().OrderBy(m => (int)m).ToList();
            foreach (var mod in mods)
            {
                var size = Math.Max(BossService.GetMinimumBoardSize(mod), 6);
                if (mod == BossModifierId.AntiBishop) size = 9;
                var run = new RunDirector();
                try
                {
                    run.StartTutorialRun(new TutorialSetupConfig { BoardSize = size, Stars = 4, RegionVariant = 0, SelectedModifiers = new List<BossModifierId> { mod } }, 5000 + (int)mod);
                }
                catch (Exception ex) { failures.Add($"{mod}: {ex.GetType().Name}"); continue; }
                var b = run.CurrentBoard;
                if (b == null) { Probe($"  {mod,-18} {size}x{size}: NO BOARD"); continue; }
                var n = b.Size;
                var sol = new int[n, n];
                var cells = new int[n, n];
                var reg = new int[n, n];
                for (var r = 0; r < n; r++)
                for (var c = 0; c < n; c++) { sol[r, c] = b.Solution[r, c]; cells[r, c] = b.Solution[r, c]; reg[r, c] = b.RegionMap[r, c]; }
                var filled = new SudokuBoard(n, sol, cells, reg);
                var rules = ModifierFactory.BuildRules(new List<BossModifierId> { mod });
                int constrainedCells = 0, rejections = 0, solutionRejected = 0;
                for (var r = 0; r < n; r++)
                for (var c = 0; c < n; c++)
                {
                    var keep = filled.Cells[r, c];
                    filled.Cells[r, c] = 0;
                    if (rules.Any(rule => !rule.IsValid(filled, r, c, keep, run.CurrentOverlay))) solutionRejected++;
                    var cellRejects = 0;
                    for (var v = 1; v <= n; v++)
                        if (v != keep && rules.Any(rule => !rule.IsValid(filled, r, c, v, run.CurrentOverlay))) cellRejects++;
                    if (cellRejects > 0) constrainedCells++;
                    rejections += cellRejects;
                    filled.Cells[r, c] = keep;
                }
                Probe($"  {mod,-18} {n}x{n} rules={rules.Count} constrainedCells={constrainedCells}/{n * n} wrongDigitRejections={rejections} solutionDigitRejected={solutionRejected}");
                if (rules.Count == 0) failures.Add($"{mod}: no constraint rule registered");
                if (mod != BossModifierId.FogOfWar && constrainedCells == 0) failures.Add($"{mod}: rule never rejects any wrong digit");
                if (solutionRejected > 0) failures.Add($"{mod}: rule rejects the stored solution digit in {solutionRejected} cells");
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }
    }
}
