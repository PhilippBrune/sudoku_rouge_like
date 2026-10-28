using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Tutorial
{
    /// <summary>
    /// [REQ: CUSTOM-CODE-001] A Custom Puzzle's share code (RM-209): the size, stars, regions, resources, class, rules and
    /// seed in about twenty letters and digits, so a friend who pastes it plays the same board. "R1-" and Crockford
    /// base32 in groups of four: no I, L, O or U, and a pasted I, L or O reads as 1, 1 or 0. The last byte is a check, so a
    /// mistyped code is refused rather than read as another puzzle.
    /// </summary>
    public static class CustomPuzzleCode
    {
        public const string Prefix = "R1";
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        private const int MaxBytes = 2 + TutorialModeService.CustomModifierLimit + 4 + 1;

        public static string Encode(TutorialSetupConfig setup)
        {
            var mods = setup.SelectedModifiers ?? new List<BossModifierId>();
            var bytes = new List<byte>
            {
                (byte)(((setup.BoardSize - 5) & 7) << 5 | ((setup.Stars - 1) & 7) << 2 | (setup.RegionVariant & 3)),
                (byte)((setup.ResourceMode == TutorialResourceMode.Simulation ? 1 : 0) << 7
                       | ((int)setup.SimulationClassId & 15) << 3 | (Math.Min(mods.Count, 7) & 7)),
            };
            foreach (var mod in mods) bytes.Add((byte)mod);
            bytes.AddRange(BitConverter.GetBytes(setup.Seed));
            bytes.Add(Check(bytes));

            var text = ToBase32(bytes);
            var grouped = new StringBuilder(Prefix);
            for (var i = 0; i < text.Length; i += 4)
                grouped.Append('-').Append(text, i, Math.Min(4, text.Length - i));
            return grouped.ToString();
        }

        /// <summary>Reads a pasted code; false for anything that is not a whole, valid code of this version.</summary>
        public static bool TryDecode(string code, out TutorialSetupConfig setup)
        {
            setup = null;
            if (string.IsNullOrWhiteSpace(code) || code.Length > 64) return false;
            var text = code.Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
            if (!text.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            if (!TryFromBase32(text.Substring(Prefix.Length), out var bytes) || bytes.Count < 7 || bytes.Count > MaxBytes) return false;
            if (bytes[bytes.Count - 1] != Check(bytes.GetRange(0, bytes.Count - 1))) return false;

            var size = (bytes[0] >> 5) + 5;
            var stars = ((bytes[0] >> 2) & 7) + 1;
            var region = bytes[0] & 3;
            var classId = (bytes[1] >> 3) & 15;
            var modCount = bytes[1] & 7;
            if (size > 9 || stars > 7 || region == 2 || modCount > TutorialModeService.CustomModifierLimit
                || !Enum.IsDefined(typeof(ClassId), classId) || bytes.Count != 2 + modCount + 4 + 1)
                return false;

            var mods = new List<BossModifierId>();
            for (var i = 0; i < modCount; i++)
            {
                var mod = (BossModifierId)bytes[2 + i];
                if (!Enum.IsDefined(typeof(BossModifierId), mod) || mods.Contains(mod)) return false;
                mods.Add(mod);
            }
            setup = new TutorialSetupConfig
            {
                BoardSize = size,
                Stars = stars,
                RegionVariant = region,
                ResourceMode = (bytes[1] & 0x80) != 0 ? TutorialResourceMode.Simulation : TutorialResourceMode.Free,
                SimulationClassId = (ClassId)classId,
                SelectedModifiers = mods,
                Seed = BitConverter.ToInt32(bytes.ToArray(), 2 + modCount),
            };
            return true;
        }

        private static byte Check(IEnumerable<byte> bytes)
        {
            unchecked
            {
                var h = 0x9Du;
                foreach (var b in bytes) h = (h ^ b) * 0x1Bu;
                return (byte)(h ^ (h >> 8));
            }
        }

        private static string ToBase32(List<byte> bytes)
        {
            var sb = new StringBuilder();
            int buffer = 0, bits = 0;
            foreach (var b in bytes)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        private static bool TryFromBase32(string text, out List<byte> bytes)
        {
            bytes = new List<byte>();
            int buffer = 0, bits = 0;
            foreach (var raw in text)
            {
                var c = raw == 'I' || raw == 'L' ? '1' : raw == 'O' ? '0' : raw;
                var value = Alphabet.IndexOf(c);
                if (value < 0) return false;
                buffer = ((buffer << 5) | value) & 0xFFFF;
                bits += 5;
                if (bits >= 8)
                {
                    bytes.Add((byte)(buffer >> (bits - 8)));
                    bits -= 8;
                }
            }
            return bytes.Count > 0;
        }
    }
}
