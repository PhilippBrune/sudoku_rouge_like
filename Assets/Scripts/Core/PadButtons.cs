using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SudokuRoguelike.Core
{
    public enum ControllerLayoutMode { Automatic, XInput, DInput }
    public enum PadLayout { XInput, DInput }

    /// <summary>
    /// [REQ: INPUT-PAD-002] The game's controller buttons are written as Xbox (X-input) numbers: A = 0, B = 1, X = 2, Y = 3,
    /// LB = 4, RB = 5, Back = 6, Start = 7, stick presses 8 and 9, D-pad 12–15. A pad in D-input mode (a Legion Go with one
    /// controller off, a PlayStation pad without Steam Input, most older PC pads) numbers them X = 0, A = 1, B = 2, Y = 3,
    /// LB = 4, RB = 5, LT = 6, RT = 7, Back = 8, Start = 9, stick presses 10 and 11, and reports its D-pad as a hat.
    /// <see cref="Map"/> turns an Xbox number into the one the pad in use sends, so every action stays on the same button.
    /// </summary>
    public static class PadButtons
    {
        /// <summary>The player's choice (Options → Controls); Automatic follows the pad that is connected.</summary>
        public static ControllerLayoutMode Mode { get; set; } = ControllerLayoutMode.Automatic;

        public static PadLayout Layout => LayoutFor(ControllerGlyphService.Detect());

        public static PadLayout LayoutFor(ControllerPlatform platform) => Mode switch
        {
            ControllerLayoutMode.XInput => PadLayout.XInput,
            ControllerLayoutMode.DInput => PadLayout.DInput,
            _ => platform == ControllerPlatform.PlayStation || platform == ControllerPlatform.Generic ? PadLayout.DInput : PadLayout.XInput
        };

        // D-input number of each Xbox number 0–15; the D-pad (12–15) is a hat there.
        private static readonly KeyCode[] DInput =
        {
            KeyCode.JoystickButton1, KeyCode.JoystickButton2, KeyCode.JoystickButton0, KeyCode.JoystickButton3,
            KeyCode.JoystickButton4, KeyCode.JoystickButton5, KeyCode.JoystickButton8, KeyCode.JoystickButton9,
            KeyCode.JoystickButton10, KeyCode.JoystickButton11, KeyCode.JoystickButton10, KeyCode.JoystickButton11,
            KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None
        };

        /// <summary>The button the pad in use sends for an Xbox button number; other keys pass through.</summary>
        public static KeyCode Map(KeyCode xboxButton)
        {
            var n = xboxButton - KeyCode.JoystickButton0;
            if (n < 0 || n > 15 || Layout == PadLayout.XInput) return xboxButton;
            return DInput[n];
        }

        /// <summary>
        /// [REQ: INPUT-PAD-003] Was the Xbox button pressed this frame. A pad the Input System knows as a gamepad is read
        /// there, by the button's place (A bottom, B right, LB, Start, L3 …) in any mode; other joysticks through the legacy
        /// numbers and <see cref="Map"/>. Only one of the two is read: both could count one press twice, a frame apart.
        /// The legacy read alone missed LB/RB in the Options on the owner's Xbox One pad (GAP-077).
        /// </summary>
        public static bool Pressed(KeyCode xboxButton) => Read(xboxButton, true);

        /// <summary>Is the Xbox button held (B held clears a cell).</summary>
        public static bool Held(KeyCode xboxButton) => Read(xboxButton, false);

        private static int _submitUsedFrame = -1;

        /// <summary>[REQ: INPUT-PAD-005] PadUiBridge submitted the chosen control with this frame's A: other readers of A
        /// skip it, so one press acts once.</summary>
        public static void UseSubmit() => _submitUsedFrame = Time.frameCount;

        /// <summary>[REQ: INPUT-PAD-005] Any pad button pressed this frame, read through the Input System (the legacy
        /// "any key" never sees the owner's pad).</summary>
        public static bool AnyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
                foreach (var control in pad.allControls)
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button && !button.synthetic && button.wasPressedThisFrame)
                        return true;
#endif
            for (var k = 0; k < 20; k++)
                if (Input.GetKeyDown(KeyCode.JoystickButton0 + k)) return true;
            return false;
        }

        /// <summary>A pad the Input System reads is connected (its buttons and sticks are read there).</summary>
        public static bool InputSystemPad
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return UnityEngine.InputSystem.Gamepad.current != null;
#else
                return false;
