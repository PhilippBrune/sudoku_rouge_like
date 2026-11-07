using System;
using System.Collections.Generic;
using System.Globalization;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [REQ: CLASS-CHALLENGE-001] [REQ: CLASS-CHALLENGE-002] Three challenges per class (RM-116, owner's pick 2026-11-03):
    /// the class's own, a win in under 40 minutes and a win at Harmony VI. A Garden Run with the class meets them (not the
    /// Daily Walk); each met one is a seal on the class's plaque and a Steam achievement ("challenge_&lt;id&gt;").
    /// </summary>
    public static class ClassChallenges
    {
        public const int PerClass = 3;
        public const float SwiftSeconds = 40 * 60f;
        public const float SwiftSecondsThreeFloors = 25 * 60f;   // [REQ: RUN-FLOORS-001]
        public const int HighHarmony = 6;

        /// <summary>The challenge's id: the class's icon name and 1 to 3 ("number_freak_1").</summary>
        public static string Id(ClassId classId, int index) => ClassCatalog.GetIconName(classId) + "_" + (index + 1);

        public static string AchievementId(ClassId classId, int index) => "challenge_" + Id(classId, index);

        public static IEnumerable<string> AllAchievementIds()
        {
            foreach (ClassId classId in Enum.GetValues(typeof(ClassId)))
                for (var i = 0; i < PerClass; i++)
                    yield return AchievementId(classId, i);
        }

        /// <summary>Whether this run meets the class's challenge <paramref name="index"/> (0: the class's own).</summary>
        public static bool Meets(ClassId classId, int index, RunResult result, RunState state)
        {
            if (result == null || result.Mode != GameMode.GardenRun || result.DailyWalk || result.TutorialMode
                || result.PlayedClassId != classId) return false;
            switch (index)
            {
                case 1: return result.Victory && state != null && state.ActiveSeconds > 0f
                    && state.ActiveSeconds < (state.ThreeFloors ? SwiftSecondsThreeFloors : SwiftSeconds);
                case 2: return result.Victory && result.HarmonyLevel >= HighHarmony;
            }
            return classId switch
            {
                ClassId.NumberFreak => result.Victory && result.PerfectPuzzleCount >= 10,
                ClassId.GardenMonk => result.Victory && result.ItemsUsedThisRun == 0,
                ClassId.ShrineArchivist => result.RelicsCollectedThisRun >= 4,
                ClassId.KoiGambler => result.Victory && result.CursedPuzzlesAccepted >= 3,
                ClassId.StoneGardener => result.Victory && result.ItemsUsedThisRun >= 8,
                ClassId.LanternSeer => state != null && state.MostBossRulesCleared >= 3,
                ClassId.ReedDuelist => state != null && state.CleanFloors >= 1,
                ClassId.QuietCartographer => result.Victory && RiskTiles(state) >= 6,
                _ => false
            };
        }

        /// <summary>Risk-path tiles taken in the run.</summary>
        public static int RiskTiles(RunState state)
        {
            var count = 0;
            if (state?.RouteHistory != null)
                foreach (var route in state.RouteHistory) if (route == (int)RouteType.RiskRoute) count++;
            return count;
        }

        /// <summary>Counts a solved puzzle: a pencil mark or HP lost marks the floor, and at a boss the most player rules it
        /// brought and whether the floor stayed quiet (a heal later does not undo a loss); the next floor starts clean.</summary>
        public static void OnPuzzleSolved(RunState state, LevelState level, LevelConfig config)
        {
            if (state == null || level == null || config == null) return;
            if (!level.NoPencilUsed || level.HpLost > 0) state.FloorMarked = true;
            if (!config.IsBoss || state.IsSeasonalChallenge) return;
            var playerRules = 0;
            foreach (var mod in config.ActiveModifiers) if ((int)mod < Sudoku.ModifierPresence.FirstSystemId) playerRules++;
            state.MostBossRulesCleared = Math.Max(state.MostBossRulesCleared, playerRules);
            if (!state.FloorMarked) state.CleanFloors++;
            state.FloorMarked = false;
        }

        public static bool IsMet(MetaProgressionState meta, ClassId classId, int index) => MetOn(meta, classId, index) != null;

        /// <summary>The day the challenge was met (yyyy-MM-dd), or null.</summary>
        public static string MetOn(MetaProgressionState meta, ClassId classId, int index)
        {
            var id = Id(classId, index);
            if (meta?.ClassChallenges != null)
                foreach (var entry in meta.ClassChallenges)
                    if (entry.Id == id) return entry.Date ?? string.Empty;
            return null;
        }

        public static int MetCount(MetaProgressionState meta, ClassId classId)
        {
            var count = 0;
            for (var i = 0; i < PerClass; i++) if (IsMet(meta, classId, i)) count++;
            return count;
        }

        /// <summary>Keeps every challenge this run met for the first time; returns their indices.</summary>
        public static List<int> Record(MetaProgressionState meta, RunResult result, RunState state, DateTime today)
        {
            var met = new List<int>();
            if (meta == null || result == null) return met;
            meta.ClassChallenges ??= new List<ClassChallengeEntry>();
            for (var i = 0; i < PerClass; i++)
            {
                if (IsMet(meta, result.PlayedClassId, i) || !Meets(result.PlayedClassId, i, result, state)) continue;
                meta.ClassChallenges.Add(new ClassChallengeEntry
                    { Id = Id(result.PlayedClassId, i), Date = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
                met.Add(i);
            }
            return met;
        }

        // The texts: the class's own challenge has its own key; the other two are shared.
        private static string Key(ClassId classId, int index) =>
            index == 0 ? $"ClassChallenge.{classId}" : index == 1 ? "ClassChallenge.Swift" : "ClassChallenge.High";

        public static string Name(ClassId classId, int index) =>
            LocalizationService.T(Key(classId, index) + ".Name", FallbackName(classId, index));

        public static string Description(ClassId classId, int index) =>
            LocalizationService.T(Key(classId, index) + ".Desc", FallbackDescription(classId, index));

        private static string FallbackName(ClassId classId, int index) => index == 1 ? "The Swift Path" : index == 2 ? "The High Path" : classId switch
        {
            ClassId.NumberFreak => "Ten Clean Counts",
            ClassId.GardenMonk => "Empty Hands, Full Heart",
            ClassId.ShrineArchivist => "The Archive of Four",
            ClassId.KoiGambler => "Three Curses Bet",
            ClassId.StoneGardener => "The Busy Rake",
            ClassId.LanternSeer => "Three Lights at Once",
            ClassId.ReedDuelist => "A Quiet Floor",
            _ => "Six Steps of Risk"
        };

        private static string FallbackDescription(ClassId classId, int index) =>
            index == 1 ? "Win a Garden Run in under 25 minutes." : index == 2 ? "Win a Garden Run on Harmony VI or higher." : classId switch
            {
                ClassId.NumberFreak => "Win a Garden Run with 10 perfect puzzles.",
                ClassId.GardenMonk => "Win a Garden Run without using an item.",
                ClassId.ShrineArchivist => "End a Garden Run holding 4 relics.",
                ClassId.KoiGambler => "Win a Garden Run with 3 cursed puzzles finished.",
                ClassId.StoneGardener => "Win a Garden Run using 8 items.",
                ClassId.LanternSeer => "Defeat a boss that brings 3 rules at once.",
                ClassId.ReedDuelist => "Clear a floor without a pencil mark and without losing HP.",
                _ => "Win a Garden Run taking 6 tiles of the risk path."
            };
    }
}
