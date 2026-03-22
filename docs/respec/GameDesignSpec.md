# Run of the Nine — Game Design Specification

**Version:** 1.0 | **Date:** 2026-03-21 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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
2. Enter garden floor (5 floors per run)
3. Choose Calm or Risk route through branching path
4. Solve Sudoku puzzles under HP + Pencil constraints
5. Gain Gold + XP on completion
6. Item reward phase (pick one slot or Nothing)
7. Optional reroll of eligible slots
8. Reach boss gate → choose modifiers → solve boss puzzle
9. Advance to next floor or complete run
10. End-of-run screen: XP breakdown, level-up animation, class progression

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

- **Items:** Single-use consumables in 3 rarities (Normal / Rare / Epic). 8 tiered item families + 10 unique items.
- **Relics:** Passive permanent-for-run effects. Player holds one relic at a time. 20 relics across 5 tiers (Tier 1 → Legendary).
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

## 10) Run Structure — 5-Floor Garden

See [PathSystem_GardenOverview](PathSystem_GardenOverview_implemented.md) for full path routing, floor themes, and layout algorithm.

### Summary

- 5 floors per run, each with a Japanese garden theme
- Dual-path routing: Calm (longer, safer) and Risk (shorter, harder, better rewards)
- One cross-edge per floor connecting Calm ↔ Risk
- Node types: Puzzle, Elite, Shop, Relic, Event, Boss
- Boss gate at end of each floor with modifier choice scaling (Floor N → pick N from N+1)
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
- Choice scaling: Floor N offers N+1 options, player picks N
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

---

## Open Issues

No open issues remaining.

