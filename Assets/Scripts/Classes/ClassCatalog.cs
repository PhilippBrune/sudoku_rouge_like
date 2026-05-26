using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassDefinition
    {
        public ClassId Id;
        public string Name;
        public int BaseHP;
        public int BasePencil;
        public int BaseItemSlots;
        public int BaseStartingRelics = 1;
        public string PassiveDescription;
        public string UnlockCondition;
    }

    public static class ClassCatalog
    {
        private static readonly ClassDefinition[] Definitions =
        {
            new ClassDefinition
            {
                Id = ClassId.NumberFreak, Name = "Number Freak",
                BaseHP = 10, BasePencil = 10, BaseItemSlots = 2,
                PassiveDescription = "After completing a puzzle with zero mistakes: +1 Reroll Token.",
                UnlockCondition = "Default (always unlocked)"
            },
            new ClassDefinition
            {
                Id = ClassId.GardenMonk, Name = "Garden Monk",
                BaseHP = 14, BasePencil = 5, BaseItemSlots = 1,
                PassiveDescription = "Every 5 correct placements: +1 HP. Level 15+: every 4.",
                UnlockCondition = "Defeat 2 bosses"
            },
            new ClassDefinition
            {
                Id = ClassId.ShrineArchivist, Name = "Shrine Archivist",
                BaseHP = 8, BasePencil = 15, BaseItemSlots = 2,
                PassiveDescription = "Each pencil mark placed has a 25% chance to auto-reveal the correct digit for that cell.",
                UnlockCondition = "Use 15 items"
            },
            new ClassDefinition
            {
                Id = ClassId.KoiGambler, Name = "Koi Gambler",
                BaseHP = 9, BasePencil = 8, BaseItemSlots = 2, BaseStartingRelics = 2,
                PassiveDescription = "33% wrong costs 0 HP; 33% correct grants +3 Gold.",
                UnlockCondition = "Collect 10 relics"
            },
            new ClassDefinition
            {
                Id = ClassId.StoneGardener, Name = "Stone Gardener",
                BaseHP = 11, BasePencil = 8, BaseItemSlots = 3,
                PassiveDescription = "First item used each puzzle node is not consumed.",
                UnlockCondition = "Defeat 10 bosses"
            },
            new ClassDefinition
            {
                Id = ClassId.LanternSeer, Name = "Lantern Seer",
                BaseHP = 7, BasePencil = 12, BaseItemSlots = 2,
                PassiveDescription = "Boss modifiers are 30% weaker. Before each boss puzzle, preview all active modifiers and reroll one for free.",
                UnlockCondition = "Collect 50,000 gold"
            },
            new ClassDefinition
            {
                Id = ClassId.ReedDuelist, Name = "Reed Duelist",
                BaseHP = 9, BasePencil = 9, BaseItemSlots = 2,
                PassiveDescription = "Perfect no-pencil puzzle: +2 Pencil. Every 5 such puzzles in a run: +1 HP.",
                UnlockCondition = "Find every unique item"
            },
            new ClassDefinition
            {
                Id = ClassId.QuietCartographer, Name = "Quiet Cartographer",
                BaseHP = 12, BasePencil = 6, BaseItemSlots = 1,
                PassiveDescription = "Preview next node's star & board size after a perfect tile. If you then choose that node, its difficulty is reduced by \u00bd star (min 1\u2605).",
                UnlockCondition = "Complete a full run (all 5 floors + boss) with zero mistakes"
            }
        };

        public static ClassDefinition GetDefinition(ClassId id)
        {
            for (var i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].Id == id)
                    return Definitions[i];
            }
            return Definitions[0];
        }

        public static ClassDefinition[] GetAll() => Definitions;

        // ── Per-level unlock schedules ──
        // Each entry: (level, description). Levels not listed have no unlock.
        private static readonly (int level, string unlock)[][] LevelUnlocks =
        {
            // NumberFreak
            new[] { (3,"+ 1 Pencil"),(5,"+10 Starting Gold"),(7,"+2 HP"),(10,"+2 Pencil, +2 HP, +1 Starting Relic"),
                    (13,"+10 Starting Gold"),(15,"+1 Reroll Token"),(18,"+1 Pencil"),(20,"+1 Item Slot, +1 Starting Relic"),
                    (25,"+2 HP, +2 Pencil"),(30,"Start: Rare Solver"),(35,"+1 Reroll Token"),(40,"Cosmetic: Golden Number frame") },
            // GardenMonk
            new[] { (3,"+1 Pencil"),(5,"+2 HP"),(7,"+10 Starting Gold"),(10,"+1 Item Slot"),
                    (13,"+2 HP"),(15,"Passive: heal every 4 placements (was 5)"),(18,"+1 Pencil"),(20,"+2 HP, +1 Reroll Token, +1 Starting Relic"),
                    (25,"+1 Pencil"),(30,"Start: Meditation Stone"),(35,"+2 HP"),(40,"Cosmetic: Blooming Monk frame") },
            // ShrineArchivist
            new[] { (3,"+2 Pencil"),(5,"+1 HP"),(7,"+10 Starting Gold"),(10,"+1 Reroll Token, +1 Starting Relic"),
                    (13,"+2 Pencil"),(15,"+1 HP, +1 Item Slot"),(18,"+10 Starting Gold"),(20,"Passive: 2 free pencil uses/cell"),
                    (25,"+2 Pencil, +1 HP"),(30,"Start: Pattern Scroll"),(35,"+1 Reroll Token"),(40,"Cosmetic: Scroll Archive frame") },
            // KoiGambler
            new[] { (3,"+1 HP"),(5,"+15 Starting Gold"),(7,"+1 Pencil"),(10,"Passive: correct gold chance 30%, +1 Starting Relic"),
                    (13,"+1 HP"),(15,"+1 Item Slot"),(18,"+15 Starting Gold"),(20,"Passive: no-HP-loss chance 30%"),
                    (25,"+2 Pencil, +1 HP"),(30,"Start: Lucky Charm"),(35,"+1 Reroll Token"),(40,"Cosmetic: Golden Koi frame") },
            // StoneGardener
            new[] { (3,"+1 Pencil"),(5,"+1 HP"),(7,"+1 Item Slot"),(10,"+10 Starting Gold, +1 Starting Relic"),
                    (13,"+2 Pencil"),(15,"+1 HP, +1 Reroll Token"),(18,"+1 Item Slot"),(20,"Passive: 2 free items/level, +1 Starting Relic"),
                    (25,"+2 HP"),(30,"Start: Harmony Charm"),(35,"+1 Pencil, +1 HP"),(40,"Cosmetic: Mossy Stone frame") },
            // LanternSeer
            new[] { (3,"+1 Pencil"),(5,"+1 HP"),(7,"+10 Starting Gold"),(10,"Passive: modifiers 25% weaker"),
                    (13,"+2 Pencil"),(15,"+1 HP, +1 Reroll Token"),(18,"+1 Item Slot"),(20,"+2 HP, +1 Starting Relic"),
                    (25,"Passive: modifiers 30% weaker"),(30,"Start: Lantern of Clarity"),(35,"+2 Pencil, +1 HP"),(40,"Cosmetic: Spirit Lantern frame") },
            // ReedDuelist
            new[] { (3,"+1 Pencil"),(5,"+1 HP"),(7,"+1 Reroll Token"),(10,"+10 Starting Gold"),
                    (13,"+2 Pencil"),(15,"Passive: perfect no-pencil → +3 Pencil"),(18,"+1 HP"),(20,"+1 Item Slot, +1 Starting Relic"),
                    (25,"+2 HP, +1 Pencil"),(30,"Start: Tea of Focus"),(35,"+1 Reroll Token"),(40,"Cosmetic: Cutting Reed frame") },
            // QuietCartographer
            new[] { (3,"+1 Pencil"),(5,"+10 Starting Gold"),(7,"+1 HP"),(10,"+1 Reroll Token, +1 Starting Relic"),
                    (13,"+1 Pencil, +1 HP"),(15,"Passive: preview always active"),(18,"+10 Starting Gold"),(20,"+1 Item Slot"),
                    (25,"+2 Pencil, +1 HP"),(30,"Start: Compass of Order"),(35,"+2 HP"),(40,"Cosmetic: Ink Map frame") },
        };

        /// <summary>Returns the unlock description for the given level, or null if none.</summary>
        public static string GetUnlockAtLevel(ClassId id, int level)
        {
            var idx = (int)id - 1; // enum starts at 1
            if (idx < 0 || idx >= LevelUnlocks.Length) return null;
            var table = LevelUnlocks[idx];
            for (var i = 0; i < table.Length; i++)
                if (table[i].level == level) return table[i].unlock;
            return null;
        }

        /// <summary>Returns all (level, unlock) entries for the class.</summary>
        public static (int level, string unlock)[] GetAllUnlocks(ClassId id)
        {
            var idx = (int)id - 1;
            if (idx < 0 || idx >= LevelUnlocks.Length) return new (int, string)[0];
            return LevelUnlocks[idx];
        }

        /// <summary>Returns the next unlock after currentLevel, or null if none.</summary>
        public static (int level, string unlock)? GetNextUnlock(ClassId id, int currentLevel)
        {
            var idx = (int)id - 1; // enum starts at 1
            if (idx < 0 || idx >= LevelUnlocks.Length) return null;
            var table = LevelUnlocks[idx];
            for (var i = 0; i < table.Length; i++)
                if (table[i].level > currentLevel) return table[i];
            return null;
        }

        public static string GetIconName(ClassId id)
        {
            return id switch
            {
                ClassId.NumberFreak       => "number_freak",
                ClassId.GardenMonk        => "garden_monk",
                ClassId.ShrineArchivist   => "shrine_archivist",
                ClassId.KoiGambler        => "koi_gambler",
                ClassId.StoneGardener     => "stone_gardener",
                ClassId.LanternSeer       => "lantern_seer",
                ClassId.ReedDuelist       => "reed_duelist",
                ClassId.QuietCartographer => "quiet_cartographer",
                _ => ""
            };
        }
    }
}
