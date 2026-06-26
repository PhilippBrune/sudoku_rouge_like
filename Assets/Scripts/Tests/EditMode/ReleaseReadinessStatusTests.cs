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

            StringAssert.Contains("Prototype-to-alpha hardening", status);
            StringAssert.Contains("Alpha Readiness Hardening", status);
            StringAssert.Contains("Release readiness score: **68/100**", status);
            StringAssert.Contains("Unity batchmode EditMode and PlayMode XML results", status);
            StringAssert.Contains("Unity Profiler captures on target hardware", status);
            StringAssert.Contains("docs/release/platform-release-manifest.md", status);
            StringAssert.Contains("docs/repository-working-tree-triage.md", status);
            StringAssert.Contains("Do not label this project beta", status);
        }
    }
}
