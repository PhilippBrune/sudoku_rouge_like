# TC-SUDOKU-069: Check if clicking on an empty cell updates the selected cell

- **Requirement:** REQ-SUDOKU-069
- **Type:** Manual
- **Risk:** Medium
- **Created:** 2026-03-22 11:37:19

## Precondition

Game object exists in the scene and game board is initialized correctly

## Steps

1. Click on one of the cells on the game board
2. Observe the selected state of that cell
3. Verify if any changes to other cells or the grid lines occur

## Expected Result

The clicked cell should be highlighted and its value updated to 1
