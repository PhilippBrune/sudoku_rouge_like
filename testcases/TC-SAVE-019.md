# TC-SAVE-019: Resume a saved game state

- **Requirement:** REQ-SAVE-021
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 13:50:37

## Precondition

A game is in progress and a save file has been created.

## Steps

1. Start a new game.
2. Save the game state.
3. Close the game and open it again to check if the game can be resumed from the last saved state.

## Expected Result

The system should allow resuming the game at the point where it was left off, using the information stored in the save file. If validation fails during resume (e.g., due to corrupted data), the system should display an error message indicating the issue and let the user choose whether to load a backup copy of the save or start a new game.
