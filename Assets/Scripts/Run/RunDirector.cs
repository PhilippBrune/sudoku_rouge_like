using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Run
{
    public sealed class RunDirector
    {
        // ── Services ──
        private BossService _bossService;
        private ItemService _itemService;
        private ShopService _shopService;
        private RelicService _relicService;
        private RunGraphService _graphService;
        private RunEventService _eventService;
        private RunVarianceService _varianceService;
        private MidRunAdaptationService _adaptationService;
        private CurseService _curseService;
        private PostRunAnalyticsService _analytics;
        private RouteService _routeService;
        private EndlessZenService _endlessZenService;
        private SpiritTrialsService _spiritTrialsService;
        private readonly PuzzleGenerationService _puzzleGenerationService = new PuzzleGenerationService();

        // ── State ──
        public RunState State { get; private set; }
        public LevelConfig CurrentLevelConfig { get; private set; }
        public LevelState CurrentLevelState { get; private set; }
        public SudokuBoard CurrentBoard { get; private set; }
        public ModifierOverlayData CurrentOverlay { get; private set; }

        // Blurred pencil cells from the blurred_sight curse; set by InRunController at Configure time
        // so ExportPuzzleSaveState can capture them without a UI-layer reference.
        public HashSet<(int, int)> BlurredPencilCells { get; set; }
        public SudokuConstraintEngine ConstraintEngine { get; private set; }
        public List<RunNode> CurrentFloorGraph { get; private set; }
        public List<ShopOffer> CurrentShopOffers { get; private set; }
        public RunEvent CurrentEvent { get; private set; }
        public List<TileXpEntry> TileXpLog { get; private set; } = new List<TileXpEntry>(); // [REQ: XP-LOG-001] per-tile XP tracked for end-of-run breakdown display
        public List<ItemInstance> RolledItemSlots { get; private set; }
        public List<BossModifierId> BossModifierChoices { get; private set; }
        public List<RelicInstance> RolledRelicChoices { get; private set; }

        /// <summary>Injected by GameBootstrap after profile load. Used for daily goal evaluation.</summary>
        public DailyGoalState DailyGoals { get; set; }

        // Pre-baked boss board generated at floor entry to reduce the work done when modifiers are confirmed.
        // Written on a background thread — volatile ensures the main thread sees the write.
        // Null means StartLevel/StartLevelAsync generate a fresh board through PuzzleGenerationService.
        private volatile SudokuBoard _preBakedBossBoard;
        private volatile LevelConfig _preBakedBossConfig;

        // Pre-baked full puzzles (board + overlay) for non-boss floor nodes, keyed by node index.
        // Populated in background tasks spawned by RunMapController.PrepareFixedNodeConfigs.
        // ConcurrentDictionary: written on background threads, read+removed on the main thread.
        private readonly ConcurrentDictionary<int, (SudokuBoard board, ModifierOverlayData overlay)>
            _preBakedNodePuzzles = new ConcurrentDictionary<int, (SudokuBoard, ModifierOverlayData)>();

        // Async overlay generation — background task state for StartLevelAsync / TryCompleteAsyncLevel.
        // _asyncResult is written inside Task.Run and read on the main thread after Task.IsCompleted
        // returns true; Task completion acts as the memory barrier — no volatile needed.
        private Task _pendingOverlayTask;
        private AsyncLevelResult _asyncResult;
        private LevelConfig _pendingLevelConfig;
        private CancellationTokenSource _pendingLevelGenerationCancel;

        public bool IsAsyncLevelPending => _pendingOverlayTask != null && !_pendingOverlayTask.IsCompleted;
        public PuzzleGenerationMetrics LastGenerationMetrics { get; private set; }
        public IReadOnlyList<BossModifierId> LastGenerationMissingModifiers { get; private set; } = new List<BossModifierId>();

        // ── Init ──

        /// <summary>Restore state from a save file. Used by RunResumeService.</summary>
        public void RestoreState(RunState state)
        {
            State = state;
        }

        public void StartRun(LaunchRequest request, int seed)
        {
            // Clear all state from any previous run so ApplyResumeState cannot
            // mistake the old board for an active puzzle.
            CurrentBoard       = null;
            CurrentLevelState  = null;
            CurrentLevelConfig = null;
            CurrentOverlay     = null;
            ConstraintEngine   = null;
            CurrentFloorGraph  = null;
            CurrentShopOffers  = null;
            CurrentEvent       = null;
            RolledItemSlots    = null;
            BossModifierChoices = null;
            RolledRelicChoices = null;
            _pendingLevelGenerationCancel?.Cancel();
            _pendingLevelGenerationCancel = null;
            _pendingOverlayTask = null;
            _pendingLevelConfig = null;
            _asyncResult = default;
            _preBakedBossBoard = null;
            _preBakedBossConfig = null;
            _preBakedNodePuzzles.Clear();
            TileXpLog.Clear();
            InitServices(seed);
            _analytics = new PostRunAnalyticsService();

            if (request.Mode == GameMode.EndlessZen)
            {
                State = EndlessZenService.CreateZenRunState(request.ClassId, seed, request.AllowIrregularPuzzles);
                CurrentLevelConfig = _endlessZenService.BuildNextLevel(State.Depth, State.AllowIrregularPuzzles);
                StartLevel(CurrentLevelConfig);
                return;
            }

            if (request.Mode == GameMode.SpiritTrials)
            {
                var tier = request.SelectedTier ?? SpiritTrialsTier.Apprentice;
                State = SpiritTrialsService.CreateTrialRunState(request.ClassId, tier, seed, request.AllowIrregularPuzzles);
                CurrentLevelConfig = _spiritTrialsService.BuildTrialLevel(tier, State.AllowIrregularPuzzles);
                StartLevel(CurrentLevelConfig);
                return;
            }

            State = RunArchetypeService.CreateRunState(request.ClassId, seed, request.AllowIrregularPuzzles, request.ClassLevel,
                request.HarmonyLevel, request.HarmonyPerk);
            State.Mode = request.Mode;

            // Roll floor modifiers for the initial floor (Floor 1 = 0 modifiers)
            var startingRelicCount = RunArchetypeService.GetStartingRelicCount(request.ClassId, request.ClassLevel);
            _relicService.AssignStartingRelics(State, startingRelicCount, State.HarmonyLevel);
            RollFloorModifiers();
            RebuildFloorGraph();
            State.HpAtCurrentFloorEntry = State.CurrentHP; // floor 0 entry HP for full_hp_floor goal
        }

        /// <summary>Start a Seasonal Challenge run using a pre-built RunState and LevelConfig.
        /// State must already be set via RestoreState before calling this.</summary>
        public void StartSeasonalChallenge(LevelConfig config)
        {
            InitServices(State.Seed);
            TileXpLog.Clear();
            _analytics = new PostRunAnalyticsService();

            CurrentLevelConfig = config;
            StartLevel(config);
        }

        public void StartTutorialRun(TutorialSetupConfig setup, int seed)
        {
            State = Tutorial.TutorialModeService.CreateTutorialRunState(setup, seed);

            InitServices(seed);
            TileXpLog.Clear();
            _analytics = new PostRunAnalyticsService();

            var tutService = new Tutorial.TutorialModeService();
            CurrentLevelConfig = tutService.BuildTutorialLevel(setup, seed);
            StartLevel(CurrentLevelConfig);
        }

        /// <summary>
        /// Initialises the Sudoku Basics (RunNumber=0) tutorial with a fully deterministic hand-crafted
        /// 4×4 board. Unlike StartRun / StartTutorialRun, no generator randomness is involved.
        /// </summary>
        public void StartSudokuBasicsTutorial(RunState state, LevelConfig config)
        {
            // Clear all state from any previous run
            CurrentBoard       = null;
            CurrentLevelState  = null;
            CurrentLevelConfig = null;
            CurrentOverlay     = null;
            ConstraintEngine   = null;

            State = state; // RunNumber=0, TutorialMode=true, invincible resources

            InitServices(state.Seed);
            TileXpLog.Clear();
            _analytics = new PostRunAnalyticsService();
            RollFloorModifiers();
            RebuildFloorGraph();

            // Override board with the fixed hand-crafted tutorial layout
            CurrentBoard       = Tutorial.TutorialModeService.BuildBasicsTutorialBoard();
            CurrentLevelConfig = config;
            CurrentLevelState  = new LevelState();
            ConstraintEngine   = new SudokuConstraintEngine();
            CurrentOverlay     = new ModifierOverlayData();
        }

        private void InitServices(int seed)
        {
            _bossService = new BossService(seed);
            _itemService = new ItemService(seed + 1);
            _shopService = new ShopService(seed + 2);
            _relicService = new RelicService(seed + 3);
            _graphService = new RunGraphService();
            _eventService = new RunEventService(seed + 4);
            _varianceService = new RunVarianceService(seed + 5);
            _adaptationService = new MidRunAdaptationService();
            _curseService = new CurseService(seed + 6);
            _routeService = new RouteService();
            _endlessZenService = new EndlessZenService(seed + 7);
            _spiritTrialsService = new SpiritTrialsService(seed + 8);
        }

        // ── Floor / Graph ──

        public void RebuildFloorGraph()
        {
            CurrentFloorGraph = _graphService.BuildFloorGraph(State.CurrentFloor, State.Seed);
            State.CurrentNodeIndex = 0;
        }

        // [REQ: MAP-STRUCT-001] AdvanceToNextFloor: run spans exactly 5 floors (index 0–4); floor 4 boss win = run complete
        public void AdvanceToNextFloor()
        {
            State.CurrentFloor++;
            State.Depth = 0;
            State.HpAtCurrentFloorEntry = State.CurrentHP; // capture before relic heals apply

            // Clear boss modifiers from previous floor
            State.ChosenBossModifiers.Clear();
            State.HasChosenBossModifier = false;

            // Roll floor modifiers for the new floor
            RollFloorModifiers();

            // Reset per-floor item flags
            State.WornChiselActive = false;
            State.DimLanternUsed = false;
            State.ShopRerollCount = 0;

            // Relic: floor-start effects (WisteriaBranch heal, GoldenRoot interest, AccurateMap)
            RelicService.OnFloorStart(State);
            // [REQ: CURSE-INT-004] fraying_thread curse: -1 MaxHP at start of each new floor
            CurseService.OnFloorStart(State);

            // Daily goals: floor-reached triggers
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                if (State.CurrentFloor == 1)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerReachedFloor2);
                if (State.CurrentFloor == 3)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerReachedFloor4);
            }

            RebuildFloorGraph();
        }

        public void AdvanceEndlessZenLevel()
        {
            if (State == null || State.Mode != GameMode.EndlessZen) return;

            // [REQ: ZENMODE-DEPTH-001] Depth starts at 0; increments after each solved puzzle
            State.Depth++;
            // [REQ: ZENMODE-RES-001] Between levels: +1 HP and +3 pencil restored
            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + 1);
            State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + 3);

            CurrentLevelConfig = _endlessZenService.BuildNextLevel(State.Depth, State.AllowIrregularPuzzles);
            StartLevel(CurrentLevelConfig);
        }

        private void RollFloorModifiers()
        {
            var minSize = FloorThemeData.GetMinBoardSize(State.CurrentFloor);
            State.ActiveFloorModifiers = _bossService.RollFloorModifiers(State.CurrentFloor, minSize, State.HarmonyLevel);

            // Add floor modifiers to seen set for "???" reveal tracking
            for (var i = 0; i < State.ActiveFloorModifiers.Count; i++)
                State.SeenBossModifiers.Add(State.ActiveFloorModifiers[i]);

            Debug.Log(
                $"[ModifierDiscovery] Floor roll: floor={State.CurrentFloor + 1}, " +
                $"rolled={ModifierDiscoveryService.Describe(State.ActiveFloorModifiers)}, " +
                $"runSeen={ModifierDiscoveryService.Describe(State.SeenBossModifiers)}");

            // Roll positive floor effect (one per floor, starting from floor 0)
            // [REQ: MAP-FX-001] One random positive floor effect rolled per floor
            // [REQ: MAP-FX-002] Seeded from RunState.Seed + CurrentFloor * 6271
            // [REQ: MAP-FX-005] Each non-None effect has equal probability (uniform pick from index 1+)
            // [M6-A] Skip if VoidWard is active; apply PositiveEffectMult to spawn probability.
            var positiveEffectProb = State.HarmonyConfig.PositiveEffectMult;
            if (!State.VoidWardActive && positiveEffectProb > 0f)
            {
                var floorRng = new System.Random(State.Seed + State.CurrentFloor * 6271);
                if (floorRng.NextDouble() < positiveEffectProb)
                {
                    var effects = (PositiveFloorEffect[])System.Enum.GetValues(typeof(PositiveFloorEffect));
                    // Skip None (index 0)
                    var picked = (PositiveFloorEffect)effects[1 + floorRng.Next(effects.Length - 1)];
                    State.HasPositiveFloorEffect = true;
                    State.ActivePositiveFloorEffect = picked;
                }
            }
        }

        // ── Level Generation ──

        public LevelConfig BuildLevelConfig(bool isBoss, bool isElite = false, int nodeIndex = 0,
            bool isPreBoss = false)
        {
            SanitizeGeneratedModifierList(State.ActiveFloorModifiers, "saved floor modifiers");
            if (isBoss)
                SanitizeGeneratedModifierList(State.ChosenBossModifiers, "saved boss modifiers");

            var floor = State.CurrentFloor;
            var minSize = FloorThemeData.GetMinBoardSize(floor);
            var maxSize = FloorThemeData.GetMaxBoardSize(floor);
            var rng = new System.Random(State.Seed + State.Depth * 31 + floor * 997 + nodeIndex * 13);
            // Pre-compute minimum board size from the modifiers that will be active so the rolled
            // size already satisfies constraints — avoids post-hoc board-size bumps that misalign
            // the RegionVariant with the final BoardSize.
            var modMinSize = ComputeMinBoardSizeForModifiers(State.ActiveFloorModifiers);
            if (isBoss) modMinSize = Math.Max(modMinSize, ComputeMinBoardSizeForModifiers(State.ChosenBossModifiers));
            var requiredMinSize = Math.Max(minSize, modMinSize);
            var size = requiredMinSize <= maxSize
                ? rng.Next(requiredMinSize, maxSize + 1)
                : requiredMinSize;
            if ((HasBoardShapingModifier(State.ActiveFloorModifiers)
                    || (isBoss && HasBoardShapingModifier(State.ChosenBossModifiers)))
                && size == 7)
            {
                size = minSize <= 6 ? 6 : 8;
            }
            var stars = RollStars(rng, floor, isBoss, isElite);

            var config = new LevelConfig
            {
                BoardSize = size,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                // [REQ: GEN-REGION-003] AllowIrregularPuzzles=false restricts to variants 0–1 (rectangular only); true allows all 4
                RegionVariant = State.AllowIrregularPuzzles ? rng.Next(4) : rng.Next(2),
                IsBoss = isBoss,
                IsElite = isElite,
                IsPreBoss = isPreBoss,
                Seed = rng.Next(),
                Intensity = BossService.IntensityForRunNumber(State.RunNumber),
                Difficulty = MapDifficulty(floor, stars)
            };

            // Floor modifiers apply to non-boss puzzles only (all puzzles on floors 2–5).
            // Boss nodes have their own chosen modifier set and should be a clean slate.
            if (State.ActiveFloorModifiers != null)
            {
                for (var i = 0; i < State.ActiveFloorModifiers.Count; i++)
                {
                    var modifier = State.ActiveFloorModifiers[i];
                    if (!config.ActiveModifiers.Contains(modifier)
                        && BossService.CanCombineModifiers(config.ActiveModifiers, modifier))
                        config.ActiveModifiers.Add(modifier);
                }
            }

            // Add boss-chosen modifiers (plural)
            if (isBoss && State.ChosenBossModifiers != null)
            {
                for (var i = 0; i < State.ChosenBossModifiers.Count; i++)
                {
                    var modifier = State.ChosenBossModifiers[i];
                    if (!config.ActiveModifiers.Contains(modifier)
                        && BossService.CanCombineModifiers(config.ActiveModifiers, modifier))
                        config.ActiveModifiers.Add(modifier);
                }
                Debug.Log($"[RunDirector] BuildLevelConfig (boss): ChosenBossModifiers={State.ChosenBossModifiers.Count}, ActiveModifiers={config.ActiveModifiers.Count} [{string.Join(", ", config.ActiveModifiers)}]");
            }

            _varianceService.ApplyVariance(config, State.Depth, floor,
                State.AllowIrregularPuzzles ? 3 : 1);

            // Mid-run adaptation: nudge difficulty based on most recent puzzle performance (non-boss only)
            if (!isBoss)
                _adaptationService.AdaptDifficulty(config, State, CurrentLevelState);

            // Floor-gated pressure mechanic — only on elite / pre-boss / boss puzzles from floor 2+
            RollPressureMechanic(config, floor, rng);
            EnsureBoardSizeSupportsActiveModifiers(config);

            return config;
        }

        // [REQ: MAP-NODE-CURSED-001] BuildCursedLevelConfig: produces the Accept path puzzle with IsCursed=true
        // [REQ: MAP-NODE-CURSED-002] CursedGoldMult/CursedXpMult=1.5 and one extra random modifier added
        /// <summary>Build a Cursed version of a normal LevelConfig.
        /// Stacks one extra floor modifier (randomly chosen from unseen pool) and applies +50% gold/XP.</summary>
        public LevelConfig BuildCursedLevelConfig(int nodeIndex = 0)
        {
            var config = BuildLevelConfig(false, false, nodeIndex);
            config.IsCursed = true;
            config.CursedGoldMult = 1.5f;
            config.CursedXpMult  = 1.5f;

            // Add one extra random modifier beyond what the floor provides
            var rng = new System.Random(State.Seed + nodeIndex * 3137 + State.Depth * 17);
            var allIds = (BossModifierId[])System.Enum.GetValues(typeof(BossModifierId));
            // Prefer active pool (0-14)
            for (var attempts = 0; attempts < 20; attempts++)
            {
                var pick = (BossModifierId)allIds[rng.Next(15)];
                if (!config.ActiveModifiers.Contains(pick)
                    && BossService.CanCombineModifiers(config.ActiveModifiers, pick))
                {
                    config.ActiveModifiers.Add(pick);
                    break;
                }
            }
            SanitizeGeneratedLevelConfig(config);
            EnsureBoardSizeSupportsActiveModifiers(config);
            return config;
        }

        private static SudokuBoard CreatePuzzleForConfig(LevelConfig config, int seed,
            GenerationDeadline deadline = default)
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                deadline.ThrowIfExceeded();
                var usedSeed = attempt == 0 ? seed : (seed ^ (0xF1D00000 + attempt));
                try
                {
                    return CreatePuzzleForConfigSingleAttempt(config, unchecked((int)usedSeed), deadline);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogWarning($"[RunDirector] CreatePuzzleForConfig: generation failed (attempt {attempt + 1}/4, seed={usedSeed}, size={config.BoardSize}): {ex.Message}");
                }
            }
            Debug.LogError($"[RunDirector] CreatePuzzleForConfig: all 4 attempts exhausted for size={config.BoardSize}. Returning null board.");
            return null;
        }

        private static SudokuBoard CreatePuzzleForConfigWithBudgetedRetries(
            LevelConfig config,
            int seed,
            PuzzleGenerationSettings settings,
            CancellationToken cancellationToken,
            PuzzleGenerationMetrics metrics,
            bool logAttemptWarnings = true)
        {
            settings ??= PuzzleGenerationSettings.SynchronousDefault;
            metrics ??= new PuzzleGenerationMetrics();

            var attempts = Math.Max(1, settings.BoardRetries);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var rng = new System.Random(seed ^ unchecked((int)0x6D2B79F5));

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var usedSeed = attempt == 0 ? seed : rng.Next();
                metrics.BoardRetriesAttempted++;

                try
                {
                    var board = CreatePuzzleForConfigSingleAttempt(
                        config,
                        usedSeed,
                        new GenerationDeadline(settings.TimeBudgetMs, cancellationToken));
                    metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    if (board != null)
                    {
                        metrics.BudgetExceeded = false;
                        metrics.LastError = null;
                        return board;
                    }

                    metrics.BoardGenerationFailures++;
                    metrics.LastError = "Board generation returned null.";
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (TimeoutException ex)
                {
                    metrics.BoardGenerationFailures++;
                    metrics.BudgetExceeded = true;
                    metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    metrics.LastError = ex.Message;
                    if (logAttemptWarnings)
                    {
                        Debug.LogWarning(
                            $"[RunDirector] CreatePuzzleForConfig: timed out " +
                            $"(attempt {attempt + 1}/{attempts}, seed={usedSeed}, size={config.BoardSize}, " +
                            $"mods=[{string.Join(", ", config.ActiveModifiers)}]): {ex.Message}");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    metrics.BoardGenerationFailures++;
                    metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    metrics.LastError = ex.Message;
                    if (logAttemptWarnings)
                    {
                        Debug.LogWarning(
                            $"[RunDirector] CreatePuzzleForConfig: generation failed " +
                            $"(attempt {attempt + 1}/{attempts}, seed={usedSeed}, size={config.BoardSize}): {ex.Message}");
                    }
                }
            }

            metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return null;
        }

        private static SudokuBoard CreatePuzzleForConfigSingleAttempt(
            LevelConfig config,
            int seed,
            GenerationDeadline deadline = default)
        {
            return SudokuGenerator.CreatePuzzleWithUniquenessCheck(
                config.BoardSize, config.MissingPercent, seed, config.RegionVariant,
                config.ActiveModifiers.Contains(BossModifierId.Nonconsecutive),
                config.ActiveModifiers.Contains(BossModifierId.Antiknight),
                config.ActiveModifiers.Contains(BossModifierId.NonconsecDiagonal),
                config.ActiveModifiers.Contains(BossModifierId.AntiBishop),
                config.ActiveModifiers.Contains(BossModifierId.Antiking),
                config.ActiveModifiers.Contains(BossModifierId.DistanceGe2),
                deadline);
        }

        private static void IncludeInitialBoardMetrics(
            PuzzleGenerationMetrics target,
            PuzzleGenerationMetrics initialBoardMetrics)
        {
            if (target == null || initialBoardMetrics == null) return;

            target.BoardRetriesAttempted += initialBoardMetrics.BoardRetriesAttempted;
            target.BoardGenerationFailures += initialBoardMetrics.BoardGenerationFailures;
            target.BudgetExceeded |= initialBoardMetrics.BudgetExceeded;
            target.ElapsedMilliseconds += initialBoardMetrics.ElapsedMilliseconds;
            if (!string.IsNullOrEmpty(initialBoardMetrics.LastError)
                && string.IsNullOrEmpty(target.LastError))
            {
                target.LastError = initialBoardMetrics.LastError;
            }
        }

        private static SudokuBoard TryCreateBoardAfterDroppingBoardShapingModifiers(
            LevelConfig originalConfig,
            int seed,
            CancellationToken cancellationToken,
            PuzzleGenerationMetrics aggregateMetrics,
            out LevelConfig effectiveConfig,
            out List<BossModifierId> droppedModifiers)
        {
            effectiveConfig = originalConfig;
            droppedModifiers = new List<BossModifierId>();

            if (!TryBuildBoardGenerationFallbackConfig(originalConfig, out var fallbackConfig, out var fallbackDropped))
                return null;

            var fallbackMetrics = new PuzzleGenerationMetrics();
            var fallbackSettings = SelectAsyncGenerationSettings(fallbackConfig);
            var fallbackBoard = CreatePuzzleForConfigWithBudgetedRetries(
                fallbackConfig,
                seed,
                fallbackSettings,
                cancellationToken,
                fallbackMetrics,
                logAttemptWarnings: false);

            IncludeInitialBoardMetrics(aggregateMetrics, fallbackMetrics);
            if (fallbackBoard == null)
                return null;

            effectiveConfig = fallbackConfig;
            droppedModifiers = fallbackDropped;
            aggregateMetrics.BudgetExceeded = true;
            aggregateMetrics.LastError =
                $"Dropped board-shaping modifiers after generation failure: {string.Join(", ", droppedModifiers)}.";

            Debug.LogWarning(
                $"[RunDirector] Board generation failed for mods=[{string.Join(", ", originalConfig.ActiveModifiers)}]. " +
                $"Starting this puzzle without board-shaping modifiers=[{string.Join(", ", droppedModifiers)}].");

            return fallbackBoard;
        }

        private static bool HasBoardShapingModifier(LevelConfig config)
        {
            return HasBoardShapingModifier(config?.ActiveModifiers);
        }

        private static void SanitizeGeneratedLevelConfig(LevelConfig config)
        {
            if (config == null) return;
            SanitizeGeneratedModifierList(config.ActiveModifiers, "prepared level configuration");
        }

        private static void SanitizeGeneratedModifierList(List<BossModifierId> modifiers, string source)
        {
            if (modifiers == null || modifiers.Count == 0) return;

            var accepted = new List<BossModifierId>(modifiers.Count);
            var removed = new List<BossModifierId>();
            for (var i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (!BossService.IsSupportedForGeneratedRun(modifier)
                    || !BossService.CanCombineModifiers(accepted, modifier))
                {
                    removed.Add(modifier);
                    continue;
                }
                accepted.Add(modifier);
            }

            if (removed.Count == 0) return;

            modifiers.Clear();
            modifiers.AddRange(accepted);
            Debug.Log(
                $"[RunDirector] Removed unsupported generated modifiers from {source}: " +
                $"{string.Join(", ", removed)}.");
        }

        private static bool HasBoardShapingModifier(IList<BossModifierId> modifiers)
        {
            if (modifiers == null) return false;
            for (var i = 0; i < modifiers.Count; i++)
            {
                if (IsBoardShapingModifier(modifiers[i]))
                    return true;
            }
            return false;
        }

        private static bool TryBuildBoardGenerationFallbackConfig(
            LevelConfig source,
            out LevelConfig fallback,
            out List<BossModifierId> droppedModifiers)
        {
            fallback = null;
            droppedModifiers = new List<BossModifierId>();

            if (source?.ActiveModifiers == null || source.ActiveModifiers.Count == 0)
                return false;

            fallback = source.Clone();
            for (var i = fallback.ActiveModifiers.Count - 1; i >= 0; i--)
            {
                var modifier = fallback.ActiveModifiers[i];
                if (!IsBoardShapingModifier(modifier))
                    continue;

                droppedModifiers.Add(modifier);
                fallback.ActiveModifiers.RemoveAt(i);
            }

            if (droppedModifiers.Count == 0)
            {
                fallback = null;
                return false;
            }

            droppedModifiers.Reverse();
            if (fallback.ActiveModifiers.Count == 0)
                fallback.RequireUniqueModifierSolution = false;

            return true;
        }

        private static void EnsureBoardSizeSupportsActiveModifiers(LevelConfig config)
        {
            if (config == null || config.ActiveModifiers == null) return;

            var requiredSize = 5;
            var boardShapingCount = 0;
            for (var i = 0; i < config.ActiveModifiers.Count; i++)
            {
                var mod = config.ActiveModifiers[i];
                requiredSize = Math.Max(requiredSize, GetMinimumBoardSizeForModifier(mod));
                if (IsBoardShapingModifier(mod))
                    boardShapingCount++;
            }

            // Multiple global geometry constraints can over-constrain 5x5/6x6 boards.
            if (boardShapingCount >= 2)
                requiredSize = Math.Max(requiredSize, 8);

            // All generated 7x7 regions are irregular; current structural-board generation
            // is reliable on 6x6 and 8x8+, but not on the 7x7 templates.
            if (boardShapingCount > 0 && (config.BoardSize == 7 || requiredSize == 7))
                requiredSize = Math.Max(requiredSize, 8);

            if (config.BoardSize >= requiredSize) return;

            var oldSize = config.BoardSize;
            config.BoardSize = Math.Min(9, requiredSize);
            Debug.LogWarning(
                $"[RunDirector] Raised board size from {oldSize} to {config.BoardSize} " +
                $"for active modifiers: {string.Join(", ", config.ActiveModifiers)}");
        }

        private static int ComputeMinBoardSizeForModifiers(IList<BossModifierId> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0) return 5;
            var required = 5;
            var shapingCount = 0;
            for (var i = 0; i < modifiers.Count; i++)
            {
                required = Math.Max(required, GetMinimumBoardSizeForModifier(modifiers[i]));
                if (IsBoardShapingModifier(modifiers[i])) shapingCount++;
            }
            if (shapingCount >= 2) required = Math.Max(required, 8);
            return required;
        }

        private static bool IsModifierPresentInOverlay(BossModifierId mod, ModifierOverlayData overlay)
        {
            var temp = new List<BossModifierId>(1) { mod };
            return HasAllModifiersPresent(temp, overlay);
        }

        private static int GetMinimumBoardSizeForModifier(BossModifierId mod)
        {
            switch (mod)
            {
                case BossModifierId.GermanWhispers:
                case BossModifierId.KillerCages:
                case BossModifierId.AntiBishop:
                    return 7;
                case BossModifierId.EntropyGlobal:
                    return 9;
                case BossModifierId.DutchWhispers:
                case BossModifierId.Nonconsecutive:
                case BossModifierId.Antiknight:
                case BossModifierId.Antiking:
                case BossModifierId.NonconsecDiagonal:
                case BossModifierId.DistanceGe2:
                case BossModifierId.FullKropki:
                case BossModifierId.FortressCells:
                    return 6;
                default:
                    return 5;
            }
        }

        private static bool IsBoardShapingModifier(BossModifierId mod)
        {
            return mod == BossModifierId.Nonconsecutive
                || mod == BossModifierId.Antiknight
                || mod == BossModifierId.NonconsecDiagonal
                || mod == BossModifierId.AntiBishop
                || mod == BossModifierId.Antiking
                || mod == BossModifierId.DistanceGe2;
        }

        /// <summary>
        /// Pre-generate the boss board at floor entry so StartLevel can skip board generation when
        /// the player confirms modifiers. Uses the structural modifiers already present in the config
        /// (floor-level mods) so the bake is valid whenever the player's boss choices do not add or
        /// remove any structural modifier.
        /// Safe to call off the main thread — SudokuGenerator uses no Unity APIs.
        /// </summary>
        public void BakeBossBoard(LevelConfig config, CancellationToken cancel = default)
        {
            try
            {
                SanitizeGeneratedLevelConfig(config);
                EnsureBoardSizeSupportsActiveModifiers(config);
                var metrics = new PuzzleGenerationMetrics();
                var settings = SelectAsyncGenerationSettings(config);
                _preBakedBossBoard = CreatePuzzleForConfigWithBudgetedRetries(
                    config,
                    config.Seed,
                    settings,
                    cancel,
                    metrics,
                    logAttemptWarnings: false);
                if (_preBakedBossBoard != null && !cancel.IsCancellationRequested)
                    _preBakedBossConfig = config.Clone();
                else if (!cancel.IsCancellationRequested)
                    Debug.Log($"[RunDirector] BakeBossBoard skipped: {metrics.LastError ?? "board generation returned null"}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RunDirector] BakeBossBoard failed: {ex.Message}");
            }
        }

        // Pre-generate the full puzzle (board + overlay) for a non-boss node on a background thread.
        // Results are stored in _preBakedNodePuzzles; consume via TryConsumePreBakedNode.
        // Safe to call from any thread — CreatePuzzleForConfig and PuzzleGenerationService use no Unity APIs.
        public void BakeNodePuzzle(LevelConfig config, int nodeIndex, CancellationToken cancel)
        {
            SanitizeGeneratedLevelConfig(config);
            EnsureBoardSizeSupportsActiveModifiers(config);
            PuzzleGenerationSettings settings = HasBoardShapingModifier(config)
                ? PuzzleGenerationSettings.StructuralModifierDefault
                : PuzzleGenerationSettings.SynchronousDefault;
            SudokuBoard board;
            var boardMetrics = new PuzzleGenerationMetrics();
            try
            {
                board = CreatePuzzleForConfigWithBudgetedRetries(
                    config,
                    config.Seed,
                    settings,
                    cancel,
                    boardMetrics,
                    logAttemptWarnings: false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RunDirector] BakeNodePuzzle: board generation failed for node {nodeIndex}: {ex.Message}");
                return;
            }

            if (board == null || cancel.IsCancellationRequested)
            {
                if (!cancel.IsCancellationRequested)
                {
                    Debug.Log(
                        $"[RunDirector] BakeNodePuzzle: board generation failed for node {nodeIndex}: " +
                        $"{boardMetrics.LastError ?? "board generation returned null"}");
                }
                return;
            }

            var levelRng = new System.Random(config.Seed ^ 0x1A2B3C4D);
            var result   = _puzzleGenerationService.Generate(
                config, board, levelRng.Next(), CreatePuzzleForConfig,
                settings, cancel);
            IncludeInitialBoardMetrics(result.Metrics, boardMetrics);

            if (result.IsSuccess && !cancel.IsCancellationRequested)
            {
                _preBakedNodePuzzles[nodeIndex] = (result.Board, result.Overlay);
                Debug.Log($"[RunDirector] BakeNodePuzzle: node {nodeIndex} pre-baked (size={config.BoardSize}, mods={config.ActiveModifiers.Count}).");
            }
            else if (!cancel.IsCancellationRequested)
            {
                Debug.Log(
                    $"[RunDirector] BakeNodePuzzle: node {nodeIndex} pre-bake failed " +
                    $"({result.FailureReason}) size={config.BoardSize}, stars={config.Stars}, seed={config.Seed}.");
            }
        }

        // Atomically reads and removes a pre-baked puzzle for the given node index.
        // Returns false when the background task hasn't finished yet (or was cancelled);
        // the caller should fall back to synchronous StartLevel in that case.
        public bool TryConsumePreBakedNode(int nodeIndex, out SudokuBoard board, out ModifierOverlayData overlay)
        {
            if (_preBakedNodePuzzles.TryRemove(nodeIndex, out var bake))
            {
                board   = bake.board;
                overlay = bake.overlay;
                return true;
            }
            board   = null;
            overlay = null;
            return false;
        }

        // Discard all pre-baked non-boss node puzzles. Call at floor change and run start.
        public void ClearPreBakedNodePuzzles() => _preBakedNodePuzzles.Clear();

        /// <summary>
        /// Identical to <see cref="StartLevel"/> but skips board and overlay generation by using
        /// the pre-baked results from <see cref="BakeNodePuzzle"/>.
        /// </summary>
        public void StartLevelWithPreBakedPuzzle(LevelConfig config, SudokuBoard board, ModifierOverlayData overlay)
        {
            SanitizeGeneratedLevelConfig(config);
            EnsureBoardSizeSupportsActiveModifiers(config);

            _pendingLevelGenerationCancel?.Cancel();
            _pendingLevelGenerationCancel = null;
            _pendingOverlayTask = null;
            _pendingLevelConfig = null;
            _asyncResult        = default;

            CurrentLevelConfig = config;
            CurrentLevelState  = new LevelState();
            State.HpAtFloorStart = State.CurrentHP;

            _preBakedBossBoard  = null;
            _preBakedBossConfig = null;

            CurrentBoard   = board;
            CurrentOverlay = overlay;

            LastGenerationMetrics          = new PuzzleGenerationMetrics();
            LastGenerationMissingModifiers = new List<BossModifierId>();

            if (config.ActiveModifiers.Count > 0)
                ClearOverlayCellsFromGivenMask();

            ConstraintEngine = new SudokuConstraintEngine();
            var rules = ModifierFactory.BuildRules(config.ActiveModifiers);
            for (var i = 0; i < rules.Count; i++)
                ConstraintEngine.RegisterRule(rules[i]);

            RelicService.OnPuzzleStart(State, CurrentBoard);
            CurseService.OnPuzzleStart(State);
            State.ShieldPoints = 0;
            State.LastMistakeShieldAbsorbed = false;

            var pressureRng = new System.Random(config.Seed ^ 0x50A4B3C2);
            InitializePressureMechanics(config, CurrentBoard, CurrentLevelState, pressureRng);
        }

        /// <summary>
        /// Async variant for boss nodes: sets up the board on the main thread (uses pre-bake when valid),
        /// then generates the modifier overlay on a background thread.
        /// Poll <see cref="TryCompleteAsyncLevel"/> each frame — it returns true when overlay generation
        /// is done and all level state has been finalized on the main thread.
        /// </summary>
        public void StartLevelAsync(LevelConfig config)
        {
            SanitizeGeneratedLevelConfig(config);
            EnsureBoardSizeSupportsActiveModifiers(config);
            Debug.Log($"[RunDirector] StartLevelAsync: ActiveModifiers={config.ActiveModifiers.Count} [{string.Join(", ", config.ActiveModifiers)}]");
            _pendingLevelGenerationCancel?.Cancel();
            _pendingLevelGenerationCancel = new CancellationTokenSource();
            var cancellationToken = _pendingLevelGenerationCancel.Token;
            CurrentLevelConfig = config;
            CurrentLevelState  = new LevelState();
            State.HpAtFloorStart = State.CurrentHP;
            _pendingLevelConfig = config;
            _asyncResult        = AsyncLevelResult.Pending();

            // Pre-initialise an empty constraint engine so PlaceNumber is never called against a null
            // engine while the background overlay task is still in flight.
            // TryCompleteAsyncLevel will replace it with the fully-wired engine once generation finishes.
            ConstraintEngine = new SudokuConstraintEngine();

            // Capture any valid pre-bake before clearing the fields so the background thread can
            // use it without touching the volatile fields after Task.Run starts.
            // Board generation (CreatePuzzleForConfig) runs inside Task.Run — the main thread is
            // no longer blocked during the Boss Awakening panel animation.
            SudokuBoard capturedPreBake = null;
            if (config.IsBoss && _preBakedBossBoard != null
                && _preBakedBossConfig != null
                && _preBakedBossConfig.Seed == config.Seed
                && _preBakedBossConfig.BoardSize == config.BoardSize
                && StructuralModsMatch(config.ActiveModifiers, _preBakedBossConfig.ActiveModifiers))
            {
                capturedPreBake = _preBakedBossBoard;
                Debug.Log("[RunDirector] StartLevelAsync: pre-bake hit — structural mods match, board generation skipped.");
            }
            else
            {
                Debug.Log("[RunDirector] StartLevelAsync: pre-bake miss — structural mods changed or no pre-bake, board generation deferred to background thread.");
            }
            _preBakedBossBoard  = null;
            _preBakedBossConfig = null;

            var levelRng  = new System.Random(config.Seed ^ 0x1A2B3C4D);
            var settings  = SelectAsyncGenerationSettings(config);

            _pendingOverlayTask = Task.Run(() =>
            {
                try
                {
                    // SudokuGenerator uses no Unity APIs — safe to call from a background thread.
                    var initialBoardMetrics = new PuzzleGenerationMetrics();
                    var generationConfig = config;
                    var droppedBoardShapingModifiers = new List<BossModifierId>();
                    var initialBoard = capturedPreBake;
                    if (initialBoard == null)
                    {
                        initialBoard = CreatePuzzleForConfigWithBudgetedRetries(
                            config,
                            config.Seed,
                            settings,
                            cancellationToken,
                            initialBoardMetrics,
                            logAttemptWarnings: false);

                        if (initialBoard == null)
                        {
                            initialBoard = TryCreateBoardAfterDroppingBoardShapingModifiers(
                                config,
                                config.Seed,
                                cancellationToken,
                                initialBoardMetrics,
                                out generationConfig,
                                out droppedBoardShapingModifiers);
                        }
                    }
                    if (initialBoard == null)
                    {
                        _asyncResult = AsyncLevelResult.Failure(
                            initialBoardMetrics.BudgetExceeded
                                ? AsyncLevelFailureReason.BudgetExceeded
                                : AsyncLevelFailureReason.GenerationFailed,
                            initialBoardMetrics);
                        return;
                    }

                    if (!ReferenceEquals(generationConfig, config))
                        _pendingLevelConfig = generationConfig;

                    var generationSettings = ReferenceEquals(generationConfig, config)
                        ? settings
                        : SelectAsyncGenerationSettings(generationConfig);
                    var result = _puzzleGenerationService.Generate(
                        generationConfig,
                        initialBoard,
                        levelRng.Next(),
                        CreatePuzzleForConfig,
                        generationSettings,
                        cancellationToken);
                    if (capturedPreBake == null)
                        IncludeInitialBoardMetrics(result.Metrics, initialBoardMetrics);

                    _asyncResult = result.IsSuccess
                        ? AsyncLevelResult.Success(result, droppedBoardShapingModifiers)
                        : AsyncLevelResult.Failure(MapFailureReason(result.FailureReason), result.Metrics);
                }
                catch (OperationCanceledException)
                {
                    _asyncResult = AsyncLevelResult.Failure(AsyncLevelFailureReason.Cancelled);
                }
                catch (TimeoutException ex)
                {
                    Debug.LogWarning($"[RunDirector] Async puzzle generation timed out: {ex.Message}");
                    _asyncResult = AsyncLevelResult.Failure(
                        AsyncLevelFailureReason.BudgetExceeded,
                        new PuzzleGenerationMetrics
                        {
                            BudgetExceeded = true,
                            LastError = ex.Message
                        });
                }
                catch (Exception ex)
                {
                    // Unity logging is thread-safe; message is queued to the main thread.
                    Debug.LogError($"[RunDirector] Async overlay generation threw: {ex.Message}");
                    _asyncResult = AsyncLevelResult.Failure(AsyncLevelFailureReason.TaskFaulted);
                }
            });
        }

        /// <summary>
        /// Returns generation settings tuned for the given config.
        /// High-complexity boss puzzles (7+ modifiers) use fewer overlay seed attempts so the first
        /// valid overlay is accepted sooner, with an extended time budget for board generation.
        /// </summary>
        private static PuzzleGenerationSettings SelectAsyncGenerationSettings(LevelConfig config)
        {
            if (HasBoardShapingModifier(config))
                return PuzzleGenerationSettings.StructuralModifierDefault;
            if (config.IsBoss && config.ActiveModifiers != null && config.ActiveModifiers.Count >= 7)
                return PuzzleGenerationSettings.HighComplexityBossDefault;
            return PuzzleGenerationSettings.AsyncBossDefault;
        }

        // Returns true when the structural modifier subset of both lists is identical.
        // The pre-bake is valid whenever the player's boss choices leave the structural set unchanged.
        private static bool StructuralModsMatch(IList<BossModifierId> a, IList<BossModifierId> b)
        {
            BossModifierId[] structural =
            {
                BossModifierId.Nonconsecutive, BossModifierId.Antiknight,
                BossModifierId.NonconsecDiagonal, BossModifierId.AntiBishop,
                BossModifierId.Antiking, BossModifierId.DistanceGe2
            };
            foreach (var mod in structural)
            {
                var inA = a != null && a.Contains(mod);
                var inB = b != null && b.Contains(mod);
                if (inA != inB) return false;
            }
            return true;
        }

        /// <summary>
        /// Call each frame after <see cref="StartLevelAsync"/>. Returns true once the background overlay
        /// generation is done and all level state has been finalized on the main thread.
        /// </summary>
        public bool TryCompleteAsyncLevel()
        {
            if (_pendingOverlayTask == null) return false;
            if (!_pendingOverlayTask.IsCompleted) return false;

            var config = _pendingLevelConfig;

            Debug.Log($"[RunDirector] TryCompleteAsyncLevel: config mods={config?.ActiveModifiers?.Count ?? -1}, success={_asyncResult.IsSuccess}, reason={_asyncResult.FailureReason}");
            LastGenerationMetrics = _asyncResult.Metrics;
            LastGenerationMissingModifiers = _asyncResult.MissingModifiers ?? new List<BossModifierId>();

            if (config == null)
            {
                Debug.LogError("[RunDirector] TryCompleteAsyncLevel: _pendingLevelConfig is null after async completion — aborting.");
                _pendingOverlayTask = null;
                return false;
            }

            // AsyncLevelResult.IsSuccess is false when the background task threw or board generation
            // failed. Do not fall back to synchronous generation here; the UI handles the controlled
            // failure state without blocking the main thread.
            if (!_asyncResult.IsSuccess)
            {
                Debug.LogWarning($"[RunDirector] TryCompleteAsyncLevel: async generation failed " +
                    $"({_asyncResult.FailureReason}) — leaving puzzle unloaded.");
                LogGenerationFailure("TryCompleteAsyncLevel", config, _asyncResult.FailureReason, _asyncResult.Metrics);
                _pendingOverlayTask = null;
                _pendingLevelConfig = null;
                _pendingLevelGenerationCancel?.Dispose();
                _pendingLevelGenerationCancel = null;
                CurrentLevelConfig = null;
                CurrentLevelState = null;
                CurrentBoard = null;
                CurrentOverlay = new ModifierOverlayData();
                ConstraintEngine = null;
                return true;
            }

            CurrentLevelConfig = config;
            CurrentBoard   = _asyncResult.Board;
            CurrentOverlay = _asyncResult.Overlay;

            if (config.ActiveModifiers.Count > 0 && !HasAllModifiersPresent(config.ActiveModifiers, CurrentOverlay))
            {
                // Async overlay did not cover all modifiers — drop those without geometry to prevent
                // the constraint engine from registering rules against non-existent overlay data.
                for (var i = config.ActiveModifiers.Count - 1; i >= 0; i--)
                {
                    if (!IsModifierPresentInOverlay(config.ActiveModifiers[i], CurrentOverlay))
                    {
                        Debug.LogWarning($"[RunDirector] TryCompleteAsyncLevel: dropping modifier {config.ActiveModifiers[i]} — no overlay geometry in async result.");
                        config.ActiveModifiers.RemoveAt(i);
                    }
                }
            }

            if (config.ActiveModifiers.Count > 0)
                ClearOverlayCellsFromGivenMask();

            ConstraintEngine = new SudokuConstraintEngine();
            var rules = ModifierFactory.BuildRules(config.ActiveModifiers);
            for (var i = 0; i < rules.Count; i++)
                ConstraintEngine.RegisterRule(rules[i]);

            RelicService.OnPuzzleStart(State, CurrentBoard);
            CurseService.OnPuzzleStart(State);
            State.ShieldPoints = 0;
            State.LastMistakeShieldAbsorbed = false;

            var pressureRng = new System.Random(config.Seed ^ 0x50A4B3C2);
            InitializePressureMechanics(config, CurrentBoard, CurrentLevelState, pressureRng);

            _pendingOverlayTask = null;
            _pendingLevelConfig = null;
            _pendingLevelGenerationCancel?.Dispose();
            _pendingLevelGenerationCancel = null;
            return true;
        }

        private static AsyncLevelFailureReason MapFailureReason(PuzzleGenerationFailureReason reason)
        {
            switch (reason)
            {
                case PuzzleGenerationFailureReason.BoardGenerationFailed:
                    return AsyncLevelFailureReason.GenerationFailed;
                case PuzzleGenerationFailureReason.BudgetExceeded:
                    return AsyncLevelFailureReason.BudgetExceeded;
                case PuzzleGenerationFailureReason.Cancelled:
                    return AsyncLevelFailureReason.Cancelled;
                case PuzzleGenerationFailureReason.TaskFaulted:
                    return AsyncLevelFailureReason.TaskFaulted;
                case PuzzleGenerationFailureReason.UniqueSolutionNotFound:
                    return AsyncLevelFailureReason.GenerationFailed;
                default:
                    return AsyncLevelFailureReason.None;
            }
        }

        private static void LogGenerationFailure(
            string source,
            LevelConfig config,
            AsyncLevelFailureReason reason,
            PuzzleGenerationMetrics metrics)
        {
            if (config == null) return;

            var mods = config.ActiveModifiers == null || config.ActiveModifiers.Count == 0
                ? "none"
                : string.Join(", ", config.ActiveModifiers);
            var elapsed = metrics?.ElapsedMilliseconds ?? 0;
            var error = string.IsNullOrEmpty(metrics?.LastError) ? "none" : metrics.LastError;
            Debug.LogWarning(
                $"[RunDirector] {source}: puzzle generation failed " +
                $"reason={reason}, size={config.BoardSize}, stars={config.Stars}, " +
                $"region={config.RegionVariant}, seed={config.Seed}, boss={config.IsBoss}, " +
                $"elite={config.IsElite}, preBoss={config.IsPreBoss}, cursed={config.IsCursed}, " +
                $"mods=[{mods}], elapsedMs={elapsed}, error={error}");
        }

        public void StartLevel(LevelConfig config)
        {
            SanitizeGeneratedLevelConfig(config);
            EnsureBoardSizeSupportsActiveModifiers(config);

            // Discard any in-flight async boss task so its completion cannot overwrite
            // the board/overlay/rules we're about to build synchronously here.
            _pendingLevelGenerationCancel?.Cancel();
            _pendingLevelGenerationCancel = null;
            _pendingOverlayTask = null;
            _pendingLevelConfig = null;
            _asyncResult        = default;

            CurrentLevelConfig = config;
            CurrentLevelState = new LevelState();
            State.HpAtFloorStart = State.CurrentHP;

            var levelRng = new System.Random(config.Seed ^ 0x1A2B3C4D);
            var settings = PuzzleGenerationSettings.SynchronousDefault;
            var deadline = new GenerationDeadline(settings.TimeBudgetMs);

            // Use pre-baked board when seed, size, and structural mods all match.
            if (config.IsBoss && _preBakedBossBoard != null
                && _preBakedBossConfig != null
                && _preBakedBossConfig.Seed == config.Seed
                && _preBakedBossConfig.BoardSize == config.BoardSize
                && StructuralModsMatch(config.ActiveModifiers, _preBakedBossConfig.ActiveModifiers))
            {
                CurrentBoard = _preBakedBossBoard;
            }
            else
            {
                try
                {
                    CurrentBoard = CreatePuzzleForConfig(config, config.Seed, deadline);
                }
                catch (TimeoutException ex)
                {
                    Debug.LogError($"[RunDirector] StartLevel: board generation timed out for seed={config.Seed}, size={config.BoardSize}: {ex.Message}");
                    LogGenerationFailure(
                        "StartLevel",
                        config,
                        AsyncLevelFailureReason.BudgetExceeded,
                        new PuzzleGenerationMetrics { BudgetExceeded = true, LastError = ex.Message });
                    CurrentBoard = null;
                    CurrentOverlay = new ModifierOverlayData();
                    return;
                }
                if (CurrentBoard == null)
                {
                    Debug.LogError($"[RunDirector] StartLevel: board generation failed for seed={config.Seed}, size={config.BoardSize}. Aborting.");
                    LogGenerationFailure("StartLevel", config, AsyncLevelFailureReason.GenerationFailed, null);
                    return;
                }
            }

            _preBakedBossBoard = null;
            _preBakedBossConfig = null;

            var generationResult = _puzzleGenerationService.Generate(
                config,
                CurrentBoard,
                levelRng.Next(),
                CreatePuzzleForConfig,
                settings);

            LastGenerationMetrics = generationResult.Metrics;
            LastGenerationMissingModifiers = generationResult.MissingModifiers;

            if (!generationResult.IsSuccess)
            {
                Debug.LogError($"[RunDirector] StartLevel: generation failed ({generationResult.FailureReason}). Aborting.");
                LogGenerationFailure("StartLevel", config, MapFailureReason(generationResult.FailureReason), generationResult.Metrics);
                CurrentBoard = null;
                CurrentOverlay = new ModifierOverlayData();
                return;
            }

            CurrentBoard = generationResult.Board;
            CurrentOverlay = generationResult.Overlay;

            if (config.ActiveModifiers.Count > 0 && !generationResult.HasCompleteOverlay)
            {
                // Overlay exhausted — drop modifiers that have no geometry to prevent the
                // constraint engine from registering rules against non-existent overlay data.
                for (var i = config.ActiveModifiers.Count - 1; i >= 0; i--)
                {
                    if (!IsModifierPresentInOverlay(config.ActiveModifiers[i], CurrentOverlay))
                    {
                        Debug.LogWarning($"[RunDirector] StartLevel: dropping modifier {config.ActiveModifiers[i]} — no overlay geometry after all retries.");
                        config.ActiveModifiers.RemoveAt(i);
                    }
                }
            }

            if (config.ActiveModifiers.Count > 0)
                ClearOverlayCellsFromGivenMask();

            // Build constraint engine
            ConstraintEngine = new SudokuConstraintEngine();
            var rules = ModifierFactory.BuildRules(config.ActiveModifiers);
            for (var i = 0; i < rules.Count; i++)
                ConstraintEngine.RegisterRule(rules[i]);

            // Relic: puzzle-start effects
            RelicService.OnPuzzleStart(State, CurrentBoard);
            // Curse: reset puzzle-scoped state
            CurseService.OnPuzzleStart(State);
            // Shield: resets each puzzle so Silk Fan is a per-node decision
            State.ShieldPoints = 0;
            State.LastMistakeShieldAbsorbed = false;

            // [REQ: PRESSURE-SAVE-002] InitializePressureMechanics is called at the end of every StartLevel path (floor-gated; no-op in tutorial)
            var pressureRng = new System.Random(config.Seed ^ 0x50A4B3C2);
            InitializePressureMechanics(config, CurrentBoard, CurrentLevelState, pressureRng);
        }

        // ── Gameplay ──

        public PlaceResult PlaceNumber(int row, int col, int value)
        {
            if (CurrentBoard == null || CurrentLevelState == null) return PlaceResult.Correct;
            if (CurrentBoard.IsGiven(row, col))
                return PlaceResult.IsGiven;
            if (CurrentLevelState != null && CurrentLevelState.PresolvedCells.Contains((row, col)))
                return PlaceResult.IsPresolved;

            // Re-placing the same value into a cell that already holds it shouldn't
            // count as a new placement or advance the combo streak.
            if (CurrentBoard.Cells[row, col] == value)
                return PlaceResult.Correct;

            // Fog of War: when a cell is actively fogged, defer correctness evaluation
            // until the fog is removed from that cell.
            if (State.FogDisabledMoves <= 0 && IsCellInFog(row, col))
            {
                CurrentBoard.PlaceValue(row, col, value);
                var tremblingDmgFog = CurseService.GetTremblingHandDamage(State);
                if (tremblingDmgFog > 0) ApplyMistakePenalty(tremblingDmgFog);
                CurrentLevelState.FogPendingCells.Add((row, col));
                return PlaceResult.Correct; // confirmation deferred until fog lifts
            }

            // [REQ: GEN-VALID-002] Placement validity determined by SudokuConstraintEngine.ValidateAll (all active modifier rules)
            var isValid = ConstraintEngine.ValidateAll(CurrentBoard, row, col, value, CurrentOverlay);

            CurrentBoard.PlaceValue(row, col, value);

            // [REQ: CURSE-INT-002] trembling_hand: first placement per puzzle costs 1 HP regardless of correctness
            var tremblingDmg = CurseService.GetTremblingHandDamage(State);
            if (tremblingDmg > 0) ApplyMistakePenalty(tremblingDmg);

            if (isValid)
            {
                // Determine fog state before the overlay is mutated by reveal.
                var cellIsFogged = IsCellInFog(row, col);

                CurrentLevelState.CorrectPlacements++;
                CurrentLevelState.EnsureCountPlaced(CurrentBoard.Size);
                if (value >= 1 && value <= CurrentBoard.Size)
                    CurrentLevelState.CountPlaced[value]++;
                if (State.MonksBeadsCountdown > 0)
                {
                    State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + 1);
                    State.MonksBeadsCountdown--;
                }
                ClassPassiveService.OnCorrectPlacement(State, CurrentLevelState);
                TickLockedCells();
                TickDebuffTimers(); // [REQ: DEBUFF-HOOK-019] decrements RevealCost + CellBlur counters on correct placement
                TickPressureThreats(row, col);
                if (State.FogDisabledMoves > 0) State.FogDisabledMoves--;

                if (!cellIsFogged)
                {
                    // Visible placement: reveal adjacent fog, evaluate fog-pending cells, award combo.
                    if (CurrentOverlay?.FogCells?.Count > 0)
                    {
                        var prevFog = BuildFogSet();
                        ModifierGeometryGenerator.RevealAdjacentFog(CurrentOverlay, row, col, CurrentBoard.Size);
                        EvaluateFogPendingCells(prevFog);
                    }
                    State.ComboStreak++;
                    if (State.ComboStreak > State.PeakComboThisRun)
                        State.PeakComboThisRun = State.ComboStreak;
                }
                // Fog placement (lantern active): treat normally, no deferred logic.

                RelicService.OnCorrectPlacement(State);

                // Pencil mark cleanup: remove the placed digit from all cells in the same row and column.
                var boardSize = CurrentBoard.Size;
                for (var pc = 0; pc < boardSize; pc++)
                    CurrentBoard.RemovePencilMark(row, pc, value);
                for (var pr = 0; pr < boardSize; pr++)
                    CurrentBoard.RemovePencilMark(pr, col, value);

                return PlaceResult.Correct;
            }

            State.LastComboBeforeMistake = State.ComboStreak;
            State.LastMistakeRow = row;
            State.LastMistakeCol = col;
            // Shield: if the mistake was fully absorbed by ShieldPoints, preserve the combo streak.
            if (!State.LastMistakeShieldAbsorbed)
                State.ComboStreak = 0;
            CurrentLevelState.Mistakes++;
            CurrentLevelState.PerfectSoFar = false;
            // [REQ: PRESSURE-TICK-002] Wrong placements also decrement threat counters (no success check).
            TickPressureCountersOnly();
            return PlaceResult.Invalid;
        }

        /// <summary>Builds a set of currently fogged cell coordinates.</summary>
        private HashSet<(int, int)> BuildFogSet()
        {
            var set = new HashSet<(int, int)>();
            var fog = CurrentOverlay?.FogCells;
            if (fog == null) return set;
            for (var i = 0; i < fog.Count; i++)
                set.Add((fog[i].Row, fog[i].Col));
            return set;
        }

        /// <summary>
        /// After fog cells are revealed, evaluate any fog-pending placements that are now visible.
        /// Applies a mistake penalty (with debuff handling) for any wrong fog placements.
        /// </summary>
        private void EvaluateFogPendingCells(HashSet<(int, int)> prevFogSet)
        {
            if (CurrentLevelState == null || CurrentLevelState.FogPendingCells == null || CurrentLevelState.FogPendingCells.Count == 0) return;

            var currentFog = BuildFogSet();
            var toEvaluate = new List<(int r, int c)>();
            foreach (var cell in CurrentLevelState.FogPendingCells)
                if (prevFogSet.Contains(cell) && !currentFog.Contains(cell))
                    toEvaluate.Add(cell);

            foreach (var (r, c) in toEvaluate)
            {
                CurrentLevelState.FogPendingCells.Remove((r, c));
                var placed = CurrentBoard.Cells[r, c];
                if (placed > 0 && placed != CurrentBoard.Solution[r, c])
                {
                    // Deferred wrong placement revealed — apply full debuff-aware penalty.
                    CurrentLevelState.Mistakes++;
                    CurrentLevelState.PerfectSoFar = false;
                    State.LastComboBeforeMistake = State.ComboStreak;
                    State.LastMistakeRow = r;
                    State.LastMistakeCol = c;
                    ApplyMistakePenalty(r, c);
                }
            }
        }

        public bool TryAddPencilMark(int row, int col, int value)
        {
            if (CurrentBoard.IsGiven(row, col)) return false;
            var isRemoving = CurrentBoard.GetPencilMarks(row, col).Contains(value);
            var cellAlreadyHasMark = CurrentBoard.HasAnyPencilMark(row, col);
            var free = isRemoving
                || State.Mode == GameMode.Tutorial
                || ClassPassiveService.IsPencilFree(State, CurrentLevelState, row, col, cellAlreadyHasMark)
                || RelicService.HasEndlessArchive(State);

            if (!free && State.CurrentPencil <= 0) return false;

            // ReedPledge: any paid pencil use triggers penalty and cancels the pledge
            if (!free && State.PledgeActive)
            {
                State.CurrentHP = Math.Max(0, State.CurrentHP - 1);
                State.PledgeActive = false;
            }

            CurrentBoard.TogglePencilMark(row, col, value);
            if (!free)
            {
                var pencilCost = CurseService.IsActive(State, "hollow_pencil") ? 2 : 1; // [REQ: CURSE-POOL-001]
                State.CurrentPencil = Math.Max(0, State.CurrentPencil - pencilCost);
                CurrentLevelState.PencilMarksUsed++;
                CurrentLevelState.NoPencilUsed = false;
                if (DailyGoals != null && !State.IsSeasonalChallenge)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerPencilMarkUsed);
            }
            return true;
        }

        // [REQ: PRESSURE-TICK-019] ApplyMistakePenalty(int damage): single HP-deduct overload — all pressure/debuff HP costs route through here
        public void ApplyMistakePenalty(int damage = 1)
        {
            // Clear per-call flag before any early returns
            State.LastMistakeShieldAbsorbed = false;

            // TempleStamp: HP is locked for the entire puzzle — absorb all damage silently.
            if (State.TempleSealActive) { State.LastMistakeHpLost = 0; return; }

            if (State.MistakeShieldCharges > 0) { State.MistakeShieldCharges--; State.LastMistakeHpLost = 0; return; }
            if (ClassPassiveService.OnMistake(State)) { State.LastMistakeHpLost = 0; return; } // class passive absorbs
            if (RelicService.TryAbsorbMistake(State)) { State.LastMistakeHpLost = 0; return; }
            State.MonksBeadsCountdown = 0; // any mistake cancels MonksBeads streak

            // Shield system: drain ShieldPoints before touching CurrentHP.
            if (State.ShieldPoints > 0)
            {
                var absorbed = Math.Min(damage, State.ShieldPoints);
                State.ShieldPoints -= absorbed;
                damage -= absorbed;
                if (damage == 0)
                {
                    State.LastMistakeShieldAbsorbed = true;
                    State.LastMistakeHpLost = 0;
                    return;
                }
                // Partial absorb: remaining damage hits HP, combo is NOT protected.
            }

            var hpBefore = State.CurrentHP;
            var newHp = State.CurrentHP - damage;
            if (newHp <= 0 && RelicService.TryPreventDeath(State))
            {
                State.LastMistakeHpLost = Math.Max(0, hpBefore - State.CurrentHP);
                if (CurrentLevelState != null)
                    CurrentLevelState.HpLost += State.LastMistakeHpLost;
                RelicService.OnHpChanged(State);
                return;
            }

            State.CurrentHP = Math.Max(0, newHp);
            State.LastMistakeHpLost = hpBefore - State.CurrentHP;
            if (CurrentLevelState != null)
                CurrentLevelState.HpLost += State.LastMistakeHpLost;
            RelicService.OnHpChanged(State);
        }

        /// <summary>
        /// Debuff-aware mistake penalty. Triggers RowWipe, ColWipe, PencilBlind, CellLock, DoublePenalty
        /// based on active modifiers in the current level config.
        /// </summary>
        // [REQ: DEBUFF-HOOK-001] ApplyMistakePenalty(row,col) is the central dispatch for all debuff effects
        public void ApplyMistakePenalty(int row, int col)
        {
            // [REQ: DEBUFF-HOOK-002] DoublePenalty: damage=2 instead of 1
            // [REQ: CURSE-INT-002] phantom_pain curse raises base damage to 2
            var damage = IsDebuffActive(BossModifierId.DoublePenalty) ? 2 : 1;
            damage = Math.Max(damage, CurseService.GetMistakeDamage(State)); // phantom_pain
            ApplyMistakePenalty(damage);

            // [REQ: DEBUFF-HOOK-003] RowWipe: clear all non-given digits in wrong cell's row
            if (IsDebuffActive(BossModifierId.RowWipe))
                ClearBoardRow(row);

            // [REQ: DEBUFF-HOOK-004] ColWipe: clear all non-given digits in wrong cell's column
            if (IsDebuffActive(BossModifierId.ColWipe))
                ClearBoardCol(col);

            // [REQ: DEBUFF-HOOK-005] PencilBlind: clear all pencil marks in row + column
            if (IsDebuffActive(BossModifierId.PencilBlind))
                ClearPencilRowCol(row, col);

            // [REQ: DEBUFF-HOOK-006] CellLock: mark wrong cell as un-editable for 3 correct placements
            if (IsDebuffActive(BossModifierId.CellLock))
                LockCell(row, col);

            if (IsDebuffActive(BossModifierId.BoxWipe))
                ClearBoardRegion(row, col);

            if (IsDebuffActive(BossModifierId.CrossWipe))
            {
                ClearBoardRow(row);
                ClearBoardCol(col);
            }

            if (IsDebuffActive(BossModifierId.PencilDrain))
                State.CurrentPencil = Math.Max(0, State.CurrentPencil - 1);

            if (IsDebuffActive(BossModifierId.GoldFine))
                State.CurrentGold = Math.Max(0, State.CurrentGold - 5);

            // Pressure: mistake-driven effects (HauntedCell extra HP, CrumblingRegion fragility)
            TickPressureMistake(row, col);
            // [REQ: PRESSURE-TICK-002] Wrong placements also decrement all active threat counters
            TickPressureThreats(row, col);

            // [REQ: DEBUFF-DEF-005] [REQ: DEBUFF-HOOK-014] RadiusWipe: clear all non-given digits within Chebyshev radius 2
            if (IsDebuffActive(BossModifierId.RadiusWipe))
                ClearBoardRadius(row, col);

            // [REQ: DEBUFF-DEF-008] [REQ: DEBUFF-HOOK-015] PencilScramble: scramble all pencil marks board-wide with random wrong values
            if (IsDebuffActive(BossModifierId.PencilScramble))
                ScramblePencilMarks();

            // [REQ: DEBUFF-DEF-017] [REQ: DEBUFF-HOOK-016] GivenReveal: promote up to 2 correctly placed non-given digits to given
            if (IsDebuffActive(BossModifierId.GivenReveal))
                ConvertCellsToGiven(2);

            // [REQ: DEBUFF-DEF-015] [REQ: DEBUFF-HOOK-017] RevealCost: block item use for 2 correct placements
            if (IsDebuffActive(BossModifierId.RevealCost))
                CurrentLevelState.ItemUseBlockedMoves = Math.Max(CurrentLevelState.ItemUseBlockedMoves, 2);

            // [REQ: DEBUFF-DEF-011] [REQ: DEBUFF-HOOK-018] CellBlur: hide digit labels in 3×3 area for 3 correct placements
            if (IsDebuffActive(BossModifierId.CellBlur))
                ApplyCellBlur(row, col);
        }

        private bool IsDebuffActive(BossModifierId mod)
        {
            return CurrentLevelConfig != null && CurrentLevelConfig.ActiveModifiers.Contains(mod);
        }

        private void ClearBoardRow(int row)
        {
            if (CurrentBoard == null) return;
            var size = CurrentBoard.Size;
            for (var c = 0; c < size; c++)
            {
                if (!CurrentBoard.IsGiven(row, c))
                    CurrentBoard.ClearValue(row, c);
            }
        }

        private void ClearBoardCol(int col)
        {
            if (CurrentBoard == null) return;
            var size = CurrentBoard.Size;
            for (var r = 0; r < size; r++)
            {
                if (!CurrentBoard.IsGiven(r, col))
                    CurrentBoard.ClearValue(r, col);
            }
        }

        private void ClearBoardRegion(int row, int col)
        {
            if (CurrentBoard == null) return;
            var regionId = CurrentBoard.RegionMap[row, col];
            var size = CurrentBoard.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (CurrentBoard.RegionMap[r, c] == regionId && !CurrentBoard.IsGiven(r, c))
                    CurrentBoard.ClearValue(r, c);
        }

        private void ClearPencilRowCol(int row, int col)
        {
            if (CurrentBoard == null) return;
            var size = CurrentBoard.Size;
            for (var c = 0; c < size; c++)
                CurrentBoard.ClearPencilMarks(row, c);
            for (var r = 0; r < size; r++)
                CurrentBoard.ClearPencilMarks(r, col);
        }

        // [REQ: DEBUFF-DEF-005] RadiusWipe: clear all non-given digits within Chebyshev radius of the mistake cell
        private void ClearBoardRadius(int row, int col, int radius = 2)
        {
            if (CurrentBoard == null) return;
            var size = CurrentBoard.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (Math.Max(Math.Abs(r - row), Math.Abs(c - col)) <= radius && !CurrentBoard.IsGiven(r, c))
                    CurrentBoard.ClearValue(r, c);
        }

        // [REQ: DEBUFF-DEF-008] PencilScramble: replace pencil marks in all empty cells with random wrong values
        private void ScramblePencilMarks()
        {
            if (CurrentBoard == null || CurrentLevelState == null) return;
            var size = CurrentBoard.Size;
            var rng = new System.Random((State?.Seed ?? 0) ^ CurrentLevelState.Mistakes);
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (CurrentBoard.Cells[r, c] != 0) continue;
                CurrentBoard.ClearPencilMarks(r, c);
                var markCount = 2 + rng.Next(2); // 2 or 3 marks per empty cell
                for (var i = 0; i < markCount; i++)
                {
                    var mark = 1 + rng.Next(size);
                    if (mark != CurrentBoard.Solution[r, c])
                        CurrentBoard.AddPencilMark(r, c, mark);
                }
            }
        }

        // [REQ: DEBUFF-DEF-017] GivenReveal: promote up to `count` correctly placed non-given cells to given
        private void ConvertCellsToGiven(int count)
        {
            if (CurrentBoard == null || CurrentLevelState == null) return;
            var size = CurrentBoard.Size;
            var candidates = new List<(int r, int c)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (!CurrentBoard.IsGiven(r, c) && CurrentBoard.Cells[r, c] != 0 && CurrentBoard.IsCorrectAt(r, c))
                    candidates.Add((r, c));
            var rng = new System.Random((State?.Seed ?? 0) ^ CurrentLevelState.Mistakes);
            var toConvert = Math.Min(count, candidates.Count);
            for (var i = 0; i < toConvert; i++)
            {
                var idx = rng.Next(candidates.Count);
                var (r, c) = candidates[idx];
                candidates.RemoveAt(idx);
                CurrentBoard.GivenMask[r, c] = true;
            }
        }

        // [REQ: DEBUFF-DEF-011] CellBlur: mark cells in a (2*radius+1)² area as blurred for `duration` correct placements
        private void ApplyCellBlur(int row, int col, int radius = 1, int duration = 3)
        {
            if (CurrentBoard == null || CurrentLevelState == null) return;
            var size = CurrentBoard.Size;
            for (var r = Math.Max(0, row - radius); r <= Math.Min(size - 1, row + radius); r++)
            for (var c = Math.Max(0, col - radius); c <= Math.Min(size - 1, col + radius); c++)
                CurrentLevelState.CellBlurCells.Add((r, c));
            CurrentLevelState.CellBlurMovesLeft = Math.Max(CurrentLevelState.CellBlurMovesLeft, duration);
        }

        private void LockCell(int row, int col)
        {
            if (CurrentLevelState == null) return;
            var key = row * 100 + col;
            CurrentLevelState.LockedCells[key] = 3;
        }

        /// <summary>
        /// Called after each correct placement. Decrements CellLock counters and removes expired locks.
        /// </summary>
        // [REQ: DEBUFF-HOOK-007] TickLockedCells: decrement/remove CellLock entries after each correct placement
        public void TickLockedCells()
        {
            if (CurrentLevelState == null || CurrentLevelState.LockedCells.Count == 0) return;

            var toRemove = new List<int>();
            var keys = new List<int>(CurrentLevelState.LockedCells.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                CurrentLevelState.LockedCells[k]--;
                if (CurrentLevelState.LockedCells[k] <= 0)
                    toRemove.Add(k);
            }
            for (var i = 0; i < toRemove.Count; i++)
                CurrentLevelState.LockedCells.Remove(toRemove[i]);
        }

        /// <summary>
        /// Called after each correct placement. Decrements RevealCost and CellBlur timers.
        /// </summary>
        // [REQ: DEBUFF-DEF-015] RevealCost timer; [REQ: DEBUFF-DEF-011] CellBlur timer
        private void TickDebuffTimers()
        {
            if (CurrentLevelState == null) return;
            if (CurrentLevelState.ItemUseBlockedMoves > 0)
                CurrentLevelState.ItemUseBlockedMoves--;
            if (CurrentLevelState.CellBlurMovesLeft > 0)
            {
                CurrentLevelState.CellBlurMovesLeft--;
                if (CurrentLevelState.CellBlurMovesLeft == 0)
                    CurrentLevelState.CellBlurCells.Clear();
            }
        }

        /// <summary>Returns the set of currently locked cell positions as (row, col) pairs.</summary>
        // [REQ: DEBUFF-HOOK-008] GetLockedCells: consumed by InRunController (input block) and BoardViewController (tint)
        public HashSet<(int row, int col)> GetLockedCells()
        {
            var result = new HashSet<(int, int)>();
            if (CurrentLevelState == null) return result;
            foreach (var kv in CurrentLevelState.LockedCells)
                result.Add((kv.Key / 100, kv.Key % 100));
            return result;
        }

        // ── Pressure mechanics ────────────────────────────────────────────────

        /// <summary>
        /// Floor- and node-type-gated rolling. Adds one pressure mechanic modifier to
        /// config.ActiveModifiers when the puzzle qualifies. Called from BuildLevelConfig.
        /// </summary>
        // [REQ: PRESSURE-ROLL-001] no floor 1 / tutorial  [PRESSURE-ROLL-002] node-type gate
        // [REQ: PRESSURE-ROLL-003] floor probability       [PRESSURE-ROLL-004] one mechanic max
        // [REQ: PRESSURE-ROLL-005] PressureWave floor 3+
        private void RollPressureMechanic(LevelConfig config, int floor, System.Random rng)
        {
            if (State.TutorialMode) return;
            if (floor < 2) return;
            if (!config.IsElite && !config.IsPreBoss && !config.IsBoss) return;

            float chance = floor switch { 2 => 0.12f, 3 => 0.30f, 4 => 0.55f, _ => 0.80f };
            if (rng.NextDouble() > chance) return;

            var candidates = new System.Collections.Generic.List<BossModifierId>
            {
                BossModifierId.CountdownFill,
                BossModifierId.HauntedCell,
                BossModifierId.CrumblingRegion
            };
            if (floor >= 3) candidates.Add(BossModifierId.PressureWave);

            var chosen = candidates[rng.Next(candidates.Count)];
            if (!config.ActiveModifiers.Contains(chosen))
                config.ActiveModifiers.Add(chosen);
        }

        /// <summary>
        /// Allocates per-puzzle pressure state for any pressure mechanic IDs in the config.
        /// Also applies curse-driven pressure (counting_shadow, hollow_eye).
        /// No-op in tutorial mode or when no pressure mechanic is active.
        /// </summary>
        // [REQ: PRESSURE-INIT-001] CountdownFill    [PRESSURE-INIT-002] HauntedCell
        // [REQ: PRESSURE-INIT-003] CrumblingRegion  [PRESSURE-INIT-004] PressureWave
        // [REQ: PRESSURE-INIT-005] Chain length     [PRESSURE-INIT-006] PresolvedCells exclusion
        // [REQ: PRESSURE-INIT-007] counting_shadow  [PRESSURE-INIT-008] hollow_eye
        // [REQ: PRESSURE-INIT-001] InitializePressureMechanics: sets up all pressure state for the level
        // [REQ: PRESSURE-INIT-002] HauntedCell: intensity-keyed selection via SelectHauntedCell
        // [REQ: PRESSURE-INIT-004] PressureWave: WaveInterval/WaveThreatCounter/WaveCounter initialized
        // [REQ: PRESSURE-INIT-006] PresolvedCells excluded from all cell selection helpers
        // [REQ: PRESSURE-INIT-008] hollow_eye curse: haunts a cell when HauntedCell == (-1,-1)
        // [REQ: PRESSURE-EDGE-002] CrumblingRegion: falls back when no qualifying region found (regionType < 0)
        // [REQ: PRESSURE-EDGE-005] counting_shadow + modifier coexist additively
        // [REQ: PRESSURE-EDGE-006] hollow_eye defers when HauntedCell already set by modifier
        private void InitializePressureMechanics(LevelConfig config, SudokuBoard board,
            LevelState levelState, System.Random rng)
        {
            if (State.TutorialMode) return;
            if (board == null || levelState == null) return;

            foreach (var mod in config.ActiveModifiers)
            {
                switch (mod)
                {
                    case BossModifierId.CountdownFill:
                    {
                        var scope = PickCountdownScope(config.Intensity, rng);
                        var cells  = SelectThreatCells(board, scope, rng);
                        if (cells.Count == 0) break;
                        var counter = CounterForScope(scope, cells.Count, config.Intensity);
                        levelState.ActiveThreats.Add(new PressureThreat
                        {
                            Scope               = scope,
                            Cells               = cells,
                            RemainingPlacements = counter,
                            InitialPlacements   = counter,
                            IsChain             = scope == ThreatScope.Chain
                        });
                        break;
                    }
                    case BossModifierId.HauntedCell:
                    {
                        var cell = SelectHauntedCell(board, config.Intensity, rng);
                        levelState.HauntedCell = cell;
                        break;
                    }
                    case BossModifierId.CrumblingRegion:
                    {
                        var (regionType, regionIndex) = SelectCrumblingRegion(board, rng);
                        if (regionType < 0) break;
                        var fragility = FragilityForIntensity(config.Intensity);
                        levelState.CrumblingStartFragility = fragility;
                        levelState.CrumblingVeryHigh = (config.Intensity == BossModifierIntensity.VeryHigh);
                        var emptyCells = GetRegionEmptyCells(board, regionType, regionIndex);
                        foreach (var (r, c) in emptyCells)
                            levelState.CrumblingCells[r * 100 + c] = fragility;
                        break;
                    }
                    case BossModifierId.PressureWave:
                    {
                        levelState.WaveInterval      = WaveIntervalForIntensity(config.Intensity);
                        levelState.WaveThreatCounter = PerThreatCounterForIntensity(config.Intensity);
                        levelState.WaveCounter       = levelState.WaveInterval;
                        levelState.WaveSpawnCount    = 0;
                        break;
                    }
                }
            }

            // Curse-driven pressure
            if (CurseService.IsActive(State, "counting_shadow"))
            {
                var cells = SelectThreatCells(board, ThreatScope.SingleCell, rng);
                if (cells.Count > 0)
                    levelState.ActiveThreats.Add(new PressureThreat
                    {
                        Scope               = ThreatScope.SingleCell,
                        Cells               = cells,
                        RemainingPlacements = State.CurrentFloor + 4,
                        InitialPlacements   = State.CurrentFloor + 4
                    });
            }
            if (CurseService.IsActive(State, "hollow_eye") &&
                levelState.HauntedCell == (-1, -1))
            {
                levelState.HauntedCell = SelectHauntedCell(board, BossModifierIntensity.Medium, rng);
            }
        }

        /// <summary>
        /// Called after a wrong placement. Decrements threat counters and checks for expiry,
        /// but does NOT check for threat success (a wrong placement cannot complete a threat cell).
        /// </summary>
        // [REQ: PRESSURE-TICK-002]
        private void TickPressureCountersOnly()
        {
            var ls = CurrentLevelState;
            if (ls == null || ls.ActiveThreats.Count == 0 && ls.WaveInterval == 0) return;

            for (var i = 0; i < ls.ActiveThreats.Count; i++)
                ls.ActiveThreats[i].RemainingPlacements--;

            var toRemove = new System.Collections.Generic.List<PressureThreat>();
            foreach (var threat in ls.ActiveThreats)
            {
                if (threat.RemainingPlacements <= 0)
                {
                    // [REQ: PRESSURE-UI-004] flash unfilled cells red on expiry (wrong-placement path)
                    foreach (var c in threat.Cells) ls.ExpiryFlashCells.Add(c);
                    // [REQ: PRESSURE-TICK-006] DoublePenalty doubles expiry damage (4 instead of 2 per cell)
                    var penaltyPer = IsDebuffActive(BossModifierId.DoublePenalty) ? 4 : 2;
                    var count = threat.Cells.Count;
                    for (var j = 0; j < count; j++) ApplyMistakePenalty(penaltyPer);
                    if (threat.IsWaveSpawned) ls.WaveSpawnCount--;
                    toRemove.Add(threat);
                }
            }
            foreach (var t in toRemove) ls.ActiveThreats.Remove(t);

            // Wave counter still ticks on wrong placements
            if (ls.WaveInterval > 0)
            {
                ls.WaveCounter--;
                if (ls.WaveCounter <= 0)
                {
                    // [REQ: PRESSURE-TICK-008] Wave spawn capped at 3 active wave threats
                    if (ls.WaveSpawnCount < 3)
                        SpawnWaveThreat();
                    ls.WaveCounter = ls.WaveInterval;
                }
            }
        }

        /// <summary>
        /// Called after every correct placement. Decrements all threat counters, resolves
        /// successes/expiries, and advances the PressureWave spawner.
        /// </summary>
        // [REQ: PRESSURE-TICK-001] decrement all counters on correct placement
        // [REQ: PRESSURE-TICK-003] success when last cell placed  [PRESSURE-TICK-004] chain +1 HP
        // [REQ: PRESSURE-TICK-005] expiry 2 HP per cell           [PRESSURE-TICK-006] DoublePenalty doubles expiry
        // [REQ: PRESSURE-TICK-007] wave counter + reset + spawn   [PRESSURE-TICK-008] cap at 3 waves
        // [REQ: PRESSURE-TICK-009] WaveSpawnCount decrements on removal
        private void TickPressureThreats(int placedRow, int placedCol)
        {
            var ls = CurrentLevelState;
            if (ls == null || ls.ActiveThreats.Count == 0 && ls.WaveInterval == 0) return;

            // 1. Decrement counter on all threats
            for (var i = 0; i < ls.ActiveThreats.Count; i++)
                ls.ActiveThreats[i].RemainingPlacements--;

            // 2. Process each threat — resolve successes first, then expiries
            var toRemove = new System.Collections.Generic.List<PressureThreat>();
            foreach (var threat in ls.ActiveThreats)
            {
                threat.Cells.Remove((placedRow, placedCol));

                if (threat.Cells.Count == 0)
                {
                    // Success
                    if (threat.IsChain)
                    {
                        State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + 1);
                        // [REQ: PRESSURE-UI-005] gold-flash the cell that completed the chain
                        ls.ChainSuccessCells.Add((placedRow, placedCol));
                    }
                    if (threat.IsWaveSpawned) ls.WaveSpawnCount--;
                    toRemove.Add(threat);
                }
                else if (threat.RemainingPlacements <= 0)
                {
                    // Expiry — 2 HP per unfilled target cell (doubled if DoublePenalty active)
                    // [REQ: PRESSURE-UI-004] flash all still-unfilled cells red
                    foreach (var c in threat.Cells) ls.ExpiryFlashCells.Add(c);
                    var penaltyPer = IsDebuffActive(BossModifierId.DoublePenalty) ? 4 : 2;
                    var count = threat.Cells.Count;
                    for (var j = 0; j < count; j++) ApplyMistakePenalty(penaltyPer);
                    if (threat.IsWaveSpawned) ls.WaveSpawnCount--;
                    toRemove.Add(threat);
                }
            }
            foreach (var t in toRemove) ls.ActiveThreats.Remove(t);

            // 4. PressureWave counter
            if (ls.WaveInterval > 0)
            {
                ls.WaveCounter--;
                if (ls.WaveCounter <= 0)
                {
                    if (ls.WaveSpawnCount < 3)
                        SpawnWaveThreat();
                    ls.WaveCounter = ls.WaveInterval;
                }
            }
        }

        /// <summary>
        /// Called from ApplyMistakePenalty(row, col) to apply HauntedCell and CrumblingRegion effects.
        /// </summary>
        // [REQ: PRESSURE-TICK-010] HauntedCell extra HP on mistake  [PRESSURE-TICK-011] DoublePenalty stacking
        // [REQ: PRESSURE-TICK-012] CrumblingRegion all-cell erosion
        // [REQ: PRESSURE-TICK-013] CrumblingRegion VeryHigh first-cell-only erosion
        private void TickPressureMistake(int row, int col)
        {
            var ls = CurrentLevelState;
            if (ls == null) return;

            // HauntedCell: mistake elsewhere costs 1 extra HP while the haunted cell is unfilled
            if (ls.HauntedCell != (-1, -1) && (row, col) != ls.HauntedCell)
            {
                // [REQ: PRESSURE-TICK-011] HauntedCell stacks with DoublePenalty: extraDmg = 2 instead of 1
                var extraDmg = IsDebuffActive(BossModifierId.DoublePenalty) ? 2 : 1;
                ApplyMistakePenalty(extraDmg);
            }

            // CrumblingRegion: fragility erosion per mistake
            if (ls.CrumblingCells.Count > 0)
            {
                if (ls.CrumblingVeryHigh)
                {
                    // [REQ: PRESSURE-TICK-013] VeryHigh: only the lowest-key (first) crumbling cell erodes
                    var firstKey = int.MaxValue;
                    foreach (var k in ls.CrumblingCells.Keys)
                        if (k < firstKey) firstKey = k;
                    if (firstKey != int.MaxValue)
                    {
                        ls.CrumblingCells[firstKey]--;
                        // [REQ: PRESSURE-TICK-014] fragility reaches 0 → AutoFill triggers (VeryHigh path)
                        if (ls.CrumblingCells[firstKey] <= 0)
                            AutoFillCrumbledCell(firstKey / 100, firstKey % 100);
                    }
                }
                else
                {
                    // [REQ: PRESSURE-TICK-012] Normal/High: every crumbling cell erodes
                    var keys = new System.Collections.Generic.List<int>(ls.CrumblingCells.Keys);
                    foreach (var k in keys)
                    {
                        if (!ls.CrumblingCells.ContainsKey(k)) continue;
                        ls.CrumblingCells[k]--;
                        // [REQ: PRESSURE-TICK-014] fragility reaches 0 → AutoFill triggers (Normal/High path)
                        if (ls.CrumblingCells[k] <= 0)
                            AutoFillCrumbledCell(k / 100, k % 100);
                    }
                }
            }
        }

        /// <summary>
        /// Fills a crumbled cell with its solution value, costs 1 HP (doubled with DoublePenalty),
        /// removes it from CrumblingCells, and silently resolves any threats targeting it.
        /// Auto-fill is NOT a player placement and does NOT advance threat counters.
        /// </summary>
        // [REQ: PRESSURE-TICK-015] place solution value + HP cost
        // [REQ: PRESSURE-TICK-016] AutoFill silently resolves threats targeting the crumbled cell
        // [REQ: PRESSURE-TICK-017] no threat counter decrement on auto-fill
        // [REQ: PRESSURE-TICK-018] clears HauntedCell if it was the crumbled cell
        // [REQ: PRESSURE-EDGE-004] Haunted + Crumbling same cell: haunting cleared on auto-fill
        private void AutoFillCrumbledCell(int row, int col)
        {
            if (CurrentBoard == null || CurrentLevelState == null) return;

            var value = CurrentBoard.Solution[row, col];
            CurrentBoard.PlaceValue(row, col, value);
            CurrentLevelState.CrumblingCells.Remove(row * 100 + col);
            // [REQ: PRESSURE-UI-008] mark this cell for auto-fill green-flash
            CurrentLevelState.CrumbledFlashCells.Add((row, col));

            var crumbleDmg = IsDebuffActive(BossModifierId.DoublePenalty) ? 2 : 1;
            ApplyMistakePenalty(crumbleDmg);

            // Silently resolve threats that included this cell (no HP reward)
            var ls = CurrentLevelState;
            for (var i = ls.ActiveThreats.Count - 1; i >= 0; i--)
            {
                var threat = ls.ActiveThreats[i];
                threat.Cells.Remove((row, col));
                if (threat.Cells.Count == 0)
                {
                    if (threat.IsWaveSpawned) ls.WaveSpawnCount--;
                    ls.ActiveThreats.RemoveAt(i);
                }
            }

            if (ls.HauntedCell == (row, col))
                ls.HauntedCell = (-1, -1);
        }

        /// <summary>
        /// Spawns a new single-cell wave threat on a random empty cell not already targeted.
        /// Does nothing if all empty cells are already threatened.
        /// </summary>
        // [REQ: PRESSURE-TICK-007] wave spawn logic  [PRESSURE-TICK-008] cap enforced by caller
        // [REQ: PRESSURE-EDGE-003] skip when no unthreatened cells available
        private void SpawnWaveThreat()
        {
            var ls = CurrentLevelState;
            if (ls == null || CurrentBoard == null) return;

            var occupied = new HashSet<(int, int)>();
            foreach (var t in ls.ActiveThreats)
                foreach (var c in t.Cells)
                    occupied.Add(c);

            var available = new System.Collections.Generic.List<(int row, int col)>();
            for (var r = 0; r < CurrentBoard.Size; r++)
            for (var c = 0; c < CurrentBoard.Size; c++)
            {
                if (CurrentBoard.Cells[r, c] != 0) continue;
                if (CurrentBoard.GivenMask[r, c]) continue;
                if (ls.PresolvedCells.Contains((r, c))) continue;
                if (occupied.Contains((r, c))) continue;
                available.Add((r, c));
            }
            if (available.Count == 0) return;

            var rng = new System.Random((int)(CurrentLevelConfig.Seed ^ (long)ls.CorrectPlacements * 0x9E3779B9L));
            var cell = available[rng.Next(available.Count)];

            // [REQ: PRESSURE-UI-009] trigger wave ripple animation on next render
            ls.WaveRipplePending = true;
            ls.ActiveThreats.Add(new PressureThreat
            {
                Scope               = ThreatScope.SingleCell,
                Cells               = new System.Collections.Generic.List<(int row, int col)> { cell },
                RemainingPlacements = ls.WaveThreatCounter,
                InitialPlacements   = ls.WaveThreatCounter,
                IsWaveSpawned       = true
            });
            ls.WaveSpawnCount++;
        }

        // ── Pressure cell-selection helpers ──────────────────────────────────

        private static ThreatScope PickCountdownScope(BossModifierIntensity intensity, System.Random rng)
        {
            ThreatScope[] scopes;
            switch (intensity)
            {
                case BossModifierIntensity.Low:
                    scopes = new[] { ThreatScope.SingleCell, ThreatScope.Box }; break;
                case BossModifierIntensity.Medium:
                    scopes = new[] { ThreatScope.Box, ThreatScope.Row, ThreatScope.Column }; break;
                case BossModifierIntensity.High:
                    scopes = new[] { ThreatScope.Row, ThreatScope.Column, ThreatScope.Chain }; break;
                default: // VeryHigh
                    scopes = new[] { ThreatScope.Row, ThreatScope.Column, ThreatScope.Chain }; break;
            }
            return scopes[rng.Next(scopes.Length)];
        }

        // [REQ: PRESSURE-INIT-006] PresolvedCells excluded from threat cell candidates
        // [REQ: PRESSURE-EDGE-001] Chain scope: if chain grows to <3 cells, falls back to SingleCell
        private System.Collections.Generic.List<(int row, int col)> SelectThreatCells(
            SudokuBoard board, ThreatScope scope, System.Random rng)
        {
            var size      = board.Size;
            var emptyCells = new System.Collections.Generic.List<(int row, int col)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (board.Cells[r, c] != 0) continue;
                if (board.GivenMask[r, c]) continue;
                if (CurrentLevelState != null && CurrentLevelState.PresolvedCells.Contains((r, c))) continue;
                emptyCells.Add((r, c));
            }
            if (emptyCells.Count == 0)
                return new System.Collections.Generic.List<(int, int)>();

            switch (scope)
            {
                case ThreatScope.SingleCell:
                {
                    var sorted   = SortByCandidateCount(board, emptyCells);
                    var halfStart = sorted.Count / 2;
                    return new System.Collections.Generic.List<(int, int)>
                        { sorted[rng.Next(halfStart, sorted.Count)] };
                }
                case ThreatScope.Box:
                {
                    var byRegion = new System.Collections.Generic.Dictionary<int,
                        System.Collections.Generic.List<(int, int)>>();
                    foreach (var (r, c) in emptyCells)
                    {
                        var rid = board.RegionMap[r, c];
                        if (!byRegion.ContainsKey(rid))
                            byRegion[rid] = new System.Collections.Generic.List<(int, int)>();
                        byRegion[rid].Add((r, c));
                    }
                    var keys = new System.Collections.Generic.List<int>(byRegion.Keys);
                    if (keys.Count == 0)
                        return new System.Collections.Generic.List<(int, int)>();
                    return byRegion[keys[rng.Next(keys.Count)]];
                }
                case ThreatScope.Row:
                {
                    var byRow = new System.Collections.Generic.Dictionary<int,
                        System.Collections.Generic.List<(int, int)>>();
                    foreach (var (r, c) in emptyCells)
                    {
                        if (!byRow.ContainsKey(r))
                            byRow[r] = new System.Collections.Generic.List<(int, int)>();
                        byRow[r].Add((r, c));
                    }
                    var keys = new System.Collections.Generic.List<int>(byRow.Keys);
                    if (keys.Count == 0)
                        return new System.Collections.Generic.List<(int, int)>();
                    return byRow[keys[rng.Next(keys.Count)]];
                }
                case ThreatScope.Column:
                {
                    var byCol = new System.Collections.Generic.Dictionary<int,
                        System.Collections.Generic.List<(int, int)>>();
                    foreach (var (r, c) in emptyCells)
                    {
                        if (!byCol.ContainsKey(c))
                            byCol[c] = new System.Collections.Generic.List<(int, int)>();
                        byCol[c].Add((r, c));
                    }
                    var keys = new System.Collections.Generic.List<int>(byCol.Keys);
                    if (keys.Count == 0)
                        return new System.Collections.Generic.List<(int, int)>();
                    return byCol[keys[rng.Next(keys.Count)]];
                }
                case ThreatScope.Chain:
                {
                    if (emptyCells.Count < 3)
                    {
                        // Fall back to SingleCell
                        var sorted2 = SortByCandidateCount(board, emptyCells);
                        return new System.Collections.Generic.List<(int, int)>
                            { sorted2[rng.Next(sorted2.Count)] };
                    }
                    var seed  = emptyCells[rng.Next(emptyCells.Count)];
                    var chain = new System.Collections.Generic.List<(int, int)> { seed };
                    var rest  = new System.Collections.Generic.List<(int, int)>(emptyCells);
                    rest.Remove(seed);
                    while (chain.Count < 4 && rest.Count > 0)
                    {
                        var connected = new System.Collections.Generic.List<(int, int)>();
                        foreach (var cell in rest)
                        foreach (var ch in chain)
                        {
                            if (cell.Item1 == ch.Item1 || cell.Item2 == ch.Item2 ||
                                board.RegionMap[cell.Item1, cell.Item2] == board.RegionMap[ch.Item1, ch.Item2])
                            { connected.Add(cell); break; }
                        }
                        if (connected.Count == 0) break;
                        var next = connected[rng.Next(connected.Count)];
                        chain.Add(next);
                        rest.Remove(next);
                    }
                    if (chain.Count < 3)
                        return new System.Collections.Generic.List<(int, int)> { seed };
                    return chain;
                }
                default:
                    return new System.Collections.Generic.List<(int, int)>();
            }
        }

        private (int row, int col) SelectHauntedCell(
            SudokuBoard board, BossModifierIntensity intensity, System.Random rng)
        {
            var emptyCells = new System.Collections.Generic.List<(int row, int col)>();
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                if (board.Cells[r, c] != 0) continue;
                if (board.GivenMask[r, c]) continue;
                if (CurrentLevelState != null && CurrentLevelState.PresolvedCells.Contains((r, c))) continue;
                emptyCells.Add((r, c));
            }
            if (emptyCells.Count == 0) return (-1, -1);

            var sorted = SortByCandidateCount(board, emptyCells);
            switch (intensity)
            {
                case BossModifierIntensity.Low:
                {
                    var start = sorted.Count * 3 / 4; // last quartile = easiest
                    return sorted[rng.Next(start, sorted.Count)];
                }
                case BossModifierIntensity.Medium:
                {
                    var start = sorted.Count / 2;
                    return sorted[rng.Next(start, sorted.Count)];
                }
                case BossModifierIntensity.High:
                    return sorted[rng.Next(Math.Min(2, sorted.Count))];
                default: // VeryHigh
                    return sorted[0]; // fewest candidates = hardest
            }
        }

        private (int regionType, int regionIndex) SelectCrumblingRegion(
            SudokuBoard board, System.Random rng)
        {
            var size       = board.Size;
            var candidates = new System.Collections.Generic.List<(int t, int idx, int cnt)>();

            // Boxes
            var regionIds = new HashSet<int>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                regionIds.Add(board.RegionMap[r, c]);
            foreach (var rid in regionIds)
            {
                var cnt = 0;
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    if (board.RegionMap[r, c] == rid && board.Cells[r, c] == 0 && !board.GivenMask[r, c])
                        cnt++;
                if (cnt >= 2) candidates.Add((0, rid, cnt));
            }
            // Rows
            for (var r = 0; r < size; r++)
            {
                var cnt = 0;
                for (var c = 0; c < size; c++)
                    if (board.Cells[r, c] == 0 && !board.GivenMask[r, c]) cnt++;
                if (cnt >= 2) candidates.Add((1, r, cnt));
            }
            // Columns
            for (var c = 0; c < size; c++)
            {
                var cnt = 0;
                for (var r = 0; r < size; r++)
                    if (board.Cells[r, c] == 0 && !board.GivenMask[r, c]) cnt++;
                if (cnt >= 2) candidates.Add((2, c, cnt));
            }

            if (candidates.Count == 0) return (-1, -1);

            // Prefer median empty-cell count
            candidates.Sort((a, b) => a.cnt.CompareTo(b.cnt));
            var med = candidates[candidates.Count / 2];
            return (med.t, med.idx);
        }

        private System.Collections.Generic.List<(int row, int col)> GetRegionEmptyCells(
            SudokuBoard board, int regionType, int regionIndex)
        {
            var size  = board.Size;
            var cells = new System.Collections.Generic.List<(int, int)>();
            switch (regionType)
            {
                case 0: // box
                    for (var r = 0; r < size; r++)
                    for (var c = 0; c < size; c++)
                        if (board.RegionMap[r, c] == regionIndex &&
                            board.Cells[r, c] == 0 && !board.GivenMask[r, c])
                            cells.Add((r, c));
                    break;
                case 1: // row
                    for (var c = 0; c < size; c++)
                        if (board.Cells[regionIndex, c] == 0 && !board.GivenMask[regionIndex, c])
                            cells.Add((regionIndex, c));
                    break;
                case 2: // column
                    for (var r = 0; r < size; r++)
                        if (board.Cells[r, regionIndex] == 0 && !board.GivenMask[r, regionIndex])
                            cells.Add((r, regionIndex));
                    break;
            }
            return cells;
        }

        private System.Collections.Generic.List<(int row, int col)> SortByCandidateCount(
            SudokuBoard board, System.Collections.Generic.List<(int row, int col)> cells)
        {
            var pairs = new System.Collections.Generic.List<((int, int) cell, int count)>();
            foreach (var cell in cells)
                pairs.Add((cell, ComputeCandidateCount(board, cell.row, cell.col)));
            pairs.Sort((a, b) => a.count.CompareTo(b.count));
            var result = new System.Collections.Generic.List<(int, int)>(pairs.Count);
            foreach (var (c, _) in pairs) result.Add(c);
            return result;
        }

        private int ComputeCandidateCount(SudokuBoard board, int row, int col)
        {
            var size    = board.Size;
            var used    = 0;
            for (var r = 0; r < size; r++) if (board.Cells[r, col] > 0) used |= 1 << board.Cells[r, col];
            for (var c = 0; c < size; c++) if (board.Cells[row, c] > 0) used |= 1 << board.Cells[row, c];
            var regionId = board.RegionMap[row, col];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (board.RegionMap[r, c] == regionId && board.Cells[r, c] > 0)
                    used |= 1 << board.Cells[r, c];
            var count = 0;
            for (var v = 1; v <= size; v++)
                if ((used & (1 << v)) == 0) count++;
            return count;
        }

        private static int CounterForScope(ThreatScope scope, int cellCount,
            BossModifierIntensity intensity)
        {
            var mult = intensity switch
            {
                BossModifierIntensity.Low    => 2.5f,
                BossModifierIntensity.Medium => 2.0f,
                BossModifierIntensity.High   => 1.5f,
                _                            => 1.2f
            };
            return Math.Max(cellCount + 1, (int)(cellCount * mult));
        }

        private static int FragilityForIntensity(BossModifierIntensity intensity) => intensity switch
        {
            BossModifierIntensity.Low    => 4,
            BossModifierIntensity.Medium => 3,
            BossModifierIntensity.High   => 2,
            _                            => 2  // VeryHigh capped at 2 per spec
        };

        private static int WaveIntervalForIntensity(BossModifierIntensity intensity) => intensity switch
        {
            BossModifierIntensity.Low    => 8,
            BossModifierIntensity.Medium => 6,
            BossModifierIntensity.High   => 4,
            _                            => 3
        };

        private static int PerThreatCounterForIntensity(BossModifierIntensity intensity) => intensity switch
        {
            BossModifierIntensity.Low    => 6,
            BossModifierIntensity.Medium => 5,
            BossModifierIntensity.High   => 4,
            _                            => 3
        };

        public bool IsPlayerDead => State.CurrentHP <= 0;

        public bool IsLevelComplete => CurrentBoard != null && CurrentBoard.IsComplete();

        // ── Rewards ──

        // [REQ: ECON-REWARD-001] Gold awarded on puzzle completion via gold formula
        public TileXpEntry CompleteLevelAndGrantRewards()
        {
            var config = CurrentLevelConfig;
            var level = CurrentLevelState;

            // Reset per-puzzle item effects so they don't persist into the next puzzle.
            // PledgeActive is intentionally omitted — the pledge reward check below reads it first.
            State.MistakeShieldCharges = 0;
            State.FogDisabledMoves     = 0;
            State.MonksBeadsCountdown  = 0;
            State.WornChiselActive     = false;
            State.TempleSealActive     = false;
            if (level?.FogPendingCells != null)
                level.FogPendingCells.Clear();

            // Special modes without progression rewards: no gold/XP/item flow, but still log the board.
            if (State.IsSeasonalChallenge || State.DisableProgressionRewards)
            {
                var emptyEntry = new TileXpEntry { BoardSize = config.BoardSize, Stars = config.Stars };
                TileXpLog.Add(emptyEntry);
                return emptyEntry;
            }

            var goldBase = GoldTable.CalculatePuzzleGold(config.BoardSize, config.Stars);

            // Apply cursed bonus
            if (config.IsCursed)
                goldBase = (int)(goldBase * config.CursedGoldMult);

            // Apply positive floor effect
            if (State.HasPositiveFloorEffect)
            {
                switch (State.ActivePositiveFloorEffect)
                {
                    case PositiveFloorEffect.Bounty:
                        goldBase = (int)(goldBase * 1.4f);
                        break;
                    case PositiveFloorEffect.PencilRefill:
                        State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + 2);
                        break;
                    case PositiveFloorEffect.HealingPath:
                        if (level.Mistakes == 0)
                            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + 1);
                        break;
                }
            }

            // Relic: puzzle-complete gold adjustments (CopperTortoise, TransmutedSigil, SakuraSeal)
            RelicService.OnPuzzleComplete(State, level, ref goldBase);

            // GoldenKoi: +10g per puzzle star rating, then consumed.
            if (State.GoldenKoiActive)
            {
                goldBase += config.Stars * 10;
                State.GoldenKoiActive = false;
            }

            State.CurrentGold += goldBase;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                State.GoldCollectedThisRun += goldBase;
                if (State.GoldCollectedThisRun >= 150)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerCollected150Gold);
            }

            var xpEntry = XpService.CalculateTile(
                config.BoardSize, config.Stars,
                config.ActiveModifiers.Count, config.IsBoss,
                level.PerfectSoFar);

            // Apply cursed XP bonus
            if (config.IsCursed)
                xpEntry.TotalXp = (int)(xpEntry.TotalXp * config.CursedXpMult);

            if (level.PerfectSoFar) _analytics?.RecordPerfectPuzzle();

            // ReedPledge: success = +4 Pencil, +30 Gold, +25 bonus XP
            if (State.PledgeActive)
            {
                if (level.NoPencilUsed)
                {
                    State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + 4);
                    State.CurrentGold += 30;
                    if (TileXpLog.Count > 0)
                        TileXpLog[TileXpLog.Count - 1].TotalXp += 25;
                }
                State.PledgeActive = false;
            }

            // Class passive level-complete hook (e.g. ReedDuelist pencil bonus)
            ClassPassiveService.OnLevelComplete(State, level);

            // Curse: restless_inventory drain + auto-cleanse on perfect
            _curseService.OnPuzzleComplete(State, level.PerfectSoFar && level.Mistakes == 0);

            TileXpLog.Add(xpEntry);
            _analytics?.RecordPuzzleSolved(level.Mistakes, goldBase);
            _analytics?.RecordTileXp(xpEntry);

            if (config.IsBoss && !State.IsSeasonalChallenge)
                State.BossesDefeatedThisRun++;

            // Daily goals: puzzle-complete triggers
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerPuzzleComplete);
                if (level.Mistakes == 0)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerNoMistakePuzzle);
                if (level.NoPencilUsed)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerNoPencilPuzzle);
                if (config.IsBoss)
                {
                    DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerBossDefeated);
                    if (level.Mistakes == 0)
                        DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerNoMistakeBoss);
                    if (State.CurrentFloor == 2) // floor index 2 = floor 3
                        DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerBossFloor3);
                    if (State.CurrentFloor == 4) // floor index 4 = floor 5 (final boss = run won)
                        DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerRunWon);
                    if (State.CurrentHP >= State.HpAtCurrentFloorEntry)
                        DailyGoalService.EvaluateInRun(DailyGoals, State, level, DailyGoalService.TriggerFullHpFloor);
                }
            }

            return xpEntry;
        }

        // ── Item Reward Rolling ──

        public List<ItemInstance> BuildItemRewardSlots()
        {
            var bonusSlots = RelicService.GetBonusRewardSlots(State);
            if (State.HasPositiveFloorEffect && State.ActivePositiveFloorEffect == PositiveFloorEffect.LuckyItems)
                bonusSlots++;
            bonusSlots += CurseService.GetSlotPenalty(State); // [REQ: CURSE-INT-003] misfortune curse: -1 slot (value is negative)
            var classLevel = GetCurrentClassLevel();
            var stars = CurrentLevelConfig.Stars;

            RolledItemSlots = _itemService.RollSlots(stars, classLevel, bonusSlots,
                State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId);

            // [REQ: CURSE-INT-008] bad_luck curse: +15% extra chance each slot becomes Nothing
            var nothingBonus = CurseService.GetNothingChanceBonus(State);
            if (nothingBonus > 0)
            {
                var rng = new System.Random(State.Seed + State.Depth * 7 + State.CurrentFloor * 31);
                for (var i = 0; i < RolledItemSlots.Count; i++)
                    if (RolledItemSlots[i] != null && rng.NextDouble() < nothingBonus)
                        RolledItemSlots[i] = null;
            }

            // double_or_nothing curse: each slot has 20% chance to be rerolled to Nothing
            if (CurseService.IsActive(State, "double_or_nothing"))
            {
                for (var i = 0; i < RolledItemSlots.Count; i++)
                {
                    if (RolledItemSlots[i] != null && _curseService.RollDoubleOrNothing(State))
                        RolledItemSlots[i] = null;
                }
            }

            // LoadedCoin: force-fill any Nothing slots, then consume the item
            if (State.HeldItems.Exists(it => it?.Type == ItemType.LoadedCoin))
            {
                for (var i = 0; i < RolledItemSlots.Count; i++)
                    if (RolledItemSlots[i] == null)
                        RolledItemSlots[i] = _itemService.RollOneItem(stars, classLevel,
                            State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId);
                State.HeldItems.RemoveAll(it => it?.Type == ItemType.LoadedCoin);
            }

            return RolledItemSlots;
        }

        public void PickRewardItem(int slotIndex)
        {
            if (RolledItemSlots == null || slotIndex < 0 || slotIndex >= RolledItemSlots.Count) return;
            var item = RolledItemSlots[slotIndex];
            if (item == null) return;

            AddItemToInventory(item);
            RolledItemSlots = null;
        }

        public void AddItemToInventory(ItemInstance item)
        {
            if (State.HeldItems.Count < State.ItemSlots)
            {
                State.HeldItems.Add(item);
                if (DailyGoals != null && !State.IsSeasonalChallenge)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerItemCollected);
            }
        }

        public bool IsBagFull() => State != null && State.HeldItems.Count >= State.ItemSlots;

        /// <summary>Replaces the item at the given bag slot with newItem; clears any pending rolled reward.</summary>
        public void ReplaceItemInInventory(int slotIndex, ItemInstance newItem)
        {
            if (slotIndex < 0 || slotIndex >= State.HeldItems.Count) return;
            State.HeldItems[slotIndex] = newItem;
            RolledItemSlots = null;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
                DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerItemCollected);
        }

        // ── Item Usage ──

        public bool TryUseItem(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= State.HeldItems.Count) return false;

            var item = State.HeldItems[inventoryIndex];
            if (item == null) return false;

            // [REQ: DEBUFF-DEF-015] RevealCost: item use is blocked while the counter is active
            if (CurrentLevelState?.ItemUseBlockedMoves > 0) return false;

            var free = ClassPassiveService.IsItemUseFree(State, CurrentLevelState)
                || RelicService.HasInfiniteItems(State)
                || RelicService.HasFirstItemFreeRelic(State, CurrentLevelState);

            switch (item.Type)
            {
                case ItemType.GoldenKoi:
                    State.GoldenKoiActive = true;
                    break;
                case ItemType.TempleStamp:
                    State.TempleSealActive = true;
                    break;
            }

            if (CurrentLevelState != null)
                CurrentLevelState.ItemsUsedThisLevel++;
            State.ItemsUsedThisRun++;
            _analytics?.RecordItemUsed();
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                if (State.ItemsUsedThisRun >= 2)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerUsed2Items);
            }

            if (!free)
            {
                item.Charges--;
                // LoadBearingStone: 30% chance to retain 1 charge on exhaustion
                if (item.Charges <= 0 && RelicService.HasRelicOfType(State, RelicId.LoadBearingStone))
                {
                    var retain = new System.Random(State.Seed ^ State.Depth ^ item.Id.GetHashCode()).NextDouble();
                    if (retain < 0.30) item.Charges = 1;
                }
                if (item.Charges <= 0)
                    State.HeldItems.RemoveAt(inventoryIndex);
            }

            return true;
        }

        /// <summary>
        /// Applies an item's effect to the current board state.
        /// row/col are the player's selected cell (-1 if none selected).
        /// Returns an ItemEffectResult describing what happened (for UI rendering).
        /// </summary>
        public ItemEffectResult ApplyItemEffect(ItemInstance item, int row = -1, int col = -1,
            int row2 = -1, int col2 = -1)
        {
            if (item == null || CurrentBoard == null) return ItemEffectResult.None;
            var size = CurrentBoard.Size;

            switch (item.Type)
            {
                case ItemType.InkWell:
                {
                    var gain = ItemService.GetInkWellAmount(item.Rarity);
                    State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + gain);
                    return ItemEffectResult.Message($"+{gain} pencil marks restored.");
                }

                case ItemType.MeditationStone:
                {
                    var gain = ItemService.GetMeditationStoneAmount(item.Rarity);
                    State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + gain);
                    return ItemEffectResult.Message($"+{gain} HP restored.");
                }

                case ItemType.OfferingBowl:
                {
                    if (State.CurrentHP <= 1) return ItemEffectResult.Message("Need at least 2 HP.");
                    State.CurrentHP--;
                    State.CurrentGold += 30;
                    return ItemEffectResult.Message("Sacrificed 1 HP for 30 gold.");
                }

                case ItemType.RicePaperUmbrella:
                {
                    State.MistakeShieldCharges += 1;
                    return ItemEffectResult.Message("Next mistake will be absorbed.");
                }

                case ItemType.LanternOfClarity:
                {
                    var moves = ItemService.GetLanternOfClarityMoves(item.Rarity);
                    State.FogDisabledMoves += moves;
                    return ItemEffectResult.Message($"Fog disabled for {moves} moves.");
                }

                case ItemType.WindChime:
                {
                    // Undo last mistake — full logic handled in InRunController via _lastMistakeRow.
                    // RunDirector tracks no UI-layer state; this path is a no-op placeholder.
                    return ItemEffectResult.Message("Wind Chime: undo last mistake.");
                }

                case ItemType.GardenRake:
                {
                    if (row < 0) return ItemEffectResult.Message("Select a cell first.");
                    for (var c = 0; c < size; c++) CurrentBoard.ClearPencilMarks(row, c);
                    for (var r = 0; r < size; r++) CurrentBoard.ClearPencilMarks(r, col);
                    return ItemEffectResult.BoardChanged("Row and column pencil marks cleared.");
                }

                case ItemType.Solver:
                {
                    if (row < 0) return ItemEffectResult.Message("Select a cell first.");
                    var filled = new List<(int, int)>();
                    FillCellIfEmpty(row, col, filled);
                    var neighbors = ItemService.GetSolverNeighborCount(item.Rarity);
                    if (neighbors > 0)
                    {
                        TryFillNeighbors(row, col, size, neighbors, filled);
                    }
                    return ItemEffectResult.CellsFilled(filled, $"Filled {filled.Count} cell(s).");
                }

                case ItemType.TempleIncense:
                {
                    if (row < 0) return ItemEffectResult.Message("Select a cell first.");
                    if (CurrentBoard.Cells[row, col] != 0)
                        return ItemEffectResult.Message("Cell is already filled.");
                    var correctVal = CurrentBoard.Solution[row, col];
                    if (correctVal <= 0) return ItemEffectResult.Message("No unique solution for this cell.");
                    CurrentBoard.PlaceValue(row, col, correctVal);
                    CurrentLevelState.CorrectPlacements++;
                    return ItemEffectResult.CellsFilled(new List<(int,int)> { (row, col) }, "Correct candidate placed.");
                }

                case ItemType.KoiDragonScale:
                {
                    var (bestAxis, bestIdx, bestCount) = FindMostFilledAxis(size);
                    if (bestIdx < 0) return ItemEffectResult.Message("No incomplete lines.");
                    var filled2 = new List<(int, int)>();
                    FillAxisCells(bestAxis, bestIdx, size, filled2);
                    return ItemEffectResult.CellsFilled(filled2, $"Completed {bestAxis} {bestIdx + 1}.");
                }

                case ItemType.KoiReflection:
                {
                    if (row < 0) return ItemEffectResult.Message("Select a cell first.");
                    var cells = ItemService.GetKoiReflectionCells(item.Rarity);
                    var hints = BuildKoiReflectionHints(row, col, size, cells);
                    return ItemEffectResult.Hints(hints, "Candidates revealed.");
                }

                case ItemType.PruningShears:
                {
                    if (row < 0) return ItemEffectResult.Message("Select a cell first.");
                    var correct = CurrentBoard.Solution[row, col];
                    var marks = CurrentBoard.GetPencilMarks(row, col);
                    var toRemove = -1;
                    foreach (var m in marks) { if (m != correct) { toRemove = m; break; } }
                    if (toRemove < 0) return ItemEffectResult.Message("No incorrect candidates to prune.");
                    CurrentBoard.RemovePencilMark(row, col, toRemove);
                    return ItemEffectResult.BoardChanged("Incorrect candidate removed.");
                }

                case ItemType.GinkgoLeaf:
                {
                    var undone = TryUndoLastMistakePlacement();
                    var hpRestored = 0;
                    if (undone)
                    {
                        hpRestored = Math.Max(0, State.LastMistakeHpLost);
                        if (hpRestored > 0)
                        {
                            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + hpRestored);
                            if (CurrentLevelState != null)
                                CurrentLevelState.HpLost = Math.Max(0, CurrentLevelState.HpLost - hpRestored);
                        }
                        if (CurrentLevelState != null && CurrentLevelState.Mistakes > 0)
                        {
                            CurrentLevelState.Mistakes--;
                            if (CurrentLevelState.Mistakes == 0)
                                CurrentLevelState.PerfectSoFar = true;
                        }
                        if (State.LastComboBeforeMistake > 0)
                            State.ComboStreak = State.LastComboBeforeMistake;
                        State.LastMistakeHpLost = 0;
                        State.LastComboBeforeMistake = 0;
                        State.LastMistakeRow = -1;
                        State.LastMistakeCol = -1;
                    }
                    return undone
                        ? ItemEffectResult.BoardChanged(LocalizationService.Format(
                            "Item.Effect.GinkgoLeaf.Restored",
                            "Ginkgo Leaf: mistake undone, +{0} HP, combo restored.",
                            hpRestored))
                        : ItemEffectResult.Message(LocalizationService.T(
                            "Item.Effect.GinkgoLeaf.NothingToUndo",
                            "Nothing to undo."));
                }

                case ItemType.SilkFan:
                {
                    if (row < 0 || row2 < 0) return ItemEffectResult.Message("Select two cells first.");
                    if (CurrentBoard.IsGiven(row, col) || CurrentBoard.IsGiven(row2, col2))
                        return ItemEffectResult.Message("Cannot swap given cells.");
                    var v1 = CurrentBoard.Cells[row, col];
                    var v2 = CurrentBoard.Cells[row2, col2];
                    CurrentBoard.PlaceValue(row, col, v2);
                    CurrentBoard.PlaceValue(row2, col2, v1);
                    return ItemEffectResult.BoardChanged("Cells swapped.");
                }

                // UI-hint items — return highlight data, no state change
                case ItemType.Finder:
                {
                    if (row < 0) return ItemEffectResult.Message("Select a cell to match.");
                    var digit = CurrentBoard.Cells[row, col];
                    if (digit == 0) return ItemEffectResult.Message("Selected cell is empty.");
                    var matches = FindMatchingCells(digit, size, ItemService.GetFinderHighlightCount(item.Rarity));
                    return ItemEffectResult.Highlights(matches, $"Highlighted {matches.Count} matching cell(s).");
                }

                case ItemType.PatternScroll:
                {
                    var zones = FindConflictZones(size, ItemService.GetPatternScrollZones(item.Rarity));
                    return ItemEffectResult.Highlights(zones,
                        $"Highlighted {zones.Count} conflict zone(s).");
                }

                case ItemType.ZenSandSifter:
                {
                    var twins = FindTwinCandidateCells(size);
                    return ItemEffectResult.Highlights(twins, $"Found {twins.Count} twin-candidate cell(s).");
                }

                case ItemType.GoldenKintsugiJar:
                {
                    var mistakes = FindMistakeCells(size);
                    return ItemEffectResult.Highlights(mistakes, $"Found {mistakes.Count} mistake(s).");
                }

                // ── Class-exclusive items (L15) ──

                case ItemType.LoadedCoin:
                    return ItemEffectResult.Message("Next item reward screen: all Nothing slots replaced with real items.");

                case ItemType.MonksBeads:
                {
                    State.MonksBeadsCountdown = 5;
                    return ItemEffectResult.Message("Next 5 correct placements each restore 1 HP.");
                }

                case ItemType.AnnotatedFolio:
                {
                    var markedCount = 0;
                    for (var r = 0; r < size; r++)
                    for (var c = 0; c < size; c++)
                    {
                        if (CurrentBoard.Cells[r, c] != 0 || CurrentBoard.IsGiven(r, c)) continue;
                        var cands = GetValidCandidates(r, c);
                        if (cands.Count == 2)
                        {
                            CurrentBoard.AddPencilMark(r, c, cands[0]);
                            CurrentBoard.AddPencilMark(r, c, cands[1]);
                            markedCount++;
                        }
                    }
                    return ItemEffectResult.Message($"Auto-marked {markedCount} cell(s) with 2 candidates.");
                }

                case ItemType.DoubleOrQuits:
                {
                    var rng = new System.Random(State.Seed + State.Depth * 13 + State.CurrentGold);
                    if (rng.NextDouble() < 0.5)
                    {
                        State.CurrentGold *= 2;
                        return ItemEffectResult.Message($"Lucky! Gold doubled → {State.CurrentGold}g.");
                    }
                    State.CurrentGold /= 2;
                    return ItemEffectResult.Message($"Unlucky. Gold halved → {State.CurrentGold}g.");
                }

                case ItemType.WornChisel:
                {
                    State.WornChiselActive = true;
                    return ItemEffectResult.Message("30% shop discount active this floor.");
                }

                case ItemType.DimLantern:
                {
                    State.DimLanternUsed = true;
                    return ItemEffectResult.Message("Boss modifier list revealed on the map.");
                }

                case ItemType.ReedPledge:
                {
                    State.PledgeActive = true;
                    return ItemEffectResult.Message("Pledge active: no pencil marks → +4 Pencil, +30 Gold, +25 XP. Any pencil use: -1 HP.");
                }

                case ItemType.SurveyNotes:
                {
                    State.AllNodesRevealed = true;
                    return ItemEffectResult.Message("All floor nodes revealed.");
                }

                default:
                    return ItemEffectResult.None;
            }
        }

        // ── Item effect helpers ──────────────────────────────────────────────────

        private List<int> GetValidCandidates(int row, int col)
        {
            var size = CurrentBoard.Size;
            var result = new List<int>();
            for (var v = 1; v <= size; v++)
                if (ConstraintEngine.ValidateAll(CurrentBoard, row, col, v, CurrentOverlay))
                    result.Add(v);
            return result;
        }

        private void RevealBoxCandidates(int row, int col, List<(int, int)> revealed)
        {
            var size = CurrentBoard.Size;
            var boxSize = (int)Math.Round(Math.Sqrt(size));
            var br = (row / boxSize) * boxSize;
            var bc = (col / boxSize) * boxSize;
            for (var r = br; r < br + boxSize && r < size; r++)
            for (var c = bc; c < bc + boxSize && c < size; c++)
            {
                if (CurrentBoard.Cells[r, c] != 0 || CurrentBoard.IsGiven(r, c)) continue;
                var sol = CurrentBoard.Solution[r, c];
                if (sol > 0) { CurrentBoard.AddPencilMark(r, c, sol); revealed.Add((r, c)); }
            }
        }

        private void FillCellIfEmpty(int row, int col, List<(int, int)> filled)
        {
            if (CurrentBoard.Cells[row, col] != 0 || CurrentBoard.IsGiven(row, col)) return;
            var v = CurrentBoard.Solution[row, col];
            if (v <= 0) return;
            CurrentBoard.PlaceValue(row, col, v);
            CurrentLevelState.CorrectPlacements++;
            filled.Add((row, col));
        }

        private void TryFillNeighbors(int row, int col, int size, int count, List<(int, int)> filled)
        {
            int[][] dirs = { new[]{-1,0}, new[]{1,0}, new[]{0,-1}, new[]{0,1} };
            var done = 0;
            foreach (var d in dirs)
            {
                if (done >= count) break;
                var nr = row + d[0]; var nc = col + d[1];
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                if (CurrentBoard.Cells[nr, nc] != 0) continue;
                FillCellIfEmpty(nr, nc, filled);
                done++;
            }
        }

        private (string axis, int idx, int count) FindMostFilledAxis(int size)
        {
            var best = (-1, -1, -1);
            string bestAxis = "row";
            // rows
            for (var r = 0; r < size; r++)
            {
                var filled = 0; var empty = 0;
                for (var c = 0; c < size; c++) { if (CurrentBoard.Cells[r, c] != 0) filled++; else empty++; }
                if (empty > 0 && filled > best.Item3) { best = (0, r, filled); bestAxis = "row"; }
            }
            // cols
            for (var c = 0; c < size; c++)
            {
                var filled = 0; var empty = 0;
                for (var r = 0; r < size; r++) { if (CurrentBoard.Cells[r, c] != 0) filled++; else empty++; }
                if (empty > 0 && filled > best.Item3) { best = (1, c, filled); bestAxis = "col"; }
            }
            return (bestAxis, best.Item2, best.Item3);
        }

        private void FillAxisCells(string axis, int idx, int size, List<(int, int)> filled)
        {
            for (var i = 0; i < size; i++)
            {
                var r = axis == "row" ? idx : i;
                var c = axis == "row" ? i : idx;
                FillCellIfEmpty(r, c, filled);
            }
        }

        private List<(int, int)> BuildKoiReflectionHints(int row, int col, int size, int cellCount)
        {
            var result = new List<(int, int)>();
            var correct = CurrentBoard.Solution[row, col];
            if (correct <= 0) return result;
            var added = 0;
            for (var r = 0; r < size && added < cellCount; r++)
            for (var c = 0; c < size && added < cellCount; c++)
            {
                if (CurrentBoard.Cells[r, c] != 0) continue;
                var sol = CurrentBoard.Solution[r, c];
                if (sol <= 0) continue;
                CurrentBoard.AddPencilMark(r, c, sol);
                result.Add((r, c));
                added++;
            }
            return result;
        }

        private bool TryUndoLastPlacement()
        {
            if (CurrentBoard == null) return false;
            var size = CurrentBoard.Size;
            // Scan board for a placed non-given cell we can clear (last column, last row order — rough heuristic)
            for (var r = size - 1; r >= 0; r--)
            for (var c = size - 1; c >= 0; c--)
            {
                if (!CurrentBoard.IsGiven(r, c) && CurrentBoard.Cells[r, c] != 0)
                {
                    CurrentBoard.ClearValue(r, c);
                    return true;
                }
            }
            return false;
        }

        private bool TryUndoLastMistakePlacement()
        {
            if (CurrentBoard == null || State == null) return false;
            var row = State.LastMistakeRow;
            var col = State.LastMistakeCol;
            if (row < 0 || col < 0 || row >= CurrentBoard.Size || col >= CurrentBoard.Size)
                return false;
            if (CurrentBoard.IsGiven(row, col) || CurrentBoard.Cells[row, col] == 0)
                return false;
            if (CurrentBoard.Cells[row, col] == CurrentBoard.Solution[row, col])
                return false;

            CurrentBoard.ClearValue(row, col);
            return true;
        }

        private List<(int, int)> FindMatchingCells(int digit, int size, int maxCount)
        {
            var result = new List<(int, int)>();
            for (var r = 0; r < size && result.Count < maxCount; r++)
            for (var c = 0; c < size && result.Count < maxCount; c++)
                if (CurrentBoard.Solution[r, c] == digit && CurrentBoard.Cells[r, c] == 0)
                    result.Add((r, c));
            return result;
        }

        private List<(int, int)> FindConflictZones(int size, int maxCount)
        {
            var result = new List<(int, int)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = CurrentBoard.Cells[r, c];
                if (v == 0 || CurrentBoard.IsGiven(r, c)) continue;
                if (v != CurrentBoard.Solution[r, c])
                {
                    result.Add((r, c));
                    if (maxCount > 0 && result.Count >= maxCount) return result;
                }
            }
            return result;
        }

        private List<(int, int)> FindTwinCandidateCells(int size)
        {
            var result = new List<(int, int)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (CurrentBoard.Cells[r, c] == 0 && CurrentBoard.GetPencilMarks(r, c).Count == 2)
                    result.Add((r, c));
            return result;
        }

        private List<(int, int)> FindMistakeCells(int size)
        {
            var result = new List<(int, int)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var v = CurrentBoard.Cells[r, c];
                if (v != 0 && !CurrentBoard.IsGiven(r, c) && v != CurrentBoard.Solution[r, c])
                    result.Add((r, c));
            }
            return result;
        }

        // ── Shop ──

        public List<ShopOffer> BuildShopOffers()
        {
            var priceMultiplier = RelicService.GetShopPriceMultiplier(State)
                                * CurseService.GetShopPriceMultiplier(State)
                                * (State.WornChiselActive ? 0.70f : 1.0f);
            var classLevel = GetCurrentClassLevel();
            var shopClassId = State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId;
            CurrentShopOffers = _shopService.BuildOffers(State.CurrentFloor, classLevel, priceMultiplier, State.HarmonyConfig.ShopSurcharge, shopClassId);
            return CurrentShopOffers;
        }

        public int GetShopRerollCost() => 15 + State.ShopRerollCount * 5;

        public bool RerollShop()
        {
            // Consume a reroll token first (free); fall back to gold if none remain.
            if (State.RerollTokens > 0)
            {
                State.RerollTokens--;
                State.ShopRerollCount++;
                BuildShopOffers();
                return true;
            }

            var cost = GetShopRerollCost();
            if (State.CurrentGold < cost) return false;
            State.CurrentGold -= cost;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                State.GoldSpentThisRun += cost;
                if (State.GoldSpentThisRun >= 50)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerSpentGold50);
            }
            State.ShopRerollCount++;
            BuildShopOffers();
            return true;
        }

        public bool TryPurchaseShopOffer(int offerIndex)
        {
            if (CurrentShopOffers == null || offerIndex < 0 || offerIndex >= CurrentShopOffers.Count) return false;

            var offer = CurrentShopOffers[offerIndex];
            if (offer.IsSold || State.CurrentGold < offer.Price) return false;

            State.CurrentGold -= offer.Price;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                State.GoldSpentThisRun += offer.Price;
                if (State.GoldSpentThisRun >= 50)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerSpentGold50);
            }
            offer.IsSold = true;
            AddItemToInventory(offer.Item);
            return true;
        }

        public bool TryBuyEmergencyHeal()
        {
            var price = ShopService.EmergencyHealPrice(State.PencilPurchaseCount);
            if (State.CurrentGold < price) return false;

            State.CurrentGold -= price;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                State.GoldSpentThisRun += price;
                if (State.GoldSpentThisRun >= 50)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerSpentGold50);
            }
            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + 1);
            State.PencilPurchaseCount++;
            return true;
        }

        // ── Boss Modifier Choice ──

        public List<BossModifierId> RollBossModifierChoices()
        {
            var minSize = FloorThemeData.GetMinBoardSize(State.CurrentFloor);
            BossModifierChoices = _bossService.RollBossChoices(State.CurrentFloor, State.ActiveFloorModifiers, minSize, State.HarmonyLevel);
            return BossModifierChoices;
        }

        public void GetBossModifierCounts(out int shown, out int picks)
        {
            BossService.GetBossModifierCounts(State.CurrentFloor, out shown, out picks);
        }

        public void ChooseBossModifiers(List<BossModifierId> chosen)
        {
            State.ChosenBossModifiers.Clear();
            State.HasChosenBossModifier = false;
            if (chosen == null) return;

            for (var i = 0; i < chosen.Count; i++)
            {
                var modifier = chosen[i];
                if (!BossService.IsSupportedForGeneratedRun(modifier)
                    || !BossService.CanCombineModifiers(State.ChosenBossModifiers, modifier))
                {
                    Debug.LogWarning(
                        $"[RunDirector] Ignored incompatible boss modifier selection: {modifier}.");
                    continue;
                }

                State.ChosenBossModifiers.Add(modifier);
                State.SeenBossModifiers.Add(modifier);
            }

            // Keep legacy singular field in sync for save compat
            if (State.ChosenBossModifiers.Count > 0)
            {
                State.HasChosenBossModifier = true;
                State.ChosenBossModifierId = State.ChosenBossModifiers[0];
            }

            Debug.Log(
                $"[ModifierDiscovery] Boss choice confirmed: chosen={ModifierDiscoveryService.Describe(State.ChosenBossModifiers)}, " +
                $"runSeen={ModifierDiscoveryService.Describe(State.SeenBossModifiers)}");
            Debug.Log($"[RunDirector] ChooseBossModifiers: [{string.Join(", ", State.ChosenBossModifiers)}]");
        }

        /// <summary>Legacy single-choice shortcut (picks 1 modifier).</summary>
        public void ChooseBossModifier(BossModifierId chosen)
        {
            ChooseBossModifiers(new List<BossModifierId> { chosen });
        }

        // ── Events ──

        public RunEvent BuildCurrentEvent()
        {
            CurrentEvent = _eventService.BuildEvent(State.CurrentFloor, State.Depth);
            return CurrentEvent;
        }

        /// <returns>Human-readable description of what was gained/lost.</returns>
        public string ResolveCurrentEventChoice(int optionIndex)
        {
            if (CurrentEvent == null) return string.Empty;
            var summary = _eventService.ResolveChoice(State, CurrentEvent, optionIndex);
            CurrentEvent = null;
            return summary;
        }

        // ── Relic ──

        public RelicInstance RollRelicReward()
        {
            var tierBonus = RelicService.GetRelicNodeTierBonus(State);
            return _relicService.RollRelic(State.CurrentFloor, tierBonus);
        }

        /// <summary>Roll distinct choices for this panel; copies may still be acquired across different nodes.</summary>
        public List<RelicInstance> RollRelicChoices(int count = 3)
        {
            var tierBonus = RelicService.GetRelicNodeTierBonus(State);
            var classId = State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId;
            RolledRelicChoices = _relicService.RollRelicChoices(State.CurrentFloor, count, tierBonus,
                classId, GetCurrentClassLevel());
            return RolledRelicChoices;
        }

        /// <summary>Accept the relic at the given choice index (from RolledRelicChoices).</summary>
        public void AcceptRelicChoice(int index)
        {
            if (RolledRelicChoices == null || index < 0 || index >= RolledRelicChoices.Count) return;
            AcceptRelic(RolledRelicChoices[index]);
            RolledRelicChoices = null;
        }

        public void AcceptRelic(RelicInstance relic)
        {
            State.HeldRelics.Add(relic);
            // Keep legacy fields in sync for serialization compatibility
            State.HasRelic = true;
            State.HeldRelic = State.HeldRelics[0];
            RelicService.ApplyPickupPassives(State, relic);
            if (DailyGoals != null && !State.IsSeasonalChallenge)
                DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerRelicCollected);
        }

        /// <summary>Called when the player accepts a Cursed node offer.</summary>
        public void OnCursedNodeAccepted()
        {
            if (DailyGoals == null || State.IsSeasonalChallenge) return;
            DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerAcceptedCursed);
        }

        // ── Curse API ── [REQ: CURSE-DATA-002]

        // [REQ: CURSE-INT-001] [REQ: CURSE-ACQUIRE-001] [REQ: CURSE-ACQUIRE-002] ApplyCurse: unified entry point for all curse acquisition (events, items, rest trades)
        public bool ApplyCurse(string curseId) => CurseService.ApplyCurse(State, curseId);
        public bool RemoveCurse(string curseId = null) => CurseService.TryRemoveCurse(State, curseId);
        public CurseDefinition RollCurse() => _curseService.RollCurse(State.CurrentFloor, State);
        public List<CurseDefinition> GetActiveCurses() => CurseService.GetActiveCurses(State);
        public bool HasAnyCurse() => CurseService.HasActiveCurse(State);

        // ── Rest Node Options ──

        public int GetRestHealAmount() => Math.Max(1, State.MaxHP / 3);

        public void AcceptRestHeal()
        {
            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + GetRestHealAmount());
        }

        public void AcceptRestPencilBoost()
        {
            State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + 4);
        }

        /// <summary>Removes the oldest active curse at the rest node. Returns false if no curses are active.</summary>
        // [REQ: CURSE-INT-010] [REQ: CURSE-REMOVE-001] Rest node cleanse: removes oldest active curse
        public bool AcceptRestCurseRemoval() => CurseService.TryRemoveCurse(State);

        public void AcceptRestRerollShop()
        {
            State.RerollTokens++;
        }

        // ── Navigation ──

        public RunNavigationSnapshot CaptureNavigationSnapshot()
        {
            var visited = CurrentFloorGraph == null ? null : new bool[CurrentFloorGraph.Count];
            var reachable = CurrentFloorGraph == null ? null : new bool[CurrentFloorGraph.Count];
            if (CurrentFloorGraph != null)
            {
                for (var i = 0; i < CurrentFloorGraph.Count; i++)
                {
                    visited[i] = CurrentFloorGraph[i].Visited;
                    reachable[i] = CurrentFloorGraph[i].Reachable;
                }
            }

            return new RunNavigationSnapshot(
                State?.CurrentNodeIndex ?? -1,
                State?.Depth ?? 0,
                State?.NodePath?.Count ?? 0,
                State?.RouteHistory?.Count ?? 0,
                visited,
                reachable);
        }

        public void RestoreNavigationSnapshot(RunNavigationSnapshot snapshot)
        {
            if (State == null) return;

            State.CurrentNodeIndex = snapshot.CurrentNodeIndex;
            State.Depth = snapshot.Depth;
            TrimList(State.NodePath, snapshot.NodePathCount);
            TrimList(State.RouteHistory, snapshot.RouteHistoryCount);

            if (CurrentFloorGraph != null && snapshot.Visited != null && snapshot.Reachable != null)
            {
                var count = Math.Min(CurrentFloorGraph.Count, Math.Min(snapshot.Visited.Length, snapshot.Reachable.Length));
                for (var i = 0; i < count; i++)
                {
                    CurrentFloorGraph[i].Visited = snapshot.Visited[i];
                    CurrentFloorGraph[i].Reachable = snapshot.Reachable[i];
                }
            }

            CurrentLevelConfig = null;
            CurrentLevelState = null;
            CurrentBoard = null;
            CurrentOverlay = null;
            ConstraintEngine = null;
        }

        private static void TrimList<T>(List<T> list, int count)
        {
            if (list == null) return;
            while (list.Count > count)
                list.RemoveAt(list.Count - 1);
        }

        public bool TryAdvanceToNode(int nodeIndex, bool forced = false)
        {
            if (!forced && !_routeService.CanMoveTo(CurrentFloorGraph, State.CurrentNodeIndex, nodeIndex))
                return false;

            _routeService.MarkVisited(CurrentFloorGraph, nodeIndex);
            State.CurrentNodeIndex = nodeIndex;
            State.NodePath.Add(nodeIndex);
            State.Depth++;

            // Daily goal: node-type triggers
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                var node = GetCurrentNode();
                if (node != null)
                {
                    if (node.Type == NodeType.Rest)
                        DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerRestVisited);
                    if (node.Type == NodeType.Shop)
                        DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerShopVisited);
                    if (node.Route == RouteType.RiskRoute)
                        DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerRiskRouteChosen);
                }
            }

            return true;
        }

        public List<int> GetReachableNodes()
        {
            return _routeService.GetReachableNodes(CurrentFloorGraph, State.CurrentNodeIndex);
        }

        public RunNode GetCurrentNode()
        {
            if (CurrentFloorGraph == null || State.CurrentNodeIndex < 0
                || State.CurrentNodeIndex >= CurrentFloorGraph.Count)
                return null;
            return CurrentFloorGraph[State.CurrentNodeIndex];
        }

        // ── Save / Restore ──

        public PuzzleSaveState ExportPuzzleSaveState()
        {
            if (CurrentBoard == null) return null;

            var size = CurrentBoard.Size;
            var totalCells = size * size;
            var save = new PuzzleSaveState
            {
                BoardSize = size,
                Stars = CurrentLevelConfig?.Stars ?? 1,
                IsBoss = CurrentLevelConfig?.IsBoss ?? false,
                Board = new int[totalCells],
                Solution = new int[totalCells],
                GivenMask = new int[totalCells],
                RegionMap = new int[totalCells]
            };

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var idx = r * size + c;
                save.Board[idx] = CurrentBoard.Cells[r, c];
                save.Solution[idx] = CurrentBoard.Solution[r, c];
                save.GivenMask[idx] = CurrentBoard.GivenMask[r, c] ? 1 : 0;
                save.RegionMap[idx] = CurrentBoard.RegionMap[r, c];
            }

            // Save pencil marks
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var marks = CurrentBoard.GetPencilMarks(r, c);
                if (marks.Count > 0)
                {
                    var pms = new PencilMarkSaveData { Row = r, Col = c };
                    pms.Marks.AddRange(marks);
                    save.PencilMarks.Add(pms);
                }
            }

            // Save CellLock debuff state
            if (CurrentLevelState != null)
            {
                foreach (var kv in CurrentLevelState.LockedCells)
                    save.LockedCells.Add(new LockedCellSaveData { Key = kv.Key, Count = kv.Value });
            }

            // Save blurred_sight curse state (wired by InRunController.Configure via BlurredPencilCells)
            if (BlurredPencilCells != null)
            {
                foreach (var cell in BlurredPencilCells)
                    save.BlurredPencilCells.Add(new BlurredCellSaveData { Row = cell.Item1, Col = cell.Item2 });
            }

            UnityEngine.Debug.Log($"[SaveExport] Board non-zero={CountNonZero(save.Board)} Moves={save.Moves.Count} Pencils={save.PencilMarks.Count} LockedCells={save.LockedCells.Count} BlurredCells={save.BlurredPencilCells.Count}");
            return save;
        }

        private static int CountNonZero(int[] arr)
        {
            var n = 0;
            for (var i = 0; i < arr.Length; i++) if (arr[i] != 0) n++;
            return n;
        }

        public bool TryRestorePuzzleSaveState(PuzzleSaveState save)
        {
            if (!SaveFileService.IsUsablePuzzleSave(save)) return false;

            var size = save.BoardSize;
            var solution = new int[size, size];
            var cells = new int[size, size];
            var regionMap = new int[size, size];

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var idx = r * size + c;
                solution[r, c] = save.Solution[idx];
                cells[r, c] = save.Board[idx];
                regionMap[r, c] = save.RegionMap[idx];
            }

            CurrentBoard = new SudokuBoard(size, solution, cells, regionMap);
            CurrentLevelState = new LevelState();

            // Overwrite GivenMask
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var idx = r * size + c;
                if (save.GivenMask[idx] == 1)
                    CurrentBoard.GivenMask[r, c] = true;
            }

            UnityEngine.Debug.Log($"[SaveRestore] Board non-zero={CountNonZero(save.Board)} Moves={save.Moves.Count} Pencils={save.PencilMarks.Count} LockedCells={save.LockedCells?.Count ?? 0} BlurredCells={save.BlurredPencilCells?.Count ?? 0}");

            // Restore pencil marks
            for (var i = 0; i < save.PencilMarks.Count; i++)
            {
                var pm = save.PencilMarks[i];
                for (var j = 0; j < pm.Marks.Count; j++)
                    CurrentBoard.AddPencilMark(pm.Row, pm.Col, pm.Marks[j]);
            }

            // Restore CellLock debuff state
            if (save.LockedCells != null)
            {
                foreach (var lc in save.LockedCells)
                    CurrentLevelState.LockedCells[lc.Key] = lc.Count;
            }

            // Restore blurred_sight curse cells; uses the existing set if InRunController already wired it
            if (save.BlurredPencilCells != null && save.BlurredPencilCells.Count > 0)
            {
                BlurredPencilCells ??= new HashSet<(int, int)>();
                foreach (var bc in save.BlurredPencilCells)
                    BlurredPencilCells.Add((bc.Row, bc.Col));
            }

            return true;
        }

        // ── Helpers ──

        // Per-floor difficulty tables (floor index 0–4)
        private static readonly int[] FloorDefaultStars = { 1, 2, 3, 4, 5 };
        private static readonly int[] FloorBossStars    = { 2, 3, 4, 5, 6 };

        private int RollStars(System.Random rng, int floor, bool isBoss, bool isElite)
        {
            var fi = Math.Clamp(floor, 0, FloorDefaultStars.Length - 1);

            if (isBoss)
            {
                var bossStars = FloorBossStars[fi];
                UnityEngine.Debug.Log($"[Difficulty] Floor {floor} BOSS: fixed {bossStars}★");
                return bossStars;
            }

            var defaultStars = isElite
                ? Math.Min(FloorDefaultStars[fi] + 1, 6)
                : FloorDefaultStars[fi];

            // 20% chance to bump to any star level strictly above the default (up to 6★)
            if (rng.NextDouble() <= 0.20 && defaultStars < 6)
            {
                var stars = defaultStars + 1 + rng.Next(0, 6 - defaultStars);
                UnityEngine.Debug.Log($"[Difficulty] Floor {floor}: variant {stars}★ (base {defaultStars}★, 20% roll)");
                return stars;
            }

            UnityEngine.Debug.Log($"[Difficulty] Floor {floor}: default {defaultStars}★");
            return defaultStars;
        }

        private static DifficultyTier MapDifficulty(int floor, int stars)
        {
            var score = floor + stars;
            if (score <= 2) return DifficultyTier.Diff1;
            if (score <= 4) return DifficultyTier.Diff2;
            if (score <= 6) return DifficultyTier.Diff3;
            if (score <= 8) return DifficultyTier.Diff4;
            return DifficultyTier.Diff5;
        }

        private int GetCurrentClassLevel() => State.ClassLevel;

        /// <summary>
        /// True when the cell is inside the active fog and the lantern is not suppressing it.
        /// </summary>
        private bool IsCellInFog(int row, int col)
        {
            var fogCells = CurrentOverlay?.FogCells;
            if (fogCells == null || fogCells.Count == 0) return false;
            if (State.FogDisabledMoves > 0) return false;  // lantern active — all cells visible
            for (var i = 0; i < fogCells.Count; i++)
                if (fogCells[i].Row == row && fogCells[i].Col == col) return true;
            return false;
        }

        private static bool HasAllModifiersPresent(List<BossModifierId> modifiers, ModifierOverlayData overlay)
        {
            if (overlay == null) return false;
            for (var i = 0; i < modifiers.Count; i++)
            {
                switch (modifiers[i])
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
                        if (overlay.Lines.Count == 0) return false;
                        break;
                    case BossModifierId.DifferenceKropki:
                    case BossModifierId.RatioKropki:
                    case BossModifierId.FullKropki:
                    case BossModifierId.SumKropki:
                    case BossModifierId.FullWhiteKropki:
                    case BossModifierId.FullBlackKropki:
                        if (overlay.KropkiDots.Count == 0) return false;
                        break;
                    case BossModifierId.GreaterLessThan:
                    case BossModifierId.XVPairs:
                        if (overlay.PairConstraints.Count == 0) return false;
                        break;
                    case BossModifierId.KillerCages:
                    case BossModifierId.KillerHiddenSum:
                    case BossModifierId.CageProduct:
                    case BossModifierId.CageDifference:
                    case BossModifierId.CageRatio:
                    case BossModifierId.RenbanCage:
                        if (overlay.KillerCages.Count == 0) return false;
                        break;
                    case BossModifierId.ArrowSums:
                    case BossModifierId.ArrowAverage:
                    case BossModifierId.ArrowProduct:
                    case BossModifierId.PillArrow:
                    case BossModifierId.DoubleArrow:
                        if (overlay.Arrows.Count == 0) return false;
                        break;
                    case BossModifierId.FogOfWar:
                        if (overlay.FogCells.Count == 0) return false;
                        break;
                    case BossModifierId.EvenOdd:
                    case BossModifierId.PrimeCells:
                    case BossModifierId.FortressCells:
                        if (overlay.CellMarkers.Count == 0) return false;
                        break;
                }
            }
            return true;
        }

        private void ClearOverlayCellsFromGivenMask()
        {
            if (CurrentOverlay == null || CurrentBoard == null) return;

            // Remove overlay elements whose cells are all givens (player can't solve them)
            for (var i = CurrentOverlay.Lines.Count - 1; i >= 0; i--)
            {
                var allGiven = true;
                var line = CurrentOverlay.Lines[i];
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    if (!CurrentBoard.GivenMask[line.Cells[j].Row, line.Cells[j].Col])
                    {
                        allGiven = false;
                        break;
                    }
                }
                if (allGiven) CurrentOverlay.Lines.RemoveAt(i);
            }
        }

        // ── Analytics Access ──

        public PostRunAnalyticsService GetAnalytics() => _analytics;
        public int GetTotalRunXp() => XpService.SumRunXp(TileXpLog);
    }

    public readonly struct RunNavigationSnapshot
    {
        internal readonly int CurrentNodeIndex;
        internal readonly int Depth;
        internal readonly int NodePathCount;
        internal readonly int RouteHistoryCount;
        internal readonly bool[] Visited;
        internal readonly bool[] Reachable;

        internal RunNavigationSnapshot(
            int currentNodeIndex,
            int depth,
            int nodePathCount,
            int routeHistoryCount,
            bool[] visited,
            bool[] reachable)
        {
            CurrentNodeIndex = currentNodeIndex;
            Depth = depth;
            NodePathCount = nodePathCount;
            RouteHistoryCount = routeHistoryCount;
            Visited = visited;
            Reachable = reachable;
        }
    }

    public enum AsyncLevelFailureReason
    {
        None,
        GenerationFailed,
        ModifierGeometryFailed,
        TaskFaulted,
        BudgetExceeded,
        Cancelled
    }

    /// <summary>
    /// Carries the result of a background overlay generation initiated by
    /// <see cref="RunDirector.StartLevelAsync"/>. Construct via the
    /// <see cref="Success"/>, <see cref="Failure"/>, and <see cref="Pending"/> factory methods.
    /// Only access <see cref="Board"/> and <see cref="Overlay"/> when <see cref="IsSuccess"/> is true.
    /// </summary>
    public readonly struct AsyncLevelResult
    {
        public readonly bool IsSuccess;
        public readonly SudokuBoard Board;
        public readonly ModifierOverlayData Overlay;
        public readonly AsyncLevelFailureReason FailureReason;
        public readonly PuzzleGenerationMetrics Metrics;
        public readonly IReadOnlyList<BossModifierId> MissingModifiers;

        private AsyncLevelResult(
            bool ok,
            SudokuBoard board,
            ModifierOverlayData overlay,
            AsyncLevelFailureReason reason,
            PuzzleGenerationMetrics metrics,
            IReadOnlyList<BossModifierId> missingModifiers)
        {
            IsSuccess     = ok;
            Board         = board;
            Overlay       = overlay;
            FailureReason = reason;
            Metrics       = metrics;
            MissingModifiers = missingModifiers;
        }

        /// <summary>Successful result. <paramref name="overlay"/> may be null (replaced with empty).</summary>
        public static AsyncLevelResult Success(SudokuBoard board, ModifierOverlayData overlay)
            => new AsyncLevelResult(
                true,
                board,
                overlay ?? new ModifierOverlayData(),
                AsyncLevelFailureReason.None,
                new PuzzleGenerationMetrics(),
                new List<BossModifierId>());

        public static AsyncLevelResult Success(PuzzleGenerationResult result)
            => Success(result, null);

        public static AsyncLevelResult Success(
            PuzzleGenerationResult result,
            IReadOnlyList<BossModifierId> additionalMissingModifiers)
            => new AsyncLevelResult(
                true,
                result.Board,
                result.Overlay ?? new ModifierOverlayData(),
                AsyncLevelFailureReason.None,
                result.Metrics,
                MergeMissingModifiers(additionalMissingModifiers, result.MissingModifiers));

        /// <summary>Failure result. <see cref="Board"/> and <see cref="Overlay"/> will be null.</summary>
        public static AsyncLevelResult Failure(AsyncLevelFailureReason reason, PuzzleGenerationMetrics metrics = null)
            => new AsyncLevelResult(false, null, null, reason, metrics ?? new PuzzleGenerationMetrics(), new List<BossModifierId>());

        /// <summary>Initial state — set before the background task begins.</summary>
        public static AsyncLevelResult Pending()
            => new AsyncLevelResult(false, null, null, AsyncLevelFailureReason.None, new PuzzleGenerationMetrics(), new List<BossModifierId>());

        private static IReadOnlyList<BossModifierId> MergeMissingModifiers(
            IReadOnlyList<BossModifierId> first,
            IReadOnlyList<BossModifierId> second)
        {
            var merged = new List<BossModifierId>();
            AddMissingModifiers(merged, first);
            AddMissingModifiers(merged, second);
            return merged;
        }

        private static void AddMissingModifiers(List<BossModifierId> target, IReadOnlyList<BossModifierId> source)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++)
            {
                if (!target.Contains(source[i]))
                    target.Add(source[i]);
            }
        }
    }

    public enum PlaceResult
    {
        Correct,
        Invalid,
        IsGiven,
        IsPresolved
    }
}
