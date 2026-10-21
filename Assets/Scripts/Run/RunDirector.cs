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
    public sealed partial class RunDirector
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
        // Analytics and the XP log live in RunState, so a resumed run keeps them (GAP-038).
        private PostRunAnalyticsService _analytics => State?.Analytics;
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
        // [REQ: XP-LOG-001] per-tile XP tracked for end-of-run breakdown display; saved with the run (GAP-038)
        public List<TileXpEntry> TileXpLog => State != null ? State.TileXpLog ??= new List<TileXpEntry>() : _detachedXpLog;
        private readonly List<TileXpEntry> _detachedXpLog = new List<TileXpEntry>();
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
        private int _pendingOriginalSeed;

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
            InitServices(seed);

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
                request.HarmonyLevel, request.HarmonyPerk, request.ClassPrestigeTier);
            State.Mode = request.Mode;

            // Roll floor modifiers for the initial floor (Floor 1 = 0 modifiers)
            var startingRelicCount = RunArchetypeService.GetStartingRelicCount(request.ClassId, request.ClassLevel);
            _relicService.ReseedForDecision(DecisionSeed(DecisionPurpose.StartingRelics));
            _relicService.AssignStartingRelics(State, startingRelicCount, State.HarmonyLevel);
            RollFloorModifiers();
            State.FloorGraphVersion = RunGraphService.CurrentGraphVersion;
            RebuildFloorGraph();
            State.HpAtCurrentFloorEntry = State.CurrentHP; // floor 0 entry HP for full_hp_floor goal
        }

        /// <summary>Start a Seasonal Challenge run using a pre-built RunState and LevelConfig.
        /// State must already be set via RestoreState before calling this.</summary>
        public void StartSeasonalChallenge(LevelConfig config)
        {
            // [REQ: SEASON-PUZZLE-001] Clear the previous run's level first: a failed generation must
            // leave no board rather than the old run's board (GAP-004).
            ClearLevelForNewMode();
            InitServices(State.Seed);
            TileXpLog.Clear();
            State.Analytics = new PostRunAnalyticsService();

            CurrentLevelConfig = config;
            StartLevel(config);
        }

        /// <summary>
        /// Resumes a saved Seasonal Challenge attempt without regenerating the puzzle. Requires a save
        /// that carries its level definition (written after the GAP-001 fix).
        /// </summary>
        public bool ResumeSeasonalChallenge(PuzzleSaveState savedPuzzle)
        {
            if (savedPuzzle == null || !savedPuzzle.HasLevelConfig) return false;
            ClearLevelForNewMode();
            InitServices(State.Seed);
            return TryRestorePuzzleSaveState(savedPuzzle);
        }

        private void ClearLevelForNewMode()
        {
            _pendingLevelGenerationCancel?.Cancel();
            _pendingLevelGenerationCancel = null;
            _pendingOverlayTask = null;
            _pendingLevelConfig = null;
            _asyncResult = default;
            CurrentBoard = null;
            CurrentLevelState = null;
            CurrentLevelConfig = null;
            CurrentOverlay = null;
            ConstraintEngine = null;
        }

        public void StartTutorialRun(TutorialSetupConfig setup, int seed)
        {
            State = Tutorial.TutorialModeService.CreateTutorialRunState(setup, seed);

            InitServices(seed);
            TileXpLog.Clear();
            State.Analytics = new PostRunAnalyticsService();

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
            State.Analytics = new PostRunAnalyticsService();
            RollFloorModifiers();
            State.FloorGraphVersion = RunGraphService.CurrentGraphVersion;
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

        // [REQ: MAP-CROSS-001] The floor graph is rebuilt from the seed with the layout version the floor started with.
        public void RebuildFloorGraph()
        {
            CurrentFloorGraph = _graphService.BuildFloorGraph(State.CurrentFloor, State.Seed, State.FloorGraphVersion);
            State.CurrentNodeIndex = 0;
        }

        // [REQ: MAP-STRUCT-001] AdvanceToNextFloor: run spans exactly 5 floors (index 0–4); floor 4 boss win = run complete
        public void AdvanceToNextFloor()
        {
            State.CurrentFloor++;
            State.FloorGraphVersion = RunGraphService.CurrentGraphVersion; // a new floor always uses the current layout
            State.Depth = 0;
            State.HpAtCurrentFloorEntry = State.CurrentHP; // capture before relic heals apply
            State.FloorTally = new FloorTally();

            // Clear boss modifiers from previous floor
            State.ChosenBossModifiers.Clear();
            State.HasChosenBossModifier = false;

            // Roll floor modifiers for the new floor
            RollFloorModifiers();

            // Reset per-floor item flags
            State.WornChiselActive = false;
            State.DimLanternUsed = false;
            State.ShopRerollCount = 0;

            // Survey Notes reveals tile details for one floor; Accurate Map sets it again below (OnFloorStart).
            State.AllNodesRevealed = false;
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
            _bossService.ReseedForDecision(DecisionSeed(DecisionPurpose.FloorModifiers));
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

        // ── Gameplay ──

        // [REQ: GEN-VALID-001] A digit is correct when it matches the stored solution (SPEC-CONF-003, decided
        // 2026-10-06). Run, mode and boss puzzles keep a clue whenever removing it would allow a second base-rule
        // solution, so the stored solution is the only valid completion. Custom Puzzle 7★ boards start empty and
        // have many solutions; there a digit is correct when it breaks no active rule.
        public bool UsesSolutionValidation =>
            !(State != null && State.Mode == GameMode.Tutorial && CurrentLevelConfig != null && CurrentLevelConfig.Stars >= 7);

        public bool IsPlacementCorrect(int row, int col, int value)
        {
            if (CurrentBoard == null) return false;
            var solution = CurrentBoard.Solution;
            if (UsesSolutionValidation && solution != null && solution[row, col] > 0)
                return solution[row, col] == value;
            // [REQ: GEN-VALID-002] Rule-based check through SudokuConstraintEngine.ValidateAll (all active modifier rules)
            return ConstraintEngine == null || ConstraintEngine.ValidateAll(CurrentBoard, row, col, value, CurrentOverlay);
        }

        // [REQ: REQ-SUD-103] A wrong digit is rejected (the cell keeps its previous value) and its penalty is applied
        // here, not in the UI, so items, automation and tests get the same rules (GAP-035).
        public PlaceResult PlaceNumber(int row, int col, int value)
        {
            if (CurrentBoard == null || CurrentLevelState == null) return PlaceResult.Correct;
            if (CurrentBoard.IsGiven(row, col))
                return PlaceResult.IsGiven;
            if (CurrentLevelState != null && CurrentLevelState.PresolvedCells.Contains((row, col)))
                return PlaceResult.IsPresolved;

            // Re-entering the digit a cell already holds changes nothing: no placement, combo or penalty.
            if (CurrentBoard.Cells[row, col] == value)
                return PlaceResult.Unchanged;

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

            var isValid = IsPlacementCorrect(row, col, value);

            if (isValid)
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

                // [REQ: OPT-GAME-002] Auto Pencil Cleanup: the placed digit leaves the marks of its row, column and box
                // (the box was missed, GAP-061); with the option off every mark stays.
                if (AutoPencilCleanup)
                {
                    var boardSize = CurrentBoard.Size;
                    var region = CurrentBoard.RegionMap[row, col];
                    for (var pr = 0; pr < boardSize; pr++)
                    for (var pc = 0; pc < boardSize; pc++)
                        if (pr == row || pc == col || CurrentBoard.RegionMap[pr, pc] == region)
                            CurrentBoard.RemovePencilMark(pr, pc, value);
                }

                return PlaceResult.Correct;
            }

            State.LastComboBeforeMistake = State.ComboStreak;
            State.LastMistakeRow = row;
            State.LastMistakeCol = col;
            CurrentLevelState.Mistakes++;
            _analytics?.RecordMistake();   // GAP-059: counted when it happens
            CurrentLevelState.PerfectSoFar = false;
            // [REQ: PRESSURE-TICK-002] Wrong placements also decrement threat counters (no success check).
            TickPressureCountersOnly();
            // HP (shields, class passives, relics), debuff and pressure effects of the mistake.
            ApplyMistakePenalty(row, col);
            // Shield: if the mistake was fully absorbed by ShieldPoints, preserve the combo streak.
            if (!State.LastMistakeShieldAbsorbed)
                State.ComboStreak = 0;
            return PlaceResult.Invalid;
        }

        // [REQ: REQ-RUN-102] [REQ: REQ-RUN-103] Every roll (boss choices, rewards, shop, relics, events, curses) uses a
        // random stream seeded from the run seed, the player's position and the purpose of the roll, instead of one
        // stream per service that restarted on resume (GAP-018). A roll is therefore the same before and after
        // Save & Quit, the same when a screen is rebuilt, and still differs between nodes and floors.
        private enum DecisionPurpose
        {
            StartingRelics = 1, FloorModifiers, BossChoices, ItemReward, RewardCurse, ShopOffers,
            RelicReward, RelicChoices, Event, EventChoice, CurseRoll, PuzzleCurse, BossDebuff
        }

        private int DecisionSeed(DecisionPurpose purpose, int salt = 0)
        {
            unchecked
            {
                var h = Mix((uint)State.Seed * 0x9E3779B1u);
                h = Mix(h ^ (uint)State.CurrentFloor * 0x85EBCA77u);
                h = Mix(h ^ (uint)State.Depth * 0xC2B2AE3Du);
                h = Mix(h ^ (uint)State.CurrentNodeIndex * 0x27D4EB2Fu);
                h = Mix(h ^ (uint)purpose * 0x165667B1u);
                h = Mix(h ^ (uint)salt);
                return (int)h;
            }
        }

        private static uint Mix(uint x)
        {
            unchecked
            {
                x ^= x >> 16; x *= 0x7FEB352Du;
                x ^= x >> 15; x *= 0x846CA68Bu;
                x ^= x >> 16;
                return x;
            }
        }

        /// <summary>Run-state changes at the start of every new puzzle (not on resume).</summary>
        private void PrepareRunStateForNewPuzzle()
        {
            if (State == null) return;
            // Forget the last mistake so Ginkgo Leaf cannot undo one from an earlier puzzle.
            State.LastMistakeRow = -1;
            State.LastMistakeCol = -1;
            State.LastMistakeHpLost = 0;
            State.LastComboBeforeMistake = 0;
            // [REQ: HARMONY-SURV-003] Combo decay (H7+): the combo carried into a new puzzle is halved
            // (SPEC-AMB resolution 2026-10-06; the "8 placements without a bonus" wording could never trigger).
            if (State.ComboDecayEnabled)
                State.ComboStreak /= 2;
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
                    _analytics?.RecordMistake();   // GAP-059: counted when it happens
                    CurrentLevelState.PerfectSoFar = false;
                    State.LastComboBeforeMistake = State.ComboStreak;
                    State.LastMistakeRow = r;
                    State.LastMistakeCol = c;
                    ApplyMistakePenalty(r, c);
                    // Wrong digits never stay on the board (SPEC-CONF-003); this one was hidden until now.
                    if (!CurrentBoard.IsGiven(r, c))
                        CurrentBoard.ClearValue(r, c);
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

        /// <summary>[REQ: OPT-GAME-002] Placing a digit removes it from the pencil marks around it (Options, Gameplay).</summary>
        public bool AutoPencilCleanup { get; set; } = true;

        public const int LateFloorIndex = 3;   // Floor 4
        public const int LateFloorMistakeHp = 2;

        /// <summary>[REQ: ECON-BAL-003] The least a mistake costs on this floor: 2 HP from Floor 4 of a Garden Run, else 1.</summary>
        public static int LateFloorMistakeHpCost(RunState state) =>
            state != null && state.Mode == GameMode.GardenRun && !state.TutorialMode && !state.IsSeasonalChallenge
                && state.CurrentFloor >= LateFloorIndex ? LateFloorMistakeHp : 1;

        /// <summary>
        /// Debuff-aware mistake penalty. Triggers RowWipe, ColWipe, PencilBlind, CellLock, DoublePenalty
        /// based on active modifiers in the current level config.
        /// </summary>
        // [REQ: DEBUFF-HOOK-001] ApplyMistakePenalty(row,col) is the central dispatch for all debuff effects
        public void ApplyMistakePenalty(int row, int col)
        {
            // [REQ: HARMONY-SURV-002] Base damage is the run's mistake HP cost (1; 2 from H5; 3 from H9) (GAP-008).
            // [REQ: DEBUFF-HOOK-002] DoublePenalty doubles it.
            // [REQ: CURSE-INT-002] phantom_pain curse raises base damage to 2
            var damage = Math.Max(1, State.MistakeHpCost);
            // [REQ: ECON-BAL-003] From Floor 4 of a Garden Run a mistake costs at least 2 HP, at every Harmony level
            // (gold-and-items concept L6): healing and shield items become survival tools late in a run.
            damage = Math.Max(damage, LateFloorMistakeHpCost(State));
            if (IsDebuffActive(BossModifierId.DoublePenalty)) damage *= 2;
            damage = Math.Max(damage, CurseService.GetMistakeDamage(State)); // phantom_pain
            ApplyMistakePenalty(damage);

            // [REQ: HARMONY-FLAG-003] H7+: every mistake also costs 1 pencil.
            if (Harmony.MistakeDrainsPencil)
                State.CurrentPencil = Math.Max(0, State.CurrentPencil - 1);

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

        /// <summary>Correct placements left before items can be used again (Item Block debuff).</summary>
        public int ItemUseBlockedMoves => CurrentLevelState?.ItemUseBlockedMoves ?? 0;

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
        // [REQ: PRESSURE-ROLL-001] [REQ: PRESSURE-ROLL-003] [REQ: PRESSURE-ROLL-005] `floor` is the 0-based
        // State.CurrentFloor; the spec counts floors from 1 (GAP-014: the rolls used to come one floor late).
        private void RollPressureMechanic(LevelConfig config, int floor, System.Random rng)
        {
            if (State.TutorialMode) return;
            if (floor < 1) return; // never on Floor 1
            if (!config.IsElite && !config.IsPreBoss && !config.IsBoss) return;

            // Floor 2 = 12 %, Floor 3 = 30 %, Floor 4 = 55 %, Floor 5 = 80 %
            float chance = floor switch { 1 => 0.12f, 2 => 0.30f, 3 => 0.55f, _ => 0.80f };
            if (rng.NextDouble() > chance) return;

            var candidates = new System.Collections.Generic.List<BossModifierId>
            {
                BossModifierId.CountdownFill,
                BossModifierId.HauntedCell,
                BossModifierId.CrumblingRegion
            };
            if (floor >= 2) candidates.Add(BossModifierId.PressureWave); // from Floor 3

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
            // [REQ: REQ-BOSS-107] The floor's numbers for the boss-defeated screen (older saves have no tally yet).
            (State.FloorTally ??= new FloorTally()).Add(level);

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

            // [REQ: HARMONY-ECON-001] Harmony gold multiplier (×1.00 at H0 … ×0.60 at H10) (GAP-008).
            if (State.GlobalGoldMultiplier > 0f)
                goldBase = (int)Math.Round(goldBase * State.GlobalGoldMultiplier);

            // [REQ: REQ-ECON-101] GainGold also counts collected gold for unlocks and stats (GAP-027).
            State.GainGold(goldBase);
            if (DailyGoals != null && !State.IsSeasonalChallenge && State.GoldCollectedThisRun >= 150)
                DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerCollected150Gold);

            var xpEntry = XpService.CalculateTile(
                config.BoardSize, config.Stars,
                config.ActiveModifiers.Count, config.IsBoss,
                level.PerfectSoFar);

            // Apply cursed XP bonus
            if (config.IsCursed)
                xpEntry.TotalXp = (int)(xpEntry.TotalXp * config.CursedXpMult);

            if (level.PerfectSoFar) _analytics?.RecordPerfectPuzzle();
            // [REQ: MAP-NODE-CURSED-004] A cursed puzzle accepted and completed earns its run-score bonus (GAP-054).
            if (config.IsCursed) _analytics?.RecordCursedPuzzleCompleted();

            // ReedPledge: success = +4 Pencil, +30 Gold, +25 bonus XP
            // [REQ: XP-LOG-001] The bonus belongs to this tile's entry, which is added below (GAP-034).
            if (State.PledgeActive)
            {
                if (level.NoPencilUsed)
                {
                    State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + 4);
                    State.GainGold(30);
                    xpEntry.TotalXp += 25;
                }
                State.PledgeActive = false;
            }

            // Class passive level-complete hook (e.g. ReedDuelist pencil bonus)
            ClassPassiveService.OnLevelComplete(State, level);

            // Curse: restless_inventory drain + auto-cleanse on perfect
            _curseService.ReseedForDecision(DecisionSeed(DecisionPurpose.PuzzleCurse));
            _curseService.OnPuzzleComplete(State, level.PerfectSoFar && level.Mistakes == 0);

            TileXpLog.Add(xpEntry);
            _analytics?.RecordPuzzleSolved(level.Mistakes, goldBase, State.CurrentFloor);
            _analytics?.RecordTileXp(xpEntry);

            if (config.IsBoss && !State.IsSeasonalChallenge)
            {
                State.BossesDefeatedThisRun++;
                // [REQ: META-COMPLETE-001] Player modifiers of a defeated boss count toward completion (GAP-009).
                State.ClearedBossModifiersThisRun ??= new List<BossModifierId>();
                foreach (var mod in config.ActiveModifiers)
                    if (BossService.IsImplementedModifier(mod) && !State.ClearedBossModifiersThisRun.Contains(mod))
                        State.ClearedBossModifiersThisRun.Add(mod);
            }

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

            _itemService.ReseedForDecision(DecisionSeed(DecisionPurpose.ItemReward));
            _curseService.ReseedForDecision(DecisionSeed(DecisionPurpose.RewardCurse));
            RolledItemSlots = _itemService.RollSlots(stars, classLevel, bonusSlots,
                State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId);

            // [REQ: HARMONY-ITEM-001] Harmony adds +1.5 % per level to each slot's Nothing chance, capped at 40 % in
            // total (spec §4.7, GAP-008). Like the base roll, it never removes the last real item of a reward.
            var baseNothing = ItemService.GetNothingChance(stars);
            var harmonyNothing = Math.Max(0f, Math.Min(Harmony.NothingChanceBonus, 0.40f - baseNothing));
            if (harmonyNothing > 0f && baseNothing < 1f)
            {
                var extra = harmonyNothing / (1f - baseNothing);
                var harmonyRng = new System.Random(State.Seed ^ (State.Depth * 7919 + State.CurrentFloor * 104729 + 0x4A11));
                for (var i = 0; i < RolledItemSlots.Count; i++)
                {
                    if (RolledItemSlots[i] == null || harmonyRng.NextDouble() >= extra) continue;
                    if (RolledItemSlots.FindAll(slot => slot != null).Count <= 1) break;
                    RolledItemSlots[i] = null;
                }
            }

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
                    State.GainGold(30);
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
                            _analytics?.RecordMistakeUndone();   // GAP-059: the run total follows the undo
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
                    return ItemEffectResult.Message(LocalizationService.T("Item.Effect.SurveyNotes", "Board size and stars shown for every tile on this floor."));
                }

                // [REQ: ITEM-COUNTER-001..006] Counter items (RunDirector.CounterItems.cs)
                case ItemType.WaterBasin:
                case ItemType.TwinPebbles:
                case ItemType.RiceBowl:
                case ItemType.MossStone:
                case ItemType.CompassOfOrder:
                case ItemType.SunMedallion:
                    return ApplyCounterItem(item, row, col);

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

        private bool TryUndoLastMistakePlacement()
        {
            if (CurrentBoard == null || State == null) return false;
            var row = State.LastMistakeRow;
            var col = State.LastMistakeCol;
            if (row < 0 || col < 0 || row >= CurrentBoard.Size || col >= CurrentBoard.Size)
                return false;
            // Wrong digits are rejected (SPEC-CONF-003), so normally nothing is left to clear; the undo restores HP
            // and combo. A wrong digit kept by a save from before 1.1 is still removed.
            var held = CurrentBoard.Cells[row, col];
            if (!CurrentBoard.IsGiven(row, col) && held != 0 && UsesSolutionValidation
                && held != CurrentBoard.Solution[row, col])
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

            // [REQ: SAVE-RESUME-001] Persist the level definition, overlay and counters so resume
            // can rebuild rules, overlay and pressure instead of leaving them null (GAP-001).
            if (CurrentLevelConfig != null)
            {
                save.HasLevelConfig = true;
                save.LevelConfig = CurrentLevelConfig.Clone();
            }
            if (CurrentOverlay != null)
            {
                save.HasOverlay = true;
                save.Overlay = CurrentOverlay;
            }
            if (CurrentLevelState != null)
            {
                save.HasLevelCounters = true;
                save.Mistakes = CurrentLevelState.Mistakes;
                save.HpLost = CurrentLevelState.HpLost;
                save.CorrectPlacements = CurrentLevelState.CorrectPlacements;
                save.PencilMarksUsed = CurrentLevelState.PencilMarksUsed;
                save.ItemsUsedThisLevel = CurrentLevelState.ItemsUsedThisLevel;
                save.PerfectSoFar = CurrentLevelState.PerfectSoFar;
                save.NoPencilUsed = CurrentLevelState.NoPencilUsed;
                if (CurrentLevelState.FogPendingCells != null)
                {
                    foreach (var (r, c) in CurrentLevelState.FogPendingCells)
                        save.FogPendingCells.Add(r * 100 + c);
                }
            }

            UnityEngine.Debug.Log($"[SaveExport] Board non-zero={CountNonZero(save.Board)} Moves={save.Moves.Count} Pencils={save.PencilMarks.Count} LockedCells={save.LockedCells.Count} BlurredCells={save.BlurredPencilCells.Count} Mods={save.LevelConfig?.ActiveModifiers?.Count ?? 0}");
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

            RestoreLevelRulesAfterResume(save);
            return true;
        }

        // [REQ: SAVE-RESUME-001] Rebuilds level config, overlay, constraint engine, level counters and
        // pressure state after TryRestorePuzzleSaveState replaced the board. Without this, every
        // placement after a mid-puzzle resume dereferenced a null engine (GAP-001).
        private void RestoreLevelRulesAfterResume(PuzzleSaveState save)
        {
            // Any in-flight async generation belongs to the level that was replaced.
            _pendingLevelGenerationCancel?.Cancel();
            _pendingLevelGenerationCancel = null;
            _pendingOverlayTask = null;
            _pendingLevelConfig = null;
            _asyncResult = default;

            var keepRegeneratedLevel = false;
            if (save.HasLevelConfig && save.LevelConfig != null)
            {
                CurrentLevelConfig = save.LevelConfig;
                CurrentLevelConfig.ActiveModifiers ??= new List<BossModifierId>();
            }
            else if (CurrentLevelConfig != null && CurrentLevelConfig.BoardSize == save.BoardSize
                && ConstraintEngine != null)
            {
                // Legacy save on a path that already regenerated the same level deterministically
                // (Seasonal Challenge resume): keep that config, overlay and engine.
                keepRegeneratedLevel = true;
            }
            else
            {
                CurrentLevelConfig = BuildLegacyResumeConfig(save);
                Debug.LogWarning(
                    "[SaveRestore] Save has no level definition (written before GAP-001 fix). " +
                    "Rebuilt the level from run state; modifier geometry from the original puzzle is not available.");
            }

            if (!keepRegeneratedLevel)
            {
                CurrentOverlay = save.HasOverlay && save.Overlay != null
                    ? save.Overlay
                    : new ModifierOverlayData();

                var effectiveMods = new List<BossModifierId>(CurrentLevelConfig.ActiveModifiers);
                var missing = new List<BossModifierId>();
                for (var i = effectiveMods.Count - 1; i >= 0; i--)
                {
                    if (IsModifierPresentInOverlay(effectiveMods[i], CurrentOverlay)) continue;
                    missing.Insert(0, effectiveMods[i]);
                    effectiveMods.RemoveAt(i);
                }

                ConstraintEngine = new SudokuConstraintEngine();
                var rules = ModifierFactory.BuildRules(effectiveMods);
                for (var i = 0; i < rules.Count; i++)
                    ConstraintEngine.RegisterRule(rules[i]);

                LastGenerationMetrics = new PuzzleGenerationMetrics();
                LastGenerationMissingModifiers = missing;
            }

            if (save.HasLevelCounters)
            {
                CurrentLevelState.Mistakes = save.Mistakes;
                CurrentLevelState.HpLost = save.HpLost;
                CurrentLevelState.CorrectPlacements = save.CorrectPlacements;
                CurrentLevelState.PencilMarksUsed = save.PencilMarksUsed;
                CurrentLevelState.ItemsUsedThisLevel = save.ItemsUsedThisLevel;
                CurrentLevelState.PerfectSoFar = save.PerfectSoFar;
                CurrentLevelState.NoPencilUsed = save.NoPencilUsed;
            }
            if (save.FogPendingCells != null)
            {
                for (var i = 0; i < save.FogPendingCells.Count; i++)
                    CurrentLevelState.FogPendingCells.Add((save.FogPendingCells[i] / 100, save.FogPendingCells[i] % 100));
            }

            // [REQ: PRESSURE-SAVE-002] Pressure re-initialises from the level seed on resume.
            var pressureRng = new System.Random(CurrentLevelConfig.Seed ^ 0x50A4B3C2);
            InitializePressureMechanics(CurrentLevelConfig, CurrentBoard, CurrentLevelState, pressureRng);
        }

        // Older saves carry only size, stars and the boss flag. Rebuild a playable config from the
        // run state; the modifiers that need overlay geometry cannot be recovered.
        private LevelConfig BuildLegacyResumeConfig(PuzzleSaveState save)
        {
            var stars = Math.Max(1, Math.Min(7, save.Stars));
            var config = new LevelConfig
            {
                BoardSize = save.BoardSize,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                IsBoss = save.IsBoss,
                Seed = State.Seed + State.CurrentFloor * 997 + State.CurrentNodeIndex * 13,
                Intensity = save.IsBoss ? GetBossIntensity() : BossService.IntensityForRunNumber(State.RunNumber),
                Difficulty = MapDifficulty(State.CurrentFloor, stars)
            };

            var node = GetCurrentNode();
            if (node != null)
            {
                config.IsElite = node.Type == NodeType.ElitePuzzle;
                config.IsPreBoss = node.Type == NodeType.PreBoss;
            }

            if (State.ActiveFloorModifiers != null)
                config.ActiveModifiers.AddRange(State.ActiveFloorModifiers);
            if (save.IsBoss && State.ChosenBossModifiers != null)
            {
                for (var i = 0; i < State.ChosenBossModifiers.Count; i++)
                    if (!config.ActiveModifiers.Contains(State.ChosenBossModifiers[i]))
                        config.ActiveModifiers.Add(State.ChosenBossModifiers[i]);
            }
            return config;
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

            // 20% upward variance. The higher star level is drawn with the Harmony star weights (spec §4.6, GAP-008)
            // and capped on floors 1–2 at one star above the default (GDS §15 "low stars in first runs", GAP-033).
            var maxStars = MaxStarsForFloor(floor, isElite);
            if (rng.NextDouble() <= 0.20 && defaultStars < maxStars)
            {
                var stars = HarmonyDifficultyService.PickStarsAbove(Harmony, defaultStars, maxStars, rng);
                UnityEngine.Debug.Log($"[Difficulty] Floor {floor}: variant {stars}★ (base {defaultStars}★, 20% roll)");
                return stars;
            }

            UnityEngine.Debug.Log($"[Difficulty] Floor {floor}: default {defaultStars}★");
            return defaultStars;
        }

        // [REQ: REQ-RUN-106] Highest star level a non-boss puzzle may have on a floor (0-based index):
        // floors 1–2 at most one star above the floor default (elite included), later floors up to 6★.
        private static int MaxStarsForFloor(int floor, bool isElite)
        {
            var fi = Math.Clamp(floor, 0, FloorDefaultStars.Length - 1);
            if (fi >= 2) return 6;
            var defaultStars = isElite ? Math.Min(FloorDefaultStars[fi] + 1, 6) : FloorDefaultStars[fi];
            return Math.Min(6, defaultStars + 1);
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

        // [REQ: BOSS-MOD-004] One presence check for generation and resume (GAP-047: this used to be a second copy).
        private static bool HasAllModifiersPresent(List<BossModifierId> modifiers, ModifierOverlayData overlay) =>
            PuzzleGenerationService.HasAllModifiersPresent(modifiers, overlay);

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
        IsPresolved,
        Unchanged
    }
}
