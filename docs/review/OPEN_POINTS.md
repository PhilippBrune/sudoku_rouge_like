# Open points

**Version:** 2.4 | **Date:** 2026-11-04 | **Status:** review (a living list; update it whenever an item closes or a new one opens)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.4 | 2026-11-04 | review | Owner's 0.4.1 playtest on a Legion Go (2026-11-04): four bugs fixed in 0.4.2 (GAP-115 to GAP-118); six decisions wait on `docs/design/playtest-041-proposals.html` (the path's controller focus, the bag-full screen, three floors, the fog items and the item review, the Garden Monk, every class passive). 0.4.2 built and tagged, not uploaded. |
| 2.3 | 2026-11-04 | review | Owner (2026-11-04): GAP-073 round 2 picked and built (AUDIO-SFX-001, GAP-114); the Steam Deck and the legal check closed ("Schließe"); the 0.4.1 announcement published by the owner, mirrored to #rotn-news, how-to-play, known-issues and the channel topic posted; RM-101, RM-104 and RM-106 are next. |
| 2.2 | 2026-11-04 | review | 0.4.0 is live (build 26400103); the upload row becomes the announcement and the Discord posts. |
| 2.1 | 2026-11-03 | review | The owner's answers to 2.0 (2026-11-03) and what was done the same day: GAP-108 checked in Steamworks, RM-113 closed, RM-108 recorded, the Deck work, RM-120, catalog steps 3 and 5, the Pattern Scroll, the class presenter removed, the content survey draft and the trademark look. Open: the listening page, RM-118 and RM-116 picks, a Deck start. |
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

**0.4.1 playtest on a Legion Go (owner, 2026-11-04):** pick per section on `docs/design/playtest-041-proposals.html`
(mockups in `docs/design/layout-proposals/path_focus_*.png` and `bag_swap_*.png`), copy the choice into the chat:

| Point | Proposal | Owner |
|---|---|---|
| The path's controller focus is hard to see | A brush ring + class marker (recommended), B lantern light, C gold ring + slip | Owner picks |
| The bag-full screen is outdated | A ema plaques (recommended), B two cards, C list on a scroll | Owner picks |
| The main game on Floors 1, 3 and 5 | T1 three dense gardens (recommended), T2 short start, T3 floors with two halves; story, achievements, scores and saves follow | Owner picks (spec change) |
| Fog items useless; item review | F1 one lantern that always helps (recommended), F2 only with fog, F3 into the shop | Owner picks |
| The Garden Monk heals too much | M1 combo of 10 heals 1, max 2 (recommended), M2 perfect puzzle heals 2, M3 first mistake free, M4 weaker as today | Owner picks |
| Every class passive | Number Freak, Shrine Archivist, Lantern Seer, Reed Duelist, Quiet Cartographer get proposals; Koi Gambler and Stone Gardener stay | Owner ticks |
| Upload 0.4.2 | `Builds/RunOfTheNine_0.4.2_win64.zip`, steps and patch notes in `steam_assets/ReleaseNotes_v0.4.2.md` | Owner |

The owner's answers of 2026-11-03 ("ok", "schließen", "ja machen", "genau so implementieren", "machen", "okay") and what
came of them:

