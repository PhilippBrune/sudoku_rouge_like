using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Platform;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class LegalReleaseReadinessTests
    {
        private const string CreditsDocPath = "docs/Credits.md";
        private const string ThirdPartyNoticesPath = "docs/legal/third-party-notices.md";
        private const string AssetProvenancePath = "docs/legal/asset-provenance-register.md";
        private const string PrivacyAnalyticsNoticePath = "docs/legal/privacy-analytics-notice.md";
        private const string PlatformChecklistPath = "docs/release/platform-readiness-checklist.md";
        private const string MainMenuBuilderPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.cs";
        private const string PackageManifestPath = "Packages/manifest.json";

        [TearDown]
        public void TearDown()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            LocalizationService.ResetDiagnostics();
            PlatformServiceRegistry.ResetForTests();
            SteamAchievementService.ResetPlatformBridgeForTests();
        }

        [Test]
        public void LegalReleaseGate_DocumentsRequiredCreditsAndThirdPartyNotices()
        {
            var credits = File.ReadAllText(CreditsDocPath);
            var notices = File.ReadAllText(ThirdPartyNoticesPath);
            var provenance = File.ReadAllText(AssetProvenancePath);
            var privacyNotice = File.ReadAllText(PrivacyAnalyticsNoticePath);
            var inGameCreditsSource = File.ReadAllText(MainMenuBuilderPath);
            var inGameEnglishCredits = BuildInGameCreditsText(LanguageOption.English);
            var inGameGermanCredits = BuildInGameCreditsText(LanguageOption.German);

            Assert.That(credits, Does.Contain("Google Mobile Dependency Resolver for Unity"));
            Assert.That(credits, Does.Contain("No Steamworks SDK binaries are bundled"));
            Assert.That(credits, Does.Contain("No third-party sprite or stock audio asset files are currently declared"));
            Assert.That(credits, Does.Contain("Roboto Regular by Google"));
            Assert.That(credits, Does.Contain("Privacy & Analytics Notice"));
            Assert.That(credits, Does.Contain("docs/legal/privacy-analytics-notice.md"));
            Assert.That(credits, Does.Contain("No platform telemetry or Unity Analytics package is enabled"));
            Assert.That(credits, Does.Not.Contain("Unity Analytics packages are present"));

            Assert.That(notices, Does.Contain("Google Mobile Dependency Resolver For Unity"));
            Assert.That(notices, Does.Contain("No Steamworks SDK binaries are currently bundled"));
            Assert.That(notices, Does.Contain("Roboto Regular"));

            Assert.That(provenance, Does.Contain("Assets/Resources/Fonts/"));
            Assert.That(provenance, Does.Contain("Runtime audio WAV files"));
            Assert.That(privacyNotice, Does.Contain("does not include Unity Analytics"));
            Assert.That(privacyNotice, Does.Contain("analytics-free"));

            Assert.That(inGameCreditsSource, Does.Contain("Credits.Body.GoogleResolver"));
            Assert.That(inGameCreditsSource, Does.Contain("Credits.Body.PrivacyAnalyticsNotice"));
            Assert.That(inGameCreditsSource, Does.Contain("Credits.Body.AssetLicenses"));
            Assert.That(inGameEnglishCredits, Does.Contain("Google Mobile Dependency Resolver for Unity"));
            Assert.That(inGameEnglishCredits, Does.Contain("No Steamworks SDK binaries are bundled"));
            Assert.That(inGameEnglishCredits, Does.Contain("No third-party sprite or stock audio asset files are currently declared"));
            Assert.That(inGameEnglishCredits, Does.Contain("Roboto Regular by Google"));
            Assert.That(inGameEnglishCredits, Does.Contain("Privacy & Analytics Notice"));
            Assert.That(inGameEnglishCredits, Does.Contain("No platform telemetry or Unity Analytics package is enabled"));
            Assert.That(inGameEnglishCredits, Does.Contain("docs/legal/privacy-analytics-notice.md"));
            Assert.That(inGameGermanCredits, Does.Contain("Datenschutzhinweis"));
            Assert.That(inGameGermanCredits, Does.Contain("kein Plattform-Telemetriepaket"));
            Assert.That(inGameGermanCredits, Does.Contain("Roboto Regular"));
            Assert.That(inGameGermanCredits, Does.Contain("docs/legal/privacy-analytics-notice.md"));
        }

        [Test]
        public void LegalReleaseGate_DocumentsAnalyticsFreeReleaseBaseline()
        {
            var manifest = File.ReadAllText(PackageManifestPath);
            var checklist = File.ReadAllText(PlatformChecklistPath);

            Assert.That(manifest, Does.Not.Contain("\"com.unity.analytics\""));
            Assert.That(manifest, Does.Not.Contain("\"com.unity.services.analytics\""));
            Assert.That(manifest, Does.Not.Contain("\"com.unity.modules.unityanalytics\""));

            Assert.That(checklist, Does.Contain("Status: **not platform-release ready**"));
            Assert.That(checklist, Does.Contain("Unity Analytics packages are not present"));
            Assert.That(checklist, Does.Contain("privacy/analytics as"));
            Assert.That(checklist, Does.Not.Contain("Privacy/analytics release policy is not finalized"));
        }

        [Test]
        public void DefaultPlatformReadiness_HasNoPrivacyAnalyticsReleaseBlocker()
        {
            var status = PlatformServiceRegistry.CurrentStatus;

            Assert.AreEqual(PlatformCapabilityState.Available, status.PrivacyNotice.State);
            Assert.AreEqual("None", status.PrivacyNotice.ProviderName);
            Assert.That(status.PrivacyNotice.Detail, Does.Contain("No platform telemetry package"));
            CollectionAssert.DoesNotContain(status.ReleaseBlockers, "Privacy/analytics notice is not finalized.");
            Assert.IsTrue(status.HasReleaseBlockers);
        }

        private static string BuildInGameCreditsText(LanguageOption language)
        {
            LocalizationService.SetLanguage(language);
            return string.Join("\n", LocalizationService.RequiredCreditKeys.Select(key => LocalizationService.T(key)));
        }
    }
}
