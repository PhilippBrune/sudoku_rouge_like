using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class ReleaseReadinessStatusTests
    {
        private const string ReleaseStatusPath = "docs/release/release-readiness-status.md";

        [Test]
        public void ReleaseStatus_DocumentsCurrentMilestoneAndOpenGates()
        {
            var status = File.ReadAllText(ReleaseStatusPath);
            var version = Regex.Match(File.ReadAllText("ProjectSettings/ProjectSettings.asset"), @"bundleVersion: (\S+)").Groups[1].Value;

            // SPEC-PROC-005: the status page names the Steam build of the current bundleVersion and points to its evidence.
            StringAssert.Contains("Steam build v" + version, status);
            StringAssert.Contains("Unity batchmode EditMode and PlayMode XML results", status);
            StringAssert.Contains("Unity Profiler captures on target hardware", status);
            StringAssert.Contains("docs/release/platform-release-manifest.md", status);
            StringAssert.Contains("docs/release/platform-readiness-checklist.md", status);
            StringAssert.Contains("docs/repository-working-tree-triage.md", status);
            StringAssert.Contains("docs/review/E2E_TEST_REPORT.md", status);
        }
    }
}
