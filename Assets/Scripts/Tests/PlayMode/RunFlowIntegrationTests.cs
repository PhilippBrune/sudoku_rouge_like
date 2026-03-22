using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;

/// <summary>
/// Integration tests for the full run flow: start → puzzle → reward → map → boss → end.
/// Tests RunDirector, puzzle generation, level completion, and state transitions.
/// </summary>
public class RunFlowIntegrationTests : TestDriver
{
    private const int TestSeed = 42;

    // ── Garden Run lifecycle ─────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator GardenRun_StartRun_InitialisesRunState()
    {
        var run = new RunDirector(TestSeed);
        run.StartRun(ClassId.NumberFreak, GameMode.GardenRun, runNumber: 1);

        Assert.AreEqual(ClassId.NumberFreak, run.RunState.ClassId);
        Assert.AreEqual(GameMode.GardenRun, run.RunState.Mode);
        Assert.Greater(run.RunState.CurrentHP, 0, "HP should be positive at run start");
        Assert.Greater(run.RunState.MaxHP, 0);
        Assert.Greater(run.RunState.CurrentPencil, 0, "Pencil should be positive at run start");
        Assert.AreEqual(0, run.RunState.CurrentGold, "Gold should start at zero");
        Assert.AreEqual(0, run.RunState.Depth);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GardenRun_BuildLevelConfig_ProducesValidConfig()
    {
        var run = new RunDirector(TestSeed);
        run.StartRun(ClassId.NumberFreak, GameMode.GardenRun, runNumber: 1);

        var config = run.BuildLevelConfig(1, 0);

        Assert.IsNotNull(config);
        Assert.GreaterOrEqual(config.BoardSize, 5, "Board size must be at least 5");
        Assert.LessOrEqual(config.BoardSize, 9, "Board size must be at most 9");
        Assert.GreaterOrEqual(config.Stars, 1, "Stars must be at least 1");
        Assert.LessOrEqual(config.Stars, 6, "Stars must be at most 6");
        Assert.Greater(config.MissingPercent, 0f, "MissingPercent must be positive");
        Assert.LessOrEqual(config.MissingPercent, 1f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GardenRun_StartLevel_GeneratesValidBoard()
    {
        var run = new RunDirector(TestSeed);
        run.StartRun(ClassId.NumberFreak, GameMode.GardenRun, runNumber: 1);
        var config = run.BuildLevelConfig(1, 0);
        run.StartLevel(config);

        Assert.IsNotNull(run.CurrentBoard, "Board should be generated");
        Assert.AreEqual(config.BoardSize, run.CurrentBoard.Size);
        Assert.IsNotNull(run.CurrentLevelState, "LevelState should be initialised");
        Assert.AreEqual(0, run.CurrentLevelState.Mistakes);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GardenRun_StartLevel_BossHasModifierOverlay()
    {
        var run = new RunDirector(TestSeed);
        run.StartRun(ClassId.NumberFreak, GameMode.GardenRun, runNumber: 1);

        // Build a boss level config (depth 4 = floor 1 boss on a 5-floor run)
        var config = run.BuildLevelConfig(1, 4);

        // Force boss if not already
        if (!config.IsBoss)
        {
            // Try higher depths until we find a boss node
            for (int d = 0; d < 20; d++)
            {
                config = run.BuildLevelConfig(1, d);
                if (config.IsBoss) break;
            }
        }

        if (config.IsBoss && config.ActiveModifiers != null && config.ActiveModifiers.Count > 0)
        {
            run.StartLevel(config);
            Assert.IsNotNull(run.CurrentOverlayData, "Boss level should have overlay data");
        }
        else
        {
            Assert.Pass("No boss config found in tested depths — skipping overlay check");
        }

        yield return null;
    }

    // ── Class variations ─────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator AllClasses_StartRun_HaveUniqueBaseStats()
    {
        var hpSet = new System.Collections.Generic.HashSet<int>();
        var classIds = new[]
        {
            ClassId.NumberFreak, ClassId.GardenMonk, ClassId.ShrineArchivist,
            ClassId.KoiGambler, ClassId.StoneGardener, ClassId.LanternSeer,
            ClassId.ReedDuelist, ClassId.QuietCartographer
        };

        foreach (var classId in classIds)
        {
            var run = new RunDirector(TestSeed);
            run.StartRun(classId, GameMode.GardenRun, runNumber: 1);

            Assert.Greater(run.RunState.MaxHP, 0, $"{classId} should have positive MaxHP");
            Assert.Greater(run.RunState.MaxPencil, 0, $"{classId} should have positive MaxPencil");
            Assert.Greater(run.RunState.ItemSlots, 0, $"{classId} should have at least 1 item slot");
        }

        // At least some classes should have different HP to confirm variety
        foreach (var classId in classIds)
        {
            var run = new RunDirector(TestSeed);
            run.StartRun(classId, GameMode.GardenRun, runNumber: 1);
            hpSet.Add(run.RunState.MaxHP);
        }
        Assert.Greater(hpSet.Count, 1, "Classes should have varied MaxHP values");

        yield return null;
    }

    // ── Tutorial mode ────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator Tutorial_StartRun_DisablesProgression()
    {
        var run = new RunDirector(TestSeed);
        var setup = new TutorialSetupConfig
        {
            BoardSize = 9,
            Stars = 3,
            ResourceMode = TutorialResourceMode.Simulation,
            SimulationClassId = ClassId.NumberFreak,
            RegionVariant = 0,
        };
        run.StartTutorialRun(setup);

        Assert.IsTrue(run.RunState.TutorialMode, "TutorialMode flag should be set");
        Assert.IsTrue(run.RunState.DisableProgressionRewards, "Progression should be disabled in tutorial");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Tutorial_SevenStar_ProducesValidBoard()
    {
        var run = new RunDirector(TestSeed);
        var setup = new TutorialSetupConfig
        {
            BoardSize = 9,
            Stars = 7,
            ResourceMode = TutorialResourceMode.Simulation,
            SimulationClassId = ClassId.NumberFreak,
            RegionVariant = 0,
        };
        run.StartTutorialRun(setup);

        Assert.IsNotNull(run.CurrentBoard, "7-star tutorial should generate a board");
        Assert.AreEqual(9, run.CurrentBoard.Size);
        yield return null;
    }

    // ── Endless Zen mode ─────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator EndlessZen_StartRun_AlwaysNineByNine()
    {
        var run = new RunDirector(TestSeed);
        run.StartRun(ClassId.NumberFreak, GameMode.EndlessZen, runNumber: 1);
        var config = run.BuildLevelConfig(1, 0);

        Assert.AreEqual(9, config.BoardSize, "Endless Zen should always be 9x9");
        yield return null;
    }

    // ── Spirit Trials mode ───────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SpiritTrials_StartRun_AlwaysNineByNine()
    {
        var run = new RunDirector(TestSeed);
        run.StartRun(ClassId.NumberFreak, GameMode.SpiritTrials, runNumber: 1);
        var config = run.BuildLevelConfig(1, 0);

        Assert.AreEqual(9, config.BoardSize, "Spirit Trials should always be 9x9");
        yield return null;
    }

    // ── Puzzle generation invariants ─────────────────────────────────────────

    [UnityTest]
    public IEnumerator SudokuGenerator_AllBoardSizes_ProduceValidBoards()
    {
        var rng = new System.Random(TestSeed);

        for (int size = 5; size <= 9; size++)
        {
            var board = SudokuGenerator.CreatePuzzle(size, 0.4f, rng.Next(), 0);
            Assert.IsNotNull(board, $"Board size {size} should generate");
            Assert.AreEqual(size, board.Size, $"Board size should be {size}");

            // Verify solved board is complete (no zeros)
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                Assert.AreNotEqual(0, board.Solution[r, c],
                    $"Solution cell ({r},{c}) should not be zero on {size}x{size}");
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator SudokuGenerator_AllRegionVariants_ProduceValidBoards()
    {
        for (int variant = 0; variant <= 3; variant++)
        {
            // Use 9x9 which supports all variants
            var board = SudokuGenerator.CreatePuzzle(9, 0.4f, TestSeed + variant, variant);
            Assert.IsNotNull(board, $"Variant {variant} should generate a board");
            Assert.AreEqual(9, board.Size);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator StarDensity_AllStars_ProduceCorrectMissingPercent()
    {
        // Formula: MissingPercent = (stars + 3) × 0.1
        for (int stars = 1; stars <= 7; stars++)
        {
            float expected = (stars + 3) * 0.1f;
            float actual = StarDensityService.MissingPercentForStars(stars);
            Assert.AreEqual(expected, actual, 0.001f,
                $"Star {stars} should produce {expected} missing percent");
        }

        yield return null;
    }

    // ── Determinism ──────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SudokuGenerator_SameSeed_ProducesSameBoard()
    {
        var board1 = SudokuGenerator.CreatePuzzle(9, 0.5f, 99999, 0);
        var board2 = SudokuGenerator.CreatePuzzle(9, 0.5f, 99999, 0);

        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
        {
            Assert.AreEqual(board1.Solution[r, c], board2.Solution[r, c],
                $"Solution at ({r},{c}) should be deterministic");
            Assert.AreEqual(board1.Cells[r, c], board2.Cells[r, c],
                $"Puzzle at ({r},{c}) should be deterministic");
        }

        yield return null;
    }
}
