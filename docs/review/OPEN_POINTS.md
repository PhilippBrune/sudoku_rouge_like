# Open points

**Version:** 1.9 | **Date:** 2026-10-27 | **Status:** review (a living list; update it whenever an item closes or a new one opens)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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
page points to them.

## 1. For the next playtest review

### New since the 0.2.0 build, to try at the next playtest

| What | Where to look | Notes |
|---|---|---|
| Entering a floor (garden flow D) | Start a run; beat a boss | Two seconds of the painting with the floor's name; a click or any key skips it. Floor 4 and 5 say that a mistake costs 2 HP. |
| Pencil mode in indigo | Puzzle, Pencil switch or P | Number pad and pencil marks in indigo. |
| A grade per puzzle | End of a run | One stamp per puzzle (Perfect, Clean, Rough, Messy), floors apart; Rough and Messy rows replace the old mistake row. |
| Gold that matters | Shops, boss gate, end of a run | Prices ×2.5, puzzle gold −20 %; strike a trial or lower the intensity at the gate; gold kept scores. |
| Counter items | Shops from Floor 2 on | Six items that write digits into the cells of one family of rules; offered for the floor's rules. |
| Options that act | Options → Gameplay, Display, Accessibility | Highlight Errors, Auto Pencil Cleanup, Show Digits Left, Jump to Next Empty Cell, Double-Tap to Confirm, Screen Shake, Master Volume, Board Brightness/Contrast, Text Size on the board, colour-blind line colours (GAP-061). |
| Options as a hand scroll | Main menu → Options; Options in a puzzle | Bookmarks (LB/RB), a note under every switch, Reset this page; Accessibility is a page now (with the preview board), the run has a Run page. Check that long German and French bookmark names stay readable. |
| The washi window on the path | Any floor of a Garden Run; High Contrast on and off | The painting shows through the thin paper; every label on a slip. Say if any label is hard to read on a floor. |
| Input Lock option | Options → Gameplay | Off: a digit counts right after choosing a cell; the time row goes 0.10–1.00 s. Say which time feels right with a controller. |
| Select Class without a confirmation window | Start Run (and Endless Zen, Spirit Trials) | Seals with level or unlock goal, the class on the scroll, Harmony and the perk on the strip (LB/RB, Y); Begin starts the run, with a saved run it reads "Abandon & Begin". Say if anything from the old window is missed. |
| Game Modes as five curtains | Main menu → Game Modes | Each curtain's state strip, the slip of the focused mode, Records under Endless Zen; a locked mode says what opens it. Harmony set here shows on Select Class and back. |
| Results of Endless Zen, Spirit Trials, Monthly Walk | End of each mode | The record large with the personal best and a "New best" stamp, the numbers on a tally paper; "Back to the garden gate". |
| Reduce Motion | Options → Accessibility | With it on: no pulsing path nodes or boss node, no digit pops (GAP-066). |
| Walk again and "This run" | End of any run | Walk again starts the next run at once (same class, mode, Harmony, perk); a lost run says how close it came and what it moved. Say if it makes you start another run. |
| Run length | Play two or three runs in one sitting | Then run `python tools/playtest/run_length_report.py` (or send the save): it says whether a won Harmony 0 run takes over 45 minutes (RM-113). |
| The D-pad on the menus | Codex, Game Modes, Options with a controller | The D-pad moves along rows and columns; an arrow key moves once (GAP-062, closed). |
| Harmony perks | Moonshade Offering, Scholar's Burden, Empty Canvas | Moonshade: +2 pencil, first reward's first slot empty; Scholar's Burden: Ink Well in the bag, +5 pencil; Empty Canvas: Rare+ rewards. |
| Numbers in the game's language | Any screen with numbers, English on a German PC | "XP ×1.4", "50,000" in English; "1,4", "50.000" in German (GAP-065). |
| Class rewards at Level 30 and 40 | A class at Level 30+ (start a run), a class at 40 or prestiged (Meta Progression, the puzzle's class badge) | The run starts with the class's item; the frame in the class's colour. Say if Golden Koi, Rice Paper Umbrella and Temple Incense fit their classes. |
| Garden of classes | Main menu → Meta Progression | Plaques with level or unlock goal, the scroll with runs, best floor and the level path, prestige with two presses (X). Runs and best floor count from this build on. |
| Codex as a collector's book | Main menu → Codex | Bookmarks Items, Relics, Rules, Curses with counts; Y filters; the right page shows how often an item was found and used. The Curses page fills from this build on (curses met before it are not known). |

**Balance check (RM-108):** after a won run note the gold left at the end, how many shop visits ended with a purchase
and how many items were used. Targets from the concept: under 80 gold left, a purchase in 70 % of shop visits, at least
4 items used per run. The new late-floor damage (2 HP per mistake from Floor 4) and the quarter heal at the rest make
late floors harder for everyone; say if it feels too harsh.

## 2. Being built (owner picks of 2026-10-20)

| Item | State | Reference |
|---|---|---|
| Steam Deck (RM-102, RM-107) | Decided 2026-10-22: Steam Input, the Deck runs the Windows build through Proton (no build of its own). Left: the Steam Input default layout and the compatibility review in Steamworks (owner), text of size 13 or more at 1280×800, the on-screen keyboard for the seed field, the Deck's button names | `docs/platform-ports-macos-linux.md` 1.1 |
| Playtest build 0.2.2 on the password beta branch | Asked for 2026-10-22: Walk again, "This run", the fixed perks and the D-pad, two or three runs per sitting for the run-length report (RM-113); the content survey in the same Steamworks visit | `steam_assets/ReleaseNotes_v0.2.2-beta.md` |
| Keystone relics (RM-114) with more relics, items and events (RM-104) | Owner pick 2026-10-22: a keystone may bend Sudoku's own rules (for example free pencil marks), not only the gold and items around them | GameDesignSpec.md §14 (End Screens) |

## 3. Waiting for the owner

| Decision | Why it matters | Reference |
|---|---|---|
| Apple Developer account (99 USD a year) for signing and notarizing the macOS build | Without it macOS warns about or blocks the game | platform plan, decision 2 |
| A Steam Deck for a first start, and a Mac later | A build that was never started on the system should not be published; the Deck can test the beta branch | platform plan, risks |
| The 8-bit soundtrack set (optional) | The game's audio is accepted as it is (GAP-030 closed); the longer 8-bit loops would replace the 6-second ones if you want them | `steam_assets/audio_8bit/synth`, `docs/audio-8bit-prompts.md` |
| Pick from the rest of the one-more-run concept (RM-113, RM-116 to RM-121) | RM-114 is picked; RM-113 waits for the run-length report from the 0.2.2 playtest | GameDesignSpec.md §14 (End Screens) |
| Legal check: Steamworks content survey (AI content, simulated gambling), trader status (DSA), trademark search, Impressum, tax | No law broken by the game; these are store and business duties | `docs/legal/legal-check-2026-10.md` |
| Upload and release of 0.2.1, and a build with the changes since (CHANGELOG "Unreleased") | 0.2.1 is built, tagged and pushed (`steam_assets/ReleaseNotes_v0.2.1.md`); the newer commits are local | `CHANGELOG.md` |

## 4. Known technical leftovers

- Modifier catalog plan, Phase 0 step 3 (one catalog entry per modifier instead of the switch statements) and step 5
  (golden geometry snapshots); see BossMechanicsSystem.md, "Modifier catalog".
- The Pattern Scroll item's list (`InRunController.BuildPatternScrollMechanics`) leaves out cell pairs, cell markers and outside clues, shows line rules by their internal names ("GermanWhispers #1") and its entries are English only (not through the localization catalogs).

- `MainMenuClassInfoPresenter` lost its last caller with the confirmation window (its tests still cover the unlock texts); remove it once `ClassUnlockText` carries those tests.

  move twice; to fix with the controller work of platform phase 1 and to check in the player with a controller.
- Codex book (UI-STYLE-018): the mockup's Normal · Rare · Epic filter and "Best paired with" are left out (no game data backs them); add them only if the owner wants them. Item uses are counted from this build on.
- `Game.csproj` and the test `.csproj` files are regenerated by Unity on the owner's machine and stay uncommitted on
  purpose; `Assets/Neuer Ordner.meta` and `Assets/Resources/audio/music_new.meta` are the owner's files and are left alone.
