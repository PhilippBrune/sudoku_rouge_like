# One more run: making Run of the Nine feel like a roguelike

**Version:** 1.1 | **Date:** 2026-10-22 | **Status:** review (the first slice of §5 is built; the other proposals wait for the owner's pick)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-10-22 | review | Owner request: "implement 5. A first slice for the next update and measure the run length to decide on RM-113. Do not change the amount of floors!" RM-110, RM-111, RM-112 and RM-115 built; the run-length measures of §6 are recorded in the game and read by `tools/playtest/run_length_report.py`; five floors stay. See §8. |
| 1.0 | 2026-10-21 | review | First concept, at the owner's request: what makes a player start one more run, what the game already has, what is missing, and twelve proposals ranked by effect and effort (suggested RM-110 to RM-121). |

## 1. What pulls a player into one more run

The roguelikes people cannot put down (Slay the Spire, Balatro, Hades, Into the Breach, Dead Cells) share a handful of
mechanics. None of them is about difficulty; they are about the moment right after a run ends.

| Pull | What the player feels | How the classics do it |
|---|---|---|
| **A short way back in** | "It is only a few seconds to try again." | One button from death to the next run; the first real choice within a minute. |
| **This run could be the one** | "If I get that relic early, this build breaks the game." | Build-defining pieces that change the rules, offered early and often enough to chase. |
| **The near miss** | "I was so close. I know what I did wrong." | The end screen shows how far you got and what killed you, next to your best. |
| **Something always moves forward** | "Even a bad run gave me something." | Every run feeds a bar: an unlock, a level, a codex entry, a step up the ladder. |
| **A named next goal** | "Two more bosses and the Stone Gardener is mine." | The end screen names the closest unlock and how far it is. |
| **Choices with stakes** | "Do I take the cursed tile for the gold?" | Risk and reward on the map; elites, wagers, pacts. |
| **Today's puzzle** | "Everybody plays the same run today." | A daily seed with one try and a leaderboard. |
| **A ladder you climb** | "Ascension 7 is next." | A difficulty ladder that unlocks one step per win. |

The two strongest are the first and the third: if trying again is fast and the end screen says "so close", players press
the button before they decide to stop.

## 2. What Run of the Nine already has

Most of the long-term pulls are there:

- **The ladder:** Harmony 0 to Void Garden, one step per win at the highest level (HARMONY-UNLOCK-001), with perks from H4.
- **Something moves forward:** class XP with level rewards (bonuses, the Level 30 item, the Level 40 frame), prestige,
  class unlocks (bosses, items used, relics, gold, the codex, a run with no mistakes), the Codex with Curses.
- **Choices with stakes:** two lanes per floor, cursed tiles (×1.5 gold and XP for an extra rule), the boss gate's trials,
  shops where gold now matters (RM-108), events.
- **Today's puzzle:** the Daily Walk's three goals, the week's seed on Select Class, the Monthly Walk with one score.
- **Grades:** a stamp per puzzle and a run score that adds up (SCORE-GRADE-003).
- **The run-end scroll** already names the next class to unlock.

## 3. What keeps it from feeling like "one more"

1. **The way back in is long.** After a run: Back to the garden gate → main menu → Game Modes or Start → Select Class →
   Begin, then the floor entry. Five screens before the first choice.
2. **A run is long.** Five floors of Sudoku, a boss on each; a 9×9 takes minutes. A run that takes an evening is not a
   run you repeat "quickly". (We have no measured run length; see §6.)
3. **Runs feel alike.** Items and relics help (reveal, check, heal), but few change *how* the player solves. In a
   roguelike the build is the run's identity; here the identity is mostly the class.
4. **A loss teaches little.** The end scroll shows the floor reached and the grades, but not "you lost on the Floor 3
   boss with 2 HP left; your best with this class is Floor 4" or what this run added to the next unlock.
5. **Stakes are hidden in numbers.** A cursed tile says ×1.5; the boss gate's trials do not say what each extra trial
   earns. The player cannot feel the wager.

## 4. Proposals

Ranked by what they do for "one more run" against their cost. Effort: **S** = a day or less, **M** = a few days,
**L** = a week or more. Each is a suggestion with a roadmap ID for the owner's pick.

| ID | Proposal | What the player gets | Why it pulls | Builds on | Effort |
|---|---|---|---|---|---|
| RM-110 | **Walk again** | On the run-end scroll and the mode results: "Walk again" (same class, mode and Harmony, new seed) as the first button, Back second. | The way back in drops from five screens to one press. | Run-end scroll (UI-STYLE-016/026), LaunchRequest | S |
| RM-111 | **So close** | The end scroll says where and how the run ended ("the Floor 3 boss, 2 HP short", "4 cells from the end"), and puts it next to the best with this class ("best: Floor 4"). | The near miss is the strongest pull; we only show the floor. | RunAnalytics, best floor per class (META-STATS-001) | S |
| RM-112 | **What this run moved** | Under the XP bar: the bars this run filled ("Stone Gardener: bosses 7 → 9 of 10", "Codex 41 → 44", "Harmony V: win at IV"), the closest one first. | Even a lost run feels useful; the next goal is named with a number. | ClassUnlockService progress, Codex counts | S–M |
| RM-113 | **A short walk** | A three-floor Garden Run (Floors 1, 3, 5 with their bosses), half the XP, its own Harmony track; for an evening's second run. | A run you can finish in half an hour is a run you start again. Measure first (§6). | RunGraphService floors, Game Modes curtain | M |
| RM-114 | **Keystone relics** | After the first boss, one of three keystones that bend a rule in the player's favour for the run: "every finished box gives 1 gold", "pencil marks in 6×6 are free", "the first mistake in each puzzle costs no HP", "Killer cages show their sums' last digit". | Runs get an identity and a chase ("I want the cage keystone with the Monk"). | Relic system, the boss-defeated screen (pick of three exists) | M–L |
| RM-115 | **Wagers you can feel** | The boss gate shows what each extra trial adds ("+1 trial: relic of the next rarity, ×1.2 score"); cursed tiles show the gold and XP they add in numbers. | A choice with visible stakes is a story the player tells. | Boss gate (UI-STYLE-003), cursed tile scene | S–M |
| RM-116 | **Class challenges** | Three challenges per class on the garden of classes ("Win with no pencil", "Clear Floor 5 with 1 HP", "Win at Harmony VI"), each with a seal on the plaque and a Steam achievement. | Long-term goals per class; each class gets more than its unlock. | Garden of classes (UI-STYLE-019), achievements | M |
| RM-117 | **The daily garden** | One seeded Garden Run per day, one try, a Steam leaderboard and the friends' scores on the curtain. | "Everybody plays the same run today" is a reason to open the game every day. | Daily Walk, leaderboards, seed sharing (RM-003, RM-101) | M |
| RM-118 | **Unlocks every run early on** | In the first ten runs something new opens each time: a relic into the pool, an event, a rule in the Codex, the next class; listed as "New in the garden" on the run end. | The first hours decide whether a player stays. | Unlock tables, the run end's "New" column | M |
| RM-119 | **Pacts instead of a fixed ladder** | From H4, pick pacts (each a rule: "no shop on Floor 1", "bosses bring an extra trial") worth Harmony points; the ladder counts points, not levels. | Players set their own challenge and chase a score; Hades' Heat. | Harmony perks and table | L |
| RM-120 | **Faster first choice** | The floor entry skippable from the first frame (exists), the first puzzle's board ready behind it (pre-generated), the path's first pick focused. | The first decision within a minute of pressing Walk again. | Floor entry (UI-STYLE-021), pre-bake | S |
| RM-121 | **A rival run** | Seed sharing with the friend's result shown as a ghost line on the end scroll ("Mara: Floor 5, score 1,840"). | Social comparison is the oldest pull; cheap once seeds are shared. | RM-003, RM-204 | M |

## 5. A first slice for the next update

Small, uses what the game already records, and changes how the end of every run feels:

1. **RM-110 Walk again** (S): the biggest change for the least work.
2. **RM-111 So close** and **RM-112 What this run moved** (S–M): the end scroll becomes the reason to press Walk again.
3. **RM-115 Wagers you can feel** (S–M): the boss gate and cursed tiles say what they give.
4. **Measure the run length** (§6) to decide on **RM-113 A short walk**.

Then, as the content update: **RM-114 Keystone relics** with RM-104 (more relics, items and events).

## 6. What to measure

The game keeps its analytics on the player's PC; there is no telemetry and there should not be one without consent. In
the playtests (and from Steam's own stats where they exist) note:

| Measure | Target for "one more run" |
|---|---|
| Time from the end of a run to the first choice of the next | under 30 s with Walk again |
| Length of a won Garden Run at H0 | under 45 min (else RM-113) |
| Sessions with two or more runs | more than half |
| Where runs end (floor) | spread over the floors, not a wall on one |
| What the player says after a loss | "I know what to do next time" |

## 8. As built (1.1, 2026-10-22)

| Proposal | In the game | Requirement |
|---|---|---|
| RM-110 Walk again | Every run end (the Garden Run scroll, the Endless Zen, Spirit Trials and Monthly Walk results) shows **Walk again** first and focused, Back to the garden gate under it. The next run starts at once with the same class, mode, Harmony and perk (and Spirit Trials tier); the Monthly Walk starts the month's garden again. Tutorials and custom puzzles keep Back only. | RUN-AGAIN-001 |
| RM-111 So close | Under "This run" a lost Garden Run names where it ended ("So close: the Floor 3 boss, 9 cells from the end") and the best floor with the class; lost trials and walks note the cells left. | RUN-END-NEAR-001 |
| RM-112 What this run moved | The same block lists up to three things the run moved: a newly opened Harmony, the class unlocks it brought closer (closest first), the Codex, or what opens the next Harmony. | RUN-END-MOVED-001 |
| RM-115 Wagers you can feel | The cursed tile shows the gold and XP in numbers ("Gold 34 → 51 (+50%)"); the boss gate says what the fight pays and what a struck trial costs in XP. | STAKES-001 |
| §6 Measure the run length | The game counts how long each run was open, the time to its first path choice and the gap since the previous run of the session, and keeps the last 200 runs in the save (on this PC only). `python tools/playtest/run_length_report.py` prints the §6 table and the verdict on RM-113 (a short walk only if a won Harmony 0 run takes over 45 minutes). The run-end banner names the run's length. | RUN-LENGTH-001 |

Not built: RM-113 (waits for the measures; the floor count stays five), RM-114 and the rest of §4.

**For the next playtests:** play two or three runs in one sitting, then run the report on the playtester's save (`AppData/LocalLow/Kratossos/sudoku_rouge_like/save_profile_*.json`, or pass the file sent in).

## 7. Open questions for the owner

1. Which of RM-110 to RM-121 go on the roadmap, and which first?
2. Is a three-floor Short walk (RM-113) right for this game, or should runs stay five floors and get faster puzzles
   (smaller boards on Floors 1 and 2)?
3. Keystones (RM-114): may a relic change Sudoku's rules (for example free pencil marks), or only the economy around them?
