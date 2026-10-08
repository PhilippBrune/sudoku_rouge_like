using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-BOSS-107] [REQ: REQ-UI-107] [REQ: MAP-STRUCT-001]
    // GAP-002: after a non-final boss the player must see a labelled Continue button, then the
    // relic choice, and reach the next floor. Drives the real InRunController completion flow.
    [TestFixture]
    [Requirement("REQ-BOSS-107", "REQ-UI-107", "MAP-STRUCT-001")]
    public sealed class BossClearedFlowPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalActiveSlot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            DeleteSlotFiles();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            for (var i = _createdRoots.Count - 1; i >= 0; i--)
                if (_createdRoots[i] != null) Object.Destroy(_createdRoots[i]);
            _createdRoots.Clear();
            Time.timeScale = 1f;
            DeleteSlotFiles();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossCleared_ContinueIsLabelledThenRelicChoiceIsVisibleThenNextFloorLoads()
        {
            var host = BuildInRunHarness();
            yield return null;

            var map = host.GetComponentInChildren<RunMapController>(true);
            var runScreen = host.GetComponentInChildren<InRunController>(true);
            Assert.NotNull(map, "RunMapController missing from in-run harness");
            Assert.NotNull(runScreen, "InRunController missing from in-run harness");

            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = false,
                ClassLevel = 1
            }, 8101);
            run.ChooseBossModifiers(new List<BossModifierId>());
            var boss = run.CurrentFloorGraph.First(n => n.Type == NodeType.Boss);
            run.TryAdvanceToNode(boss.Index, forced: true);
            run.StartLevel(run.BuildLevelConfig(true, false, boss.Index));
            Assert.NotNull(run.CurrentBoard, "boss generation failed for the test seed");

            map.BindRun(run, new SaveFileService(TestSlot));
            runScreen.NotifyRunStarted();
            yield return null;
            yield return null;
            Assert.IsTrue(FindObject(host, "SudokuGameplayPanel").activeInHierarchy, "boss puzzle should be shown");

            var relicsBefore = run.State.HeldRelics.Count;
            FillWithSolution(run);

            // HandleCompletion runs in Update, then the reward coroutine waits 0.35 s realtime.
            yield return WaitFor(() => FindObjectOrNull(host, "BossClearedContinueBtn") != null, 3f);
            var continueGo = FindObjectOrNull(host, "BossClearedContinueBtn");
            Assert.NotNull(continueGo, "Boss-cleared Continue button was not created");
            Assert.IsTrue(continueGo.activeInHierarchy, "Continue button must be visible");

            var label = continueGo.GetComponentInChildren<Text>(true);
            Assert.NotNull(label, "Continue button has no label");
            Assert.NotNull(label.font, "Continue label has no font, so it renders blank in a player build");
            Assert.IsFalse(string.IsNullOrWhiteSpace(label.text), "Continue label text is empty");
            Assert.AreEqual(continueGo, EventSystem.current.currentSelectedGameObject,
                "Continue must be selected for keyboard/controller users");

            continueGo.GetComponent<Button>().onClick.Invoke();
            yield return null;

            var relicPanel = FindObjectOrNull(host, "RelicChoicePanel");
            Assert.NotNull(relicPanel, "Relic choice panel was not created");
            Assert.IsTrue(relicPanel.activeInHierarchy, "Relic choice panel must be visible after Continue");
            Assert.IsFalse(FindObject(host, "SudokuGameplayPanel").activeInHierarchy,
                "The solved boss board must not cover the relic choice");

            ClickButton(relicPanel, "RelicBtn_0");
            yield return null;
            ClickButton(relicPanel, "BtnAccept");
            yield return null;
            yield return null;

            Assert.AreEqual(relicsBefore + 1, run.State.HeldRelics.Count, "accepted relic must be added");
            Assert.AreEqual(1, run.State.CurrentFloor, "run must advance to floor 2");
            Assert.IsTrue(FindObject(host, "PathOverviewPanel").activeInHierarchy, "path overview of the new floor");
        }

        // ── helpers ──

        private GameObject BuildInRunHarness()
        {
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("BossFlowCamera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera";
                camera.GetComponent<Camera>().orthographic = true;
                _createdRoots.Add(camera);
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("BossFlowEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));

            var host = new GameObject("BossClearedFlowInRunHost");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();
            Assert.NotNull(FindObjectOrNull(host, "InRunUI"));
            return host;
        }

        private static void FillWithSolution(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (!board.GivenMask[r, c]) board.PlaceValue(r, c, board.Solution[r, c]);
        }

        private static IEnumerator WaitFor(Func<bool> condition, float timeoutSeconds)
        {
            var end = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }

        private static void ClickButton(GameObject root, string name)
        {
            var go = FindObjectOrNull(root, name);
            Assert.NotNull(go, $"Button not found: {name}");
            var button = go.GetComponent<Button>();
            Assert.IsTrue(go.activeInHierarchy, $"Button is not active in hierarchy: {name}");
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

        private static void DeleteSlotFiles()
        {
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }
    }
}
