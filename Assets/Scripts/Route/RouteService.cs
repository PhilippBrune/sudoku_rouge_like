using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Route
{
    public sealed class RouteChoice
    {
        public RouteType Left;
        public RouteType Right;
    }

    public sealed class RouteService
    {
        private readonly Random _random;

        public RouteService(int seed)
        {
            _random = new Random(seed);
        }

        public RouteChoice RollChoice()
        {
            var all = (RouteType[])Enum.GetValues(typeof(RouteType));
            var left = all[_random.Next(all.Length)];
            var right = all[_random.Next(all.Length)];

            while (right == left)
            {
                right = all[_random.Next(all.Length)];
            }

            return new RouteChoice { Left = left, Right = right };
        }

        public void ApplyRouteProfile(RouteType route, LevelConfig config, ref int mistakePenalty, ref float bonusGoldMultiplier, ref int bonusPencilReward, ref int bonusXp)
        {
            mistakePenalty = 1;
            bonusGoldMultiplier = 1f;
            bonusPencilReward = 0;
            bonusXp = 0;

            switch (route)
            {
                case RouteType.Bamboo:
                    bonusPencilReward += 2;
                    bonusGoldMultiplier -= 0.15f;
                    break;
                case RouteType.Lantern:
                    config.Stars = Math.Min(5, config.Stars + 1);
                    break;
                case RouteType.KoiPond:
                    config.Stars = Math.Min(5, Math.Max(config.Stars, 3));
                    bonusGoldMultiplier += 0.25f;
                    mistakePenalty = 2;
                    break;
                case RouteType.StoneGarden:
                    bonusXp += 100;
                    bonusGoldMultiplier -= 0.2f;
                    break;
                case RouteType.Blossom:
                    bonusXp -= 50;
                    break;
            }
        }
    }
}
