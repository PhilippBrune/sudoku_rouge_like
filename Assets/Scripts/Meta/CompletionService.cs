using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// Evaluates 4 global completion checks per MetaProgressionSystem spec.
    /// Each check is worth 25% of global completion.
    /// </summary>
    public sealed class CompletionService
    {
        // 5 board sizes (5-9) × 6 star levels (1-6) = 30 combos
        private const int TotalSizeStarCombos = 30;
        private const int TotalModifiers = 15;
        private const int TotalClasses = 8;
        private const int RequiredClassLevel = 30;

        public void Recalculate(CompletionTrackerState completion, MetaProgressionState meta,
            MasteryAchievementState mastery, ProfileStats stats)
        {
            var checks = 4f;
            var score = 0f;

            // Check 1: All board sizes × all stars cleared (30 combos)
            completion.AllSizesAllStarsCleared = stats.SizeStarCombosCleared >= TotalSizeStarCombos;

            // Check 2: All 15 modifiers cleared at least once
            completion.AllModifiersCleared = mastery.BossClearsByModifier.Count >= TotalModifiers;

            // Check 3: All 8 classes at Level 30+
            completion.AllClassesLevelThirty = CheckAllClassesAtLevel(meta, RequiredClassLevel);

            // Check 4: All relics discovered
            var totalRelics = System.Enum.GetValues(typeof(RelicId)).Length;
            completion.AllRelicsUnlocked = meta.DiscoveredRelics.Count >= totalRelics;

            if (completion.AllSizesAllStarsCleared) score += 1f;
            if (completion.AllModifiersCleared) score += 1f;
            if (completion.AllClassesLevelThirty) score += 1f;
            if (completion.AllRelicsUnlocked) score += 1f;

            completion.GlobalCompletionPercent = score / checks;
        }

        private static bool CheckAllClassesAtLevel(MetaProgressionState meta, int requiredLevel)
        {
            if (meta.GardenProgression?.ClassEntries == null)
                return false;

            var classesAtLevel = 0;
            for (var i = 0; i < meta.GardenProgression.ClassEntries.Count; i++)
            {
                var entry = meta.GardenProgression.ClassEntries[i];
                var (level, _, _) = XpService.DeriveLevel(entry.TotalXp);
                if (level >= requiredLevel)
                    classesAtLevel++;
            }

            return classesAtLevel >= TotalClasses;
        }
    }
}
