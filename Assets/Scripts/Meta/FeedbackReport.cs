using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [REQ: FEEDBACK-001] The text of a player's feedback report (the owner's pick K3 after 0.4.2): the game's version, the
    /// device, the language and the input, and for a run its mode, class, Harmony, floor, tile, run seed, board code and
    /// rules, HP, gold, pencil, items, relics and time. No name, no Steam ID; a random ID per installation only.
    /// </summary>
    public static class FeedbackReport
    {
        public const int LogLines = 200;
        public const int MaxTextLength = 2000;

        public static readonly string[] Kinds = { "Bug", "Idea", "Balance", "Text" };

        /// <summary>The report as plain text (also the "what is sent" preview).</summary>
        public static string Build(string kind, string text, string discordName, string version, string device, string language,
            bool usingPad, RunDirector run, bool includeRun)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{kind}] {version}");
            sb.AppendLine($"Device: {device} · Language: {language} · Input: {(usingPad ? "controller" : "keyboard and mouse")}");
            if (!string.IsNullOrWhiteSpace(discordName)) sb.AppendLine($"Discord: {Clean(discordName, 40)}");
            sb.AppendLine();
            sb.AppendLine(Clean(text, MaxTextLength));
            if (!includeRun || run?.State == null) return sb.ToString();

            var s = run.State;
            var config = run.CurrentLevelConfig;
            sb.AppendLine();
            sb.AppendLine("Run");
            sb.AppendLine($"Mode: {s.Mode}{(s.DailyWalk ? " (Daily Walk)" : "")} · Class: {s.ClassId} Lv {s.ClassLevel} · Harmony {s.HarmonyLevel}");
            sb.AppendLine($"Floor {s.WalkFloor} of {s.TotalFloors} (garden {s.CurrentFloor + 1}) · Tile {s.CurrentNodeIndex} · Depth {s.Depth}");
            sb.AppendLine($"Run seed: {s.Seed}");
            if (config != null) sb.AppendLine($"Board code: {BoardCode(config)}");
            if (config?.ActiveModifiers != null && config.ActiveModifiers.Count > 0)
                sb.AppendLine($"Rules: {string.Join(", ", config.ActiveModifiers)}");
            sb.AppendLine($"HP {s.CurrentHP}/{s.MaxHP} · Gold {s.CurrentGold} · Pencil {s.CurrentPencil}/{s.MaxPencil} · Run time {Minutes(s.ActiveSeconds)}");
            sb.AppendLine($"Items: {Names(s.HeldItems, i => $"{i.Type} ({i.Rarity})")}");
            sb.AppendLine($"Relics: {Names(s.HeldRelics, r => r.Id.ToString())}");
            if (s.ActiveCurseIds != null && s.ActiveCurseIds.Count > 0) sb.AppendLine($"Curses: {string.Join(", ", s.ActiveCurseIds)}");
            return sb.ToString();
        }

        /// <summary>The board as the owner can rebuild it: size, stars, regions and the level's seed.</summary>
        public static string BoardCode(LevelConfig config) =>
            $"{config.BoardSize}x{config.BoardSize}-{config.Stars}s-r{config.RegionVariant}-{config.Seed}{(config.IsBoss ? "-boss" : "")}";

        /// <summary>The title line of the report on the owner's channel.</summary>
        public static string Title(string kind, string version, RunDirector run) =>
            run?.State == null ? $"[{kind}] {version}"
                : $"[{kind}] {version} · {run.State.ClassId} · Floor {run.State.WalkFloor} · Seed {run.State.Seed}";

        /// <summary>The last <paramref name="count"/> lines of the player log, with the Windows user's folder and name
        /// replaced (the log names paths under it).</summary>
        public static string LogTail(string logPath, int count = LogLines)
        {
            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath)) return string.Empty;
            string all;
            try
            {
                using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                all = reader.ReadToEnd();
            }
            catch (IOException) { return string.Empty; }
            catch (UnauthorizedAccessException) { return string.Empty; }
            var lines = all.Replace("\r\n", "\n").Split('\n');
            var tail = string.Join("\n", lines, Math.Max(0, lines.Length - count), Math.Min(count, lines.Length));
            return Redact(tail, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.UserName);
        }

        /// <summary>Replaces the user's folder (either slash) and name with %USERPROFILE% and %USERNAME%.</summary>
        public static string Redact(string text, string userFolder, string userName)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            if (!string.IsNullOrEmpty(userFolder))
            {
                text = text.Replace(userFolder, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase)
                           .Replace(userFolder.Replace('\\', '/'), "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
            }
            if (!string.IsNullOrEmpty(userName) && userName.Length >= 3)
                text = text.Replace(userName, "%USERNAME%", StringComparison.OrdinalIgnoreCase);
            var machine = Environment.MachineName;
            if (!string.IsNullOrEmpty(machine) && machine.Length >= 3)
                text = text.Replace(machine, "%COMPUTERNAME%", StringComparison.OrdinalIgnoreCase);
            return System.Text.RegularExpressions.Regex.Replace(text, @"7656119\d{10}", "%STEAMID%");   // SteamID64
        }

        private static string Clean(string text, int max)
        {
            text = (text ?? string.Empty).Trim();
            return text.Length > max ? text.Substring(0, max) : text;
        }

        private static string Minutes(float seconds) =>
            TimeSpan.FromSeconds(Math.Max(0f, seconds)).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);

        private static string Names<T>(List<T> list, Func<T, string> name)
        {
            if (list == null || list.Count == 0) return "none";
            var names = new List<string>();
            foreach (var entry in list) if (entry != null) names.Add(name(entry));
            return string.Join(", ", names);
        }
    }
}
