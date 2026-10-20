using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public enum RunScoreLineKind
    {
        Perfect,
        Rough,
        Messy,
        Cursed,
        Victory,
        Gold
    }

    /// <summary>One row of the run score: how often it applied, its share of the base in percent and the points it adds.</summary>
    public readonly struct RunScoreLine
    {
        public readonly RunScoreLineKind Kind;
        public readonly int Count;
        public readonly int Percent;
        public readonly int Points;

        public RunScoreLine(RunScoreLineKind kind, int count, int percent, int points)
        {
            Kind = kind;
            Count = count;
            Percent = percent;
            Points = points;
        }
    }

    /// <summary>
    /// The run score as GameDesignSpec §9c defines it: FinalScore = BaseScore × (1 + Perfect + Cursed + Victory − Rough
    /// − Messy) + Gold. Every puzzle gets a grade from its own mistakes (SCORE-GRADE-001, owner pick C after the 0.2.0
    /// playtest): Perfect (0) +5 %, Clean (1) 0, Rough (2–3) −2 %, Messy (4+, and the puzzle that ended the run) −5 %, the
    /// penalty at most −50 % in all. Unspent gold adds a point per 5 gold, at most 15 % of the base (SCORE-GOLD-001).
    /// Each row is rounded to whole points and the final score is the base plus the rows, so a shown breakdown adds up
    /// (GAP-053).
    /// </summary>
    public sealed class RunScoreBreakdown
    {
        public const int PerfectPercent = 5;      // per Perfect puzzle (no mistake)
        public const int RoughPercent = 2;        // per Rough puzzle (2–3 mistakes)
        public const int MessyPercent = 5;        // per Messy puzzle (4+ mistakes, or the puzzle that ended the run)
        public const int MaxPenaltyPercent = 50;
        public const int CursedPercent = 10;      // per cursed puzzle accepted and completed
        public const int VictoryPercent = 25;     // the Floor 5 boss defeated
        public const int GoldPerPoint = 5;        // unspent gold: one point per 5 gold
        public const int MaxGoldPercent = 15;

        private readonly List<RunScoreLine> _lines;

        private RunScoreBreakdown(int baseXp, List<RunScoreLine> lines, int total, IReadOnlyList<PuzzleGrade> grades)
        {
            BaseXp = baseXp;
            _lines = lines;
            Total = total;
            Grades = grades;
        }

        public int BaseXp { get; }
        public IReadOnlyList<RunScoreLine> Lines => _lines;
        public int Total { get; }
        /// <summary>The puzzles' grades in the order played (the end screen's stamp row).</summary>
        public IReadOnlyList<PuzzleGrade> Grades { get; }

        /// <summary>[REQ: SCORE-GRADE-001] Perfect 0 mistakes, Clean 1, Rough 2–3, Messy 4 or more.</summary>
        public static PuzzleGrade GradeFor(int mistakes) =>
            mistakes <= 0 ? PuzzleGrade.Perfect
            : mistakes == 1 ? PuzzleGrade.Clean
            : mistakes <= 3 ? PuzzleGrade.Rough
            : PuzzleGrade.Messy;

        public static List<PuzzleGrade> GradesOf(IReadOnlyList<PuzzleGradeEntry> entries)
        {
            var grades = new List<PuzzleGrade>(entries?.Count ?? 0);
            if (entries == null) return grades;
            foreach (var entry in entries)
                if (entry != null) grades.Add(entry.Grade);
            return grades;
        }

        public static RunScoreBreakdown Calculate(int baseXp, IReadOnlyList<PuzzleGrade> grades, int cursedCompleted,
            bool victory, int goldLeft)
        {
            baseXp = Math.Max(0, baseXp);
            grades ??= Array.Empty<PuzzleGrade>();
            int perfect = 0, rough = 0, messy = 0;
            foreach (var grade in grades)
            {
                if (grade == PuzzleGrade.Perfect) perfect++;
                else if (grade == PuzzleGrade.Rough) rough++;
                else if (grade == PuzzleGrade.Messy) messy++;
            }

            var lines = new List<RunScoreLine>();
            int Share(int percent) => (int)Math.Round(baseXp * percent / 100.0, MidpointRounding.AwayFromZero);
            void Add(RunScoreLineKind kind, int count, int percent)
            {
                if (count <= 0 || percent == 0) return;
                lines.Add(new RunScoreLine(kind, count, percent, Share(percent)));
            }

            // [REQ: SCORE-GRADE-004] Rough and Messy together cost at most half the base.
            var roughPercent = Math.Min(MaxPenaltyPercent, rough * RoughPercent);
            var messyPercent = Math.Min(MaxPenaltyPercent - roughPercent, messy * MessyPercent);
            Add(RunScoreLineKind.Perfect, perfect, perfect * PerfectPercent);
            Add(RunScoreLineKind.Rough, rough, -roughPercent);
            Add(RunScoreLineKind.Messy, messy, -messyPercent);
            Add(RunScoreLineKind.Cursed, cursedCompleted, cursedCompleted * CursedPercent);
            Add(RunScoreLineKind.Victory, victory ? 1 : 0, VictoryPercent);

            // [REQ: SCORE-GOLD-001] Gold is counted in points, not as a share: the percent is only shown. The cap rounds
            // down, so it never passes 15 %.
            goldLeft = Math.Max(0, goldLeft);
            var goldPoints = Math.Min(goldLeft / GoldPerPoint, baseXp * MaxGoldPercent / 100);
            if (goldPoints > 0)
                lines.Add(new RunScoreLine(RunScoreLineKind.Gold, goldLeft,
                    (int)Math.Round(goldPoints * 100.0 / baseXp, MidpointRounding.AwayFromZero), goldPoints));

            var total = baseXp;
            foreach (var line in lines) total += line.Points;
            return new RunScoreBreakdown(baseXp, lines, Math.Max(0, total), grades);
        }

        /// <summary>The breakdown a finished run was scored with (its analytics), or one rebuilt from the result.</summary>
        public static RunScoreBreakdown For(RunResult result)
        {
            if (result == null) return Calculate(0, null, 0, false, 0);
            var baseXp = 0;
            foreach (var tile in result.TileXpEntries) baseXp += tile.TotalXp;
            var a = result.Analytics;
            // Analytics built by the run carry the base XP and the grades; one without them (made elsewhere, or older) is
            // rebuilt from the result, which knows only how many puzzles were perfect.
            if (a != null && (a.BaseXp > 0 || baseXp == 0))
                return Calculate(a.BaseXp, GradesOf(a), a.CursedPuzzlesCompleted, a.RunCompleted, a.GoldLeft);
            return Calculate(baseXp, PerfectOnly(result.PerfectPuzzleCount), result.CursedPuzzlesAccepted, result.Victory, 0);
        }

        private static List<PuzzleGrade> GradesOf(PostRunAnalytics a)
        {
            if (a.PuzzleGrades != null && a.PuzzleGrades.Count > 0) return GradesOf(a.PuzzleGrades);
            var grades = new List<PuzzleGrade>();
            if (a.MistakesPerPuzzle != null)
                foreach (var mistakes in a.MistakesPerPuzzle) grades.Add(GradeFor(mistakes));
            return grades;
        }

        private static List<PuzzleGrade> PerfectOnly(int count)
        {
            var grades = new List<PuzzleGrade>();
            for (var i = 0; i < count; i++) grades.Add(PuzzleGrade.Perfect);
            return grades;
        }
    }
}
