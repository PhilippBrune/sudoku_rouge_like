using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.Economy
{
    public sealed class ShopService
    {
        private Random _random;

        /// <summary>[REQ: REQ-RUN-103] Starts a fresh random stream for one decision. RunDirector seeds it from the
        /// run seed and the player's position, so a roll is the same before and after Save &amp; Quit (GAP-018).</summary>
        public void ReseedForDecision(int decisionSeed) => _random = new Random(decisionSeed);

        public ShopService(int seed)
        {
            _random = new Random(seed);
        }

        // [REQ: ECON-SHOP-001] Shop: 2/3/4 items by floor (GetShopSlotCount), no relic (SPEC-CONF-005)
        // [REQ: SHOP-PRICE-001] [REQ: ECON-SHOP-002] Price: BasePrice × (1 + FloorIndex × 0.5) × priceMultiplier × harmonySurcharge
        // [HARMONY-ECON-002] harmonySurcharge (1.00–1.50) applies the Harmony difficulty shop penalty on top of all other multipliers.
        // [REQ: CLASS-SHOP-001] Class-exclusive items can appear in shops at 25% injection chance when unlocked.
        // [REQ: ITEM-COUNTER-007] With counter items for the board's rules, each slot offers one of them (each once) at
        // CounterItemChance; their rarity is fixed.
        public const double CounterItemChance = 0.5;

        public List<ShopOffer> BuildOffers(int floorIndex, int classLevel, float priceMultiplier, float harmonySurcharge,
            ClassId classId = (ClassId)0, IReadOnlyList<ItemType> counterItems = null)
        {
            var offers = new List<ShopOffer>();
            var slotCount = GetShopSlotCount(floorIndex);
            var counters = counterItems != null ? new List<ItemType>(counterItems) : new List<ItemType>();

            for (var i = 0; i < slotCount; i++)
            {
                var rarity = RollShopItemRarity(floorIndex, classLevel);
                ItemType type;
                if (counters.Count > 0 && _random.NextDouble() < CounterItemChance)
                {
                    type = counters[_random.Next(counters.Count)];
                    counters.Remove(type);
                    rarity = ItemService.GetFixedRarity(type);
                }
                else
                    type = RollShopItemType(rarity, classId, classLevel);
                var basePrice = GoldTable.BaseItemPrice(rarity);
                var scaledPrice = (int)Math.Round(basePrice * GoldTable.FloorPriceFactor(floorIndex) * priceMultiplier * harmonySurcharge);

                offers.Add(new ShopOffer
                {
                    Item = new ItemInstance
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Type = type,
                        Rarity = ItemService.IsTiered(type) ? rarity : ItemService.GetFixedRarity(type),
                        Charges = 1
                    },
                    Price = scaledPrice
                });
            }

            return offers;
        }

        public static int GetShopSlotCount(int floorIndex)
        {
            return floorIndex switch
            {
                <= 1 => 2,
                <= 3 => 3,
                _ => 4
            };
        }

        public static int EmergencyHealPrice(int healsPurchased)
        {
            return (int)Math.Round(25 * (1f + healsPurchased * 0.5f));
        }

        private ItemRarity RollShopItemRarity(int floorIndex, int classLevel)
        {
            var roll = _random.NextDouble();

            if (classLevel >= 15)
            {
                var epicChance = 0.02 + (classLevel - 15) * 0.002;
                if (floorIndex >= 3) epicChance += 0.03;
                if (roll < epicChance) return ItemRarity.Epic;
            }

            var rareChance = 0.30 + floorIndex * 0.05;
            return roll < rareChance ? ItemRarity.Rare : ItemRarity.Normal;
        }

        private ItemType RollShopItemType(ItemRarity rarity, ClassId classId = (ClassId)0, int classLevel = 0)
        {
            // Class-exclusive item: 25% chance to inject the exclusive if unlocked
            if (classId != (ClassId)0 && _random.NextDouble() < 0.25)
            {
                var exclusive = ItemService.GetExclusiveItemForClass(classId);
                if (exclusive.HasValue && ItemService.IsAvailableForClass(exclusive.Value, classId, classLevel))
                    return exclusive.Value;
            }

            var resourceTypes = new[]
            {
                ItemType.InkWell, ItemType.MeditationStone, ItemType.WindChime,
                ItemType.Solver, ItemType.Finder, ItemType.PatternScroll
            };

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

            return resourceTypes[_random.Next(resourceTypes.Length)];
        }
    }
}
