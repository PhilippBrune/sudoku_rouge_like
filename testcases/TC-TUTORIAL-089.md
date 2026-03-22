# TC-TUTORIAL-089: Test TutorialModeService setup with different configurations

- **Requirement:** REQ-TUTORIAL-088
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:40:32

## Precondition

GameController is initialized and TutorialModeService has been instantiated.

## Steps

1. Set up various TutorialSetupConfig objects, each with different board sizes, star difficulties, modifier availability, resource mode and region layout.
2. Pass these configurations to the TutorialModeService.
3. Verify if the service properly applies these configurations.

## Expected Result

The TutorialModeService correctly configures itself based on the given setup.
