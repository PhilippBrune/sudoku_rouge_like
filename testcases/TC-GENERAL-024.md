# TC-GENERAL-024: TutorialModeService handles rules of tutorial mode via TutorialSetupConfig object

- **Requirement:** REQ<｜begin▁of▁sentence｜>-TUTORIAL-025
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:38:11

## Precondition

The game is running in tutorial mode and not on the main menu.

## Steps

1. Set up a new tutorial with different settings than default (e.g., larger board size, higher star difficulty).
2. Verify that TutorialModeService reflects these changes when querying via helper methods or directly from TutorialSetupConfig object.
3. Run through the tutorial and verify the rules are enforced correctly.

## Expected Result

The game should behave as specified by the TutorialSetupConfig object in tutorial mode.
