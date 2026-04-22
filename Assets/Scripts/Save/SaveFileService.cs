using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Save
{
    // [REQ: SAVE-DUAL-001] Dual-file architecture: profile save + run save (both in same envelope)
    // [REQ: SAVE-SLOT-001] 3 profile slots: save_profile_0/1/2.json
    public sealed class SaveFileService
    {
        public const int MaxSlots = 3;

        private readonly int _slotIndex;

        public SaveFileService(int slotIndex = 0)
        {
            _slotIndex = Math.Max(0, Math.Min(slotIndex, MaxSlots - 1));
        }

        private string SavePath => Path.Combine(
            Application.persistentDataPath, $"save_profile_{_slotIndex}.json");

        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }

        public SaveFileEnvelope Load()
        {
            // Try primary file, then .bak fallback (crash-safe recovery)
            foreach (var path in new[] { SavePath, SavePath + ".bak" })
            {
                if (!File.Exists(path)) continue;

                // File.Replace() on Windows (via ReplaceFile()) briefly holds an exclusive
                // lock on the destination while it performs the atomic swap, which can cause a
                // sharing violation even though we open with FileShare.ReadWrite.
                // FileShare.Delete additionally permits the rename/replace to proceed while we
                // have a handle open.  The retry loop (3×5 ms) covers that tiny exclusive window.
                string json = null;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (var reader = new System.IO.StreamReader(fs, System.Text.Encoding.UTF8))
                            json = reader.ReadToEnd();
                        break; // success
                    }
                    catch (IOException) when (attempt < 2)
                    {
                        System.Threading.Thread.Sleep(5);
                    }
                    catch (Exception e)
                    {
                        // FileNotFound can occur in a rare race between Exists() and open.
                        if (e is System.IO.FileNotFoundException)
                            Debug.Log($"[SaveFileService] '{path}' not found on read — trying backup.");
                        else
                            Debug.LogWarning($"[SaveFileService] Failed to load '{path}': {e.Message} — trying backup.");
                        break;
                    }
                }

                if (json == null) continue;
                var result = JsonUtility.FromJson<SaveFileEnvelope>(json);
                if (result != null) return result;
            }
            return new SaveFileEnvelope();
        }

        // Serializes on the calling (main) thread; file I/O runs on a background thread.
        // Static lock prevents overlapping writes from rapid successive saves.
        private static readonly object _writeLock = new object();

        public void Save(SaveFileEnvelope envelope)
        {
            string json;
            try
            {
                // Compact JSON (~15% smaller, faster serialization than pretty-print).
                json = JsonUtility.ToJson(envelope, false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileService] Failed to serialize: {e.Message}");
                return;
            }

            var savePath = SavePath; // capture before leaving main thread
            Task.Run(() =>
            {
                lock (_writeLock)
                {
                    try
                    {
                        var tmpPath = savePath + ".tmp";
                        var bakPath = savePath + ".bak";
                        // Write to temp first — if the process crashes here the real save is untouched
                        File.WriteAllText(tmpPath, json);
                        // Atomic replace: promotes the temp file over the live save and keeps a .bak copy
                        if (File.Exists(savePath))
                            File.Replace(tmpPath, savePath, bakPath);
                        else
                            File.Move(tmpPath, savePath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SaveFileService] Failed to write save: {e.Message}");
                    }
                }
            });
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    File.Delete(SavePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveFileService] Failed to delete save: {e.Message}");
                }
            }
        }

        public bool HasActiveRun()
        {
            if (!HasSaveFile()) return false;
            var envelope = Load();
            // Tutorial runs are never resumable — they don't save progress
            return envelope.ActiveRunState != null && !envelope.ActiveRunState.TutorialMode;
        }
    }
}
