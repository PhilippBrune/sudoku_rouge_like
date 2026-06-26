using System;
using System.IO;
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
        private readonly SaveWriteQueue _writeQueue;

        public SaveFileService(int slotIndex = 0, SaveWriteQueue writeQueue = null)
        {
            _slotIndex = Math.Max(0, Math.Min(slotIndex, MaxSlots - 1));
            _writeQueue = writeQueue ?? SaveWriteQueue.Shared;
        }

        private string SavePath => Path.Combine(
            Application.persistentDataPath, $"save_profile_{_slotIndex}.json");

        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }

        public SaveFileEnvelope Load()
        {
            FlushPendingWrites();

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
                    MigrateIfNeeded(result);
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
            _writeQueue.Enqueue(() => WriteEnvelope(savePath, json));
        }

        public void DeleteSaveFile()
        {
            FlushPendingWrites();

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
            return IsResumableRunState(envelope.ActiveRunState);
        }

        /// <summary>
        /// Returns true when a Seasonal Challenge puzzle was saved mid-attempt
        /// and the saved month matches the current calendar month.
        /// </summary>
        public bool HasActiveSeasonalRun(int year, int month)
        {
            if (!HasSaveFile()) return false;
            var envelope = Load();
            var saved = envelope.ActiveSeasonalRunState;
            if (!IsResumableSeasonalRunState(saved)) return false;
            // [REQ: META-ASCEND-004] [REQ: SEASON-SEED-001] [REQ: TRIAL-LEAD-002] Seasonal seed encodes year*100+month — compare to requested month
            var expectedSeed = year * 100 + month;
            return saved.Seed == expectedSeed;
        }

        public static bool IsResumableRunState(RunState runState)
        {
            // JsonUtility serializes null custom-class fields as default objects.
            // A default RunState has HP/MaxHP/RunNumber all zero and must not be offered as a resume.
            return runState != null
                && runState.Mode == GameMode.GardenRun
                && !runState.TutorialMode
                && !runState.DisableProgressionRewards
                && runState.RunNumber > 0
                && runState.TotalFloors > 0
                && runState.CurrentFloor >= 0
                && runState.CurrentFloor < runState.TotalFloors
                && runState.MaxHP > 0
                && runState.CurrentHP > 0
                && runState.MaxPencil > 0;
        }

        public static bool IsResumableSeasonalRunState(RunState runState)
        {
            return runState != null
                && runState.Mode == GameMode.SeasonalChallenge
                && runState.IsSeasonalChallenge
                && runState.MaxHP > 0
                && runState.CurrentHP > 0
                && runState.MaxPencil > 0;
        }

        public static bool IsUsablePuzzleSave(PuzzleSaveState puzzle)
        {
            if (puzzle == null || puzzle.BoardSize <= 0) return false;

            var cellCount = puzzle.BoardSize * puzzle.BoardSize;
            return puzzle.Board != null && puzzle.Board.Length == cellCount
                && puzzle.Solution != null && puzzle.Solution.Length == cellCount
                && puzzle.GivenMask != null && puzzle.GivenMask.Length == cellCount
                && puzzle.RegionMap != null && puzzle.RegionMap.Length == cellCount;
        }

        public void FlushPendingWrites()
        {
            _writeQueue.Flush();
        }

        public static void FlushSharedPendingWrites()
        {
            SaveWriteQueue.Shared.Flush();
        }

        // Current schema version written to every new save file.
        private const string CurrentVersion = "1.0.0";

        // Forward-compatible migration: fills in fields that were absent in older saves and bumps
        // the version string so the migration does not re-run on the next load.
        // Add a new 'else if' block here whenever the schema gains required fields.
        private static void MigrateIfNeeded(SaveFileEnvelope envelope)
        {
            var v = envelope.SaveVersion;

            // Nothing to migrate if the version is already current.
            if (v == CurrentVersion) return;

            Debug.LogWarning($"[SaveFileService] Migrating save from version '{(string.IsNullOrEmpty(v) ? "<none>" : v)}' → '{CurrentVersion}'.");

            // Version <none> / "" → "1.0.0"
            // These are saves written before the SaveVersion field existed.
            // SanitizeEnvelope has already seeded null collections; this block handles
            // any additional fields that must default to non-zero values on old saves.
            if (string.IsNullOrEmpty(v))
            {
                // Ensure per-class garden entries are present for every known class.
                // (Older saves may have an empty ClassEntries list.)
                if (envelope.MetaProgress?.GardenProgression?.ClassEntries != null
                    && envelope.MetaProgress.GardenProgression.ClassEntries.Count == 0)
                {
                    // Leave empty — the garden system creates entries on first use.
                    // Explicit no-op: documents intent for future migration authors.
                }

                // Harmony defaults: MaxUnlockedHarmonyLevel stays 0 (correct for an old save).
                // No additional action needed; SanitizeEnvelope already clamped the values.
            }

            // Future schema versions: add 'else if (v == "1.0.0") { ... }' blocks here.

            envelope.SaveVersion = CurrentVersion;
        }

        // [BUG-FIX] Post-deserialization normalization: Unity's JsonUtility bypasses C# field
        // initializers, so collections that default to non-empty must be seeded explicitly here.
        private static void SanitizeEnvelope(SaveFileEnvelope envelope)
        {
            if (envelope.PlayerProfile == null)
                envelope.PlayerProfile = new ProfileSaveData();

            if (envelope.PlayerProfile.Options == null)
                envelope.PlayerProfile.Options = new OptionsState();

            SanitizeOptions(envelope.PlayerProfile.Options);

            if (envelope.TutorialProgress == null)
                envelope.TutorialProgress = new TutorialProgressState();

            if (envelope.Statistics == null)
                envelope.Statistics = new ProfileStats();

            if (envelope.Mastery == null)
                envelope.Mastery = new MasteryAchievementState();

            if (envelope.Completion == null)
                envelope.Completion = new CompletionTrackerState();

            if (envelope.DailyGoals == null)
                envelope.DailyGoals = new DailyGoalState();

            if (envelope.SeasonalChallenge == null)
                envelope.SeasonalChallenge = new SeasonalChallengeState();

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

            if (!IsResumableRunState(envelope.ActiveRunState))
                envelope.ActiveRunState = null;
            if (!IsUsablePuzzleSave(envelope.ActivePuzzle))
                envelope.ActivePuzzle = null;

            if (!IsResumableSeasonalRunState(envelope.ActiveSeasonalRunState))
                envelope.ActiveSeasonalRunState = null;
            if (!IsUsablePuzzleSave(envelope.ActiveSeasonalPuzzle))
                envelope.ActiveSeasonalPuzzle = null;
        }

        private static void SanitizeOptions(OptionsState options)
        {
            if (options.Audio == null)
                options.Audio = new AudioSettingsModel();
            if (options.Graphics == null)
                options.Graphics = new GraphicsSettingsModel();
            if (options.Accessibility == null)
                options.Accessibility = new AccessibilitySettings();
            if (options.Gameplay == null)
                options.Gameplay = new GameplaySettings();
            if (options.FirstRun == null)
                options.FirstRun = new FirstRunSetupState();

            options.Graphics.Brightness = Mathf.Clamp01(options.Graphics.Brightness);
            options.Graphics.Contrast = Mathf.Clamp01(options.Graphics.Contrast);
            options.Graphics.ParticleIntensity = Mathf.Clamp01(options.Graphics.ParticleIntensity);

            if (options.Graphics.ResolutionWidth  <= 0) options.Graphics.ResolutionWidth  = 1920;
            if (options.Graphics.ResolutionHeight <= 0) options.Graphics.ResolutionHeight = 1080;

            if (!Enum.IsDefined(typeof(DisplayModeOption), options.Graphics.DisplayMode))
                options.Graphics.DisplayMode = DisplayModeOption.Fullscreen;

            if (!options.Graphics.Fullscreen && options.Graphics.DisplayMode == DisplayModeOption.Fullscreen)
                options.Graphics.DisplayMode = DisplayModeOption.Windowed;

            options.Graphics.Fullscreen = options.Graphics.DisplayMode != DisplayModeOption.Windowed;
        }

        private static void WriteEnvelope(string savePath, string json)
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
    }
}
