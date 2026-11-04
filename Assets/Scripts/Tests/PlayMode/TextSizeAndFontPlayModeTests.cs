using System.Collections;
using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // RM-106 (owner 2026-11-04): Text Size on every screen and the OpenDyslexic option.
    [TestFixture]
    public sealed class TextSizeAndFontPlayModeTests
    {
        private GameObject _canvas;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("TextSizeCanvas", typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        [TearDown]
        public void TearDown()
        {
            TextSizeFit.SetScale(1f);
            FontAssetService.SetDyslexiaFont(false);
            Object.Destroy(_canvas);
        }

        private Text Make(string text, Vector2 box, bool follow = true)
        {
            // As InRunUiFactory.CreateText builds a text (followTextSize adds the fit).
            var t = new GameObject("T", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(_canvas.transform, false);
            t.font = FontAssetService.GetFont();
            t.text = text;
            t.fontSize = 20;
            if (follow) t.gameObject.AddComponent<TextSizeFit>();
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.rectTransform.sizeDelta = box;
            return t;
        }

        [UnityTest]
        [Requirement("ACCESS-FONT-003")]
        public IEnumerator TextSize_GrowsWhereItFits_StaysInATightBox_AndLeavesTheBoardAlone()
        {
            var roomy = Make("Start", new Vector2(600f, 120f));
            var tight = Make("A long label that has to wrap", new Vector2(80f, 26f));
            var board = Make("7", new Vector2(600f, 120f), follow: false);
            var label = Make("Colorblind Mode", new Vector2(190f, 120f));   // one line at 20, two at 30
            yield return null;
            Assert.AreEqual(20, roomy.fontSize, "100 % keeps the built size");

            TextSizeFit.SetScale(1.5f);
            yield return null;
            Assert.AreEqual(30, roomy.fontSize, "150 % where the box has room");
            Assert.AreEqual(20, tight.fontSize, "a box too small keeps the built size and never goes below it");
            Assert.AreEqual(20, board.fontSize, "the board sizes its own digits (ACCESS-FONT-001)");
            Assert.That(label.fontSize, Is.InRange(21, 29), "a label grows only as far as it stays on one line");

            roomy.text = "Start";   // a new text is fitted again
            roomy.fontSize = 24;    // another script's size becomes the built size
            yield return null;
            Assert.AreEqual(36, roomy.fontSize);

            TextSizeFit.SetScale(0.8f);
            yield return null;
            Assert.AreEqual(19, roomy.fontSize, "below 100 % the text shrinks");
            Assert.AreEqual(16, tight.fontSize);
        }

        [UnityTest]
        [Requirement("ACCESS-FONT-004")]
        public IEnumerator DyslexiaFont_SwitchesEveryTextAtOnce_AndBack()
        {
            var shown = Make("Garden", new Vector2(300f, 60f));
            var tight = Make("A long label that has to wrap", new Vector2(80f, 26f));
            var main = shown.font;
            FontAssetService.SetDyslexiaFont(true);
            yield return null;
            Assert.AreEqual("OpenDyslexic", shown.font.name, "the text on screen changes");
            Assert.AreEqual(20, shown.fontSize, "where it fits it keeps its size");
            Assert.AreEqual(15, tight.fontSize, "the wider font may shrink to 75 % where the box is too small");
            Assert.AreEqual("OpenDyslexic", Make("New", new Vector2(300f, 60f)).font.name, "and the next one is built with it");

            FontAssetService.SetDyslexiaFont(false);
            yield return null;
            Assert.AreSame(main, shown.font);
            Assert.AreEqual(20, tight.fontSize, "back in Roboto it is back at its built size");
        }
    }
}
