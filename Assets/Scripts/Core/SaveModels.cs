using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Core
{
    [Serializable]
    public sealed class SaveFileEnvelope
    {
        public string SaveVersion = "1.0.0";
        public string TimestampUtc;
        public ProfileSaveData PlayerProfile = new ProfileSaveData();
        public MetaProgressionState MetaProgress = new MetaProgressionState();
        public RunState ActiveRunState;
        public PuzzleSaveState ActivePuzzle;

        // Separate slot so Seasonal Challenge (Monthly Walk) does not overwrite an in-progress run.
        public RunState ActiveSeasonalRunState;
        public PuzzleSaveState ActiveSeasonalPuzzle;

        public TutorialProgressState TutorialProgress = new TutorialProgressState();
        public ProfileStats Statistics = new ProfileStats();
        public MasteryAchievementState Mastery = new MasteryAchievementState();
        public CompletionTrackerState Completion = new CompletionTrackerState();
        public DailyGoalState DailyGoals = new DailyGoalState();
        public SeasonalChallengeState SeasonalChallenge = new SeasonalChallengeState();
    }

    [Serializable]
    public sealed class ProfileSaveData
    {
        public OptionsState Options = new OptionsState();
    }

    [Serializable]
    public sealed class OptionsState
    {
        public AudioSettingsModel Audio = new AudioSettingsModel();
        public GraphicsSettingsModel Graphics = new GraphicsSettingsModel();
        public AccessibilitySettings Accessibility = new AccessibilitySettings();
        public GameplaySettings Gameplay = new GameplaySettings();
        public LanguageOption Language = LanguageOption.English;
    }

    [Serializable]
    public sealed class AudioSettingsModel
    {
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.7f;
        public float UiVolume = 0.6f;
        public bool MuteAll;
        public bool MuteWhenUnfocused;
        public int MenuMusicStyleIndex;
    }

    [Serializable]
    public sealed class GraphicsSettingsModel
    {
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public bool Fullscreen = true;
        public float ParticleIntensity = 1.0f;
        public bool ScreenShake = true;
        public bool VSync = true;
    }

    [Serializable]
    public sealed class AccessibilitySettings
    {
        public bool ColorblindMode;
        public bool HighContrastMode;
        public float FontScale = 1.0f;
        public bool ReduceMotion;
        public bool AlternativeConstraintSymbols;
        public bool ScreenReaderEnabled;
    }

    [Serializable]
    public sealed class GameplaySettings
    {
        public bool ConfirmBeforeWrongPlacement;
        public bool DoubleTapConfirmNumberEntry;
        public bool AutoPencilCleanup;
        public bool HighlightConflicts = true;
        public bool ShowCandidateCount = true;
        public bool CursorSnapOnSelection;
        public bool ControllerRumbleEnabled = true;
    }

    [Serializable]
    public sealed class MetaProgressionState
    {
        public List<ClassId> UnlockedClasses = new List<ClassId> { ClassId.NumberFreak };
        public List<RelicId> DiscoveredRelics = new List<RelicId>();
        public GardenProgressionData GardenProgression = new GardenProgressionData();
        public ClassUnlockProgress ClassUnlocks = new ClassUnlockProgress();
        public ItemCodexData ItemCodex = new ItemCodexData();
        public int AscensionLevel;
        public int PrestigeCount;
        public int MaxStarCap = 6;
        public bool EndlessZenUnlocked;
        public bool SpiritTrialsUnlocked;
        public bool HiddenDualModifierBossUnlocked;
        public List<BossModifierId> DiscoveredBossModifiers = new List<BossModifierId>();
        // [HARMONY-UNLOCK-001] Global Harmony difficulty level progression (0–10).
        // MaxUnlockedHarmonyLevel is never decremented; LastSelectedHarmonyLevel clamped to it on load.
        public int MaxUnlockedHarmonyLevel;
        public int LastSelectedHarmonyLevel;
        // [HARMONY-BADGE-001] Per-level bitmask: bit0 = perfect-run badge, bit1 = speed badge.
        public List<int> HarmonyBadgeFlags = new List<int>();
        // [HARMONY-ACHIEVE-001] Classes that have won GardenRun at H5+ (for "Master of Harmony" achievement).
        public List<ClassId> HarmonyV5PlusWins = new List<ClassId>();
    }

    [Serializable]
    public sealed class GardenProgressionData
    {
        public List<ClassGardenProgressEntry> ClassEntries = new List<ClassGardenProgressEntry>();
    }

    [Serializable]
    public sealed class ClassGardenProgressEntry
    {
        public ClassId ClassId;
        public int TotalXp;
        public int PrestigeTier;
    }

    [Serializable]
    public sealed class ClassUnlockProgress
    {
        public int BossesDefeated;
        public int ItemsUsed;
        public int RelicsCollected;
        public int GoldCollected;
        public bool ItemCodexComplete;
        public bool PerfectNoPencilStage;
        // [BUG-FIX] True when the player completes a full run (all 5 floors + boss) with zero mistakes.
        // Replaces PerfectNoPencilStage as the QuietCartographer unlock gate.
        public bool PerfectFullRunAllFloors;
    }

    [Serializable]
    public sealed class ItemCodexData
    {
        public List<ItemCodexEntry> Entries = new List<ItemCodexEntry>();
    }

    [Serializable]
    public sealed class ItemCodexEntry
    {
        public string ItemId;
        public bool Discovered;
        public bool Mastered;
        public string DiscoveredDate;
    }

    [Serializable]
    public sealed class PuzzleSaveState
    {
        public int BoardSize;
        public int Stars;
        public bool IsBoss;
        public int[] Board;
        public int[] Solution;
        public int[] GivenMask;
        public int[] RegionMap;
        public List<MoveSaveData> Moves = new List<MoveSaveData>();
        public List<PencilMarkSaveData> PencilMarks = new List<PencilMarkSaveData>();
        public List<int> FogHiddenCells = new List<int>();

        // Boss debuff CellLock state: Key=row*100+col, Value=placements until unlock
        public List<LockedCellSaveData> LockedCells = new List<LockedCellSaveData>();
        // blurred_sight curse: cells whose pencil marks are currently hidden
        public List<BlurredCellSaveData> BlurredPencilCells = new List<BlurredCellSaveData>();
    }

    [Serializable]
    public sealed class LockedCellSaveData
    {
        public int Key;   // row*100+col
        public int Count; // placements remaining until unlock
    }

    [Serializable]
    public sealed class BlurredCellSaveData
    {
        public int Row;
        public int Col;
    }

    [Serializable]
    public sealed class MoveSaveData
    {
        public int Row;
        public int Col;
        public int Value;
        public int PreviousValue;
    }

    [Serializable]
    public sealed class PencilMarkSaveData
    {
        public int Row;
        public int Col;
        public List<int> Marks = new List<int>();
    }

    [Serializable]
    public sealed class TutorialProgressState
    {
        public List<string> CompletedKeys = new List<string>();
    }

    [Serializable]
    public sealed class ProfileStats
    {
        public int TotalRunsStarted;
        public int TotalRunsCompleted;
        public int TotalPuzzlesSolved;
        public int TotalMistakes;
        public int TotalGoldEarned;
        public int TotalXpEarned;
        public int TotalBossesDefeated;
        public int TotalItemsUsed;
        public long TotalPlayTimeMs;
        public int HighestEndlessDepth;

        // Spirit Trials personal best scores per tier
        public int TrialsBestScore_Apprentice;
        public int TrialsBestScore_Adept;
        public int TrialsBestScore_Master;
        public int TrialsBestScore_Grandmaster;

        // Endless Zen session tracking
        public int TotalZenSessions;
    }

    [Serializable]
    public sealed class MasteryAchievementState
    {
        public List<MasteryRecord> Records = new List<MasteryRecord>();
        public List<string> UnlockedAchievements = new List<string>();
    }

    [Serializable]
    public sealed class MasteryRecord
    {
        public BossModifierId ModifierId;
        public int ClearCount;
        public int HighStarClears;
        public int NoHpLossClears;
    }

    [Serializable]
    public sealed class CompletionTrackerState
    {
        public List<string> ClearedSizeStarCombos = new List<string>();
        public List<BossModifierId> ClearedModifiers = new List<BossModifierId>();
        public List<ClassId> ClassesAtLevel30 = new List<ClassId>();
        public bool AllRelicsDiscovered;
    }

    [Serializable]
    public sealed class DailyGoalState
    {
        public string LastGoalDate;           // "yyyy-MM-dd" of the last day goals were generated
        public int[] TodayGoalIds = new int[3]; // indices into the full goal pool
        public bool[] TodayGoalCompleted = new bool[3];

        public int CurrentStreak;
        public string LastStreakDate;         // last date all 3 goals were completed

        public int TotalInkStamps;
        public List<int> UnlockedCosmetics = new List<int>(); // indices of unlocked cosmetics

        // Pending run-start rewards (applied at next LaunchRun)
        public int PendingStartGold;
        public int PendingStartRerollTokens;
    }

    [Serializable]
    public sealed class SeasonalChallengeState
    {
        // Key: "YYYY-MM" (e.g. "2026-04"); parallel lists for JsonUtility compat
        public List<string> BestScoreKeys    = new List<string>();
        public List<int>    BestScoreValues  = new List<int>();

        // Optional: breakdown for the best score attempt (same index as BestScoreKeys)
        // -1 indicates "unknown / not recorded" (older saves).
        public List<int> BestHpRemainingValues   = new List<int>();
        public List<int> BestPencilUsedValues    = new List<int>();
        public List<int> BestTimeSecondsValues   = new List<int>();

        public int GetBest(int year, int month)
        {
            var key = $"{year:D4}-{month:D2}";
            var idx = BestScoreKeys.IndexOf(key);
            return idx >= 0 && idx < BestScoreValues.Count ? BestScoreValues[idx] : 0;
        }

        public bool TryGetBestBreakdown(int year, int month,
            out int score,
            out int hpRemaining,
            out int pencilUsed,
            out int timeSeconds)
        {
            score = 0;
            hpRemaining = -1;
            pencilUsed = -1;
            timeSeconds = -1;

            var key = $"{year:D4}-{month:D2}";
            var idx = BestScoreKeys.IndexOf(key);
            if (idx < 0) return false;

            if (idx < BestScoreValues.Count) score = BestScoreValues[idx];
            if (idx < BestHpRemainingValues.Count) hpRemaining = BestHpRemainingValues[idx];
            if (idx < BestPencilUsedValues.Count) pencilUsed = BestPencilUsedValues[idx];
            if (idx < BestTimeSecondsValues.Count) timeSeconds = BestTimeSecondsValues[idx];

            return score > 0;
        }

        public void SetBest(int year, int month, int score)
        {
            // Legacy setter: updates score only.
            var key = $"{year:D4}-{month:D2}";
            var idx = BestScoreKeys.IndexOf(key);
            if (idx >= 0)
            {
                if (idx < BestScoreValues.Count && score > BestScoreValues[idx])
                    BestScoreValues[idx] = score;
            }
            else
            {
                BestScoreKeys.Add(key);
                BestScoreValues.Add(score);
            }
        }

        public void SetBest(int year, int month, int score, int hpRemaining, int pencilUsed, int timeSeconds)
        {
            var key = $"{year:D4}-{month:D2}";
            var idx = BestScoreKeys.IndexOf(key);

            if (idx >= 0)
            {
                if (idx < BestScoreValues.Count && score <= BestScoreValues[idx]) return;

                EnsureBreakdownListsLength(idx + 1);
                if (idx < BestScoreValues.Count) BestScoreValues[idx] = score;
                else
                {
                    // Repair mismatched legacy data (shouldn't happen but keep safe).
                    while (BestScoreValues.Count < idx) BestScoreValues.Add(0);
                    BestScoreValues.Add(score);
                }

                BestHpRemainingValues[idx] = hpRemaining;
                BestPencilUsedValues[idx] = pencilUsed;
                BestTimeSecondsValues[idx] = timeSeconds;
            }
            else
            {
                BestScoreKeys.Add(key);
                BestScoreValues.Add(score);
                BestHpRemainingValues.Add(hpRemaining);
                BestPencilUsedValues.Add(pencilUsed);
                BestTimeSecondsValues.Add(timeSeconds);
            }
        }

        private void EnsureBreakdownListsLength(int count)
        {
            while (BestHpRemainingValues.Count < count) BestHpRemainingValues.Add(-1);
            while (BestPencilUsedValues.Count < count) BestPencilUsedValues.Add(-1);
            while (BestTimeSecondsValues.Count < count) BestTimeSecondsValues.Add(-1);
        }
    }

    /// <summary>
    /// Lightweight summary of a save slot shown on the profile-select screen.
    /// Derived from the full SaveFileEnvelope at load time — never persisted separately.
    /// </summary>
    public sealed class ProfileSlotSummary
    {
        public int SlotIndex;          // 0, 1, or 2
        public bool IsEmpty;
        public string DisplayName;     // e.g. "Profile 1"
        public ClassId LastPlayedClass;
        public int TotalRunsStarted;
        public int TotalRunsCompleted;
        public int TotalBossesDefeated;
        public bool HasActiveRun;
        public string LastPlayedUtc;   // ISO timestamp from envelope
    }
}
