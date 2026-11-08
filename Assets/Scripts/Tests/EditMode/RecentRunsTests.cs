using System;
using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.Tutorial;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // RM-230 and RM-232 (owner's picks after the 0.4.2 playtest): the Recent Runs slip's data and the Custom Puzzle's rules.
    [TestFixture]
    public sealed class RecentRunsTests
    {
        private static RunDirector StartPuzzle(int seed, params BossModifierId[] rules)
        {
            var run = new RunDirector();
            run.StartTutorialRun(new TutorialSetupConfig
            {
                BoardSize = 6, Stars = 3, Seed = seed,
                SelectedModifiers = new List<BossModifierId>(rules)
            }, seed);
            Assert.IsNotNull(run.CurrentBoard);
            return run;
        }

        private static (int row, int col) AnEmptyCell(SudokuBoard board)
        {
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0) return (r, c);
            throw new AssertionException("no empty cell");
        }

        [Test]
        [Requirement("RUN-HISTORY-001")]
        public void Capture_KeepsTheBoardAndItsLastWrongDigit()
        {
            var run = StartPuzzle(8101, BossModifierId.Thermo);
            var (row, col) = AnEmptyCell(run.CurrentBoard);
            var right = run.CurrentBoard.Solution[row, col];
            var wrong = right % run.CurrentBoard.Size + 1;
            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, wrong));

            var entry = RecentRuns.Capture(run, false, 2, new DateTime(2026, 11, 8, 20, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(8101, entry.Seed);
            Assert.AreEqual(2, entry.FloorReached);
            Assert.AreEqual(6, entry.BoardSize);
            Assert.AreEqual(36, entry.Digits.Length);
            Assert.AreEqual(36, entry.Givens.Length);
            Assert.AreEqual(36, entry.Regions.Length);
            Assert.AreEqual((row, col), (entry.WrongRow, entry.WrongCol));
            Assert.AreEqual((wrong, right), (entry.WrongDigit, entry.RightDigit));
            CollectionAssert.Contains(entry.Rules, BossModifierId.Thermo);
        }

        [Test]
        [Requirement("RUN-HISTORY-001")]
        public void Capture_WithoutAWrongDigit_MarksNone()
        {
            var run = StartPuzzle(8102);
            Assert.AreEqual(-1, RecentRuns.Capture(run, true, 3, DateTime.UtcNow).WrongRow);
        }

        [Test]
        [Requirement("RUN-HISTORY-001")]
        public void Add_KeepsTheFiveNewest_NewestFirst()
        {
            var envelope = new SaveFileEnvelope();
            for (var seed = 1; seed <= 7; seed++) RecentRuns.Add(envelope, new RecentRun { Seed = seed });
            Assert.AreEqual(5, envelope.RecentRuns.Count);
            Assert.AreEqual(7, envelope.RecentRuns[0].Seed);
            Assert.AreEqual(3, envelope.RecentRuns[4].Seed);
        }

        [Test]
        [Requirement("RUN-HISTORY-001")]
        public void TheList_SurvivesTheSaveFormat_AndAnOldSaveHasAnEmptyOne()
        {
            var envelope = new SaveFileEnvelope();
            var entry = new RecentRun { Seed = 482913, BoardSize = 6, Digits = new string('1', 36), WrongRow = 3, WrongDigit = 5, RightDigit = 3 };
            entry.Rules.Add(BossModifierId.KillerCages);
            entry.Items.Add(ItemType.PatternScroll);
            entry.Relics.Add((RelicId)1);
            RecentRuns.Add(envelope, entry);

            var read = JsonUtility.FromJson<SaveFileEnvelope>(JsonUtility.ToJson(envelope)).RecentRuns[0];

            Assert.AreEqual(482913, read.Seed);
            Assert.AreEqual(new string('1', 36), read.Digits);
            Assert.AreEqual((3, 5, 3), (read.WrongRow, read.WrongDigit, read.RightDigit));
            CollectionAssert.AreEqual(new[] { BossModifierId.KillerCages }, read.Rules);
            CollectionAssert.AreEqual(new[] { ItemType.PatternScroll }, read.Items);
            Assert.IsEmpty(JsonUtility.FromJson<SaveFileEnvelope>("{}").RecentRuns, "a save from before 0.4.4");
        }

        [Test]
        [Requirement("CUSTOM-RULE-002")]
        public void ACustomPuzzle_TakesSevenRules_NotAnEighth_AndItsCodeHoldsThem()
        {
            var rules = new List<BossModifierId>
            {
                BossModifierId.GermanWhispers, BossModifierId.Thermo, BossModifierId.KillerCages, BossModifierId.DifferenceKropki,
                BossModifierId.EvenOdd, BossModifierId.Quadruples, BossModifierId.ArrowSums
            };
            var setup = new TutorialSetupConfig { BoardSize = 9, Stars = 3, Seed = 4242, SelectedModifiers = rules };
            Assert.IsTrue(TutorialModeService.TryValidateCustomSetup(setup, out var message), message);
            Assert.IsFalse(TutorialModeService.CanAddCustomModifier(9, rules, BossModifierId.RenbanLines, out _), "the eighth");

            Assert.IsTrue(CustomPuzzleCode.TryDecode(CustomPuzzleCode.Encode(setup), out var read));
            CollectionAssert.AreEqual(rules, read.SelectedModifiers);
        }

        [Test]
        [Requirement("CUSTOM-RULE-003")]
        public void AChosenPressureRule_WorksInACustomPuzzle_AndItsCodeHoldsIt()
        {
            var run = StartPuzzle(8103, BossModifierId.HauntedCell, BossModifierId.CountdownFill, BossModifierId.PressureWave);
            Assert.AreNotEqual((-1, -1), run.CurrentLevelState.HauntedCell);
            Assert.IsNotEmpty(run.CurrentLevelState.ActiveThreats);
            Assert.Greater(run.CurrentLevelState.WaveInterval, 0);
            Assert.IsEmpty(run.LastGenerationMissingModifiers, "a pressure rule draws nothing, so nothing is missing");

            var setup = new TutorialSetupConfig { BoardSize = 6, Stars = 3, Seed = 9, SelectedModifiers = new List<BossModifierId> { BossModifierId.CrumblingRegion } };
            Assert.IsTrue(TutorialModeService.TryValidateCustomSetup(setup, out var message), message);
            Assert.IsTrue(CustomPuzzleCode.TryDecode(CustomPuzzleCode.Encode(setup), out var read));
            CollectionAssert.AreEqual(setup.SelectedModifiers, read.SelectedModifiers);
        }

        [Test]
        [Requirement("CUSTOM-RULE-003")]
        public void ThePressureTab_IsInTheRuleBookOnly()
        {
            CollectionAssert.Contains(Boss.ModifierFamilies.BookOrder, Boss.ModifierFamily.Pressure);
            CollectionAssert.DoesNotContain(Boss.ModifierFamilies.Order, Boss.ModifierFamily.Pressure);
            CollectionAssert.DoesNotContain(new List<BossModifierId>(Boss.ModifierFamilies.All()), BossModifierId.HauntedCell);
            Assert.IsFalse(Boss.ModifierFamilies.TryGetFamily(BossModifierId.HauntedCell, out _));
            Assert.AreEqual(4, Boss.ModifierFamilies.Get(Boss.ModifierFamily.Pressure).Count);
        }
    }
}
