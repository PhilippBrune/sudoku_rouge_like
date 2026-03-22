# TC-GENERAL-035: Test helper methods for UI dropdowns

- **Requirement:** REQ<｜begin▁of▁sentence｜>TUTORIAL-088
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:40:32

## Precondition

GameController is initialized and TutorialModeService has been instantiated with different configurations.

## Steps

1. Call the helper methods provided by TutorialModeService to populate UI dropdowns.
2. Verify if the returned values from these methods match what's expected based on current configuration.

## Expected Result

The helper methods return correct values for each UI dropdown, reflecting the current configuration of the game.
