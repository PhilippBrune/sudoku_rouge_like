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
        Solver,
        Finder,
        InkWell,
        MeditationStone,
        WindChime,
        PatternScroll,
        KoiReflection,
        LanternOfClarity,
        TeaOfFocus,
        CherryBlossomPact,
        FortuneEnvelope,
        StoneShift,
        HarmonyCharm,
        CompassOfOrder
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
        ZenMaster,
        GardenMonk,
        ShrineArchivist,
        KoiGambler,
        LanternSeer,
        StoneGardener,
        ChaosMonk
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
        Boss
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
        ComboFlowMaster,
        ChaosMonk
    }

    public enum RelicCategory
    {
        Economy,
        Survival,
        Modifier,
        Combo,
        Chaos,
        Utility
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
        RatioKropki
    }

    public enum BossModifierTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4,
        Tier5
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
        Line = 2,
        Dot = 3,
        Arithmetic = 4,
        FogPostProcess = 5
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
