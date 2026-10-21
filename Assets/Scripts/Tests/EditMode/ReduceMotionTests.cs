using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: ACCESS-MOTION-002] Reduce Motion stops the scale pulses and pops (path nodes, the boss node, digits, the combo,
    // reward plaques): the element keeps its size and the animation ends at once.
    [TestFixture]
    [Requirement("ACCESS-MOTION-002")]
    public sealed class ReduceMotionTests
    {
        private GameObject _target;

        [SetUp]
        public void SetUp() => _target = new GameObject("PulseTarget");

        [TearDown]
        public void TearDown()
        {
            OptionsController.LiveAccessibility.ReduceMotion = false;
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void PulseScale_WithReduceMotion_EndsAtOnceAtTheBaseScale()
        {
            OptionsController.LiveAccessibility.ReduceMotion = true;
            _target.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            var pulse = AnimationHelper.PulseScale(_target.transform, 1f, 1.3f, 0.4f);

            Assert.IsFalse(pulse.MoveNext(), "no frame of animation");
            Assert.AreEqual(Vector3.one, _target.transform.localScale);
        }

        [Test]
        public void PulseScale_WithoutReduceMotion_Animates()
        {
            OptionsController.LiveAccessibility.ReduceMotion = false;

            var pulse = AnimationHelper.PulseScale(_target.transform, 1f, 1.3f, 0.4f);

            Assert.IsTrue(pulse.MoveNext(), "the pulse runs over several frames");
        }
    }
}
