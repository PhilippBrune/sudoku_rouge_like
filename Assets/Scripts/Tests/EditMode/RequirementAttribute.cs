using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace SudokuRoguelike.Tests
{
    /// <summary>
    /// Links a test or fixture to requirement IDs from docs/ (REQUIREMENT_MAP.md and
    /// docs/review/REQUIREMENT_TRACEABILITY.md). Each ID becomes an NUnit category "REQ:&lt;id&gt;", so
    /// <c>run-unity-tests.ps1 -TestCategory "REQ:SAVE-RESUME-001"</c> runs the tests for one requirement,
    /// and a "Requirement" property in the results XML (GAP-010: requirement → test traceability).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class RequirementAttribute : NUnitAttribute, IApplyToTest
    {
        public const string CategoryPrefix = "REQ:";
        public const string PropertyName = "Requirement";

        public RequirementAttribute(params string[] ids)
        {
            Ids = ids ?? Array.Empty<string>();
        }

        public string[] Ids { get; }

        public void ApplyToTest(Test test)
        {
            foreach (var id in Ids)
            {
                test.Properties.Add(PropertyNames.Category, CategoryPrefix + id);
                test.Properties.Add(PropertyName, id);
            }
        }
    }
}
