# TC-TUTORIAL-080: TutorialModeService handles tutorial mode rules correctly

- **Requirement:** REQ-TUTORIAL-079
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:40:12

## Precondition

A user exists in the system with a profile and is in tutorial mode.

## Steps

1. Create an instance of TutorialModeService.
2. Call methods to set up board size, star difficulty, modifier availability, resource mode, region layout using valid parameters.
3. Verify that these settings are correctly reflected within the TutorialSetupConfig object returned by calling getter methods.

## Expected Result

Correct settings and configuration in response from getter methods after setting them with setter methods.
