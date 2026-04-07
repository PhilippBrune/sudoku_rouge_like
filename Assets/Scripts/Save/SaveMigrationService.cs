using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Save
{
    public static class SaveMigrationService
    {
        private const string CurrentVersion = "1.0.0";

        public static SaveFileEnvelope MigrateIfNeeded(SaveFileEnvelope envelope)
        {
            if (envelope == null) return new SaveFileEnvelope();

            if (string.IsNullOrEmpty(envelope.SaveVersion))
            {
                envelope.SaveVersion = CurrentVersion;
                Debug.Log("[SaveMigration] Assigned initial version.");
            }

            return envelope;
        }
    }
}
