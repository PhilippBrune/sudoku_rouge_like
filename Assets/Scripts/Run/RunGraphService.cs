using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunGraphService
    {
        private readonly Random _random;

        public RunGraphService(int seed)
        {
            _random = new Random(seed);
        }

        public List<RunNode> BuildRunGraph(int runNumber, int minNodes = 8, int maxNodes = 12)
        {
            var riskScore = Math.Clamp(runNumber / 2, 0, 4);
            var totalNodes = Math.Clamp(minNodes + riskScore, minNodes, maxNodes);
            var graph = new List<RunNode>(totalNodes);

            graph.Add(new RunNode { Depth = 1, Layer = 1, Type = NodeType.Start, IsRevealed = true, IsRiskPath = false });

            for (var depth = 2; depth <= totalNodes - 2; depth++)
            {
                var type = RollNodeTypeByDepth(depth, totalNodes);
                graph.Add(new RunNode
                {
                    Depth = depth,
                    Layer = depth,
                    Type = type,
                    IsRiskPath = _random.NextDouble() < 0.45,
                    IsRevealed = depth <= 3
                });
            }

            graph.Add(new RunNode { Depth = totalNodes - 1, Layer = totalNodes - 1, Type = NodeType.PreBoss, IsRevealed = true, IsRiskPath = true });
            graph.Add(new RunNode { Depth = totalNodes, Layer = totalNodes, Type = NodeType.Boss, IsRevealed = true, IsRiskPath = true });

            EnforceEconomyFloor(graph);
            return graph;
        }

        public void RevealNextTwoLayers(List<RunNode> graph, int currentDepth)
        {
            for (var i = 0; i < graph.Count; i++)
            {
                var delta = graph[i].Depth - currentDepth;
                graph[i].IsRevealed = delta <= 2;
            }
        }

        private NodeType RollNodeTypeByDepth(int depth, int totalNodes)
        {
            if (depth <= 2)
            {
                return WeightedRoll((NodeType.Puzzle, 55), (NodeType.Shop, 20), (NodeType.Rest, 15), (NodeType.Relic, 5), (NodeType.Event, 5));
            }

            if (depth <= totalNodes / 2)
            {
                return WeightedRoll((NodeType.Puzzle, 45), (NodeType.ElitePuzzle, 10), (NodeType.Shop, 15), (NodeType.Rest, 10), (NodeType.Relic, 10), (NodeType.Event, 10));
            }

            return WeightedRoll((NodeType.Puzzle, 40), (NodeType.ElitePuzzle, 20), (NodeType.Shop, 12), (NodeType.Rest, 8), (NodeType.Relic, 10), (NodeType.Event, 10));
        }

        private NodeType WeightedRoll(params (NodeType Type, int Weight)[] entries)
        {
            var total = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                total += entries[i].Weight;
            }

            var roll = _random.Next(total);
            var cursor = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                cursor += entries[i].Weight;
                if (roll < cursor)
                {
                    return entries[i].Type;
                }
            }

            return NodeType.Puzzle;
        }

        private static void EnforceEconomyFloor(List<RunNode> graph)
        {
            var sinceEconomy = 0;
            for (var i = 0; i < graph.Count; i++)
            {
                if (graph[i].Type == NodeType.Shop || graph[i].Type == NodeType.Rest)
                {
                    sinceEconomy = 0;
                    continue;
                }

                sinceEconomy++;
                if (sinceEconomy >= 3 && graph[i].Type == NodeType.Puzzle)
                {
                    graph[i].Type = NodeType.Shop;
                    sinceEconomy = 0;
                }
            }
        }
    }
}
