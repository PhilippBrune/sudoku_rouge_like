using NUnit.Framework;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: INPUT-PAD-002] X-input and D-input: the game reads its controller buttons by position (A is the bottom face
    // button), so a pad in D-input mode (a Legion Go with one controller off, a PlayStation pad without Steam Input, older
    // PC pads) uses the same buttons for the same actions.
    [TestFixture]
    [Requirement("INPUT-PAD-002")]
    public sealed class PadButtonsTests
    {
        [TearDown]
        public void TearDown() => PadButtons.Mode = ControllerLayoutMode.Automatic;

        [Test]
        public void XInput_KeepsTheXboxNumbers()
        {
            PadButtons.Mode = ControllerLayoutMode.XInput;
            for (var n = 0; n <= 15; n++)
                Assert.AreEqual(KeyCode.JoystickButton0 + n, PadButtons.Map(KeyCode.JoystickButton0 + n));
        }

        [Test]
        public void DInput_ReadsEachButtonAtItsPosition()
        {
            PadButtons.Mode = ControllerLayoutMode.DInput;
            Assert.AreEqual(KeyCode.JoystickButton1, PadButtons.Map(KeyCode.JoystickButton0), "A: the bottom button");
            Assert.AreEqual(KeyCode.JoystickButton2, PadButtons.Map(KeyCode.JoystickButton1), "B: the right button");
            Assert.AreEqual(KeyCode.JoystickButton0, PadButtons.Map(KeyCode.JoystickButton2), "X: the left button");
            Assert.AreEqual(KeyCode.JoystickButton3, PadButtons.Map(KeyCode.JoystickButton3), "Y: the top button");
            Assert.AreEqual(KeyCode.JoystickButton4, PadButtons.Map(KeyCode.JoystickButton4), "LB");
            Assert.AreEqual(KeyCode.JoystickButton5, PadButtons.Map(KeyCode.JoystickButton5), "RB");
            Assert.AreEqual(KeyCode.JoystickButton8, PadButtons.Map(KeyCode.JoystickButton6), "Back / Select");
            Assert.AreEqual(KeyCode.JoystickButton9, PadButtons.Map(KeyCode.JoystickButton7), "Start");
            Assert.AreEqual(KeyCode.JoystickButton10, PadButtons.Map(KeyCode.JoystickButton8), "left stick press");
            Assert.AreEqual(KeyCode.JoystickButton11, PadButtons.Map(KeyCode.JoystickButton9), "right stick press");
            Assert.AreEqual(KeyCode.None, PadButtons.Map(KeyCode.JoystickButton12), "the D-pad is a hat, not buttons 12-15");
            Assert.AreEqual(KeyCode.Return, PadButtons.Map(KeyCode.Return), "keys stay keys");
        }

        [Test]
        public void Automatic_UsesDInputForPlayStationAndUnknownPads_AndXInputOtherwise()
        {
            PadButtons.Mode = ControllerLayoutMode.Automatic;
            Assert.AreEqual(PadLayout.XInput, PadButtons.LayoutFor(ControllerPlatform.None));
            Assert.AreEqual(PadLayout.XInput, PadButtons.LayoutFor(ControllerPlatform.Xbox));
            Assert.AreEqual(PadLayout.XInput, PadButtons.LayoutFor(ControllerPlatform.Nintendo), "a Switch pad numbers by position like Xbox");
            Assert.AreEqual(PadLayout.DInput, PadButtons.LayoutFor(ControllerPlatform.PlayStation));
            Assert.AreEqual(PadLayout.DInput, PadButtons.LayoutFor(ControllerPlatform.Generic));
        }

        [Test]
        public void AChosenMode_WinsOverTheDetectedPad()
        {
            PadButtons.Mode = ControllerLayoutMode.XInput;
            Assert.AreEqual(PadLayout.XInput, PadButtons.LayoutFor(ControllerPlatform.PlayStation));
            PadButtons.Mode = ControllerLayoutMode.DInput;
            Assert.AreEqual(PadLayout.DInput, PadButtons.LayoutFor(ControllerPlatform.Xbox));
        }

        [Test]
        public void TheUiModule_ConfirmsAndCancelsWithTheSameButtons()
        {
            Assert.AreEqual(("Submit", "Cancel"), PadButtons.UiAxes(PadLayout.XInput));
            Assert.AreEqual(("Submit (D-input)", "Cancel (D-input)"), PadButtons.UiAxes(PadLayout.DInput));
        }

        [Test]
        public void TheInputManager_DefinesTheDInputAxes()
        {
            var asset = System.IO.File.ReadAllText("ProjectSettings/InputManager.asset");
            StringAssert.Contains("m_Name: Submit (D-input)", asset);
            StringAssert.Contains("m_Name: Cancel (D-input)", asset);
        }
    }
}
