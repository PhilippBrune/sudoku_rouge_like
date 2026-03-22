# TC-TUTORIAL-014: ProfileService Stores and Loads Tutorial Progress Correctly

- **Requirement:** REQ-TUTORIAL-015
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:37:50

## Precondition

The ProfileService is loaded with a specific tutorial progress data.

## Steps

1. Store some tutorial progress data in the ProfileService.
2. Verify if this data can be correctly retrieved later using load function.

## Expected Result

Data should be stored and loaded correctly, so that when retrieve, it matches what was originally saved.
