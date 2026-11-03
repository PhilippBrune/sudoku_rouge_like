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
            [BossModifierId.MagicSquareRegion] = KillerCageType.MagicSquare,
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

        /// <summary>The modifier that draws lines of this type, if any.</summary>
        public static BossModifierId? ModifierOf(LineType type)
        {
            foreach (var pair in LineTypes)
                if (pair.Value == type) return pair.Key;
            return null;
        }

        /// <summary>[REQ: ITEM-SCROLL-001] Each piece a modifier drew on the board (a line, a cage, an arrow, a dot, a marked cell,
        /// a cell pair, the cells an outside clue reads, a quadruple's four cells), in drawing order. Global rules, fog and the
        /// whole-board houses (X-Sudoku, Hyper, Disjoint Groups) draw no piece. The Pattern Scroll lists them by the modifier's name.</summary>
        public static List<List<CellCoord>> Pieces(BossModifierId modifier, ModifierOverlayData overlay, int size)
        {
            var pieces = new List<List<CellCoord>>();
            if (overlay == null) return pieces;
            if (LineTypes.TryGetValue(modifier, out var lineType))
                foreach (var line in overlay.Lines) { if (line.Type == lineType) pieces.Add(new List<CellCoord>(line.Cells)); }
            else if (CageTypes.TryGetValue(modifier, out var cageType))
                foreach (var cage in overlay.KillerCages) { if (cage.CageType == cageType) pieces.Add(new List<CellCoord>(cage.Cells)); }
            else if (ArrowTypes.TryGetValue(modifier, out var arrowType))
                foreach (var a in overlay.Arrows)
                {
                    if (a.ArrowType != arrowType) continue;
                    var cells = new List<CellCoord> { a.CircleCell };
                    if (a.ArrowType == ArrowType.DoubleEndpoint || a.ArrowType == ArrowType.Pill) cells.Add(a.SecondCircle);
                    cells.AddRange(a.ArrowCells);
                    pieces.Add(cells);
                }
            else
                switch (modifier)
                {
                    case BossModifierId.DifferenceKropki: Dots(d => !d.IsBlack && d.SumValue == 0); break;
                    case BossModifierId.RatioKropki: Dots(d => d.IsBlack); break;
                    case BossModifierId.SumKropki: Dots(d => d.SumValue > 0); break;
                    case BossModifierId.FullKropki: Dots(d => d.SumValue == 0); break;
                    case BossModifierId.FullWhiteKropki: Dots(d => !d.IsBlack && d.SumValue == 0); break;
                    case BossModifierId.FullBlackKropki: Dots(d => d.IsBlack); break;
                    case BossModifierId.ExtraRegion:
                        foreach (var r in overlay.ExtraRegions) pieces.Add(new List<CellCoord>(r.Cells));
                        break;
                    case BossModifierId.GreaterLessThan: Pairs(t => t == PairConstraintType.GreaterThan || t == PairConstraintType.LessThan); break;
                    case BossModifierId.XVPairs: Pairs(t => t == PairConstraintType.SumX || t == PairConstraintType.SumV); break;
                    case BossModifierId.DifferenceK: Pairs(t => t == PairConstraintType.DifferenceK); break;
                    case BossModifierId.RatioN: Pairs(t => t == PairConstraintType.RatioN); break;
                    case BossModifierId.EvenOdd: Markers(t => t == MarkerType.Even || t == MarkerType.Odd); break;
                    case BossModifierId.PrimeCells: Markers(t => t == MarkerType.Prime); break;
                    case BossModifierId.FortressCells: Markers(t => t == MarkerType.Fortress); break;
                    case BossModifierId.MultipleCells: Markers(t => t == MarkerType.Multiple); break;
                    case BossModifierId.CountingCircles: Markers(t => t == MarkerType.CountingCircle); break;
                    case BossModifierId.DistanceCell: Markers(t => t == MarkerType.DistanceCellSource); break;
                    case BossModifierId.MirrorCells: CellPairs(CellPairType.Mirror); break;
                    case BossModifierId.DistancePair: CellPairs(CellPairType.Distance); break;
                    case BossModifierId.OppositeCellsSum: CellPairs(CellPairType.OppositeSum); break;
                    case BossModifierId.Skyscrapers: Clues(OutsideClueType.Skyscrapers); break;
                    case BossModifierId.Sandwich: Clues(OutsideClueType.Sandwich); break;
                    case BossModifierId.XSums: Clues(OutsideClueType.XSums); break;
                    case BossModifierId.LittleKiller: Clues(OutsideClueType.LittleKiller); break;
                    case BossModifierId.Quadruples:
                        foreach (var q in overlay.Quadruples)
                            pieces.Add(new List<CellCoord> { q.TopLeft, new CellCoord(q.TopLeft.Row, q.TopLeft.Col + 1),
                                new CellCoord(q.TopLeft.Row + 1, q.TopLeft.Col), new CellCoord(q.TopLeft.Row + 1, q.TopLeft.Col + 1) });
                        break;
                }
            return pieces;

            void Dots(System.Predicate<KropkiDot> match)
            {
                foreach (var d in overlay.KropkiDots) if (match(d)) pieces.Add(new List<CellCoord> { d.CellA, d.CellB });
            }
            void Pairs(System.Predicate<PairConstraintType> match)
            {
                foreach (var p in overlay.PairConstraints) if (match(p.Type)) pieces.Add(new List<CellCoord> { p.CellA, p.CellB });
            }
            void Markers(System.Predicate<MarkerType> match)
            {
                foreach (var m in overlay.CellMarkers) if (match(m.Type)) pieces.Add(new List<CellCoord> { m.Cell });
            }
            void CellPairs(CellPairType type)
            {
                foreach (var p in overlay.CellPairs) if (p.PairType == type) pieces.Add(new List<CellCoord> { p.CellA, p.CellB });
            }
            void Clues(OutsideClueType type)
            {
                foreach (var c in overlay.OutsideClues) if (c.Type == type) pieces.Add(OutsideClueGeometry.Cells(c, size));
            }
        }

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
                case BossModifierId.Skyscrapers:
                    return overlay.OutsideClues.Exists(c => c.Type == OutsideClueType.Skyscrapers);
                case BossModifierId.Sandwich:
                    return overlay.OutsideClues.Exists(c => c.Type == OutsideClueType.Sandwich);
                case BossModifierId.XSums:
                    return overlay.OutsideClues.Exists(c => c.Type == OutsideClueType.XSums);
                case BossModifierId.LittleKiller:
                    return overlay.OutsideClues.Exists(c => c.Type == OutsideClueType.LittleKiller);
                case BossModifierId.Quadruples:
                    return overlay.Quadruples.Count > 0;
                case BossModifierId.XSudoku:
                    return overlay.IsXSudoku;
                case BossModifierId.HyperSudoku:
                    return overlay.IsHyperSudoku;
                case BossModifierId.DisjointGroups:
                    return overlay.IsDisjointGroups;
                case BossModifierId.ExtraRegion:
                    return overlay.ExtraRegions.Count > 0;
                default:
                    return false;
            }
        }
    }
}
