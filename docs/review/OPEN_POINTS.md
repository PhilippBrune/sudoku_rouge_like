# Open points

**Version:** 2.1 | **Date:** 2026-11-03 | **Status:** review (a living list; update it whenever an item closes or a new one opens)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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

The owner's answers of 2026-11-03 ("ok", "schließen", "ja machen", "genau so implementieren", "machen", "okay") and what
came of them:

| Point | State | Next | Who |
|---|---|---|---|
| **GAP-108** The D-pad placed digits | Steamworks default checked: already "Generic Gamepad", all pads opted in; nothing changed | Your own layout in Steam: Library → Run of the Nine → Controller → "Gamepad" (or revert to default) | Owner |
| **RM-113** run length | Closed: the Daily Walk is the short run | Reopen only if the report shows won H0 runs over 45 min | – |
| **RM-108** balance check | Built: every run records gold left, shop visits with a purchase, items used (RUN-BALANCE-001); `run_length_report.py` prints them | Read at the next playtest | Owner plays |
| **GAP-073** 25 silent sounds | Round 1: five kept and wired (AUDIO-UI-001). Round 2: A and B (garden) and C (8-bit) for the other 21 and for the two too-low sounds (GAP-114) on `steam_assets/audio_proposals/round2/sfx_round2.html` | Pick A, B, C or none per event, paste the list; then I wire them | Owner, then me |
| **Steam Deck** (RM-102) | Built: no text under 9 px at 1280×800 (DECK-TEXT-001), Steam's gamepad keyboard on the seed fields (DECK-KEYBOARD-001), L1/R1 and L2/R2 in the hints (DECK-BUTTONS-001) | A start on a Deck, then the compatibility review in Steamworks | Owner |
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
| **Upload 0.4.0** | The story, the scenes, the story achievements and the fixes since 0.3.0 are local (`steam_assets/ReleaseNotes_v0.4.0.md`) | First publish the achievements (39, five languages), then upload and set live, then the announcement. After the upload: `walk_posts.py walk-live`, `discord_admin.py replace how-to-play`, `discord_admin.py apply` (channel topic). |
| **Legal check** (content survey, DSA trader status, trademark, Impressum, tax) | Store and business duties (`docs/legal/legal-check-2026-10.md` 1.2) | Content survey: a draft adds "simulated gambling" to the June answers, not saved; you press "Save and Continue" and publish the ratings. Trademark: no US hit; the German and EU registers could not be searched online, so a paid DPMA/EUIPO search is the next step. Trader status, Impressum and tax stay with you. |
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
