using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class StartupExperienceReadinessTests
    {
        private const string BuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
        private const string SplashScenePath = "Assets/Scenes/Splash.unity";
        private const string PrototypeScenePath = "Assets/Scenes/Prototype.unity";
        private const string StartupFlowControllerPath = "Assets/Scripts/Bootstrap/StartupFlowController.cs";
        private const string GameBootstrapPath = "Assets/Scripts/Bootstrap/GameBootstrap.cs";
        private const string SplashSceneBootstrapPath = "Assets/Scripts/Bootstrap/SplashSceneBootstrap.cs";
        private const string FirstRunSetupControllerPath = "Assets/Scripts/UI/FirstRunSetupModalController.cs";
        private const string SaveModelsPath = "Assets/Scripts/Core/SaveModels.cs";
        private const string SaveFileServicePath = "Assets/Scripts/Save/SaveFileService.cs";
        private const string FirstRunPlayModeTestPath = "Assets/Scripts/Tests/PlayMode/FirstRunSetupPlayModeTests.cs";
        private const string StartupExperienceDocPath = "docs/startup-experience.md";
        private const string DevelopmentDocPath = "docs/development.md";

        [Test]
        public void BuildSettings_UseDedicatedSplashBeforePrototype()
        {
            Assert.IsTrue(File.Exists(SplashScenePath), "Dedicated Splash scene is missing.");
            Assert.IsTrue(File.Exists(PrototypeScenePath), "Prototype scene is missing.");
            Assert.IsTrue(File.Exists(BuildSettingsPath), "Unity build settings are missing.");

            var buildSettings = File.ReadAllText(BuildSettingsPath);
            var splashIndex = buildSettings.IndexOf("path: Assets/Scenes/Splash.unity", System.StringComparison.Ordinal);
            var prototypeIndex = buildSettings.IndexOf("path: Assets/Scenes/Prototype.unity", System.StringComparison.Ordinal);

            Assert.GreaterOrEqual(splashIndex, 0, "Splash scene must be included in build settings.");
            Assert.GreaterOrEqual(prototypeIndex, 0, "Prototype scene must be included in build settings.");
            Assert.Less(splashIndex, prototypeIndex, "Splash scene must load before Prototype in build settings.");
            Assert.That(buildSettings, Does.Contain("guid: 8dfedc31e3df4f4a8a9821f3182ccf7c"));
            Assert.That(buildSettings, Does.Contain("guid: ddb6fc8aa20d6de4cb8496a9471f2d6a"));
        }

        [Test]
        public void StartupFlow_GatesFirstRunBeforeTutorialAndMainMenu()
        {
            var startupFlow = File.ReadAllText(StartupFlowControllerPath);
            var gameBootstrap = File.ReadAllText(GameBootstrapPath);

            Assert.That(startupFlow, Does.Contain("CurrentLegalNoticeVersion"));
            Assert.That(startupFlow, Does.Contain("LanguageSelected"));
            Assert.That(startupFlow, Does.Contain("AccessibilityReviewed"));
            Assert.That(startupFlow, Does.Contain("AcceptedLegalNoticeVersion"));
            Assert.That(startupFlow, Does.Contain("StartupFlowStep.FirstRunSetup"));
            Assert.That(startupFlow, Does.Contain("StartupFlowStep.SudokuBasicsPrompt"));
            Assert.That(startupFlow, Does.Contain("MarkFirstRunSetupComplete"));

            Assert.That(gameBootstrap, Does.Contain("RunStartupFlow"));
            Assert.That(gameBootstrap, Does.Contain("menu.ShowFirstRunSetupPrompt"));
            Assert.That(gameBootstrap, Does.Contain("ContinueAfterFirstRunSetup"));
            Assert.That(gameBootstrap, Does.Contain("refreshedMenu.ShowSudokuBasicsPrompt"));
        }

        [Test]
        public void FirstRunState_IsPersistedAndMigratedForExistingSaves()
        {
            var saveModels = File.ReadAllText(SaveModelsPath);
            var saveFileService = File.ReadAllText(SaveFileServicePath);

            Assert.That(saveModels, Does.Contain("public FirstRunSetupState FirstRun = new FirstRunSetupState()"));
            Assert.That(saveModels, Does.Contain("public bool LanguageSelected"));
            Assert.That(saveModels, Does.Contain("public bool AccessibilityReviewed"));
            Assert.That(saveModels, Does.Contain("public string AcceptedLegalNoticeVersion"));
            Assert.That(saveModels, Does.Contain("public string CompletedUtc"));

            Assert.That(saveFileService, Does.Contain("options.FirstRun == null"));
            Assert.That(saveFileService, Does.Contain("options.FirstRun = new FirstRunSetupState()"));
        }

        [Test]
        public void FirstRunModal_CoversLanguageAccessibilityAndLegalNotice()
        {
            var controller = File.ReadAllText(FirstRunSetupControllerPath);
            var playModeTest = File.ReadAllText(FirstRunPlayModeTestPath);

            foreach (var key in new[]
            {
                "FirstRun.Title",
                "FirstRun.Body",
                "FirstRun.LanguageHeader",
                "FirstRun.Language.English",
                "FirstRun.Language.German",
                "FirstRun.AccessibilityHeader",
                "FirstRun.Accessibility.Colorblind",
                "FirstRun.Accessibility.HighContrast",
                "FirstRun.Accessibility.ReduceMotion",
                "FirstRun.Accessibility.ScreenReader",
                "FirstRun.LegalHeader",
                "FirstRun.LegalBody",
                "FirstRun.Continue"
            })
            {
                Assert.That(controller, Does.Contain(key), $"{key} is not wired into the first-run modal.");
            }

            Assert.That(controller, Does.Contain("CompleteSetup"));
            Assert.That(controller, Does.Contain("SelectLanguage(LanguageOption.English)"));
            Assert.That(controller, Does.Contain("SelectLanguage(LanguageOption.German)"));
            Assert.That(controller, Does.Contain("ColorblindMode"));
            Assert.That(controller, Does.Contain("HighContrast"));
            Assert.That(controller, Does.Contain("ReduceMotion"));
            Assert.That(controller, Does.Contain("ScreenReaderEnabled"));

            Assert.That(playModeTest, Does.Contain("FirstRunModal_PersistsLanguageAccessibilityLegalAndShowsTutorialPrompt"));
            Assert.That(playModeTest, Does.Contain("BtnLanguageGerman"));
            Assert.That(playModeTest, Does.Contain("AcceptedLegalNoticeVersion"));
            Assert.That(playModeTest, Does.Contain("AccessibilityReviewed"));
            Assert.That(playModeTest, Does.Contain("SudokuBasicsPrompt"));
        }

        [Test]
        public void SplashPipeline_KeepsDedicatedSceneAndEditorFallbackDocumented()
        {
            var splashBootstrap = File.ReadAllText(SplashSceneBootstrapPath);
            var gameBootstrap = File.ReadAllText(GameBootstrapPath);
            var development = File.ReadAllText(DevelopmentDocPath);

            Assert.That(splashBootstrap, Does.Contain("LoadScene(\"Prototype\")"));
            Assert.That(splashBootstrap, Does.Contain("HasShownSplash = true"));
            Assert.That(gameBootstrap, Does.Contain("SplashSceneBootstrap.HasShownSplash"));
            Assert.That(gameBootstrap, Does.Contain("SplashScreenController"));
            Assert.That(development, Does.Contain("docs/startup-experience.md"));
        }

        [Test]
        public void StartupExperienceDocument_TracksResolvedIntroAuditItems()
        {
            Assert.IsTrue(File.Exists(StartupExperienceDocPath), "Startup experience documentation is missing.");

            var doc = File.ReadAllText(StartupExperienceDocPath);
            foreach (var introId in new[] { "INTRO-001", "INTRO-002", "INTRO-003", "INTRO-004", "INTRO-005" })
                Assert.That(doc, Does.Contain(introId), $"{introId} must stay documented after deleting the completed plan file.");

            Assert.That(doc, Does.Contain("Assets/Scenes/Splash.unity"));
            Assert.That(doc, Does.Contain("Assets/Scenes/Prototype.unity"));
            Assert.That(doc, Does.Contain("FirstRunSetupModalController"));
            Assert.That(doc, Does.Contain("FirstRunSetupPlayModeTests"));
            Assert.That(doc, Does.Contain("StartupFlowController.CurrentLegalNoticeVersion"));
        }
    }
}
