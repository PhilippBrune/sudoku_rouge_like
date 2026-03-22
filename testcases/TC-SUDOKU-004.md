# TC-SUDOKU-004: Check for single value in column

- **Requirement:** REQ-SUDOKU-004
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 08:57:03

## Precondition

A new Sudoku grid is initialized.

## Steps

1. Insert a number into the first cell of any column.
2. Verify if the same number appears again in that particular column.
3. If it does, mark this test case as failed; else mark it as passed.

## Expected Result

The number should not appear twice in a single column.
