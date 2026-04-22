# Run of the Nine — Game Design Specification

**Version:** 1.5 | **Date:** 2026-04-07 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.5 | 2026-04-07 | implemented | Game loop improvements: (1) Combo counter HUD — live streak label in puzzle, resets on mistake. (2) Class passive label shown in puzzle HUD. (3) Rest node replaced with 3-way choice (Heal / +Pencil / +Reroll Token). (4) Relic node replaced with 3-of-3 relic choice panel. (5) Cursed node added (Accept/Decline risk/reward). (6) Positive floor effect per floor (Bounty/LuckyItems/PencilRefill/HealingPath). (7) Boss modifier preview hint on path map nodes. (8) End-of-run score breakdown (base XP × bonuses − mistake penalty). |
| 1.4 | 2026-03-28 | implemented | Game flow: Start Game → Class Select → Garden Path (path overview shows before first puzzle). All submenus are full-screen opaque overlays. Locked classes display unlock conditions. 7-star requires ≥1 modifier. Hint/Undo features removed; Q = Save & Quit. |
| 1.3 | 2026-03-24 | implemented | Localization system (EN/DE): LocalizationService with dictionary-based translation. Language selector rebuilds all menu UI. Class select info panel moved to dedicated dark box between button grid and bottom controls. Game launch fixed: GameBootstrap.BindRunToMap() wires RunDirector→RunMapController→first level start. |
| 1.2 | 2026-03-24 | implemented | UI polish: Meta Progression panel shows all 8 classes with level/prestige/XP or "Locked" state. Class Select panel highlights selected class with gold outline and displays HP/Pencil/Slots/Passive attributes. Resume Game button disabled when no save exists. Main menu color scheme: cream buttons, dark text, gold accents. |
| 1.1 | 2026-03-23 | implemented | Floor modifier system: floors 2–5 apply persistent modifiers to all puzzles (not just boss). Boss choice scaling unchanged (N from N+1). Implemented in RunDirector, BossService, and UI. |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## 1) Vision

A serene but tense Sudoku roguelike where each solved board is a deeper step into a Japanese garden journey. The game uses calm visual presentation and strict mechanical pressure from HP loss and limited pencilmarks.

### Theme & Art Direction

- Pixel art visual language (16x16 icons, procedural UI)
- Palette: calm greens, stone neutrals, lantern gold for reward highlights
- Ambient effects: light petals, water ripple loops, subtle lantern glows
- Inspirations: Kyoto gardens, Ryoan-ji minimalism, Kinkaku-ji elegance

---

## 2) Core Loop

1. Start run with chosen class
2. Enter garden floor (5 floors per run) — a **positive floor effect** may apply to all puzzles this floor
3. Choose Calm or Risk route through branching path
4. Encounter a node — may be: Puzzle, Rest (3-way choice), Relic (3-of-3 choice), Shop, Elite, or Cursed (Accept/Decline)
5. Solve Sudoku puzzles under HP + Pencil constraints — **combo streak** tracked per correct tile
6. Gain Gold + XP on completion (multiplied if Cursed was accepted or Bounty floor effect active)
7. Item reward phase (pick one slot or Nothing; +1 slot if LuckyItems floor effect active)
8. Optional reroll of eligible slots
9. Reach boss gate → preview boss hint on map → choose modifiers → solve boss puzzle
10. Advance to next floor or complete run
11. End-of-run screen: XP breakdown, **run score breakdown**, level-up animation, class progression

Run failure occurs when HP reaches 0. XP is still awarded at the game over screen.

---

## 3) Board Difficulty System

### 3.1 Structural Tier (Board Size)

| Board Size | Floor Range |
|:---:|-------------|
| 5×5 | Floor 1 |
| 6×6 | Floor 1–2 |
| 7×7 | Floor 2–3 |
| 8×8 | Floor 3–4 |
| 9×9 | Floor 4–5 |

### 3.2 Star Density (Missing Cells)

Stars control how many cells are removed from the solved board:

| Stars | Missing % | Formula |
|:---:|:---:|---------|
| 1★ | 40% | `(1+3)×0.1` |
| 2★ | 50% | `(2+3)×0.1` |
| 3★ | 60% | `(3+3)×0.1` |
| 4★ | 70% | `(4+3)×0.1` |
| 5★ | 80% | `(5+3)×0.1` |
| 6★ | 90% | `(6+3)×0.1` |
| 7★ | 100% | Tutorial only — 0 givens |

