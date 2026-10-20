using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: SCORE-GRADE-003] The puzzles' grades as stamps in the order played, grouped by floor (GameDesignSpec §9c):
    /// one colour and one localized letter per grade, shared by the run-end scroll and the text breakdown.
    /// </summary>
    internal static class RunGradeStamps
    {
        private static readonly Color PerfectColor = new Color(0.20f, 0.42f, 0.22f, 1f);
        private static readonly Color CleanColor = new Color(0.33f, 0.29f, 0.24f, 1f);
        private static readonly Color RoughColor = new Color(0.72f, 0.47f, 0.13f, 1f);
        private static readonly Color MessyColor = new Color(0.70f, 0.20f, 0.15f, 1f);

        internal static Color ColorOf(PuzzleGrade grade) => grade switch
        {
            PuzzleGrade.Perfect => PerfectColor,
            PuzzleGrade.Clean => CleanColor,
            PuzzleGrade.Rough => RoughColor,
            _ => MessyColor
        };

        internal static string MarkOf(PuzzleGrade grade) => LocalizationService.T($"InRun.End.Grade.{grade}Mark");

        internal static string NameOf(PuzzleGrade grade) => LocalizationService.T($"InRun.End.Grade.{grade}");

        /// <summary>The run's grades with their floors (-1 when unknown), from its analytics or rebuilt from the score.</summary>
        internal static List<(PuzzleGrade Grade, int Floor)> EntriesOf(RunResult result)
        {
            var entries = new List<(PuzzleGrade, int)>();
            var recorded = result?.Analytics?.PuzzleGrades;
            if (recorded != null && recorded.Count > 0)
            {
                foreach (var entry in recorded)
                    if (entry != null) entries.Add((entry.Grade, entry.Floor));
                return entries;
            }
            foreach (var grade in RunScoreBreakdown.For(result).Grades) entries.Add((grade, -1));
            return entries;
        }

        /// <summary>The stamps as letters, floors apart: "P C P | R P M".</summary>
        internal static string Line(RunResult result)
        {
            var entries = EntriesOf(result);
            var sb = new StringBuilder();
            for (var i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(NewFloor(entries, i) ? " | " : " ");
                sb.Append(MarkOf(entries[i].Grade));
            }
            return sb.ToString();
        }

        /// <summary>The legend: each grade's letter in its colour, then its name.</summary>
        internal static string Legend()
        {
            var sb = new StringBuilder();
            foreach (var grade in new[] { PuzzleGrade.Perfect, PuzzleGrade.Clean, PuzzleGrade.Rough, PuzzleGrade.Messy })
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(ColorOf(grade))}><b>{MarkOf(grade)}</b></color> {NameOf(grade)}");
            }
            return sb.ToString();
        }

        internal static bool NewFloor(List<(PuzzleGrade Grade, int Floor)> entries, int index) =>
            index > 0 && entries[index].Floor >= 0 && entries[index - 1].Floor >= 0 && entries[index].Floor != entries[index - 1].Floor;
    }
}
