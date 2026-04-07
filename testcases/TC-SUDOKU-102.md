# TC-SUDOKU-102: UniquenessCheckerPerformanceTest

- **Requirement:** REQ-SUDOKU-103
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 12:41:55

## Precondition

The game is running with increasing puzzles generated and checks made.

## Steps

1. Generate a large number of puzzles and perform an increasing number of checks on them.
2. Monitor the time taken for each check in the uniqueness checker.

## Expected Result

Time taken for checking uniqueness scales linearly as puzzles increase, indicating optimal performance.
