# TC-SHOP-033: Test SellMechanicsSystem to ensure it functions correctly

- **Requirement:** REQ-SHOP-032, REQ-SHOP-023
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 13:48:45

## Precondition

Player has acquired some items and gold in their inventory

## Steps

1. Navigate to the shop or marketplace interface
2. Select an item to sell, for instance a potion or sword
3. Input the desired selling price and press sell
4. Verify if the correct amount of gold is added to the player's inventory after successful selling

## Expected Result

The expected result is that the SellMechanicsSystem should correctly manage the addition of gold post-transaction when items are sold
