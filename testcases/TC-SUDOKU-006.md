# TC-SUDOKU-006: Check for multiple values within the same row, column and block

- **Requirement:** REQ-SUDOKU-006
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:57:03

## Precondition

A new Sudoku grid is initialized.

## Steps

1. Insert a number into one cell of any row, column or block.
2. Try inserting another identical number in the next available position.
3. Verify if this second insertion leads to either a row, column or block violation.
4. If it does, mark this test case as failed; else mark it as passed.

## Expected Result

The same number should not appear twice within the same 3x3 block, row and column.
