using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-CURSOR-001] 0.3.1 playtest (owner): the pointer is a Japanese brush whose ink tip is the hot spot.
    [TestFixture]
    [Requirement("UI-CURSOR-001")]
    public sealed class BrushCursorTests
    {
        [Test]
        public void TheBrush_HasItsInkTipTopLeft_AndAClearCorner()
        {
            var tex = BrushCursor.Draw();
            try
            {
                Assert.AreEqual(BrushCursor.Size, tex.width);
                var nearTip = tex.GetPixel(4, BrushCursor.Size - 5);   // a few pixels down the hair from the hot spot (1, 1)
                Assert.AreEqual(1f, nearTip.a, 0.01f, "the hair is opaque");
                Assert.Less(nearTip.r + nearTip.g + nearTip.b, 0.9f, "and dark as ink");
                Assert.AreEqual(0f, tex.GetPixel(BrushCursor.Size - 1, BrushCursor.Size - 1).a, 0.01f, "the top right is see-through");
                Assert.Greater(tex.GetPixel(BrushCursor.Size - 5, 5).a, 0.9f, "the handle reaches the lower right");
            }
            finally { Object.DestroyImmediate(tex); }
        }
    }
}
