# Open points

**Version:** 1.0 | **Date:** 2026-10-21 | **Status:** review (a living list; update it whenever an item closes or a new one opens)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | review | First list, at the owner's request after the 0.2.0 playtest: what to check at the next playtest (GAP-057 included), what is being built, what waits for the owner, and known technical leftovers. |

This list collects what is still open across the project. Bugs keep their details in [GAME_GAPS](GAME_GAPS.md), features in
[ROADMAP_BACKLOG](../ROADMAP_BACKLOG.md), specification questions in [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md); this
page points to them.

## 1. For the next playtest review

### GAP-057: the menu music on a volume change (not reproduced)

In the 0.2.0 playtest, changing a sound volume in a puzzle started the menu music. A test that starts the real game,
enters a puzzle, opens its Options and presses every volume button and Mute all could not make it happen: the floor
music keeps playing and the menu music stays off (`InRunAudioPlayModeTests`). The gap stays open.

Questions for the playtest, if it happens again:

- Which screen was open: the puzzle, the garden path, the reward, the shop, the boss gate?
- Which control: a volume arrow, Mute all, Mute when unfocused, the menu music style, Reset?
- Keyboard and mouse, or a controller?
- Which floor, and was it the first puzzle of the run? (Before the first puzzle the path plays a generic track that may
  sound like the menu.)

### New since the 0.2.0 build, to try at the next playtest

| What | Where to look | Notes |
|---|---|---|
| Entering a floor (garden flow D) | Start a run; beat a boss | Two seconds of the painting with the floor's name; a click or any key skips it. Floor 4 and 5 say that a mistake costs 2 HP. |
| Pencil mode in indigo | Puzzle, Pencil switch or P | Number pad and pencil marks in indigo. |
| A grade per puzzle | End of a run | One stamp per puzzle (Perfect, Clean, Rough, Messy), floors apart; Rough and Messy rows replace the old mistake row. |
| Gold that matters | Shops, boss gate, end of a run | Prices ×2.5, puzzle gold −20 %; strike a trial or lower the intensity at the gate; gold kept scores. |
| Counter items | Shops from Floor 2 on | Six items that write digits into the cells of one family of rules; offered for the floor's rules. |
| Options that act | Options → Gameplay, Display, Accessibility | Highlight Errors, Auto Pencil Cleanup, Show Digits Left, Jump to Next Empty Cell, Double-Tap to Confirm, Screen Shake, Master Volume, Board Brightness/Contrast, Text Size on the board, colour-blind line colours (GAP-061). |

**Balance check (RM-108):** after a won run note the gold left at the end, how many shop visits ended with a purchase
and how many items were used. Targets from the concept: under 80 gold left, a purchase in 70 % of shop visits, at least
4 items used per run. The new late-floor damage (2 HP per mistake from Floor 4) and the quarter heal at the rest make
late floors harder for everyone; say if it feels too harsh.

## 2. Being built (owner picks of 2026-10-20)

| Item | State | Reference |
|---|---|---|
| Options as a hand scroll with bookmarks (UI-STYLE-017) | The audit is done (GAP-061); the new look is next | `docs/design/layout-proposals.md` |
| Codex as a collector's book (UI-STYLE-018) | To build after the Options | same |
| Meta Progression as the garden of classes (UI-STYLE-019) | To build after the Codex | same |
| Linux and Steam Deck, then macOS (RM-107) | Phase 1 next: controller buttons through the Input System, rumble through the Input System, Frameless Window hidden off Windows; then a Linux build | `docs/platform-ports-macos-linux.md` |

## 3. Waiting for the owner

| Decision | Why it matters | Reference |
|---|---|---|
| Apple Developer account (99 USD a year) for signing and notarizing the macOS build | Without it macOS warns about or blocks the game | platform plan, decision 2 |
| Steam Input default layout or the Input System rewrite for controllers | The rewrite is planned as the default (it is needed for the Deck either way); Steam Input could be added on top | platform plan, decision 3 |
| A Mac and a Steam Deck (or a Linux PC) for a first start | A build that was never started on the system should not be published | platform plan, risks |
| Listening to the 8-bit soundtrack set | The music loops are still the short placeholders (GAP-030) | `steam_assets/audio_8bit/synth`, `docs/audio-8bit-prompts.md` |
| Sign-offs SPEC-CONF-008 (class balance values) and SPEC-PROC-007 (legal wording) | Open owner sign-offs | [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md) |
| Result screens of the Monthly Walk, Spirit Trials and Endless Zen in the ink-and-paper look | The Garden Run end uses the new scroll; these three still show text | `docs/design/layout-proposals.md` (no mockup yet) |
| Upload and release of the next build | Everything since 0.2.0 is in the CHANGELOG under "Unreleased"; local commits on `feature/game-ui-refactor`, not pushed | `CHANGELOG.md` |

## 4. Known technical leftovers

- Modifier catalog plan, Phase 0 step 3 (one catalog entry per modifier instead of the switch statements) and step 5
  (golden geometry snapshots) — `docs/design/modifier-catalog-plan.md`.
- The Pattern Scroll item's list (`InRunController.BuildPatternScrollMechanics`) leaves out cell pairs, cell markers and outside clues, shows line rules by their internal names ("GermanWhispers #1") and its entries are English only (not through the localization catalogs).
- `Game.csproj` and the test `.csproj` files are regenerated by Unity on the owner's machine and stay uncommitted on
  purpose; `Assets/Neuer Ordner.meta` and `Assets/Resources/audio/music_new.meta` are the owner's files and are left alone.
