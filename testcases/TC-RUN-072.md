# TC-RUN-072: Verify if the game recognizes when an invalid solution has been entered

- **Requirement:** REQ-RUN-072
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 12:40:52

## Precondition

A partially filled 3x3 sudoku board is visible on the screen

## Steps

1. Fill in any number of cells with valid numbers and a few more incorrectly to complete a Sudoku puzzle
2. Click on "Check Solution" button

## Expected Result

The system should correctly identify that the entered solution is incorrect and display appropriate failure message
