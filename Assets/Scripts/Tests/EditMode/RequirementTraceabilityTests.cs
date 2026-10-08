using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    // [Requirement] tags must name requirements that exist in the docs, so a typo cannot create a
    // link to nothing (GAP-010: requirement → implementation → test traceability, see CLAUDE.md).
    [TestFixture]
    [Requirement("REQ-TEST-101")]
    public sealed class RequirementTraceabilityTests
    {
        private static readonly string[] RequirementSources =
        {
            "docs/REQUIREMENT_MAP.md",
            "docs/review/REQUIREMENT_TRACEABILITY.md"
        };

        [Test]
        public void RequirementTags_ReferenceDocumentedRequirementIds()
        {
            var documented = string.Join("\n", RequirementSources.Where(File.Exists).Select(File.ReadAllText));
            Assert.IsNotEmpty(documented, "No requirement source document found.");

            var tagged = TaggedIds().ToList();
            Assert.IsNotEmpty(tagged, "No test carries a [Requirement] tag.");

            var unknown = tagged
                .Where(entry => !documented.Contains(entry.Id))
                .Select(entry => $"{entry.Owner}: {entry.Id}")
                .Distinct()
                .ToArray();
            Assert.IsEmpty(unknown, "Unknown requirement IDs in [Requirement] tags.");
        }

        private static IEnumerable<(string Owner, string Id)> TaggedIds()
        {
            foreach (var type in typeof(RequirementTraceabilityTests).Assembly.GetTypes())
            {
                foreach (var attribute in type.GetCustomAttributes<RequirementAttribute>(false))
                    foreach (var id in attribute.Ids)
                        yield return (type.Name, id);

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    foreach (var attribute in method.GetCustomAttributes<RequirementAttribute>(false))
                        foreach (var id in attribute.Ids)
                            yield return ($"{type.Name}.{method.Name}", id);
            }
        }
    }
}
