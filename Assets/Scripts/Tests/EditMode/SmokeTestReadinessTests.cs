using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class SmokeTestReadinessTests
    {
        private const string ManifestPath = "docs/smoke-test-readiness.md";
        private const string WorkflowPath = ".github/workflows/static-validation.yml";
        private const string DevelopmentDocPath = "docs/development.md";

        [Test]
        public void SmokeManifest_TracksRequiredRuntimeAndGeneratedUiScenarios()
        {
            var manifest = File.ReadAllText(ManifestPath);

            StringAssert.Contains("SMOKE-EDIT-RUNFLOW", manifest);
            StringAssert.Contains("RunFlowAcceptanceTests.cs", manifest);
            StringAssert.Contains("SMOKE-UI-WIRING", manifest);
            StringAssert.Contains("GeneratedUiWiringContractTests.cs", manifest);
            StringAssert.Contains("SMOKE-FIRST-RUN", manifest);
            StringAssert.Contains("FirstRunSetupPlayModeTests.cs", manifest);
            StringAssert.Contains("SMOKE-PLAYMODE-UI", manifest);
            StringAssert.Contains("GeneratedUiPlayModeTraversalTests.cs", manifest);
            StringAssert.Contains("SMOKE-BATCHMODE", manifest);
            StringAssert.Contains("RUN_UNITY_BATCHMODE", manifest);
            StringAssert.Contains("not for beta, early-access, or release-candidate approval", manifest);
        }

        [Test]
        public void SmokeManifest_ReferencesExistingTestFilesAndRunner()
        {
            Assert.IsTrue(File.Exists("Assets/Scripts/Tests/EditMode/RunFlowAcceptanceTests.cs"));
            Assert.IsTrue(File.Exists("Assets/Scripts/Tests/EditMode/GeneratedUiWiringContractTests.cs"));
            Assert.IsTrue(File.Exists("Assets/Scripts/Tests/EditMode/GeneratedUiRegistryTests.cs"));
            Assert.IsTrue(File.Exists("Assets/Scripts/Tests/EditMode/ConditionalButtonStateTests.cs"));
            Assert.IsTrue(File.Exists("Assets/Scripts/Tests/PlayMode/FirstRunSetupPlayModeTests.cs"));
            Assert.IsTrue(File.Exists("Assets/Scripts/Tests/PlayMode/GeneratedUiPlayModeTraversalTests.cs"));
            Assert.IsTrue(File.Exists("Assets/Scripts/Tests/PlayMode/PlayModeSmokeTests.cs"));
            Assert.IsTrue(File.Exists("tools/run-unity-tests.ps1"));
        }

        [Test]
        public void WorkflowAndDevelopmentDocs_MakeUnityRuntimeGateExplicit()
        {
            var workflow = File.ReadAllText(WorkflowPath);
            var development = File.ReadAllText(DevelopmentDocPath);

            StringAssert.Contains("docs/smoke-test-readiness.md", workflow);
            StringAssert.Contains("unity-batchmode", workflow);
            StringAssert.Contains("RUN_UNITY_BATCHMODE", workflow);
            StringAssert.Contains(".\\tools\\run-unity-tests.ps1 -TestPlatform All", workflow);

            StringAssert.Contains("Smoke Test Readiness", development);
            StringAssert.Contains("docs/smoke-test-readiness.md", development);
            StringAssert.Contains("PlayMode test sources live under `Assets/Scripts/Tests/PlayMode/`", development);
            StringAssert.Contains("not an authoritative Unity runtime pass", development);
        }
    }
}
