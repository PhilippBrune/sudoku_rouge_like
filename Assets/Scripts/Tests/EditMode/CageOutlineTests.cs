using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // A killer cage is one dashed outline around all its cells, not one box per cell (owner report 2026-10-15).
    [TestFixture]
    [Requirement("UI-STYLE-006")]
    public sealed class CageOutlineTests
    {
        private const float Cell = 60f;
        private const float Gap = 2f;
        private const float Inset = 5f;

        [Test]
        public void TwoCellCage_IsOneRectangle_WithNoLineBetweenTheCells()
        {
            var runs = Build((4, 3), (4, 4));

            Assert.AreEqual(4, runs.Count, Describe(runs));
            var top = runs.Single(r => r.Horizontal && r.From.y < 4 * (Cell + Gap) + Cell / 2);
            Assert.AreEqual(3 * (Cell + Gap) + Inset, Mathf.Min(top.From.x, top.To.x), 0.001f);
            Assert.AreEqual(4 * (Cell + Gap) + Cell - Inset, Mathf.Max(top.From.x, top.To.x), 0.001f);
            var shared = 4 * (Cell + Gap) - Gap / 2f;
            Assert.IsFalse(runs.Any(r => !r.Horizontal && Mathf.Abs(r.From.x - shared) < Cell / 2),
                "no vertical line may separate the two cells: " + Describe(runs));
        }

        [Test]
        public void LShapedCage_IsOneClosedLoop_ThatTurnsIntoTheInnerCorner()
        {
            var runs = Build((1, 1), (2, 1), (3, 1), (2, 0));

            Assert.AreEqual(8, runs.Count, Describe(runs));
            AssertOneClosedLoop(runs);
            // The concave corner above (2,0): the top run of (2,0) reaches into column 1 and meets its left border.
            var corner = new Vector2(1 * (Cell + Gap) + Inset, 2 * (Cell + Gap) + Inset);
            Assert.AreEqual(2, runs.Count(r => Near(r.From, corner) || Near(r.To, corner)), Describe(runs));
        }

        [Test]
        public void EveryCageShapeUpToFourCells_IsOneClosedLoop()
        {
            var shapes = ConnectedShapes(4, 4);
            Assert.Greater(shapes.Count, 100, "the enumeration should cover all dominoes, trominoes and tetrominoes");
            foreach (var shape in shapes)
                AssertOneClosedLoop(Build(shape.ToArray()), string.Join(" ", shape));
        }

        [Test]
        public void Dashes_StartAndEndOnADash_SoTheCornersAreClosed()
        {
            foreach (var length in new[] { 3f, 8f, 20f, 57f, 124f, 250f })
            {
                var dashes = CageOutline.Dashes(length, 8f, 5f);
                Assert.AreEqual(0f, dashes[0].offset, 0.001f, $"length {length}");
                var last = dashes[dashes.Count - 1];
                Assert.AreEqual(length, last.offset + last.length, 0.001f, $"length {length}");
                for (var i = 1; i < dashes.Count; i++)
                    Assert.GreaterOrEqual(dashes[i].offset - (dashes[i - 1].offset + dashes[i - 1].length), 5f - 0.001f,
                        $"length {length}: gaps are only ever stretched");
            }
        }

        // ── helpers ──

        private static List<CageOutline.Run> Build(params (int r, int c)[] cells) =>
            CageOutline.Build(cells.Select(x => new CellCoord(x.r, x.c)).ToList(), 9, Cell, Cell, Gap, Gap, Inset);

        private static void AssertOneClosedLoop(List<CageOutline.Run> runs, string label = "")
        {
            // Every corner joins exactly one horizontal and one vertical run, and all runs form a single cycle.
            var corners = new Dictionary<Vector2Int, List<int>>();
            for (var i = 0; i < runs.Count; i++)
                foreach (var p in new[] { runs[i].From, runs[i].To })
                {
                    var key = Vector2Int.RoundToInt(p * 1000f);
                    if (!corners.TryGetValue(key, out var list)) corners[key] = list = new List<int>();
                    list.Add(i);
                }
            foreach (var pair in corners)
            {
                Assert.AreEqual(2, pair.Value.Count, $"{label}: corner {pair.Key} has {pair.Value.Count} runs; {Describe(runs)}");
                Assert.AreNotEqual(runs[pair.Value[0]].Horizontal, runs[pair.Value[1]].Horizontal,
                    $"{label}: corner {pair.Key} does not turn; {Describe(runs)}");
            }

            var seen = new HashSet<int> { 0 };
            var stack = new Stack<int>(seen);
            while (stack.Count > 0)
            {
                var run = runs[stack.Pop()];
                foreach (var p in new[] { run.From, run.To })
                foreach (var next in corners[Vector2Int.RoundToInt(p * 1000f)])
                    if (seen.Add(next)) stack.Push(next);
            }
            Assert.AreEqual(runs.Count, seen.Count, $"{label}: the outline falls apart; {Describe(runs)}");
        }

        private static List<List<(int, int)>> ConnectedShapes(int maxCells, int area)
        {
            var found = new HashSet<string>();
            var shapes = new List<List<(int, int)>>();
            var frontier = new List<List<(int, int)>>();
            for (var r = 0; r < area; r++)
            for (var c = 0; c < area; c++)
                frontier.Add(new List<(int, int)> { (r, c) });
            for (var size = 2; size <= maxCells; size++)
            {
                var grown = new List<List<(int, int)>>();
                foreach (var shape in frontier)
                foreach (var (r, c) in shape)
                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    var next = (r + dr, c + dc);
                    if (next.Item1 < 0 || next.Item2 < 0 || next.Item1 >= area || next.Item2 >= area) continue;
                    if (shape.Contains(next)) continue;
                    var bigger = shape.Concat(new[] { next }).OrderBy(x => x).ToList();
                    if (!found.Add(string.Join(";", bigger))) continue;
                    grown.Add(bigger);
                    shapes.Add(bigger);
                }
                frontier = grown;
            }
            return shapes;
        }

        private static bool Near(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 0.0001f;

        private static string Describe(IEnumerable<CageOutline.Run> runs) =>
            string.Join(", ", runs.Select(r => $"({r.From.x:0.#},{r.From.y:0.#})-({r.To.x:0.#},{r.To.y:0.#})"));
    }
}
