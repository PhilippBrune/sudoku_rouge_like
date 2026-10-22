using System;
using System.Text.RegularExpressions;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;

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

        /// <summary>Stat bonuses from the class's level milestones up to <paramref name="classLevel"/>.</summary>
        // [REQ: REQ-CLASS-103] Shared by run start and the class screen's "base (+bonus)" display (GAP-023).
        public readonly struct LevelBonuses
        {
            public readonly int Hp, Pencil, Gold, Slots, Rerolls;

            public LevelBonuses(int hp, int pencil, int gold, int slots, int rerolls)
            {
                Hp = hp; Pencil = pencil; Gold = gold; Slots = slots; Rerolls = rerolls;
            }
        }

        public static LevelBonuses GetLevelBonuses(ClassId classId, int classLevel)
        {
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
            return new LevelBonuses(bonusHp, bonusPencil, bonusGold, bonusSlots, bonusRerolls);
        }

        public static RunState CreateRunState(ClassId classId, int seed,
            bool allowIrregular = true, int classLevel = 1,
            int harmonyLevel = 0, HarmonyPerkId harmonyPerk = HarmonyPerkId.None, int prestigeTier = 0)
        {
            var def = ClassCatalog.GetDefinition(classId);

            // Accumulate all stat bonuses from level milestones earned up to classLevel.
            var bonus = GetLevelBonuses(classId, classLevel);
            int bonusHp = bonus.Hp, bonusPencil = bonus.Pencil, bonusGold = bonus.Gold,
                bonusSlots = bonus.Slots, bonusRerolls = bonus.Rerolls;

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
                CurrentGold  = bonusGold + SudokuRoguelike.Meta.ClassGardenProgressionService.PrestigeStartingGold(prestigeTier),
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

            // [HARMONY-PERK-002] The perk first (its slot cost and flags), then the bag in a fixed order: the perk's items,
            // then the Level 30 class item if a slot is left (GAP-064). Empty Canvas starts with nothing.
            ApplyHarmonyPerk(state, harmonyPerk);
            AddPerkItems(state);
            if (!state.EmptyCanvasActive) AddStartingItem(state, classId, classLevel);

            return state;
        }

        // [REQ: META-XP-007] From Level 30 the run starts with the class's item (GAP-063). The id is fixed, so a seeded run
        // stays the same; tiered items take the catalog's rarity, the others their fixed one.
        private static void AddStartingItem(RunState state, ClassId classId, int classLevel)
        {
            if (classLevel < ClassCatalog.StartingItemLevel || state.ItemSlots <= state.HeldItems.Count) return;
            var (type, rarity) = ClassCatalog.GetStartingItem(classId);
            state.HeldItems.Add(new ItemInstance
            {
                Id = "start-" + type,
                Type = type,
                Rarity = ItemService.IsTiered(type) ? rarity : ItemService.GetFixedRarity(type),
                Charges = ItemService.GetDefaultCharges(type)
            });
        }

        // [HARMONY-PERK-001] Applies the selected pre-run perk to a freshly created RunState (HarmonyDifficultySystem §7).
        private static void ApplyHarmonyPerk(RunState state, HarmonyPerkId perk)
        {
            switch (perk)
            {
                case HarmonyPerkId.MoonshadeOffering:
                    // [REQ: HARMONY-PERK-003] +20 gold, +2 pencil (the maximum too: a run starts full, so the cap would take
                    // it away, GAP-067); the first reward's slot 1 is Nothing (RunDirector.BuildItemRewardSlots).
                    state.CurrentGold += 20;
                    AddPencil(state, MoonshadePencil);
                    state.MoonshadeOfferingActive = true;
                    break;
                case HarmonyPerkId.ScholarsBurden:
                    // [REQ: HARMONY-PERK-004] One item slot less, but never below one (it used to raise a bag of one to
                    // two); +5 pencil with the maximum; the Ink Well comes in AddPerkItems (GAP-064).
                    state.ItemSlots = Math.Max(1, state.ItemSlots - 1);
                    AddPencil(state, ScholarsBurdenPencil);
                    break;
                case HarmonyPerkId.VoidWard:
                    // All positive floor effects disabled; start with 1 mistake-absorb charge
                    state.MistakeShieldCharges += 1;
                    state.VoidWardActive = true;
                    break;
                case HarmonyPerkId.EmptyCanvas:
                    // [REQ: HARMONY-PERK-005] No starting items (the class item is not added); item rewards Rare or better
                    // (RunDirector.ApplyEmptyCanvas, GAP-069).
                    state.HeldItems.Clear();
                    state.EmptyCanvasActive = true;
                    break;
            }
        }

        private const int MoonshadePencil = 2;
        private const int ScholarsBurdenPencil = 5;
        private const int ScholarsBurdenTwoInkWellsFrom = 8;   // Harmony level

        private static void AddPencil(RunState state, int amount)
        {
            state.MaxPencil += amount;
            state.CurrentPencil += amount;
        }

        // [REQ: HARMONY-PERK-004] Scholar's Burden's Rare Ink Well (two from H8), first in the bag and never past its slots.
        // Fixed ids, so a seeded run starts the same.
        private static void AddPerkItems(RunState state)
        {
            if (state.HarmonyPerk != HarmonyPerkId.ScholarsBurden) return;
            var count = state.HarmonyLevel >= ScholarsBurdenTwoInkWellsFrom ? 2 : 1;
            for (var i = 1; i <= count && state.HeldItems.Count < state.ItemSlots; i++)
                state.HeldItems.Add(new ItemInstance
                {
                    Id = "perk-inkwell-" + i,
                    Type = ItemType.InkWell,
                    Rarity = ItemRarity.Rare,
                    Charges = ItemService.GetDefaultCharges(ItemType.InkWell)
                });
        }
    }
}