Formula: `MissingPercent = (stars + 3) × 0.1`, clamped to `[0.01, 0.95]` (except 7★ tutorial).

### 3.3 Region Variants

Board regions can use different layouts for visual and mechanical variety:

| Variant | Description | Available Sizes |
|:---:|-------------|-----------------|
| 0 | Standard rectangular regions | All |
| 1 | Alternate rectangular (rotated box dimensions) | 6×6, 8×8, 9×9 |
| 2 | Irregular template A | 5×5–9×9 |
| 3 | Jigsaw / Irregular template B | 5×5–9×9 |

Irregular puzzles are controlled by `RunState.AllowIrregularPuzzles` toggle (default: true). When disabled, only variants 0–1 are used.

---

## 4) Controls Specification

### 4.1 Selection

- Mouse click/drag: select cells
- CTRL + click/drag: add/remove from selection
- Arrow keys: move selection cursor
- CTRL/SHIFT + Arrow: expand selection
- CTRL + A: select all
- CTRL + SHIFT + A: deselect all
- CTRL + I: invert selection
- Double click / long tap: tool-sensitive select

### 4.2 Number Input

- Keyboard `0-9`
- Numpad keys
- On-screen numpad buttons

### 4.3 Pencil Mode

- Toggle by hotkey (default `P`)
- UI toggle button mirrored with keyboard state
- Each pencilmark consumes 1 Pencil Unit

---

## 5) Resource Systems

### 5.1 HP

- Wrong number placement: `-1 HP` (unless modified by class passive, relic, or item)
- HP = 0: run ends (unless Phoenix Feather relic triggers)
- Base HP is class-dependent (range: 7–14)

### 5.2 Pencil Units

- Each pencilmark consumes 1 Pencil Unit
- If units = 0, pencilmark placement is blocked
- Pencil units are limited per run
- Awarded after level completion
- Can be bought with Gold during level

### 5.3 Gold

Run-based currency, reset each run.

**Base Gold per Board Size:**

| Board Size | Base Gold |
|:---:|:---:|
| 5×5 | 20 |
| 6×6 | 30 |
| 7×7 | 45 |
| 8×8 | 65 |
| 9×9 | 100 |

**Reward Formula:**
```
Gold = BaseDifficultyGold × (1 + stars × 0.2)
```

**Spend Formulas:**
- Pencil buy cost (during level): `20 + (20 × timesPurchasedThisRun)`
- Reroll cost (post-level): `20 + (20 × timesRerolledThisRun)`

---

## 6) Classes

8 playable classes with unique base stats, passive abilities, and unlock conditions. See [ClassXpProgressionSystem](ClassXpProgressionSystem_implemented.md) for full XP curve, level rewards, and prestige system.

### 6.1 Class Roster

| # | Class | HP | Pencil | Slots | Passive | Unlock Condition |
|:---:|-------|:---:|:---:|:---:|---------|-----------------|
| 1 | Number Freak | 10 | 10 | 2 | Balanced — +1 Reroll Token | Default (unlocked) |
| 2 | Garden Monk | 14 | 5 | 1 | Every 5 correct placements → +1 HP. Cannot buy Pencil mid-level. | Defeat 2 bosses |
| 3 | Shrine Archivist | 8 | 15 | 2 | First pencilmark in each cell is free | Use 15 items (cumulative) |
| 4 | Koi Gambler | 9 | 8 | 2 | 25% chance wrong placement costs 0 HP; 25% chance correct placement grants +1 Gold | Collect 10 relics (cumulative) |
| 5 | Stone Gardener | 11 | 8 | 3 | First item used each level is not consumed | Defeat 10 bosses (cumulative) |
| 6 | Lantern Seer | 7 | 12 | 2 | Boss modifiers are 20% weaker | Collect 50,000 gold (cumulative) |
| 7 | Reed Duelist | 9 | 9 | 2 | Perfect no-pencil tile bonus: +2 Pencil. Speed-focused — rewards clean, decisive play. | Find every unique item at least once (codex complete) |
| 8 | Quiet Cartographer | 12 | 6 | 1 | After solving a tile with no mistakes, preview the next tile's star rating and board size. Route-focused — rewards methodical planning. | Clear a full stage with no pencil use and no HP loss |

### 6.2 Level Reward Example (Number Freak)

- L3: +1 Pencil
- L7: +2 HP
- L10: +2 Pencil, +2 HP
- L15: +1 Reroll
- L20: +1 Item Slot
- L30: Start run with 1 Rare Solver
- Max Level: 40

---

## 7) Item & Relic System

