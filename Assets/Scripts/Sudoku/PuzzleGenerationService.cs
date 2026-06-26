using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public readonly struct GenerationDeadline
    {
        private readonly long _deadlineTimestamp;
        private readonly CancellationToken _cancellationToken;

        public GenerationDeadline(int timeBudgetMs, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            if (timeBudgetMs > 0)
            {
                var budgetTicks = (long)(timeBudgetMs / 1000.0 * Stopwatch.Frequency);
                _deadlineTimestamp = Stopwatch.GetTimestamp() + Math.Max(1, budgetTicks);
            }
            else
            {
                _deadlineTimestamp = 0;
            }
        }

        public void ThrowIfExceeded()
        {
            if (_cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(_cancellationToken);

            if (_deadlineTimestamp > 0 && Stopwatch.GetTimestamp() >= _deadlineTimestamp)
                throw new TimeoutException("Puzzle generation exceeded the configured time budget.");
        }
    }

    public delegate SudokuBoard PuzzleBoardFactory(LevelConfig config, int seed, GenerationDeadline deadline);

    public delegate ModifierOverlayData ModifierOverlayFactory(
        SudokuBoard board,
        List<BossModifierId> modifiers,
        int seed,
        BossModifierIntensity intensity);

    public enum PuzzleGenerationFailureReason
    {
        None,
        BoardGenerationFailed,
        BudgetExceeded,
        Cancelled,
        TaskFaulted,
        UniqueSolutionNotFound
    }

    public sealed class PuzzleGenerationSettings
    {
        public int BoardRetries = 3;
        public int OverlaySeedAttempts = 8;
        public int TimeBudgetMs = 2500;
        public bool ParallelOverlaySeeds = true;

        public static PuzzleGenerationSettings AsyncBossDefault => new PuzzleGenerationSettings
        {
            BoardRetries = 3,
            OverlaySeedAttempts = 8,
            TimeBudgetMs = 2500,
            ParallelOverlaySeeds = true
        };

        public static PuzzleGenerationSettings SynchronousDefault => new PuzzleGenerationSettings
        {
            BoardRetries = 5,
            OverlaySeedAttempts = 12,
            TimeBudgetMs = 4000,
            ParallelOverlaySeeds = false
        };

        /// <summary>
        /// Settings for high-complexity boss puzzles (7+ active modifiers, floors 4–5).
        /// Reduces overlay seed attempts so the first valid overlay is accepted quickly;
        /// extends the time budget to absorb the extra board-generation cost on large grids.
        /// </summary>
        public static PuzzleGenerationSettings HighComplexityBossDefault => new PuzzleGenerationSettings
        {
            BoardRetries = 3,
            OverlaySeedAttempts = 4,
            TimeBudgetMs = 4000,
            ParallelOverlaySeeds = true
        };

        public static PuzzleGenerationSettings StructuralModifierDefault => new PuzzleGenerationSettings
        {
            BoardRetries = 4,
            OverlaySeedAttempts = 6,
            TimeBudgetMs = 6000,
            ParallelOverlaySeeds = true
        };
    }

    public sealed class PuzzleGenerationMetrics
    {
        public int BoardRetriesAttempted { get; internal set; }
        public int OverlaySeedAttempts { get; internal set; }
        public int BoardGenerationFailures { get; internal set; }
        public int OverlayGenerationFailures { get; internal set; }
        public int UniqueValidationAttempts { get; internal set; }
        public int UniqueValidationFailures { get; internal set; }
        public long ElapsedMilliseconds { get; internal set; }
        public bool BudgetExceeded { get; internal set; }
        public bool RequiredUniqueModifierSolution { get; internal set; }
        public bool UniqueSolutionVerified { get; internal set; }
        public string LastError { get; internal set; }
    }

    public sealed class PuzzleGenerationResult
    {
        private readonly List<BossModifierId> _missingModifiers;

        private PuzzleGenerationResult(
            bool success,
            SudokuBoard board,
            ModifierOverlayData overlay,
            PuzzleGenerationFailureReason failureReason,
            PuzzleGenerationMetrics metrics,
            List<BossModifierId> missingModifiers,
            bool uniqueModifierSolutionVerified)
        {
            IsSuccess = success;
            Board = board;
            Overlay = overlay ?? new ModifierOverlayData();
            FailureReason = failureReason;
            Metrics = metrics ?? new PuzzleGenerationMetrics();
            _missingModifiers = missingModifiers ?? new List<BossModifierId>();
            UniqueModifierSolutionVerified = uniqueModifierSolutionVerified;
        }

        public bool IsSuccess { get; }
        public SudokuBoard Board { get; }
        public ModifierOverlayData Overlay { get; }
        public PuzzleGenerationFailureReason FailureReason { get; }
        public PuzzleGenerationMetrics Metrics { get; }
        public IReadOnlyList<BossModifierId> MissingModifiers => _missingModifiers;
        public bool HasCompleteOverlay => _missingModifiers.Count == 0;
        public bool UniqueModifierSolutionVerified { get; }

        public static PuzzleGenerationResult Success(
            SudokuBoard board,
            ModifierOverlayData overlay,
            PuzzleGenerationMetrics metrics,
            List<BossModifierId> missingModifiers,
            bool uniqueModifierSolutionVerified = false)
        {
            return new PuzzleGenerationResult(
                true,
                board,
                overlay,
                PuzzleGenerationFailureReason.None,
                metrics,
                missingModifiers,
                uniqueModifierSolutionVerified);
        }

        public static PuzzleGenerationResult Failure(
            PuzzleGenerationFailureReason reason,
            PuzzleGenerationMetrics metrics)
        {
            return new PuzzleGenerationResult(false, null, null, reason, metrics, null, false);
        }
    }

    public sealed class PuzzleGenerationService
    {
        public PuzzleGenerationResult Generate(
            LevelConfig config,
            SudokuBoard initialBoard,
            int seed,
            PuzzleBoardFactory boardFactory,
            PuzzleGenerationSettings settings = null,
            CancellationToken cancellationToken = default,
            ModifierOverlayFactory overlayFactory = null)
        {
            settings ??= PuzzleGenerationSettings.AsyncBossDefault;
            overlayFactory ??= ModifierGeometryGenerator.Generate;

            var metrics = new PuzzleGenerationMetrics();
            var stopwatch = Stopwatch.StartNew();
            var deadline = new GenerationDeadline(settings.TimeBudgetMs, cancellationToken);

            try
            {
                deadline.ThrowIfExceeded();
                if (config == null)
                {
                    metrics.LastError = "LevelConfig is null.";
                    return PuzzleGenerationResult.Failure(PuzzleGenerationFailureReason.BoardGenerationFailed, metrics);
                }

                if (initialBoard == null)
                {
                    metrics.LastError = "Initial board generation returned null.";
                    return PuzzleGenerationResult.Failure(PuzzleGenerationFailureReason.BoardGenerationFailed, metrics);
                }

                var modifiers = config.ActiveModifiers ?? new List<BossModifierId>();
                var requireUnique = config.RequireUniqueModifierSolution && modifiers.Count > 0;
                metrics.RequiredUniqueModifierSolution = requireUnique;

                if (modifiers.Count == 0)
                    return CompleteSuccess(initialBoard, new ModifierOverlayData(), modifiers, metrics, stopwatch);

                var boardRetries = Math.Max(1, settings.BoardRetries);
                var overlaySeedAttempts = Math.Max(1, settings.OverlaySeedAttempts);
                var rng = new Random(seed);
                var fallbackBoard = initialBoard;
                var fallbackOverlay = new ModifierOverlayData();
                var hasFallbackOverlay = false;

                for (var boardRetry = 0; boardRetry < boardRetries; boardRetry++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    deadline.ThrowIfExceeded();
                    if (BudgetExceeded(stopwatch, settings))
                    {
                        metrics.BudgetExceeded = true;
                        if (requireUnique)
                        {
                            metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                            metrics.LastError = "Unique modifier solution validation exceeded the generation budget.";
                            return PuzzleGenerationResult.Failure(PuzzleGenerationFailureReason.BudgetExceeded, metrics);
                        }

                        return CompleteSuccess(
                            fallbackBoard,
                            fallbackOverlay,
                            modifiers,
                            metrics,
                            stopwatch);
                    }

                    metrics.BoardRetriesAttempted++;
                    var board = boardRetry == 0
                        ? initialBoard
                        : CreateRetryBoard(config, rng.Next(), boardFactory, metrics, deadline);

                    if (board == null)
                        continue;

                    var overlays = GenerateOverlayAttempts(
                        board,
                        modifiers,
                        rng,
                        config.Intensity,
                        overlaySeedAttempts,
                        overlayFactory,
                        settings.ParallelOverlaySeeds,
                        metrics,
                        cancellationToken);

                    for (var i = 0; i < overlays.Length; i++)
                    {
                        var overlay = overlays[i];
                        if (overlay == null)
                            continue;

                        if (!hasFallbackOverlay)
                        {
                            fallbackBoard = board;
                            fallbackOverlay = overlay;
                            hasFallbackOverlay = true;
                        }

                        if (!HasAllModifiersPresent(modifiers, overlay))
                            continue;

                        if (requireUnique && !HasUniqueModifierSolution(board, modifiers, overlay, metrics, deadline))
                            continue;

                        return CompleteSuccess(board, overlay, modifiers, metrics, stopwatch, requireUnique);
                    }
                }

                if (requireUnique)
                {
                    metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    if (string.IsNullOrEmpty(metrics.LastError))
                        metrics.LastError = "Unique modifier solution could not be verified.";
                    return PuzzleGenerationResult.Failure(PuzzleGenerationFailureReason.UniqueSolutionNotFound, metrics);
                }

                if (!hasFallbackOverlay)
                    fallbackOverlay = new ModifierOverlayData();

                return CompleteSuccess(fallbackBoard, fallbackOverlay, modifiers, metrics, stopwatch);
            }
            catch (OperationCanceledException)
            {
                metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                metrics.LastError = "Generation cancelled.";
                return PuzzleGenerationResult.Failure(PuzzleGenerationFailureReason.Cancelled, metrics);
            }
            catch (TimeoutException ex)
            {
                metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                metrics.BudgetExceeded = true;
                metrics.LastError = ex.Message;
                return PuzzleGenerationResult.Failure(PuzzleGenerationFailureReason.BudgetExceeded, metrics);
            }
            catch (Exception ex)
            {
                metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                metrics.LastError = ex.Message;
                return PuzzleGenerationResult.Failure(PuzzleGenerationFailureReason.TaskFaulted, metrics);
            }
        }

        public static bool HasAllModifiersPresent(List<BossModifierId> modifiers, ModifierOverlayData overlay)
        {
            if (overlay == null)
                return false;

            var missing = FindMissingModifiers(modifiers, overlay);
            return missing.Count == 0;
        }

        public static bool IsModifierPresentInOverlay(BossModifierId modifier, ModifierOverlayData overlay)
        {
            var modifiers = new List<BossModifierId> { modifier };
            return HasAllModifiersPresent(modifiers, overlay);
        }

        public static List<BossModifierId> FindMissingModifiers(
            List<BossModifierId> modifiers,
            ModifierOverlayData overlay)
        {
            var missing = new List<BossModifierId>();
            if (modifiers == null || modifiers.Count == 0)
                return missing;

            if (overlay == null)
            {
                missing.AddRange(modifiers);
                return missing;
            }

            for (var i = 0; i < modifiers.Count; i++)
            {
                if (!IsModifierPresent(modifiers[i], overlay))
                    missing.Add(modifiers[i]);
            }

            return missing;
        }

        private static PuzzleGenerationResult CompleteSuccess(
            SudokuBoard board,
            ModifierOverlayData overlay,
            List<BossModifierId> modifiers,
            PuzzleGenerationMetrics metrics,
            Stopwatch stopwatch,
            bool uniqueModifierSolutionVerified = false)
        {
            metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            if (uniqueModifierSolutionVerified)
                metrics.UniqueSolutionVerified = true;
            var missing = FindMissingModifiers(modifiers, overlay);
            return PuzzleGenerationResult.Success(board, overlay, metrics, missing, uniqueModifierSolutionVerified);
        }

        private static bool HasUniqueModifierSolution(
            SudokuBoard board,
            List<BossModifierId> modifiers,
            ModifierOverlayData overlay,
            PuzzleGenerationMetrics metrics,
            GenerationDeadline deadline = default)
        {
            deadline.ThrowIfExceeded();
            metrics.UniqueValidationAttempts++;

            var engine = new SudokuConstraintEngine();
            var rules = ModifierFactory.BuildRules(modifiers);
            for (var i = 0; i < rules.Count; i++)
                engine.RegisterRule(rules[i]);

            var unique = SudokuBacktrackingSolver.HasUniqueSolution(board, overlay, engine, deadline);
            if (!unique)
                metrics.UniqueValidationFailures++;
            return unique;
        }

        private static bool BudgetExceeded(Stopwatch stopwatch, PuzzleGenerationSettings settings)
        {
            return settings.TimeBudgetMs > 0 && stopwatch.ElapsedMilliseconds >= settings.TimeBudgetMs;
        }

        private static SudokuBoard CreateRetryBoard(
            LevelConfig config,
            int seed,
            PuzzleBoardFactory boardFactory,
            PuzzleGenerationMetrics metrics,
            GenerationDeadline deadline = default)
        {
            if (boardFactory == null)
                return null;

            try
            {
                deadline.ThrowIfExceeded();
                var board = boardFactory(config, seed, deadline);
                if (board == null)
                    metrics.BoardGenerationFailures++;
                return board;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                metrics.BoardGenerationFailures++;
                metrics.LastError = ex.Message;
                return null;
            }
        }

        private static ModifierOverlayData[] GenerateOverlayAttempts(
            SudokuBoard board,
            List<BossModifierId> modifiers,
            Random rng,
            BossModifierIntensity intensity,
            int seedCount,
            ModifierOverlayFactory overlayFactory,
            bool runInParallel,
            PuzzleGenerationMetrics metrics,
            CancellationToken cancellationToken)
        {
            var seeds = new int[seedCount];
            for (var i = 0; i < seeds.Length; i++)
                seeds[i] = rng.Next();

            metrics.OverlaySeedAttempts += seedCount;
            var overlays = new ModifierOverlayData[seedCount];

            if (runInParallel)
            {
                Parallel.For(0, seedCount, new ParallelOptions { CancellationToken = cancellationToken }, i =>
                {
                    overlays[i] = GenerateOverlay(board, modifiers, seeds[i], intensity, overlayFactory, metrics);
                });
            }
            else
            {
                for (var i = 0; i < seedCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    overlays[i] = GenerateOverlay(board, modifiers, seeds[i], intensity, overlayFactory, metrics);
                }
            }

            return overlays;
        }

        private static ModifierOverlayData GenerateOverlay(
            SudokuBoard board,
            List<BossModifierId> modifiers,
            int seed,
            BossModifierIntensity intensity,
            ModifierOverlayFactory overlayFactory,
            PuzzleGenerationMetrics metrics)
        {
            try
            {
                return overlayFactory(board, modifiers, seed, intensity);
            }
            catch (Exception ex)
            {
                lock (metrics)
                {
                    metrics.OverlayGenerationFailures++;
                    metrics.LastError = ex.Message;
                }
                return null;
            }
        }

        private static bool IsModifierPresent(BossModifierId modifier, ModifierOverlayData overlay)
        {
            switch (modifier)
            {
                case BossModifierId.GermanWhispers:
                case BossModifierId.DutchWhispers:
                case BossModifierId.ParityLines:
                case BossModifierId.RenbanLines:
                case BossModifierId.Palindrome:
                case BossModifierId.Thermo:
                case BossModifierId.BetweenLines:
                case BossModifierId.ConsecutiveLine:
                case BossModifierId.SlowThermo:
                case BossModifierId.UniqueSetLine:
                case BossModifierId.WhisperGeneralized:
                case BossModifierId.NonconsecLine:
                case BossModifierId.AlternatingParityLine:
                case BossModifierId.HighLowAlternating:
                case BossModifierId.NabnerLine:
                case BossModifierId.EntropicLine:
                case BossModifierId.ModularLine:
                case BossModifierId.ZipperLine:
                case BossModifierId.RegionSumLine:
                case BossModifierId.NLine:
                case BossModifierId.IndexLine:
                case BossModifierId.SumLine:
                case BossModifierId.SkyscraperLine:
                case BossModifierId.ThermoLoop:
                case BossModifierId.FastThermo:
                case BossModifierId.AmbiguousThermo:
                    return overlay.Lines.Count > 0;
                case BossModifierId.DifferenceKropki:
                case BossModifierId.RatioKropki:
                case BossModifierId.FullKropki:
                case BossModifierId.SumKropki:
                case BossModifierId.FullWhiteKropki:
                case BossModifierId.FullBlackKropki:
                    return overlay.KropkiDots.Count > 0;
                case BossModifierId.GreaterLessThan:
                case BossModifierId.XVPairs:
                    return overlay.PairConstraints.Count > 0;
                case BossModifierId.KillerCages:
                case BossModifierId.KillerHiddenSum:
                case BossModifierId.CageProduct:
                case BossModifierId.CageDifference:
                case BossModifierId.CageRatio:
                case BossModifierId.RenbanCage:
                    return overlay.KillerCages.Count > 0;
                case BossModifierId.ArrowSums:
                case BossModifierId.ArrowAverage:
                case BossModifierId.ArrowProduct:
                case BossModifierId.PillArrow:
                case BossModifierId.DoubleArrow:
                    return overlay.Arrows.Count > 0;
                case BossModifierId.FogOfWar:
                    return overlay.FogCells.Count > 0;
                case BossModifierId.EvenOdd:
                case BossModifierId.PrimeCells:
                case BossModifierId.FortressCells:
                    return overlay.CellMarkers.Count > 0;
                default:
                    return true;
            }
        }
    }
}
