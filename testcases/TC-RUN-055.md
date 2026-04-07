# TC-RUN-055: Validate star rating scaling with depth

- **Requirement:** REQ-RUN-056
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 12:39:53

## Precondition

User is in gameplay session and has solved at least one puzzle

## Steps

1. Increase the depth level by solving multiple puzzles.
2. Check the current star rating value.
3. Verify if it scales correctly based on the formula Clamp(1 + depth/4, 1, 5).

## Expected Result

Star rating should scale linearly with depth up to a maximum of 5 stars.
