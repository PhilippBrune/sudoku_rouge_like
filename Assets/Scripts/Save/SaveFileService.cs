using System;
using System.IO;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Save
{
    public sealed class SaveFileService
    {
        public const string CurrentSaveVersion = "1.0.0";
        private readonly SaveMigrationService _migrationService = new();

        public string ProfilePath => Path.Combine(Application.persistentDataPath, "profile_save.json");
        public string RunPath => Path.Combine(Application.persistentDataPath, "run_save.json");

        public void SaveProfile(SaveFileEnvelope envelope)
        {
            envelope.SaveVersion = CurrentSaveVersion;
            var json = JsonUtility.ToJson(envelope, true);
            File.WriteAllText(ProfilePath, json);
        }

        public void SaveRun(SaveFileEnvelope envelope)
        {
            envelope.SaveVersion = CurrentSaveVersion;
            var json = JsonUtility.ToJson(envelope, true);
            File.WriteAllText(RunPath, json);
        }

        public bool TryLoadProfile(out SaveFileEnvelope envelope)
        {
            return TryLoad(ProfilePath, out envelope);
        }

        public bool TryLoadRun(out SaveFileEnvelope envelope)
        {
            return TryLoad(RunPath, out envelope);
        }

        private bool TryLoad(string path, out SaveFileEnvelope envelope)
        {
            envelope = null;
            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path);
            var parsed = JsonUtility.FromJson<SaveFileEnvelope>(json);
            if (parsed == null)
            {
                return false;
            }

            var loadResult = _migrationService.TryMigrate(parsed, CurrentSaveVersion, out var migrated, out var requiresBackup);
            if (!loadResult)
            {
                BackupFile(path);
                return false;
            }

            if (requiresBackup)
            {
                BackupFile(path);
            }

            envelope = migrated;
            return true;
        }

        private static void BackupFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var backupPath = path + ".backup_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            File.Copy(path, backupPath, overwrite: true);
        }
    }
}
