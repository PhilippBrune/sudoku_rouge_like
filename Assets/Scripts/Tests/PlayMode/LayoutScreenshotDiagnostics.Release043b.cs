using System.Collections;
using System.Linq;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // Before/after shots of the 0.4.2 playtest fixes (the owner's Daily Walk run, Paul's notes). Only APIs 0.4.3's first
    // build has too, so the same scenes render the "before" on that build.
    public sealed partial class LayoutScreenshotDiagnostics
    {
        // Paul: the chosen cell among highlighted digits; the owner: "comb / o x11!" at 150 % Text Size.
        [UnityTest]
        public IEnumerator RenderChosenCellAndComboAt150()
        {
            var camera = CreateCamera();
            TextSizeFit.SetScale(1.5f);
            var (host, run) = StartRun(7441, true);
            yield return DismissIntro();
            yield return new WaitForSecondsRealtime(0.5f);
            var board = run.CurrentBoard;
            run.State.ComboStreak = 10;
            var empty = Enumerable.Range(0, board.Size * board.Size).Select(i => (r: i / board.Size, c: i % board.Size))
                .First(p => board.Cells[p.r, p.c] == 0);
            ClickCell(host, empty.r, empty.c);
            yield return null;
            var digit = board.Solution[empty.r, empty.c];
            host.GetComponentInChildren<InRunController>(true).SendMessage("EnterNumber", digit, SendMessageOptions.DontRequireReceiver);
            yield return null;
            ClickCell(host, empty.r, empty.c);   // the cell with the digit, among its highlighted twins
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, "rel043b_cell_combo150.png");
            TextSizeFit.SetScale(1f);
            host.SetActive(false);
        }

        // The owner: the Koi Pond Wager felt like nothing happened.
        [UnityTest]
        public IEnumerator RenderEventResult()
        {
            var camera = CreateCamera();
            for (var seed = 6000; seed < 6400; seed++)
            {
                var probe = new RunDirector();
                probe.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
                var e = probe.BuildCurrentEvent();
                if (e == null || !e.Options.Any(o => o.GoldCost > 0)) continue;
                var (host, run) = StartRun(seed, false);
                yield return DismissIntro();
                run.State.CurrentGold = 120;
                var flow = host.GetComponentInChildren<InRunUiFlowController>(true);
                flow.OnNodeEntered(NodeType.Event);
                yield return null;
                var paid = run.CurrentEvent.Options.FindIndex(o => o.GoldCost > 0);
                flow.EventChoiceScreen.SelectChoiceByIndex(paid);
                flow.EventChoiceScreen.SelectChoiceByIndex(paid);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shoot(host, camera, "rel043b_event_result.png");
                host.SetActive(false);
                yield break;
            }
        }

        // The owner: the shop closed after the first purchase.
        [UnityTest]
        public IEnumerator RenderShopAfterAPurchase()
        {
            var camera = CreateCamera();
            var (host, run) = StartRun(7442, false);
            yield return DismissIntro();
            run.State.CurrentGold = 400;
            var shop = host.GetComponentInChildren<ShopViewController>(true);
            shop.ShowShop();
            yield return new WaitForSecondsRealtime(0.5f);
            var first = run.CurrentShopOffers.FindIndex(o => o?.Item != null);
            shop.SelectSlotByIndex(first);
            var buy = host.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "ShopBuyBtn");
            buy?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "rel043b_shop_after_buy.png");
            host.SetActive(false);
        }

        // The owner: after a rest the path still showed the old HP.
        [UnityTest]
        public IEnumerator RenderPathAfterRest()
        {
            var camera = CreateCamera();
            var (host, run) = StartRun(7443, false);
            yield return DismissIntro();
            run.State.CurrentHP = 2;
            var reward = host.GetComponentInChildren<RewardViewController>(true);
            reward.ShowRestChoicePanel();
            yield return null;
            reward.SelectRestByIndex(0);
            reward.SelectRestByIndex(0);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, "rel043b_path_after_rest.png");
            host.SetActive(false);
        }

        private static void ClickCell(GameObject host, int r, int c)
        {
            var cell = host.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == $"C_{r}_{c}");
            cell?.onClick.Invoke();
        }
    }
}
