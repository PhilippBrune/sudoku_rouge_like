using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;

namespace SudokuRoguelike.Tests
{
    // 1.1 (2026-10-07): rules where the specs were aligned to the code (SPEC-CONF-005/006/007/015). These tests pin the
    // documented values so a later code change has to update the spec too.
    [TestFixture]
    public sealed class SpecAlignmentTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [Test]
        [Requirement("GEN-REMOVE-003")]
        public void CustomPuzzle_SevenStars_NeedsAtLeastOneMode()
        {
            var setup = new TutorialSetupConfig { BoardSize = 9, Stars = 7 };
            Assert.IsFalse(TutorialModeService.TryValidateCustomSetup(setup, out _));

            setup.SelectedModifiers.Add(BossModifierId.EvenOdd);
            Assert.IsTrue(TutorialModeService.TryValidateCustomSetup(setup, out _));
        }

        [Test]
        [Requirement("ECON-SHOP-001")]
        public void Shop_OffersTwoThreeOrFourItemsByFloor()
        {
            CollectionAssert.AreEqual(new[] { 2, 2, 3, 3, 4 }, Enumerable.Range(0, 5).Select(ShopService.GetShopSlotCount).ToArray());
            var offers = new ShopService(5101).BuildOffers(4, 1, 1f, 1f);
            Assert.AreEqual(4, offers.Count);
            Assert.IsTrue(offers.All(o => o.Item != null && o.Price > 0));
        }

        [Test]
        [Requirement("ZENMODE-UNLOCK-001")]
        public void EndlessZen_UnlocksWithTheTenthRecordedRun()
        {
            var profile = FreshProfile();
            for (var i = 0; i < 9; i++) RecordRun(profile, bosses: 0);
            Assert.IsFalse(Meta().EndlessZenUnlocked);
            RecordRun(profile, bosses: 0);
            Assert.IsTrue(Meta().EndlessZenUnlocked);
        }

        [Test]
        [Requirement("TRIAL-UNLOCK-001")]
        public void SpiritTrials_UnlockWithTheFirstBossDefeat()
        {
            var profile = FreshProfile();
            RecordRun(profile, bosses: 0);
            Assert.IsFalse(Meta().SpiritTrialsUnlocked);
            RecordRun(profile, bosses: 1);
            Assert.IsTrue(Meta().SpiritTrialsUnlocked);
        }

        [Test]
        [Requirement("META-ASCEND-004", "SEASON-SEED-001")]
        public void MonthlyWalkSeed_IsYearTimes100PlusMonth()
        {
            Assert.AreEqual(202610, SudokuRoguelike.Run.SeasonalChallengeService.BuildSeed(2026, 10));
            Assert.AreEqual(202701, SudokuRoguelike.Run.SeasonalChallengeService.BuildSeed(2027, 1));
        }

        private static ProfileService FreshProfile()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());
            return new ProfileService(save);
        }

        private static void RecordRun(ProfileService profile, int bosses) =>
            profile.RecordRunAndGetNewUnlocks(new RunResult
            {
                PlayedClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                BossesDefeatedThisRun = bosses
            });

        private static MetaProgressionState Meta()
        {
            SaveFileService.FlushSharedPendingWrites();
            return new SaveFileService(TestSlot).Load().MetaProgress;
        }
    }
}
