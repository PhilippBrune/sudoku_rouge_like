using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Boss
{
    public sealed class BossService
    {
        private readonly Random _random;

        private static readonly Dictionary<BossModifierId, (int Tier, float Impact)> ModifierData = new()
        {
            [BossModifierId.ParityLines] = (1, 0.15f),
            [BossModifierId.DifferenceKropki] = (1, 0.20f),
            [BossModifierId.Nonconsecutive] = (1, 0.25f),
            [BossModifierId.DutchWhispers] = (2, 0.30f),
            [BossModifierId.RenbanLines] = (2, 0.35f),
            [BossModifierId.RatioKropki] = (2, 0.40f),
            [BossModifierId.Palindrome] = (2, 0.30f),
            [BossModifierId.Antiknight] = (2, 0.35f),
            [BossModifierId.EvenOdd] = (2, 0.25f),
            [BossModifierId.KillerCages] = (3, 0.50f),
            [BossModifierId.ArrowSums] = (3, 0.55f),
            [BossModifierId.Thermo] = (3, 0.45f),
            [BossModifierId.BetweenLines] = (3, 0.45f),
            [BossModifierId.FogOfWar] = (4, 0.60f),
            [BossModifierId.GermanWhispers] = (5, 0.75f),

            // Extended pool (IDs 15-29)
            [BossModifierId.Antiking]          = (2, 0.30f),
            [BossModifierId.AntiBishop]        = (3, 0.45f),
            [BossModifierId.NonconsecDiagonal] = (2, 0.28f),
            [BossModifierId.DistanceGe2]       = (2, 0.32f),
            [BossModifierId.EntropyGlobal]     = (4, 0.60f),
            [BossModifierId.ModularRegions]    = (3, 0.40f),
            [BossModifierId.ConsecutiveLine]   = (2, 0.35f),
            [BossModifierId.SlowThermo]        = (2, 0.30f),
            [BossModifierId.UniqueSetLine]     = (2, 0.30f),
            [BossModifierId.FullKropki]        = (3, 0.50f),
            [BossModifierId.SumKropki]         = (2, 0.35f),
            [BossModifierId.GreaterLessThan]   = (2, 0.30f),
            [BossModifierId.XVPairs]           = (2, 0.30f),
            [BossModifierId.PrimeCells]        = (1, 0.20f),
            [BossModifierId.FortressCells]     = (2, 0.35f),

            // Boss debuffs (IDs 94–102) — reactive penalties on wrong placement
            // [REQ: DEBUFF-HOOK-011] All debuff entries registered here; IDs ≥94 excluded from BuildEligiblePool
            [BossModifierId.RowWipe]       = (3, 0.55f), // [REQ: DEBUFF-DEF-001]
            [BossModifierId.ColWipe]       = (3, 0.55f), // [REQ: DEBUFF-DEF-002]
            [BossModifierId.DoublePenalty] = (2, 0.40f), // [REQ: DEBUFF-DEF-012]
            [BossModifierId.CellLock]      = (2, 0.45f), // [REQ: DEBUFF-DEF-018]
            [BossModifierId.PencilBlind]   = (3, 0.50f), // [REQ: DEBUFF-DEF-007]
            [BossModifierId.BoxWipe]       = (3, 0.55f), // [REQ: DEBUFF-DEF-003]
            [BossModifierId.CrossWipe]     = (4, 0.70f), // [REQ: DEBUFF-DEF-004]
            [BossModifierId.PencilDrain]   = (2, 0.35f), // [REQ: DEBUFF-DEF-013]
            [BossModifierId.GoldFine]      = (2, 0.30f), // [REQ: DEBUFF-DEF-014]

            // Pressure mechanics (IDs 103–106) — in-puzzle urgency timers (rolled via dedicated path)
            // [REQ: PRESSURE-ROLL-004] registered here so BuildEligiblePool can exclude them; actual rolling in RunDirector.RollPressureMechanic
            [BossModifierId.CountdownFill]   = (2, 0.35f),
            [BossModifierId.HauntedCell]     = (2, 0.30f),
            [BossModifierId.CrumblingRegion] = (3, 0.50f),
            [BossModifierId.PressureWave]    = (4, 0.60f)  // [REQ: PRESSURE-ROLL-005] floor 3+ only
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

        public List<BossModifierId> RollBossOptions(int count, List<BossModifierId> excludeUsed)
        {
            var pool = new List<BossModifierId>();
            foreach (var pair in ModifierData)
                pool.Add(pair.Key);

            if (excludeUsed != null)
            {
                for (var i = pool.Count - 1; i >= 0; i--)
                {
                    for (var j = 0; j < excludeUsed.Count; j++)
                    {
                        if (pool[i] == excludeUsed[j])
                        {
                            pool.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            var result = new List<BossModifierId>();
            var safeCount = Math.Min(count, pool.Count);
            for (var picked = 0; picked < safeCount; picked++)
            {
                var idx = _random.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return result;
        }

        /// <summary>
        /// Roll floor modifiers for the given floor. Floor N has N−1 modifiers (Floor 1 = 0).
        /// Excludes modifiers that require board sizes larger than the floor's minimum.
        /// </summary>
        // [REQ: BOSS-FLOOR-001] Floor N has N−1 floor modifiers; rolls from eligible pool at floor entry
        public List<BossModifierId> RollFloorModifiers(int floor, int minBoardSize)
        {
            var count = floor; // Floor 0(1st)=0, Floor 1(2nd)=1, Floor 2(3rd)=2, ..., Floor 4(5th)=4
            if (count == 0) return new List<BossModifierId>();

            var pool = BuildEligiblePool(null, minBoardSize);
            return DrawDistinct(pool, count);
        }

        /// <summary>
        /// Roll boss modifier choices. Excludes active floor modifiers from the pool.
        /// Floor N offers N+1 options (min 3), player picks N (min 2).
        /// </summary>
        public List<BossModifierId> RollBossChoices(int floor, List<BossModifierId> activeFloorModifiers, int minBoardSize)
        {
            GetBossModifierCounts(floor, out var shown, out _);
            var pool = BuildEligiblePool(activeFloorModifiers, minBoardSize);
            return DrawDistinct(pool, shown);
        }

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

                // Board size restrictions: German Whispers and Killer Cages require ≥ 7×7
                if (minBoardSize < 7 && (pair.Key == BossModifierId.GermanWhispers || pair.Key == BossModifierId.KillerCages))
                    continue;

                // Entropy requires exactly 9×9 (low/mid/high bands are 1-3/4-6/7-9)
                if (pair.Key == BossModifierId.EntropyGlobal && minBoardSize < 9)
                    continue;

                if (exclude != null && exclude.Contains(pair.Key))
                    continue;

                pool.Add(pair.Key);
            }
            return pool;
        }

        private List<BossModifierId> DrawDistinct(List<BossModifierId> pool, int count)
        {
            var result = new List<BossModifierId>();
            var seen   = new HashSet<BossModifierId>();
            var safeCount = Math.Min(count, pool.Count);
            for (var i = 0; i < safeCount; i++)
            {
                var idx  = _random.Next(pool.Count);
                var pick = pool[idx];
                pool.RemoveAt(idx);
                if (seen.Add(pick))   // HashSet.Add returns false for duplicates
                    result.Add(pick);
            }
            return result;
        }

        /// <summary>
        /// Boss choice formula: Floor N offers N+1 options (min 3), player picks N (min 2).
        /// </summary>
        public static void GetBossModifierCounts(int floor, out int optionsShown, out int playerChooses)
        {
            // Floor 0 (1st): pick 1 of 2; Floor 1 (2nd): pick 2 of 3; Floor 2 (3rd): pick 3 of 4; etc.
            optionsShown = floor + 2;
            playerChooses = floor + 1;
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
                BossModifierId.EntropyGlobal     => "rgb_circle",
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
                _ => ""
            };
        }

        public static string GetModifierName(BossModifierId id)
        {
            return id switch
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
                _ => id.ToString()
            };
        }

        public static string GetModifierDescription(BossModifierId id)
        {
            return id switch
            {
                BossModifierId.GermanWhispers => "Adjacent cells on green lines must differ by 5 or more. Example: 1-7-2-8.",
                BossModifierId.DutchWhispers => "Adjacent cells on teal lines must differ by 4 or more. Example: 1-6-2-7.",
                BossModifierId.ParityLines => "Cells along purple lines alternate odd/even. Example: 3-4-7-2.",
                BossModifierId.RenbanLines => "Cells on orange lines form a consecutive set. Example: 3-5-4 (set {3,4,5}).",
                BossModifierId.DifferenceKropki => "White dots: adjacent cells differ by 1. Example: 4-5.",
                BossModifierId.RatioKropki => "Black dots: one cell is double the other. Example: 3-6.",
                BossModifierId.KillerCages => "Cages: cells sum to the shown total, no repeats. Example: 14=[5,9].",
                BossModifierId.ArrowSums => "Arrow: circle digit equals the sum of arrow digits. Example: 9=3+2+4.",
                BossModifierId.FogOfWar => "Fog hides unsolved cells. Correct placements reveal neighbors.",
                BossModifierId.Palindrome => "Lines read the same forwards and backwards. Example: 3-7-5-7-3.",
                BossModifierId.Thermo => "Digits increase along thermometers from bulb to tip. Example: 2-4-6-8.",
                BossModifierId.BetweenLines => "Interior cells are strictly between endpoint values. Example: 2-[4,5]-8.",
                BossModifierId.EvenOdd => "Marked cells must match: blue square = even, orange circle = odd.",
                BossModifierId.Nonconsecutive => "No two orthogonally adjacent cells may be consecutive.",
                BossModifierId.Antiknight          => "No two cells a chess knight's move apart may hold the same digit.",
                BossModifierId.Antiking            => "No two cells a chess king's move apart may hold the same digit. Example: 3 cannot be next to or diagonal to another 3.",
                BossModifierId.AntiBishop          => "No two cells on the same diagonal may hold the same digit. Example: 5 can only appear once per diagonal line.",
                BossModifierId.NonconsecDiagonal   => "Diagonally adjacent cells cannot contain consecutive digits. Example: 4 cannot be diagonal to 3 or 5.",
                BossModifierId.DistanceGe2         => "Equal digits must be at least 2 cells apart (Chebyshev distance). No two adjacent or diagonal cells may share a digit.",
                BossModifierId.EntropyGlobal       => "Every 3 consecutive cells in any row or column must contain one low (1–3), one mid (4–6), and one high (7–9) digit.",
                BossModifierId.ModularRegions      => "Every region must contain at least one digit from {1–3}, one from {4–6}, and one from {7–9}.",
                BossModifierId.ConsecutiveLine     => "Adjacent cells on orange lines must differ by exactly 1. Example: 3-4-5 or 7-6-5.",
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
                BossModifierId.DoublePenalty => "Wrong placements deal 2 HP damage instead of 1.",
                BossModifierId.CellLock      => "Wrong placement: the cell locks for 3 correct placements before you can edit it again.",
                BossModifierId.PencilBlind   => "Wrong placement: all pencil marks in that row and column are cleared.",
                _ => ""
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
    }
}
