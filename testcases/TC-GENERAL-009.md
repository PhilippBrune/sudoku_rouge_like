# TC-GENERAL-009: Testing Gold Preservation Between Runs

- **Requirement:** REQ<｜begin▁of▁sentence｜>011
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 08:54:09

## Precondition

Game is running with gold preservation feature activated.

## Steps

1. Start a new game and play through to the end.
2. End the game and note down the final amount of gold.
3. Repeat steps 1-2 several times, ensuring that each time you start from a fresh game state (i.e., no previous games have been played).
4. Verify that the final amounts of gold from each run are consistent with each other.

## Expected Result

Each run should start with a fresh set of resources (no preservation) and thus the gold reserve at the end of each run should be different from all preceding runs, indicating successful implementation of gold preservation feature.
