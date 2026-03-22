# TC-SUDOKU-022: Test if the game can handle multiple active games simultaneously.

- **Requirement:** REQ-SUDOKU-022
- **Type:** Integration Testing
- **Risk:** High
- **Created:** 2026-03-22 08:57:39

## Precondition

A valid Sudoku board is loaded and shown to user on UI.

## Steps

1. Click "Start Game" button three times, each time creating a new game with different rules or difficulty level.
2. Solve one of the games partially using numbers.

## Expected Result

Each newly created game should independently solve its own puzzle without affecting others.
