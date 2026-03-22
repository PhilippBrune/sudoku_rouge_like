# TC-TUTORIAL-082: TutorialModeService handles tutorial mode rules correctly - RULE_03

- **Requirement:** REQ-TUTORIAL-082
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 11:40:12

## Precondition

A user exists in the system with a profile and is in tutorial mode.

## Steps

1. Create an instance of TutorialModeService.
2. Verify that UI dropdowns for board size, star difficulty, modifier availability, resource mode, region layout are correctly populated using the helper methods based on TutorialSetupConfig object.

## Expected Result

The options in each dropdown match what is configured in TutorialSetupConfig object and can be selected by user without throwing any exceptions or errors.