See [ItemsAndRelicsSystem](ItemsAndRelicsSystem.md) for the full item table, relic pool, pricing, codex, and art prompts.

### Summary

- **Items:** Single-use consumables in 3 rarities (Normal / Rare / Epic). 8 tiered item families + 10 unique items. Sold in the Shop and awarded as puzzle rewards.
- **Relics:** Passive permanent-for-run effects. Player holds one relic at a time. 23 relics across 5 tiers (Tier 1 → Legendary). Acquired from Relic tile nodes (3-of-3 choice), Elite rewards, and Run events — **not sold in the Shop**.
- **Codex:** Unified item + relic discovery tracker. Discovery triggers on first acquisition.

---

## 8) Post-Level Item Roll Phase

Reward slots scale with star difficulty:

| Star Rating | Reward Slots |
|:---:|:---:|
| 1★ | 2 |
| 2★ | 3 |
| 3★ | 3 |
| 4★ | 4 |
| 5★ | 5 |

Rules:
- Player picks exactly ONE slot, or chooses Nothing
- Nothing slot grants a small gold bonus
- Picked and Nothing slots become locked
- Only unpicked, non-Nothing slots are reroll-eligible
- If inventory full (4★+): replace existing item or choose Nothing

---

## 9) XP and Progression

See [ClassXpProgressionSystem](ClassXpProgressionSystem_implemented.md) for the full XP curve, per-run formula, and level-up display.

### Summary

- XP is per-class, stored as single `TotalXp` integer, level derived at runtime
- Two-phase curve: L1–15 linear, L16–40 accelerating. Total to L40: 16,860 XP
- Target: ~80 runs to reach Level 40
- XP formula per tile: `BaseXP(boardSize) × StarMult + ModBonus + BossMult + PerfectBonus`
- Level-up displayed only at game over / completion screen
- Prestige system: max tier 9, requires L40 + boss gates

---

## 9b) In-Run Feedback Systems

### Combo Counter
- Correct tile placements increment `RunState.ComboStreak` by 1 each.
- Any mistake resets `ComboStreak` to 0.
- When `ComboStreak ≥ 2`, a **gold label** appears in the puzzle HUD showing `"Combo ×N!"`.
- Peak combo across the run is stored in `RunState.PeakComboThisRun`.

### Class Passive Label
- A small label below the HUD shows the active class's passive ability description (from `ClassCatalog`).
- Displayed at all times during puzzle solving — serves as a persistent reminder.

### Rest Node — 3-Way Choice
Rest tiles present the player with three options instead of automatically healing:

| Option | Effect |
|--------|--------|
| **Heal** | Restore `+2 HP` (capped at max HP) |
| **+Pencil** | Restore `+3 Pencil` |
| **+Reroll Token** | Grant `+1 Reroll Token` for the next item reward screen |

Only one option may be chosen. The panel dismisses after selection.

### Boss Modifier Preview Hint
- Boss Gate nodes on the path map display a tooltip/hint showing the floor's active modifiers and expected intensity level.
- Format: `"Floor N Boss | Floor mods: <name>, <name> | Intensity: <level>"`.
- Returned by `RunMapController.GetBossNodePreviewHint()`.
- Intensity label: Low (runs 1–2), Medium (runs 3–5), High (runs 6–8), VeryHigh (runs 9+).

---

## 9c) Post-Run Score

A **run score** is calculated at the end of every run (victory or defeat) and shown on the end screen.

### Score Formula

```
BaseScore  = Total XP earned across all tiles
PerfectBonus = +5% per puzzle completed without any mistakes
CursedBonus  = +10% per Cursed puzzle accepted and completed
VictoryBonus = +25% if the run was completed (Floor 5 boss defeated)
MistakePenalty = −2% per mistake, capped at −50%

FinalScore = BaseScore × (1 + PerfectBonus + CursedBonus + VictoryBonus + MistakePenalty)
```

### Breakdown Display

The end screen shows an itemized breakdown:
```
Run Score Breakdown
  Base (XP earned):    1 200
  Perfect puzzles ×3:  +180   (15%)
  Cursed accepted ×2:  +240   (20%)
  Victory bonus:       +300   (25%)
  Mistakes ×8:         −192   (−16%)
  ──────────────────────────
  Final Score:         1 728
```

- Tracked in `PostRunAnalyticsService`: `PerfectPuzzleCount`, `CursedPuzzlesCompleted`, `RunScore`.
- Stored in `RunResult`: `PerfectPuzzleCount`, `CursedPuzzlesAccepted`, `RunScore`.
- Score of 0 (e.g. immediate game over) suppresses the breakdown section.

