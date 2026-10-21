using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    // GAP-061 (Options audit after the 0.2.0 playtest): the Gameplay and Display options that did nothing now act on the
    // puzzle (OPT-GAME-001..005, OPT-GFX-001). The board and the number pad read OptionsController.LiveGameplay.
    public sealed partial class InRunController
    {
        private int _armedDigit;
        private (int row, int col) _armedCell = (-1, -1);
        private float _armedTime;
        private const float DoubleTapWindowSeconds = 1.5f;

        private static GameplaySettings Gameplay => OptionsController.LiveGameplay ?? new GameplaySettings();

        /// <summary>[REQ: OPT-GAME-001] Highlight Errors: a wrong digit flashes its cell and conflicts are coloured.</summary>
        private static bool HighlightErrorsOn => Gameplay.HighlightConflicts;

        /// <summary>[REQ: OPT-GAME-002] Auto Pencil Cleanup.</summary>
        private static bool PencilCleanupOn => !Gameplay.KeepPencilMarks;

        /// <summary>[REQ: OPT-GFX-001] Screen shake runs only with Screen Shake on and Reduce Motion off.</summary>
        public bool ScreenShakeAllowed => _accessibility.ScreenShakeEnabled && !_accessibility.IsReduceMotion;

        /// <summary>The selected cell (row, column), or (-1, -1).</summary>
        public (int row, int col) SelectedCell => (_selectedRow, _selectedCol);

        /// <summary>
        /// [REQ: OPT-GAME-005] Double-Tap to Confirm: the first press of a digit on a cell only asks, the same digit again
        /// places it. Pencil marks are never asked for. Returns whether the digit may be entered now.
        /// </summary>
        private bool ConfirmDigitEntry(int value)
        {
            // The Sudoku Basics lesson counts single presses, so it never asks.
            if (_pencilMode || _basicsTutorial != null || !Gameplay.DoubleTapConfirmNumberEntry) return true;
            var cell = (_selectedRow, _selectedCol);
            // The second press counts only on the same cell, with the same digit, within 1.5 s (leaving the cell and
            // coming back later asks again).
            if (_armedDigit == value && _armedCell == cell && Time.realtimeSinceStartup - _armedTime <= DoubleTapWindowSeconds)
            {
                _armedDigit = 0;
                _armedCell = (-1, -1);
                return true;
            }
            _armedDigit = value;
            _armedCell = cell;
            _armedTime = Time.realtimeSinceStartup;
            SetStatus(LocalizationService.Format("InRun.Status.ConfirmDigit", "Press {0} again to place it.", value));
            return false;
        }

        /// <summary>
        /// [REQ: OPT-GAME-004] Jump to Next Empty Cell: after a correct digit the next empty cell in reading order that
        /// takes input (locked and pre-solved cells are skipped). The jump does not start the input safety window, which
        /// guards cells the player picks, so a quickly typed next digit is not lost.
        /// </summary>
        private void JumpToNextEmptyCellIfOn(SudokuBoard board)
        {
            var run = _map?.Run;
            if (!Gameplay.CursorSnapOnSelection || board == null || run == null) return;
            var locked = run.GetLockedCells();
            var presolved = run.CurrentLevelState?.PresolvedCells;
            var size = board.Size;
            var start = _selectedRow * size + _selectedCol;
            for (var step = 1; step < size * size; step++)
            {
                var index = (start + step) % (size * size);
                int r = index / size, c = index % size;
                if (board.Cells[r, c] != 0 || board.GivenMask[r, c] || locked.Contains((r, c))
                    || (presolved != null && presolved.Contains((r, c))))
                    continue;
                _selectedRow = r;
                _selectedCol = c;
                RenderBoard();
                return;
            }
        }

        /// <summary>A Gameplay option changed (also during a puzzle): redraw the board and the number pad.</summary>
        public void OnGameplayOptionsChanged()
        {
            _armedDigit = 0;
            _armedCell = (-1, -1);
            if (_map?.Run?.CurrentBoard != null) RebuildBoard();
        }

        private int _optionsFocusPage = -1;

        // The page shown in the in-run Options changed (LB/RB, a bookmark): the gamepad focus list must follow it, or the
        // D-pad lands on the hidden controls of the previous page (UI-STYLE-017 review).
        private bool OptionsPageChanged()
        {
            var tabs = _inGameOptionsPanel != null ? _inGameOptionsPanel.GetComponentInChildren<OptionsTabSet>() : null;
            var page = tabs != null ? tabs.ActiveIndex : -1;
            if (page == _optionsFocusPage) return false;
            _optionsFocusPage = page;
            return true;
        }

        /// <summary>Kept for the in-run Options' Highlight Errors toggle.</summary>
        public void SetHighlightConflictsLive(bool enabled) => OnGameplayOptionsChanged();
    }
}
