# TC-SUDOKU-051: Check if a row or column contains duplicate numbers

- **Requirement:** REQ-SUDOKU-051
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:36:27

## Precondition

A Sudoku board exists with some filled cells

## Steps

1. Call the function 'hasDuplicates(row/column)', depending on what we want to check  
2. Verify if it returns true or false based on whether there are duplicates in the row or column  
3. Assert that the returned value matches our expectations

## Expected Result

A boolean indicating whether there are any duplicates in the provided row or column
