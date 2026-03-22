# TC-TUTORIAL-081: TutorialModeService handles tutorial mode rules correctly - RULE_02

- **Requirement:** REQ-TUTORIAL-081
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 11:40:12

## Precondition

A user exists in the system with a profile and is in tutorial mode.

## Steps

1. Create an instance of TutorialModeService.
2. Call methods to set up board size, star difficulty, modifier availability, resource mode, region layout using specific values (for example, 5x5 board size).

## Expected Result

Correct settings and configuration in response from getter methods after setting them with setter methods for the specified values.
