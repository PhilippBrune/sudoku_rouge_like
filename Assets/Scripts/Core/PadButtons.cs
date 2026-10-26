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

        /// <summary>D-input reports LT and RT as buttons 6 and 7 (X-input as axes, read through the Input System).</summary>
        public static bool TriggerHeld(bool left) =>
            Layout == PadLayout.DInput && Input.GetKey(left ? KeyCode.JoystickButton6 : KeyCode.JoystickButton7);

        /// <summary>The UI module's confirm and cancel axes (ProjectSettings/InputManager.asset) for a layout.</summary>
        public static (string Submit, string Cancel) UiAxes(PadLayout layout) =>
            layout == PadLayout.DInput ? ("Submit (D-input)", "Cancel (D-input)") : ("Submit", "Cancel");

        /// <summary>Points the UI input module's confirm and cancel at the pad in use; called every frame by the menus.</summary>
        public static void SyncUiModule()
        {
            var module = EventSystem.current != null ? EventSystem.current.currentInputModule as StandaloneInputModule : null;
            if (module == null) return;
            var (submit, cancel) = UiAxes(Layout);
            if (module.submitButton != submit) module.submitButton = submit;
            if (module.cancelButton != cancel) module.cancelButton = cancel;
        }
    }
}
