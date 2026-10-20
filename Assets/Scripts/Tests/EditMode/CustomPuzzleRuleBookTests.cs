using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-STYLE-012] The Custom Puzzle rule book: every player modifier sits in exactly one family and has its icon;
    // the setup takes up to five rules in the player's order and refuses one that does not fit, with the reason.
    [TestFixture]
    [Requirement("UI-STYLE-012")]
    public sealed class CustomPuzzleRuleBookTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
        }

        [Test]
        public void Families_HoldEveryPlayerModifierOnce_WithItsIcon()
        {
            var all = ModifierFamilies.All().ToList();
            var expected = BossService.GetGeneratablePlayerModifiers(9).Append(BossModifierId.ModularRegions).ToList();

            Assert.AreEqual(71, all.Count, "the catalog plan's 71 player modifiers");
            Assert.AreEqual(all.Count, all.Distinct().Count(), "no modifier in two families");
            CollectionAssert.AreEquivalent(expected, all, "the families hold exactly the modifiers a custom puzzle can use");
            foreach (var id in all)
            {
                Assert.Contains(id, ModifierFamilies.Get(ModifierFamilies.FamilyOf(id)).ToList());
                var path = BossService.GetIconFolder(id) + "/icon_" + BossService.GetIconName(id);
                Assert.NotNull(Resources.Load<Texture2D>(path), $"{id} has no icon at Resources/{path}");
            }
            Assert.AreEqual(41, all.Count(ModifierFamilies.IsCatalogAddition), "41 rules came with the catalog batches");
        }

        [Test]
        public void Setup_TakesFiveRulesAtMost_AndSaysWhyNotASixth()
        {
            var setup = NewSetup(9);
            var picks = new[]
            {
                BossModifierId.DutchWhispers, BossModifierId.KillerCages, BossModifierId.Skyscrapers,
                BossModifierId.EvenOdd, BossModifierId.XVPairs
            };
            foreach (var id in picks) Assert.IsTrue(setup.ToggleModifier(id), $"{id} can be added");

            CollectionAssert.AreEqual(picks, setup.SelectedModifiers, "the rules keep the player's order");
            Assert.IsFalse(setup.ToggleModifier(BossModifierId.Thermo), "a sixth rule is refused");
            Assert.IsFalse(setup.CanSelect(BossModifierId.Thermo, out var reason));
            StringAssert.Contains("5", reason);
            Assert.IsNull(setup.ValidationMessage, "five fitting rules are a valid setup");

            Assert.IsTrue(setup.ToggleModifier(BossModifierId.KillerCages), "choosing a chosen rule takes it out");
            Assert.IsFalse(setup.IsSelected(BossModifierId.KillerCages));
            Assert.IsTrue(setup.CanSelect(BossModifierId.Thermo, out _), "and frees a place");
        }

        [Test]
        public void Setup_RefusesBlockedPairsAndRulesTooBigForTheBoard_AndDropsThemWhenTheBoardShrinks()
        {
            var setup = NewSetup(9);
            Assert.IsTrue(setup.ToggleModifier(BossModifierId.RatioKropki));
            Assert.IsFalse(setup.CanSelect(BossModifierId.DistanceGe2, out var blocked), "a blocked pair cannot be chosen");
            Assert.IsNotEmpty(blocked);

            Assert.IsTrue(setup.ToggleModifier(BossModifierId.Quadruples), "quadruples fit a 9×9");
            setup.SetBoardSizeIndex(1);   // 6×6
            CollectionAssert.AreEqual(new[] { BossModifierId.RatioKropki }, setup.SelectedModifiers,
                "a smaller board drops the rule that no longer fits");
            Assert.IsFalse(setup.CanSelect(BossModifierId.Quadruples, out var tooSmall));
            StringAssert.Contains("9", tooSmall);
        }

        [Test]
        public void Setup_SevenStarsNeedARule_AndClassBasedUsesTheChosenClass()
        {
            var setup = NewSetup(9);
            setup.SetStarsIndex(6);
            Assert.IsNotNull(setup.ValidationMessage, "7 stars without a rule is refused");
            setup.ToggleModifier(BossModifierId.EvenOdd);
            Assert.IsNull(setup.ValidationMessage);

            setup.RefreshClassDropdown(new List<ClassId> { ClassId.NumberFreak, ClassId.GardenMonk });
            setup.SetResourceModeIndex(1);
            setup.StepClass(1);
            Assert.AreEqual(ClassId.GardenMonk, setup.GetConfig().SimulationClassId);
            setup.StepClass(1);
            Assert.AreEqual(ClassId.NumberFreak, setup.GetConfig().SimulationClassId, "the class steps wrap");
        }

        private TutorialMenuController NewSetup(int size)
        {
            _host = new GameObject("CustomPuzzleSetup");
            var setup = _host.AddComponent<TutorialMenuController>();
            setup.SetBoardSizeIndex(size - 5);
            return setup;
        }
    }
}
