# TC-SUDOKU-012: Check if game saves progress

- **Requirement:** REQ-SUDOKU-012
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 08:57:14

## Precondition

The game is running with a partially or fully populated board

## Steps

1. Modify the grid to some extent (fill in numbers)
2. Close and restart the game
3. Verify that the UI shows an initial fully populated board

## Expected Result

A non-empty sudoku grid in all cells
