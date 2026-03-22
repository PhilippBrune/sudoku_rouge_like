# TC-SUDOKU-020: Test if a game can be paused and resumed without loss of progression.

- **Requirement:** REQ-SUDOKU-020
- **Type:** Manual Testing
- **Risk:** Medium
- **Created:** 2026-03-22 08:57:39

## Precondition

A valid Sudoku board is loaded and shown to user on UI.

## Steps

1. Click "Start Game" button.
2. Solve a puzzle partially, then click the "Pause Game" button.
3. Make some changes in the game state (like adding more numbers) while the game is paused.
4. Resume the game by clicking on the "Resume Game" button.

## Expected Result

The resumed game should continue where it left off without any loss of progression or user intervention needed to resume playing.
