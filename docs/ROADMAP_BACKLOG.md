# Roadmap backlog: Run of the Nine

**Version:** 1.18 | **Date:** 2026-11-04 | **Status:** review (short term set by the owner; mid and long term are suggestions waiting for the owner's pick)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.18 | 2026-11-04 | review | RM-101 started (owner 2026-11-04, "week and month"): the boards of the seed of the week and the Monthly Walk are filled (SEED-BOARD-001/002); the standing panel with the friends filter waits for the mockup pick. RM-104: proposals `docs/design/rm104-content-proposals.html` wait for the owner. |
| 1.17 | 2026-11-04 | review | RM-106 done (owner 2026-11-04): Text Size on every screen (ACCESS-FONT-003) and OpenDyslexic (ACCESS-FONT-004). |
| 1.16 | 2026-11-04 | review | Owner (2026-11-04): the sound work of RM-004 is done (GAP-073 round 2, AUDIO-SFX-001; the 8-bit soundtrack stays optional); RM-102 closed by the owner (the game's Deck work is built, a Deck start and the review are the owner's); RM-101, RM-104 and RM-106 are next. |
| 1.15 | 2026-11-03 | review | Rows brought to the code (owner's question 2026-11-03, "what is still open"): RM-001 (catalog B1 to B6, 71 player modifiers), RM-006, RM-105 (the Custom Puzzle share code), RM-122 (the Daily Walk) and RM-123 (the Incense Walk) are built; RM-004 keeps the sound work (GAP-073 round 2) and the optional 8-bit soundtrack; RM-106 keeps the text size for every screen and a dyslexia-friendly font. |
| 1.14 | 2026-11-03 | review | RM-118 and RM-116 built (owner's picks 2026-11-03: "behutsam öffnen"; layout A, the proposed list, 24 achievements). |
| 1.13 | 2026-11-03 | review | Owner (2026-11-03), on the proposals of OPEN_POINTS 2.0: RM-102 Deck work built (left: a Deck start and the review), RM-108 balance numbers recorded, RM-113 closed, RM-120 built, RM-118 and RM-116 picked (in that order). |
| 1.12 | 2026-11-02 | review | Owner (0.3.1 playtest): RM-225 built as pick A; RM-227 (XP animation, pick B), RM-228 (legend, pick A) and RM-229 (story achievements) built. |
| 1.11 | 2026-11-02 | review | Owner (2026-11-02): RM-224 built as pick A; RM-125 built (the story of the Nine); RM-226 irregular regions on every size; RM-225 conversation scenes as three mockups for the pick. |
| 1.10 | 2026-11-02 | review | RM-218 to RM-224 from the owner's 0.3.1 playtest (2026-11-02): a short-term block; RM-218 to RM-223 built, RM-224 (quit screen) as three mockups for the owner's pick. |
| 1.9 | 2026-10-28 | review | Mockups for RM-208, RM-209, RM-211 and RM-217 (layout-proposals 3.5, UI-STYLE-032 to 038), waiting for the owner's pick. |
| 1.8 | 2026-10-27 | review | RM-206 to RM-217 from the owner's 0.3.0 playtest (`docs/review/PLAYTEST_0.3.0.md`): a short-term block "0.3.0 playtest". |
| 1.7 | 2026-10-27 | review | Owner request (2026-10-27): the concept files are merged into their specs; every roadmap reference now points to the spec section (no roadmap status changed). |
| 1.6 | 2026-10-26 | review | Suggested RM-122 to RM-125 from the owner's requests of 2026-10-25 (concepts, for the owner's pick; merged into the specs in 1.7); 21 new achievements built (`docs/Achievements.md`). |
| 1.5 | 2026-10-22 | review | Owner picks: the Steam Deck next with Steam Input (RM-102; the Deck runs the Windows build, `docs/platform-ports-macos-linux.md` 1.1), and keystone relics that may bend Sudoku's rules with more relics, items and events (RM-114 with RM-104). |
| 1.4 | 2026-10-22 | review | Owner pick: the first slice of the one-more-run concept built (RM-110 Walk again, RM-111 So close, RM-112 What this run moved, RM-115 Wagers you can feel) and the run length measured for RM-113; five floors stay. |
| 1.3 | 2026-10-21 | review | RM-006 done (Select Class, Game Modes and the mode results in the new look; the main menu stays). Suggested RM-110 to RM-121 from the one-more-run concept (now GameDesignSpec §14), for the owner's pick. |
| 1.2 | 2026-10-21 | review | Owner picks after the 0.2.0 playtest built: RM-006 (Pencil mode in indigo, the floor entry; Options, Codex and Meta Progression being built), RM-108 (levers L1, L2, L5, L6; L3 counter items next), RM-109 (per-puzzle grades). |
| 1.1 | 2026-10-20 | review | Owner's 0.2.0 playtest: RM-005 (small wishes, built), RM-006 (menus in the new look, mockups), RM-107 (macOS and Linux/SteamOS plan), RM-108 (gold and items concept), RM-109 (mistake scoring proposals). |
| 1.0 | 2026-10-18 | review | First roadmap. Short term from the owner: more modifiers, more languages, seed sharing, visual and sound improvements. Mid and long term proposed for the owner's review. The public picture is `tools/discord/brand/roadmap.png` (made from `roadmap.html`). |

This is the feature backlog. Bugs go to `docs/review/GAME_GAPS.md`. An item here becomes work when it gets requirement IDs in the matching spec (then it is listed in `REQUIREMENT_MAP.md`). The public roadmap (Discord `#roadmap`, picture and text in `tools/discord/`) shows a short version of the same list. Rule for the public version: say what players get, never a date we cannot keep.

Horizons: **short term** = the next one to three updates, **mid term** = after that, until the full release, **long term** = after the full release.

## Short term (owner, 2026-10-18)

| ID | Item | What players get | Notes and dependencies | Status |
|---|---|---|---|---|
| RM-001 | More modifiers | New puzzle rules in batches from the 64 catalog rules (IDs 30–93): diagonals and extra regions, outside clues such as Sandwich and Little Killer, new cage and arrow types. | Plan: BossMechanicsSystem.md, "Modifier catalog". Each batch needs rules, generation with a unique solution, drawing, texts in every language, icons and codex entries. | done: catalog batches B1 to B6 live, 71 player modifiers (BossMechanicsSystem "Modifier catalog"); 22 IDs dropped by the owner |
| RM-002 | More languages | Next languages after English, German, French, Spanish and Brazilian Portuguese. Suggested order: Italian, Polish, Russian and Turkish (the current font covers them), then Japanese, Simplified Chinese and Korean (need a CJK font). | Community translators get the Translator role and are named in the credits (CREDITS-COMMUNITY-001). Check the font's glyph coverage before each language. | planned |
| RM-003 | Seed sharing | Share a run as a short code (seed, class, mode, Harmony). Paste it to play the same run; the end screen shows the code and a Copy button. The Seed of the week (RUN-WEEK-001) uses the same code. | Builds on the start-run seed field. A code format with a version number keeps old codes readable. | planned |
| RM-005 | 0.2.0 playtest wishes | The version on the main menu, a Random button for Custom Puzzle rules (with a count), the boss screen shows the whole floor's numbers, a placed digit is highlighted, digits are taken sooner after choosing a cell. | Built 2026-10-20 with the playtest bugs GAP-055..059 (`layout-proposals.md` 1.9). | done (next build) |
| RM-006 | Menus in the new look | Options, Codex and Meta Progression as scroll, collector's book and garden of classes; the garden painting back on the path; Pencil mode in its own colour. | Owner picks 2026-10-20 (`docs/design/layout-proposals.md` 2.0): Pencil mode in indigo ink (UI-STYLE-020) and the floor entry (UI-STYLE-021) are built; Options, Codex and Meta Progression (UI-STYLE-017 to 019) are being built. | done: Options, Codex, Meta Progression and every menu in the new look (UI-STYLE-017 to 043) |
| RM-004 | Visual and sound improvements | The whole run in the new ink-and-paper look: garden path as a hand scroll, puzzle on a hanging scroll, then reward, relic and shop screens. A full 8-bit soundtrack and new sound effects. | Boss gate and boss reward are done (UI-STYLE-004/005). Path and puzzle: UI-STYLE-001/002. Music: `steam_assets/audio_8bit/synth`, waiting for the owner's listening. | done: the look (UI-STYLE-*) and the sound effects (GAP-073, AUDIO-UI-001, AUDIO-SFX-001); the 8-bit soundtrack stays optional |

## Short term: 0.3.0 playtest (owner, 2026-10-27)

| ID | Item | What players get | Notes and dependencies | Status |
|---|---|---|---|---|
| RM-206 | Puzzle pad layout | LB opens the bag, LT the relics, RB switches Solve and Pencil, Start opens the Options, a tap on Select saves and quits, L3 highlights the digit; the right stick moves the number pad cursor. | Owner, 0.3.0 playtest (docs/review/PLAYTEST_0.3.0.md); built with GAP-077..083 (InputAndProfilesSpec 2.5, INPUT-PAD-004); a real pad still to check. | built |
| RM-207 | Key hints follow the device | The hints on every screen show pad buttons after a pad press and keys after a key or mouse press. | Owner, 0.3.0 playtest; built for ten hints (INPUT-HINT-001, InputAndProfilesSpec 2.6). | built |
| RM-208 | Puzzle screen details | Gold shown on the puzzle screen, item and relic explanations readable there, no click sound on the board, a hint for the highlight key. | Owner, 0.3.0 playtest; built as UI-STYLE-036 (HUD-GOLD-001, HUD-DETAIL-001); the click sound is gone and the highlight key is in the hint. | built |
| RM-209 | Custom Puzzle: help, seed, save | What the stars and every Board field mean; a seed field and a share code; save and quit a Custom Puzzle without touching the Garden Run save. | Owner, 0.3.0 playtest; help, seed, share code and the save built (UI-STYLE-037, CUSTOM-SAVE-001, TutorialModeSystem 1.14); Continue on the main menu comes with main menu B. | built |
| RM-210 | Drag the option rows | Every value row in the Options can be dragged with the mouse as well as stepped with the arrows. | Owner, 0.3.0 playtest; built (OPT-DRAG-001, GameDesignSpec 1.26). | built |
| RM-211 | Reroll the floor's rule in the shop | From Floor 2 the shop offers to roll the floor's rule again for gold. | Owner, 0.3.0 playtest; built as UI-STYLE-038 with the owner's 100 gold, once per floor (SHOP-RULE-001, GoldEconomySystem). | built |
| RM-212 | Golden Koi line on the reward | The reward tally lists the Golden Koi's extra gold on its own line. | Owner, 0.3.0 playtest; built (REWARD-KOI-001, GoldEconomySystem 2.4). | built |
| RM-213 | Game Modes without the Garden Run | Start Game starts the Garden Run; Game Modes shows the other modes; Harmony is chosen on Select Class. | Owner, 0.3.0 playtest ("Ja, nur Select Class"); built as UI-STYLE-031 (layout-proposals 3.4). | built |
| RM-214 | Bigger clue numbers | Text Size also enlarges the small numbers of rules such as Opposite Sums (for handhelds like the Legion Go). | Owner, 0.3.0 playtest on the Legion Go 2; built (ACCESS-FONT-002, AccessibilitySpec 2.5). | built |
| RM-215 | Title sound | A short sound when the title screen opens. | Owner, 0.3.0 playtest; built: a code-made 8-bit sting, no paid service (AUDIO-TITLE-001, AudioVisualDirection 1.7). | built |
| RM-216 | Daily Walk paths and themes | Concept: the Daily Walk on one path with its own garden themes; the main game on Floors 1, 3 and 5, the Daily Walk on Floors 2 and 4. | Owner, 0.3.0 playtest; way B and "Read an event" built (DAILY-WALK-009, DailyGoalSystem 0.9); way A waits for the run-length report. | built |
| RM-217 | Mockups for four screens | Three main menu proposals, the Monthly Walk, Select Profile and Spirit Trials. | Owner, 0.3.0 playtest; built: main menu B (UI-STYLE-032), the Monthly Walk (033), Select Profile (034), Spirit Trials (035). | built |

## Short term: 0.3.1 playtest (owner, 2026-11-02)

| ID | Item | What | Source | Status |
|----|------|------|--------|--------|
| RM-218 | Event tiles in the Garden Run | Event tiles roll on both routes again. | Owner ("Ereignisfelder im Gartenlauf: ja"); built (MAP-NODE-EVENT-001, PathSystem_GardenOverview 1.10). | built |
| RM-219 | Daily Walk shorter, saved only for its day | Two tiles fewer per floor; Save & Quit keeps the walk until midnight and the player is told. | Owner; built (DAILY-WALK-010, DailyGoalSystem 0.10). | built |
| RM-220 | Custom Puzzle finish screen | A solved puzzle shows its time, mistakes, pencil marks, cells and grade. | Owner; built in the mode-result family (CUSTOM-FINISH-001, TutorialModeSystem 1.15). | built |
| RM-221 | Regions per board size | Regions a size does not have are greyed; 9×9 irregular checked. | Owner; built (CUSTOM-REGION-001). | built |
| RM-222 | Enter clears pencil marks | Enter on an empty cell with pencil marks clears them. | Owner; built (INPUT-KEY-001, InputAndProfilesSpec 2.7). | built |
| RM-223 | Brush cursor | The mouse pointer as a Japanese brush. | Owner; built (UI-CURSOR-001, GameDesignSpec). | built |
| RM-224 | Quit screen | A goodbye screen with Kratossos that makes the player want to come back. | Owner's pick A, the garden at dusk; built (UI-STYLE-039). | built |
| RM-225 | Conversation scenes for the story | Dedicated scenes where keeper and warden talk. | Owner's pick A, the painted dialogue, one scene per chapter at the first floor's entry, again in the Codex; built (STORY-SCENE-001, UI-STYLE-040). | built |
| RM-226 | Irregular regions on every size | 6×6, 8×8 and 9×9 irregular puzzles in both templates, like 5×5 and 7×7. | Owner (2026-11-02); built (CUSTOM-REGION-002). | built |
| RM-227 | XP animation | XP is painted into the bar so it feels added; skippable. | Owner's pick B, the brush stroke; built (UI-STYLE-041). | built |
| RM-228 | The legend's place and layout | The legend opened at a different place each time. | Owner's pick A, the legend in the rule slip; built (UI-STYLE-042). | built |
| RM-229 | Story achievements | New Steam achievements for the story. | Owner ("Zehn"); built (ACHIEVE-STORY-001); they need entering in Steamworks. | built |

## Mid term (suggested)

| ID | Item | What players get | Why | Effort |
|---|---|---|---|---|
| RM-101 | Seed leaderboards | A leaderboard for every Seed of the week and Monthly Walk, with a friends filter. | Makes seed sharing (RM-003) competitive; Steam leaderboards already exist in the game (`SteamLeaderboardService`). | in progress: the boards of the seed of the week and the Monthly Walk are filled (SEED-BOARD-001/002); the standing panel with the friends filter waits for the mockup pick |
| RM-102 | Steam Deck Verified | Readable at 1280×800, the on-screen keyboard for the seed field, Deck button glyphs. | Owner pick 2026-10-22: next, with Steam Input; the Deck runs the Windows build through Proton, no build of its own (`docs/platform-ports-macos-linux.md` 1.1). Built 2026-11-03: text size, the gamepad keyboard, L1/R1 names (DECK-TEXT-001, DECK-KEYBOARD-001, DECK-BUTTONS-001); left: a start on a Deck and the compatibility review. | closed by the owner (2026-11-04): the Deck work is built (DECK-TEXT-001, DECK-KEYBOARD-001, DECK-BUTTONS-001); a Deck start and the review are the owner's |
| RM-103 | A signature rule per boss | Each of the five bosses brings its own rule on top of the chosen trials (for example, the Koi Warden moves one given digit). | Bosses feel different, not only harder. | L |
| RM-104 | More relics, items and events | A content pack: new relics with build-defining effects, new events with choices that matter. | Owner pick 2026-10-22: together with the keystone relics (RM-114). | in progress: the proposals (6 relics, 4 items, 4 events) wait for the owner's ticks (`docs/design/rm104-content-proposals.html`) |
| RM-105 | Custom puzzle codes | Share a Custom Puzzle as a code; a "community puzzle of the week" on Discord. | Uses the same code idea as RM-003; gives the community something to make. | done: the Custom Puzzle share code (CUSTOM-CODE-001, RM-209); Workshop sharing stays RM-203 |
| RM-107 | macOS and Linux / SteamOS | The game on Mac (Intel and Apple Silicon) and Linux, Steam Deck included, with one cloud save on every system. | Plan: `docs/platform-ports-macos-linux.md`. First step: controller input that is the same on every OS (also RM-102). | M |
| RM-108 | Gold and items that matter | Fewer but meaningful purchases: prices against income, gold to shape the boss, items that answer the floor's rules, leftover gold in the score. | Concept, now GoldEconomySystem.md "Why and the levers"; the owner picks the levers. | L1, L2, L3, L5, L6 built (GoldEconomySystem 2.2, ItemsAndRelicsSystem 2.3); balance check recorded in every run since 2026-11-03 (RUN-BALANCE-001), read at the next playtest |
| RM-109 | Mistakes that count fairly | Every mistake counts (done, GAP-059); how much depends on when it happens, for example a grade per puzzle on the end screen. | Proposals, now GameDesignSpec.md §9c. | built: per-puzzle grades (GameDesignSpec 1.14 §9c) |
| RM-106 | More accessibility | Colour-blind safe colours for every overlay, a text size setting for all screens, a dyslexia-friendly font option. | Sudoku players skew older; accessibility widens the audience. | done: colour-blind line colours (GAP-061), Text Size on the board and the clues (ACCESS-FONT-001/002) and on every screen (ACCESS-FONT-003), OpenDyslexic (ACCESS-FONT-004), Reduce Motion |
| RM-110, RM-111, RM-112, RM-115 | One more run, first slice | Built 2026-10-22: Walk again, So close, What this run moved, the stakes in numbers; the run length is measured in the game (RM-113 decided from it). | GameDesignSpec.md §14 (End Screens) | done |
| RM-114 | Keystone relics | After the first boss, one of three keystones that shape the run; a keystone may bend Sudoku's own rules. | Built 2026-10-26 with the class signature relics (RM-124): Gilded Box, Open Margin, Steady Hand, Cage Whisper (ItemsAndRelicsSystem 2.4). | done |
| RM-113, RM-116 to RM-121 | One more run | A short walk, class challenges, a daily garden, early unlocks, pacts, a faster first choice, a rival run. | Roguelike pull after each run; details and effort in GameDesignSpec.md §14. Owner 2026-11-03: RM-113 closed (the Daily Walk is the short run); RM-120 (RUN-FIRST-001), RM-118 (RUN-EARLY-001/002) and RM-116 (CLASS-CHALLENGE-001/002) built; RM-119 and RM-121 later. | S to L each |
| RM-122 | A quick run every day | The Daily Walk as one floor of its own (three puzzles and a boss, about 15 minutes), the same seed for everyone, paid in class XP with a streak bonus instead of gold. | Spec: DailyGoalSystem.md, "The Daily Walk as a run". | done: the Daily Walk as a two-floor run (DAILY-WALK-001 to 010) |
| RM-123 | The Incense Walk (time attack) | A clock instead of HP: start with 8 minutes, each solved puzzle adds time, mistakes cost time, over at 0:00. | Spec: IncenseWalkMode.md. | done: the Incense Walk (IncenseWalkMode.md, INCENSE-*) |
| RM-124 | Class relic sets | A signature relic per class from Level 1 next to the existing Level 30 exclusive. | Built 2026-10-26 (ItemsAndRelicsSystem 2.4; the concept is merged in 2.5). | done |
| RM-125 | The story of the Nine | The eight classes as the keepers who stayed, five wardens, chapters told on the floor entry, the boss gate and the run end, a Tales page in the Codex. | Owner's answers 2026-11-02: built (STORY-CHAPTER-001 to STORY-TALES-001, narrative-direction.md 1.2); conversation scenes are RM-225. | built |

## Long term (suggested)

| ID | Item | What players get | Why | Effort |
|---|---|---|---|---|
| RM-201 | Full release (1.0) | A complete balance pass, all screens in the new look, all languages complete. | The milestone the mid term leads to. | — |
| RM-202 | A second garden | Five new floors with new region shapes, new bosses and new paintings, as a second act after the Shrine Summit. | The largest content step; reuses every system. | XL |
| RM-203 | Steam Workshop | Players share custom puzzles and modifier sets through the Workshop. | Endless content without our time; needs RM-105 first. | L |
| RM-204 | Race mode | Two players race on the same seed; first as "ghosts" (play against a friend's recorded run), later live. | Seed sharing (RM-003) turned into a social mode. | XL |
| RM-205 | More platforms | After Steam Deck: other platforms once the PC version is stable (touch controls would be needed for mobile). | Reach; decide per platform with sales data. | XL |

Effort: S = days, M = one to two weeks, L = a month, XL = several months.

## Open decisions for the owner

1. Which mid- and long-term items go on the public roadmap (and in which order)?
2. RM-002: which languages, and through community translators or a paid service?
3. RM-201: is there a target window for 1.0 that can be named publicly?
