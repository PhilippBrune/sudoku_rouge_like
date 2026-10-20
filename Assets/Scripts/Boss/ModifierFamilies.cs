using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Boss
{
    public enum ModifierFamily
    {
        Lines,
        DotsAndPairs,
        CagesAndArrows,
        Cells,
        WholeBoard,
        OutsideClues
    }

    /// <summary>
    /// [REQ: UI-STYLE-012] The 71 player modifiers in the six families of the modifier catalog plan
    /// (docs/design/modifier-catalog-plan.md), in the order the Custom Puzzle rule book shows them.
    /// </summary>
    public static class ModifierFamilies
    {
        /// <summary>IDs below this are the first 30 modifiers; the catalog batches added the rest ("new").</summary>
        public const int FirstCatalogId = 30;

        public static readonly ModifierFamily[] Order =
        {
            ModifierFamily.Lines, ModifierFamily.DotsAndPairs, ModifierFamily.CagesAndArrows,
            ModifierFamily.Cells, ModifierFamily.WholeBoard, ModifierFamily.OutsideClues
        };

        private static readonly Dictionary<ModifierFamily, BossModifierId[]> Members = new Dictionary<ModifierFamily, BossModifierId[]>
        {
            [ModifierFamily.Lines] = new[]
            {
                BossModifierId.GermanWhispers, BossModifierId.DutchWhispers, BossModifierId.ParityLines, BossModifierId.RenbanLines,
                BossModifierId.Palindrome, BossModifierId.Thermo, BossModifierId.BetweenLines, BossModifierId.ConsecutiveLine,
                BossModifierId.SlowThermo, BossModifierId.UniqueSetLine, BossModifierId.NonconsecLine, BossModifierId.HighLowAlternating,
                BossModifierId.NabnerLine, BossModifierId.EntropicLine, BossModifierId.ModularLine, BossModifierId.ZipperLine,
                BossModifierId.FastThermo, BossModifierId.AmbiguousThermo, BossModifierId.RegionSumLine, BossModifierId.WhisperGeneralized,
                BossModifierId.NLine, BossModifierId.SumLine
            },
            [ModifierFamily.DotsAndPairs] = new[]
            {
                BossModifierId.DifferenceKropki, BossModifierId.RatioKropki, BossModifierId.FullKropki, BossModifierId.SumKropki,
                BossModifierId.GreaterLessThan, BossModifierId.XVPairs, BossModifierId.FullWhiteKropki, BossModifierId.FullBlackKropki,
                BossModifierId.DifferenceK, BossModifierId.RatioN, BossModifierId.DistancePair
            },
            [ModifierFamily.CagesAndArrows] = new[]
            {
                BossModifierId.KillerCages, BossModifierId.KillerHiddenSum, BossModifierId.CageProduct, BossModifierId.CageDifference,
                BossModifierId.CageRatio, BossModifierId.RenbanCage, BossModifierId.ArrowSums, BossModifierId.ArrowAverage,
                BossModifierId.DoubleArrow, BossModifierId.PillArrow
            },
            [ModifierFamily.Cells] = new[]
            {
                BossModifierId.EvenOdd, BossModifierId.PrimeCells, BossModifierId.FortressCells, BossModifierId.FogOfWar,
                BossModifierId.MultipleCells, BossModifierId.MirrorCells, BossModifierId.OppositeCellsSum, BossModifierId.CountingCircles,
                BossModifierId.DistanceCell
            },
            [ModifierFamily.WholeBoard] = new[]
            {
                BossModifierId.Nonconsecutive, BossModifierId.Antiknight, BossModifierId.Antiking, BossModifierId.AntiBishop,
                BossModifierId.NonconsecDiagonal, BossModifierId.DistanceGe2, BossModifierId.EntropyGlobal, BossModifierId.ModularRegions,
                BossModifierId.XSudoku, BossModifierId.HyperSudoku, BossModifierId.ExtraRegion, BossModifierId.DisjointGroups,
                BossModifierId.MagicSquareRegion, BossModifierId.ConstraintNegationZone
            },
            [ModifierFamily.OutsideClues] = new[]
            {
                BossModifierId.Quadruples, BossModifierId.Skyscrapers, BossModifierId.Sandwich, BossModifierId.XSums,
                BossModifierId.LittleKiller
            }
        };

        public static IReadOnlyList<BossModifierId> Get(ModifierFamily family) => Members[family];

        public static ModifierFamily FamilyOf(BossModifierId id)
        {
            foreach (var pair in Members)
                if (Array.IndexOf(pair.Value, id) >= 0) return pair.Key;
            throw new ArgumentOutOfRangeException(nameof(id), id, "not a player modifier");
        }

        /// <summary>All player modifiers, family by family.</summary>
        public static IEnumerable<BossModifierId> All()
        {
            foreach (var family in Order)
                foreach (var id in Members[family])
                    yield return id;
        }

        public static bool IsCatalogAddition(BossModifierId id) => (int)id >= FirstCatalogId;

        public static string LocalizedName(ModifierFamily family) => family switch
        {
            ModifierFamily.Lines => LocalizationService.T("CustomPuzzle.Family.Lines", "Lines"),
            ModifierFamily.DotsAndPairs => LocalizationService.T("CustomPuzzle.Family.DotsAndPairs", "Dots & pairs"),
            ModifierFamily.CagesAndArrows => LocalizationService.T("CustomPuzzle.Family.CagesAndArrows", "Cages & arrows"),
            ModifierFamily.Cells => LocalizationService.T("CustomPuzzle.Family.Cells", "Cells"),
            ModifierFamily.WholeBoard => LocalizationService.T("CustomPuzzle.Family.WholeBoard", "Whole board"),
            _ => LocalizationService.T("CustomPuzzle.Family.OutsideClues", "Outside clues")
        };
    }
}
