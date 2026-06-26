using System;
using System.Threading;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class SudokuBacktrackingSolverTests
    {
        [Test]
        public void CountSolutions_AppliesConstraintEngineCandidateValidation()
        {
            var board = Ambiguous4x4Puzzle();
            var overlay = MarkerOverlay(0, 0, MarkerType.Odd);
            var engine = EngineWithEvenOddRule();

            Assert.AreEqual(2, SudokuBacktrackingSolver.CountSolutions(board, maxCount: 2));
            Assert.AreEqual(1, SudokuBacktrackingSolver.CountSolutions(board, maxCount: 2, overlay, engine));
            Assert.IsTrue(SudokuBacktrackingSolver.HasUniqueSolution(board, overlay, engine));

            Assert.IsTrue(SudokuBacktrackingSolver.TrySolve(board, out var solution, overlay, engine));
            Assert.AreEqual(1, solution[0, 0]);
            Assert.AreEqual(0, board.Cells[0, 0], "solver must not mutate the source board");
            Assert.AreEqual(0, board.Cells[2, 0], "solver must not mutate the source board");
        }

        [Test]
        public void TrySolve_ReturnsFalse_WhenExistingClueViolatesConstraintEngine()
        {
            var board = Solved4x4BoardWithEvenValueAtZeroZero();
            var overlay = MarkerOverlay(0, 0, MarkerType.Odd);
            var engine = EngineWithEvenOddRule();

            Assert.AreEqual(0, SudokuBacktrackingSolver.CountSolutions(board, maxCount: 2, overlay, engine));
            Assert.IsFalse(SudokuBacktrackingSolver.TrySolve(board, out var solution, overlay, engine));
            Assert.IsNull(solution);
            Assert.AreEqual(2, board.Cells[0, 0], "failed validation must not mutate the source board");
        }

        [Test]
        public void Generator_ThrowsPromptly_WhenDeadlineIsCancelled()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() =>
                    SudokuGenerator.CreatePuzzleWithUniquenessCheck(
                        9,
                        0.7f,
                        12345,
                        deadline: new GenerationDeadline(1000, cts.Token)));
            }
        }

        [Test]
        public void CountSolutions_AppliesLineConstraintPackageValidation()
        {
            var board = Ambiguous4x4Puzzle();
            var overlay = new ModifierOverlayData();
            overlay.Lines.Add(new ModifierLine
            {
                Type = LineType.Thermo,
                Cells =
                {
                    new CellCoord(0, 0),
                    new CellCoord(0, 1)
                }
            });
            var engine = EngineWithRules(new ThermoRule());

            AssertSolvesAsUniquePackage(board, overlay, engine, expectedZeroZero: 1);
        }

        [Test]
        public void CountSolutions_AppliesKropkiDotPackageValidation()
        {
            var board = Ambiguous4x4Puzzle();
            var overlay = new ModifierOverlayData();
            overlay.KropkiDots.Add(new KropkiDot
            {
                CellA = new CellCoord(0, 1),
                CellB = new CellCoord(1, 1),
                IsBlack = true
            });
            var engine = EngineWithRules(new RatioKropkiRule());

            AssertSolvesAsUniquePackage(board, overlay, engine, expectedZeroZero: 1);
        }

        [Test]
        public void CountSolutions_AppliesAdjacentPairConstraintPackageValidation()
        {
            var board = Ambiguous4x4Puzzle();
            var overlay = new ModifierOverlayData();
            overlay.PairConstraints.Add(new AdjacentPairConstraint
            {
                CellA = new CellCoord(0, 1),
                CellB = new CellCoord(0, 0),
                Type = PairConstraintType.GreaterThan
            });
            var engine = EngineWithRules(new GreaterLessThanRule());

            AssertSolvesAsUniquePackage(board, overlay, engine, expectedZeroZero: 1);
        }

        [Test]
        public void CountSolutions_AppliesKillerCagePackageValidation()
        {
            var board = Ambiguous4x4Puzzle();
            var overlay = new ModifierOverlayData();
            overlay.KillerCages.Add(new KillerCage
            {
                Sum = 4,
                Cells =
                {
                    new CellCoord(0, 0),
                    new CellCoord(1, 0)
                }
            });
            var engine = EngineWithRules(new KillerCagesRule());

            AssertSolvesAsUniquePackage(board, overlay, engine, expectedZeroZero: 1);
        }

        private static SudokuBoard Ambiguous4x4Puzzle()
        {
            var solution = new[,]
            {
                { 1, 2, 3, 4 },
                { 3, 4, 1, 2 },
                { 2, 1, 4, 3 },
                { 4, 3, 2, 1 }
            };

            var cells = new[,]
            {
                { 0, 0, 3, 4 },
                { 3, 4, 1, 2 },
                { 0, 0, 4, 3 },
                { 4, 3, 2, 1 }
            };

            return new SudokuBoard(4, solution, cells, RegionMap4x4());
        }

        private static SudokuBoard Solved4x4BoardWithEvenValueAtZeroZero()
        {
            var cells = new[,]
            {
                { 2, 1, 3, 4 },
                { 3, 4, 1, 2 },
                { 1, 2, 4, 3 },
                { 4, 3, 2, 1 }
            };

            return new SudokuBoard(4, (int[,])cells.Clone(), cells, RegionMap4x4());
        }

        private static int[,] RegionMap4x4()
        {
            var map = new int[4, 4];
            for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                map[r, c] = (r / 2) * 2 + (c / 2);
            return map;
        }

        private static ModifierOverlayData MarkerOverlay(int row, int col, MarkerType markerType)
        {
            var overlay = new ModifierOverlayData();
            overlay.CellMarkers.Add(new CellMarker
            {
                Cell = new CellCoord(row, col),
                Type = markerType
            });
            return overlay;
        }

        private static SudokuConstraintEngine EngineWithEvenOddRule()
        {
            var engine = new SudokuConstraintEngine();
            engine.RegisterRule(new EvenOddRule());
            return engine;
        }

        private static SudokuConstraintEngine EngineWithRules(params IOrderedConstraintRule[] rules)
        {
            var engine = new SudokuConstraintEngine();
            for (var i = 0; i < rules.Length; i++)
                engine.RegisterRule(rules[i]);
            return engine;
        }

        private static void AssertSolvesAsUniquePackage(
            SudokuBoard board,
            ModifierOverlayData overlay,
            SudokuConstraintEngine engine,
            int expectedZeroZero)
        {
            Assert.AreEqual(2, SudokuBacktrackingSolver.CountSolutions(board, maxCount: 2));
            Assert.AreEqual(1, SudokuBacktrackingSolver.CountSolutions(board, maxCount: 2, overlay, engine));
            Assert.IsTrue(SudokuBacktrackingSolver.HasUniqueSolution(board, overlay, engine));
            Assert.IsTrue(SudokuBacktrackingSolver.TrySolve(board, out var solution, overlay, engine));
            Assert.AreEqual(expectedZeroZero, solution[0, 0]);
            Assert.AreEqual(0, board.Cells[0, 0], "solver must not mutate the source board");
            Assert.AreEqual(0, board.Cells[2, 0], "solver must not mutate the source board");
        }
    }
}
