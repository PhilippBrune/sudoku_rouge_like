# TC-SUDOKU-011: Check if game allows only unique entries within rows, columns and boxes

- **Requirement:** REQ-SUDOKU-011
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 08:57:14

## Precondition

The UI shows a partially or fully populated board

## Steps

1. Attempt to fill one cell with a number that already exists in the same row, column, or box
2. Verify that the value remains unchanged and an error is shown

## Expected Result

No change in grid values and correct error messages displayed when attempting to enter duplicate numbers
