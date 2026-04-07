namespace SudokuRoguelike.Core
{
    public enum ClassId
    {
        NumberFreak = 1,
        GardenMonk = 2,
        ShrineArchivist = 3,
        KoiGambler = 4,
        StoneGardener = 5,
        LanternSeer = 6,
        ReedDuelist = 7,
        QuietCartographer = 8
    }

    public enum GameMode
    {
        GardenRun,
        Tutorial,
        EndlessZen,
        SpiritTrials
    }

    public enum BossModifierId
    {
        // ── Active pool (1–15, fully implemented) ──
        FogOfWar = 0,
        ArrowSums = 1,
        GermanWhispers = 2,
        DutchWhispers = 3,
        ParityLines = 4,
        RenbanLines = 5,
        KillerCages = 6,
        DifferenceKropki = 7,
        RatioKropki = 8,
        Palindrome = 9,
        Thermo = 10,
        BetweenLines = 11,
        EvenOdd = 12,
        Nonconsecutive = 13,
        Antiknight = 14,

        // ── Extended pool (16–30, newly implemented) ──
        Antiking = 15,            // Global: no equal digit a king's move apart
        AntiBishop = 16,          // Global: no equal digit on same diagonal
        NonconsecDiagonal = 17,   // Global: diagonally adjacent cells can't be consecutive
        DistanceGe2 = 18,         // Global: equal digits must be Chebyshev distance ≥ 2
        EntropyGlobal = 19,       // Global: every 3 consecutive row/col cells contain low/mid/high
        ModularRegions = 20,      // Global: each region contains ≥1 from {1-3}, {4-6}, {7-9}
        ConsecutiveLine = 21,     // Line: adjacent cells differ by exactly 1
        SlowThermo = 22,          // Line: non-strict increase from bulb (≥)
        UniqueSetLine = 23,       // Line: no digit repeats on line
        FullKropki = 24,          // Dot: all valid Kropki pairs shown; absence = neither relation
        SumKropki = 25,           // Dot: labelled dot — two cells sum to shown value
        GreaterLessThan = 26,     // Pair: chevron between cells indicating relative order
        XVPairs = 27,             // Pair: X=sum10, V=sum5 between cells
        PrimeCells = 28,          // Cell: marked cells must contain prime digits (2,3,5,7)
        FortressCells = 29,        // Cell: shaded cells must be > all orthogonal non-shaded neighbors

        // ── Catalog batch 2 (IDs 30-94, newly implemented) ──
        XSudoku = 30,              // Global: main diagonals are extra no-repeat regions
        HyperSudoku = 31,          // Region: 4 overlapping 3×3 extra regions
        DisjointGroups = 32,       // Global: same relative box position can't share a digit
        IndexParity = 33,          // Global: cells where (row+col) is even share parity class
        ModularGlobal = 34,        // Global: cells in same residue class mod 3 can't repeat
        KnightKingCombined = 35,   // Global: combines Antiknight + Antiking
        Toroidal = 36,             // Global: rows/cols wrap around; edges are adjacent
        WhisperGeneralized = 37,   // Line: adjacent cells differ by ≥ K (K stored in LineValue)
        NonconsecLine = 38,        // Line: no two adjacent cells on line are consecutive
        AlternatingParityLine = 39,// Line: cells alternate strictly odd/even
        HighLowAlternating = 40,   // Line: cells alternate above/below midpoint
        NabnerLine = 41,           // Line: no repeats AND no two consecutive values on line
        EntropicLine = 42,         // Line: every 3 consecutive cells = one low/mid/high each
        ModularLine = 43,          // Line: every 3 consecutive cells differ in mod-3 residue
        ZipperLine = 44,           // Line: pairs equidistant from centre sum to same total
        RegionSumLine = 45,        // Line: each box-crossing segment sums to same value
        NLine = 46,                // Line: each box segment sums to LineValue (default 10)
        IndexLine = 47,            // Line: digit at 1-indexed position P equals P
        SumLine = 48,              // Line: entire line sums to LineValue
        SkyscraperLine = 49,       // Line: digit at each end encodes visible "buildings"
        ThermoLoop = 50,           // Line: closed-loop non-strict increase (≥) cyclically
        FastThermo = 51,           // Line: each cell ≥ previous + LineValue (default 2)
        AmbiguousThermo = 52,      // Line: direction unknown; enforced from bulb end
        ExtraRegion = 53,          // Region: additional arbitrary no-repeat region
        RegionSumEquality = 54,    // Region: all standard regions must share same sum
        RegionParityBalance = 55,  // Region: each region has equal odd and even digits
        RegionIndexRule = 56,      // Region: a cell's digit = count of a property in its region
        ConnectedRegions = 57,     // Region: every digit's occurrences are orthogonally connected
        FullWhiteKropki = 58,      // Dot: all diff=1 pairs shown; absence forbids diff=1
        FullBlackKropki = 59,      // Dot: all ratio=1:2 pairs shown; absence forbids ratio=1:2
        DifferenceK = 60,          // Pair: adjacent cells must differ by ≥ K (K=3 default)
        RatioN = 61,               // Pair: one adjacent cell is N× the other (N=3 default)
        InequalityChains = 62,     // Pair: >/< between consecutive cells in every row and col
        DistancePair = 63,         // Pair: |A−B| equals taxicab distance between paired cells
        KillerHiddenSum = 64,      // Cage: cells must have no repeats and sum correctly; sum hidden
        CageProduct = 65,          // Cage: cells multiply to displayed product; no repeats
        CageDifference = 66,       // Cage: two-cell cage; |A−B| = displayed value
        CageRatio = 67,            // Cage: two-cell cage; larger/smaller = displayed ratio
        MagicSquareRegion = 68,    // Region: a 3×3 sub-region satisfies magic-square rules
        RenbanCage = 69,           // Cage: cage digits form a consecutive set
        ArrowAverage = 70,         // Arrow: circle digit = average of arrow path digits
        ArrowProduct = 71,         // Arrow: circle digit = product of arrow path digits
        PillArrow = 72,            // Arrow: two-cell pill bulb forms two-digit number = path sum
        DoubleArrow = 73,          // Arrow: path sum = sum of both circle endpoint digits
        MultipleCells = 74,        // Cell: marked cells must contain a multiple of N (N=3 default)
        IndexCell = 75,            // Cell: a cell's digit = count of given cells in its row
        MirrorCells = 76,          // Cell: paired cells must contain the same digit
        OppositeCellsSum = 77,     // Cell: cells symmetric about grid centre sum to constant
        CountingCircles = 78,      // Cell: a circled cell's digit = count of circles matching that digit
        Quadruples = 79,           // Corner: all listed digits appear in the 4 surrounding cells
        DistanceCell = 80,         // Cell: a cell's digit = its taxicab distance to a target cell
        Skyscrapers = 81,          // Outside: clue = count of strictly-increasing "buildings" visible
        Sandwich = 82,             // Outside: clue = sum of digits strictly between two sentinels
        XSums = 83,                // Outside: first digit = X; clue = sum of first X digits
        LittleKiller = 84,         // Outside: diagonal arrow; clue = sum along that diagonal
        Battlefield = 85,          // Outside: clue = total length of consecutive runs in row/col
        OutsideInequalities = 86,  // Outside: edge clue requires first cell > or < reference
        KnightPath = 87,           // Path: digits 1–N placed so consecutive values are knight's move apart
        HamiltonianPath = 88,      // Path: numbered path through all cells; adjacent path cells consecutive
        SnakeConstraint = 89,      // Path: a defined digit sequence forms a connected snake
        LoopConstraint = 90,       // Path: specified digits collectively form a single closed loop
        DigitGraph = 91,           // Meta: a graph over digits; adjacent-in-graph digits obey positional rule
        SelfReferentialGrid = 92,  // Meta: a cell's digit encodes a count/property of the whole grid
        ConstraintNegationZone = 93// Meta: cells inside a zone invert one active modifier rule
    }

    public enum BossModifierIntensity
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum LineType
    {
        GermanWhispers,
        DutchWhispers,
        ParityLine,
        RenbanLine,
        Palindrome,
        Thermo,
        BetweenLine,
        ConsecutiveLine,   // adjacent cells differ by exactly 1
        SlowThermo,        // non-strict increase (≥) from bulb
        UniqueSetLine,     // no digit repeats on line
        WhisperGeneralized,  // adjacent differ by ≥ LineValue
        NonconsecLine,       // no consecutive adjacent pairs
        AlternatingParity,   // strict odd/even alternation
        HighLowAlternating,  // alternate above/below midpoint
        NabnerLine,          // no repeats, no consecutive
        EntropicLine,        // every 3 consecutive = low/mid/high
        ModularLine,         // every 3 consecutive differ mod 3
        ZipperLine,          // equidistant pairs sum to same
        RegionSumLine,       // box segments sum equally
        NLine,               // box segments each sum to LineValue
        IndexLine,           // digit at position P = P
        SumLine,             // whole line sums to LineValue
        SkyscraperLine,      // endpoint encodes visible count
        ThermoLoop,          // closed-loop non-strict increase
        FastThermo,          // each cell ≥ prev + LineValue
        AmbiguousThermo      // direction hidden; enforce from first cell
    }

    public enum MarkerType
    {
        Even,
        Odd,
        Prime,    // cell must contain a prime digit (2, 3, 5, or 7)
        Fortress, // cell must be > all orthogonally adjacent non-fortress cells
        Multiple,        // cell must contain a multiple of N
        IndexCellRow,    // cell digit = count of givens in its row
        MirrorA,         // first of a mirror pair (same digit as MirrorB partner)
        MirrorB,         // second of a mirror pair
        OppositeSum,     // cell digit + symmetric partner = constant
        CountingCircle,  // digit = count of circles matching that digit
        DistanceCellTarget, // this cell is the target for DistanceCell rule
        DistanceCellSource  // this cell's digit = taxicab distance to target
    }

    public enum PairConstraintType
    {
        GreaterThan,  // CellA > CellB
        LessThan,     // CellA < CellB
        SumX,         // CellA + CellB = 10
        SumV,         // CellA + CellB = 5
        SumK,         // CellA + CellB = Value field
        DifferenceK,   // |A−B| ≥ Value (Value = K)
        RatioN,        // max(A,B)/min(A,B) = Value (Value = N)
        GreaterChain,  // A > B in a consecutive-cell chain (row/col)
        LesserChain    // A < B in a consecutive-cell chain
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

    public enum ItemRarity
    {
        Normal,
        Rare,
        Epic
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
        GardenRake,
        OfferingBowl,
        PruningShears,
        ZenSandSifter,
        GinkgoLeaf,
        RicePaperUmbrella,
        TempleIncense,
        KoiDragonScale,
        GoldenKintsugiJar,
        SilkFan
    }

    public enum RelicId
    {
        SmoothPebble,
        CrackedTeacup,
        WoodenComb,
        MossToken,
        KoiReflectionRelic,
        MonkCharm,
        CopperTortoise,
        JadeHairpin,
        StoneSundial,
        SakuraSeal,
        CrimsonFan,
        WisteriaBranch,
        PaperCrane,
        PorcelainMask,
        TransmutedSigil,
        PhoenixFeather,
        SpiritLantern,
        MoonstoneCompass,
        GoldenRoot,
        SilentGrid,
        ShiftingGarden,
        EternalLotus,
        DragonsEye
    }

    public enum RelicTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4,
        Legendary
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
        CrossLink,
        Cursed   // Optional-risk node: accept to play with +1 stacked modifier for +50% gold/XP
    }

    public enum PositiveFloorEffect
    {
        None = 0,
        Bounty,       // All puzzles on floor grant +40% gold
        LuckyItems,   // All item reward screens get +1 extra slot
        PencilRefill, // Each completed puzzle restores +2 pencil
        HealingPath   // Each mistake-free puzzle restores +1 HP
    }

    public enum RouteType
    {
        CalmRoute,
        RiskRoute
    }

    public enum DifficultyTier
    {
        Diff1 = 1,
        Diff2 = 2,
        Diff3 = 3,
        Diff4 = 4,
        Diff5 = 5
    }

    public enum SpiritTrialsTier
    {
        Apprentice,
        Adept,
        Master,
        Grandmaster
    }

    public enum TutorialResourceMode
    {
        Free,
        Simulation
    }

    public enum MenuScreen
    {
        MainMenu,
        ClassSelect,
        TutorialSetup,
        TutorialProgress,
        GameModes,
        MetaProgression,
        Items,
        Options,
        Credits,
        SaveConflict,
        ConfirmQuit,
        ConfirmDeleteSave,
        Onboarding
    }

    public enum AppScreen
    {
        Menu,
        Game,
        EndScreen
    }

    public enum LanguageOption
    {
        English,
        German
    }

    public enum RunSaveTrigger
    {
        LevelComplete,
        ShopExit,
        EventResolved,
        FloorTransition
    }

    public enum RunArchetype
    {
        Undefined,
        EconomyMerchantMonk,
        ModifierRuleBender,
        SurvivalEnduringSage,
        ComboFlowMaster
    }

    public enum EventCategory
    {
        Sacrifice,
        RiskAmplification,
        ResourceTrade
    }

    public enum BossModifierTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4,
        Tier5
    }

    public enum PuzzleDifficultyTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4
    }

    public enum KillerCageType
    {
        Sum,         // standard: cells sum to Sum field
        HiddenSum,   // cells sum correctly but Sum is not shown to player
        Product,     // cells multiply to Sum field (reused as product)
        Difference,  // two-cell: |A−B| = Sum field
        Ratio,       // two-cell: larger/smaller = Sum field (as int ratio)
        RenbanSet,   // cells form a consecutive set (no sum restriction)
        MagicSquare  // 3×3 subregion satisfies magic-square rule
    }

    public enum ArrowType
    {
        Sum,          // standard: circle = sum of arrow path
        Average,      // circle = average of arrow path
        Product,      // circle = product of arrow path
        Pill,         // two-cell bulb = path sum (two-digit number)
        DoubleEndpoint// path sum = sum of both endpoint circles
    }

    public enum OutsideClueEdge
    {
        Top,
        Bottom,
        Left,
        Right,
        DiagonalDownRight,  // for LittleKiller
        DiagonalDownLeft
    }

    public enum OutsideClueType
    {
        Skyscrapers,
        Sandwich,
        XSums,
        LittleKiller,
        Battlefield,
        Inequality // edge cell > or < reference value (Value field)
    }
}
