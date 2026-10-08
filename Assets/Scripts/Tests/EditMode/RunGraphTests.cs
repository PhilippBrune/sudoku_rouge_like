using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: MAP-CROSS-001] [REQ: MAP-STRUCT-002] [REQ: MAP-ROUTE-001] [REQ: MAP-ROUTE-002]
    // SPEC-CONF-010 / GAP-022: one cross-connection per floor between the Calm tile at floor(CalmLength/2) and the Risk
    // tile at floor(RiskLength/2); both keep their type. Graph connectivity promoted from review diagnostic D06.
    // Saves from 1.0 keep the legacy bridge layout until their next floor.
    [TestFixture]
    [Requirement("MAP-CROSS-001", "MAP-STRUCT-002")]
    public sealed class RunGraphTests
    {
        [Test]
        public void EachFloorHasExactlyOneCrossConnection_BetweenTheMidLaneTiles()
        {
            var service = new RunGraphService();
            for (var seed = 0; seed < 25; seed++)
            for (var floor = 0; floor < 5; floor++)
            {
                var graph = service.BuildFloorGraph(floor, 5000 + seed);
                var calm = graph.Where(n => n.Route == RouteType.CalmRoute && n.Type != NodeType.Start && n.Type != NodeType.Boss).ToList();
                var risk = graph.Where(n => n.Route == RouteType.RiskRoute).ToList();
                var cross = graph.Where(n => n.IsCrossLink).ToList();
                var where = $"seed {seed} floor {floor + 1}";

                Assert.AreEqual(2, cross.Count, where);
                Assert.IsTrue(cross.All(n => n.Type != NodeType.CrossLink), $"{where}: cross-link tiles keep their type");
                var calmAnchor = calm[System.Math.Min(calm.Count / 2, calm.Count - 2)];
                var riskAnchor = risk[System.Math.Min(risk.Count / 2, risk.Count - 2)];
                Assert.IsTrue(calmAnchor.IsCrossLink && riskAnchor.IsCrossLink, $"{where}: anchors");
                CollectionAssert.Contains(calmAnchor.NextNodes, riskAnchor.Index, where);
                CollectionAssert.Contains(riskAnchor.NextNodes, calmAnchor.Index, where);
                Assert.AreEqual(0, graph.Count(n => n.NextNodes.Any(next => graph[next].Route != n.Route
                    && n.Type != NodeType.Start && graph[next].Type != NodeType.Boss && !n.IsCrossLink)),
                    $"{where}: lanes connect only at the cross-link tiles");
            }
        }

        [Test]
        public void EveryTileIsReachableFromStart_AndReachesTheBoss()
        {
            var service = new RunGraphService();
            for (var seed = 0; seed < 25; seed++)
            for (var floor = 0; floor < 5; floor++)
            {
                var graph = service.BuildFloorGraph(floor, 6000 + seed);
                var reachable = Reach(graph, 0);
                Assert.AreEqual(graph.Count, reachable.Count, $"seed {seed} floor {floor + 1}: unreachable tiles");
                var boss = graph.Single(n => n.Type == NodeType.Boss).Index;
                foreach (var node in graph)
                    Assert.IsTrue(Reach(graph, node.Index).Contains(boss), $"seed {seed} floor {floor + 1}: node {node.Index} cannot reach the boss");
            }
        }

        [Test]
        [Requirement("MAP-CROSS-001")]
        public void LegacyLayout_StaysForSavesFromVersionOne_UntilTheNextFloor()
        {
            var legacy = new RunGraphService().BuildFloorGraph(2, 7001, graphVersion: 0);
            Assert.Greater(legacy.Count(n => n.Type == NodeType.CrossLink), 0, "1.0 layout: bridge tiles of type CrossLink");

            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 7001);
            Assert.AreEqual(RunGraphService.CurrentGraphVersion, run.State.FloorGraphVersion, "new runs use the current layout");
            run.State.CurrentFloor = 2;
            run.State.FloorGraphVersion = 0;             // as loaded from a 1.0 save
            run.RebuildFloorGraph();
            var expected = new RunGraphService().BuildFloorGraph(2, run.State.Seed, graphVersion: 0);
            CollectionAssert.AreEqual(expected.Select(n => n.Type), run.CurrentFloorGraph.Select(n => n.Type),
                "a resumed 1.0 floor keeps its graph");
            CollectionAssert.AreEqual(expected.Select(n => string.Join(",", n.NextNodes)),
                run.CurrentFloorGraph.Select(n => string.Join(",", n.NextNodes)));

            run.AdvanceToNextFloor();
            Assert.AreEqual(RunGraphService.CurrentGraphVersion, run.State.FloorGraphVersion);
            Assert.AreEqual(2, run.CurrentFloorGraph.Count(n => n.IsCrossLink));
        }

        private static HashSet<int> Reach(List<RunNode> graph, int from)
        {
            var seen = new HashSet<int> { from };
            var stack = new Stack<int>();
            stack.Push(from);
            while (stack.Count > 0)
                foreach (var next in graph[stack.Pop()].NextNodes)
                    if (seen.Add(next)) stack.Push(next);
            return seen;
        }
    }
}
