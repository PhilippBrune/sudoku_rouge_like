using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Meta
{
    public sealed class ClassGardenProgressionService
    {
        // [REQ: META-XP-001] Adds to TotalXp — the single persistent XP value per class
        public void AddXp(MetaProgressionState meta, ClassId classId, int xpAmount)
        {
            var entry = FindOrCreateEntry(meta, classId);
            entry.TotalXp += xpAmount;
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

        // [REQ: META-PRESTIGE-001] Requires Level 40
        // [REQ: META-PRESTIGE-002] Boss gate: cumulative defeats >= (PrestigeTier + 1) × 3
        // [REQ: META-PRESTIGE-003] On prestige: TotalXp=0, PrestigeTier++
        public bool TryPrestige(MetaProgressionState meta, ClassId classId)
        {
            var entry = FindOrCreateEntry(meta, classId);
            var level = XpTable.DeriveLevel(entry.TotalXp);
            if (level < 40 || entry.PrestigeTier >= 9) return false;

            // Boss-gate: cumulative boss defeats >= (PrestigeTier + 1) × 3
            var requiredBosses = (entry.PrestigeTier + 1) * 3;
            var bossesDefeated = meta.ClassUnlocks?.BossesDefeated ?? 0;
            if (bossesDefeated < requiredBosses) return false;

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
