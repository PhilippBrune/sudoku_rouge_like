# TC-TUTORIAL-051: TestProfileServiceSavesAndLoadsTutorialProgressPerProfile

- **Requirement:** REQ-TUTORIAL-051
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:11

## Precondition

ProfileService is instantiated and a profile exists.

## Steps

1. Setup some progress data for the tutorial in TutorialModeService.
2. Save this progress data using the ProfileService.
3. Load progress from the same ProfileService.

## Expected Result

Loaded progress data matches saved data and should not be null or default values.
