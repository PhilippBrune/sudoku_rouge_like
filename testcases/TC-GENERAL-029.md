# TC-GENERAL-029: TestMainMenuControllerStartsTutorialModeFromMainMenu

- **Requirement:** REQ<｜begin▁of▁sentence｜>TUTORIAL-049
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:11

## Precondition

MainMenuController and TutorialModeService are instantiated.

## Steps

1. Click on the option to start tutorial mode from the main menu.
2. Verify that a call to TutorialModeService is made with default settings.

## Expected Result

TutorialModeService method 'StartTutorial' should be called once and without any parameters.
