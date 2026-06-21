using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// [HARMONY-CONFIG-001] Immutable snapshot of all difficulty-scaling parameters for a given
    /// Harmony level. Built once at run-start by <see cref="HarmonyDifficultyService.BuildConfig"/>;
    /// consumed by RunArchetypeService, BossService, ShopService, and RelicService.
    /// No service other than HarmonyDifficultyService should contain Harmony level conditionals.
    /// </summary>
    public readonly struct HarmonyConfig
    {
        public readonly int Level;

        // ── Puzzle sizing ──
        /// <summary>Normalized weights for grid sizes [5×5, 6×6, 7×7, 8×8, 9×9].</summary>
        public readonly float[] GridSizeWeights;
        /// <summary>Normalized weights for star difficulty [1★ … 6★].</summary>
        public readonly float[] StarWeights;

        // ── Boss & floor modifiers ──
        /// <summary>[HARMONY-BOSS-001] Extra floor modifiers added on top of the standard (floor-index) count.
        /// Floor-specific bonus is resolved via <see cref="HarmonyDifficultyService.GetBonusFloorModifiers"/>.</summary>
        public readonly int BonusFloorModifiersBase;
        /// <summary>[HARMONY-BOSS-002] Added to both options-shown and player-picks at the boss gate,
        /// keeping the exclusion count constant (always 1 modifier can be excluded).</summary>
        public readonly int BossOptionsBonus;

        // ── Economy ──
        /// <summary>[HARMONY-ECON-001] Multiplied onto GlobalGoldMultiplier at run start (0.60–1.00).</summary>
        public readonly float GoldMultiplier;
        /// <summary>[HARMONY-ECON-002] Multiplied onto shop prices on top of the floor-index scaling (1.00–1.50).</summary>
        public readonly float ShopSurcharge;
        /// <summary>[HARMONY-ECON-003] Subtracted from starting reroll tokens (min 0).</summary>
        public readonly int StartingRerollPenalty;

        // ── Survivability ──
        /// <summary>[HARMONY-SURV-001] Subtracted from starting HP (min 3).</summary>
        public readonly int HpPenalty;
        /// <summary>[HARMONY-SURV-002] HP lost per mistake (base 1; 2 at H5; 3 at H9).</summary>
        public readonly int MistakeHpCost;
        /// <summary>[HARMONY-SURV-003] True at H7+. Combo streak decays by 1 every 8 placements without a bonus.</summary>
        public readonly bool ComboDecayEnabled;
        /// <summary>[HARMONY-SURV-004] Subtracted from starting pencil (min 3).</summary>
        public readonly int PencilPenalty;
        /// <summary>[HARMONY-SURV-005] Subtracted from max item slot cap (min 1).</summary>
        public readonly int ItemSlotCapPenalty;

        // ── Item rewards ──
        /// <summary>[HARMONY-ITEM-001] Added to each Nothing-slot probability (cap at 40%).</summary>
        public readonly float NothingChanceBonus;

        // ── RNG pressure ──
        /// <summary>[HARMONY-RNG-001] Multiplied onto positive floor effect chance (1.00 = unchanged; 0.30 at H10).</summary>
        public readonly float PositiveEffectMult;

        // ── Mechanic flags ──
        /// <summary>[HARMONY-FLAG-001] H5+: boss ??? labels are never automatically revealed even on repeat encounter.</summary>
        public readonly bool HintMaskingEnabled;
        /// <summary>[HARMONY-FLAG-002] H7+: AssignStartingRelics draws only from the T1 pool.</summary>
        public readonly bool StartingRelicT1Only;
        /// <summary>[HARMONY-FLAG-003] H7+: each mistake also costs 1 pencil in addition to HP.</summary>
        public readonly bool MistakeDrainsPencil;
        /// <summary>[HARMONY-FLAG-004] H10: rest nodes restore only 50% of stated HP (rounded up).</summary>
        public readonly bool RestHealHalved;
        /// <summary>[HARMONY-FLAG-005] H9+: cursed puzzle +50% gold/XP bonus is suppressed.</summary>
        public readonly bool CursedBonusGoldDisabled;

        // ── XP ──
        /// <summary>[HARMONY-XP-001] Multiplied onto class XP earned at run end (1.0 + level × 0.1).</summary>
        public readonly float XpMultiplier;

        // ── Pool gates (new relics / items available only at or above their harmony level) ──
        public readonly bool CrackedStoneAvail;   // H2+
        public readonly bool FrostedMirrorAvail;  // H5+
        public readonly bool VoidPetalAvail;      // H7+
        public readonly bool LanternOfVoidAvail;  // H9+
        public readonly bool BambooCompassAvail;  // H3+

        public HarmonyConfig(
            int level,
            float[] gridSizeWeights,
            float[] starWeights,
            int bonusFloorModifiersBase,
            int bossOptionsBonus,
            float goldMultiplier,
            float shopSurcharge,
            int startingRerollPenalty,
            int hpPenalty,
            int mistakeHpCost,
            bool comboDecayEnabled,
            int pencilPenalty,
            int itemSlotCapPenalty,
            float nothingChanceBonus,
            float positiveEffectMult,
            bool hintMaskingEnabled,
            bool startingRelicT1Only,
            bool mistakeDrainsPencil,
            bool restHealHalved,
            bool cursedBonusGoldDisabled,
            float xpMultiplier)
        {
            Level                    = level;
            GridSizeWeights          = gridSizeWeights;
            StarWeights              = starWeights;
            BonusFloorModifiersBase  = bonusFloorModifiersBase;
            BossOptionsBonus         = bossOptionsBonus;
            GoldMultiplier           = goldMultiplier;
            ShopSurcharge            = shopSurcharge;
            StartingRerollPenalty    = startingRerollPenalty;
            HpPenalty                = hpPenalty;
            MistakeHpCost            = mistakeHpCost;
            ComboDecayEnabled        = comboDecayEnabled;
            PencilPenalty            = pencilPenalty;
            ItemSlotCapPenalty       = itemSlotCapPenalty;
            NothingChanceBonus       = nothingChanceBonus;
            PositiveEffectMult       = positiveEffectMult;
            HintMaskingEnabled       = hintMaskingEnabled;
            StartingRelicT1Only      = startingRelicT1Only;
            MistakeDrainsPencil      = mistakeDrainsPencil;
            RestHealHalved           = restHealHalved;
            CursedBonusGoldDisabled  = cursedBonusGoldDisabled;
            XpMultiplier             = xpMultiplier;
            CrackedStoneAvail        = level >= 2;
            FrostedMirrorAvail       = level >= 5;
            VoidPetalAvail           = level >= 7;
            LanternOfVoidAvail       = level >= 9;
            BambooCompassAvail       = level >= 3;
        }
    }

    /// <summary>
    /// [HARMONY-CONFIG-001] Central authority for all Harmony difficulty scaling.
    /// Every difficulty parameter is defined here; no other service may contain per-level conditionals.
    /// </summary>
    public static class HarmonyDifficultyService
    {
        // ── Grid size weights [5×5, 6×6, 7×7, 8×8, 9×9] ──────────────────────────────────────
        private static readonly float[][] GridWeights =
        {
            new[] { 30f, 25f, 20f, 15f, 10f },  // H0
            new[] { 25f, 25f, 22f, 17f, 11f },  // H1
            new[] { 20f, 22f, 23f, 20f, 15f },  // H2
            new[] { 15f, 20f, 22f, 23f, 20f },  // H3
            new[] { 10f, 18f, 22f, 25f, 25f },  // H4
            new[] {  5f, 15f, 20f, 27f, 33f },  // H5
            new[] {  3f, 12f, 18f, 27f, 40f },  // H6
            new[] {  0f, 10f, 17f, 28f, 45f },  // H7
            new[] {  0f,  5f, 15f, 27f, 53f },  // H8
            new[] {  0f,  0f, 12f, 28f, 60f },  // H9
            new[] {  0f,  0f,  5f, 25f, 70f },  // H10
        };

        // ── Star weights [1★ … 6★] ──────────────────────────────────────────────────────────────
        private static readonly float[][] StarWeights =
        {
            new[] { 40f, 30f, 20f, 10f,  0f,  0f },  // H0
            new[] { 35f, 30f, 23f, 12f,  0f,  0f },  // H1
            new[] { 30f, 30f, 25f, 15f,  0f,  0f },  // H2
            new[] { 25f, 28f, 25f, 17f,  5f,  0f },  // H3
            new[] { 20f, 25f, 25f, 20f, 10f,  0f },  // H4
            new[] { 15f, 22f, 25f, 23f, 12f,  3f },  // H5
            new[] { 10f, 20f, 25f, 25f, 15f,  5f },  // H6
            new[] {  8f, 18f, 22f, 25f, 20f,  7f },  // H7
            new[] {  5f, 15f, 20f, 25f, 25f, 10f },  // H8
            new[] {  3f, 10f, 18f, 25f, 30f, 14f },  // H9
            new[] {  0f,  5f, 15f, 25f, 35f, 20f },  // H10
        };

        // ── Per-level scalar table ──────────────────────────────────────────────────────────────
        // Columns: bonusFloorMods, bossOptBonus, goldMult, shopSurcharge, rerollPenalty,
        //          hpPenalty, mistakeHpCost, comboDecay, pencilPenalty, slotPenalty,
        //          nothingBonus, posEffectMult, hintMask, t1Only, drainsPencil, restHalved, cursedDisabled, xpMult
        private static readonly object[][] LevelTable =
        {
          //                              bF bO gold  shop  re hp mk cd pe sl  nth  pos    hm t1  dp  rh  cg  xp
          /*H0 */ new object[] { 0, 0, 1.00f, 1.00f, 0, 0, 1, false, 0, 0, 0.000f, 1.00f, false, false, false, false, false, 1.0f },
          /*H1 */ new object[] { 0, 0, 0.96f, 1.05f, 0, 0, 1, false, 0, 0, 0.015f, 0.93f, false, false, false, false, false, 1.1f },
          /*H2 */ new object[] { 0, 0, 0.92f, 1.10f, 0, 0, 1, false, 0, 0, 0.030f, 0.86f, false, false, false, false, false, 1.2f },
          /*H3 */ new object[] { 0, 0, 0.88f, 1.15f, 0, 1, 1, false, 0, 0, 0.045f, 0.79f, false, false, false, false, false, 1.3f },
          /*H4 */ new object[] { 0, 1, 0.84f, 1.20f, 0, 1, 1, false, 1, 0, 0.060f, 0.72f, false, false, false, false, false, 1.4f },
          /*H5 */ new object[] { 0, 1, 0.80f, 1.25f, 1, 1, 2, false, 1, 0, 0.075f, 0.65f,  true, false, false, false, false, 1.5f },
          /*H6 */ new object[] { 1, 1, 0.76f, 1.30f, 1, 2, 2, false, 1, 1, 0.090f, 0.58f,  true, false, false, false, false, 1.6f },
          /*H7 */ new object[] { 1, 2, 0.72f, 1.35f, 1, 2, 2,  true, 2, 1, 0.105f, 0.51f,  true,  true,  true, false, false, 1.7f },
          /*H8 */ new object[] { 1, 2, 0.68f, 1.40f, 2, 2, 2,  true, 2, 2, 0.120f, 0.44f,  true,  true,  true, false, false, 1.8f },
          /*H9 */ new object[] { 2, 3, 0.64f, 1.45f, 2, 3, 3,  true, 2, 2, 0.135f, 0.37f,  true,  true,  true, false,  true, 1.9f },
          /*H10*/ new object[] { 2, 3, 0.60f, 1.50f, 2, 3, 3,  true, 3, 2, 0.150f, 0.30f,  true,  true,  true,  true,  true, 2.0f },
        };

        /// <summary>
        /// [HARMONY-CONFIG-001] Build the complete difficulty configuration for a given Harmony level.
        /// Level is clamped to [0, 10].
        /// </summary>
        public static HarmonyConfig BuildConfig(int level)
        {
            level = Math.Clamp(level, 0, 10);
            var row = LevelTable[level];

            return new HarmonyConfig(
                level:                   level,
                gridSizeWeights:         GridWeights[level],
                starWeights:             StarWeights[level],
                bonusFloorModifiersBase: (int)row[0],
                bossOptionsBonus:        (int)row[1],
                goldMultiplier:          (float)row[2],
                shopSurcharge:           (float)row[3],
                startingRerollPenalty:   (int)row[4],
                hpPenalty:               (int)row[5],
                mistakeHpCost:           (int)row[6],
                comboDecayEnabled:       (bool)row[7],
                pencilPenalty:           (int)row[8],
                itemSlotCapPenalty:      (int)row[9],
                nothingChanceBonus:      (float)row[10],
                positiveEffectMult:      (float)row[11],
                hintMaskingEnabled:      (bool)row[12],
                startingRelicT1Only:     (bool)row[13],
                mistakeDrainsPencil:     (bool)row[14],
                restHealHalved:          (bool)row[15],
                cursedBonusGoldDisabled: (bool)row[16],
                xpMultiplier:            (float)row[17]);
        }

        /// <summary>
        /// [HARMONY-BOSS-001] Returns the extra floor modifier count for a specific floor index
        /// at the given harmony level. Zen 3–5 only applies the bonus on floors 2–4 (indices).
        /// </summary>
        public static int GetBonusFloorModifiers(int harmonyLevel, int floorIndex)
        {
            var config = BuildConfig(harmonyLevel);
            int raw;
            if (harmonyLevel >= 9)                          raw = 2;
            else if (harmonyLevel >= 6)                     raw = 1;
            else if (harmonyLevel >= 3 && floorIndex >= 2) raw = 1;
            else                                            raw = 0;
            return Math.Min(config.BonusFloorModifiersBase, raw);
        }

        /// <summary>
        /// [HARMONY-DISPLAY-001] Player-facing display name for each Harmony level.
        /// </summary>
        public static string GetDisplayName(int level)
        {
            return level switch
            {
                0  => "Garden Path",
                1  => "Harmony I",
                2  => "Harmony II",
                3  => "Harmony III",
                4  => "Harmony IV",
                5  => "Harmony V",
                6  => "Harmony VI",
                7  => "Harmony VII",
                8  => "Harmony VIII",
                9  => "Harmony IX",
                10 => "Void Garden",
                _  => $"Harmony {level}"
            };
        }

        /// <summary>Returns the short badge label used in the HUD (e.g. "HV", "HX", "H0").</summary>
        public static string GetHudLabel(int level)
        {
            if (level == 0)  return "H0";
            if (level == 10) return "VG";
            var romanNumerals = new[] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return "H" + romanNumerals[level];
        }

        /// <summary>
        /// Returns a short description of what changes at this level compared to the previous one.
        /// Used in the Game Modes panel to inform the player of new penalties/changes before starting.
        /// </summary>
        public static string GetDescription(int level) => level switch
        {
            0  => "Base difficulty. No penalties.",
            1  => "Gold \u22124%. Shop prices +5%. Puzzles slightly larger and harder.",
            2  => "Gold \u22124%. Shop +5%. Larger grids more frequent.",
            3  => "Gold \u22124%. Shop +5%. Starting HP \u22121.",
            4  => "Gold \u22124%. Shop +5%. Boss gate shows +1 extra option. Starting pencil \u22121. Perks unlock.",
            5  => "Gold \u22124%. Shop +5%. Mistakes cost 2 HP. Starting rerolls \u22121. Boss ??? labels never auto-revealed on repeat. 6\u2605 puzzles now possible.",
            6  => "Gold \u22124%. Shop +5%. Starting HP \u22121 again. Item slot cap \u22121. Extra floor modifier per floor.",
            7  => "Gold \u22124%. Shop +5%. Boss gate +1 more option. Combo decay active. Starting relics Tier 1 only. Mistakes also drain 1 pencil. Starting pencil \u22121.",
            8  => "Gold \u22124%. Shop +5%. Item slot cap \u22121 again. Starting rerolls \u22121.",
            9  => "Gold \u22124%. Shop +5%. Starting HP \u22121. Mistakes cost 3 HP. More floor and boss modifiers. Cursed puzzle gold bonus suppressed.",
            10 => "Gold \u22124%. Shop +5%. Rest heals only 50%. Starting pencil \u22121.",
            _  => ""
        };

        /// <summary>
        /// [HARMONY-XP-001] XP multiplier applied to class XP at run end.
        /// Formula: 1.0 + level × 0.1, capped at 2.0 at H10.
        /// </summary>
        public static float GetXpMultiplier(int level) =>
            1.0f + Math.Clamp(level, 0, 10) * 0.1f;

        /// <summary>
        /// Returns the minimum Harmony level required to unlock a given Harmony Perk.
        /// Returns 99 (never) for HarmonyPerkId.None.
        /// </summary>
        public static int GetPerkMinLevel(HarmonyPerkId perk) => perk switch
        {
            HarmonyPerkId.MoonshadeOffering => 4,
            HarmonyPerkId.ScholarsBurden    => 6,
            HarmonyPerkId.VoidWard          => 8,
            HarmonyPerkId.EmptyCanvas       => 10,
            _                               => 99
        };

        /// <summary>
        /// Returns true when <paramref name="perk"/> is available at the given Harmony level.
        /// </summary>
        public static bool IsPerkAvailable(HarmonyPerkId perk, int harmonyLevel) =>
            perk != HarmonyPerkId.None && harmonyLevel >= GetPerkMinLevel(perk);

        public static string GetPerkDisplayName(HarmonyPerkId perk) => perk switch
        {
            HarmonyPerkId.MoonshadeOffering => "Moonshade Offering",
            HarmonyPerkId.ScholarsBurden    => "Scholar's Burden",
            HarmonyPerkId.VoidWard          => "Void Ward",
            HarmonyPerkId.EmptyCanvas       => "Empty Canvas",
            _                               => "No Perk"
        };

        public static string GetPerkDescription(HarmonyPerkId perk) => perk switch
        {
            HarmonyPerkId.MoonshadeOffering => "+20 gold, +2 pencil. First reward slot becomes Nothing.",
            HarmonyPerkId.ScholarsBurden    => "-1 item slot, +5 pencil, free InkWell start.",
            HarmonyPerkId.VoidWard          => "No positive floor effects. Start with 1 shield charge.",
            HarmonyPerkId.EmptyCanvas       => "No starting items. Reward slots are guaranteed Rare+.",
            _                               => "Standard Garden Run rules."
        };
    }
}
