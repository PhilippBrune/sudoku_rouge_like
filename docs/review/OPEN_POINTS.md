# Open points

**Version:** 2.0 | **Date:** 2026-11-03 | **Status:** review (a living list; update it whenever an item closes or a new one opens)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.0 | 2026-11-03 | review | Owner (2026-11-03): "Which points are still open? Make proposals to fix them." Every point checked against the code, the roadmap and the gaps: what closed since 1.9 is listed at the end; every open point has a proposal. The 0.2.0 playtest list is replaced by what to check in 0.4.0. |
| 1.9 | 2026-10-27 | review | Owner request (2026-10-27): the concept files are merged into their specs; the references of the open points point to the spec sections. No open point changed. |
| 1.8 | 2026-10-22 | review | The owner signed off SPEC-CONF-008 and SPEC-PROC-007, chose Steam Input with the Steam Deck next (the Deck runs the Windows build), asked for a playtest build on the password beta branch with the content survey, and picked keystone relics that may bend Sudoku's rules (RM-114 with RM-104). |
| 1.7 | 2026-10-22 | review | The owner closed GAP-030, GAP-057 and GAP-062: no gap is open. |
| 1.6 | 2026-10-22 | review | GAP-062, GAP-064, GAP-067, GAP-068 fixed, GAP-069 found and fixed; the one-more-run slice and the run-length measures built; the Codex rarity filter, "Best paired with" and the Trials tiers built. Open gaps: GAP-030 (listening), GAP-057 (not reproduced). |
| 1.5 | 2026-10-21 | review | Select Class, Game Modes and the three mode results built (UI-STYLE-024 to 026); the main menu stays (owner); GAP-065 and GAP-066 fixed; GAP-064 planned with three rules for the owner, GAP-067 logged; the one-more-run concept and the legal check wait for the owner; 0.2.1 built. |
| 1.4 | 2026-10-21 | review | Input Lock option and the washi window built; proposals for the main menu, Select Class and Game Modes wait for the owner's pick; GAP-064 and GAP-065 logged. |
| 1.3 | 2026-10-21 | review | SPEC-CONF-041 decided and GAP-063 fixed on the owner's request; the class rewards moved to the playtest table. |
| 1.2 | 2026-10-21 | review | Garden of classes built (UI-STYLE-019); SPEC-CONF-041 and GAP-063 wait for the owner. |
| 1.1 | 2026-10-21 | review | Options scroll and Codex book built (UI-STYLE-017/018): moved to the playtest table; the Codex's left-out parts listed. |
| 1.0 | 2026-10-21 | review | First list, at the owner's request after the 0.2.0 playtest: what to check at the next playtest (GAP-057 included), what is being built, what waits for the owner, and known technical leftovers. |

This list collects what is still open across the project. Bugs keep their details in [GAME_GAPS](GAME_GAPS.md), features in
[ROADMAP_BACKLOG](../ROADMAP_BACKLOG.md), specification questions in [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md); this
page points to them. Since 2.0 every open point has a **proposal**: what would close it, and who does it.

## 1. To check at the next playtest (0.4.0)

The 0.2.0 list of 1.9 was played in the 0.3.0 and 0.3.1 playtests; their findings became GAP-074 to GAP-113 and are
fixed except GAP-108. These are new in 0.4.0 or could only be checked by hand:

| What | Where to look | Proposal |
|---|---|---|
| The pointer hides while the pad is used (INPUT-PAD-006) and the hints do not flicker (INPUT-HINT-002) | Any screen with a real pad through Steam | Owner's playtest; batch mode cannot move a real pointer. Say if the pointer comes back too early or too late. |
| Story scenes, the XP brush, the legend in the rule slip (UI-STYLE-040 to 042) | First floor of a run with a new chapter; end of a run; any puzzle with rules | Owner's playtest. Say if a scene is too long or the brush too slow (2.4 s). |
| A Spirit Trial's starting item (TRIAL-SESSION-001, GAP-113) | Game Modes → Spirit Trials, any tier | Owner's playtest: one item in the bag, Normal or Rare by tier. |
| Story achievements (ACHIEVE-STORY-001) | Open a second chapter | They unlock only after the owner publishes the achievements in Steamworks (section 3). |

## 2. Open, with a proposal

