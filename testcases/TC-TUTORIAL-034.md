# TC-TUTORIAL-034: Tutorial Mode Test Case for selecting board size, star difficulty, modifier availability, resource mode and region layout via TutorialSetupConfig object

- **Requirement:** REQ-TUTORIAL-034
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:36

## Precondition

A new tutorial session has been started.

## Steps

1. Start a new tutorial session.
2. Using the helper methods provided by TutorialModeService to set different configurations for board size, star difficulty, modifier availability, resource mode and region layout in TutorialSetupConfig object.
3. Verify if all these settings are correctly reflected in the game world.

## Expected Result

The UI dropdowns show the correct values after setting them via TutorialModeService's helper methods.
