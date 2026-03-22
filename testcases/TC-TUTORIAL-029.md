# TC-TUTORIAL-029: Start tutorial mode from MainMenuController triggers TutorialModeService with default settings

- **Requirement:** REQ-TUTORIAL-031
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:22

## Precondition

UIManager, RunDirector and MainMenuController are initialized.

## Steps

1. From the main menu, select the option to start tutorial mode.
2. Verify if a call is made to TutorialModeService with default settings for setup.

## Expected Result

Option from MainMenuController triggers call to TutorialModeService.
