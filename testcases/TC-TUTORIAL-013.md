# TC-TUTORIAL-013: UIController Dropdowns Verification

- **Requirement:** REQ-TUTORIAL-013
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:37:50

## Precondition

The game is in tutorial mode and the UIController is loaded.

## Steps

1. Check if the UIController has dropdown menus for board/stars/class/region selection.
2. Choose a specific option from each drop-down menu and verify if it correctly affects the game state or settings as per the selected option.
3. Verify that all options provided in TutorialSetupConfig object are visible and functional in UIController dropdowns.

## Expected Result

All dropdown menus should show correct available options, and selecting an option should lead to corresponding changes in game state or settings.
