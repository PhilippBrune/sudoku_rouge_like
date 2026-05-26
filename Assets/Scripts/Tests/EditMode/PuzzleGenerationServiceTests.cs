using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class PuzzleGenerationServiceTests
    {
        [Test]
        public void Generate_NoModifiers_ReturnsInitialBoardWithEmptyOverlay()
        {
            var service = new PuzzleGenerationService();
            var config = new LevelConfig();
            var board = Empty4x4Board();

            var result = service.Generate(
                config,
                board,
                seed: 123,
                boardFactory: null,
                settings: TestSettings());

            Assert.IsTrue(result.IsSuccess);
            Assert.AreSame(board, result.Board);
            Assert.IsTrue(result.HasCompleteOverlay);
            Assert.AreEqual(0, result.Overlay.CellMarkers.Count);
            Assert.AreEqual(0, result.Metrics.BoardRetriesAttempted);
            Assert.AreEqual(0, result.Metrics.OverlaySeedAttempts);
        }

        [Test]
        public void Generate_IncompleteOverlayReportsMissingModifierWithoutFailingLevel()
        {
            var service = new PuzzleGenerationService();
            var config = ConfigWith(BossModifierId.EvenOdd);

            var result = service.Generate(
                config,
                Empty4x4Board(),
                seed: 123,
                boardFactory: null,
                settings: TestSettings(),
                overlayFactory: EmptyOverlay);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.HasCompleteOverlay);
            CollectionAssert.Contains(result.MissingModifiers, BossModifierId.EvenOdd);
            Assert.AreEqual(1, result.Metrics.BoardRetriesAttempted);
            Assert.AreEqual(2, result.Metrics.OverlaySeedAttempts);
        }

        [Test]
        public void Generate_UsesRetryBoardWhenLaterAttemptProducesCompleteOverlay()
        {
            var service = new PuzzleGenerationService();
            var config = ConfigWith(BossModifierId.EvenOdd);
            var retryBoard = Empty4x4Board();
            retryBoard.Cells[0, 0] = 1;

            var result = service.Generate(
                config,
                Empty4x4Board(),
                seed: 123,
                boardFactory: (_, __, ___) => retryBoard,
                settings: new PuzzleGenerationSettings
                {
                    BoardRetries = 2,
                    OverlaySeedAttempts = 1,
                    TimeBudgetMs = 1000,
                    ParallelOverlaySeeds = false
                },
                overlayFactory: OverlayOnlyForRetryBoard);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.HasCompleteOverlay);
            Assert.AreSame(retryBoard, result.Board);
            Assert.AreEqual(2, result.Metrics.BoardRetriesAttempted);
            Assert.AreEqual(2, result.Metrics.OverlaySeedAttempts);
        }

        [Test]
        public void Generate_RequiresUniqueModifierSolution_WhenCompetitiveConfigRequestsIt()
        {
            var service = new PuzzleGenerationService();
            var config = ConfigWith(BossModifierId.EvenOdd);
            config.RequireUniqueModifierSolution = true;

            var result = service.Generate(
                config,
                Ambiguous4x4Puzzle(),
                seed: 123,
                boardFactory: null,
                settings: TestSettings(),
                overlayFactory: UniqueEvenOddOverlay);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.UniqueModifierSolutionVerified);
            Assert.IsTrue(result.Metrics.RequiredUniqueModifierSolution);
            Assert.IsTrue(result.Metrics.UniqueSolutionVerified);
            Assert.AreEqual(1, result.Metrics.UniqueValidationAttempts);
            Assert.AreEqual(0, result.Metrics.UniqueValidationFailures);
        }

        [Test]
        public void Generate_FailsCompetitiveConfig_WhenUniqueModifierSolutionCannotBeVerified()
        {
            var service = new PuzzleGenerationService();
            var config = ConfigWith(BossModifierId.EvenOdd);
            config.RequireUniqueModifierSolution = true;

            var result = service.Generate(
                config,
                Ambiguous4x4Puzzle(),
                seed: 123,
                boardFactory: null,
                settings: TestSettings(),
                overlayFactory: NonUniqueEvenOddOverlay);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PuzzleGenerationFailureReason.UniqueSolutionNotFound, result.FailureReason);
            Assert.IsTrue(result.Metrics.RequiredUniqueModifierSolution);
            Assert.IsFalse(result.Metrics.UniqueSolutionVerified);
            Assert.AreEqual(2, result.Metrics.UniqueValidationAttempts);
            Assert.AreEqual(2, result.Metrics.UniqueValidationFailures);
            Assert.That(result.Metrics.LastError, Does.Contain("Unique modifier solution"));
        }

        [Test]
        public void Generate_AllowsNonUniqueModifierPuzzle_WhenConfigDoesNotRequireGate()
        {
            var service = new PuzzleGenerationService();
            var config = ConfigWith(BossModifierId.EvenOdd);

            var result = service.Generate(
                config,
                Ambiguous4x4Puzzle(),
                seed: 123,
                boardFactory: null,
                settings: TestSettings(),
                overlayFactory: NonUniqueEvenOddOverlay);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.UniqueModifierSolutionVerified);
            Assert.IsFalse(result.Metrics.RequiredUniqueModifierSolution);
            Assert.AreEqual(0, result.Metrics.UniqueValidationAttempts);
        }

        [Test]
        public void Generate_ReturnsBudgetExceeded_WhenBoardRetryExceedsDeadline()
        {
            var service = new PuzzleGenerationService();
            var config = ConfigWith(BossModifierId.EvenOdd);

            var result = service.Generate(
                config,
                Empty4x4Board(),
                seed: 123,
                boardFactory: DeadlineLoopBoardFactory,
                settings: new PuzzleGenerationSettings
                {
                    BoardRetries = 2,
                    OverlaySeedAttempts = 1,
                    TimeBudgetMs = 1,
                    ParallelOverlaySeeds = false
                },
                overlayFactory: (_, __, ___, ____) => null);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PuzzleGenerationFailureReason.BudgetExceeded, result.FailureReason);
            Assert.IsTrue(result.Metrics.BudgetExceeded);
        }

        [Test]
        public void Generate_ReturnsCancelled_WhenCancellationTokenIsAlreadyCancelled()
        {
            var service = new PuzzleGenerationService();
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                var result = service.Generate(
                    ConfigWith(BossModifierId.EvenOdd),
                    Empty4x4Board(),
                    seed: 123,
                    boardFactory: (_, __, ___) => Empty4x4Board(),
                    settings: TestSettings(),
                    cancellationToken: cts.Token,
                    overlayFactory: EmptyOverlay);

                Assert.IsFalse(result.IsSuccess);
                Assert.AreEqual(PuzzleGenerationFailureReason.Cancelled, result.FailureReason);
            }
        }

        [Test]
        public void ScoredModifierModes_RequestUniqueModifierSolutionGate()
        {
            var spiritTrials = new SpiritTrialsService(seed: 123);
            var apprentice = spiritTrials.BuildTrialLevel(SpiritTrialsTier.Apprentice, allowIrregularPuzzles: false);
            var adept = spiritTrials.BuildTrialLevel(SpiritTrialsTier.Adept, allowIrregularPuzzles: false);
            var seasonal = SeasonalChallengeService.BuildChallengeConfig(2026, 5);

            Assert.IsFalse(apprentice.RequireUniqueModifierSolution);
            Assert.AreEqual(0, apprentice.ActiveModifiers.Count);

            Assert.IsTrue(adept.RequireUniqueModifierSolution);
            Assert.Greater(adept.ActiveModifiers.Count, 0);

            Assert.IsTrue(seasonal.RequireUniqueModifierSolution);
            Assert.Greater(seasonal.ActiveModifiers.Count, 0);
        }

        [Test]
        public void GenerationSettings_DefaultBudgetsMatchReadinessContract()
        {
            AssertSettings(
                PuzzleGenerationSettings.AsyncBossDefault,
                boardRetries: 3,
                overlaySeedAttempts: 8,
                timeBudgetMs: 2500,
                parallelOverlaySeeds: true);
            AssertSettings(
                PuzzleGenerationSettings.HighComplexityBossDefault,
                boardRetries: 3,
                overlaySeedAttempts: 4,
                timeBudgetMs: 4000,
                parallelOverlaySeeds: true);
            AssertSettings(
                PuzzleGenerationSettings.StructuralModifierDefault,
                boardRetries: 4,
                overlaySeedAttempts: 6,
                timeBudgetMs: 6000,
                parallelOverlaySeeds: true);
            AssertSettings(
                PuzzleGenerationSettings.SynchronousDefault,
                boardRetries: 5,
                overlaySeedAttempts: 12,
                timeBudgetMs: 4000,
                parallelOverlaySeeds: false);
        }

        [Test]
        public void AsyncGenerationSettings_UseHighComplexityBudgetOnlyForModifierHeavyBosses()
        {
            var standardBoss = BossConfigWithModifierCount(6);
            var heavyBoss = BossConfigWithModifierCount(7);
            var heavyNonBoss = BossConfigWithModifierCount(7);
            heavyNonBoss.IsBoss = false;
            var structuralNonBoss = new LevelConfig { BoardSize = 7, IsBoss = false };
            structuralNonBoss.ActiveModifiers.Add(BossModifierId.Antiknight);

            AssertSameSettings(
                PuzzleGenerationSettings.AsyncBossDefault,
                SelectAsyncGenerationSettingsForTests(standardBoss));
            AssertSameSettings(
                PuzzleGenerationSettings.HighComplexityBossDefault,
                SelectAsyncGenerationSettingsForTests(heavyBoss));
            AssertSameSettings(
                PuzzleGenerationSettings.AsyncBossDefault,
                SelectAsyncGenerationSettingsForTests(heavyNonBoss));
            AssertSameSettings(
                PuzzleGenerationSettings.StructuralModifierDefault,
                SelectAsyncGenerationSettingsForTests(structuralNonBoss));
        }

        [Test]
        public void BoardGenerationFallback_DropsOnlyBoardShapingModifiers()
        {
            var config = new LevelConfig
            {
                BoardSize = 8,
                IsBoss = true,
                RequireUniqueModifierSolution = true
            };
            config.ActiveModifiers.Add(BossModifierId.FortressCells);
            config.ActiveModifiers.Add(BossModifierId.AntiBishop);
            config.ActiveModifiers.Add(BossModifierId.ConsecutiveLine);

            Assert.IsTrue(TryBuildBoardGenerationFallbackConfigForTests(
                config,
                out var fallback,
                out var dropped));

            CollectionAssert.AreEqual(
                new[] { BossModifierId.AntiBishop },
                dropped);
            CollectionAssert.AreEqual(
                new[] { BossModifierId.FortressCells, BossModifierId.ConsecutiveLine },
                fallback.ActiveModifiers);
            CollectionAssert.Contains(config.ActiveModifiers, BossModifierId.AntiBishop);
            Assert.IsTrue(fallback.RequireUniqueModifierSolution);
        }

        [Test]
        public void BoardSizeGate_SkipsSevenBySevenForStructuralModifiers()
        {
            var single = new LevelConfig { BoardSize = 7 };
            single.ActiveModifiers.Add(BossModifierId.Antiknight);

            var distance = new LevelConfig { BoardSize = 7 };
            distance.ActiveModifiers.Add(BossModifierId.DistanceGe2);

            var stacked = new LevelConfig { BoardSize = 6 };
            stacked.ActiveModifiers.Add(BossModifierId.Nonconsecutive);
            stacked.ActiveModifiers.Add(BossModifierId.NonconsecDiagonal);

            EnsureBoardSizeSupportsActiveModifiersForTests(single);
            EnsureBoardSizeSupportsActiveModifiersForTests(distance);
            EnsureBoardSizeSupportsActiveModifiersForTests(stacked);

            Assert.AreEqual(8, single.BoardSize);
            Assert.AreEqual(8, distance.BoardSize);
            Assert.AreEqual(8, stacked.BoardSize);
        }

        [Test]
        public void GeneratedConfigSanitizer_RemovesAntiBishopAndBlockedModifierPair()
        {
            var config = new LevelConfig { BoardSize = 8 };
            config.ActiveModifiers.Add(BossModifierId.EvenOdd);
            config.ActiveModifiers.Add(BossModifierId.AntiBishop);
            config.ActiveModifiers.Add(BossModifierId.RatioKropki);
            config.ActiveModifiers.Add(BossModifierId.DistanceGe2);

            SanitizeGeneratedLevelConfigForTests(config);

            CollectionAssert.AreEqual(
                new[] { BossModifierId.EvenOdd, BossModifierId.RatioKropki },
                config.ActiveModifiers);
        }

        [Test]
        public void PuzzleGenerationReadinessDocument_TracksBudgetMatrixAndReleaseCaveats()
        {
            var doc = File.ReadAllText("docs/puzzle-generation-readiness.md");

            StringAssert.Contains("Generation Budget Matrix", doc);
            StringAssert.Contains("High-complexity async boss", doc);
            StringAssert.Contains("7+", doc);
            StringAssert.Contains("AsyncBossDefault", doc);
            StringAssert.Contains("HighComplexityBossDefault", doc);
            StringAssert.Contains("StructuralModifierDefault", doc);
            StringAssert.Contains("SynchronousDefault", doc);
            StringAssert.Contains("Garden Run modifier uniqueness: recommended, not currently required.", doc);
            StringAssert.Contains("Unity Profiler captures are still required", doc);
            StringAssert.Contains("MissingModifiers", doc);
            StringAssert.Contains("RequireUniqueModifierSolution", doc);
        }

        private static PuzzleGenerationSettings TestSettings()
        {
            return new PuzzleGenerationSettings
            {
                BoardRetries = 1,
                OverlaySeedAttempts = 2,
                TimeBudgetMs = 1000,
                ParallelOverlaySeeds = false
            };
        }

        private static LevelConfig ConfigWith(BossModifierId modifier)
        {
            var config = new LevelConfig
            {
                BoardSize = 4,
                Intensity = BossModifierIntensity.Medium
            };
            config.ActiveModifiers.Add(modifier);
            return config;
        }

        private static LevelConfig BossConfigWithModifierCount(int count)
        {
            var config = new LevelConfig
            {
                BoardSize = 9,
                IsBoss = true,
                Intensity = BossModifierIntensity.High
            };

            var modifiers = new[]
            {
                BossModifierId.EvenOdd,
                BossModifierId.FogOfWar,
                BossModifierId.GermanWhispers,
                BossModifierId.Thermo,
                BossModifierId.KillerCages,
                BossModifierId.ArrowSums,
                BossModifierId.RenbanLines
            };

            for (var i = 0; i < count && i < modifiers.Length; i++)
                config.ActiveModifiers.Add(modifiers[i]);

            return config;
        }

        private static SudokuBoard DeadlineLoopBoardFactory(
            LevelConfig config,
            int seed,
            GenerationDeadline deadline)
        {
            while (true)
                deadline.ThrowIfExceeded();
        }

        private static PuzzleGenerationSettings SelectAsyncGenerationSettingsForTests(LevelConfig config)
        {
            var method = typeof(RunDirector).GetMethod(
                "SelectAsyncGenerationSettings",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "RunDirector.SelectAsyncGenerationSettings should remain visible to the release budget test.");
            return (PuzzleGenerationSettings)method.Invoke(null, new object[] { config });
        }

        private static bool TryBuildBoardGenerationFallbackConfigForTests(
            LevelConfig config,
            out LevelConfig fallback,
            out List<BossModifierId> dropped)
        {
            var method = typeof(RunDirector).GetMethod(
                "TryBuildBoardGenerationFallbackConfig",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "RunDirector.TryBuildBoardGenerationFallbackConfig should stay available for fallback contract tests.");

            var args = new object[] { config, null, null };
            var ok = (bool)method.Invoke(null, args);
            fallback = (LevelConfig)args[1];
            dropped = (List<BossModifierId>)args[2];
            return ok;
        }

        private static void EnsureBoardSizeSupportsActiveModifiersForTests(LevelConfig config)
        {
            var method = typeof(RunDirector).GetMethod(
                "EnsureBoardSizeSupportsActiveModifiers",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            method.Invoke(null, new object[] { config });
        }

        private static void SanitizeGeneratedLevelConfigForTests(LevelConfig config)
        {
            var method = typeof(RunDirector).GetMethod(
                "SanitizeGeneratedLevelConfig",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            method.Invoke(null, new object[] { config });
        }

        private static void AssertSameSettings(
            PuzzleGenerationSettings expected,
            PuzzleGenerationSettings actual)
        {
            AssertSettings(
                actual,
                expected.BoardRetries,
                expected.OverlaySeedAttempts,
                expected.TimeBudgetMs,
                expected.ParallelOverlaySeeds);
        }

        private static void AssertSettings(
            PuzzleGenerationSettings settings,
            int boardRetries,
            int overlaySeedAttempts,
            int timeBudgetMs,
            bool parallelOverlaySeeds)
        {
            Assert.AreEqual(boardRetries, settings.BoardRetries);
            Assert.AreEqual(overlaySeedAttempts, settings.OverlaySeedAttempts);
            Assert.AreEqual(timeBudgetMs, settings.TimeBudgetMs);
            Assert.AreEqual(parallelOverlaySeeds, settings.ParallelOverlaySeeds);
        }

        private static ModifierOverlayData EmptyOverlay(
            SudokuBoard board,
            List<BossModifierId> modifiers,
            int seed,
            BossModifierIntensity intensity)
        {
            return new ModifierOverlayData();
        }

        private static ModifierOverlayData OverlayOnlyForRetryBoard(
            SudokuBoard board,
            List<BossModifierId> modifiers,
            int seed,
            BossModifierIntensity intensity)
        {
            if (board.Cells[0, 0] != 1)
                return new ModifierOverlayData();

            var overlay = new ModifierOverlayData();
            overlay.CellMarkers.Add(new CellMarker
            {
                Cell = new CellCoord(0, 0),
                Type = MarkerType.Odd
            });
            return overlay;
        }

        private static ModifierOverlayData UniqueEvenOddOverlay(
            SudokuBoard board,
            List<BossModifierId> modifiers,
            int seed,
            BossModifierIntensity intensity)
        {
            var overlay = new ModifierOverlayData();
            overlay.CellMarkers.Add(new CellMarker
            {
                Cell = new CellCoord(0, 0),
                Type = MarkerType.Odd
            });
            return overlay;
        }

        private static ModifierOverlayData NonUniqueEvenOddOverlay(
            SudokuBoard board,
            List<BossModifierId> modifiers,
            int seed,
            BossModifierIntensity intensity)
        {
            var overlay = new ModifierOverlayData();
            overlay.CellMarkers.Add(new CellMarker
            {
                Cell = new CellCoord(0, 2),
                Type = MarkerType.Odd
            });
            return overlay;
        }

        private static SudokuBoard Empty4x4Board()
        {
            return new SudokuBoard(4, new int[4, 4], new int[4, 4], RegionMap4x4());
        }

        private static SudokuBoard Ambiguous4x4Puzzle()
        {
            var solution = new[,]
            {
                { 1, 2, 3, 4 },
                { 3, 4, 1, 2 },
                { 2, 1, 4, 3 },
                { 4, 3, 2, 1 }
            };

            var cells = new[,]
            {
                { 0, 0, 3, 4 },
                { 3, 4, 1, 2 },
                { 0, 0, 4, 3 },
                { 4, 3, 2, 1 }
            };

            return new SudokuBoard(4, solution, cells, RegionMap4x4());
        }

        private static int[,] RegionMap4x4()
        {
            var map = new int[4, 4];
            for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                map[r, c] = (r / 2) * 2 + (c / 2);
            return map;
        }
    }
}
