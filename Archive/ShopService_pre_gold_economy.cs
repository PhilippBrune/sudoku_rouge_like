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

        /// <summary>Build shop offers for the current floor. Includes 3 items + optionally 1 relic.</summary>
        public List<ShopOffer> BuildOffers(int floorIndex, int classLevel, float priceMultiplier)
        {
            var offers = new List<ShopOffer>();

            // 3 consumable item offers
            for (var i = 0; i < 3; i++)
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

            // Chance for a relic offer (increases with floor)
            var relicChance = 0.20 + floorIndex * 0.10;
            if (_random.NextDouble() < relicChance)
            {
                var relicService = new RelicService(_random.Next());
                var relic = relicService.RollRelic(floorIndex + 1, false);
                var relicBasePrice = RelicService.GetBasePrice(relic.Tier);
                var relicPrice = (int)Math.Round(relicBasePrice * (1f + floorIndex * 0.5f) * priceMultiplier);

                offers.Add(new ShopOffer
                {
                    OfferId = Guid.NewGuid().ToString("N"),
                    IsRelic = true,
                    RelicOffer = relic,
                    Price = relicPrice
                });
            }

            return offers;
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
