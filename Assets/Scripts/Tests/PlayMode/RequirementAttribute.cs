using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace SudokuRoguelike.Tests
{
    /// <summary>
    /// PlayMode copy of the EditMode RequirementAttribute (separate test assemblies). Links a test or
    /// fixture to requirement IDs from docs/; each ID becomes category "REQ:&lt;id&gt;" and a
    /// "Requirement" property in the results XML (GAP-010).
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
