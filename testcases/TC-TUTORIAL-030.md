# TC-TUTORIAL-030: RunDirector isolates run logic during tutorial based on TutorialSetupConfig object

- **Requirement:** REQ-TUTORIAL-032
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:22

## Precondition

UIManager, RunDirector and MainMenuController are initialized.

## Steps

1. Trigger a new tutorial run from the MainMenuController with default settings.
2. Verify if non-puzzle features of RunDirector like progression, items, path system are disabled during tutorial.

## Expected Result

Non-puzzle features are correctly disabled based on TutorialSetupConfig object.
