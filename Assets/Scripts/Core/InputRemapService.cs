using System;
using System.Collections.Generic;
using UnityEngine;

namespace SudokuRoguelike.Core
{
    public enum InputAction
    {
        MoveUp, MoveDown, MoveLeft, MoveRight,
        Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
        TogglePencil, ClearCell, Undo,
        Confirm, Cancel, TabNext, TabPrevious, SkipAnimation
    }

    public sealed class InputRemapService
    {
        private static readonly Dictionary<InputAction, KeyCode> Defaults = new()
        {
            [InputAction.MoveUp] = KeyCode.UpArrow,
            [InputAction.MoveDown] = KeyCode.DownArrow,
            [InputAction.MoveLeft] = KeyCode.LeftArrow,
            [InputAction.MoveRight] = KeyCode.RightArrow,
            [InputAction.Digit1] = KeyCode.Alpha1,
            [InputAction.Digit2] = KeyCode.Alpha2,
            [InputAction.Digit3] = KeyCode.Alpha3,
            [InputAction.Digit4] = KeyCode.Alpha4,
            [InputAction.Digit5] = KeyCode.Alpha5,
            [InputAction.Digit6] = KeyCode.Alpha6,
            [InputAction.Digit7] = KeyCode.Alpha7,
            [InputAction.Digit8] = KeyCode.Alpha8,
            [InputAction.Digit9] = KeyCode.Alpha9,
            [InputAction.TogglePencil] = KeyCode.P,
            [InputAction.ClearCell] = KeyCode.Delete,
            [InputAction.Undo] = KeyCode.Z,
            [InputAction.Confirm] = KeyCode.Return,
            [InputAction.Cancel] = KeyCode.Escape,
            [InputAction.TabNext] = KeyCode.Tab,
            [InputAction.TabPrevious] = KeyCode.LeftShift,
            [InputAction.SkipAnimation] = KeyCode.Space
        };

        private readonly Dictionary<InputAction, KeyCode> _overrides = new();

        public InputRemapService()
        {
            LoadOverrides();
        }

        public KeyCode GetBinding(InputAction action)
        {
            return _overrides.TryGetValue(action, out var code) ? code : GetDefault(action);
        }

        public static KeyCode GetDefault(InputAction action)
        {
            return Defaults.TryGetValue(action, out var code) ? code : KeyCode.None;
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

        public bool WasActionPressed(InputAction action)
        {
            return Input.GetKeyDown(GetBinding(action));
        }

        public string GetDisplayName(InputAction action)
        {
            return GetBinding(action).ToString();
        }

        private void SaveOverrides()
        {
            var entries = new List<string>();
            foreach (var kvp in _overrides)
                entries.Add($"{kvp.Key}={kvp.Value}");
            PlayerPrefs.SetString("InputOverrides", string.Join(",", entries));
            PlayerPrefs.Save();
        }

        private void LoadOverrides()
        {
            _overrides.Clear();
            var data = PlayerPrefs.GetString("InputOverrides", "");
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
