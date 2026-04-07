# TC-GENERAL-044: Sell Mechanics Restricted

- **Requirement:** REQ<｜begin▁of▁sentence｜>-023
- **Type:** Manual
- **Risk:** Low
- **Created:** 2026-03-22 12:40:29

## Precondition

A player has purchased an item or reroll and now wants to sell them.

## Steps

1. Have a known quantity of both items/rerolls in the inventory when starting the game.
2. Attempt to sell any item or reroll, while playing the game.
3. Verify that no gold is received from selling these items and they are not considered for progression criteria like ClassUnlockProgress.

## Expected Result

The system does not allow selling of permanent items/rerolls and prevents them from contributing towards progressions related to ClassUnlockProgress or other such conditions.