#endif
            }
        }

        private static bool Read(KeyCode xboxButton, bool pressedThisFrame)
        {
            if (pressedThisFrame && xboxButton == KeyCode.JoystickButton0 && _submitUsedFrame == Time.frameCount) return false;
#if ENABLE_INPUT_SYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            var control = pad == null ? null : (xboxButton - KeyCode.JoystickButton0) switch
            {
                0 => pad.buttonSouth, 1 => pad.buttonEast, 2 => pad.buttonWest, 3 => pad.buttonNorth,
                4 => pad.leftShoulder, 5 => pad.rightShoulder, 6 => pad.selectButton, 7 => pad.startButton,
                8 => pad.leftStickButton, 9 => pad.rightStickButton,
                _ => (UnityEngine.InputSystem.Controls.ButtonControl)null
            };
            if (control != null)
            {
                var on = pressedThisFrame ? control.wasPressedThisFrame : control.isPressed;
                if (on && pressedThisFrame) LogFirstPress(xboxButton);
                return on;
            }
#endif
            var key = Map(xboxButton);
            return pressedThisFrame ? Input.GetKeyDown(key) : Input.GetKey(key);
        }

        private static readonly HashSet<KeyCode> LoggedPresses = new HashSet<KeyCode>();

        // GAP-077: the first press of each button per start says whether the legacy read saw it too (the real-pad check).
        private static void LogFirstPress(KeyCode xboxButton)
        {
            if (!LoggedPresses.Add(xboxButton)) return;
            Debug.Log($"[Pad] button {xboxButton - KeyCode.JoystickButton0}: Input System yes, legacy {(Input.GetKey(Map(xboxButton)) ? "yes" : "no")}");
        }

        /// <summary>
        /// [REQ: INPUT-PAD-003] The UI module's Submit acts on the chosen control this frame (pad A or Enter through the
        /// legacy axes). A handler that also clicks the chosen control on A or Enter skips then, so a press acts once: a
        /// toggle flipped twice and stayed, a button fired twice (C# review of the 0.3.0 playtest fixes).
        /// </summary>
        public static bool UiSubmitThisFrame()
        {
            var es = EventSystem.current;
            if (es == null || !es.sendNavigationEvents || es.currentSelectedGameObject == null) return false;
            var module = es.currentInputModule as StandaloneInputModule;
            if (module == null) return false;
            try { return Input.GetButtonDown(module.submitButton); }
            catch (System.ArgumentException) { return false; }   // an axis the Input Manager lacks
        }

        /// <summary>A (or <paramref name="alsoKey"/>) pressed, and the UI's Submit does not already handle it.</summary>
        public static bool ConfirmPressed(KeyCode alsoKey = KeyCode.None) =>
            (Pressed(KeyCode.JoystickButton0) || (alsoKey != KeyCode.None && Input.GetKeyDown(alsoKey))) && !UiSubmitThisFrame();

        /// <summary>The right stick (the number pad's cursor in a puzzle); the legacy axes only without a gamepad.</summary>
        public static Vector2 RightStick()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null) return pad.rightStick.ReadValue();
#endif
            return new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));
        }

        /// <summary>D-input reports LT and RT as buttons 6 and 7 (X-input as axes, read through the Input System).</summary>
        public static bool TriggerHeld(bool left) =>
            Layout == PadLayout.DInput && Input.GetKey(left ? KeyCode.JoystickButton6 : KeyCode.JoystickButton7);

        /// <summary>The UI module's confirm and cancel axes (ProjectSettings/InputManager.asset) for a layout.</summary>
        public static (string Submit, string Cancel) UiAxes(PadLayout layout) =>
            layout == PadLayout.DInput ? ("Submit (D-input)", "Cancel (D-input)") : ("Submit", "Cancel");

        /// <summary>Points the UI input module's confirm, cancel and move axes at the pad in use; called every frame by
        /// PadUiBridge. [REQ: INPUT-PAD-005] With a pad the Input System reads, the module keeps the keyboard only: the pad's
        /// A, B, D-pad and stick are read through the Input System (PadUiBridge, MenuDpad), so a pad the legacy input also
        /// sees does not move or confirm twice.</summary>
        public static void SyncUiModule()
        {
            var module = EventSystem.current != null ? EventSystem.current.currentInputModule as StandaloneInputModule : null;
            if (module == null) return;
            var keysOnly = InputSystemPad;
            var (submit, cancel) = keysOnly ? ("Submit (keys)", "Cancel (keys)") : UiAxes(Layout);
            var (horizontal, vertical) = keysOnly ? ("Horizontal (keys)", "Vertical (keys)") : ("Horizontal", "Vertical");
            if (module.submitButton != submit) module.submitButton = submit;
            if (module.cancelButton != cancel) module.cancelButton = cancel;
            if (module.horizontalAxis != horizontal) module.horizontalAxis = horizontal;
            if (module.verticalAxis != vertical) module.verticalAxis = vertical;
        }
    }
}
