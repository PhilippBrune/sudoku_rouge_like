using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunGraphService
    {
        public List<RunNode> BuildFloorGraph(int floorIndex, int seed)
        {
            var rng = new Random(seed + floorIndex * 7919);
            GetFloorLengths(floorIndex, rng, out var calmLength, out var riskLength);

            var nodes = new List<RunNode>();
            var index = 0;

            // Start node
            var startNode = new RunNode
            {
                Index = index++,
                Type = NodeType.Start,
                Route = RouteType.CalmRoute,
                Floor = floorIndex,
                Reachable = true,
                CanvasX = 0.05f,
                CanvasY = 0.5f
            };
            nodes.Add(startNode);

            // Calm route nodes
            var calmNodes = new List<RunNode>();
            for (var step = 0; step < calmLength; step++)
            {
                var type = step == calmLength - 1
                    ? NodeType.PreBoss
                    : RollNodeType(step, calmLength, false, rng);

                var node = new RunNode
                {
                    Index = index++,
                    Type = type,
                    Route = RouteType.CalmRoute,
                    Floor = floorIndex,
                    Reachable = true
                };
                calmNodes.Add(node);
                nodes.Add(node);
            }

            // Risk route nodes
            var riskNodes = new List<RunNode>();
            for (var step = 0; step < riskLength; step++)
            {
                var type = step == riskLength - 1
                    ? NodeType.PreBoss
                    : RollNodeType(step, riskLength, true, rng, 1 + floorIndex / 2);

                var node = new RunNode
                {
                    Index = index++,
                    Type = type,
                    Route = RouteType.RiskRoute,
                    Floor = floorIndex,
                    Reachable = true
                };
                riskNodes.Add(node);
                nodes.Add(node);
            }

            // Boss node
            var bossNode = new RunNode
            {
                Index = index,
                Type = NodeType.Boss,
                Route = RouteType.CalmRoute,
                Floor = floorIndex,
                Reachable = true,
                CanvasX = 0.95f,
                CanvasY = 0.5f
            };
            nodes.Add(bossNode);

            // Wire edges: start → first calm & first risk
            if (calmNodes.Count > 0) startNode.NextNodes.Add(calmNodes[0].Index);
            if (riskNodes.Count > 0) startNode.NextNodes.Add(riskNodes[0].Index);

            // Calm chain
            for (var i = 0; i < calmNodes.Count - 1; i++)
                calmNodes[i].NextNodes.Add(calmNodes[i + 1].Index);
            if (calmNodes.Count > 0) calmNodes[calmNodes.Count - 1].NextNodes.Add(bossNode.Index);

            // Risk chain
            for (var i = 0; i < riskNodes.Count - 1; i++)
                riskNodes[i].NextNodes.Add(riskNodes[i + 1].Index);
            if (riskNodes.Count > 0) riskNodes[riskNodes.Count - 1].NextNodes.Add(bossNode.Index);

            // Cross-links
            InsertCrossLinks(calmNodes, riskNodes);

            // Enforce economy pacing
            EnforceEconomyFloor(calmNodes);
            EnforceEconomyFloor(riskNodes);
            PreventAdjacentEconomy(calmNodes);
            PreventAdjacentEconomy(riskNodes);

            // Assign canvas positions
            AssignCanvasPositions(calmNodes, riskNodes, floorIndex, seed);

            return nodes;
        }

        // ── Floor Lengths ──

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

        // ── Node Type Rolling ──

        private static NodeType RollNodeType(int step, int branchLength, bool riskPath, Random rng,
            int riskPressure = 0)
        {
            if (step <= 0) return NodeType.Puzzle;

            var progress = branchLength <= 0 ? 1f : (float)step / branchLength;

            if (progress <= 0.30f)
                return WeightedRoll(rng, (NodeType.Puzzle, 66), (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 8), (NodeType.Cursed, 2));

            if (progress <= 0.70f)
            {
                return riskPath
                    ? WeightedRoll(rng,
                        (NodeType.Puzzle, Math.Max(16, 36 - riskPressure * 4)),
                        (NodeType.ElitePuzzle, 21 + riskPressure * 4),
                        (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 13), (NodeType.Cursed, 6))
                    : WeightedRoll(rng,
                        (NodeType.Puzzle, 50), (NodeType.ElitePuzzle, 10),
                        (NodeType.Shop, 10), (NodeType.Rest, 16), (NodeType.Relic, 10), (NodeType.Cursed, 4));
            }

            return riskPath
                ? WeightedRoll(rng,
                    (NodeType.Puzzle, Math.Max(10, 30 - riskPressure * 4)),
                    (NodeType.ElitePuzzle, 30 + riskPressure * 4),
                    (NodeType.Shop, 6), (NodeType.Rest, 12), (NodeType.Relic, 14), (NodeType.Cursed, 8))
                : WeightedRoll(rng,
                    (NodeType.Puzzle, 44), (NodeType.ElitePuzzle, 14),
                    (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 12), (NodeType.Cursed, 6));
        }

        private static NodeType WeightedRoll(Random rng, params (NodeType Type, int Weight)[] entries)
        {
            var total = 0;
            for (var i = 0; i < entries.Length; i++) total += entries[i].Weight;

            var roll = rng.Next(total);
            var cursor = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                cursor += entries[i].Weight;
                if (roll < cursor) return entries[i].Type;
            }

            return NodeType.Puzzle;
        }

        // ── Economy Pacing ──

        private static void EnforceEconomyFloor(List<RunNode> lane)
        {
            var sinceEconomy = 0;
            for (var i = 0; i < lane.Count; i++)
            {
                if (lane[i].Type == NodeType.Shop || lane[i].Type == NodeType.Rest)
                {
                    sinceEconomy = 0;
                    continue;
                }

                if (lane[i].Type == NodeType.PreBoss) continue;

                sinceEconomy++;
                if (sinceEconomy >= 5 && lane[i].Type == NodeType.Puzzle)
                {
                    lane[i].Type = NodeType.Rest;
                    sinceEconomy = 0;
                }
            }
        }

        private static void PreventAdjacentEconomy(List<RunNode> lane)
        {
            for (var i = 1; i < lane.Count; i++)
            {
                if (IsEconomy(lane[i - 1].Type) && IsEconomy(lane[i].Type))
                    lane[i].Type = NodeType.Puzzle;
            }
        }

        private static bool IsEconomy(NodeType type) => type == NodeType.Shop || type == NodeType.Rest;

        // ── Cross-Links ──

        private static void InsertCrossLinks(List<RunNode> calm, List<RunNode> risk)
        {
            var shortest = Math.Min(calm.Count, risk.Count);
            if (shortest < 3) return;

            var crossStep = Math.Clamp(shortest / 2, 1, shortest - 2);
            if (crossStep < calm.Count && crossStep < risk.Count)
            {
                calm[crossStep].NextNodes.Add(risk[crossStep].Index);
                risk[crossStep].NextNodes.Add(calm[crossStep].Index);
            }
        }

        // ── Canvas Positions ──

        private static void AssignCanvasPositions(List<RunNode> calm, List<RunNode> risk,
            int floorIndex, int seed)
        {
            var laneOffset = floorIndex <= 1 ? 0.18f : floorIndex == 2 ? 0.22f : 0.26f;
            var jitterMax = floorIndex <= 1 ? 0.04f : floorIndex == 2 ? 0.05f : 0.06f;

            var totalSlots = Math.Max(calm.Count, risk.Count) + 2;
            var slotWidth = 1f / totalSlots;

            for (var i = 0; i < calm.Count; i++)
            {
                var x = slotWidth * (i + 1.5f);
                var jRng = new Random(seed + floorIndex * 100 + i);
                var jitter = (float)(jRng.NextDouble() * 2 - 1) * jitterMax;
                calm[i].CanvasX = x;
                calm[i].CanvasY = 0.5f - laneOffset + jitter;
            }

            for (var i = 0; i < risk.Count; i++)
            {
                float x;
                if (risk.Count == 1)
                {
                    x = slotWidth * 1.5f;
                }
                else
                {
                    var fraction = (float)i / (risk.Count - 1);
                    var startX = slotWidth * 1.5f;
                    var endX = slotWidth * (calm.Count + 0.5f);
                    x = startX + fraction * (endX - startX);
                }

                var jRng = new Random(seed + floorIndex * 100 + 50 + i);
                var jitter = (float)(jRng.NextDouble() * 2 - 1) * jitterMax;
                risk[i].CanvasX = x;
                risk[i].CanvasY = 0.5f + laneOffset + jitter;
            }
        }
    }
}
