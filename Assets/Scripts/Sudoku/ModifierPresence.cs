using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    /// <summary>
    /// [REQ: BOSS-MOD-004] Whether a generated overlay carries the geometry a modifier needs, checked per type: a Thermo
    /// needs a thermometer, not just any line; a Difference Kropki needs a white dot, not just any dot (GAP-047). Rules
    /// without geometry (anti-knight and the other global rules, boss debuffs, pressure mechanics) are always present.
    /// Catalog IDs without a generator report missing, so a request for one never passes silently.
    /// </summary>
    public static class ModifierPresence
    {
        private static readonly Dictionary<BossModifierId, LineType> LineTypes = new Dictionary<BossModifierId, LineType>
        {
            [BossModifierId.GermanWhispers] = LineType.GermanWhispers,
            [BossModifierId.DutchWhispers] = LineType.DutchWhispers,
            [BossModifierId.ParityLines] = LineType.ParityLine,
            [BossModifierId.RenbanLines] = LineType.RenbanLine,
            [BossModifierId.Palindrome] = LineType.Palindrome,
            [BossModifierId.Thermo] = LineType.Thermo,
            [BossModifierId.BetweenLines] = LineType.BetweenLine,
            [BossModifierId.ConsecutiveLine] = LineType.ConsecutiveLine,
            [BossModifierId.SlowThermo] = LineType.SlowThermo,
            [BossModifierId.UniqueSetLine] = LineType.UniqueSetLine,
            [BossModifierId.WhisperGeneralized] = LineType.WhisperGeneralized,
            [BossModifierId.NonconsecLine] = LineType.NonconsecLine,
            [BossModifierId.AlternatingParityLine] = LineType.AlternatingParity,
            [BossModifierId.HighLowAlternating] = LineType.HighLowAlternating,
            [BossModifierId.NabnerLine] = LineType.NabnerLine,
            [BossModifierId.EntropicLine] = LineType.EntropicLine,
            [BossModifierId.ModularLine] = LineType.ModularLine,
            [BossModifierId.ZipperLine] = LineType.ZipperLine,
            [BossModifierId.RegionSumLine] = LineType.RegionSumLine,
            [BossModifierId.NLine] = LineType.NLine,
            [BossModifierId.IndexLine] = LineType.IndexLine,
            [BossModifierId.SumLine] = LineType.SumLine,
            [BossModifierId.SkyscraperLine] = LineType.SkyscraperLine,
            [BossModifierId.ThermoLoop] = LineType.ThermoLoop,
            [BossModifierId.FastThermo] = LineType.FastThermo,
            [BossModifierId.AmbiguousThermo] = LineType.AmbiguousThermo,
            [BossModifierId.ConstraintNegationZone] = LineType.ZoneWhisper,
        };

        private static readonly Dictionary<BossModifierId, KillerCageType> CageTypes = new Dictionary<BossModifierId, KillerCageType>
        {
            [BossModifierId.KillerCages] = KillerCageType.Sum,
            [BossModifierId.KillerHiddenSum] = KillerCageType.HiddenSum,
            [BossModifierId.CageProduct] = KillerCageType.Product,
            [BossModifierId.CageDifference] = KillerCageType.Difference,
            [BossModifierId.CageRatio] = KillerCageType.Ratio,
            [BossModifierId.RenbanCage] = KillerCageType.RenbanSet,
        };

        private static readonly Dictionary<BossModifierId, ArrowType> ArrowTypes = new Dictionary<BossModifierId, ArrowType>
        {
            [BossModifierId.ArrowSums] = ArrowType.Sum,
            [BossModifierId.ArrowAverage] = ArrowType.Average,
            [BossModifierId.ArrowProduct] = ArrowType.Product,
            [BossModifierId.PillArrow] = ArrowType.Pill,
            [BossModifierId.DoubleArrow] = ArrowType.DoubleEndpoint,
        };

        // Rules that act on the whole grid and need no geometry.
        private static readonly HashSet<BossModifierId> GlobalRules = new HashSet<BossModifierId>
        {
            BossModifierId.Nonconsecutive, BossModifierId.Antiknight, BossModifierId.Antiking, BossModifierId.AntiBishop,
            BossModifierId.NonconsecDiagonal, BossModifierId.DistanceGe2, BossModifierId.EntropyGlobal,
            BossModifierId.ModularRegions,
        };

        /// <summary>The first ID of the boss debuffs and pressure mechanics, which ride along in a level's modifiers.</summary>
        public const int FirstSystemId = 94;

        public static bool IsPresent(BossModifierId modifier, ModifierOverlayData overlay)
        {
            if ((int)modifier >= FirstSystemId || GlobalRules.Contains(modifier)) return true;
            if (overlay == null) return false;

            if (LineTypes.TryGetValue(modifier, out var lineType))
                return overlay.Lines.Exists(l => l.Type == lineType);
            if (CageTypes.TryGetValue(modifier, out var cageType))
                return overlay.KillerCages.Exists(c => c.CageType == cageType);
            if (ArrowTypes.TryGetValue(modifier, out var arrowType))
                return overlay.Arrows.Exists(a => a.ArrowType == arrowType);

            switch (modifier)
            {
                case BossModifierId.DifferenceKropki:
                    return overlay.KropkiDots.Exists(d => !d.IsBlack && d.SumValue == 0);
                case BossModifierId.RatioKropki:
                    return overlay.KropkiDots.Exists(d => d.IsBlack);
                case BossModifierId.SumKropki:
                    return overlay.KropkiDots.Exists(d => d.SumValue > 0);
                case BossModifierId.FullKropki:
                    return overlay.FullKropkiNegativeInference;
                case BossModifierId.FullWhiteKropki:
                    return overlay.FullWhiteKropkiNeg;
                case BossModifierId.FullBlackKropki:
                    return overlay.FullBlackKropkiNeg;
                case BossModifierId.GreaterLessThan:
                    return overlay.PairConstraints.Exists(p => p.Type == PairConstraintType.GreaterThan || p.Type == PairConstraintType.LessThan);
                case BossModifierId.XVPairs:
                    return overlay.PairConstraints.Exists(p => p.Type == PairConstraintType.SumX || p.Type == PairConstraintType.SumV);
                case BossModifierId.DifferenceK:
                    return overlay.PairConstraints.Exists(p => p.Type == PairConstraintType.DifferenceK);
                case BossModifierId.FogOfWar:
                    return overlay.FogCells.Count > 0;
                case BossModifierId.EvenOdd:
                    return overlay.CellMarkers.Exists(m => m.Type == MarkerType.Even || m.Type == MarkerType.Odd);
                case BossModifierId.PrimeCells:
                    return overlay.CellMarkers.Exists(m => m.Type == MarkerType.Prime);
                case BossModifierId.FortressCells:
                    return overlay.CellMarkers.Exists(m => m.Type == MarkerType.Fortress);
                case BossModifierId.MultipleCells:
                    return overlay.CellMarkers.Exists(m => m.Type == MarkerType.Multiple);
                case BossModifierId.MirrorCells:
                    return overlay.CellPairs.Exists(p => p.PairType == CellPairType.Mirror);
                case BossModifierId.RatioN:
                    return overlay.PairConstraints.Exists(p => p.Type == PairConstraintType.RatioN);
                case BossModifierId.DistancePair:
                    return overlay.CellPairs.Exists(p => p.PairType == CellPairType.Distance);
                case BossModifierId.OppositeCellsSum:
                    return overlay.CellPairs.Exists(p => p.PairType == CellPairType.OppositeSum);
                case BossModifierId.CountingCircles:
                    return overlay.CellMarkers.Exists(m => m.Type == MarkerType.CountingCircle);
                case BossModifierId.DistanceCell:
                    return overlay.CellMarkers.Exists(m => m.Type == MarkerType.DistanceCellSource);
                default:
                    return false;
            }
        }
    }
}
