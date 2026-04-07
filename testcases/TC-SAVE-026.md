# TC-SAVE-026: Test Support for Multiple Save Files

- **Requirement:** REQ-SAVE-027
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 13:50:58

## Precondition

The game supports multiple save files.

## Steps

1. Complete a level and save the progress in first of five slots.
2. Repeat this process with different levels to fill up all five slots.
3. Open any one of the saved games and play it again.

## Expected Result

New saves replace older ones following the rotation policy (i.e., the latest 5 saves, no more than that). Game loads successfully without errors and presents new game state as expected upon load. Saved states are written atomically to prevent corruption.
