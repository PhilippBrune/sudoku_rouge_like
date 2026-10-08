using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: PRESSURE-ROLL-001] [REQ: PRESSURE-ROLL-002] [REQ: PRESSURE-ROLL-003] [REQ: PRESSURE-ROLL-005]
    // Promoted from review diagnostic D16 (GAP-014): pressure mechanics appear on elite/pre-boss/boss puzzles
    // with 0 / 12 / 30 / 55 / 80 % on floors 1–5, never on normal puzzles, and Pressure Wave only from floor 3.
    [TestFixture]
    [Requirement("PRESSURE-ROLL-001", "PRESSURE-ROLL-002", "PRESSURE-ROLL-003", "PRESSURE-ROLL-005")]
    public sealed class PressureRollTests
    {
        private static readonly BossModifierId[] PressureIds =
        {
            BossModifierId.CountdownFill, BossModifierId.HauntedCell,
            BossModifierId.CrumblingRegion, BossModifierId.PressureWave
        };

        [TestCase(1, 0.00, 0.00)]
        [TestCase(2, 0.07, 0.17)]
        [TestCase(3, 0.24, 0.36)]
        [TestCase(4, 0.48, 0.62)]
        [TestCase(5, 0.73, 0.87)]
        public void EliteRollRate_MatchesSpecPerFloor(int floorNumber, double min, double max)
        {
            var run = RunOnFloor(floorNumber);
            const int samples = 400;
            var hits = 0;
            var waves = 0;
            for (var node = 0; node < samples; node++)
            {
                var config = run.BuildLevelConfig(false, true, node);
                var pressure = config.ActiveModifiers.Where(PressureIds.Contains).ToList();
                Assert.LessOrEqual(pressure.Count, 1, "at most one pressure mechanic per puzzle");
                if (pressure.Count == 1) hits++;
                if (pressure.Contains(BossModifierId.PressureWave)) waves++;
            }

            var rate = hits / (double)samples;
            Assert.That(rate, Is.InRange(min, max), $"floor {floorNumber}: {rate:P0}");
            if (floorNumber < 3) Assert.AreEqual(0, waves, "Pressure Wave starts on floor 3");
        }

        [Test]
        public void NormalPuzzles_NeverRollPressure()
        {
            var run = RunOnFloor(5);
            for (var node = 0; node < 200; node++)
                Assert.IsFalse(run.BuildLevelConfig(false, false, node).ActiveModifiers.Any(PressureIds.Contains));
        }

        private static RunDirector RunOnFloor(int floorNumber)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 31337);
            for (var f = 1; f < floorNumber; f++) run.AdvanceToNextFloor();
            Assert.AreEqual(floorNumber - 1, run.State.CurrentFloor);
            return run;
        }
    }
}
