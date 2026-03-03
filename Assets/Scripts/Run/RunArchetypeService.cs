using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    public sealed class RunArchetypeService
    {
        private readonly RelicCatalogService _catalog = new();

        public RunArchetype Evaluate(RunState runState)
        {
            if (runState == null)
            {
                return RunArchetype.Undefined;
            }

            if (runState.ClassId == ClassId.ChaosMonk)
            {
                return RunArchetype.ChaosMonk;
            }

            var scores = new Dictionary<RelicCategory, int>();
            for (var i = 0; i < runState.RelicIds.Count; i++)
            {
                var category = _catalog.ResolveCategory(runState.RelicIds[i]);
                scores.TryGetValue(category, out var score);
                scores[category] = score + 1;
            }

            var economy = Get(scores, RelicCategory.Economy) + (runState.CurrentGold >= 120 ? 1 : 0);
            var survival = Get(scores, RelicCategory.Survival) + (runState.CurrentHP <= 3 ? 1 : 0);
            var modifier = Get(scores, RelicCategory.Modifier) + (runState.CurrentHeatScore >= 3f ? 1 : 0);
            var combo = Get(scores, RelicCategory.Combo);

            if (economy >= survival && economy >= modifier && economy >= combo) return RunArchetype.EconomyMerchantMonk;
            if (modifier >= survival && modifier >= combo) return RunArchetype.ModifierRuleBender;
            if (survival >= combo) return RunArchetype.SurvivalEnduringSage;
            return RunArchetype.ComboFlowMaster;
        }

        private static int Get(Dictionary<RelicCategory, int> scores, RelicCategory category)
        {
            return scores.TryGetValue(category, out var value) ? value : 0;
        }
    }
}
