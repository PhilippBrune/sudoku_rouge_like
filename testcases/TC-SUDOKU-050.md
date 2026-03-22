# TC-SUDOKU-050: Fill a cell with a given number

- **Requirement:** REQ-SUDOKU-050
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:36:27

## Precondition

A Sudoku board exists and an empty cell on it

## Steps

1. Call the function 'fillCell(x, y, num)' to fill a cell in the grid  
2. Verify if the cell at position (x,y) contains num after filling  
3. Assert that the returned value matches our expectations

## Expected Result

The filled number is set at the provided cell position
