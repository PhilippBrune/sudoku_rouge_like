# TC-TUTORIAL-028: Reference actual service/controller names in linked systems

- **Requirement:** REQ-TUTORIAL-028
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:22

## Precondition

UIManager, MainMenuController and RunDirector are initialized.

## Steps

1. Check if UIManager references an actual TutorialSetupConfig object.
2. From the main menu, verify if selecting tutorial mode triggers a call to TutorialModeService with default settings for setup.
3. Verify that RunDirector is isolating run logic during tutorial by disabling all non-puzzle features.

## Expected Result

References are valid and actions match expected behaviors.
