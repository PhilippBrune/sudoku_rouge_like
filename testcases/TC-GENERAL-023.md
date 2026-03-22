# TC-GENERAL-023: New Tutorial Run Setup Using Default Settings

- **Requirement:** REQ<｜begin▁of▁sentence｜>022
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 11:37:59

## Precondition

The game is running and the user has triggered tutorial mode from MainMenuController.

## Steps

1. Trigger a new tutorial run using default settings via MainMenuController.
2. Verify that TutorialModeService sets up a new tutorial run as expected by checking if necessary objects/services have been initialized correctly.

## Expected Result

A new tutorial run should be set up successfully with default settings and all related objects/services should have been properly initialized.
