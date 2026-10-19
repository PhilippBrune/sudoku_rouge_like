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

        private static readonly (string name, Color color)[] LineColours =
        {
            ("German Whispers", GamePalette.LineGermanWhispers), ("Dutch Whispers", GamePalette.LineDutchWhispers),
            ("Parity", GamePalette.LineParityLine), ("Renban", GamePalette.LineRenbanLine),
            ("Palindrome", GamePalette.LinePalindrome), ("Thermo", GamePalette.LineThermo),
            ("Between", GamePalette.LineBetweenLine), ("Consecutive", GamePalette.LineConsecutiveLine),
            ("Slow Thermo", GamePalette.LineSlowThermo), ("Unique Set", GamePalette.LineUniqueSetLine)
        };

        // GAP-048: the colours puzzle setters use. Hue in degrees from Color.RGBToHSV.
        [Test]
        public void LineColours_FollowTheCommonConvention()
        {
            Assert.That(Hue(GamePalette.LineGermanWhispers), Is.InRange(90f, 150f), "German Whispers are green");
            Assert.That(Hue(GamePalette.LineDutchWhispers), Is.InRange(15f, 35f), "Dutch Whispers are orange");
            Assert.That(Hue(GamePalette.LineRenbanLine), Is.InRange(300f, 340f), "Renban lines are pink");
            Assert.That(Saturation(GamePalette.LinePalindrome), Is.LessThan(0.08f), "Palindromes are grey");
            Assert.That(Saturation(GamePalette.LineThermo), Is.LessThan(0.08f), "thermometers are grey");
            Assert.That(Hue(GamePalette.LineConsecutiveLine), Is.InRange(40f, 60f), "Consecutive lines are gold");
        }

        [Test]
        public void LineColours_AreOpaque_TellApart_AndKeepInkDigitsReadable()
        {
            for (var i = 0; i < LineColours.Length; i++)
            {
                var (name, color) = LineColours[i];
                Assert.AreEqual(1f, color.a, $"{name}: opaque, so overlapping segments never darken");
                Assert.GreaterOrEqual(Contrast(GamePalette.CellNumberGiven, color), MinTextContrast, $"ink digit on {name}");
                for (var j = i + 1; j < LineColours.Length; j++)
                    Assert.Greater(Distance(color, LineColours[j].color), 0.2f, $"{name} vs {LineColours[j].name}");
            }
        }

        private static float Hue(Color c)
        {
            Color.RGBToHSV(c, out var h, out _, out _);
            return h * 360f;
        }

        private static float Saturation(Color c)
        {
            Color.RGBToHSV(c, out _, out var s, out _);
            return s;
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
