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

        // ── Class-Exclusive Definitions ──

        private sealed class ExclusiveDef
        {
            public ClassId ClassId;
            public int UnlockLevel;
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
                    Charges = 1
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
                    Charges = 1
                };
            }

            return slots;
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
                var rares = new[] { ItemType.GinkgoLeaf, ItemType.RicePaperUmbrella, ItemType.TempleIncense };
                return rares[_random.Next(rares.Length)];
            }

            var tiered = new[]
            {
                ItemType.Solver, ItemType.Finder, ItemType.InkWell, ItemType.MeditationStone,
                ItemType.WindChime, ItemType.PatternScroll, ItemType.KoiReflection, ItemType.LanternOfClarity
            };
            return tiered[_random.Next(tiered.Length)];
        }

        // ── Item Classification ──

        public static bool IsTiered(ItemType type)
        {
            return type switch
            {
                ItemType.Solver or ItemType.Finder or ItemType.InkWell or ItemType.MeditationStone
                    or ItemType.WindChime or ItemType.PatternScroll or ItemType.KoiReflection
                    or ItemType.LanternOfClarity => true,
                _ => false
            };
        }

        public static ItemRarity GetFixedRarity(ItemType type)
        {
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
                _ => ItemRarity.Normal
            };
        }

        // ── Item Effects (rarity-scaled) ──
        // Rebalanced: Normal = old Epic baseline, Rare = old Epic +1, Epic = old Epic +2

        public static int GetSolverNeighborCount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 4 : rarity == ItemRarity.Rare ? 3 : 2;

        public static int GetFinderHighlightCount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 4 : rarity == ItemRarity.Rare ? 3 : 2;

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

        public static string GetItemName(ItemType type)
        {
            return type switch
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
                _ => type.ToString()
            };
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
                _ => ""
            };
        }

        public static string GetItemDescription(ItemType type, ItemRarity rarity)
        {
            return type switch
            {
                ItemType.Solver => $"Fill a cell with its correct digit + {GetSolverNeighborCount(rarity)} neighbors.",
                ItemType.Finder => $"Highlight {GetFinderHighlightCount(rarity)} cell(s) matching the selected digit.",
                ItemType.InkWell => $"Restore {GetInkWellAmount(rarity)} pencil marks.",
                ItemType.MeditationStone => $"Restore {GetMeditationStoneAmount(rarity)} HP.",
                ItemType.WindChime => rarity == ItemRarity.Epic
                    ? "Click a cell to fill its row + column + box."
                    : rarity == ItemRarity.Rare
                        ? "Choose any 2 of row/column/box, then click a cell to fill them."
                        : "Choose row, column, OR box, then click a cell to fill it.",
                ItemType.PatternScroll => $"Select {GetPatternScrollZones(rarity)} mechanic instance(s) to solve using the solution. ESC cancels.",
                ItemType.KoiReflection => $"Click {GetKoiReflectionCells(rarity)} empty cell(s) to reveal their candidates.",
                ItemType.LanternOfClarity => $"Disable fog for {GetLanternOfClarityMoves(rarity)} moves.",
                ItemType.GardenRake => "Clear all pencil marks in the selected row and column.",
                ItemType.OfferingBowl => "Sacrifice 1 HP to gain 30 gold.",
                ItemType.PruningShears => "Remove one incorrect candidate from a cell.",
                ItemType.ZenSandSifter => "Highlight all cells with exactly two candidates.",
                ItemType.GinkgoLeaf => "Undo the last mistake and restore the lost HP.",
                ItemType.RicePaperUmbrella => "Block the next mistake penalty (2 charges).",
                ItemType.TempleIncense => $"Click {(rarity == ItemRarity.Epic ? 6 : rarity == ItemRarity.Rare ? 4 : 2)} empty cell(s) to fill their correct answer.",
                ItemType.KoiDragonScale => "Complete the most-filled row, column, or box.",
                ItemType.GoldenKintsugiJar => "Highlight all current mistakes on the board.",
                ItemType.SilkFan => "Swap two non-given cells.",
                // Class-exclusive items (L15)
                ItemType.LoadedCoin     => "Gain 1 Reroll Token. (NumberFreak exclusive)",
                ItemType.MonksBeads     => "Your next 9 placements cost no pencil marks. (GardenMonk exclusive)",
                ItemType.AnnotatedFolio => "Reveal all candidates in a selected box. (ShrineArchivist exclusive)",
                ItemType.DoubleOrQuits  => "Double gold from this puzzle or lose 1 HP. (KoiGambler exclusive)",
                ItemType.WornChisel     => "30% discount at the next shop. (StoneGardener exclusive)",
                ItemType.DimLantern     => "Remove fog from 3 random hidden cells permanently. (LanternSeer exclusive)",
                ItemType.ReedPledge     => "Complete this puzzle using zero pencil marks for +2 HP. (ReedDuelist exclusive)",
                ItemType.SurveyNotes    => "Reveal all node types on the current floor. (QuietCartographer exclusive)",
                _ => ""
            };
        }
    }
}