| Point | State | Proposal | Who |
|---|---|---|---|
| **GAP-108** The D-pad placed digits and the right stick moved the pointer | Open. The game reads the D-pad as moves only; the pattern matches a Steam Input template with keys and a mouse stick | Steamworks → Steam Input: set the default configuration to the "Gamepad" template (no keyboard keys, no mouse stick). I can set it in the Steamworks window and stop before Publish with screenshots. Then a test with Steam Input on and off. | Owner's OK, then me |
| **RM-113** run length | Not enough data: this PC has 3 runs recorded, none a won Harmony 0 Garden Run (`run_length_report.py`, 2026-11-03) | Close RM-113 without a fourth mode: the Daily Walk already is the short run (two floors, RM-117). If the report later shows won H0 runs over 45 min, open it again. | Owner decides |
| **RM-108** balance check (gold left, purchases, items used) | Open since 0.2.0; nobody noted the numbers by hand | Write them into the run history the game already keeps (RUN-LENGTH-001) and let `run_length_report.py` print them: no manual notes, and testers' saves give the numbers too. About half a day. | Me, on a yes |
| **GAP-073** 25 of 35 sound effects never play | Open | A listening page with the 25 clips (HTML, play buttons); the owner ticks the wanted ones; I call each from its event (shop reroll, item use, reward, rest, relic, boss gate, level up, gold gain first). | Owner listens, then me |
| **Steam Deck** (RM-102): text size, on-screen keyboard, Deck button names | Open; the Deck runs the Windows build | (1) A diagnostic that renders every main screen at 1280×800 and lists text under 13 px, then fix those; (2) the seed fields open Steam's floating keyboard on a Deck (`SteamUtils.IsSteamRunningOnSteamDeck`, `ShowFloatingGamepadTextInput`); (3) the Deck uses Xbox letters, so only View/Menu need names. About two days. The compatibility review stays in Steamworks (owner). | Me; review: owner |
| **RM-104** more relics, items and events | Open (RM-114 keystones are built) | A short concept with 6 relics, 4 items and 4 events, each tied to a rule family, for the owner's pick, like the modifier catalog. | Me (concept), owner picks |
| **RM-116, RM-118 to RM-121** one more run | Open for the owner's pick | My order: RM-118 (something new in each of the first ten runs; helps new players most), then RM-116 (class challenges, three per class with an achievement), then RM-120 (first board ready behind the floor entry). RM-119 pacts and RM-121 rival run later (RM-121 needs seed sharing). | Owner picks |
| **Spirit Trials "Solver Start"** | The 0.1 spec let the player choose a random item or a Rare Solver; 0.4.0 has the random one only (GAP-113) | Leave it out unless the owner wants the choice; it would be one toggle on the tier slip. | Owner decides |
| **Modifier catalog, Phase 0 step 3** | Open: name and abbreviation switches still stand | One `ModifierInfo` entry per ID (name key, abbreviation, overlay group, legend colour, Custom Puzzle list) in place of the switch statements, with the contract test BOSS-MOD-005. A refactor of about a day with no player-visible change; best before RM-104 adds rules. | Me, on a yes |
| **Modifier catalog, Phase 0 step 5** | Open: golden geometry snapshots | EditMode snapshots of the geometry helpers (seed → overlay as JSON) for each rule family, so a generator change that moves lines or cages shows up in a test. Half a day. | Me, on a yes |
| **Pattern Scroll list** | Open: internal names ("GermanWhispers #1"), English only, no cell markers or outside clues | Name entries with `BossService.GetModifierName` and one localized pattern ("{0} #{1}, {2} cells"), add markers and outside clues; five catalogs. Small. | Me, on a yes |
| **`MainMenuClassInfoPresenter`** | No caller since the confirmation window went; only its tests use it | Move its two unlock-text tests to `ClassUnlockText` and delete the class. Small. | Me, on a yes |

## 3. Waiting for the owner

| Decision | Why it matters | Proposal |
|---|---|---|
| **Upload 0.4.0** | The story, the scenes, the story achievements and the fixes since 0.3.0 are local (`steam_assets/ReleaseNotes_v0.4.0.md`) | First publish the achievements (39, five languages), then upload and set live, then the announcement. After the upload: `walk_posts.py walk-live`, `discord_admin.py replace how-to-play`, `discord_admin.py apply` (channel topic). |
| **Legal check** (content survey, DSA trader status, trademark, Impressum, tax) | Store and business duties (`docs/legal/legal-check-2026-10.md`) | I fill the content survey in Steamworks (AI content: art, text, music; simulated gambling) and stop before Save with screenshots; I run a trademark search for "Run of the Nine" (DPMA, EUIPO, USPTO) and report. Trader status, Impressum and tax stay with the owner (personal data, adviser). |
| **Apple Developer account** (99 USD a year) | Without it macOS warns about or blocks the game | Postpone the Mac until the Deck is Verified; the Deck and Linux run the Windows build through Proton. |
| **A Steam Deck for a first start** | A system the game never ran on should not be promised | Borrow or buy one before the Deck review; until then Steam's "Unknown" rating stays. |
| **The 8-bit soundtrack set** | Optional; the 6-second loops stay otherwise | The same listening page as GAP-073 with the eight loops; keep or drop in one go. |

## 4. Known technical leftovers

- `Game.csproj` and the test `.csproj` files are regenerated by Unity on the owner's machine and stay uncommitted on
  purpose; `Assets/Neuer Ordner.meta` and `Assets/Resources/audio/music_new.meta` are the owner's files and are left alone.

## 5. Closed since 1.9

- **Upload of 0.2.1 and the build after it:** 0.3.0 is live since 2026-10-27 (Steam announcement "Update v0.3.0").
- **Playtest build 0.2.2 on the beta branch:** dropped by the owner; 0.3.0 carried it.
- **Keystone relics (RM-114):** built 2026-10-26 (RELIC-KEY-001/002).
- **Codex filter and "Best paired with":** built in 1.6.
- **The old Daily Walk panel:** removed (GAP-110, DAILY-GOAL-004, 2026-11-03).
- **The trial's starting item:** built (TRIAL-SESSION-001, GAP-113, 2026-11-03).
- **The ten story achievements in Steamworks:** created with icons and five languages, saved (2026-11-03); publishing is in section 3.
- The stray line "move twice; to fix with the controller work …" of 1.9 belonged to GAP-062, closed on 2026-10-22.
