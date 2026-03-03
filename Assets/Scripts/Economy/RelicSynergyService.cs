using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public sealed class RelicSynergySnapshot
    {
        public float GoldMultiplier = 1f;
        public int MistakeShieldCharges;
        public int ComboMistakeProtectionCharges;
        public float ModifierRewardMultiplier = 1f;
        public float ModifierWeightFactor = 1f;
        public bool CarryGoldInterest;
    }

    public sealed class RelicSynergyService
    {
        private readonly RelicCatalogService _catalog = new();

        public RelicSynergySnapshot Build(IReadOnlyList<string> relicIds)
        {
            var counts = new Dictionary<RelicCategory, int>();
            for (var i = 0; i < relicIds.Count; i++)
            {
                var category = _catalog.ResolveCategory(relicIds[i]);
                counts.TryGetValue(category, out var current);
                counts[category] = current + 1;
            }

            var snapshot = new RelicSynergySnapshot();
            ApplyCategorySynergy(counts, RelicCategory.Economy, snapshot);
            ApplyCategorySynergy(counts, RelicCategory.Survival, snapshot);
            ApplyCategorySynergy(counts, RelicCategory.Modifier, snapshot);
            ApplyCategorySynergy(counts, RelicCategory.Combo, snapshot);
            ApplyCategorySynergy(counts, RelicCategory.Chaos, snapshot);
            return snapshot;
        }

        private static void ApplyCategorySynergy(Dictionary<RelicCategory, int> counts, RelicCategory category, RelicSynergySnapshot snapshot)
        {
            counts.TryGetValue(category, out var count);
            if (count < 2)
            {
                return;
            }

            if (category == RelicCategory.Economy)
            {
                snapshot.GoldMultiplier += count >= 4 ? 0.35f : count >= 3 ? 0.20f : 0.10f;
                snapshot.CarryGoldInterest = count >= 4;
            }
            else if (category == RelicCategory.Survival)
            {
                snapshot.MistakeShieldCharges += count >= 4 ? 3 : count >= 3 ? 2 : 1;
            }
            else if (category == RelicCategory.Modifier)
            {
                snapshot.ModifierWeightFactor *= count >= 4 ? 0.78f : count >= 3 ? 0.85f : 0.92f;
                snapshot.ModifierRewardMultiplier += count >= 4 ? 0.35f : count >= 3 ? 0.20f : 0.08f;
            }
            else if (category == RelicCategory.Combo)
            {
                snapshot.ComboMistakeProtectionCharges += count >= 4 ? 1 : 0;
                snapshot.GoldMultiplier += count >= 3 ? 0.10f : 0.04f;
            }
            else if (category == RelicCategory.Chaos)
            {
                snapshot.GoldMultiplier += 0.05f * count;
            }
        }
    }
}
