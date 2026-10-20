using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using UnityEngine.TestTools;

namespace SudokuRoguelike.Tests
{
    // [REQ: GEN-SOLVE-002] [REQ: REQ-BOSS-101] GAP-051, the real flow: the boss board is baked at floor entry with the floor
    // modifiers, then the player picks a solution-shaping rule at the gate. The boss puzzle must not reuse the baked board,
    // because its solution was not built with that rule.
    [TestFixture]
    [Requirement("GEN-SOLVE-002", "REQ-BOSS-101")]
    public sealed class PreBakedBossBoardTests
    {
        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;   // generation logs pre-bake hits and misses

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static (RunDirector Run, LevelConfig Config) BossOnNineByNine(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false }, seed);
            var config = run.BuildLevelConfig(true);
            config.BoardSize = 9;
            config.RegionVariant = 0;
            config.ActiveModifiers.Clear();
            return (run, config);
        }

        [Test]
        public void EntropyPickedAtTheGate_GetsABoardBuiltWithEntropy()
        {
            foreach (var seed in new[] { 41, 42, 43 })
            {
                var (run, baked) = BossOnNineByNine(seed);
                run.BakeBossBoard(baked.Clone());
                var picked = baked.Clone();
                picked.ActiveModifiers.Add(BossModifierId.EntropyGlobal);
                run.StartLevel(picked);

                var solution = run.CurrentBoard.Solution;
                var solved = new SudokuBoard(9, solution, (int[,])solution.Clone(), run.CurrentBoard.RegionMap);
                var rule = new EntropyGlobalRule();
                for (var r = 0; r < 9; r++)
                for (var c = 0; c < 9; c++)
                    Assert.IsTrue(rule.IsValid(solved, r, c, solution[r, c], run.CurrentOverlay),
                        $"seed {seed}: the boss board's solution breaks Entropy at ({r},{c})");
            }
        }

        [Test]
        public void XSudokuPickedAtTheGate_GetsABoardBuiltWithTheDiagonals()
        {
            foreach (var seed in new[] { 51, 52 })
            {
                var (run, baked) = BossOnNineByNine(seed);
                run.BakeBossBoard(baked.Clone());
                var picked = baked.Clone();
                picked.ActiveModifiers.Add(BossModifierId.XSudoku);
                run.StartLevel(picked);

                Assert.IsTrue(run.CurrentLevelConfig.ActiveModifiers.Contains(BossModifierId.XSudoku), $"seed {seed}: X-Sudoku kept");
                Assert.IsTrue(run.CurrentOverlay.IsXSudoku, $"seed {seed}: the solution keeps both diagonals");
                var solution = run.CurrentBoard.Solution;
                Assert.AreEqual(9, Enumerable.Range(0, 9).Select(i => solution[i, i]).Distinct().Count(), $"seed {seed}: main diagonal");
            }
        }
    }
}
