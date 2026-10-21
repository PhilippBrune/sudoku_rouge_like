using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassUnlockService
    {
        // [REQ: META-UNLOCK-001] The unlock goals; CheckNewUnlocks and the progress shown in the class garden share them.
        public const int GardenMonkBosses = 2;
        public const int ShrineArchivistItemsUsed = 15;
        public const int KoiGamblerRelics = 10;
        public const int StoneGardenerBosses = 10;
        public const int LanternSeerGold = 50000;

        /// <summary>How far a class is toward its unlock goal; a goal without a count (a perfect run) is 0 or 1 of 1.</summary>
        public readonly struct UnlockProgress
        {
            public readonly int Current;
            public readonly int Target;
            public readonly bool Unlocked;

            public UnlockProgress(int current, int target, bool unlocked)
            {
                Target = Math.Max(1, target);
                Current = Math.Clamp(current, 0, Target);
                Unlocked = unlocked;
            }

            public float Fraction => (float)Current / Target;
        }

        public List<ClassId> CheckNewUnlocks(MetaProgressionState meta)
        {
            var unlocked = new List<ClassId>();
            if (meta == null) return unlocked;

            var progress = meta.ClassUnlocks ?? new ClassUnlockProgress();

            // NumberFreak: always unlocked (starter)
            TryUnlock(meta, ClassId.NumberFreak, true, unlocked);

            // GardenMonk: defeat 2 bosses
            TryUnlock(meta, ClassId.GardenMonk, progress.BossesDefeated >= GardenMonkBosses, unlocked);

            // ShrineArchivist: use 15 items
            TryUnlock(meta, ClassId.ShrineArchivist, progress.ItemsUsed >= ShrineArchivistItemsUsed, unlocked);

            // KoiGambler: collect 10 relics
            TryUnlock(meta, ClassId.KoiGambler, progress.RelicsCollected >= KoiGamblerRelics, unlocked);

            // StoneGardener: defeat 10 bosses
            TryUnlock(meta, ClassId.StoneGardener, progress.BossesDefeated >= StoneGardenerBosses, unlocked);

            // LanternSeer: collect 50,000 gold total
            TryUnlock(meta, ClassId.LanternSeer, progress.GoldCollected >= LanternSeerGold, unlocked);

            // ReedDuelist: complete the item codex
            TryUnlock(meta, ClassId.ReedDuelist, progress.ItemCodexComplete, unlocked);

            // QuietCartographer: complete a full run (all 5 floors + boss) with zero mistakes
            TryUnlock(meta, ClassId.QuietCartographer, progress.PerfectFullRunAllFloors, unlocked);

            return unlocked;
        }

        /// <summary>[REQ: META-UNLOCK-006] A class's way to its unlock, for the class garden (UI-STYLE-019).</summary>
        public static UnlockProgress GetProgress(MetaProgressionState meta, ClassId id)
        {
            var unlocked = id == ClassId.NumberFreak || (meta?.UnlockedClasses != null && meta.UnlockedClasses.Contains(id));
            var p = meta?.ClassUnlocks ?? new ClassUnlockProgress();
            switch (id)
            {
                case ClassId.NumberFreak: return new UnlockProgress(1, 1, true);
                case ClassId.GardenMonk: return new UnlockProgress(p.BossesDefeated, GardenMonkBosses, unlocked);
                case ClassId.ShrineArchivist: return new UnlockProgress(p.ItemsUsed, ShrineArchivistItemsUsed, unlocked);
                case ClassId.KoiGambler: return new UnlockProgress(p.RelicsCollected, KoiGamblerRelics, unlocked);
                case ClassId.StoneGardener: return new UnlockProgress(p.BossesDefeated, StoneGardenerBosses, unlocked);
                case ClassId.LanternSeer: return new UnlockProgress(p.GoldCollected, LanternSeerGold, unlocked);
                case ClassId.ReedDuelist:
                    var items = CodexProgress.RequiredItems(meta);
                    return new UnlockProgress(p.ItemCodexComplete ? items.Total : items.Found, items.Total, unlocked);
                default: return new UnlockProgress(p.PerfectFullRunAllFloors ? 1 : 0, 1, unlocked);
            }
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
