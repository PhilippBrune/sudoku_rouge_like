using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    public static class ModifierFactory
    {
        public static List<IOrderedConstraintRule> BuildRules(
            List<BossModifierId> modifiers, ModifierOverlayData overlay)
        {
            var rules = new List<IOrderedConstraintRule>();

            for (var i = 0; i < modifiers.Count; i++)
            {
                switch (modifiers[i])
                {
                    case BossModifierId.GermanWhispers:
                        rules.Add(new GermanWhispersRule(overlay.Lines));
                        break;
                    case BossModifierId.DutchWhispers:
                        rules.Add(new DutchWhispersRule(overlay.Lines));
                        break;
                    case BossModifierId.ParityLines:
                        rules.Add(new ParityLinesRule(overlay.Lines));
                        break;
                    case BossModifierId.RenbanLines:
                        rules.Add(new RenbanLinesRule(overlay.Lines));
                        break;
                    case BossModifierId.DifferenceKropki:
                        rules.Add(new DifferenceKropkiRule(overlay.Dots));
                        break;
                    case BossModifierId.RatioKropki:
                        rules.Add(new RatioKropkiRule(overlay.Dots));
                        break;
                    case BossModifierId.KillerCages:
                        rules.Add(new KillerCageRule(overlay.Cages));
                        break;
                    case BossModifierId.ArrowSums:
                        rules.Add(new ArrowSumRule(overlay.Arrows));
                        break;
                    case BossModifierId.FogOfWar:
                        rules.Add(new FogOfWarRule(overlay));
                        break;
                }
            }

            return rules;
        }
    }
}
