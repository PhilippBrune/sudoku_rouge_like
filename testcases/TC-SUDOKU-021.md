# TC-SUDOKU-021: Check if the correct error message is displayed when an incorrect number is entered into a cell.

- **Requirement:** REQ-SUDOKU-021
- **Type:** Unit Testing
- **Risk:** Low
- **Created:** 2026-03-22 08:57:39

## Precondition

A valid Sudoku board is loaded and shown to user on UI.

## Steps

1. Click "Start Game" button.
2. Try to enter a number that is not allowed in the current game's rules into an unfilled cell (like trying to put '5' where only '6' or '7' are allowed).

## Expected Result

An error message should be displayed informing the user of the incorrect input and indicating what rule was violated.