| Point | State | Next | Who |
|---|---|---|---|
| **GAP-108** The D-pad placed digits | Steamworks default checked: already "Generic Gamepad", all pads opted in; nothing changed | Your own layout in Steam: Library → Run of the Nine → Controller → "Gamepad" (or revert to default) | Owner |
| **RM-113** run length | Closed: the Daily Walk is the short run | Reopen only if the report shows won H0 runs over 45 min | – |
| **RM-108** balance check | Built: every run records gold left, shop visits with a purchase, items used (RUN-BALANCE-001); `run_length_report.py` prints them | Read at the next playtest | Owner plays |
| **RM-120** faster first choice | Built: the reachable tiles' boards are baked first (RUN-FIRST-001); the first tile has the focus | – | – |
| **RM-118** something new in each of the first ten runs | Built ("behutsam öffnen"): ten things held back in a new profile's Garden Runs, one opens per finished run, shown in the run end's "New" column (RUN-EARLY-001/002) | Your feel at the next playtest with a fresh profile | Owner plays |
| **RM-116** class challenges | Built (layout A, the proposed list): 24 challenges, seals on the plaques, the scroll's second page, 24 Steam achievements (CLASS-CHALLENGE-001/002, UI-STYLE-043) | Publish the achievements in Steamworks (63 saved, five languages) | Owner |
| **RM-119, RM-121** | Later (owner's order) | – | – |
| **RM-104** more relics, items and events | Not picked this round | – | – |
| **Modifier catalog, steps 3 and 5** | Built: `ModifierCatalog` (BOSS-MOD-005) and golden geometry snapshots (BOSS-MOD-007) | – | – |
| **Pattern Scroll list** | Built: every drawn piece, by its rule's name, in five languages (ITEM-SCROLL-001) | – | – |
| **`MainMenuClassInfoPresenter`** | Removed with its 20 orphaned texts; its tests now cover `ClassUnlockText` | – | – |
| **Spirit Trials "Solver Start"** | Not built (random item only) | Only if you want the choice | Owner |

## 3. Waiting for the owner

| Decision | Why it matters | Proposal |
|---|---|---|
| **Apple Developer account** (99 USD a year) | Without it macOS warns about or blocks the game | Postpone the Mac until the Deck is Verified; the Deck and Linux run the Windows build through Proton. |
| **The 8-bit soundtrack set** | Optional; the 6-second loops stay otherwise | The same listening page as GAP-073 with the eight loops; keep or drop in one go. |

## 4. Known technical leftovers

- `Game.csproj` and the test `.csproj` files are regenerated by Unity on the owner's machine and stay uncommitted on
  purpose; `Assets/Neuer Ordner.meta` and `Assets/Resources/audio/music_new.meta` are the owner's files and are left alone.

## 5. Closed since 1.9

- **GAP-073 and GAP-114, the sounds:** round 2 picked by the owner and built (AUDIO-SFX-001, 2026-11-04).
- **Steam Deck (RM-102) and a Deck for a first start:** closed by the owner (2026-11-04); the game's Deck work is built (DECK-TEXT-001, DECK-KEYBOARD-001, DECK-BUTTONS-001), a Deck start and Valve's review are the owner's.
- **Legal check:** closed by the owner (2026-11-04): the content survey, trader status, Impressum, tax and the trademark search are the owner's (`docs/legal/legal-check-2026-10.md` 1.2).
- **0.4.0 and 0.4.1:** 0.4.0 live 2026-11-02 (build 26400103); 0.4.1 built and uploaded by the owner, announced on Steam 2026-11-04 ("Update v0.4.1: the story of the Nine"), mirrored to #rotn-news; how-to-play, known-issues and the #rotn-seeds-and-walks topic posted.
- **Upload of 0.2.1 and the build after it:** 0.3.0 is live since 2026-10-27 (Steam announcement "Update v0.3.0").
- **Playtest build 0.2.2 on the beta branch:** dropped by the owner; 0.3.0 carried it.
- **Keystone relics (RM-114):** built 2026-10-26 (RELIC-KEY-001/002).
- **Codex filter and "Best paired with":** built in 1.6.
- **The old Daily Walk panel:** removed (GAP-110, DAILY-GOAL-004, 2026-11-03).
- **The trial's starting item:** built (TRIAL-SESSION-001, GAP-113, 2026-11-03).
- **The ten story achievements in Steamworks:** created with icons and five languages, saved (2026-11-03); publishing is in section 3.
- The stray line "move twice; to fix with the controller work …" of 1.9 belonged to GAP-062, closed on 2026-10-22.
