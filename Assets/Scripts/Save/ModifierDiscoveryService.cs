using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

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
