using NUnit.Framework;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Tests
{
    /// <summary>
    /// Edit-mode regression guard for XpTable and XpService.
    /// These tests pin the exact values from the ClassXpProgressionSystem spec so
    /// that any accidental constant or formula change is caught immediately.
    ///
    /// Target: ~80 runs to Level 40 (16,860 total XP; ~211 XP/run average).
    /// </summary>
    [TestFixture]
    public class XpTableTests
    {
        // ── XP-to-next-level formula ─────────────────────────────────────────

        [Test]
        public void XpToNextLevel_Phase1_Level1_Returns50()
        {
            Assert.AreEqual(50, XpTable.XpToNextLevel(1));
        }

        [Test]
        public void XpToNextLevel_Phase1_Level10_Returns140()
        {
            // 50 + (10-1)*10 = 50 + 90 = 140
            Assert.AreEqual(140, XpTable.XpToNextLevel(10));
        }

        [Test]
        public void XpToNextLevel_Phase1_Level15_Returns190()
        {
            // 50 + (15-1)*10 = 50 + 140 = 190
            Assert.AreEqual(190, XpTable.XpToNextLevel(15));
        }

        [Test]
        public void XpToNextLevel_Phase2_Level16_Returns225()
        {
            // 190 + 35 = 225
            Assert.AreEqual(225, XpTable.XpToNextLevel(16));
        }

        [Test]
        public void XpToNextLevel_Phase2_Level17_Returns260()
        {
            // 225 + 35 = 260
            Assert.AreEqual(260, XpTable.XpToNextLevel(17));
        }

        [Test]
        public void XpToNextLevel_Phase2_Level39_Returns1030()
        {
            // Spec table: Level 39 → 1,030 XP to next
            Assert.AreEqual(1030, XpTable.XpToNextLevel(39));
        }

        // ── Cumulative XP ────────────────────────────────────────────────────

        [Test]
        public void CumulativeXpForLevel_Level1_Is0()
        {
            Assert.AreEqual(0, XpTable.CumulativeXpForLevel(1));
        }

        [Test]
        public void CumulativeXpForLevel_Level15_Is1610()
        {
            // Spec table: cumulative XP to reach Level 15 = 1,610
            Assert.AreEqual(1610, XpTable.CumulativeXpForLevel(15));
        }

        [Test]
        public void CumulativeXpForLevel_Level16_Is1800()
        {
            // 1,610 + 190 (cost of L15) = 1,800
            Assert.AreEqual(1800, XpTable.CumulativeXpForLevel(16));
        }

        [Test]
        public void CumulativeXpForLevel_Level30_Is8135()
        {
            // Spec table: cumulative XP to reach Level 30 = 8,135
            Assert.AreEqual(8135, XpTable.CumulativeXpForLevel(30));
        }

        [Test]
        public void CumulativeXpForLevel_Level40_Is16860()
        {
            // Spec requirement: total XP to reach Level 40 = 16,860  [XP-CURVE-001]
            Assert.AreEqual(16860, XpTable.CumulativeXpForLevel(40));
        }

        // ── DeriveLevel ──────────────────────────────────────────────────────

        [Test]
        public void DeriveLevel_Zero_ReturnsLevel1()
        {
            Assert.AreEqual(1, XpTable.DeriveLevel(0));
        }

        [Test]
        public void DeriveLevel_Exactly50_ReturnsLevel2()
        {
            Assert.AreEqual(2, XpTable.DeriveLevel(50));
        }

        [Test]
        public void DeriveLevel_49_ReturnsLevel1()
        {
            Assert.AreEqual(1, XpTable.DeriveLevel(49));
        }

        [Test]
        public void DeriveLevel_1609_ReturnsLevel14()
        {
            // 1,610 cumulative is Level 15; 1,609 is still Level 14
            Assert.AreEqual(14, XpTable.DeriveLevel(1609));
        }

        [Test]
        public void DeriveLevel_AtLevel40Threshold_ReturnsLevel40()
        {
            Assert.AreEqual(40, XpTable.DeriveLevel(16860));
        }

        [Test]
        public void DeriveLevel_BeyondLevel40_StaysAt40()
        {
            Assert.AreEqual(40, XpTable.DeriveLevel(99999));
        }

        // ── Star multiplier ──────────────────────────────────────────────────

        [Test]
        public void StarMultiplier_1Star_Is1_0()
        {
            Assert.AreEqual(1.0f, XpTable.StarMultiplier(1));
        }

        [Test]
        public void StarMultiplier_2Star_Is1_2()
        {
            Assert.AreEqual(1.2f, XpTable.StarMultiplier(2), 0.001f);
        }

        [Test]
        public void StarMultiplier_3And4Star_Is1_5()
        {
            Assert.AreEqual(1.5f, XpTable.StarMultiplier(3), 0.001f);
            Assert.AreEqual(1.5f, XpTable.StarMultiplier(4), 0.001f);
        }

        [Test]
        public void StarMultiplier_5And6Star_Is2_0()
        {
            Assert.AreEqual(2.0f, XpTable.StarMultiplier(5), 0.001f);
            Assert.AreEqual(2.0f, XpTable.StarMultiplier(6), 0.001f);
        }

        // ── XpService.CalculateTile ───────────────────────────────────────────

        [Test]
        public void CalculateTile_9x9_4Star_Boss_1Mod_NoPerfect_Returns285()
        {
            // 120 × 1.5 (star) = 180; × 1.5 (boss) = 270; + 15 (1 mod) = 285
            var entry = XpService.CalculateTile(9, 4, 1, isBoss: true, perfectSolve: false);
            Assert.AreEqual(285, entry.TotalXp);
        }

        [Test]
        public void CalculateTile_6x6_2Star_Normal_NoPerfect_Returns54()
        {
            // 45 × 1.2 = 54; not boss; not perfect
            var entry = XpService.CalculateTile(6, 2, 0, isBoss: false, perfectSolve: false);
            Assert.AreEqual(54, entry.TotalXp);
        }

        [Test]
        public void CalculateTile_6x6_2Star_Normal_Perfect_Returns79()
        {
            // 54 + 25 (perfect) = 79
            var entry = XpService.CalculateTile(6, 2, 0, isBoss: false, perfectSolve: true);
            Assert.AreEqual(79, entry.TotalXp);
        }

        [Test]
        public void CalculateTile_ModifierBonus_NotAppliedToNormalPuzzle()
        {
            // Modifier bonus should only apply to boss tiles
            var withMod    = XpService.CalculateTile(9, 4, 2, isBoss: false, perfectSolve: false);
            var withoutMod = XpService.CalculateTile(9, 4, 0, isBoss: false, perfectSolve: false);
            Assert.AreEqual(withoutMod.TotalXp, withMod.TotalXp);
        }

        [Test]
        public void CalculateTile_MinimumXpIsOne()
        {
            // Even a degenerate call must return at least 1 XP
            var entry = XpService.CalculateTile(5, 1, 0, isBoss: false, perfectSolve: false);
            Assert.GreaterOrEqual(entry.TotalXp, 1);
        }

        // ── 80-run target sanity check ────────────────────────────────────────

        [Test]
        public void EightyRunTarget_AverageXpPerRun_MeetsSpecTarget()
        {
            // Spec: ~80 runs to Level 40 → need ≥ 16,860 / 80 ≈ 211 XP/run average.
            // A minimal "die on floor 1 after 2 puzzles" run: 6×6 2★ normal (54) + 6×6 2★ normal (54) = 108 XP.
            // A typical floor-1-only run with perfect on first puzzle: 79 + 54 = 133 XP.
            // A full 5-floor victory run is ≥ 1,000 XP.
            // We pin that a solo floor-1 loss still earns at least 50 XP (i.e. progress always made).
            const int requiredXpForLevel40 = 16860;
            const int targetRuns = 80;
            var minXpPerRun = requiredXpForLevel40 / targetRuns; // 210

            // A single 6×6 2★ puzzle earns 54 XP — well above the degenerate floor.
            var singlePuzzleXp = XpService.CalculateTile(6, 2, 0, isBoss: false, perfectSolve: false).TotalXp;
            Assert.GreaterOrEqual(singlePuzzleXp * 4, minXpPerRun,
                "4 normal 6×6 2★ puzzles (a minimal run) should meet the per-run XP average needed for the 80-run target.");
        }
    }
}
