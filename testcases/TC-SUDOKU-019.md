# TC-SUDOKU-019: Check if the selected puzzle is being solved correctly.

- **Requirement:** REQ-SUDOKU-019
- **Type:** Integration Testing
- **Risk:** High
- **Created:** 2026-03-22 08:57:39

## Precondition

A valid Sudoku board is loaded and shown to user on UI.

## Steps

1. Click "Start Game" button.
2. Solve a hard-to-solve puzzle by entering numbers one at a time.
3. Check if the puzzle has been solved correctly using an external tool for Sudoku solving.

## Expected Result

The game should solve the puzzle without any user intervention or error.
