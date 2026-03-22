# TC-SUDOKU-039: Test the Sudoku game cell selection

- **Requirement:** REQ-SUDOKU-039
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:36:02

## Precondition

An empty sudoku grid.

## Steps

1. Start a new instance of the Sudoku game with an empty grid.
2. Select one random cell on the board.
3. Assert if the selected cell value is 0 and its coordinates are correct.

## Expected Result

The selected cell should have a value of 0 and its row and column indices must be valid.
