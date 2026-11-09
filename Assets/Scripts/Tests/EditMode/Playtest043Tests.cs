using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // The owner's 0.4.3 playtest: rules with a gain, longer lines, turned regions, X-Sums, prices, the Stone Gardener and the challenge.
    [TestFixture]
    public sealed class Playtest043Tests
    {
        private static SudokuBoard Board(int size, bool allGiven)
        {
            var solution = new int[size, size];
            var cells = new int[size, size];
            var regions = new int[size, size];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                solution[r, c] = (r + c) % size + 1;
                cells[r, c] = allGiven ? solution[r, c] : 0;
                regions[r, c] = r / 2 * 3 + c / 3;   // 6×6 boxes of 2 by 3
            }
            return new SudokuBoard(size, solution, cells, regions);
        }

        [Test]
        [Requirement("BOSS-MOD-008")]
        public void ANoRepeatLineInsideOneBox_SaysNothing_OneAcrossBoxesDoes()
        {
            var board = Board(6, false);
            var inBox = new List<CellCoord> { new CellCoord(0, 0), new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(1, 2) };
            var across = new List<CellCoord> { new CellCoord(0, 2), new CellCoord(0, 3), new CellCoord(1, 3), new CellCoord(1, 4) };
            Assert.IsFalse(ModifierGeometryGenerator.SaysMoreThanTheHouses(board, inBox));
            Assert.IsTrue(ModifierGeometryGenerator.SaysMoreThanTheHouses(board, across));
        }

        [Test]
        [Requirement("BOSS-MOD-008")]
        public void APieceOnGivensOnly_Goes_ButARuleKeepsOnePiece()
        {
            var board = Board(6, true);
            var overlay = new ModifierOverlayData();
            overlay.Lines.Add(new ModifierLine { Type = LineType.Thermo, Cells = { new CellCoord(0, 0), new CellCoord(0, 1) } });
            ModifierGeometryGenerator.DropPiecesWithoutGain(board, overlay);
            Assert.AreEqual(1, overlay.Lines.Count, "the rule's only piece stays");

            board.ClearValue(2, 2);
            board.GivenMask[2, 2] = false;
            overlay.Lines.Add(new ModifierLine { Type = LineType.Thermo, Cells = { new CellCoord(2, 2), new CellCoord(2, 3) } });
            ModifierGeometryGenerator.DropPiecesWithoutGain(board, overlay);
            Assert.AreEqual(1, overlay.Lines.Count);
            Assert.AreEqual(2, overlay.Lines[0].Cells[0].Row, "the piece with an open cell stays");
        }

        [Test]
        [Requirement("BOSS-MOD-009")]
        public void LinesMayRunAsLongAsTheBoardIsWide()
        {
            Assert.AreEqual(6, ModifierGeometryGenerator.LongestLine(5));
            Assert.AreEqual(9, ModifierGeometryGenerator.LongestLine(9));
        }

        [Test]
        [Requirement("BOSS-FLOOR-001")]
        public void TheFloorsKeepTheirRuleCounts()
        {
            var bosses = new BossService(7);
            Assert.AreEqual(0, bosses.RollFloorModifiers(0, 6, 0).Count);
            Assert.AreEqual(1, bosses.RollFloorModifiers(2, 7, 0).Count);
            Assert.AreEqual(2, bosses.RollFloorModifiers(4, 9, 0).Count);
        }

        [Test]
        [Requirement("BOSS-MOD-010")]
        public void ARuleShowsUpMoreOften_TheFewerRulesShareTheBoard()
        {
            Assert.AreEqual(2f, ModifierGeometryGenerator.PiecesPerRule(1));
            Assert.AreEqual(1.5f, ModifierGeometryGenerator.PiecesPerRule(3));
            Assert.AreEqual(1f, ModifierGeometryGenerator.PiecesPerRule(7));

            var board = Board(9, false);
            var overlay = ModifierGeometryGenerator.Generate(board, new List<BossModifierId> { BossModifierId.DifferenceKropki }, 11);
            Assert.GreaterOrEqual(overlay.KropkiDots.Count, 20, "twice the twelve dots a 9×9 had");
        }

        [Test]
        [Requirement("BOSS-TEXT-001")]
        public void PrimeCells_NameOnlyTheBoardsDigits()
        {
            StringAssert.DoesNotContain("7", BossService.GetModifierDescription(BossModifierId.PrimeCells, 6));
            StringAssert.Contains("2, 3, 5, 7", BossService.GetModifierDescription(BossModifierId.PrimeCells, 9));
            StringAssert.DoesNotContain("{0}", BossService.GetModifierDescription(BossModifierId.PrimeCells));
        }

        [Test]
        [Requirement("BOSS-XSUM-001")]
        public void AnXSumIsFulfilled_WhenItsFirstXDigitsAreThereAndAddUp()
        {
            var clue = new OutsideClue { Edge = OutsideClueEdge.Left, Index = 0, Value = 9, Type = OutsideClueType.XSums };
            var cells = new int[6, 6];
            cells[0, 0] = 3;
            cells[0, 1] = 1;
            Assert.IsFalse(OutsideClueGeometry.IsXSumFulfilled(clue, cells, 6), "the third digit is missing");
            cells[0, 2] = 5;
            Assert.IsTrue(OutsideClueGeometry.IsXSumFulfilled(clue, cells, 6));
            cells[0, 2] = 4;
            Assert.IsFalse(OutsideClueGeometry.IsXSumFulfilled(clue, cells, 6), "they add up to 8");
        }

        [Test]
        [Requirement("GEN-REGION-004")]
        public void IrregularRegionsTurnWithTheSeed_BoxesDoNot()
        {
            var maps = new List<string>();
            foreach (var seed in new[] { 0, 16, 32, 48 })
            {
                var map = SudokuGenerator.RegionMapFor(8, 2, seed);
                maps.Add(string.Join(",", map.Cast<int>()));
                foreach (var size in Enumerable.Range(0, 8).Select(id => map.Cast<int>().Count(v => v == id)))
                    Assert.AreEqual(8, size, "a turned region keeps its size");
            }
            Assert.AreEqual(4, maps.Distinct().Count(), "four boards from one template");
            CollectionAssert.AreEqual(SudokuGenerator.RegionMapFor(9, 0, 0).Cast<int>(), SudokuGenerator.RegionMapFor(9, 0, 16).Cast<int>());
        }

        [Test]
        [Requirement("ECON-SHOP-003")]
        public void TheDailyWalksFirstFloorHasFirstFloorPrices()
        {
            var plain = new ShopService(17).BuildOffers(1, 1, 1f, 1f);
            var walk = new ShopService(17).BuildOffers(1, 1, 1f, 1f, priceFloor: 0);
            Assert.IsTrue(plain.All(o => o.Price == (int)System.Math.Round(GoldTable.BaseItemPrice(o.Item.Rarity) * 1.5)), "garden 2 costs half again");
            Assert.IsTrue(walk.All(o => o.Price == GoldTable.BaseItemPrice(o.Item.Rarity)));
        }

        [Test]
        [Requirement("CLASS-PASSIVE-004")]
        public void TheStoneGardenersItemsHaveTwoUses_ThreeFromLevelTwenty()
        {
            var state = new RunState { ClassId = ClassId.StoneGardener, ClassLevel = 1 };
            var item = RelicService.OnItemReceived(state, new ItemInstance { Type = ItemType.InkWell, Charges = 1 });
            Assert.AreEqual(2, item.Charges);
            state.ClassLevel = 20;
            Assert.AreEqual(3, ClassPassiveService.ItemUses(state));
            Assert.AreEqual(1, ClassPassiveService.ItemUses(new RunState { ClassId = ClassId.NumberFreak }));
        }

        private static RunDirector PuzzleRun(int seed)
        {
            var run = new RunDirector();
            run.StartTutorialRun(new TutorialSetupConfig { BoardSize = 6, Stars = 3, Seed = seed }, seed);
            return run;
        }

        private static (int row, int col) EmptyCell(RunDirector run)
        {
            var board = run.CurrentBoard;
            return Enumerable.Range(0, 36).Select(i => (i / 6, i % 6)).First(p => board.Cells[p.Item1, p.Item2] == 0);
        }

        private static void PlaceWrong(RunDirector run)
        {
            var (row, col) = EmptyCell(run);
            run.PlaceNumber(row, col, run.CurrentBoard.Solution[row, col] % 6 + 1);
        }

        [Test]
        [Requirement("ITEM-REWORK-002")]
        public void TheChimeOfNotes_WritesCandidates_LeavesThePlayersNotesAndPlacesNoDigit()
        {
            var run = PuzzleRun(9410);
            var board = run.CurrentBoard;
            var (row, col) = EmptyCell(run);
            var digit = board.Solution[row, col];
            board.AddPencilMark(row, col, digit);
            var digitsBefore = board.Cells.Cast<int>().Count(v => v != 0);
            var pencil = run.State.CurrentPencil;

            Assert.Greater(run.WriteAllNotes(), 0);

            CollectionAssert.AreEqual(new[] { digit }, board.GetPencilMarks(row, col).ToArray(), "the player's own note stays alone");
            Assert.AreEqual(digitsBefore, board.Cells.Cast<int>().Count(v => v != 0));
            Assert.AreEqual(pencil, run.State.CurrentPencil, "free");
            var other = Enumerable.Range(0, 36).Select(i => (i / 6, i % 6)).First(p => board.Cells[p.Item1, p.Item2] == 0 && p != (row, col));
            CollectionAssert.Contains(board.GetPencilMarks(other.Item1, other.Item2).ToArray(), board.Solution[other.Item1, other.Item2]);
            Assert.AreEqual(0, run.WriteAllNotes(), "nothing left to write");
        }

        [Test]
        [Requirement("ITEM-REWORK-002")]
        public void TheRiverBell_KeepsTheComboThroughMistakes()
        {
            var run = PuzzleRun(9411);
            run.State.HeldRelics.Clear();
            run.State.HeldItems.Clear();
            run.State.HeldItems.Add(new ItemInstance { Id = "bell", Type = ItemType.KoiDragonScale, Rarity = ItemRarity.Epic, Charges = 1 });
            Assert.IsTrue(run.TryUseItem(0));
            run.State.ComboStreak = 12;
            PlaceWrong(run);
            Assert.AreEqual(12, run.State.ComboStreak);
        }

        [Test]
        [Requirement("ITEM-REWORK-002")]
        public void TheStoneOath_CostsOneHp_AndNeverKills()
        {
            var run = PuzzleRun(9412);
            run.State.HeldItems.Clear();
            run.State.HeldItems.Add(new ItemInstance { Id = "oath", Type = ItemType.TempleStamp, Rarity = ItemRarity.Rare, Charges = 2 });
            run.State.CurrentHP = 1;
            Assert.IsFalse(run.TryUseItem(0), "at 1 HP the oath is refused");
            run.State.CurrentHP = 5;
            Assert.IsTrue(run.TryUseItem(0));
            Assert.AreEqual(4, run.State.CurrentHP);
            Assert.IsTrue(run.State.StoneOathActive);
            Assert.IsFalse(run.TryUseItem(0), "a second oath in the same puzzle would pay for nothing");
            Assert.AreEqual(4, run.State.CurrentHP);
        }

        [Test]
        [Requirement("ITEM-REWORK-002")]
        public void TheMothLight_LiftsOneBoxOfFog_ForGood()
        {
            var run = PuzzleRun(9413);
            var board = run.CurrentBoard;
            run.CurrentOverlay.FogCells.Clear();
            run.CurrentOverlay.FogCells.Add(new CellCoord(0, 0));
            run.CurrentOverlay.FogCells.Add(new CellCoord(5, 5));
            Assert.AreNotEqual(board.RegionMap[0, 0], board.RegionMap[5, 5]);

            Assert.AreEqual(0, run.LiftFogFromBox(2, 5) * (board.RegionMap[2, 5] == board.RegionMap[5, 5] ? 0 : 1), "a box without fog lifts nothing");
            var lifted = run.LiftFogFromBox(0, 1);
            Assert.AreEqual(board.RegionMap[0, 1] == board.RegionMap[0, 0] ? 1 : 0, lifted);
            Assert.IsFalse(run.CurrentOverlay.FogCells.Exists(c => c.Row == 0 && c.Col == 0));
            Assert.IsTrue(run.CurrentOverlay.FogCells.Exists(c => c.Row == 5 && c.Col == 5), "the other box stays fogged");
        }

        [Test]
        [Requirement("ITEM-REWORK-002")]
        public void TheChimeOfNotes_IsANormalItemInTheRewardPool()
        {
            Assert.IsFalse(ItemService.IsTiered(ItemType.WindChime));
            Assert.AreEqual(ItemRarity.Normal, ItemService.GetFixedRarity(ItemType.WindChime));
            var service = new ItemService(23);
            var seen = Enumerable.Range(0, 400).Select(_ => service.RollOneItem(1, 1).Type);
            CollectionAssert.Contains(seen.ToList(), ItemType.WindChime);
        }

        [Test]
        [Requirement("RELIC-REWORK-001")]
        public void TheSteadyBrush_MakesPencilCheaper()
        {
            var state = new RunState();
            state.HeldRelics.Add(new RelicInstance { Id = RelicId.SteadyHand, UsesRemaining = -1 });
            Assert.AreEqual(12, RelicService.PencilBuyCost(state, 20));
            Assert.AreEqual(20, RelicService.PencilBuyCost(new RunState(), 20));
        }

        [Test]
        [Requirement("RELIC-REWORK-001")]
        public void TheVoidPetal_PaysThreePencilOncePerFloor_AndNeedsThem()
        {
            var state = new RunState { CurrentPencil = 4 };
            state.HeldRelics.Add(new RelicInstance { Id = RelicId.VoidPetal, UsesRemaining = -1 });
            Assert.IsTrue(RelicService.TryVoidPetal(state));
            Assert.AreEqual(1, state.CurrentPencil);
            state.CurrentPencil = 9;
            Assert.IsFalse(RelicService.TryVoidPetal(state), "once per floor");
            RelicService.OnFloorStart(state);
            state.CurrentPencil = 2;
            Assert.IsFalse(RelicService.TryVoidPetal(state), "without 3 pencil the mistake costs HP");
            state.CurrentPencil = 3;
            Assert.IsTrue(RelicService.TryVoidPetal(state), "and the petal waits");
        }

        [Test]
        [Requirement("RELIC-THREAD-001")]
        public void TheUnbrokenThread_HalvesTheCombo()
        {
            var run = PuzzleRun(9414);
            run.State.HeldRelics.Clear();
            run.State.HeldRelics.Add(new RelicInstance { Id = RelicId.UnbrokenThread, UsesRemaining = -1 });
            run.State.ComboStreak = 9;
            PlaceWrong(run);
            Assert.AreEqual(4, run.State.ComboStreak);
        }

        [Test]
        [Requirement("RELIC-TEACUP-001")]
        public void TheCrackedTeacup_KeepsTheCombo()
        {
            var run = new RunDirector();
            run.StartTutorialRun(new TutorialSetupConfig { BoardSize = 6, Stars = 3, Seed = 9401 }, 9401);
            run.State.HeldRelics.Add(new RelicInstance { Id = RelicId.CrackedTeacup, UsesRemaining = 1 });
            run.State.ComboStreak = 7;
            var board = run.CurrentBoard;
            var (row, col) = Enumerable.Range(0, 36).Select(i => (i / 6, i % 6)).First(p => board.Cells[p.Item1, p.Item2] == 0);
            run.PlaceNumber(row, col, board.Solution[row, col] % 6 + 1);
            Assert.AreEqual(7, run.State.ComboStreak);
        }

        [Test]
        [Requirement("EVENT-CHALLENGE-001")]
        public void TheRelicChallenge_AsksForMoreOnATighterClock()
        {
            var (itemSize, itemStars, itemSeconds) = RunDirector.ChallengeTerms(RunDirector.ChallengeItem, 6, 0);
            var (relicSize, relicStars, relicSeconds) = RunDirector.ChallengeTerms(RunDirector.ChallengeRelic, 6, 0);
            Assert.AreEqual((6, 2), (itemSize, itemStars));
            Assert.AreEqual((7, 3), (relicSize, relicStars));
            Assert.Greater(itemSeconds, 60);
            Assert.Less(relicSeconds / (float)(relicSize * relicSize), itemSeconds / (float)(itemSize * itemSize) * 1.6f);
            Assert.AreEqual(9, RunDirector.ChallengeTerms(RunDirector.ChallengeRelic, 9, 4).size, "never above 9×9");
        }

        [Test]
        [Requirement("PROFILE-NAME-001")]
        public void AProfileName_IsPlainTextOfAtMostTwentyCharacters()
        {
            Assert.AreEqual("Paul", SaveProfileService.CleanProfileName("  Paul "));
            Assert.AreEqual("a b", SaveProfileService.CleanProfileName("a\nb"));
            StringAssert.DoesNotContain("<", SaveProfileService.CleanProfileName("<b>x</b>"));
            Assert.AreEqual(20, SaveProfileService.CleanProfileName(new string('x', 40)).Length);
        }
    }
}
