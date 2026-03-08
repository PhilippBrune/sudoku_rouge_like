using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Core
{
    [Serializable]
    public sealed class PuzzleStepStat
    {
        public SudokuTechnique Technique;
        public int Count;
    }

    [Serializable]
    public sealed class PuzzleAnalysis
    {
        public int StepCount;
        public SudokuTechnique HighestTechnique = SudokuTechnique.NakedSingle;
        public int DependencyDepth;
        public float ModifierComplexityWeight;
        public float DifficultyScore;
        public PuzzleDifficultyTier DifficultyTier = PuzzleDifficultyTier.Tier1;
        public bool IsLogicallySolvable;
        public bool HasUniqueSolution;
        public readonly List<PuzzleStepStat> TechniqueUsage = new();
    }

    [Serializable]
    public sealed class PuzzleGenerationRequest
    {
        public int BoardSize;
        public int Stars;
        public PuzzleDifficultyTier TargetTier;
        public bool AllowBruteForceOnly;
        public int Seed;
        public int RegionVariant;
        public List<BossModifierId> ActiveModifiers = new();
    }

    [Serializable]
    public sealed class PuzzleGenerationResult
    {
        public bool Success;
        public string FailureReason;
        public Sudoku.SudokuBoard Board;
        public PuzzleAnalysis Analysis;
    }

    [Serializable]
    public sealed class RunFeelState
    {
        public int CurrentCorrectStreak;
        public int PeakCorrectStreak;
        public bool MadeMistake;
        public bool UsedSolverItem;
        public bool LostHp;
        public bool ClearMindAwarded;
        public bool IsNearDeath;
        public MusicLayer CurrentMusicLayer = MusicLayer.CalmGardenBase;
    }

    [Serializable]
    public sealed class MasteryAchievementState
    {
        public readonly List<BossModifierId> BossClearsByModifier = new();
        public readonly List<BossModifierId> PerfectBossClearsByModifier = new();
        public int NineByNineFiveStarClears;
        public int DualModifierClears;
        public int NoItemRuns;
    }

    [Serializable]
    public sealed class ModifierMasteryEntry
    {
        public BossModifierId Modifier;
        public ModifierBadgeTier BadgeTier;
    }

    [Serializable]
    public sealed class CompletionTrackerState
    {
        public float GlobalCompletionPercent;
        public bool AllSizesAllStarsCleared;
        public bool AllModifiersCleared;
        public bool AllClassesLevelThirty;
        public bool AllRelicsUnlocked;
        public bool MultiStageBossHighHeatClear;
    }

    [Serializable]
    public sealed class PuzzleSaveState
    {
        public int BoardSize;
        public int[] SolutionFlat;
        public int[] RegionMapFlat;
        public int[] CellsFlat;
        public bool[] GivenFlat;
        public string[] PencilSerializedPerCell;
        public string ModifierStateJson;
        public int CurrentHP;
        public int CurrentPencil;
        public int CurrentGold;
        public int ComboStreak;
        public int PeakCombo;
        public MusicLayer MusicLayer;
        public int Mistakes;
        public int CorrectPlacements;
        public int Stars;
        public int Difficulty;
        public bool IsBoss;
    }

    [Serializable]
    public sealed class SaveFileEnvelope
    {
        public string SaveVersion = "1.0.0";
        public ProfileSaveData PlayerProfile = new();
        public MetaProgressionState MetaProgress = new();
        public RunState ActiveRunState;
        public TutorialProgressState TutorialProgress = new();
        public ProfileStats Statistics = new();
        public MasteryAchievementState Mastery = new();
        public CompletionTrackerState Completion = new();
        public PuzzleSaveState ActivePuzzle;
    }

    [Serializable]
    public sealed class ProfileSaveData
    {
        public string PlayerName = "Gardener";
        public OptionsState Options = new();
    }
}
