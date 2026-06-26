using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class BuildWorkflowReadinessTests
    {
        private const string ReadmePath = "README.md";
        private const string DevelopmentDocPath = "docs/development.md";
        private const string CiValidationDocPath = "docs/ci-validation.md";
        private const string StaticValidationWorkflowPath = ".github/workflows/static-validation.yml";
        private const string UnityTestRunnerScriptPath = "tools/run-unity-tests.ps1";
        private const string WorkingTreeTriagePath = "docs/repository-working-tree-triage.md";
        private const string CanonicalSolutionPath = "sudoku_rouge_like.slnx";
        private const string LegacySolutionPath = "sudoku_rouge_like.sln";
        private const string GitignorePath = ".gitignore";
        private const string GameProjectPath = "Game.csproj";
        private const string EditModeProjectPath = "GameTests.EditMode.csproj";

        [Test]
        public void CanonicalDotnetValidation_UsesSlnxNotLegacySln()
        {
            Assert.IsTrue(File.Exists(CanonicalSolutionPath), "Canonical static compile solution is missing.");
            Assert.IsFalse(File.Exists(LegacySolutionPath), "Retired legacy root solution must stay absent; use sudoku_rouge_like.slnx for direct dotnet compile smoke checks.");

            var readme = File.ReadAllText(ReadmePath);
            var development = File.ReadAllText(DevelopmentDocPath);

            Assert.That(readme, Does.Contain("Use [Development Workflow](docs/development.md) as the supported local validation guide."));
            Assert.That(readme, Does.Contain("dotnet build sudoku_rouge_like.slnx --verbosity minimal"));
            Assert.That(readme, Does.Contain("The retired root `sudoku_rouge_like.sln` file must remain absent"));

            Assert.That(development, Does.Contain("For direct dotnet validation, use `sudoku_rouge_like.slnx`."));
            Assert.That(development, Does.Contain("The stale `sudoku_rouge_like.sln` root solution has been retired"));
            Assert.That(development, Does.Contain("dotnet build sudoku_rouge_like.slnx --verbosity minimal"));
        }

        [Test]
        public void UnityBatchmodeReleaseGate_IsDocumentedAndScripted()
        {
            Assert.IsTrue(File.Exists(UnityTestRunnerScriptPath), "Unity batchmode test runner script is missing.");

            var development = File.ReadAllText(DevelopmentDocPath);
            var ciValidation = File.ReadAllText(CiValidationDocPath);
            var script = File.ReadAllText(UnityTestRunnerScriptPath);

            Assert.That(development, Does.Contain(".\\tools\\run-unity-tests.ps1 -TestPlatform EditMode"));
            Assert.That(development, Does.Contain(".\\tools\\run-unity-tests.ps1 -TestPlatform All"));
            Assert.That(development, Does.Contain("Before a release candidate:"));

            Assert.That(ciValidation, Does.Contain("Unity batchmode tests are the authoritative gate"));
            Assert.That(ciValidation, Does.Contain("RUN_UNITY_BATCHMODE"));
            Assert.That(ciValidation, Does.Contain("Release candidates still require local or CI Unity batchmode EditMode and PlayMode results"));

            Assert.That(script, Does.Contain("[ValidateSet(\"EditMode\", \"PlayMode\", \"All\")]"));
            Assert.That(script, Does.Contain("-runTests"));
            Assert.That(script, Does.Contain("-testResults"));
        }

        [Test]
        public void StaticValidationWorkflow_TracksRequiredBuildAndLocalizationInputs()
        {
            Assert.IsTrue(File.Exists(StaticValidationWorkflowPath), "Static validation workflow is missing.");

            var workflow = File.ReadAllText(StaticValidationWorkflowPath);

            Assert.That(workflow, Does.Contain("docs/development.md"));
            Assert.That(workflow, Does.Contain("docs/ci-validation.md"));
            Assert.That(workflow, Does.Contain("tools/run-unity-tests.ps1"));
            Assert.That(workflow, Does.Contain("sudoku_rouge_like.slnx"));
            Assert.That(workflow, Does.Contain("Retired root solution must remain absent"));
            Assert.That(workflow, Does.Contain("GameTests.EditMode.csproj"));
            Assert.That(workflow, Does.Contain("GameTests.PlayMode.csproj"));
            Assert.That(workflow, Does.Contain("Assets/Resources/Localization/en.json"));
            Assert.That(workflow, Does.Contain("Assets/Resources/Localization/de.json"));
            Assert.That(workflow, Does.Contain("dotnet build sudoku_rouge_like.slnx --verbosity minimal"));
        }

        [Test]
        public void ReadmeToolTable_ReferencesExistingRepoManagedScripts()
        {
            var readme = File.ReadAllText(ReadmePath);

            Assert.That(readme, Does.Not.Contain("docs/ProductionGDD.md"));
            Assert.That(readme, Does.Not.Contain("docs/CompleteProductionBlueprint.md"));
            Assert.That(readme, Does.Not.Contain("docs/DesignBible_Chapters_1-17.md"));
            Assert.That(readme, Does.Not.Contain("docs/UnityArchitecture.md"));
            Assert.That(readme, Does.Not.Contain("docs/UnityInstall_Windows.md"));

            var scripts = new[]
            {
                "run-unity-tests.ps1",
                "agent.ps1",
                "_lint_assets.ps1",
                "_fix_filter_mode.py",
                "_fix_bg_meta.ps1",
                "_fix_csv.ps1",
                "_fix_paths.ps1",
                "_fix_node_icons.ps1",
                "_icon_cleanup.ps1",
                "_rename_bg.ps1",
                "_rename_bg.py",
                "_create_modifier_icons.py",
                "_create_placeholder_bgs.py",
                "_cleanup_folders.py",
                "_probe.ps1"
            };

            foreach (var script in scripts)
            {
                Assert.That(readme, Does.Contain($"`{script}`"), $"README no longer documents tools/{script}; update this test with the intended tool inventory.");
                Assert.IsTrue(File.Exists(Path.Combine("tools", script)), $"README references tools/{script}, but the file is missing.");
                Assert.IsFalse(File.Exists(script), $"{script} should stay under tools/ instead of the repository root.");
            }
        }

        [Test]
        public void Gitignore_AllowsRepoManagedCiAndToolScripts()
        {
            var gitignore = File.ReadAllText(GitignorePath);

            Assert.That(gitignore, Does.Contain("!.github/"));
            Assert.That(gitignore, Does.Contain("!.github/workflows/"));
            Assert.That(gitignore, Does.Contain("!.github/workflows/*.yml"));
            Assert.That(gitignore, Does.Contain("!.github/workflows/*.yaml"));
            Assert.That(gitignore, Does.Contain("!tools/"));
            Assert.That(gitignore, Does.Contain("!tools/run-unity-tests.ps1"));
            Assert.That(gitignore, Does.Contain("!tools/*.ps1"));
            Assert.That(gitignore, Does.Contain("!tools/*.py"));
        }

        [Test]
        public void GeneratedProjectMetadata_IncludesKnownSourceAndEditModeTests()
        {
            var gameProject = File.ReadAllText(GameProjectPath);
            var editModeProject = File.ReadAllText(EditModeProjectPath);

            Assert.That(gameProject, Does.Contain(@"Assets\Scripts\UI\ButtonExtensions.cs"));
            Assert.That(editModeProject, Does.Contain(@"Assets\Scripts\Tests\EditMode\XpTableTests.cs"));
        }

        [Test]
        public void WorkingTreeTriage_DocumentsDirtyTreeReviewPolicy()
        {
            Assert.IsTrue(File.Exists(WorkingTreeTriagePath), "Repository working tree triage note is missing.");

            var triage = File.ReadAllText(WorkingTreeTriagePath);
            Assert.That(triage, Does.Contain("REPO-004"));
            Assert.That(triage, Does.Contain("git status --short"));
            Assert.That(triage, Does.Contain("Do not use `git reset --hard`"));
            Assert.That(triage, Does.Contain("docs/art-source/background"));
            Assert.That(triage, Does.Contain(".\\tools\\run-unity-tests.ps1 -TestPlatform All"));
        }
    }
}
