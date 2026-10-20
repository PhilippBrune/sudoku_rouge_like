using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // Owner's 0.2.0 playtest: "it feels like gold and items are not needed" (design/gold-and-items-concept.md, RM-108).
    // L1: item prices ×2.5 and puzzle gold −20 %, so a shop visit uses most of a floor's gold. L6: from Floor 4 a mistake
    // costs at least 2 HP, and the rest heals a quarter of max HP instead of a third.
    [TestFixture]
    [Requirement("ECON-BAL-001", "ECON-BAL-002", "ECON-BAL-003", "ECON-BAL-004")]
    public sealed class GoldAndPressureBalanceTests
    {
        // [REQ: ECON-BAL-001] Normal 40, Rare 75, Epic 150, still × (1 + 0.5 × floor index).
        [Test]
        [Requirement("ECON-BAL-001")]
        public void ItemPrices_NormalRareEpic_Are40_75_150_TimesTheFloor()
        {
            Assert.AreEqual(40, GoldTable.BaseItemPrice(ItemRarity.Normal));
            Assert.AreEqual(75, GoldTable.BaseItemPrice(ItemRarity.Rare));
            Assert.AreEqual(150, GoldTable.BaseItemPrice(ItemRarity.Epic));
            Assert.AreEqual(75, ItemService.GetBasePrice(ItemRarity.Rare), "one price table");
            Assert.AreEqual(150, ItemService.GetShopPrice(ItemRarity.Rare, 2), "a Rare item on Floor 3 costs 75 × 2");
            Assert.AreEqual(120, GoldTable.ShopItemPrice(40, 4), "a Normal item on Floor 5 costs 40 × 3");

            var offers = new ShopService(17).BuildOffers(0, 1, 1f, 1f);
            Assert.IsNotEmpty(offers);
            foreach (var offer in offers)
                CollectionAssert.Contains(new[] { 40, 75, 150 }, offer.Price, "Floor 1 offers cost the base price");
        }

        // [REQ: ECON-BAL-002] Puzzle gold is four fifths of the size-and-stars table.
        [Test]
        [Requirement("ECON-BAL-002")]
        public void PuzzleGold_IsFourFifthsOfTheTable()
        {
            Assert.AreEqual(29, GoldTable.CalculatePuzzleGold(6, 1), "30 × 1.2 × 0.8 = 28.8");
            Assert.AreEqual(128, GoldTable.CalculatePuzzleGold(9, 3), "100 × 1.6 × 0.8");
            for (var size = 5; size <= 9; size++)
            for (var stars = 1; stars <= 6; stars++)
                Assert.AreEqual(
                    UnityEngine.Mathf.RoundToInt(GoldTable.BaseGoldForBoardSize(size) * GoldTable.StarGoldMultiplier(stars) * 0.8f),
                    GoldTable.CalculatePuzzleGold(size, stars), $"{size}×{size}, {stars}★");
        }

        // [REQ: ECON-BAL-003] Floors 1–3 keep the Harmony cost (1 at H0); from Floor 4 a mistake costs at least 2 HP.
        [Test]
        [Requirement("ECON-BAL-003")]
        public void Mistake_CostsTwoHpFromFloorFour()
        {
            Assert.AreEqual(1, HpLostByOneMistake(floorIndex: 2), "Floor 3 at Harmony 0");
            Assert.AreEqual(2, HpLostByOneMistake(floorIndex: 3), "Floor 4 at Harmony 0");
            Assert.AreEqual(2, HpLostByOneMistake(floorIndex: 4), "Floor 5 at Harmony 0");
            Assert.AreEqual(3, HpLostByOneMistake(floorIndex: 4, harmonyCost: 3), "a higher Harmony cost stays");
        }

        // [REQ: ECON-BAL-004] The rest heals a quarter of max HP, at least 1.
        [Test]
        [Requirement("ECON-BAL-004")]
        public void Rest_HealsAQuarterOfMaxHp()
        {
            var run = StartRun(6104);
            run.State.MaxHP = 12;
            run.State.CurrentHP = 2;
            Assert.AreEqual(3, run.GetRestHealAmount(), "12 / 4");
            run.State.MaxHP = 9;
            Assert.AreEqual(2, run.GetRestHealAmount(), "9 / 4, rounded down");
            run.State.MaxHP = 2;
            Assert.AreEqual(1, run.GetRestHealAmount(), "never below 1");
        }

        private static RunDirector StartRun(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            return run;
        }

        private static int HpLostByOneMistake(int floorIndex, int harmonyCost = 1)
        {
            var run = StartRun(8930 + floorIndex);
            for (var f = 0; f < floorIndex; f++) run.AdvanceToNextFloor();
            run.State.HeldRelics.Clear();
            run.State.MistakeHpCost = harmonyCost;
            run.State.CurrentHP = run.State.MaxHP = 50;
            run.StartLevel(new LevelConfig { BoardSize = 5, Stars = 1, Seed = 41,
                MissingPercent = StarDensityService.MissingPercentForStars(1) });
            var b = run.CurrentBoard;
            var (row, col) = Enumerable.Range(0, b.Size * b.Size).Select(i => (i / b.Size, i % b.Size))
                .First(cell => b.Cells[cell.Item1, cell.Item2] == 0);
            Assert.AreNotEqual(PlaceResult.Correct, run.PlaceNumber(row, col, b.Solution[row, col] % b.Size + 1));
            return 50 - run.State.CurrentHP;
        }
    }
}
