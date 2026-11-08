using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-UI-100] [REQ: INPUT-PROFILE-001] [REQ: INTRO-002] GAP-006: on a fresh install the real GameBootstrap shows
    // Profile Select, then first-run setup, then the Sudoku basics prompt. MainMenuController.Start used to replace
    // Profile Select with the main menu when the dedicated splash scene had already run.
    [TestFixture]
    [Requirement("REQ-UI-100", "INPUT-PROFILE-001", "INTRO-002")]
    public sealed class StartupFlowPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private readonly HashSet<GameObject> _rootsBefore = new HashSet<GameObject>();
        private int _originalActiveSlot;
        private bool _originalSplashShown;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            DeleteAllSlots();
            _originalSplashShown = SplashSceneBootstrap.HasShownSplash;
            SetSplashShown(true); // the build path: the Splash scene ran before the main scene
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                _rootsBefore.Add(t.root.gameObject);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.parent == null && !_rootsBefore.Contains(t.gameObject))
                    Object.Destroy(t.gameObject);
            _rootsBefore.Clear();
            SetSplashShown(_originalSplashShown);
            DeleteAllSlots();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            LocalizationService.SetLanguage(LanguageOption.English);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreshInstall_ShowsProfileSelect_ThenSetup_ThenBasicsPrompt()
        {
            new GameObject("StartupBootstrapUnderTest").AddComponent<GameBootstrap>();
            for (var i = 0; i < 5; i++) yield return null;

            var profileSelect = Find("ProfileSelectPanel");
            Assert.NotNull(profileSelect, "profile select panel was not built");
            Assert.IsTrue(profileSelect.activeInHierarchy, "a fresh install starts at Profile Select");
            Assert.IsFalse(Find("MenuCard").activeInHierarchy, "the main menu must not cover Profile Select");

            Object.FindAnyObjectByType<MainMenuController>().SelectProfileSlot(TestSlot);
            for (var i = 0; i < 3; i++) yield return null;

            var setup = Find("FirstRunSetupModal");
            Assert.NotNull(setup, "first-run setup follows the profile choice");
            Find("BtnContinueFirstRun").GetComponent<Button>().onClick.Invoke();
            for (var i = 0; i < 3; i++) yield return null;

            Assert.IsNull(Find("FirstRunSetupModal"), "setup closes after Continue");
            Assert.IsNull(Find("SudokuBasicsPrompt"), "Enter the garden replaces the Basics prompt (UI-STYLE-045)");
            Assert.IsTrue(StartupFlowController.IsFirstRunSetupComplete(new SaveFileService(TestSlot).Load()));
        }

        // [REQ: UI-MENU-TITLE-001] GAP-097: the main menu waited behind the title screen with "Start Game" chosen, so a
        // pad's A (or Enter) on the title opened Select Class ("New Run"). Both the menu's A handler and the input
        // module's Submit act on the chosen button; a virtual pad cannot press the old input's joystick button, so the
        // test checks that nothing is chosen while the title shows, and that the menu takes the focus after it.
        [UnityTest]
        [Requirement("UI-MENU-TITLE-001")]
        public IEnumerator UnderTheTitle_NoMenuButtonIsChosen()
        {
            var envelope = new SaveFileEnvelope();
            StartupFlowController.MarkFirstRunSetupComplete(envelope);
            envelope.TutorialProgress.CompletedKeys.Add("sudoku_basics");
            new SaveFileService(TestSlot).Save(envelope);
            SaveFileService.FlushSharedPendingWrites();
            SetSplashShown(false);   // a start from the main scene: the title screen shows inline

            new GameObject("TitleBootstrapUnderTest").AddComponent<GameBootstrap>();
            for (var i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(SplashScreenController.Showing, "the title screen is up");
            Assert.IsNull(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject,
                "no menu button is chosen under the title, so A there presses nothing");

            yield return new WaitForSecondsRealtime(4f);
            Assert.IsFalse(SplashScreenController.Showing, "the title screen ended");
            Assert.IsTrue(Find("MenuCard").activeInHierarchy, "the main menu shows after the title");
            Assert.NotNull(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, "and takes the focus");
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.name == name) return t.gameObject;
            return null;
        }

        private static void SetSplashShown(bool value)
        {
            var property = typeof(SplashSceneBootstrap).GetProperty("HasShownSplash",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(property, "SplashSceneBootstrap.HasShownSplash not found");
            property.GetSetMethod(true).Invoke(null, new object[] { value });
        }

        private static void DeleteAllSlots()
        {
            for (var slot = 0; slot < SaveFileService.MaxSlots; slot++)
            {
                var path = SaveFileService.GetSlotPath(slot);
                foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                    if (File.Exists(p)) File.Delete(p);
            }
        }
    }
}
