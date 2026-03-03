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
            var offerCount = runDepth >= 6 ? 4 : 3;

            for (var i = 0; i < offerCount; i++)
            {
                var isRelic = runDepth >= 4 && _random.NextDouble() < 0.35;
                var basePrice = isRelic ? 80 : 35;
                var price = PriceCurve(basePrice, purchaseCount + i);
                var relicId = isRelic ? BuildRelicId(runDepth, i) : null;

                offers.Add(new ShopOffer
                {
                    OfferId = Guid.NewGuid().ToString("N"),
                    IsRelic = isRelic,
                    RelicId = relicId,
                    Item = isRelic ? null : new ItemInstance
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Type = _random.NextDouble() < 0.5 ? ItemType.InkWell : ItemType.MeditationStone,
                        Rarity = runDepth >= 7 && _random.NextDouble() < 0.3 ? ItemRarity.Rare : ItemRarity.Normal,
                        Charges = 1
                    },
                    Price = price
                });
            }

            return offers;
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
