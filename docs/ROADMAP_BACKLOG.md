# Roadmap backlog: Run of the Nine

**Version:** 1.0 | **Date:** 2026-10-18 | **Status:** review (short term set by the owner; mid and long term are suggestions waiting for the owner's pick)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-18 | review | First roadmap. Short term from the owner: more modifiers, more languages, seed sharing, visual and sound improvements. Mid and long term proposed for the owner's review. The public picture is `tools/discord/brand/roadmap.png` (made from `roadmap.html`). |

This is the feature backlog. Bugs go to `docs/review/GAME_GAPS.md`. An item here becomes work when it gets requirement IDs in the matching spec (then it is listed in `REQUIREMENT_MAP.md`). The public roadmap (Discord `#roadmap`, picture and text in `tools/discord/`) shows a short version of the same list. Rule for the public version: say what players get, never a date we cannot keep.

Horizons: **short term** = the next one to three updates, **mid term** = after that, until the full release, **long term** = after the full release.

## Short term (owner, 2026-10-18)

| ID | Item | What players get | Notes and dependencies | Status |
|---|---|---|---|---|
| RM-001 | More modifiers | New puzzle rules in batches from the 64 catalog rules (IDs 30–93): diagonals and extra regions, outside clues such as Sandwich and Little Killer, new cage and arrow types. | Plan: `docs/design/modifier-catalog-plan.md`. Each batch needs rules, generation with a unique solution, drawing, texts in every language, icons and codex entries. | planned |
| RM-002 | More languages | Next languages after English, German, French, Spanish and Brazilian Portuguese. Suggested order: Italian, Polish, Russian and Turkish (the current font covers them), then Japanese, Simplified Chinese and Korean (need a CJK font). | Community translators get the Translator role and are named in the credits (CREDITS-COMMUNITY-001). Check the font's glyph coverage before each language. | planned |
| RM-003 | Seed sharing | Share a run as a short code (seed, class, mode, Harmony). Paste it to play the same run; the end screen shows the code and a Copy button. The Seed of the week (RUN-WEEK-001) uses the same code. | Builds on the start-run seed field. A code format with a version number keeps old codes readable. | planned |
| RM-004 | Visual and sound improvements | The whole run in the new ink-and-paper look: garden path as a hand scroll, puzzle on a hanging scroll, then reward, relic and shop screens. A full 8-bit soundtrack and new sound effects. | Boss gate and boss reward are done (UI-STYLE-004/005). Path and puzzle: UI-STYLE-001/002. Music: `steam_assets/audio_8bit/synth`, waiting for the owner's listening. | in progress |

## Mid term (suggested)

| ID | Item | What players get | Why | Effort |
|---|---|---|---|---|
| RM-101 | Seed leaderboards | A leaderboard for every Seed of the week and Monthly Walk, with a friends filter. | Makes seed sharing (RM-003) competitive; Steam leaderboards already exist in the game (`SteamLeaderboardService`). | M |
| RM-102 | Steam Deck Verified | Readable at 1280×800, the on-screen keyboard for the seed field, Deck button glyphs. | The game is built controller-first, so most of the work is done; the Deck badge helps sales. | M |
| RM-103 | A signature rule per boss | Each of the five bosses brings its own rule on top of the chosen trials (for example, the Koi Warden moves one given digit). | Bosses feel different, not only harder. | L |
| RM-104 | More relics, items and events | A content pack: new relics with build-defining effects, new events with choices that matter. | Run variety is what brings players back to a roguelike. | M |
| RM-105 | Custom puzzle codes | Share a Custom Puzzle as a code; a "community puzzle of the week" on Discord. | Uses the same code idea as RM-003; gives the community something to make. | M |
| RM-106 | More accessibility | Colour-blind safe colours for every overlay, a text size setting for all screens, a dyslexia-friendly font option. | Sudoku players skew older; accessibility widens the audience. | M |

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
