using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Tests
{
    // [REQ: META-PRESTIGE-001..004] GAP-017: a level-40 class prestiges from the Meta Progression panel once the
    // cumulative boss gate (PrestigeTier + 1) × 3 is met; prestige resets the class XP and raises its tier (max 9).
    [TestFixture]
    [Requirement("META-PRESTIGE-001", "META-PRESTIGE-002", "META-PRESTIGE-003", "META-PRESTIGE-004")]
    public sealed class PrestigeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private static readonly int Level40Xp = XpTable.CumulativeXpForLevel(XpTable.MaxLevel);

        [Test]
        public void BelowLevel40_CannotPrestige()
        {
            var meta = Meta(Level40Xp - 1, bosses: 30);
            Assert.IsFalse(new ClassGardenProgressionService().CanPrestige(meta, ClassId.GardenMonk));
        }

        [Test]
        public void Level40_NeedsThreeBossDefeatsForTheFirstPrestige()
        {
            var garden = new ClassGardenProgressionService();
            Assert.IsFalse(garden.CanPrestige(Meta(Level40Xp, bosses: 2), ClassId.GardenMonk));
            Assert.IsTrue(garden.CanPrestige(Meta(Level40Xp, bosses: 3), ClassId.GardenMonk));
        }

        [Test]
        public void Prestige_ResetsXpAndRaisesTheTier_ThenTheGateRises()
        {
            var garden = new ClassGardenProgressionService();
            var meta = Meta(Level40Xp, bosses: 5);

            Assert.IsTrue(garden.TryPrestige(meta, ClassId.GardenMonk));
            Assert.AreEqual(1, garden.GetPrestigeTier(meta, ClassId.GardenMonk));
            Assert.AreEqual(0, garden.GetTotalXp(meta, ClassId.GardenMonk));
            Assert.AreEqual(1, garden.GetLevel(meta, ClassId.GardenMonk));

            garden.AddXp(meta, ClassId.GardenMonk, Level40Xp);
            Assert.AreEqual(6, ClassGardenProgressionService.RequiredBossesForPrestige(1));
            Assert.IsFalse(garden.TryPrestige(meta, ClassId.GardenMonk), "5 bosses do not meet the tier-2 gate");
            meta.ClassUnlocks.BossesDefeated = 6;
            Assert.IsTrue(garden.TryPrestige(meta, ClassId.GardenMonk));
            Assert.AreEqual(2, garden.GetPrestigeTier(meta, ClassId.GardenMonk));
        }

        [Test]
        public void TierNine_IsTheCap()
        {
            var meta = Meta(Level40Xp, bosses: 999);
            meta.GardenProgression.ClassEntries[0].PrestigeTier = ClassGardenProgressionService.MaxPrestigeTier;
            Assert.IsFalse(new ClassGardenProgressionService().TryPrestige(meta, ClassId.GardenMonk));
            Assert.AreEqual(Level40Xp, meta.GardenProgression.ClassEntries[0].TotalXp);
        }

        [Test]
        public void ProfileService_PrestigesAndSaves_OnlyWhenEligible()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope { MetaProgress = Meta(Level40Xp, bosses: 3) });
            var profile = new ProfileService(save);

            Assert.IsTrue(profile.CanPrestigeClass(ClassId.GardenMonk));
            Assert.IsTrue(profile.TryPrestigeClass(ClassId.GardenMonk));
            SaveFileService.FlushSharedPendingWrites();

            var reloaded = new SaveFileService(TestSlot).Load().MetaProgress;
            var garden = new ClassGardenProgressionService();
            Assert.AreEqual(1, garden.GetPrestigeTier(reloaded, ClassId.GardenMonk));
            Assert.AreEqual(0, garden.GetTotalXp(reloaded, ClassId.GardenMonk));
            Assert.IsFalse(profile.TryPrestigeClass(ClassId.GardenMonk), "level 1 again: not eligible");
        }

        private static MetaProgressionState Meta(int xp, int bosses)
        {
            var meta = new MetaProgressionState();
            meta.UnlockedClasses.Add(ClassId.GardenMonk);
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry { ClassId = ClassId.GardenMonk, TotalXp = xp });
            meta.ClassUnlocks.BossesDefeated = bosses;
            return meta;
        }
    }
}
