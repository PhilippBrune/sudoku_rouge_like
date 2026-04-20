using System;
using System.Collections.Generic;
using UnityEngine;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Core
{
    public enum InputAction
    {
        MoveUp, MoveDown, MoveLeft, MoveRight,
        Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
        TogglePencil, ClearCell,
        Confirm, Cancel, TabNext, TabPrevious, SkipAnimation,
        UndoLastDigit, BagNext, BagPrevious, UseItem, InspectItem,
        HighlightDigit
    }

    // [REQ: INPUT-REMAP-001] Keyboard defaults + GamepadDefaults dict (Xbox/generic layout)
    public sealed class InputRemapService
    {
        // ── Keyboard defaults ────────────────────────────────────────────────────
        private static readonly Dictionary<InputAction, KeyCode> Defaults = new()
        {
            [InputAction.MoveUp]         = KeyCode.UpArrow,
            [InputAction.MoveDown]       = KeyCode.DownArrow,
            [InputAction.MoveLeft]       = KeyCode.LeftArrow,
            [InputAction.MoveRight]      = KeyCode.RightArrow,
            [InputAction.Digit1]         = KeyCode.Alpha1,
            [InputAction.Digit2]         = KeyCode.Alpha2,
            [InputAction.Digit3]         = KeyCode.Alpha3,
            [InputAction.Digit4]         = KeyCode.Alpha4,
            [InputAction.Digit5]         = KeyCode.Alpha5,
            [InputAction.Digit6]         = KeyCode.Alpha6,
            [InputAction.Digit7]         = KeyCode.Alpha7,
            [InputAction.Digit8]         = KeyCode.Alpha8,
            [InputAction.Digit9]         = KeyCode.Alpha9,
            [InputAction.TogglePencil]   = KeyCode.P,
            [InputAction.ClearCell]      = KeyCode.Delete,
            [InputAction.Confirm]        = KeyCode.Return,
            [InputAction.Cancel]         = KeyCode.Escape,
            [InputAction.TabNext]        = KeyCode.Tab,
            [InputAction.TabPrevious]    = KeyCode.LeftShift,
            [InputAction.SkipAnimation]  = KeyCode.Space,
            [InputAction.UndoLastDigit]  = KeyCode.Z,             // Ctrl+Z handled in InRunController
            [InputAction.BagNext]        = KeyCode.None,          // Tab cycles bag when items-focus active
            [InputAction.BagPrevious]    = KeyCode.None,
            [InputAction.UseItem]        = KeyCode.Return,
            [InputAction.InspectItem]    = KeyCode.I,
            [InputAction.HighlightDigit] = KeyCode.H
        };

        // ── Gamepad defaults (Unity legacy: JoystickButton = generic Xbox/PS layout) ──
        // Button 0 = A/Cross, 1 = B/Circle, 2 = X/Square, 3 = Y/Triangle,
        // 4 = LB/L1, 5 = RB/R1, 6 = Back/Select, 7 = Start, 8 = L3, 9 = R3
        private static readonly Dictionary<InputAction, KeyCode> GamepadDefaults = new()
        {
            [InputAction.MoveUp]        = KeyCode.JoystickButton12, // D-pad up (some controllers)
            [InputAction.MoveDown]      = KeyCode.JoystickButton13,
            [InputAction.MoveLeft]      = KeyCode.JoystickButton14,
            [InputAction.MoveRight]     = KeyCode.JoystickButton15,
            [InputAction.Digit1]        = KeyCode.None,  // digits via numpad buttons in numpad view
            [InputAction.Digit2]        = KeyCode.None,
            [InputAction.Digit3]        = KeyCode.None,
            [InputAction.Digit4]        = KeyCode.None,
            [InputAction.Digit5]        = KeyCode.None,
            [InputAction.Digit6]        = KeyCode.None,
            [InputAction.Digit7]        = KeyCode.None,
            [InputAction.Digit8]        = KeyCode.None,
            [InputAction.Digit9]        = KeyCode.None,
            [InputAction.TogglePencil]  = KeyCode.JoystickButton2, // X/Square (RB also wired in HandleGamepadPuzzleFocus)
            [InputAction.ClearCell]     = KeyCode.JoystickButton1, // B/Circle
            [InputAction.Confirm]       = KeyCode.JoystickButton0, // A/Cross
            [InputAction.Cancel]        = KeyCode.JoystickButton1, // B/Circle
            [InputAction.TabNext]       = KeyCode.JoystickButton5, // RB/R1
            [InputAction.TabPrevious]   = KeyCode.JoystickButton4, // LB/L1
            [InputAction.SkipAnimation] = KeyCode.JoystickButton6, // Select/Back
            [InputAction.UndoLastDigit] = KeyCode.JoystickButton3, // Y/Triangle
            [InputAction.BagNext]       = KeyCode.JoystickButton13,// D-down
            [InputAction.BagPrevious]   = KeyCode.JoystickButton12,// D-up
            [InputAction.UseItem]       = KeyCode.JoystickButton0, // A/Cross
            [InputAction.InspectItem]   = KeyCode.None,
            [InputAction.HighlightDigit]= KeyCode.JoystickButton9  // R3
        };

        // Axis names used for gamepad stick navigation (Unity Input Manager defaults)
        private const string AxisHorizontal = "Horizontal";
        private const string AxisVertical   = "Vertical";
        private const float  AxisThreshold  = 0.5f;

        private readonly Dictionary<InputAction, KeyCode> _overrides = new();
        private bool _overridesLoaded;
        private int _activeSlot = -1;

        // Debounce: track which axis direction was already "pressed" this frame
        private bool _axisUpConsumed, _axisDownConsumed, _axisLeftConsumed, _axisRightConsumed;
        private float _prevH, _prevV;

        public InputRemapService()
        {
            // LoadOverrides is deferred to first use — PlayerPrefs must not be
            // accessed during MonoBehaviour field initialization (Unity restriction).
        }

        private void EnsureOverridesLoaded()
        {
            if (_overridesLoaded) return;
            _overridesLoaded = true;
            LoadOverrides();
        }

        // ── Binding access ───────────────────────────────────────────────────────

        public KeyCode GetBinding(InputAction action)
        {
            EnsureOverridesLoaded();
            return _overrides.TryGetValue(action, out var code) ? code : GetDefault(action);
        }

        public static KeyCode GetDefault(InputAction action)
        {
            return Defaults.TryGetValue(action, out var code) ? code : KeyCode.None;
        }

        public static KeyCode GetGamepadDefault(InputAction action)
        {
            return GamepadDefaults.TryGetValue(action, out var code) ? code : KeyCode.None;
        }

        public void SetBinding(InputAction action, KeyCode newKey)
        {
            _overrides[action] = newKey;
            SaveOverrides();
        }

        public void ResetBinding(InputAction action)
        {
            _overrides.Remove(action);
            SaveOverrides();
        }

        public void ResetAllBindings()
        {
            _overrides.Clear();
            SaveOverrides();
        }

        /// <summary>
        /// Returns the first action (other than <paramref name="excluding"/>) whose current
        /// keyboard binding matches <paramref name="key"/>, or null if no conflict exists.
        /// </summary>
        public InputAction? FindConflict(KeyCode key, InputAction excluding)
        {
            if (key == KeyCode.None) return null;
            foreach (InputAction a in Enum.GetValues(typeof(InputAction)))
            {
                if (a == excluding) continue;
                if (GetBinding(a) == key) return a;
            }
            return null;
        }

        /// <summary>Reloads keybinding overrides for the given save-slot from PlayerPrefs.</summary>
        public void ReloadForSlot(int slot)
        {
            _overrides.Clear();
            _overridesLoaded = false;
            _activeSlot = slot;
            LoadOverrides();
            _overridesLoaded = true;
        }

        // ── Input query (keyboard OR gamepad) ────────────────────────────────────

        /// <summary>
        /// Returns true if the action's keyboard binding was pressed this frame,
        /// OR if the mapped gamepad button was pressed, OR if the left-stick axis
        /// crossed the threshold for movement actions.
        /// </summary>
        // [REQ: INPUT-REMAP-002] Checks keyboard binding → gamepad button → axis crossing
        public bool WasActionPressed(InputAction action)
        {
            // Keyboard / gamepad button
            var key = GetBinding(action);
            if (key != KeyCode.None && Input.GetKeyDown(key)) return true;

            var pad = GetGamepadDefault(action);
            if (pad != KeyCode.None && Input.GetKeyDown(pad)) return true;

            // Analogue stick axis for movement
            return action switch
            {
                InputAction.MoveUp    => WasAxisPressed(AxisVertical,   +1),
                InputAction.MoveDown  => WasAxisPressed(AxisVertical,   -1),
                InputAction.MoveLeft  => WasAxisPressed(AxisHorizontal, -1),
                InputAction.MoveRight => WasAxisPressed(AxisHorizontal, +1),
                _ => false
            };
        }

        /// <summary>
        /// Call once per frame from a MonoBehaviour.Update() to reset axis debounce.
        /// </summary>
        public void Tick()
        {
            var h = Input.GetAxis(AxisHorizontal);
            var v = Input.GetAxis(AxisVertical);

            _axisLeftConsumed  = h < -AxisThreshold && _prevH >= -AxisThreshold ? false : _axisLeftConsumed;
            _axisRightConsumed = h >  AxisThreshold && _prevH <=  AxisThreshold ? false : _axisRightConsumed;
            _axisDownConsumed  = v < -AxisThreshold && _prevV >= -AxisThreshold ? false : _axisDownConsumed;
            _axisUpConsumed    = v >  AxisThreshold && _prevV <=  AxisThreshold ? false : _axisUpConsumed;

            _prevH = h;
            _prevV = v;
        }

        private bool WasAxisPressed(string axis, int sign)
        {
            var val = Input.GetAxis(axis);
            var crossed = sign > 0 ? (val > AxisThreshold) : (val < -AxisThreshold);
            var prevVal = axis == AxisHorizontal ? _prevH : _prevV;
            var wasBelow = sign > 0 ? (prevVal <= AxisThreshold) : (prevVal >= -AxisThreshold);
            return crossed && wasBelow;
        }

        // ── Listening for rebind ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the first key pressed this frame (keyboard or joystick button).
        /// Returns KeyCode.None if nothing was pressed.
        /// Useful for "press any key to rebind" UI.
        /// </summary>
        // [REQ: INPUT-REMAP-003] Returns first pressed key (keyboard or joystick button)
        public static KeyCode GetAnyKeyDown()
        {
            if (!Input.anyKeyDown) return KeyCode.None;
            foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
            {
                if (kc == KeyCode.None) continue;
                if (Input.GetKeyDown(kc)) return kc;
            }
            return KeyCode.None;
        }

        public string GetDisplayName(InputAction action)
        {
            var k = GetBinding(action);
            return FormatKeyCode(k);
        }

        public static string FormatKeyCode(KeyCode key) => key switch
        {
            KeyCode.None       => "—",
            KeyCode.UpArrow    => "↑",
            KeyCode.DownArrow  => "↓",
            KeyCode.LeftArrow  => "←",
            KeyCode.RightArrow => "→",
            KeyCode.Return     => "Enter",
            KeyCode.Escape     => "Esc",
            KeyCode.LeftShift  => "LShift",
            KeyCode.RightShift => "RShift",
            KeyCode.LeftControl => "LCtrl",
            KeyCode.RightControl => "RCtrl",
            KeyCode.Tab        => "Tab",
            KeyCode.Space      => "Space",
            KeyCode.Delete     => "Del",
            KeyCode.Backspace  => "Bksp",
            KeyCode.Alpha0     => "0",
            KeyCode.Alpha1     => "1",
            KeyCode.Alpha2     => "2",
            KeyCode.Alpha3     => "3",
            KeyCode.Alpha4     => "4",
            KeyCode.Alpha5     => "5",
            KeyCode.Alpha6     => "6",
            KeyCode.Alpha7     => "7",
            KeyCode.Alpha8     => "8",
            KeyCode.Alpha9     => "9",
            _ => key.ToString()
        };

        // ── Persistence ──────────────────────────────────────────────────────────

        private string PrefsKey()
        {
            var slot = _activeSlot >= 0 ? _activeSlot : SaveProfileService.ActiveSlot;
            return $"InputOverrides_{slot}";
        }

        private void SaveOverrides()
        {
            EnsureOverridesLoaded();
            var entries = new List<string>();
            foreach (var kvp in _overrides)
                entries.Add($"{kvp.Key}={kvp.Value}");
            PlayerPrefs.SetString(PrefsKey(), string.Join(",", entries));
            PlayerPrefs.Save();
        }

        private void LoadOverrides()
        {
            _overrides.Clear();
            var data = PlayerPrefs.GetString(PrefsKey(), "");
            if (string.IsNullOrEmpty(data)) return;

            var parts = data.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var kv = parts[i].Split('=');
                if (kv.Length == 2
                    && Enum.TryParse<InputAction>(kv[0], out var action)
                    && Enum.TryParse<KeyCode>(kv[1], out var key))
                {
                    _overrides[action] = key;
                }
            }
        }
    }
}
