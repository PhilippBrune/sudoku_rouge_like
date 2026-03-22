# TC-TUTORIAL-075: Verify MainMenuController triggers TutorialModeService with default settings when starting tutorial mode

- **Requirement:** REQ-TUTORIAL-074
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:39:57

## Precondition

The game has been built and compiled successfully.

## Steps

1. Open the game in Unity.
2. Navigate to the main menu.
3. Click on the option to start tutorial mode from the main menu.
4. Verify that a call to TutorialModeService is made with default settings to setup a new tutorial run.

## Expected Result

The MainMenuController triggers a call to TutorialModeService with default settings when starting tutorial mode.
