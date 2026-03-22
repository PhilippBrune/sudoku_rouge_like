# TC-SUDOKU-060: Test Solving of a Given Board

- **Requirement:** REQ-SUDOKU-060
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 11:36:56

## Precondition

An instance of Sudoku game class exists with a given, but not necessarily solved board.

## Steps

1. Call the method solveBoard() on an instance of Sudoku game class.
2. Verify that the returned board is identical to the initial given board after solving it.
3. Verify that every row, column and 3x3 subgrid contains exactly one occurrence of each number from 1-9.

## Expected Result

A solved sudoku game board with no repetition or missing numbers.
