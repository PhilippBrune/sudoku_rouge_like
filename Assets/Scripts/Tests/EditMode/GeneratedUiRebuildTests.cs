using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class GeneratedUiRebuildTests
    {
        [Test]
        public void ForceRebuild_ReplacesGeneratedRootChildrenAndSlotHandlers()
        {
            var hadEventSystem = Object.FindAnyObjectByType<EventSystem>() != null;
            var host = new GameObject("GeneratedUiRebuildTestHost");

            try
            {
                var builder = host.AddComponent<MainMenuBlueprintBuilder>();

                builder.Build();

                var optionsController = host.GetComponent<OptionsController>();
                var root = FindMainMenuRoot(host);
                Assert.NotNull(optionsController);
                Assert.NotNull(root);

                var firstChildCount = root.childCount;
                Assert.Greater(firstChildCount, 0);
                Assert.AreEqual(3, builder.SlotChangedHandlerRegistrationCount);
                Assert.AreEqual(1, GetSlotChangedHandlerCount(optionsController));
                Assert.AreEqual(2, GetOptionsChangedHandlerCount(optionsController));

                var staleChild = new GameObject("StaleGeneratedChild", typeof(RectTransform));
                staleChild.transform.SetParent(root, false);

                builder.ForceRebuild();

                var rebuiltRoot = FindMainMenuRoot(host);
                Assert.AreSame(root, rebuiltRoot);
                Assert.IsNull(rebuiltRoot.Find("StaleGeneratedChild"));
                Assert.AreEqual(firstChildCount, rebuiltRoot.childCount);
                Assert.AreEqual(3, builder.SlotChangedHandlerRegistrationCount);
                Assert.AreEqual(1, GetSlotChangedHandlerCount(optionsController));
                Assert.AreEqual(2, GetOptionsChangedHandlerCount(optionsController));

                var controllerSource = System.IO.File.ReadAllText("Assets/Scripts/UI/MainMenuController.cs");
                StringAssert.Contains("NotifyOptionsChangedForUiRefresh", controllerSource);
                Assert.That(controllerSource, Does.Not.Contain("RefreshOptionsPanel() => _optionsController?.OnSlotChanged?.Invoke()"));
            }
            finally
            {
                Object.DestroyImmediate(host);

                if (!hadEventSystem)
                {
                    var eventSystem = Object.FindAnyObjectByType<EventSystem>();
                    if (eventSystem != null)
                        Object.DestroyImmediate(eventSystem.gameObject);
                }
            }
        }

        private static RectTransform FindMainMenuRoot(GameObject host)
        {
            var canvas = host.GetComponentInChildren<Canvas>();
            return canvas == null ? null : canvas.transform.Find("MainMenuRoot") as RectTransform;
        }

        private static int GetSlotChangedHandlerCount(OptionsController optionsController)
        {
            return optionsController.OnSlotChanged?.GetInvocationList().Length ?? 0;
        }

        private static int GetOptionsChangedHandlerCount(OptionsController optionsController)
        {
            return optionsController.OnOptionsChanged?.GetInvocationList().Length ?? 0;
        }
    }
}
