using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// Mirrors local ProfileStats to Steam User Stats via reflection so the
    /// "Stats" store-page category is satisfied without a hard SDK dependency.
    /// Requires Steamworks.NET imported and SteamManager initialised at runtime.
    /// </summary>
    public static class SteamStatsService
    {
        private static readonly MethodInfo SetStatInt;
        private static readonly MethodInfo StoreStats;
        private static readonly PropertyInfo SteamManagerInitialized;

        static SteamStatsService()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var steamUserStats = FindType(assemblies, "Steamworks.SteamUserStats");

            // bool SetStat(string pchName, int nData)
            SetStatInt = FindStaticMethod(steamUserStats, "SetStat", typeof(string), typeof(int));
            // bool StoreStats()
            StoreStats = FindStaticMethod(steamUserStats, "StoreStats");

            var steamManager = FindType(assemblies, "SteamManager");
            SteamManagerInitialized = steamManager?.GetProperty(
                "Initialized",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }

        public static bool IsAvailable
        {
            get
            {
                if (SetStatInt == null || StoreStats == null) return false;
                if (SteamManagerInitialized == null) return true;
                try { return (bool)SteamManagerInitialized.GetValue(null, null); }
                catch { return false; }
            }
        }

        // ── Public API ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Mirrors the four core stats to Steam and persists them with StoreStats().
        /// Call once at the end of every run (inside ProfileService.RecordRunAndGetNewUnlocks).
        /// </summary>
        public static void SyncFromLocalStats(ProfileStats stats)
        {
            if (!IsAvailable || stats == null) return;
            try
            {
                TrySetStat("stat_puzzles_solved",  stats.TotalPuzzlesSolved);
                TrySetStat("stat_bosses_defeated", stats.TotalBossesDefeated);
                TrySetStat("stat_runs_completed",  stats.TotalRunsCompleted);
                TryFlushStats();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SteamStats] SyncFromLocalStats failed: {ex.Message}");
            }
        }

        // ── Internals ───────────────────────────────────────────────────────────────

        private static void TrySetStat(string name, int value)
        {
            try { SetStatInt?.Invoke(null, new object[] { name, value }); }
            catch (Exception ex) { Debug.LogWarning($"[SteamStats] SetStat({name}) failed: {ex.Message}"); }
        }

        private static void TryFlushStats()
        {
            try { StoreStats?.Invoke(null, null); }
            catch (Exception ex) { Debug.LogWarning($"[SteamStats] StoreStats failed: {ex.Message}"); }
        }

        private static Type FindType(Assembly[] assemblies, string fullName)
        {
            return assemblies
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.FullName == fullName || t.Name == fullName);
        }

        private static Type[] SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).ToArray(); }
        }

        private static MethodInfo FindStaticMethod(Type type, string name, params Type[] paramTypes)
        {
            if (type == null) return null;
            return type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, paramTypes, null);
        }
    }
}
