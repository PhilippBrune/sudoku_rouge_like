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

        // [REQ: ECON-BAL-002] Puzzle gold is four fifths of the table (GoldEconomySystem lever L1, 0.2.0 playtest): gold had
        // outrun every sink.
        public const float PuzzleGoldFactor = 0.8f;

        // [REQ: ECON-REWARD-001] Entry point: gold = BaseGold(size) × StarMult(stars) × PuzzleGoldFactor
        public static int CalculatePuzzleGold(int boardSize, int stars)
        {
            var baseGold = BaseGoldForBoardSize(boardSize);
            return Mathf.RoundToInt(baseGold * StarGoldMultiplier(stars) * PuzzleGoldFactor);
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
        public static float FloorPriceFactor(int floorIndex) => 1f + floorIndex * 0.5f;

        public static int ShopItemPrice(int basePrice, int floorIndex)
        {
            return Mathf.RoundToInt(basePrice * FloorPriceFactor(floorIndex));
        }

        // [REQ: SHOP-PRICE-001] [REQ: ECON-BAL-001] Base prices: Normal 40, Rare 75, Epic 150 (×2.5 since the 0.2.0
        // playtest, GoldEconomySystem lever L1; they were 15/30/60). The one price table: ItemService and ShopService read it.
        public static int BaseItemPrice(Core.ItemRarity rarity)
        {
            switch (rarity)
            {
                case Core.ItemRarity.Normal: return 40;
                case Core.ItemRarity.Rare: return 75;
                case Core.ItemRarity.Epic: return 150;
                default: return 40;
            }
        }
    }
}
