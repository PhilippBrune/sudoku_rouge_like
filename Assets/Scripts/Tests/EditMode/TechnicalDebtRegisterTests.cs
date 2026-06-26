using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class TechnicalDebtRegisterTests
    {
        private const string RegisterPath = "docs/technical-debt-register.md";
        private const string ScriptsRoot = "Assets/Scripts";

        private static readonly string[] DebtMarkers =
        {
            "TODO",
            "FIXME",
            "HACK",
            "UNUSED",
            "DEPRECATED",
            "placeholder",
            "stub",
            "WIP"
        };

        [Test]
        public void PlaceholderAndStubMarkers_InProductionScriptsAreRegistered()
        {
            var register = File.ReadAllText(RegisterPath);
            var findings = FindDebtMarkers().ToArray();

            Assert.That(findings.Length, Is.GreaterThan(0), "No production debt markers were found; remove the register entries or update this test.");
            foreach (var finding in findings)
            {
                Assert.That(register, Does.Contain(finding.Path), $"{finding.Path} has an unregistered debt marker.");
                Assert.That(register, Does.Contain(finding.Marker), $"{finding.Path} uses marker '{finding.Marker}' but the register does not classify it.");
            }
        }

        [Test]
        public void TechnicalDebtRegister_ClassifiesCurrentAuditFindings()
        {
            var register = File.ReadAllText(RegisterPath);

            Assert.That(register, Does.Contain("CBH-006"));
            Assert.That(register, Does.Contain("DEAD-006"));
            Assert.That(register, Does.Contain("intentional fallback"));
            Assert.That(register, Does.Contain("remove or update the register"));
        }

        private static IEnumerable<(string Path, string Marker)> FindDebtMarkers()
        {
            foreach (var path in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = Normalize(path);
                if (normalized.Contains("/Tests/"))
                    continue;

                var text = File.ReadAllText(path);
                foreach (var marker in DebtMarkers)
                {
                    if (text.Contains(marker))
                        yield return (normalized, marker);
                }
            }
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
