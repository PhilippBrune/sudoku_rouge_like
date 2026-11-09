using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // RM-232 analysis (owner, 2026-11-08): how many rules a Custom Puzzle can carry. For 5 to 10 rules, random combinations
    // the Custom Puzzle screen would accept (minimum size, combinations, structural stack) on every size, generated as the
    // game does; prints the share of boards with every rule drawn and the time.
    [TestFixture]
    [Explicit("Diagnostic for RM-232; prints a table")]
    public sealed class CustomRuleCountDiagnostics
    {
        [Test]
        public void RuleCount_Sweep() => Sweep("RM-232", size => (5, size >= 8 ? 10 : 7));

        // [REQ: BOSS-MOD-010] One to four rules, the usual boards: pieces per rule, and that no rule loses its place to another.
        [Test]
        public void FewRules_Sweep() => Sweep("BOSS-MOD-010", size => (1, 4));

        private static void Sweep(string label, Func<int, (int from, int to)> range)
        {
            var lines = new List<string> { "size rules  boards  all-rules  missing/board  pieces/rule  mean ms  max ms" };
            foreach (var size in new[] { 9, 8, 7, 6, 5 })
            for (var count = range(size).from; count <= range(size).to; count++)
            {
                var pieces = 0;
                int boards = 0, complete = 0, missing = 0;
                long total = 0, max = 0;
                for (var trial = 0; trial < 12; trial++)
                {
                    var rng = new Random(count * 1000 + trial * 17 + size);
                    var mods = Combination(size, count, rng);
                    if (mods.Count < count) continue;
                    // The screen's run with one rule, then the board with all of them (StartTutorialRun refuses more than the limit).
                    var seed = 5000 + trial * 31;
                    var run = new RunDirector();
                    run.StartTutorialRun(new TutorialSetupConfig { BoardSize = size, Stars = 3, Seed = seed }, seed);
                    var config = new LevelConfig { BoardSize = size, Stars = 3, MissingPercent = StarDensityService.MissingPercentForStars(3),
                        RegionVariant = 0, IsBoss = true, Seed = seed, Difficulty = DifficultyTier.Diff1 };
                    config.ActiveModifiers.AddRange(mods);
                    var watch = Stopwatch.StartNew();
                    run.StartLevel(config);
                    watch.Stop();
                    if (run.CurrentBoard == null) continue;
                    boards++;
                    var miss = run.LastGenerationMissingModifiers.Count + mods.Count(m => !run.CurrentLevelConfig.ActiveModifiers.Contains(m));
                    missing += miss;
                    if (miss == 0) complete++;
                    var o = run.CurrentOverlay;
                    pieces += o.Lines.Count + o.KropkiDots.Count + o.KillerCages.Count + o.Arrows.Count + o.CellMarkers.Count;
                    total += watch.ElapsedMilliseconds;
                    max = Math.Max(max, watch.ElapsedMilliseconds);
                }
                lines.Add($"{size}x{size} {count,5}  {boards,6}  {complete,9}  {(boards > 0 ? missing / (double)boards : 0),13:0.00}  {(boards > 0 ? pieces / (double)boards / count : 0),11:0.0}  {(boards > 0 ? total / boards : 0),7}  {max,6}");
            }
            TestContext.WriteLine(string.Join("\n", lines));
            UnityEngine.Debug.Log($"[{label}]\n" + string.Join("\n", lines));
        }

        // A random set the Custom Puzzle screen accepts, ignoring only its count limit.
        private static List<BossModifierId> Combination(int size, int count, Random rng)
        {
            var pool = ModifierFamilies.Order.SelectMany(ModifierFamilies.Get).Distinct().ToList();
            var chosen = new List<BossModifierId>();
            for (var tries = 0; tries < 400 && chosen.Count < count; tries++)
            {
                var candidate = pool[rng.Next(pool.Count)];
                if (chosen.Contains(candidate) || !BossService.IsSupportedForGeneratedRun(candidate)) continue;
                if (size < BossService.GetMinimumBoardSize(candidate) || !BossService.CanCombineModifiers(chosen, candidate)) continue;
                if (size == 7 && BossService.IsBoardShapingModifier(candidate)) continue;
                var structural = chosen.Count(BossService.IsBoardShapingModifier) + (BossService.IsBoardShapingModifier(candidate) ? 1 : 0);
                if (structural >= 2 && size < 8) continue;
                chosen.Add(candidate);
            }
            return chosen;
        }
    }
}
