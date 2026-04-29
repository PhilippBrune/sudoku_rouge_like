using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.Economy
{
    public sealed class ShopService
    {
        private readonly Random _random;

        public ShopService(int seed)
        {
            _random = new Random(seed);
        }

        // [REQ: ECON-SHOP-001] Shop: 3 items + optional relic per visit
        // [REQ: SHOP-PRICE-001] [REQ: ECON-SHOP-002] Price: BasePrice × (1 + FloorIndex × 0.5) × priceMultiplier × harmonySurcharge
        // [HARMONY-ECON-002] harmonySurcharge (1.00–1.50) applies the Harmony difficulty shop penalty on top of all other multipliers.
        public List<ShopOffer> BuildOffers(int floorIndex, int classLevel, float priceMultiplier, float harmonySurcharge)
        {
            var offers = new List<ShopOffer>();
            var slotCount = GetShopSlotCount(floorIndex);

            for (var i = 0; i < slotCount; i++)
            {
                var rarity = RollShopItemRarity(floorIndex, classLevel);
                var type = RollShopItemType(rarity);
                var basePrice = ItemService.GetBasePrice(rarity);
                var scaledPrice = (int)Math.Round(basePrice * (1f + floorIndex * 0.5f) * priceMultiplier * harmonySurcharge);

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

        private ItemType RollShopItemType(ItemRarity rarity)
        {
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
