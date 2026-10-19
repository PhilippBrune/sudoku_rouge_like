using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class ModifierFactory
    {
        public static List<IOrderedConstraintRule> BuildRules(List<BossModifierId> modifiers)
        {
            var rules = new List<IOrderedConstraintRule>();

            for (var i = 0; i < modifiers.Count; i++)
            {
                switch (modifiers[i])
                {
                    case BossModifierId.GermanWhispers:
                        rules.Add(new GermanWhispersRule());
                        break;
                    case BossModifierId.DutchWhispers:
                        rules.Add(new DutchWhispersRule());
                        break;
                    case BossModifierId.ParityLines:
                        rules.Add(new ParityLinesRule());
                        break;
                    case BossModifierId.RenbanLines:
                        rules.Add(new RenbanLinesRule());
                        break;
                    case BossModifierId.DifferenceKropki:
                        rules.Add(new DifferenceKropkiRule());
                        break;
                    case BossModifierId.RatioKropki:
                        rules.Add(new RatioKropkiRule());
                        break;
                    case BossModifierId.KillerCages:
                        rules.Add(new KillerCagesRule());
                        break;
                    case BossModifierId.ArrowSums:
                        rules.Add(new ArrowSumsRule());
                        break;
                    case BossModifierId.FogOfWar:
                        rules.Add(new FogOfWarRule());
                        break;
                    case BossModifierId.Palindrome:
                        rules.Add(new PalindromeRule());
                        break;
                    case BossModifierId.Thermo:
                        rules.Add(new ThermoRule());
                        break;
                    case BossModifierId.BetweenLines:
                        rules.Add(new BetweenLinesRule());
                        break;
                    case BossModifierId.EvenOdd:
                        rules.Add(new EvenOddRule());
                        break;
                    case BossModifierId.Nonconsecutive:
                        rules.Add(new NonconsecutiveRule());
                        break;
                    case BossModifierId.Antiknight:
                        rules.Add(new AntiknightRule());
                        break;
                    case BossModifierId.Antiking:
                        rules.Add(new AntikingRule());
                        break;
                    case BossModifierId.AntiBishop:
                        rules.Add(new AntiBishopRule());
                        break;
                    case BossModifierId.NonconsecDiagonal:
                        rules.Add(new NonconsecDiagonalRule());
                        break;
                    case BossModifierId.DistanceGe2:
                        rules.Add(new DistanceGe2Rule());
                        break;
                    case BossModifierId.EntropyGlobal:
                        rules.Add(new EntropyGlobalRule());
                        break;
                    case BossModifierId.ModularRegions:
                        rules.Add(new ModularRegionsRule());
                        break;
                    case BossModifierId.ConsecutiveLine:
                        rules.Add(new ConsecutiveLineRule());
                        break;
                    case BossModifierId.SlowThermo:
                        rules.Add(new SlowThermoRule());
                        break;
                    case BossModifierId.UniqueSetLine:
                        rules.Add(new UniqueSetLineRule());
                        break;
                    case BossModifierId.FullKropki:
                        // Negative inference: undotted pairs cannot have diff=1 or ratio=2
                        rules.Add(new FullKropkiRule());
                        // Positive constraints: dotted pairs MUST satisfy their marker
                        rules.Add(new DifferenceKropkiRule());
                        rules.Add(new RatioKropkiRule());
                        break;
                    case BossModifierId.SumKropki:
                        rules.Add(new SumKropkiRule());
                        break;
                    case BossModifierId.GreaterLessThan:
                        rules.Add(new GreaterLessThanRule());
                        break;
                    case BossModifierId.XVPairs:
                        rules.Add(new XVPairsRule());
                        break;
                    case BossModifierId.PrimeCells:
                        rules.Add(new PrimeCellsRule());
                        break;
                    case BossModifierId.FortressCells:
                        rules.Add(new FortressCellsRule());
                        break;

                    // Catalog batch B1: line variants (ConstraintRules.LineVariants.cs)
                    case BossModifierId.NonconsecLine:
                        rules.Add(new NonconsecLineRule());
                        break;
                    case BossModifierId.HighLowAlternating:
                        rules.Add(new HighLowLineRule());
                        break;
                    case BossModifierId.NabnerLine:
                        rules.Add(new NabnerLineRule());
                        break;
                    case BossModifierId.EntropicLine:
                        rules.Add(new TriadLineRule(LineType.EntropicLine));
                        break;
                    case BossModifierId.ModularLine:
                        rules.Add(new TriadLineRule(LineType.ModularLine));
                        break;
                    case BossModifierId.ZipperLine:
                        rules.Add(new ZipperLineRule());
                        break;
                    case BossModifierId.FastThermo:
                        rules.Add(new FastThermoRule());
                        break;
                    case BossModifierId.AmbiguousThermo:
                        rules.Add(new AmbiguousThermoRule());
                        break;

                    // Catalog batch B2: dots, pairs, cells (ConstraintRules.DotsPairsCells.cs)
                    case BossModifierId.FullWhiteKropki:
                        rules.Add(new FullKropkiSetRule(black: false));
                        rules.Add(new DifferenceKropkiRule());
                        break;
                    case BossModifierId.FullBlackKropki:
                        rules.Add(new FullKropkiSetRule(black: true));
                        rules.Add(new RatioKropkiRule());
                        break;
                    case BossModifierId.DifferenceK:
                        rules.Add(new DifferenceKPairRule());
                        break;
                    case BossModifierId.MultipleCells:
                        rules.Add(new MultipleCellsRule());
                        break;
                    case BossModifierId.MirrorCells:
                        rules.Add(new MirrorCellsRule());
                        break;
                }
            }

            return rules;
        }
    }
}
