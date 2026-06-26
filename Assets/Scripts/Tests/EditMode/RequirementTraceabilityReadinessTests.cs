using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class RequirementTraceabilityReadinessTests
    {
        private const string AnnotationGapReportPath = "docs/AnnotationGapReport.md";
        private const string CoverageResultPath = "_coverage_result.txt";

        [Test]
        public void AnnotationGapReport_StatesTraceabilityIsNotRuntimeVerification()
        {
            var report = File.ReadAllText(AnnotationGapReportPath);

            Assert.That(report, Does.Contain("Traceability-only warning"));
            Assert.That(report, Does.Contain("does not prove runtime behavior"));
            Assert.That(report, Does.Contain("does not prove UI wiring"));
            Assert.That(report, Does.Contain("does not prove localization completeness"));
            Assert.That(report, Does.Contain("Unity batchmode"));
        }

        [Test]
        public void CoverageResult_StatesAnnotationCoverageIsNotReleaseCompleteness()
        {
            var result = File.ReadAllText(CoverageResultPath);

            Assert.That(result, Does.Contain("TRACEABILITY ONLY"));
            Assert.That(result, Does.Contain("not runtime verification"));
            Assert.That(result, Does.Contain("not release readiness"));
        }
    }
}
