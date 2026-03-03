using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public enum HeatBand
    {
        Relaxed,
        Focused,
        HighTension,
        Critical,
        BossTierStress
    }

    public static class HeatScoreService
    {
        private static readonly Dictionary<int, float> GridComplexity = new()
        {
            [5] = 1.0f,
            [6] = 1.2f,
            [7] = 1.5f,
            [8] = 1.9f,
            [9] = 2.4f
        };

        private static readonly Dictionary<BossModifierTier, float> ConstraintLoad = new()
        {
            [BossModifierTier.Tier1] = 1.15f,
            [BossModifierTier.Tier2] = 1.30f,
            [BossModifierTier.Tier3] = 1.50f,
            [BossModifierTier.Tier4] = 1.60f,
            [BossModifierTier.Tier5] = 1.75f
        };

        public static float ComputeHeatScore(int boardSize, float missingPercentage, BossModifierTier modifierTier, bool arithmetic, bool fog, bool dualModifiers, float hpRatio, float pencilRatio)
        {
            var g = GridComplexity.TryGetValue(boardSize, out var gValue) ? gValue : 1f;
            var s = 1f + (missingPercentage * 1.8f);
            var c = ConstraintLoad[modifierTier];
            var i = ComputeInterference(arithmetic, fog, dualModifiers);
            var r = ComputeResourcePressure(hpRatio, pencilRatio);

            return g * s * c * i * r;
        }

        public static float ComputeResourcePressure(float hpRatio, float pencilRatio)
        {
            var hp = Math.Clamp(hpRatio, 0f, 1f);
            var pencil = Math.Clamp(pencilRatio, 0f, 1f);
            return 1f + ((1f - hp) * 0.5f) + ((1f - pencil) * 0.4f);
        }

        public static float ComputeInterference(bool arithmetic, bool fog, bool dualModifiers)
        {
            if (dualModifiers)
            {
                return 1.40f;
            }

            if (fog)
            {
                return 1.25f;
            }

            if (arithmetic)
            {
                return 1.15f;
            }

            return 1f;
        }

        public static bool IsValidHeatStep(float previousHeat, float nextHeat, bool isBossEncounter)
        {
            if (previousHeat <= 0f)
            {
                return true;
            }

            var growth = (nextHeat - previousHeat) / previousHeat;
            var cap = isBossEncounter ? 0.70f : 0.35f;
            return growth <= cap;
        }

        public static HeatBand ToBand(float heat)
        {
            if (heat < 2f)
            {
                return HeatBand.Relaxed;
            }

            if (heat < 3f)
            {
                return HeatBand.Focused;
            }

            if (heat < 4f)
            {
                return HeatBand.HighTension;
            }

            if (heat < 5.5f)
            {
                return HeatBand.Critical;
            }

            return HeatBand.BossTierStress;
        }
    }
}
