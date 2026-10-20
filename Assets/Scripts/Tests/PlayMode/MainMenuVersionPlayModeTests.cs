using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-MENU-VERSION-001] The main menu shows the game's version (0.2.0 playtest).
    [TestFixture]
    [Requirement("UI-MENU-VERSION-001")]
    public sealed class MainMenuVersionPlayModeTests
    {
        private readonly List<GameObject> _createdRoots = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in _createdRoots) if (root != null) Object.Destroy(root);
            _createdRoots.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_ShowsTheVersion()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("VersionTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("VersionTestMainMenu");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            host.GetComponent<MainMenuController>().ShowMainMenu();
            yield return null;

            Text version = null;
            foreach (var t in host.GetComponentsInChildren<Text>(true))
                if (t.name == "VersionText") version = t;
            Assert.NotNull(version, "a version label on the main menu");
            Assert.IsTrue(version.gameObject.activeInHierarchy);
            StringAssert.Contains(Application.version, version.text);
        }
    }
}
