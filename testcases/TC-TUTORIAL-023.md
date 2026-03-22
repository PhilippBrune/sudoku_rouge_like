# TC-TUTORIAL-023: RunDirector isolates run logic during tutorial

- **Requirement:** REQ-TUTORIAL-023
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:38:11

## Precondition

The game is running in tutorial mode and not on the main menu.

## Steps

1. Verify that all non-puzzle features (progression, items, path system) are disabled when entering tutorial mode.
2. Run a puzzle to verify that run logic has been isolated from other parts of the game.
3. Exit tutorial mode and reenter it.

## Expected Result

All non-puzzle features remain disabled during tutorial mode.
