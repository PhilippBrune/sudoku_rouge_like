using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunGraphService
    {
        /// <summary>
        /// Graph layout version stored in <c>RunState.FloorGraphVersion</c>. 0/1: legacy layout with floor+1 "Bridge"
        /// tiles (saves from 1.0 keep it until their next floor). 2: one cross-connection per floor between two tiles
        /// that keep their type (MAP-CROSS-001, SPEC-CONF-010, 2026-10-06). 3: the Garden Run's routes roll event tiles
        /// again and the Daily Walk's path is two tiles shorter (MAP-NODE-EVENT-001, DAILY-WALK-010, 0.3.1).
        /// </summary>
        public const int CurrentGraphVersion = 3;

        // [REQ: MAP-STRUCT-002] BuildFloorGraph: single entry point — Shared Start node → Calm/Risk lane nodes → Boss Gate, then edges + cross-links
        // [REQ: DAILY-WALK-009] onePath: the Daily Walk's single row of tiles, no Risk route and no cross-link, with one event.
        // [REQ: RUN-FLOORS-001] threeFloors: the Garden Run's gardens 1, 3 and 5 are shorter and denser (ThreeFloorLengths).
        public List<RunNode> BuildFloorGraph(int floorIndex, int seed, int graphVersion = CurrentGraphVersion, bool onePath = false,
            bool threeFloors = false)
        {
            var rng = new Random(seed + floorIndex * 7919);
            int calmLength, riskLength;
            if (threeFloors) ThreeFloorLengths(floorIndex, rng, out calmLength, out riskLength);
            else GetFloorLengths(floorIndex, rng, out calmLength, out riskLength);
            if (onePath) riskLength = 0;
            if (onePath && graphVersion >= 3) calmLength = Math.Max(4, calmLength - 2);   // [REQ: DAILY-WALK-010]
            var events = graphVersion >= 3 && !onePath;   // the walk keeps its one event (EnsureEvent)

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

            // [REQ: MAP-ROUTE-001] Calm route: lower-risk lane; node count scales with floor (5–12 interior nodes)
            // Calm route nodes
            var calmNodes = new List<RunNode>();
            for (var step = 0; step < calmLength; step++)
            {
                var type = step == calmLength - 1
                    ? NodeType.PreBoss
                    : RollNodeType(step, calmLength, false, rng, 0, events);

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

            // [REQ: MAP-ROUTE-002] Risk route: higher-pressure lane (1–2 nodes shorter than calm); higher ElitePuzzle weight
            // Risk route nodes
            var riskNodes = new List<RunNode>();
            for (var step = 0; step < riskLength; step++)
            {
                var type = step == riskLength - 1
                    ? NodeType.PreBoss
                    : RollNodeType(step, riskLength, true, rng, 1 + floorIndex / 2, events);

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

            var bridgePairs = onePath ? new List<(RunNode calm, RunNode risk)>()
                : graphVersion >= 2
                ? InsertSingleCrossLink(calmNodes, riskNodes)
                : InsertCrossLinks(calmNodes, riskNodes, floorIndex);

            // Enforce economy pacing
            EnforceEconomyFloor(calmNodes);
            EnforceEconomyFloor(riskNodes);
            PreventAdjacentEconomy(calmNodes);
            PreventAdjacentEconomy(riskNodes);

            // Guarantee at least one Shop per lane so players can always spend gold
            EnsureMinimumShop(calmNodes, rng);
            EnsureMinimumShop(riskNodes, rng);
            if (onePath) EnsureEvent(calmNodes);

            if (threeFloors && !onePath)
            {
                PlaceWardensAndShop(floorIndex, calmNodes, riskNodes, bridgePairs);
                EnsureMinimumShop(riskNodes, rng);   // a warden may have taken the Risk route's only shop
            }

            // Assign canvas positions
            AssignCanvasPositions(calmNodes, riskNodes, floorIndex, seed);

            // Phase 2: snap bridge pairs to the same CanvasX so dotted lines are vertical
            AlignBridgeCanvasX(bridgePairs);

            return nodes;
        }

        /// <summary>[REQ: RUN-FLOORS-001] Calm route lengths of a three-floor run: 4–5, 6–7 and 7–8 tiles; the Risk route 1–2
        /// tiles shorter (at least 3).</summary>
        public static void ThreeFloorLengths(int floorIndex, Random rng, out int calmLength, out int riskLength)
        {
            var (min, max) = floorIndex <= 0 ? (4, 5) : floorIndex <= 2 ? (6, 7) : (7, 8);
            calmLength = rng.Next(min, max + 1);
            riskLength = Math.Max(3, calmLength - rng.Next(1, 3));
        }

        /// <summary>[REQ: RUN-FLOORS-002] On the gardens of Floors 3 and 5 the lane anchors in the middle are the skipped
        /// garden's warden (an elite with a relic choice after it), and the Calm route opens with a shop after the boss.</summary>
        private static void PlaceWardensAndShop(int floorIndex, List<RunNode> calm, List<RunNode> risk,
            List<(RunNode calm, RunNode risk)> anchors)
        {
            if (floorIndex < 2) return;
            foreach (var (c, r) in anchors)
                foreach (var node in new[] { c, r })
                    if (node != null && node.Type != NodeType.PreBoss) { node.Type = NodeType.ElitePuzzle; node.IsWarden = true; }
            if (calm.Count > 1 && !calm[0].IsWarden) calm[0].Type = NodeType.Shop;
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

        // [REQ: MAP-NODE-EVENT-001] events: the Event column of the weight table (graph version 3); 0 before, so a saved
        // floor keeps its tiles.
        private static NodeType RollNodeType(int step, int branchLength, bool riskPath, Random rng,
            int riskPressure = 0, bool events = false)
        {
            if (step <= 0) return NodeType.Puzzle;

            var progress = branchLength <= 0 ? 1f : (float)step / branchLength;
            int Event(int weight) => events ? weight : 0;

            if (progress <= 0.30f)
                return WeightedRoll(rng, (NodeType.Puzzle, 66), (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 8), (NodeType.Cursed, 2),
                    (NodeType.Event, Event(4)));

            if (progress <= 0.70f)
            {
                return riskPath
                    ? WeightedRoll(rng,
                        (NodeType.Puzzle, Math.Max(16, 36 - riskPressure * 4)),
                        (NodeType.ElitePuzzle, 21 + riskPressure * 4),
                        (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 13), (NodeType.Cursed, 6), (NodeType.Event, Event(14)))
                    : WeightedRoll(rng,
                        (NodeType.Puzzle, 50), (NodeType.ElitePuzzle, 10),
                        (NodeType.Shop, 10), (NodeType.Rest, 16), (NodeType.Relic, 10), (NodeType.Cursed, 4), (NodeType.Event, Event(6)));
            }

            return riskPath
                ? WeightedRoll(rng,
                    (NodeType.Puzzle, Math.Max(10, 30 - riskPressure * 4)),
                    (NodeType.ElitePuzzle, 30 + riskPressure * 4),
                    (NodeType.Shop, 6), (NodeType.Rest, 12), (NodeType.Relic, 14), (NodeType.Cursed, 8), (NodeType.Event, Event(14)))
                : WeightedRoll(rng,
                    (NodeType.Puzzle, 44), (NodeType.ElitePuzzle, 14),
                    (NodeType.Shop, 8), (NodeType.Rest, 16), (NodeType.Relic, 12), (NodeType.Cursed, 6), (NodeType.Event, Event(10)));
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
                    if (lane[idx].Type == NodeType.PreBoss || lane[idx].Type == NodeType.Boss || lane[idx].IsWarden) continue;
                    var prevEcon = idx > 0 && IsEconomy(lane[idx - 1].Type);
                    var nextEcon = idx < lane.Count - 1 && IsEconomy(lane[idx + 1].Type);
                    if (prevEcon || nextEcon) continue;
                    lane[idx].Type = NodeType.Shop;
                    return;
                }
            }
        }

        /// <summary>[REQ: DAILY-WALK-009] One event tile on the Daily Walk's path, on the puzzle nearest the middle (the goal
        /// "Read an event" needs one). The walk's path rolls no event tiles; the Garden Run's routes do (graph version 3).</summary>
        private static void EnsureEvent(List<RunNode> lane)
        {
            var mid = lane.Count / 2;
            for (var offset = 0; offset <= mid; offset++)
                foreach (var idx in new[] { mid + offset, mid - offset })
                {
                    if (idx <= 0 || idx >= lane.Count || lane[idx].Type != NodeType.Puzzle) continue;
                    lane[idx].Type = NodeType.Event;
                    return;
                }
        }

        private static bool IsEconomy(NodeType type) =>
            type == NodeType.Shop || type == NodeType.Rest
            || type == NodeType.Relic || type == NodeType.CrossLink;

        // ── Cross-Links ──

        // [REQ: MAP-CROSS-001] Exactly one cross-connection per floor, between the Calm tile at index floor(CalmLength/2)
        // and the Risk tile at index floor(RiskLength/2). Both tiles keep their type (Puzzle, Shop, …); IsCrossLink marks
        // them, and the edge lets the player switch lanes there. The pre-boss tile at the end of a lane is never an anchor.
        private static List<(RunNode calm, RunNode risk)> InsertSingleCrossLink(List<RunNode> calm, List<RunNode> risk)
        {
            var pairs = new List<(RunNode, RunNode)>();
            if (calm.Count < 2 || risk.Count < 2) return pairs;
            var calmPos = Math.Min(calm.Count / 2, calm.Count - 2);
            var riskPos = Math.Min(risk.Count / 2, risk.Count - 2);
            calm[calmPos].NextNodes.Add(risk[riskPos].Index);
            risk[riskPos].NextNodes.Add(calm[calmPos].Index);
            calm[calmPos].IsCrossLink = true;
            risk[riskPos].IsCrossLink = true;
            pairs.Add((calm[calmPos], risk[riskPos]));
            return pairs;
        }

        // Legacy layout (graph version 0/1, saves from 1.0): bidirectional bridges between lanes; count = floor+1 (clamped);
        // evenly distributed, no two share a position index; bridge tiles become NodeType.CrossLink.
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
            // One path (the Daily Walk) runs along the middle.
            var laneOffset = risk.Count == 0 ? 0f : floorIndex <= 1 ? 0.18f : floorIndex == 2 ? 0.22f : 0.26f;
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
