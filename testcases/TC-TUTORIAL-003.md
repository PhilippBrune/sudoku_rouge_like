# TC-TUTORIAL-003: ProfileService stores and loads tutorial completion data per profile in TutorialProgress field

- **Requirement:** REQ-TUTORIAL-003
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 11:37:31

## Precondition

A new user has been registered.

## Steps

1. Create a new profile.
2. Set the 'TutorialProgress' field to some dummy value, for instance "Completed_Lvl1".
3. Save this progress.
4. Load the saved data back from ProfileService and check if the TutorialProgress is equal to the initial set value.

## Expected Result

The loaded 'TutorialProgress' should be exactly the same as what was saved before.
