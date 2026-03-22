# TC-SUDOKU-036: Checking for Invalid Entries in Sudoku Puzzle

- **Requirement:** REQ-SUDOKU-036
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:35:48

## Precondition

A game is in progress and the user enters an invalid entry.

## Steps

1. Attempt to enter a number that already exists in its row, column or block.
2. Verify if the error message "Invalid Move" appears.

## Expected Result

The system should display an error message "Invalid Move" whenever the user tries entering a number that is already present in its respective row, column or block of the grid.
