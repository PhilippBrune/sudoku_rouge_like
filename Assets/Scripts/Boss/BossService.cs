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

            // [REQ: GEN-MOD-002] Catalog batches B1 and B2 (BossMechanicsSystem.md "Modifier catalog"; min sizes from the same spec)
            [BossModifierId.NonconsecLine] = (1, 0.20f, 6),
            [BossModifierId.HighLowAlternating] = (2, 0.25f, 6),
            [BossModifierId.NabnerLine] = (2, 0.30f, 6),
            [BossModifierId.EntropicLine] = (3, 0.40f, 7),
            [BossModifierId.ModularLine] = (2, 0.30f, 6),
            [BossModifierId.ZipperLine] = (2, 0.35f, 6),
            [BossModifierId.FastThermo] = (2, 0.30f, 6),
            [BossModifierId.AmbiguousThermo] = (3, 0.40f, 6),
            [BossModifierId.FullWhiteKropki] = (3, 0.45f, 5),
            [BossModifierId.FullBlackKropki] = (2, 0.35f, 6),
            [BossModifierId.DifferenceK] = (1, 0.20f, 5),
            [BossModifierId.MultipleCells] = (1, 0.15f, 5),
            [BossModifierId.MirrorCells] = (1, 0.20f, 5),

            // Catalog batch B3: cages, arrows, region sum line (min sizes from BossMechanicsSystem.md)
            [BossModifierId.RegionSumLine] = (3, 0.45f, 7),
            [BossModifierId.KillerHiddenSum] = (3, 0.50f, 7),
            [BossModifierId.CageProduct] = (3, 0.45f, 6),
            [BossModifierId.CageDifference] = (1, 0.20f, 5),
            [BossModifierId.RenbanCage] = (2, 0.35f, 5),
            [BossModifierId.ArrowAverage] = (3, 0.45f, 7),
            [BossModifierId.DoubleArrow] = (3, 0.45f, 6),

            // Catalog batch B6 (min sizes from BossMechanicsSystem.md; the inverted zone is 9×9 only, owner decision)
            [BossModifierId.WhisperGeneralized] = (2, 0.30f, 6),
            [BossModifierId.NLine] = (3, 0.40f, 7),
            [BossModifierId.SumLine] = (2, 0.30f, 6),
            [BossModifierId.RatioN] = (2, 0.25f, 6),
            [BossModifierId.DistancePair] = (2, 0.25f, 6),
            [BossModifierId.CageRatio] = (2, 0.25f, 5),
            [BossModifierId.PillArrow] = (3, 0.45f, 8),
            [BossModifierId.OppositeCellsSum] = (1, 0.20f, 5),
            [BossModifierId.CountingCircles] = (2, 0.30f, 6),
            [BossModifierId.DistanceCell] = (1, 0.15f, 7),
            [BossModifierId.ConstraintNegationZone] = (3, 0.45f, 9),

            // Catalog batch B4: clues outside the grid and on corners (min sizes from BossMechanicsSystem.md)
            [BossModifierId.Quadruples] = (2, 0.30f, 9),
            [BossModifierId.Skyscrapers] = (3, 0.45f, 5),
            [BossModifierId.Sandwich] = (3, 0.50f, 6),
            [BossModifierId.XSums] = (3, 0.50f, 5),
            [BossModifierId.LittleKiller] = (3, 0.45f, 6),

            // Catalog batch B5: extra houses and the magic square (board-shaping; Hyper, Disjoint and the magic square
            // need the 3×3 boxes of a 9×9, the extra region reads off the solution)
            [BossModifierId.XSudoku] = (2, 0.35f, 5),
            [BossModifierId.HyperSudoku] = (3, 0.45f, 9),
            [BossModifierId.DisjointGroups] = (3, 0.45f, 9),
            [BossModifierId.ExtraRegion] = (2, 0.30f, 5),
            [BossModifierId.MagicSquareRegion] = (3, 0.45f, 9),

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
                || modifier == BossModifierId.DistanceGe2
                // Catalog batch B5: extra houses and the magic square shape the solution too (rectangular regions).
                || modifier == BossModifierId.XSudoku
                || modifier == BossModifierId.HyperSudoku
                || modifier == BossModifierId.DisjointGroups
                || modifier == BossModifierId.MagicSquareRegion;
        }

        /// <summary>
        /// [REQ: GEN-SOLVE-002] True for every rule the generator builds the solution with: the board-shaping rules and
        /// Entropy. A board made without one of them does not satisfy it (GAP-051).
        /// </summary>
        public static bool ShapesSolution(BossModifierId modifier) =>
            IsBoardShapingModifier(modifier) || modifier == BossModifierId.EntropyGlobal;

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
            // Nonconsecutive forbids exactly the neighbours a white dot marks: Difference Kropki could never place a
            // dot, and the player saw the rule without any dots (GAP-049).
            (BossModifierId.Nonconsecutive, BossModifierId.DifferenceKropki),
            // Catalog B1/B2: one thermometer kind per puzzle; complete dot sets never stack with another dot rule of
            // the same colour (redundant) or with Nonconsecutive (no white dot can exist); two plain grey lines without
            // a bulb (Palindrome, Ambiguous Thermo) would look alike.
            (BossModifierId.FastThermo, BossModifierId.Thermo),
            (BossModifierId.FastThermo, BossModifierId.SlowThermo),
            (BossModifierId.FastThermo, BossModifierId.AmbiguousThermo),
            (BossModifierId.AmbiguousThermo, BossModifierId.Thermo),
            (BossModifierId.AmbiguousThermo, BossModifierId.SlowThermo),
            (BossModifierId.AmbiguousThermo, BossModifierId.Palindrome),
            (BossModifierId.FullWhiteKropki, BossModifierId.FullKropki),
            (BossModifierId.FullWhiteKropki, BossModifierId.FullBlackKropki),
            (BossModifierId.FullWhiteKropki, BossModifierId.DifferenceKropki),
            (BossModifierId.FullWhiteKropki, BossModifierId.Nonconsecutive),
            (BossModifierId.FullBlackKropki, BossModifierId.FullKropki),
            (BossModifierId.FullBlackKropki, BossModifierId.RatioKropki),
            // A complete set and another dot kind could put two dots on one edge (1-2 is both a difference of 1 and a
            // 1:2 ratio; sum dots sit on any edge).
            (BossModifierId.FullWhiteKropki, BossModifierId.RatioKropki),
            (BossModifierId.FullWhiteKropki, BossModifierId.SumKropki),
            (BossModifierId.FullBlackKropki, BossModifierId.DifferenceKropki),
            (BossModifierId.FullBlackKropki, BossModifierId.SumKropki),
            // B3: a cage with a hidden sum next to cages with sums would look like a missing number.
            (BossModifierId.KillerHiddenSum, BossModifierId.KillerCages),
            // B6: the inverted zone draws German-green whisper lines; counting circles would read like the other
            // circled cells (prime and multiple markers).
            (BossModifierId.ConstraintNegationZone, BossModifierId.GermanWhispers),
            (BossModifierId.CountingCircles, BossModifierId.PrimeCells),
            (BossModifierId.CountingCircles, BossModifierId.MultipleCells),
            // B4: one kind of outside clue per board, so every number in the ring means the same thing.
            (BossModifierId.Skyscrapers, BossModifierId.Sandwich),
            (BossModifierId.Skyscrapers, BossModifierId.XSums),
            (BossModifierId.Skyscrapers, BossModifierId.LittleKiller),
            (BossModifierId.Sandwich, BossModifierId.XSums),
            (BossModifierId.Sandwich, BossModifierId.LittleKiller),
            (BossModifierId.XSums, BossModifierId.LittleKiller),
            // B5: a magic square always has consecutive digits side by side (7 next to 6) and diagonally (4 and 6 next
            // to the 5 in its middle).
            (BossModifierId.MagicSquareRegion, BossModifierId.Nonconsecutive),
            (BossModifierId.MagicSquareRegion, BossModifierId.NonconsecDiagonal),
            // B5 pair benchmark (SolutionShapePairBenchmark, 9×9, 4 seeds, 6 s each, 2026-10-19): these time out or have no
            // solution at all (Disjoint Groups + Entropy); every older rule alone fills in under 0.3 s.
            (BossModifierId.XSudoku, BossModifierId.Nonconsecutive),
            (BossModifierId.XSudoku, BossModifierId.Antiknight),
            (BossModifierId.XSudoku, BossModifierId.NonconsecDiagonal),
            (BossModifierId.XSudoku, BossModifierId.EntropyGlobal),
            (BossModifierId.HyperSudoku, BossModifierId.Nonconsecutive),
            (BossModifierId.HyperSudoku, BossModifierId.NonconsecDiagonal),
            (BossModifierId.HyperSudoku, BossModifierId.EntropyGlobal),
            (BossModifierId.DisjointGroups, BossModifierId.EntropyGlobal),
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
            BossModifierId.NonconsecLine, BossModifierId.HighLowAlternating, BossModifierId.NabnerLine, BossModifierId.EntropicLine, BossModifierId.ModularLine, BossModifierId.ZipperLine, BossModifierId.FastThermo, BossModifierId.AmbiguousThermo, BossModifierId.FullWhiteKropki, BossModifierId.FullBlackKropki, BossModifierId.DifferenceK, BossModifierId.MultipleCells, BossModifierId.MirrorCells,
            BossModifierId.RegionSumLine, BossModifierId.KillerHiddenSum, BossModifierId.CageProduct, BossModifierId.CageDifference, BossModifierId.RenbanCage, BossModifierId.ArrowAverage, BossModifierId.DoubleArrow,
            BossModifierId.WhisperGeneralized, BossModifierId.NLine, BossModifierId.SumLine, BossModifierId.RatioN, BossModifierId.DistancePair, BossModifierId.CageRatio, BossModifierId.PillArrow, BossModifierId.OppositeCellsSum, BossModifierId.CountingCircles, BossModifierId.DistanceCell, BossModifierId.ConstraintNegationZone,
            BossModifierId.Quadruples, BossModifierId.Skyscrapers, BossModifierId.Sandwich, BossModifierId.XSums, BossModifierId.LittleKiller,
            BossModifierId.XSudoku, BossModifierId.HyperSudoku, BossModifierId.DisjointGroups, BossModifierId.ExtraRegion, BossModifierId.MagicSquareRegion,
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
            BossModifierId.NonconsecLine, BossModifierId.HighLowAlternating, BossModifierId.NabnerLine, BossModifierId.EntropicLine, BossModifierId.ModularLine, BossModifierId.ZipperLine, BossModifierId.FastThermo, BossModifierId.AmbiguousThermo, BossModifierId.FullWhiteKropki, BossModifierId.FullBlackKropki, BossModifierId.DifferenceK, BossModifierId.MultipleCells, BossModifierId.MirrorCells,
            BossModifierId.RegionSumLine, BossModifierId.KillerHiddenSum, BossModifierId.CageProduct, BossModifierId.CageDifference, BossModifierId.RenbanCage, BossModifierId.ArrowAverage, BossModifierId.DoubleArrow,
            BossModifierId.WhisperGeneralized, BossModifierId.NLine, BossModifierId.SumLine, BossModifierId.RatioN, BossModifierId.DistancePair, BossModifierId.CageRatio, BossModifierId.PillArrow, BossModifierId.OppositeCellsSum, BossModifierId.CountingCircles, BossModifierId.DistanceCell, BossModifierId.ConstraintNegationZone,
            BossModifierId.Quadruples, BossModifierId.Skyscrapers, BossModifierId.Sandwich, BossModifierId.XSums, BossModifierId.LittleKiller,
            BossModifierId.XSudoku, BossModifierId.HyperSudoku, BossModifierId.DisjointGroups, BossModifierId.ExtraRegion, BossModifierId.MagicSquareRegion,
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

        public static string GetModifierName(BossModifierId id) =>
            LocalizationService.T(BossNameKey(id), ModifierCatalog.Name(id));

        public static string GetModifierDescription(BossModifierId id)
        {
            var fallback = ModifierCatalog.Rule(id);
            return string.IsNullOrEmpty(fallback)
                ? string.Empty
                : LocalizationService.T(BossDescriptionKey(id), fallback);
        }

        /// <summary>The letters on a line: its modifier's abbreviation (the Palindrome line read "PA", its gate "PL").</summary>
        public static string GetLineTypeAbbreviation(LineType type) =>
            SudokuRoguelike.Sudoku.ModifierPresence.ModifierOf(type) is BossModifierId id ? ModifierCatalog.Abbreviation(id) : "??";

        public static string GetConstraintAbbreviation(BossModifierId id) => ModifierCatalog.Abbreviation(id);

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
