using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // [REQ: META-XP-007] Level 30: each class starts its runs with its item. [REQ: META-XP-008] Level 40: each class's frame,
    // kept after prestige. GAP-063: both were listed in the schedule but never granted.
    [TestFixture]
    public sealed class ClassLevelRewardTests
    {
        [Test]
        [Requirement("META-XP-007")]
        public void EveryClass_HasAStartingItemThatExists_AndTheScheduleNamesIt()
        {
            foreach (ClassId id in System.Enum.GetValues(typeof(ClassId)))
            {
                var start = ClassCatalog.GetStartingItem(id);
                Assert.IsTrue(System.Enum.IsDefined(typeof(ItemType), start.Type), $"{id}: {start.Type} is an item");
                var entry = ClassCatalog.GetUnlockAtLevel(id, ClassCatalog.StartingItemLevel);
                var expected = "Start: " + (start.Rarity == ItemRarity.Rare ? "Rare " : "") + ItemService.GetItemName(start.Type);
                StringAssert.Contains(expected, entry, $"{id}: the Level 30 entry names the item the run starts with");
            }
        }

        [TestCase(ClassId.NumberFreak, ItemType.Solver, ItemRarity.Rare)]
        [TestCase(ClassId.KoiGambler, ItemType.GoldenKoi, ItemRarity.Normal)]
        [TestCase(ClassId.StoneGardener, ItemType.RicePaperUmbrella, ItemRarity.Normal)]
        [TestCase(ClassId.ReedDuelist, ItemType.TempleIncense, ItemRarity.Normal)]
        [Requirement("META-XP-007")]
        public void Level30_StartsTheRunWithTheClassItem(ClassId id, ItemType type, ItemRarity rarity)
        {
            var state = RunArchetypeService.CreateRunState(id, 1234, classLevel: 30);

            Assert.AreEqual(1, state.HeldItems.Count);
            var item = state.HeldItems[0];
            Assert.AreEqual(type, item.Type);
            Assert.AreEqual(ItemService.IsTiered(type) ? rarity : ItemService.GetFixedRarity(type), item.Rarity);
            Assert.AreEqual(ItemService.GetDefaultCharges(type), item.Charges);
            Assert.IsFalse(string.IsNullOrEmpty(item.Id));
        }

        [Test]
        [Requirement("META-XP-007")]
        public void BelowLevel30_TheRunStartsWithAnEmptyBag()
        {
            Assert.AreEqual(0, RunArchetypeService.CreateRunState(ClassId.NumberFreak, 1234, classLevel: 29).HeldItems.Count);
        }

        [Test]
        [Requirement("META-XP-007")]
        public void EmptyCanvasPerk_StripsTheClassItemToo()
        {
            var state = RunArchetypeService.CreateRunState(ClassId.NumberFreak, 1234, classLevel: 30, harmonyLevel: 10,
                harmonyPerk: HarmonyPerkId.EmptyCanvas);

            Assert.AreEqual(0, state.HeldItems.Count, "Empty Canvas: no starting items");
        }

        [Test]
        [Requirement("META-XP-007")]
        public void StartingItem_IsTheSameForTheSameSeed()
        {
            var a = RunArchetypeService.CreateRunState(ClassId.GardenMonk, 77, classLevel: 35).HeldItems[0];
            var b = RunArchetypeService.CreateRunState(ClassId.GardenMonk, 77, classLevel: 35).HeldItems[0];
            Assert.AreEqual(a.Id, b.Id, "no random id in a seeded run");
        }

        [Test]
        [Requirement("META-XP-008")]
        public void Frame_IsUnlockedAtLevel40_AndStaysAfterPrestige()
        {
            var meta = new MetaProgressionState();
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
            {
                ClassId = ClassId.GardenMonk, TotalXp = XpTable.CumulativeXpForLevel(39)
            });
            Assert.IsFalse(ClassFrames.IsUnlocked(meta, ClassId.GardenMonk));

            meta.GardenProgression.ClassEntries[0].TotalXp = XpTable.CumulativeXpForLevel(XpTable.MaxLevel);
            Assert.IsTrue(ClassFrames.IsUnlocked(meta, ClassId.GardenMonk));

            meta.GardenProgression.ClassEntries[0].TotalXp = 0;
            meta.GardenProgression.ClassEntries[0].PrestigeTier = 1;
            Assert.IsTrue(ClassFrames.IsUnlocked(meta, ClassId.GardenMonk), "prestige resets the level, not the frame");
            Assert.IsFalse(ClassFrames.IsUnlocked(meta, ClassId.NumberFreak));
        }

        [Test]
        [Requirement("META-XP-008")]
        public void EveryClass_HasItsOwnFrameNamedInTheSchedule()
        {
            var colours = ((ClassId[])System.Enum.GetValues(typeof(ClassId))).Select(ClassFrames.Colour).ToList();
            Assert.AreEqual(colours.Count, colours.Distinct().Count(), "one colour per class");
            foreach (ClassId id in System.Enum.GetValues(typeof(ClassId)))
                StringAssert.StartsWith("Cosmetic: ", ClassCatalog.GetUnlockAtLevel(id, ClassFrames.UnlockLevel));
        }
    }
}
