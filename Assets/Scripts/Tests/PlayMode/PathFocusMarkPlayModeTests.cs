using System.Collections;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // Playtest 0.4.1 on a Legion Go (owner's pick A): the chosen path tile wears the brush ring and the class marker.
    [TestFixture]
    public sealed class PathFocusMarkPlayModeTests
    {
        [UnityTest]
        [Requirement("UI-PATH-FOCUS-001")]
        public IEnumerator TheRingAndTheMarker_ShowOnlyOnTheChosenTile()
        {
            var root = new GameObject("PathFocusTest", typeof(Canvas));
            var es = Object.FindAnyObjectByType<EventSystem>() == null
                ? new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)) : null;
            try
            {
                Button Tile(string name)
                {
                    var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                    go.transform.SetParent(root.transform, false);
                    ((RectTransform)go.transform).sizeDelta = new Vector2(80f, 80f);
                    return go.GetComponent<Button>();
                }
                var a = Tile("Node_Puzzle_1");
                var b = Tile("Node_Shop_2");
                var markA = PathFocusMark.Attach(a, ClassId.GardenMonk, 80f);
                var markB = PathFocusMark.Attach(b, ClassId.GardenMonk, 80f);
                yield return null;
                Assert.IsFalse(markA.Shown || markB.Shown, "nothing chosen, nothing marked");
                Assert.NotNull(a.transform.Find("FocusMarker/Icon"), "the class's marker");

                EventSystem.current.SetSelectedGameObject(a.gameObject);
                yield return null;
                Assert.IsTrue(markA.Shown);
                Assert.IsFalse(markB.Shown);

                EventSystem.current.SetSelectedGameObject(b.gameObject);
                yield return null;
                Assert.IsFalse(markA.Shown, "the ring follows the choice");
                Assert.IsTrue(markB.Shown);
            }
            finally
            {
                Object.Destroy(root);
                if (es != null) Object.Destroy(es);
            }
        }
    }
}
