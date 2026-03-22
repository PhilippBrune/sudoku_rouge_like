# TC-TUTORIAL-004: TutorialModeService handles tutorials-related rules in TutorialSetupConfig object

- **Requirement:** REQ-TUTORIAL-004
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:37:31

## Precondition

A new user starts tutorial mode.

## Steps

1. Start a new tutorial run using default settings.
2. Retrieve the 'TutorialSetupConfig' object after initialization.
3. Check if all rules defined in TutorialSetupConfig are correctly implemented and functioning.

## Expected Result

All values inside the 'TutorialSetupConfig' should correspond to their expected defaults or custom user-set configurations.
