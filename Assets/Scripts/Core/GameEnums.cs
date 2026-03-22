using System;

namespace SudokuRoguelike.Core
{
    public enum DifficultyTier
    {
        Diff1 = 1,
        Diff2 = 2,
        Diff3 = 3,
        Diff4 = 4,
        Diff5 = 5
    }

    public enum ItemType
    {
        // Tiered items (Normal / Rare / Epic variants)
        Solver,
        Finder,
        InkWell,
        MeditationStone,
        WindChime,
        PatternScroll,
        KoiReflection,
        LanternOfClarity,

        // Unique items (fixed rarity)
        GardenRake,        // Normal — highlights cells with only 2 candidates in row/col
        OfferingBowl,      // Normal — spend 5 HP to reveal correct number for one cell
        PruningShears,     // Normal — removes 1 impossible candidate from a 3x3 box
        ZenSandSifter,     // Normal — highlights Hidden Pairs in current row
        GinkgoLeaf,        // Rare  — highlights all instances of a chosen number until placed
        RicePaperUmbrella, // Rare  — protects HP from next 2 mistakes
        TempleIncense,     // Rare  — correct cells for a number pulse for 5 moves
        KoiDragonScale,    // Epic  — instantly completes the most-filled line or box
        GoldenKintsugiJar, // Epic  — highlights all current mistakes in red (no HP penalty)
        SilkFan            // Epic  — swap positions of two placed numbers
    }

    public enum ItemRarity
    {
        Normal,
        Rare,
        Epic
    }

    public enum ClassId
    {
        NumberFreak,
        GardenMonk,
        ShrineArchivist,
        KoiGambler,
        StoneGardener,
        LanternSeer,
        ReedDuelist,
        QuietCartographer
    }

    public enum RouteType
    {
        Bamboo,
        Lantern,
        KoiPond,
        StoneGarden,
        Blossom
    }

    public enum NodeType
    {
        Start,
        Puzzle,
        ElitePuzzle,
        Shop,
        Rest,
        Relic,
        Event,
        PreBoss,
        Boss,
        CrossLink
    }

    public enum GameMode
    {
        GardenRun,
        EndlessZen,
        SpiritTrials,
        Tutorial
    }

    public enum RunArchetype
    {
        Undefined,
        EconomyMerchantMonk,
        ModifierRuleBender,
        SurvivalEnduringSage,
        ComboFlowMaster
    }

    public enum RelicId
    {
        // Tier 1 — Minor Passives
        SmoothPebble,       // +5 Pencil at start of each puzzle
        CrackedTeacup,      // First wrong placement each puzzle costs 0 HP
        WoodenComb,          // Nothing-slot gold bonus doubled
        MossToken,           // Shop reroll costs reduced by 25%

        // Tier 2 — Noticeable Effects
        KoiReflectionRelic,  // +1 combo grace
        MonkCharm,           // Every 5 correct placements in a row grants +2 Gold
        CopperTortoise,      // +5 Gold bonus on no-mistake puzzle completion
        JadeHairpin,         // Reveal all candidates in one row (1 use/puzzle)
        StoneSundial,        // +1 reward slot on puzzle completion

        // Tier 3 — Build-Enabling
        SakuraSeal,          // Perfect puzzle → next puzzle starts with +3 HP
        CrimsonFan,          // Boss modifier intensity reduced one step
        WisteriaBranch,      // +2 HP at start of each floor
        PaperCrane,          // Skip one puzzle tile (2 uses/run)
        PorcelainMask,       // Elite tiles drop items one rarity tier higher

        // Tier 4 — Run-Defining
        TransmutedSigil,     // +1 Max HP, +3 Gold per puzzle completed
        PhoenixFeather,      // Prevent death once: full HP+Pencil restore (1 use/run)
        SpiritLantern,       // All shop items cost 20% less gold
        MoonstoneCompass,    // Relic nodes offer one tier higher

