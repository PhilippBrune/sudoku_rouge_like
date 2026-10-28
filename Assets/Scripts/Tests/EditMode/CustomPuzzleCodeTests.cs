using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Tutorial;

namespace SudokuRoguelike.Tests
{
    // [REQ: CUSTOM-CODE-001] RM-209: a share code carries the whole Custom Puzzle setup and its seed; a damaged code is refused.
    [TestFixture]
    [Requirement("CUSTOM-CODE-001")]
    public sealed class CustomPuzzleCodeTests
    {
        private static TutorialSetupConfig Setup() => new TutorialSetupConfig
        {
            BoardSize = 9, Stars = 4, RegionVariant = 3, ResourceMode = TutorialResourceMode.Simulation,
            SimulationClassId = ClassId.KoiGambler, Seed = 48213,
            SelectedModifiers = new List<BossModifierId> { BossModifierId.KillerCages, BossModifierId.SumKropki, BossModifierId.Thermo },
        };

        [Test]
        public void ACode_ReadsBackAsTheSameSetup()
        {
            var code = CustomPuzzleCode.Encode(Setup());
            StringAssert.StartsWith("R1-", code);
            Assert.LessOrEqual(code.Replace("-", "").Length, 26, "short enough to type");

            Assert.IsTrue(CustomPuzzleCode.TryDecode(code, out var read));
            Assert.AreEqual(9, read.BoardSize);
            Assert.AreEqual(4, read.Stars);
            Assert.AreEqual(3, read.RegionVariant);
            Assert.AreEqual(TutorialResourceMode.Simulation, read.ResourceMode);
            Assert.AreEqual(ClassId.KoiGambler, read.SimulationClassId);
            Assert.AreEqual(48213, read.Seed);
            CollectionAssert.AreEqual(Setup().SelectedModifiers, read.SelectedModifiers);

            Assert.IsTrue(CustomPuzzleCode.TryDecode(" " + code.ToLowerInvariant().Replace("-", "") + " ", out _),
                "spaces, missing dashes and small letters are fine");
        }

        [Test]
        public void AFreePuzzleWithoutRules_ReadsBack()
        {
            var free = new TutorialSetupConfig { BoardSize = 5, Stars = 1, Seed = 7 };
            Assert.IsTrue(CustomPuzzleCode.TryDecode(CustomPuzzleCode.Encode(free), out var read));
            Assert.AreEqual(5, read.BoardSize);
            Assert.AreEqual(TutorialResourceMode.Free, read.ResourceMode);
            Assert.IsEmpty(read.SelectedModifiers);
            Assert.AreEqual(7, read.Seed);
        }

        [TestCase("")]
        [TestCase("hello")]
        [TestCase("R2-0000-0000-0000")]
        [TestCase("R1-UUUU-UUUU")]
        public void NotACode_IsRefused(string text)
        {
            Assert.IsFalse(CustomPuzzleCode.TryDecode(text, out var read));
            Assert.IsNull(read);
        }

        // [REQ: CUSTOM-SEED-001] The same setup and seed give the same board, so a shared code shares the puzzle.
        [Test]
        [Requirement("CUSTOM-SEED-001")]
        public void TheSameSetupAndSeed_GiveTheSameBoard()
        {
            var setup = Setup();
            setup.Stars = 3;
            var first = new Run.RunDirector();
            first.StartTutorialRun(setup, setup.Seed);
            var second = new Run.RunDirector();
            second.StartTutorialRun(setup, setup.Seed);
            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
                Assert.AreEqual(first.CurrentBoard.Cells[r, c], second.CurrentBoard.Cells[r, c], $"cell {r},{c}");
        }

        // [REQ: CUSTOM-CODE-001] A pasted code sets the screen; a class not unlocked here leaves it as it was.
        [Test]
        public void APastedCode_SetsTheScreen_UnlessItsClassIsLocked()
        {
            var go = new UnityEngine.GameObject("CustomSetupUnderTest");
            try
            {
                var menu = go.AddComponent<UI.TutorialMenuController>();
                Assert.IsTrue(menu.TryApply(Setup(), out _));
                var config = menu.GetConfig();
                Assert.AreEqual(9, config.BoardSize);
                Assert.AreEqual(48213, config.Seed);
                Assert.AreEqual(ClassId.KoiGambler, config.SimulationClassId);
                CollectionAssert.AreEqual(Setup().SelectedModifiers, config.SelectedModifiers);

                menu.RefreshClassDropdown(new List<ClassId> { ClassId.NumberFreak });
                var locked = Setup();
                locked.BoardSize = 6;
                Assert.IsFalse(menu.TryApply(locked, out var reason));
                StringAssert.Contains(Classes.ClassCatalog.DisplayName(ClassId.KoiGambler), reason);
                Assert.AreEqual(9, menu.GetConfig().BoardSize, "nothing changed");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AMistypedCode_IsRefused()
        {
            var code = CustomPuzzleCode.Encode(Setup());
            var at = code.Length / 2;
            if (code[at] == '-') at++;
            var typo = code.Substring(0, at) + (code[at] == 'A' ? 'B' : 'A') + code.Substring(at + 1);   // a letter mid-code
            Assert.IsFalse(CustomPuzzleCode.TryDecode(typo, out _), "the check byte catches a changed letter");
            Assert.IsFalse(CustomPuzzleCode.TryDecode(code.Substring(0, code.Length - 3), out _), "and a cut one");
        }
    }
}
