using System.Collections.Generic;
using System.Linq;
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

        // [REQ: CUSTOM-REGION-002] Every size has irregular regions (0.3.1 playtest: "also 6×6, 8×8 and 9×9"): a Custom
        // Puzzle asked for them gets them, never boxes, and the seed picks one of the size's two templates.
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [Requirement("CUSTOM-REGION-002")]
        public void AnIrregularPuzzle_HasIrregularRegions_InBothShapes(int size)
        {
            var shapes = new HashSet<string>();
            for (var seed = 1; seed <= 4; seed++)
            {
                var run = new RunDirector();
                run.StartTutorialRun(new TutorialSetupConfig { BoardSize = size, Stars = 3, RegionVariant = Irregular }, 7100 + seed);
                Assert.IsNotNull(run.CurrentBoard, $"seed {seed}: a board");
                Assert.AreEqual(TutorialModeService.RegionFor(size, Irregular, 7100 + seed), run.CurrentLevelConfig.RegionVariant,
                    $"seed {seed}: no fallback to boxes");
                var map = run.CurrentBoard.RegionMap;
                Assert.IsFalse(BoxShaped(map, size), $"seed {seed}: the regions are not boxes");
                var key = new System.Text.StringBuilder();
                foreach (var region in map) key.Append(region).Append(',');
                shapes.Add(key.ToString());
            }
            Assert.GreaterOrEqual(shapes.Count, 2, "both irregular templates appear, turned by the seed (GEN-REGION-004)");
        }

        // True when the map is the size's boxes, upright or turned (5×5 and 7×7 have none).
        private static bool BoxShaped(int[,] map, int size)
        {
            foreach (var (h, w) in new[] { (2, 3), (3, 2), (2, 4), (4, 2), (3, 3) })
            {
                if (h * w != size) continue;
                var same = true;
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    if (map[r, c] != map[r / h * h, c / w * w]) same = false;
                if (same) return true;
            }
            return false;
        }

        // Diagnostic: how often an irregular 6×6, 8×8 or 9×9 puzzle falls back to boxes, with no rule and with common rules.
        [Test, Explicit]
        public void Probe_IrregularFallbacks()
        {
            var rules = new[] { BossModifierId.Thermo, BossModifierId.KillerCages, BossModifierId.GermanWhispers,
                BossModifierId.DifferenceKropki, BossModifierId.XVPairs, BossModifierId.ArrowSums };
            var report = new System.Text.StringBuilder();
            foreach (var size in new[] { 6, 8, 9 })
            foreach (var variant in new[] { 2, 3 })
            {
                var fallbacks = 0; var total = 0; var ms = 0L;
                foreach (var rule in new BossModifierId?[] { null }.Concat(rules.Select(r => (BossModifierId?)r)))
                for (var seed = 1; seed <= 5; seed++)
                {
                    var mods = rule.HasValue ? new List<BossModifierId> { rule.Value } : new List<BossModifierId>();
                    if (rule.HasValue && !Tutorial.TutorialModeService.CanAddCustomModifier(size, new List<BossModifierId>(), rule.Value, out _)) continue;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var run = new RunDirector();
                    run.StartTutorialRun(new TutorialSetupConfig { BoardSize = size, Stars = 4, RegionVariant = variant, SelectedModifiers = mods }, 7300 + seed * 13 + size);
                    ms += watch.ElapsedMilliseconds;
                    total++;
                    if (run.CurrentLevelConfig.RegionVariant != variant)
                    {
                        fallbacks++;
                        report.AppendLine($"  fallback {size}x{size} v{variant} rule={rule} seed={seed}");
                    }
                }
                report.AppendLine($"{size}x{size} variant {variant}: {fallbacks}/{total} fell back to boxes, avg {ms / total} ms");
            }
            TestContext.WriteLine(report.ToString());
            UnityEngine.Debug.Log("[IrregularProbe] " + report);
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
