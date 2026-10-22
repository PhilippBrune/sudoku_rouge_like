using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: INPUT-NAV-001] The D-pad on the main menu's panels (GAP-062): one step along the focused control's own link
    /// (its explicit Up/Down/Left/Right, or Unity's automatic neighbour), once per press. The legacy input module reads only
    /// the arrow keys and the left stick, so the D-pad is read here: through the Input System (XInput, PlayStation, Switch
    /// pads) and the legacy D-pad buttons 12–15 some controllers report. Arrow keys and the stick stay with the module, so
    /// nothing moves twice.
    /// </summary>
    public static class MenuDpad
    {
        /// <summary>The D-pad direction pressed this frame, or None.</summary>
        public static MoveDirection ReadPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame) return MoveDirection.Up;
                if (pad.dpad.down.wasPressedThisFrame) return MoveDirection.Down;
                if (pad.dpad.left.wasPressedThisFrame) return MoveDirection.Left;
                if (pad.dpad.right.wasPressedThisFrame) return MoveDirection.Right;
            }
#endif
            if (Input.GetKeyDown(KeyCode.JoystickButton12)) return MoveDirection.Up;
            if (Input.GetKeyDown(KeyCode.JoystickButton13)) return MoveDirection.Down;
            if (Input.GetKeyDown(KeyCode.JoystickButton14)) return MoveDirection.Left;
            if (Input.GetKeyDown(KeyCode.JoystickButton15)) return MoveDirection.Right;
            return MoveDirection.None;
        }

        /// <summary>The control one step from <paramref name="current"/> in <paramref name="direction"/>, or null when
        /// there is none (the focus then stays). Like Unity's navigation, an explicit link may lead to a locked control (a
        /// locked curtain shows what opens it); automatic neighbours skip locked ones.</summary>
        public static Selectable Step(Selectable current, MoveDirection direction)
        {
            if (current == null) return null;
            var next = direction switch
            {
                MoveDirection.Up => current.FindSelectableOnUp(),
                MoveDirection.Down => current.FindSelectableOnDown(),
                MoveDirection.Left => current.FindSelectableOnLeft(),
                MoveDirection.Right => current.FindSelectableOnRight(),
                _ => null
            };
            if (next != null && next.IsActive()) return next;
            // An explicit link to a hidden control (a slip not shown): the nearest control that way, as Automatic would.
            var automatic = current.FindSelectable(current.transform.rotation * DirectionVector(direction));
            return automatic != null && automatic.IsActive() ? automatic : null;
        }

        private static Vector3 DirectionVector(MoveDirection direction) => direction switch
        {
            MoveDirection.Up => Vector3.up,
            MoveDirection.Down => Vector3.down,
            MoveDirection.Left => Vector3.left,
            _ => Vector3.right
        };

        /// <summary>A movement key the player bound in Keybindings, when it is one the UI input module does not read
        /// (arrow keys and W/A/S/D move through the module already).</summary>
        public static MoveDirection ReadRemapped(Core.InputRemapService remap)
        {
            if (remap == null) return MoveDirection.None;
            if (Pressed(remap, Core.InputAction.MoveUp)) return MoveDirection.Up;
            if (Pressed(remap, Core.InputAction.MoveDown)) return MoveDirection.Down;
            if (Pressed(remap, Core.InputAction.MoveLeft)) return MoveDirection.Left;
            return Pressed(remap, Core.InputAction.MoveRight) ? MoveDirection.Right : MoveDirection.None;
        }

        private static bool Pressed(Core.InputRemapService remap, Core.InputAction action)
        {
            var key = remap.GetBinding(action);
            return key != KeyCode.None && !ModuleKeys.Contains(key) && Input.GetKeyDown(key);
        }

        private static readonly System.Collections.Generic.HashSet<KeyCode> ModuleKeys = new System.Collections.Generic.HashSet<KeyCode>
        {
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D
        };
    }
}
