using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Meta
{
    public sealed class ClassGardenProgressionService
    {
        private const int MaxLevel = 40;
        private const int MaxPrestigeTier = 9;

        // ---- Level Derivation ----

        /// <summary>XP required to advance FROM the given level. Delegates to XpService two-phase curve.</summary>
        public static int XpToNextLevel(int level) => XpService.XpToNextLevel(level);

        /// <summary>Derives (level, progressXp, xpToNext) from a total XP value.</summary>
        public static (int Level, int ProgressXp, int XpToNext) DeriveLevel(int totalXp)
            => XpService.DeriveLevel(totalXp);

        // ---- Per-Class Stat Bonuses ----

        /// <summary>
        /// Returns cumulative stat bonuses for a given class at a given level.
        /// Each class has its own unlock schedule per the spec.
        /// </summary>
        public static (int HpBonus, int PencilBonus, int RerollBonus, int SlotBonus, int StartingGold)
            GetStatBonuses(ClassId classId, int level)
        {
            var hp = 0; var pencil = 0; var rerolls = 0; var slots = 0; var gold = 0;
            var schedule = GetUnlockSchedule(classId);
            for (var i = 0; i < schedule.Count; i++)
            {
                if (level >= schedule[i].Level)
                {
                    hp += schedule[i].Hp;
                    pencil += schedule[i].Pencil;
                    rerolls += schedule[i].Reroll;
                    slots += schedule[i].Slot;
                    gold += schedule[i].Gold;
                }
            }
            return (hp, pencil, rerolls, slots, gold);
        }

        /// <summary>Returns a short text describing what the next level-up grants.</summary>
        public static string GetNextUnlock(ClassId classId, int level)
        {
            var schedule = GetUnlockSchedule(classId);
            for (var i = 0; i < schedule.Count; i++)
            {
                if (schedule[i].Level > level)
                    return $"Lv {schedule[i].Level} — {schedule[i].Description}";
            }
            return level >= MaxLevel ? "Max Level" : "No upcoming unlock";
        }

        /// <summary>Returns all unlocks triggered when levelling from oldLevel to newLevel.</summary>
        public static List<string> GetUnlocksInRange(ClassId classId, int oldLevel, int newLevel)
        {
            var unlocks = new List<string>();
            var schedule = GetUnlockSchedule(classId);
            for (var i = 0; i < schedule.Count; i++)
            {
                if (oldLevel < schedule[i].Level && newLevel >= schedule[i].Level)
                    unlocks.Add($"Lv {schedule[i].Level} — {schedule[i].Description}");
            }
            return unlocks;
        }

        // ---- Apply Run ----

        public GardenProgressionUpdate ApplyRun(MetaProgressionState meta, ClassId playedClass, RunResult result)
        {
            var state = meta.GardenProgression ??= new GardenClassProgressionState();
            var entry = GetOrCreateClassEntry(state, playedClass);

            var runXp = result.XpEarned;

            state.ArchiveRunCount++;
            if (result.Victory) state.ArchiveSeedsBloomed++;
            if (result.ClearedBoss) state.ArchiveBossesDefeated++;
            if (result.PerfectClear) state.ArchivePerfectRuns++;
            state.TotalXpEarned += runXp;

            // Per-class XP — stored as cumulative total, level derived
            var oldTotalXp = entry.TotalXp;
            entry.TotalXp += runXp;
            var (oldLevel, _, _) = DeriveLevel(oldTotalXp);
            var (newLevel, progressXp, xpToNext) = DeriveLevel(entry.TotalXp);

            var classUnlocks = GetUnlocksInRange(playedClass, oldLevel, newLevel);

            // Class-level prestige check
            var prestigeGranted = false;
            if (CanPrestige(entry, state.ArchiveBossesDefeated))
            {
                entry.PrestigeTier++;
                entry.TotalXp = 0; // reset XP
                prestigeGranted = true;
                newLevel = 1;
                progressXp = 0;
            }

            UnityEngine.Debug.Log(
                $"[XP] Run end — Class:{playedClass} XP+{runXp} " +
                $"ClassLv:{oldLevel}→{newLevel} " +
                $"Prestige:{entry.PrestigeTier} " +
                $"Unlocks:{classUnlocks.Count}");

            return new GardenProgressionUpdate
            {
                XpAwarded = runXp,
                LevelsGained = newLevel - oldLevel,
                PrestigeGranted = prestigeGranted,
                NewLevel = newLevel,
                NewPrestigeTier = entry.PrestigeTier,
                ClassId = playedClass,
                ClassOldLevel = oldLevel,
                ClassNewLevel = newLevel,
                ClassLevelsGained = newLevel - oldLevel,
                ClassUnlocksGranted = classUnlocks,
                ClassProgressXp = progressXp,
                ClassXpToNext = xpToNext
            };
        }

        /// <summary>Tries to prestige a class. Returns true if prestige was applied.</summary>
        public static bool TryPrestige(ClassGardenProgressEntry entry, int archiveBossesDefeated)
        {
            if (!CanPrestige(entry, archiveBossesDefeated)) return false;
            entry.PrestigeTier++;
            entry.TotalXp = 0;
            return true;
        }

        private static ClassGardenProgressEntry GetOrCreateClassEntry(GardenClassProgressionState state, ClassId classId)
        {
            for (var i = 0; i < state.ClassEntries.Count; i++)
            {
                if (state.ClassEntries[i].ClassId == classId)
                    return state.ClassEntries[i];
            }
            var created = new ClassGardenProgressEntry { ClassId = classId };
            state.ClassEntries.Add(created);
            return created;
        }

        private static bool CanPrestige(ClassGardenProgressEntry entry, int archiveBossesDefeated)
        {
            if (entry.PrestigeTier >= MaxPrestigeTier) return false;
            var (level, _, _) = DeriveLevel(entry.TotalXp);
            return level >= MaxLevel && archiveBossesDefeated >= (entry.PrestigeTier + 1) * 3;
        }

        // ---- Unlock Schedule Data ----

        private struct LevelUnlock
        {
            public int Level;
            public int Hp, Pencil, Reroll, Slot, Gold;
            public string Description;
        }

        private static List<LevelUnlock> GetUnlockSchedule(ClassId classId)
        {
            return classId switch
            {
                ClassId.NumberFreak => new List<LevelUnlock>
                {
                    new() { Level = 3, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 5, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 7, Hp = 2, Description = "+2 HP" },
                    new() { Level = 10, Pencil = 2, Hp = 2, Description = "+2 Pencil, +2 HP" },
                    new() { Level = 13, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 15, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 18, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 20, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 25, Hp = 2, Pencil = 2, Description = "+2 HP, +2 Pencil" },
                    new() { Level = 30, Description = "Start with 1 Rare Solver" },
                    new() { Level = 35, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 40, Description = "Cosmetic: Golden Number frame" },
                },
                ClassId.GardenMonk => new List<LevelUnlock>
                {
                    new() { Level = 3, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 5, Hp = 2, Description = "+2 HP" },
                    new() { Level = 7, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 10, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 13, Hp = 2, Description = "+2 HP" },
                    new() { Level = 15, Description = "Passive: heal every 4 placements (was 5)" },
                    new() { Level = 18, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 20, Hp = 2, Reroll = 1, Description = "+2 HP, +1 Reroll Token" },
                    new() { Level = 25, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 30, Description = "Start with 1 Meditation Stone" },
                    new() { Level = 35, Hp = 2, Description = "+2 HP" },
                    new() { Level = 40, Description = "Cosmetic: Blooming Monk frame" },
                },
                ClassId.ShrineArchivist => new List<LevelUnlock>
                {
                    new() { Level = 3, Pencil = 2, Description = "+2 Pencil" },
                    new() { Level = 5, Hp = 1, Description = "+1 HP" },
                    new() { Level = 7, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 10, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 13, Pencil = 2, Description = "+2 Pencil" },
                    new() { Level = 15, Hp = 1, Slot = 1, Description = "+1 HP, +1 Item Slot" },
                    new() { Level = 18, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 20, Description = "Passive: first 2 pencil uses per cell free (was 1)" },
                    new() { Level = 25, Pencil = 2, Hp = 1, Description = "+2 Pencil, +1 HP" },
                    new() { Level = 30, Description = "Start with 1 Pattern Scroll" },
                    new() { Level = 35, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 40, Description = "Cosmetic: Scroll Archive frame" },
                },
                ClassId.KoiGambler => new List<LevelUnlock>
                {
                    new() { Level = 3, Hp = 1, Description = "+1 HP" },
                    new() { Level = 5, Gold = 15, Description = "+15 Starting Gold" },
                    new() { Level = 7, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 10, Description = "Passive: correct gold chance → 30% (was 25%)" },
                    new() { Level = 13, Hp = 1, Description = "+1 HP" },
                    new() { Level = 15, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 18, Gold = 15, Description = "+15 Starting Gold" },
                    new() { Level = 20, Description = "Passive: wrong no-HP-loss chance → 30% (was 25%)" },
                    new() { Level = 25, Pencil = 2, Hp = 1, Description = "+2 Pencil, +1 HP" },
                    new() { Level = 30, Description = "Start with 1 Lucky Charm" },
                    new() { Level = 35, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 40, Description = "Cosmetic: Golden Koi frame" },
                },
                ClassId.StoneGardener => new List<LevelUnlock>
                {
                    new() { Level = 3, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 5, Hp = 1, Description = "+1 HP" },
                    new() { Level = 7, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 10, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 13, Pencil = 2, Description = "+2 Pencil" },
                    new() { Level = 15, Hp = 1, Reroll = 1, Description = "+1 HP, +1 Reroll Token" },
                    new() { Level = 18, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 20, Description = "Passive: first 2 items free each level (was 1)" },
                    new() { Level = 25, Hp = 2, Description = "+2 HP" },
                    new() { Level = 30, Description = "Start with 1 Harmony Charm" },
                    new() { Level = 35, Pencil = 1, Hp = 1, Description = "+1 Pencil, +1 HP" },
                    new() { Level = 40, Description = "Cosmetic: Mossy Stone frame" },
                },
                ClassId.LanternSeer => new List<LevelUnlock>
                {
                    new() { Level = 3, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 5, Hp = 1, Description = "+1 HP" },
                    new() { Level = 7, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 10, Description = "Passive: boss modifiers 25% weaker (was 20%)" },
                    new() { Level = 13, Pencil = 2, Description = "+2 Pencil" },
                    new() { Level = 15, Hp = 1, Reroll = 1, Description = "+1 HP, +1 Reroll Token" },
                    new() { Level = 18, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 20, Hp = 2, Description = "+2 HP" },
                    new() { Level = 25, Description = "Passive: boss modifiers 30% weaker (was 25%)" },
                    new() { Level = 30, Description = "Start with 1 Lantern of Clarity" },
                    new() { Level = 35, Pencil = 2, Hp = 1, Description = "+2 Pencil, +1 HP" },
                    new() { Level = 40, Description = "Cosmetic: Spirit Lantern frame" },
                },
                ClassId.ReedDuelist => new List<LevelUnlock>
                {
                    new() { Level = 3, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 5, Hp = 1, Description = "+1 HP" },
                    new() { Level = 7, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 10, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 13, Pencil = 2, Description = "+2 Pencil" },
                    new() { Level = 15, Description = "Passive: perfect no-pencil bonus → +3 Pencil (was +2)" },
                    new() { Level = 18, Hp = 1, Description = "+1 HP" },
                    new() { Level = 20, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 25, Hp = 2, Pencil = 1, Description = "+2 HP, +1 Pencil" },
                    new() { Level = 30, Description = "Start with 1 Tea of Focus" },
                    new() { Level = 35, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 40, Description = "Cosmetic: Cutting Reed frame" },
                },
                ClassId.QuietCartographer => new List<LevelUnlock>
                {
                    new() { Level = 3, Pencil = 1, Description = "+1 Pencil" },
                    new() { Level = 5, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 7, Hp = 1, Description = "+1 HP" },
                    new() { Level = 10, Reroll = 1, Description = "+1 Reroll Token" },
                    new() { Level = 13, Pencil = 1, Hp = 1, Description = "+1 Pencil, +1 HP" },
                    new() { Level = 15, Description = "Passive: preview always active (no perfect needed)" },
                    new() { Level = 18, Gold = 10, Description = "+10 Starting Gold" },
                    new() { Level = 20, Slot = 1, Description = "+1 Item Slot" },
                    new() { Level = 25, Pencil = 2, Hp = 1, Description = "+2 Pencil, +1 HP" },
                    new() { Level = 30, Description = "Start with 1 Compass of Order" },
                    new() { Level = 35, Hp = 2, Description = "+2 HP" },
                    new() { Level = 40, Description = "Cosmetic: Ink Map frame" },
                },
                _ => new List<LevelUnlock>()
            };
        }
    }

    public sealed class GardenProgressionUpdate
    {
        public int XpAwarded;
        public int LevelsGained;
        public bool PrestigeGranted;
        public int NewLevel;
        public int NewPrestigeTier;

        public ClassId ClassId;
        public int ClassOldLevel;
        public int ClassNewLevel;
        public int ClassLevelsGained;
        public List<string> ClassUnlocksGranted = new();
        public int ClassProgressXp;
        public int ClassXpToNext;
    }
}
