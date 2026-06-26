using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class MainMenuBlueprintBuilderExtractionTests
    {
        private const string MainBuilderPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.cs";
        private const string ChallengePanelsPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.ChallengePanels.cs";

        [Test]
        public void ChallengePanels_AreOwnedByFocusedPartialBuilderFile()
        {
            var mainBuilder = File.ReadAllText(MainBuilderPath);
            var challengePanels = File.ReadAllText(ChallengePanelsPath);

            StringAssert.Contains(
                "public sealed partial class MainMenuBlueprintBuilder",
                mainBuilder,
                "Main menu builder must remain partial so focused panel slices can be owned in smaller files.");
            StringAssert.DoesNotContain(
                "private GameObject BuildDailyWalkPanel",
                mainBuilder,
                "Daily Walk panel should stay out of the main generated builder file.");
            StringAssert.DoesNotContain(
                "private GameObject BuildMonthlyWalkPanel",
                mainBuilder,
                "Monthly Walk panel should stay out of the main generated builder file.");
            StringAssert.Contains(
                "private GameObject BuildDailyWalkPanel",
                challengePanels);
            StringAssert.Contains(
                "private GameObject BuildMonthlyWalkPanel",
                challengePanels);
        }
    }
}
