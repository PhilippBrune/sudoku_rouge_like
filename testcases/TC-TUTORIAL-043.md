# TC-TUTORIAL-043: TutorialModeService handles tutorial mode configuration

- **Requirement:** REQ-TUTORIAL-043
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:38:57

## Precondition

A new instance of TutorialModeService is created.

## Steps

1. Create a new instance of TutorialSetupConfig object.
2. Set up the board size, star difficulty, modifier availability, resource mode and region layout in the configuration object.
3. Use the setter methods to update these configurations in TutorialModeService.
4. Retrieve the updated configurations using getter methods from TutorialModeService.

## Expected Result

The returned values match the set values.
