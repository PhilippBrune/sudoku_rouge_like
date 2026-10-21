using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-STYLE-019] The data the garden of classes shows: each locked class's unlock goal with its progress
    // (META-UNLOCK-006), the Garden runs and best floor per class (META-STATS-001), the Codex totals (META-CODEX-001)
    // and the level schedule in the player's language (META-XP-006).
    [TestFixture]
    public sealed class ClassGardenDataTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            var path = Path.Combine(SaveFileService.SaveRoot, $"save_profile_{TestSlot}.json");
            if (File.Exists(path)) File.Delete(path);
            LocalizationService.SetLanguage(LanguageOption.English);
        }

        // ── Unlock goals ─────────────────────────────────────────────────────────

        [TestCase(ClassId.GardenMonk, 1, 2)]
        [TestCase(ClassId.ShrineArchivist, 7, 15)]
        [TestCase(ClassId.KoiGambler, 4, 10)]
        [TestCase(ClassId.StoneGardener, 1, 10)]
        [TestCase(ClassId.LanternSeer, 12400, 50000)]
        [Requirement("META-UNLOCK-006")]
        public void UnlockProgress_CountsTowardTheClassesGoal(ClassId id, int current, int target)
        {
            var meta = new MetaProgressionState
            {
                ClassUnlocks = new ClassUnlockProgress { BossesDefeated = 1, ItemsUsed = 7, RelicsCollected = 4, GoldCollected = 12400 }
            };

            var progress = ClassUnlockService.GetProgress(meta, id);

            Assert.AreEqual(current, progress.Current);
            Assert.AreEqual(target, progress.Target);
            Assert.IsFalse(progress.Unlocked);
        }

        [Test]
        [Requirement("META-UNLOCK-006")]
        public void UnlockProgress_NeverShowsMoreThanTheGoal_AndKnowsAnUnlockedClass()
        {
            var meta = new MetaProgressionState { ClassUnlocks = new ClassUnlockProgress { BossesDefeated = 7 } };
            meta.UnlockedClasses.Add(ClassId.GardenMonk);

            var monk = ClassUnlockService.GetProgress(meta, ClassId.GardenMonk);

            Assert.AreEqual(2, monk.Current);
            Assert.IsTrue(monk.Unlocked);
            Assert.IsTrue(ClassUnlockService.GetProgress(meta, ClassId.NumberFreak).Unlocked, "the starter is always unlocked");
        }

        [Test]
        [Requirement("META-UNLOCK-006", "META-CODEX-006")]
        public void ReedDuelistProgress_CountsTheFoundItemsWithoutTheCounterItems()
        {
            var meta = new MetaProgressionState { ItemCodex = new ItemCodexData() };
            foreach (var id in new[] { "Solver", "Finder", "WaterBasin", "NotAnItem", "Solver" })
                meta.ItemCodex.Entries.Add(new ItemCodexEntry { ItemId = id, Discovered = true });

            var progress = ClassUnlockService.GetProgress(meta, ClassId.ReedDuelist);

            Assert.AreEqual(2, progress.Current, "Solver once, Finder; the counter item and the unknown id do not count");
            Assert.AreEqual(CodexProgress.RequiredItems(meta).Total, progress.Target);
            Assert.Less(progress.Target, CodexProgress.Items(meta).Total, "the counter items are not required");
        }

        [Test]
        [Requirement("META-UNLOCK-006")]
        public void QuietCartographerProgress_IsDoneOrNotDone()
        {
            var meta = new MetaProgressionState();
            Assert.AreEqual((0, 1), (ClassUnlockService.GetProgress(meta, ClassId.QuietCartographer).Current,
                ClassUnlockService.GetProgress(meta, ClassId.QuietCartographer).Target));

            meta.ClassUnlocks.PerfectFullRunAllFloors = true;
            Assert.AreEqual(1, ClassUnlockService.GetProgress(meta, ClassId.QuietCartographer).Current);
        }

        [Test]
        [Requirement("META-UNLOCK-001")]
        public void CheckNewUnlocks_UsesTheSameGoalsAsTheProgress()
        {
            var meta = new MetaProgressionState
            {
                ClassUnlocks = new ClassUnlockProgress
                {
                    BossesDefeated = ClassUnlockService.StoneGardenerBosses,
                    ItemsUsed = ClassUnlockService.ShrineArchivistItemsUsed - 1
                }
            };

            var unlocked = new ClassUnlockService().CheckNewUnlocks(meta);

            CollectionAssert.Contains(unlocked, ClassId.StoneGardener);
            CollectionAssert.DoesNotContain(unlocked, ClassId.ShrineArchivist);
        }

        // ── Codex totals ─────────────────────────────────────────────────────────

        [Test]
        [Requirement("META-CODEX-001")]
        public void CodexProgress_CountsEachCollectionOnce_AndThePercentOfAll()
        {
            var meta = new MetaProgressionState
            {
                ItemCodex = new ItemCodexData(),
                DiscoveredRelics = new List<RelicId> { (RelicId)0, (RelicId)0 },
                DiscoveredCurses = new List<string> { "inkblot", "not_a_curse" }
            };
            meta.ItemCodex.Entries.Add(new ItemCodexEntry { ItemId = "Solver", Discovered = true });

            Assert.AreEqual(1, CodexProgress.Items(meta).Found);
            Assert.AreEqual(1, CodexProgress.Relics(meta).Found);
            Assert.AreEqual(1, CodexProgress.Curses(meta).Found);
            Assert.AreEqual(CurseService.All.Count, CodexProgress.Curses(meta).Total);
            var all = CodexProgress.Items(meta).Total + CodexProgress.Relics(meta).Total
                + CodexProgress.Rules(meta).Total + CodexProgress.Curses(meta).Total;
            Assert.AreEqual(3 * 100 / all, CodexProgress.Percent(meta));
            Assert.AreEqual(0, CodexProgress.Percent(null));
        }

        // ── Runs and best floor per class ───────────────────────────────────────

        [Test]
        [Requirement("META-STATS-001")]
        public void RecordRunAndGetNewUnlocks_CountsGardenRunsAndKeepsTheBestFloor()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());
            var profile = new ProfileService(save);

            profile.RecordRunAndGetNewUnlocks(GardenRun(ClassId.GardenMonk, floor: 3));
            profile.RecordRunAndGetNewUnlocks(GardenRun(ClassId.GardenMonk, floor: 2));
            profile.RecordRunAndGetNewUnlocks(new RunResult { PlayedClassId = ClassId.GardenMonk, Mode = GameMode.EndlessZen, FloorReached = 1 });
            profile.RecordRunAndGetNewUnlocks(new RunResult
            {
                PlayedClassId = ClassId.GardenMonk, Mode = GameMode.GardenRun, TutorialMode = true, FloorReached = 5
            });

            var entry = save.Load().MetaProgress.GardenProgression.ClassEntries.Single(e => e.ClassId == ClassId.GardenMonk);
            Assert.AreEqual(2, entry.GardenRuns, "only Garden Runs count, not Endless Zen or the tutorial");
            Assert.AreEqual(3, entry.BestFloor);
        }

        [Test]
        [Requirement("META-STATS-001")]
        public void RunResultFactory_RecordsTheFloorTheRunEndedOn()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 4401);
            run.State.CurrentFloor = 2;

            Assert.AreEqual(3, RunResultFactory.Build(run, false, 0, 1).FloorReached, "floor index 2 is the third floor");

            run.State.CurrentFloor = run.State.TotalFloors + 3;
            Assert.AreEqual(run.State.TotalFloors, RunResultFactory.Build(run, true, 0, 1).FloorReached);
        }

        [Test]
        [Requirement("META-COMPLETE-001")]
        public void LoadCompletionPercent_ShowsTheQuartersAsWholePercent()
        {
            var save = new SaveFileService(TestSlot);
            var completion = new CompletionTrackerState { AllRelicsDiscovered = true };
            foreach (ClassId id in System.Enum.GetValues(typeof(ClassId))) completion.ClassesAtLevel30.Add(id);
            save.Save(new SaveFileEnvelope { Completion = completion });

            Assert.AreEqual(50, new ProfileService(save).LoadCompletionPercent(), "all relics (25 %) and all classes at Level 30 (25 %)");
        }

        private static RunResult GardenRun(ClassId id, int floor) =>
            new RunResult { PlayedClassId = id, Mode = GameMode.GardenRun, FloorReached = floor };

        // ── The level schedule in the player's language ─────────────────────────

        [Test]
        [Requirement("META-XP-006")]
        public void ClassUnlockText_TranslatesEveryPartOfAScheduleEntry()
        {
            LocalizationService.SetLanguage(LanguageOption.German);

            var text = ClassUnlockText.Localize("+2 Pencil, +2 HP, +1 Starting Relic");

            Assert.AreEqual("+2 Bleistift, +2 LP, +1 Start-Relikt", text);
        }

        [Test]
        [Requirement("META-XP-006")]
        public void ClassUnlockText_EveryScheduleEntryHasATranslationInEveryLanguage()
        {
            var missing = new List<string>();
            foreach (LanguageOption language in System.Enum.GetValues(typeof(LanguageOption)))
            {
                if (language == LanguageOption.English) continue;
                LocalizationService.SetLanguage(language);
                foreach (ClassId id in System.Enum.GetValues(typeof(ClassId)))
                    foreach (var (_, unlock) in ClassCatalog.GetAllUnlocks(id))
                        foreach (var part in unlock.Split(new[] { ", " }, System.StringSplitOptions.None))
                            if (ClassUnlockText.Localize(part) == part)
                                missing.Add($"{language}: {part}");
            }

            CollectionAssert.IsEmpty(missing.Distinct().ToList());
        }

        [TestCase(ClassId.KoiGambler, 10, "35%")]
        [TestCase(ClassId.KoiGambler, 20, "35%")]
        [TestCase(ClassId.LanternSeer, 10, "35%")]
        [TestCase(ClassId.LanternSeer, 25, "40%")]
        [Requirement("META-XP-006")]
        public void ScheduleTexts_NameThePassiveValuesTheGameUses(ClassId id, int level, string value)
        {
            StringAssert.Contains(value, ClassCatalog.GetUnlockAtLevel(id, level));
        }
    }
}
