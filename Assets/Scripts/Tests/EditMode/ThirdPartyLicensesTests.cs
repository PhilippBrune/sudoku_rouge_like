using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Tests
{
    // [REQ: LEGAL-LICENSE-001] The licence texts travel with the game (GAP-068): bundled for the Credits screen, identical to
    // their sources in docs/legal/third-party, and copied into a build's Licenses folder by the build step.
    [TestFixture]
    [Requirement("LEGAL-LICENSE-001")]
    public sealed class ThirdPartyLicensesTests
    {
        [Test]
        public void EveryLicenceIsBundled_AndMatchesItsSource()
        {
            foreach (var entry in ThirdPartyLicenses.All)
            {
                var bundled = ThirdPartyLicenses.Load(entry);
                Assert.IsNotEmpty(bundled, $"{entry.ResourcePath} is not in Resources");
                var source = File.ReadAllText(Path.Combine(ThirdPartyLicenses.SourceFolder, entry.SourceFile)).Replace("\r\n", "\n").TrimEnd();
                Assert.AreEqual(source, bundled, $"{entry.ResourcePath} differs from {entry.SourceFile}: copy the source again");
            }
        }

        [Test]
        public void TheLicencesThePlayerMustReceive_AreAllListed()
        {
            var texts = string.Join("\n", ThirdPartyLicenses.All.Select(ThirdPartyLicenses.Load));
            StringAssert.Contains("Apache License", texts, "Roboto's licence");
            StringAssert.Contains("MIT License", texts, "Steamworks.NET's licence");
            StringAssert.Contains("Roboto", texts, "the font's notice");
        }

        [Test]
        public void TheBuildStep_CopiesEveryLicenceIntoTheLicensesFolder()
        {
            var build = Path.Combine(Path.GetTempPath(), "sr_license_build_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(build);
                var copied = ThirdPartyLicenses.CopyToBuild(ThirdPartyLicenses.SourceFolder, build);

                Assert.AreEqual(ThirdPartyLicenses.All.Count, copied);
                foreach (var entry in ThirdPartyLicenses.All)
                    FileAssert.Exists(Path.Combine(build, ThirdPartyLicenses.BuildFolderName, entry.SourceFile));
            }
            finally
            {
                if (Directory.Exists(build)) Directory.Delete(build, true);
            }
        }

        [Test]
        public void TheCreditsScreen_ShowsTheLicenceTexts()
        {
            var source = File.ReadAllText("Assets/Scripts/UI/MainMenuBlueprintBuilder.cs");
            StringAssert.Contains("Credits.Section.Licenses", source);
            StringAssert.Contains("ThirdPartyLicenses.Load(license)", source);
            FileAssert.Exists("Assets/Scripts/Editor/LicenseFilesBuildStep.cs");
        }
    }
}
