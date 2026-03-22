# TC-GENERAL-025: UI elements of Tutorial Mode setup correctly based on TutorialSetupConfig object

- **Requirement:** REQ<｜begin▁of▁sentence｜>TUTORIAL-029
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:22

## Precondition

UIManager is initialized and references a valid TutorialSetupConfig object.

## Steps

1. In the tutorial mode, select different board/stars/class/region options in dropdowns for UI elements.
2. Update modifier toggles to reflect settings defined in TutorialSetupConfig object.

## Expected Result

Dropdowns and toggles display correct values based on TutorialSetupConfig object.
