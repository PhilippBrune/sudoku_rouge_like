using System.Collections;
using System.Linq;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // The 0.4.3 shots (0.4.1 playtest on a Legion Go): the chosen path tile on every floor (UI-PATH-FOCUS-001, RUN-FLOORS-001)
    // and the bag-full screen (UI-STYLE-044). Uses only what 0.4.2 has too, so the same scene gives the "before" shots.
    public sealed partial class LayoutScreenshotDiagnostics
    {
        [UnityTest]
        public IEnumerator RenderPathFocusOnEveryFloor()
        {
            var camera = CreateCamera();
            for (var floor = 0; ; floor++)
            {
                var target = floor;
                _beforeBindAlways = run =>
                {
                    for (var f = 0; f < target; f++) run.AdvanceToNextFloor();
                    run.State.HeldRelics.Clear();
                };
                var (host, run) = StartRun(7430, false);
                _beforeBindAlways = null;
                yield return DismissIntro();
                yield return new WaitForSecondsRealtime(2.0f);
                var reachable = run.GetReachableNodes();
                var tile = host.GetComponentsInChildren<Button>()
                    .FirstOrDefault(b => b.name.StartsWith("Node_") && reachable.Any(i => b.name.EndsWith("_" + i)) && !b.name.Contains("Start"));
                if (tile != null) EventSystem.current?.SetSelectedGameObject(tile.gameObject);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shoot(host, camera, $"rel043_path_floor{floor + 1}.png");
                host.SetActive(false);
                if (run.State.CurrentFloor + run.State.FloorStep >= 5) break;   // the summit is the last floor on both layouts
            }
        }

        [UnityTest]
        public IEnumerator RenderBagFullSwap()
        {
            var camera = CreateCamera();
            for (var seed = 8606; seed < 8640; seed++)
            {
                var (host, run) = StartRun(seed, true);
                yield return DismissIntro();
                var state = run.State;
                state.HeldItems.Clear();
                var bag = new[] { ItemType.InkWell, ItemType.MeditationStone, ItemType.GinkgoLeaf };
                for (var i = 0; i < state.ItemSlots; i++)
                    state.HeldItems.Add(new ItemInstance { Id = "full-" + i, Type = bag[i % bag.Length], Rarity = ItemRarity.Normal, Charges = 1 });
                var board = run.CurrentBoard;
                for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.Cells[r, c] == 0) board.PlaceValue(r, c, board.Solution[r, c]);
                var reward = host.GetComponentInChildren<RewardViewController>(true);
                for (var waited = 0f; waited < 3f && !reward.IsAwaitingReward; waited += Time.unscaledDeltaTime)
                    yield return null;
                if (!reward.IsAwaitingReward) reward.ShowRewardScreen();
                var slot = run.RolledItemSlots?.FindIndex(s => s != null) ?? -1;
                if (slot < 0 || slot > 2) { host.SetActive(false); continue; }
                reward.SelectRewardByIndex(slot);
                reward.SelectRewardByIndex(slot);
                yield return new WaitForSecondsRealtime(0.6f);
                if (!reward.IsSwapOpen) { host.SetActive(false); continue; }
                yield return Shoot(host, camera, "rel043_bag_swap.png");
                host.SetActive(false);
                yield break;
            }
            Debug.LogWarning("[LayoutShot] no seed rolled an item in the first three plaques");
        }
    }
}
