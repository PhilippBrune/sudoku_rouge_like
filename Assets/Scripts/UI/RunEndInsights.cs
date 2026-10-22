using System.Collections.Generic;
using System.Linq;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// The lines under "This run" at the end of a Garden Run (one-more-run concept RM-111, RM-112).
    /// [REQ: RUN-END-NEAR-001] So close: where a lost run ended (a boss or a puzzle, how many cells were left) next to the
    /// best floor with this class. [REQ: RUN-END-MOVED-001] What this run moved: the unlocks it brought closer (the closest
    /// first), the Codex, and the next Harmony level, at most <see cref="MaxMoved"/> lines.
    /// </summary>
    public static class RunEndInsights
    {
        public const int MaxMoved = 3;

        /// <summary>The near-miss lines of a lost run (none for a win).</summary>
        public static List<string> SoClose(bool victory, int floorReached, bool atBoss, int cellsLeft, int bestFloorBefore,
            string className)
        {
            var lines = new List<string>();
            if (victory) return lines;
            if (cellsLeft > 0)
                lines.Add(atBoss
                    ? LocalizationService.FormatPlural(cellsLeft, "InRun.End.Near.BossOne", "InRun.End.Near.BossMany",
                        "So close: the Floor {1} boss, {0} cell from the end.",
                        "So close: the Floor {1} boss, {0} cells from the end.", floorReached)
                    : LocalizationService.FormatPlural(cellsLeft, "InRun.End.Near.PuzzleOne", "InRun.End.Near.PuzzleMany",
                        "So close: Floor {1}, {0} cell from the end.",
                        "So close: Floor {1}, {0} cells from the end.", floorReached));
            if (bestFloorBefore > 0 && floorReached > bestFloorBefore)
                lines.Add(LocalizationService.Format("InRun.End.Near.NewBest", "A new best floor with the {0}.", className));
            else if (bestFloorBefore > 0)
                lines.Add(LocalizationService.Format("InRun.End.Near.Best", "Your best with the {0}: Floor {1}.", className,
                    bestFloorBefore));
            return lines;
        }

        /// <summary>What the run moved, compared with the profile before it.</summary>
        public static List<string> Moved(MetaProgressionState before, MetaProgressionState after, RunState run, bool victory)
        {
            var lines = new List<string>();
            if (before == null || after == null) return lines;

            var harmony = HarmonyLine(before, after, run, out var harmonyOpened);
            if (harmonyOpened) lines.Add(harmony);

            var unlocks = new List<(float Left, string Line)>();
            foreach (var def in ClassCatalog.GetAll())
            {
                var was = ClassUnlockService.GetProgress(before, def.Id);
                var now = ClassUnlockService.GetProgress(after, def.Id);
                if (was.Unlocked || now.Unlocked || now.Current <= was.Current) continue;
                unlocks.Add((1f - now.Fraction, LocalizationService.Format("InRun.End.Moved.Unlock", "{0}: {1:N0} → {2:N0} of {3:N0}",
                    LocalizationService.T(def.Name, def.Name), was.Current, now.Current, now.Target)));
            }
            lines.AddRange(unlocks.OrderBy(u => u.Left).Select(u => u.Line));

            var codexBefore = CodexFound(before, out var total);
            var codexAfter = CodexFound(after, out _);
            if (codexAfter > codexBefore)
                lines.Add(LocalizationService.Format("InRun.End.Moved.Codex", "Codex: {0} → {1} of {2} found", codexBefore, codexAfter, total));

            if (!harmonyOpened && harmony != null) lines.Add(harmony);
            return lines.Take(MaxMoved).ToList();
        }

        // A Garden Run at the highest open Harmony: a win opens the next level, otherwise the line names what opens it.
        private static string HarmonyLine(MetaProgressionState before, MetaProgressionState after, RunState run, out bool opened)
        {
            opened = false;
            if (run == null || run.Mode != GameMode.GardenRun || run.IsSeasonalChallenge) return null;
            var max = before.MaxUnlockedHarmonyLevel;
            if (run.HarmonyLevel != max || max >= 10) return null;
            if (after.MaxUnlockedHarmonyLevel > max)
            {
                opened = true;
                return LocalizationService.Format("InRun.End.Moved.HarmonyOpen", "{0} is open.", HarmonyName(after.MaxUnlockedHarmonyLevel));
            }
            return LocalizationService.Format("InRun.End.Moved.HarmonyNext", "{0}: win a run on {1}.", HarmonyName(max + 1), HarmonyName(max));
        }

        private static string HarmonyName(int level) =>
            LocalizationService.T($"Harmony.Name.{level}", Run.HarmonyDifficultyService.GetDisplayName(level));

        private static int CodexFound(MetaProgressionState meta, out int total)
        {
            var counts = new[] { CodexProgress.Items(meta), CodexProgress.Relics(meta), CodexProgress.Rules(meta), CodexProgress.Curses(meta) };
            total = counts.Sum(c => c.Total);
            return counts.Sum(c => c.Found);
        }
    }
}
