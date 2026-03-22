# TC-SUDOKU-029: Check for valid input in a cell

- **Requirement:** REQ-SUDOKU-029
- **Type:** Manual
- **Risk:** Medium
- **Created:** 2026-03-22 11:35:36

## Precondition

A game is being played and the user can enter numbers into cells

## Steps

1. Input a number into an empty cell
3. Attempt to place a value that doesn't satisfy sudoku rules (i.e., duplicate in row, column or block)

## Expected Result

An error message indicating invalid input appears on screen
