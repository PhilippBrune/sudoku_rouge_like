using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public enum RunScoreLineKind
    {
        Perfect,
        Cursed,
        Victory,
        Mistakes
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
    /// The run score as GameDesignSpec §9c defines it: FinalScore = BaseScore × (1 + Perfect + Cursed + Victory + Mistakes),
    /// the bonuses and the penalty added, not multiplied one after another (GAP-053). Each row is its percentage of the
    /// base, rounded to whole points, and the final score is the base plus the rows, so a shown breakdown adds up.
    /// </summary>
    public sealed class RunScoreBreakdown
    {
        public const int PerfectPercent = 5;      // per puzzle without a mistake
        public const int CursedPercent = 10;      // per cursed puzzle accepted and completed
        public const int VictoryPercent = 25;     // the Floor 5 boss defeated
        public const int MistakePercent = 2;      // per mistake
        public const int MaxMistakePercent = 50;

        private readonly List<RunScoreLine> _lines;

        private RunScoreBreakdown(int baseXp, List<RunScoreLine> lines, int total)
        {
            BaseXp = baseXp;
            _lines = lines;
            Total = total;
        }

        public int BaseXp { get; }
        public IReadOnlyList<RunScoreLine> Lines => _lines;
        public int Total { get; }

        public static RunScoreBreakdown Calculate(int baseXp, int perfectPuzzles, int cursedCompleted, int mistakes, bool victory)
        {
            baseXp = Math.Max(0, baseXp);
            var lines = new List<RunScoreLine>();
            void Add(RunScoreLineKind kind, int count, int percent)
            {
                if (count <= 0 || percent == 0) return;
                lines.Add(new RunScoreLine(kind, count, percent,
                    (int)Math.Round(baseXp * percent / 100.0, MidpointRounding.AwayFromZero)));
            }

            Add(RunScoreLineKind.Perfect, perfectPuzzles, perfectPuzzles * PerfectPercent);
            Add(RunScoreLineKind.Cursed, cursedCompleted, cursedCompleted * CursedPercent);
            Add(RunScoreLineKind.Victory, victory ? 1 : 0, VictoryPercent);
            Add(RunScoreLineKind.Mistakes, mistakes, -Math.Min(MaxMistakePercent, mistakes * MistakePercent));

            var total = baseXp;
            foreach (var line in lines) total += line.Points;
            return new RunScoreBreakdown(baseXp, lines, Math.Max(0, total));
        }

        /// <summary>The breakdown a finished run was scored with (its analytics), or one rebuilt from the result.</summary>
        public static RunScoreBreakdown For(RunResult result)
        {
            if (result == null) return Calculate(0, 0, 0, 0, false);
            var baseXp = 0;
            foreach (var tile in result.TileXpEntries) baseXp += tile.TotalXp;
            var a = result.Analytics;
            // Analytics built by the run carry the base XP; one without it (made elsewhere, or older) is rebuilt from the result.
            if (a != null && (a.BaseXp > 0 || baseXp == 0))
                return Calculate(a.BaseXp, a.PerfectPuzzleCount, a.CursedPuzzlesCompleted, a.TotalMistakes, a.RunCompleted);
            return Calculate(baseXp, result.PerfectPuzzleCount, result.CursedPuzzlesAccepted, result.MistakesMade, result.Victory);
        }
    }
}
