using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-BOSS-108] GAP-026 / SPEC-CONF-021: from Floor 3 every Garden Run boss carries one debuff, drawn
    // from the run seed and floor so the boss gate and the boss puzzle agree. Cross Wipe and Pencil Scramble are
    // reserved for VeryHigh intensity; Given Reveal is not delivered.
    // The hook tests trigger each debuff through a rejected wrong digit (PlaceNumber → ApplyMistakePenalty).
    [TestFixture]
    [Requirement("REQ-BOSS-108", "DEBUFF-HOOK-001")]
    public sealed class BossDebuffTests
    {
        // ── Delivery ──

        [Test]
        [Requirement("DEBUFF-DELIV-001")]
        public void BossesOnFloorsOneAndTwo_CarryNoDebuff()
        {
            for (var seed = 0; seed < 10; seed++)
            {
                var run = StartRun(8100 + seed);
                for (var floor = 0; floor < BossService.FirstBossDebuffFloor; floor++)
                {
                    run.State.CurrentFloor = floor;
                    Assert.IsNull(run.GetBossDebuffForCurrentFloor(), $"seed {seed} floor {floor + 1}");
                    Assert.IsFalse(run.BuildLevelConfig(true).ActiveModifiers.Any(BossService.IsBossDebuff));
                }
            }
        }

        [Test]
        [Requirement("DEBUFF-DELIV-001")]
        public void BossesFromFloorThree_CarryExactlyOneDebuff_NormalPuzzlesNone()
        {
            var seen = new HashSet<BossModifierId>();
            for (var seed = 0; seed < 40; seed++)
            {
                var run = StartRun(8200 + seed);
                run.State.RunNumber = 1;
                for (var floor = BossService.FirstBossDebuffFloor; floor < 5; floor++)
                {
                    run.State.CurrentFloor = floor;
                    var where = $"seed {seed} floor {floor + 1}";
                    var debuffs = run.BuildLevelConfig(true).ActiveModifiers.Where(BossService.IsBossDebuff).ToList();

                    Assert.AreEqual(1, debuffs.Count, where);
                    CollectionAssert.Contains(BossService.GetBossDebuffPool(BossModifierIntensity.Low), debuffs[0], where);
                    Assert.AreEqual(run.GetBossDebuffForCurrentFloor(), debuffs[0], $"{where}: the gate announces the boss's debuff");
                    Assert.IsFalse(run.BuildLevelConfig(false).ActiveModifiers.Any(BossService.IsBossDebuff),
                        $"{where}: normal puzzles carry no debuff");
                    seen.Add(debuffs[0]);
                }
            }
            Assert.GreaterOrEqual(seen.Count, 6, "the debuff varies between runs and floors");
        }

        [Test]
        [Requirement("DEBUFF-DELIV-002")]
        public void Debuff_DependsOnlyOnRunSeedAndFloor()
        {
            var a = StartRun(8301);
            var b = StartRun(8301);
            a.State.CurrentFloor = b.State.CurrentFloor = 3;
            b.State.Depth = 4;
            b.State.CurrentNodeIndex = 5;

            Assert.AreEqual(a.GetBossDebuffForCurrentFloor(), b.GetBossDebuffForCurrentFloor(),
                "moving along the floor must not change the announced debuff");
        }

        [Test]
        [Requirement("DEBUFF-DELIV-003")]
        public void CrossWipeAndPencilScramble_OnlyAtVeryHighIntensity_GivenRevealNever()
        {
            foreach (BossModifierIntensity intensity in Enum.GetValues(typeof(BossModifierIntensity)))
            {
                var pool = BossService.GetBossDebuffPool(intensity);
                var veryHigh = intensity == BossModifierIntensity.VeryHigh;
                Assert.AreEqual(veryHigh, pool.Contains(BossModifierId.CrossWipe), intensity.ToString());
                Assert.AreEqual(veryHigh, pool.Contains(BossModifierId.PencilScramble), intensity.ToString());
                CollectionAssert.DoesNotContain(pool, BossModifierId.GivenReveal);
                Assert.IsTrue(pool.All(BossService.IsBossDebuff), intensity.ToString());
            }
        }

        [TestCase(GameMode.Tutorial)]
        [TestCase(GameMode.EndlessZen)]
        [TestCase(GameMode.SpiritTrials)]
        [TestCase(GameMode.SeasonalChallenge)]
        [Requirement("DEBUFF-DELIV-001")]
        public void OtherModes_HaveNoBossDebuff(GameMode mode)
        {
            var run = StartRun(8401);
            run.State.Mode = mode;
            run.State.CurrentFloor = 4;
            Assert.IsNull(run.GetBossDebuffForCurrentFloor());
        }

        [Test]
        public void EveryDeliverableDebuff_HasANameAndDescription()
        {
            foreach (var debuff in BossService.GetBossDebuffPool(BossModifierIntensity.VeryHigh))
            {
                Assert.AreNotEqual(debuff.ToString(), BossService.GetModifierName(debuff), $"{debuff}: name");
                Assert.IsNotEmpty(BossService.GetModifierDescription(debuff), $"{debuff}: description");
            }
        }

        // ── Hooks ──

        [Test]
        [Requirement("DEBUFF-DEF-001", "DEBUFF-HOOK-003")]
        public void RowWipe_ErasesYourDigitsInTheRow()
        {
            var run = StartPuzzleWith(BossModifierId.RowWipe, 8501);
            var (row, col) = FirstEmptyCell(run);
            var filled = FillCorrect(run, Cells(run).Where(p => p.r == row && p.c != col));
            var keep = FillCorrect(run, Cells(run).Where(p => p.r != row).Take(1));

            PlaceWrong(run, row, col);

            AssertCleared(run, filled);
            AssertKept(run, keep);
        }

        [Test]
        [Requirement("DEBUFF-DEF-002", "DEBUFF-HOOK-004")]
        public void ColWipe_ErasesYourDigitsInTheColumn()
        {
            var run = StartPuzzleWith(BossModifierId.ColWipe, 8502);
            var (row, col) = FirstEmptyCell(run);
            var filled = FillCorrect(run, Cells(run).Where(p => p.c == col && p.r != row));
            var keep = FillCorrect(run, Cells(run).Where(p => p.c != col).Take(1));

            PlaceWrong(run, row, col);

            AssertCleared(run, filled);
            AssertKept(run, keep);
        }

        [Test]
        [Requirement("DEBUFF-DEF-003")]
        public void BoxWipe_ErasesYourDigitsInTheBox()
        {
            var run = StartPuzzleWith(BossModifierId.BoxWipe, 8503);
            var (row, col) = FirstEmptyCell(run);
            var region = run.CurrentBoard.RegionMap[row, col];
            var filled = FillCorrect(run, Cells(run).Where(p => run.CurrentBoard.RegionMap[p.r, p.c] == region && (p.r, p.c) != (row, col)));
            var keep = FillCorrect(run, Cells(run).Where(p => run.CurrentBoard.RegionMap[p.r, p.c] != region).Take(1));

            PlaceWrong(run, row, col);

            AssertCleared(run, filled);
            AssertKept(run, keep);
        }

        [Test]
        [Requirement("DEBUFF-DEF-004")]
        public void CrossWipe_ErasesYourDigitsInRowAndColumn()
        {
            var run = StartPuzzleWith(BossModifierId.CrossWipe, 8504);
            var (row, col) = FirstEmptyCell(run);
            var filled = FillCorrect(run, Cells(run).Where(p => (p.r == row) != (p.c == col)));
            var keep = FillCorrect(run, Cells(run).Where(p => p.r != row && p.c != col).Take(1));

            PlaceWrong(run, row, col);

            AssertCleared(run, filled);
            AssertKept(run, keep);
        }

        [Test]
        [Requirement("DEBUFF-DEF-005", "DEBUFF-HOOK-014")]
        public void RadiusWipe_ErasesYourDigitsWithinTwoCells()
        {
            var run = StartPuzzleWith(BossModifierId.RadiusWipe, 8505);
            var (row, col) = FirstEmptyCell(run);
            int Distance((int r, int c) p) => Math.Max(Math.Abs(p.r - row), Math.Abs(p.c - col));
            var filled = FillCorrect(run, Cells(run).Where(p => Distance(p) is > 0 and <= 2));
            var keep = FillCorrect(run, Cells(run).Where(p => Distance(p) > 2).Take(1));

            PlaceWrong(run, row, col);

            AssertCleared(run, filled);
            AssertKept(run, keep);
        }

        [Test]
        [Requirement("DEBUFF-DEF-012", "DEBUFF-HOOK-002")]
        public void DoublePenalty_DoublesTheHpLoss()
        {
            var run = StartPuzzleWith(BossModifierId.DoublePenalty, 8506);
            var (row, col) = FirstEmptyCell(run);
            var hp = run.State.CurrentHP;

            PlaceWrong(run, row, col);

            Assert.AreEqual(hp - 2 * Math.Max(1, run.State.MistakeHpCost), run.State.CurrentHP);
        }

        [Test]
        [Requirement("DEBUFF-DEF-018", "DEBUFF-HOOK-006", "DEBUFF-HOOK-007")]
        public void CellLock_LocksTheCellForThreeCorrectPlacements()
        {
            var run = StartPuzzleWith(BossModifierId.CellLock, 8507);
            var (row, col) = FirstEmptyCell(run);

            PlaceWrong(run, row, col);
            Assert.IsTrue(run.GetLockedCells().Contains((row, col)));

            var others = Cells(run).Where(p => (p.r, p.c) != (row, col)).Take(3).ToList();
            FillCorrect(run, others.Take(2));
            Assert.IsTrue(run.GetLockedCells().Contains((row, col)), "still locked after 2 correct placements");
            FillCorrect(run, others.Skip(2));
            Assert.IsFalse(run.GetLockedCells().Contains((row, col)), "unlocked after the 3rd correct placement");
        }

        [Test]
        [Requirement("DEBUFF-DEF-007", "DEBUFF-HOOK-005")]
        public void PencilBlind_ClearsPencilMarksInRowAndColumn()
        {
            var run = StartPuzzleWith(BossModifierId.PencilBlind, 8508);
            var board = run.CurrentBoard;
            var (row, col) = FirstEmptyCell(run);
            var cleared = Cells(run).Where(p => p.r == row || p.c == col).Take(4).ToList();
            var kept = Cells(run).First(p => p.r != row && p.c != col);
            foreach (var p in cleared.Append(kept)) board.AddPencilMark(p.r, p.c, 1);

            PlaceWrong(run, row, col);

            Assert.IsTrue(cleared.All(p => !board.HasAnyPencilMark(p.r, p.c)));
            Assert.IsTrue(board.HasAnyPencilMark(kept.r, kept.c));
        }

        [Test]
        [Requirement("DEBUFF-DEF-013")]
        public void PencilDrain_CostsOnePencil()
        {
            var run = StartPuzzleWith(BossModifierId.PencilDrain, 8509);
            var (row, col) = FirstEmptyCell(run);
            run.State.CurrentPencil = 5;

            PlaceWrong(run, row, col);

            Assert.AreEqual(4, run.State.CurrentPencil);
        }

        [Test]
        [Requirement("DEBUFF-DEF-014")]
        public void GoldFine_CostsFiveGold_NeverBelowZero()
        {
            var run = StartPuzzleWith(BossModifierId.GoldFine, 8510);
            var (row, col) = FirstEmptyCell(run);
            run.State.CurrentGold = 12;
            PlaceWrong(run, row, col);
            Assert.AreEqual(7, run.State.CurrentGold);

            run.State.CurrentGold = 3;
            PlaceWrong(run, row, col);
            Assert.AreEqual(0, run.State.CurrentGold);
        }

        [Test]
        [Requirement("DEBUFF-DEF-008", "DEBUFF-HOOK-015")]
        public void PencilScramble_ReplacesMarksWithWrongCandidates()
        {
            var run = StartPuzzleWith(BossModifierId.PencilScramble, 8511);
            var board = run.CurrentBoard;
            var (row, col) = FirstEmptyCell(run);
            foreach (var p in Cells(run)) board.AddPencilMark(p.r, p.c, board.Solution[p.r, p.c]);

            PlaceWrong(run, row, col);

            foreach (var p in Cells(run))
                CollectionAssert.DoesNotContain(board.GetPencilMarks(p.r, p.c), board.Solution[p.r, p.c],
                    $"({p.r},{p.c}) must not keep its solution as a mark");
            Assert.IsTrue(Cells(run).Any(p => board.HasAnyPencilMark(p.r, p.c)), "scrambled marks are placed");
        }

        [Test]
        [Requirement("DEBUFF-DEF-017", "DEBUFF-HOOK-016")]
        public void GivenReveal_TurnsUpToTwoCorrectDigitsIntoGivens()
        {
            var run = StartPuzzleWith(BossModifierId.GivenReveal, 8512);
            var (row, col) = FirstEmptyCell(run);
            var filled = FillCorrect(run, Cells(run).Where(p => (p.r, p.c) != (row, col)).Take(4));

            PlaceWrong(run, row, col);

            Assert.AreEqual(2, filled.Count(p => run.CurrentBoard.IsGiven(p.r, p.c)));
        }

        [Test]
        [Requirement("DEBUFF-DEF-015", "DEBUFF-HOOK-017", "DEBUFF-HOOK-019")]
        public void RevealCost_BlocksItemsForTwoCorrectPlacements()
        {
            var run = StartPuzzleWith(BossModifierId.RevealCost, 8513);
            var (row, col) = FirstEmptyCell(run);
            run.State.HeldItems.Add(new ItemInstance { Id = "test", Type = ItemType.InkWell, Charges = 1 });
            var slot = run.State.HeldItems.Count - 1;

            PlaceWrong(run, row, col);
            Assert.AreEqual(2, run.ItemUseBlockedMoves);
            Assert.IsFalse(run.TryUseItem(slot), "items are blocked");

            FillCorrect(run, Cells(run).Where(p => (p.r, p.c) != (row, col)).Take(2));
            Assert.AreEqual(0, run.ItemUseBlockedMoves);
        }

        [Test]
        [Requirement("DEBUFF-DEF-011", "DEBUFF-HOOK-018", "DEBUFF-HOOK-019")]
        public void CellBlur_HidesTheSurroundingCellsForThreeCorrectPlacements()
        {
            var run = StartPuzzleWith(BossModifierId.CellBlur, 8514);
            var (row, col) = FirstEmptyCell(run);

            PlaceWrong(run, row, col);
            var blur = run.CurrentLevelState.CellBlurCells;
            Assert.IsTrue(blur.Contains((row, col)));
            Assert.IsTrue(blur.All(p => Math.Abs(p.row - row) <= 1 && Math.Abs(p.col - col) <= 1));

            FillCorrect(run, Cells(run).Where(p => (p.r, p.c) != (row, col)).Take(3));
            Assert.AreEqual(0, run.CurrentLevelState.CellBlurCells.Count);
        }

        // ── Helpers ──

        private static RunDirector StartRun(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = false,
                ClassLevel = 1
            }, seed);
            // Starting relics can absorb mistakes; isolate the debuff hooks from them.
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            return run;
        }

        private static RunDirector StartPuzzleWith(BossModifierId debuff, int seed)
        {
            var run = StartRun(seed);
            run.State.CurrentHP = run.State.MaxHP = 20;
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i])
                .First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.ActiveModifiers.Add(debuff);
            run.StartLevel(config);
            Assert.IsNotNull(run.CurrentBoard, "puzzle generation failed for the test seed");
            CollectionAssert.Contains(run.CurrentLevelConfig.ActiveModifiers, debuff);
            return run;
        }

        // Empty, non-given cells in reading order.
        private static List<(int r, int c)> Cells(RunDirector run)
        {
            var board = run.CurrentBoard;
            var cells = new List<(int, int)>();
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0 && !board.IsGiven(r, c))
                    cells.Add((r, c));
            return cells;
        }

        private static (int r, int c) FirstEmptyCell(RunDirector run)
        {
            var cells = Cells(run);
            Assert.IsNotEmpty(cells, "no empty cell");
            return cells[0];
        }

        private static List<(int r, int c)> FillCorrect(RunDirector run, IEnumerable<(int r, int c)> cells)
        {
            var list = cells.ToList();
            foreach (var (r, c) in list)
                Assert.AreEqual(PlaceResult.Correct, run.PlaceNumber(r, c, run.CurrentBoard.Solution[r, c]));
            return list;
        }

        private static void PlaceWrong(RunDirector run, int row, int col)
        {
            var wrong = run.CurrentBoard.Solution[row, col] % run.CurrentBoard.Size + 1;
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, wrong));
        }

        private static void AssertCleared(RunDirector run, IEnumerable<(int r, int c)> cells)
        {
            var list = cells.ToList();
            Assert.IsNotEmpty(list, "the test needs digits to erase");
            foreach (var (r, c) in list)
                Assert.AreEqual(0, run.CurrentBoard.Cells[r, c], $"({r},{c}) should be erased");
        }

        private static void AssertKept(RunDirector run, IEnumerable<(int r, int c)> cells)
        {
            foreach (var (r, c) in cells)
                Assert.AreEqual(run.CurrentBoard.Solution[r, c], run.CurrentBoard.Cells[r, c], $"({r},{c}) should stay");
        }
    }
}
