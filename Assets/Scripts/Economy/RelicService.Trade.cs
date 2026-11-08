using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.Economy
{
    // [REQ: RELIC-PACK-001] [REQ: RELIC-TRADE-001] The RM-104 relics' hooks outside the per-puzzle loops (owner's picks
    // 2026-11-04): what they take when picked up, a mistake's extra HP, a defeated boss, an item received, the route's gold.
    public sealed partial class RelicService
    {
        public const int CrackedBellMaxHp = 2;
        public const int DryInkstonePencil = 5;

        // Cracked Bell: −2 Max HP; Dry Inkstone: −5 max pencil. Never below 1, and the current value follows.
        private static void ApplyTradePickup(RunState state, RelicId id)
        {
            switch (id)
            {
                case RelicId.CrackedBell:
                    state.MaxHP = Math.Max(1, state.MaxHP - CrackedBellMaxHp);
                    state.CurrentHP = Math.Min(state.CurrentHP, state.MaxHP);
                    break;
                case RelicId.DryInkstone:
                    state.MaxPencil = Math.Max(1, state.MaxPencil - DryInkstonePencil);
                    state.CurrentPencil = Math.Min(state.CurrentPencil, state.MaxPencil);
                    break;
            }
        }

        /// <summary>A mistake's HP: +1 for every Oni Pact.</summary>
        public static int MistakeDamage(RunState state, int damage) => damage + CountRelicsOfType(state, RelicId.OniPact);

        /// <summary>Counts the boss; every Cracked Bell gives +2 Max HP and heals the same.</summary>
        public static void OnBossDefeated(RunState state)
        {
            state.BossesDefeatedThisRun++;
            var bells = CountRelicsOfType(state, RelicId.CrackedBell);
            if (bells == 0) return;
            state.MaxHP += CrackedBellMaxHp * bells;
            state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + CrackedBellMaxHp * bells);
        }

        /// <summary>The item as it goes into the bag: one more charge for every Twin Koi.</summary>
        public static ItemInstance OnItemReceived(RunState state, ItemInstance item)
        {
            if (item != null)
                item.Charges = Math.Max(Math.Max(1, item.Charges), Classes.ClassPassiveService.ItemUses(state)) + CountRelicsOfType(state, RelicId.TwinKoi);
            return item;
        }

        // Lantern of Daring: the tile just solved is the last route taken; +50 % on the Risk route, −20 % on the Calm one.
        private static int LanternOfDaringGold(RunState state, int gold)
        {
            var routes = state.RouteHistory;
            if (routes == null || routes.Count == 0) return gold;
            var factor = routes[routes.Count - 1] == (int)RouteType.RiskRoute ? 1.5f : 0.8f;
            return (int)Math.Round(gold * factor, MidpointRounding.AwayFromZero);
        }
    }
}
