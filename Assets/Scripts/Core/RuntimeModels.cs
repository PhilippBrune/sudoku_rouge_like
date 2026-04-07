using System;
using System.Collections.Generic;
using UnityEngine;

namespace SudokuRoguelike.Core
{
    [Serializable]
    public sealed class RunState
    {
        public ClassId ClassId;
        public GameMode Mode;
        public int RunNumber;
        public int Seed;
        public int Depth;
        public int CurrentFloor;
        public int TotalFloors = 5;
        public int CurrentNodeIndex;

        // Resources
        public int CurrentHP;
        public int MaxHP;
        public int CurrentPencil;
        public int MaxPencil;
        public int CurrentGold;
        public int CurrentXP;
        public int RerollTokens;
        public int ItemSlots;

        // Inventory
        public List<ItemInstance> HeldItems = new List<ItemInstance>();
        public bool HasRelic;
        public RelicInstance HeldRelic;

        // Path
        public List<int> RouteHistory = new List<int>();
        public List<int> NodePath = new List<int>();

        // Floor modifiers (applied to all puzzles on the current floor)
        public List<BossModifierId> ActiveFloorModifiers = new List<BossModifierId>();

        // Boss modifiers (chosen by player at boss gate)
        public bool HasChosenBossModifier;
        public BossModifierId ChosenBossModifierId;
        public List<BossModifierId> ChosenBossModifiers = new List<BossModifierId>();
        public List<BossModifierId> SeenBossModifierList = new List<BossModifierId>();

        [NonSerialized] public HashSet<BossModifierId> SeenBossModifiers = new HashSet<BossModifierId>();

        // Combo
        public int ComboStreak;
        public int PeakComboThisRun;

        // Positive floor effect (one per floor, rolled alongside floor modifiers)
        public bool HasPositiveFloorEffect;
        public PositiveFloorEffect ActivePositiveFloorEffect;

        // Settings
        public bool AllowIrregularPuzzles = true;

        // Tutorial / Mode flags
        public bool TutorialMode;
        public bool DisableProgressionRewards;

        // Economy tracking
        public float GlobalGoldMultiplier = 1.0f;
        public int PencilPurchaseCount;
        public int RerollCount;

        public BossModifierId? ChosenBossModifier
        {
            get => HasChosenBossModifier ? ChosenBossModifierId : (BossModifierId?)null;
            set
            {
                if (value.HasValue)
                {
                    HasChosenBossModifier = true;
                    ChosenBossModifierId = value.Value;
                }
                else
                {
                    HasChosenBossModifier = false;
                }
            }
        }

        public void SyncSeenModifiersToList()
        {
            SeenBossModifierList.Clear();
            foreach (var mod in SeenBossModifiers)
                SeenBossModifierList.Add(mod);
        }

        public void SyncSeenModifiersFromList()
        {
            SeenBossModifiers.Clear();
            for (var i = 0; i < SeenBossModifierList.Count; i++)
                SeenBossModifiers.Add(SeenBossModifierList[i]);
        }
    }

    [Serializable]
    public sealed class LevelConfig
    {
        public int BoardSize = 9;
        public int Stars = 1;
        public float MissingPercent = 0.4f;
        public int RegionVariant;
        public bool IsBoss;
        public bool IsCursed;        // player accepted cursed node — +1 extra modifier, +50% gold/XP
        public float CursedGoldMult = 1f; // 1.5 when IsCursed
        public float CursedXpMult  = 1f; // 1.5 when IsCursed
        public List<BossModifierId> ActiveModifiers = new List<BossModifierId>();
        public BossModifierIntensity Intensity = BossModifierIntensity.Medium;
        public int Seed;
        public DifficultyTier Difficulty = DifficultyTier.Diff1;

