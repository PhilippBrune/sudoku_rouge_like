using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-STYLE-012] The Custom Puzzle rule book driven through the real main menu: a tab per family, a tile per
    // rule, a rule that does not fit the board says why, chosen rules sit as chips in the tray and leave again on a click.
    [TestFixture]
    [Requirement("UI-STYLE-012")]
    public sealed class CustomPuzzleScreenPlayModeTests
    {
        private readonly List<GameObject> _createdRoots = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var i = _createdRoots.Count - 1; i >= 0; i--)
                if (_createdRoots[i] != null) Object.Destroy(_createdRoots[i]);
            _createdRoots.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuleBook_TabsTilesAndTray_ChooseAndRemoveRules()
        {
            var host = BuildMainMenu();
            yield return null;
            ClickButton(host, "BtnTutorial");
            yield return null;
            var panel = FindObject(host, "TutorialSetupPanel");
            Assert.IsTrue(panel.activeInHierarchy, "Custom Puzzle opens");
            var screen = panel.GetComponent<CustomPuzzleScreen>();
            var setup = host.GetComponentInChildren<TutorialMenuController>(true);

            foreach (var family in ModifierFamilies.Order)
                Assert.NotNull(FindObjectOrNull(panel, $"FamilyTab_{family}"), $"a tab for {family}");
            foreach (var id in ModifierFamilies.Get(ModifierFamily.Lines))
                Assert.NotNull(FindObjectOrNull(panel, $"RuleTile_{id}"), $"the Lines page shows {id}");

            // 5×5 by default: German Whispers needs 7×7, so choosing it explains why instead.
            ClickButton(panel, $"RuleTile_{BossModifierId.GermanWhispers}");
            Assert.IsEmpty(setup.SelectedModifiers);
            StringAssert.Contains("7", FindObject(panel, "RuleHint").GetComponent<Text>().text, "the hint gives the reason");

            for (var i = 0; i < 4; i++) ClickButton(panel, "SizeInc");
            Assert.AreEqual(9, setup.GetBoardSize());
            Assert.AreEqual("9×9", FindObject(panel, "SizeValue").GetComponent<Text>().text);
            ClickButton(panel, $"RuleTile_{BossModifierId.DutchWhispers}");
            yield return null;
            Assert.IsTrue(setup.IsSelected(BossModifierId.DutchWhispers));
            Assert.IsTrue(FindObject(FindObject(panel, $"RuleTile_{BossModifierId.DutchWhispers}"), "Enso").GetComponent<Image>().enabled,
                "a chosen rule gets the ensō");
            Assert.NotNull(FindObjectOrNull(FindObject(panel, "RuleChip_0"), "Icon"), "and a chip in the tray");

            ClickButton(panel, $"FamilyTab_{ModifierFamily.OutsideClues}");
            yield return null;
            Assert.AreEqual(ModifierFamily.OutsideClues, screen.CurrentFamily);
            Assert.IsNull(FindObjectOrNull(panel, $"RuleTile_{BossModifierId.DutchWhispers}"), "one family at a time");
            ClickButton(panel, $"RuleTile_{BossModifierId.Skyscrapers}");
            screen.StepFamily(1);   // RB from the last tab wraps to Lines
            yield return null;
            Assert.AreEqual(ModifierFamily.Lines, screen.CurrentFamily);

            Assert.AreEqual(2, setup.SelectedModifiers.Count);
            Assert.IsTrue(FindObject(panel, "BtnTutStart").GetComponent<Button>().interactable, "a valid setup can start");
            ClickButton(panel, "RuleChip_0");   // Dutch Whispers leaves again
            yield return null;
            CollectionAssert.AreEqual(new[] { BossModifierId.Skyscrapers }, setup.SelectedModifiers);
            Assert.IsNull(FindObjectOrNull(FindObject(panel, "RuleChip_1"), "Icon"), "the second slot is empty again");

            ClickButton(panel, "BtnTutBack");
            yield return null;
            Assert.IsFalse(panel.activeInHierarchy, "Back closes the rule book");
        }

        private GameObject BuildMainMenu()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("CustomPuzzleEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("CustomPuzzleMainMenuHost");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = FindObject(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            host.GetComponent<MainMenuController>().ShowMainMenu();
            return host;
        }

        private static void ClickButton(GameObject root, string name)
        {
            var go = FindObject(root, name);
            Assert.IsTrue(go.activeInHierarchy, $"Button is not active in hierarchy: {name}");
            var button = go.GetComponent<Button>();
            Assert.IsTrue(button.interactable, $"Button is not interactable: {name}");
            button.onClick.Invoke();
        }

        private static GameObject FindObject(GameObject root, string name)
        {
            var go = FindObjectOrNull(root, name);
            Assert.NotNull(go, $"Generated object not found: {name}");
            return go;
        }

        private static GameObject FindObjectOrNull(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
