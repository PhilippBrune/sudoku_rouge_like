using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RouteService
    {
        public List<int> GetReachableNodes(List<RunNode> graph, int currentNodeIndex)
        {
            var reachable = new List<int>();
            if (currentNodeIndex < 0 || currentNodeIndex >= graph.Count) return reachable;

            var current = graph[currentNodeIndex];
            for (var i = 0; i < current.NextNodes.Count; i++)
            {
                var nextIndex = current.NextNodes[i];
                if (nextIndex >= 0 && nextIndex < graph.Count)
                    reachable.Add(nextIndex);
            }

            return reachable;
        }

        public bool CanMoveTo(List<RunNode> graph, int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= graph.Count) return false;
            return graph[fromIndex].NextNodes.Contains(toIndex);
        }

        public void MarkVisited(List<RunNode> graph, int nodeIndex)
        {
            if (nodeIndex >= 0 && nodeIndex < graph.Count)
                graph[nodeIndex].Visited = true;
        }

        public void UpdateReachability(List<RunNode> graph, int currentNodeIndex)
        {
            for (var i = 0; i < graph.Count; i++)
                graph[i].Reachable = false;

            if (currentNodeIndex < 0 || currentNodeIndex >= graph.Count) return;

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(currentNodeIndex);
            visited.Add(currentNodeIndex);

            while (queue.Count > 0)
            {
                var idx = queue.Dequeue();
                graph[idx].Reachable = true;

                var node = graph[idx];
                for (var i = 0; i < node.NextNodes.Count; i++)
                {
                    var next = node.NextNodes[i];
                    if (next >= 0 && next < graph.Count && visited.Add(next))
                        queue.Enqueue(next);
                }
            }
        }
    }
}
