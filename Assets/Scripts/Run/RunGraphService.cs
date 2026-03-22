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

        /// <summary>Builds the path graph for a single floor.</summary>
        public List<RunNode> BuildFloorGraph(int floorIndex, int seed)
        {
            var floorRng = new Random(seed + floorIndex * 7919);
            GetFloorLengths(floorIndex, floorRng, out var calmLength, out var riskLength);

            var longestBranch = Math.Max(calmLength, riskLength);
            var graph = new List<RunNode>(2 + calmLength + riskLength + 3);

            // Shared Start Tile
            graph.Add(new RunNode { Depth = 1, Layer = 0, Type = NodeType.Start, IsRevealed = true, IsRiskPath = false });

            for (var step = 1; step <= longestBranch; step++)
            {
                var depth = step + 1;

                if (step <= calmLength)
                {
                    var type = step == calmLength
                        ? NodeType.PreBoss
                        : RollNodeTypeByProgress(step, calmLength, false, floorRng);
                    graph.Add(new RunNode
                    {
                        Depth = depth,
                        Layer = 0,
                        Type = type,
                        IsRiskPath = false,
                        IsRevealed = true
                    });
                }

                if (step <= riskLength)
                {
                    var type = step == riskLength
                        ? NodeType.PreBoss
                        : RollNodeTypeByProgress(step, riskLength, true, floorRng, 1 + floorIndex / 2);
                    graph.Add(new RunNode
                    {
                        Depth = depth,
                        Layer = 1,
                        Type = type,
                        IsRiskPath = true,
                        IsRevealed = true
                    });
                }
            }

            // Shared Boss Gate
            var bossDepth = longestBranch + 2;
            graph.Add(new RunNode { Depth = bossDepth, Layer = 2, Type = NodeType.Boss, IsRevealed = true, IsRiskPath = false });

            EnforceEconomyFloor(graph);
            PreventAdjacentEconomyNodes(graph);
            InsertCrossLinks(graph, calmLength, riskLength);
            AssignCanvasPositions(graph, calmLength, riskLength, floorIndex, seed);
            return graph;
        }

        /// <summary>Legacy overload — builds graph using old runNumber-based logic.</summary>
        public List<RunNode> BuildRunGraph(int runNumber, int minNodes = 8, int maxNodes = 12)
        {
            return BuildFloorGraph(0, _random.Next());
        }

        private static void GetFloorLengths(int floorIndex, Random rng, out int calmLength, out int riskLength)
        {
            int calmMin, calmMax, riskSub;
            switch (floorIndex)
            {
                case 0: calmMin = 5; calmMax = 8; riskSub = 1; break;
                case 1: calmMin = 6; calmMax = 9; riskSub = 2; break;
                case 2: calmMin = 7; calmMax = 10; riskSub = 2; break;
                case 3: calmMin = 8; calmMax = 11; riskSub = 3; break;
                default: calmMin = 9; calmMax = 12; riskSub = 3; break;
            }

            calmLength = rng.Next(calmMin, calmMax + 1);

            var riskMin = Math.Max(3, calmLength - riskSub);
            var riskMax = Math.Max(riskMin, calmLength - Math.Max(1, riskSub - 1));
            riskLength = rng.Next(riskMin, riskMax + 1);
        }

        public void RevealNextTwoLayers(List<RunNode> graph, int currentDepth)
        {
            for (var i = 0; i < graph.Count; i++)
            {
                graph[i].IsRevealed = true; // All tiles visible per spec
            }
        }

        /// <summary>
        /// Assigns normalised [0,1] canvas positions to every node.
        /// Layout is LEFT-TO-RIGHT: X progresses from start (left) to boss (right).
        /// Calm route occupies the upper lane (low Y), Risk route the lower lane (high Y).
        /// Start and Boss sit at the vertical centre.
        /// </summary>
        private static void AssignCanvasPositions(List<RunNode> graph, int calmLength, int riskLength,
            int floorIndex, int seed)
        {
            var longestBranch = Math.Max(calmLength, riskLength);
            var totalSlots = longestBranch + 2; // +1 for start, +1 for boss
            var slotWidth = 1f / totalSlots;

            float laneOffset, jitterMax;
            if (floorIndex <= 1) { laneOffset = 0.18f; jitterMax = 0.04f; }
            else if (floorIndex == 2) { laneOffset = 0.22f; jitterMax = 0.05f; }
            else { laneOffset = 0.26f; jitterMax = 0.06f; }

            var calmNodes = new List<RunNode>();
            var riskNodes = new List<RunNode>();

            for (var i = 0; i < graph.Count; i++)
            {
                var node = graph[i];
                if (node.Type == NodeType.Start)
                {
                    node.CanvasX = slotWidth * 0.5f;      // left edge
                    node.CanvasY = 0.5f;                   // vertical centre
                }
                else if (node.Type == NodeType.Boss)
                {
                    node.CanvasX = 1f - slotWidth * 0.5f;  // right edge
                    node.CanvasY = 0.5f;                    // vertical centre
                }
                else if (!node.IsRiskPath)
                {
                    calmNodes.Add(node);
                }
                else
                {
                    riskNodes.Add(node);
                }
            }

            // Distribute calm nodes (upper lane) evenly across horizontal slots
            for (var ci = 0; ci < calmNodes.Count; ci++)
            {
                var slot = ci + 1;
                var x = slotWidth * (slot + 0.5f);
                var jRng = new Random(seed + floorIndex * 100 + ci);
                var jitter = (float)(jRng.NextDouble() * 2 - 1) * jitterMax;
                calmNodes[ci].CanvasX = x;
                calmNodes[ci].CanvasY = 0.5f - laneOffset + jitter;  // upper half
            }

            // Distribute risk nodes (lower lane) proportionally spanning the same horizontal range
            for (var ri = 0; ri < riskNodes.Count; ri++)
            {
                float x;
                if (riskNodes.Count == 1)
                {
                    x = slotWidth * 1.5f;
                }
                else
                {
                    var fraction = (float)ri / (riskNodes.Count - 1);
                    var startX = slotWidth * 1.5f;
                    var endX = slotWidth * (calmLength + 0.5f);
                    x = startX + fraction * (endX - startX);
                }

                var jRng = new Random(seed + floorIndex * 100 + 50 + ri);
                var jitter = (float)(jRng.NextDouble() * 2 - 1) * jitterMax;
                riskNodes[ri].CanvasX = x;
                riskNodes[ri].CanvasY = 0.5f + laneOffset + jitter;  // lower half
            }
        }

        private static NodeType RollNodeTypeByProgress(int step, int branchLength, bool riskPath,
            Random rng, int riskHighDifficultyPressure = 0)
        {
            if (step <= 1)
            {
                return NodeType.Puzzle;
            }

            var progress = branchLength <= 0 ? 1f : (float)step / branchLength;

            if (progress <= 0.30f)
            {
                return WeightedRoll(rng, (NodeType.Puzzle, 68), (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 8));
            }

            if (progress <= 0.70f)
            {
                return riskPath
                    ? WeightedRoll(rng,
                        (NodeType.Puzzle, Math.Max(18, 38 - (riskHighDifficultyPressure * 4))),
                        (NodeType.ElitePuzzle, 21 + (riskHighDifficultyPressure * 4)),
                        (NodeType.Shop, 8),
                        (NodeType.Rest, 18),
                        (NodeType.Relic, 15))
                    : WeightedRoll(rng, (NodeType.Puzzle, 54), (NodeType.ElitePuzzle, 10), (NodeType.Shop, 10), (NodeType.Rest, 16), (NodeType.Relic, 10));
            }

            return riskPath
                ? WeightedRoll(rng,
                    (NodeType.Puzzle, Math.Max(12, 32 - (riskHighDifficultyPressure * 4))),
                    (NodeType.ElitePuzzle, 30 + (riskHighDifficultyPressure * 4)),
                    (NodeType.Shop, 6),
                    (NodeType.Rest, 14),
                    (NodeType.Relic, 18))
                : WeightedRoll(rng, (NodeType.Puzzle, 48), (NodeType.ElitePuzzle, 14), (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 14));
        }

        private static NodeType WeightedRoll(Random rng, params (NodeType Type, int Weight)[] entries)
        {
            var total = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                total += entries[i].Weight;
            }

            var roll = rng.Next(total);
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

        private static void InsertCrossLinks(List<RunNode> graph, int calmBranchLength, int riskBranchLength)
        {
            var shortestBranch = Math.Min(calmBranchLength, riskBranchLength);
            if (shortestBranch < 3)
            {
                return;
            }

            var crossLinkStep = Math.Clamp(shortestBranch / 2, 2, shortestBranch - 1);
            var crossLinkDepth = crossLinkStep + 1;

            for (var i = 0; i < graph.Count; i++)
            {
                var node = graph[i];
                if (node.Depth == crossLinkDepth && node.Layer <= 1 &&
                    node.Type != NodeType.Start && node.Type != NodeType.Boss && node.Type != NodeType.PreBoss)
                {
                    // Mark as cross-link but keep original tile type (puzzle, shop, rest, etc.)
                    node.IsCrossLink = true;
                    node.IsRevealed = true;
                }
            }
        }

        /// <summary>Returns the floor-scaled boss modifier options and required choices.</summary>
        public static void GetBossModifierCounts(int floorIndex, out int optionsShown, out int playerChooses)
        {
            optionsShown = Math.Clamp(floorIndex + 2, 2, 6);
            playerChooses = Math.Clamp(floorIndex + 1, 1, 5);
        }
    }
}
