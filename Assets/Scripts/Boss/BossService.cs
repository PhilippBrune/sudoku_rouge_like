using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Boss
{
    public sealed class BossService
    {
        private Random _random;

        /// <summary>[REQ: REQ-RUN-103] Starts a fresh random stream for one decision. RunDirector seeds it from the
        /// run seed and the player's position, so a roll is the same before and after Save &amp; Quit (GAP-018).</summary>
        public void ReseedForDecision(int decisionSeed) => _random = new Random(decisionSeed);

        // MinBoardSize: 0=any, 5=short-line/parity ok on 5×5, 6=needs ≥6 cells of geometry,
        //              7=cage-based needs ≥7, 9=entropy requires full digit range 1-9
        // [REQ: BOSS-MOD-002] ModifierData: tier/impact/minBoardSize per modifier — used for eligibility filtering, difficulty scaling, and board-size gating
        private static readonly Dictionary<BossModifierId, (int Tier, float Impact, int MinBoardSize)> ModifierData = new()
        {
            [BossModifierId.ParityLines]     = (1, 0.15f, 5),
            [BossModifierId.DifferenceKropki] = (1, 0.20f, 0),
            [BossModifierId.Nonconsecutive]  = (1, 0.25f, 6),
            [BossModifierId.DutchWhispers]   = (2, 0.30f, 6),
            [BossModifierId.RenbanLines]     = (2, 0.35f, 5),
            [BossModifierId.RatioKropki]     = (2, 0.40f, 0),
            [BossModifierId.Palindrome]      = (2, 0.30f, 6),
            [BossModifierId.Antiknight]      = (2, 0.35f, 6),
            [BossModifierId.EvenOdd]         = (2, 0.25f, 0),
            [BossModifierId.KillerCages]     = (3, 0.50f, 7),
            [BossModifierId.ArrowSums]       = (3, 0.55f, 6),
            [BossModifierId.Thermo]          = (3, 0.45f, 6),
            [BossModifierId.BetweenLines]    = (3, 0.45f, 6),
            [BossModifierId.FogOfWar]        = (4, 0.60f, 0),
            [BossModifierId.GermanWhispers]  = (5, 0.75f, 7),

            // Extended pool (IDs 15-29)
            [BossModifierId.Antiking]          = (2, 0.30f, 6),
            [BossModifierId.AntiBishop]        = (3, 0.45f, 6), // diagonal range 2 (SPEC-CONF-038)
            [BossModifierId.NonconsecDiagonal] = (2, 0.28f, 6),
            [BossModifierId.DistanceGe2]       = (2, 0.32f, 6),
            [BossModifierId.EntropyGlobal]     = (4, 0.60f, 9),
            [BossModifierId.ModularRegions]    = (3, 0.40f, 7),
            [BossModifierId.ConsecutiveLine]   = (2, 0.35f, 6),
            [BossModifierId.SlowThermo]        = (2, 0.30f, 6),
            [BossModifierId.UniqueSetLine]     = (2, 0.30f, 5),
            [BossModifierId.FullKropki]        = (3, 0.50f, 6),
            [BossModifierId.SumKropki]         = (2, 0.35f, 0),
            [BossModifierId.GreaterLessThan]   = (2, 0.30f, 5),
            [BossModifierId.XVPairs]           = (2, 0.30f, 0),
            [BossModifierId.PrimeCells]        = (1, 0.20f, 0),
            [BossModifierId.FortressCells]     = (2, 0.35f, 6),

            // Boss debuffs (IDs 94–102) — reactive penalties on wrong placement
            // [REQ: DEBUFF-HOOK-011] All debuff entries registered here; IDs ≥94 excluded from BuildEligiblePool
            [BossModifierId.RowWipe]       = (3, 0.55f, 0), // [REQ: DEBUFF-DEF-001]
            [BossModifierId.ColWipe]       = (3, 0.55f, 0), // [REQ: DEBUFF-DEF-002]
            [BossModifierId.DoublePenalty] = (2, 0.40f, 0), // [REQ: DEBUFF-DEF-012]
            [BossModifierId.CellLock]      = (2, 0.45f, 0), // [REQ: DEBUFF-DEF-018]
            [BossModifierId.PencilBlind]   = (3, 0.50f, 0), // [REQ: DEBUFF-DEF-007]
            [BossModifierId.BoxWipe]       = (3, 0.55f, 0), // [REQ: DEBUFF-DEF-003]
            [BossModifierId.CrossWipe]     = (4, 0.70f, 0), // [REQ: DEBUFF-DEF-004]
            [BossModifierId.PencilDrain]   = (2, 0.35f, 0), // [REQ: DEBUFF-DEF-013]
            [BossModifierId.GoldFine]      = (2, 0.30f, 0), // [REQ: DEBUFF-DEF-014]

            // Pressure mechanics (IDs 103–106) — in-puzzle urgency timers (rolled via dedicated path)
            // [REQ: PRESSURE-ROLL-004] registered here so BuildEligiblePool can exclude them; actual rolling in RunDirector.RollPressureMechanic
            [BossModifierId.CountdownFill]   = (2, 0.35f, 0),
            [BossModifierId.HauntedCell]     = (2, 0.30f, 0),
            [BossModifierId.CrumblingRegion] = (3, 0.50f, 0),
            [BossModifierId.PressureWave]    = (4, 0.60f, 0), // [REQ: PRESSURE-ROLL-005] floor 3+ only

            // [REQ: DEBUFF-HOOK-021] Boss debuffs Batch 2 (IDs 107–111) registered in ModifierData; all excluded from player pool via ≥94 guard in BuildEligiblePool
            [BossModifierId.RadiusWipe]     = (3, 0.50f, 0), // [REQ: DEBUFF-DEF-005]
            [BossModifierId.PencilScramble] = (4, 0.65f, 0), // [REQ: DEBUFF-DEF-008]
            [BossModifierId.GivenReveal]    = (2, 0.40f, 0), // [REQ: DEBUFF-DEF-017]
            [BossModifierId.RevealCost]     = (2, 0.30f, 0), // [REQ: DEBUFF-DEF-015]
            [BossModifierId.CellBlur]       = (2, 0.35f, 0)  // [REQ: DEBUFF-DEF-011]
        };

        public BossService(int seed)
        {
            _random = new Random(seed);
        }

        public float GetImpact(BossModifierId id)
        {
            return ModifierData.TryGetValue(id, out var data) ? data.Impact : 0.3f;
        }

        public int GetTier(BossModifierId id)
        {
            return ModifierData.TryGetValue(id, out var data) ? data.Tier : 1;
        }

        public static int GetMinimumBoardSize(BossModifierId modifier)
        {
            return ModifierData.TryGetValue(modifier, out var data)
                ? Math.Max(5, data.MinBoardSize)
                : 5;
        }

        /// <summary>
        /// Returns true if the modifier is a fully implemented player-eligible modifier
        /// (i.e. registered in ModifierData and not a debuff or pressure mechanic).
        /// </summary>
        public static bool IsImplementedModifier(BossModifierId modifier) =>
            (int)modifier < 94 && ModifierData.ContainsKey(modifier);

        public static bool IsBoardShapingModifier(BossModifierId modifier)
        {
            return modifier == BossModifierId.Nonconsecutive
                || modifier == BossModifierId.Antiknight
                || modifier == BossModifierId.NonconsecDiagonal
                || modifier == BossModifierId.AntiBishop
                || modifier == BossModifierId.Antiking
                || modifier == BossModifierId.DistanceGe2;
        }

        /// <summary>
        /// True when puzzle generation can deliver the modifier. Excludes enum values with no
        /// rule or geometry (the "future library" IDs 30–93), which Endless Zen and Spirit Trials
        /// used to draw as inert modifiers (GAP-007).
        /// Debuffs and pressure mechanics stay supported: level configs carry them through their
        /// own delivery paths.
        /// </summary>
        // [REQ: REQ-MODE-101] Modes and runs only receive modifiers that have a rule and generate
        public static bool IsSupportedForGeneratedRun(BossModifierId modifier)
        {
            // Anti-Bishop is supported again since its rule covers two diagonal steps (SPEC-CONF-038).
            return ModifierData.ContainsKey(modifier);
        }

        /// <summary>
        /// Player modifiers (IDs below 94) that generation can deliver on a board of the given size,
        /// in ascending ID order so seeded draws are deterministic. Same eligibility as the run pools.
        /// </summary>
        // [REQ: REQ-MODE-101] Endless Zen / Spirit Trials draw from the playable modifiers only (GAP-007)
        public static List<BossModifierId> GetGeneratablePlayerModifiers(int boardSize)
        {
            var pool = new List<BossModifierId>();
            foreach (var pair in ModifierData)
            {
                if ((int)pair.Key >= 94) continue;
                // Modular Regions disabled by design decision (same as BuildEligiblePool)
                if (pair.Key == BossModifierId.ModularRegions) continue;
                if (!IsSupportedForGeneratedRun(pair.Key)) continue;
                if (pair.Value.MinBoardSize > boardSize) continue;
                pool.Add(pair.Key);
            }
            pool.Sort();
            return pool;
        }

        /// <summary>
        /// Draws up to <paramref name="count"/> distinct modifiers from <paramref name="pool"/>,
        /// skipping candidates that cannot be combined with the ones already drawn.
        /// </summary>
        public static List<BossModifierId> DrawCombinable(List<BossModifierId> pool, int count, Random random)
        {
            var candidates = new List<BossModifierId>(pool);
            var result = new List<BossModifierId>(count);
            while (result.Count < count && candidates.Count > 0)
            {
                var idx = random.Next(candidates.Count);
                var pick = candidates[idx];
                candidates.RemoveAt(idx);
                result.Add(pick);
                candidates.RemoveAll(candidate => !CanCombineModifiers(result, candidate));
            }
            return result;
        }

        /// <summary>
        /// Returns false for modifier pairs that are individually valid but are not stable
        /// together within the bounded runtime generation budget.
        /// </summary>
        public static bool CanCombineModifiers(IList<BossModifierId> selected, BossModifierId candidate)
        {
            if (selected == null) return true;
            for (var i = 0; i < selected.Count; i++)
            {
                if (IsBlockedCombination(selected[i], candidate))
                    return false;
            }
            return true;
        }

        // [REQ: BOSS-MOD-002] Pairs that cannot be generated together (documented in BossMechanicsSystem §Modifier Eligibility).
        private static readonly (BossModifierId, BossModifierId)[] BlockedPairs =
        {
            (BossModifierId.RatioKropki, BossModifierId.DistanceGe2),
            // Both constrain diagonal adjacency; together they over-constrain the board and the backtracker times out.
            (BossModifierId.DistanceGe2, BossModifierId.NonconsecDiagonal),
            // Anti-Bishop (diagonal range 2) benchmark, 2026-10-06, 8 seeds per size: with Anti-Knight no 6×6 board
            // exists and 8×8/9×9 time out; with Nonconsecutive no 6×6/8×8 board exists; with Entropy no 9×9 board exists.
            (BossModifierId.AntiBishop, BossModifierId.Antiknight),
            (BossModifierId.AntiBishop, BossModifierId.Nonconsecutive),
            (BossModifierId.AntiBishop, BossModifierId.EntropyGlobal),
        };

        private static bool IsBlockedCombination(BossModifierId first, BossModifierId second)
        {
            foreach (var (a, b) in BlockedPairs)
                if ((first == a && second == b) || (first == b && second == a))
                    return true;
            return false;
        }

        /// <summary>The pairs that are never combined, for documentation and tests.</summary>
        public static IReadOnlyList<(BossModifierId, BossModifierId)> GetBlockedPairs() => BlockedPairs;

        [System.Obsolete("Use RollBossChoices(floor, activeFloorModifiers, minBoardSize, harmonyLevel) to ensure board-size filtering.")]
        public List<BossModifierId> RollBossOptions(int count, List<BossModifierId> excludeUsed, int minBoardSize = 0)
        {
            // Delegate to BuildEligiblePool so board-size restrictions are always applied.
            var pool = BuildEligiblePool(excludeUsed, minBoardSize);
            return DrawDistinct(pool, count);
        }

        /// <summary>
        /// Roll floor modifiers for the given floor. Floor N has N−1 modifiers (Floor 1 = 0).
        /// [HARMONY-BOSS-001] <paramref name="harmonyBonus"/> extra modifiers are added on top
        /// (per <see cref="HarmonyDifficultyService.GetBonusFloorModifiers"/>).
        /// Excludes modifiers that require board sizes larger than the floor's minimum.
        /// </summary>
        // [REQ: BOSS-FLOOR-001] Floor modifier counts by floor index (0-based): 0,1,1,1,2
        private static readonly int[] BaseFloorModifierCounts = { 0, 1, 1, 1, 2 };
        public List<BossModifierId> RollFloorModifiers(int floor, int minBoardSize, int harmonyLevel)
        {
            var harmonyBonus = HarmonyDifficultyService.GetBonusFloorModifiers(harmonyLevel, floor);
            var baseCount = floor >= 0 && floor < BaseFloorModifierCounts.Length
                ? BaseFloorModifierCounts[floor] : floor;
            var count = baseCount + harmonyBonus;
            if (count == 0) return new List<BossModifierId>();

            var pool = BuildEligiblePool(null, minBoardSize);
            return DrawDistinct(pool, count);
        }

        /// <summary>
        /// Roll boss modifier choices. Excludes active floor modifiers from the pool.
        /// Floor N (1-based) shows N+1 options and the player picks N, so exactly one option is excluded
        /// (REQ-BOSS-100 as decided in SPEC-CONF-002: Floor 1 = 1 of 2 … Floor 5 = 5 of 6).
        /// </summary>
        public List<BossModifierId> RollBossChoices(int floor, List<BossModifierId> activeFloorModifiers, int minBoardSize, int harmonyLevel)
        {
            // [REQ: HARMONY-BOSS-002] The gate bonus is BossOptionsBonus, not the floor-modifier bonus (GAP-008).
            var harmonyBonus = HarmonyDifficultyService.GetBossOptionsBonus(harmonyLevel);
            GetBossModifierCounts(floor, out var shown, out _, harmonyBonus);
            var pool = BuildEligiblePool(activeFloorModifiers, minBoardSize);
            return DrawDistinct(pool, shown);
        }

        // [REQ: BOSS-FLOOR-002] BuildEligiblePool: boss choice pool excludes active floor modifiers (passed as exclude); prevents duplicate modifier stacking
        private List<BossModifierId> BuildEligiblePool(List<BossModifierId> exclude, int minBoardSize)
        {
            var pool = new List<BossModifierId>();
            foreach (var pair in ModifierData)
            {
                // [REQ: DEBUFF-HOOK-011] IDs ≥94 are debuffs/pressure — exclude from player-facing modifier rolling
                var id = (int)pair.Key;
                if (id >= 94) continue;

                // Modular Regions disabled by design decision
                if (pair.Key == BossModifierId.ModularRegions) continue;

                if (!IsSupportedForGeneratedRun(pair.Key)) continue;

                // Board-size gate: MinBoardSize=0 means any size is fine
                if (pair.Value.MinBoardSize > minBoardSize) continue;

                if (exclude != null && exclude.Contains(pair.Key))
                    continue;

                if (!CanCombineModifiers(exclude, pair.Key))
                    continue;

                pool.Add(pair.Key);
            }
            return pool;
        }

        private List<BossModifierId> DrawDistinct(List<BossModifierId> pool, int count)
        {
            var result = new List<BossModifierId>();
            var seen   = new HashSet<BossModifierId>();
            for (var i = 0; i < count && pool.Count > 0; i++)
            {
                var idx  = _random.Next(pool.Count);
                var pick = pool[idx];
                pool.RemoveAt(idx);
                if (seen.Add(pick))   // HashSet.Add returns false for duplicates
                {
                    result.Add(pick);
                    pool.RemoveAll(candidate => !CanCombineModifiers(result, candidate));
                }
            }
            return result;
        }

        /// <summary>
        /// [REQ: REQ-BOSS-100] Boss choice formula (SPEC-CONF-002): Floor N (1-based) shows N+1 options and the
        /// player picks N. There is no minimum of 3 shown / 2 picked.
        /// [HARMONY-BOSS-002] <paramref name="harmonyBonus"/> is added to both shown and picked,
        /// keeping the "one exclusion" invariant while requiring more total modifiers.
        /// </summary>
        public static void GetBossModifierCounts(int floor, out int optionsShown, out int playerChooses, int harmonyBonus = 0)
        {
            // Floor 0 (1st): pick 1 of 2; Floor 1 (2nd): pick 2 of 3; Floor 2 (3rd): pick 3 of 4; etc.
            optionsShown  = floor + 2 + harmonyBonus;
            playerChooses = floor + 1 + harmonyBonus;
        }

        public static string GetIconFolder(BossModifierId id)
        {
            var fallback = id switch
            {
                BossModifierId.RowWipe or BossModifierId.ColWipe or BossModifierId.DoublePenalty
                    or BossModifierId.CellLock or BossModifierId.PencilBlind or BossModifierId.BoxWipe
                    or BossModifierId.CrossWipe or BossModifierId.PencilDrain or BossModifierId.GoldFine
                    or BossModifierId.RadiusWipe or BossModifierId.PencilScramble or BossModifierId.GivenReveal
                    or BossModifierId.RevealCost or BossModifierId.CellBlur
                    => "debuff",
                _ => "modifier"
            };
            return fallback;
        }

        public static string GetIconName(BossModifierId id)
        {
            return id switch
            {
                BossModifierId.GermanWhispers   => "german_whisper",
                BossModifierId.DutchWhispers    => "dutch_whisper",
                BossModifierId.ParityLines      => "parity_line",
                BossModifierId.RenbanLines      => "renban_chain",
                BossModifierId.DifferenceKropki => "white_dot",
                BossModifierId.RatioKropki      => "black_dot",
                BossModifierId.KillerCages      => "killer_cage",
                BossModifierId.ArrowSums        => "arrow_sum",
                BossModifierId.FogOfWar         => "fog_cloud",
                BossModifierId.Palindrome       => "palindrome_line",
                BossModifierId.Thermo           => "thermo_line",
                BossModifierId.BetweenLines     => "between_line",
                BossModifierId.EvenOdd          => "even_odd",
                BossModifierId.Nonconsecutive   => "nonconsecutive",
                BossModifierId.Antiknight        => "antiknight",
                BossModifierId.Antiking          => "antiking",
                BossModifierId.AntiBishop        => "antibishop",
                BossModifierId.NonconsecDiagonal => "nonconsec_diagonal",
                BossModifierId.DistanceGe2       => "distance_ge2",
                BossModifierId.EntropyGlobal     => "tricolor_regions", // F8: renamed from rgb_circle
                BossModifierId.ModularRegions    => "modular_regions",
                BossModifierId.ConsecutiveLine   => "consecutive_line",
                BossModifierId.SlowThermo        => "slow_thermo",
                BossModifierId.UniqueSetLine     => "unique_set_line",
                BossModifierId.FullKropki        => "full_kropki",
                BossModifierId.SumKropki         => "sum_dot",
                BossModifierId.GreaterLessThan   => "greater_less",
                BossModifierId.XVPairs           => "xv_pairs",
                BossModifierId.PrimeCells        => "prime_cells",
                BossModifierId.FortressCells     => "fortress_cells",
                // Batch 2 constraint modifiers (IDs 30–93 subset with icons)
                BossModifierId.XSudoku                => "xsudoku",
                BossModifierId.HyperSudoku            => "hypersudoku",
                BossModifierId.DisjointGroups         => "disjoint_groups",
                BossModifierId.EntropicLine           => "entropic_line",
                BossModifierId.ZipperLine             => "zipper_line",
                BossModifierId.RegionSumLine          => "region_sum_line",
                BossModifierId.CageProduct            => "cage_product",
                BossModifierId.KillerHiddenSum        => "killer_hidden_sum",
                BossModifierId.MagicSquareRegion      => "magic_square",
                BossModifierId.ArrowAverage           => "arrow_average",
                BossModifierId.PillArrow              => "pill_arrow",
                BossModifierId.DoubleArrow            => "double_arrow",
                BossModifierId.Skyscrapers            => "skyscrapers",
                BossModifierId.Sandwich               => "sandwich",
                BossModifierId.XSums                  => "xsums",
                BossModifierId.LittleKiller           => "little_killer",
                BossModifierId.KnightPath             => "knight_path",
                BossModifierId.SnakeConstraint        => "snake_constraint",
                BossModifierId.DigitGraph             => "digit_graph",
                BossModifierId.ConstraintNegationZone => "constraint_negation",
                BossModifierId.IndexParity            => "index_parity",
                BossModifierId.ModularGlobal          => "modular_global",
                BossModifierId.KnightKingCombined     => "knight_king_combined",
                BossModifierId.Toroidal               => "toroidal",
                BossModifierId.WhisperGeneralized     => "whisper_generalized",
                BossModifierId.NonconsecLine          => "nonconsec_line",
                BossModifierId.AlternatingParityLine  => "alternating_parity_line",
                BossModifierId.HighLowAlternating     => "high_low_alternating",
                BossModifierId.NabnerLine             => "nabner_line",
                BossModifierId.ModularLine            => "modular_line",
                BossModifierId.NLine                  => "n_line",
                BossModifierId.IndexLine              => "index_line",
                BossModifierId.SumLine                => "sum_line",
                BossModifierId.SkyscraperLine         => "skyscraper_line",
                BossModifierId.ThermoLoop             => "thermo_loop",
                BossModifierId.FastThermo             => "fast_thermo",
                BossModifierId.AmbiguousThermo        => "ambiguous_thermo",
                BossModifierId.ExtraRegion            => "extra_region",
                BossModifierId.RegionSumEquality      => "region_sum_equality",
                BossModifierId.RegionParityBalance    => "region_parity_balance",
                BossModifierId.RegionIndexRule        => "region_index_rule",
                BossModifierId.ConnectedRegions       => "connected_regions",
                BossModifierId.FullWhiteKropki        => "full_white_kropki",
                BossModifierId.FullBlackKropki        => "full_black_kropki",
                BossModifierId.DifferenceK            => "difference_k",
                BossModifierId.RatioN                 => "ratio_n",
                BossModifierId.InequalityChains       => "inequality_chains",
                BossModifierId.DistancePair           => "distance_pair",
                BossModifierId.CageDifference         => "cage_difference",
                BossModifierId.CageRatio              => "cage_ratio",
                BossModifierId.RenbanCage             => "renban_cage",
                BossModifierId.ArrowProduct           => "arrow_product",
                BossModifierId.MultipleCells          => "multiple_cells",
                BossModifierId.IndexCell              => "index_cell",
                BossModifierId.MirrorCells            => "mirror_cells",
                BossModifierId.OppositeCellsSum       => "opposite_cells_sum",
                BossModifierId.CountingCircles        => "counting_circles",
                BossModifierId.Quadruples             => "quadruples",
                BossModifierId.DistanceCell           => "distance_cell",
                BossModifierId.Battlefield            => "battlefield",
                BossModifierId.OutsideInequalities    => "outside_inequalities",
                BossModifierId.HamiltonianPath        => "hamiltonian_path",
                BossModifierId.LoopConstraint         => "loop_constraint",
                BossModifierId.SelfReferentialGrid    => "self_referential_grid",
                // Boss debuffs (IDs 94–102)
                BossModifierId.RowWipe                => "row_wipe",
                BossModifierId.ColWipe                => "col_wipe",
                BossModifierId.DoublePenalty          => "double_penalty",
                BossModifierId.CellLock               => "cell_lock",
                BossModifierId.PencilBlind            => "pencil_blind",
                BossModifierId.BoxWipe                => "box_wipe",
                BossModifierId.CrossWipe              => "cross_wipe",
                BossModifierId.PencilDrain            => "pencil_drain",
                BossModifierId.GoldFine               => "gold_fine",
                // Pressure mechanics (IDs 103–106)
                BossModifierId.CountdownFill          => "countdown_fill",
                BossModifierId.HauntedCell            => "haunted_cell",
                BossModifierId.CrumblingRegion        => "crumbling_region",
                BossModifierId.PressureWave           => "pressure_wave",
                // Boss debuffs Batch 2 (IDs 107–111)
                // Batch 2 debuffs reuse the closest shipped debuff art until unique icons land.
                BossModifierId.RadiusWipe             => "cross_wipe",
                BossModifierId.PencilScramble         => "pencil_drain",
                BossModifierId.GivenReveal            => "cell_lock",
                BossModifierId.RevealCost             => "gold_fine",
                BossModifierId.CellBlur               => "pencil_blind",
                _ => ""
            };
        }

        private static readonly BossModifierId[] LocalizedNameIds =
        {
            BossModifierId.GermanWhispers, BossModifierId.DutchWhispers, BossModifierId.ParityLines,
            BossModifierId.RenbanLines, BossModifierId.DifferenceKropki, BossModifierId.RatioKropki,
            BossModifierId.KillerCages, BossModifierId.ArrowSums, BossModifierId.FogOfWar,
            BossModifierId.Palindrome, BossModifierId.Thermo, BossModifierId.BetweenLines,
            BossModifierId.EvenOdd, BossModifierId.Nonconsecutive, BossModifierId.Antiknight,
            BossModifierId.Antiking, BossModifierId.AntiBishop, BossModifierId.NonconsecDiagonal,
            BossModifierId.DistanceGe2, BossModifierId.EntropyGlobal, BossModifierId.ModularRegions,
            BossModifierId.ConsecutiveLine, BossModifierId.SlowThermo, BossModifierId.UniqueSetLine,
            BossModifierId.FullKropki, BossModifierId.SumKropki, BossModifierId.GreaterLessThan,
            BossModifierId.XVPairs, BossModifierId.PrimeCells, BossModifierId.FortressCells,
            BossModifierId.RowWipe, BossModifierId.ColWipe, BossModifierId.DoublePenalty,
            BossModifierId.CellLock, BossModifierId.PencilBlind, BossModifierId.RadiusWipe,
            BossModifierId.PencilScramble, BossModifierId.GivenReveal, BossModifierId.RevealCost,
            BossModifierId.CellBlur, BossModifierId.BoxWipe, BossModifierId.CrossWipe,
            BossModifierId.PencilDrain, BossModifierId.GoldFine
        };

        private static readonly BossModifierId[] LocalizedDescriptionIds =
        {
            BossModifierId.GermanWhispers, BossModifierId.DutchWhispers, BossModifierId.ParityLines,
            BossModifierId.RenbanLines, BossModifierId.DifferenceKropki, BossModifierId.RatioKropki,
            BossModifierId.KillerCages, BossModifierId.ArrowSums, BossModifierId.FogOfWar,
            BossModifierId.Palindrome, BossModifierId.Thermo, BossModifierId.BetweenLines,
            BossModifierId.EvenOdd, BossModifierId.Nonconsecutive, BossModifierId.Antiknight,
            BossModifierId.Antiking, BossModifierId.AntiBishop, BossModifierId.NonconsecDiagonal,
            BossModifierId.DistanceGe2, BossModifierId.EntropyGlobal, BossModifierId.ModularRegions,
            BossModifierId.ConsecutiveLine, BossModifierId.SlowThermo, BossModifierId.UniqueSetLine,
            BossModifierId.FullKropki, BossModifierId.SumKropki, BossModifierId.GreaterLessThan,
            BossModifierId.XVPairs, BossModifierId.PrimeCells, BossModifierId.FortressCells,
            BossModifierId.RowWipe, BossModifierId.ColWipe, BossModifierId.DoublePenalty,
            BossModifierId.CellLock, BossModifierId.PencilBlind, BossModifierId.BoxWipe,
            BossModifierId.CrossWipe, BossModifierId.RadiusWipe, BossModifierId.PencilDrain,
            BossModifierId.GoldFine, BossModifierId.PencilScramble, BossModifierId.GivenReveal,
            BossModifierId.RevealCost, BossModifierId.CellBlur
        };

        private static string BossNameKey(BossModifierId id) => $"Boss.Name.{id}";

        private static string BossDescriptionKey(BossModifierId id) => $"Boss.Description.{id}";

        public static IEnumerable<string> GetLocalizationKeys()
        {
            foreach (var id in LocalizedNameIds)
                yield return BossNameKey(id);
            foreach (var id in LocalizedDescriptionIds)
                yield return BossDescriptionKey(id);
        }

        public static string GetModifierName(BossModifierId id)
        {
            var fallback = id switch
            {
                BossModifierId.GermanWhispers => "German Whispers",
                BossModifierId.DutchWhispers => "Dutch Whispers",
                BossModifierId.ParityLines => "Parity Lines",
                BossModifierId.RenbanLines => "Renban Lines",
                BossModifierId.DifferenceKropki => "Difference Kropki",
                BossModifierId.RatioKropki => "Ratio Kropki",
                BossModifierId.KillerCages => "Killer Cages",
                BossModifierId.ArrowSums => "Arrow Sums",
                BossModifierId.FogOfWar => "Fog of War",
                BossModifierId.Palindrome => "Palindrome",
                BossModifierId.Thermo => "Thermometer",
                BossModifierId.BetweenLines => "Between Lines",
                BossModifierId.EvenOdd => "Even/Odd",
                BossModifierId.Nonconsecutive => "Nonconsecutive",
                BossModifierId.Antiknight          => "Anti-Knight",
                BossModifierId.Antiking            => "Anti-King",
                BossModifierId.AntiBishop          => "Anti-Bishop",
                BossModifierId.NonconsecDiagonal   => "Nonconsec. Diagonal",
                BossModifierId.DistanceGe2         => "Distance ≥ 2",
                BossModifierId.EntropyGlobal       => "Entropy",
                BossModifierId.ModularRegions      => "Modular Regions",
                BossModifierId.ConsecutiveLine     => "Consecutive Line",
                BossModifierId.SlowThermo          => "Slow Thermo",
                BossModifierId.UniqueSetLine       => "Unique Set Line",
                BossModifierId.FullKropki          => "Full Kropki",
                BossModifierId.SumKropki           => "Sum Dot",
                BossModifierId.GreaterLessThan     => "Greater/Less Than",
                BossModifierId.XVPairs             => "XV Pairs",
                BossModifierId.PrimeCells          => "Prime Cells",
                BossModifierId.FortressCells       => "Fortress Cells",
                BossModifierId.RowWipe             => "Row Wipe",
                BossModifierId.ColWipe             => "Column Wipe",
                BossModifierId.DoublePenalty       => "Double Penalty",
                BossModifierId.CellLock            => "Cell Lock",
                BossModifierId.PencilBlind         => "Pencil Blind",
                BossModifierId.BoxWipe             => "Box Wipe",
                BossModifierId.CrossWipe           => "Cross Wipe",
                BossModifierId.PencilDrain         => "Pencil Drain",
                BossModifierId.GoldFine            => "Gold Fine",
                BossModifierId.RadiusWipe          => "Radius Wipe",
                BossModifierId.PencilScramble      => "Pencil Scramble",
                BossModifierId.GivenReveal         => "Given Reveal",
                BossModifierId.RevealCost          => "Item Block",
                BossModifierId.CellBlur            => "Cell Blur",
                _ => id.ToString()
            };
            return LocalizationService.T(BossNameKey(id), fallback);
        }

        public static string GetModifierDescription(BossModifierId id)
        {
            var fallback = id switch
            {
                BossModifierId.GermanWhispers => "Adjacent cells on green lines must differ by 5 or more. Example: 1-7-2-8.",
                BossModifierId.DutchWhispers => "Adjacent cells on orange lines must differ by 4 or more. Example: 1-6-2-7.",
                BossModifierId.ParityLines => "Cells along blue lines alternate odd/even. Example: 3-4-7-2.",
                BossModifierId.RenbanLines => "Cells on pink lines form a consecutive set. Example: 3-5-4 (set {3,4,5}).",
                BossModifierId.DifferenceKropki => "White dots: adjacent cells differ by 1. Example: 4-5.",
                BossModifierId.RatioKropki => "Black dots: one cell is double the other. Example: 3-6.",
                BossModifierId.KillerCages => "Cages: cells sum to the shown total, no repeats. Example: 14=[5,9].",
                BossModifierId.ArrowSums => "Arrow: circle digit equals the sum of arrow digits. Example: 9=3+2+4.",
                BossModifierId.FogOfWar => "Fog hides unsolved cells. Correct placements reveal neighbors.",
                BossModifierId.Palindrome => "Grey lines read the same forwards and backwards. Example: 3-7-5-7-3.",
                BossModifierId.Thermo => "Digits increase along thermometers from bulb to tip. Example: 2-4-6-8.",
                BossModifierId.BetweenLines => "Interior cells are strictly between endpoint values. Example: 2-[4,5]-8.",
                BossModifierId.EvenOdd => "Marked cells must match: blue square = even, orange circle = odd.",
                BossModifierId.Nonconsecutive => "No two orthogonally adjacent cells may be consecutive.",
                BossModifierId.Antiknight          => "No two cells a chess knight's move apart may hold the same digit.",
                BossModifierId.Antiking            => "No two cells a chess king's move apart may hold the same digit. Example: 3 cannot be next to or diagonal to another 3.",
                BossModifierId.AntiBishop          => "A digit may not repeat on a diagonal within two steps. Example: a 5 rules out 5s one and two cells away diagonally.",
                BossModifierId.NonconsecDiagonal   => "Diagonally adjacent cells cannot contain consecutive digits. Example: 4 cannot be diagonal to 3 or 5.",
                BossModifierId.DistanceGe2         => "Equal digits must be at least 2 cells apart (Chebyshev distance). No two adjacent or diagonal cells may share a digit.",
                BossModifierId.EntropyGlobal       => "Every 3 consecutive cells in any row or column must contain one low (1–3), one mid (4–6), and one high (7–9) digit.",
                BossModifierId.ModularRegions      => "Every region must contain at least one digit from {1–3}, one from {4–6}, and one from {7–9}.",
                BossModifierId.ConsecutiveLine     => "Adjacent cells on gold lines must differ by exactly 1. Example: 3-4-5 or 7-6-5.",
                BossModifierId.SlowThermo          => "Digits increase or stay equal along the thermometer from bulb to tip. Example: 2-2-3-5 is valid.",
                BossModifierId.UniqueSetLine       => "No digit may repeat anywhere on the line, independent of standard Sudoku rules.",
                BossModifierId.FullKropki          => "All adjacent pairs that differ by 1 show a white dot; all 1:2 ratio pairs show a black dot. Absence of a dot forbids both relations.",
                BossModifierId.SumKropki           => "A labelled dot between two cells means those two cells sum to the shown value.",
                BossModifierId.GreaterLessThan     => "Chevrons (> or <) between adjacent cells show which must be the larger digit.",
                BossModifierId.XVPairs             => "X between two cells: they sum to 10. V between two cells: they sum to 5.",
                BossModifierId.PrimeCells          => "Marked cells (shown with a P) must contain a prime digit: 2, 3, 5, or 7.",
                BossModifierId.FortressCells       => "Grey-shaded cells must be strictly greater than every orthogonally adjacent non-shaded cell.",

                // Boss debuffs
                BossModifierId.RowWipe       => "Wrong placement: all your digits in that row are erased.",
                BossModifierId.ColWipe       => "Wrong placement: all your digits in that column are erased.",
                BossModifierId.DoublePenalty => "Wrong placements deal double HP damage.",
                BossModifierId.CellLock      => "Wrong placement: the cell locks for 3 correct placements before you can edit it again.",
                BossModifierId.PencilBlind   => "Wrong placement: all pencil marks in that row and column are cleared.",
                BossModifierId.BoxWipe       => "Wrong placement: all your digits in that box are erased.",
                BossModifierId.CrossWipe     => "Wrong placement: all your digits in that row and column are erased.",
                BossModifierId.RadiusWipe    => "Wrong placement: all your digits within two cells of it are erased.",
                BossModifierId.PencilDrain   => "Wrong placements also cost 1 pencil.",
                BossModifierId.GoldFine      => "Wrong placements also cost 5 gold.",
                BossModifierId.PencilScramble => "Wrong placement: every pencil mark on the board is scrambled.",
                BossModifierId.GivenReveal   => "Wrong placement: up to 2 of your correct digits turn into fixed givens.",
                BossModifierId.RevealCost    => "Wrong placement: items are blocked for your next 2 correct placements.",
                BossModifierId.CellBlur      => "Wrong placement: the digits around it are hidden for 3 correct placements.",
                _ => ""
            };
            return string.IsNullOrEmpty(fallback)
                ? string.Empty
                : LocalizationService.T(BossDescriptionKey(id), fallback);
        }

        public static string GetLineTypeAbbreviation(LineType type) => type switch
        {
            LineType.GermanWhispers  => "GW",
            LineType.DutchWhispers   => "DW",
            LineType.ParityLine      => "PR",
            LineType.RenbanLine      => "RB",
            LineType.Palindrome      => "PA",
            LineType.Thermo          => "TH",
            LineType.BetweenLine     => "BL",
            LineType.ConsecutiveLine => "CL",
            LineType.SlowThermo      => "ST",
            LineType.UniqueSetLine   => "UL",
            _                        => "??"
        };

        public static string GetConstraintAbbreviation(BossModifierId id)
        {
            return id switch
            {
                BossModifierId.GermanWhispers   => "GW",
                BossModifierId.DutchWhispers    => "DW",
                BossModifierId.ParityLines      => "PR",
                BossModifierId.RenbanLines      => "RB",
                BossModifierId.DifferenceKropki => "WD",
                BossModifierId.RatioKropki      => "BD",
                BossModifierId.KillerCages      => "KG",
                BossModifierId.ArrowSums        => "AR",
                BossModifierId.FogOfWar         => "FG",
                BossModifierId.Palindrome       => "PL",
                BossModifierId.Thermo           => "TH",
                BossModifierId.BetweenLines     => "BL",
                BossModifierId.EvenOdd          => "EO",
                BossModifierId.Nonconsecutive   => "NC",
                BossModifierId.Antiknight       => "AK",
                BossModifierId.Antiking         => "AI",
                BossModifierId.AntiBishop       => "AB",
                BossModifierId.NonconsecDiagonal=> "ND",
                BossModifierId.DistanceGe2      => "D2",
                BossModifierId.EntropyGlobal    => "EN",
                BossModifierId.ModularRegions   => "MR",
                BossModifierId.ConsecutiveLine  => "CL",
                BossModifierId.SlowThermo       => "ST",
                BossModifierId.UniqueSetLine    => "UL",
                BossModifierId.FullKropki       => "FK",
                BossModifierId.SumKropki        => "SK",
                BossModifierId.GreaterLessThan  => "GL",
                BossModifierId.XVPairs          => "XV",
                BossModifierId.PrimeCells       => "PM",
                BossModifierId.FortressCells    => "FC",
                _                              => "??"
            };
        }

        // [REQ: BOSS-INT-001] [REQ: BOSS-INT-002] Intensity for run number: 1-2=Low, 3-5=Med, 6-8=High, 9+=VH
        public static BossModifierIntensity IntensityForRunNumber(int runNumber)
        {
            if (runNumber <= 2) return BossModifierIntensity.Low;
            if (runNumber <= 5) return BossModifierIntensity.Medium;
            if (runNumber <= 8) return BossModifierIntensity.High;
            return BossModifierIntensity.VeryHigh;
        }

        // [REQ: REQ-BOSS-103] Localized intensity label for the boss gate (GAP-025).
        public static string GetIntensityName(BossModifierIntensity intensity) => intensity switch
        {
            BossModifierIntensity.Low      => LocalizationService.T("Boss.Intensity.Low", "Low"),
            BossModifierIntensity.Medium   => LocalizationService.T("Boss.Intensity.Medium", "Medium"),
            BossModifierIntensity.High     => LocalizationService.T("Boss.Intensity.High", "High"),
            _                              => LocalizationService.T("Boss.Intensity.VeryHigh", "Very High")
        };

        // ── Boss debuff delivery (REQ-BOSS-108, GAP-026) ──

        /// <summary>First 0-based floor whose boss carries a debuff (Floor 3).</summary>
        public const int FirstBossDebuffFloor = 2;

        // Given Reveal is not delivered: wrong digits are rejected since 1.1, so it can only turn correct digits into
        // givens, which protects them from the wipe debuffs instead of punishing the player.
        private static readonly BossModifierId[] BossDebuffPool =
        {
            BossModifierId.RowWipe, BossModifierId.ColWipe, BossModifierId.BoxWipe, BossModifierId.RadiusWipe,
            BossModifierId.DoublePenalty, BossModifierId.CellLock, BossModifierId.PencilBlind,
            BossModifierId.PencilDrain, BossModifierId.GoldFine, BossModifierId.RevealCost, BossModifierId.CellBlur
        };

        // [REQ: DEBUFF-DEF-004] [REQ: DEBUFF-DEF-008] Cross Wipe and Pencil Scramble are reserved for VeryHigh intensity.
        private static readonly BossModifierId[] VeryHighOnlyBossDebuffs =
        {
            BossModifierId.CrossWipe, BossModifierId.PencilScramble
        };

        /// <summary>The debuffs a boss can carry at the given intensity.</summary>
        // [REQ: REQ-BOSS-108] One debuff per Garden Run boss from Floor 3, drawn from this pool.
        public static IReadOnlyList<BossModifierId> GetBossDebuffPool(BossModifierIntensity intensity)
        {
            var pool = new List<BossModifierId>(BossDebuffPool);
            if (intensity == BossModifierIntensity.VeryHigh)
                pool.AddRange(VeryHighOnlyBossDebuffs);
            return pool;
        }

        public static BossModifierId PickBossDebuff(BossModifierIntensity intensity, Random rng)
        {
            var pool = GetBossDebuffPool(intensity);
            return pool[rng.Next(pool.Count)];
        }

        /// <summary>Debuff IDs 94–102 and 107–111 (103–106 are pressure mechanics).</summary>
        public static bool IsBossDebuff(BossModifierId id) =>
            ((int)id >= 94 && (int)id <= 102) || ((int)id >= 107 && (int)id <= 111);
    }
}
