using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    public sealed class AscensionService
    {
        public void ApplyAscension(MetaProgressionState meta)
        {
            if (meta == null)
            {
                return;
            }

            meta.AscensionLevel++;
            meta.MaxStarCap = Math.Min(7, meta.MaxStarCap + 1);
            meta.SeasonalChallengeUnlocked = true;
        }

        public bool TryPrestigeReset(MetaProgressionState meta)
        {
            if (meta == null || meta.AscensionLevel <= 0)
            {
                return false;
            }

            meta.PrestigeCount++;
            meta.AscensionLevel = 0;
            meta.GardenEssence = 0;
            meta.MaxStarCap = 5;
            meta.PurchasedPermanentUpgrades.Clear();
            return true;
        }

        public int BuildMonthlySeed(int year, int month)
        {
            var clampedMonth = Math.Clamp(month, 1, 12);
            return (year * 100) + clampedMonth;
        }
    }
}
