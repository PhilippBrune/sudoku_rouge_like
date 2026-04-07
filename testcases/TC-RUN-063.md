# TC-RUN-063: Verify Achievements Tracking

- **Requirement:** REQ-RUN-063
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 12:40:16

## Precondition

SteamAchievementService is correctly implemented and integrated into the game.

## Steps

1. Start a new game and reach Endless Zen depth of 20.
2. Reach 50 Eternal Patience.
3. Check if achievements have been unlocked on SteamAchievementService.

## Expected Result

Achievements REQ-RUN-063-1 and REQ-RUN-063-2 should be locked, but REQ-RUN-063-3 should be unlocked.
