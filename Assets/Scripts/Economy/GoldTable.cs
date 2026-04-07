using UnityEngine;

namespace SudokuRoguelike.Economy
{
    public static class GoldTable
    {
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

        public static float StarGoldMultiplier(int stars)
        {
            return 1.0f + stars * 0.2f;
        }

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

        public static int ShopItemPrice(int basePrice, int floorIndex)
        {
            return Mathf.RoundToInt(basePrice * (1f + floorIndex * 0.5f));
        }

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
