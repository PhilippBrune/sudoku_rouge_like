using System;
using System.Collections.Generic;
using UnityEngine;

namespace SudokuRoguelike.Core
{
    /// <summary>
    /// Manages keyboard input remapping. Stores overrides in PlayerPrefs as JSON.
    /// Supports conflict detection and reset-to-defaults.
    /// </summary>
    public sealed class InputRemapService
    {
        private const string PrefsKey = "InputBindingOverrides";

        // ── Default bindings ────────────────────────────────────────────

        public enum InputAction
        {
            // Grid Navigation
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,

            // Number Input
            Digit1, Digit2, Digit3, Digit4, Digit5,
            Digit6, Digit7, Digit8, Digit9,

            // Mode Toggle
            TogglePencil,

            // Edit
            ClearCell,
            Undo,

            // Selection
            SelectAll,
            DeselectAll,
            InvertSelection,

            // UI
            Confirm,
            Cancel,
            TabNext,
            TabPrevious,
            SkipAnimation
        }

        [Serializable]
        private class BindingEntry
        {
            public string Action;
            public string Key;
        }

        [Serializable]
        private class BindingList
        {
            public List<BindingEntry> Entries = new();
        }

        private static readonly Dictionary<InputAction, KeyCode> Defaults = new()
        {
            { InputAction.MoveUp, KeyCode.UpArrow },
            { InputAction.MoveDown, KeyCode.DownArrow },
            { InputAction.MoveLeft, KeyCode.LeftArrow },
            { InputAction.MoveRight, KeyCode.RightArrow },
            { InputAction.Digit1, KeyCode.Alpha1 },
            { InputAction.Digit2, KeyCode.Alpha2 },
            { InputAction.Digit3, KeyCode.Alpha3 },
            { InputAction.Digit4, KeyCode.Alpha4 },
            { InputAction.Digit5, KeyCode.Alpha5 },
            { InputAction.Digit6, KeyCode.Alpha6 },
            { InputAction.Digit7, KeyCode.Alpha7 },
            { InputAction.Digit8, KeyCode.Alpha8 },
            { InputAction.Digit9, KeyCode.Alpha9 },
            { InputAction.TogglePencil, KeyCode.P },
            { InputAction.ClearCell, KeyCode.Delete },
            { InputAction.Undo, KeyCode.Z },
            { InputAction.SelectAll, KeyCode.A },
            { InputAction.DeselectAll, KeyCode.A },
            { InputAction.InvertSelection, KeyCode.I },
            { InputAction.Confirm, KeyCode.Return },
            { InputAction.Cancel, KeyCode.Escape },
            { InputAction.TabNext, KeyCode.Tab },
            { InputAction.TabPrevious, KeyCode.Tab },
            { InputAction.SkipAnimation, KeyCode.Space }
        };

        private readonly Dictionary<InputAction, KeyCode> _overrides = new();

        public InputRemapService()
        {
            LoadOverrides();
        }

        /// <summary>
        /// Get the current key binding for an action.
        /// </summary>
        public KeyCode GetBinding(InputAction action)
        {
            return _overrides.TryGetValue(action, out var key) ? key : GetDefault(action);
        }

        /// <summary>
        /// Get the default key binding for an action.
        /// </summary>
        public static KeyCode GetDefault(InputAction action)
        {
            return Defaults.TryGetValue(action, out var key) ? key : KeyCode.None;
        }

        /// <summary>
        /// Set a custom binding. Returns the conflicting action name if a conflict exists, null otherwise.
        /// </summary>
        public string SetBinding(InputAction action, KeyCode newKey)
        {
            // Check for conflicts
            foreach (var kvp in Defaults)
            {
                if (kvp.Key == action) continue;
                var currentKey = GetBinding(kvp.Key);
                if (currentKey == newKey)
                {
                    return kvp.Key.ToString();
                }
            }

            _overrides[action] = newKey;
            SaveOverrides();
            return null;
        }

        /// <summary>
        /// Reset a single binding to default.
        /// </summary>
        public void ResetBinding(InputAction action)
        {
            _overrides.Remove(action);
            SaveOverrides();
        }

        /// <summary>
        /// Reset all bindings to defaults.
        /// </summary>
        public void ResetAllBindings()
        {
            _overrides.Clear();
            SaveOverrides();
        }

        /// <summary>
        /// Check if an action was pressed this frame using legacy Input.
        /// </summary>
        public bool WasActionPressed(InputAction action)
        {
            var key = GetBinding(action);
            return Input.GetKeyDown(key);
        }

        /// <summary>
        /// Get a display-friendly name for the current binding.
        /// </summary>
        public string GetDisplayName(InputAction action)
        {
            var key = GetBinding(action);
            return key switch
            {
                KeyCode.UpArrow => "Up",
                KeyCode.DownArrow => "Down",
                KeyCode.LeftArrow => "Left",
                KeyCode.RightArrow => "Right",
                KeyCode.Return => "Enter",
                KeyCode.Escape => "Esc",
                KeyCode.Delete => "Del",
                KeyCode.Backspace => "Bksp",
                KeyCode.Space => "Space",
                KeyCode.Tab => "Tab",
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                _ => key.ToString()
            };
        }

        /// <summary>
        /// Get all actions in a category for display purposes.
        /// </summary>
        public static InputAction[] GetActionsInCategory(string category)
        {
            return category switch
            {
                "Grid Navigation" => new[] { InputAction.MoveUp, InputAction.MoveDown, InputAction.MoveLeft, InputAction.MoveRight },
                "Number Input" => new[] { InputAction.Digit1, InputAction.Digit2, InputAction.Digit3, InputAction.Digit4, InputAction.Digit5, InputAction.Digit6, InputAction.Digit7, InputAction.Digit8, InputAction.Digit9 },
                "Mode Toggle" => new[] { InputAction.TogglePencil },
                "Edit" => new[] { InputAction.ClearCell, InputAction.Undo },
                "Selection" => new[] { InputAction.SelectAll, InputAction.DeselectAll, InputAction.InvertSelection },
                "UI" => new[] { InputAction.Confirm, InputAction.Cancel, InputAction.TabNext, InputAction.TabPrevious, InputAction.SkipAnimation },
                _ => Array.Empty<InputAction>()
            };
        }

        public static string[] Categories => new[]
        {
            "Grid Navigation", "Number Input", "Mode Toggle", "Edit", "Selection", "UI"
        };

        // ── Persistence ─────────────────────────────────────────────────

        private void SaveOverrides()
        {
            var list = new BindingList();
            foreach (var kvp in _overrides)
            {
                list.Entries.Add(new BindingEntry { Action = kvp.Key.ToString(), Key = kvp.Value.ToString() });
            }
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }

        private void LoadOverrides()
        {
            _overrides.Clear();
            if (!PlayerPrefs.HasKey(PrefsKey)) return;

            var json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var list = JsonUtility.FromJson<BindingList>(json);
                if (list?.Entries == null) return;
                foreach (var entry in list.Entries)
                {
                    if (Enum.TryParse<InputAction>(entry.Action, out var action) &&
                        Enum.TryParse<KeyCode>(entry.Key, out var key))
                    {
                        _overrides[action] = key;
                    }
                }
            }
            catch
            {
                // Corrupted prefs — ignore
            }
        }
    }
}
