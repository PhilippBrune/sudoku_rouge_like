# TC-SUDOKU-057: Check that the game correctly handles invalid numbers (violating Sudoku rules).

- **Requirement:** REQ-SUDOKU-057
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:36:41

## Precondition

There exists a partially filled board with an invalid number.

## Steps

1. Create a new instance of the Sudoku class.
2. Call the `set_cell` method on a cell that has been assigned an invalid number (e.g., assigning 9 to a cell which already contains a number from {1, ... ,9}).
3. Verify that this action does not change any of the existing numbers and return an error message indicating an illegal move.

## Expected Result

Attempting to place an invalid number in a cell returns an error message and leaves the board unchanged.
