using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Meta
{
    public sealed class ClassGardenProgressionService
    {
        // [REQ: META-XP-001] Adds to TotalXp — the single persistent XP value per class
        // [REQ: XP-CURVE-002] XP above the Level 40 threshold is discarded (ClassXpProgressionSystem §Excess XP,
        // SPEC-CONF-034). A class that already stored more keeps its value; prestige resets TotalXp to 0.
        public void AddXp(MetaProgressionState meta, ClassId classId, int xpAmount)
        {
            var entry = FindOrCreateEntry(meta, classId);
            var cap = XpTable.CumulativeXpForLevel(XpTable.MaxLevel);
            if (entry.TotalXp >= cap || xpAmount <= 0) return;
            entry.TotalXp = Math.Min(cap, entry.TotalXp + xpAmount);
        }

        // [REQ: META-XP-001] [REQ: XP-CURVE-002] Level derived at runtime from TotalXp via DeriveLevel
        public int GetLevel(MetaProgressionState meta, ClassId classId)
        {
            var entry = FindEntry(meta, classId);
            return entry == null ? 1 : XpTable.DeriveLevel(entry.TotalXp);
        }

        public int GetTotalXp(MetaProgressionState meta, ClassId classId)
        {
            var entry = FindEntry(meta, classId);
            return entry?.TotalXp ?? 0;
        }

        // [REQ: META-PRESTIGE-001] PrestigeTier 0–9 per class
        public int GetPrestigeTier(MetaProgressionState meta, ClassId classId)
        {
            var entry = FindEntry(meta, classId);
            return entry?.PrestigeTier ?? 0;
        }

        public const int MaxPrestigeTier = 9;

        // [REQ: META-PRESTIGE-005] Prestige reward (owner decision 2026-10-20, SPEC-CONF-039): the badge plus one starting
        // gold per tier for runs with that class, so at most +9.
        public const int StartingGoldPerPrestigeTier = 1;

        public static int PrestigeStartingGold(int prestigeTier) =>
            Math.Clamp(prestigeTier, 0, MaxPrestigeTier) * StartingGoldPerPrestigeTier;

        // [REQ: META-PRESTIGE-003] Boss gate: cumulative boss defeats >= (PrestigeTier + 1) × 3
        public static int RequiredBossesForPrestige(int prestigeTier) => (prestigeTier + 1) * 3;

        // [REQ: META-PRESTIGE-001] [REQ: META-PRESTIGE-002] [REQ: META-PRESTIGE-003]
        // Level 40, a tier below 9 and the boss gate (GAP-017).
        public bool CanPrestige(MetaProgressionState meta, ClassId classId)
        {
            var entry = FindEntry(meta, classId);
            if (entry == null) return false;
            return XpTable.DeriveLevel(entry.TotalXp) >= XpTable.MaxLevel
                && entry.PrestigeTier < MaxPrestigeTier
                && (meta.ClassUnlocks?.BossesDefeated ?? 0) >= RequiredBossesForPrestige(entry.PrestigeTier);
        }

        /// <summary>[REQ: META-STATS-001] Counts a Garden Run with the class and keeps its best floor.</summary>
        public void RecordGardenRun(MetaProgressionState meta, ClassId classId, int floorReached)
        {
            var entry = FindOrCreateEntry(meta, classId);
            entry.GardenRuns = Math.Max(0, entry.GardenRuns) + 1;
            entry.BestFloor = Math.Max(entry.BestFloor, Math.Max(0, floorReached));
        }

        /// <summary>The class's saved progress, or null before its first run.</summary>
        public ClassGardenProgressEntry GetEntry(MetaProgressionState meta, ClassId classId) => FindEntry(meta, classId);

        // [REQ: META-PRESTIGE-004] On prestige: TotalXp=0, PrestigeTier++
        public bool TryPrestige(MetaProgressionState meta, ClassId classId)
        {
            if (!CanPrestige(meta, classId)) return false;
            var entry = FindEntry(meta, classId);
            entry.PrestigeTier++;
            entry.TotalXp = 0;
            return true;
        }

        private static ClassGardenProgressEntry FindEntry(MetaProgressionState meta, ClassId classId)
        {
            if (meta?.GardenProgression?.ClassEntries == null) return null;

            for (var i = 0; i < meta.GardenProgression.ClassEntries.Count; i++)
            {
                if (meta.GardenProgression.ClassEntries[i].ClassId == classId)
                    return meta.GardenProgression.ClassEntries[i];
            }

            return null;
        }

        private static ClassGardenProgressEntry FindOrCreateEntry(MetaProgressionState meta, ClassId classId)
        {
            var entry = FindEntry(meta, classId);
            if (entry != null) return entry;

            entry = new ClassGardenProgressEntry { ClassId = classId, TotalXp = 0, PrestigeTier = 0 };
            meta.GardenProgression.ClassEntries.Add(entry);
            return entry;
        }
    }
}
