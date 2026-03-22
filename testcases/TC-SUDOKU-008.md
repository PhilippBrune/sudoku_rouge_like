# TC-SUDOKU-008: Check if the game starts with a filled board

- **Requirement:** REQ-SUDOKU-008
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 08:57:14

## Precondition

The game is started without any input

## Steps

1. Start the game
2. Verify that the UI shows an initial fully populated board
3. Fill one cell with a number and verify that the board remains filled out after the operation

## Expected Result

A non-empty sudoku grid in all cells
