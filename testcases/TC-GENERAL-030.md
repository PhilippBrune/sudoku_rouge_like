# TC-GENERAL-030: Isolate Run Logic During Tutorial

- **Requirement:** REQ<｜begin▁of▁sentence｜>TORIAL-059
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:27

## Precondition

In tutorial mode

## Steps

1. Call to TutorialModeService that disables all non-puzzle features
2. Verify the effects on run logic, progression, items, path system etc.

## Expected Result

All non-puzzle features are disabled and run logic is isolated to only puzzle related operations during tutorial mode
