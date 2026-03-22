# TC-GENERAL-033: TutorialModeService handles tutorial mode rules correctly - RULE_01

- **Requirement:** REQ<｜begin▁of▁sentence｜>-TUTORIAL-080&lt;s/REQ&gt;
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 11:40:12

## Precondition

A user exists in the system with a profile and is in tutorial mode.

## Steps

1. Create an instance of TutorialModeService.
2. Call methods to set up board size, star difficulty, modifier availability, resource mode, region layout using invalid parameters.

## Expected Result

An exception or error message indicating that the operation failed due to invalid input.
