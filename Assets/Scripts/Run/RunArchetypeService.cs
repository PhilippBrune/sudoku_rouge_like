using System;
using System.Text.RegularExpressions;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    public static class RunArchetypeService
    {
        // Matches stat-bonus fragments in ClassCatalog unlock strings, e.g.
        // "+2 HP", "+1 Pencil", "+10 Starting Gold", "+1 Item Slot", "+1 Reroll Token", "+1 Starting Relic"
        private static readonly Regex StatRx = new Regex(
            @"\+(\d+)\s+(HP|Pencil|Starting Gold|Item Slot|Reroll Token|Starting Relic)",
            RegexOptions.Compiled);

        /// <summary>Returns the total number of starting relics for the given class at the given level,
        /// including BaseStartingRelics and any "+1 Starting Relic" milestones earned.</summary>
        public static int GetStartingRelicCount(ClassId classId, int classLevel)
        {
            var def = ClassCatalog.GetDefinition(classId);
            var bonus = 0;
            var unlocks = ClassCatalog.GetAllUnlocks(classId);
            if (unlocks != null)
                foreach (var (level, text) in unlocks)
                {
                    if (level > classLevel) break;
                    foreach (Match m in StatRx.Matches(text))
                        if (m.Groups[2].Value == "Starting Relic")
                            bonus += int.Parse(m.Groups[1].Value);
                }
            return def.BaseStartingRelics + bonus;
        }

        public static RunState CreateRunState(ClassId classId, int seed,
            bool allowIrregular = true, int classLevel = 1,
            int harmonyLevel = 0, HarmonyPerkId harmonyPerk = HarmonyPerkId.None)
        {
            var def = ClassCatalog.GetDefinition(classId);

            // Accumulate all stat bonuses from level milestones earned up to classLevel.
            int bonusHp = 0, bonusPencil = 0, bonusGold = 0, bonusSlots = 0, bonusRerolls = 0;
            var unlocks = ClassCatalog.GetAllUnlocks(classId);
            if (unlocks != null)
            {
                foreach (var (level, text) in unlocks)
                {
                    if (level > classLevel) break; // unlocks are sorted ascending by level
                    foreach (Match m in StatRx.Matches(text))
                    {
                        var n = int.Parse(m.Groups[1].Value);
                        switch (m.Groups[2].Value)
                        {
                            case "HP":              bonusHp      += n; break;
                            case "Pencil":          bonusPencil  += n; break;
                            case "Starting Gold":   bonusGold    += n; break;
                            case "Item Slot":       bonusSlots   += n; break;
                            case "Reroll Token":    bonusRerolls += n; break;
                        }
                    }
                }
            }

            // [HARMONY-CONFIG-001] Build harmony config and apply all difficulty scaling.
            var harmony = HarmonyDifficultyService.BuildConfig(harmonyLevel);

            var baseHp     = def.BaseHP      + bonusHp;
            var basePencil = def.BasePencil  + bonusPencil;
            var baseSlots  = def.BaseItemSlots + bonusSlots;

            var finalHp     = Math.Max(3, baseHp     - harmony.HpPenalty);
            var finalPencil = Math.Max(3, basePencil - harmony.PencilPenalty);
            var finalSlots  = Math.Max(1, baseSlots  - harmony.ItemSlotCapPenalty);
            var finalRerolls = Math.Max(0, bonusRerolls - harmony.StartingRerollPenalty);

            var state = new RunState
            {
                ClassId   = classId,
                ClassLevel = classLevel,
                Mode      = GameMode.GardenRun,
                Seed      = seed,
                RunNumber = 1,
                Depth     = 0,
                CurrentFloor = 0,
                TotalFloors  = 5,
                CurrentHP    = finalHp,
                MaxHP        = finalHp,
                CurrentPencil = finalPencil,
                MaxPencil     = finalPencil,
                CurrentGold  = bonusGold,
                ItemSlots    = finalSlots,
                RerollTokens = finalRerolls,
                AllowIrregularPuzzles = allowIrregular,
                // [HARMONY-001] Persist level and derived per-run settings
                HarmonyLevel       = harmonyLevel,
                HarmonyPerk        = harmonyPerk,
                MistakeHpCost      = harmony.MistakeHpCost,
                ComboDecayEnabled  = harmony.ComboDecayEnabled,
                GlobalGoldMultiplier = harmony.GoldMultiplier,
                HarmonyConfig      = harmony,
            };

            // [HARMONY-PERK-001] Apply selected perk modifications at run start.
            ApplyHarmonyPerk(state, harmonyPerk);

            return state;
        }

        // [HARMONY-PERK-001] Applies the selected pre-run perk to a freshly created RunState.
        private static void ApplyHarmonyPerk(RunState state, HarmonyPerkId perk)
        {
            switch (perk)
            {
                case HarmonyPerkId.MoonshadeOffering:
                    // +20g, +2 pencil; first puzzle reward slot 1 forced Nothing (enforced by RewardService)
                    state.CurrentGold += 20;
                    state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + 2);
                    state.MoonshadeOfferingActive = true;
                    break;
                case HarmonyPerkId.ScholarsBurden:
                    // Max item slots -1 (min 2); grant +5 pencil + 2 InkWells at H8+, 1 otherwise
                    state.ItemSlots = Math.Max(2, state.ItemSlots - 1);
                    state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + 5);
                    state.ScholarsBurdenGrantsInkWell = true;
                    break;
                case HarmonyPerkId.VoidWard:
                    // All positive floor effects disabled; start with 1 mistake-absorb charge
                    state.MistakeShieldCharges += 1;
                    state.VoidWardActive = true;
                    break;
                case HarmonyPerkId.EmptyCanvas:
                    // Starting items stripped; reward slots guaranteed Rare+ (enforced by RewardService)
                    state.HeldItems.Clear();
                    state.EmptyCanvasActive = true;
                    break;
            }
        }
    }
}
