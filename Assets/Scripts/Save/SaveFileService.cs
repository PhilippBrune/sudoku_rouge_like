using System;
using System.IO;
using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Save
{
    public sealed class SaveFileService
    {
        private const string SaveFileName = "save_data.json";

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }

        public SaveFileEnvelope Load()
        {
            if (!File.Exists(SavePath))
                return new SaveFileEnvelope();

            try
            {
                var json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<SaveFileEnvelope>(json) ?? new SaveFileEnvelope();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveFileService] Failed to load save: {e.Message}");
                return new SaveFileEnvelope();
            }
        }

        public void Save(SaveFileEnvelope envelope)
        {
            try
            {
                var json = JsonUtility.ToJson(envelope, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileService] Failed to save: {e.Message}");
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
            // Tutorial runs are never resumable — they don't save progress
            return envelope.ActiveRunState != null && !envelope.ActiveRunState.TutorialMode;
        }
    }
}
