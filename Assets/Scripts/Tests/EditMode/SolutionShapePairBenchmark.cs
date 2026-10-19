using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: GEN-SOLVE-002] Pair harness for the solution-shaping rules (modifier-catalog-plan.md, Phase 0 step 7): every
    // B5 rule with every older rule the generator builds the solution with, on 9×9 boards, seeds 1–4, with the run's
    // budget per board. Prints a table; a pair that fails or is slow becomes a blocked pair. On demand only.
    [TestFixture]
    [Explicit("Benchmark: run on demand when the solution-shaping rules change.")]
    public sealed class SolutionShapePairBenchmark
    {
        private const int BudgetMs = 6000;   // PuzzleGenerationSettings.StructuralModifierDefault

        [Test]
        public void EveryB5RuleWithEveryOlderShapingRule()
        {
            var b5 = new[] { BossModifierId.XSudoku, BossModifierId.HyperSudoku, BossModifierId.DisjointGroups, BossModifierId.MagicSquareRegion };
            var older = new[]
            {
                BossModifierId.Nonconsecutive, BossModifierId.Antiknight, BossModifierId.NonconsecDiagonal, BossModifierId.AntiBishop,
                BossModifierId.Antiking, BossModifierId.DistanceGe2, BossModifierId.EntropyGlobal
            };
            var partners = new List<BossModifierId>(older);
            partners.AddRange(b5);
            var report = new List<string>();
            var failures = new List<string>();
            var runs = new List<(int Size, List<BossModifierId> Mods)>();
            foreach (var other in older) runs.Add((9, new List<BossModifierId> { other }));     // baseline: the older rule alone
            // X-Sudoku alone on the smaller boards. The irregular 7×7 has no X solution within budget, but a run raises a 7×7
            // board with a board-shaping rule to 8×8 (RunDirector.EnsureBoardSizeSupportsActiveModifiers).
            for (var size = 5; size <= 8; size++) runs.Add((size, new List<BossModifierId> { BossModifierId.XSudoku }));
            for (var i = 0; i < b5.Length; i++)
            foreach (var other in partners)
            {
                if (other == b5[i] || (Array.IndexOf(b5, other) >= 0 && Array.IndexOf(b5, other) < i)) continue;
                if (!BossService.CanCombineModifiers(new List<BossModifierId> { b5[i] }, other)) continue;   // blocked pairs never roll
                runs.Add((9, new List<BossModifierId> { b5[i], other }));
                if (b5[i] == BossModifierId.XSudoku && Array.IndexOf(older, other) >= 0)
                    runs.Add((8, new List<BossModifierId> { b5[i], other }));                    // two shaping rules: 8×8 and up
            }
            // A few triples of two B5 rules and an older one (two shaping rules or more: 8×8 and up, so 9×9 here).
            foreach (var triple in new[]
                     {
                         new[] { BossModifierId.XSudoku, BossModifierId.HyperSudoku, BossModifierId.Antiking },
                         new[] { BossModifierId.XSudoku, BossModifierId.DisjointGroups, BossModifierId.AntiBishop },
                         new[] { BossModifierId.HyperSudoku, BossModifierId.DisjointGroups, BossModifierId.Antiknight },
                         new[] { BossModifierId.XSudoku, BossModifierId.HyperSudoku, BossModifierId.MagicSquareRegion },
                         new[] { BossModifierId.HyperSudoku, BossModifierId.MagicSquareRegion, BossModifierId.Antiknight },
                         new[] { BossModifierId.MagicSquareRegion, BossModifierId.EntropyGlobal, BossModifierId.Antiking },
                     })
            {
                var drawn = new List<BossModifierId>();
                foreach (var mod in triple)
                    if (BossService.CanCombineModifiers(drawn, mod)) drawn.Add(mod);
                if (drawn.Count == triple.Length) runs.Add((9, drawn));   // a triple with a blocked pair never rolls
            }
            foreach (var (size, mods) in runs)
            {
                long worst = 0;
                var failed = 0;
                for (var seed = 1; seed <= 4; seed++)
                {
                    var watch = Stopwatch.StartNew();
                    try
                    {
                        SudokuGenerator.CreatePuzzleWithUniquenessCheck(size, 0.55f, seed * 101, regionVariant: 0,
                            nonconsecutive: mods.Contains(BossModifierId.Nonconsecutive),
                            antiknight: mods.Contains(BossModifierId.Antiknight),
                            nonconsecDiagonal: mods.Contains(BossModifierId.NonconsecDiagonal),
                            antibishop: mods.Contains(BossModifierId.AntiBishop),
                            antiking: mods.Contains(BossModifierId.Antiking),
                            distanceGe2: mods.Contains(BossModifierId.DistanceGe2),
                            entropy: mods.Contains(BossModifierId.EntropyGlobal),
                            deadline: new GenerationDeadline(BudgetMs),
                            shape: SolutionShape.For(mods, size));
                    }
                    catch (Exception e) when (e is TimeoutException || e is InvalidOperationException)
                    {
                        failed++;
                    }
                    worst = Math.Max(worst, watch.ElapsedMilliseconds);
                }
                var line = $"{size}×{size} {string.Join(" + ", mods)}: worst {worst} ms, {failed}/4 failed";
                report.Add(line);
                if (mods.Count > 1 && (failed > 0 || worst > BudgetMs / 2)) failures.Add(line);
            }
            TestContext.WriteLine(string.Join("\n", report));
            Assert.IsEmpty(failures, "pairs to block:\n" + string.Join("\n", failures));
        }
    }
}
