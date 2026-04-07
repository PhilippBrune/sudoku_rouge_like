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

        // ── Slot Rolling ──

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

        public List<ItemInstance> RollSlots(int stars, int classLevel, int bonusSlots = 0)
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
                var type = RollItemType(rarity, classLevel);
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
                var type = RollItemType(rarity, classLevel);
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

            if (classLevel >= 15)
            {
                var epicChance = 0.02 + (classLevel - 15) * 0.002;
                if (roll < epicChance) return ItemRarity.Epic;
            }

            var rareChance = 0.15 + stars * 0.05;
            return roll < rareChance ? ItemRarity.Rare : ItemRarity.Normal;
        }

        private ItemType RollItemType(ItemRarity rarity, int classLevel)
        {
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
                _ => ItemRarity.Normal
            };
        }

        // ── Item Effects (rarity-scaled) ──

        public static int GetSolverNeighborCount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 2 : rarity == ItemRarity.Rare ? 1 : 0;

        public static int GetFinderHighlightCount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 2 : rarity == ItemRarity.Rare ? 3 : 1;

        public static int GetInkWellAmount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 10 : rarity == ItemRarity.Rare ? 6 : 3;

        public static int GetMeditationStoneAmount(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 3 : rarity == ItemRarity.Rare ? 2 : 1;

        public static int GetLanternOfClarityMoves(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 10 : rarity == ItemRarity.Rare ? 6 : 3;

        public static int GetKoiReflectionCells(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? 3 : rarity == ItemRarity.Rare ? 2 : 1;

        public static int GetPatternScrollZones(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? -1 : rarity == ItemRarity.Rare ? 2 : 1;

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
                ItemType.WindChime => "Reroll item reward slots.",
                ItemType.PatternScroll => GetPatternScrollZones(rarity) < 0 ? "Highlight all conflict zones." : $"Highlight {GetPatternScrollZones(rarity)} conflict zone(s).",
                ItemType.KoiReflection => $"Reveal candidates for {GetKoiReflectionCells(rarity)} cell(s).",
                ItemType.LanternOfClarity => $"Disable fog for {GetLanternOfClarityMoves(rarity)} moves.",
                ItemType.GardenRake => "Clear all pencil marks in the selected row and column.",
                ItemType.OfferingBowl => "Sacrifice 1 HP to gain 30 gold.",
                ItemType.PruningShears => "Remove one incorrect candidate from a cell.",
                ItemType.ZenSandSifter => "Highlight all cells with exactly two candidates.",
                ItemType.GinkgoLeaf => "Undo the last mistake placement.",
                ItemType.RicePaperUmbrella => "Block the next mistake penalty (2 charges).",
                ItemType.TempleIncense => "Add the single correct candidate to an empty cell.",
                ItemType.KoiDragonScale => "Complete the most-filled row, column, or box.",
                ItemType.GoldenKintsugiJar => "Highlight all current mistakes on the board.",
                ItemType.SilkFan => "Swap two non-given cells.",
                _ => ""
            };
        }
    }
}
