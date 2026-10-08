using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SudokuRoguelike.Platform;

namespace SudokuRoguelike.Tests
{
    // SPEC-PROC-005 (2026-10-07): the release documents exist again and describe the 1.1.0 Steam release. These tests
    // keep them consistent with the repository: every runtime release blocker is listed, the gate states match what is
    // bundled, and the version matches the player settings. (The 1.0-era version pinned an internal-validation baseline
    // that stopped being true when Steamworks.NET was added.)
    [TestFixture]
    public sealed class PlatformReleaseReadinessTests
    {
        private const string PlatformChecklistPath = "docs/release/platform-readiness-checklist.md";
        private const string PlatformManifestPath = "docs/release/platform-release-manifest.md";
        private const string FutureScopeDecisionRegisterPath = "docs/future-scope-decision-register.md";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string PackageManifestPath = "Packages/manifest.json";

        private static readonly string[] Gates =
        {
            "PRG-STEAM-SDK", "PRG-STEAM-APPID", "PRG-ACHIEVEMENTS", "PRG-STATS-LEADERBOARD", "PRG-CLOUD-SAVE",
            "PRG-PRIVACY", "PRG-PLAYER-METADATA", "PRG-STORE-ASSETS", "PRG-PLATFORM-BUILDS", "PRG-LOCALIZATION", "PRG-AUDIO"
        };

        private static readonly string[] States = { "READY", "OWNER_ACTION", "RELEASE_BLOCKER", "MILESTONE_DEFERRED" };

        [TearDown]
        public void TearDown()
        {
            PlatformServiceRegistry.ResetForTests();
        }

        [Test]
        public void EveryRuntimeReleaseBlocker_IsListedInChecklistAndManifest()
        {
            var checklist = File.ReadAllText(PlatformChecklistPath);
            var manifest = File.ReadAllText(PlatformManifestPath);
            var status = PlatformServiceRegistry.CurrentStatus;

            Assert.IsTrue(status.HasReleaseBlockers);
            foreach (var blocker in status.ReleaseBlockers)
            {
                StringAssert.Contains(blocker, checklist, "checklist misses a runtime blocker");
                StringAssert.Contains(blocker, manifest, "manifest misses a runtime blocker");
            }
        }

        [Test]
        public void Manifest_HasEveryGate_WithOneKnownState()
        {
            var manifest = File.ReadAllText(PlatformManifestPath);
            foreach (var gate in Gates)
            {
                var row = manifest.Split('\n').FirstOrDefault(line => line.StartsWith("| " + gate + " |", StringComparison.Ordinal));
                Assert.NotNull(row, $"{gate} row missing");
                Assert.AreEqual(1, States.Count(state => row.Contains("| " + state + " |")), $"{gate}: exactly one state expected");
            }
            Assert.That(manifest, Does.Contain("Exit Criteria"));
            Assert.That(manifest, Does.Contain("Owner Area"));
            Assert.That(manifest, Does.Contain("Repository Evidence"));
        }

        [Test]
        public void GateStates_MatchWhatTheRepositoryBundles()
        {
            var manifest = File.ReadAllText(PlatformManifestPath);
            var packages = File.ReadAllText(PackageManifestPath);

            var steamBundled = ExistsAny("Assets", "steam_api64.dll") && ExistsAny("Assets", "steam_api.dll");
            Assert.AreEqual(steamBundled, GateState(manifest, "PRG-STEAM-SDK") == "READY",
                "PRG-STEAM-SDK must be READY exactly when the Steam API redistributables are bundled");

            var analyticsFree = !packages.Contains("\"com.unity.analytics\"")
                                && !packages.Contains("\"com.unity.services.analytics\"")
                                && !packages.Contains("\"com.unity.modules.unityanalytics\"");
            Assert.IsTrue(analyticsFree, "an analytics package was added; update PRG-PRIVACY and the privacy notice");
            Assert.AreEqual("READY", GateState(manifest, "PRG-PRIVACY"));
        }

        [Test]
        public void Manifest_NamesThePlayerSettingsVersionAndCompany()
        {
            var settings = File.ReadAllText(ProjectSettingsPath);
            var manifest = File.ReadAllText(PlatformManifestPath);
            var version = Regex.Match(settings, @"bundleVersion: (\S+)").Groups[1].Value;
            var company = Regex.Match(settings, @"companyName: (\S+)").Groups[1].Value;

            StringAssert.Contains("Release:** " + version, manifest, "manifest release version differs from bundleVersion");
            StringAssert.Contains("Company `" + company + "`", manifest);
        }

        [Test]
        public void Checklist_DoesNotClaimReleaseWhileGatesAreOpen()
        {
            var checklist = File.ReadAllText(PlatformChecklistPath);
            var manifest = File.ReadAllText(PlatformManifestPath);
            var openGates = Gates.Count(gate => GateState(manifest, gate) != "READY" && GateState(manifest, gate) != "MILESTONE_DEFERRED");

            if (openGates > 0)
            {
                Assert.That(checklist, Does.Not.Contain("Status: **platform-release ready**"));
                Assert.That(manifest, Does.Contain("Public release state: blocked"));
            }
            Assert.That(checklist, Does.Contain("Unity Analytics packages are not present."));
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

        private static string GateState(string manifest, string gate)
        {
            var row = manifest.Split('\n').FirstOrDefault(line => line.StartsWith("| " + gate + " |", StringComparison.Ordinal));
            return row == null ? null : States.FirstOrDefault(state => row.Contains("| " + state + " |"));
        }

        private static bool ExistsAny(string root, params string[] fileNames)
        {
            if (!Directory.Exists(root))
                return false;
            foreach (var fileName in fileNames)
            foreach (var path in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                if (path.IndexOf(".meta", StringComparison.OrdinalIgnoreCase) < 0)
                    return true;
            return false;
        }
    }
}
