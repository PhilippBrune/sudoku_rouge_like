using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // DailyGoalSystem: the ink stamp shop, looks only (DAILY-WALK-007).
    [TestFixture]
    [Requirement("DAILY-WALK-007")]
    public sealed class InkStampShopTests
    {
        [TearDown]
        public void WearTheGamesOwnLooks() => InkStampShop.Apply(new DailyGoalState());

        private static InkStampShop.Look Look(string key) => InkStampShop.Catalog.First(l => l.Key == key);

        [Test]
        public void BuyingALook_SpendsItsStamps_OwnsIt_AndWearsIt_OnlyWhenThereAreEnough()
        {
            var state = new DailyGoalState { TotalInkStamps = 9 };
            var gold = Look("GoldLeaf");
            Assert.IsFalse(InkStampShop.TryBuy(state, gold), "12 stamps, 9 in the purse");
            Assert.AreEqual(9, state.TotalInkStamps);

            var indigo = Look("Indigo");
            Assert.IsTrue(InkStampShop.TryBuy(state, indigo));
            Assert.AreEqual(4, state.TotalInkStamps);
            Assert.IsTrue(InkStampShop.Owns(state, indigo));
            Assert.IsTrue(InkStampShop.IsWorn(state, indigo));
            Assert.IsFalse(InkStampShop.TryBuy(state, indigo), "owned already");
            Assert.AreEqual(4, state.TotalInkStamps);
        }

        [Test]
        public void TheGamesOwnLooks_AreFree_AndKeepTheGamesColours()
        {
            var state = new DailyGoalState();
            foreach (var free in InkStampShop.Catalog.Where(l => l.Price == 0))
                Assert.IsTrue(InkStampShop.Owns(state, free), free.Key);
            InkStampShop.Apply(state);
            var ink = new Color(0.1f, 0.2f, 0.3f, 1f);
            Assert.AreEqual(ink, InkStampShop.GivenDigitColour(ink));
            Assert.AreEqual(ink, InkStampShop.LineColour(ink));
            Assert.AreEqual(ink, InkStampShop.SealRingColour(ink));
            Assert.AreEqual(ink, InkStampShop.PathSealColour(ink));
            Assert.AreEqual(ink, InkStampShop.PlacedDigitColour(ink));
        }

        [Test]
        public void WearingAnOwnedLook_ChangesWhatTheScreensDraw_AndAnUnownedOneCannotBeWorn()
        {
            var state = new DailyGoalState { TotalInkStamps = 20 };
            InkStampShop.Wear(state, Look("Moss"));
            Assert.AreEqual(0, state.WornBoardInk, "not owned");

            InkStampShop.TryBuy(state, Look("Indigo"));
            InkStampShop.TryBuy(state, Look("RoundSumiSeal"));
            Assert.AreEqual(Look("Indigo").Colour, InkStampShop.LineColour(Color.white));
            Assert.AreNotEqual(Color.white, InkStampShop.PlacedDigitColour(Color.white), "own digits change under indigo");
            Assert.IsTrue(InkStampShop.WornPathSeal.Round);

            InkStampShop.Wear(state, Look("SumiBlack"));
            Assert.AreEqual(Color.white, InkStampShop.LineColour(Color.white), "back to the game's own ink");
        }

        [Test]
        public void EveryLook_HasANameAndADescription()
        {
            foreach (var look in InkStampShop.Catalog)
            {
                Assert.AreNotEqual("StampShop.Look." + look.Key, InkStampShop.Name(look), look.Key);
                Assert.AreNotEqual("StampShop.Look." + look.Key + ".Desc", InkStampShop.Description(look), look.Key);
            }
            Assert.AreEqual(InkStampShop.Catalog.Length, InkStampShop.Catalog.Select(l => l.Code).Distinct().Count());
        }
    }
}
