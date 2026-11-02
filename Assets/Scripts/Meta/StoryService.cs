using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [REQ: STORY-CHAPTER-001] The story of the Nine (narrative-direction.md, owner's answers 2026-11-02): the eight classes
    /// are the eight keepers who stayed; each has five chapters, told by a secret voice, opened by that class's milestones
    /// in a Garden Run: chapter 1 from the start, 2 a warden defeated, 3 Floor 3 reached, 4 a win, 5 a win at Harmony III.
    /// [REQ: STORY-NINTH-001] The last chapter opens after a win with every keeper: the voice is the ninth keeper, and so is
    /// the player. [REQ: STORY-WARDEN-001] The five wardens have a name and a line for each keeper at the gate, and a parting
    /// line at the offering table.
    /// </summary>
    public static class StoryService
    {
        public const int Chapters = 5;
        public const int HarmonyForLastChapter = 3;
        public const int Wardens = 5;

        public static readonly ClassId[] Keepers =
        {
            ClassId.NumberFreak, ClassId.GardenMonk, ClassId.ShrineArchivist, ClassId.KoiGambler,
            ClassId.StoneGardener, ClassId.LanternSeer, ClassId.ReedDuelist, ClassId.QuietCartographer,
        };

        /// <summary>The highest chapter the keeper has opened; chapter 1 is open from the start.</summary>
        public static int ChapterOf(MetaProgressionState meta, ClassId keeper)
        {
            if (meta?.StoryChapters != null)
                foreach (var entry in meta.StoryChapters)
                    if (entry.ClassId == keeper) return System.Math.Clamp(entry.Chapter, 1, Chapters);
            return 1;
        }

        public static bool NinthOpened(MetaProgressionState meta) => meta?.StoryNinthOpened == true;

        /// <summary>[REQ: STORY-SHOW-001] A run that tells the story: a Garden Run with a keeper (no Daily Walk, Custom Puzzle
        /// or other mode).</summary>
        public static bool Tells(RunState state) =>
            state != null && state.Mode == GameMode.GardenRun && !state.DailyWalk && !state.TutorialMode
            && System.Array.IndexOf(Keepers, state.ClassId) >= 0;

        /// <summary>Only a Garden Run with a class tells the story (no Daily Walk, Custom Puzzle or other mode).</summary>
        public static bool Counts(RunResult result) =>
            result != null && result.Mode == GameMode.GardenRun && !result.DailyWalk && !result.TutorialMode
            && System.Array.IndexOf(Keepers, result.PlayedClassId) >= 0;

        /// <summary>The chapter this run's milestones reach (1 when it reached none).</summary>
        public static int Milestone(RunResult result)
        {
            if (result.Victory && result.HarmonyLevel >= HarmonyForLastChapter) return 5;
            if (result.Victory) return 4;
            if (result.FloorReached >= 3) return 3;
            return result.BossesDefeatedThisRun >= 1 ? 2 : 1;
        }

        /// <summary>[REQ: STORY-SAVE-001] Opens the chapters this run reached (never closes one) and, after a win with every
        /// keeper, the last chapter. Returns true when anything opened.</summary>
        public static bool Record(MetaProgressionState meta, RunResult result)
        {
            if (meta == null || !Counts(result)) return false;
            meta.StoryChapters ??= new List<StoryChapterEntry>();
            var reached = Milestone(result);
            var opened = false;
            var entry = meta.StoryChapters.Find(e => e.ClassId == result.PlayedClassId);
            if (entry == null && reached > 1)
            {
                meta.StoryChapters.Add(new StoryChapterEntry { ClassId = result.PlayedClassId, Chapter = reached });
                opened = true;
            }
            else if (entry != null && reached > entry.Chapter)
            {
                entry.Chapter = reached;
                opened = true;
            }
            if (!meta.StoryNinthOpened && System.Array.TrueForAll(Keepers, k => ChapterOf(meta, k) >= 4))
            {
                meta.StoryNinthOpened = true;
                opened = true;
            }
            return opened;
        }

        public static string Title(ClassId keeper, int chapter) => LocalizationService.T(TitleKey(keeper, chapter));
        public static string Text(ClassId keeper, int chapter) => LocalizationService.T(TextKey(keeper, chapter));
        public static string NinthTitle => LocalizationService.T("Story.Ninth.Title");
        public static string NinthText => LocalizationService.T("Story.Ninth.Text");

        /// <summary>The warden of a floor (0 to 4): its name, its line to this keeper, its parting line.</summary>
        public static string WardenName(int floor) => LocalizationService.T($"Story.Warden.{Clamp(floor)}.Name");
        public static string WardenLine(int floor, ClassId keeper) => LocalizationService.T($"Story.Warden.{Clamp(floor)}.{keeper}");
        public static string WardenParting(int floor) => LocalizationService.T($"Story.Warden.{Clamp(floor)}.Parting");

        /// <summary>What a milestone is, for the Codex's locked chapters ("Opens with a win").</summary>
        public static string MilestoneName(int chapter) => LocalizationService.T("Story.Milestone." + chapter);

        /// <summary>Every catalog key the story uses, for the localization tests.</summary>
        public static IEnumerable<string> LocalizationKeys()
        {
            foreach (var keeper in Keepers)
                for (var chapter = 1; chapter <= Chapters; chapter++)
                {
                    yield return TitleKey(keeper, chapter);
                    yield return TextKey(keeper, chapter);
                }
            yield return "Story.Ninth.Title";
            yield return "Story.Ninth.Text";
            for (var floor = 0; floor < Wardens; floor++)
            {
                yield return $"Story.Warden.{floor}.Name";
                yield return $"Story.Warden.{floor}.Parting";
                foreach (var keeper in Keepers) yield return $"Story.Warden.{floor}.{keeper}";
            }
            for (var chapter = 1; chapter <= Chapters; chapter++) yield return "Story.Milestone." + chapter;
            yield return "Story.Milestone.Ninth";
        }

        private static string TitleKey(ClassId keeper, int chapter) => $"Story.{keeper}.{chapter}.Title";
        private static string TextKey(ClassId keeper, int chapter) => $"Story.{keeper}.{chapter}.Text";
        private static int Clamp(int floor) => System.Math.Clamp(floor, 0, Wardens - 1);
    }
}
