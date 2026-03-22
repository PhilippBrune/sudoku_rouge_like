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

        /// <summary>Build shop offers for the current floor. Items only — relics are NOT sold in shops.</summary>
        public List<ShopOffer> BuildOffers(int floorIndex, int classLevel, float priceMultiplier)
        {
            var offers = new List<ShopOffer>();

            // Floor-based shop inventory size: F1-2=2, F3-4=3, F5=4
            var slotCount = GetShopSlotCount(floorIndex);

            for (var i = 0; i < slotCount; i++)
            {
                var rarity = RollShopItemRarity(floorIndex, classLevel);
                var type = RollShopItemType(rarity, classLevel);
                var basePrice = ItemService.GetBasePrice(rarity);
                var scaledPrice = (int)Math.Round(basePrice * (1f + floorIndex * 0.5f) * priceMultiplier);

                offers.Add(new ShopOffer
                {
                    OfferId = Guid.NewGuid().ToString("N"),
                    IsRelic = false,
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

        /// <summary>Shop inventory size scales with floor: F1-2=2, F3-4=3, F5=4.</summary>
        public static int GetShopSlotCount(int floorIndex)
        {
            return floorIndex switch
            {
                <= 1 => 2,
                <= 3 => 3,
                _ => 4
            };
        }

        public int EmergencyHealPrice(int healsPurchased)
        {
            return (int)Math.Round(25 * (1f + healsPurchased * 0.5f));
        }

        private ItemRarity RollShopItemRarity(int floorIndex, int classLevel)
        {
            var roll = _random.NextDouble();

            // Epic items only available at class Level 15+
            if (classLevel >= 15)
            {
                var epicChance = 0.02 + (classLevel - 15) * 0.002;
                if (floorIndex >= 3) epicChance += 0.03; // late floors boost
                if (roll < epicChance) return ItemRarity.Epic;
            }

            var rareChance = 0.30 + floorIndex * 0.05;
            return roll < rareChance ? ItemRarity.Rare : ItemRarity.Normal;
        }

        private ItemType RollShopItemType(ItemRarity rarity, int classLevel)
        {
            // Weighted pool: prefer resource items in shop
            var resourceTypes = new[]
            {
                ItemType.InkWell, ItemType.MeditationStone, ItemType.WindChime,
                ItemType.Solver, ItemType.Finder, ItemType.PatternScroll
            };

            // Chance for unique items based on rarity
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
