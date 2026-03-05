using System;
using System.Collections.Generic;

namespace SudokuRoguelike.Core
{
    [Serializable]
    public sealed class ItemInstance
    {
        public string Id;
        public ItemType Type;
        public ItemRarity Rarity;
        public int Charges = 1;
    }

    [Serializable]
    public sealed class RunState
    {
        public int Seed;
        public int Depth;
        public int CurrentNodeIndex;
        public ClassId ClassId;
        public GameMode Mode = GameMode.GardenRun;
        public bool TutorialMode;
        public TutorialResourceMode TutorialResourceMode = TutorialResourceMode.Simulation;
        public bool DisableProgressionRewards;

        public int CurrentHP;
        public int MaxHP;
        public int CurrentPencil;
        public int MaxPencil;
        public int CurrentGold;

        public int Level = 1;
        public int CurrentXP;
        public int RerollTokens;
        public int ItemSlots;

        public int PencilPurchasesThisRun;
        public int RerollsThisRun;

        public readonly List<ItemInstance> Inventory = new();
        public readonly List<string> RelicIds = new();
        public readonly List<RouteType> RouteHistory = new();
        public readonly List<RunNode> NodePath = new();

        public float CurrentHeatScore = 1f;
        public float PeakHeatScore = 1f;
        public RunArchetype CurrentArchetype = RunArchetype.Undefined;
        public float GlobalGoldMultiplier = 1f;
        public int MistakeShieldCharges;
        public int ComboMistakeProtectionCharges;
        public bool CarryGoldInterest;
        public bool CorruptedGardenPath;
        public int MutationNodesRemaining;
        public AdaptationMutationType ActiveMutation = AdaptationMutationType.None;
        public bool RiskyRebuildUsed;

        public readonly List<CurseType> ActiveCurses = new();
        public readonly List<float> HeatHistory = new();
        public readonly List<string> RunNotes = new();

        public bool IsDead => CurrentHP <= 0;
    }

    [Serializable]
    public sealed class LevelConfig
    {
        public DifficultyTier Difficulty;
        public int Stars;
        public int BoardSize;
        public float MissingPercent;
        public bool IsBoss;
        public StressVariant StressVariant;
        public float ExpectedHeat;
        public float VarianceBand;
        public readonly List<BossModifierId> ActiveModifiers = new();
    }

    [Serializable]
    public sealed class LevelState
    {
        public int Mistakes;
        public int CorrectPlacements;
        public bool PuzzleComplete;
        public bool TeaOfFocusActive;
        public int TeaOfFocusRemainingPlacements;
        public readonly List<MoveRecord> Moves = new();

        public float StartHeatScore = 1f;
        public float CurrentHeatScore = 1f;
    }

    [Serializable]
    public sealed class MoveRecord
    {
        public int Row;
        public int Col;
        public int Value;
        public bool WasCorrect;
        public bool WasPencil;
    }

    [Serializable]
    public sealed class ItemRollSlot
    {
        public bool IsNothing;
        public bool IsLocked;
        public ItemInstance RolledItem;
        public int NothingGoldBonus;
    }

    [Serializable]
    public sealed class ShopOffer
    {
        public string OfferId;
        public bool IsRelic;
        public string RelicId;
        public ItemInstance Item;
        public int Price;
    }

    [Serializable]
    public sealed class RunNode
    {
        public int Depth;
        public int Layer;
        public NodeType Type;
        public bool IsRiskPath;
        public bool IsRevealed;
    }

    [Serializable]
    public sealed class BossPhase
    {
        public int PhaseIndex;
        public DifficultyTier Difficulty;
        public int Stars;
        public List<BossModifierId> Modifiers = new();
        public int MistakePenalty = 1;
        public int StartingPencilPenalty;
    }

    [Serializable]
    public sealed class RunResult
    {
        public ClassId PlayedClassId = ClassId.NumberFreak;
        public GameMode Mode;
        public bool Victory;
        public int GardenDepthReached;
        public float FinalHeatScore;
        public float PeakHeatScore;
        public int GoldEarned;
        public int XpEarned;
        public int EssenceEarned;
        public int BossPhaseReached;
        public int MistakesMade;
        public int SecondsPlayed;
        public bool TutorialMode;

        public bool ClearedBoss;
        public BossModifierTier ClearedBossTier = BossModifierTier.Tier1;
        public bool SolvedEightByEightFourStar;
        public bool CompletedKoiPathRoute;
        public bool WonWithUnderThreeHp;
        public bool WonWithOneHp;
        public bool ClearedGermanWhispersBoss;
        public bool ClearedMultiStageBoss;
        public bool PerfectClear;
        public int PeakCombo;
        public RunArchetype FinalArchetype;
        public PostRunAnalytics Analytics;
    }

    [Serializable]
    public sealed class PostRunAnalytics
    {
        public readonly List<float> HeatCurve = new();
        public readonly List<int> MistakesPerPuzzle = new();
        public int TotalMistakes;
        public int HighestSinglePuzzleMistakes;
        public int HardestPuzzleStars;
        public BossModifierId HardestPuzzleModifier;
        public PuzzleDifficultyTier HardestPuzzleTier = PuzzleDifficultyTier.Tier1;
        public float ModifierImpactRating;
        public readonly List<string> ImprovementSuggestions = new();
    }

    [Serializable]
    public sealed class RunEventOption
    {
        public string OptionId;
        public string Label;
        public string Tradeoff;
    }

    [Serializable]
    public sealed class RunEvent
    {
        public string EventId;
        public EventCategory Category;
        public string Prompt;
        public readonly List<RunEventOption> Options = new();
    }

