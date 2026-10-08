using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SudokuRoguelike.Tutorial;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // [REQ: TUTO-BASICS-STEP-001] [REQ: TUTO-BASICS-STEP-002] [REQ: TUTO-BASICS-STEP-006] SPEC-CONF-037: the Sudoku
    // Basics steps documented in FirstTimePlayTutorial v2.1 match the controller, and every interactive step asks for
    // the digit the fixed 6×6 board's solution has in its target cell.
    [TestFixture]
    [Requirement("TUTO-BASICS-STEP-001", "TUTO-BASICS-STEP-002", "TUTO-BASICS-STEP-006")]
    public sealed class SudokuBasicsStepsTests
    {
        [Test]
        public void Steps_MatchTheDocumentedLesson_AndTheBoardSolution()
        {
            var board = TutorialModeService.BuildBasicsTutorialBoard();
            var build = typeof(SudokuBasicsTutorialController).GetMethod("BuildSteps", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(build, "SudokuBasicsTutorialController.BuildSteps not found");
            var steps = ((IEnumerable)build.Invoke(null, new object[] { board })).Cast<object>().ToList();

            Assert.AreEqual(15, steps.Count, "FirstTimePlayTutorial v2.1 documents 15 steps (0–14)");

            var interactions = new List<string>();
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var input = Field<bool>(step, "RequiresInput");
                var pencil = Field<bool>(step, "RequiresPencilMarks");
                Assert.IsFalse(string.IsNullOrEmpty(Field<string>(step, "Title")), $"step {i} has no title");
                if (!input && !pencil) continue;

                int row = Field<int>(step, "TargetRow"), col = Field<int>(step, "TargetCol");
                Assert.IsFalse(board.IsGiven(row, col), $"step {i} targets a given cell");
                var solution = board.Solution[row, col];
                if (input)
                {
                    Assert.AreEqual(solution, Field<int>(step, "CorrectValue"), $"step {i} asks for a digit the solution does not have");
                    interactions.Add($"{i}:place {solution} at ({row},{col})");
                }
                else
                {
                    var marks = Field<HashSet<int>>(step, "RequiredMarkValues");
                    CollectionAssert.Contains(marks, solution, $"step {i} must include the solution among its pencil marks");
                    interactions.Add($"{i}:mark {string.Join("+", marks.OrderBy(v => v))} at ({row},{col})");
                }
            }

            CollectionAssert.AreEqual(
                new[] { "7:place 6 at (3,1)", "9:place 1 at (3,2)", "11:mark 2+6 at (0,1)", "13:place 2 at (0,1)" },
                interactions);
        }

        private static T Field<T>(object step, string name) =>
            (T)step.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(step);
    }
}
