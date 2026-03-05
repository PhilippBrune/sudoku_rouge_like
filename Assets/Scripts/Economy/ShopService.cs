using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public sealed class ShopService
    {
        private readonly Random _random;

        public ShopService(int seed)
        {
            _random = new Random(seed);
        }

        public List<ShopOffer> BuildOffers(int runDepth, int purchaseCount)
        {
            var offers = new List<ShopOffer>();
            const int offerCount = 3;

            for (var i = 0; i < offerCount; i++)
            {
                var rarity = RollItemRarity(runDepth);
                var basePrice = rarity switch
                {
                    ItemRarity.Epic => 72,
                    ItemRarity.Rare => 52,
                    _ => 34
                };
                var price = PriceCurve(basePrice, purchaseCount + i);
                var itemType = RollShopItemType();

                offers.Add(new ShopOffer
                {
                    OfferId = Guid.NewGuid().ToString("N"),
                    IsRelic = false,
                    RelicId = null,
                    Item = new ItemInstance
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Type = itemType,
                        Rarity = rarity,
                        Charges = 1
                    },
                    Price = price
                });
            }

            return offers;
        }

        private ItemType RollShopItemType()
        {
            var roll = _random.Next(3);
            return roll switch
            {
                0 => ItemType.InkWell,
                1 => ItemType.MeditationStone,
                _ => ItemType.WindChime
            };
        }

        private ItemRarity RollItemRarity(int runDepth)
        {
            var roll = _random.NextDouble();
            if (runDepth >= 8 && roll < 0.15)
            {
                return ItemRarity.Epic;
            }

            if (roll < 0.42)
            {
                return ItemRarity.Rare;
            }

            return ItemRarity.Normal;
        }

        private string BuildRelicId(int runDepth, int slot)
        {
            if (_random.NextDouble() < 0.045)
            {
                return RollLegendaryRelicId();
            }

            var category = RollCategoryTag();
            var tierTag = runDepth >= 8 ? "t4" : runDepth >= 6 ? "t3" : runDepth >= 4 ? "t2" : "t1";
            return $"relic_{category}_{tierTag}_{runDepth}_{slot}";
        }

        private string RollCategoryTag()
        {
            var roll = _random.Next(6);
            return roll switch
            {
                0 => "eco",
                1 => "sur",
                2 => "mod",
                3 => "combo",
                4 => "chaos",
                _ => "util"
            };
        }

        private string RollLegendaryRelicId()
        {
            var roll = _random.Next(3);
            return roll switch
            {
                0 => "relic_legend_shifting_garden",
                1 => "relic_legend_silent_grid",
                _ => "relic_legend_golden_root"
            };
        }

        public int PriceCurve(int basePrice, int purchases)
        {
            var growth = 1f + 0.22f * MathF.Pow(Math.Max(0, purchases), 1.15f);
            return Math.Max(basePrice, (int)MathF.Round(basePrice * growth));
        }

        public int EmergencyHealPrice(int healsPurchased)
        {
            return PriceCurve(25, healsPurchased);
        }
    }
}
