using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Manages the keybindings panel: displays all rebindable actions,
    /// enters a "listening" state when a row's Rebind button is clicked,
    /// captures the next key press, and saves via InputRemapService.
    /// Supports per-row highlight on listen (M8), conflict detection (M6),
    /// and RefreshAllLabels for profile-slot switches (M7).
    /// </summary>
    public sealed class KeybindingsPanelController : MonoBehaviour
    {
        private InputRemapService _remap;
        private InputAction? _listeningAction;
        private InputAction? _pendingConflictAction; // set when a duplicate is detected
        private KeyCode _pendingConflictKey;

        private readonly Dictionary<InputAction, Text>  _keyLabels  = new();
        private readonly Dictionary<InputAction, Image> _rowBgs     = new();
        private readonly Dictionary<InputAction, Color> _rowBgColors = new();
        private Text _statusText;

        // Row highlight color while a rebind is in progress
        private static readonly Color ListenHighlight = new(0.98f, 0.83f, 0.26f, 0.20f);

        public void Configure(InputRemapService remap, Text statusText)
        {
            _remap = remap;
            _statusText = statusText;
        }

        private void Update()
        {
            if (_listeningAction == null || _remap == null) return;

            var pressed = InputRemapService.GetAnyKeyDown();
            if (pressed == KeyCode.None) return;

            if (pressed == KeyCode.Escape)
            {
                CancelListening();
                return;
            }

            // Check for duplicate binding
            var conflict = _remap.FindConflict(pressed, _listeningAction.Value);
            if (conflict.HasValue && _pendingConflictKey != pressed)
            {
                // First press with a conflicting key — warn but don't bind yet
                _pendingConflictAction = _listeningAction;
                _pendingConflictKey    = pressed;
                var conflictLabel = GetActionLabel(conflict.Value);
                SetStatus($"'{InputRemapService.FormatKeyCode(pressed)}' is already bound to [{conflictLabel}]. Press again to override, or Esc to cancel.");
                return;
            }

            // Either no conflict, or player confirmed the override by pressing the same key again
            _remap.SetBinding(_listeningAction.Value, pressed);
            RefreshLabel(_listeningAction.Value);

            // Clear the old conflicting binding if the player confirmed the override
            if (conflict.HasValue)
            {
                _remap.ResetBinding(conflict.Value);
                RefreshLabel(conflict.Value);
                SetStatus($"Binding saved. [{GetActionLabel(conflict.Value)}] was unbound.");
            }
            else
            {
                SetStatus("Binding saved.");
            }

            RestoreRowColor(_listeningAction.Value);
            _listeningAction       = null;
            _pendingConflictAction = null;
            _pendingConflictKey    = KeyCode.None;
        }

        public void StartListening(InputAction action)
        {
            // Restore previous listening row if switching mid-rebind
            if (_listeningAction.HasValue)
                RestoreRowColor(_listeningAction.Value);

            _listeningAction       = action;
            _pendingConflictAction = null;
            _pendingConflictKey    = KeyCode.None;

            HighlightRow(action);
            SetStatus($"Press a key to bind [{InputRemapService.GetDefault(action)} → new key] (Esc to cancel)...");
        }

        private void CancelListening()
        {
            if (_listeningAction.HasValue)
                RestoreRowColor(_listeningAction.Value);

            _listeningAction       = null;
            _pendingConflictAction = null;
            _pendingConflictKey    = KeyCode.None;
            SetStatus("Rebind cancelled.");
        }

        public void ResetAll()
        {
            _remap?.ResetAllBindings();
            foreach (InputAction a in Enum.GetValues(typeof(InputAction)))
                RefreshLabel(a);
            SetStatus("All bindings reset to defaults.");
        }

        /// <summary>Refreshes all displayed key labels — call after a profile-slot switch.</summary>
        public void RefreshAllLabels()
        {
            foreach (InputAction a in Enum.GetValues(typeof(InputAction)))
                RefreshLabel(a);
        }

        public void RegisterLabel(InputAction action, Text label)
        {
            _keyLabels[action] = label;
            RefreshLabel(action);
        }

        /// <summary>
        /// Registers the row background Image for a given action so the controller
        /// can highlight it during listening mode.
        /// </summary>
        public void RegisterRowBackground(InputAction action, Image bg, Color defaultColor)
        {
            _rowBgs[action]     = bg;
            _rowBgColors[action] = defaultColor;
        }

        private void RefreshLabel(InputAction action)
        {
            if (_keyLabels.TryGetValue(action, out var lbl) && lbl != null && _remap != null)
                lbl.text = _remap.GetDisplayName(action);
        }

        private void HighlightRow(InputAction action)
        {
            if (_rowBgs.TryGetValue(action, out var img) && img != null)
                img.color = ListenHighlight;
        }

        private void RestoreRowColor(InputAction action)
        {
            if (_rowBgs.TryGetValue(action, out var img) && img != null
                && _rowBgColors.TryGetValue(action, out var col))
                img.color = col;
        }

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        private static string GetActionLabel(InputAction action) => action switch
        {
            InputAction.MoveUp         => "Move Up",
            InputAction.MoveDown       => "Move Down",
            InputAction.MoveLeft       => "Move Left",
            InputAction.MoveRight      => "Move Right",
            InputAction.TogglePencil   => "Toggle Pencil",
            InputAction.ClearCell      => "Clear Cell",
            InputAction.UndoLastDigit  => "Undo",
            InputAction.HighlightDigit => "Highlight Digit",
            InputAction.BagNext        => "Bag Next",
            InputAction.BagPrevious    => "Bag Prev",
            InputAction.UseItem        => "Use Item",
            InputAction.InspectItem    => "Inspect Item",
            InputAction.Confirm        => "Confirm",
            InputAction.Cancel         => "Cancel",
            InputAction.SkipAnimation  => "Skip Animation",
            InputAction.TabNext        => "Tab Next",
            InputAction.TabPrevious    => "Tab Prev",
            _                          => action.ToString()
        };
    }
}
