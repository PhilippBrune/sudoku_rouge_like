# TC-RUN-061: Test High Score Tracking Using ProfileStats.HighestEndlessDepth

- **Requirement:** REQ-RUN-062
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 12:40:06

## Precondition

Player has completed multiple levels with varying depths

## Steps

1. Complete a series of levels with increasing depth
2. Check the value stored in ProfileStats.HighestEndlessDepth after each level completion
3. Verify if it updates correctly to the highest depth reached

## Expected Result

The ProfileStats.HighestEndlessDepth should update to the maximum depth that is achieved, showing accurate high score tracking
