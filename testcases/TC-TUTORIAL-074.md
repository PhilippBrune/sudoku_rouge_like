# TC-TUTORIAL-074: Verify UIManager references actual service/controller names

- **Requirement:** REQ-TUTORIAL-073
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:57

## Precondition

The game has been built and compiled successfully.

## Steps

1. Open the game in Unity.
2. Check if all UI elements are correctly linked to their respective services or controllers (e.g., UIManager for UI elements, RunDirector and ProfileService for persistent data handling).
3. Verify that the UIManager is set up to handle the UI elements of Tutorial Mode including dropdowns for board/stars/class/region selection and 15 modifier toggles. It should update based on settings defined in TutorialSetupConfig object.

## Expected Result

All UI elements are correctly linked with their respective services or controllers, and the UIManager is set up to handle the UI elements of Tutorial Mode as per requirements.
