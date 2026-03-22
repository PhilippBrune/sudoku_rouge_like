# TC-GENERAL-022: RunDirector Disables Unnecessary Features During Tutorial Mode

- **Requirement:** REQ<｜begin▁of▁sentence｜>TUTORIAL-014
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 11:37:50

## Precondition

The game is in tutorial mode and the RunDirector is loaded.

## Steps

1. Check if all features not applicable to a simple puzzle play (progression, items, path system) are disabled or hidden during tutorial mode.
2. Try performing some actions that should be restricted while in tutorial mode and verify if they fail or lead to expected outcomes.

## Expected Result

All non-tutorial-appropriate features should be disabled when the game is in tutorial mode.
