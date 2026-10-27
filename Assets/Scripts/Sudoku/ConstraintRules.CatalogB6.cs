using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Sudoku
{
    // Batch B6 of the modifier catalog (docs/BossMechanicsSystem.md "Modifier catalog"): a whisper with its own K, the inverted
    // whisper zone, the 1:3 dot, distance pairs, opposite sums, counting circles and distance cells. The ratio cage, pill
    // arrow, N-line and sum line live with their families (ConstraintRules.Cages.cs, ConstraintRules.ArrowsSums.cs).
    // Every rule reads only the digits already on the board, so it is safe on partial boards.

    internal static class PairRuleSupport
    {
        /// <summary>The other cell of the pair when (row, col) is one of its two cells.</summary>
        internal static bool Partner(CellCoord a, CellCoord b, int row, int col, out CellCoord other)
        {
            if (a.Row == row && a.Col == col) { other = b; return true; }
            if (b.Row == row && b.Col == col) { other = a; return true; }
            other = default;
            return false;
        }

        /// <summary>True when some digit on the board is exactly <paramref name="difference"/> away from value.</summary>
        internal static bool HasPartnerAt(int value, int difference, int size) =>
            value + difference <= size || value - difference >= 1;
    }

    // [REQ: BOSS-LIB-037] Whisper (any K): neighbours on a magenta line differ by at least the number on its tag.
    public sealed class WhisperKLineRule : IOrderedConstraintRule
    {
        public const int DefaultK = 3;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.WhisperGeneralized) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                var k = line.LineValue > 0 ? line.LineValue : DefaultK;
                if (value - k < 1 && value + k > board.Size) return false;   // no neighbour could be far enough away
                for (var n = j - 1; n <= j + 1; n += 2)
                {
                    if (n < 0 || n >= line.Cells.Count) continue;
                    var p = board.Cells[line.Cells[n].Row, line.Cells[n].Col];
                    if (p != 0 && Math.Abs(value - p) < k) return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-093] Inverted Zone: green lines are whispers (neighbours differ by 5 or more), except for a step
    // between two cells inside the shaded zone, where the neighbours differ by less than 5.
    public sealed class ZoneWhisperRule : IOrderedConstraintRule
    {
        public const int Gap = 5;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Line;

        internal static bool InZone(ModifierOverlayData overlay, CellCoord c)
        {
            foreach (var z in overlay.NegationZone)
                if (z.Row == c.Row && z.Col == c.Col) return true;
            return false;
        }

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.Lines.Count; i++)
            {
                var line = overlay.Lines[i];
                if (line.Type != LineType.ZoneWhisper) continue;
                var j = LineRuleSupport.IndexOf(line, row, col);
                if (j < 0) continue;
                var here = InZone(overlay, line.Cells[j]);
                for (var n = j - 1; n <= j + 1; n += 2)
                {
                    if (n < 0 || n >= line.Cells.Count) continue;
                    var inside = here && InZone(overlay, line.Cells[n]);
                    var p = board.Cells[line.Cells[n].Row, line.Cells[n].Col];
                    if (p == 0)
                    {
                        // An empty neighbour outside the zone needs some digit at least 5 away.
                        if (!inside && !PairRuleSupport.HasPartnerAt(value, Gap, board.Size)) return false;
                        continue;
                    }
                    var difference = Math.Abs(value - p);
                    if (inside ? difference >= Gap : difference < Gap) return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-061] Ratio 1:3: across a black dot marked 3, one cell is three times the other.
    public sealed class RatioNPairRule : IOrderedConstraintRule
    {
        public const int DefaultN = 3;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.Dot;

        internal static int N(AdjacentPairConstraint pair) => pair.Value >= 2 ? pair.Value : DefaultN;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.PairConstraints.Count; i++)
            {
                var pair = overlay.PairConstraints[i];
                if (pair.Type != PairConstraintType.RatioN) continue;
                if (!PairRuleSupport.Partner(pair.CellA, pair.CellB, row, col, out var other)) continue;
                var n = N(pair);
                var ov = board.Cells[other.Row, other.Col];
                if (ov != 0)
                {
                    if (Math.Max(value, ov) != Math.Min(value, ov) * n) return false;
                }
                else if (value * n > board.Size && value % n != 0) return false;
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-063] Distance Pair: two linked cells differ by the number of steps between them.
    // [REQ: BOSS-LIB-077] Opposite Sums: a numbered cell and its mirror cell through the centre add up to the number.
    public sealed class CellPairValueRule : IOrderedConstraintRule
    {
        private readonly CellPairType _type;

        public CellPairValueRule(CellPairType type) => _type = type;

        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            for (var i = 0; i < overlay.CellPairs.Count; i++)
            {
                var pair = overlay.CellPairs[i];
                if (pair.PairType != _type) continue;
                if (!PairRuleSupport.Partner(pair.CellA, pair.CellB, row, col, out var other)) continue;
                var ov = board.Cells[other.Row, other.Col];
                if (_type == CellPairType.Distance)
                {
                    if (ov != 0 ? Math.Abs(value - ov) != pair.Value : !PairRuleSupport.HasPartnerAt(value, pair.Value, board.Size))
                        return false;
                }
                else if (_type == CellPairType.OppositeSum)
                {
                    if (ov != 0 ? value + ov != pair.Value : pair.Value - value < 1 || pair.Value - value > board.Size)
                        return false;
                }
            }
            return true;
        }
    }

    // [REQ: BOSS-LIB-078] Counting Circles: a circled digit says how many circles hold it (a circled 3: exactly three
    // circles hold 3).
    public sealed class CountingCirclesRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            if (value < 1 || value > board.Size) return false;
            var circled = false;
            var same = 1;          // circles holding `value`, the candidate included
            var empty = 0;
            Span<int> counts = stackalloc int[board.Size + 1];
            counts[value]++;
            foreach (var m in overlay.CellMarkers)
            {
                if (m.Type != MarkerType.CountingCircle) continue;
                if (m.Cell.Row == row && m.Cell.Col == col) { circled = true; continue; }
                var v = board.Cells[m.Cell.Row, m.Cell.Col];
                if (v == 0) { empty++; continue; }
                counts[v]++;
                if (v == value) same++;
            }
            if (!circled) return true;
            if (same > value) return false;
            if (empty > 0) return true;
            // Every circle is filled: each digit present appears exactly as often as its value.
            for (var d = 1; d <= board.Size; d++)
                if (counts[d] != 0 && counts[d] != d) return false;
            return true;
        }
    }

    // [REQ: BOSS-LIB-080] Distance Cells: a cell with a navy dot holds its distance in steps (up, down, left, right) to
    // the cell in the navy ring.
    public sealed class DistanceCellRule : IOrderedConstraintRule
    {
        public ConstraintRuleCategory Category => ConstraintRuleCategory.CellLevel;

        public bool IsValid(SudokuBoard board, int row, int col, int value, ModifierOverlayData overlay)
        {
            if (overlay == null) return true;
            CellMarker target = null;
            var source = false;
            foreach (var m in overlay.CellMarkers)
            {
                if (m.Type == MarkerType.DistanceCellTarget) target = m;
                else if (m.Type == MarkerType.DistanceCellSource && m.Cell.Row == row && m.Cell.Col == col) source = true;
            }
            if (!source || target == null) return true;
            return value == Math.Abs(row - target.Cell.Row) + Math.Abs(col - target.Cell.Col);
        }
    }
}
