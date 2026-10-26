using System;
using System.Collections.Generic;
using UnityEngine;
#if STEAMWORKS_NET
using Steamworks;
#endif

namespace SudokuRoguelike.Meta
{
    /// <summary>Where the player stands on a board: the top three, the own place and the place among friends.</summary>
    public sealed class BoardStanding
    {
        public int Entries;      // players on the board
        public int Rank;         // the player's place, 0 when not on the board
        public int FriendRank;   // the place among friends (the player counted), 0 when not on the board
        public int Friends;      // friends on the board, the player counted
        public readonly List<(string Name, int Score)> Top = new List<(string, int)>();
    }

    // [REQ: DAILY-WALK-008] [REQ: INCENSE-BOARD-001] Boards by name (one per day, one per week): found or created, the
    // score uploaded (the best one kept), then the standing read back. Without Steam the callback gets null.
    public static partial class SteamLeaderboardService
    {
        public const int TopEntries = 3;
        private const int MaxNameLength = 20;

        /// <summary>The day's walk board, "DailyWalk_2026-10-26".</summary>
        public static string DailyWalkBoard(DateTime day) => "DailyWalk_" + day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>A player name from Steam as plain text for a Text with rich text: no tags, at most 20 characters.</summary>
        public static string PlainName(string name)
        {
            var plain = (name ?? string.Empty).Replace('<', '‹').Replace('>', '›').Trim();
            if (plain.Length <= MaxNameLength) return plain;
            var cut = MaxNameLength - 1;
            if (char.IsHighSurrogate(plain[cut - 1])) cut--;   // never half an emoji
            return plain.Substring(0, cut) + "…";
        }

#if STEAMWORKS_NET
        // One request: keeps its call results alive until the standing is back.
        private sealed class BoardRequest
        {
            public string Name;
            public int Score;
            public Action<BoardStanding> Done;
            public SteamLeaderboard_t Board;
            public readonly BoardStanding Standing = new BoardStanding();
            public CallResult<LeaderboardFindResult_t> Find;
            public CallResult<LeaderboardScoreUploaded_t> Upload;
            public CallResult<LeaderboardScoresDownloaded_t> Download;
        }

        private static readonly List<BoardRequest> Requests = new List<BoardRequest>();
        // A read (score 0) answered within a minute is answered again from here: menus refresh often.
        private const float ReadCacheSeconds = 60f;
        private static readonly Dictionary<string, (float At, BoardStanding Standing)> ReadCache =
            new Dictionary<string, (float, BoardStanding)>();

        /// <summary>Uploads the score (0: only reads) and calls back with the standing, or null without Steam.</summary>
        public static void SubmitToBoard(string board, int score, Action<BoardStanding> done)
        {
            if (!SudokuRoguelike.Platform.PlatformServiceRegistry.SteamworksCallsEnabled || !SteamManager.Initialized)
            {
                done?.Invoke(null);
                return;
            }
            if (score <= 0 && ReadCache.TryGetValue(board, out var cached) && Time.realtimeSinceStartup - cached.At < ReadCacheSeconds)
            {
                done?.Invoke(cached.Standing);
                return;
            }
            var request = new BoardRequest { Name = board, Score = score, Done = done };
            try
            {
                request.Find = CallResult<LeaderboardFindResult_t>.Create((found, failed) => Guard(request, () => OnBoardFound(request, found, failed)));
                request.Find.Set(SteamUserStats.FindOrCreateLeaderboard(board,
                    ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric));
                Requests.Add(request);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SteamLeaderboard] {board}: {ex.Message}");
                done?.Invoke(null);
            }
        }

        private static void OnBoardFound(BoardRequest request, LeaderboardFindResult_t found, bool failed)
        {
            if (failed || found.m_bLeaderboardFound == 0) { Finish(request, false); return; }
            request.Board = found.m_hSteamLeaderboard;
            request.Standing.Entries = SteamUserStats.GetLeaderboardEntryCount(request.Board);
            if (request.Score <= 0) { DownloadTop(request); return; }
            request.Upload = CallResult<LeaderboardScoreUploaded_t>.Create((_, uploadFailed) => Guard(request, () =>
            {
                if (uploadFailed) { Finish(request, false); return; }
                request.Standing.Entries = SteamUserStats.GetLeaderboardEntryCount(request.Board);
                DownloadTop(request);
            }));
            request.Upload.Set(SteamUserStats.UploadLeaderboardScore(request.Board,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, request.Score, null, 0));
        }

        // The top three, then the player's own place, then the friends: three downloads, one after the other.
        private static void DownloadTop(BoardRequest request) =>
            Download(request, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 1, TopEntries, entries =>
            {
                foreach (var entry in entries)
                    request.Standing.Top.Add((PlainName(SteamFriends.GetFriendPersonaName(entry.m_steamIDUser)), entry.m_nScore));
                Download(request, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0, own =>
                {
                    if (own.Count > 0) request.Standing.Rank = own[0].m_nGlobalRank;
                    Download(request, ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends, 0, 0, friends =>
                    {
                        var me = SteamUser.GetSteamID();
                        request.Standing.Friends = friends.Count;
                        request.Standing.FriendRank = friends.FindIndex(e => e.m_steamIDUser == me) + 1;
                        Finish(request, true);
                    });
                });
            });

        private static void Download(BoardRequest request, ELeaderboardDataRequest kind, int start, int end,
            Action<List<LeaderboardEntry_t>> next)
        {
            request.Download = CallResult<LeaderboardScoresDownloaded_t>.Create((downloaded, failed) => Guard(request, () =>
            {
                if (failed) { Finish(request, false); return; }
                var entries = new List<LeaderboardEntry_t>();
                for (var i = 0; i < downloaded.m_cEntryCount; i++)
                    if (SteamUserStats.GetDownloadedLeaderboardEntry(downloaded.m_hSteamLeaderboardEntries, i, out var entry, null, 0))
                        entries.Add(entry);
                next(entries);
            }));
            request.Download.Set(SteamUserStats.DownloadLeaderboardEntries(request.Board, kind, start, end));
        }

        // A Steam call that throws ends its request, so the board never hangs on "Reading".
        private static void Guard(BoardRequest request, Action step)
        {
            try { step(); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SteamLeaderboard] {request.Name}: {ex.Message}");
                Finish(request, false);
            }
        }

        private static void Finish(BoardRequest request, bool ok)
        {
            if (!Requests.Remove(request)) return;   // answered once
            if (ok) ReadCache[request.Name] = (Time.realtimeSinceStartup, request.Standing);
            try { request.Done?.Invoke(ok ? request.Standing : null); }
            catch (Exception ex) { Debug.LogWarning($"[SteamLeaderboard] board callback failed: {ex.Message}"); }
        }
#else
        public static void SubmitToBoard(string board, int score, Action<BoardStanding> done) => done?.Invoke(null);
#endif
    }
}
