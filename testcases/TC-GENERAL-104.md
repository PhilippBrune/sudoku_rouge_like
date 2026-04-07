# TC-GENERAL-104: Test handling of new save file with default data on first launch

- **Requirement:** REQ<｜begin▁of▁sentence｜>ave-039</s>
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 13:51:56

## Precondition

A fresh game instance is running.

## Steps

1. Launch the game and complete a small, non-trivial action to progress in gameplay (e.g., fill a single cell).
2. Save the game state. 
3. Quit the game and restart it without making any further actions.
4. Verify that a new save file has been created with default data.

## Expected Result

A new save file is created with default data when launching the game for the first time after playing an action.
