# TC-SAVE-033: Save & Resume Functionality Test

- **Requirement:** REQ-SAVE-033
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 13:51:28

## Precondition

User is in the Garden Run mode.

## Steps

1. Start a new game.
2. Play through some levels or time and save the game state using the 'Save Game' button.
3. Quit the game using the 'Quit to Main Menu' button while the game is paused (press ESC).
4. Return to the game by selecting 'Continue' from the main menu, ensuring that the game has resumed correctly and it’s in the same state as before the quit.

## Expected Result

The game should resume exactly where it left off without any loss of progress or stats.
