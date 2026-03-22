# TC-SUDOKU-062: Test Reset of Game

- **Requirement:** REQ-SUDOKU-062
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 11:36:56

## Precondition

An instance of Sudoku game class exists with a board (solved or not).

## Steps

1. Call the method resetGame() on an instance of Sudoku game class.
2. Verify that all elements in the returned board are within expected range (i.e., from 0-9) after being reset.
3. Check if it resets the game back to initial state by calling generateBoard() again and ensuring a new, unsolved game is generated.

## Expected Result

A valid sudoku game board with no repetition or missing numbers after resetting. And a new game instance from where the user can start playing.
