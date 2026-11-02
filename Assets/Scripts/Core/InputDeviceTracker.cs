using System;
using UnityEngine;

namespace SudokuRoguelike.Core
{
    /// <summary>
    /// [REQ: INPUT-HINT-001] Which device the player used last: a pad (a button or a stick) or the keyboard and mouse. The key
    /// hints on the screens follow it (RM-207). Ticked once per frame by <see cref="InputRemapService.UpdateCursorVisibility"/>.
    /// </summary>
    public static class InputDeviceTracker
    {
        private const float StickThreshold = 0.5f;
        private const float MouseThreshold = 0.3f;
        // [REQ: INPUT-HINT-002] Mouse motion counts only after the pad has rested this long: a stick that moves the mouse
        // (a Steam Input template) flipped the hints between pad and keys every frame (0.3.1 playtest: "the hint flickers").
        private const float PadRestSeconds = 2f;
        private static int _frame = -1;
        private static float _lastPadAt = -100f;

        public static bool UsingPad { get; private set; }

        /// <summary>Raised when the device in use changes.</summary>
        public static event Action Changed;

        public static void Tick()
        {
            if (_frame == Time.frameCount) return;
            var first = _frame < 0;
            _frame = Time.frameCount;
            // A pad connected at start (Steam Deck, Legion Go) shows its buttons until a key or the mouse is used.
            var padActive = PadActive();
            if (padActive) _lastPadAt = Time.unscaledTime;
            if (first && PadConnected()) Set(true);
            else if (!UsingPad && padActive) Set(true);
            else if (UsingPad && !padActive && KeyboardOrMouseActive(Time.unscaledTime - _lastPadAt > PadRestSeconds)) Set(false);
        }

        /// <summary>Sets the device directly (tests).</summary>
        public static void Set(bool pad)
        {
            if (UsingPad == pad) return;
            UsingPad = pad;
            Changed?.Invoke();
        }

        private static bool PadConnected()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Gamepad.current != null) return true;
#endif
            foreach (var name in Input.GetJoystickNames())
                if (!string.IsNullOrEmpty(name)) return true;
            return false;
        }

        private static bool PadActive()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
            {
                foreach (var control in pad.allControls)
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button && !button.synthetic && button.wasPressedThisFrame)
                        return true;
                if (pad.leftStick.ReadValue().magnitude > StickThreshold || pad.rightStick.ReadValue().magnitude > StickThreshold)
                    return true;
            }
#endif
            for (var k = 0; k < 20; k++)
                if (Input.GetKeyDown(KeyCode.JoystickButton0 + k)) return true;
            return false;
        }

        private static bool KeyboardOrMouseActive(bool padRested)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return true;
            if (padRested && (Mathf.Abs(Input.GetAxis("Mouse X")) > MouseThreshold || Mathf.Abs(Input.GetAxis("Mouse Y")) > MouseThreshold))
                return true;
            if (!Input.anyKeyDown) return false;
            for (var k = 0; k < 20; k++)
                if (Input.GetKeyDown(KeyCode.JoystickButton0 + k)) return false;   // a pad button also counts as "any key"
            return true;
        }
    }
}
