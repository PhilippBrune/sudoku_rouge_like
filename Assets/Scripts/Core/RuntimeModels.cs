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
        public bool IsInfinite; // set by Eternal Lotus relic
    }

    [Serializable]
    public sealed class RelicInstance
    {
        public RelicId Id;
        public RelicTier Tier;
        public int UsesRemaining = -1; // -1 = passive (unlimited); >0 = active uses left
    }

    [Serializable]
    public sealed class RunState
    {
        public int Seed;
        public int Depth;
        public int CurrentNodeIndex;
        public int CurrentFloor;
        public int TotalFloors = 5;
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

        public int CurrentXP;   // accumulated run XP total (committed to class progression at run end)
        public int RerollTokens;
        public int ItemSlots;

        public int PencilPurchasesThisRun;
        public int RerollsThisRun;
        public int ItemsUsedCount;
        public int PencilUsedCount;
        public bool LostHpThisRun;

        public readonly List<ItemInstance> Inventory = new();

        // Single relic slot — player holds one relic at a time
        public bool HasRelic;
        public RelicInstance HeldRelic;

        // Active relic state tracked per-puzzle
        public bool CrackedTeacupUsedThisPuzzle;   // first mistake free
        public int PaperCraneUsesRemaining = 2;     // skip puzzle (2/run)
        public bool PhoenixFeatherUsed;              // death prevention (1/run)
        public bool DragonsEyeUsedThisFloor;         // reveal solution (1/floor)
        public int MonkCharmStreakCount;              // consecutive correct placements
        public int StoneSundialBonusSlots;            // +1 reward slot
        public bool SakuraSealPerfectLastPuzzle;      // track for next puzzle HP bonus
        public int UmbrellaShieldCharges;             // Rice Paper Umbrella remaining
        public int SilkFanPendingIndex = -1;             // inventory index while awaiting second cell
        public int SilkFanFirstRow = -1;
        public int SilkFanFirstCol = -1;
        public readonly List<RouteType> RouteHistory = new();
        public readonly List<RunNode> NodePath = new();

        public RunArchetype CurrentArchetype = RunArchetype.Undefined;
        public float GlobalGoldMultiplier = 1f;
        public int MistakeShieldCharges;
        public int ComboMistakeProtectionCharges;
        public bool CarryGoldInterest;
        public bool CorruptedGardenPath;
        public int MutationNodesRemaining;
        public AdaptationMutationType ActiveMutation = AdaptationMutationType.None;
        public bool RiskyRebuildUsed;
        public int PreBossPuzzlesCompleted;

        // Serialization-friendly boss modifier fields (JsonUtility does not support nullable/HashSet)
        public bool HasChosenBossModifier;
        public BossModifierId ChosenBossModifierId;
        public List<BossModifierId> SeenBossModifierList = new();

        [System.NonSerialized]
        public HashSet<BossModifierId> SeenBossModifiers = new();

        public BossModifierId? ChosenBossModifier
        {
            get => HasChosenBossModifier ? ChosenBossModifierId : (BossModifierId?)null;
            set
            {
                HasChosenBossModifier = value.HasValue;
                if (value.HasValue) ChosenBossModifierId = value.Value;
            }
        }

        public void SyncSeenModifiersToList()
        {
            SeenBossModifierList.Clear();
            foreach (var m in SeenBossModifiers) SeenBossModifierList.Add(m);
        }

        public void SyncSeenModifiersFromList()
        {
            SeenBossModifiers.Clear();
            for (var i = 0; i < SeenBossModifierList.Count; i++) SeenBossModifiers.Add(SeenBossModifierList[i]);
        }

        public bool AllowIrregularPuzzles = true;

        public readonly List<BossModifierId> ChosenBossModifiers = new();

        public readonly List<CurseType> ActiveCurses = new();
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
        public float VarianceBand;
        public int RegionVariant;
        public BossModifierIntensity ModifierIntensity = BossModifierIntensity.Medium;
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
    }

    [Serializable]
    public sealed class ShopOffer
    {
        public string OfferId;
        public bool IsRelic;
        public RelicInstance RelicOffer;  // set when IsRelic=true
        public ItemInstance Item;         // set when IsRelic=false
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
        public bool IsCrossLink;
        public float CanvasX;
        public float CanvasY;
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
        public int GoldEarned;
        public int XpEarned;
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

        // New fields for class unlock tracking
        public int ItemsUsedThisRun;
        public int RelicsCollectedThisRun;
        public bool FoundAllUniqueItems;
        public bool ClearedStageNoPencilNoHpLoss;

        // Achievement-relevant fields
        public int HighestBoardSize;
        public int HighestStarCleared;
        public int RiskRoutesChosen;
        public int SimultaneousModifiersOnBoss;
        public bool UsedAnyItem;
        public bool SwappedRelic;
        public bool BoughtFromShop;
        public bool AcquiredEpicItem;
        public bool AcquiredRelic;
        public bool FlawlessFloor;
        public int SpiritTrialScore;

        // Per-tile XP breakdown for end-of-run display
        public readonly List<Economy.TileXpEntry> TileXpEntries = new();
    }

    [Serializable]
    public sealed class PostRunAnalytics
    {
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
        public int HighestEndlessDepth;
        public int TotalAchievementsUnlocked;

        // Completion tracking
        public int SizeStarCombosCleared;
        public readonly HashSet<string> ClearedSizeStarKeys = new();

        // Achievement tracking
        public int HighestGoldInSingleRun;
        public int RiskRoutesChosenInBestRun;
        public int HighestCombo;
        public int HighestFloorReached;
        public int ModifiersEncountered;
        public bool CompletedFullRun;
        public bool UsedItem;
        public bool AcquiredRelic;
        public bool BoughtFromShop;
        public bool SwappedRelic;
        public bool CompletedTutorialPuzzle;
        public bool CompletedNoItemRun;
        public bool CompletedFlawlessFloor;
        public bool AcquiredEpicItem;
        public int HighestSpiritTrialScore;

        // Spirit Trials per-tier personal bests
        public readonly SpiritTrialsPersonalBest[] SpiritTrialsBests = new SpiritTrialsPersonalBest[4];
    }

    [Serializable]
    public sealed class SpiritTrialsPersonalBest
    {
        public int BestScore;
        public int BestTimeSeconds;
        public int BestNoMistakeTimeSeconds;
        public int TotalSessionsPlayed;
    }

    [Serializable]
    public sealed class TutorialSetupConfig
    {
        public int BoardSize = 5;
        public int Stars = 1;
        public int RegionVariant;
        public List<BossModifierId> SelectedModifiers = new();
        public TutorialResourceMode ResourceMode = TutorialResourceMode.Simulation;
        public ClassId SimulationClassId = ClassId.NumberFreak;
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
        public readonly List<ClassId> UnlockedClasses = new();
        public readonly List<RelicId> DiscoveredRelics = new();
        public bool EndlessZenUnlocked;
        public bool SpiritTrialsUnlocked;
        public int MaxStarCap = 5;
        public int AscensionLevel;
        public int PrestigeCount;
        public bool HiddenDualModifierBossUnlocked;
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
        public int TotalXp;       // single persistent value; level derived at runtime
        public int PrestigeTier;  // 0-9
    }

    [Serializable]
    public sealed class ClassUnlockProgress
    {
        public int CumulativeBossDefeats;
        public int CumulativeItemsUsed;
        public int CumulativeRelicsCollected;
        public int CumulativeGoldCollected;
        public bool AllUniqueItemsFound;
        public bool ClearedStageZeroPencilZeroHpLost;
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
