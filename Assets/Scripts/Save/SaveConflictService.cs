using SudokuRoguelike.Core;
using UnityEngine;
using System;
using System.IO;

namespace SudokuRoguelike.Save
{
    public enum SaveConflictDecision
    {
        KeepLocal,
        KeepCloud,
        BackupBothAbort
    }

    public sealed class SaveConflictService
    {
        private readonly SaveFileService _saveFile;
        private readonly ICloudSaveProvider _cloud;

        public SaveConflictService(SaveFileService saveFile, ICloudSaveProvider cloud)
        {
            _saveFile = saveFile;
            _cloud = cloud;
        }

        public bool HasRunConflict()
        {
            var local = _saveFile.TryLoadRun(out var _);
            var cloud = _cloud.TryLoadRun(out var __, out var ___);
            return local && cloud;
        }

        public bool TryBuildRunConflictSummary(out string summary)
        {
            summary = string.Empty;

            var hasLocal = _saveFile.TryLoadRun(out var localEnvelope);
            var hasCloud = TryLoadCloudRun(out var cloudEnvelope, out var cloudTimestamp);
            if (!hasLocal || !hasCloud)
            {
                return false;
            }

            var localTimestamp = 0L;
            if (File.Exists(_saveFile.RunPath))
            {
                localTimestamp = new DateTimeOffset(File.GetLastWriteTimeUtc(_saveFile.RunPath)).ToUnixTimeSeconds();
            }

            summary = $"Local [{DescribeEnvelope(localEnvelope, localTimestamp)}] vs Cloud [{DescribeEnvelope(cloudEnvelope, cloudTimestamp)}].";
            return true;
        }

        public bool TryResolveRunConflict(SaveConflictDecision decision, out SaveFileEnvelope envelope)
        {
            envelope = null;

            var hasLocal = _saveFile.TryLoadRun(out var localEnvelope);
            var hasCloud = TryLoadCloudRun(out var cloudEnvelope, out var _);

            if (!hasLocal && !hasCloud)
            {
                return false;
            }

            if (hasLocal && !hasCloud)
            {
                envelope = localEnvelope;
                return true;
            }

            if (!hasLocal && hasCloud)
            {
                envelope = cloudEnvelope;
                _saveFile.SaveRun(envelope);
                return true;
            }

            switch (decision)
            {
                case SaveConflictDecision.KeepCloud:
                    envelope = cloudEnvelope;
                    _saveFile.SaveRun(envelope);
                    return true;
                case SaveConflictDecision.KeepLocal:
                    envelope = localEnvelope;
                    return true;
                case SaveConflictDecision.BackupBothAbort:
                default:
                    var localJson = JsonUtility.ToJson(localEnvelope, true);
                    var cloudJson = JsonUtility.ToJson(cloudEnvelope, true);
                    var now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    _cloud.SaveRun(cloudJson, now);
                    _cloud.SaveProfile(cloudJson, now);
                    return false;
            }
        }

        public void SyncRunToCloud(SaveFileEnvelope envelope)
        {
            var json = JsonUtility.ToJson(envelope, true);
            var timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _cloud.SaveRun(json, timestamp);
        }

        private static string DescribeEnvelope(SaveFileEnvelope envelope, long timestampUtc)
        {
            var mode = envelope?.ActiveRunState != null ? envelope.ActiveRunState.Mode.ToString() : "Unknown";
            var boss = envelope?.ActivePuzzle != null && envelope.ActivePuzzle.IsBoss ? "Boss" : "Run";
            var time = timestampUtc > 0
                ? DateTimeOffset.FromUnixTimeSeconds(timestampUtc).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
                : "n/a";
            return $"{boss}, mode={mode}, time={time}";
        }

        private bool TryLoadCloudRun(out SaveFileEnvelope envelope, out long timestampUtc)
        {
            envelope = null;
            timestampUtc = 0;
            if (!_cloud.TryLoadRun(out var json, out timestampUtc))
            {
                return false;
            }

            var parsed = JsonUtility.FromJson<SaveFileEnvelope>(json);
            if (parsed == null)
            {
                return false;
            }

            envelope = parsed;
            return true;
        }
    }
}
