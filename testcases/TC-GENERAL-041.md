# TC-GENERAL-041: Test Resource Tracking Using HP, Pencil Resources

- **Requirement:** REQ<｜begin▁of▁sentence｜>-059
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:40:06

## Precondition

The game is initialized with HP and Pencil resources at a certain value

## Steps

1. Create an instance of the game in a test environment
2. Complete a level that requires resource consumption
3. Verify if the HP and Pencil values decrease by the required amount
4. Complete another level after restoring some HP

## Expected Result

The HP and Pencil resources should decrease as levels are completed, and increase back to full when restored