        public LevelConfig Clone()
        {
            var clone = new LevelConfig
            {
                BoardSize = BoardSize,
                Stars = Stars,
                MissingPercent = MissingPercent,
                RegionVariant = RegionVariant,
                IsBoss = IsBoss,
                IsCursed = IsCursed,
                CursedGoldMult = CursedGoldMult,
                CursedXpMult = CursedXpMult,
                Intensity = Intensity,
                Seed = Seed,
                Difficulty = Difficulty
            };
            clone.ActiveModifiers.AddRange(ActiveModifiers);
            return clone;
        }
    }

    [Serializable]
    public sealed class LevelState
    {
        public int Mistakes;
        public int CorrectPlacements;
        public int PencilMarksUsed;
        public int ItemsUsedThisLevel;
        public bool PerfectSoFar = true;
        public bool NoPencilUsed = true;
    }

    [Serializable]
    public sealed class RunNode
    {
        public int Index;
        public NodeType Type;
        public RouteType Route;
        public int Floor;
        public float CanvasX;
        public float CanvasY;
        public List<int> NextNodes = new List<int>();
        public bool Visited;
        public bool Reachable;
    }

    [Serializable]
    public sealed class TileXpEntry
    {
        public int BoardSize;
        public int Stars;
        public bool IsBoss;
        public int BaseXp;
        public float StarMultiplier;
        public int ModifierBonus;
        public float BossMultiplier;
        public int PerfectBonus;
        public int TotalXp;
    }

    [Serializable]
    public sealed class ItemInstance
    {
        public string Id;
        public ItemType Type;
        public ItemRarity Rarity;
        public int Charges;
        public bool IsInfinite;
    }

    [Serializable]
    public sealed class RelicInstance
    {
        public RelicId Id;
        public RelicTier Tier;
        public int UsesRemaining = -1;
    }

    [Serializable]
    public sealed class RunEvent
    {
        public string Id;
        public string Title;
        public string Description;
        public List<EventOption> Options = new List<EventOption>();
    }

    [Serializable]
    public sealed class EventOption
    {
        public string Label;
        public string EffectDescription;
        public int GoldCost;
        public int HpCost;
        public int PencilCost;
    }

    [Serializable]
    public sealed class ShopOffer
    {
        public ItemInstance Item;
        public int Price;
        public bool IsSold;
    }

    [Serializable]
    public sealed class TutorialSetupConfig
    {
        public int BoardSize = 9;
        public int Stars = 1;
        public TutorialResourceMode ResourceMode = TutorialResourceMode.Free;
        public ClassId SimulationClassId = ClassId.NumberFreak;
        public int RegionVariant;
        public List<BossModifierId> SelectedModifiers = new List<BossModifierId>();
    }

    [Serializable]
    public sealed class LaunchRequest
    {
        public ClassId ClassId;
        public GameMode Mode;
        public bool AllowIrregularPuzzles = true;
    }

    [Serializable]
    public sealed class FloorGraph
    {
        public int FloorIndex;
        public List<RunNode> Nodes = new List<RunNode>();
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
        public bool DisableProgressionRewards;

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

        public int ItemsUsedThisRun;
        public int RelicsCollectedThisRun;
        public int PerfectPuzzleCount;
        public int CursedPuzzlesAccepted;
        public int RunScore;
        public bool FoundAllUniqueItems;
        public bool ClearedStageNoPencilNoHpLoss;

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

        public List<TileXpEntry> TileXpEntries = new List<TileXpEntry>();
    }

    [Serializable]
    public sealed class PostRunAnalytics
    {
        public List<int> MistakesPerPuzzle = new List<int>();
        public int TotalMistakes;
        public int HighestSinglePuzzleMistakes;
        public int HardestPuzzleStars;
        public BossModifierId HardestPuzzleModifier;
        public PuzzleDifficultyTier HardestPuzzleTier = PuzzleDifficultyTier.Tier1;
        public float ModifierImpactRating;
        public List<string> ImprovementSuggestions = new List<string>();
        public int RunScore;
        public int PerfectPuzzleCount;
        public int CursedPuzzlesCompleted;
    }
}
