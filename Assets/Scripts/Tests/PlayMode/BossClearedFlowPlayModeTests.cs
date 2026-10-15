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
    // [REQ: REQ-BOSS-107] [REQ: REQ-UI-107] [REQ: MAP-STRUCT-001] [REQ: UI-STYLE-005]
    // GAP-002 and the offering table: after a non-final boss the player sees the fight's numbers and the relic
    // offerings on one screen, takes one (or leaves them) and reaches the next floor. Drives the real InRunController
    // completion flow.
    [TestFixture]
    [Requirement("REQ-BOSS-107", "REQ-UI-107", "MAP-STRUCT-001", "UI-STYLE-005")]
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
        public IEnumerator BossDefeated_ShowsTheOfferingsOnOneScreen_TakingOneLoadsTheNextFloor()
        {
            var flow = new Flow();
            yield return SolveFirstBoss(flow);
            var host = flow.Host;

            var screen = FindObject(host, "BossDefeatedScreen");
            Assert.IsTrue(screen.activeInHierarchy, "the offering table must be visible");
            Assert.IsFalse(FindObject(host, "SudokuGameplayPanel").activeInHierarchy,
                "the solved board is hidden, so its keys and shortcuts are off");
            Assert.IsFalse(FindObject(host, "GameOverPanel").activeInHierarchy,
                "the game-over panel stays closed: its save rules would clear the run");
            Assert.NotNull(FindObjectOrNull(screen, "BossClearedStats"), "the fight's numbers are on the same screen");

            var take = FindObject(screen, "OfferingTake_0");
            var label = take.GetComponentInChildren<Text>(true);
            Assert.NotNull(label.font, "Take label has no font, so it renders blank in a player build");
            Assert.IsFalse(string.IsNullOrWhiteSpace(label.text), "Take label text is empty");
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.NotNull(selected, "a Take button must be selected for keyboard/controller users");
            StringAssert.StartsWith("OfferingTake_", selected.name);

            ClickButton(screen, "OfferingTake_0");
            FindObject(screen, "OfferingTake_1").GetComponent<Button>().onClick.Invoke(); // a second click is ignored
            yield return null;
            yield return null;

            Assert.AreEqual(flow.RelicsBefore + 1, flow.Run.State.HeldRelics.Count, "exactly one offering is taken");
            Assert.AreEqual(1, flow.Run.State.CurrentFloor, "run must advance to floor 2");
            Assert.IsNull(FindObjectOrNull(host, "BossDefeatedScreen"), "the offering table closes");
            Assert.IsTrue(FindObject(host, "PathOverviewPanel").activeInHierarchy, "path overview of the new floor");
        }

        [UnityTest]
        public IEnumerator BossDefeated_LeavingTheOfferings_LoadsTheNextFloorWithoutARelic()
        {
            var flow = new Flow();
            yield return SolveFirstBoss(flow);

            ClickButton(FindObject(flow.Host, "BossDefeatedScreen"), "BtnLeaveOfferings");
            yield return null;
            yield return null;

            Assert.AreEqual(flow.RelicsBefore, flow.Run.State.HeldRelics.Count);
            Assert.AreEqual(1, flow.Run.State.CurrentFloor);
            Assert.IsTrue(FindObject(flow.Host, "PathOverviewPanel").activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator BossDefeated_WithoutOfferings_HasOneSelectedContinueButton()
        {
            var host = BuildInRunHarness();
            yield return null;
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 8102);
            var continued = 0;
            host.GetComponentInChildren<EndScreenViewController>(true)
                .ShowBossCleared(run, new List<RelicInstance>(), _ => Assert.Fail("nothing to take"), () => continued++);
            yield return null;

            var continueGo = FindObject(host, "BossClearedContinueBtn");
            Assert.AreEqual(continueGo, EventSystem.current.currentSelectedGameObject);
            Assert.IsNull(FindObjectOrNull(host, "OfferingTake_0"));
            ClickButton(host, "BossClearedContinueBtn");
            Assert.IsFalse(continueGo.GetComponent<Button>().interactable, "Continue locks after the first click");
            Assert.AreEqual(1, continued);
        }

        private sealed class Flow
        {
            public GameObject Host;
            public RunDirector Run;
            public int RelicsBefore;
        }

        // Starts a run on the Floor 1 boss, solves it and waits for the offering table.
        private IEnumerator SolveFirstBoss(Flow flow)
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

            flow.Host = host;
            flow.Run = run;
            flow.RelicsBefore = run.State.HeldRelics.Count;
            FillWithSolution(run);

            // HandleCompletion runs in Update, then the reward coroutine waits 0.35 s realtime.
            yield return WaitFor(() => FindObjectOrNull(host, "OfferingTake_0") != null, 3f);
            Assert.NotNull(FindObjectOrNull(host, "OfferingTake_0"), "the offering table did not open");
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
