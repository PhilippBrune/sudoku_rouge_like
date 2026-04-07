# TC-GENERAL-047: RemoveCells from generated boards based on star difficulty adhering to density formula

- **Requirement:** REQ<｜begin▁of▁sentence｜>-SUDOKU-080
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:41:16

## Precondition

A solved Sudoku board has been generated using SudokuGenerator class.

## Steps

1. Create a new instance of the StarDifficultyManager class with an input parameter of a solved Sudoku grid.
2. Call the removeCells() function on this object to set the density level.
3. Verify that the returned board after removing cells is still solvable and meets star difficulty requirements as defined by the chosen density level.

## Expected Result

A solvable, "star" shaped Sudoku puzzle based on specified density
