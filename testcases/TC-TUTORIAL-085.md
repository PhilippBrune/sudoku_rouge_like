# TC-TUTORIAL-085: Test MainMenuController option to start tutorial mode from main menu

- **Requirement:** REQ-TUTORIAL-085
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 11:40:25

## Precondition

The game is running and the MainMenuController script has a "Start Tutorial" button.

## Steps

1. Open the game and navigate to the main menu.
2. Click on "Start Tutorial" button.
3. Verify if call to TutorialModeService with default settings is triggered.

## Expected Result

The TutorialModeService should start a new tutorial run when the "Start Tutorial" button is clicked.
