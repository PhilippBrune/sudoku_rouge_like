using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // Playtest 0.4.1 (owner's pick T1): the Garden Run walks the gardens of Floors 1, 3 and 5.
    [TestFixture]
    public sealed class ThreeFloorRunTests
    {
        private static RunDirector Start(int seed = 7301)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1 }, seed);
            return run;
        }

        [Test]
        [Requirement("RUN-FLOORS-001")]
        public void ANewGardenRun_WalksGardensOneThreeAndFive()
        {
            var run = Start();
            Assert.IsTrue(run.State.ThreeFloors);
            Assert.AreEqual(3, run.State.TotalFloors);
            var gardens = new[] { 0, 2, 4 };
            for (var floor = 0; floor < 3; floor++)
            {
                Assert.AreEqual(gardens[floor], run.State.CurrentFloor);
                Assert.AreEqual(floor + 1, run.State.WalkFloor);
                Assert.AreEqual(gardens[floor], run.State.GardenOf(floor));
                Assert.AreEqual(floor == 2, run.State.OnLastFloor);
                if (floor < 2) run.AdvanceToNextFloor();
            }
        }

        [Test]
        [Requirement("RUN-FLOORS-001")]
        public void TheFloorsAreShorter_AndTheirBoardsBigger()
        {
            for (var seed = 1; seed <= 25; seed++)
                foreach (var (garden, min, max) in new[] { (0, 4, 5), (2, 6, 7), (4, 7, 8) })
                {
                    var graph = new RunGraphService().BuildFloorGraph(garden, seed, threeFloors: true);
                    var calm = graph.Count(n => n.Route == RouteType.CalmRoute && n.Type != NodeType.Start && n.Type != NodeType.Boss);
                    Assert.That(calm, Is.InRange(min, max), $"garden {garden}, seed {seed}");
                }
            Assert.AreEqual((6, 7), RunDirector.BoardSizesFor(0, true));
            Assert.AreEqual((7, 9), RunDirector.BoardSizesFor(2, true));
            Assert.AreEqual((9, 9), RunDirector.BoardSizesFor(4, true));
            var run = Start();
            var config = run.BuildLevelConfig(false, false, 1);
            Assert.That(config.BoardSize, Is.InRange(6, 7), "Floor 1's boards");
        }

        [Test]
        [Requirement("RUN-FLOORS-002")]
        public void TheLaterFloors_HaveAWardenInTheMiddle_AndAShopFirst()
        {
            for (var seed = 1; seed <= 20; seed++)
            {
                var first = new RunGraphService().BuildFloorGraph(0, seed, threeFloors: true);
                Assert.IsFalse(first.Any(n => n.IsWarden), "Floor 1 has no warden");
                foreach (var garden in new[] { 2, 4 })
                {
                    var graph = new RunGraphService().BuildFloorGraph(garden, seed, threeFloors: true);
                    var wardens = graph.Where(n => n.IsWarden).ToList();
                    Assert.That(wardens.Count, Is.InRange(1, 2), $"garden {garden}, seed {seed}");
                    Assert.IsTrue(wardens.All(w => w.Type == NodeType.ElitePuzzle && w.IsCrossLink));
                    var start = graph.First(n => n.Type == NodeType.Start);
                    Assert.IsTrue(start.NextNodes.Any(i => graph[i].Type == NodeType.Shop), "a shop right after the boss");
                    foreach (var route in new[] { RouteType.CalmRoute, RouteType.RiskRoute })
                        Assert.IsTrue(graph.Any(n => n.Route == route && n.Type == NodeType.Shop),
                            $"garden {garden}, seed {seed}: the {route} route keeps a shop beside its warden");
                }
            }
        }

        [Test]
        [Requirement("RUN-FLOORS-001")]
        public void TheDailyWalk_AndAFiveFloorSave_KeepTheirFloors()
        {
            var walk = new RunDirector();
            walk.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, DailyWalk = true, AllowIrregularPuzzles = false }, 11);
            Assert.IsFalse(walk.State.ThreeFloors);
            Assert.AreEqual(2, walk.State.TotalFloors);
            var old = new RunState { TotalFloors = 5, CurrentFloor = 3 };
            Assert.AreEqual(4, old.WalkFloor, "a five-floor run saved before 0.4.3");
            Assert.AreEqual(1, old.FloorStep);
            Assert.AreEqual(3, old.GardenOf(3));
        }

        [Test]
        [Requirement("RUN-FLOORS-001")]
        public void StoryTheSummitAndTheSwiftPath_FollowTheRunsLength()
        {
            var three = new RunResult { Mode = GameMode.GardenRun, ThreeFloors = true, FloorReached = 2, PlayedClassId = ClassId.NumberFreak };
            Assert.AreEqual(3, Meta.StoryService.Milestone(three), "the middle floor is chapter 3");
            var five = new RunResult { Mode = GameMode.GardenRun, FloorReached = 2, PlayedClassId = ClassId.NumberFreak };
            Assert.AreEqual(1, Meta.StoryService.Milestone(five));
            Assert.Less(Meta.ClassChallenges.SwiftSecondsThreeFloors, Meta.ClassChallenges.SwiftSeconds);
            Assert.AreEqual(36, RunResultFactory.ThreeFloorParMinutes);
        }
    }
}
