using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassUnlockService
    {
        public List<ClassId> CheckNewUnlocks(MetaProgressionState meta)
        {
            var unlocked = new List<ClassId>();
            if (meta == null) return unlocked;

            var progress = meta.ClassUnlocks ?? new ClassUnlockProgress();

            // NumberFreak: always unlocked (starter)
            TryUnlock(meta, ClassId.NumberFreak, true, unlocked);

            // GardenMonk: defeat 2 bosses
            TryUnlock(meta, ClassId.GardenMonk, progress.BossesDefeated >= 2, unlocked);

            // ShrineArchivist: use 15 items
            TryUnlock(meta, ClassId.ShrineArchivist, progress.ItemsUsed >= 15, unlocked);

            // KoiGambler: collect 10 relics
            TryUnlock(meta, ClassId.KoiGambler, progress.RelicsCollected >= 10, unlocked);

            // StoneGardener: defeat 10 bosses
            TryUnlock(meta, ClassId.StoneGardener, progress.BossesDefeated >= 10, unlocked);

            // LanternSeer: collect 50,000 gold total
            TryUnlock(meta, ClassId.LanternSeer, progress.GoldCollected >= 50000, unlocked);

            // ReedDuelist: complete the item codex
            TryUnlock(meta, ClassId.ReedDuelist, progress.ItemCodexComplete, unlocked);

            // QuietCartographer: complete a full run (all 5 floors + boss) with zero mistakes
            TryUnlock(meta, ClassId.QuietCartographer, progress.PerfectFullRunAllFloors, unlocked);

            return unlocked;
        }

        private static void TryUnlock(MetaProgressionState meta, ClassId id, bool condition, List<ClassId> newlyUnlocked)
        {
            if (!condition) return;
            if (meta.UnlockedClasses.Contains(id)) return;

            meta.UnlockedClasses.Add(id);
            newlyUnlocked.Add(id);
        }
    }
}
