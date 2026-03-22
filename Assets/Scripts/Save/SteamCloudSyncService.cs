using System;
using System.IO;
using UnityEngine;

namespace SudokuRoguelike.Save
{
    /// <summary>
    /// Steam Cloud sync service using Steamworks SteamRemoteStorage API.
    /// Implements ICloudSaveProvider for profile and run save sync.
    /// Deferred to near-release — requires Steamworks.NET integration.
    /// </summary>
    public sealed class SteamCloudSyncService : ICloudSaveProvider
    {
        private const string CloudProfileKey = "profile_save.json";
        private const string CloudRunKey = "run_save.json";
        private const string SyncMetadataFile = "sync_metadata.json";

        private readonly string _localSyncMetaPath;

        public SteamCloudSyncService()
        {
            _localSyncMetaPath = Path.Combine(Application.persistentDataPath, SyncMetadataFile);
        }

        public bool IsAvailable
        {
            get
            {
                // TODO: Return SteamManager.Initialized && SteamRemoteStorage.IsCloudEnabledForAccount()
                return false;
            }
        }

        public bool TryLoadProfile(out string json, out long timestampUtc)
        {
            return TryReadCloudFile(CloudProfileKey, out json, out timestampUtc);
        }

        public bool TryLoadRun(out string json, out long timestampUtc)
        {
            return TryReadCloudFile(CloudRunKey, out json, out timestampUtc);
        }

        public void SaveProfile(string json, long timestampUtc)
        {
            WriteCloudFile(CloudProfileKey, json, timestampUtc);
        }

        public void SaveRun(string json, long timestampUtc)
        {
            WriteCloudFile(CloudRunKey, json, timestampUtc);
        }

        /// <summary>
        /// Full sync cycle on game launch:
        /// 1. Compare local vs cloud timestamps
        /// 2. Upload/download as needed
        /// 3. Show conflict dialog if both modified
        /// </summary>
        public SyncResult SyncOnLaunch(SaveFileService localSave)
        {
            if (!IsAvailable)
                return new SyncResult { Status = SyncStatus.CloudUnavailable };

            var hasLocalProfile = File.Exists(localSave.ProfilePath);
            var hasCloudProfile = TryLoadProfile(out var cloudProfileJson, out var cloudProfileTime);

            if (!hasLocalProfile && !hasCloudProfile)
                return new SyncResult { Status = SyncStatus.NoSaveFound };

            if (hasLocalProfile && !hasCloudProfile)
            {
                UploadLocalToCloud(localSave);
                return new SyncResult { Status = SyncStatus.Uploaded };
            }

            if (!hasLocalProfile && hasCloudProfile)
            {
                DownloadCloudToLocal(localSave, cloudProfileJson);
                return new SyncResult { Status = SyncStatus.Downloaded };
            }

            // Both exist — compare timestamps
            var localTime = new DateTimeOffset(File.GetLastWriteTimeUtc(localSave.ProfilePath)).ToUnixTimeSeconds();
            if (localTime == cloudProfileTime)
                return new SyncResult { Status = SyncStatus.InSync };

            if (localTime > cloudProfileTime)
            {
                UploadLocalToCloud(localSave);
                return new SyncResult { Status = SyncStatus.Uploaded };
            }

            if (cloudProfileTime > localTime)
            {
                DownloadCloudToLocal(localSave, cloudProfileJson);
                return new SyncResult { Status = SyncStatus.Downloaded };
            }

            // Conflict — both modified since last sync
            return new SyncResult
            {
                Status = SyncStatus.Conflict,
                LocalTimestamp = localTime,
                CloudTimestamp = cloudProfileTime
            };
        }

        private void UploadLocalToCloud(SaveFileService localSave)
        {
            if (File.Exists(localSave.ProfilePath))
            {
                var json = File.ReadAllText(localSave.ProfilePath);
                var ts = new DateTimeOffset(File.GetLastWriteTimeUtc(localSave.ProfilePath)).ToUnixTimeSeconds();
                SaveProfile(json, ts);
            }

            if (File.Exists(localSave.RunPath))
            {
                var json = File.ReadAllText(localSave.RunPath);
                var ts = new DateTimeOffset(File.GetLastWriteTimeUtc(localSave.RunPath)).ToUnixTimeSeconds();
                SaveRun(json, ts);
            }

            UpdateSyncMetadata();
        }

        private void DownloadCloudToLocal(SaveFileService localSave, string profileJson)
        {
            if (!string.IsNullOrEmpty(profileJson))
            {
                File.WriteAllText(localSave.ProfilePath, profileJson);
            }

            if (TryLoadRun(out var runJson, out _) && !string.IsNullOrEmpty(runJson))
            {
                File.WriteAllText(localSave.RunPath, runJson);
            }

            UpdateSyncMetadata();
        }

        private bool TryReadCloudFile(string key, out string json, out long timestampUtc)
        {
            json = null;
            timestampUtc = 0;

            // TODO: Implement using SteamRemoteStorage.FileRead()
            // if (!SteamRemoteStorage.FileExists(key)) return false;
            // var bytes = new byte[SteamRemoteStorage.GetFileSize(key)];
            // SteamRemoteStorage.FileRead(key, bytes, bytes.Length);
            // json = System.Text.Encoding.UTF8.GetString(bytes);
            // timestampUtc = SteamRemoteStorage.GetFileTimestamp(key);
            // return true;

            return false;
        }

        private void WriteCloudFile(string key, string json, long timestampUtc)
        {
            // TODO: Implement using SteamRemoteStorage.FileWrite()
            // var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            // SteamRemoteStorage.FileWrite(key, bytes, bytes.Length);

            Debug.Log($"[SteamCloudSync] Would upload {key} ({json.Length} bytes) at {timestampUtc}");
        }

        private void UpdateSyncMetadata()
        {
            var meta = $"{{\"lastSync\":{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}}}";
            try
            {
                File.WriteAllText(_localSyncMetaPath, meta);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SteamCloudSync] Failed to update sync metadata: {e.Message}");
            }
        }
    }

    public enum SyncStatus
    {
        InSync,
        Uploaded,
        Downloaded,
        Conflict,
        CloudUnavailable,
        NoSaveFound
    }

    public sealed class SyncResult
    {
        public SyncStatus Status;
        public long LocalTimestamp;
        public long CloudTimestamp;
    }
}
