using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // [REQ: META-COMPLETE-001] [REQ: XP-LOG-001] [REQ: REQ-XP-100] GAP-009 / GAP-034:
    // solved tiles and defeated boss modifiers reach completion tracking, the end-of-run breakdown labels
    // tiles with size/stars/boss, and the Reed Pledge bonus lands on the tile that earned it.
    [TestFixture]
    [Requirement("META-COMPLETE-001", "XP-LOG-001", "REQ-XP-100")]
    public sealed class CompletionTrackingTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [Test]
        public void DefeatingABoss_RecordsItsModifiersInTheRun()
        {
            var run = StartRun(8801);
            var config = new LevelConfig { BoardSize = 6, Stars = 2, IsBoss = true, Seed = 4,
                MissingPercent = StarDensityService.MissingPercentForStars(2) };
            config.ActiveModifiers.Add(BossModifierId.EvenOdd);
            run.StartLevel(config);
            Solve(run);

            var entry = run.CompleteLevelAndGrantRewards();

            Assert.IsTrue(entry.IsBoss);
            Assert.AreEqual(6, entry.BoardSize);
            Assert.AreEqual(1, run.State.BossesDefeatedThisRun);
            CollectionAssert.AreEqual(new[] { BossModifierId.EvenOdd }, run.State.ClearedBossModifiersThisRun);
        }

        [Test]
        public void RunEnd_RecordsSizeStarKeysAndClearedModifiers()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());
            var result = new RunResult { PlayedClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun };
            result.TileXpEntries.Add(new TileXpEntry { BoardSize = 6, Stars = 1, TotalXp = 45 });
            result.ClearedBossModifiers.Add(BossModifierId.Thermo);

            new ProfileService(save).RecordRunAndGetNewUnlocks(result);
            SaveFileService.FlushSharedPendingWrites();

            var completion = new SaveFileService(TestSlot).Load().Completion;
            CollectionAssert.Contains(completion.ClearedSizeStarCombos, "6x6_s1");
            CollectionAssert.Contains(completion.ClearedModifiers, BossModifierId.Thermo);
        }

        [Test]
        public void ModifierCompletion_CountsEveryModifierABossCanCarry()
        {
            Assert.AreEqual(60, CompletionService.ClearableModifierCount, "61 player modifiers (30 + catalog batches B1, B2, B3 and B6) minus the disabled Modular Regions");
            var state = new CompletionTrackerState();
            var service = new CompletionService();
            foreach (var mod in SudokuRoguelike.Boss.BossService.GetGeneratablePlayerModifiers(9))
                service.RecordModifierCleared(state, mod);
            Assert.AreEqual(0.25f, service.GetCompletionPercent(state), 0.0001f);
        }

        [Test]
        public void XpBreakdown_LabelsBossTilesWithSizeAndStars()
        {
            var result = new RunResult { XpEarned = 300 };
            result.TileXpEntries.Add(new TileXpEntry { BoardSize = 8, Stars = 2, IsBoss = true, TotalXp = 162 });
            var language = LocalizationService.Current;
            LocalizationService.SetLanguage(LanguageOption.English);
            try
            {
                var text = new EndScreenPresenter().BuildXpBreakdown(result);
                StringAssert.Contains("Boss 8×8 2★", text, "GAP-029: one size/star notation everywhere");
            }
            finally
            {
                LocalizationService.SetLanguage(language);
            }
        }

        [Test]
        [Requirement("XP-LOG-001")]
        public void ReedPledgeBonus_IsBookedOnTheTileThatFulfilledIt()
        {
            var run = StartRun(8802);
            run.State.PledgeActive = true;
            run.StartLevel(new LevelConfig { BoardSize = 5, Stars = 1, Seed = 2,
                MissingPercent = StarDensityService.MissingPercentForStars(1) });
            Solve(run);

            var entry = run.CompleteLevelAndGrantRewards();

            Assert.AreEqual(1, run.TileXpLog.Count, "the pledge was fulfilled on the run's first tile");
            var expected = XpTable.CalculateTileXp(5, 1, 0, false, entry.PerfectBonus > 0) + 25;
            Assert.AreEqual(expected, entry.TotalXp);
        }

        private static RunDirector StartRun(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            return run;
        }

        private static void Solve(RunDirector run)
        {
            Assert.NotNull(run.CurrentBoard);
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0) run.PlaceNumber(r, c, b.Solution[r, c]);
            Assert.IsTrue(run.IsLevelComplete);
        }
    }
}
