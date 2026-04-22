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

            // Cross-links — count scales with floor index (1 bridge on floor 1, up to 5 on floor 5)
            var bridgePairs = InsertCrossLinks(calmNodes, riskNodes, floorIndex);

            // Enforce economy pacing
            EnforceEconomyFloor(calmNodes);
            EnforceEconomyFloor(riskNodes);
            PreventAdjacentEconomy(calmNodes);
            PreventAdjacentEconomy(riskNodes);

            // Guarantee at least one Shop per lane so players can always spend gold
            EnsureMinimumShop(calmNodes, rng);
            EnsureMinimumShop(riskNodes, rng);

            // Assign canvas positions
            AssignCanvasPositions(calmNodes, riskNodes, floorIndex, seed);

            // Phase 2: snap bridge pairs to the same CanvasX so dotted lines are vertical
            AlignBridgeCanvasX(bridgePairs);

            return nodes;
        }

        // ── Floor Lengths ──

        private static void GetFloorLengths(int floorIndex, Random rng, out int calmLength, out int riskLength)
        {
            int calmMin, calmMax;
            switch (floorIndex)
            {
                case 0: calmMin = 5; calmMax = 8;  break;
                case 1: calmMin = 6; calmMax = 9;  break;
                case 2: calmMin = 7; calmMax = 10; break;
                case 3: calmMin = 8; calmMax = 11; break;
                default: calmMin = 9; calmMax = 12; break;
            }

            calmLength = rng.Next(calmMin, calmMax + 1);
            riskLength = Math.Max(3, calmLength - rng.Next(1, 3)); // risk path is 1-2 nodes shorter than calm
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

        /// <summary>
        /// If the lane has no Shop node, convert the most central interior Puzzle/Relic/Rest node
        /// to a Shop. Skips PreBoss/Boss terminals and already-adjacent economy nodes.
        /// </summary>
        private static void EnsureMinimumShop(List<RunNode> lane, Random rng)
        {
            if (lane.Count == 0) return;
            var hasShop = false;
            for (var i = 0; i < lane.Count; i++)
                if (lane[i].Type == NodeType.Shop) { hasShop = true; break; }
            if (hasShop) return;

            // Pick a middle interior node that is safe to convert (not PreBoss, not adjacent to economy)
            var mid = lane.Count / 2;
            for (var offset = 0; offset <= mid; offset++)
            {
                foreach (var idx in new[] { mid - offset, mid + offset })
                {
                    if (idx < 0 || idx >= lane.Count) continue;
                    if (lane[idx].Type == NodeType.PreBoss || lane[idx].Type == NodeType.Boss) continue;
                    var prevEcon = idx > 0 && IsEconomy(lane[idx - 1].Type);
                    var nextEcon = idx < lane.Count - 1 && IsEconomy(lane[idx + 1].Type);
                    if (prevEcon || nextEcon) continue;
                    lane[idx].Type = NodeType.Shop;
                    return;
                }
            }
        }

        private static bool IsEconomy(NodeType type) =>
            type == NodeType.Shop || type == NodeType.Rest
            || type == NodeType.Relic || type == NodeType.CrossLink;

        // ── Cross-Links ──

        /// <summary>
        /// Inserts 1..floorIndex+1 bidirectional bridge edges between calm and risk lanes.
        /// Bridges are evenly distributed across the interior of the shorter lane.
        /// No two bridges share the same position index. Returns the placed (calm, risk) node pairs
        /// so canvas positions can be aligned in a second pass.
        /// </summary>
        private static List<(RunNode calm, RunNode risk)> InsertCrossLinks(
            List<RunNode> calm, List<RunNode> risk, int floorIndex)
        {
            var pairs = new List<(RunNode, RunNode)>();
            var shortest = Math.Min(calm.Count, risk.Count);

            // Need at least 3 interior slots to place any bridge safely
            if (shortest < 3) return pairs;

            // Maximum bridges that can fit with at least 1-node spacing
            var maxBridges = Math.Max(1, (shortest - 2) / 2);
            var targetBridges = Math.Clamp(floorIndex + 1, 1, maxBridges);

            var usedCalm = new HashSet<int>();
            var usedRisk = new HashSet<int>();

            for (var b = 0; b < targetBridges; b++)
            {
                // Evenly distribute positions across interior [1 .. shortest-2]
                var pos = (int)Math.Round((b + 1.0) * shortest / (targetBridges + 1.0));
                var calmPos = Math.Clamp(pos, 1, calm.Count - 2);
                var riskPos  = Math.Clamp(pos, 1, risk.Count  - 2);

                // Nudge to avoid duplicate positions — try forward first, then backward
                while (usedCalm.Contains(calmPos) && calmPos < calm.Count - 2) calmPos++;
                if (usedCalm.Contains(calmPos)) // still blocked — try backward
                    while (usedCalm.Contains(calmPos) && calmPos > 1) calmPos--;

                while (usedRisk.Contains(riskPos) && riskPos < risk.Count - 2) riskPos++;
                if (usedRisk.Contains(riskPos))  // still blocked — try backward
                    while (usedRisk.Contains(riskPos) && riskPos > 1) riskPos--;
                if (usedCalm.Contains(calmPos) || usedRisk.Contains(riskPos)) continue;

                calm[calmPos].NextNodes.Add(risk[riskPos].Index);
                risk[riskPos].NextNodes.Add(calm[calmPos].Index);
                calm[calmPos].IsCrossLink = true;
                calm[calmPos].Type        = NodeType.CrossLink;
                risk[riskPos].IsCrossLink = true;
                risk[riskPos].Type        = NodeType.CrossLink;

                usedCalm.Add(calmPos);
                usedRisk.Add(riskPos);
                pairs.Add((calm[calmPos], risk[riskPos]));
            }

            return pairs;
        }

        /// <summary>
        /// For each bridge pair, snap both nodes to the same CanvasX (their midpoint).
        /// This makes the dotted bridge line vertical rather than diagonal.
        /// Must be called after AssignCanvasPositions.
        /// </summary>
        private static void AlignBridgeCanvasX(List<(RunNode calm, RunNode risk)> pairs)
        {
            for (var i = 0; i < pairs.Count; i++)
            {
                var avgX = (pairs[i].calm.CanvasX + pairs[i].risk.CanvasX) * 0.5f;
                pairs[i].calm.CanvasX = avgX;
                pairs[i].risk.CanvasX = avgX;
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
