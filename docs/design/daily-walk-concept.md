# A quick run every day: the Daily Walk

**Version:** 1.1 | **Date:** 2026-10-26 | **Status:** review (owner's answers in; mockups for the owner's pick)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-10-26 | review | The owner's answers to section 7 (below) change the proposal: two floors with a boss each, no class, the XP to a class chosen at the end, the goals only inside the daily walk, a stamp shop for looks and a leaderboard of the day now. Mockups: `docs/design/layout-proposals.md` 2.9 (proposed UI-STYLE-027 to 029). |
| 1.0 | 2026-10-25 | new | Owner request: "The day run should give XP, not gold. Create a concept for how it makes it more worth playing a quick run every day." Suggested roadmap item RM-122. |

## 1. What the Daily Walk is today

- Three goals a day (easy, medium, hard), the same for every player on a date (`DailyGoalService`, DailyGoalSystem).
- The goals are met inside any Garden Run: "complete any puzzle", "reach Floor 2", "win a run".
- Rewards: easy +20 gold, medium +1 reroll token (both go into the current run, or the next one), hard 1 or 2 ink stamps.
- A streak counts the days with at least one goal done.

## 2. Why it does not pull a player back every day

1. **The rewards belong to one run.** 20 gold or a reroll token is gone when the run ends. Nothing stays.
2. **Ink stamps buy nothing.** They are counted and shown, and nothing in the game spends them.
3. **There is no short session.** The goals sit inside a five-floor run; the hard ones ("win a run", "reach Floor 4") need an evening.
4. **Nothing to compare.** Every player gets the same goals, but nobody sees how others did.

## 3. The proposal: a daily walk of one floor

A short run of its own, the same for every player that day, and it pays out in things that stay.

| Part | Proposal |
|---|---|
| **Length** | One floor: three puzzles and the floor's boss, about 10 to 15 minutes. The floor and its rules change daily (Floor 1 on Monday up to Floor 5 on Friday, a free pick on the weekend). |
| **Seed** | The day's seed: same boards, same rules, same offers for everyone (like the seed of the week, `WeeklySeedService`). |
| **Class** | The player's choice. The XP goes to that class, so the daily walk is also the quick way to level a class. |
| **Tries** | One scored try a day. Playing it again is allowed, but pays nothing and does not change the score. |
| **Goals** | The three goals stay, but are met inside the daily walk and fit its length ("no mistake on the boss", "finish with 3 HP or more", "use no pencil on one puzzle"). |

## 4. Rewards: XP, not gold

| Reward | Amount | Why |
|---|---|---|
| Daily walk finished | The run's XP **×1.5** for the class played ("dawn bonus") | A daily walk on Floor 3 gives about 400 XP, 600 with the bonus: one and a half levels for a class around Level 20 (365 XP to the next level). |
| Each goal | Easy +50 XP, medium +100 XP, hard +200 XP, for the class played | Replaces 20 gold, the reroll token and the ink stamp. |
| All three goals | +1 ink stamp | The ink stamp becomes the streak's mark, not a currency without use. |
| Streak | Day 3: +10 % XP, day 7: +25 % XP, kept while the streak holds | A reason to come back tomorrow. One missed day costs the bonus, not the stamps. |
| 7 ink stamps | A **weekly scroll**: 500 XP for any class, picked by the player | A reason to finish the week. |

Optional, and only with the owner's pick: ink stamps also buy looks (an ink colour for the board, a frame for the class seal, a seal for the path). That gives stamps a lasting use without touching balance.

## 5. Something to compare

- The daily walk's run score (GameDesignSpec §9c) goes to a Steam leaderboard of the day, with a friends filter on the Daily Walk curtain. This is RM-117 "The daily garden" in its short form.
- The end scroll says where the player stands that day ("12th of 140, 3rd among friends").

## 6. What it touches

- `DailyGoalService`: goal pool for one floor, XP instead of gold and tokens, streak bonus.
- A run of one floor: `RunGraphService` builds a floor already; the run ends after its boss with the run-end scroll (Walk again does not apply).
- Game Modes: the Daily Walk curtain gets "Walk today's floor" (the curtain exists, UI-STYLE-025).
- Steam: one leaderboard per day (`SteamLeaderboardService` creates leaderboards by name).
- Five catalogs for the new goal texts.

Effort: M (a few days). Requirement IDs follow in DailyGoalSystem when the owner picks.

## 7. Questions for the owner

1. One floor (three puzzles and a boss), or three puzzles without a boss?
2. The class free, or one class per day for everyone (fairer for the leaderboard)?
3. Do the old goals inside the Garden Run go away, or stay as a second way to earn the daily XP?
4. Ink stamps: only the streak's mark, or also a shop for looks?
5. A leaderboard of the day now, or later with RM-117?

## 8. The owner's answers (2026-10-25)

1. Length: two floors, each with its boss ("2 floors + boss"; asked again: "Jede Etage mit Boss").
2. Class: none, "to avoid that a class is still not unlocked". Everyone walks with the same HP, Pencil and bag. The
   XP goes to an unlocked class the player chooses on the result screen ("Klasse am Ende wählen").
3. The old goals inside the Garden Run go away; the three goals are met inside the daily walk.
4. Ink stamps also buy looks in a shop.
5. A Steam leaderboard of the day now.

Open for the build: the neutral walker's HP, Pencil and bag; the goal pool for two floors; whether the weekly scroll
(7 ink stamps, section 4) stays now that stamps are spent in the shop.
