using UnityEngine;

// Full leaderboard implementation compiles only when Steamworks.NET is present.
// The stub below ensures the project always compiles regardless.
#if STEAMWORKS_NET
using Steamworks;
#endif

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// Submits a run score to the "Harmony_RunScore" Steam leaderboard at run-win.
    /// Requires Steamworks.NET imported and the leaderboard created in the Steamworks
    /// partner portal (Descending / Numeric).
    /// Call Initialize() once at game startup, then SubmitRunScore(xp) on each win.
    /// </summary>
    public static class SteamLeaderboardService
    {
        private const string LeaderboardName = "Harmony_RunScore";

#if STEAMWORKS_NET
        private enum State { Idle, FetchingHandle, Ready }

        private static State _state = State.Idle;
        private static SteamLeaderboard_t _leaderboard;
        private static CallResult<LeaderboardFindResult_t> _findResult;
        private static int _pendingScore = -1;

        public static void Initialize()
        {
            if (_state != State.Idle) return;
            try
            {
                if (!SteamManager.Initialized) return;
                var call = SteamUserStats.FindOrCreateLeaderboard(
                    LeaderboardName,
                    ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
                    ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
                _findResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
                _findResult.Set(call);
                _state = State.FetchingHandle;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SteamLeaderboard] Initialize failed: {ex.Message}");
            }
        }

        private static void OnLeaderboardFound(LeaderboardFindResult_t result, bool bIOFailure)
        {
            if (bIOFailure || result.m_bLeaderboardFound == 0)
            {
                _state = State.Idle;
                Debug.LogWarning("[SteamLeaderboard] Leaderboard not found.");
                return;
            }

            _leaderboard = result.m_hSteamLeaderboard;
            _state = State.Ready;
            Debug.Log($"[SteamLeaderboard] Ready (handle={_leaderboard.m_SteamLeaderboard}).");

            if (_pendingScore >= 0)
            {
                UploadScore(_pendingScore);
                _pendingScore = -1;
            }
        }

        public static void SubmitRunScore(int score)
        {
            if (score <= 0) return;
            try
            {
                if (!SteamManager.Initialized) return;
                if (_state == State.Ready)
                    UploadScore(score);
                else
                    _pendingScore = score;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SteamLeaderboard] SubmitRunScore failed: {ex.Message}");
            }
        }

        private static void UploadScore(int score)
        {
            SteamUserStats.UploadLeaderboardScore(
                _leaderboard,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score, null, 0);
            Debug.Log($"[SteamLeaderboard] Submitted score {score} to {LeaderboardName}.");
        }
#else
        // Stub when Steamworks.NET is not imported — project still compiles.
        public static void Initialize() { }
        public static void SubmitRunScore(int score) { }
#endif
    }
}
