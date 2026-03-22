using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    public sealed class CompletionService
    {
        public void Recalculate(CompletionTrackerState completion, MetaProgressionState meta, MasteryAchievementState mastery, ProfileStats stats)
        {
            var checks = 4f;
            var score = 0f;

            completion.AllSizesAllStarsCleared = stats.TotalRuns >= 25;
            completion.AllModifiersCleared = mastery.BossClearsByModifier.Count >= System.Enum.GetValues(typeof(BossModifierId)).Length;
            completion.AllClassesLevelThirty = meta.UnlockedClasses.Count >= 6;
            completion.AllRelicsUnlocked = meta.DiscoveredRelics.Count >= 5;

            if (completion.AllSizesAllStarsCleared) score += 1f;
            if (completion.AllModifiersCleared) score += 1f;
            if (completion.AllClassesLevelThirty) score += 1f;
            if (completion.AllRelicsUnlocked) score += 1f;

            completion.GlobalCompletionPercent = score / checks;
        }
    }
}
