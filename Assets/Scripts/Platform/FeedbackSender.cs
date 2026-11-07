using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SudokuRoguelike.Platform
{
    /// <summary>
    /// [REQ: FEEDBACK-002] Where a feedback report goes (the owner's pick K3): to the owner's Discord through a small
    /// Cloudflare Worker, which holds the webhook (the game never carries it, or anyone could read it from the files and
    /// flood the channel). Without the Worker's address, offline or when it fails, the report is written as a ZIP into the
    /// game's data folder for the player to post. A random ID per installation lets the Worker count reports (5 an hour).
    /// </summary>
    public static class FeedbackSender
    {
        /// <summary>The Worker's https address; empty until the owner has set it up (tools/feedback-worker/README.md).</summary>
        public const string Endpoint = "";

        public const int MaxBytes = 2 * 1024 * 1024;
        public const float SecondsBetweenSends = 60f;
        private const string InstallIdKey = "FeedbackInstallId";
        private static float _lastSentAt = -1000f;

        public sealed class Report
        {
            public string Kind, Title, Text;
            public byte[] Screenshot;   // JPEG
            public string Log, Save;
        }

        /// <summary>A random ID made once per installation (no Steam ID, no name).</summary>
        public static string InstallId
        {
            get
            {
                var id = PlayerPrefs.GetString(InstallIdKey, string.Empty);
                if (!string.IsNullOrEmpty(id)) return id;
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(InstallIdKey, id);
                PlayerPrefs.Save();
                return id;
            }
        }

        public static bool TooSoon => Time.realtimeSinceStartup - _lastSentAt < SecondsBetweenSends;

        public static string FeedbackFolder => Path.Combine(Application.persistentDataPath, "Feedback");

        /// <summary>Sends the report, or writes it as a ZIP; <paramref name="done"/> gets true when sent, else the ZIP's
        /// path (null when that failed too).</summary>
        public static IEnumerator Send(Report report, Action<bool, string> done)
        {
            _lastSentAt = Time.realtimeSinceStartup;
            FitInto(report, MaxBytes);
            if (!string.IsNullOrEmpty(Endpoint))
            {
                var form = new List<IMultipartFormSection>
                {
                    new MultipartFormDataSection("install", InstallId),
                    new MultipartFormDataSection("kind", report.Kind ?? "Bug"),
                    new MultipartFormDataSection("title", report.Title ?? string.Empty),
                    new MultipartFormFileSection("report", Encoding.UTF8.GetBytes(report.Text ?? string.Empty), "report.txt", "text/plain")
                };
                if (report.Screenshot != null) form.Add(new MultipartFormFileSection("screenshot", report.Screenshot, "screenshot.jpg", "image/jpeg"));
                if (!string.IsNullOrEmpty(report.Log)) form.Add(new MultipartFormFileSection("log", Encoding.UTF8.GetBytes(report.Log), "log.txt", "text/plain"));
                if (!string.IsNullOrEmpty(report.Save)) form.Add(new MultipartFormFileSection("save", Encoding.UTF8.GetBytes(report.Save), "save.json", "application/json"));
                using var request = UnityWebRequest.Post(Endpoint, form);
                request.timeout = 20;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    done?.Invoke(true, null);
                    yield break;
                }
                Debug.LogWarning($"[Feedback] Sending failed ({request.responseCode} {request.error}); writing a ZIP instead.");
            }
            done?.Invoke(false, WriteZip(report));
        }

        /// <summary>Keeps a report under the Worker's limit: the save goes first, then the log is cut to its end.</summary>
        public static void FitInto(Report report, int maxBytes)
        {
            int Size() => Encoding.UTF8.GetByteCount(report.Text ?? string.Empty) + (report.Screenshot?.Length ?? 0)
                          + Encoding.UTF8.GetByteCount(report.Log ?? string.Empty) + Encoding.UTF8.GetByteCount(report.Save ?? string.Empty);
            const int Margin = 16 * 1024;   // the multipart headers
            if (Size() + Margin <= maxBytes) return;
            if (report.Save != null)
            {
                report.Save = null;
                report.Text += "\n(The save file was left out: too big to send.)";
            }
            if (Size() + Margin > maxBytes && !string.IsNullOrEmpty(report.Log))
            {
                var keep = Math.Max(0, maxBytes - Margin - (Size() - Encoding.UTF8.GetByteCount(report.Log)));
                report.Log = report.Log.Length > keep ? report.Log.Substring(report.Log.Length - keep) : report.Log;
            }
        }

        /// <summary>Writes the report as a ZIP into the game's data folder; returns its path, or null.</summary>
        public static string WriteZip(Report report, string folder = null)
        {
            try
            {
                folder ??= FeedbackFolder;
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, $"feedback_{DateTime.Now:yyyyMMdd_HHmmss_fff}.zip");
                using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    Add(zip, "report.txt", Encoding.UTF8.GetBytes(report.Text ?? string.Empty));
                    if (report.Screenshot != null) Add(zip, "screenshot.jpg", report.Screenshot);
                    if (!string.IsNullOrEmpty(report.Log)) Add(zip, "log.txt", Encoding.UTF8.GetBytes(report.Log));
                    if (!string.IsNullOrEmpty(report.Save)) Add(zip, "save.json", Encoding.UTF8.GetBytes(report.Save));
                }
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Feedback] Could not write the ZIP: {e.Message}");
                return null;
            }
        }

        private static void Add(ZipArchive zip, string name, byte[] bytes)
        {
            using var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal).Open();
            entry.Write(bytes, 0, bytes.Length);
        }
    }
}
