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
        private static readonly object _writeQueueLock = new object();
        private static Task _pendingWrite = Task.CompletedTask;

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
            WaitForPendingWrites();

            // Try primary file, then .bak fallback (crash-safe recovery)
            foreach (var path in new[] { SavePath, SavePath + ".bak" })
            {
                if (!File.Exists(path)) continue;

                // File.Replace() on Windows (via ReplaceFile()) briefly holds an exclusive
                // lock on the destination while it performs the atomic swap, which can cause a
                // sharing violation even though we open with FileShare.ReadWrite.
                // FileShare.Delete additionally permits the rename/replace to proceed while we
                // have a handle open. The retry loop (3x5 ms) covers that tiny exclusive window.
                string json = null;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (var reader = new System.IO.StreamReader(fs, System.Text.Encoding.UTF8))
                            json = reader.ReadToEnd();
                        break;
                    }
                    catch (IOException) when (attempt < 2)
                    {
                        System.Threading.Thread.Sleep(5);
                    }
                    catch (Exception e)
                    {
                        // FileNotFound can occur in a rare race between Exists() and open.
                        if (e is FileNotFoundException)
                            Debug.Log($"[SaveFileService] '{path}' not found on read - trying backup.");
                        else
                            Debug.LogWarning($"[SaveFileService] Failed to load '{path}': {e.Message} - trying backup.");
                        break;
                    }
                }

                if (json == null) continue;
                var result = JsonUtility.FromJson<SaveFileEnvelope>(json);
                if (result != null)
                {
                    SanitizeEnvelope(result);
                    return result;
                }
            }

            return new SaveFileEnvelope();
        }

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

            var savePath = SavePath;
            lock (_writeQueueLock)
            {
                _pendingWrite = _pendingWrite.ContinueWith(
                    _ => WriteEnvelope(savePath, json),
                    TaskScheduler.Default);
            }
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
            // Tutorial runs are never resumable - they do not save progress.
            return envelope.ActiveRunState != null
                && !envelope.ActiveRunState.TutorialMode
                && !envelope.ActiveRunState.DisableProgressionRewards;
        }

        // [BUG-FIX] Post-deserialization normalization: Unity's JsonUtility bypasses C# field
        // initializers, so collections that default to non-empty must be seeded explicitly here.
        private static void SanitizeEnvelope(SaveFileEnvelope envelope)
        {
            if (envelope.MetaProgress == null)
                envelope.MetaProgress = new MetaProgressionState();

            var meta = envelope.MetaProgress;
            if (meta.UnlockedClasses == null)
                meta.UnlockedClasses = new System.Collections.Generic.List<ClassId>();

            // NumberFreak is always unlocked - ensure it is present after deserialization.
            if (!meta.UnlockedClasses.Contains(ClassId.NumberFreak))
                meta.UnlockedClasses.Insert(0, ClassId.NumberFreak);

            if (meta.ClassUnlocks == null)
                meta.ClassUnlocks = new ClassUnlockProgress();

            ModifierDiscoveryService.SanitizeMetaProgress(meta);

            // [HARMONY-SAVE-001] Clamp harmony fields and ensure list is initialised.
            meta.MaxUnlockedHarmonyLevel = Math.Clamp(meta.MaxUnlockedHarmonyLevel, 0, 10);
            meta.LastSelectedHarmonyLevel = Math.Clamp(meta.LastSelectedHarmonyLevel, 0, meta.MaxUnlockedHarmonyLevel);
            if (meta.HarmonyBadgeFlags == null)
                meta.HarmonyBadgeFlags = new System.Collections.Generic.List<int>();
            if (meta.HarmonyV5PlusWins == null)
                meta.HarmonyV5PlusWins = new System.Collections.Generic.List<ClassId>();
        }

        private static void WaitForPendingWrites()
        {
            Task pending;
            lock (_writeQueueLock)
                pending = _pendingWrite;

            pending.GetAwaiter().GetResult();
        }

        private static void WriteEnvelope(string savePath, string json)
        {
            try
            {
                var tmpPath = savePath + ".tmp";
                var bakPath = savePath + ".bak";
                // Write to temp first - if the process crashes here the real save is untouched.
                File.WriteAllText(tmpPath, json);
                // Atomic replace: promotes the temp file over the live save and keeps a .bak copy.
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
    }
}
