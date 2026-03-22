# TC-TUTORIAL-039: Verify MainMenuController triggers TutorialModeService with default settings on tutorial mode start

- **Requirement:** REQ-TUTORIAL-040
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:50

## Precondition

MainMenuController, TutorialModeService and GameManager objects have been created.

## Steps

1. Call the 'start tutorial' function from the MainMenuController.
2. Assert that the TutorialModeService was triggered with default settings.

## Expected Result

The start of tutorial mode should trigger a call to TutorialModeService with default settings.
