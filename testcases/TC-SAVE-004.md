# TC-SAVE-004: Validation & Sanitization of Save Data Test

- **Requirement:** REQ-SAVE-005
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 08:55:23

## Precondition

A game with corrupt data is loaded

## Steps

1. Corrupt a save file and load it into the system using the `LoadProfile` or `LoadRunState` methods.
2. Validate that all sanitization rules are enforced, preventing any inconsistency during loading.

## Expected Result

The system should either throw an error or correct the data based on its sanitation rules to ensure consistency and prevent corruptions.
