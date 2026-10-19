using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-STYLE-002] The paper board of the hanging scroll keeps its digits readable (WCAG 2.x contrast >= 4.5:1).
    // [REQ: BOSS-MOD-002] Parity Lines are blue as specified and stay apart from the teal Dutch Whispers (GAP-048).
    [TestFixture]
    [Requirement("UI-STYLE-002", "BOSS-MOD-002")]
    public sealed class PuzzleScrollPaletteTests
    {
        private const double MinTextContrast = 4.5;

        private static readonly Color[] PlainCells =
        {
            GamePalette.CellEmpty, GamePalette.CellGiven, GamePalette.CellRowCol, GamePalette.CellMatch,
            GamePalette.CellJigsawGivenAlt, GamePalette.CellJigsawGivenMain,
            GamePalette.CellJigsawEmptyAlt, GamePalette.CellJigsawEmptyMain
        };

        [Test]
        public void GivenAndPlacedDigits_AreReadable_OnEveryPlainCellState()
        {
            foreach (var cell in PlainCells)
            {
                Assert.GreaterOrEqual(Contrast(GamePalette.CellNumberGiven, cell), MinTextContrast, $"given digit on {cell}");
                Assert.GreaterOrEqual(Contrast(GamePalette.CellNumberPlaced, cell), MinTextContrast, $"placed digit on {cell}");
            }
        }

        [Test]
        public void Digits_OnSelectedConflictAndFogCells_UseTheirOwnReadableColour()
        {
            Assert.GreaterOrEqual(Contrast(GamePalette.CellNumberOnSelected, GamePalette.CellSelected), MinTextContrast, "selected");
            // A conflicting digit is written in ink (BoardViewController switches the label to CellNumberGiven).
            Assert.GreaterOrEqual(Contrast(GamePalette.CellNumberGiven, GamePalette.CellConflict), MinTextContrast, "conflict");
            Assert.GreaterOrEqual(Contrast(GamePalette.CellNumberOnFog, GamePalette.CellFog), MinTextContrast, "fog");
        }

        [Test]
        public void ParityLines_AreBlue_AndApartFromDutchWhispers()
        {
            var parity = GamePalette.LineParityLine;
            Assert.Greater(parity.b, parity.r + 0.3f, "parity lines are blue (BossMechanicsSystem.md)");
            Assert.Greater(parity.b, parity.g + 0.3f, "parity lines are blue, not teal");
            Assert.Greater(Distance(parity, GamePalette.LineDutchWhispers), 0.3f,
                "parity and Dutch whisper lines must look different on one board");
        }

        // WCAG 2.x relative luminance and contrast ratio; alpha is ignored (board colours are opaque).
        private static double Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (System.Math.Max(la, lb) + 0.05) / (System.Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color c) => 0.2126 * Channel(c.r) + 0.7152 * Channel(c.g) + 0.0722 * Channel(c.b);

        private static double Channel(float v) => v <= 0.03928 ? v / 12.92 : System.Math.Pow((v + 0.055) / 1.055, 2.4);

        private static float Distance(Color a, Color b) =>
            Mathf.Sqrt((a.r - b.r) * (a.r - b.r) + (a.g - b.g) * (a.g - b.g) + (a.b - b.b) * (a.b - b.b));
    }
}
