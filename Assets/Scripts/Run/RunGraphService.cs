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
            var calmMin = 4;
            var calmMax = 8;
            var calmRollFloor = Math.Clamp(calmMin + (riskScore / 2), calmMin, calmMax);
            var calmBranchLength = _random.Next(calmRollFloor, calmMax + 1);

            // Higher intended high-difficulty pressure shortens the risk route.
            var targetRiskHighDifficultyNodes = Math.Clamp(1 + runNumber / 2, 1, 4);
            var riskLengthPenalty = Math.Clamp(1 + (targetRiskHighDifficultyNodes / 2), 1, 3);
            var riskBranchLength = Math.Clamp(calmBranchLength - riskLengthPenalty, calmMin - 2, calmMax - 1);
            var longestBranch = Math.Max(calmBranchLength, riskBranchLength);

            var graph = new List<RunNode>(1 + calmBranchLength + riskBranchLength + 3);
            graph.Add(new RunNode { Depth = 1, Layer = 0, Type = NodeType.Start, IsRevealed = true, IsRiskPath = false });

            for (var step = 1; step <= longestBranch; step++)
            {
                var depth = step + 1;

                if (step <= calmBranchLength)
                {
                    graph.Add(new RunNode
                    {
                        Depth = depth,
                        Layer = 0,
                        Type = RollNodeTypeByProgress(step, calmBranchLength, false),
                        IsRiskPath = false,
                        IsRevealed = depth <= 3
                    });
                }

                if (step <= riskBranchLength)
                {
                    graph.Add(new RunNode
                    {
                        Depth = depth,
                        Layer = 1,
                        Type = RollNodeTypeByProgress(step, riskBranchLength, true, targetRiskHighDifficultyNodes),
                        IsRiskPath = true,
                        IsRevealed = depth <= 3
                    });
                }
            }

            var preBossDepth = longestBranch + 2;
            graph.Add(new RunNode { Depth = preBossDepth, Layer = 0, Type = NodeType.PreBoss, IsRevealed = true, IsRiskPath = false });
            graph.Add(new RunNode { Depth = preBossDepth, Layer = 1, Type = NodeType.PreBoss, IsRevealed = true, IsRiskPath = true });
            graph.Add(new RunNode { Depth = preBossDepth + 1, Layer = 2, Type = NodeType.Boss, IsRevealed = true, IsRiskPath = true });

            EnforceEconomyFloor(graph);
            PreventAdjacentEconomyNodes(graph);
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

        private NodeType RollNodeTypeByProgress(int step, int branchLength, bool riskPath, int riskHighDifficultyPressure = 0)
        {
            if (step <= 1)
            {
                return NodeType.Puzzle;
            }

            var progress = branchLength <= 0 ? 1f : (float)step / branchLength;

            if (progress <= 0.30f)
            {
                return WeightedRoll((NodeType.Puzzle, 68), (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 8));
            }

            if (progress <= 0.70f)
            {
                return riskPath
                    ? WeightedRoll(
                        (NodeType.Puzzle, Math.Max(18, 38 - (riskHighDifficultyPressure * 4))),
                        (NodeType.ElitePuzzle, 21 + (riskHighDifficultyPressure * 4)),
                        (NodeType.Shop, 8),
                        (NodeType.Rest, 18),
                        (NodeType.Relic, 15))
                    : WeightedRoll((NodeType.Puzzle, 54), (NodeType.ElitePuzzle, 10), (NodeType.Shop, 10), (NodeType.Rest, 16), (NodeType.Relic, 10));
            }

            return riskPath
                ? WeightedRoll(
                    (NodeType.Puzzle, Math.Max(12, 32 - (riskHighDifficultyPressure * 4))),
                    (NodeType.ElitePuzzle, 30 + (riskHighDifficultyPressure * 4)),
                    (NodeType.Shop, 6),
                    (NodeType.Rest, 14),
                    (NodeType.Relic, 18))
                : WeightedRoll((NodeType.Puzzle, 48), (NodeType.ElitePuzzle, 14), (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 14));
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
            var calmSinceEconomy = 0;
            var riskSinceEconomy = 0;

            for (var i = 0; i < graph.Count; i++)
            {
                if (graph[i].Type == NodeType.Start || graph[i].Type == NodeType.Boss)
                {
                    continue;
                }

                if (graph[i].Type == NodeType.Shop || graph[i].Type == NodeType.Rest)
                {
                    if (graph[i].IsRiskPath)
                    {
                        riskSinceEconomy = 0;
                    }
                    else
                    {
                        calmSinceEconomy = 0;
                    }

                    continue;
                }

                if (graph[i].IsRiskPath)
                {
                    riskSinceEconomy++;
                    if (riskSinceEconomy >= 5 && graph[i].Type == NodeType.Puzzle)
                    {
                        graph[i].Type = NodeType.Rest;
                        riskSinceEconomy = 0;
                    }
                }
                else
                {
                    calmSinceEconomy++;
                    if (calmSinceEconomy >= 5 && graph[i].Type == NodeType.Puzzle)
                    {
                        graph[i].Type = NodeType.Rest;
                        calmSinceEconomy = 0;
                    }
                }
            }
        }
        private static void PreventAdjacentEconomyNodes(List<RunNode> graph)
        {
            for (var lane = 0; lane <= 1; lane++)
            {
                var isRisk = lane == 1;
                RunNode prev = null;
                for (var i = 0; i < graph.Count; i++)
                {
                    var node = graph[i];
                    if (node.IsRiskPath != isRisk)
                        continue;
                    if (node.Type == NodeType.Start || node.Type == NodeType.Boss || node.Type == NodeType.PreBoss)
                    {
                        prev = node;
                        continue;
                    }

                    if (prev != null && IsEconomyNode(prev.Type) && IsEconomyNode(node.Type))
                    {
                        node.Type = NodeType.Puzzle;
                    }

                    prev = node;
                }
            }
        }

        private static bool IsEconomyNode(NodeType type) => type == NodeType.Shop || type == NodeType.Rest;
    }
}
