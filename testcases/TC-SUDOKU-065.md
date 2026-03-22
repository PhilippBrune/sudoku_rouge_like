# TC-SUDOKU-065: Test the validate puzzle function with invalid puzzles (duplicate numbers in row, column or box)

- **Requirement:** REQ-SUDOKU-065
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:37:07

## Precondition

A running sudoku game with a working validator

## Steps

1. Create a test case where rows, columns or boxes contain duplicate numbers.
2. Call the validatePuzzle() method on this puzzle.

## Expected Result

The function should return false.
