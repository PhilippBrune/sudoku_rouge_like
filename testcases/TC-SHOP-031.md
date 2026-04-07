# TC-SHOP-031: Test EconomyBalanceTargetsSystem to ensure it functions correctly

- **Requirement:** REQ-SHOP-030, REQ-SHOP-021
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 13:48:45

## Precondition

Player has visited multiple floors and acquired different amounts of gold

## Steps

1. Activate the game and navigate to various new floors (town or dungeons) with randomized gold acquisition
2. Complete all available actions on each floor, for instance by defeating monsters, gathering resources, etc.
3. Verify if the balance between gold gain and loss is maintained across different rounds of playthrough

## Expected Result

The expected result is that the EconomyBalanceTargetsSystem should maintain a balance between gold gain and loss as players progress through multiple floors
