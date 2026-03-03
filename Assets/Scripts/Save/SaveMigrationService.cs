using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Save
{
    public sealed class SaveMigrationService
    {
        public bool TryMigrate(SaveFileEnvelope source, string targetVersion, out SaveFileEnvelope migrated, out bool requiresBackup)
        {
            migrated = source;
            requiresBackup = false;

            if (string.IsNullOrWhiteSpace(source.SaveVersion))
            {
                source.SaveVersion = "1.0.0";
                requiresBackup = true;
                return true;
            }

            if (!Version.TryParse(source.SaveVersion, out var current))
            {
                return false;
            }

            if (!Version.TryParse(targetVersion, out var target))
            {
                return false;
            }

            if (current.Major > target.Major)
            {
                return false;
            }

            if (current.Major < target.Major)
            {
                return false;
            }

            if (current.Minor != target.Minor)
            {
                ApplyMinorMigration(source, current, target);
                requiresBackup = true;
            }

            source.SaveVersion = targetVersion;
            migrated = source;
            return true;
        }

        private static void ApplyMinorMigration(SaveFileEnvelope file, Version current, Version target)
        {
            if (file.PlayerProfile == null)
            {
                file.PlayerProfile = new ProfileSaveData();
            }

            if (file.MetaProgress == null)
            {
                file.MetaProgress = new MetaProgressionState();
            }

            if (file.TutorialProgress == null)
            {
                file.TutorialProgress = new TutorialProgressState();
            }

            if (file.Statistics == null)
            {
                file.Statistics = new ProfileStats();
            }

            if (file.Mastery == null)
            {
                file.Mastery = new MasteryAchievementState();
            }

            if (file.Completion == null)
            {
                file.Completion = new CompletionTrackerState();
            }

        }
    }
}
