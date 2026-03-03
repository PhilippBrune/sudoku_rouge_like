using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    public sealed class CompletionService
    {
        public void Recalculate(CompletionTrackerState completion, MetaProgressionState meta, MasteryAchievementState mastery, ProfileStats stats)
        {
            var checks = 5f;
            var score = 0f;

            completion.AllSizesAllStarsCleared = stats.TotalRuns >= 25;
            completion.AllModifiersCleared = mastery.BossClearsByModifier.Count >= System.Enum.GetValues(typeof(BossModifierId)).Length;
            completion.AllClassesLevelThirty = meta.UnlockedClasses.Count >= 6;
            completion.AllRelicsUnlocked = meta.UnlockedRelics.Count >= 5;
            completion.MultiStageBossHighHeatClear = stats.HighestHeatScore >= 5.5f && stats.BossClears > 0;

            if (completion.AllSizesAllStarsCleared) score += 1f;
            if (completion.AllModifiersCleared) score += 1f;
            if (completion.AllClassesLevelThirty) score += 1f;
            if (completion.AllRelicsUnlocked) score += 1f;
            if (completion.MultiStageBossHighHeatClear) score += 1f;

            completion.GlobalCompletionPercent = score / checks;
        }
    }
}
