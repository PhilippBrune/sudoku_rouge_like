using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using InputAction = SudokuRoguelike.Core.InputAction;

namespace SudokuRoguelike.Tests
{
    // [REQ: INPUT-PAD-001] GAP-070: on Windows an Xbox pad (and every pad Steam Input makes into one: Steam Deck, Legion Go)
    // reports its D-pad as axes the legacy Input Manager does not define, and LT/RT had no axis there at all, so in a run
    // the D-pad and the triggers did nothing. Both are read through the Input System now.
    [TestFixture]
    [Requirement("INPUT-PAD-001")]
    public sealed class GamepadDpadPlayModeTests
    {
        private readonly List<InputDevice> _disabled = new List<InputDevice>();
        private Gamepad _pad;

        [SetUp]
        public void SetUp()
        {
            // A real pad on the test machine must not take over Gamepad.current.
            foreach (var device in InputSystem.devices)
                if (device is Gamepad && device.enabled) { InputSystem.DisableDevice(device); _disabled.Add(device); }
            _pad = InputSystem.AddDevice<Gamepad>();
            _pad.MakeCurrent();
        }

        [TearDown]
        public void TearDown()
        {
            if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad);
            _pad = null;
            foreach (var device in _disabled) if (device.added) InputSystem.EnableDevice(device);
            _disabled.Clear();
        }

        [UnityTest]
        public IEnumerator DpadUp_MovesUpOnce_AndNowhereElse()
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState().WithButton(GamepadButton.DpadUp));
            yield return null;

            var remap = new InputRemapService();
            Assert.IsTrue(remap.WasGamepadActionPressed(InputAction.MoveUp), "D-pad up moves up");
            Assert.IsFalse(remap.WasGamepadActionPressed(InputAction.MoveDown), "not down");
            Assert.IsFalse(remap.WasGamepadActionPressed(InputAction.MoveLeft), "not left");
            Assert.IsTrue(remap.WasActionPressed(InputAction.MoveUp), "the path cursor reads it too");
            Assert.AreEqual(MoveDirection.Up, MenuDpad.ReadPressed(), "the menus read the same D-pad");

            yield return null;
            Assert.IsFalse(remap.WasGamepadActionPressed(InputAction.MoveUp), "a held D-pad moves once per press");
        }

        [UnityTest]
        public IEnumerator Triggers_ReadTheirPull()
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftTrigger = 1f, rightTrigger = 0f });
            yield return null;

            Assert.Greater(InputRemapService.ReadTrigger(left: true), 0.9f, "LT pulled");
            Assert.Less(InputRemapService.ReadTrigger(left: false), 0.1f, "RT released");
        }

        // [REQ: INPUT-PAD-003] GAP-077: LB/RB, Start, Select and the stick presses come from the Input System, once per press.
        [UnityTest]
        [Requirement("INPUT-PAD-003")]
        public IEnumerator ShoulderStartSelectAndStickPresses_ReadOncePerPress()
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState()
                .WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.Start).WithButton(GamepadButton.LeftStick));
            yield return null;
            Assert.IsTrue(PadButtons.Pressed(UnityEngine.KeyCode.JoystickButton4), "LB");
            Assert.IsTrue(PadButtons.Pressed(UnityEngine.KeyCode.JoystickButton7), "Start");
            Assert.IsTrue(PadButtons.Pressed(UnityEngine.KeyCode.JoystickButton8), "L3");
            Assert.IsFalse(PadButtons.Pressed(UnityEngine.KeyCode.JoystickButton5), "not RB");
            Assert.IsFalse(PadButtons.Pressed(UnityEngine.KeyCode.JoystickButton6), "not Select");
            Assert.IsTrue(PadButtons.Held(UnityEngine.KeyCode.JoystickButton4), "LB held");

            yield return null;
            Assert.IsFalse(PadButtons.Pressed(UnityEngine.KeyCode.JoystickButton4), "a held LB counts once");
            Assert.IsTrue(PadButtons.Held(UnityEngine.KeyCode.JoystickButton4), "and is still held");
        }

        // [REQ: UI-STYLE-017] GAP-077: RB on an Xbox-type pad turns the Options page.
        [UnityTest]
        [Requirement("INPUT-PAD-003")]
        public IEnumerator Rb_TurnsTheOptionsPage()
        {
            var card = new UnityEngine.GameObject("PadOptionsCard", typeof(UnityEngine.RectTransform));
            try
            {
                var tabs = OptionsTabSet.On((UnityEngine.RectTransform)card.transform);
                tabs.Add("TabA", "SectionA", "A");
                tabs.Add("TabB", "SectionB", "B");
                tabs.Activate(0);
                yield return null;
                InputSystem.QueueStateEvent(_pad, new GamepadState().WithButton(GamepadButton.RightShoulder));
                yield return null;
                yield return null;
                Assert.AreEqual(1, tabs.ActiveIndex, "RB opened the next page");
            }
            finally
            {
                UnityEngine.Object.Destroy(card);
            }
        }

        // [REQ: INPUT-PAD-004] GAP-081: the right stick reaches the game through the Input System.
        [UnityTest]
        [Requirement("INPUT-PAD-004")]
        public IEnumerator RightStick_IsRead()
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState { rightStick = new UnityEngine.Vector2(1f, 0f) });
            yield return null;
            Assert.Greater(PadButtons.RightStick().x, 0.9f);
        }

        // [REQ: INPUT-HINT-001] A pad press switches the key hints to the pad's buttons; the keyboard's text comes back with a
        // key or the mouse (RM-207).
        [UnityTest]
        [Requirement("INPUT-HINT-001")]
        public IEnumerator APadPress_SwitchesTheHintToThePadsButtons()
        {
            var go = new UnityEngine.GameObject("HintUnderTest", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Text));
            try
            {
                InputDeviceTracker.Set(false);
                var text = go.GetComponent<UnityEngine.UI.Text>();
                DeviceHint.Attach(text, "GameModes.KeyHint");
                Assert.AreEqual(LocalizationService.T("GameModes.KeyHint.Keys"), text.text, "keyboard first");

                InputSystem.QueueStateEvent(_pad, new GamepadState().WithButton(GamepadButton.South));
                yield return null;
                InputDeviceTracker.Tick();
                Assert.IsTrue(InputDeviceTracker.UsingPad);
                Assert.AreEqual(LocalizationService.T("GameModes.KeyHint"), text.text, "the pad's buttons after a pad press");

                InputDeviceTracker.Set(false);
                Assert.AreEqual(LocalizationService.T("GameModes.KeyHint.Keys"), text.text, "and back");
            }
            finally
            {
                InputDeviceTracker.Set(false);
                UnityEngine.Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator WithoutAPad_NothingMoves_AndTheTriggersReadZero()
        {
            InputSystem.RemoveDevice(_pad);
            yield return null;

            Assert.IsFalse(InputRemapService.WasDpadPressed(InputAction.MoveUp));
            Assert.AreEqual(0f, InputRemapService.ReadTrigger(left: true));
        }
    }
}
