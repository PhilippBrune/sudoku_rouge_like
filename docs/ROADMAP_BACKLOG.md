# Roadmap backlog: Run of the Nine

**Version:** 1.7 | **Date:** 2026-10-27 | **Status:** review (short term set by the owner; mid and long term are suggestions waiting for the owner's pick)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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
| RM-001 | More modifiers | New puzzle rules in batches from the 64 catalog rules (IDs 30–93): diagonals and extra regions, outside clues such as Sandwich and Little Killer, new cage and arrow types. | Plan: BossMechanicsSystem.md, "Modifier catalog". Each batch needs rules, generation with a unique solution, drawing, texts in every language, icons and codex entries. | planned |
| RM-002 | More languages | Next languages after English, German, French, Spanish and Brazilian Portuguese. Suggested order: Italian, Polish, Russian and Turkish (the current font covers them), then Japanese, Simplified Chinese and Korean (need a CJK font). | Community translators get the Translator role and are named in the credits (CREDITS-COMMUNITY-001). Check the font's glyph coverage before each language. | planned |
| RM-003 | Seed sharing | Share a run as a short code (seed, class, mode, Harmony). Paste it to play the same run; the end screen shows the code and a Copy button. The Seed of the week (RUN-WEEK-001) uses the same code. | Builds on the start-run seed field. A code format with a version number keeps old codes readable. | planned |
| RM-005 | 0.2.0 playtest wishes | The version on the main menu, a Random button for Custom Puzzle rules (with a count), the boss screen shows the whole floor's numbers, a placed digit is highlighted, digits are taken sooner after choosing a cell. | Built 2026-10-20 with the playtest bugs GAP-055..059 (`layout-proposals.md` 1.9). | done (next build) |
| RM-006 | Menus in the new look | Options, Codex and Meta Progression as scroll, collector's book and garden of classes; the garden painting back on the path; Pencil mode in its own colour. | Owner picks 2026-10-20 (`docs/design/layout-proposals.md` 2.0): Pencil mode in indigo ink (UI-STYLE-020) and the floor entry (UI-STYLE-021) are built; Options, Codex and Meta Progression (UI-STYLE-017 to 019) are being built. | in progress |
| RM-004 | Visual and sound improvements | The whole run in the new ink-and-paper look: garden path as a hand scroll, puzzle on a hanging scroll, then reward, relic and shop screens. A full 8-bit soundtrack and new sound effects. | Boss gate and boss reward are done (UI-STYLE-004/005). Path and puzzle: UI-STYLE-001/002. Music: `steam_assets/audio_8bit/synth`, waiting for the owner's listening. | in progress |

## Mid term (suggested)

| ID | Item | What players get | Why | Effort |
|---|---|---|---|---|
| RM-101 | Seed leaderboards | A leaderboard for every Seed of the week and Monthly Walk, with a friends filter. | Makes seed sharing (RM-003) competitive; Steam leaderboards already exist in the game (`SteamLeaderboardService`). | M |
| RM-102 | Steam Deck Verified | Readable at 1280×800, the on-screen keyboard for the seed field, Deck button glyphs. | Owner pick 2026-10-22: next, with Steam Input; the Deck runs the Windows build through Proton, no build of its own (`docs/platform-ports-macos-linux.md` 1.1). | M |
| RM-103 | A signature rule per boss | Each of the five bosses brings its own rule on top of the chosen trials (for example, the Koi Warden moves one given digit). | Bosses feel different, not only harder. | L |
| RM-104 | More relics, items and events | A content pack: new relics with build-defining effects, new events with choices that matter. | Owner pick 2026-10-22: together with the keystone relics (RM-114). | M |
| RM-105 | Custom puzzle codes | Share a Custom Puzzle as a code; a "community puzzle of the week" on Discord. | Uses the same code idea as RM-003; gives the community something to make. | M |
| RM-107 | macOS and Linux / SteamOS | The game on Mac (Intel and Apple Silicon) and Linux, Steam Deck included, with one cloud save on every system. | Plan: `docs/platform-ports-macos-linux.md`. First step: controller input that is the same on every OS (also RM-102). | M |
| RM-108 | Gold and items that matter | Fewer but meaningful purchases: prices against income, gold to shape the boss, items that answer the floor's rules, leftover gold in the score. | Concept, now GoldEconomySystem.md "Why and the levers"; the owner picks the levers. | L1, L2, L3, L5, L6 built (GoldEconomySystem 2.2, ItemsAndRelicsSystem 2.3); balance check at the next playtest |
| RM-109 | Mistakes that count fairly | Every mistake counts (done, GAP-059); how much depends on when it happens, for example a grade per puzzle on the end screen. | Proposals, now GameDesignSpec.md §9c. | built: per-puzzle grades (GameDesignSpec 1.14 §9c) |
| RM-106 | More accessibility | Colour-blind safe colours for every overlay, a text size setting for all screens, a dyslexia-friendly font option. | Sudoku players skew older; accessibility widens the audience. | M |
| RM-110, RM-111, RM-112, RM-115 | One more run, first slice | Built 2026-10-22: Walk again, So close, What this run moved, the stakes in numbers; the run length is measured in the game (RM-113 decided from it). | GameDesignSpec.md §14 (End Screens) | done |
| RM-114 | Keystone relics | After the first boss, one of three keystones that shape the run; a keystone may bend Sudoku's own rules. | Built 2026-10-26 with the class signature relics (RM-124): Gilded Box, Open Margin, Steady Hand, Cage Whisper (ItemsAndRelicsSystem 2.4). | done |
| RM-113, RM-116 to RM-121 | One more run | A short walk, class challenges, a daily garden, early unlocks, pacts, a faster first choice, a rival run. | Roguelike pull after each run; details and effort in GameDesignSpec.md §14. RM-113 waits for the run-length report of the 0.2.2 playtest. | S to L each |
| RM-122 | A quick run every day | The Daily Walk as one floor of its own (three puzzles and a boss, about 15 minutes), the same seed for everyone, paid in class XP with a streak bonus instead of gold. | Spec: DailyGoalSystem.md, "The Daily Walk as a run". | M |
| RM-123 | The Incense Walk (time attack) | A clock instead of HP: start with 8 minutes, each solved puzzle adds time, mistakes cost time, over at 0:00. | Spec: IncenseWalkMode.md. | M–L |
| RM-124 | Class relic sets | A signature relic per class from Level 1 next to the existing Level 30 exclusive. | Built 2026-10-26 (ItemsAndRelicsSystem 2.4; the concept is merged in 2.5). | done |
| RM-125 | The story of the Nine | The eight classes as the keepers who stayed, five wardens, chapters told on the floor entry, the boss gate and the run end, a Tales page in the Codex. | Proposal: narrative-direction.md. | M |

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
