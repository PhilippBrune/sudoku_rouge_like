using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: GEN-VALID-001] [REQ: GEN-VALID-002] [REQ: REQ-SUD-103] [REQ: REQ-SAVE-101]
    // SPEC-CONF-003 (decided 2026-10-06): a digit is wrong when it differs from the stored solution, even
    // without a visible conflict. Wrong digits are rejected and their penalty is applied by RunDirector
    // (GAP-011, GAP-035). Custom Puzzle 7★ boards have no unique solution and stay rule-based.
    [TestFixture]
    [Requirement("GEN-VALID-001", "GEN-VALID-002", "REQ-SUD-103")]
    public sealed class PlacementRulesTests
    {
        // SPEC-CONF-016 (1.1): Undo was removed, as GDS v1.4 decided; no input action or binding is left.
        [Test]
        [Requirement("REQ-SUD-105")]
        public void Undo_IsNotAnInputAction()
        {
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(InputAction)), "UndoLastDigit");
            Assert.IsFalse(System.Enum.GetNames(typeof(InputAction)).Any(n => n.Contains("Undo")));
        }

        [Test]
        public void NonSolutionDigitWithoutConflict_IsRejectedAndCostsOneHp()
        {
            var run = StartPuzzle(7301);
            var (row, col, value) = FindRuleValidWrongDigit(run);
            run.State.ComboStreak = 3;
            var hp = run.State.CurrentHP;

            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, value));

            Assert.AreEqual(0, run.CurrentBoard.Cells[row, col], "a wrong digit must not stay on the board");
            Assert.AreEqual(hp - 1, run.State.CurrentHP);
            Assert.AreEqual(0, run.State.ComboStreak);
            Assert.AreEqual(1, run.CurrentLevelState.Mistakes);
            Assert.IsFalse(run.CurrentLevelState.PerfectSoFar);
        }

        [Test]
        public void SolutionDigit_IsCorrect_AndReenteringItChangesNothing()
        {
            var run = StartPuzzle(7302);
            var (row, col) = FirstEmptyCell(run);
            var solution = run.CurrentBoard.Solution[row, col];

            Assert.AreEqual(PlaceResult.Correct, run.PlaceNumber(row, col, solution));
            var combo = run.State.ComboStreak;
            var placements = run.CurrentLevelState.CorrectPlacements;

            Assert.AreEqual(PlaceResult.Unchanged, run.PlaceNumber(row, col, solution));
            Assert.AreEqual(combo, run.State.ComboStreak, "re-entering a digit must not advance the combo");
            Assert.AreEqual(placements, run.CurrentLevelState.CorrectPlacements);
        }

        [Test]
        public void RepeatingAWrongDigit_IsANewMistake()
        {
            var run = StartPuzzle(7303);
            var (row, col, value) = FindRuleValidWrongDigit(run);
            var hp = run.State.CurrentHP;

            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, value));
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, value),
                "the repeated wrong digit must not be reported as correct (D15)");
            Assert.AreEqual(hp - 2, run.State.CurrentHP);
            Assert.AreEqual(2, run.CurrentLevelState.Mistakes);
        }

        [Test]
        public void WrongDigit_OverACorrectDigit_KeepsTheCorrectDigit()
        {
            var run = StartPuzzle(7304);
            var (row, col) = FirstEmptyCell(run);
            var solution = run.CurrentBoard.Solution[row, col];
            Assert.AreEqual(PlaceResult.Correct, run.PlaceNumber(row, col, solution));

            var wrong = solution % run.CurrentBoard.Size + 1;
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, wrong));
            Assert.AreEqual(solution, run.CurrentBoard.Cells[row, col]);
        }

        [Test]
        public void UmbrellaCharge_AbsorbsTheHpLoss()
        {
            var run = StartPuzzle(7305);
            var (row, col, value) = FindRuleValidWrongDigit(run);
            run.State.MistakeShieldCharges = 1;
            var hp = run.State.CurrentHP;

            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, value));
            Assert.AreEqual(hp, run.State.CurrentHP);
            Assert.AreEqual(0, run.State.MistakeShieldCharges);
            Assert.AreEqual(1, run.CurrentLevelState.Mistakes, "an absorbed mistake still counts as a mistake");
        }

        [Test]
        public void CustomPuzzleSevenStar_AcceptsAnyRuleValidDigit()
        {
            var run = new RunDirector();
            run.StartTutorialRun(new TutorialSetupConfig
            {
                BoardSize = 7,
                Stars = 7,
                RegionVariant = 0,
                SelectedModifiers = new List<BossModifierId> { BossModifierId.KillerCages }
            }, 7306);
            Assert.IsNotNull(run.CurrentBoard, "7★ custom puzzle did not generate");
            Assert.IsFalse(run.UsesSolutionValidation);

            var (row, col, value) = FindRuleValidWrongDigit(run);
            Assert.AreEqual(PlaceResult.Correct, run.PlaceNumber(row, col, value),
                "a 7★ board has many solutions, so any digit that breaks no rule is accepted");
        }

        [Test]
        public void MistakeFromAnEarlierPuzzle_CannotBeUndoneByGinkgoLeaf()
        {
            var run = StartPuzzle(7307);
            var (row, col, value) = FindRuleValidWrongDigit(run);
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, value));
            Assert.AreEqual(row, run.State.LastMistakeRow);

            var next = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i])
                .FirstOrDefault(n => n.Type == NodeType.Puzzle);
            Assume.That(next, Is.Not.Null, "no further puzzle node reachable for this seed");
            run.TryAdvanceToNode(next.Index);
            run.StartLevel(run.BuildLevelConfig(false, false, next.Index));

            Assert.AreEqual(-1, run.State.LastMistakeRow);
            Assert.AreEqual(0, run.State.LastMistakeHpLost);
        }

        // ── helpers ──

        private static RunDirector StartPuzzle(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = false,
                ClassLevel = 1
            }, seed);
            // Starting relics can absorb mistakes; isolate the placement rules from them.
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i])
                .First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            run.StartLevel(run.BuildLevelConfig(false, false, node.Index));
            Assert.IsNotNull(run.CurrentBoard, "puzzle generation failed for the test seed");
            Assert.IsTrue(run.UsesSolutionValidation);
            return run;
        }

        private static (int row, int col) FirstEmptyCell(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0 && !board.IsGiven(r, c))
                    return (r, c);
            Assert.Fail("no empty cell");
            return (-1, -1);
        }

        // A digit that differs from the solution but conflicts with nothing on the board.
        private static (int row, int col, int value) FindRuleValidWrongDigit(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                if (board.Cells[r, c] != 0 || board.IsGiven(r, c)) continue;
                for (var v = 1; v <= board.Size; v++)
                {
                    if (v == board.Solution[r, c]) continue;
                    if (run.ConstraintEngine.ValidateAll(board, r, c, v, run.CurrentOverlay))
                        return (r, c, v);
                }
            }
            Assert.Inconclusive("no rule-valid wrong digit on this board");
            return (-1, -1, -1);
        }
    }
}
