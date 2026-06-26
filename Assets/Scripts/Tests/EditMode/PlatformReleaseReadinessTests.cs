using System;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Platform;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class PlatformReleaseReadinessTests
    {
        private const string PlatformChecklistPath = "docs/release/platform-readiness-checklist.md";
        private const string PlatformManifestPath = "docs/release/platform-release-manifest.md";
        private const string FutureScopeDecisionRegisterPath = "docs/future-scope-decision-register.md";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string PackageManifestPath = "Packages/manifest.json";

        [TearDown]
        public void TearDown()
        {
            PlatformServiceRegistry.ResetForTests();
        }

        [Test]
        public void PlatformReleaseGate_DocumentsCurrentRegistryBlockers()
        {
            var checklist = File.ReadAllText(PlatformChecklistPath);
            var manifest = File.ReadAllText(PlatformManifestPath);
            var status = PlatformServiceRegistry.CurrentStatus;

            Assert.IsTrue(status.HasReleaseBlockers);
            CollectionAssert.Contains(status.ReleaseBlockers, "Platform achievements are not release-verified.");
            CollectionAssert.Contains(status.ReleaseBlockers, "Cloud save is not configured; local saves only.");
            CollectionAssert.DoesNotContain(status.ReleaseBlockers, "Privacy/analytics notice is not finalized.");

            Assert.That(checklist, Does.Contain("Platform achievements are not release-verified."));
            Assert.That(checklist, Does.Contain("Cloud save is not configured; local saves only."));
            Assert.That(checklist, Does.Contain("Unity Analytics packages are not present."));
            Assert.That(manifest, Does.Contain("PRG-ACHIEVEMENTS"));
            Assert.That(manifest, Does.Contain("Platform achievements are not release-verified."));
            Assert.That(manifest, Does.Contain("PRG-CLOUD-SAVE"));
            Assert.That(manifest, Does.Contain("Save system is local-only."));
            Assert.That(manifest, Does.Contain("PRG-PRIVACY"));
            Assert.That(manifest, Does.Contain("No platform telemetry package is configured"));
            Assert.That(manifest, Does.Contain("READY"));
        }

        [Test]
        public void PlatformReleaseGate_DetectsDevelopmentPlayerMetadataAndMissingIcons()
        {
            var projectSettings = File.ReadAllText(ProjectSettingsPath);
            var checklist = File.ReadAllText(PlatformChecklistPath);

            Assert.That(projectSettings, Does.Contain("companyName: DefaultCompany"));
            Assert.That(projectSettings, Does.Contain("productName: sudoku_rouge_like"));
            Assert.That(projectSettings, Does.Contain("applicationIdentifier: {}"));
            Assert.That(projectSettings, Does.Contain("m_BuildTargetIcons: []"));
            Assert.That(projectSettings, Does.Contain("m_BuildTargetPlatformIcons: []"));

            Assert.That(checklist, Does.Contain("Player metadata still uses development defaults."));
            Assert.That(checklist, Does.Contain("Store/platform art package is missing."));
        }

        [Test]
        public void PlatformReleaseManifest_TracksEveryOpenReleaseDecision()
        {
            var manifest = File.ReadAllText(PlatformManifestPath);

            Assert.That(manifest, Does.Contain("Current approved milestone: internal development validation."));
            Assert.That(manifest, Does.Contain("Option A internal-validation baseline"));
            Assert.That(manifest, Does.Contain("Do not mark the project platform-release ready"));
            Assert.That(manifest, Does.Contain("RELEASE_BLOCKER"));
            Assert.That(manifest, Does.Contain("MILESTONE_DEFERRED"));

            AssertManifestDecision(manifest, "PRG-STEAM-SDK", "Steamworks SDK binaries are not bundled.", "RELEASE_BLOCKER");
            AssertManifestDecision(manifest, "PRG-ACHIEVEMENTS", "Platform achievements", "RELEASE_BLOCKER");
            AssertManifestDecision(manifest, "PRG-CLOUD-SAVE", "Save system is local-only.", "MILESTONE_DEFERRED");
            AssertManifestDecision(manifest, "PRG-PRIVACY", "No platform telemetry package is configured", "READY");
            AssertManifestDecision(manifest, "PRG-PLAYER-METADATA", "Unity player settings still use development defaults.", "RELEASE_BLOCKER");
            AssertManifestDecision(manifest, "PRG-STORE-ASSETS", "Store capsule, header, library art, and platform icon package are missing.", "RELEASE_BLOCKER");
            AssertManifestDecision(manifest, "PRG-PLATFORM-BUILDS", "No target platform build validation is recorded.", "RELEASE_BLOCKER");

            Assert.That(manifest, Does.Contain("Exit Criteria"));
            Assert.That(manifest, Does.Contain("Owner Area"));
            Assert.That(manifest, Does.Contain("Repository Evidence"));
        }

        [Test]
        public void InternalValidationBaseline_DoesNotClaimPlatformReleaseReadiness()
        {
            var checklist = File.ReadAllText(PlatformChecklistPath);
            var manifest = File.ReadAllText(PlatformManifestPath);

            Assert.That(checklist, Does.Contain("Option A Internal-Validation Baseline"));
            Assert.That(checklist, Does.Contain("does not claim Steam, store, cloud-save, or platform-release readiness"));
            Assert.That(checklist, Does.Contain("all public-release blockers remain visible"));

            Assert.That(manifest, Does.Contain("Public release state: blocked."));
            Assert.That(manifest, Does.Contain("Store submission state: blocked."));
            Assert.That(manifest, Does.Contain("Steam runtime validation state: blocked."));
            Assert.That(manifest, Does.Contain("do not claim platform-release readiness"));

            Assert.That(checklist, Does.Not.Contain("Status: **platform-release ready**"));
            Assert.That(manifest, Does.Not.Contain("Public release state: ready."));
            Assert.That(manifest, Does.Not.Contain("Store submission state: ready."));
            Assert.That(manifest, Does.Not.Contain("Steam runtime validation state: ready."));
        }

        [Test]
        public void PlatformReleaseGate_DetectsAnalyticsFreeBaselineAndStoreAssetBlockers()
        {
            var manifest = File.ReadAllText(PackageManifestPath);
            var checklist = File.ReadAllText(PlatformChecklistPath);

            Assert.That(manifest, Does.Not.Contain("\"com.unity.analytics\""));
            Assert.That(manifest, Does.Not.Contain("\"com.unity.services.analytics\""));
            Assert.That(manifest, Does.Not.Contain("\"com.unity.modules.unityanalytics\""));

            Assert.IsFalse(ExistsAny(
                "Assets",
                "Steamworks.NET.dll",
                "steam_api64.dll",
                "steam_api.dll"));
            Assert.IsFalse(ExistsAny(
                "Assets",
                "store_capsule.png",
                "store_header.png",
                "library_hero.png",
                "app_icon.png"));

            Assert.That(checklist, Does.Contain("Steam SDK is not imported."));
            Assert.That(checklist, Does.Contain("Unity Analytics packages are not present."));
            Assert.That(checklist, Does.Contain("Store/platform art package is missing."));
        }

        [Test]
        public void FutureScopeRegister_DefersCloudSaveLeaderboardsAndModdingClaims()
        {
            var register = File.ReadAllText(FutureScopeDecisionRegisterPath);

            Assert.That(register, Does.Contain("FUTURE-005"));
            Assert.That(register, Does.Contain("FUTURE-006"));
            Assert.That(register, Does.Contain("FUTURE-007"));
            Assert.That(register, Does.Contain("FUTURE-008"));
            Assert.That(register, Does.Contain("Online leaderboards are out of current scope"));
            Assert.That(register, Does.Contain("Cloud save is explicitly reported as `NotConfigured`"));
            Assert.That(register, Does.Contain("External import/export and modding APIs are out of current scope"));
            Assert.That(register, Does.Contain("Do not market cloud save"));
        }

        private static bool ExistsAny(string root, params string[] fileNames)
        {
            if (!Directory.Exists(root))
                return false;

            foreach (var fileName in fileNames)
            {
                foreach (var path in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                {
                    if (path.IndexOf(".meta", StringComparison.OrdinalIgnoreCase) < 0)
                        return true;
                }
            }

            return false;
        }

        private static void AssertManifestDecision(
            string manifest,
            string id,
            string evidence,
            string gateState)
        {
            Assert.That(manifest, Does.Contain(id));
            Assert.That(manifest, Does.Contain(evidence));
            Assert.That(manifest, Does.Contain(gateState));
        }
    }
}
