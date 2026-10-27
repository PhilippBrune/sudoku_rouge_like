# Incense Walk Mode

**Version:** 0.3 | **Date:** 2026-10-27 | **Status:** review (built: the mode, its curtain on Game Modes, the puzzle screen with the incense stick, the result and the week's board)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.3 | 2026-10-27 | review | Owner request (2026-10-27): the time attack concept (v1.1) is merged here: "Why", "How it differs from the other modes", "Fairness and technique" and the owner's answers. The two open answers of 2026-10-27 are now rules: a free Koi Gambler mistake counts for nothing (INCENSE-CLASS-001, built) and a walk is not a run (INCENSE-XP-001, built). Only the midnight edge of the week's board stays open. Its file is removed. |
| 0.2 | 2026-10-26 | review | Owner: "Implementier die UI-Styles". Built: `GameMode.IncenseWalk` in `RunDirector` (no path, no bag, free pencil marks, a wrong digit costs time instead of HP), the sixth curtain (UI-STYLE-027) with a practice walk on a random seed, the incense stick in place of the lanterns (UI-STYLE-030), the result in the mode-result family and the week's Steam board "IncenseWalk_yyyy-Www" for a walk on the week's seed. The walk cannot be resumed: Save & Quit ends it without a result, as in Endless Zen. |
| 0.1 | 2026-10-26 | under review | From the time attack concept (merged in 0.3) and the owner's answers (2026-10-25): "implement as it is now for playtesting and balancing", both mistake penalties, no boons, classes with time passives, a sixth mode. Roadmap RM-123. |

## Overview

A time attack. An incense stick burns down; every solved puzzle adds time to it, every mistake costs time. When it has
burned down the walk ends at once, also in the middle of a puzzle. The score is how far the player got. All numbers
are first values for playtesting and balancing; they live as constants in `IncenseWalkService`.

## Why

The owner asked (2026-10-25) for a time attack: the player starts with some minutes, solving a puzzle adds time that
depends on the mistakes, and the walk is over the moment the timer reaches 0:00. The name is The Incense Walk: the
incense stick is the clock, and "Time Attack" is the subtitle on the curtain.

## How it differs from the other modes

| Mode | What drives it |
|------|----------------|
| Garden Run | HP, items, relics, five floors |
| Endless Zen | Depth; HP carry over between puzzles; no clock |
| Spirit Trials | A par time per tier as a score multiplier, no hard end |
| **Incense Walk** | **The clock is the life bar; it ends at 0:00** |

## Fairness and technique

- **Generation never costs time.** The next puzzle is generated while the current one is played. If it is not ready, the
  clock stops and the screen says "Drawing the next garden" (INCENSE-CLOCK-005).
- **Pause** (Escape or Start) covers the board, so a pause cannot be used to think (INCENSE-CLOCK-005).
- **Reduce Motion:** the stick still shrinks; the smoke and the flicker go.
- **Last 30 seconds:** the ember turns red (Screens). The concept also asked for a quiet tick, both off with Reduce
  Motion and the sound options. The tick was not found in the code (checked 2026-10-27).

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
| INCENSE-XP-001 | Each solved puzzle gives the chosen class its normal XP; no XP for the time left. A walk is not a run: TotalRunsStarted is not raised, so walks do not count toward the Endless Zen unlock (10 runs); the XP is kept (owner, 2026-10-27: "nein", built). Its puzzle gold does not count toward class unlocks. |

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
| Koi Gambler | a third of the mistakes cost no time; a mistake that costs no time does not count at all (no 20 % cut to the bonus, the perfect bonus is kept; owner, 2026-10-27: "ja", built) |
| Stone Gardener | starts with +1:00 |
| Lantern Seer | its passive as in the Garden Run (the rules are weaker) |
| Reed Duelist | a perfect puzzle solved without pencil +30 s |
| Quiet Cartographer | the first 5 s of each new board do not burn |

## Owner answers

Owner, 2026-10-25 (the concept's questions; the answers are the rules above):

1. Start time and bonus table: "implement as it is now for playtesting and balancing" (8:00 and the table above).
2. Mistakes: both penalties (−10 s at once and −20 % of the bonus).
3. Boons: none ("yes no choices").
4. Classes: with time passives.
5. Game Modes: a sixth curtain ("a sixth playmode").

Owner, 2026-10-27 (answers to the open questions of 0.2, built):

- Should a free Koi Gambler mistake count at all? "ja": a mistake that costs no time does not count at all
  (INCENSE-CLASS-001).
- Should a walk count as a run? "nein": it is not a run toward the Endless Zen unlock; the XP is still kept
  (INCENSE-XP-001).

## Open for the owner

- A walk on the week's seed that ends after Sunday midnight is scored on the new week's board. Not decided.

## Screens

Built (owner 2026-10-26, "Implementier die UI-Styles"): the sixth curtain on Game Modes with the rule, the records, the
week's board and a practice walk (UI-STYLE-027), the puzzle screen with the incense stick in place of the HP and Pencil
lanterns (UI-STYLE-030) and the result in the mode-result family; screenshots in `docs/design/layout-proposals.md` 3.1.
The stick has no smoke and no flicker, so Reduce Motion changes nothing; in the last 30 seconds the ember and the time
turn red.
