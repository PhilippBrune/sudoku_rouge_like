# TC-GENERAL-086: Verify correct handling of Gold earnings and spending

- **Requirement:** REQ-GENERAL-022 — GoldResource: The system shall handle currency management of Gold earned and spent during each run.
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 13:47:33

## Precondition

The game is running with a user initiated round

## Steps

1. Start a new game or load an existing saved game
2. Complete the current Sudoku puzzle without any errors
3. Verify if Gold has been earned after completion of the puzzle
4. Spend some Gold on buying items in-game for testing

## Expected Result

The system should handle Gold correctly, increasing it when a round is completed and decreasing it when spent in-game.
