using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // RM-104 (owner's picks 2026-11-04): the new relics, items and events.
    [TestFixture]
    public sealed class Rm104ContentTests
    {
        private static RunState State(params RelicId[] relics) => new RunState
        {
            MaxHP = 10, CurrentHP = 10, MaxPencil = 20, CurrentPencil = 20, ItemSlots = 3,
            HeldRelics = relics.Select(id => new RelicInstance { Id = id }).ToList(),
            HeldItems = new List<ItemInstance>(), ActiveCurseIds = new List<string>(), RouteHistory = new List<int>()
        };

        private static ItemInstance Item(ItemType type, ItemRarity rarity) =>
            new ItemInstance { Id = type.ToString(), Type = type, Rarity = rarity, Charges = 1 };

        [Test]
        [Requirement("RELIC-PACK-001")]
        public void CrackedBell_TakesTwoMaxHp_AndEveryBossGivesTwoBack()
        {
            var s = State();
            var bell = new RelicInstance { Id = RelicId.CrackedBell };
            s.HeldRelics.Add(bell);
            RelicService.ApplyPickupPassives(s, bell);
            Assert.AreEqual((8, 8), (s.MaxHP, s.CurrentHP));
            RelicService.OnBossDefeated(s);
            Assert.AreEqual((10, 10, 1), (s.MaxHP, s.CurrentHP, s.BossesDefeatedThisRun));
            var plain = State();
            RelicService.OnBossDefeated(plain);
            Assert.AreEqual((10, 1), (plain.MaxHP, plain.BossesDefeatedThisRun), "without the bell a boss only counts");
        }

        [Test]
        [Requirement("RELIC-PACK-001")]
        public void TwinKoi_GivesEveryReceivedItemOneMoreCharge()
        {
            Assert.AreEqual(2, RelicService.OnItemReceived(State(RelicId.TwinKoi), Item(ItemType.InkWell, ItemRarity.Normal)).Charges);
            Assert.AreEqual(1, RelicService.OnItemReceived(State(), Item(ItemType.InkWell, ItemRarity.Normal)).Charges);
        }

        [Test]
        [Requirement("RELIC-PACK-001")]
        public void GoldLeafBrush_ReadiesTheNextRewardAtACombOfTen_OncePerPuzzle()
        {
            var s = State(RelicId.GoldLeafBrush);
            s.ComboStreak = 9;
            RelicService.OnCorrectPlacement(s);
            Assert.IsFalse(s.GoldLeafReady);
            s.ComboStreak = 10;
            RelicService.OnCorrectPlacement(s);
            Assert.IsTrue(s.GoldLeafReady);
            s.GoldLeafReady = false;   // the reward took it
            s.ComboStreak = 20;
            RelicService.OnCorrectPlacement(s);
            Assert.IsFalse(s.GoldLeafReady, "once per puzzle");
        }

        [Test]
        [Requirement("RELIC-PACK-001")]
        public void LanternOfDaring_PaysByTheRouteOfTheTileJustSolved()
        {
            var s = State(RelicId.LanternOfDaring);
            s.RouteHistory.Add((int)RouteType.RiskRoute);
            var gold = 40;
            RelicService.OnPuzzleComplete(s, new LevelState(), ref gold);
            Assert.AreEqual(60, gold);
            s.RouteHistory.Add((int)RouteType.CalmRoute);
            gold = 40;
            RelicService.OnPuzzleComplete(s, new LevelState(), ref gold);
            Assert.AreEqual(32, gold);
        }

        [Test]
        [Requirement("RELIC-TRADE-001")]
        public void OniPact_MistakesCostMore_PuzzlesPayMore_AndShowAnotherReward()
        {
            var s = State(RelicId.OniPact);
            Assert.AreEqual(3, RelicService.MistakeDamage(s, 2));
            var gold = 40;
            RelicService.OnPuzzleComplete(s, new LevelState(), ref gold);
            Assert.AreEqual(60, gold);
            Assert.AreEqual(1, RelicService.GetBonusRewardSlots(s));
        }

        [Test]
        [Requirement("RELIC-TRADE-001")]
        public void DryInkstone_TakesFivePencil_AndEveryComboOfFiveHeals()
        {
            var s = State();
            var stone = new RelicInstance { Id = RelicId.DryInkstone };
            s.HeldRelics.Add(stone);
            RelicService.ApplyPickupPassives(s, stone);
            Assert.AreEqual((15, 15), (s.MaxPencil, s.CurrentPencil));
            s.CurrentHP = 5;
            s.ComboStreak = 5;
            RelicService.OnCorrectPlacement(s);
            Assert.AreEqual(6, s.CurrentHP);
            RelicService.OnCorrectPlacement(s, comboUp: false);
            Assert.AreEqual(6, s.CurrentHP, "a fogged digit leaves the combo at 5: no second heal");
        }

        [Test]
        [Requirement("RELIC-TRADE-001")]
        public void SmoulderingIncense_CostsAnHpAtPuzzleStart_NeverTheLast_AndSolvesABox()
        {
            var size = 4;
            var solution = new int[size, size];
            var regions = new int[size, size];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                solution[r, c] = (r * 2 + r / 2 + c) % size + 1;
                regions[r, c] = (r / 2) * 2 + c / 2;
            }
            var board = new SudokuBoard(size, solution, new int[size, size], regions);
            var s = State(RelicId.SmoulderingIncense);
            s.CurrentHP = 1;
            RelicService.OnPuzzleStart(s, board);
            Assert.AreEqual(1, s.CurrentHP, "never below 1");
            var filled = 0;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (board.Cells[r, c] != 0) filled++;
            Assert.AreEqual(size, filled, "one box solved");
        }

        [Test]
        [Requirement("ITEM-PACK-001")]
        public void SeedPouch_RipensAfterTwoSolvedPuzzles_IntoARareItem()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1 }, 9001);
            run.State.HeldRelics.Clear();
            run.State.HeldItems.Clear();
            run.PlantSeedPouch();
            for (var puzzle = 0; puzzle < 2; puzzle++)
            {
                Assert.IsEmpty(run.State.HeldItems, $"nothing before puzzle {puzzle + 1} is solved");
                run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 2, Seed = 5 + puzzle, MissingPercent = StarDensityService.MissingPercentForStars(2) });
                var board = run.CurrentBoard;
                for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.Cells[r, c] == 0 && !board.IsGiven(r, c)) run.PlaceNumber(r, c, board.Solution[r, c]);
                run.CompleteLevelAndGrantRewards();
            }
            Assert.AreEqual(1, run.State.HeldItems.Count, "the pouch ripened");
            Assert.AreNotEqual(ItemRarity.Normal, run.State.HeldItems[0].Rarity);
            Assert.IsEmpty(run.State.SeedPouchTimers);
            Assert.AreSame(run.State.HeldItems[0], run.LastHarvest, "the reward names it");
        }

        [Test]
        [Requirement("RELIC-PACK-001")]
        public void OneTierUp_RaisesEveryRarityByOneStep()
        {
            var items = new ItemService(7);
            Assert.AreEqual(ItemRarity.Rare, items.OneTierUp(Item(ItemType.InkWell, ItemRarity.Normal), 1).Rarity);
            var epic = items.OneTierUp(Item(ItemType.InkWell, ItemRarity.Rare), 1);
            Assert.AreEqual((ItemType.InkWell, ItemRarity.Epic), (epic.Type, epic.Rarity), "a tiered item keeps its type");
            Assert.AreEqual(ItemRarity.Epic, items.OneTierUp(Item(ItemType.GinkgoLeaf, ItemRarity.Rare), 1).Rarity);
            var top = Item(ItemType.SilkFan, ItemRarity.Epic);
            Assert.AreSame(top, items.OneTierUp(top, 1));
        }

        private static string Resolve(RunState s, EventOption option, int seed = 3)
        {
            var events = new RunEventService(seed);
            events.ReseedForDecision(seed);
            return events.ResolveChoice(s, new RunEvent { Options = new List<EventOption> { option } }, 0);
        }

        [Test]
        [Requirement("EVENT-PACK-001")]
        public void KoiPondWager_IsHalfAndHalf_AndTheSameForTheSameSeed()
        {
            var outcomes = new HashSet<int>();
            for (var seed = 0; seed < 40; seed++)
            {
                var s = State();
                s.CurrentGold = 100;
                Resolve(s, new EventOption { GoldCost = 20, WagerGold = 50 }, seed);
                outcomes.Add(s.CurrentGold);
                var again = State();
                again.CurrentGold = 100;
                Resolve(again, new EventOption { GoldCost = 20, WagerGold = 50 }, seed);
                Assert.AreEqual(s.CurrentGold, again.CurrentGold, "the choice's seed decides");
            }
            CollectionAssert.AreEquivalent(new[] { 80, 130 }, outcomes);
        }

        [Test]
        [Requirement("EVENT-PACK-001")]
        public void WanderingMonk_TradesTheWeakestItemUp_AndNeedsAnItem()
        {
            var s = State();
            Assert.AreEqual("InRun.Event.NoItem", RunEventService.PackBlockKey(new EventOption { UpgradeItem = true }, s));
            s.HeldItems.Add(Item(ItemType.InkWell, ItemRarity.Rare));
            s.HeldItems.Add(Item(ItemType.Finder, ItemRarity.Normal));
            Resolve(s, new EventOption { UpgradeItem = true });
            Assert.AreEqual((ItemType.Finder, ItemRarity.Rare), (s.HeldItems[1].Type, s.HeldItems[1].Rarity));
            Assert.AreEqual(ItemRarity.Rare, s.HeldItems[0].Rarity, "the stronger one stays");
        }

        [Test]
        [Requirement("EVENT-TRADE-001")]
        public void BoneCarver_TakesMaxHp_AndIsBlockedWhenItWouldTakeTheLast()
        {
            var s = State();
            s.MaxHP = s.CurrentHP = 2;
            Assert.AreEqual("InRun.Event.NeedMaxHp", RunEventService.PackBlockKey(new EventOption { MaxHpCost = 2 }, s));
            s.MaxHP = s.CurrentHP = 10;
            Assert.IsNull(RunEventService.PackBlockKey(new EventOption { MaxHpCost = 2 }, s));
            Resolve(s, new EventOption { MaxHpCost = 1, GrantRareItem = true });
            Assert.AreEqual((9, 9), (s.MaxHP, s.CurrentHP));
            Assert.AreEqual(1, s.HeldItems.Count);
            Assert.AreNotEqual(ItemRarity.Normal, s.HeldItems[0].Rarity);
        }

        [Test]
        [Requirement("EVENT-TRADE-001")]
        public void BloodLantern_LiftsCurses_AndOnlyComesWhileACurseIsActive()
        {
            var s = State();
            Assert.AreEqual("InRun.Event.NoCurse", RunEventService.PackBlockKey(new EventOption { RemoveCurses = 1 }, s));
            s.ActiveCurseIds.AddRange(new[] { "inkblot", "misfortune" });
            Resolve(s, new EventOption { HpCost = 3, RemoveCurses = -1 });
            Assert.IsEmpty(s.ActiveCurseIds);
            Assert.AreEqual(7, s.CurrentHP);

            var keys = new HashSet<string>();
            var cursed = State();
            cursed.ActiveCurseIds.Add("inkblot");
            var seenWithCurse = false;
            for (var seed = 0; seed < 300; seed++)
            {
                keys.Add(new RunEventService(seed).BuildEvent(2, 3, State()).Key);
                seenWithCurse |= new RunEventService(seed).BuildEvent(2, 3, cursed).Key == "blood_lantern";
            }
            CollectionAssert.DoesNotContain(keys, "blood_lantern");
            Assert.IsTrue(seenWithCurse);
            CollectionAssert.IsSubsetOf(new[] { "koi_pond_wager", "fallen_lantern", "wandering_monk", "bone_carver", "ink_spring", "ghost_merchant" }, keys);
        }

        [Test]
        [Requirement("EVENT-TRADE-001")]
        public void Trades_AreBlockedWhenTheyCannotBePaidInFull()
        {
            var s = State();
            s.CurrentHP = 2;

            Assert.AreEqual("InRun.Event.NeedHp", RunEventService.PackBlockKey(new EventOption { HpCost = 2, HpCostBlocks = true }, s));
            Assert.IsNull(RunEventService.PackBlockKey(new EventOption { HpCost = 2 }, s), "the older events still stop at 1 HP");
            s.CurrentHP = 10;
            for (var i = 0; i < s.ItemSlots; i++) s.HeldItems.Add(Item(ItemType.SilkFan, ItemRarity.Epic));
            Assert.AreEqual("InRun.Event.BagFull", RunEventService.PackBlockKey(new EventOption { MaxHpCost = 1, GrantRareItem = true }, s));
            Assert.AreEqual("InRun.Event.NothingToRaise", RunEventService.PackBlockKey(new EventOption { UpgradeItem = true }, s));
            foreach (var key in new[] { "ghost_merchant", "blood_lantern", "ink_spring", "fallen_lantern" })
                for (var seed = 0; seed < 400; seed++)
                {
                    var built = new RunEventService(seed).BuildEvent(2, 3, State());
                    if (built.Key != key) continue;
                    Assert.IsTrue(built.Options.Where(o => o.HpCost > 0).All(o => o.HpCostBlocks), key + ": an HP trade is paid in full");
                    break;
                }
        }

        [Test]
        [Requirement("ITEM-REFUND-001")]
        public void RefundItem_PutsTheItemBack_AndDoesNotCountTheUse()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1 }, 9001);
            run.State.HeldRelics.Clear();
            run.State.HeldItems.Clear();
            var incense = Item(ItemType.TempleIncense, ItemRarity.Rare);
            var well = Item(ItemType.InkWell, ItemRarity.Normal);
            run.State.HeldItems.Add(well);
            run.State.HeldItems.Add(incense);
            var usedBefore = run.State.ItemsUsedThisRun;
            Assert.IsTrue(run.TryUseItem(1));
            CollectionAssert.DoesNotContain(run.State.HeldItems, incense, "a single charge empties the slot");
            run.RefundItem(1, incense, 1);
            Assert.AreSame(incense, run.State.HeldItems[1], "back in its slot");
            Assert.AreEqual(1, incense.Charges);
            Assert.AreEqual(usedBefore, run.State.ItemsUsedThisRun, "the use is not counted");

            incense.Charges = 2;
            Assert.IsTrue(run.TryUseItem(1));
            run.RefundItem(1, incense, 2);
            Assert.AreEqual(2, run.State.HeldItems.Count, "an item with charges left is not added twice");
            Assert.AreEqual(2, incense.Charges);
        }

        [Test]
        [Requirement("ITEM-PACK-001")]
        public void SeedPouch_AtMostThreeGrowAtOnce()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1 }, 9001);
            Assert.IsTrue(run.PlantSeedPouch() && run.PlantSeedPouch() && run.PlantSeedPouch());
            Assert.IsFalse(run.PlantSeedPouch(), "an item that is never used up cannot plant without end");
            Assert.AreEqual(RunDirector.SeedPouchesAtOnce, run.State.SeedPouchTimers.Count);
        }

        [Test]
        [Requirement("EVENT-TRADE-001")]
        public void InkSpring_RaisesMaxPencil()
        {
            var s = State();
            s.CurrentPencil = 5;
            Resolve(s, new EventOption { HpCost = 2, PencilGain = 10, MaxPencilGain = 2 });
            Assert.AreEqual((22, 17, 8), (s.MaxPencil, s.CurrentPencil, s.CurrentHP));
        }
    }
}
