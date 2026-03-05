using System;
using System.IO;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Save
{
    public sealed class SaveFileService
    {
        public const string CurrentSaveVersion = "1.0.0";
        private const int MaxBackupSnapshots = 5;
        private readonly SaveMigrationService _migrationService = new();

        public string ProfilePath => Path.Combine(Application.persistentDataPath, "profile_save.json");
        public string RunPath => Path.Combine(Application.persistentDataPath, "run_save.json");

        public void SaveProfile(SaveFileEnvelope envelope)
        {
            Save(ProfilePath, envelope);
        }

        public void SaveRun(SaveFileEnvelope envelope)
        {
            Save(RunPath, envelope);
        }

        public bool TryLoadProfile(out SaveFileEnvelope envelope)
        {
            return TryLoad(ProfilePath, out envelope);
        }

        public bool TryLoadRun(out SaveFileEnvelope envelope)
        {
            return TryLoad(RunPath, out envelope);
        }

        public bool DeleteRunSave()
        {
            if (!File.Exists(RunPath))
            {
                return false;
            }

            File.Delete(RunPath);
            return true;
        }

        public bool DeleteProfileSave()
        {
            if (!File.Exists(ProfilePath))
            {
                return false;
            }

            File.Delete(ProfilePath);
            return true;
        }

        public bool TryRestoreLatestRunBackup()
        {
            return TryRestoreLatestBackup(RunPath);
        }

        public bool TryRestoreLatestProfileBackup()
        {
            return TryRestoreLatestBackup(ProfilePath);
        }

        private static bool IsRunPath(string path)
        {
            return string.Equals(Path.GetFileName(path), "run_save.json", StringComparison.OrdinalIgnoreCase);
        }

        private void Save(string path, SaveFileEnvelope envelope)
        {
            envelope ??= new SaveFileEnvelope();
            envelope.SaveVersion = CurrentSaveVersion;
            EnsureProfileDefaults(envelope);

            var json = JsonUtility.ToJson(envelope, true);

            if (File.Exists(path))
            {
                BackupFile(path);
            }

            WriteAtomically(path, json);
        }

        private bool TryLoad(string path, out SaveFileEnvelope envelope)
        {
            envelope = null;
            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                BackupFile(path);
                return false;
            }

            var parsed = JsonUtility.FromJson<SaveFileEnvelope>(json);
            if (parsed == null)
            {
                BackupFile(path);
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

            if (!ValidateEnvelope(migrated, IsRunPath(path), out var validated))
            {
                BackupFile(path);
                return false;
            }

            envelope = validated;
            return true;
        }

        private static bool ValidateEnvelope(SaveFileEnvelope source, bool requireRunSnapshot, out SaveFileEnvelope validated)
        {
            validated = source;
            if (validated == null)
            {
                return false;
            }

            EnsureProfileDefaults(validated);

            SanitizeProfileAndMeta(validated);

            if (validated.ActiveRunState != null)
            {
                SanitizeRunState(validated.ActiveRunState);
            }

            if (validated.ActivePuzzle != null &&
                !ValidateAndSanitizePuzzleState(validated.ActivePuzzle))
            {
                if (requireRunSnapshot)
                {
                    return false;
                }

                validated.ActivePuzzle = null;
            }

            if (requireRunSnapshot && validated.ActiveRunState == null)
            {
                return false;
            }

            return true;
        }

        private static void EnsureProfileDefaults(SaveFileEnvelope envelope)
        {
            if (envelope.PlayerProfile == null)
            {
                envelope.PlayerProfile = new ProfileSaveData();
            }

            if (envelope.PlayerProfile.Options == null)
            {
                envelope.PlayerProfile.Options = new OptionsState();
            }

            if (envelope.MetaProgress == null)
            {
                envelope.MetaProgress = new MetaProgressionState();
            }

            if (envelope.TutorialProgress == null)
            {
                envelope.TutorialProgress = new TutorialProgressState();
            }

            if (envelope.Statistics == null)
            {
                envelope.Statistics = new ProfileStats();
            }

            if (envelope.Mastery == null)
            {
                envelope.Mastery = new MasteryAchievementState();
            }

            if (envelope.Completion == null)
            {
                envelope.Completion = new CompletionTrackerState();
            }

            if (string.IsNullOrWhiteSpace(envelope.SaveVersion))
            {
                envelope.SaveVersion = CurrentSaveVersion;
            }
        }

        private static void SanitizeProfileAndMeta(SaveFileEnvelope envelope)
        {
            var options = envelope.PlayerProfile.Options;
            options.Audio.MasterVolume = Mathf.Clamp01(options.Audio.MasterVolume);
            options.Audio.MusicVolume = Mathf.Clamp01(options.Audio.MusicVolume);
            options.Audio.SfxVolume = Mathf.Clamp01(options.Audio.SfxVolume);
            options.Audio.UiVolume = Mathf.Clamp01(options.Audio.UiVolume);

            options.Graphics.Width = Math.Max(640, options.Graphics.Width);
            options.Graphics.Height = Math.Max(360, options.Graphics.Height);
            options.Graphics.FrameLimit = Math.Max(30, options.Graphics.FrameLimit);
            options.Graphics.UiScale = Mathf.Clamp(options.Graphics.UiScale, 0.5f, 2f);
            options.Graphics.ParticleIntensity = Mathf.Clamp(options.Graphics.ParticleIntensity, 0f, 2f);

            options.Accessibility.FontScale = Mathf.Clamp(options.Accessibility.FontScale, 0.75f, 2f);

            envelope.MetaProgress.GardenEssence = Math.Max(0, envelope.MetaProgress.GardenEssence);
            envelope.MetaProgress.MaxStarCap = Mathf.Clamp(envelope.MetaProgress.MaxStarCap, 1, 10);
            envelope.MetaProgress.AscensionLevel = Math.Max(0, envelope.MetaProgress.AscensionLevel);
            envelope.MetaProgress.PrestigeCount = Math.Max(0, envelope.MetaProgress.PrestigeCount);
            envelope.MetaProgress.GardenProgression ??= new GardenClassProgressionState();
            envelope.MetaProgress.GardenProgression.CurrentLevel = Mathf.Clamp(envelope.MetaProgress.GardenProgression.CurrentLevel, 1, 40);
            envelope.MetaProgress.GardenProgression.CurrentXp = Math.Max(0, envelope.MetaProgress.GardenProgression.CurrentXp);
            envelope.MetaProgress.GardenProgression.PrestigeTier = Mathf.Clamp(envelope.MetaProgress.GardenProgression.PrestigeTier, 0, 9);
            envelope.MetaProgress.GardenProgression.PassiveTier = Math.Max(0, envelope.MetaProgress.GardenProgression.PassiveTier);
            envelope.MetaProgress.GardenProgression.TotalXpEarned = Math.Max(0, envelope.MetaProgress.GardenProgression.TotalXpEarned);
            envelope.MetaProgress.GardenProgression.ArchiveRunCount = Math.Max(0, envelope.MetaProgress.GardenProgression.ArchiveRunCount);
            envelope.MetaProgress.GardenProgression.ArchiveSeedsBloomed = Math.Max(0, envelope.MetaProgress.GardenProgression.ArchiveSeedsBloomed);
            envelope.MetaProgress.GardenProgression.ArchiveBossesDefeated = Math.Max(0, envelope.MetaProgress.GardenProgression.ArchiveBossesDefeated);
            envelope.MetaProgress.GardenProgression.ArchivePerfectRuns = Math.Max(0, envelope.MetaProgress.GardenProgression.ArchivePerfectRuns);

            for (var i = 0; i < envelope.MetaProgress.GardenProgression.ClassEntries.Count; i++)
            {
                var entry = envelope.MetaProgress.GardenProgression.ClassEntries[i];
                entry.Level = Mathf.Clamp(entry.Level, 1, 40);
                entry.CurrentXp = Math.Max(0, entry.CurrentXp);
                entry.PrestigeTier = Mathf.Clamp(entry.PrestigeTier, 0, 9);
                entry.TotalXpEarned = Math.Max(0, entry.TotalXpEarned);
            }

            if (envelope.MetaProgress.UnlockedClasses.Count == 0)
            {
                envelope.MetaProgress.UnlockedClasses.Add(ClassId.NumberFreak);
            }
        }

        private static void SanitizeRunState(RunState run)
        {
            run.Seed = Math.Max(0, run.Seed);
            run.Depth = Math.Max(0, run.Depth);
            run.CurrentNodeIndex = Math.Max(0, run.CurrentNodeIndex);

            if (!Enum.IsDefined(typeof(ClassId), run.ClassId))
            {
                run.ClassId = ClassId.NumberFreak;
            }

            if (!Enum.IsDefined(typeof(GameMode), run.Mode))
            {
                run.Mode = GameMode.GardenRun;
            }

            run.MaxHP = Mathf.Clamp(run.MaxHP, 1, 99);
            run.CurrentHP = Mathf.Clamp(run.CurrentHP, 0, run.MaxHP);
            run.MaxPencil = Mathf.Clamp(run.MaxPencil, 1, 99);
            run.CurrentPencil = Mathf.Clamp(run.CurrentPencil, 0, run.MaxPencil);
            run.CurrentGold = Math.Max(0, run.CurrentGold);
            run.Level = Math.Max(1, run.Level);
            run.CurrentXP = Math.Max(0, run.CurrentXP);
            run.RerollTokens = Math.Max(0, run.RerollTokens);
            run.ItemSlots = Mathf.Clamp(run.ItemSlots, 1, 10);

            run.CurrentHeatScore = Mathf.Clamp(run.CurrentHeatScore, 0f, 999f);
            run.PeakHeatScore = Mathf.Clamp(run.PeakHeatScore, 0f, 999f);
            if (run.PeakHeatScore < run.CurrentHeatScore)
            {
                run.PeakHeatScore = run.CurrentHeatScore;
            }

            run.GlobalGoldMultiplier = Mathf.Clamp(run.GlobalGoldMultiplier, 0.1f, 10f);
            run.MistakeShieldCharges = Math.Max(0, run.MistakeShieldCharges);
            run.ComboMistakeProtectionCharges = Math.Max(0, run.ComboMistakeProtectionCharges);
            run.MutationNodesRemaining = Math.Max(0, run.MutationNodesRemaining);
        }

        private static bool ValidateAndSanitizePuzzleState(PuzzleSaveState puzzle)
        {
            if (puzzle == null)
            {
                return false;
            }

            var boardSize = IsSupportedBoardSize(puzzle.BoardSize)
                ? puzzle.BoardSize
                : 9;

            puzzle.BoardSize = boardSize;
            var expected = boardSize * boardSize;

            if (puzzle.SolutionFlat == null || puzzle.CellsFlat == null || puzzle.GivenFlat == null)
            {
                return false;
            }

            if (puzzle.SolutionFlat.Length != expected ||
                puzzle.CellsFlat.Length != expected ||
                puzzle.GivenFlat.Length != expected)
            {
                return false;
            }

            if (puzzle.RegionMapFlat != null && puzzle.RegionMapFlat.Length != expected)
            {
                return false;
            }

            if (puzzle.PencilSerializedPerCell == null || puzzle.PencilSerializedPerCell.Length != expected)
            {
                puzzle.PencilSerializedPerCell = new string[expected];
            }

            puzzle.CurrentHP = Mathf.Clamp(puzzle.CurrentHP, 0, 99);
            puzzle.CurrentPencil = Mathf.Clamp(puzzle.CurrentPencil, 0, 99);
            puzzle.CurrentGold = Math.Max(0, puzzle.CurrentGold);
            puzzle.ComboStreak = Math.Max(0, puzzle.ComboStreak);
            puzzle.PeakCombo = Math.Max(puzzle.ComboStreak, puzzle.PeakCombo);
            puzzle.Mistakes = Math.Max(0, puzzle.Mistakes);
            puzzle.CorrectPlacements = Mathf.Clamp(puzzle.CorrectPlacements, 0, expected);
            puzzle.Stars = Mathf.Clamp(puzzle.Stars, 1, 5);
            puzzle.Difficulty = Mathf.Clamp(puzzle.Difficulty, (int)DifficultyTier.Diff1, (int)DifficultyTier.Diff5);

            return true;
        }

        private static bool IsSupportedBoardSize(int boardSize)
        {
            return boardSize == 4 || boardSize == 5 || boardSize == 6 || boardSize == 8 || boardSize == 9;
        }

        private static void WriteAtomically(string path, string json)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        private static void BackupFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var backupPath = path + ".backup_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            File.Copy(path, backupPath, overwrite: true);
            RotateBackups(path, MaxBackupSnapshots);
        }

        private static void RotateBackups(string path, int keep)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var pattern = Path.GetFileName(path) + ".backup_*";
            var backups = Directory.GetFiles(directory, pattern);
            if (backups.Length <= keep)
            {
                return;
            }

            Array.Sort(backups, (a, b) => File.GetCreationTimeUtc(b).CompareTo(File.GetCreationTimeUtc(a)));
            for (var i = keep; i < backups.Length; i++)
            {
                File.Delete(backups[i]);
            }
        }

        private static bool TryRestoreLatestBackup(string path)
        {
            var latestBackup = GetLatestBackupPath(path);
            if (string.IsNullOrWhiteSpace(latestBackup) || !File.Exists(latestBackup))
            {
                return false;
            }

            try
            {
                File.Copy(latestBackup, path, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetLatestBackupPath(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return string.Empty;
            }

            var pattern = Path.GetFileName(path) + ".backup_*";
            var backups = Directory.GetFiles(directory, pattern);
            if (backups.Length == 0)
            {
                return string.Empty;
            }

            Array.Sort(backups, (a, b) => File.GetCreationTimeUtc(b).CompareTo(File.GetCreationTimeUtc(a)));
            return backups[0];
        }
    }
}
