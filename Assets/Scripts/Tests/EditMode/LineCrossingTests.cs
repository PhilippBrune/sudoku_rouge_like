using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Tests
{
    // [REQ: GEN-MOD-002] [REQ: BOSS-MOD-002] GAP-050: no generated line or arrow path forms an X with itself (two diagonal
    // steps through the same 2×2 square). BossMechanicsSystem "Line Constraints": 8-directional adjacency, no
    // self-crossing. Covers every line and arrow modifier, the original ones included, on many seeds and sizes.
    [TestFixture]
    [Requirement("GEN-MOD-002", "BOSS-MOD-002")]
    public sealed class LineCrossingTests
    {
        private static readonly BossModifierId[] PathModifiers =
        {
            BossModifierId.GermanWhispers, BossModifierId.DutchWhispers, BossModifierId.ParityLines, BossModifierId.RenbanLines,
            BossModifierId.Palindrome, BossModifierId.Thermo, BossModifierId.BetweenLines, BossModifierId.ConsecutiveLine,
            BossModifierId.SlowThermo, BossModifierId.UniqueSetLine, BossModifierId.ArrowSums,
            BossModifierId.NonconsecLine, BossModifierId.HighLowAlternating, BossModifierId.NabnerLine, BossModifierId.EntropicLine,
            BossModifierId.ModularLine, BossModifierId.ZipperLine, BossModifierId.FastThermo, BossModifierId.AmbiguousThermo,
            BossModifierId.RegionSumLine, BossModifierId.WhisperGeneralized, BossModifierId.NLine, BossModifierId.SumLine,
            BossModifierId.ConstraintNegationZone, BossModifierId.ArrowAverage, BossModifierId.DoubleArrow, BossModifierId.PillArrow
        };

        [Test]
        public void NoLineOrArrowCrossesItself()
        {
            var failures = new List<string>();
            foreach (var mod in PathModifiers)
            {
                var minimum = BossService.GetMinimumBoardSize(mod);
                foreach (var size in new[] { minimum, 9 }.Distinct())
                for (var seed = 1; seed <= 12; seed++)
                {
                    var board = SudokuGenerator.CreatePuzzleWithUniquenessCheck(size, 0.5f, seed * 37 + size,
                        regionVariant: size == 6 || size == 8 || size == 9 ? 0 : 0);
                    var overlay = ModifierGeometryGenerator.Generate(board, new List<BossModifierId> { mod }, seed,
                        BossModifierIntensity.VeryHigh);
                    foreach (var path in Paths(overlay))
                        if (Crosses(path.Cells, out var square))
                            failures.Add($"{mod} {size}×{size} seed {seed}: {path.Name} crosses itself at {square}");
                }
            }
            Assert.IsEmpty(failures);
        }

        private static IEnumerable<(string Name, List<CellCoord> Cells)> Paths(ModifierOverlayData overlay)
        {
            foreach (var line in overlay.Lines) yield return (line.Type + " line", line.Cells);
            foreach (var arrow in overlay.Arrows)
            {
                var path = new List<CellCoord> { arrow.ArrowType == ArrowType.Pill ? arrow.SecondCircle : arrow.CircleCell };
                path.AddRange(arrow.ArrowCells);
                if (arrow.ArrowType == ArrowType.DoubleEndpoint) path.Add(arrow.SecondCircle);
                yield return (arrow.ArrowType + " arrow", path);
            }
        }

        // Two diagonal steps of the same path through one 2×2 square, one falling and one rising, form an X.
        internal static bool Crosses(List<CellCoord> cells, out string square)
        {
            var steps = new HashSet<(int, int, bool)>();
            for (var i = 0; i < cells.Count - 1; i++)
            {
                CellCoord a = cells[i], b = cells[i + 1];
                if (a.Row == b.Row || a.Col == b.Col) continue;
                var top = Math.Min(a.Row, b.Row);
                var left = Math.Min(a.Col, b.Col);
                var falling = (a.Row < b.Row) == (a.Col < b.Col);
                steps.Add((top, left, falling));
                if (!steps.Contains((top, left, !falling))) continue;
                square = $"({top},{left})";
                return true;
            }
            square = null;
            return false;
        }
    }
}