        // Legendary — Exceptional Named Relics
        GoldenRoot,          // Gold carries 50% interest between stages
        SilentGrid,          // +2 mistake shield charges per puzzle
        ShiftingGarden,      // Extra branching tile per floor
        EternalLotus,        // Items never consumed — infinite uses
        DragonsEye           // Reveal full solution for 3 seconds (1 use/floor)
    }

    public enum RelicTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4,
        Legendary
    }

    public enum CurseType
    {
        CursedRelicBacklash,
        LockedItemSlot,
        TemporaryBlindness,
        IncreasedMistakePenalty,
        MinorCurse
    }

    public enum EventCategory
    {
        Sacrifice,
        RiskAmplification,
        ResourceTrade
    }

    public enum AdaptationMutationType
    {
        None,
        SurvivalGetsCombo,
        EconomyGetsHpBoost,
        ModifierGetsUtilityDiscount,
        ComboGetsShield
    }

    public enum StressVariant
    {
        None,
        TimePressure,
        LimitedPencilMarks,
        LockedRows,
        GradualFogCreep
    }

    public enum TutorialResourceMode
    {
        Free,
        Simulation,
        ClassBased
    }

    public enum MenuScreen
    {
        Main,
        ModeSelect,
        ClassSelect,
        SeedSelect,
        TutorialSetup,
        TutorialProgress,
        MetaProgression,
        GameModes,
        Options,
        Credits,
        Pause,
        EndRun,
        Victory
    }

    public enum LanguageOption
    {
        English,
        German
    }

    public enum ClassComplexity
    {
        Low,
        Medium,
        High
    }

    public enum PlayerSkillBand
    {
        Beginner,
        Early,
        Intermediate,
        Adaptive,
        Advanced,
        Expert
    }

    public enum BossModifierId
    {
        FogOfWar,
        ArrowSums,
        GermanWhispers,
        DutchWhispers,
        ParityLines,
        RenbanLines,
        KillerCages,
        DifferenceKropki,
        RatioKropki,
        Palindrome,
        Thermo,
        BetweenLines,
        EvenOdd,
        Nonconsecutive,
        Antiknight
    }

    public enum BossModifierTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4,
        Tier5
    }

    /// <summary>Controls how many modifier elements (lines, dots, cages, arrows) appear in a puzzle.</summary>
    public enum BossModifierIntensity
    {
        Low,      // ~50% of normal element count
        Medium,   // 100% (default)
        High,     // ~150%
        VeryHigh  // ~200%
    }

    public enum SudokuTechnique
    {
        NakedSingle,
        HiddenSingle,
        NakedPair,
        PointingPair,
        BoxLineReduction,
        NakedTriples,
        XWing,
        Swordfish
    }

    public enum PuzzleDifficultyTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4
    }

    public enum MusicLayer
    {
        CalmGardenBase,
        Focus,
        Tension,
        BossPercussion
    }

    public enum ModifierBadgeTier
    {
        None,
        Bronze,
        Silver,
        Gold,
        Spirit
    }

    public enum RunSaveTrigger
    {
        Pause,
        Quit,
        BossPhaseTransition,
        ManualCheckpoint
    }

    public enum ConstraintRuleCategory
    {
        BaseSudoku = 0,
        Region = 1,
        GlobalNegative = 2,
        Line = 3,
        Dot = 4,
        Arithmetic = 5,
        CellLevel = 6,
        FogPostProcess = 7
    }

    public enum SpiritTrialsTier
    {
        Apprentice,
        Adept,
        Master,
        Grandmaster
    }

    public enum SpiritTrialsItemMode
    {
        Random,
        SolverStart
    }

    public enum AchievementTier
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert,
        Hidden
    }

    [Flags]
    public enum SelectionFlags
    {
        None = 0,
        Additive = 1,
        Subtractive = 2,
        Expand = 4,
        ToolSensitive = 8
    }
}
