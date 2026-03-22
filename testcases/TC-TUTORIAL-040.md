# TC-TUTORIAL-040: Verify RunDirector isolates run logic during tutorial based on TutorialSetupConfig object

- **Requirement:** REQ-TUTORIAL-041
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:38:50

## Precondition

RunDirector, GameManager and TutorialSetupConfig objects have been created.

## Steps

1. Instantiate a new TutorialSetupConfig with specific settings for isolating run logic.
2. Pass this configuration to the RunDirector instance.
3. Assert that all non-puzzle features (progression, items, path system) are disabled during tutorial run based on these settings.

## Expected Result

The non-puzzle features should be correctly isolated based on changes made by TutorialSetupConfig object.
