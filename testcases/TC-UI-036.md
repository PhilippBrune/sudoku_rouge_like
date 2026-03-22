# TC-UI-036: Test AnimationsInEndOfRunExperience in end of run experience

- **Requirement:** REQ-UI-036
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 12:38:05

## Precondition

The player has finished a level/run and is in the XP bar fill animation (end-of-run) phase

## Steps

1. Complete a level or run successfully
2. Press Space key during the XP bar fill animation to skip it
3. Check if the rising tone SFX plays when the XP bar fills up smoothly

## Expected Result

The XP bar fill should be smooth and play a skippable rising tone SFX on completion regardless of outcome, and able to be skipped with space key.
