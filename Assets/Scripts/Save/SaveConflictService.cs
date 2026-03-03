using SudokuRoguelike.Core;
using UnityEngine;

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

        public bool TryResolveRunConflict(SaveConflictDecision decision, out SaveFileEnvelope envelope)
        {
            envelope = null;

            var hasLocal = _saveFile.TryLoadRun(out var localEnvelope);
            var hasCloud = TryLoadCloudRun(out var cloudEnvelope);

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

        private bool TryLoadCloudRun(out SaveFileEnvelope envelope)
        {
            envelope = null;
            if (!_cloud.TryLoadRun(out var json, out var _))
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
