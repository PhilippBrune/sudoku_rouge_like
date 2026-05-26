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
