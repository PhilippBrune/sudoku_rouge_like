# TC-SAVE-034: Resume Button Visibility Test Case

- **Requirement:** REQ-SAVE-035
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 13:51:28

## Precondition

User is in a paused game and there's an existing save file.

## Steps

1. Pause the game and attempt to access the resume button while no valid save files exist.
2. Check if the 'Resume' button is visible.

## Expected Result

The 'Resume' button should not be visible when there are no valid save files to resume from.
