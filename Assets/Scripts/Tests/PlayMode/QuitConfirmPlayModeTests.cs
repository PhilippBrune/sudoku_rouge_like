using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    // GAP-074 (0.3.0 playtest): one press on Quit no longer closes the game; it says to press again. (A second press would
    // end the test's play mode, so only the first is checked.)
    [TestFixture]
    [Requirement("UI-MENU-QUIT-001")]
    public sealed class QuitConfirmPlayModeTests
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
            DeleteSlot();
            _originalSplashShown = SplashSceneBootstrap.HasShownSplash;
            SetSplashShown(true);
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
            DeleteSlot();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnePressOnQuit_AsksForASecond()
        {
            new GameObject("QuitBootstrapUnderTest").AddComponent<GameBootstrap>();
            for (var i = 0; i < 5; i++) yield return null;
            Object.FindAnyObjectByType<MainMenuController>().SelectProfileSlot(TestSlot);
            for (var i = 0; i < 3; i++) yield return null;
            Find("BtnContinueFirstRun")?.GetComponent<Button>().onClick.Invoke();
            for (var i = 0; i < 3; i++) yield return null;

            Find("BtnQuit").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(Application.isPlaying, "the game is still running");
            Assert.AreEqual(LocalizationService.T("MainMenu.Status.QuitConfirm"), Find("StatusText").GetComponent<Text>().text);
        }

        // [REQ: UI-STYLE-039] 0.3.1 playtest (owner's pick A): the goodbye scroll with the Kratossos seal and what waits
        // tomorrow; it ends on its own after its seconds (here the game is not closed: the test passes its own end).
        [UnityTest]
        [Requirement("UI-STYLE-039")]
        public IEnumerator TheQuitScreen_ShowsTheGardenAtDusk_ThenEnds()
        {
            var ended = false;
            var screen = QuitScreen.Show(() => ended = true);
            yield return null;
            Assert.AreSame(screen, QuitScreen.Show(), "shown once");
            Assert.IsNotNull(Find("QuitScroll"));
            Assert.AreEqual("KRATOSSOS", Find("KratossosSeal/Studio")?.GetComponent<Text>()?.text ?? Find("Studio")?.GetComponent<Text>()?.text);
            Assert.IsNotNull(Find("TomorrowLine_0"), "the day's walk");
            Assert.IsNotNull(Find("TomorrowLine_1"), "the Monthly Walk (no saved run here)");
            Assert.IsFalse(ended);
            yield return new WaitForSecondsRealtime(QuitScreen.Seconds + 0.3f);
            Assert.IsTrue(ended, "it ends on its own");
            Object.Destroy(screen.gameObject);
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.name == name) return t.gameObject;
            return null;
        }

        private static void SetSplashShown(bool value) =>
            typeof(SplashSceneBootstrap).GetProperty("HasShownSplash", BindingFlags.Public | BindingFlags.Static)
                .GetSetMethod(true).Invoke(null, new object[] { value });

        private static void DeleteSlot()
        {
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }
    }
}
