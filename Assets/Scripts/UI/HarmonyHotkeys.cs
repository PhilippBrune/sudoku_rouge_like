using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// The Harmony shortcuts of Select Class and Game Modes (UI-STYLE-024, UI-STYLE-025): LB / RB (PageUp / PageDown) step
    /// the level, Y (F) changes the perk. Nothing while a text field (the seed) has the keyboard.
    /// </summary>
    internal static class HarmonyHotkeys
    {
        internal static (int Step, bool Perk) Read()
        {
            var events = EventSystem.current;
            var selected = events != null ? events.currentSelectedGameObject : null;
            if (selected != null)
            {
                var field = selected.GetComponent<InputField>();
                if (field != null && field.isFocused) return (0, false);
            }
            var step = Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.PageDown) ? 1
                : Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.PageUp) ? -1 : 0;
            return (step, Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.F));
        }
    }
}
