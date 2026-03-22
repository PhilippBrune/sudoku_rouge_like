# TC-TUTORIAL-015: TutorialModeService Handles Rules of Tutorial Mode Correctly

- **Requirement:** REQ-TUTORIAL-016
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:37:50

## Precondition

The TutorialModeService is loaded with a specific TutorialSetupConfig object.

## Steps

1. Verify the behaviour of all methods in TutorialModeService based on configuration defined in TutorialSetupConfig objects for different board sizes, star difficulties, modifier availability, resource modes and region layouts.
2. Check if helper methods provided by TutorialModeService work correctly for UI dropdowns based on these configurations.

## Expected Result

All the rules of tutorial mode should be handled correctly and the helper methods should return correct values for UI dropdowns.