---

## 10) Run Structure — 5-Floor Garden

See [PathSystem_GardenOverview](PathSystem_GardenOverview_implemented.md) for full path routing, floor themes, and layout algorithm.

### Summary

- 5 floors per run, each with a Japanese garden theme
- Dual-path routing: Calm (longer, safer) and Risk (shorter, harder, better rewards)
- One cross-edge per floor connecting Calm ↔ Risk
- Node types: Puzzle, Elite, Shop, Relic, Event, Boss
- **Floor modifiers**: floors 2–5 apply persistent modifiers to **all** puzzles on that floor (not just boss). Count increases per floor (0 → 1 → 2 → 3 → 4). Rolled automatically on floor entry from full modifier pool.
- Boss gate at end of each floor with modifier choice scaling (Floor N → pick N from N+1). Boss puzzles stack floor modifiers + boss-chosen modifiers.
- Floor transition: Garden Cleared card → cutscene → fade-in

### Floor Themes

| Floor | Theme | Grid Size Range |
|:---:|-------|:---:|
| 1 | Bamboo Courtyard | 5×5–6×6 |
| 2 | Moss Garden | 6×6–7×7 |
| 3 | Koi Terrace | 7×7–8×8 |
| 4 | Stone Lantern Walk | 8×8–9×9 |
| 5 | Shrine Summit | 9×9 |

---

## 11) Boss System

See [BossMechanicsSystem](BossMechanicsSystem.md) for the full 15-modifier pool, intensity scaling, geometry generation, overlay rendering, and legend panel.

### Summary

- 15 implemented modifiers: 7 line, 2 dot, 2 arithmetic, 1 cell-level, 2 global negative, 1 visibility
- All 15 eligible at every floor (no tier gating). Board size restrictions apply.
- **Floor modifiers** (persistent per floor, applied to all puzzles including boss):

| Floor | Floor Modifiers | Boss Options Shown | Boss Player Picks |
|:---:|:---:|:---:|:---:|
| 1 | 0 | 3 | 2 |
| 2 | 1 | 3 | 2 |
| 3 | 2 | 4 | 3 |
| 4 | 3 | 5 | 4 |
| 5 | 4 | 6 | 5 |

- Boss puzzles have **floor modifiers + boss-chosen modifiers** active simultaneously
- Intensity scales with run number: Low (runs 1–2) → VeryHigh (runs 9+)
- Modifier legend panel with tap-to-isolate for visual clarity
- Puzzles are not required to be logically solvable — items and relics compensate

---

## 12) Tutorial Mode

See [TutorialModeSystem](TutorialModeSystem.md) for sandbox configuration, modifier toggles, and 7★ rules.

### Summary

- Isolated sandbox with no progression (no XP, no gold, no unlocks)
- Player configures: board size (5–9), star difficulty (1–7), region variant, modifier toggles
- 7★ = 100% missing (0 givens, tutorial only)
- All 15 modifiers available as individual toggles
- Tutorial completion unlocks achievements only (no gameplay progression)

---

## 13) Additional Modes

### Endless Zen

- Unlocked after completing a full 5-floor run
- All board sizes, all 15 modifiers eligible (board size restrictions apply)
- Depth < 10: 1 random modifier per level
- Depth ≥ 10: 2 random modifiers per level
- No item drops, minimal gold
- XP leaderboard focus

### Spirit Trials

- Fixed 9×9, baseline 3★
- Timer active, no gold, limited items
- Score: `BasePoints × SpeedMultiplier × ConstraintBonus − MistakePenalty`

---

## 14) Menus and UX Structure

### Main Menu

- Start Game
- Resume Game (only when run in progress)
- Tutorial
- Meta Progression
- Game Modes (Endless Zen / Spirit Trials)
- Items (Codex — items + relics)
- Options
- Credits
- Quit Game

### Start Game Flow

1. Class Select (locked classes shown disabled with unlock condition)
2. Seed Select (random or entered seed)
3. Allow Irregular Puzzles toggle
4. Launch run

### Options Structure

- Language (English / German)
- Audio (Master / Music / SFX / UI volumes)
- Graphics (resolution, fullscreen/window/borderless, VSync, frame limit)
- Gameplay (confirm wrong placement, auto pencil cleanup, conflict highlights)
- Accessibility (colorblind, high contrast, font scaling, reduce motion)

### In-Run Pause Menu

