# Open points

**Version:** 1.7 | **Date:** 2026-10-22 | **Status:** review (a living list; update it whenever an item closes or a new one opens)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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
| Linux and Steam Deck, then macOS (RM-107) | Phase 1 next: controller buttons through the Input System, rumble through the Input System, Frameless Window hidden off Windows; then a Linux build | `docs/platform-ports-macos-linux.md` |

## 3. Waiting for the owner

| Decision | Why it matters | Reference |
|---|---|---|
| Apple Developer account (99 USD a year) for signing and notarizing the macOS build | Without it macOS warns about or blocks the game | platform plan, decision 2 |
| Steam Input default layout or the Input System rewrite for controllers | The rewrite is planned as the default (it is needed for the Deck either way); Steam Input could be added on top | platform plan, decision 3 |
| A Mac and a Steam Deck (or a Linux PC) for a first start | A build that was never started on the system should not be published | platform plan, risks |
| The 8-bit soundtrack set (optional) | The game's audio is accepted as it is (GAP-030 closed); the longer 8-bit loops would replace the 6-second ones if you want them | `steam_assets/audio_8bit/synth`, `docs/audio-8bit-prompts.md` |
| Pick from the rest of the one-more-run concept (RM-113, RM-114, RM-116 to RM-121) | The first slice is built; RM-113 waits for the run-length report | `docs/design/one-more-run-concept.md` §4, §8 |
| Legal check: Steamworks content survey (AI content, simulated gambling), trader status (DSA), trademark search, Impressum, tax | No law broken by the game; these are store and business duties | `docs/legal/legal-check-2026-10.md` |
| Sign-offs SPEC-CONF-008 (class balance values) and SPEC-PROC-007 (legal wording) | Open owner sign-offs | [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md) |
| Upload and release of 0.2.1, and a build with the changes since (CHANGELOG "Unreleased") | 0.2.1 is built, tagged and pushed (`steam_assets/ReleaseNotes_v0.2.1.md`); the newer commits are local | `CHANGELOG.md` |

## 4. Known technical leftovers

- Modifier catalog plan, Phase 0 step 3 (one catalog entry per modifier instead of the switch statements) and step 5
  (golden geometry snapshots) — `docs/design/modifier-catalog-plan.md`.
- The Pattern Scroll item's list (`InRunController.BuildPatternScrollMechanics`) leaves out cell pairs, cell markers and outside clues, shows line rules by their internal names ("GermanWhispers #1") and its entries are English only (not through the localization catalogs).

- `MainMenuClassInfoPresenter` lost its last caller with the confirmation window (its tests still cover the unlock texts); remove it once `ClassUnlockText` carries those tests.

  move twice; to fix with the controller work of platform phase 1 and to check in the player with a controller.
- Codex book (UI-STYLE-018): the mockup's Normal · Rare · Epic filter and "Best paired with" are left out (no game data backs them); add them only if the owner wants them. Item uses are counted from this build on.
- `Game.csproj` and the test `.csproj` files are regenerated by Unity on the owner's machine and stay uncommitted on
  purpose; `Assets/Neuer Ordner.meta` and `Assets/Resources/audio/music_new.meta` are the owner's files and are left alone.
