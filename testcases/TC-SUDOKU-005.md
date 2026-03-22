# TC-SUDOKU-005: Check for single value in block

- **Requirement:** REQ-SUDOKU-005
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 08:57:03

## Precondition

A new Sudoku grid is initialized.

## Steps

1. Insert a number into the first cell of any block (3x3 square).
2. Verify if the same number appears again in that particular block.
3. If it does, mark this test case as failed; else mark it as passed.

## Expected Result

The number should not appear twice in a single 3x3 block.
