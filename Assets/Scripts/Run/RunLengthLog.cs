using System;
using System.Globalization;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// [REQ: RUN-LENGTH-001] Measures the runs for the one-more-run concept §6, on this PC only (no telemetry): how long a
    /// run was open in the game, how long until its first path choice, and how long the player took to start it after the
    /// previous run ended in the same session. Each finished run adds one <see cref="RunHistoryEntry"/> to the save (the
    /// last <see cref="MaxEntries"/>); `tools/playtest/run_length_report.py` reads them.
    /// </summary>
    public static class RunLengthLog
    {
        public const int MaxEntries = 200;
        private const float MaxFrameSeconds = 0.5f;   // a frame after a hitch or a sleeping PC counts this much at most

        /// <summary>One id per game start: runs with the same id were played in one session.</summary>
        public static readonly string SessionId = Guid.NewGuid().ToString("N").Substring(0, 12);

        private static float _lastRunEnd = -1f;

        /// <summary>A new run starts: its clock at zero and the gap since the previous run of this session.</summary>
        public static void OnRunStarted(RunState state, float realtimeNow)
        {
            if (state == null) return;
            state.ActiveSeconds = 0f;
            state.SecondsToFirstChoice = -1f;
            state.SecondsSincePreviousRun = _lastRunEnd >= 0f ? Mathf.Max(0f, realtimeNow - _lastRunEnd) : -1f;
        }

        /// <summary>Called every frame while the run is on screen and not over.</summary>
        public static void Tick(RunState state, float unscaledDeltaTime)
        {
            if (state == null) return;
            state.ActiveSeconds += Mathf.Clamp(unscaledDeltaTime, 0f, MaxFrameSeconds);
        }

        /// <summary>The first path choice of the run stamps its time.</summary>
        public static void OnPathChoice(RunState state)
        {
            if (state != null && state.SecondsToFirstChoice < 0f) state.SecondsToFirstChoice = state.ActiveSeconds;
        }

        public static RunHistoryEntry Entry(RunState state, bool victory, int floorReached, DateTime utcNow) => new RunHistoryEntry
        {
            EndedUtc = utcNow.ToString("o", CultureInfo.InvariantCulture),
            SessionId = SessionId,
            Mode = state.IsSeasonalChallenge ? GameMode.SeasonalChallenge : state.Mode,
            ClassId = state.ClassId,
            HarmonyLevel = state.HarmonyLevel,
            Victory = victory,
            FloorReached = floorReached,
            ActiveSeconds = Mathf.RoundToInt(state.ActiveSeconds),
            PuzzleSeconds = Mathf.RoundToInt(state.TotalRunSeconds),
            SecondsToFirstChoice = state.SecondsToFirstChoice < 0f ? -1 : Mathf.RoundToInt(state.SecondsToFirstChoice),
            SecondsSincePreviousRun = state.SecondsSincePreviousRun < 0f ? -1 : Mathf.RoundToInt(state.SecondsSincePreviousRun)
        };

        /// <summary>Adds the entry and drops the oldest beyond <see cref="MaxEntries"/>.</summary>
        public static void Append(SaveFileEnvelope envelope, RunHistoryEntry entry)
        {
            if (envelope == null || entry == null) return;
            envelope.RunHistory ??= new System.Collections.Generic.List<RunHistoryEntry>();
            envelope.RunHistory.Add(entry);
            var extra = envelope.RunHistory.Count - MaxEntries;
            if (extra > 0) envelope.RunHistory.RemoveRange(0, extra);
        }

        /// <summary>Records a finished run once (tutorial runs are not counted) and starts the clock for the gap to the next.</summary>
        public static void Record(SaveFileService save, RunState state, bool victory, int floorReached, float realtimeNow)
        {
            if (save == null || state == null || state.TutorialMode || state.RunLengthRecorded) return;
            state.RunLengthRecorded = true;
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            Append(envelope, Entry(state, victory, floorReached, DateTime.UtcNow));
            save.Save(envelope);
            _lastRunEnd = realtimeNow;
        }

        /// <summary>"41 min" or "1 h 05 min", in the game's language.</summary>
        public static string FormatDuration(float seconds)
        {
            var minutes = Mathf.Max(0, Mathf.RoundToInt(seconds / 60f));
            return minutes < 60
                ? LocalizationService.Format("Time.Minutes", "{0} min", minutes)
                : LocalizationService.Format("Time.HoursMinutes", "{0} h {1:D2} min", minutes / 60, minutes % 60);
        }
    }
}