- Resume
- Options
- View Modifiers
- View Seed
- Save & Quit (returns to main menu, run resumes later)
- Abandon Run

### End Screens

**Game Over / Run Complete:**
- XP breakdown per tile (bar filling animation, skippable with Space)
- Level-up display (only shown here)
- Gold earned
- Floor reached
- Mistakes made
- Items used / relics held

---

## 15) Balancing Guardrails

- Preserve early clarity: low stars + low modifier complexity in first runs
- Keep Pencil scarcity meaningful; avoid runaway refill loops
- Prevent reroll economy abuse with escalating costs and lock rules
- Keep mistake penalties readable and predictable
- Do not stack too many hidden constraints in early routes
- Equal modifier pool rolling (no difficulty weighting)
- Puzzles not required to be logically solvable — items/relics compensate

---

## 16) Extensibility Requirements

All of these must be data-driven, not hard-coded:

- Classes and passives
- Item definitions and rarity behavior
- Star levels and missing-cell density mapping
- Modifier pools (all 15 + future additions from Extended Modifier Library)
- Route node definitions and rewards
- Relic effects and tier assignments

---

## 17) Cross-References

| System | Respec Document |
|--------|-----------------|
| Path & Garden | [PathSystem_GardenOverview](PathSystem_GardenOverview_implemented.md) |
| Class XP & Progression | [ClassXpProgressionSystem](ClassXpProgressionSystem_implemented.md) |
| Items & Relics | [ItemsAndRelicsSystem](ItemsAndRelicsSystem.md) |
| Tutorial Mode | [TutorialModeSystem](TutorialModeSystem.md) |
| Boss Mechanics | [BossMechanicsSystem](BossMechanicsSystem.md) |
| Sudoku Generation | [SudokuGenerationPipeline](SudokuGenerationPipeline.md) |
| Gold Economy | [GoldEconomySystem](GoldEconomySystem.md) |
| Save & Resume | [SaveLoadArchitecture](SaveLoadArchitecture.md) |
| Meta Progression | [MetaProgressionSystem](MetaProgressionSystem.md) |
| Spirit Trials | [SpiritTrialsMode](SpiritTrialsMode.md) |
| Endless Zen | [EndlessZenMode](EndlessZenMode.md) |
| Audio & Visual | [AudioVisualDirection](AudioVisualDirection.md) |
| Accessibility | [AccessibilitySpec](AccessibilitySpec.md) |
| Unity Implementation | [UnityImplementationSpec](UnityImplementationSpec.md) |

---

## Open Issues

No open issues remaining.



---

---

---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Code File | Verified Classes | Status |
|--------|-------|-----------|-----------------|--------|
| REQ-GENERAL-013 | TITLE | `—` | — | ✗ missing |
| REQ-GENERAL-014 | RunStartSequenceSystem | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-GENERAL-015 | ClassSelectionScreen | `—` | — | ✗ missing |
| REQ-GENERAL-016 | TITLE | `—` | — | ✗ missing |
| REQ-GENERAL-017 | GameStateController | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-GENERAL-018 | FloorSelectionScreen | `Assets\Scripts\UI\MainMenuController.cs` | — | ✓ verified |
| REQ-GENERAL-019 | CoreLoopSudokuPuzzle | `Assets\Scripts\Run\RunDirector.cs` | — | ✓ verified |
| REQ-GENERAL-020 | HpUnitsResource | `Assets\Scripts\Economy\RelicService.cs` | — | ✓ verified |
| REQ-GENERAL-021 | PencilUnitsResource | `Assets\Scripts\Run\RunDirector.cs` | — | ✓ verified |
| REQ-GENERAL-022 | GoldResource | `—` | — | ✗ missing |
| REQ-GENERAL-023 | ItemAndRelicManagement | `Assets\Scripts\Run\RunDirector.cs` | — | ✓ verified |
| REQ-GENERAL-024 | XpProgressionSystem | `Assets\Scripts\Economy\XpService.cs` | — | ✓ verified |
| REQ-GENERAL-025 | PostLevelItemRollPhase | `Assets\Scripts\Run\RunDirector.cs` | — | ✓ verified |
| REQ-GENERAL-026 | CoreLoopBosses | `Assets\Scripts\Run\SpiritTrialsService.cs` | — | ✓ verified |
| REQ-GENERAL-027 | PathSystem | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-GENERAL-028 | ClassXpProgressionSystem | `Assets\Scripts\UI\MainMenuController.cs` | — | ✓ verified |
| REQ-GENERAL-029 | UiController | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
