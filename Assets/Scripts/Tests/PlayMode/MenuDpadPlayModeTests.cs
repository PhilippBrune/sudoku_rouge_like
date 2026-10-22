using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // [REQ: INPUT-NAV-001] GAP-062: on the main menu's panels the D-pad takes one step along the focused control's link (the
    // Codex's grid goes down a row, Game Modes along its curtains), not to the next control in creation order; the arrow
    // keys and the stick are left to the input module, so a press moves once.
    [TestFixture]
    [Requirement("INPUT-NAV-001")]
    public sealed class MenuDpadPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalSlot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            LocalizationService.SetLanguage(LanguageOption.English);
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in _createdRoots) if (root != null) Object.Destroy(root);
            _createdRoots.Clear();
            SaveFileService.FlushSharedPendingWrites();
            SaveProfileService.ActiveSlot = _originalSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Codex_DownGoesOneRowDown_NotToTheNextTile()
        {
            var (host, menu) = OpenMenu();
            menu.OpenItems();
            yield return null;

            var first = Find(host, "CodexTile_0");
            var down = MenuDpad.Step(first, MoveDirection.Down);
            Assert.NotNull(down, "a tile below the first");
            Assert.AreSame(first.navigation.selectOnDown, down, "the grid's own link");
            Assert.AreNotSame(Find(host, "CodexTile_1"), down, "not the next tile in creation order");
            Assert.AreSame(Find(host, "CodexTile_1"), MenuDpad.Step(first, MoveDirection.Right));
        }

        [UnityTest]
        public IEnumerator GameModes_LeftAndRightFollowTheCurtains_DownLeadsToTheWayIn()
        {
            var (host, menu) = OpenMenu();
            menu.ShowGameModes();
            yield return null;

            var garden = Find(host, "BtnGarden");
            Assert.AreSame(Find(host, "BtnEndless"), MenuDpad.Step(garden, MoveDirection.Right));
            Assert.IsNull(MenuDpad.Step(garden, MoveDirection.Left), "the first curtain: Left stays");
            Assert.AreSame(Find(host, "BtnGardenEnter"), MenuDpad.Step(garden, MoveDirection.Down));
        }

        [Test]
        public void TheMenu_LeavesArrowKeysAndTheStickToTheInputModule()
        {
            var source = File.ReadAllText("Assets/Scripts/UI/MainMenuController.cs");
            var start = source.IndexOf("private void HandleMenuGamepadNavigation()", System.StringComparison.Ordinal);
            var body = source.Substring(start, source.IndexOf("private void EnsureControllerSelection()", start, System.StringComparison.Ordinal) - start);
            StringAssert.Contains("MenuDpad.ReadPressed()", body);
            StringAssert.DoesNotContain("InputAction.Move", body, "arrow keys and the stick would move a second time");
            StringAssert.DoesNotContain("% active.Count", body, "no cycle in creation order");
        }

        private (GameObject Host, MainMenuController Menu) OpenMenu()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("MenuDpadTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("MenuDpadTestMainMenu");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            return (host, menu);
        }

        private static Selectable Find(GameObject host, string name)
        {
            var match = host.GetComponentsInChildren<Selectable>(true).FirstOrDefault(s => s.name == name);
            Assert.NotNull(match, $"missing {name}");
            return match;
        }
    }
}
