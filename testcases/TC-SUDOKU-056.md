# TC-SUDOKU-056: Check that a game can be paused and resumed from any point.

- **Requirement:** REQ-SUDOKU-056
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 11:36:41

## Precondition

There exists an instance of Sudoku in progress.

## Steps

1. Create a new instance of the Sudoku class.
2. Start a game, fill in some numbers.
3. Pause the game.
4. Resume the game and make another move.
5. Verify that it is possible to continue making moves after pausing and resuming.

## Expected Result

The game can be paused and resumed from any point without losing progress or starting over.
