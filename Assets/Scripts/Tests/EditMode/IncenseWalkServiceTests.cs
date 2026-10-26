using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // IncenseWalkMode.md: the clock, the bonus table, the score and the class passives in time form.
    [TestFixture]
    public sealed class IncenseWalkServiceTests
    {
        [Test]
        [Requirement("INCENSE-CLOCK-002", "INCENSE-CLOCK-003")]
        public void ASolvedPuzzle_AddsItsBoardsSeconds_CutByMistakes()
        {
            Assert.AreEqual(90 + 15 + 20, IncenseWalkService.SolveSeconds(6, 2, 0), "6×6 ★★ perfect");
            Assert.AreEqual(105 * 80 / 100, IncenseWalkService.SolveSeconds(6, 2, 1), "one mistake: −20 %, no perfect bonus");
            Assert.AreEqual(105 * 20 / 100, IncenseWalkService.SolveSeconds(6, 2, 9), "at most −80 %");
            Assert.AreEqual(210 + 20, IncenseWalkService.SolveSeconds(9, 1, 0), "9×9 ★");
        }

        [Test]
        [Requirement("INCENSE-CLOCK-001", "INCENSE-CLOCK-003", "INCENSE-CLOCK-004")]
        public void TheStickBurnsFromEightMinutes_AMistakeCostsTenSeconds_AndAtZeroTheWalkEnds()
        {
            var walk = new IncenseWalkService(ClassId.NumberFreak, 1);
            Assert.AreEqual(480f, walk.SecondsLeft);
            walk.Tick(100f);
            Assert.IsTrue(walk.OnMistake());
            Assert.AreEqual(370f, walk.SecondsLeft);
            walk.Tick(1000f);
            Assert.IsTrue(walk.Over);
            Assert.AreEqual(0f, walk.SecondsLeft);
            Assert.AreEqual(0, walk.OnPuzzleSolved(6, 2, 0, false), "nothing counts after 0:00");
            Assert.AreEqual(0, walk.PuzzlesSolved);
        }

        [Test]
        [Requirement("INCENSE-SCORE-001")]
        public void TheScore_IsTheBoardsPointsPlusTheSecondsLeftAtTheSolve()
        {
            var walk = new IncenseWalkService(ClassId.NumberFreak, 1);
            walk.Tick(80f);
            walk.OnPuzzleSolved(6, 2, 0, true);
            Assert.AreEqual(54 + 400, walk.Score, "45 XP points × 1.2, then 400 s left");
            Assert.AreEqual(525f, walk.SecondsLeft);
            Assert.AreEqual(1, walk.PuzzlesSolved);
            Assert.AreEqual(6, walk.LargestBoard);
        }

        [Test]
        [Requirement("INCENSE-CLASS-001")]
        public void EachClassPassive_HasATimeForm()
        {
            Assert.AreEqual(540f, new IncenseWalkService(ClassId.StoneGardener, 1).SecondsLeft, "Stone Gardener: +60 s");

            var monk = new IncenseWalkService(ClassId.GardenMonk, 1);
            monk.OnPuzzleStart();
            Assert.AreEqual(3, Enumerable.Range(0, 5).Sum(_ => monk.OnCorrectDigit(false)), "Garden Monk: every 5th digit");

            var freak = new IncenseWalkService(ClassId.NumberFreak, 1);
            for (var i = 0; i < 9; i++) freak.OnCorrectDigit(false);
            freak.OnMistake();
            Assert.AreEqual(0, freak.OnCorrectDigit(false), "a mistake breaks the combo");
            for (var i = 0; i < 8; i++) freak.OnCorrectDigit(false);
            Assert.AreEqual(5, freak.OnCorrectDigit(false), "Number Freak: 10 in a row");

            Assert.AreEqual(2, new IncenseWalkService(ClassId.ShrineArchivist, 1).OnCorrectDigit(true), "Archivist: a pencilled cell");

            var reed = new IncenseWalkService(ClassId.ReedDuelist, 1);
            Assert.AreEqual(IncenseWalkService.SolveSeconds(6, 2, 0) + 30, reed.OnPuzzleSolved(6, 2, 0, false), "Reed: perfect, no pencil");

            var cartographer = new IncenseWalkService(ClassId.QuietCartographer, 1);
            cartographer.OnPuzzleStart();
            cartographer.Tick(8f);
            Assert.AreEqual(477f, cartographer.SecondsLeft, "Cartographer: the first 5 s of a board are free");

            var free = Enumerable.Range(0, 300).Count(seed => !new IncenseWalkService(ClassId.KoiGambler, seed).OnMistake());
            Assert.That(free, Is.InRange(70, 130), "Koi Gambler: about a third of mistakes cost no time");
        }

        [Test]
        [Requirement("INCENSE-PUZZLE-001")]
        public void ThePuzzlesGrow_EveryThreePuzzles()
        {
            var walk = new IncenseWalkService(ClassId.NumberFreak, 42);
            var levels = Enumerable.Range(0, 12).Select(walk.BuildLevel).ToList();
            Assert.IsTrue(levels.Take(3).All(l => l.BoardSize <= 6 && l.ActiveModifiers.Count == 0));
            Assert.IsTrue(levels.Skip(3).Take(3).All(l => l.BoardSize is 6 or 7 && l.ActiveModifiers.Count == 1));
            Assert.IsTrue(levels.Skip(9).All(l => l.BoardSize >= 8 && l.ActiveModifiers.Count == 2));

            var again = new IncenseWalkService(ClassId.GardenMonk, 42);
            Assert.AreEqual(levels.Select(l => l.Seed), Enumerable.Range(0, 12).Select(i => again.BuildLevel(i).Seed),
                "the same seed draws the same walk for everyone");
        }
    }
}
