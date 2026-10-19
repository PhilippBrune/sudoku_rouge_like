using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: BOSS-MOD-006] [REQ: GEN-SOLVE-002] [REQ: GEN-SOLVE-003] Batch B5 of the modifier catalog
    // (modifier-catalog-plan.md): the extra-house and magic-square rules on hand-made boards, the generator building
    // solutions that keep them, and the pre-baked boss board matching on every solution-shaping rule (GAP-051).
    [TestFixture]
    [Requirement("BOSS-MOD-006", "GEN-SOLVE-002")]
    public sealed class CatalogExtraHouseTests
    {
        private static SudokuBoard Empty()
        {
            var regions = new int[9, 9];
            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
                regions[r, c] = (r / 3) * 3 + c / 3;
            return new SudokuBoard(9, new int[9, 9], new int[9, 9], regions);
        }

        [Test]
        [Requirement("BOSS-LIB-030", "BOSS-LIB-031", "BOSS-LIB-032")]
        public void ExtraHouses_RejectARepeatInTheirHouseOnly()
        {
            var board = Empty();
            board.Cells[0, 0] = 4;
            var x = new ModifierOverlayData { IsXSudoku = true };
            Assert.IsFalse(new ExtraHouseRule(BossModifierId.XSudoku).IsValid(board, 5, 5, 4, x), "same main diagonal");
            Assert.IsTrue(new ExtraHouseRule(BossModifierId.XSudoku).IsValid(board, 5, 6, 4, x), "off the diagonal");
            Assert.IsTrue(new ExtraHouseRule(BossModifierId.XSudoku).IsValid(board, 5, 5, 4, new ModifierOverlayData()), "rule off");

            board.Cells[1, 1] = 7;   // inside the top-left Hyper window (rows 1-3, columns 1-3)
            var hyper = new ModifierOverlayData { IsHyperSudoku = true };
            Assert.IsFalse(new ExtraHouseRule(BossModifierId.HyperSudoku).IsValid(board, 3, 3, 7, hyper));
            Assert.IsTrue(new ExtraHouseRule(BossModifierId.HyperSudoku).IsValid(board, 4, 4, 7, hyper), "row 4 is outside the windows");

            var disjoint = new ModifierOverlayData { IsDisjointGroups = true };
            Assert.IsFalse(new ExtraHouseRule(BossModifierId.DisjointGroups).IsValid(board, 3, 6, 4, disjoint), "top-left cell of its box, like (0,0)");
            Assert.IsTrue(new ExtraHouseRule(BossModifierId.DisjointGroups).IsValid(board, 3, 7, 4, disjoint));
        }

        [Test]
        [Requirement("BOSS-LIB-053")]
        public void ExtraRegion_RejectsARepeatInsideIt()
        {
            var overlay = new ModifierOverlayData();
            var region = new ExtraRegion();
            region.Cells.AddRange(new[] { new CellCoord(0, 2), new CellCoord(0, 3), new CellCoord(1, 3) });
            overlay.ExtraRegions.Add(region);
            var board = Empty();
            board.Cells[0, 2] = 5;
            var rule = new ExtraHouseRule(BossModifierId.ExtraRegion);
            Assert.IsFalse(rule.IsValid(board, 1, 3, 5, overlay));
            Assert.IsTrue(rule.IsValid(board, 1, 3, 6, overlay));
            Assert.IsTrue(rule.IsValid(board, 1, 4, 5, overlay), "outside the region");
        }

        [Test]
        [Requirement("BOSS-LIB-068")]
        public void MagicSquare_EveryLineAddsUpTo15()
        {
            var overlay = new ModifierOverlayData();
            var box = new KillerCage { CageType = KillerCageType.MagicSquare, Sum = 15 };
            for (var i = 0; i < 9; i++) box.Cells.Add(new CellCoord(i / 3, i % 3));
            overlay.KillerCages.Add(box);
            var board = Empty();
            board.Cells[0, 0] = 2;
            board.Cells[0, 1] = 7;
            var rule = new MagicSquareRule();
            Assert.IsTrue(rule.IsValid(board, 0, 2, 6, overlay), "2 + 7 + 6 = 15");
            Assert.IsFalse(rule.IsValid(board, 0, 2, 5, overlay));
            Assert.IsFalse(rule.IsValid(board, 1, 1, 9, overlay), "the diagonal 2 + 9 + ? would need 4, the column 7 + 9 needs -1");
            Assert.IsTrue(rule.IsValid(board, 1, 1, 5, overlay));
        }

        [Test]
        [Requirement("GEN-SOLVE-002", "GEN-SOLVE-003")]
        public void Generator_BuildsSolutionsThatKeepTheShape()
        {
            var mods = new List<BossModifierId>
                { BossModifierId.XSudoku, BossModifierId.HyperSudoku, BossModifierId.DisjointGroups, BossModifierId.MagicSquareRegion };
            foreach (var seed in new[] { 3, 14, 27 })
            {
                var board = SudokuGenerator.CreatePuzzleWithUniquenessCheck(9, 0.5f, seed, shape: SolutionShape.For(mods, 9));
                var overlay = ModifierGeometryGenerator.Generate(board, mods, seed);
                Assert.IsTrue(overlay.IsXSudoku, $"seed {seed}: diagonals");
                Assert.IsTrue(overlay.IsHyperSudoku, $"seed {seed}: windows");
                Assert.IsTrue(overlay.IsDisjointGroups, $"seed {seed}: disjoint groups");
                Assert.IsTrue(overlay.KillerCages.Exists(c => c.CageType == KillerCageType.MagicSquare), $"seed {seed}: magic square");
                Assert.IsTrue(board.IsUniqueSolution, $"seed {seed}: givens chosen for one solution");
            }
        }

        [Test]
        public void MagicSquareBox_IsLeftOutWithConsecutiveRules()
        {
            var magic = new List<BossModifierId> { BossModifierId.MagicSquareRegion };
            Assert.IsTrue(SolutionShape.For(magic, 9).MagicSquareBox);
            Assert.IsNull(SolutionShape.For(magic, 8), "no 3×3 boxes on 8×8");
            Assert.IsNull(SolutionShape.For(new List<BossModifierId> { BossModifierId.MagicSquareRegion, BossModifierId.Nonconsecutive }, 9),
                "a magic square always holds 7 beside 6");
        }

        // GAP-051: a boss board baked without Entropy (or a B5 rule) must not be reused when the player picks it at the gate.
        [Test]
        [Requirement("GEN-SOLVE-002")]
        public void PreBakedBoard_MatchesOnEverySolutionShapingRule()
        {
            var match = typeof(RunDirector).GetMethod("StructuralModsMatch", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(match);
            bool Matches(BossModifierId[] a, BossModifierId[] b) => (bool)match.Invoke(null, new object[] { a, b });
            var none = new BossModifierId[0];
            foreach (var mod in new[] { BossModifierId.EntropyGlobal, BossModifierId.XSudoku, BossModifierId.HyperSudoku,
                         BossModifierId.DisjointGroups, BossModifierId.MagicSquareRegion, BossModifierId.Antiknight })
                Assert.IsFalse(Matches(new[] { mod }, none), $"{mod} added at the gate");
            Assert.IsTrue(Matches(new[] { BossModifierId.KillerCages }, none), "geometry rules do not need a new board");
            Assert.IsTrue(BossService.ShapesSolution(BossModifierId.EntropyGlobal));
            Assert.IsFalse(BossService.ShapesSolution(BossModifierId.ExtraRegion), "extra regions are read off the solution");
        }
    }
}
