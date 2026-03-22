# Tutorial Mode System

**Version:** 1.0 | **Date:** 2026-03-21 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Purpose

Tutorial mode is an isolated practice environment using the same Sudoku engine as normal play, but with **all progression disabled**. It allows players to learn board mechanics, modifier rules, and class resource management without risk.

**Disabled in tutorial:**
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
- Main Menu → Tutorial button
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

Note: Dropdown index 2 maps to internal variant 3 (jigsaw templates).

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

### Resource Mode

Two resource modes are available:

| Mode | HP | Pencil | Behaviour |
|------|:---:|:---:|-----------|
| **Free** | ∞ | ∞ | No penalties — pure practice. Mistakes have no HP cost, pencil marks are unlimited. |
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
| `Assets/Scripts/Tutorial/TutorialProgressService.cs` | Completion tracking by configuration key |
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

## 7★ — Empty Grid Rules

7★ (100% missing, 0 givens) is allowed on **all board sizes** without restrictions. On small boards (5×5) without modifiers, the puzzle may have multiple solutions — this is acceptable in tutorial mode as a sandbox experience. No modifier requirement is enforced.

---

## Tutorial Completion Achievements

Completing tutorial milestones unlocks **achievements only** — no gameplay progression (no XP, no class unlocks, no gold).

Suggested achievement triggers:
- Complete a puzzle at every board size (5×5 through 9×9)
- Complete a puzzle with each of the 15 modifiers at least once
- Complete the full board×star grid (35 configurations)
- Complete a 7★ puzzle (empty grid)
- Complete a 9×9 7★ puzzle with at least one modifier active
