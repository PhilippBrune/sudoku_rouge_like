using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-RUN-102] [REQ: REQ-RUN-103] Promoted from review diagnostic D12 (GAP-018): boss choices, item
    // rewards, shop offers and relic choices are the same before Save & Quit and after Resume, the same when a screen
    // is rebuilt, and still differ between nodes. Also GAP-021 (relic accept), GAP-036 (full-bag purchase) and
    // GAP-038 (XP log and analytics survive a resume).
    [TestFixture]
    [Requirement("REQ-RUN-102", "REQ-RUN-103")]
    public sealed class DeterminismAcrossResumeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [SetUp]
        [TearDown]
        public void CleanSlot()
        {
            SaveFileService.FlushSharedPendingWrites();
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }

        [Test]
        public void RollsBeforeSaveEqualRollsAfterResume()
        {
            var run = StartPuzzle(14001);
            var boss = Describe(run.RollBossModifierChoices());
            var reward = Describe(run.BuildItemRewardSlots());
            var shop = Describe(run.BuildShopOffers());
            var relics = Describe(run.RollRelicChoices(3));

            var resumed = SaveAndResume(run);

            Assert.AreEqual(boss, Describe(resumed.RollBossModifierChoices()), "boss gate");
            Assert.AreEqual(reward, Describe(resumed.BuildItemRewardSlots()), "item reward");
            Assert.AreEqual(shop, Describe(resumed.BuildShopOffers()), "shop");
            Assert.AreEqual(relics, Describe(resumed.RollRelicChoices(3)), "relic choice");
        }

        [Test]
        public void RebuildingAScreenDoesNotReroll_ButAShopRerollDoes()
        {
            var run = StartPuzzle(14002);
            Assert.AreEqual(Describe(run.BuildItemRewardSlots()), Describe(run.BuildItemRewardSlots()));
            var shop = Describe(run.BuildShopOffers());
            Assert.AreEqual(shop, Describe(run.BuildShopOffers()));

            run.State.RerollTokens = 5;
            var rerolled = new HashSet<string>();
            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(run.RerollShop());
                rerolled.Add(Describe(run.CurrentShopOffers));
            }
            Assert.Greater(rerolled.Count, 1, "rerolls must produce new offers");
        }

        [Test]
        public void RewardsDifferBetweenNodes()
        {
            var run = StartPuzzle(14003);
            var seen = new HashSet<string>();
            for (var depth = 0; depth < 12; depth++)
            {
                run.State.Depth = depth;
                seen.Add(Describe(run.BuildItemRewardSlots()));
            }
            Assert.Greater(seen.Count, 3, "rewards must vary across nodes (F04 saw the same reward every time)");
        }

        [Test]
        [Requirement("REQ-ITEM-104")]
        public void AcceptingTheShownRelic_WorksWithoutTheRolledList()
        {
            var run = StartPuzzle(14004);
            run.State.HeldRelics.Clear();
            run.State.HasRelic = false;   // legacy single-relic fields; a resume migrates them back otherwise
            run.State.HeldRelic = null;
            var shown = run.RollRelicChoices(3);
            var resumed = SaveAndResume(run);
            Assert.IsNull(resumed.RolledRelicChoices, "a resumed run has no rolled list");

            resumed.AcceptRelicChoice(shown[1]);

            Assert.AreEqual(1, resumed.State.HeldRelics.Count);
            Assert.AreEqual(shown[1].Id, resumed.State.HeldRelics[0].Id);
        }

        [Test]
        [Requirement("REQ-ITEM-101")]
        public void FullBag_PurchaseNeedsASlotAndChargesNothingWithoutOne()
        {
            var run = StartPuzzle(14005);
            var offers = run.BuildShopOffers();
            var index = offers.FindIndex(o => o.Item != null);
            Assume.That(index, Is.GreaterThanOrEqualTo(0));
            while (!run.IsBagFull())
                run.AddItemToInventory(new ItemInstance { Type = ItemType.Solver, Rarity = ItemRarity.Normal });
            run.State.CurrentGold = 1000;

            Assert.IsFalse(run.TryPurchaseShopOffer(index), "no slot to replace");
            Assert.AreEqual(1000, run.State.CurrentGold, "nothing charged");

            Assert.IsTrue(run.TryPurchaseShopOffer(index, 0));
            Assert.AreEqual(1000 - offers[index].Price, run.State.CurrentGold);
            Assert.AreEqual(offers[index].Item.Type, run.State.HeldItems[0].Type);
        }

        [Test]
        [Requirement("XP-LOG-001")]
        public void XpLogAndAnalytics_SurviveSaveAndResume()
        {
            // GAP-038: StartRun on resume used to clear the XP log and analytics, so XP and item counts of the tiles
            // played before Save & Quit were lost at run end.
            var run = StartPuzzle(9401);
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0)
                    run.PlaceNumber(r, c, board.Solution[r, c]);
            var entry = run.CompleteLevelAndGrantRewards();
            Assert.Greater(entry.TotalXp, 0);
            run.GetAnalytics().RecordItemUsed();
            var xp = run.GetTotalRunXp();

            var resumed = SaveAndResume(run);

            Assert.AreEqual(xp, resumed.GetTotalRunXp());
            Assert.AreEqual(1, resumed.TileXpLog.Count);
            Assert.AreEqual(1, resumed.GetAnalytics().TotalPuzzlesSolved);
            Assert.AreEqual(1, resumed.GetAnalytics().TotalItemsUsed);
        }

        // ── helpers ──

        private static RunDirector StartPuzzle(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            run.StartLevel(run.BuildNodeLevelConfig(node, node.Index));
            Assert.NotNull(run.CurrentBoard);
            return run;
        }

        private static RunDirector SaveAndResume(RunDirector run)
        {
            var save = new SaveFileService(TestSlot);
            new RunAutoSaveCoordinator(save).Save(run.State, run.ExportPuzzleSaveState());
            save.FlushPendingWrites();
            Assert.IsTrue(new RunResumeService(save).TryResumeFromSave(out var runState, out var puzzleState));
            var resumed = new RunDirector();
            RunResumeService.ApplyToRunDirector(resumed, runState, puzzleState);
            return resumed;
        }

        private static string Describe(IEnumerable<BossModifierId> mods) => string.Join(",", mods);
        private static string Describe(IEnumerable<ItemInstance> items) =>
            string.Join(",", items.Select(i => i == null ? "-" : $"{i.Type}/{i.Rarity}"));
        private static string Describe(IEnumerable<ShopOffer> offers) =>
            string.Join(",", offers.Select(o => $"{o.Item?.Type}/{o.Item?.Rarity}/{o.Price}"));
        private static string Describe(IEnumerable<RelicInstance> relics) =>
            string.Join(",", relics.Select(r => $"{r.Id}/{r.Tier}"));
    }
}
