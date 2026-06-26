using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class CreditsLegalContentTests
    {
        private const string CreditsDocPath = "docs/Credits.md";
        private const string MainMenuBuilderPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.cs";

        [Test]
        public void CreditsMarkdownAndInGamePanel_SourceIncludeGoogleResolverNotice()
        {
            AssertGoogleResolverNotice(File.ReadAllText(CreditsDocPath), CreditsDocPath);
            AssertGoogleResolverNotice(File.ReadAllText(MainMenuBuilderPath), MainMenuBuilderPath);
        }

        [Test]
        public void CreditsMarkdownAndInGamePanel_SourceIncludeFinalAssetAndThanksWording()
        {
            AssertFinalAssetAndThanksWording(File.ReadAllText(CreditsDocPath), CreditsDocPath);
            AssertFinalAssetAndThanksWording(File.ReadAllText(MainMenuBuilderPath), MainMenuBuilderPath);
        }

        [Test]
        public void CreditsMarkdownAndInGamePanel_SourceDoNotClaimBundledSteamworksSdk()
        {
            AssertSteamworksDisclaimer(File.ReadAllText(CreditsDocPath), CreditsDocPath, allowTrademarkNotice: true);
            AssertSteamworksDisclaimer(File.ReadAllText(MainMenuBuilderPath), MainMenuBuilderPath, allowTrademarkNotice: true);
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

        private static void AssertSteamworksDisclaimer(string source, string path, bool allowTrademarkNotice)
        {
            Assert.That(source, Does.Contain("Local achievement tracking is implemented"), path);
            Assert.That(source, Does.Contain("Steamworks-compatible"), path);
            Assert.That(source, Does.Contain("supported Steamworks SDK assembly is imported"), path);
            Assert.That(source, Does.Contain("No Steamworks SDK binaries are bundled in this repository"), path);
            Assert.That(source, Does.Contain("before claiming Steam release support"), path);

            if (!allowTrademarkNotice)
                Assert.That(source, Does.Not.Contain("Steam\u00ae is a registered trademark"), path);

            Assert.That(source, Does.Not.Contain("Steamworks SDK \u00a9 Valve Corporation"), path);
        }
    }
}
