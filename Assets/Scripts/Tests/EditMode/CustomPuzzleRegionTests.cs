using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Tutorial;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: CUSTOM-REGION-001] 0.3.1 playtest (owner): the Regions choices a board size does not have are greyed. Boxes on
    // 6×6, 8×8 and 9×9, turned boxes on 6×6 and 8×8 (9×9 turned is the same 3×3), irregular regions on every size and the
    // only ones on 5×5 and 7×7; a rule that needs boxes rules out irregular regions on 6×6, 8×8 and 9×9.
    [TestFixture]
    [Requirement("CUSTOM-REGION-001")]
    public sealed class CustomPuzzleRegionTests
    {
        private const int Boxes = 0, Turned = 1, Irregular = 3;
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
        }

        [TestCase(5, false, false, true)]
        [TestCase(6, true, true, true)]
        [TestCase(7, false, false, true)]
        [TestCase(8, true, true, true)]
        [TestCase(9, true, false, true)]
        public void EachSize_OffersTheRegionsItHas(int size, bool boxes, bool turned, bool irregular)
        {
            var none = new List<BossModifierId>();
            Assert.AreEqual(boxes, TutorialModeService.SupportsRegionVariant(size, Boxes, none), "boxes");
            Assert.AreEqual(turned, TutorialModeService.SupportsRegionVariant(size, Turned, none), "turned");
            Assert.AreEqual(irregular, TutorialModeService.SupportsRegionVariant(size, Irregular, none), "irregular");
        }

        [Test]
        public void ARuleThatNeedsBoxes_RulesOutIrregularRegions_On9x9()
        {
            var antiking = new List<BossModifierId> { BossModifierId.Antiking };
            Assert.IsFalse(TutorialModeService.SupportsRegionVariant(9, Irregular, antiking));
            Assert.IsTrue(TutorialModeService.SupportsRegionVariant(9, Boxes, antiking));
        }

        // The generator has irregular templates for 9×9: a Custom Puzzle asked for them gets them, not boxes.
        [Test]
        public void A9x9IrregularPuzzle_HasIrregularRegions()
        {
            for (var seed = 1; seed <= 4; seed++)
            {
                var run = new RunDirector();
                run.StartTutorialRun(new TutorialSetupConfig { BoardSize = 9, Stars = 3, RegionVariant = Irregular }, 7100 + seed);
                Assert.IsNotNull(run.CurrentBoard, $"seed {seed}: a board");
                Assert.AreEqual(Irregular, run.CurrentLevelConfig.RegionVariant, $"seed {seed}: no fallback to boxes");
                var map = run.CurrentBoard.RegionMap;
                var boxShaped = true;
                for (var r = 0; r < 9; r++)
                for (var c = 0; c < 9; c++)
                    if (map[r, c] != map[r / 3 * 3, c / 3 * 3]) boxShaped = false;
                Assert.IsFalse(boxShaped, $"seed {seed}: the regions are not the 3×3 boxes");
            }
        }

        [Test]
        public void TheSlip_MovesAChoiceTheSizeHasNot()
        {
            _host = new GameObject("CustomPuzzleSetup");
            var setup = _host.AddComponent<TutorialMenuController>();
            setup.SetBoardSizeIndex(6 - 5);
            setup.SetRegionIndex(1);
            Assert.AreEqual(1, setup.GetRegionIndex(), "6×6 has turned boxes");

            setup.SetBoardSizeIndex(9 - 5);
            Assert.AreEqual(0, setup.GetRegionIndex(), "9×9 turned is boxes");
            Assert.IsFalse(setup.CanUseRegion(1));
            setup.SetRegionIndex(1);
            Assert.AreEqual(0, setup.GetRegionIndex(), "a greyed choice cannot be taken");

            setup.SetBoardSizeIndex(7 - 5);
            Assert.AreEqual(2, setup.GetRegionIndex(), "7×7 has only irregular regions");

            setup.SetBoardSizeIndex(9 - 5);
            setup.SetRegionIndex(2);
            Assert.IsTrue(setup.ToggleModifier(BossModifierId.Antiking));
            Assert.AreEqual(0, setup.GetRegionIndex(), "a rule that needs boxes turns irregular regions into boxes");
        }
    }
}
