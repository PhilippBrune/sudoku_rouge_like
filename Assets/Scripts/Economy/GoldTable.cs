using UnityEngine;

namespace SudokuRoguelike.Economy
{
    public static class GoldTable
    {
        // [REQ: ECON-REWARD-001] Base gold per board size used in puzzle completion formula
        public static int BaseGoldForBoardSize(int boardSize)
        {
            switch (boardSize)
            {
                case 5: return 20;
                case 6: return 30;
                case 7: return 45;
                case 8: return 65;
                case 9: return 100;
                default: return 20;
            }
        }

        // [REQ: ECON-REWARD-001] Star multiplier: 1.0 + stars × 0.2
        public static float StarGoldMultiplier(int stars)
        {
            return 1.0f + stars * 0.2f;
        }

        // [REQ: ECON-REWARD-001] Entry point: gold = BaseGold(size) × StarMult(stars)
        public static int CalculatePuzzleGold(int boardSize, int stars)
        {
            var baseGold = BaseGoldForBoardSize(boardSize);
            return Mathf.RoundToInt(baseGold * StarGoldMultiplier(stars));
        }

        public static int PencilBuyCost(int timesPurchasedThisRun)
        {
            return 20 + 20 * timesPurchasedThisRun;
        }

        public static int RerollCost(int timesRerolledThisRun)
        {
            return 20 + 20 * timesRerolledThisRun;
        }

        // [REQ: SHOP-PRICE-001] [REQ: ECON-SHOP-002] Price = BasePrice × (1 + FloorIndex × 0.5)
        public static int ShopItemPrice(int basePrice, int floorIndex)
        {
            return Mathf.RoundToInt(basePrice * (1f + floorIndex * 0.5f));
        }

        // [REQ: SHOP-PRICE-001] Base prices: Normal=15, Rare=30, Epic=60
        public static int BaseItemPrice(Core.ItemRarity rarity)
        {
            switch (rarity)
            {
                case Core.ItemRarity.Normal: return 15;
                case Core.ItemRarity.Rare: return 30;
                case Core.ItemRarity.Epic: return 60;
                default: return 15;
            }
        }
    }
}
