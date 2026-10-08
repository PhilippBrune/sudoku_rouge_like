using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// Level configuration, puzzle generation (sync, async, pre-bake) and the never-block
    /// fallback chain (GAP-003/GAP-005). Split out of RunDirector.cs per docs/controller-extraction-plan.md.
    /// </summary>
    public sealed partial class RunDirector
    {
        // ── Level Generation ──

        // State.HarmonyConfig is rebuilt after load; a default struct (old saves, tests) falls back to the level's table.
        private HarmonyConfig Harmony =>
            State.HarmonyConfig.StarWeights != null ? State.HarmonyConfig : HarmonyDifficultyService.BuildConfig(State.HarmonyLevel);

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
            // [REQ: REQ-HARM-100] Harmony grid weights choose the size within the floor's range (GAP-008).
            var size = requiredMinSize <= maxSize
                ? HarmonyDifficultyService.PickBoardSize(Harmony, requiredMinSize, maxSize, rng)
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

            // [REQ: REQ-BOSS-108] From Floor 3 every Garden Run boss carries one debuff (GAP-026).
            if (isBoss && GetBossDebuffForCurrentFloor() is { } debuff && !config.ActiveModifiers.Contains(debuff))
                config.ActiveModifiers.Add(debuff);

            _varianceService.ApplyVariance(config, State.Depth, floor,
                State.AllowIrregularPuzzles ? 3 : 1, rng);

            // Mid-run adaptation: nudge difficulty based on most recent puzzle performance (non-boss only)
            if (!isBoss)
            {
                _adaptationService.AdaptDifficulty(config, State, CurrentLevelState);
                // The star cap of early floors also covers adaptation, and the clue density follows the final stars.
                config.Stars = Math.Min(config.Stars, MaxStarsForFloor(floor, isElite));
                config.MissingPercent = StarDensityService.MissingPercentForStars(config.Stars);
                config.Difficulty = MapDifficulty(floor, config.Stars);
            }

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
            // [REQ: HARMONY-FLAG-005] H9+: the cursed +50 % gold/XP bonus is suppressed (GAP-008).
            var bonus = Harmony.CursedBonusGoldDisabled ? 1f : 1.5f;
            config.CursedGoldMult = bonus;
            config.CursedXpMult  = bonus;

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

        // [REQ: REQ-PATH-101] [REQ: REQ-PATH-102] One config per map node, built once and used for both the tile
        // preview and generation, so the board shown on the map is the board that is played (GAP-013).
        // Boss boards follow the floor's size range (SPEC-CONF-028); there is no 8×8 minimum any more.
        // [REQ: MAP-ROUTE-002] Risk tiles get +1 star and, within the floor's size range, +1 board size.
        public LevelConfig BuildNodeLevelConfig(RunNode node, int nodeIndex)
        {
            if (node == null) return BuildLevelConfig(false, false, nodeIndex);
            var isBoss = node.Type == NodeType.Boss;
            var config = BuildLevelConfig(isBoss, node.Type == NodeType.ElitePuzzle, nodeIndex, node.Type == NodeType.PreBoss);
            if (!isBoss && node.Route == RouteType.RiskRoute)
            {
                config.Difficulty = (DifficultyTier)Math.Clamp((int)config.Difficulty + 1, (int)DifficultyTier.Diff1, (int)DifficultyTier.Diff5);
                config.Stars = Math.Min(Math.Max(config.Stars, 5), config.Stars + 1); // +1 up to 5★; a rolled 6★ stays 6★
                config.MissingPercent = StarDensityService.MissingPercentForStars(config.Stars);
                var floorMax = FloorThemeData.GetMaxBoardSize(State.CurrentFloor);
                config.BoardSize = Math.Min(Math.Max(config.BoardSize, Math.Min(config.BoardSize + 1, floorMax)), 9);
                // A structural modifier cannot run on the irregular 7×7 layouts; keep the bump consistent with that rule.
                if (config.BoardSize == 7 && HasBoardShapingModifier(config.ActiveModifiers))
                    config.BoardSize = 8;
                EnsureBoardSizeSupportsActiveModifiers(config);
            }
            return config;
        }

        /// <summary>
        /// The saved node configs of the current floor, or null when none were saved for this floor or the graph changed.
        /// </summary>
        public List<LevelConfig> GetSavedNodeConfigs()
        {
            if (State == null || CurrentFloorGraph == null || State.NodeConfigs == null) return null;
            if (State.NodeConfigsFloor != State.CurrentFloor || State.NodeConfigs.Count != CurrentFloorGraph.Count) return null;
            return State.NodeConfigs.Any(c => c == null || c.BoardSize <= 0) ? null : State.NodeConfigs;
        }

        /// <summary>Saves the node configs built at floor entry (see <see cref="GetSavedNodeConfigs"/>).</summary>
        public void SaveNodeConfigs(IList<LevelConfig> configs)
        {
            if (State == null || configs == null) return;
            State.NodeConfigsFloor = State.CurrentFloor;
            State.NodeConfigs = configs.Select(c => c?.Clone()).ToList();
        }

        /// <summary>
        /// Re-applies the modifier rules to a config whose modifier list changed after it was built (the boss gate
        /// adds the chosen modifiers): drops unsupported or blocked entries and raises the size where a modifier needs it.
        /// </summary>
        public void NormalizeLevelConfig(LevelConfig config)
        {
            SanitizeGeneratedLevelConfig(config);
            EnsureBoardSizeSupportsActiveModifiers(config);
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
                config.ActiveModifiers.Contains(BossModifierId.EntropyGlobal),
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

        // [REQ: REQ-SUD-101] [REQ: REQ-PERF-100] Board generation never blocks progress (GAP-003).
        // Fallback order, each step deterministic for the seed:
        //   1. the config as rolled;
        //   2. the rectangular region variant (all modifiers kept) — irregular templates are the slow case;
        //   3. without board-shaping modifiers the player did not choose (floor modifiers);
        //   4. without any board-shaping modifier (last resort, logged; reported as missing modifiers).
        // [REQ: REQ-BOSS-101] Player-chosen boss modifiers are only dropped in step 4 (GAP-005).
        private static SudokuBoard CreateBoardWithFallbacks(
            LevelConfig config,
            IList<BossModifierId> mandatoryModifiers,
            int seed,
            PuzzleGenerationSettings settings,
            CancellationToken cancellationToken,
            PuzzleGenerationMetrics aggregateMetrics,
            out LevelConfig effectiveConfig,
            out List<BossModifierId> droppedModifiers)
        {
            effectiveConfig = config;
            droppedModifiers = new List<BossModifierId>();
            settings ??= PuzzleGenerationSettings.SynchronousDefault;

            // Restarts happen inside the generator now, so a couple of seeds per step is enough.
            var firstStep = new PuzzleGenerationSettings
            {
                BoardRetries = Math.Min(Math.Max(1, settings.BoardRetries), 2),
                OverlaySeedAttempts = settings.OverlaySeedAttempts,
                TimeBudgetMs = settings.TimeBudgetMs,
                ParallelOverlaySeeds = settings.ParallelOverlaySeeds
            };
            var board = CreatePuzzleForConfigWithBudgetedRetries(
                config, seed, firstStep, cancellationToken, aggregateMetrics, logAttemptWarnings: false);
            if (board != null) return board;

            var fallbackStep = new PuzzleGenerationSettings
            {
                BoardRetries = 1,
                OverlaySeedAttempts = settings.OverlaySeedAttempts,
                TimeBudgetMs = settings.TimeBudgetMs,
                ParallelOverlaySeeds = settings.ParallelOverlaySeeds
            };
            foreach (var (stepConfig, stepDropped, label) in BuildBoardFallbackSteps(config, mandatoryModifiers))
            {
                var stepMetrics = new PuzzleGenerationMetrics();
                board = CreatePuzzleForConfigWithBudgetedRetries(
                    stepConfig, seed, fallbackStep, cancellationToken, stepMetrics, logAttemptWarnings: false);
                IncludeInitialBoardMetrics(aggregateMetrics, stepMetrics);
                if (board == null) continue;

                effectiveConfig = stepConfig;
                droppedModifiers = stepDropped;
                aggregateMetrics.LastError = $"Board generation fallback used: {label}.";
                Debug.LogWarning(
                    $"[RunDirector] Board generation fallback ({label}) for size={config.BoardSize}, " +
                    $"region={config.RegionVariant}, mods=[{string.Join(", ", config.ActiveModifiers)}]" +
                    (stepDropped.Count > 0 ? $"; dropped=[{string.Join(", ", stepDropped)}]" : string.Empty));
                return board;
            }

            return null;
        }

        private static IEnumerable<(LevelConfig config, List<BossModifierId> dropped, string label)> BuildBoardFallbackSteps(
            LevelConfig source, IList<BossModifierId> mandatory)
        {
            var current = source;

            // Rectangular layouts exist for 6×6, 8×8 and 9×9; 5×5 and 7×7 only have templates.
            if (source.RegionVariant >= 2 && (source.BoardSize == 6 || source.BoardSize == 8 || source.BoardSize == 9))
            {
                current = source.Clone();
                current.RegionVariant = source.RegionVariant % 2;
                yield return (current, new List<BossModifierId>(), "rectangular regions");
            }

            var optional = new List<BossModifierId>();
            var required = new List<BossModifierId>();
            for (var i = 0; i < current.ActiveModifiers.Count; i++)
            {
                var mod = current.ActiveModifiers[i];
                if (!IsBoardShapingModifier(mod)) continue;
                if (mandatory != null && mandatory.Contains(mod)) required.Add(mod);
                else optional.Add(mod);
            }

            if (optional.Count > 0)
            {
                var withoutOptional = current.Clone();
                withoutOptional.ActiveModifiers.RemoveAll(optional.Contains);
                if (withoutOptional.ActiveModifiers.Count == 0)
                    withoutOptional.RequireUniqueModifierSolution = false;
                yield return (withoutOptional, new List<BossModifierId>(optional), "without floor board-shaping modifiers");
                current = withoutOptional;
            }

            if (required.Count > 0)
            {
                var withoutAny = current.Clone();
                withoutAny.ActiveModifiers.RemoveAll(required.Contains);
                if (withoutAny.ActiveModifiers.Count == 0)
                    withoutAny.RequireUniqueModifierSolution = false;
                var dropped = new List<BossModifierId>(optional);
                dropped.AddRange(required);
                yield return (withoutAny, dropped, "without any board-shaping modifier");
            }
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

            // [REQ: REQ-SUD-101] Global structural rules (anti-king/knight, non-consecutive, entropy, …)
            // have no generatable solutions within budget on the irregular templates: every measured
            // seed timed out (GAP-003 benchmark). Such levels use the rectangular layout instead.
            if (RequiresRectangularRegions(config.ActiveModifiers) && config.RegionVariant >= 2
                && (config.BoardSize == 6 || config.BoardSize == 8 || config.BoardSize == 9
                    || requiredSize > config.BoardSize))
            {
                var oldVariant = config.RegionVariant;
                config.RegionVariant %= 2;
                Debug.Log(
                    $"[RunDirector] Using rectangular regions (variant {oldVariant} -> {config.RegionVariant}) " +
                    $"for global structural modifiers: {string.Join(", ", config.ActiveModifiers)}");
            }

            if (config.BoardSize >= requiredSize) return;

            var oldSize = config.BoardSize;
            config.BoardSize = Math.Min(9, requiredSize);
            Debug.LogWarning(
                $"[RunDirector] Raised board size from {oldSize} to {config.BoardSize} " +
                $"for active modifiers: {string.Join(", ", config.ActiveModifiers)}");
        }

        private static bool RequiresRectangularRegions(IList<BossModifierId> modifiers)
        {
            if (modifiers == null) return false;
            for (var i = 0; i < modifiers.Count; i++)
            {
                if (IsBoardShapingModifier(modifiers[i]) || modifiers[i] == BossModifierId.EntropyGlobal)
                    return true;
            }
            return false;
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

        // [REQ: BOSS-MOD-002] One minimum-size table for eligibility and size gating: BossService.ModifierData
        // (SPEC-CONF-013). This file used to keep a second, diverging copy.
        private static int GetMinimumBoardSizeForModifier(BossModifierId mod) => BossService.GetMinimumBoardSize(mod);

        private static bool IsBoardShapingModifier(BossModifierId mod) => BossService.IsBoardShapingModifier(mod);

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
            PrepareRunStateForNewPuzzle();

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
            _pendingOriginalSeed = ApplyGenerationRetrySalt(config);
            Debug.Log($"[RunDirector] StartLevelAsync: ActiveModifiers={config.ActiveModifiers.Count} [{string.Join(", ", config.ActiveModifiers)}]");
            _pendingLevelGenerationCancel?.Cancel();
            _pendingLevelGenerationCancel = new CancellationTokenSource();
            var cancellationToken = _pendingLevelGenerationCancel.Token;
            CurrentLevelConfig = config;
            CurrentLevelState  = new LevelState();
            State.HpAtFloorStart = State.CurrentHP;
            PrepareRunStateForNewPuzzle();
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

            var settings  = SelectAsyncGenerationSettings(config);
            var mandatory = GetMandatoryModifiers(config);

            _pendingOverlayTask = Task.Run(() =>
            {
                try
                {
                    // SudokuGenerator uses no Unity APIs — safe to call from a background thread.
                    var content = GenerateLevelContent(
                        config, mandatory, capturedPreBake, settings, cancellationToken, out var effectiveConfig);
                    if (content.IsSuccess && !ReferenceEquals(effectiveConfig, config))
                        _pendingLevelConfig = effectiveConfig;
                    _asyncResult = content;
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
                if (_asyncResult.FailureReason != AsyncLevelFailureReason.Cancelled)
                    RecordGenerationFailure(_pendingOriginalSeed);
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

            var effectiveModsAsync = config.ActiveModifiers;
            if (config.ActiveModifiers.Count > 0 && !HasAllModifiersPresent(config.ActiveModifiers, CurrentOverlay))
            {
                // Async overlay did not cover all modifiers — build a copy that excludes those
                // without geometry. Do NOT mutate config.ActiveModifiers so the HUD info-box
                // can still display what the player selected.
                effectiveModsAsync = new System.Collections.Generic.List<BossModifierId>(config.ActiveModifiers);
                for (var i = effectiveModsAsync.Count - 1; i >= 0; i--)
                {
                    if (!IsModifierPresentInOverlay(effectiveModsAsync[i], CurrentOverlay))
                    {
                        Debug.LogWarning($"[RunDirector] TryCompleteAsyncLevel: dropping modifier {effectiveModsAsync[i]} from engine — no overlay geometry in async result.");
                        effectiveModsAsync.RemoveAt(i);
                    }
                }
            }

            if (effectiveModsAsync.Count > 0)
                ClearOverlayCellsFromGivenMask();

            ConstraintEngine = new SudokuConstraintEngine();
            var rules = ModifierFactory.BuildRules(effectiveModsAsync);
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

        private const int MandatoryOverlayPasses = 3;

        // Board (with fallbacks) and overlay for one level. Shared by StartLevel and StartLevelAsync so
        // both paths get the same never-block behaviour. Safe off the main thread.
        private AsyncLevelResult GenerateLevelContent(
            LevelConfig config,
            IList<BossModifierId> mandatoryModifiers,
            SudokuBoard preBakedBoard,
            PuzzleGenerationSettings settings,
            CancellationToken cancellationToken,
            out LevelConfig effectiveConfig)
        {
            effectiveConfig = config;
            var levelRng = new System.Random(config.Seed ^ 0x1A2B3C4D);
            var boardMetrics = new PuzzleGenerationMetrics();
            var dropped = new List<BossModifierId>();

            var board = preBakedBoard;
            if (board == null)
            {
                board = CreateBoardWithFallbacks(
                    config, mandatoryModifiers, config.Seed, settings, cancellationToken, boardMetrics,
                    out effectiveConfig, out dropped);
                if (board == null)
                {
                    return AsyncLevelResult.Failure(
                        boardMetrics.BudgetExceeded
                            ? AsyncLevelFailureReason.BudgetExceeded
                            : AsyncLevelFailureReason.GenerationFailed,
                        boardMetrics);
                }
            }

            var retryBoardSettings = new PuzzleGenerationSettings
            {
                BoardRetries = 1,
                OverlaySeedAttempts = settings.OverlaySeedAttempts,
                TimeBudgetMs = settings.TimeBudgetMs,
                ParallelOverlaySeeds = settings.ParallelOverlaySeeds
            };

            PuzzleGenerationResult result = null;
            for (var pass = 0; pass < MandatoryOverlayPasses; pass++)
            {
                result = _puzzleGenerationService.Generate(
                    effectiveConfig, board, levelRng.Next(), CreatePuzzleForConfig, settings, cancellationToken);
                if (!result.IsSuccess || !MissesAny(result.MissingModifiers, mandatoryModifiers))
                    break;

                // [REQ: REQ-BOSS-101] A chosen modifier got no geometry on this board (e.g. no valid
                // thermo path); try another board instead of starting the boss without it (GAP-005).
                var retryBoard = CreatePuzzleForConfigWithBudgetedRetries(
                    effectiveConfig, levelRng.Next(), retryBoardSettings, cancellationToken, boardMetrics,
                    logAttemptWarnings: false);
                if (retryBoard == null)
                    break;
                board = retryBoard;
            }

            if (preBakedBoard == null)
                IncludeInitialBoardMetrics(result.Metrics, boardMetrics);

            return result.IsSuccess
                ? AsyncLevelResult.Success(result, dropped)
                : AsyncLevelResult.Failure(MapFailureReason(result.FailureReason), result.Metrics);
        }

        private static bool MissesAny(IReadOnlyList<BossModifierId> missing, IList<BossModifierId> required)
        {
            if (missing == null || required == null) return false;
            for (var i = 0; i < missing.Count; i++)
                if (required.Contains(missing[i])) return true;
            return false;
        }

        // Boss modifiers the player picked at the gate; these must not be dropped silently.
        private List<BossModifierId> GetMandatoryModifiers(LevelConfig config)
        {
            var mandatory = new List<BossModifierId>();
            if (config == null || !config.IsBoss || State?.ChosenBossModifiers == null) return mandatory;
            for (var i = 0; i < State.ChosenBossModifiers.Count; i++)
                if (config.ActiveModifiers.Contains(State.ChosenBossModifiers[i]))
                    mandatory.Add(State.ChosenBossModifiers[i]);
            return mandatory;
        }

        // [REQ: REQ-SUD-101] A level whose generation failed is retried with a salted seed, so the
        // player is never stuck on a deterministic failure (GAP-003). Failures persist in RunState.
        // Returns the unsalted seed, which is the key failures are recorded under.
        private int ApplyGenerationRetrySalt(LevelConfig config)
        {
            var originalSeed = config.Seed;
            var failures = State?.FailedGenerationSeeds;
            if (failures == null || failures.Count == 0) return originalSeed;

            var attempts = 0;
            for (var i = 0; i < failures.Count; i++)
                if (failures[i] == originalSeed) attempts++;
            if (attempts == 0) return originalSeed;

            config.Seed = unchecked(originalSeed + attempts * 0x5BD1E995);
            Debug.Log($"[RunDirector] Retrying level generation with salted seed (attempt {attempts + 1}, seed {originalSeed} -> {config.Seed}).");
            return originalSeed;
        }

        private void RecordGenerationFailure(int originalSeed)
        {
            if (State == null) return;
            State.FailedGenerationSeeds ??= new List<int>();
            State.FailedGenerationSeeds.Add(originalSeed);
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

            var originalSeed = ApplyGenerationRetrySalt(config);
            CurrentLevelConfig = config;
            CurrentLevelState = new LevelState();
            State.HpAtFloorStart = State.CurrentHP;
            PrepareRunStateForNewPuzzle();
            CurrentBoard = null;

            var settings = PuzzleGenerationSettings.SynchronousDefault;

            // Use pre-baked board when seed, size, and structural mods all match.
            SudokuBoard preBaked = null;
            if (config.IsBoss && _preBakedBossBoard != null
                && _preBakedBossConfig != null
                && _preBakedBossConfig.Seed == config.Seed
                && _preBakedBossConfig.BoardSize == config.BoardSize
                && StructuralModsMatch(config.ActiveModifiers, _preBakedBossConfig.ActiveModifiers))
            {
                preBaked = _preBakedBossBoard;
            }

            _preBakedBossBoard = null;
            _preBakedBossConfig = null;

            AsyncLevelResult content;
            LevelConfig effectiveConfig;
            try
            {
                content = GenerateLevelContent(
                    config, GetMandatoryModifiers(config), preBaked, settings, CancellationToken.None, out effectiveConfig);
            }
            catch (TimeoutException ex)
            {
                Debug.LogError($"[RunDirector] StartLevel: board generation timed out for seed={config.Seed}, size={config.BoardSize}: {ex.Message}");
                LogGenerationFailure(
                    "StartLevel",
                    config,
                    AsyncLevelFailureReason.BudgetExceeded,
                    new PuzzleGenerationMetrics { BudgetExceeded = true, LastError = ex.Message });
                RecordGenerationFailure(originalSeed);
                CurrentBoard = null;
                CurrentOverlay = new ModifierOverlayData();
                return;
            }

            LastGenerationMetrics = content.Metrics;
            LastGenerationMissingModifiers = content.MissingModifiers ?? new List<BossModifierId>();

            if (!content.IsSuccess)
            {
                Debug.LogError($"[RunDirector] StartLevel: generation failed ({content.FailureReason}). Aborting.");
                LogGenerationFailure("StartLevel", config, content.FailureReason, content.Metrics);
                RecordGenerationFailure(originalSeed);
                CurrentBoard = null;
                CurrentOverlay = new ModifierOverlayData();
                return;
            }

            if (!ReferenceEquals(effectiveConfig, config))
            {
                config = effectiveConfig;
                CurrentLevelConfig = config;
            }
            CurrentBoard = content.Board;
            CurrentOverlay = content.Overlay;

            // effectiveMods = modifiers that actually have overlay geometry.
            // We do NOT mutate config.ActiveModifiers so the HUD info-box can still display
            // whatever the player selected (even if the overlay was incomplete).
            var effectiveMods = config.ActiveModifiers;
            if (config.ActiveModifiers.Count > 0 && !HasAllModifiersPresent(config.ActiveModifiers, CurrentOverlay))
            {
                // Overlay exhausted — build a copy that excludes modifiers with no geometry so
                // the constraint engine never registers rules against non-existent overlay data.
                effectiveMods = new System.Collections.Generic.List<BossModifierId>(config.ActiveModifiers);
                for (var i = effectiveMods.Count - 1; i >= 0; i--)
                {
                    if (!IsModifierPresentInOverlay(effectiveMods[i], CurrentOverlay))
                    {
                        Debug.LogWarning($"[RunDirector] StartLevel: dropping modifier {effectiveMods[i]} from engine — no overlay geometry after all retries.");
                        effectiveMods.RemoveAt(i);
                    }
                }
            }

            if (effectiveMods.Count > 0)
                ClearOverlayCellsFromGivenMask();

            // Build constraint engine
            ConstraintEngine = new SudokuConstraintEngine();
            var rules = ModifierFactory.BuildRules(effectiveMods);
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
    }
}
