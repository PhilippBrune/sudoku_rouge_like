# TC-SUDOKU-055: Check that a new game can be started from an existing, partially filled board.

- **Requirement:** REQ-SUDOKU-055
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:36:41

## Precondition

There exists a partially filled Sudoku board.

## Steps

1. Create a new instance of the Sudoku class.
2. Call the `initialize_board` method on an already partially filled board.
3. Verify that all empty cells in the newly initialized board are still 0, and the rest contain pre-filled values.

## Expected Result

All empty cells in the newly initialized board contain 0 after initialization, while non-empty cells retain their initial values.
