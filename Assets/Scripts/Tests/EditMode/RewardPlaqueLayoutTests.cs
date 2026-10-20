using NUnit.Framework;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-STYLE-007] GAP-056: with more than three rewards the plaques shrink to fit the cord right of the tally
    // instead of running off the screen and over the tally (five plaques in the 0.2.0 playtest).
    [TestFixture]
    [Requirement("UI-STYLE-007")]
    public sealed class RewardPlaqueLayoutTests
    {
        private const float TallyRight = 0.2938f;

        [Test]
        public void UpToThree_KeepTheMockupSize()
        {
            for (var count = 1; count <= 3; count++)
                Assert.AreEqual((0.638f, 0.1875f, 0.1719f, 0.3333f), RewardViewController.PlaqueLayout(count));
        }

        [Test]
        public void FourToNine_StayOnScreenRightOfTheTally_AndKeepTheirShape()
        {
            for (var count = 4; count <= 9; count++)
            {
                var (middle, step, width, bottom) = RewardViewController.PlaqueLayout(count);
                var left = middle - (count - 1) * 0.5f * step - width * 0.5f;
                var right = middle + (count - 1) * 0.5f * step + width * 0.5f;
                Assert.Greater(left, TallyRight, $"{count} plaques: the first one clears the tally");
                Assert.LessOrEqual(right, 1f, $"{count} plaques: the last one stays on the screen");
                Assert.Less(width, step, $"{count} plaques: they do not overlap");
                Assert.AreEqual(0.1719f / (0.7407f - 0.3333f), width / (0.7407f - bottom), 0.001f,
                    $"{count} plaques: width and height shrink together");
            }
        }
    }
}
