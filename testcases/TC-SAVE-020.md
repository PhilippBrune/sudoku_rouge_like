# TC-SAVE-020: Save & resume functionality in Garden Run mode

- **Requirement:** REQ-SAVE-022
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 13:50:37

## Precondition

The Garden Run mode is active and there are available saved games.

## Steps

1. Start the game in Garden Run mode.
2. Complete a few stages of this mode to generate some progression data.
3. Save the current state of the Garden Run.
4. Close the game, return to the main menu and open it again.
5. Select the option to resume the saved Garden Run.

## Expected Result

The system should allow resuming from a saved state in Garden Run mode, using the information stored in the save file (e.g., progression data). If validation fails during resume (e.g., due to corrupted data), the system should display an error message indicating the issue and let the user choose whether to load a backup copy of the save or start a new game.
