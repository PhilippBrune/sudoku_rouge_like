using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Meta
{
    public sealed class AscensionService
    {
        public int GetAscensionLevel(MetaProgressionState meta)
        {
            return meta?.AscensionLevel ?? 0;
        }

        public bool TryAscend(MetaProgressionState meta)
        {
            if (meta == null) return false;
            meta.AscensionLevel++;
            meta.MaxStarCap = Mathf.Min(meta.MaxStarCap + 1, 7);
            return true;
        }

        public bool TryPrestigeReset(MetaProgressionState meta)
        {
            if (meta == null || meta.AscensionLevel <= 0) return false;

            meta.PrestigeCount++;
            meta.AscensionLevel = 0;
            meta.MaxStarCap = 5;
            return true;
        }

        public float GetAscensionDifficultyMultiplier(int ascensionLevel)
        {
            return 1f + ascensionLevel * 0.1f;
        }

        public static int BuildMonthlySeed(int year, int month)
        {
            return year * 100 + month;
        }
    }
}
