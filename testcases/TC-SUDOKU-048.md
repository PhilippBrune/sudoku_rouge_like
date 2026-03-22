# TC-SUDOKU-048: Create a new game board

- **Requirement:** REQ-SUDOKU-048
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:36:27

## Precondition

The Sudoku game engine is initialized

## Steps

1. Call the function 'newGame()' to start a new game 
2. Verify if an empty sudoku grid of size NxN (9x9 for example) has been created  
3. Assert that all cells in the grid are initially zero

## Expected Result

A new Sudoku board is returned with all cells initialized to 0
