# TC-RUN-070: Verify if invalid numbers are rejected from being inputted into cells

- **Requirement:** REQ-RUN-070
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:40:52

## Precondition

A blank 3x3 sudoku board is visible on the screen and a cell has been selected

## Steps

1. Attempt to enter an invalid number (i.e., one that violates the Sudoku rules) into the currently highlighted cell
2. Check if the inputted number is rejected by the system

## Expected Result

A sudoku board where any attempt at entering an invalid number into a cell results in no changes
