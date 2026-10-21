using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    /// <summary>
    /// Shared helper for boss-modifier discovery state across run persistence and meta progression.
    /// Keeps the reveal pipeline consistent between autosave, run-end save, and next-run seeding.
    /// </summary>
    public static class ModifierDiscoveryService
    {
        public static void SanitizeMetaProgress(MetaProgressionState meta)
        {
            if (meta == null) return;

            // [REQ: META-CODEX-008] Saves from before the Curses page have no list. Empty and doubled ids are dropped; ids this
            // build does not know stay (a curse of a newer build must survive a start of an older one). The Codex shows
            // only the curses of the catalog.
            var curses = meta.DiscoveredCurses ?? new List<string>();
            var kept = new List<string>(curses.Count);
            var seenCurses = new HashSet<string>();
            foreach (var id in curses)
                if (!string.IsNullOrEmpty(id) && seenCurses.Add(id)) kept.Add(id);
            meta.DiscoveredCurses = kept;

            if (meta.DiscoveredBossModifiers == null)
            {
                meta.DiscoveredBossModifiers = new List<BossModifierId>();
                return;
            }

            var sanitized = new List<BossModifierId>(meta.DiscoveredBossModifiers.Count);
            var seen = new HashSet<BossModifierId>();
            for (var i = 0; i < meta.DiscoveredBossModifiers.Count; i++)
            {
                var modifier = meta.DiscoveredBossModifiers[i];
                if (!IsValidModifier(modifier) || !seen.Add(modifier))
                    continue;

                sanitized.Add(modifier);
            }

            if (sanitized.Count == meta.DiscoveredBossModifiers.Count)
                return;

            meta.DiscoveredBossModifiers.Clear();
            meta.DiscoveredBossModifiers.AddRange(sanitized);
        }

        /// <summary>[REQ: META-CODEX-008] Adds the curses met in a run to the Codex: each known curse once, in the order met.
        /// Returns how many were new.</summary>
        public static int MergeCursesIntoMeta(MetaProgressionState meta, IEnumerable<string> curseIds)
        {
            if (meta == null || curseIds == null) return 0;
            meta.DiscoveredCurses ??= new List<string>();

            var added = 0;
            foreach (var id in curseIds)
            {
                if (string.IsNullOrEmpty(id) || CurseService.GetDefinition(id) == null || meta.DiscoveredCurses.Contains(id))
                    continue;
                meta.DiscoveredCurses.Add(id);
                added++;
            }

            return added;
        }

        public static int MergeIntoMeta(MetaProgressionState meta, IEnumerable<BossModifierId> modifiers)
        {
            if (meta == null || modifiers == null) return 0;

            SanitizeMetaProgress(meta);

            var added = 0;
            var seen = new HashSet<BossModifierId>(meta.DiscoveredBossModifiers);
            foreach (var modifier in modifiers)
            {
                if (!IsValidModifier(modifier) || !seen.Add(modifier))
                    continue;

                meta.DiscoveredBossModifiers.Add(modifier);
                added++;
            }

            return added;
        }

        public static int SeedRunSeenModifiers(RunState runState, IEnumerable<BossModifierId> modifiers)
        {
            if (runState == null || modifiers == null) return 0;

            if (runState.SeenBossModifiers == null)
                runState.SeenBossModifiers = new HashSet<BossModifierId>();
            if (runState.SeenBossModifierList == null)
                runState.SeenBossModifierList = new List<BossModifierId>();

            var added = 0;
            foreach (var modifier in modifiers)
            {
                if (!IsValidModifier(modifier) || !runState.SeenBossModifiers.Add(modifier))
                    continue;

                added++;
            }

            runState.SyncSeenModifiersToList();
            return added;
        }

        public static string Describe(IEnumerable<BossModifierId> modifiers)
        {
            if (modifiers == null) return "[]";

            var names = new List<string>();
            foreach (var modifier in modifiers)
            {
                if (!IsValidModifier(modifier))
                    continue;

                names.Add(modifier.ToString());
            }

            return names.Count == 0
                ? "[]"
                : $"[{string.Join(", ", names)}]";
        }

        private static bool IsValidModifier(BossModifierId modifier)
        {
            return Enum.IsDefined(typeof(BossModifierId), modifier);
        }
    }
}
