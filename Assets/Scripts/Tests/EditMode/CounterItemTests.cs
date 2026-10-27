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
    // Counter items (ItemsAndRelicsSystem.md "Counter Items" (lever L3), owner pick after the 0.2.0 playtest): one item per modifier
    // family writes correct digits into the cells that family's rules use; shops lean toward the counters of the board's
    // rules.
    [TestFixture]
    [Requirement("ITEM-COUNTER-001", "ITEM-COUNTER-002", "ITEM-COUNTER-003", "ITEM-COUNTER-004", "ITEM-COUNTER-005",
        "ITEM-COUNTER-006", "ITEM-COUNTER-007")]
    public sealed class CounterItemTests
    {
        private static RunDirector StartLevel(int size, BossModifierId modifier, int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            run.State.HeldRelics.Clear();
            run.State.CurrentHP = run.State.MaxHP = 50;
            var config = new LevelConfig { BoardSize = size, Stars = 2, Seed = seed,
                MissingPercent = StarDensityService.MissingPercentForStars(2) };
            config.ActiveModifiers.Add(modifier);
            run.StartLevel(config);
            Assert.NotNull(run.CurrentBoard, $"{modifier} on {size}×{size} did not generate");
            return run;
        }

        private static HashSet<(int, int)> Cells(IEnumerable<CellCoord> cells) =>
            new HashSet<(int, int)>(cells.Select(c => (c.Row, c.Col)));

        // Every counter writes the solution's digits into its own family's cells and no more than its count.
        private static void AssertFills(RunDirector run, ItemType type, HashSet<(int, int)> familyCells)
        {
            var targets = run.GetCounterItemTargets(type);
            Assert.IsNotEmpty(targets, $"{type} finds cells");
            Assert.LessOrEqual(targets.Count, CounterItems.CellsPerUse(type));
            if (familyCells != null)
                foreach (var cell in targets) Assert.IsTrue(familyCells.Contains(cell), $"{type}: {cell} belongs to its family");

            var effect = run.ApplyItemEffect(new ItemInstance { Id = "counter", Type = type, Rarity = ItemService.GetFixedRarity(type), Charges = 1 });

            Assert.AreEqual(ItemEffectResult.ResultKind.CellsFilled, effect.Kind);
            CollectionAssert.AreEquivalent(targets, effect.Cells);
            foreach (var (r, c) in effect.Cells)
                Assert.AreEqual(run.CurrentBoard.Solution[r, c], run.CurrentBoard.Cells[r, c], $"{type} wrote the right digit");
        }

        [Test]
        [Requirement("ITEM-COUNTER-001")]
        public void WaterBasin_FillsOneLine_TheSelectedCellsLineFirst()
        {
            var run = StartLevel(6, BossModifierId.DutchWhispers, 9301);
            var line = run.CurrentOverlay.Lines.First(l => l.Cells.Count(c => run.CurrentBoard.Cells[c.Row, c.Col] == 0) >= 2);
            var selected = line.Cells.First(c => run.CurrentBoard.Cells[c.Row, c.Col] == 0);

            var targets = run.GetCounterItemTargets(ItemType.WaterBasin, selected.Row, selected.Col);

            Assert.AreEqual((selected.Row, selected.Col), targets[0], "the selected cell first");
            Assert.IsTrue(targets.All(t => Cells(line.Cells).Contains(t)), "all on the selected cell's line");
            AssertFills(run, ItemType.WaterBasin, Cells(run.CurrentOverlay.Lines.SelectMany(l => l.Cells)));
        }

        [Test]
        [Requirement("ITEM-COUNTER-002")]
        public void TwinPebbles_FillsBothCellsOfOneMark()
        {
            var run = StartLevel(6, BossModifierId.DifferenceKropki, 9302);
            var marks = run.CurrentOverlay.KropkiDots.SelectMany(d => new[] { d.CellA, d.CellB })
                .Concat(run.CurrentOverlay.PairConstraints.SelectMany(p => new[] { p.CellA, p.CellB }));
            AssertFills(run, ItemType.TwinPebbles, Cells(marks));
        }

        [Test]
        [Requirement("ITEM-COUNTER-003")]
        public void RiceBowl_FillsOneCage()
        {
            var run = StartLevel(6, BossModifierId.KillerCages, 9303);
            AssertFills(run, ItemType.RiceBowl, Cells(run.CurrentOverlay.KillerCages.SelectMany(k => k.Cells)));
        }

        [Test]
        [Requirement("ITEM-COUNTER-004")]
        public void MossStone_FillsMarkedCells()
        {
            var run = StartLevel(6, BossModifierId.EvenOdd, 9304);
            AssertFills(run, ItemType.MossStone, Cells(run.CurrentOverlay.CellMarkers.Select(m => m.Cell)));
        }

        [Test]
        [Requirement("ITEM-COUNTER-005")]
        public void CompassOfOrder_FillsDiagonalCells_OnAnXSudoku()
        {
            var run = StartLevel(9, BossModifierId.XSudoku, 9305);
            var diagonals = new HashSet<(int, int)>(Enumerable.Range(0, 9).SelectMany(i => new[] { (i, i), (i, 8 - i) }));
            AssertFills(run, ItemType.CompassOfOrder, diagonals);
        }

        [Test]
        [Requirement("ITEM-COUNTER-006")]
        public void SunMedallion_FillsTheFirstCellsAClueReads()
        {
            var run = StartLevel(9, BossModifierId.Skyscrapers, 9306);
            var read = Cells(run.CurrentOverlay.OutsideClues.SelectMany(c => OutsideClueGeometry.Cells(c, 9)));
            AssertFills(run, ItemType.SunMedallion, read);
        }

        // Review finding: the compass reads whole-board rules; Anti-Knight has no houses, so the whole board counts, while a
        // board without a whole-board rule gives it nothing.
        [Test]
        [Requirement("ITEM-COUNTER-005")]
        public void CompassOfOrder_ReadsTheWholeBoardOnlyUnderAWholeBoardRule()
        {
            var knight = StartLevel(6, BossModifierId.Antiknight, 9308);
            Assert.AreEqual(2, knight.GetCounterItemTargets(ItemType.CompassOfOrder).Count, "Anti-Knight: the whole board");
            var lines = StartLevel(6, BossModifierId.DutchWhispers, 9309);
            Assert.IsEmpty(lines.GetCounterItemTargets(ItemType.CompassOfOrder), "no whole-board rule");
        }

        [Test]
        public void ACounterWithoutItsFamily_FillsNothing_AndIsNotUsedUp()
        {
            var run = StartLevel(6, BossModifierId.DutchWhispers, 9307);
            Assert.IsEmpty(run.GetCounterItemTargets(ItemType.RiceBowl), "no cages on this board");
            var effect = run.ApplyItemEffect(new ItemInstance { Id = "bowl", Type = ItemType.RiceBowl, Charges = 1 });
            Assert.AreNotEqual(ItemEffectResult.ResultKind.CellsFilled, effect.Kind);
        }

        // [REQ: ITEM-COUNTER-007]
        [Test]
        [Requirement("ITEM-COUNTER-007")]
        public void CountersForRules_FollowTheFamilies_AndSkipDebuffs()
        {
            CollectionAssert.AreEqual(new[] { ItemType.WaterBasin, ItemType.RiceBowl },
                CounterItems.ForRules(new[] { BossModifierId.KillerCages, BossModifierId.DutchWhispers, BossModifierId.RowWipe,
                    BossModifierId.GermanWhispers }));
            Assert.IsEmpty(CounterItems.ForRules(null));
        }

        [Test]
        [Requirement("ITEM-COUNTER-007")]
        public void Shops_LeanTowardTheBoardsCounters_EachOnce_AtTheirFixedRarity()
        {
            var counters = new[] { ItemType.WaterBasin, ItemType.RiceBowl };
            var seen = 0;
            for (var seed = 1; seed <= 40; seed++)
            {
                var offers = new ShopService(seed).BuildOffers(2, 1, 1f, 1f, (ClassId)0, counters);
                var counterOffers = offers.Where(o => CounterItems.IsCounterItem(o.Item.Type)).ToList();
                seen += counterOffers.Count;
                Assert.IsTrue(counterOffers.All(o => counters.Contains(o.Item.Type)), "only the board's counters");
                Assert.AreEqual(counterOffers.Count, counterOffers.Select(o => o.Item.Type).Distinct().Count(), "each once");
                foreach (var offer in counterOffers)
                {
                    Assert.AreEqual(ItemService.GetFixedRarity(offer.Item.Type), offer.Item.Rarity);
                    Assert.AreEqual(GoldTable.ShopItemPrice(GoldTable.BaseItemPrice(offer.Item.Rarity), 2), offer.Price);
                }
                Assert.IsFalse(new ShopService(seed).BuildOffers(2, 1, 1f, 1f).Any(o => CounterItems.IsCounterItem(o.Item.Type)),
                    "no counters without rules");
            }
            Assert.Greater(seen, 20, "about half the slots of 40 three-slot shops");
        }
    }
}
