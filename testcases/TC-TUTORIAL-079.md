# TC-TUTORIAL-079: Store and Load Tutorial Progress per profile

- **Requirement:** REQ-TUTORIAL-078
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:40:12

## Precondition

A user exists in the system with a profile.

## Steps

1. Create a new instance of ProfileService.
2. Call the method to save tutorial progress for the specific profile.
3. Save some data into it (for example, steps completed).
4. Call the method to load tutorial progress for that same profile.

## Expected Result

The loaded data matches the one previously saved.
