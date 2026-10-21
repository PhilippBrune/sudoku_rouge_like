using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [REQ: META-CODEX-001] How much of each Codex collection a profile has found. The Codex's bookmarks (UI-STYLE-018),
    /// the class garden's totals (UI-STYLE-019) and the Reed Duelist's unlock (META-CODEX-006) all count from here.
    /// Unknown ids and doubled entries in a save never count.
    /// </summary>
    public static class CodexProgress
    {
        public readonly struct Count
        {
            public readonly int Found;
            public readonly int Total;

            public Count(int found, int total)
            {
                Found = found;
                Total = total;
            }

            public bool Complete => Total > 0 && Found >= Total;
        }

        public static Count Items(MetaProgressionState meta) => CountItems(meta, includeCounterItems: true);

        /// <summary>The items the Reed Duelist unlock asks for: every item except the counter items, which only shops sell
        /// and only for the floor's rules (ITEM-COUNTER-007).</summary>
        public static Count RequiredItems(MetaProgressionState meta) => CountItems(meta, includeCounterItems: false);

        public static Count Relics(MetaProgressionState meta)
        {
            var found = new HashSet<RelicId>();
            if (meta?.DiscoveredRelics != null)
                foreach (var id in meta.DiscoveredRelics)
                    if (Enum.IsDefined(typeof(RelicId), id)) found.Add(id);
            return new Count(found.Count, Enum.GetValues(typeof(RelicId)).Length);
        }

        /// <summary>The boss modifiers the game can deal (the Codex's Rules).</summary>
        public static Count Rules(MetaProgressionState meta)
        {
            var total = 0;
            var found = 0;
            foreach (var id in ModifierFamilies.All())
            {
                if (!BossService.IsImplementedModifier(id)) continue;
                total++;
                if (meta?.DiscoveredBossModifiers != null && meta.DiscoveredBossModifiers.Contains(id)) found++;
            }
            return new Count(found, total);
        }

        public static Count Curses(MetaProgressionState meta)
        {
            var found = 0;
            foreach (var curse in CurseService.All)
                if (meta?.DiscoveredCurses != null && meta.DiscoveredCurses.Contains(curse.Id)) found++;
            return new Count(found, CurseService.All.Count);
        }

        /// <summary>The share of all four collections found, in whole percent (rounded down).</summary>
        public static int Percent(MetaProgressionState meta)
        {
            if (meta == null) return 0;
            var items = Items(meta);
            var relics = Relics(meta);
            var rules = Rules(meta);
            var curses = Curses(meta);
            var total = items.Total + relics.Total + rules.Total + curses.Total;
            return total == 0 ? 0 : (items.Found + relics.Found + rules.Found + curses.Found) * 100 / total;
        }

        private static Count CountItems(MetaProgressionState meta, bool includeCounterItems)
        {
            var found = new HashSet<ItemType>();
            if (meta?.ItemCodex?.Entries != null)
                foreach (var entry in meta.ItemCodex.Entries)
                    if (entry != null && entry.Discovered && Enum.TryParse(entry.ItemId, out ItemType type)
                        && Enum.IsDefined(typeof(ItemType), type) && (includeCounterItems || !CounterItems.IsCounterItem(type)))
                        found.Add(type);

            var total = 0;
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
                if (includeCounterItems || !CounterItems.IsCounterItem(type)) total++;
            return new Count(found.Count, total);
        }
    }
}
