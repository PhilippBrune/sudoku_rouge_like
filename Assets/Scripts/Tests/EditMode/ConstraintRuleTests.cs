using System;
using NUnit.Framework;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    /// <summary>
    /// Edit-mode unit tests for Nonconsecutive, Antiknight, and all three Kropki
    /// rule variants. Each test targets one specific behaviour to make failures easy
    /// to diagnose.
    /// </summary>
    [TestFixture]
    public class ConstraintRuleTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Creates an all-empty 9×9 board with standard 3×3 regions.</summary>
        private static SudokuBoard Empty9x9()
        {
            var solution  = new int[9, 9];
            var cells     = new int[9, 9];
            var regionMap = new int[9, 9];
            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
                regionMap[r, c] = (r / 3) * 3 + (c / 3);
            return new SudokuBoard(9, solution, cells, regionMap);
        }

        private static ModifierOverlayData WhiteDotOverlay(int r1, int c1, int r2, int c2)
        {
            var o = new ModifierOverlayData();
            o.KropkiDots.Add(new KropkiDot
                { CellA = new CellCoord(r1, c1), CellB = new CellCoord(r2, c2), IsBlack = false, SumValue = 0 });
            return o;
        }

        private static ModifierOverlayData BlackDotOverlay(int r1, int c1, int r2, int c2)
        {
            var o = new ModifierOverlayData();
            o.KropkiDots.Add(new KropkiDot
                { CellA = new CellCoord(r1, c1), CellB = new CellCoord(r2, c2), IsBlack = true });
            return o;
        }

        // ── NonconsecutiveRule ────────────────────────────────────────────────────

        [Test]
        public void Nonconsecutive_EmptyNeighbours_Valid()
        {
            var board = Empty9x9();
            var rule  = new NonconsecutiveRule();
            for (var v = 1; v <= 9; v++)
                Assert.IsTrue(rule.IsValid(board, 4, 4, v, null),
                    $"value {v} with all-empty neighbours must be valid");
        }

        [Test]
        public void Nonconsecutive_NeighbourDiff2_Valid()
        {
            var board = Empty9x9();
            board.Cells[1, 1] = 3;   // neighbour above (0,1) relative to (1,1) is unused;
            board.Cells[3, 1] = 3;   // place 3 at south neighbour of (2,1)
            var rule = new NonconsecutiveRule();
            Assert.IsTrue(rule.IsValid(board, 2, 1, 5, null),
                "diff=2 from orthogonal neighbour must be allowed");
        }

        [Test]
        public void Nonconsecutive_ValuePlusOne_Adjacent_Invalid()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 2;   // east neighbour of (0,0) has 2
            var rule  = new NonconsecutiveRule();
            Assert.IsFalse(rule.IsValid(board, 0, 0, 3, null),
                "placing 3 next to 2 must be rejected (diff=1)");
        }

        [Test]
        public void Nonconsecutive_ValueMinusOne_Adjacent_Invalid()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 4;   // east neighbour of (0,0) has 4
            var rule  = new NonconsecutiveRule();
            Assert.IsFalse(rule.IsValid(board, 0, 0, 3, null),
                "placing 3 next to 4 must be rejected (diff=1)");
        }

        [Test]
        public void Nonconsecutive_AllFourDirections_Checked()
        {
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = {  0, 0,-1, 1 };
            string[] names = { "north", "south", "west", "east" };
            var rule = new NonconsecutiveRule();

            for (var d = 0; d < 4; d++)
            {
                var board = Empty9x9();
                board.Cells[4 + dr[d], 4 + dc[d]] = 6;   // consecutive neighbour
                Assert.IsFalse(rule.IsValid(board, 4, 4, 7, null),
                    $"{names[d]} neighbour=6, placing 7 should be invalid");
                Assert.IsFalse(rule.IsValid(board, 4, 4, 5, null),
                    $"{names[d]} neighbour=6, placing 5 should be invalid");
            }
        }

        [Test]
        public void Nonconsecutive_Generation_SolutionRespected()
        {
            var board = SudokuGenerator.CreatePuzzle(9, 0.5f, seed: 42, nonconsecutive: true);
            var sol   = board.Solution;
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = {  0, 0,-1, 1 };

            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
            for (var d = 0; d < 4; d++)
            {
                var nr = r + dr[d]; var nc = c + dc[d];
                if (nr < 0 || nr >= 9 || nc < 0 || nc >= 9) continue;
                Assert.AreNotEqual(1, Math.Abs(sol[r, c] - sol[nr, nc]),
                    $"Solution violates nonconsecutive at ({r},{c})={sol[r,c]} ↔ ({nr},{nc})={sol[nr,nc]}");
            }
        }

        // ── AntiknightRule ────────────────────────────────────────────────────────

        [Test]
        public void Antiknight_NoKnightMoveConflict_Valid()
        {
            var board = Empty9x9();
            var rule  = new AntiknightRule();
            Assert.IsTrue(rule.IsValid(board, 4, 4, 5, null));
        }

        [Test]
        public void Antiknight_SameValueAtKnightMove_Invalid()
        {
            var board = Empty9x9();
            board.Cells[2, 3] = 5;   // (2,3) is a knight's move from (4,4)
            var rule  = new AntiknightRule();
            Assert.IsFalse(rule.IsValid(board, 4, 4, 5, null));
        }

        [Test]
        public void Antiknight_DifferentValueAtKnightMove_Valid()
        {
            var board = Empty9x9();
            board.Cells[2, 3] = 7;
            var rule  = new AntiknightRule();
            Assert.IsTrue(rule.IsValid(board, 4, 4, 5, null));
        }

        [Test]
        public void Antiknight_AllEightKnightOffsets_Detected()
        {
            int[] Dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] Dc = { -1,  1, -2,  2,-2, 2,-1, 1 };
            var rule = new AntiknightRule();

            for (var k = 0; k < 8; k++)
            {
                var board = Empty9x9();
                board.Cells[4 + Dr[k], 4 + Dc[k]] = 3;
                Assert.IsFalse(rule.IsValid(board, 4, 4, 3, null),
                    $"Knight offset ({Dr[k]},{Dc[k]}) should be detected as a conflict");
            }
        }

        [Test]
        public void Antiknight_Generation_SolutionRespected()
        {
            var board = SudokuGenerator.CreatePuzzle(9, 0.5f, seed: 99, antiknight: true);
            var sol   = board.Solution;
            int[] Dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] Dc = { -1,  1, -2,  2,-2, 2,-1, 1 };

            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
            for (var k = 0; k < 8; k++)
            {
                var nr = r + Dr[k]; var nc = c + Dc[k];
                if (nr < 0 || nr >= 9 || nc < 0 || nc >= 9) continue;
                Assert.AreNotEqual(sol[r, c], sol[nr, nc],
                    $"Solution violates antiknight: ({r},{c})={sol[r,c]} ↔ ({nr},{nc})={sol[nr,nc]}");
            }
        }

        // ── DifferenceKropkiRule (white dot) ──────────────────────────────────────

        [Test]
        public void DifferenceKropki_WhiteDot_Diff1_Valid()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 4;
            var rule = new DifferenceKropkiRule();
            Assert.IsTrue(rule.IsValid(board, 0, 0, 3, WhiteDotOverlay(0, 0, 0, 1)),
                "placing 3 next to 4 with white dot (diff=1) must be valid");
            Assert.IsTrue(rule.IsValid(board, 0, 0, 5, WhiteDotOverlay(0, 0, 0, 1)),
                "placing 5 next to 4 with white dot (diff=1) must be valid");
        }

        [Test]
        public void DifferenceKropki_WhiteDot_Diff2_Invalid()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 4;
            var rule = new DifferenceKropkiRule();
            Assert.IsFalse(rule.IsValid(board, 0, 0, 2, WhiteDotOverlay(0, 0, 0, 1)),
                "diff=2 on white dot must be invalid");
            Assert.IsFalse(rule.IsValid(board, 0, 0, 6, WhiteDotOverlay(0, 0, 0, 1)),
                "diff=2 on white dot must be invalid");
        }

        [Test]
        public void DifferenceKropki_PartnerEmpty_Valid()
        {
            // Partner cell not yet filled — cannot constrain yet
            var board   = Empty9x9();
            var overlay = WhiteDotOverlay(0, 0, 0, 1);
            var rule    = new DifferenceKropkiRule();
            Assert.IsTrue(rule.IsValid(board, 0, 0, 7, overlay),
                "empty partner must not constrain the placement");
        }

        [Test]
        public void DifferenceKropki_SumDot_Skipped()
        {
            // SumValue>0 dots are sum-kropki dots and must NOT be treated as white dots
            var board = Empty9x9();
            board.Cells[0, 1] = 4;
            var overlay = new ModifierOverlayData();
            overlay.KropkiDots.Add(new KropkiDot
            {
                CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1),
                IsBlack = false, SumValue = 9
            });
            var rule = new DifferenceKropkiRule();
            // diff=2: would be invalid for a real white dot but must pass for a sum dot
            Assert.IsTrue(rule.IsValid(board, 0, 0, 6, overlay),
                "DifferenceKropkiRule must skip SumKropki dots (SumValue > 0)");
        }

        [Test]
        public void DifferenceKropki_BlackDot_Skipped()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 4;
            var rule = new DifferenceKropkiRule();
            // Black dot — DifferenceKropkiRule must not enforce diff=1 on it
            Assert.IsTrue(rule.IsValid(board, 0, 0, 2, BlackDotOverlay(0, 0, 0, 1)),
                "DifferenceKropkiRule must skip black dots");
        }

        [Test]
        public void DifferenceKropki_Bidirectional_CellBToA()
        {
            // Rule must fire when the placed cell is CellB, not just CellA
            var board = Empty9x9();
            board.Cells[0, 0] = 4;   // CellA is already placed
            var rule = new DifferenceKropkiRule();
            Assert.IsFalse(rule.IsValid(board, 0, 1, 6, WhiteDotOverlay(0, 0, 0, 1)),
                "rule must fire regardless of which cell in the pair is being placed");
            Assert.IsTrue(rule.IsValid(board, 0, 1, 3, WhiteDotOverlay(0, 0, 0, 1)));
        }

        // ── RatioKropkiRule (black dot) ───────────────────────────────────────────

        [Test]
        public void RatioKropki_BlackDot_Ratio2_Valid()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 4;
            var rule = new RatioKropkiRule();
            Assert.IsTrue(rule.IsValid(board, 0, 0, 2, BlackDotOverlay(0, 0, 0, 1)),
                "2 × 2 = 4 → ratio=2 must be valid");
            Assert.IsTrue(rule.IsValid(board, 0, 0, 8, BlackDotOverlay(0, 0, 0, 1)),
                "8 = 2 × 4 → ratio=2 must be valid");
        }

        [Test]
        public void RatioKropki_BlackDot_NotRatio2_Invalid()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 4;
            var rule = new RatioKropkiRule();
            Assert.IsFalse(rule.IsValid(board, 0, 0, 3, BlackDotOverlay(0, 0, 0, 1)),
                "3:4 is not ratio=2 — must be invalid");
            Assert.IsFalse(rule.IsValid(board, 0, 0, 6, BlackDotOverlay(0, 0, 0, 1)),
                "6:4 is not ratio=2 — must be invalid");
        }

        [Test]
        public void RatioKropki_Bidirectional_SmallOverLarge()
        {
            // Ratio check must work when the larger value is placed first (CellA=8, place 4 at CellB)
            var board = Empty9x9();
            board.Cells[0, 0] = 8;
            var rule = new RatioKropkiRule();
            Assert.IsTrue(rule.IsValid(board, 0, 1, 4, BlackDotOverlay(0, 0, 0, 1)));
            Assert.IsFalse(rule.IsValid(board, 0, 1, 3, BlackDotOverlay(0, 0, 0, 1)));
        }

        [Test]
        public void RatioKropki_WhiteDot_Skipped()
        {
            var board = Empty9x9();
            board.Cells[0, 1] = 4;
            var rule = new RatioKropkiRule();
            // White dot — RatioKropkiRule must not fire (no black dot present)
            Assert.IsTrue(rule.IsValid(board, 0, 0, 3, WhiteDotOverlay(0, 0, 0, 1)),
                "RatioKropkiRule must skip white dots");
        }

        // ── FullKropkiRule ────────────────────────────────────────────────────────

        [Test]
        public void RatioKropki_AndDistanceGe2_AllowAnAdjacentRatioPair()
        {
            var board = Empty9x9();
            board.Cells[4, 5] = 6;
            var overlay = BlackDotOverlay(4, 4, 4, 5);

            Assert.IsTrue(new RatioKropkiRule().IsValid(board, 4, 4, 3, overlay));
            Assert.IsTrue(new DistanceGe2Rule().IsValid(board, 4, 4, 3, overlay));
        }

        [Test]
        public void FullKropki_NotActive_NoConstraint()
        {
            var board   = Empty9x9();
            board.Cells[0, 1] = 4;
            var overlay = new ModifierOverlayData { FullKropkiNegativeInference = false };
            var rule    = new FullKropkiRule();
            Assert.IsTrue(rule.IsValid(board, 0, 0, 3, overlay),
                "inactive FullKropki must not constrain diff=1");
            Assert.IsTrue(rule.IsValid(board, 0, 0, 2, overlay),
                "inactive FullKropki must not constrain ratio=2");
        }

        [Test]
        public void FullKropki_UndottedPair_Diff1_Invalid()
        {
            var board   = Empty9x9();
            board.Cells[0, 1] = 4;
            var overlay = new ModifierOverlayData { FullKropkiNegativeInference = true };
            var rule    = new FullKropkiRule();
            Assert.IsFalse(rule.IsValid(board, 0, 0, 3, overlay),
                "undotted pair with diff=1 must be invalid under FullKropki negative inference");
            Assert.IsFalse(rule.IsValid(board, 0, 0, 5, overlay));
        }

        [Test]
        public void FullKropki_UndottedPair_Ratio2_Invalid()
        {
            var board   = Empty9x9();
            board.Cells[0, 1] = 4;
            var overlay = new ModifierOverlayData { FullKropkiNegativeInference = true };
            var rule    = new FullKropkiRule();
            Assert.IsFalse(rule.IsValid(board, 0, 0, 2, overlay),
                "undotted pair with ratio=2 must be invalid under FullKropki negative inference");
            Assert.IsFalse(rule.IsValid(board, 0, 0, 8, overlay));
        }

        [Test]
        public void FullKropki_UndottedPair_SafeValue_Valid()
        {
            var board   = Empty9x9();
            board.Cells[0, 1] = 4;
            var overlay = new ModifierOverlayData { FullKropkiNegativeInference = true };
            var rule    = new FullKropkiRule();
            // 6 next to 4: diff=2 (not 1), ratio=6/4 (not 2) → valid
            Assert.IsTrue(rule.IsValid(board, 0, 0, 6, overlay));
            // 1 next to 4: diff=3, 1×2≠4 and 4×2≠1 → valid
            Assert.IsTrue(rule.IsValid(board, 0, 0, 1, overlay));
        }

        [Test]
        public void FullKropki_DottedPair_SkippedByFullRule()
        {
            // FullKropkiRule must NOT constrain pairs that have a dot
            // (DifferenceKropkiRule / RatioKropkiRule handle those)
            var board   = Empty9x9();
            board.Cells[0, 1] = 4;
            var overlay = new ModifierOverlayData { FullKropkiNegativeInference = true };
            overlay.KropkiDots.Add(new KropkiDot
                { CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1), IsBlack = false });
            var rule = new FullKropkiRule();
            // White dot exists → FullKropkiRule skips → returns true regardless of diff
            Assert.IsTrue(rule.IsValid(board, 0, 0, 3, overlay),
                "FullKropkiRule must skip pairs that have a dot");
        }

        [Test]
        public void FullKropki_EmptyNeighbour_Valid()
        {
            var board   = Empty9x9();
            var overlay = new ModifierOverlayData { FullKropkiNegativeInference = true };
            var rule    = new FullKropkiRule();
            // Neighbour is empty — cannot constrain yet
            Assert.IsTrue(rule.IsValid(board, 0, 0, 3, overlay));
        }
    }
}
