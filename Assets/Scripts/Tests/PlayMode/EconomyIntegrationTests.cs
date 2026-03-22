using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

/// <summary>
/// Integration tests for gold economy, XP calculations, and item/relic systems.
/// </summary>
public class EconomyIntegrationTests : TestDriver
{
    // ── XP curve invariants ──────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator XpCurve_Level40_Requires16860Xp()
    {
        int totalXp = 0;
        for (int level = 1; level <= 40; level++)
        {
            totalXp += XpService.XpToNextLevel(level);
        }

        Assert.AreEqual(16860, totalXp, "Total XP to level 40 should be 16,860");
        yield return null;
    }

    [UnityTest]
    public IEnumerator XpCurve_DeriveLevel_CorrectAtBoundaries()
    {
        // Level 1: 0 XP
        Assert.AreEqual(1, XpService.DeriveLevel(0));

        // Level 2: 50 XP threshold
        Assert.AreEqual(1, XpService.DeriveLevel(49));
        Assert.AreEqual(2, XpService.DeriveLevel(50));

        // Level 40: at total cap
        Assert.AreEqual(40, XpService.DeriveLevel(16860));

        // Beyond cap: still level 40
        Assert.AreEqual(40, XpService.DeriveLevel(99999));

        yield return null;
    }

    // ── XP per tile ──────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator XpPerTile_BaseXpByBoardSize()
    {
        // Base XP: 5x5=30, 6x6=45, 7x7=60, 8x8=90, 9x9=120
        var expected = new[] { (5, 30), (6, 45), (7, 60), (8, 90), (9, 120) };

        foreach (var (size, baseXp) in expected)
        {
            var entry = XpService.CalculateTile(size, stars: 1, activeModCount: 0, isBoss: false, perfectSolve: false);
            Assert.AreEqual(baseXp, entry.BaseXp,
                $"Board size {size} base XP should be {baseXp}");
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator XpPerTile_StarMultiplier_ScalesCorrectly()
    {
        // Star multipliers: 1★=×1.0, 2★=×1.2, 3-4★=×1.5, 5-6★=×2.0
        int baseXp = 120; // 9x9
        var entry1 = XpService.CalculateTile(9, 1, 0, false, false);
        var entry2 = XpService.CalculateTile(9, 2, 0, false, false);
        var entry5 = XpService.CalculateTile(9, 5, 0, false, false);

        Assert.AreEqual(baseXp, entry1.TotalXp, "1★ should be ×1.0");
        Assert.AreEqual((int)(baseXp * 1.2f), entry2.TotalXp, "2★ should be ×1.2");
        Assert.AreEqual(baseXp * 2, entry5.TotalXp, "5★ should be ×2.0");

        yield return null;
    }

    [UnityTest]
    public IEnumerator XpPerTile_BossMultiplier_Adds50Percent()
    {
        var normal = XpService.CalculateTile(9, 1, 0, isBoss: false, perfectSolve: false);
        var boss = XpService.CalculateTile(9, 1, 0, isBoss: true, perfectSolve: false);

        Assert.AreEqual((int)(normal.TotalXp * 1.5f), boss.TotalXp,
            "Boss should multiply by ×1.5");

        yield return null;
    }

    [UnityTest]
    public IEnumerator XpPerTile_PerfectBonus_Adds25Flat()
    {
        var normal = XpService.CalculateTile(9, 1, 0, false, perfectSolve: false);
        var perfect = XpService.CalculateTile(9, 1, 0, false, perfectSolve: true);

        Assert.AreEqual(normal.TotalXp + 25, perfect.TotalXp,
            "Perfect bonus should add +25 flat");

        yield return null;
    }

    [UnityTest]
    public IEnumerator XpPerTile_ModifierBonus_Adds15PerMod()
    {
        var noMod = XpService.CalculateTile(9, 1, 0, isBoss: true, false);
        var oneMod = XpService.CalculateTile(9, 1, 1, isBoss: true, false);
        var twoMod = XpService.CalculateTile(9, 1, 2, isBoss: true, false);

        Assert.AreEqual(noMod.TotalXp + 15, oneMod.TotalXp,
            "One modifier should add +15 (boss only)");
        Assert.AreEqual(noMod.TotalXp + 30, twoMod.TotalXp,
            "Two modifiers should add +30 (boss only)");

        yield return null;
    }

    // ── Gold economy ─────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator GoldFormula_BaseGoldByBoardSize()
    {
        // Base gold: 5x5=20, 6x6=30, 7x7=45, 8x8=65, 9x9=100
        var expected = new[] { (5, 20), (6, 30), (7, 45), (8, 65), (9, 100) };

        foreach (var (size, baseGold) in expected)
        {
            int actual = FormulaService.BaseGoldForBoardSize(size);
            Assert.AreEqual(baseGold, actual,
                $"Board size {size} base gold should be {baseGold}");
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator PencilBuyCost_EscalatesCorrectly()
    {
        // Formula: 20 + (20 × purchases)
        Assert.AreEqual(20, FormulaService.PencilBuyCost(0), "First buy should cost 20");
        Assert.AreEqual(40, FormulaService.PencilBuyCost(1), "Second buy should cost 40");
        Assert.AreEqual(60, FormulaService.PencilBuyCost(2), "Third buy should cost 60");
        Assert.AreEqual(100, FormulaService.PencilBuyCost(4), "Fifth buy should cost 100");

        yield return null;
    }

    [UnityTest]
    public IEnumerator RerollCost_EscalatesCorrectly()
    {
        // Formula: 20 + (20 × rerolls)
        Assert.AreEqual(20, FormulaService.RerollCost(0), "First reroll should cost 20");
        Assert.AreEqual(40, FormulaService.RerollCost(1), "Second reroll should cost 40");
        Assert.AreEqual(60, FormulaService.RerollCost(2), "Third reroll should cost 60");

        yield return null;
    }

    // ── Star density formula ─────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator StarDensity_Formula_Correct()
    {
        // (stars + 3) × 0.1
        Assert.AreEqual(0.4f, StarDensityService.MissingPercentForStars(1), 0.001f, "1★ = 40%");
        Assert.AreEqual(0.5f, StarDensityService.MissingPercentForStars(2), 0.001f, "2★ = 50%");
        Assert.AreEqual(0.6f, StarDensityService.MissingPercentForStars(3), 0.001f, "3★ = 60%");
        Assert.AreEqual(0.7f, StarDensityService.MissingPercentForStars(4), 0.001f, "4★ = 70%");
        Assert.AreEqual(0.8f, StarDensityService.MissingPercentForStars(5), 0.001f, "5★ = 80%");
        Assert.AreEqual(0.9f, StarDensityService.MissingPercentForStars(6), 0.001f, "6★ = 90%");
        Assert.AreEqual(1.0f, StarDensityService.MissingPercentForStars(7), 0.001f, "7★ = 100%");

        yield return null;
    }
}
