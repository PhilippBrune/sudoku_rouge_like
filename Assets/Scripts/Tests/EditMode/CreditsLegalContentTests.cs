using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class CreditsLegalContentTests
    {
        private const string CreditsDocPath = "docs/Credits.md";
        private const string MainMenuBuilderPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.cs";
        private const string InGameCreditsSource = "Assets/Resources/Localization/en.json (Credits.Body.*)";

        [Test]
        public void CreditsMarkdownAndInGamePanel_SourceIncludeGoogleResolverNotice()
        {
            AssertGoogleResolverNotice(DocText.Read(CreditsDocPath), CreditsDocPath);
            // The in-game credits panel shows the Credits.Body.* catalog entries (moved out of the builder in 9f19bba).
            AssertGoogleResolverNotice(DocText.EnglishCatalog(), InGameCreditsSource);
        }

        [Test]
        public void CreditsMarkdownAndInGamePanel_SourceIncludeFinalAssetAndThanksWording()
        {
            AssertFinalAssetAndThanksWording(DocText.Read(CreditsDocPath), CreditsDocPath);
            AssertFinalAssetAndThanksWording(DocText.EnglishCatalog(), InGameCreditsSource);
        }

        // SPEC-PROC-007 (decided 2026-10-07): Steamworks.NET and the Steam API redistributables ship with the game, so the
        // credits name them with their licences instead of claiming that no Steamworks binaries are bundled.
        [Test]
        public void CreditsMarkdownAndInGamePanel_NameSteamworksNetAndValveRedistributables()
        {
            AssertSteamworksNotice(DocText.Read(CreditsDocPath), CreditsDocPath);
            AssertSteamworksNotice(DocText.EnglishCatalog(), InGameCreditsSource);
            Assert.IsTrue(File.Exists("docs/legal/third-party/Steamworks.NET-LICENSE.md"), "Steamworks.NET licence copy is missing");
            StringAssert.Contains("MIT License", File.ReadAllText("docs/legal/third-party/Steamworks.NET-LICENSE.md"));
        }

        private static void AssertGoogleResolverNotice(string source, string path)
        {
            Assert.That(source, Does.Contain("Google Mobile Dependency Resolver for Unity"), path);
            Assert.That(source, Does.Contain("version 1.2.185"), path);
            Assert.That(source, Does.Contain("Assets/MobileDependencyResolver/Editor"), path);
            Assert.That(source, Does.Contain("Apache License 2.0"), path);
            Assert.That(source, Does.Contain("Assets/MobileDependencyResolver/Editor/LICENSE"), path);
        }

        private static void AssertFinalAssetAndThanksWording(string source, string path)
        {
            Assert.That(source, Does.Contain("Runtime asset provenance is tracked"), path);
            Assert.That(source, Does.Contain("docs/legal/asset-provenance-register.md"), path);
            Assert.That(source, Does.Contain("Authored candidate music, SFX, and UI audio generated through"), path);
            Assert.That(source, Does.Contain("project-owned procedural synthesis"), path);
            Assert.That(source, Does.Contain("Runtime procedural fallback remains"), path);
            Assert.That(source, Does.Contain("Roboto Regular by Google"), path);
            Assert.That(source, Does.Contain("Assets/Resources/Fonts/MainFont.ttf"), path);
            Assert.That(source, Does.Contain("docs/legal/third-party/Roboto-NOTICE.md"), path);
            Assert.That(source, Does.Contain("docs/legal/third-party/APACHE-2.0.txt"), path);
            Assert.That(source, Does.Contain("No third-party sprite or stock audio asset files are currently declared"), path);
            Assert.That(source, Does.Contain("No public special-thanks names are listed for this build"), path);
            Assert.That(source, Does.Contain("Asset Licenses"), path);
            Assert.That(source, Does.Contain("No separate third-party stock"), path);
        }

        private static void AssertSteamworksNotice(string source, string path)
        {
            Assert.That(source, Does.Contain("Steamworks.NET by Riley Labrecque"), path);
            Assert.That(source, Does.Contain("MIT License"), path);
            Assert.That(source, Does.Contain("docs/legal/third-party/Steamworks.NET-LICENSE.md"), path);
            Assert.That(source, Does.Contain("Steamworks API redistributables"), path);
            Assert.That(source, Does.Contain("Valve Corporation"), path);
            Assert.That(source, Does.Contain("Steamworks SDK Access Agreement"), path);
            Assert.That(source, Does.Not.Contain("No Steamworks SDK binaries are bundled in this repository"), path);
        }
    }
}
