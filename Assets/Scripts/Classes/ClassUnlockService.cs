using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    /// <summary>
    /// Evaluates class unlock conditions based on the ClassXpProgressionSystem spec.
    /// All conditions are tracked cumulatively across all classes and runs.
    /// </summary>
    public sealed class ClassUnlockService
    {
        public IReadOnlyList<ClassId> EvaluateUnlocks(MetaProgressionState meta)
        {
            var newlyUnlocked = new List<ClassId>();
            var p = meta.ClassUnlocks;

            // 2. Garden Monk — defeat 2 bosses (cumulative)
            UnlockIfEligible(meta, ClassId.GardenMonk,
                p.CumulativeBossDefeats >= 2,
                newlyUnlocked);

            // 3. Shrine Archivist — use 15 items (cumulative)
            UnlockIfEligible(meta, ClassId.ShrineArchivist,
                p.CumulativeItemsUsed >= 15,
                newlyUnlocked);

            // 4. Koi Gambler — collect 10 relics (cumulative)
            UnlockIfEligible(meta, ClassId.KoiGambler,
                p.CumulativeRelicsCollected >= 10,
                newlyUnlocked);

            // 5. Stone Gardener — defeat 10 bosses (cumulative)
            UnlockIfEligible(meta, ClassId.StoneGardener,
                p.CumulativeBossDefeats >= 10,
                newlyUnlocked);

            // 6. Lantern Seer — collect 50,000 gold (cumulative)
            UnlockIfEligible(meta, ClassId.LanternSeer,
                p.CumulativeGoldCollected >= 50000,
                newlyUnlocked);

            // 7. Reed Duelist — find every unique item at least once (evaluate codex)
            UnlockIfEligible(meta, ClassId.ReedDuelist,
                p.AllUniqueItemsFound || EvaluateItemCodexComplete(meta),
                newlyUnlocked);

            // 8. Quiet Cartographer — clear an entire stage with zero pencil use and zero HP lost
            UnlockIfEligible(meta, ClassId.QuietCartographer,
                p.ClearedStageZeroPencilZeroHpLost,
                newlyUnlocked);

            return newlyUnlocked;
        }

        public void UpdateProgressFromRunResult(MetaProgressionState meta, RunResult result)
        {
            if (result.TutorialMode || result.Mode == GameMode.Tutorial)
                return;

            var p = meta.ClassUnlocks;

            if (result.ClearedBoss)
                p.CumulativeBossDefeats++;

            p.CumulativeGoldCollected += result.GoldEarned;
            p.CumulativeItemsUsed += result.ItemsUsedThisRun;
            p.CumulativeRelicsCollected += result.RelicsCollectedThisRun;

            if (result.FoundAllUniqueItems)
                p.AllUniqueItemsFound = true;

            if (result.ClearedStageNoPencilNoHpLoss)
                p.ClearedStageZeroPencilZeroHpLost = true;
        }

        public string GetUnlockRequirementText(ClassId classId)
        {
            return classId switch
            {
                ClassId.NumberFreak => "Default unlocked",
                ClassId.GardenMonk => "Defeat 2 bosses (cumulative)",
                ClassId.ShrineArchivist => "Use 15 items (cumulative)",
                ClassId.KoiGambler => "Collect 10 relics (cumulative)",
                ClassId.StoneGardener => "Defeat 10 bosses (cumulative)",
                ClassId.LanternSeer => "Collect 50,000 gold (cumulative)",
                ClassId.ReedDuelist => "Find every unique item at least once",
                ClassId.QuietCartographer => "Clear a stage with 0 pencil use and 0 HP lost",
                _ => "Unknown unlock requirement"
            };
        }

        private static bool EvaluateItemCodexComplete(MetaProgressionState meta)
        {
            if (meta.ItemCodex?.Entries == null || meta.ItemCodex.Entries.Count == 0)
                return false;

            for (var i = 0; i < meta.ItemCodex.Entries.Count; i++)
            {
                // Only check item entries (not relics) for Reed Duelist
                if (meta.ItemCodex.Entries[i].Type == "Relic") continue;
                if (!meta.ItemCodex.Entries[i].Discovered) return false;
            }
            return true;
        }

        private static void UnlockIfEligible(MetaProgressionState meta, ClassId classId,
            bool condition, List<ClassId> newlyUnlocked)
        {
            if (!condition || meta.UnlockedClasses.Contains(classId))
                return;

            meta.UnlockedClasses.Add(classId);
            newlyUnlocked.Add(classId);
        }
    }
}
