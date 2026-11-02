# Tutorial Mode System

**Version:** 1.15 | **Date:** 2026-11-02 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.15 | 2026-11-02 | implemented | 0.3.1 playtest (owner): the Regions choices a board size does not have are greyed (CUSTOM-REGION-001); a solved or lost Custom Puzzle ends on its numbers (CUSTOM-FINISH-001). |
| 1.14 | 2026-10-28 | implemented | 0.3.0 playtest (RM-209, owner: one per profile, Continue also on the main menu): a Custom Puzzle is saved in a pair of its own and continued (CUSTOM-SAVE-001); the 1.12 concept is built. SAVE-TUTO-001 now covers Sudoku Basics only. |
| 1.13 | 2026-10-28 | implemented | 0.3.0 playtest (RM-209, owner's pick UI-STYLE-037): a "?" beside each Board field explains it (CUSTOM-HELP-001); the puzzle's seed is shown, typed or rolled, and gives the same board (CUSTOM-SEED-001); a share code holds the whole setup with Copy and Paste (CUSTOM-CODE-001). |
| 1.12 | 2026-10-28 | implemented | Concept (RM-209, 0.3.0 playtest): save and quit a Custom Puzzle in a pair of its own beside the Garden Run and the Monthly Walk. Not built. |
| 1.11 | 2026-10-27 | implemented | 0.3.0 playtest: every chosen rule is mandatory for the board, and a rule without room is named on the status line (CUSTOM-RULE-001, GAP-086); Free resources mean no class: no passive shown or applied (CUSTOM-CLASS-001, GAP-087); one timer (HUD-TIMER-001, GAP-088). |
| 1.10 | 2026-10-21 | implemented | Docs clean-up: completion is stored in the save (`TutorialProgressState.CompletedKeys`, written by `MainMenuController`); there is no `TutorialProgressService`. |
| 1.9 | 2026-10-07 | implemented | 7★ requires a modifier, as the code enforces (SPEC-CONF-015); dropdown → region variant mapping and why variant 2 is not offered (SPEC-CONF-018); entry named Custom Puzzle, Sudoku Basics separate (SPEC-CONF-033); 7★ uses rule-based validation (SPEC-CONF-003). |
| 1.8 | 2026-04-16 | implemented | Inline requirement IDs added (SAVE-TUTO-001). Stale REQ-TUTORIAL-173..202 auto-generated table replaced with canonical IDs. |
| 1.7 | 2026-03-29 | implemented | Tutorial saves hard-blocked: RunAutoSaveCoordinator.Save() returns early when runState.TutorialMode=true; SaveFileService.HasActiveRun() returns false when ActiveRunState.TutorialMode=true. This ensures Resume Game is always greyed out after tutorial sessions and no tutorial state ever writes to the save file ActiveRunState slot. |
| 1.6 | 2026-03-29 | implemented | Tutorial Save&Quit button renamed "Quit (Q)" when in tutorial mode — WireInGameButtons() in PrototypeRunScreenController changes the button label text at run start when TutorialMode=true (SaveAndQuit already had no-save behavior; this makes it visible). Each "Start Puzzle" press generates a new puzzle via BuildRuntimeSeed() (UtcNow.Ticks component ensures unique seed per call). Resume Game button remains grey for tutorial runs — HasActiveRun() checks ActiveRunState≠null, tutorial never sets it. |
| 1.5 | 2026-03-29 | implemented | Tutorial config fully wired: selected board size, stars, region variant, and modifiers are now used when generating the puzzle. Star range: min = stars×10%+30%, max = next_star% − 1%; 7★ = 100% missing (empty grid). Resource Mode converted from two toggles to a single dropdown (Free / Class-Based); font colour matches other labels; dropdown text colour fixed to dark for readability. `TutorialMenuController.ConfigureResourceModeDropdown()` added; `GetConfig()` reads dropdown first, falls back to legacy toggle. |
| 1.4 | 2026-03-28 | implemented | Resource Mode section added to tutorial setup UI: Free (∞ HP/Pencil) toggle and Class-Based toggle with class dropdown. `TutorialMenuController.ConfigureResourceMode()` method added. Tutorial panel resized (0.05–0.95) to fit new rows; all dropdown templates sized to show all items without scrolling. |
| 1.3 | 2026-03-28 | implemented | Bug fix: StartTutorialRun now calls StartLevel(CurrentLevelConfig) so the puzzle board is generated before the tutorial screen is shown. Previously the board was null and no puzzle appeared after pressing "Start Puzzle". |
| 1.2 | 2026-03-24 | implemented | Star dropdown now shows removal percentage labels ("1 Star (40%)" through "7 Stars (100%)"); StarDensityService clamped 1-7; 7-star option added |
| 1.1 | 2026-03-23 | implemented | Full rebuild verified: TutorialModeService, TutorialProgressService, TutorialMenuController, TutorialRunBannerController all present and correct |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Purpose

Tutorial mode is an isolated practice environment using the same Sudoku engine as normal play, but with **all progression disabled**. It allows players to learn board mechanics, modifier rules, and class resource management without risk.

**Disabled in Sudoku Basics:** `[SAVE-TUTO-001]` (a Custom Puzzle is saved, CUSTOM-SAVE-001)
- No XP earned or applied
- No Gold earned
- No item or relic acquisition
- No class leveling or unlock progress
- No meta progression tracking (bosses defeated, runs completed, etc.)
- No prestige advancement
- No save/resume — each tutorial session starts fresh

---

## Main Menu Integration

The runtime menu flow includes tutorial entry points:
- Main Menu → **Custom Puzzle** button (named "Tutorial" before; it is a sandbox, so the name was kept as built, SPEC-CONF-033). The Sudoku Basics lesson is a separate tutorial, offered at first launch and in Options (FirstTimePlayTutorial).
- Tutorial Setup Screen (configure puzzle parameters)
- Tutorial Progress Screen (view completion grid)

---

## Setup Configuration

### Board Size

Selectable board sizes: **5×5, 6×6, 7×7, 8×8, 9×9**

### Star Difficulty

| Stars | Missing Cells | Notes |
|:---:|:---:|-------|
| 1★ | ~10% | Beginner — most cells pre-filled |
| 2★ | ~25% | Easy |
| 3★ | ~35% | Medium |
| 4★ | ~50% | Challenging |
| 5★ | ~70% | Hard |
| 6★ | ~90% | Expert — as few as 8 givens on 9×9 |
| 7★ | 100% | **Tutorial only** — completely empty grid, no givens |

7★ is exclusive to tutorial mode and does not appear in normal runs. It tests the player's ability to solve a puzzle purely from constraints and modifiers.

### Region Layout

Three grid layout variants selectable via dropdown:

| Dropdown Index | Variant | Description |
|:---:|:---:|-------------|
| 0 | Standard (0) | Standard rectangular regions |
| 1 | Alt Rectangular (1) | Alternative rectangular region shapes |
| 2 | Jigsaw (3) | Irregular/jigsaw region templates |

Note: Dropdown index 2 maps to internal variant 3 (jigsaw templates). Variant 2 (the other irregular template set) is not offered: one irregular choice keeps the sandbox simple; runs use all four variants (SPEC-CONF-018). Levels with a global structural modifier use the rectangular layout regardless of the choice (SPEC-AMB-014).

### Modifier Selection

All **15 modifiers** are available as individual toggles in the tutorial setup panel:

| # | Modifier | Board Restriction |
|:---:|----------|-------------------|
| 1 | Fog of War | None |
| 2 | Arrow Sums | None |
| 3 | German Whispers | **7×7+ only** |
| 4 | Dutch Whispers | None |
| 5 | Parity Lines | None |
| 6 | Renban Lines | None |
| 7 | Killer Cages | **7×7+ only** |
| 8 | Difference Kropki | None |
| 9 | Ratio Kropki | None |
| 10 | Palindrome | None |
| 11 | Thermo | None |
| 12 | Between Lines | None |
| 13 | Even Odd | None |
| 14 | Nonconsecutive | None |
| 15 | Antiknight | None |

**Balancing constraint:** German Whispers and Killer Cages are automatically disabled on boards smaller than 7×7. The toggle is greyed out and unselectable.

Multiple modifiers can be combined for advanced practice.

**[CUSTOM-RULE-001]** Every chosen rule must be on the board: generation retries with other boards until each one has its marks, as for the boss gate's picks. If a rule still finds no room, the puzzle starts and the status line names it ("No room on this board for: …"); a rule is never dropped silently (GAP-086, 0.3.0 playtest: a 9×9 at 7 stars with Sum Dot showed no dots).

### Help, seed and share code (1.13, RM-209)

- **[CUSTOM-HELP-001]** A "?" beside Size, Difficulty, Regions and Resources opens a paper slip beside the Board paper that
  says what the field does; Difficulty's slip shows each star count with the share of empty cells (1 star about 40 %, each
  star a tenth more, 7 stars an empty grid that the rules alone decide). The pointer over a "?" shows it, a click pins it;
  with a pad the slip follows the focus on the row's controls.
