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
        /// <summary>Class level at the moment this run was started; drives Epic gating, exclusive item/relic pools, and passive thresholds.</summary>
        public int ClassLevel = 1;

        // Inventory
        public List<ItemInstance> HeldItems = new List<ItemInstance>();

        // Relic inventory — unlimited slots; HasRelic/HeldRelic kept for save-file compatibility
        public bool HasRelic;                                    // legacy compat — true when any relic is held
        public RelicInstance HeldRelic;                          // legacy compat — first relic added
        public List<RelicInstance> HeldRelics = new List<RelicInstance>(); // [REQ: RELIC-SLOT-002] multi-relic list (future-proof; currently max 1 entry)

        // Path
        public List<int> RouteHistory = new List<int>();
        public List<int> NodePath = new List<int>();

        // [REQ: BOSS-FLOOR-003] Floor modifiers stored in RunState for persistence (applied to all puzzles on the current floor)
        public List<BossModifierId> ActiveFloorModifiers = new List<BossModifierId>();

        // Boss modifiers (chosen by player at boss gate)
        public bool HasChosenBossModifier;           // [REQ: SAVE-SERIAL-002] nullable via bool+value pattern
        public BossModifierId ChosenBossModifierId;
        public List<BossModifierId> ChosenBossModifiers = new List<BossModifierId>();
        public List<BossModifierId> SeenBossModifierList = new List<BossModifierId>(); // [REQ: SAVE-SERIAL-003] HashSet synced to List for serialization

        [NonSerialized] public HashSet<BossModifierId> SeenBossModifiers = new HashSet<BossModifierId>();

        // Combo
        public int ComboStreak;
        public int PeakComboThisRun;
        public int PerfectPuzzleStreak; // used by SakuraSeal relic

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
        public int ShopRerollCount;
        public int BossesDefeatedThisRun;  // boss puzzles cleared during this run

        // Item effect state
        public int MistakeShieldCharges;   // RicePaperUmbrella: absorbs next N mistakes
        public int FogDisabledMoves;       // LanternOfClarity: fog disabled for next N moves
        public int LastMistakeHpLost;      // GinkgoLeaf: HP actually lost on last mistake (for restoration)

        // Run timer
        public float TotalRunSeconds;      // Accumulated puzzle time across all puzzles this run

        // Class-exclusive item effect state
        public int MonksBeadsCountdown;    // GardenMonk L15: placements remaining in current bead sequence
        public bool WornChiselActive;      // StoneGardener L15: 30% shop discount active this floor
        public bool PledgeActive;          // ReedDuelist L15: no-pencil pledge in progress
        public bool AllNodesRevealed;      // QuietCartographer L15/L30: all floor nodes visible
        public int FortunesLedgerCounter;  // NumberFreak L30: correct placements since last Reroll Token
        public bool TempleVowReady;        // GardenMonk L30: trigger ready (HP ≤ 25%, fires once/puzzle)
        public bool WardingFlameUsed;      // LanternSeer L30: once-per-run auto-remove used

        // Mode flags
        public bool IsSeasonalChallenge;   // suppresses XP/gold awards, class passives, relics, curses

        // Curse system — [REQ: CURSE-DATA-001] [REQ: CURSE-STACK-003]
        public List<string> ActiveCurseIds = new List<string>();
        public bool TremblingHandFired;    // reset each puzzle — tracks first-placement curse

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
        public bool IsElite;    // [REQ: PRESSURE-ROLL-002] node type is elite — eligible for pressure mechanic rolling
        public bool IsPreBoss;  // [REQ: PRESSURE-ROLL-002] node is immediately before a boss — eligible for pressure mechanics
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
                IsElite = IsElite,
                IsPreBoss = IsPreBoss,
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

        // ── Pre-solved cells (Thermo start, Arrow sums, etc.) ────────────────
        // Cells whose value was set by modifier geometry; player cannot overwrite them.
        // [NonSerialized] — rebuilt from the overlay on StartLevel.
        [System.NonSerialized]
        public HashSet<(int row, int col)> PresolvedCells = new HashSet<(int, int)>();

        // ── Fog-pending evaluation cells ──────────────────────────────────────
        // Cells the player placed a number in while covered by fog.
        // Correctness is deferred until the fog is removed from that cell.
        [System.NonSerialized]
        public HashSet<(int row, int col)> FogPendingCells = new HashSet<(int, int)>();

        // ── Digit completion tracking ─────────────────────────────────────────
        // Index 1..boardSize: how many of each digit have been correctly placed.
        // [NonSerialized] — rebuilt on resume from the board state.
        //
        // Current use: incremented in RunDirector on each correct placement.
        // Future use: per-digit XP breakdown on the end-of-puzzle summary screen
        // (e.g. "You placed 7× 'fours' — +14 XP"). NumpadViewController does NOT
        // read this array; it recounts directly from the board via UpdateButtonStates.
        [System.NonSerialized]
        public int[] CountPlaced;

        public void EnsureCountPlaced(int boardSize)
        {
            if (CountPlaced == null || CountPlaced.Length != boardSize + 1)
                CountPlaced = new int[boardSize + 1];
        }

        // ── Boss debuff: CellLock ──────────────────────────────────────────────
        // [REQ: DEBUFF-HOOK-009] CellLock state: Key = row*100+col; Value = placements until unlock.
        // [NonSerialized] because lock state is transient — does not survive a save/resume.
        [System.NonSerialized]
        public Dictionary<int, int> LockedCells = new Dictionary<int, int>();

        // ── Pressure mechanics (per-puzzle, all [NonSerialized]) ──────────────
        // [REQ: PRESSURE-SAVE-001] all fields NonSerialized — ephemeral per puzzle
        [System.NonSerialized]
        public List<PressureThreat> ActiveThreats = new List<PressureThreat>();   // [REQ: PRESSURE-TICK-001..009]

        /// <summary>(-1,-1) = no haunted cell this puzzle.</summary>
        [System.NonSerialized]
        public (int row, int col) HauntedCell = (-1, -1);                         // [REQ: PRESSURE-TICK-010..011]

        /// <summary>Key = row*100+col; Value = current fragility (decrements on each mistake).</summary>
        [System.NonSerialized]
        public Dictionary<int, int> CrumblingCells = new Dictionary<int, int>(); // [REQ: PRESSURE-TICK-012..015]

        [System.NonSerialized]
        public int WaveCounter;       // [REQ: PRESSURE-TICK-007] placements remaining until next wave spawn

        [System.NonSerialized]
        public int WaveSpawnCount;    // [REQ: PRESSURE-TICK-008,009] number of currently active wave-spawned threats

        [System.NonSerialized]
        public int WaveInterval;      // [REQ: PRESSURE-TICK-007] W: placements between waves (set at puzzle start)

        [System.NonSerialized]
        public int WaveThreatCounter; // [REQ: PRESSURE-TICK-007] per-threat countdown length for wave threats

        [System.NonSerialized]
        public int CrumblingStartFragility; // [REQ: PRESSURE-UI-007] initial fragility for crack-render normalisation

        [System.NonSerialized]
        public bool CrumblingVeryHigh; // [REQ: PRESSURE-TICK-013] VeryHigh intensity: only first cell erodes per mistake

        [System.NonSerialized]
        public List<(int row, int col)> ChainSuccessCells = new List<(int, int)>(); // [REQ: PRESSURE-UI-005] gold-flash cells

        [System.NonSerialized]
        public HashSet<(int row, int col)> CrumbledFlashCells = new HashSet<(int, int)>(); // [REQ: PRESSURE-UI-008] green-flash cells

        [System.NonSerialized]
        public HashSet<(int row, int col)> ExpiryFlashCells = new HashSet<(int, int)>(); // [REQ: PRESSURE-UI-004] red-flash on threat expiry

        [System.NonSerialized]
        public bool WaveRipplePending; // [REQ: PRESSURE-UI-009] triggers ripple animation on next render
    }

    /// <summary>
    /// Describes a single timed pressure threat active during a puzzle.
    /// Pressure threats are per-puzzle and never serialised.
    /// </summary>
    // [REQ: PRESSURE-TICK-001..009] core threat data consumed by RunDirector.TickPressureThreats
    public sealed class PressureThreat
    {
        public ThreatScope Scope;
        /// <summary>Empty cells that must be filled before RemainingPlacements reaches zero.</summary>
        public List<(int row, int col)> Cells = new List<(int, int)>();
        /// <summary>Decrements on every correct player placement. Reaching 0 with Cells non-empty triggers expiry.</summary>
        public int RemainingPlacements;
        /// <summary>Placements at creation time — used for ratio-based badge colour ramp.</summary>
        public int InitialPlacements; // [REQ: PRESSURE-UI-003]
        /// <summary>True for Chain-scope threats; a successful resolution awards +1 HP.</summary>
        public bool IsChain;         // [REQ: PRESSURE-TICK-004]
        /// <summary>True when spawned by the PressureWave mechanic (tracked against WaveSpawnCount).</summary>
        public bool IsWaveSpawned;   // [REQ: PRESSURE-TICK-009]
    }

    /// <summary>
    /// Snapshot passed from InRunController to BoardViewController on every render call.
    /// Contains all data needed to draw pressure overlays without exposing LevelState directly.
    /// </summary>
    // [REQ: PRESSURE-UI-001..011] render DTO — populated by InRunController.BuildPressureRenderData
    public struct PressureRenderData
    {
        public List<PressureThreat> ActiveThreats;      // [REQ: PRESSURE-UI-002..005]
        public (int row, int col) HauntedCell;           // [REQ: PRESSURE-UI-006]
        /// <summary>Key = row*100+col; Value = current fragility.</summary>
        public Dictionary<int, int> CrumblingCells;     // [REQ: PRESSURE-UI-007,008]
        /// <summary>Initial fragility used to normalise crack intensity (0 = invisible, maxFragility = full crack).</summary>
        public int CrumblingMaxFragility;
        /// <summary>True for one frame after a new PressureWave threat is spawned — triggers the ripple animation.</summary>
        public bool WaveRipplePending; // [REQ: PRESSURE-UI-009]
        /// <summary>Cells that just had a chain-success gold-flash this frame.</summary>
        public HashSet<(int row, int col)> ChainSuccessCells; // [REQ: PRESSURE-UI-005]
        /// <summary>Cells that just auto-filled a crumbled cell this frame — show green flash.</summary>
        public HashSet<(int row, int col)> CrumbledFlashCells; // [REQ: PRESSURE-UI-008]
        /// <summary>Cells whose threat just expired (unfilled cells) — show one-shot red flash.</summary>
        public HashSet<(int row, int col)> ExpiryFlashCells; // [REQ: PRESSURE-UI-004]
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
        public bool IsCrossLink;  // true for nodes that act as a lane-crossing anchor
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
        public int MinFloor; // 0 = any floor, 1 = floor 2+, 2 = floor 3+, etc. (0-indexed)
    }

    [Serializable]
    public sealed class EventOption
    {
        public string Label;
        public string EffectDescription;
        // Costs (deducted)
        public int GoldCost;
        public int HpCost;
        public int PencilCost;
        // Rewards (granted)
        public int GoldGain;
        public int HpGain;
        public int PencilGain;
        public int MaxHpGain;
        public bool GrantRandomItem;
        // Curse trade
        public string CurseId; // if set, applying this option grants this curse
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
        /// <summary>Player's current class level; set by GameBootstrap before calling StartRun.</summary>
        public int ClassLevel = 1;
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
        public int BossesDefeatedThisRun;  // boss puzzles cleared during this run (one per floor boss)
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
