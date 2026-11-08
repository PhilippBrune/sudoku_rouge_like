using System;
using System.IO;
using UnityEngine;

namespace SudokuRoguelike.Platform
{
    /// <summary>
    /// [REQ: CRASH-REPORT-001] Notices a game that did not close properly: a marker file is written at the start and removed on a
    /// clean quit. The next start finds it and offers a report; nothing is sent without the player's press on Send (FeedbackScreen).
    /// </summary>
    public static class SessionGuard
    {
        private static string MarkerPath => Path.Combine(Application.persistentDataPath, "session.running");

        public static bool LastSessionCrashed { get; private set; }

        /// <summary>The log of the session before this one, which Unity keeps beside Player.log.</summary>
        public static string PreviousLogPath
        {
            get
            {
                var folder = Path.GetDirectoryName(Application.consoleLogPath ?? string.Empty);
                return string.IsNullOrEmpty(folder) ? string.Empty : Path.Combine(folder, "Player-prev.log");
            }
        }

        public static void Begin()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            try
            {
                LastSessionCrashed = File.Exists(MarkerPath);
                File.WriteAllText(MarkerPath, DateTime.UtcNow.ToString("o"));
            }
            catch (IOException e) { Debug.LogWarning("[Session] " + e.Message); }
            catch (UnauthorizedAccessException e) { Debug.LogWarning("[Session] " + e.Message); }
        }

        public static void End()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            try { File.Delete(MarkerPath); }
            catch (IOException e) { Debug.LogWarning("[Session] " + e.Message); }
            catch (UnauthorizedAccessException e) { Debug.LogWarning("[Session] " + e.Message); }
        }
    }
}
