# TC-TUTORIAL-041: Verify ProfileService stores and loads persistent data for Tutorial Progress per profile

- **Requirement:** REQ-TUTORIAL-042
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 11:38:50

## Precondition

ProfileService, GameManager and RunDirector objects have been created.

## Steps

1. Start a tutorial run in the GameManager.
2. Save the current progress of the tutorial in the ProfileService.
3. Load this saved progress from ProfileService for another profile.
4. Assert that the loaded data corresponds to the previously saved progress.

## Expected Result

The saved and loaded data should be identical, indicating correct persistent storage and retrieval of Tutorial Progress per profile.
