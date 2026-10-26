using System;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// The Incense Walk (IncenseWalkMode.md): an incense stick is the clock, every solved puzzle adds time, every mistake
    /// costs time, and the walk ends at 0:00. The numbers are the concept's, set for playtesting and balancing.
    /// </summary>
    public sealed class IncenseWalkService
    {
        // [REQ: INCENSE-CLOCK-001] [REQ: INCENSE-CLOCK-002] [REQ: INCENSE-CLOCK-003]
        public const int StartSeconds = 480;
        public const int SecondsPerStar = 15;
        public const int PerfectSeconds = 20;
        public const int MistakeSeconds = 10;
        public const int BonusCutPerMistakePercent = 20;
        public const int MaxBonusCutPercent = 80;

        // [REQ: INCENSE-CLASS-001] The class passives in time form.
        public const int FreakComboDigits = 10, FreakComboSeconds = 5;
        public const int MonkDigits = 5, MonkSeconds = 3;
        public const int ArchivistSeconds = 2;
        public const double KoiFreeMistakeChance = 0.33;
        public const int StoneStartSeconds = 60;
        public const int ReedSeconds = 30;
        public const int CartographerGraceSeconds = 5;

        private readonly ClassId _classId;
        private readonly int _seed;
        private readonly Random _random;
        private int _combo, _digitsThisPuzzle, _mistakesTotal;
        private float _grace;

        public float SecondsLeft { get; private set; }
        public int PuzzlesSolved { get; private set; }
        public int Score { get; private set; }
        public int LargestBoard { get; private set; }
        public bool Over => SecondsLeft <= 0f;   // [REQ: INCENSE-CLOCK-004]

        public IncenseWalkService(ClassId classId, int seed)
        {
            _classId = classId;
            _seed = seed;
            _random = new Random(seed);
            SecondsLeft = StartSeconds + (classId == ClassId.StoneGardener ? StoneStartSeconds : 0);
        }

        /// <summary>[REQ: INCENSE-PUZZLE-001] The walk's n-th puzzle (0-based): boards and rules grow every three puzzles.</summary>
        public LevelConfig BuildLevel(int index)
        {
            int size, stars, rules;
            if (index < 3) { size = 5 + _random.Next(2); stars = 1 + _random.Next(2); rules = 0; }
            else if (index < 6) { size = 6 + _random.Next(2); stars = 2; rules = 1; }
            else if (index < 9) { size = 7 + _random.Next(2); stars = 2 + _random.Next(2); rules = 1 + _random.Next(2); }
            else { size = 8 + _random.Next(2); stars = 3; rules = 2; }

            var config = new LevelConfig
            {
                BoardSize = size,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                RegionVariant = _random.Next(2),
                Seed = _random.Next(),
                Difficulty = (DifficultyTier)Math.Min(1 + index / 3, 5),
                RequireUniqueModifierSolution = rules > 0
            };
            if (rules > 0)
                config.ActiveModifiers.AddRange(BossService.DrawCombinable(BossService.GetGeneratablePlayerModifiers(size), rules, _random));
            return config;
        }

        /// <summary>[REQ: INCENSE-CLOCK-002] [REQ: INCENSE-CLOCK-003] Seconds a solved puzzle adds: the board's base, +15 s
        /// per star above 1, cut by 20 % per mistake (at most 80 %), +20 s when perfect.</summary>
        public static int SolveSeconds(int size, int stars, int mistakes)
        {
            var baseSeconds = size switch { 5 => 60, 6 => 90, 7 => 120, 8 => 150, _ => 210 } + SecondsPerStar * Math.Max(0, stars - 1);
            var cut = Math.Min(MaxBonusCutPercent, BonusCutPerMistakePercent * mistakes);
            return baseSeconds * (100 - cut) / 100 + (mistakes == 0 ? PerfectSeconds : 0);
        }

        /// <summary>[REQ: INCENSE-SCORE-001] A solved puzzle's points: the board's XP points × the star multiplier.</summary>
        public static int PuzzlePoints(int size, int stars) =>
            (int)Math.Round(XpTable.BaseXpForBoardSize(size) * XpTable.StarMultiplier(stars), MidpointRounding.AwayFromZero);

        public void OnPuzzleStart()
        {
            _digitsThisPuzzle = 0;
            _grace = _classId == ClassId.QuietCartographer ? CartographerGraceSeconds : 0f;
        }

        /// <summary>The stick burns; the Quiet Cartographer's look at a new board comes off first.</summary>
        public void Tick(float seconds)
        {
            if (Over || seconds <= 0f) return;
            var fromGrace = Math.Min(_grace, seconds);
            _grace -= fromGrace;
            SecondsLeft = Math.Max(0f, SecondsLeft - (seconds - fromGrace));
        }

        /// <summary>A correct digit; returns the seconds its class passive added.</summary>
        public int OnCorrectDigit(bool cellHadPencilMarks)
        {
            if (Over) return 0;
            _combo++;
            _digitsThisPuzzle++;
            var gained = _classId switch
            {
                ClassId.NumberFreak when _combo % FreakComboDigits == 0 => FreakComboSeconds,
                ClassId.GardenMonk when _digitsThisPuzzle % MonkDigits == 0 => MonkSeconds,
                ClassId.ShrineArchivist when cellHadPencilMarks => ArchivistSeconds,
                _ => 0
            };
            SecondsLeft += gained;
            return gained;
        }

        /// <summary>A wrong digit costs 10 s at once; returns false when the Koi Gambler's luck let it pass.</summary>
        public bool OnMistake()
        {
            if (Over) return false;
            _combo = 0;
            _mistakesTotal++;
            if (_classId == ClassId.KoiGambler && new Random(_seed ^ _mistakesTotal).NextDouble() < KoiFreeMistakeChance)
                return false;
            SecondsLeft = Math.Max(0f, SecondsLeft - MistakeSeconds);
            return true;
        }

        /// <summary>[REQ: INCENSE-SCORE-001] A solved puzzle scores its points plus the seconds left at the solve, then adds
        /// its seconds. Returns the seconds added.</summary>
        public int OnPuzzleSolved(int size, int stars, int mistakes, bool usedPencil)
        {
            if (Over) return 0;
            Score += PuzzlePoints(size, stars) + (int)SecondsLeft;
            PuzzlesSolved++;
            LargestBoard = Math.Max(LargestBoard, size);
            var added = SolveSeconds(size, stars, mistakes)
                        + (_classId == ClassId.ReedDuelist && mistakes == 0 && !usedPencil ? ReedSeconds : 0);
            SecondsLeft += added;
            return added;
        }
    }
}