- **[CUSTOM-SEED-001]** The Seed field under the Board paper holds the puzzle's seed: rolled when the screen opens, typed
  (1 to 999 999 999) or rolled again with Random. The same setup and seed give the same board.
- **[CUSTOM-REGION-001]** (1.15, 0.3.1 playtest) The Regions choices follow the board size: Boxes on 6×6 (2×3), 8×8 (2×4)
  and 9×9 (3×3); Turned on 6×6 (3×2) and 8×8 (4×2) only (a turned 3×3 box is the same box); Irregular on every size, and
  the only choice on 5×5 and 7×7 (their regions are always templates). A rule that needs boxes (Anti-King, Anti-Knight,
  Nonconsecutive and the other board-shaping rules, Entropy) greys Irregular on 6×6, 8×8 and 9×9, where the generator
  would use boxes. A greyed choice cannot be taken; a size change, a rule or a share code that leaves the choice without
  support moves it to Boxes, or to Irregular where there are no boxes (`TutorialModeService.SupportsRegionVariant`,
  `TutorialMenuController.FitRegion`). 9×9 irregular puzzles exist (two templates) and generate without a fallback.
- **[CUSTOM-FINISH-001]** (1.15, 0.3.1 playtest) A solved Custom Puzzle, or one lost in Class-Based mode, ends on a result
  in the mode-result family (UI-STYLE-026): the tally (time, mistakes, pencil marks, cells filled, rules), the grade large
  (Perfect, Clean, Rough, Messy by mistakes, as the Garden Run's puzzles) with the board and its seed, and Back. The saved
  puzzle is cleared when it is solved (`EndScreenViewController.ShowCustomPuzzleResult`).
- **[CUSTOM-CODE-001]** The share code holds the size, stars, regions, resources, class, rules and seed:
  `R1-` and about twenty letters and digits (Crockford base32 in groups of four, a check byte at the end). Copy puts it
  on the clipboard; Paste a code reads the clipboard and sets the screen, or says why not (no code or a typo, a class not
  unlocked on this profile, a rule that does not fit the board). Nothing else from the clipboard is taken.

### Resource Mode

Two resource modes are available:

| Mode | HP | Pencil | Behaviour |
|------|:---:|:---:|-----------|
| **Free** | ∞ | ∞ | No penalties — pure practice. Mistakes have no HP cost, pencil marks are unlimited. No class: no portrait, no passive shown or applied (**[CUSTOM-CLASS-001]**, 1.11, GAP-087; it showed and used the default class's). |
| **Class-Based** | Class stat | Class stat | Uses the selected class's base HP, Pencil, and Item Slots. Mistakes cost HP normally. |

When Class-Based is selected, the player chooses from their **unlocked classes**:

| Class | HP | Pencil | Slots | Passive |
|-------|:---:|:---:|:---:|---------|
| Number Freak (default) | 10 | 10 | 2 | — |
| Garden Monk | 14 | 5 | 1 | Every 5 correct placements → +1 HP |
| Shrine Archivist | 8 | 15 | 2 | First pencil use per cell is free |
| Koi Gambler | 9 | 8 | 2 | 25% chance wrong placement costs 0 HP; 25% chance correct placement grants +1 Gold |
| Stone Gardener | 11 | 8 | 3 | First item used each level is not consumed |
| Lantern Seer | 7 | 12 | 2 | Boss modifiers are 20% weaker |
| Reed Duelist | 8 | 7 | 2 | Perfect no-pencil puzzle grants +2 Pencil |
| Quiet Cartographer | 9 | 10 | 2 | Perfect tile completion previews adjacent tiles |

Class passives are **active** in Class-Based mode so players can practice with them. Class leveling rewards (e.g. Number Freak L3 +1 Pencil) are **not** applied — base stats only.

---

## Session Rules

In tutorial sessions:
- Item roll phase is disabled (no post-puzzle item rewards)
- Rerolls are disabled
- Route branching / path system is disabled (single puzzle, no garden map)
- Gold spending is disabled (no shop)
- Relic acquisition is disabled
- Class unlock progress is disabled
- Run events are disabled

### Mistake Behaviour

| Mode | Mistake Cost |
|------|-------------|
| Free | No HP penalty — infinite retries |
| Class-Based | Normal HP penalty per class stats. HP reaches 0 → puzzle ends (restart available) |

---

## Completion Tracking

Tutorial progress is tracked per configuration to encourage systematic mastery.

### Completion Key Format

```
{BoardSize}|{Stars}|{SortedModifierCombo}
```

**Examples:**
- `5|1|None` — 5×5, 1★, no modifiers
- `9|5|GermanWhispers` — 9×9, 5★, German Whispers only
- `9|5|FogOfWar+RenbanLines` — 9×9, 5★, two modifiers combined
- `7|7|None` — 7×7, 7★ (empty grid), no modifiers

### Progress Views

The tutorial progress screen shows:

1. **Board × Star Grid** — 5 board sizes × 7 star ratings = 35 base configurations. Each cell shows ✔ (completed) or ✖ (not attempted).
2. **Modifier Training List** — 15 individual modifiers, each marked as completed or not (at any board size/star combo).
3. **Aggregate Completion %** — total unique configurations completed out of attempted.

### Persistence

Tutorial completion data is stored in `ProfileService.TutorialProgress` and persists across sessions. It is **not** tied to any specific class — completing a tutorial puzzle counts regardless of which class/mode was used.

---

## UI Indicator

During tutorial play, a persistent label is displayed:

```
TUTORIAL MODE | No Progression Rewards
```

This label appears in the top area of the puzzle screen to clearly distinguish tutorial from normal play.

---

## Relationship to Other Systems

### Path System

The garden path system (5 floors, Calm/Risk routes, boss gates) is **completely bypassed** in tutorial. The player solves a single puzzle with their configured settings, then returns to the tutorial setup screen.

### XP System

XP is **not calculated or displayed** in tutorial mode. The end-of-puzzle screen shows only:
- Board size and star rating
- Active modifiers
- Mistakes made
- Completion time (informational only)

### Items & Relics

Items and relics are **not available** in tutorial mode. Item slots from the class are shown but remain empty. This keeps tutorial focused on core Sudoku mechanics.

### Class Progression

Class leveling and prestige are fully disabled. The tutorial uses **base class stats only** (Level 1 equivalent). This ensures consistent practice conditions regardless of the player's progression state.

---

## Implementation Reference

### Key Files

| File | Responsibility |
|------|---------------|
| `Assets/Scripts/Tutorial/TutorialModeService.cs` | Static helper — board sizes, stars (1–7), modifier availability, validation, descriptions |
| `Assets/Scripts/Core/SaveModels.cs` (`TutorialProgressState`) | Completion tracking by key (`CompletedKeys`, written by `MainMenuController`) |
| `Assets/Scripts/UI/TutorialMenuController.cs` | UI wiring — dropdowns for board/stars/class/region, 15 modifier toggles, progress display |
| `Assets/Scripts/Run/RunDirector.cs` | Tutorial run isolation logic (disables progression, items, path) |
| `Assets/Scripts/Save/ProfileService.cs` | Persistent tutorial completion state |
| `Assets/Scripts/Core/RuntimeModels.cs` | `TutorialSetupConfig` — captures board size, stars, modifiers, resource mode, region variant |

### Data Model

```
TutorialSetupConfig:
    int              BoardSize           // 5–9
    int              Stars               // 1–7 (7 = tutorial-only empty grid)
    List<BossModifierId> SelectedModifiers // any combination of 15 modifiers
    ResourceMode     Mode                // Free or ClassBased
    string           SelectedClassId     // class identifier (if ClassBased)
    int              RegionVariant       // 0, 1, or 3

TutorialProgress (in ProfileService):
    HashSet<string>  CompletedKeys       // "{Size}|{Stars}|{Modifiers}"
```

---

## Save and quit a Custom Puzzle (concept 1.12, built 1.14, RM-209)

The owner's request (0.3.0 playtest): a Custom Puzzle can be saved and quit, and continued later, without touching the
Garden Run's save.

### Today

- Custom Puzzles are never saved (`SAVE-TUTO-001`: `RunAutoSaveCoordinator.Save` returns for `TutorialMode`). Save &
  Quit on a Custom Puzzle ends it.
- The save file (`SaveFileEnvelope`) holds one Garden Run (`ActiveRunState`, `ActivePuzzle`) and, beside it, one Monthly
  Walk (`ActiveSeasonalRunState`, `ActiveSeasonalPuzzle`), added so the month's walk does not overwrite a Garden Run.
  `SaveFileService.Sanitize` drops a pair it cannot resume, so a Custom Puzzle stored in either pair would be lost or
  offered as a Garden Run.

### The proposal

A third pair for the Custom Puzzle, built like the Monthly Walk's:

1. **Save file:** `ActiveCustomRunState` and `ActiveCustomPuzzle` in `SaveFileEnvelope`; new fields only, so older saves
   load as before (no custom puzzle).
2. **Saving:** `RunAutoSaveCoordinator.Save` routes a Custom Puzzle (Tutorial mode, not Sudoku Basics) to that pair
   after each move, as it does for the Monthly Walk; Sudoku Basics stays unsaved.
3. **What is saved:** the board (digits, pencil marks, mistakes), the puzzle's setup (size, stars, regions, rules,
   Free or the class), the seed and the drawn rule marks (`PuzzleSaveState` with `HasLevelConfig` and the overlay, which
   the Monthly Walk already uses), so the puzzle comes back exactly, without generating it again.
4. **Loading:** `SaveFileService` gets `IsResumableCustomRunState` and its line in `Sanitize`; `RunResumeService` and
   `GameBootstrap` a `ResumeCustomPuzzle` like `ResumeSeasonalChallenge`.
5. **Screens:** the Custom Puzzle book shows **Continue puzzle** (with its size, stars and rules) above Start when a
   saved one exists; Start with a saved puzzle asks first ("This replaces the saved puzzle"). The main menu's Resume and
   the "abandon your run" questions only ever mean the Garden Run.
6. **Ending:** solving or abandoning the puzzle clears its pair; a Garden Run, a Daily Walk or a Monthly Walk never
   touches it, and it never touches theirs.

### What it touches

- The Save & Quit button of a Custom Puzzle saves instead of ending (today it ends at once, without the two-press
  confirm, `InRunController.PathScroll.cs`).
- Tests: a save, quit and continue round trip; a Garden Run saved beside a Custom Puzzle keeps both; an old save without
  the pair loads.
- With the seed and the setup in the save, the share code of RM-209 (seed and setup in one code) uses the same fields.

**Owner's answers (2026-10-28):** one saved Custom Puzzle per profile; Continue also on the main menu.

**[CUSTOM-SAVE-001] As built (1.14):** `SaveFileEnvelope.ActiveCustomRunState` and `ActiveCustomPuzzle`;
`RunAutoSaveCoordinator.Save` routes a Custom Puzzle (Tutorial mode, run number 1) there and Sudoku Basics nowhere;
`SaveFileService.Sanitize` drops a pair it cannot continue as a whole. Save & Quit keeps it (`InRunSaveQuitPolicy`);
solving it, losing it or starting a new one clears it. The Custom Puzzle screen shows **Continue · 7×7 ★★★★** beside Back,
and Start over a saved puzzle asks first ("This replaces your saved puzzle", a second press within 3 s);
`GameBootstrap.LaunchCustomResume` rebuilds the board from the saved level, without generating it again. The main menu's
Continue comes with its new layout (UI-STYLE-032).

## 7★ — Empty Grid Rules

7★ (100% missing, 0 givens) is allowed on **all board sizes**, but **requires at least one modifier**: `TutorialModeService.TryValidateCustomSetup` rejects 7★ without a mode, as GEN-REMOVE-003 and GDS require (decided 2026-10-07, SPEC-CONF-015; the earlier "no modifier requirement" sentence was wrong). The empty grid has many solutions, so 7★ is the one place where a digit is wrong only when it breaks a rule (rule-based validation, SPEC-CONF-003).

---

## Tutorial Completion Achievements

Completing tutorial milestones unlocks **achievements only** — no gameplay progression (no XP, no class unlocks, no gold).

Suggested achievement triggers:
- Complete a puzzle at every board size (5×5 through 9×9)
- Complete a puzzle with each of the 15 modifiers at least once
- Complete the full board×star grid (35 configurations)
- Complete a 7★ puzzle (empty grid)
- Complete a 9×9 7★ puzzle with at least one modifier active


---

---

---

## Requirements Traceability

> This spec covers the **configurable practice tutorial** (not the first-time Sudoku Basics tutorial — see `docs/FirstTimePlayTutorial.md` for TUTO-BASICS-* IDs).

| REQ-ID | Section | Code File | Status |
|--------|---------|-----------|--------|
| `SAVE-TUTO-001` | Purpose — no save/resume | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` · `Save` skips `TutorialMode=true`; `Assets/Scripts/Save/SaveFileService.cs` · `HasActiveRun` | ✅ |

> **Note:** No dedicated TUTO-PRACTICE-* IDs exist in `REQUIREMENT_MAP.md` for this spec yet. The configurable practice tutorial is implemented but not formally traced beyond `SAVE-TUTO-001`. Add TUTO-PRACTICE-* IDs to the MAP when a formal audit of this system is scheduled.
