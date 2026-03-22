# TC-TUTORIAL-052: TestTutorialModeServiceHandlesRulesOfTutorialModeCorrectly

- **Requirement:** REQ-TUTORIAL-052
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:11

## Precondition

TutorialModeService is instantiated and a valid TutorialSetupConfig exists.

## Steps

1. Setup the TutorialModeService with a new config.
2. Verify that all settings as defined in the config are handled correctly by the service.

## Expected Result

All rules for tutorial mode, including board size, star difficulty, modifier availability, resource mode and region layout should be applied to game world according to configuration.
