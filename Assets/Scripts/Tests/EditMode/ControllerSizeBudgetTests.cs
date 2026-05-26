using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class ControllerSizeBudgetTests
    {
        private const string ExtractionPlanPath = "docs/controller-extraction-plan.md";

        [TestCase("Assets/Scripts/UI/InRunController.cs", 5100)]
        [TestCase("Assets/Scripts/UI/MainMenuBlueprintBuilder.cs", 3340)]
        [TestCase("Assets/Scripts/Run/RunDirector.cs", 3350)]
        public void LargeControllers_DoNotGrowBeyondCurrentRatchetedBudget(string path, int maxLines)
        {
            var lineCount = File.ReadAllLines(path).Length;
            Assert.LessOrEqual(
                lineCount,
                maxLines,
                $"{path} grew beyond its ratcheted line budget. Extract a focused presenter/service or update docs/controller-extraction-plan.md with a reviewed budget change.");
        }

        [Test]
        public void ControllerExtractionPlan_TracksGodObjectReduction()
        {
            var plan = File.ReadAllText(ExtractionPlanPath);

            Assert.That(plan, Does.Contain("CBH-002"));
            Assert.That(plan, Does.Contain("InRunController"));
            Assert.That(plan, Does.Contain("MainMenuBlueprintBuilder"));
            Assert.That(plan, Does.Contain("RunDirector"));
            Assert.That(plan, Does.Contain("ratchet"));
            Assert.That(plan, Does.Contain("PlayMode"));
            Assert.That(plan, Does.Contain("lower the corresponding budget"));
        }
    }
}
