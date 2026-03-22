# TC-TUTORIAL-064: Verify UIManager initialization with specific values

- **Requirement:** REQ-TUTORIAL-063
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 11:39:32

## Precondition

UIManager is initialized and TutorialSetupConfig object exists.

## Steps

1. Check if the dropdowns for board/stars/class/region selection have loaded with the correct values from the TutorialSetupConfig object.
2. Verify if 15 modifier toggles are correctly set up based on the settings defined in TutorialSetupConfig object.
3. Interact with each toggle and verify its behaviour (i.e., enabling/disabling).

## Expected Result

All dropdowns display correct values, all modifiers operate as expected when interacting with them.
