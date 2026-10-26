using SudokuRoguelike.Core;
using SudokuRoguelike.Platform;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [HARMONY-ACHIEVE-001] Evaluates Harmony achievement unlocks.
    /// Unlocks are persisted locally and forwarded to a platform bridge when one is available.
    /// </summary>
    public static class SteamAchievementService
    {
        private const int TotalClasses = 8;
        private static IPlatformAchievementBridge _platformBridge = new SteamworksReflectionAchievementBridge();

        public static string PlatformProviderName => _platformBridge?.ProviderName ?? "None";
        public static bool PlatformBridgeAvailable => _platformBridge?.IsAvailable ?? false;
        public static string LastPlatformError { get; private set; }

        public static PlatformCapabilityReport GetPlatformAchievementReport()
        {
            if (PlatformBridgeAvailable)
            {
                return new PlatformCapabilityReport(
                    "Achievements",
                    PlatformCapabilityState.Available,
                    PlatformProviderName,
                    "A platform achievement bridge is available. Validate unlock IDs and StoreStats behavior in the target platform runtime before release.");
            }

            return new PlatformCapabilityReport(
                "Achievements",
                PlatformCapabilityState.NotConfigured,
                PlatformProviderName,
                "No Steamworks-compatible SDK assembly is available. Achievements persist locally and are not forwarded to a platform backend.");
        }

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

        /// <summary>Every achievement's Steam API name (docs/Achievements.md); the portal must define each one.</summary>
        public static readonly string[] AllAchievementIds =
        {
            "harmony_1_win", "harmony_3_win", "harmony_5_win", "harmony_7_win", "harmony_9_win", "harmony_10_win",
            "harmony_10_perfect", "harmony_all_classes",
            "first_puzzle", "first_boss", "first_win", "floor_5", "perfect_run", "no_items_win", "one_hp_win",
            "cursed_three", "nine_by_nine", "five_stars",
            "class_second", "class_all", "class_level_10", "class_level_40", "win_every_class",
            "endless_25", "trials_grandmaster", "monthly_walk",
            "daily_streak_7", "codex_half", "codex_all"
        };

        public const int EndlessDepthGoal = 25;
        public const int DailyStreakGoal = 7;

        /// <summary>
        /// [REQ: ACHIEVE-RUN-001] [REQ: ACHIEVE-CLASS-001] [REQ: ACHIEVE-COLLECT-001] Called by ProfileService after every
        /// Garden Run end, once the run's XP, class unlocks and Codex finds are counted. <paramref name="finalHp"/> is the HP
        /// left at the end, <paramref name="maxClassLevel"/> the highest level of any class. <paramref name="meta"/> is
        /// modified in-place (ClassesWithAWin).
        /// </summary>
        public static void EvaluateProgressAchievements(MetaProgressionState meta, ProfileStats stats,
            MasteryAchievementState mastery, RunResult result, int finalHp, int maxClassLevel, int dailyStreak)
        {
            if (stats?.TotalPuzzlesSolved >= 1) GrantAchievement(mastery, "first_puzzle");
            if (stats?.TotalBossesDefeated >= 1) GrantAchievement(mastery, "first_boss");

            if (result != null && result.Mode == GameMode.GardenRun && !result.TutorialMode && !result.DisableProgressionRewards)
            {
                if (result.FloorReached >= 5) GrantAchievement(mastery, "floor_5");
                if (result.CursedPuzzlesAccepted >= 3) GrantAchievement(mastery, "cursed_three");
                foreach (var tile in result.TileXpEntries ?? new System.Collections.Generic.List<TileXpEntry>())
                {
                    if (tile.BoardSize >= 9) GrantAchievement(mastery, "nine_by_nine");
                    if (tile.Stars >= 5) GrantAchievement(mastery, "five_stars");
                }
                if (result.Victory)
                {
                    GrantAchievement(mastery, "first_win");
                    if (result.MistakesMade == 0) GrantAchievement(mastery, "perfect_run");
                    if (result.ItemsUsedThisRun == 0) GrantAchievement(mastery, "no_items_win");
                    if (finalHp == 1) GrantAchievement(mastery, "one_hp_win");
                    if (meta != null)
                    {
                        meta.ClassesWithAWin ??= new System.Collections.Generic.List<ClassId>();
                        if (!meta.ClassesWithAWin.Contains(result.PlayedClassId)) meta.ClassesWithAWin.Add(result.PlayedClassId);
                        if (meta.ClassesWithAWin.Count >= TotalClasses) GrantAchievement(mastery, "win_every_class");
                    }
                }
            }

            var unlocked = new System.Collections.Generic.HashSet<ClassId> { ClassId.NumberFreak };
            if (meta?.UnlockedClasses != null) unlocked.UnionWith(meta.UnlockedClasses);
            if (unlocked.Count >= 2) GrantAchievement(mastery, "class_second");
            if (unlocked.Count >= TotalClasses) GrantAchievement(mastery, "class_all");
            if (maxClassLevel >= 10) GrantAchievement(mastery, "class_level_10");
            if (maxClassLevel >= 40) GrantAchievement(mastery, "class_level_40");
            if (dailyStreak >= DailyStreakGoal) GrantAchievement(mastery, "daily_streak_7");

            if (meta == null) return;
            var codex = new[] { CodexProgress.Items(meta), CodexProgress.Relics(meta), CodexProgress.Rules(meta), CodexProgress.Curses(meta) };
            int found = 0, total = 0;
            foreach (var count in codex) { found += count.Found; total += count.Total; }
            if (CodexHalf(found, total)) GrantAchievement(mastery, "codex_half");
            if (total > 0 && found >= total) GrantAchievement(mastery, "codex_all");
        }

        /// <summary>[REQ: ACHIEVE-MODE-001] At the end of an Endless Zen session, a Spirit Trial or a Monthly Walk.</summary>
        public static void EvaluateModeAchievements(MasteryAchievementState mastery, GameMode mode, bool completed,
            int endlessDepth = 0, SpiritTrialsTier tier = SpiritTrialsTier.Apprentice)
        {
            if (mode == GameMode.EndlessZen && endlessDepth >= EndlessDepthGoal) GrantAchievement(mastery, "endless_25");
            if (mode == GameMode.SpiritTrials && completed && tier == SpiritTrialsTier.Grandmaster) GrantAchievement(mastery, "trials_grandmaster");
            if (mode == GameMode.SeasonalChallenge && completed) GrantAchievement(mastery, "monthly_walk");
        }

        /// <summary>Half of every Codex page found together.</summary>
        public static bool CodexHalf(int found, int total) => total > 0 && found * 2 >= total;

        private static void GrantAchievement(MasteryAchievementState mastery, string achievementId)
        {
            if (mastery == null) return;
            if (mastery.UnlockedAchievements == null)
                mastery.UnlockedAchievements = new System.Collections.Generic.List<string>();
            if (mastery.UnlockedAchievements.Contains(achievementId))
            {
                ForwardToPlatform(achievementId);
                return;
            }

            mastery.UnlockedAchievements.Add(achievementId);
            UnityEngine.Debug.Log($"[Achievement] {achievementId}");
            ForwardToPlatform(achievementId);
        }

        public static int SyncUnlockedAchievements(MasteryAchievementState mastery)
        {
            if (mastery?.UnlockedAchievements == null)
                return 0;

            var forwarded = 0;
            foreach (var achievementId in mastery.UnlockedAchievements)
            {
                if (ForwardToPlatform(achievementId))
                    forwarded++;
            }

            return forwarded;
        }

        public static void SetPlatformBridgeForTests(IPlatformAchievementBridge bridge)
        {
            _platformBridge = bridge ?? new SteamworksReflectionAchievementBridge();
            LastPlatformError = null;
        }

        public static void ResetPlatformBridgeForTests()
        {
            _platformBridge = new SteamworksReflectionAchievementBridge();
            LastPlatformError = null;
        }

        private static bool ForwardToPlatform(string achievementId)
        {
            LastPlatformError = null;
            if (_platformBridge == null || !_platformBridge.IsAvailable)
                return false;

            if (_platformBridge.TryUnlockAchievement(achievementId, out var error))
                return true;

            LastPlatformError = error;
            UnityEngine.Debug.LogWarning($"[Achievement] Platform unlock failed for {achievementId}: {error}");
            return false;
        }
    }
}
