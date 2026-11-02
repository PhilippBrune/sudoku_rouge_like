using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // The story of the Nine (narrative-direction.md 1.2, owner's answers 2026-11-02): five chapters per keeper opened by that
    // class's milestones in a Garden Run, the last chapter after a win with every keeper, every text in all five languages.
    [TestFixture]
    [Requirement("STORY-CHAPTER-001")]
    public sealed class StoryServiceTests
    {
        private static RunResult Run(ClassId keeper, bool victory = false, int floor = 1, int bosses = 0, int harmony = 0) => new RunResult
        {
            PlayedClassId = keeper, Mode = GameMode.GardenRun, Victory = victory, FloorReached = floor, BossesDefeatedThisRun = bosses,
            HarmonyLevel = harmony,
        };

        [Test]
        [Requirement("STORY-SCENE-001")]
        public void AScene_HasTheKeeper_TheWardenOfItsChapter_AndEndsWithTheVoice()
        {
            var first = StoryService.SceneLines(ClassId.ReedDuelist, 1);
            Assert.AreEqual(2, first.Count, "chapter 1: the keeper, then the voice");
            Assert.AreEqual(StoryService.Speaker.Keeper, first[0].Speaker);

            var third = StoryService.SceneLines(ClassId.ReedDuelist, 3);
            CollectionAssert.AreEqual(new[] { StoryService.Speaker.Keeper, StoryService.Speaker.Warden, StoryService.Speaker.Keeper,
                StoryService.Speaker.Voice }, third.ConvertAll(l => l.Speaker));
            Assert.AreEqual("Story.ReedDuelist.3.Text", third[3].Key, "the voice tells the chapter");
            Assert.AreEqual(2, StoryService.SceneWardenFloor(3), "chapter 3: Kagami, the warden of floor 3");
            Assert.AreEqual(-1, StoryService.SceneWardenFloor(5));
        }

        [Test]
        [Requirement("STORY-SCENE-001")]
        public void AScene_IsDueOnce_ForTheNewestChapter()
        {
            var meta = new MetaProgressionState();
            Assert.AreEqual(1, StoryService.SceneDue(meta, ClassId.StoneGardener), "a new keeper's first scene");
            StoryService.MarkSceneSeen(meta, ClassId.StoneGardener, 1);
            Assert.AreEqual(0, StoryService.SceneDue(meta, ClassId.StoneGardener), "once only");

            StoryService.Record(meta, Run(ClassId.StoneGardener, true, 5, 5, harmony: 1));
            Assert.AreEqual(4, StoryService.SceneDue(meta, ClassId.StoneGardener), "the newest chapter, not each one between");
            StoryService.MarkSceneSeen(meta, ClassId.StoneGardener, 4);
            StoryService.MarkSceneSeen(meta, ClassId.StoneGardener, 2);
            Assert.AreEqual(0, StoryService.SceneDue(meta, ClassId.StoneGardener), "an older scene replayed closes nothing");
            Assert.AreEqual(1, StoryService.SceneDue(meta, ClassId.LanternSeer), "each keeper has its own scenes");

            Assert.IsFalse(StoryService.NinthSceneDue(meta));
            meta.StoryNinthOpened = true;
            Assert.IsTrue(StoryService.NinthSceneDue(meta));
            meta.StoryNinthSceneSeen = true;
            Assert.IsFalse(StoryService.NinthSceneDue(meta));
        }

        [Test]
        public void TheMilestones_OpenChaptersTwoToFive()
        {
            Assert.AreEqual(1, StoryService.Milestone(Run(ClassId.NumberFreak)), "a first run: chapter 1");
            Assert.AreEqual(2, StoryService.Milestone(Run(ClassId.NumberFreak, bosses: 1, floor: 2)), "a warden defeated");
            Assert.AreEqual(3, StoryService.Milestone(Run(ClassId.NumberFreak, bosses: 2, floor: 3)), "Floor 3 reached");
            Assert.AreEqual(4, StoryService.Milestone(Run(ClassId.NumberFreak, true, 5, 5, harmony: 2)), "a win");
            Assert.AreEqual(5, StoryService.Milestone(Run(ClassId.NumberFreak, true, 5, 5, harmony: 3)), "a win at Harmony III");
        }

        [Test]
        [Requirement("STORY-SAVE-001")]
        public void Record_OpensChapters_NeverClosesOne_AndCountsOnlyTheGardenRun()
        {
            var meta = new MetaProgressionState();
            Assert.AreEqual(1, StoryService.ChapterOf(meta, ClassId.KoiGambler), "chapter 1 is open from the start");

            Assert.IsTrue(StoryService.Record(meta, Run(ClassId.KoiGambler, bosses: 2, floor: 3)));
            Assert.AreEqual(3, StoryService.ChapterOf(meta, ClassId.KoiGambler));
            Assert.IsFalse(StoryService.Record(meta, Run(ClassId.KoiGambler)), "a shorter run closes nothing");
            Assert.AreEqual(3, StoryService.ChapterOf(meta, ClassId.KoiGambler));
            Assert.AreEqual(1, StoryService.ChapterOf(meta, ClassId.GardenMonk), "each keeper has its own chapters");

            var walk = Run(ClassId.GardenMonk, true, 2, 2);
            walk.DailyWalk = true;
            Assert.IsFalse(StoryService.Record(meta, walk), "the Daily Walk tells no chapter");
            var trial = Run(ClassId.GardenMonk, true, 5, 5);
            trial.Mode = GameMode.SpiritTrials;
            Assert.IsFalse(StoryService.Record(meta, trial), "nor another mode");
        }

        [Test]
        [Requirement("STORY-NINTH-001")]
        public void TheLastChapter_OpensAfterAWinWithEveryKeeper()
        {
            var meta = new MetaProgressionState();
            foreach (var keeper in StoryService.Keepers.Take(7)) StoryService.Record(meta, Run(keeper, true, 5, 5));
            Assert.IsFalse(StoryService.NinthOpened(meta), "seven keepers are not enough");
            StoryService.Record(meta, Run(StoryService.Keepers[7], true, 5, 5));
            Assert.IsTrue(StoryService.NinthOpened(meta));
        }

        [Test]
        [Requirement("STORY-SAVE-001")]
        public void ASaveFromBefore_StartsAtChapterOne()
        {
            var old = JsonUtility.FromJson<MetaProgressionState>("{\"UnlockedClasses\":[1]}");
            Assert.AreEqual(1, StoryService.ChapterOf(old, ClassId.ReedDuelist));
            Assert.IsFalse(StoryService.NinthOpened(old));
        }

        [Test]
        [Requirement("STORY-WARDEN-001")]
        public void EveryStoryText_IsInAllFiveLanguages()
        {
            var missing = (from language in new[] { LanguageOption.English, LanguageOption.German, LanguageOption.French,
                               LanguageOption.Spanish, LanguageOption.BrazilianPortuguese }
                           from key in StoryService.LocalizationKeys().Concat(new[] { "Story.Chapter", "Story.Gate",
                               "Story.Moved.Chapter", "Story.Moved.Ninth", "Codex.Tab.Tales", "Codex.Tales.Locked", "Story.Scene.Hint.Keys",
                               "Story.Scene.Watch.Keys" })
                           where !LocalizationService.HasExternalCatalogEntry(language, key)
                           select $"{language} {key}").ToList();
            Assert.IsEmpty(missing);
            // The scenes' own lines: per keeper 1 in chapters 1 and 5 and 3 in chapters 2 to 4 (the voice reads the chapter).
            Assert.AreEqual(8 * 5 * 2 + 2 + 5 * 10 + 6 + 8 * 11 + 3, StoryService.LocalizationKeys().Count(),
                "40 chapters, the last one, 5 wardens, 40 scenes");
        }
    }
}
