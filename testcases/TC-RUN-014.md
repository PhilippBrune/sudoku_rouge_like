# TC-RUN-014: Leaderboard Ranking System

- **Requirement:** REQ-RUN-015
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 08:53:42

## Precondition

Multiple players have completed a level and submitted scores to the leaderboards.

## Steps

1. Request for the list of top highscores from the leaderboard.
2. Verify that the system ranks these highscores in descending order based on their score value.

## Expected Result

The game should return a list of top highscores ordered by highest to lowest scores.
