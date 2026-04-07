# TC-SAVE-023: Test Fallback Option for Restoring Corrupted Saves

- **Requirement:** REQ-SAVE-023
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 13:50:58

## Precondition

A corrupted save file exists.

## Steps

1. Launch the game.
2. Click on the 'Load Game' option.
3. Select a corrupted save file.
4. Press 'Enter'.

## Expected Result

The system restores the valid parts of the game and provides feedback to user that some progress may be lost due to corruption.
