# Time attack: the incense walk

**Version:** 1.0 | **Date:** 2026-10-25 | **Status:** new (concept for the owner's pick; nothing built)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-25 | new | Owner request: "A time attack mode: you start with X minutes, solving a puzzle adds Y based on how many errors etc. It is over the moment the timer reaches 0:00." Suggested roadmap item RM-123. |

## 1. The idea

An incense stick burns down. Every solved puzzle adds to it; every mistake costs time. When it has burned down, the walk ends at once, also in the middle of a puzzle. The score is how far the player got.

Name suggestion: **The Incense Walk** (fits the garden; "Time Attack" as the subtitle on the curtain).

## 2. Rules

| Rule | Proposal (numbers to tune in playtests) |
|---|---|
| **Start** | 8:00 minutes. |
| **A solved puzzle adds** | 5×5 +1:00, 6×6 +1:30, 7×7 +2:00, 8×8 +2:30, 9×9 +3:30; +15 s per star above 1. |
| **Mistakes** | Each wrong digit costs 10 s at once, and the puzzle's time bonus shrinks by 20 % per mistake (at most −80 %). A perfect puzzle adds +20 s on top. |
| **No HP** | Time is the life bar. Pencil marks are free. |
| **Over** | At 0:00 the walk ends at once, mid-puzzle too. No last-second save. |
| **Puzzles grow** | Puzzles 1–3: 5×5 and 6×6; 4–6: 6×6 and 7×7 with one rule; 7–9: 7×7 and 8×8 with one or two rules; from 10: 8×8 and 9×9 with two rules. The rules come from the modifier catalog, so each walk looks different. |
| **A breather** | After every fifth puzzle: pick one of three boons (+45 s; the next mistake costs nothing; one cell of the next puzzle revealed). The clock stops while the player chooses. |
| **Classes** | The class is chosen as usual and gets the XP. Each passive gets a time form, for example Garden Monk: every 5 correct digits +3 s; Koi Gambler: 33 % of mistakes cost no time; Lantern Seer: rules start 30 % weaker. |

## 3. Score

- **Score:** the sum over solved puzzles of (board size points × star multiplier), plus the seconds left at each solve. Mistakes count through the time they cost.
- **Records:** best score, most puzzles, the largest board solved; shown on the curtain and the result scroll (the mode results of UI-STYLE-026).
- **Leaderboard:** one per week with the week's seed (same puzzles for everyone), plus a free walk with a random seed for practice.
- **XP:** each solved puzzle gives its normal XP; no XP for the time left.

## 4. Fairness and technique

- **Puzzle generation must never cost time.** Large boards can take seconds to generate. The next puzzle is generated in the background while the current one is played; if it is not ready when needed, the clock stops and the screen says "Drawing the next garden".
- **Pause:** Escape or Start pauses the clock and covers the board, so a pause cannot be used to think.
- **Reduce Motion:** the burning stick still shrinks, without the smoke and the flicker.
- **Last 30 seconds:** the stick glows red and a quiet tick plays (both off with Reduce Motion and the sound options).

## 5. How it differs from the modes that exist

| Mode | What drives it |
|---|---|
| Garden Run | HP, items, relics, five floors |
| Endless Zen | Depth; HP carry over between puzzles; no clock |
| Spirit Trials | A par time per tier as a score multiplier, no hard end |
| **Incense Walk** | **The clock is the life bar; it ends at 0:00** |

## 6. What it touches

- A new `GameMode` and a mode service like `SpiritTrialsService` (clock, bonus table, score).
- Game Modes: a sixth curtain. Five curtains fill the screen today, so the layout needs a mockup first (proposed as a sixth curtain, or the Incense Walk as a slip under Spirit Trials).
- The puzzle screen: the incense stick in place of the HP lantern.
- Background generation of the next puzzle.
- Five catalogs, achievements (see the achievements list), a Steam leaderboard per week.

Effort: M to L (a week). Requirement IDs follow in a new spec (IncenseWalkMode) when the owner picks.

## 7. Questions for the owner

1. Start time and bonus table: 8:00 with the table above, or something shorter (5:00) for faster walks?
2. Mistakes: time at once (−10 s), a smaller bonus, or both (as proposed)?
3. Boons every fifth puzzle, or a pure walk with no choices?
4. Classes with time passives, or one neutral walker for a fair leaderboard?
5. A sixth curtain on Game Modes, or a slip under Spirit Trials?
