using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Items
{
    public sealed class ItemService
    {
        private readonly Random _random;

        public ItemService(int seed)
        {
            _random = new Random(seed);
        }

        // ── Class-Exclusive Definitions ── [REQ: CLASS-EXCL-001] [REQ: CLASS-EXCL-002] [REQ: CLASS-EXCL-003]
        // 8 class-exclusive items (L15), one per class; UnlockLevel = 15

        private sealed class ExclusiveDef
        {
            public ClassId ClassId;    // [REQ: CLASS-EXCL-002]
            public int UnlockLevel;   // [REQ: CLASS-EXCL-003]
        }

        private static readonly Dictionary<ItemType, ExclusiveDef> ClassExclusiveItems = new()
        {
            [ItemType.LoadedCoin]      = new ExclusiveDef { ClassId = ClassId.NumberFreak,        UnlockLevel = 15 },
            [ItemType.MonksBeads]      = new ExclusiveDef { ClassId = ClassId.GardenMonk,         UnlockLevel = 15 },
            [ItemType.AnnotatedFolio]  = new ExclusiveDef { ClassId = ClassId.ShrineArchivist,    UnlockLevel = 15 },
            [ItemType.DoubleOrQuits]   = new ExclusiveDef { ClassId = ClassId.KoiGambler,         UnlockLevel = 15 },
            [ItemType.WornChisel]      = new ExclusiveDef { ClassId = ClassId.StoneGardener,      UnlockLevel = 15 },
            [ItemType.DimLantern]      = new ExclusiveDef { ClassId = ClassId.LanternSeer,        UnlockLevel = 15 },
            [ItemType.ReedPledge]      = new ExclusiveDef { ClassId = ClassId.ReedDuelist,        UnlockLevel = 15 },
            [ItemType.SurveyNotes]     = new ExclusiveDef { ClassId = ClassId.QuietCartographer,  UnlockLevel = 15 },
        };

        /// <summary>Returns true if <paramref name="type"/> can appear in the given class context.
        /// Pass ClassId=0 (no class) to exclude all class-exclusive items.</summary>
        // [REQ: CLASS-EXCL-004] Gate: exclusive items only rollable when classId matches and classLevel >= UnlockLevel
        public static bool IsAvailableForClass(ItemType type, ClassId classId, int classLevel)
        {
            if (!ClassExclusiveItems.TryGetValue(type, out var def)) return true; // not exclusive
            return classId == def.ClassId && classLevel >= def.UnlockLevel;
        }

        public static bool IsClassExclusive(ItemType type) => ClassExclusiveItems.ContainsKey(type);

        public static ClassId GetExclusiveClass(ItemType type) =>
            ClassExclusiveItems.TryGetValue(type, out var d) ? d.ClassId : (ClassId)0;

        public static int GetExclusiveUnlockLevel(ItemType type) =>
            ClassExclusiveItems.TryGetValue(type, out var d) ? d.UnlockLevel : 0;

        public static ItemType? GetExclusiveItemForClass(ClassId classId)
        {
            foreach (var kvp in ClassExclusiveItems)
                if (kvp.Value.ClassId == classId) return kvp.Key;
            return null;
        }

        // ── Slot Rolling ──

        // [REQ: ITEM-SLOT-001] Item reward slots by star: 1★=2, 2★=3, 3★=3, 4★=4, 5★=5
        public static int GetSlotCount(int stars, int bonusSlots = 0)
        {
            var baseSlots = stars switch
            {
                1 => 2,
                2 => 3,
                3 => 3,
                4 => 4,
                _ => 5
            };
            return baseSlots + bonusSlots;
        }

        // [REQ: ITEM-SLOT-002] Nothing slot probability: 1★=25%, 2★=22%, 3★=18%, 4★=15%, 5★=12%
        public static float GetNothingChance(int stars)
        {
            return stars switch
            {
                1 => 0.25f,
                2 => 0.22f,
                3 => 0.18f,
                4 => 0.15f,
                _ => 0.12f
            };
        }

        public List<ItemInstance> RollSlots(int stars, int classLevel, int bonusSlots = 0,
            ClassId classId = (ClassId)0)
        {
            var count = GetSlotCount(stars, bonusSlots);
            var nothingChance = GetNothingChance(stars);
            var slots = new List<ItemInstance>(count);
            var hasReal = false;

            for (var i = 0; i < count; i++)
            {
                if (_random.NextDouble() < nothingChance && (hasReal || i < count - 1))
                {
                    slots.Add(null);
                    continue;
                }

                var rarity = RollItemRarity(stars, classLevel);
                var type = RollItemType(rarity, classLevel, classId);
                var item = new ItemInstance
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Type = type,
                    Rarity = IsTiered(type) ? rarity : GetFixedRarity(type),
                    Charges = GetDefaultCharges(type)
                };
                slots.Add(item);
                hasReal = true;
            }

            if (!hasReal && slots.Count > 0)
            {
                var rarity = RollItemRarity(stars, classLevel);
                var type = RollItemType(rarity, classLevel, classId);
                slots[slots.Count - 1] = new ItemInstance
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Type = type,
                    Rarity = IsTiered(type) ? rarity : GetFixedRarity(type),
                    Charges = GetDefaultCharges(type)
                };
            }

            // Dedup: re-roll any slot whose item type was already offered in an earlier slot.
            // Try up to 5 alternative rolls per duplicate so the pool stays exhaustible.
            var seen = new HashSet<ItemType>();
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                if (!seen.Add(slots[i].Type))
                {
                    var rarity = slots[i].Rarity;
                    var newType = slots[i].Type;
                    for (var attempt = 0; attempt < 5 && seen.Contains(newType); attempt++)
                        newType = RollItemType(rarity, classLevel, classId);
                    if (newType != slots[i].Type)
                    {
                        slots[i] = new ItemInstance
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Type = newType,
                            Rarity = IsTiered(newType) ? rarity : GetFixedRarity(newType),
                            Charges = GetDefaultCharges(newType)
                        };
                        seen.Add(newType);
                    }
                }
            }

            return slots;
        }

        /// <summary>Rolls one non-Nothing item for the LoadedCoin effect.</summary>
        public ItemInstance RollOneItem(int stars, int classLevel, ClassId classId = (ClassId)0)
        {
            var rarity = RollItemRarity(stars, classLevel);
            var type = RollItemType(rarity, classLevel, classId);
            return new ItemInstance
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = type,
                Rarity = IsTiered(type) ? rarity : GetFixedRarity(type),
                Charges = GetDefaultCharges(type)
            };
        }

        // ── Rarity & Type ──

        private ItemRarity RollItemRarity(int stars, int classLevel)
        {
            var roll = _random.NextDouble();

            if (classLevel >= 15) // [REQ: ITEM-EPIC-001] Epic items only available at class Level 15+
            {
                var epicChance = 0.02 + (classLevel - 15) * 0.002;
                if (roll < epicChance) return ItemRarity.Epic;
            }

            var rareChance = 0.15 + stars * 0.05;
            return roll < rareChance ? ItemRarity.Rare : ItemRarity.Normal;
        }

        private ItemType RollItemType(ItemRarity rarity, int classLevel, ClassId classId = (ClassId)0)
        {
            // F9: The following items have CSV manifest entries and PNG art previews in
            // Assets/Resources/GeneratedIcons/ but are intentionally ABSENT from every roll pool below.
            // They are reserved for future mechanics and must not be added to tiered / rare / epic arrays:
            //   Tea Cup, Moss Stone, Rice Bowl, Pebble, Jade Amulet, Wind Bell,
            //   Water Basin, Sacred Bell, Sun Medallion, Compass of Order.
            // Their PNG files exist only as approved art; adding them to any pool requires a
            // separate design pass that defines their mechanical effects.

            // Class-exclusive item: 25% chance to inject the exclusive if unlocked
            if (classId != (ClassId)0 && _random.NextDouble() < 0.25)
            {
                foreach (var kvp in ClassExclusiveItems)
                {
                    if (kvp.Value.ClassId == classId && classLevel >= kvp.Value.UnlockLevel)
                        return kvp.Key;
                }
            }

            if (rarity == ItemRarity.Epic && _random.NextDouble() < 0.40)
            {
                var epics = new[] { ItemType.KoiDragonScale, ItemType.GoldenKintsugiJar, ItemType.SilkFan };
                return epics[_random.Next(epics.Length)];
            }

            if (rarity == ItemRarity.Rare && _random.NextDouble() < 0.30)
            {
                var rares = new[] { ItemType.GinkgoLeaf, ItemType.RicePaperUmbrella, ItemType.TempleIncense, ItemType.GoldenKoi, ItemType.TempleStamp };
                return rares[_random.Next(rares.Length)];
            }

            var tiered = new[]
            {
                ItemType.Solver, ItemType.Finder, ItemType.InkWell, ItemType.MeditationStone,
                ItemType.WindChime, ItemType.PatternScroll, ItemType.KoiReflection, ItemType.LanternOfClarity,
                ItemType.BambooScroll, ItemType.GardenLantern
            };
            return tiered[_random.Next(tiered.Length)];
        }

        // ── Item Classification ──

        public static bool IsTiered(ItemType type)
        {
            var fallback = type switch
            {
                ItemType.Solver or ItemType.Finder or ItemType.InkWell or ItemType.MeditationStone
                    or ItemType.WindChime or ItemType.PatternScroll or ItemType.KoiReflection
                    or ItemType.LanternOfClarity
                    or ItemType.BambooScroll or ItemType.GardenLantern => true,
                _ => false
            };
            return fallback;
        }

        public static ItemRarity GetFixedRarity(ItemType type)
        {
            // F7: GardenRake, OfferingBowl, PruningShears, ZenSandSifter are obtainable ONLY via Event
            // nodes or Shop stock. They are NOT in any roll pool — do not add them without a design pass.
            return type switch
            {
                ItemType.GardenRake or ItemType.OfferingBowl or ItemType.PruningShears
                    or ItemType.ZenSandSifter => ItemRarity.Normal,
                ItemType.GinkgoLeaf or ItemType.RicePaperUmbrella or ItemType.TempleIncense => ItemRarity.Rare,
                ItemType.KoiDragonScale or ItemType.GoldenKintsugiJar or ItemType.SilkFan => ItemRarity.Epic,
                // Class-exclusive items are always Rare
                ItemType.LoadedCoin or ItemType.MonksBeads or ItemType.AnnotatedFolio
                    or ItemType.DoubleOrQuits or ItemType.WornChisel or ItemType.DimLantern
                    or ItemType.ReedPledge or ItemType.SurveyNotes => ItemRarity.Rare,
                // General-pool items: BambooScroll + GardenLantern are Normal; GoldenKoi + TempleStamp are Rare
                ItemType.BambooScroll or ItemType.GardenLantern => ItemRarity.Normal,
                ItemType.GoldenKoi or ItemType.TempleStamp => ItemRarity.Rare,
                _ => ItemRarity.Normal
            };
        }

        // ── Shield (Silk Fan) ──

        /// <summary>Shield points granted by one use of Silk Fan.</summary>
        public static int GetSilkFanShieldAmount() => 5;

        // ── Item Effects (rarity-scaled) ──
        // [REDESIGN] Solver: Normal = fill cell + highlight box candidates; Rare = fill cell + complete its box;
        //            Epic = auto-complete best row, column, OR box (player's choice).
        // GetSolverNeighborCount is kept for RunDirector compatibility; values updated to drive new tiers:
        //   Normal=0 (highlight-only, no auto-neighbors), Rare=box, Epic=best-zone.
        //   The actual box/zone logic is in RunDirector.ApplySolverItem — this flag drives which path.

        public static int GetSolverNeighborCount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 4 : rarity == ItemRarity.Rare ? 3 : 2;

        // [REDESIGN] Finder: Epic tier reveals ALL cells of the same digit on the board.
        public static int GetFinderHighlightCount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? int.MaxValue : rarity == ItemRarity.Rare ? 3 : 1;

        // Convenience: true when FinderHighlightCount means “reveal all matching cells”.
        public static bool IsFinderRevealAll(ItemRarity rarity) => rarity == ItemRarity.Epic;

        public static int GetInkWellAmount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 12 : rarity == ItemRarity.Rare ? 11 : 10;

        public static int GetMeditationStoneAmount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 5 : rarity == ItemRarity.Rare ? 4 : 3;

        public static int GetLanternOfClarityMoves(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 12 : rarity == ItemRarity.Rare ? 11 : 10;

        public static int GetKoiReflectionCells(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 5 : rarity == ItemRarity.Rare ? 4 : 3;

        public static int GetPatternScrollZones(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 4 : rarity == ItemRarity.Rare ? 2 : 1;

        /// <summary>
        /// Default charge count for a newly-rolled item of the given type.
        /// Most items have 1 charge; Silk Fan has 2 (its redesigned effect).
        /// </summary>
        public static int GetDefaultCharges(ItemType type) =>
            type == ItemType.SilkFan ? 2 : 1;

        // ── Pricing ──

        public static int GetBasePrice(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Normal => 15,
                ItemRarity.Rare => 30,
                ItemRarity.Epic => 60,
                _ => 15
            };
        }

        public static int GetShopPrice(ItemRarity rarity, int floorIndex)
        {
            return (int)Math.Round(GetBasePrice(rarity) * (1f + floorIndex * 0.5f));
        }

        // ── Name & Description ──

        private static string ItemNameKey(ItemType type) => $"Item.Name.{type}";

        private static string ItemDescriptionKey(ItemType type, ItemRarity rarity)
        {
            return type switch
            {
                ItemType.Solver => rarity == ItemRarity.Epic
                    ? "Item.Description.Solver.Epic"
                    : rarity == ItemRarity.Rare
                        ? "Item.Description.Solver.Rare"
                        : "Item.Description.Solver.Normal",
                ItemType.Finder => rarity == ItemRarity.Epic
                    ? "Item.Description.Finder.Epic"
                    : "Item.Description.Finder.NormalRare",
                ItemType.WindChime => rarity == ItemRarity.Epic
                    ? "Item.Description.WindChime.Epic"
                    : rarity == ItemRarity.Rare
                        ? "Item.Description.WindChime.Rare"
                        : "Item.Description.WindChime.Normal",
                ItemType.KoiReflection => rarity == ItemRarity.Epic
                    ? "Item.Description.KoiReflection.Epic"
                    : "Item.Description.KoiReflection.NormalRare",
                _ => $"Item.Description.{type}"
            };
        }

        public static IEnumerable<string> GetLocalizationKeys()
        {
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                yield return ItemNameKey(type);
                foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                    yield return ItemDescriptionKey(type, rarity);
            }
        }

        public static string GetItemName(ItemType type)
        {
            var fallback = type switch
            {
                ItemType.Solver => "Solver",
                ItemType.Finder => "Finder",
                ItemType.InkWell => "Ink Well",
                ItemType.MeditationStone => "Meditation Stone",
                ItemType.WindChime => "Wind Chime",
                ItemType.PatternScroll => "Pattern Scroll",
                ItemType.KoiReflection => "Koi Reflection",
                ItemType.LanternOfClarity => "Lantern of Clarity",
                ItemType.GardenRake => "Garden Rake",
                ItemType.OfferingBowl => "Offering Bowl",
                ItemType.PruningShears => "Pruning Shears",
                ItemType.ZenSandSifter => "Zen Sand Sifter",
                ItemType.GinkgoLeaf => "Ginkgo Leaf",
                ItemType.RicePaperUmbrella => "Rice Paper Umbrella",
                ItemType.TempleIncense => "Temple Incense",
                ItemType.KoiDragonScale => "Koi Dragon Scale",
                ItemType.GoldenKintsugiJar => "Golden Kintsugi Jar",
                ItemType.SilkFan => "Silk Fan",
                // Class-exclusive items (L15)
                ItemType.LoadedCoin      => "Loaded Coin",
                ItemType.MonksBeads      => "Monk's Beads",
                ItemType.AnnotatedFolio  => "Annotated Folio",
                ItemType.DoubleOrQuits   => "Double or Quits",
                ItemType.WornChisel      => "Worn Chisel",
                ItemType.DimLantern      => "Dim Lantern",
                ItemType.ReedPledge      => "Reed Pledge",
                ItemType.SurveyNotes     => "Survey Notes",
                // General-pool items
                ItemType.BambooScroll    => "Bamboo Scroll",
                ItemType.GardenLantern   => "Garden Lantern",
                ItemType.GoldenKoi       => "Golden Koi",
                ItemType.TempleStamp     => "Temple Stamp",
                ItemType.BambooCompass  => "Bamboo Compass",
                _ => type.ToString()
            };
            return LocalizationService.T(ItemNameKey(type), fallback);
        }

        public static string GetIconName(ItemType type)
        {
            return type switch
            {
                ItemType.Solver            => "solver",
                ItemType.Finder            => "finder",
                ItemType.InkWell           => "ink_well",
                ItemType.MeditationStone   => "meditation_stone",
                ItemType.WindChime         => "wind_chime",
                ItemType.PatternScroll     => "pattern_scroll",
                ItemType.KoiReflection     => "koi_reflection",
                ItemType.LanternOfClarity  => "lantern_of_clarity",
                ItemType.GardenRake        => "garden_rake",
                ItemType.OfferingBowl      => "offering_bowl",
                ItemType.PruningShears     => "pruning_shears",
                ItemType.ZenSandSifter     => "zen_sand_sifter",
                ItemType.GinkgoLeaf        => "ginkgo_leaf",
                ItemType.RicePaperUmbrella => "rice_paper_umbrella",
                ItemType.TempleIncense     => "temple_incense",
                ItemType.KoiDragonScale    => "koi_dragon_scale",
                ItemType.GoldenKintsugiJar => "golden_kintsugi_jar",
                ItemType.SilkFan           => "silk_fan",
                // Class-exclusive items (L15)
                ItemType.LoadedCoin        => "loaded_coin",
                ItemType.MonksBeads        => "monks_beads",
                ItemType.AnnotatedFolio    => "annotated_folio",
                ItemType.DoubleOrQuits     => "double_or_quits",
                ItemType.WornChisel        => "worn_chisel",
                ItemType.DimLantern        => "dim_lantern",
                ItemType.ReedPledge        => "reed_pledge",
                ItemType.SurveyNotes       => "survey_notes",
                // Harmony Difficulty items
                ItemType.BambooCompass     => "bamboo_compass",
                // General-pool items
                ItemType.BambooScroll      => "bamboo_scroll",
                ItemType.GardenLantern     => "garden_lantern",
                ItemType.GoldenKoi         => "golden_koi",
                ItemType.TempleStamp       => "temple_seal",
                _ => ""
            };
        }

        // [REQ: CURSE-ACQUIRE-003] [REQ: CURSE-INT-007] Curse-granting items expose their curse cost in the tooltip; ApplyItemEffect calls ApplyCurse for these item types
        public static string GetItemDescription(ItemType type, ItemRarity rarity)
        {
            return type switch
            {
                // [REDESIGN] Solver tiers:
                // Normal: fill selected cell + highlight remaining candidates in its box
                // Rare:   fill selected cell + auto-complete its entire box
                // Epic:   auto-complete the best (most-filled) row, column, OR box (player chooses zone)
                ItemType.Solver => rarity == ItemRarity.Epic
                    ? LocalizationService.T(ItemDescriptionKey(type, rarity), "Click a zone (row/column/box) to auto-complete it.")
                    : rarity == ItemRarity.Rare
                        ? LocalizationService.T(ItemDescriptionKey(type, rarity), "Fill a cell with its correct digit and auto-complete its box.")
                        : LocalizationService.T(ItemDescriptionKey(type, rarity), "Fill a cell with its correct digit and highlight remaining candidates in its box."),

                // [REDESIGN] Finder tiers:
                // Normal: highlight 1 matching cell, Rare: highlight 3, Epic: reveal ALL cells of that digit
                ItemType.Finder => rarity == ItemRarity.Epic
                    ? LocalizationService.T(ItemDescriptionKey(type, rarity), "Reveal ALL unsolved cells containing the selected digit.")
                    : LocalizationService.Format(ItemDescriptionKey(type, rarity), "Highlight {0} cell(s) matching the selected digit.", GetFinderHighlightCount(rarity)),
                ItemType.InkWell => LocalizationService.Format(ItemDescriptionKey(type, rarity), "Restore {0} pencil marks.", GetInkWellAmount(rarity)),
                ItemType.MeditationStone => LocalizationService.Format(ItemDescriptionKey(type, rarity), "Restore {0} HP.", GetMeditationStoneAmount(rarity)),
                ItemType.WindChime => rarity == ItemRarity.Epic
                    ? LocalizationService.T(ItemDescriptionKey(type, rarity), "Click a cell to fill its row + column + box.")
                    : rarity == ItemRarity.Rare
                        ? LocalizationService.T(ItemDescriptionKey(type, rarity), "Choose any 2 of row/column/box, then click a cell to fill them.")
                        : LocalizationService.T(ItemDescriptionKey(type, rarity), "Choose row, column, OR box, then click a cell to fill it."),
                ItemType.PatternScroll => LocalizationService.Format(ItemDescriptionKey(type, rarity), "Select {0} mechanic instance(s) to solve using the solution. ESC cancels.", GetPatternScrollZones(rarity)),
                // [REDESIGN] Koi Reflection Epic: reveal candidates for every unsolved cell in the most ambiguous zone.
                ItemType.KoiReflection => rarity == ItemRarity.Epic
                    ? LocalizationService.T(ItemDescriptionKey(type, rarity), "Reveal candidates for every unsolved cell in the board's most ambiguous zone (row/column/box with most open cells).")
                    : LocalizationService.Format(ItemDescriptionKey(type, rarity), "Click {0} empty cell(s) to reveal their candidates.", GetKoiReflectionCells(rarity)),
                ItemType.LanternOfClarity => LocalizationService.Format(ItemDescriptionKey(type, rarity), "Disable fog for {0} moves.", GetLanternOfClarityMoves(rarity)),
                ItemType.GardenRake => LocalizationService.T(ItemDescriptionKey(type, rarity), "Clear all pencil marks in the selected row and column."),
                ItemType.OfferingBowl => LocalizationService.T(ItemDescriptionKey(type, rarity), "Sacrifice 1 HP to gain 30 gold."),
                // [REDESIGN] Pruning Shears: remove ALL invalid candidates from a cell (not just one).
                ItemType.PruningShears => LocalizationService.T(ItemDescriptionKey(type, rarity), "Remove all invalid candidates from a cell, leaving only valid possibilities."),
                ItemType.ZenSandSifter => LocalizationService.T(ItemDescriptionKey(type, rarity), "Highlight all cells with exactly two candidates."),
                ItemType.GinkgoLeaf => LocalizationService.T(ItemDescriptionKey(type, rarity), "Undo the last mistake, restore the lost HP, and recover your combo."),
                ItemType.RicePaperUmbrella => LocalizationService.T(ItemDescriptionKey(type, rarity), "Block the next mistake penalty (2 charges)."),
                ItemType.TempleIncense => LocalizationService.Format(ItemDescriptionKey(type, rarity), "Click {0} empty cell(s) to fill their correct answer.", rarity == ItemRarity.Epic ? 6 : rarity == ItemRarity.Rare ? 4 : 2),
                ItemType.KoiDragonScale => LocalizationService.T(ItemDescriptionKey(type, rarity), "Complete the most-filled row, column, or box."),
                // [REDESIGN] Golden Kintsugi Jar: fix all current mistakes AND reveal correct digits.
                ItemType.GoldenKintsugiJar => LocalizationService.T(ItemDescriptionKey(type, rarity), "Fix all current mistakes on the board and reveal the correct digits for all affected cells."),
                // [REDESIGN] Silk Fan: generate 5 Shield. Absorbs HP damage before health bar.
                // While shielded, mistakes don't break your combo. Shield resets each puzzle.
                ItemType.SilkFan => LocalizationService.Format(ItemDescriptionKey(type, rarity), "Generate {0} Shield. Shield absorbs HP damage before your health bar. While shielded, mistakes don't break your combo. Resets each puzzle.", GetSilkFanShieldAmount()),
                // Class-exclusive items (L15)
                ItemType.LoadedCoin     => LocalizationService.T(ItemDescriptionKey(type, rarity), "Gain 1 Reroll Token. (NumberFreak exclusive)"),
                ItemType.MonksBeads     => LocalizationService.T(ItemDescriptionKey(type, rarity), "Your next 9 placements cost no pencil marks. (GardenMonk exclusive)"),
                ItemType.AnnotatedFolio => LocalizationService.T(ItemDescriptionKey(type, rarity), "Reveal all candidates in a selected box. (ShrineArchivist exclusive)"),
                ItemType.DoubleOrQuits  => LocalizationService.T(ItemDescriptionKey(type, rarity), "Double gold from this puzzle or lose 1 HP. (KoiGambler exclusive)"),
                ItemType.WornChisel     => LocalizationService.T(ItemDescriptionKey(type, rarity), "30% discount at the next shop. (StoneGardener exclusive)"),
                ItemType.DimLantern     => LocalizationService.T(ItemDescriptionKey(type, rarity), "Remove fog from 3 random hidden cells permanently. (LanternSeer exclusive)"),
                ItemType.ReedPledge     => LocalizationService.T(ItemDescriptionKey(type, rarity), "Complete this puzzle using zero pencil marks for +2 HP. (ReedDuelist exclusive)"),
                ItemType.SurveyNotes    => LocalizationService.T(ItemDescriptionKey(type, rarity), "Reveal all node types on the current floor. (QuietCartographer exclusive)"),
                ItemType.BambooCompass  => LocalizationService.T(ItemDescriptionKey(type, rarity), "Highlight 2 cells where the next placement would likely violate an active constraint."),
                // General-pool items
                ItemType.BambooScroll   => LocalizationService.T(ItemDescriptionKey(type, rarity), "Reveal the star rating of all upcoming path nodes."),
                ItemType.GardenLantern  => LocalizationService.T(ItemDescriptionKey(type, rarity), "Lift fog from the entire board for 15 seconds, then re-fog."),
                ItemType.GoldenKoi      => LocalizationService.Format(ItemDescriptionKey(type, rarity), "Grant +{0}g per star rating when this puzzle ends.", 10 * 1),
                ItemType.TempleStamp    => LocalizationService.T(ItemDescriptionKey(type, rarity), "Protect your HP this puzzle - all wrong placements deal 0 damage. Consumed on completion."),
                _ => ""
            };
        }
    }
}
