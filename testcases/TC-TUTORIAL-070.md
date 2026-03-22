# TC-TUTORIAL-070: TutorialModeService handles rules of tutorial mode based on configuration in TutorialSetupConfig object

- **Requirement:** REQ-TUTORIAL-070
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:45

## Precondition

Game is running in tutorial mode and a valid TutorialSetupConfig has been set up

## Steps

1. Set up a specific TutorialSetupConfig for tutorial mode
2. Check how the game behaves based on this configuration through various steps of the tutorial
3. Test all helper methods provided by TutorialModeService to ensure they correctly reflect changes in TutorialSetupConfig

## Expected Result

The game should behave as per rules defined in TutorialSetupConfig during each step of the tutorial
