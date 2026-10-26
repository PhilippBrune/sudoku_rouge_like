using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Economy
{
    /// <summary>
    /// [REQ: RELIC-SIG-001] [REQ: RELIC-SIG-002] Each class has a signature relic from class Level 1 that plays to its
    /// passive; an offering puts it among the choices with a higher chance while the player does not hold it.
    /// [REQ: RELIC-KEY-001] [REQ: RELIC-KEY-002] After the first boss the offering table offers three of the four keystones,
    /// relics that bend a rule of the puzzle for the whole run.
    /// </summary>
    public sealed partial class RelicService
    {
        public const double SignatureChance = 0.35;

        public static readonly IReadOnlyDictionary<ClassId, RelicId> SignatureRelics = new Dictionary<ClassId, RelicId>
        {
            [ClassId.NumberFreak] = RelicId.CountingBeads,
            [ClassId.GardenMonk] = RelicId.PrayerBeads,
            [ClassId.ShrineArchivist] = RelicId.InkStone,
            [ClassId.KoiGambler] = RelicId.LuckyScale,
            [ClassId.StoneGardener] = RelicId.MossPouch,
            [ClassId.LanternSeer] = RelicId.SecondWick,
            [ClassId.ReedDuelist] = RelicId.SingleStroke,
            [ClassId.QuietCartographer] = RelicId.CompassNeedle,
        };

        public static readonly RelicId[] Keystones = { RelicId.GildedBox, RelicId.OpenMargin, RelicId.SteadyHand, RelicId.CageWhisper };

        public static bool IsSignature(RelicId id) => id >= RelicId.CountingBeads && id <= RelicId.CompassNeedle;
        public static bool IsKeystone(RelicId id) => Array.IndexOf(Keystones, id) >= 0;

        public static ClassId? SignatureClass(RelicId id)
        {
            foreach (var pair in SignatureRelics)
                if (pair.Value == id) return pair.Key;
            return null;
        }

        /// <summary>Three of the four keystones, in a seeded order (the boss table after Floor 1).</summary>
        public List<RelicInstance> RollKeystoneChoices(int count = 3)
        {
            var pool = new List<RelicId>(Keystones);
            var choices = new List<RelicInstance>(count);
            while (choices.Count < count && pool.Count > 0)
            {
                var index = _random.Next(pool.Count);
                choices.Add(new RelicInstance { Id = pool[index], Tier = RelicTier.Keystone, UsesRemaining = -1 });
                pool.RemoveAt(index);
            }
            return choices;
        }

        // The class's signature relic for an offering, or null (held already, offered already, or not this time).
        private RelicInstance RollSignature(ClassId classId, IReadOnlyList<RelicInstance> held, ICollection<RelicId> offered)
        {
            if (!SignatureRelics.TryGetValue(classId, out var id) || offered.Contains(id)) return null;
            if (held != null)
                foreach (var relic in held)
                    if (relic != null && relic.Id == id) return null;
            return _random.NextDouble() < SignatureChance
                ? new RelicInstance { Id = id, Tier = RelicTier.Tier2, UsesRemaining = -1 }
                : null;
        }

        /// <summary>Per-puzzle relic flags, cleared when a puzzle starts.</summary>
        private static void ResetPuzzleFlags(RunState state)
        {
            state.FirstMistakeShieldUsed = false;
            state.SingleStrokeUsed = false;
        }

        /// <summary>Steady Hand (any mistake) or Prayer Beads (while HP are full): the puzzle's first mistake costs no HP.</summary>
        public static bool TryFirstMistakeShield(RunState state)
        {
            if (state == null || state.FirstMistakeShieldUsed) return false;
            var shields = HasRelicOfType(state, RelicId.SteadyHand)
                || (HasRelicOfType(state, RelicId.PrayerBeads) && state.CurrentHP >= state.MaxHP);
            if (!shields) return false;
            state.FirstMistakeShieldUsed = true;
            return true;
        }

        /// <summary>Single Stroke: with no pencil mark on the board, the puzzle's first wrong digit is taken back.</summary>
        public static bool TrySingleStroke(RunState state, SudokuBoard board)
        {
            if (state == null || board == null || state.SingleStrokeUsed || !HasRelicOfType(state, RelicId.SingleStroke)) return false;
            for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.HasAnyPencilMark(r, c)) return false;
            state.SingleStrokeUsed = true;
            return true;
        }

        /// <summary>Open Margin: pencil marks cost nothing on boards up to 6×6.</summary>
        public static bool PencilFreeOnBoard(RunState state, int boardSize) =>
            boardSize <= 6 && HasRelicOfType(state, RelicId.OpenMargin);

        /// <summary>After a correct digit: Counting Beads (the digit's last copy, +1 Pencil) and Gilded Box (a finished box
        /// or region, +1 gold).</summary>
        public static void OnDigitPlaced(RunState state, SudokuBoard board, int row, int col, int value)
        {
            if (state == null || board == null) return;
            if (HasRelicOfType(state, RelicId.CountingBeads) && CountOf(board, value) >= board.Size)
                state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + 1);
            if (HasRelicOfType(state, RelicId.GildedBox) && RegionFilled(board, board.RegionMap[row, col]))
                state.GainGold(1);
        }

        private static int CountOf(SudokuBoard board, int value)
        {
            var count = 0;
            for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.Cells[r, c] == value) count++;
            return count;
        }

        private static bool RegionFilled(SudokuBoard board, int region)
        {
            for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.RegionMap[r, c] == region && board.Cells[r, c] == 0) return false;
            return true;
        }

        /// <summary>Second Wick: reachable cursed tiles name their extra rule on the path.</summary>
        public static bool RevealsCursedRules(RunState state) => HasRelicOfType(state, RelicId.SecondWick);

        /// <summary>Cage Whisper: cages with a hidden sum show its last digit.</summary>
        public static bool WhispersHiddenSums(RunState state) => HasRelicOfType(state, RelicId.CageWhisper);
    }
}
