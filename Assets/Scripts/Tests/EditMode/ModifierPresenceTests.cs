using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: BOSS-MOD-004] A chosen modifier counts as present only with its own geometry (GAP-047): one line modifier
    // never stands in for another, a white dot never for a black one. The plain Killer and Arrow rules ignore cage and
    // arrow subtypes, which have their own rules (modifier-catalog-plan.md, Phase 0).
    [TestFixture]
    [Requirement("BOSS-MOD-004")]
    public sealed class ModifierPresenceTests
    {
        private static ModifierOverlayData WithLine(LineType type)
        {
            var overlay = new ModifierOverlayData();
            overlay.Lines.Add(new ModifierLine { Type = type, Cells = { new CellCoord(0, 0), new CellCoord(0, 1) } });
            return overlay;
        }

        [Test]
        public void ThermoIsMissing_WhenOnlyWhisperLinesWereGenerated()
        {
            var overlay = WithLine(LineType.GermanWhispers);
            var missing = PuzzleGenerationService.FindMissingModifiers(
                new List<BossModifierId> { BossModifierId.GermanWhispers, BossModifierId.Thermo }, overlay);
            CollectionAssert.AreEqual(new[] { BossModifierId.Thermo }, missing);
        }

        [Test]
        public void EachLineModifier_CountsOnlyItsOwnLineType()
        {
            var pairs = new (BossModifierId id, LineType type)[]
            {
                (BossModifierId.GermanWhispers, LineType.GermanWhispers), (BossModifierId.DutchWhispers, LineType.DutchWhispers),
                (BossModifierId.ParityLines, LineType.ParityLine), (BossModifierId.RenbanLines, LineType.RenbanLine),
                (BossModifierId.Palindrome, LineType.Palindrome), (BossModifierId.Thermo, LineType.Thermo),
                (BossModifierId.BetweenLines, LineType.BetweenLine), (BossModifierId.ConsecutiveLine, LineType.ConsecutiveLine),
                (BossModifierId.SlowThermo, LineType.SlowThermo), (BossModifierId.UniqueSetLine, LineType.UniqueSetLine)
            };
            foreach (var (id, type) in pairs)
            foreach (var (_, other) in pairs)
                Assert.AreEqual(type == other, ModifierPresence.IsPresent(id, WithLine(other)), $"{id} with a {other} line");
        }

        [Test]
        public void KropkiKinds_AreToldApart()
        {
            var white = new ModifierOverlayData();
            white.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1) });
            var black = new ModifierOverlayData();
            black.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1), IsBlack = true });
            var sum = new ModifierOverlayData();
            sum.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1), SumValue = 7 });

            Assert.IsTrue(ModifierPresence.IsPresent(BossModifierId.DifferenceKropki, white));
            Assert.IsFalse(ModifierPresence.IsPresent(BossModifierId.DifferenceKropki, black));
            Assert.IsFalse(ModifierPresence.IsPresent(BossModifierId.DifferenceKropki, sum));
            Assert.IsTrue(ModifierPresence.IsPresent(BossModifierId.RatioKropki, black));
            Assert.IsFalse(ModifierPresence.IsPresent(BossModifierId.RatioKropki, white));
            Assert.IsTrue(ModifierPresence.IsPresent(BossModifierId.SumKropki, sum));
            Assert.IsFalse(ModifierPresence.IsPresent(BossModifierId.FullKropki, white), "Full Kropki needs its full set");
        }

        [Test]
        public void GlobalRulesDebuffsAndPressure_NeedNoGeometry_UngeneratedCatalogIdsDo()
        {
            var empty = new ModifierOverlayData();
            Assert.IsTrue(ModifierPresence.IsPresent(BossModifierId.Antiknight, empty));
            Assert.IsTrue(ModifierPresence.IsPresent(BossModifierId.EntropyGlobal, empty));
            Assert.IsTrue(ModifierPresence.IsPresent(BossModifierId.RowWipe, empty));
            Assert.IsTrue(ModifierPresence.IsPresent(BossModifierId.CountdownFill, empty));
            Assert.IsFalse(ModifierPresence.IsPresent(BossModifierId.XSudoku, empty), "no generator: never present by accident");
            Assert.IsFalse(ModifierPresence.IsPresent(BossModifierId.Thermo, null));
        }

        [Test]
        public void KillerAndArrowRules_IgnoreOtherCageAndArrowTypes()
        {
            var board = new SudokuBoard(4, new int[4, 4], new int[4, 4], new int[4, 4]);
            var product = new ModifierOverlayData();
            product.KillerCages.Add(new KillerCage
                { Sum = 6, CageType = KillerCageType.Product, Cells = { new CellCoord(0, 0), new CellCoord(0, 1) } });
            Assert.IsTrue(new KillerCagesRule().IsValid(board, 0, 0, 9, product), "a product cage is not a sum cage");

            var average = new ModifierOverlayData();
            average.Arrows.Add(new ArrowConstraint
                { CircleCell = new CellCoord(1, 1), ArrowType = ArrowType.Average, ArrowCells = { new CellCoord(1, 2) } });
            Assert.IsTrue(new ArrowSumsRule().IsValid(board, 1, 1, 1, average), "an average arrow is not a sum arrow");

            var plain = new ModifierOverlayData();
            plain.KillerCages.Add(new KillerCage { Sum = 3, Cells = { new CellCoord(0, 0), new CellCoord(0, 1) } });
            Assert.IsFalse(new KillerCagesRule().IsValid(board, 0, 0, 9, plain), "plain sum cages still check their sum");
        }
    }
}
