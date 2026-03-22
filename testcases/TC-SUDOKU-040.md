# TC-SUDOKU-040: Test the Sudoku game cell update

- **Requirement:** REQ-SUDOKU-040
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:36:02

## Precondition

An empty sudoku grid and a selected cell.

## Steps

1. Start a new instance of the Sudoku game with an empty grid.
2. Select one random cell on the board.
3. Try to assign a value greater than 9 or less than 1 to that cell.
4. Assert if the assigned value is outside valid range and the selected cell's value remains unchanged.

## Expected Result

The assigned value should be within the valid range (1-9) and the cell's value must not change after assignment.
