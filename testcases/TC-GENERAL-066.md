# TC-GENERAL-066: UnityVersionCheckerTest

- **Requirement:** REQ-GENERAL-002
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 12:45:52

## Precondition

Player starts the game at startup

## Steps

1. Use a version of Unity that is not within allowed range to check if it displays a warning message

## Expected Result

The game should display an error message stating "Unsupported Unity version detected." when using a non-allowed Unity version
