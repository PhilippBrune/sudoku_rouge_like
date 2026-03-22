# TC-TUTORIAL-019: MainMenuController Triggers TutorialModeService with Default Settings

- **Requirement:** REQ-TUTORIAL-019
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:37:59

## Precondition

The game is running and the user has selected tutorial mode from the main menu.

## Steps

1. Launch the game.
2. From the main menu, select "Tutorial Mode".
3. Verify that MainMenuController triggers TutorialModeService with default settings.

## Expected Result

A call to TutorialModeService should have been made with default settings.
