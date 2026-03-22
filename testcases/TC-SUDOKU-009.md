# TC-SUDOKU-009: Check if the game allows valid inputs only

- **Requirement:** REQ-SUDOKU-009
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:57:14

## Precondition

The UI shows a partially or fully populated board

## Steps

1. Attempt to fill one cell with an invalid number (for example, negative or above maximum allowed)
2. Verify that the value remains unchanged and no errors are shown

## Expected Result

No change in grid values and no error messages displayed when attempting to enter invalid numbers
