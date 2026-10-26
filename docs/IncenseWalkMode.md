# Incense Walk Mode

**Version:** 0.1 | **Date:** 2026-10-26 | **Status:** under review (the clock, score, puzzle growth and class time forms are built in `IncenseWalkService`; the mode, its screens and the leaderboard wait for the owner's pick of the mockups)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.1 | 2026-10-26 | under review | From `docs/design/time-attack-concept.md` 1.1 and the owner's answers (2026-10-25): "implement as it is now for playtesting and balancing", both mistake penalties, no boons, classes with time passives, a sixth mode. Roadmap RM-123. |

## Overview

A time attack. An incense stick burns down; every solved puzzle adds time to it, every mistake costs time. When it has
burned down the walk ends at once, also in the middle of a puzzle. The score is how far the player got. All numbers
are first values for playtesting and balancing; they live as constants in `IncenseWalkService`.

## The clock

| ID | Rule |
|----|------|
| INCENSE-CLOCK-001 | The walk starts with 8:00 (480 s). |
| INCENSE-CLOCK-002 | A solved puzzle adds its board's seconds: 5×5 +1:00, 6×6 +1:30, 7×7 +2:00, 8×8 +2:30, 9×9 +3:30; +15 s per star above 1; a perfect puzzle (no mistake) adds +20 s on top. |
| INCENSE-CLOCK-003 | A wrong digit costs 10 s at once, and the puzzle's bonus shrinks by 20 % per mistake (at most −80 %). |
| INCENSE-CLOCK-004 | At 0:00 the walk ends at once; nothing after that counts. |
| INCENSE-CLOCK-005 | Pause (Escape or Start) stops the clock and covers the board. If the next puzzle is not generated yet, the clock stops and the screen says "Drawing the next garden". |

There are no boons and no choices between puzzles (owner, 2026-10-25).

## The puzzles

| ID | Rule |
|----|------|
| INCENSE-PUZZLE-001 | The puzzles grow every three: puzzles 1–3 are 5×5 or 6×6 with 1–2 stars and no rule; 4–6 are 6×6 or 7×7 with 2 stars and one rule; 7–9 are 7×7 or 8×8 with 2–3 stars and one or two rules; from 10 on 8×8 or 9×9 with 3 stars and two rules. The rules are drawn from the modifiers that generate on the board's size. The same seed gives the same walk. |

## Score and records

| ID | Rule |
|----|------|
| INCENSE-SCORE-001 | Each solved puzzle scores its board's XP points × the star multiplier (`XpTable`) plus the seconds left at the moment it is solved. Mistakes count through the time they cost. |
| INCENSE-SCORE-002 | Records: best score, most puzzles, the largest board solved; on the Game Modes curtain and the result scroll (UI-STYLE-026). |
| INCENSE-BOARD-001 | One Steam leaderboard per week, played on the week's seed (same puzzles for everyone). A practice walk on a random seed scores nothing. |
| INCENSE-XP-001 | Each solved puzzle gives the chosen class its normal XP; no XP for the time left. |

## Classes

| ID | Rule |
|----|------|
| INCENSE-CLASS-001 | The class is chosen as usual and gets the XP. Its passive works in time form (table below). |

Proposed time forms, for playtesting:

| Class | Time form |
|-------|-----------|
| Number Freak | 10 correct digits in a row (no mistake between) +5 s |
| Garden Monk | every 5th correct digit of a puzzle +3 s |
| Shrine Archivist | a correct digit in a cell that held pencil marks +2 s |
| Koi Gambler | a third of the mistakes cost no time |
| Stone Gardener | starts with +1:00 |
| Lantern Seer | its passive as in the Garden Run (the rules are weaker) |
| Reed Duelist | a perfect puzzle solved without pencil +30 s |
| Quiet Cartographer | the first 5 s of each new board do not burn |

## Screens

The Game Modes curtain (proposed UI-STYLE-027) and the puzzle screen with the incense stick (proposed UI-STYLE-030) are
mockups in `docs/design/layout-proposals.md` 2.9, waiting for the owner's pick. Reduce Motion keeps the shrinking stick
and drops the smoke and the flicker; in the last 30 seconds the ember turns red.
