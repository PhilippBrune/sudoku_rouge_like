# TC-GENERAL-015: Profile Save System Test

- **Requirement:** REQ<｜begin▁of▁sentence｜>02
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:55:23

## Precondition

A new player profile is created and saved using the system

## Steps

1. Create a new player profile and save it to disk using the `SaveProfile` method.
2. Load that same profile back from disk using the `LoadProfile` method.

## Expected Result

The loaded profile should be identical to the one that was just saved.