    [Serializable]
    public sealed class ProfileStats
    {
        public int TotalRuns;
        public int BossClears;
        public float AverageMistakes;
        public int FastestSeconds;
        public float HighestHeatScore;
        public int HighestEndlessDepth;
        public int TotalAchievementsUnlocked;
    }

    [Serializable]
    public sealed class TutorialSetupConfig
    {
        public int BoardSize = 5;
        public int Stars = 1;
        public List<BossModifierId> SelectedModifiers = new();
        public TutorialResourceMode ResourceMode = TutorialResourceMode.Simulation;
    }

    [Serializable]
    public sealed class TutorialProgressState
    {
        public readonly List<string> CompletedConfigurationKeys = new();
        public readonly List<BossModifierId> CompletedSingleModifiers = new();
    }

    [Serializable]
    public sealed class TutorialCellProgress
    {
        public int BoardSize;
        public int Stars;
        public bool Completed;
    }

    [Serializable]
    public sealed class TutorialModifierProgress
    {
        public BossModifierId Modifier;
        public bool Completed;
    }

    [Serializable]
    public sealed class MetaProgressionState
    {
        public int GardenEssence;
        public readonly List<ClassId> UnlockedClasses = new();
        public readonly List<string> UnlockedRelics = new();
        public bool EndlessZenUnlocked;
        public bool SpiritTrialsUnlocked;
        public int MaxStarCap = 5;
        public int AscensionLevel;
        public int PrestigeCount;
        public bool HiddenDualModifierBossUnlocked;
        public bool ChaosMonkUnlocked;
        public bool SeasonalChallengeUnlocked;
        public ClassUnlockProgress ClassUnlocks = new();
        public GardenClassProgressionState GardenProgression = new();
        public ItemCodexState ItemCodex = new();
        public readonly List<string> PurchasedPermanentUpgrades = new();
        public readonly List<string> UnlockedAchievements = new();
    }

    [Serializable]
    public sealed class ItemCodexState
    {
        public int SaveDataVersion = 1;
        public readonly List<ItemCodexEntry> Entries = new();
    }

    [Serializable]
    public sealed class ItemCodexEntry
    {
        public string ItemID;
        public string Name;
        public string Type;
        public string RarityTier;
        public string UnlockCondition;
        public string Description;
        public string EffectFormula;
        public string SynergyTags;
        public bool Discovered;
        public bool Mastered;
        public int TimesPicked;
        public int TimesWon;
        public int TimesUsed;
        public int BestRunDepth;
        public string DiscoveredDate;
    }

    [Serializable]
    public sealed class GardenClassProgressionState
    {
        public int CurrentLevel = 1;
        public int CurrentXp;
        public int PrestigeTier;
        public int PassiveTier;
        public int TotalXpEarned;
        public int ArchiveRunCount;
        public int ArchiveSeedsBloomed;
        public int ArchiveBossesDefeated;
        public int ArchivePerfectRuns;
        public readonly List<ClassGardenProgressEntry> ClassEntries = new();
    }

    [Serializable]
    public sealed class ClassGardenProgressEntry
    {
        public ClassId ClassId = ClassId.NumberFreak;
        public int Level = 1;
        public int CurrentXp;
        public int PrestigeTier;
        public int TotalXpEarned;
    }

    [Serializable]
    public sealed class ClassUnlockProgress
    {
        public int NonTutorialRunCount;
        public bool ClearedTier1Or2Boss;
        public bool ClearedTier3Boss;
        public bool SolvedEightByEightFourStar;
        public bool CompletedKoiPath;
        public bool WonWithUnderThreeHp;
        public bool ClearedTier4Boss;
        public bool ReachedHeatFive;
        public bool ClearedGermanWhispersBoss;
        public bool ClearedMultiStageBoss;
    }

    [Serializable]
    public sealed class GameplaySettings
    {
        public bool ConfirmBeforeWrongPlacement;
        public bool DoubleTapConfirmNumberEntry;
        public bool AutoPencilCleanup;
        public bool HighlightConflicts = true;
        public bool ShowCandidateCount = true;
        public bool ShowHeatIndicator;
        public bool CursorSnapOnSelection;
    }

    [Serializable]
    public sealed class AccessibilitySettings
    {
        public bool ColorblindMode;
        public bool HighContrastMode;
        public float FontScale = 1f;
        public bool ReduceMotion;
        public bool AlternativeConstraintSymbols;
    }

    [Serializable]
    public sealed class AudioSettingsModel
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;
        public float UiVolume = 1f;
        public bool MuteAll;
        public string OutputDeviceName;
        public int MenuMusicStyleIndex;
    }

    [Serializable]
    public sealed class GraphicsSettingsModel
    {
        public int Width = 1920;
        public int Height = 1080;
        public bool Fullscreen = true;
        public bool Borderless;
        public bool VSync = true;
        public int FrameLimit = 60;
        public bool PixelPerfect = true;
        public float UiScale = 1f;
        public bool ScreenShake = true;
        public float ParticleIntensity = 1f;
    }

    [Serializable]
    public sealed class OptionsState
    {
        public LanguageOption Language = LanguageOption.English;
        public AudioSettingsModel Audio = new();
        public GraphicsSettingsModel Graphics = new();
        public GameplaySettings Gameplay = new();
        public AccessibilitySettings Accessibility = new();
    }

    [Serializable]
    public sealed class SessionState
    {
        public bool HasRunInProgress;
        public MenuScreen CurrentScreen = MenuScreen.Main;
        public GameMode SelectedMode = GameMode.GardenRun;
        public int SelectedSeed;
        public bool TutorialMode;
        public TutorialSetupConfig TutorialSetup = new();
    }
}
