using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: HARMONY-PERK-002] The pre-run perks do what HarmonyDifficultySystem §7 says (GAP-064, GAP-067, GAP-069):
    // Moonshade Offering (+2 pencil, the first reward's slot 1 is Nothing), Scholar's Burden (−1 slot but never below one,
    // +5 pencil, a free Rare Ink Well, two from H8, put in the bag before the Level 30 class item), Empty Canvas (no
    // starting items, every item reward Rare or better).
    [TestFixture]
    [Requirement("HARMONY-PERK-002")]
    public sealed class HarmonyPerkStartTests
    {
        private static RunState Start(ClassId id, int harmony, HarmonyPerkId perk, int classLevel = 1, int seed = 4242) =>
            RunArchetypeService.CreateRunState(id, seed, classLevel: classLevel, harmonyLevel: harmony, harmonyPerk: perk);

        [Test]
        [Requirement("HARMONY-PERK-003")]
        public void MoonshadeOffering_StartsWithTwoMorePencil()
        {
            var plain = Start(ClassId.NumberFreak, 4, HarmonyPerkId.None);
            var perk = Start(ClassId.NumberFreak, 4, HarmonyPerkId.MoonshadeOffering);

            Assert.AreEqual(plain.MaxPencil + 2, perk.MaxPencil, "the maximum rises, so the +2 is not capped away");
            Assert.AreEqual(plain.CurrentPencil + 2, perk.CurrentPencil);
            Assert.AreEqual(plain.CurrentGold + 20, perk.CurrentGold);
        }

        [Test]
        [Requirement("HARMONY-PERK-003")]
        public void MoonshadeOffering_TheFirstRewardsFirstSlotIsNothing_OnlyOnce()
        {
            var run = StartPuzzleRun(HarmonyPerkId.MoonshadeOffering, 4);

            var first = run.BuildItemRewardSlots();
            Assert.IsNull(first[0], "the perk's price: slot 1 of the first reward is Nothing");
            Assert.IsNull(run.BuildItemRewardSlots()[0], "rolled again (a resume on the reward screen): still Nothing");
            run.TakeNothingReward();
            Assert.IsFalse(run.State.MoonshadeOfferingActive, "paid once the reward is left");

            var anyFirstSlot = false;
            for (var depth = 1; depth < 40 && !anyFirstSlot; depth++)
            {
                run.State.Depth = depth;
                anyFirstSlot = run.BuildItemRewardSlots()[0] != null;
            }
            Assert.IsTrue(anyFirstSlot, "later rewards roll slot 1 as usual");
        }

        [Test]
        [Requirement("HARMONY-PERK-004")]
        public void ScholarsBurden_StartsWithAFreeRareInkWell_AndFiveMorePencil()
        {
            var plain = Start(ClassId.NumberFreak, 6, HarmonyPerkId.None);
            var perk = Start(ClassId.NumberFreak, 6, HarmonyPerkId.ScholarsBurden);

            var inkWells = perk.HeldItems.Where(i => i.Type == ItemType.InkWell).ToList();
            Assert.AreEqual(1, inkWells.Count, "one Ink Well below H8");
            Assert.AreEqual(ItemRarity.Rare, inkWells[0].Rarity);
            Assert.AreEqual(plain.MaxPencil + 5, perk.MaxPencil);
            Assert.AreEqual(plain.CurrentPencil + 5, perk.CurrentPencil);
            Assert.AreEqual(System.Math.Max(1, plain.ItemSlots - 1), perk.ItemSlots, "the slot cost, never below one slot");
        }

        [Test]
        [Requirement("HARMONY-PERK-004")]
        public void ScholarsBurden_TwoInkWellsFromH8_WhenTheyFit()
        {
            // Number Freak at Level 25: 2 base slots + the level's slot bonus, minus H8's penalty and the perk's slot.
            var state = Start(ClassId.NumberFreak, 8, HarmonyPerkId.ScholarsBurden, classLevel: 25);

            var expected = System.Math.Min(2, state.ItemSlots);
            Assert.AreEqual(expected, state.HeldItems.Count(i => i.Type == ItemType.InkWell));
            Assert.LessOrEqual(state.HeldItems.Count, state.ItemSlots, "never more items than slots");
        }

        [TestCase(ClassId.GardenMonk, 6)]
        [TestCase(ClassId.NumberFreak, 8)]
        [TestCase(ClassId.NumberFreak, 10)]
        [Requirement("HARMONY-PERK-004")]
        public void ScholarsBurden_NeverAddsASlot_AndTheBagNeverOverflows(ClassId id, int harmony)
        {
            for (var level = 1; level <= 40; level += 3)
            {
                var plain = Start(id, harmony, HarmonyPerkId.None, level);
                var perk = Start(id, harmony, HarmonyPerkId.ScholarsBurden, level);

                Assert.LessOrEqual(perk.ItemSlots, plain.ItemSlots, $"{id} L{level} H{harmony}: the perk costs a slot, never adds one");
                Assert.GreaterOrEqual(perk.ItemSlots, 1);
                Assert.LessOrEqual(perk.HeldItems.Count, perk.ItemSlots, $"{id} L{level} H{harmony}: bag overflow");
                Assert.GreaterOrEqual(perk.HeldItems.Count(i => i.Type == ItemType.InkWell), 1, "the Ink Well always fits");
            }
        }

        [Test]
        [Requirement("HARMONY-PERK-004", "META-XP-007")]
        public void ScholarsBurden_TheInkWellComesBeforeTheClassItem()
        {
            // Garden Monk at Level 30 on H6: one slot left after the penalties, so the Ink Well takes it.
            var state = Start(ClassId.GardenMonk, 6, HarmonyPerkId.ScholarsBurden, classLevel: 30);

            Assert.AreEqual(ItemType.InkWell, state.HeldItems[0].Type);
            if (state.ItemSlots == 1)
                Assert.AreEqual(1, state.HeldItems.Count, "no room for the class item");
            else
                Assert.IsTrue(state.HeldItems.Any(i => i.Id.StartsWith("start-")), "the class item takes a free slot");
        }

        [Test]
        [Requirement("HARMONY-PERK-004")]
        public void ScholarsBurden_TheSameSeedStartsWithTheSameIds()
        {
            var a = Start(ClassId.NumberFreak, 8, HarmonyPerkId.ScholarsBurden, 25, 99).HeldItems.Select(i => i.Id);
            var b = Start(ClassId.NumberFreak, 8, HarmonyPerkId.ScholarsBurden, 25, 99).HeldItems.Select(i => i.Id);
            CollectionAssert.AreEqual(a, b, "no random ids in a seeded run");
        }

        [Test]
        [Requirement("HARMONY-PERK-005", "META-XP-007")]
        public void EmptyCanvas_StartsEmpty_AndEveryItemRewardIsRareOrBetter()
        {
            Assert.AreEqual(0, Start(ClassId.NumberFreak, 10, HarmonyPerkId.EmptyCanvas, 35).HeldItems.Count);

            var run = StartPuzzleRun(HarmonyPerkId.EmptyCanvas, 10);
            for (var depth = 0; depth < 60; depth++)
            {
                run.State.Depth = depth;
                var items = run.BuildItemRewardSlots().Where(i => i != null).ToList();
                foreach (var item in items)
                    Assert.AreNotEqual(ItemRarity.Normal, item.Rarity, $"depth {depth}: {item.Type}");
                Assert.AreEqual(items.Count, items.Select(i => i.Type).Distinct().Count(), $"depth {depth}: one of each type");
            }
            run.State.RerollTokens = 5;
            foreach (var item in run.RerollRewardSlots().Where(i => i != null))
                Assert.AreNotEqual(ItemRarity.Normal, item.Rarity, "a reroll keeps the guarantee");
        }

        private static RunDirector StartPuzzleRun(HarmonyPerkId perk, int harmony)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1,
                HarmonyLevel = harmony, HarmonyPerk = perk
            }, 9001);
            run.State.HeldRelics.Clear();
            run.State.HasPositiveFloorEffect = false;
            run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 2, Seed = 5,
                MissingPercent = StarDensityService.MissingPercentForStars(2) });
            return run;
        }
    }
}
