using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Items
{
    /// <summary>
    /// [REQ: ITEM-COUNTER-001] One counter item per modifier family (GoldEconomySystem lever L3, owner pick after the 0.2.0
    /// playtest): each writes correct digits into the cells its family's rules use, so the item answers the rule that is
    /// hard on this board. Shops lean toward the counters of the floor's and the boss's rules (ITEM-COUNTER-007).
    /// </summary>
    public static class CounterItems
    {
        private static readonly Dictionary<ModifierFamily, ItemType> ByFamily = new Dictionary<ModifierFamily, ItemType>
        {
            [ModifierFamily.Lines] = ItemType.WaterBasin,
            [ModifierFamily.DotsAndPairs] = ItemType.TwinPebbles,
            [ModifierFamily.CagesAndArrows] = ItemType.RiceBowl,
            [ModifierFamily.Cells] = ItemType.MossStone,
            [ModifierFamily.WholeBoard] = ItemType.CompassOfOrder,
            [ModifierFamily.OutsideClues] = ItemType.SunMedallion
        };

        /// <summary>How many cells one use writes.</summary>
        public static int CellsPerUse(ItemType type) => type switch
        {
            ItemType.WaterBasin or ItemType.RiceBowl => 3,
            _ => 2
        };

        public static bool IsCounterItem(ItemType type) => ByFamily.ContainsValue(type);

        public static ItemType For(ModifierFamily family) => ByFamily[family];

        /// <summary>The counter items for the families of <paramref name="rules"/>, in family order, each once.</summary>
        public static List<ItemType> ForRules(IEnumerable<BossModifierId> rules)
        {
            var families = new HashSet<ModifierFamily>();
            if (rules != null)
                foreach (var id in rules)
                    if (ModifierFamilies.TryGetFamily(id, out var family)) families.Add(family);
            var items = new List<ItemType>();
            foreach (var family in ModifierFamilies.Order)
                if (families.Contains(family)) items.Add(ByFamily[family]);
            return items;
        }
    }
}
