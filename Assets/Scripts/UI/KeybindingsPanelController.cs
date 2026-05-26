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
    // [REQ: INPUT-REMAP-004] KeybindingsPanelController: manages full listen → capture → conflict-check → save rebind flow
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
                SetStatus(LocalizationService.Format(
                    "Keybindings.Status.Conflict",
                    "'{0}' is already bound to [{1}]. Press again to override, or Esc to cancel.",
                    InputRemapService.FormatKeyCode(pressed),
                    conflictLabel));
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
                SetStatus(LocalizationService.Format(
                    "Keybindings.Status.SavedWithUnbind",
                    "Binding saved. [{0}] was unbound.",
                    GetActionLabel(conflict.Value)));
            }
            else
            {
                SetStatus(LocalizationService.T("Keybindings.Status.Saved"));
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
            SetStatus(LocalizationService.Format(
                "Keybindings.Status.Listen",
                "Press a key to bind [{0} -> new key] (Esc to cancel)...",
                InputRemapService.GetDefault(action)));
        }

        private void CancelListening()
        {
            if (_listeningAction.HasValue)
                RestoreRowColor(_listeningAction.Value);

            _listeningAction       = null;
            _pendingConflictAction = null;
            _pendingConflictKey    = KeyCode.None;
            SetStatus(LocalizationService.T("Keybindings.Status.Cancelled"));
        }

        public void ResetAll()
        {
            _remap?.ResetAllBindings();
            foreach (InputAction a in Enum.GetValues(typeof(InputAction)))
                RefreshLabel(a);
            SetStatus(LocalizationService.T("Keybindings.Status.ResetAll"));
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

        public static string GetActionLabel(InputAction action)
        {
            return LocalizationService.T(GetActionLabelKey(action), GetActionFallback(action));
        }

        private static string GetActionLabelKey(InputAction action) => action switch
        {
            InputAction.MoveUp         => "Keybindings.Action.MoveUp",
            InputAction.MoveDown       => "Keybindings.Action.MoveDown",
            InputAction.MoveLeft       => "Keybindings.Action.MoveLeft",
            InputAction.MoveRight      => "Keybindings.Action.MoveRight",
            InputAction.TogglePencil   => "Keybindings.Action.TogglePencil",
            InputAction.ClearCell      => "Keybindings.Action.ClearCell",
            InputAction.UndoLastDigit  => "Keybindings.Action.UndoLastDigit",
            InputAction.HighlightDigit => "Keybindings.Action.HighlightDigit",
            InputAction.BagNext        => "Keybindings.Action.BagNext",
            InputAction.BagPrevious    => "Keybindings.Action.BagPrevious",
            InputAction.UseItem        => "Keybindings.Action.UseItem",
            InputAction.InspectItem    => "Keybindings.Action.InspectItem",
            InputAction.Confirm        => "Keybindings.Action.Confirm",
            InputAction.Cancel         => "Keybindings.Action.Cancel",
            InputAction.SkipAnimation  => "Keybindings.Action.SkipAnimation",
            InputAction.TabNext        => "Keybindings.Action.TabNext",
            InputAction.TabPrevious    => "Keybindings.Action.TabPrevious",
            _                          => action.ToString()
        };

        private static string GetActionFallback(InputAction action) => action switch
        {
            InputAction.MoveUp         => "Move Up",
            InputAction.MoveDown       => "Move Down",
            InputAction.MoveLeft       => "Move Left",
            InputAction.MoveRight      => "Move Right",
            InputAction.TogglePencil   => "Toggle Pencil",
            InputAction.ClearCell      => "Clear Cell",
            InputAction.UndoLastDigit  => "Undo Last Digit",
            InputAction.HighlightDigit => "Highlight Digit",
            InputAction.BagNext        => "Bag Next Slot",
            InputAction.BagPrevious    => "Bag Prev Slot",
            InputAction.UseItem        => "Use Item",
            InputAction.InspectItem    => "Inspect Item",
            InputAction.Confirm        => "Confirm",
            InputAction.Cancel         => "Cancel",
            InputAction.SkipAnimation  => "Skip Animation",
            InputAction.TabNext        => "Tab Next",
            InputAction.TabPrevious    => "Tab Previous",
            _                          => action.ToString()
        };
    }
}
