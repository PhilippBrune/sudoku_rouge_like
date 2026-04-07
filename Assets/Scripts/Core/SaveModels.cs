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
        public TutorialProgressState TutorialProgress = new TutorialProgressState();
        public ProfileStats Statistics = new ProfileStats();
        public MasteryAchievementState Mastery = new MasteryAchievementState();
        public CompletionTrackerState Completion = new CompletionTrackerState();
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
        public bool Fullscreen;
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
}
