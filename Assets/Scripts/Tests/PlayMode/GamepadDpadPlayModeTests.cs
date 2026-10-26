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
