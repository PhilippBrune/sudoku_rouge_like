// TEST INFRASTRUCTURE — temporary diagnostic created for the 2026-10-01 full game review
// (docs/review/*). It drives the real runtime services with fixed seeds and records the
// observed behaviour to TestResults/review-2026-10-01/probe.txt as review evidence.
// Assertions encode the CURRENT SPEC values, so failures here are review findings, not
// regressions. Promote individual cases into permanent tests or delete this file.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    [Explicit("Review diagnostics (2026-10-01): assertions encode the spec and fail by design; run on demand only.")]
    [Category("ReviewDiagnostics")]
    public sealed class ReviewDiagnosticsTests
    {
        private static readonly string ProbePath =
            Path.Combine("TestResults", "review-2026-10-01", "probe.txt");

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
                foreach (var e in _errors.Distinct().Take(5))
                    Probe("    " + e);
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

        // ───────────────────────── helpers ─────────────────────────

        private static RunDirector StartRun(int seed, ClassId cls = ClassId.NumberFreak,
            int harmony = 0, bool irregular = true, int classLevel = 1)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = cls,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = irregular,
                ClassLevel = classLevel,
                HarmonyLevel = harmony
            }, seed);
            return run;
        }

        private static bool IsPuzzleNode(NodeType t) =>
            t == NodeType.Puzzle || t == NodeType.ElitePuzzle || t == NodeType.PreBoss || t == NodeType.Boss;

        private static LevelConfig ConfigFor(RunDirector run, RunNode node) =>
            run.BuildLevelConfig(node.Type == NodeType.Boss, node.Type == NodeType.ElitePuzzle,
                node.Index, node.Type == NodeType.PreBoss);

        private static string GraphSig(List<RunNode> g) =>
            string.Join("|", g.Select(n => $"{n.Index}:{n.Type}:{n.Route}:{string.Join(",", n.NextNodes)}"));

        private static string BoardSig(SudokuBoard b)
        {
            if (b == null) return "null";
            var sb = new StringBuilder();
            sb.Append(b.Size).Append(':');
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                sb.Append(b.Cells[r, c]).Append(b.Solution[r, c]).Append(b.RegionMap[r, c]);
            return sb.ToString();
        }

        private static string OverlaySig(ModifierOverlayData o) => o == null
            ? "null"
            : $"L{o.Lines.Count} K{o.KropkiDots.Count} C{o.KillerCages.Count} A{o.Arrows.Count} " +
              $"M{o.CellMarkers.Count} F{o.FogCells.Count} P{o.PairConstraints.Count}";

        private static int GivenCount(SudokuBoard b)
        {
            var n = 0;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] != 0) n++;
            return n;
        }

        /// <summary>Solution satisfies base rules (rows/cols/regions each contain 1..N once).</summary>
        private static bool SolutionIsValidSudoku(SudokuBoard b)
        {
            var n = b.Size;
            for (var i = 0; i < n; i++)
            {
                var row = new HashSet<int>();
                var col = new HashSet<int>();
                for (var j = 0; j < n; j++)
                {
                    row.Add(b.Solution[i, j]);
                    col.Add(b.Solution[j, i]);
                }
                if (row.Count != n || col.Count != n || row.Min() != 1 || row.Max() != n) return false;
            }
            var regions = new Dictionary<int, HashSet<int>>();
            for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
            {
                if (!regions.TryGetValue(b.RegionMap[r, c], out var set))
                    regions[b.RegionMap[r, c]] = set = new HashSet<int>();
                set.Add(b.Solution[r, c]);
            }
            return regions.Count == n && regions.Values.All(s => s.Count == n);
        }

        private static bool RegionsContiguous(SudokuBoard b)
        {
            var n = b.Size;
            var seen = new bool[n, n];
            var regionsSeen = new HashSet<int>();
            for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
            {
                if (seen[r, c]) continue;
                var id = b.RegionMap[r, c];
                if (!regionsSeen.Add(id)) return false; // second disjoint component of same region
                var q = new Queue<(int, int)>();
                q.Enqueue((r, c));
                seen[r, c] = true;
                while (q.Count > 0)
                {
                    var (cr, cc) = q.Dequeue();
                    foreach (var (dr, dc) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int nr = cr + dr, nc = cc + dc;
                        if (nr < 0 || nc < 0 || nr >= n || nc >= n || seen[nr, nc]) continue;
                        if (b.RegionMap[nr, nc] != id) continue;
                        seen[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }
            }
            return true;
        }

        /// <summary>Counts base-rule solutions of the puzzle grid, stopping at <paramref name="limit"/>.</summary>
        private static int CountSolutions(SudokuBoard b, int limit, int nodeBudget = 200000)
        {
            var n = b.Size;
            var g = new int[n, n];
            for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
                g[r, c] = b.Cells[r, c];
            var count = 0;
            var nodes = 0;

            bool Ok(int r, int c, int v)
            {
                for (var i = 0; i < n; i++)
                    if (g[r, i] == v || g[i, c] == v) return false;
                var reg = b.RegionMap[r, c];
                for (var rr = 0; rr < n; rr++)
                for (var cc = 0; cc < n; cc++)
                    if (b.RegionMap[rr, cc] == reg && g[rr, cc] == v) return false;
                return true;
            }

            void Solve()
            {
                if (count >= limit || ++nodes > nodeBudget) return;
                int br = -1, bc = -1, best = int.MaxValue;
                for (var r = 0; r < n; r++)
                for (var c = 0; c < n; c++)
                {
                    if (g[r, c] != 0) continue;
                    var opts = 0;
                    for (var v = 1; v <= n; v++) if (Ok(r, c, v)) opts++;
                    if (opts < best) { best = opts; br = r; bc = c; }
                }
                if (br < 0) { count++; return; }
                for (var v = 1; v <= n && count < limit; v++)
                {
                    if (!Ok(br, bc, v)) continue;
                    g[br, bc] = v;
                    Solve();
                    g[br, bc] = 0;
                }
            }

            Solve();
            return nodes > nodeBudget ? -1 : count;
        }

        private static SudokuBoard FilledCopy(SudokuBoard b)
        {
            var n = b.Size;
            var sol = new int[n, n];
            var cells = new int[n, n];
            var reg = new int[n, n];
            for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
            {
                sol[r, c] = b.Solution[r, c];
                cells[r, c] = b.Solution[r, c];
                reg[r, c] = b.RegionMap[r, c];
            }
            return new SudokuBoard(n, sol, cells, reg);
        }

        private static SudokuConstraintEngine EngineFor(List<BossModifierId> mods)
        {
            var engine = new SudokuConstraintEngine();
            foreach (var rule in ModifierFactory.BuildRules(mods))
                engine.RegisterRule(rule);
            return engine;
        }

        /// <summary>Number of filled-solution cells that the full rule set flags as conflicting.</summary>
        private static int SolutionConflicts(SudokuBoard b, List<BossModifierId> mods, ModifierOverlayData overlay)
        {
            var filled = FilledCopy(b);
            var engine = EngineFor(mods);
            var conflicts = 0;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (engine.HasConstraintConflict(filled, r, c, overlay)) conflicts++;
            return conflicts;
        }

        /// <summary>Wrong digits that pass base Sudoku rules but are rejected by the modifier rules.</summary>
        private static int ModifierOnlyRejections(SudokuBoard b, List<BossModifierId> mods, ModifierOverlayData overlay)
        {
            var engine = EngineFor(mods);
            var rejections = 0;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
            {
                if (b.Cells[r, c] != 0) continue;
                for (var v = 1; v <= b.Size; v++)
                {
                    if (v == b.Solution[r, c]) continue;
                    if (!SudokuValidator.IsMoveValid(b, r, c, v)) continue;
                    if (!engine.ValidateAll(b, r, c, v, overlay)) rejections++;
                }
            }
            return rejections;
        }

        private static void SolveWithRun(RunDirector run, out int nonCorrect)
        {
            nonCorrect = 0;
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
            {
                if (b.Cells[r, c] != 0 || b.IsGiven(r, c)) continue;
                var res = run.PlaceNumber(r, c, b.Solution[r, c]);
                if (res != PlaceResult.Correct && res != PlaceResult.IsPresolved) nonCorrect++;
            }
        }

        private static RunNode FirstReachablePuzzle(RunDirector run)
        {
            foreach (var idx in run.GetReachableNodes())
                if (IsPuzzleNode(run.CurrentFloorGraph[idx].Type))
                    return run.CurrentFloorGraph[idx];
            return null;
        }

        // ───────────────────────── D01 determinism ─────────────────────────

        [Test]
        public void D01_Determinism_SameSeedReproducesPathPuzzleModifiersRewards()
        {
            string Trace(int seed)
            {
                var run = StartRun(seed);
                var sb = new StringBuilder();
                sb.Append("G0=").Append(GraphSig(run.CurrentFloorGraph)).Append('\n');
                var node = FirstReachablePuzzle(run);
                run.TryAdvanceToNode(node.Index);
                var cfg = ConfigFor(run, node);
                run.StartLevel(cfg);
                sb.Append($"CFG size={cfg.BoardSize} stars={cfg.Stars} var={cfg.RegionVariant} seed={cfg.Seed} mods=[{string.Join(",", cfg.ActiveModifiers)}]\n");
                sb.Append("B=").Append(BoardSig(run.CurrentBoard)).Append('\n');
                SolveWithRun(run, out _);
                run.CompleteLevelAndGrantRewards();
                var slots = run.BuildItemRewardSlots();
                sb.Append("R=").Append(string.Join(",", slots.Select(s => s == null ? "Nothing" : $"{s.Type}/{s.Rarity}"))).Append('\n');
                sb.Append($"GOLD={run.State.CurrentGold}\n");
                for (var f = 1; f < 5; f++)
                {
                    run.AdvanceToNextFloor();
                    sb.Append($"F{f} floorMods=[{string.Join(",", run.State.ActiveFloorModifiers)}] fx={run.State.HasPositiveFloorEffect}:{run.State.ActivePositiveFloorEffect} G={GraphSig(run.CurrentFloorGraph).GetHashCode()}\n");
                }
                sb.Append("BOSS=").Append(string.Join(",", run.RollBossModifierChoices())).Append('\n');
                sb.Append("SHOP=").Append(string.Join(",", run.BuildShopOffers().Select(o => $"{o.Item.Type}/{o.Item.Rarity}/{o.Price}"))).Append('\n');
                return sb.ToString();
            }

            var a = Trace(424200);
            var b = Trace(424200);
            var c = Trace(424201);
            Probe("  run A (seed 424200):\n" + string.Join("\n", a.Split('\n').Select(l => "    " + (l.Length > 220 ? l.Substring(0, 220) + "…" : l))));
            Probe($"  same seed identical: {a == b}");
            Probe($"  different seed differs: {a != c}");
            if (a != b)
            {
                var la = a.Split('\n');
                var lb = b.Split('\n');
                for (var i = 0; i < Math.Min(la.Length, lb.Length); i++)
                    if (la[i] != lb[i]) Probe($"  FIRST DIFF line {i}: A={la[i].Substring(0, Math.Min(160, la[i].Length))} | B={lb[i].Substring(0, Math.Min(160, lb[i].Length))}");
            }
            Assert.AreEqual(a, b, "Same seed must reproduce path, puzzle, modifiers and rewards.");
            Assert.AreNotEqual(a, c, "A different seed should produce a different run.");
        }

        // ───────────────────────── D02 generation ─────────────────────────

        [Test]
        public void D02_Generation_AllSizesVariantsStars_ValidRegionsGivensUniqueness()
        {
            var failures = new List<string>();
            for (var size = 5; size <= 9; size++)
            {
                for (var variant = 0; variant <= 3; variant++)
                {
                    var line = new StringBuilder($"  {size}x{size} v{variant}: ");
                    for (var stars = 1; stars <= 6; stars++)
                    {
                        var missing = StarDensityService.MissingPercentForStars(stars);
                        var sw = Stopwatch.StartNew();
                        SudokuBoard board;
                        try
                        {
                            board = SudokuGenerator.CreatePuzzle(size, missing, 9000 + size * 100 + variant * 10 + stars, variant);
                        }
                        catch (Exception ex)
                        {
                            failures.Add($"{size}x{size} v{variant} {stars}★ threw {ex.GetType().Name}: {ex.Message}");
                            line.Append($"{stars}★=EXC ");
                            continue;
                        }
                        sw.Stop();
                        var valid = SolutionIsValidSudoku(board);
                        var contiguous = RegionsContiguous(board);
                        var givens = GivenCount(board);
                        var total = size * size;
                        var givensMatchSolution = true;
                        for (var r = 0; r < size; r++)
                        for (var c = 0; c < size; c++)
                            if (board.Cells[r, c] != 0 && board.Cells[r, c] != board.Solution[r, c]) givensMatchSolution = false;
                        var sols = CountSolutions(board, 2);
                        var expectedMissing = (int)Math.Round(total * missing);
                        line.Append($"{stars}★ g={givens}/{total} miss={total - givens}(spec≈{expectedMissing}) sol={(sols < 0 ? "?" : sols >= 2 ? "≥2" : sols.ToString())} {sw.ElapsedMilliseconds}ms; ");
                        if (!valid) failures.Add($"{size}x{size} v{variant} {stars}★ invalid solution");
                        if (!contiguous) failures.Add($"{size}x{size} v{variant} non-contiguous region");
                        if (!givensMatchSolution) failures.Add($"{size}x{size} v{variant} {stars}★ given != solution");
                        if (Math.Abs((total - givens) - expectedMissing) > 1)
                            failures.Add($"{size}x{size} v{variant} {stars}★ missing {total - givens} vs spec {expectedMissing}");
                    }
                    Probe(line.ToString());
                }
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        [Test]
        public void D02b_StarDensity_MatchesSpecFormula()
        {
            var lines = new List<string>();
            for (var s = 1; s <= 7; s++)
                lines.Add($"{s}★={StarDensityService.MissingPercentForStars(s):0.00}");
            Probe("  " + string.Join(" ", lines));
            for (var s = 1; s <= 6; s++)
                Assert.AreEqual((s + 3) * 0.1f, StarDensityService.MissingPercentForStars(s), 0.001f, $"{s}★");
        }

        // ───────────────────────── D03 modifiers ─────────────────────────

        private static IEnumerable<BossModifierId> PlayerModifiers() =>
            ((BossModifierId[])Enum.GetValues(typeof(BossModifierId)))
            .Where(BossService.IsImplementedModifier).Distinct().OrderBy(m => (int)m);

        [Test]
        public void D03_Modifiers_EachGeneratesSatisfiableAndRestrictiveConstraints()
        {
            var failures = new List<string>();
            foreach (var mod in PlayerModifiers())
            {
                var minSize = BossService.GetMinimumBoardSize(mod);
                foreach (var size in new[] { minSize, 9 }.Distinct())
                {
                    var run = new RunDirector();
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        run.StartTutorialRun(new TutorialSetupConfig
                        {
                            BoardSize = size,
                            Stars = 4,
                            ResourceMode = TutorialResourceMode.Free,
                            RegionVariant = 0,
                            SelectedModifiers = new List<BossModifierId> { mod }
                        }, seed: 31337 + (int)mod);
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{mod}@{size}: StartTutorialRun threw {ex.GetType().Name}: {ex.Message}");
                        continue;
                    }
                    sw.Stop();
                    var b = run.CurrentBoard;
                    if (b == null)
                    {
                        failures.Add($"{mod}@{size}: no board generated");
                        Probe($"  {mod,-18} {size}x{size}: NO BOARD ({sw.ElapsedMilliseconds}ms)");
                        continue;
                    }
                    var active = run.CurrentLevelConfig.ActiveModifiers;
                    var missing = run.LastGenerationMissingModifiers;
                    var conflicts = SolutionConflicts(b, active, run.CurrentOverlay);
                    var restrict = ModifierOnlyRejections(b, active, run.CurrentOverlay);
                    SolveWithRun(run, out var nonCorrect);
                    Probe($"  {mod,-18} {b.Size}x{b.Size} active=[{string.Join(",", active)}] overlay={OverlaySig(run.CurrentOverlay)} " +
                          $"missing=[{string.Join(",", missing)}] solConflicts={conflicts} modOnlyRejections={restrict} " +
                          $"solveNonCorrect={nonCorrect} complete={run.IsLevelComplete} {sw.ElapsedMilliseconds}ms");
                    if (!active.Contains(mod)) failures.Add($"{mod}@{size}: modifier dropped from active list");
                    if (missing.Count > 0) failures.Add($"{mod}@{size}: overlay missing {string.Join(",", missing)}");
                    if (conflicts > 0) failures.Add($"{mod}@{size}: stored solution violates constraints in {conflicts} cells");
                    if (mod != BossModifierId.FogOfWar && restrict == 0)
                        failures.Add($"{mod}@{size}: modifier rejects no base-legal wrong digit (no effective constraint)");
                    if (!run.IsLevelComplete) failures.Add($"{mod}@{size}: placing the solution did not complete the level ({nonCorrect} non-correct)");
                }
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────────────────────── D04/D05 boss flow ─────────────────────────

        [Test]
        public void D04_BossGate_FloorModifierAndChoiceCountsMatchSpecTable()
        {
            int[] specFloorMods = { 0, 1, 2, 3, 4 };
            int[] specShown = { 3, 3, 4, 5, 6 };
            int[] specPicks = { 2, 2, 3, 4, 5 };
            var failures = new List<string>();
            foreach (var harmony in new[] { 0, 7, 10 })
            {
                var run = StartRun(5150 + harmony, harmony: harmony);
                for (var f = 0; f < 5; f++)
                {
                    if (f > 0) run.AdvanceToNextFloor();
                    var choices = run.RollBossModifierChoices();
                    run.GetBossModifierCounts(out var shownFormula, out var picks);
                    Probe($"  H{harmony} floor {f + 1}: floorMods={run.State.ActiveFloorModifiers.Count} [{string.Join(",", run.State.ActiveFloorModifiers)}] " +
                          $"choicesShown={choices.Count} picksRequired(UI)={picks} formulaShown={shownFormula}");
                    if (harmony == 0)
                    {
                        if (run.State.ActiveFloorModifiers.Count != specFloorMods[f])
                            failures.Add($"H0 floor {f + 1}: floor mods {run.State.ActiveFloorModifiers.Count} vs spec {specFloorMods[f]}");
                        if (choices.Count != specShown[f])
                            failures.Add($"H0 floor {f + 1}: shown {choices.Count} vs spec {specShown[f]}");
                        if (picks != specPicks[f])
                            failures.Add($"H0 floor {f + 1}: picks {picks} vs spec {specPicks[f]}");
                    }
                    else if (choices.Count != picks + 1)
                    {
                        failures.Add($"H{harmony} floor {f + 1}: shown {choices.Count} but UI requires {picks} picks (exclusion invariant broken)");
                    }
                }
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        [Test]
        public void D05_BossGate_ChosenModifiersAreInjectedIntoBossPuzzle()
        {
            var failures = new List<string>();
            foreach (var seed in new[] { 1001, 2002, 3003 })
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
                    var cfg = run.BuildLevelConfig(isBoss: true, isElite: false, nodeIndex: boss.Index);
                    var sw = Stopwatch.StartNew();
                    run.StartLevel(cfg);
                    sw.Stop();
                    var active = run.CurrentLevelConfig?.ActiveModifiers ?? new List<BossModifierId>();
                    var notApplied = chosen.Where(m => !active.Contains(m)).ToList();
                    var floorNotApplied = run.State.ActiveFloorModifiers.Where(m => !active.Contains(m)).ToList();
                    var b = run.CurrentBoard;
                    var conflicts = b == null ? -1 : SolutionConflicts(b, active, run.CurrentOverlay);
                    var minS = FloorThemeData.GetMinBoardSize(f);
                    var maxS = FloorThemeData.GetMaxBoardSize(f);
                    Probe($"  seed {seed} floor {f + 1}: chosen=[{string.Join(",", chosen)}] floor=[{string.Join(",", run.State.ActiveFloorModifiers)}] " +
                          $"active=[{string.Join(",", active)}] board={(b == null ? "NULL" : b.Size + "x" + b.Size)} (floor range {minS}-{maxS}) " +
                          $"stars={cfg.Stars} intensity={cfg.Intensity} overlay={OverlaySig(run.CurrentOverlay)} missing=[{string.Join(",", run.LastGenerationMissingModifiers)}] " +
                          $"solConflicts={conflicts} rules={run.ConstraintEngine?.RuleCount} {sw.ElapsedMilliseconds}ms");
                    if (b == null) { failures.Add($"seed {seed} F{f + 1}: boss board not generated"); continue; }
                    if (notApplied.Count > 0) failures.Add($"seed {seed} F{f + 1}: chosen not applied {string.Join(",", notApplied)}");
                    if (floorNotApplied.Count > 0) failures.Add($"seed {seed} F{f + 1}: floor mods not applied {string.Join(",", floorNotApplied)}");
                    if (run.LastGenerationMissingModifiers.Count > 0) failures.Add($"seed {seed} F{f + 1}: overlay missing {string.Join(",", run.LastGenerationMissingModifiers)}");
                    if (conflicts > 0) failures.Add($"seed {seed} F{f + 1}: solution violates constraints in {conflicts} cells");
                    if (b.Size < minS || b.Size > maxS) failures.Add($"seed {seed} F{f + 1}: boss board {b.Size} outside floor range {minS}-{maxS}");
                    SolveWithRun(run, out var nonCorrect);
                    if (!run.IsLevelComplete) failures.Add($"seed {seed} F{f + 1}: boss not completable ({nonCorrect} non-correct placements)");
                    var entry = run.CompleteLevelAndGrantRewards();
                    if (entry == null || !entry.IsBoss) failures.Add($"seed {seed} F{f + 1}: XP entry not flagged IsBoss (BoardSize={entry?.BoardSize}, Stars={entry?.Stars})");
                }
                Probe($"  seed {seed}: BossesDefeatedThisRun={run.State.BossesDefeatedThisRun}, TileXpLog={run.TileXpLog.Count}, totalXp={run.GetTotalRunXp()}");
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────────────────────── D06 path graph ─────────────────────────

        [Test]
        public void D06_PathGraph_ConnectivityLengthsCrossLinksElite()
        {
            int[] calmMin = { 5, 6, 7, 8, 9 };
            int[] calmMax = { 8, 9, 10, 11, 12 };
            var failures = new List<string>();
            foreach (var seed in new[] { 11, 22, 33, 44, 55, 66 })
            {
                var run = StartRun(seed);
                for (var f = 0; f < 5; f++)
                {
                    if (f > 0) run.AdvanceToNextFloor();
                    var g = run.CurrentFloorGraph;
                    var start = g.FindIndex(n => n.Type == NodeType.Start);
                    var bossIdx = g.FindIndex(n => n.Type == NodeType.Boss);
                    var reach = new HashSet<int> { start };
                    var q = new Queue<int>();
                    q.Enqueue(start);
                    while (q.Count > 0)
                        foreach (var nx in g[q.Dequeue()].NextNodes)
                            if (reach.Add(nx)) q.Enqueue(nx);
                    var unreachable = g.Where(n => !reach.Contains(n.Index)).Select(n => n.Index).ToList();
                    var deadEnds = new List<int>();
                    foreach (var n in g)
                    {
                        if (n.Type == NodeType.Boss) continue;
                        var seen = new HashSet<int> { n.Index };
                        var qq = new Queue<int>();
                        qq.Enqueue(n.Index);
                        var ok = false;
                        while (qq.Count > 0 && !ok)
                            foreach (var nx in g[qq.Dequeue()].NextNodes)
                            {
                                if (nx == bossIdx) { ok = true; break; }
                                if (seen.Add(nx)) qq.Enqueue(nx);
                            }
                        if (!ok) deadEnds.Add(n.Index);
                    }
                    var calm = g.Count(n => n.Route == RouteType.CalmRoute && n.Type != NodeType.Start && n.Type != NodeType.Boss);
                    var risk = g.Count(n => n.Route == RouteType.RiskRoute && n.Type != NodeType.Start && n.Type != NodeType.Boss);
                    var cross = g.Count(n => n.IsCrossLink);
                    var crossEdges = g.Sum(n => n.NextNodes.Count(nx => g[nx].Route != n.Route
                        && n.Type != NodeType.Start && g[nx].Type != NodeType.Boss));
                    var preBoss = g.Where(n => n.NextNodes.Contains(bossIdx)).Select(n => n.Type).ToList();
                    var types = string.Join(",", g.GroupBy(n => n.Type).Select(gr => $"{gr.Key}:{gr.Count()}"));
                    Probe($"  seed {seed} floor {f + 1}: nodes={g.Count} calm={calm} risk={risk} crossLinkNodes={cross} crossEdges={crossEdges} " +
                          $"preBoss=[{string.Join(",", preBoss)}] unreachable=[{string.Join(",", unreachable)}] deadEnds=[{string.Join(",", deadEnds)}] types={types}");
                    if (unreachable.Count > 0) failures.Add($"seed {seed} F{f + 1}: unreachable nodes {string.Join(",", unreachable)}");
                    if (deadEnds.Count > 0) failures.Add($"seed {seed} F{f + 1}: nodes that cannot reach boss {string.Join(",", deadEnds)}");
                    if (calm < calmMin[f] || calm > calmMax[f]) failures.Add($"seed {seed} F{f + 1}: calm length {calm} outside spec {calmMin[f]}-{calmMax[f]}");
                    if (crossEdges / 2 > 1 && cross > 2) failures.Add($"seed {seed} F{f + 1}: {cross} cross-link nodes (spec: exactly one cross-connection)");
                    if (preBoss.Any(t => t != NodeType.ElitePuzzle && t != NodeType.PreBoss))
                        failures.Add($"seed {seed} F{f + 1}: node before boss is not Elite/PreBoss ({string.Join(",", preBoss)})");
                }
            }
            foreach (var f in failures.Distinct()) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────────────────────── D07 class stats ─────────────────────────

        [Test]
        public void D07_ClassBaseStatsAtRunStart_MatchGameDesignSpecRoster()
        {
            var spec = new Dictionary<ClassId, (int hp, int pencil, int slots)>
            {
                [ClassId.NumberFreak] = (10, 10, 2),
                [ClassId.GardenMonk] = (14, 5, 1),
                [ClassId.ShrineArchivist] = (8, 15, 2),
                [ClassId.KoiGambler] = (9, 8, 2),
                [ClassId.StoneGardener] = (11, 8, 3),
                [ClassId.LanternSeer] = (7, 12, 2),
                [ClassId.ReedDuelist] = (9, 9, 2),
                [ClassId.QuietCartographer] = (12, 6, 1),
            };
            var failures = new List<string>();
            foreach (var kv in spec)
            {
                var run = StartRun(777, kv.Key);
                var s = run.State;
                var def = ClassCatalog.GetDefinition(kv.Key);
                Probe($"  {kv.Key,-18} run: HP {s.CurrentHP}/{s.MaxHP} Pencil {s.CurrentPencil}/{s.MaxPencil} Slots {s.ItemSlots} Rerolls {s.RerollTokens} Relics {s.HeldRelics.Count} Gold {s.CurrentGold} | catalog {def.BaseHP}/{def.BasePencil}/{def.BaseItemSlots} | GDS {kv.Value}");
                if (s.MaxHP != kv.Value.hp || s.MaxPencil != kv.Value.pencil || s.ItemSlots != kv.Value.slots)
                    failures.Add($"{kv.Key}: run start {s.MaxHP}/{s.MaxPencil}/{s.ItemSlots} vs GDS {kv.Value}");
            }
            // Level-up stat bonuses: NumberFreak L10 => spec +3 Pencil (+1 L3, +2 L10) and +4 HP (+2 L7, +2 L10)
            var lvl10 = StartRun(777, ClassId.NumberFreak, classLevel: 10).State;
            Probe($"  NumberFreak L10 run: HP {lvl10.MaxHP} Pencil {lvl10.MaxPencil} Gold {lvl10.CurrentGold} (spec: HP 14, Pencil 13, +10 gold at L5)");
            if (lvl10.MaxHP != 14 || lvl10.MaxPencil != 13)
                failures.Add($"NumberFreak L10: HP {lvl10.MaxHP} Pencil {lvl10.MaxPencil} vs spec 14/13");
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────────────────────── D08/D09 XP ─────────────────────────

        [Test]
        public void D08_XpCurve_AndPerClassIsolation()
        {
            Probe($"  CumulativeXpForLevel(40)={XpTable.CumulativeXpForLevel(40)} DeriveLevel(0)={XpTable.DeriveLevel(0)} DeriveLevel(49)={XpTable.DeriveLevel(49)} DeriveLevel(50)={XpTable.DeriveLevel(50)} DeriveLevel(16859)={XpTable.DeriveLevel(16859)} DeriveLevel(16860)={XpTable.DeriveLevel(16860)} DeriveLevel(999999)={XpTable.DeriveLevel(999999)}");
            Assert.AreEqual(16860, XpTable.CumulativeXpForLevel(40));
            Assert.AreEqual(1, XpTable.DeriveLevel(49));
            Assert.AreEqual(2, XpTable.DeriveLevel(50));
            Assert.AreEqual(39, XpTable.DeriveLevel(16859));
            Assert.AreEqual(40, XpTable.DeriveLevel(16860));

            var meta = new MetaProgressionState();
            var svc = new ClassGardenProgressionService();
            svc.AddXp(meta, ClassId.NumberFreak, 500);
            svc.AddXp(meta, ClassId.GardenMonk, 60);
            svc.AddXp(meta, ClassId.NumberFreak, 999999);
            Probe($"  NF total={svc.GetTotalXp(meta, ClassId.NumberFreak)} lvl={svc.GetLevel(meta, ClassId.NumberFreak)} | GM total={svc.GetTotalXp(meta, ClassId.GardenMonk)} lvl={svc.GetLevel(meta, ClassId.GardenMonk)} | KG total={svc.GetTotalXp(meta, ClassId.KoiGambler)}");
            Assert.AreEqual(60, svc.GetTotalXp(meta, ClassId.GardenMonk));
            Assert.AreEqual(0, svc.GetTotalXp(meta, ClassId.KoiGambler));
            Assert.AreEqual(40, svc.GetLevel(meta, ClassId.NumberFreak));
            Probe($"  excess XP beyond L40 retained (spec: discarded): {svc.GetTotalXp(meta, ClassId.NumberFreak) > 16860}");

            // Tile XP formula spot checks (spec XP-TILE-002..004)
            var t1 = XpService.CalculateTile(6, 2, 0, false, true);   // 45*1.2=54 +25 = 79
            var t2 = XpService.CalculateTile(9, 4, 1, true, false);   // 120*1.5=180 *1.5=270 +15 = 285
            Probe($"  tile 6x6 2★ perfect = {t1.TotalXp} (spec 79); boss 9x9 4★ 1 mod = {t2.TotalXp} (formula 285, spec example says 210); metadata size={t2.BoardSize} stars={t2.Stars} isBoss={t2.IsBoss}");
            Assert.AreEqual(79, t1.TotalXp);
            Assert.AreEqual(285, t2.TotalXp);
        }

        [Test]
        public void D09_RunEnd_XpCommittedPerClass_UnlocksAndCompletion()
        {
            const int slot = 2;
            var dir = SaveFileService.SaveRoot;
            var path = Path.Combine(dir, $"save_profile_{slot}.json");
            var backup = new Dictionary<string, string>();
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                backup[p] = File.Exists(p) ? File.ReadAllText(p) : null;
            try
            {
                foreach (var p in backup.Keys) if (File.Exists(p)) File.Delete(p);
                var save = new SaveFileService(slot);
                var profile = new ProfileService(save);

                // Real run: solve one puzzle and one boss so the tile log is produced by the game code.
                var run = StartRun(9090, ClassId.NumberFreak);
                var node = FirstReachablePuzzle(run);
                run.TryAdvanceToNode(node.Index);
                run.StartLevel(ConfigFor(run, node));
                SolveWithRun(run, out _);
                run.CompleteLevelAndGrantRewards();

                var result = new RunResult
                {
                    PlayedClassId = ClassId.NumberFreak,
                    Mode = GameMode.GardenRun,
                    Victory = false,
                    XpEarned = run.GetTotalRunXp(),
                    GoldEarned = run.State.CurrentGold,
                    BossesDefeatedThisRun = 2,
                    TileXpEntries = run.TileXpLog
                };
                var unlocks = profile.RecordRunAndGetNewUnlocks(result);
                save.FlushPendingWrites();
                var env = save.Load();
                var svc = new ClassGardenProgressionService();
                var nfXp = svc.GetTotalXp(env.MetaProgress, ClassId.NumberFreak);
                var gmXp = svc.GetTotalXp(env.MetaProgress, ClassId.GardenMonk);
                var combos = env.Completion?.ClearedSizeStarCombos ?? new List<string>();
                Probe($"  committed XP: NumberFreak={nfXp} (run xp {result.XpEarned}), GardenMonk={gmXp}; new unlocks=[{string.Join(",", unlocks)}]; unlockedClasses=[{string.Join(",", env.MetaProgress.UnlockedClasses)}]");
                Probe($"  completion ClearedSizeStarCombos=[{string.Join(",", combos)}] (tile: {run.TileXpLog[0].BoardSize}x{run.TileXpLog[0].BoardSize} {run.TileXpLog[0].Stars}★, config {run.CurrentLevelConfig.BoardSize}x{run.CurrentLevelConfig.BoardSize} {run.CurrentLevelConfig.Stars}★)");
                Assert.AreEqual(result.XpEarned, nfXp, "XP must be committed to the played class");
                Assert.AreEqual(0, gmXp, "Other classes must not receive XP");
                CollectionAssert.Contains(unlocks, ClassId.GardenMonk, "2 cumulative boss defeats unlock Garden Monk");
                CollectionAssert.DoesNotContain(combos, "0x0_s0", "Completion must record the real board size/stars");
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
        }

        // ───────────────────────── D10/D11 items & economy ─────────────────────────

        [Test]
        public void D10_ItemRewardSlots_CountsNothingRatesEpicGate()
        {
            var failures = new List<string>();
            int[] specSlots = { 2, 3, 3, 4, 5 };
            float[] specNothing = { 0.25f, 0.22f, 0.18f, 0.15f, 0.12f };
            for (var stars = 1; stars <= 6; stars++)
            {
                var svc = new ItemService(4242 + stars);
                const int samples = 3000;
                int slotsTotal = 0, nothing = 0, epicLvl1 = 0;
                for (var i = 0; i < samples; i++)
                {
                    var slots = svc.RollSlots(stars, classLevel: 1);
                    slotsTotal += slots.Count;
                    nothing += slots.Count(s => s == null);
                    epicLvl1 += slots.Count(s => s != null && s.Rarity == ItemRarity.Epic);
                }
                var svc15 = new ItemService(99 + stars);
                var epicLvl15 = 0;
                for (var i = 0; i < samples; i++)
                    epicLvl15 += svc15.RollSlots(stars, classLevel: 15).Count(s => s != null && s.Rarity == ItemRarity.Epic);
                var avgSlots = slotsTotal / (float)samples;
                var nothingRate = nothing / (float)slotsTotal;
                Probe($"  {stars}★: slots={avgSlots:0.00} nothingRate={nothingRate:0.000} epic@L1={epicLvl1} epic@L15={epicLvl15}");
                if (stars <= 5)
                {
                    if (Math.Abs(avgSlots - specSlots[stars - 1]) > 0.01f) failures.Add($"{stars}★ slots {avgSlots} vs spec {specSlots[stars - 1]}");
                    if (Math.Abs(nothingRate - specNothing[stars - 1]) > 0.03f) failures.Add($"{stars}★ nothing rate {nothingRate:0.000} vs spec {specNothing[stars - 1]}");
                }
                if (epicLvl1 > 0) failures.Add($"{stars}★: {epicLvl1} Epic items rolled at class level 1 (spec: Epic only at L15+)");
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        [Test]
        public void D11_GoldRewardAndPrices_MatchGameDesignSpec()
        {
            int[] baseGold = { 20, 30, 45, 65, 100 };
            var lines = new List<string>();
            for (var size = 5; size <= 9; size++)
                lines.Add($"{size}x{size}: " + string.Join(" ", Enumerable.Range(1, 6).Select(s => $"{s}★={GoldTable.CalculatePuzzleGold(size, s)}")));
            Probe("  puzzle gold: " + string.Join(" | ", lines));
            Probe($"  pencil buy 0..3: {string.Join(",", Enumerable.Range(0, 4).Select(GoldTable.PencilBuyCost))}; item reroll 0..3: {string.Join(",", Enumerable.Range(0, 4).Select(GoldTable.RerollCost))}");
            Probe($"  shop slots by floor: {string.Join(",", Enumerable.Range(0, 5).Select(ShopService.GetShopSlotCount))}; base prices N/R/E: {ItemService.GetBasePrice(ItemRarity.Normal)}/{ItemService.GetBasePrice(ItemRarity.Rare)}/{ItemService.GetBasePrice(ItemRarity.Epic)}; Normal price by floor: {string.Join(",", Enumerable.Range(0, 5).Select(f => ItemService.GetShopPrice(ItemRarity.Normal, f)))}");
            var run = StartRun(31);
            Probe($"  RunDirector shop reroll cost 0: {run.GetShopRerollCost()}; rest heal amount (MaxHP {run.State.MaxHP}): {run.GetRestHealAmount()} (GDS: +2 HP)");
            for (var size = 5; size <= 9; size++)
            for (var s = 1; s <= 6; s++)
                Assert.AreEqual(Mathf.RoundToInt(baseGold[size - 5] * (1 + s * 0.2f)), GoldTable.CalculatePuzzleGold(size, s));
            Assert.AreEqual(20, GoldTable.PencilBuyCost(0));
            Assert.AreEqual(40, GoldTable.PencilBuyCost(1));
        }

        // ───────────────────────── D12 save/resume ─────────────────────────

        [Test]
        public void D12_SaveResume_MidPuzzleAndBossState_RoundTrip()
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

                // --- mid-puzzle on floor 2 ---
                var run = StartRun(6060);
                run.AdvanceToNextFloor();
                var node = FirstReachablePuzzle(run);
                run.TryAdvanceToNode(node.Index);
                run.StartLevel(ConfigFor(run, node));
                var b = run.CurrentBoard;
                var placed = 0;
                for (var r = 0; r < b.Size && placed < 3; r++)
                for (var c = 0; c < b.Size && placed < 3; c++)
                    if (b.Cells[r, c] == 0 && run.PlaceNumber(r, c, b.Solution[r, c]) == PlaceResult.Correct) placed++;
                var pencilOk = false;
                for (var r = 0; r < b.Size && !pencilOk; r++)
                for (var c = 0; c < b.Size && !pencilOk; c++)
                    if (b.Cells[r, c] == 0) pencilOk = run.TryAddPencilMark(r, c, 1);
                var beforeBoard = BoardSig(run.CurrentBoard);
                var beforeState = $"HP={run.State.CurrentHP} P={run.State.CurrentPencil} G={run.State.CurrentGold} F={run.State.CurrentFloor} N={run.State.CurrentNodeIndex} path=[{string.Join(",", run.State.NodePath)}] fm=[{string.Join(",", run.State.ActiveFloorModifiers)}]";
                var beforeGraph = GraphSig(run.CurrentFloorGraph);
                var beforeOverlay = OverlaySig(run.CurrentOverlay);
                var beforeMods = string.Join(",", run.CurrentLevelConfig.ActiveModifiers);

                new RunAutoSaveCoordinator(save).Save(run.State, run.ExportPuzzleSaveState());
                save.FlushPendingWrites();

                // Same restore sequence as GameBootstrap.LaunchResume
                Assert.IsTrue(new RunResumeService(save).TryResumeFromSave(out var rs, out var ps), "resume must succeed");
                var resumed = new RunDirector();
                resumed.StartRun(new LaunchRequest { ClassId = rs.ClassId, Mode = rs.Mode, AllowIrregularPuzzles = rs.AllowIrregularPuzzles, HarmonyLevel = rs.HarmonyLevel }, rs.Seed);
                resumed.RestoreState(rs);
                var savedIdx = rs.CurrentNodeIndex;
                resumed.RebuildFloorGraph();
                resumed.State.CurrentNodeIndex = savedIdx;
                var restored = resumed.TryRestorePuzzleSaveState(ps);
                var afterBoard = BoardSig(resumed.CurrentBoard);
                var afterState = $"HP={resumed.State.CurrentHP} P={resumed.State.CurrentPencil} G={resumed.State.CurrentGold} F={resumed.State.CurrentFloor} N={resumed.State.CurrentNodeIndex} path=[{string.Join(",", resumed.State.NodePath)}] fm=[{string.Join(",", resumed.State.ActiveFloorModifiers)}]";
                Probe($"  mid-puzzle: placed={placed} pencil={pencilOk} restored={restored}");
                Probe($"    state before: {beforeState}");
                Probe($"    state after : {afterState}");
                Probe($"    board equal={beforeBoard == afterBoard} graph equal={beforeGraph == GraphSig(resumed.CurrentFloorGraph)} overlay before={beforeOverlay} after={OverlaySig(resumed.CurrentOverlay)} mods before=[{beforeMods}] after=[{string.Join(",", resumed.CurrentLevelConfig?.ActiveModifiers ?? new List<BossModifierId>())}]");
                var pencilAfter = false;
                for (var r = 0; r < b.Size; r++)
                for (var c = 0; c < b.Size; c++)
                    if (resumed.CurrentBoard != null && resumed.CurrentBoard.HasAnyPencilMark(r, c)) pencilAfter = true;
                Probe($"    pencil marks survive resume: {pencilAfter}");
                if (!restored) failures.Add("puzzle state not restored");
                if (beforeBoard != afterBoard) failures.Add("board differs after resume");
                if (beforeState != afterState) failures.Add("run state differs after resume");
                if (beforeGraph != GraphSig(resumed.CurrentFloorGraph)) failures.Add("floor graph differs after resume");
                if (pencilOk && !pencilAfter) failures.Add("pencil marks lost on resume");
                if (beforeOverlay != OverlaySig(resumed.CurrentOverlay)) failures.Add($"modifier overlay differs after resume ({beforeOverlay} -> {OverlaySig(resumed.CurrentOverlay)})");

                // --- boss gate on floor 3: choices before save vs after resume ---
                var bossRun = StartRun(7070);
                bossRun.AdvanceToNextFloor();
                bossRun.AdvanceToNextFloor();
                var boss = bossRun.CurrentFloorGraph.First(n => n.Type == NodeType.Boss);
                bossRun.TryAdvanceToNode(boss.Index, forced: true);
                var choicesBefore = bossRun.RollBossModifierChoices();
                new RunAutoSaveCoordinator(save).Save(bossRun.State, null);
                save.FlushPendingWrites();
                new RunResumeService(save).TryResumeFromSave(out var brs, out _);
                var bossResumed = new RunDirector();
                bossResumed.StartRun(new LaunchRequest { ClassId = brs.ClassId, Mode = brs.Mode, AllowIrregularPuzzles = brs.AllowIrregularPuzzles }, brs.Seed);
                bossResumed.RestoreState(brs);
                var bidx = brs.CurrentNodeIndex;
                bossResumed.RebuildFloorGraph();
                bossResumed.State.CurrentNodeIndex = bidx;
                var choicesAfter = bossResumed.RollBossModifierChoices();
                Probe($"  boss gate choices before save=[{string.Join(",", choicesBefore)}] after resume=[{string.Join(",", choicesAfter)}] same={choicesBefore.SequenceEqual(choicesAfter)}");
                if (!choicesBefore.SequenceEqual(choicesAfter)) failures.Add("boss choices change after save/quit/resume (re-roll exploit / non-determinism)");

                // chosen modifiers persisted
                bossResumed.ChooseBossModifiers(choicesAfter.Take(3).ToList());
                new RunAutoSaveCoordinator(save).Save(bossResumed.State, null);
                save.FlushPendingWrites();
                new RunResumeService(save).TryResumeFromSave(out var brs2, out _);
                Probe($"  chosen persisted: before=[{string.Join(",", bossResumed.State.ChosenBossModifiers)}] after=[{string.Join(",", brs2.ChosenBossModifiers)}] seen={brs2.SeenBossModifiers.Count}");
                if (!bossResumed.State.ChosenBossModifiers.SequenceEqual(brs2.ChosenBossModifiers)) failures.Add("chosen boss modifiers not persisted");

                // item reward re-roll after resume (first reward of a resumed run vs uninterrupted)
                var r1 = StartRun(8080);
                var n1 = FirstReachablePuzzle(r1);
                r1.TryAdvanceToNode(n1.Index);
                r1.StartLevel(ConfigFor(r1, n1));
                SolveWithRun(r1, out _);
                r1.CompleteLevelAndGrantRewards();
                var rewardA = string.Join(",", r1.BuildItemRewardSlots().Select(s => s == null ? "-" : $"{s.Type}/{s.Rarity}"));
                var rewardA2 = string.Join(",", r1.BuildItemRewardSlots().Select(s => s == null ? "-" : $"{s.Type}/{s.Rarity}"));
                Probe($"  item reward roll vs immediate re-roll on same level: [{rewardA}] vs [{rewardA2}] (each call re-rolls)");
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

        // ───────────────────────── D13 harmony ─────────────────────────

        [Test]
        public void D13_Harmony_StarAndGridWeightsAffectGeneratedLevels()
        {
            string Histogram(int harmony)
            {
                var run = StartRun(1234, harmony: harmony);
                var stars = new int[8];
                var sizes = new int[10];
                for (var f = 0; f < 5; f++)
                {
                    if (f > 0) run.AdvanceToNextFloor();
                    for (var i = 0; i < 60; i++)
                    {
                        var cfg = run.BuildLevelConfig(false, false, i);
                        stars[cfg.Stars]++;
                        sizes[cfg.BoardSize]++;
                    }
                }
                return $"stars1-6=[{string.Join(",", stars.Skip(1).Take(6))}] sizes5-9=[{string.Join(",", sizes.Skip(5).Take(5))}]";
            }
            var h0 = Histogram(0);
            var h10 = Histogram(10);
            Probe($"  H0 : {h0}");
            Probe($"  H10: {h10}");
            Assert.AreNotEqual(h0, h10, "Harmony star/grid weights (spec 4.5/4.6) should change generated levels");
        }

        // ───────────────────────── D14 tutorial ─────────────────────────

        [Test]
        public void D14_Tutorial_SizesStarsModifiersAndNoProgression()
        {
            var failures = new List<string>();
            foreach (var size in new[] { 5, 6, 7, 8, 9 })
            foreach (var stars in new[] { 1, 6, 7 })
            {
                var mods = stars == 7 ? new List<BossModifierId> { BossModifierId.KillerCages } : new List<BossModifierId>();
                var run = new RunDirector();
                try
                {
                    run.StartTutorialRun(new TutorialSetupConfig { BoardSize = size, Stars = stars, RegionVariant = 3, SelectedModifiers = mods }, 4040 + size);
                }
                catch (Exception ex)
                {
                    failures.Add($"tutorial {size}x{size} {stars}★ threw {ex.GetType().Name}");
                    continue;
                }
                var b = run.CurrentBoard;
                var givens = b == null ? -1 : GivenCount(b);
                Probe($"  tutorial {size}x{size} {stars}★ mods=[{string.Join(",", run.CurrentLevelConfig?.ActiveModifiers ?? new List<BossModifierId>())}] board={(b == null ? "NULL" : b.Size.ToString())} givens={givens} gold={run.State.CurrentGold} tutorialMode={run.State.TutorialMode} noProgress={run.State.DisableProgressionRewards}");
                if (b == null) { failures.Add($"tutorial {size}x{size} {stars}★: no board"); continue; }
                if (stars == 7 && givens != 0) failures.Add($"tutorial {size}x{size} 7★: {givens} givens (spec: 0)");
                if (!run.State.DisableProgressionRewards) failures.Add("tutorial grants progression");
            }
            foreach (var f in failures) Probe("  FAIL " + f);
            Assert.IsEmpty(failures);
        }

        // ───────────────────────── D15 game over / HP ─────────────────────────

        [Test]
        public void D15_Mistakes_ReduceHpAndRunEndsAtZero()
        {
            var run = StartRun(5555);
            var node = FirstReachablePuzzle(run);
            run.TryAdvanceToNode(node.Index);
            run.StartLevel(ConfigFor(run, node));
            var b = run.CurrentBoard;
            var startHp = run.State.CurrentHP;
            int r0 = -1, c0 = -1;
            for (var r = 0; r < b.Size && r0 < 0; r++)
            for (var c = 0; c < b.Size && r0 < 0; c++)
                if (b.Cells[r, c] == 0) { r0 = r; c0 = c; }
            var wrong = b.Solution[r0, c0] % b.Size + 1;
            var results = new List<string>();
            for (var i = 0; i < startHp + 3; i++)
                results.Add(run.PlaceNumber(r0, c0, wrong).ToString());
            Probe($"  startHP={startHp} after {startHp + 3} wrong placements HP={run.State.CurrentHP} combo={run.State.ComboStreak} results=[{string.Join(",", results.Distinct())}] cellValue={b.Cells[r0, c0]}");
            Assert.Less(run.State.CurrentHP, startHp, "wrong placement must cost HP");
            Assert.LessOrEqual(run.State.CurrentHP, 0, "HP should reach 0 after enough mistakes");
        }

        // ───────────────────────── D16 pressure rolls ─────────────────────────

        [Test]
        public void D16_Pressure_RollFrequencyByFloorAndNodeType()
        {
            var pressureIds = new[] { 103, 104, 105, 106 };
            for (var f = 0; f < 5; f++)
            {
                var sb = new StringBuilder($"  floor {f + 1}: ");
                foreach (var kind in new[] { "normal", "elite", "preboss", "boss" })
                {
                    var hits = 0;
                    const int samples = 200;
                    for (var s = 0; s < samples; s++)
                    {
                        var run = StartRun(20000 + s);
                        for (var k = 0; k < f; k++) run.AdvanceToNextFloor();
                        var cfg = run.BuildLevelConfig(kind == "boss", kind == "elite", 3, kind == "preboss");
                        if (cfg.ActiveModifiers.Any(m => pressureIds.Contains((int)m))) hits++;
                    }
                    sb.Append($"{kind}={hits * 100 / samples}% ");
                }
                Probe(sb.ToString());
            }
            Assert.Pass("frequency probe only");
        }
    }
}
