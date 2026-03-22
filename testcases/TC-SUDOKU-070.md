# TC-SUDOKU-070: Check if entering a number into an empty cell updates the game state correctly

- **Requirement:** REQ-SUDOKU-070
- **Type:** Manual
- **Risk:** High
- **Created:** 2026-03-22 11:37:19

## Precondition

Game object exists in the scene and game board is initialized with numbers

## Steps

1. Click on one of the cells on the game board
2. Enter a number between 1-9 into the selected cell
3. Observe the updated state of the game board

## Expected Result

The entered number should be displayed in the selected cell and it shouldn't be possible to enter another value
