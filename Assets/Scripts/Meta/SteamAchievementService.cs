using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [HARMONY-ACHIEVE-001] Evaluates Harmony achievement unlocks.
    /// The current implementation persists unlocked IDs locally in the save file and logs them.
    /// A future Steamworks bridge can forward the same IDs to the platform API.
    /// </summary>
    public static class SteamAchievementService
    {
        private const int TotalClasses = 8;

        /// <summary>
        /// Called by <see cref="SudokuRoguelike.Save.ProfileService"/> after every Garden Run end.
        /// <paramref name="meta"/> is modified in-place (HarmonyV5PlusWins updated).
        /// <paramref name="mastery"/> persists unlocked achievement IDs locally.
        /// </summary>
        public static void EvaluateHarmonyAchievements(
            MetaProgressionState meta, MasteryAchievementState mastery, RunResult result)
        {
            if (!result.Victory || result.Mode != GameMode.GardenRun) return;

            var level = result.HarmonyLevel;
            var classId = result.PlayedClassId;

            if (level >= 1)  GrantAchievement(mastery, "harmony_1_win");
            if (level >= 3)  GrantAchievement(mastery, "harmony_3_win");
            if (level >= 5)  GrantAchievement(mastery, "harmony_5_win");
            if (level >= 7)  GrantAchievement(mastery, "harmony_7_win");
            if (level >= 9)  GrantAchievement(mastery, "harmony_9_win");
            if (level >= 10) GrantAchievement(mastery, "harmony_10_win");

            if (level >= 10 && result.MistakesMade == 0)
                GrantAchievement(mastery, "harmony_10_perfect");

            if (level >= 5)
            {
                if (meta.HarmonyV5PlusWins == null)
                    meta.HarmonyV5PlusWins = new System.Collections.Generic.List<ClassId>();

                if (!meta.HarmonyV5PlusWins.Contains(classId))
                    meta.HarmonyV5PlusWins.Add(classId);

                if (meta.HarmonyV5PlusWins.Count >= TotalClasses)
                    GrantAchievement(mastery, "harmony_all_classes");
            }
        }

        private static void GrantAchievement(MasteryAchievementState mastery, string achievementId)
        {
            if (mastery == null) return;
            if (mastery.UnlockedAchievements == null)
                mastery.UnlockedAchievements = new System.Collections.Generic.List<string>();
            if (mastery.UnlockedAchievements.Contains(achievementId)) return;

            mastery.UnlockedAchievements.Add(achievementId);
            UnityEngine.Debug.Log($"[Achievement] {achievementId}");
        }
    }
}
