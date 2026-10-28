using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // 0.3.0 playtest: a Custom Puzzle keeps every rule the player chose (GAP-086) and Free resources mean no class (GAP-087).
    [TestFixture]
    public sealed class CustomPuzzleRulesTests
    {
        // [REQ: CUSTOM-RULE-001] The playtest's case: 9×9, 7 stars, Sum Dots. Every chosen rule is on the board.
        [TestCase(BossModifierId.SumKropki)]
        [TestCase(BossModifierId.KillerCages)]
        [TestCase(BossModifierId.DifferenceKropki)]
        [Requirement("CUSTOM-RULE-001")]
        public void AChosenRule_IsOnTheBoard_At9x9AndSevenStars(BossModifierId rule)
        {
            foreach (var region in new[] { 0, 1, 3 })   // boxes, turned, irregular (TutorialMenuController)
            for (var seed = 1; seed <= 8; seed++)
            {
                var run = new RunDirector();
                run.StartTutorialRun(new TutorialSetupConfig
                {
                    BoardSize = 9, Stars = 7, RegionVariant = region,
                    SelectedModifiers = new System.Collections.Generic.List<BossModifierId> { rule }
                }, 5000 + seed);
                Assert.IsNotNull(run.CurrentBoard, $"region {region}, seed {seed}: a board");
                Assert.IsTrue(ModifierPresence.IsPresent(rule, run.CurrentOverlay), $"region {region}, seed {seed}: {rule} is on the board");
                Assert.IsFalse(run.LastGenerationMissingModifiers.Contains(rule), $"region {region}, seed {seed}: nothing reported missing");
            }
        }

        // [REQ: CUSTOM-RULE-001] Several rules on one busy board: each one is there, or the puzzle says which is missing.
        [Test]
        [Requirement("CUSTOM-RULE-001")]
        public void SeveralChosenRules_AreAllOnTheBoard()
        {
            var sets = new[]
            {
                new[] { BossModifierId.SumKropki, BossModifierId.KillerCages, BossModifierId.Thermo, BossModifierId.GermanWhispers },
                new[] { BossModifierId.SumKropki, BossModifierId.DifferenceKropki, BossModifierId.RatioKropki, BossModifierId.ArrowSums },
                new[] { BossModifierId.SumKropki, BossModifierId.DutchWhispers, BossModifierId.ParityLines, BossModifierId.KillerCages, BossModifierId.RenbanLines },
            };
            var report = new System.Text.StringBuilder();
            foreach (var set in sets)
            for (var seed = 1; seed <= 6; seed++)
            {
                var run = new RunDirector();
                run.StartTutorialRun(new TutorialSetupConfig
                {
                    BoardSize = 9, Stars = 7, SelectedModifiers = set.ToList()
                }, 6000 + seed);
                var absent = set.Where(m => !ModifierPresence.IsPresent(m, run.CurrentOverlay)).ToList();
                var unreported = absent.Where(m => !run.LastGenerationMissingModifiers.Contains(m)).ToList();
                if (absent.Count > 0)
                    report.AppendLine($"[{string.Join(",", set)}] seed {seed}: absent {string.Join(",", absent)}, unreported {string.Join(",", unreported)}");
                Assert.IsEmpty(unreported, $"a rule that is not on the board is never silent: {report}");
            }
            UnityEngine.Debug.Log("[CustomPuzzleRulesTests] " + (report.Length == 0 ? "every rule placed" : report.ToString()));
            Assert.AreEqual(0, report.Length, report.ToString());
        }

        // [REQ: CUSTOM-CLASS-001] Free resources: no class, so no class passive (GAP-087); a chosen class keeps its own.
        [Test]
        [Requirement("CUSTOM-CLASS-001")]
        public void FreeResources_MeanNoClass()
        {
            var free = new RunDirector();
            free.StartTutorialRun(new TutorialSetupConfig
            {
                BoardSize = 4, Stars = 1, ResourceMode = TutorialResourceMode.Free, SimulationClassId = ClassId.KoiGambler
            }, 77);
            Assert.IsFalse(SudokuRoguelike.Classes.ClassCatalog.IsClass(free.State.ClassId));

            var classed = new RunDirector();
            classed.StartTutorialRun(new TutorialSetupConfig
            {
                BoardSize = 4, Stars = 1, ResourceMode = TutorialResourceMode.Simulation, SimulationClassId = ClassId.KoiGambler
            }, 77);
            Assert.AreEqual(ClassId.KoiGambler, classed.State.ClassId);
        }
    }
}
