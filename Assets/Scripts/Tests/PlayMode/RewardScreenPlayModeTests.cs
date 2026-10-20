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
    // [REQ: UI-STYLE-007] The reward after a puzzle, reached the way the game reaches it: the board is solved, closes, and
    // the reward scene opens over the path.
    // GAP-055: the tally's time read the stopped timer and showed 00:00.
    // GAP-056: number keys on the scenes over the path never arrived, because the keyboard handler runs only while the
    // board is open.
    [TestFixture]
    [Requirement("UI-STYLE-007")]
    public sealed class RewardScreenPlayModeTests
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
            DeleteSlotFiles();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SolvedPuzzle_RewardShowsItsTime_AndKeysChooseThenTake()
        {
            var (host, run) = StartPuzzle(8711);
            for (var frame = 0; frame < 30 && FindObjectOrNull(host, "C_0_0") == null; frame++)
                yield return null;
            yield return new WaitForSecondsRealtime(1.3f);   // the puzzle takes over a second
            run.State.HeldItems.Clear();
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0) board.PlaceValue(r, c, board.Solution[r, c]);

            var reward = host.GetComponentInChildren<RewardViewController>(true);
            for (var waited = 0f; waited < 4f && !reward.IsAwaitingReward; waited += Time.unscaledDeltaTime)
                yield return null;
            Assert.IsTrue(reward.IsAwaitingReward, "the reward opens after the solve");
            Assert.IsFalse(FindObject(host, "SudokuGameplayPanel").activeInHierarchy,
                "the board is closed while the reward is open (why its keys must not depend on it)");

            var time = FindObject(host, "Stat_2_Value").GetComponent<Text>().text;
            Assert.AreNotEqual("00:00", time, "the tally shows the puzzle's time");

            var controller = host.GetComponentInChildren<InRunController>(true);
            Assert.IsTrue(controller.TryHandleSceneDigit(0), "key 1 reaches the reward");
            Assert.IsTrue(reward.IsAwaitingReward, "the first press only chooses");
            controller.TryHandleSceneDigit(0);
            yield return null;
            Assert.IsFalse(reward.IsAwaitingReward, "the same key again takes the plaque");
            Assert.IsFalse(controller.TryHandleSceneDigit(0), "with no scene open the key goes to the board");
        }

        // Number keys never act behind a layer on top: the bag-swap layer swallows them, the in-run Options keep them.
        [UnityTest]
        public IEnumerator SceneKeys_DoNothingUnderTheSwapLayerOrTheOptions()
        {
            var (host, run) = StartPuzzle(8712);
            for (var frame = 0; frame < 30 && FindObjectOrNull(host, "C_0_0") == null; frame++)
                yield return null;
            var state = run.State;
            state.HeldItems.Clear();
            for (var i = 0; i < state.ItemSlots; i++)
                state.HeldItems.Add(new ItemInstance { Id = "full-" + i, Type = ItemType.InkWell, Charges = 1 });
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0) board.PlaceValue(r, c, board.Solution[r, c]);
            var reward = host.GetComponentInChildren<RewardViewController>(true);
            for (var waited = 0f; waited < 4f && !reward.IsAwaitingReward; waited += Time.unscaledDeltaTime)
                yield return null;
            var itemSlot = run.RolledItemSlots?.FindIndex(slot => slot != null) ?? -1;
            if (itemSlot < 0 || itemSlot > 8) Assert.Inconclusive("this seed rolled no item plaque");
            var controller = host.GetComponentInChildren<InRunController>(true);

            var options = FindObject(host, "InGameOptionsPanel");
            options.SetActive(true);
            Assert.IsFalse(controller.TryHandleSceneDigit(itemSlot), "with the Options open the key is theirs");
            options.SetActive(false);

            controller.TryHandleSceneDigit(itemSlot);
            controller.TryHandleSceneDigit(itemSlot);   // take: the bag is full, so the swap layer opens
            yield return null;
            Assert.IsTrue(reward.IsSwapOpen);
            Assert.IsTrue(controller.TryHandleSceneDigit(0), "the key is swallowed under the swap layer");
            Assert.IsTrue(reward.IsSwapOpen, "and nothing behind it reacts");
            Assert.AreEqual(state.ItemSlots, state.HeldItems.Count);
        }

        private (GameObject host, RunDirector run) StartPuzzle(int seed)
        {
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("RewardTestCamera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera";
                _createdRoots.Add(camera);
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("RewardTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("RewardTestHost");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();

            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false,
                ClassLevel = 1 }, seed);
            run.State.HeldRelics.Clear();
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.BoardSize = 5;
            config.RegionVariant = 0;
            config.ActiveModifiers.Clear();
            run.StartLevel(config);
            Assert.NotNull(run.CurrentBoard);
            host.GetComponentInChildren<RunMapController>(true).BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return (host, run);
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
