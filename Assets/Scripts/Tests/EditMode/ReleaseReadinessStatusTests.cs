using System.IO;
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

            // SPEC-PROC-005: the status page describes the 1.1.0 Steam release and points to its evidence.
            StringAssert.Contains("second Steam release (1.1.0)", status);
            StringAssert.Contains("Unity batchmode EditMode and PlayMode XML results", status);
            StringAssert.Contains("Unity Profiler captures on target hardware", status);
            StringAssert.Contains("docs/release/platform-release-manifest.md", status);
            StringAssert.Contains("docs/release/platform-readiness-checklist.md", status);
            StringAssert.Contains("docs/repository-working-tree-triage.md", status);
            StringAssert.Contains("docs/review/E2E_TEST_REPORT.md", status);
        }
    }
}
