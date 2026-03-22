# TC-GENERAL-031: ProfileService stores and loads persistent data for Tutorial Progress per profile

- **Requirement:** REQ<｜begin▁of▁sentence｜>TORIAL-069
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:39:45

## Precondition

Game is running with a new profile

## Steps

1. Start the game with a new profile
2. Complete some tutorial steps in the game
3. Save and quit the game, then reload it using the same profile
4. Check the tutorial progress data loaded from ProfileService

## Expected Result

The stored tutorial progress should match what was done during the session
