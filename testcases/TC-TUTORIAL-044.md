# TC-TUTORIAL-044: UI dropdowns update according to tutorial configuration

- **Requirement:** REQ-TUTORIAL-043
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:57

## Precondition

A new instance of TutorialModeService and UIManager are created. The configurations have been set up in the former.

## Steps

1. Create a mock object for UIManager.
2. Set up the return values for UI dropdown methods to correspond with configuration settings in TutorialSetupConfig object.
3. Update UI elements through UIManager based on updated configurations in TutorialModeService.

## Expected Result

The UI dropdowns reflect the correct options as per the tutorial setup.
