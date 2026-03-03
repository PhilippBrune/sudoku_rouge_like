using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public sealed class RelicCatalogService
    {
        public RelicCategory ResolveCategory(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                return RelicCategory.Utility;
            }

            var id = relicId.ToLowerInvariant();
            if (id.Contains("eco") || id.Contains("gold")) return RelicCategory.Economy;
            if (id.Contains("sur") || id.Contains("hp")) return RelicCategory.Survival;
            if (id.Contains("mod")) return RelicCategory.Modifier;
            if (id.Contains("combo")) return RelicCategory.Combo;
            if (id.Contains("chaos") || id.Contains("curse")) return RelicCategory.Chaos;
            return RelicCategory.Utility;
        }

        public RelicTier ResolveTier(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                return RelicTier.Tier1;
            }

            var id = relicId.ToLowerInvariant();
            if (id.Contains("legend") || id.Contains("shifting_garden") || id.Contains("silent_grid") || id.Contains("golden_root"))
            {
                return RelicTier.Legendary;
            }

            if (id.Contains("t4")) return RelicTier.Tier4;
            if (id.Contains("t3")) return RelicTier.Tier3;
            if (id.Contains("t2")) return RelicTier.Tier2;
            return RelicTier.Tier1;
        }

        public bool IsLegendary(string relicId)
        {
            return ResolveTier(relicId) == RelicTier.Legendary;
        }

        public bool IsCursed(string relicId)
        {
            return relicId != null && relicId.Contains("cursed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
