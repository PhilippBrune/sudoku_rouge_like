# Run of the Nine — Game Design Specification

**Version:** 1.16 | **Date:** 2026-10-21 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.16 | 2026-10-21 | implemented | Options Structure: the Options are a hand scroll with six pages (UI-STYLE-017); Accessibility, Controls and Language are pages, the separate Accessibility panel is gone. |
| 1.15 | 2026-10-21 | implemented | Options audit after the 0.2.0 playtest (GAP-061): every option now does what its name says (Options Effects, OPT-GAME-001..005, OPT-GFX-001, OPT-AUDIO-001, OPT-LABEL-001, ACCESS-COLOR-003, ACCESS-FONT-001); "Confirm Wrong Placement" removed; Show Digits Left, Jump to Next Empty Cell, Board Brightness/Contrast and Status Captions renamed. |
| 1.14 | 2026-10-21 | implemented | Owner picks after the 0.2.0 playtest: Pencil mode shows in indigo ink on the number pad and in the pencil marks (4.3, UI-STYLE-020). Run score (9c): every puzzle gets a grade from its own mistakes (Perfect, Clean, Rough, Messy; proposal C of `design/mistake-scoring-proposals.md`), which replaces the flat mistake penalty and the perfect bonus; the puzzle that ends a run is Messy; the end screen shows one stamp per puzzle (SCORE-GRADE-001..004). Unspent gold adds a point per 5 gold, at most 15 % (SCORE-GOLD-001, lever L5 of `design/gold-and-items-concept.md`). Economy levers L1 and L6 (GoldEconomySystem 2.2): from Floor 4 a mistake costs at least 2 HP (5.1, ECON-BAL-003) and the rest heals a quarter (ECON-BAL-004). Garden flow D: entering a floor shows its painting (UI-STYLE-021). |
| 1.13 | 2026-10-20 | implemented | Owner's 0.2.0 playtest: a placed digit highlights every cell with that digit (4.1); digits are taken 0.25 s after a cell is chosen, half of before (4.2); the main menu shows the version (UI-MENU-VERSION-001); every mistake counts toward the run score when it happens (9c, GAP-059). |
| 1.12 | 2026-10-20 | implemented | Post-run score (9c): rounding stated — each row is its share of the base rounded to whole points, and the final score is the base plus the rows, so the breakdown adds up. The code now adds the bonuses as the formula says; it had multiplied them one after another (GAP-053), and cleared cursed puzzles now count (GAP-054). The 71 player modifiers are chosen in the Custom Puzzle rule book (UI-STYLE-012). |
| 1.11 | 2026-10-19 | implemented | 43 player modifiers: the catalog batches B1 and B2 (13 modifiers) are live (BossMechanicsSystem 2.8). |
| 1.10 | 2026-10-15 | implemented | Seed of the week: one Garden Run seed per ISO week for all players, a button in the start-run modal (RUN-WEEK-001); the Discord posts compute the Daily Walk goals, the weekly seed and the Monthly Walk board exactly like the game (RUN-WEEK-002). Credits name community playtesters and translators and link the Discord (CREDITS-COMMUNITY-001). |
| 1.9 | 2026-10-14 | implemented | Options panels (UI-OPT-001..003): the in-run Options use the main Options' tabs, controls and look (one factory, OptionsUiFactory), plus Accessibility and Run tabs; every control is reachable with the D-pad. Options Structure updated to the built tabs. Closes GAP-029 and FUTURE-015. Evidence: GeneratedUiPlayModeTraversalTests, GeneratedUiWiringContractTests, screenshots (E2E report §10.2f). |
| 1.8 | 2026-10-14 | implemented | Window title "Run of the Nine" in every display mode (DISPLAY-MODE-007), set by the Windows player because the product name keeps the released save folder (SaveLoadArchitecture §Save folder). Evidence: build check B23 (E2E report §10.2d). |
| 1.7 | 2026-10-12 | implemented | Display modes specified and implemented (DISPLAY-MODE-001..006): Fullscreen, Borderless Fullscreen, Windowed and the new Frameless Window; window sizing, centring, save compatibility and the Fullscreen toggle rule. Evidence: DisplayModeTests, GeneratedUiPlayModeTraversalTests, build check B16–B19 (E2E report §10). |
| 1.6 | 2026-10-07 | implemented | 1.1 review decisions (2026-10-07): wrong digit = differs from the solution, rejected, mistake HP cost by Harmony (SPEC-CONF-003); pencil purchase and item-reward reroll implemented (SPEC-CONF-029); reward slot/Nothing table incl. 6★, guarantee and Nothing gold 5 × floor (SPEC-AMB-001, SPEC-AMB-007); Rest values from code + Cleanse (SPEC-CONF-017); floor modifiers 0/1/1/1/2 and boss choices floor+1/floor (SPEC-CONF-001/002); one cross-edge, tiles keep type (SPEC-CONF-010); boss boards follow floor range (SPEC-CONF-028); 30 modifiers (SPEC-CONF-019); §6 roster reduced to a link to ClassXpProgressionSystem (SPEC-CONF-008); §13 Endless Zen and Spirit Trials point to their mode docs (SPEC-CONF-006/007); multi relics, shop items only (SPEC-CONF-004/005); 5 languages, Custom Puzzle naming, main-menu Daily/Monthly widgets, start-run seed and abandon warning, in-run seed/View Modifiers/Abandon (SPEC-CONF-027/031/032/033, GAP-020/032); multi-cell selection moved to future scope (SPEC-MAC-005); links to renamed docs fixed; SPICE table archived (SPEC-OBS-001). |
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

Irregular puzzles are controlled by `RunState.AllowIrregularPuzzles` toggle (default: true; the class-select toggle and start-run confirmation also default to ON since 1.1, SPEC-CONF-031). When disabled, only variants 0–1 are used. Levels with a global structural modifier always use the rectangular layout (BossMechanicsSystem §Modifier Eligibility).

---

## 4) Controls Specification

### 4.1 Selection

- Mouse click / tap: select one cell; clicking a filled cell highlights all cells with the same digit
- Placing a correct digit highlights all cells with that digit, as clicking it would (0.2.0 playtest)
- Arrow keys / D-pad: move the selection cursor
- Multi-cell selection (CTRL/SHIFT + click or arrows, select all, deselect all, invert) is **not built** and is moved to future scope (decided 2026-10-07, SPEC-MAC-005; `future-scope-decision-register.md`). Only single-cell selection exists.

### 4.2 Number Input

- Keyboard `0-9`
- Numpad keys
- On-screen numpad buttons
- Touch safety: after a cell is chosen, digit input is ignored for 0.25 s (0.5 s before the 0.2.0 playtest)

### 4.3 Pencil Mode

- Toggle by hotkey (default `P`)
- UI toggle button mirrored with keyboard state
- Each pencilmark consumes 1 Pencil Unit
- In Pencil mode the number pad turns indigo ink with paper-white digits, and the pencil marks on the board are written in
  the same indigo, so the mode is visible at a glance (UI-STYLE-020, owner pick A after the 0.2.0 playtest)

---

## 5) Resource Systems

### 5.1 HP

- Wrong number placement: a digit that differs from the stored solution is **wrong**, even without a visible conflict. It is rejected (never written to the board) and costs the mistake HP cost: 1, 2 from Harmony H5, 3 from H9 (unless modified by class passive, relic, item or debuff). Re-entering the digit a cell already holds changes nothing. Custom Puzzle 7★ boards have no unique solution and stay rule-based. (Decided 2026-10-07, SPEC-CONF-003; every in-game puzzle has exactly one solution.)
- **From Floor 4 of a Garden Run a mistake costs at least 2 HP** at every Harmony level (ECON-BAL-003, lever L6 of
  `design/gold-and-items-concept.md`); Double Penalty doubles that. The floor's entry painting says so ("Here a mistake
  costs 2 HP").
- A mistake is saved immediately, so quitting cannot undo it (GAP-031).
- **Perfect puzzle** (SPEC-AMB-013): no wrong digit was entered in the puzzle. A mistake whose HP loss was absorbed (relic, Rice Paper Umbrella, shield, Koi Gambler) still breaks "perfect". The XP Perfect bonus, the Healing Path floor effect and the curse auto-cleanse all use this one definition (`LevelState.Mistakes == 0`).
- HP = 0: run ends (unless Phoenix Feather relic triggers)
- Base HP is class-dependent (range: 7–14)

### 5.2 Pencil Units

- Each pencilmark consumes 1 Pencil Unit
- If units = 0, pencilmark placement is blocked
- Pencil units are limited per run
- Awarded after level completion
- Can be bought with Gold during a level: the "+3 Pencil" button next to the pencil bar adds 3 Pencil (up to the maximum) for `20 + 20 × pencil purchases this run` gold. Not available when Pencil is full, in tutorials or in the Monthly Walk; Garden Monk cannot buy Pencil mid-level (implemented 1.1, SPEC-CONF-029, GAP-016).

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
- Reroll cost (post-level item reward): a Reroll Token if the player has one, otherwise `20 + (20 × rewardRerollsThisRun)` gold; rerolls paid with tokens count toward n (implemented 1.1, SPEC-CONF-029, GAP-015)

---

## 6) Classes

8 playable classes with unique base stats, passive abilities, level schedules and unlock conditions. The authoritative roster (base stats, passives, unlock conditions, level schedule, prestige) is [ClassXpProgressionSystem](ClassXpProgressionSystem.md), which documents `ClassCatalog` (decided 2026-10-07, SPEC-CONF-008). The class screen and the start-run confirmation show run-start stats as "base (+level bonus)", e.g. "HP 10 (+4)" (GAP-023).

## 7) Item & Relic System

See [ItemsAndRelicsSystem](ItemsAndRelicsSystem.md) for the full item table, relic pool, pricing, codex, and art prompts.

### Summary

- **Items:** Single-use consumables in 3 rarities (Normal / Rare / Epic). 8 tiered item families + 10 unique items. Sold in the Shop and awarded as puzzle rewards.
- **Relics:** Passive permanent-for-run effects. Runtime inventory uses `HeldRelics`, so players can hold multiple relics during a run (no cap; RELIC-SLOT-001 is deprecated, SPEC-CONF-004). Acquired from starting relic assignment, relic choices, Elite rewards, and Run events — **not sold in the Shop**.
- **Shop:** sells items only, 2/2/3/3/4 offers on floors 1–5 (SPEC-CONF-005). Buying with a full bag needs a slot to replace; nothing is charged otherwise (GAP-036).
- **Codex:** Unified item + relic discovery tracker. Discovery triggers on first acquisition.

---

## 8) Post-Level Item Roll Phase

Reward slots scale with star difficulty:

| Star Rating | Reward Slots | Nothing chance per slot |
|:---:|:---:|:---:|
| 1★ | 2 | 25 % |
| 2★ | 3 | 22 % |
| 3★ | 3 | 18 % |
| 4★ | 4 | 15 % |
| 5★–6★ | 5 | 12 % |

Rules:
- At least one slot always holds an item: the last slot cannot become Nothing while the others are Nothing. Harmony's extra Nothing chance never empties a reward either (SPEC-AMB-001).
- Player picks exactly ONE slot, or chooses Nothing / Skip
- Nothing (slot or Skip) grants `5 × floor number` gold (5 on Floor 1 … 25 on Floor 5; none in tutorials or the Monthly Walk) (decided 2026-10-07, SPEC-AMB-007)
- Reroll (before picking): replaces every slot that holds an item; Nothing slots stay. Cost: a Reroll Token, otherwise `20 + 20n` gold (§5.3)
- If inventory full: replace an existing item or choose Nothing

---

## 9) XP and Progression

See [ClassXpProgressionSystem](ClassXpProgressionSystem.md) for the full XP curve, per-run formula, and level-up display.

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

### Rest Node — Choice
Rest tiles present the player with these options instead of automatically healing (code values documented 2026-10-07, SPEC-CONF-017):

| Option | Effect |
|--------|--------|
| **Heal** | Restore `max(1, MaxHP / 4)` HP (capped at max HP; halved, rounded up, at Harmony H10). The heal scales with the class's HP pool, so Rest stays useful on later floors. A quarter since 2026-10-21 (ECON-BAL-004, lever L6); it was a third. |
| **+Pencil** | Restore `+4 Pencil` (capped at max Pencil) |
| **+Reroll Token** | Grant `+1 Reroll Token` (used by shop and item-reward rerolls) |
| **Cleanse** | Remove the oldest active curse (shown only while a curse is active, CurseSystem) |

Only one option may be chosen. The panel dismisses after selection.

### Boss Modifier Preview Hint
- Boss Gate nodes on the path map display a tooltip/hint showing the floor's active modifiers and expected intensity level.
- Format: `"Floor N Boss | Floor mods: <name>, <name> | Intensity: <level>"`.
- Returned by `RunMapController.GetBossNodePreviewHint()`.
- Intensity label: Low (runs 1–2), Medium (runs 3–5), High (runs 6–8), VeryHigh (runs 9+).

---

## 9c) Post-Run Score

A **run score** is calculated at the end of every run (victory or defeat) and shown on the end screen.

### Puzzle grades (SCORE-GRADE-001..004)

Every puzzle of the run gets a grade from its own mistakes (owner pick C, 2026-10-20):

| Grade | Mistakes on that puzzle | Run-score effect |
|---|---|---|
| Perfect | 0 | +5 % |
| Clean | 1 | 0 |
| Rough | 2–3 | −2 % |
| Messy | 4 or more | −5 % |

- **SCORE-GRADE-001** The grade is set when the puzzle is solved, from that puzzle's mistakes (a mistake taken back by the
  Ginkgo Leaf does not count). It replaces the old flat penalty (−2 % per mistake) and the perfect bonus.
- **SCORE-GRADE-002** The puzzle that ends a Garden Run unsolved (HP lost, or the run abandoned inside it, also from the
  main menu's "abandon the saved run") counts as Messy. Endless Zen, Spirit Trials and the Monthly Walk end their sessions
  inside a puzzle by design, so their last board is not graded.
- **SCORE-GRADE-003** The end screen shows one stamp per puzzle in the order played, floors apart, each in its grade's
  colour with its letter, and a legend; the text breakdown lists the letters ("P C P | R P M").
- **SCORE-GRADE-004** Rough and Messy together cost at most 50 % of BaseScore.

### Gold kept (SCORE-GOLD-001)

Gold still held when the run ends adds one point per 5 gold, at most 15 % of BaseScore (lever L5 of
`design/gold-and-items-concept.md`). It is its own ledger row, so spending now and scoring later are a visible choice.

### Score Formula

```
BaseScore    = Total XP earned across all tiles
PerfectBonus = +5% per Perfect puzzle
RoughPenalty = −2% per Rough puzzle  ┐ together at most −50%
MessyPenalty = −5% per Messy puzzle  ┘
CursedBonus  = +10% per Cursed puzzle accepted and completed
VictoryBonus = +25% if the run was completed (Floor 5 boss defeated)
GoldKept     = +1 point per 5 gold held at the end, at most 15% of BaseScore

FinalScore = BaseScore × (1 + PerfectBonus + RoughPenalty + MessyPenalty + CursedBonus + VictoryBonus) + GoldKept
```

Each row of the breakdown is its percentage of BaseScore, rounded to whole points (halves away from zero); FinalScore is
BaseScore plus the rows, so the shown breakdown always adds up and may differ from the exact product by a point or two
(`RunScoreBreakdown`, GAP-053). "Cursed" counts cursed puzzles accepted and completed (GAP-054).

### Breakdown Display

The end screen shows an itemized breakdown (base 1 200; seven puzzles with 0, 0, 0, 1, 2, 3 and 5 mistakes; two cursed
clears; victory; 212 gold kept):
```
Run Score Breakdown
  Base (XP earned):    1 200
  Perfect puzzles ×3:  +180   (15%)
  Rough puzzles ×2:     −48   (−4%)
  Messy puzzles ×1:     −60   (−5%)
  Cursed clears ×2:    +240   (20%)
  Victory bonus:       +300   (25%)
  Gold kept (212):      +42
  ──────────────────────────
  Final Score:         1 854
  Puzzles: P P P C R R M
```

- Tracked in `PostRunAnalyticsService`: each solved puzzle's mistakes and floor, `CursedPuzzlesCompleted`, `RunScore`;
  `PostRunAnalytics.PuzzleGrades` and `GoldLeft` carry the grades and the gold to the end screen. Each mistake is counted
  when it happens, also in the puzzle that ends the run (GAP-059).
- Stored in `RunResult`: `PerfectPuzzleCount`, `CursedPuzzlesAccepted`, `RunScore`.
- Score of 0 (e.g. immediate game over) suppresses the breakdown section.

---

## 10) Run Structure — 5-Floor Garden

See [PathSystem_GardenOverview](PathSystem_GardenOverview.md) for full path routing, floor themes, and layout algorithm.

### Summary

- 5 floors per run, each with a Japanese garden theme
- Dual-path routing: Calm (longer, safer) and Risk (shorter, harder, better rewards)
- One cross-edge per floor connecting Calm ↔ Risk; the two linked tiles keep their type (SPEC-CONF-010)
- Node types: Puzzle, Elite, Shop, Relic, Event, Boss
- **Floor modifiers**: floors 2–5 apply persistent modifiers to **all** puzzles on that floor (not just boss). Counts per floor 0, 1, 1, 1, 2 (SPEC-CONF-001). Rolled automatically on floor entry from the generatable modifier pool.
- Boss gate at end of each floor with modifier choice scaling (Floor N → pick N from N+1; 1 of 2 on Floor 1, SPEC-CONF-002). Boss puzzles stack floor modifiers + boss-chosen modifiers; from Floor 3 the boss also carries one debuff (BossDebuffEffects).
- Boss boards follow the floor's grid range like every other puzzle (no 8×8 minimum, SPEC-CONF-028).
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

See [BossMechanicsSystem](BossMechanicsSystem.md) for the full modifier pool, eligibility rules, intensity scaling, geometry generation, overlay rendering, and legend panel.

### Summary

- 43 player modifiers (`BossModifierId` 0–29 and the catalog batch 38, 40–44, 51, 52, 58–60, 74, 76), 42 of them generatable (Modular Regions is excluded) — SPEC-CONF-019, BossMechanicsSystem 2.8
- All eligible at every floor (no tier gating). Board-size and pairing restrictions apply.
- **Floor modifiers** (persistent per floor, applied to all puzzles including boss):

| Floor | Floor Modifiers | Boss Options Shown | Boss Player Picks |
|:---:|:---:|:---:|:---:|
| 1 | 0 | 2 | 1 |
| 2 | 1 | 3 | 2 |
| 3 | 1 | 4 | 3 |
| 4 | 1 | 5 | 4 |
| 5 | 2 | 6 | 5 |

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
- All generatable modifiers available as individual toggles
- The main-menu entry is called **Custom Puzzle**; the Sudoku Basics lesson is offered at first launch and in Options (SPEC-CONF-033)
- Tutorial completion unlocks achievements only (no gameplay progression)

---

## 13) Additional Modes

### Endless Zen

Source of truth: [EndlessZenMode](EndlessZenMode.md) (decided 2026-10-07, SPEC-CONF-006): unlocks after 10 runs started, always 9×9, modifier caps 1/2/3 at depth 0–9/10–19/20+, no XP. The earlier GDS rules (unlock after a full run, all board sizes, XP leaderboard) are retired.

### Spirit Trials

- Source of truth: [SpiritTrialsMode](SpiritTrialsMode.md); unlocks after the first boss defeat (SPEC-CONF-007)
- Fixed 9×9, baseline 3★
- Timer active, no gold, limited items
- Score: `BasePoints × SpeedMultiplier × ConstraintBonus − MistakePenalty`

---

## 14) Menus and UX Structure

### Main Menu

- Start Game
- Resume Game (only when run in progress)
- Meta Progression (class levels and prestige)
- Game Modes (Endless Zen / Spirit Trials / Daily Walk / Monthly Walk)
- Items & Relic Codex
- Custom Puzzle (sandbox, formerly "Tutorial"; SPEC-CONF-033)
- Profiles, Options, Credits, Quit
- **[UI-MENU-VERSION-001]** The game's version ("Version 0.2.0", from the player settings) in the bottom-left corner
- Daily Walk and Monthly Walk widgets next to the menu, with today's goals, streak, theme and countdown (SPEC-CONF-027, GAP-028)

### Start Game Flow

1. Class Select (locked classes shown disabled with unlock condition; Allow Irregular Puzzles toggle, default ON; Harmony selector)
2. Start Run confirmation: class, "base (+bonus)" stats, mode, irregular setting, and an optional seed field (digits; empty = random seed) with a "Week N seed" button that fills in the Seed of the week
3. If a saved run exists: warning that it ends as a defeat (its XP and progress are kept) and "Abandon & Begin" (GAP-032, SPEC-AMB-010)
4. Launch run

### Options Structure

Main menu → Options is a hand scroll with six pages (UI-STYLE-017); LB/RB or PageUp/PageDown turn the page, and "Reset this page" under the scroll resets the shown page:

- **Audio:** Master / Music / SFX / UI volume (arrow rows in 5 % steps), Mute when unfocused, Mute all, menu music style
- **Display:** Fullscreen, VSync, Screen Shake, Particles, Board Brightness, Board Contrast, Resolution, Display Mode (see Display Modes below); all five languages are also offered in first-run setup (SPEC-CONF-032)
- **Gameplay:** Highlight Errors, Auto Pencil Cleanup, Show Digits Left, Jump to Next Empty Cell, Double-Tap to Confirm, Controller Rumble

- **Accessibility:** Colorblind Mode, High Contrast, Reduce Motion, Alternative Constraint Symbols, Status Captions, Text Size, with a preview board on the page's slip
- **Controls:** Keybindings… and the Sudoku Basics lesson
- **Language:** English / German / French / Spanish / Brazilian Portuguese, applied after a confirmation

Below the scroll: Reset this page and Back. In a run the Options have the same pages (Language says where to change it) plus a Run page (UI-OPT-002).

### Options Effects (GAP-061, 1.15)

An audit after the 0.2.0 playtest found options that did nothing or less than their names said. Each now acts as named:

| ID | Option | Effect |
|---|---|---|
| OPT-GAME-001 | Highlight Errors (on) | A wrong digit flashes its cell red; placed digits that break a rule and the cells a digit would conflict with are coloured. Off: neither. |
| OPT-GAME-002 | Auto Pencil Cleanup (on) | A placed digit leaves the pencil marks of its row, column **and box** (the box was missed). Off: every mark stays. Saves from before keep it on. |
| OPT-GAME-003 | Show Digits Left (on; was "Show Candidate Count") | Each number pad tile shows in its corner how many of its digit the board still needs (nothing on a finished digit). |
| OPT-GAME-004 | Jump to Next Empty Cell (off; was "Cursor Snap") | After a correct digit the selection moves to the next empty cell in reading order that takes input (locked and pre-solved cells are skipped). The jump does not start the 0.25 s input pause of a chosen cell, so a quickly typed next digit counts. |
| OPT-GAME-005 | Double-Tap to Confirm (off) | The first press of a digit on a cell asks ("Press 4 again to place it."), the same digit on the same cell within 1.5 s places it. Pencil marks and the Sudoku Basics lesson are never asked for. |
| OPT-GFX-001 | Screen Shake | Off stops every shake of the board (it stopped only with Reduce Motion). |
| OPT-AUDIO-001 | Master Volume | Counts once: every sound multiplies it in and the listener stays at full volume (it was applied twice, so 50 % played at 25 %). |
| OPT-LABEL-001 | Names | "Confirm Wrong Placement" is removed: a question asked only before a wrong digit would give the answer away. Brightness and Contrast are **Board** Brightness and **Board** Contrast (they calibrate the puzzle board and now redraw it at once); "Screen Reader" is **Status Captions** (an on-screen caption bar, not speech). |
| ACCESS-COLOR-003 | Colorblind Mode | Line colours that tell rules apart use the Okabe–Ito palette (German Whispers green, Dutch Whispers orange, Renban purple, Parity blue, Consecutive yellow, Between sky blue, Nabner/Entropic/Modular vermilion). The cells are no longer passed through a colour-blindness simulation, which made them harder to tell apart. |
| ACCESS-FONT-001 | Text Size | Scales the board's digits and pencil marks too (it changed only the preview and some menus), never past the cell. |

Unchanged and working: the volumes, Mute all, Mute when unfocused, menu music style, Fullscreen, VSync, Particles, Resolution, Display Mode, Language, Controller Rumble, High Contrast, Reduce Motion, Alternative Constraint Symbols, Status Captions.

### Shared challenges: Daily, Weekly, Monthly

Every shared challenge comes from the date alone, so all players get the same content without a server.

| ID | Requirement |
|---|---|
| RUN-WEEK-001 | **Seed of the week:** one Garden Run seed per ISO week (Monday to Sunday), `100000 + FNV-1a("week:YYYY-Www") mod 900000` (six digits). The start-run modal offers it as a "Week N seed" button that fills the seed field; it is reachable with the controller (Up from Begin). Code: `WeeklySeedService`. |
| RUN-WEEK-002 | **The Discord posts match the game:** `tools/discord/walk_calendar.py` computes the Daily Walk goals (seed = yyyymmdd, `DailyGoalService`), the Seed of the week and the Monthly Walk board (seed = year×100+month, `SeasonalChallengeService`) with a port of `System.Random`'s seeded sequence. The same values are pinned in `WalkCalendarParityTests` (game) and `test_walk_calendar.py` (tool). `walk_posts.py` posts them daily, weekly and monthly (discord-server-setup.md §12). |

The Daily Walk uses each player's local date, so the goals change at local midnight; the Discord post names the date it is for.

### Credits

| ID | Requirement |
|---|---|
| CREDITS-COMMUNITY-001 | The credits have a **Community** section: playtesters and translators by name (translators with their language) from `Resources/credits_community.json`, and the Discord invite. The file is written by `discord_admin.py export-credits` from the Discord roles Playtester and Translator and reviewed by the owner before a build; with no names it says that helpers will be named there. |

### Options Panels (main menu and in-run)

| ID | Requirement |
|---|---|
| UI-OPT-001 | The in-run Options show the main Options' Audio, Display and Gameplay tabs with the same controls, look and reset buttons; both panels are built by one factory (`OptionsUiFactory`), so they cannot drift apart. The language row stays in the main menu (changing it rebuilds the UI); the in-run Display tab says where to change it. |
| UI-OPT-002 | The in-run Options add two tabs: **Accessibility** (colorblind, high contrast, reduce motion, alternative constraint symbols, screen reader, reset) and **Run** (run seed, View Modifiers, Save & Quit, Abandon Run; Abandon is hidden in tutorials). Back is always visible below the tabs. |
| UI-OPT-003 | Both Options panels are fully usable with the D-pad: left/right moves within a row and along the tab bar, up/down moves to the nearest control of the next row; the first row leads up to the active tab and the last row down to the buttons below the tabs. Links are rebuilt when a control appears or disappears (language confirmation, Abandon Run). Start opens and B closes the in-run Options (`CancelRouteContract`). |

### Display Modes

Options → Display → **Display Mode** (saved per profile, applied at startup and when changed):

| ID | Requirement |
|---|---|
| DISPLAY-MODE-001 | Four modes, in this order: **Fullscreen** (exclusive), **Borderless Fullscreen** (a borderless window covering the display), **Windowed** (a framed window that can be moved) and **Frameless Window** (no title bar or border). |
| DISPLAY-MODE-002 | Windowed and Frameless Window use the chosen resolution as the window size when it leaves 10 % of the display free in both directions; otherwise the largest of 3840×2160, 2560×1440, 1920×1080, 1600×900, 1280×720, 960×540 that does (1600×900 on a 1080p display). |
| DISPLAY-MODE-003 | Windowed and Frameless Window open centred on the work area of their monitor. |
| DISPLAY-MODE-004 | Frameless Window is a Windows-player feature (the window style is set by `WindowFrameService`); on other platforms and in the Editor it behaves like Windowed. A frameless window cannot be dragged; Alt+Enter switches to fullscreen and back, and the frame is re-applied. |
| DISPLAY-MODE-005 | Saved values: Fullscreen 0, Borderless Fullscreen 1, Windowed 2, Frameless Window 3; unknown values load as Fullscreen, and a save that only carries "not fullscreen" loads as Windowed. The Fullscreen flag is false exactly for the two window modes. |
| DISPLAY-MODE-006 | The Fullscreen toggle (main and in-run Options) switches between a fullscreen mode and a window: turning it off keeps Frameless Window when chosen (else Windowed); turning it on keeps Borderless Fullscreen when chosen (else Fullscreen). |
| DISPLAY-MODE-007 | The game window is titled "Run of the Nine" (the store name) as soon as the game's code runs, in every mode and again after any fullscreen/window switch. For the first one to two seconds after launch the title is the product name, because Unity names the window before any game code runs. The Windows player sets it (`WindowFrameService`), because the Player Settings product name stays `sudoku_rouge_like` for the save folder (SaveLoadArchitecture §Save folder). |

### In-Run Pause Menu

- Resume
- Options
- View Modifiers
- View Seed
- Save & Quit (returns to main menu, run resumes later)
- Abandon Run

As built (1.1, GAP-020; Run tab since 1.9, UI-OPT-002): the in-run Options panel's Run tab shows the run seed and has View Modifiers (floor, puzzle and boss-debuff rules), Save & Quit and Abandon Run (press twice; the run ends as a defeat with its XP recorded).

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
- Puzzles have exactly one solution but are not required to be logically solvable — items/relics compensate

---

## 16) Extensibility Requirements

All of these must be data-driven, not hard-coded:

- Classes and passives
- Item definitions and rarity behavior
- Star levels and missing-cell density mapping
- Modifier pools (43 player modifiers since 0.2.0 + future additions from the Extended Modifier Library)
- Route node definitions and rewards
- Relic effects and tier assignments

---

## 17) Cross-References

| System | Respec Document |
|--------|-----------------|
| Path & Garden | [PathSystem_GardenOverview](PathSystem_GardenOverview.md) |
| Class XP & Progression | [ClassXpProgressionSystem](ClassXpProgressionSystem.md) |
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

The canonical requirement IDs for this document are listed in `docs/REQUIREMENT_MAP.md`. The auto-generated `REQ-*` table from the retired SPICE pipeline was moved to `docs/archive/SPICE_REQUIREMENT_TABLES.md` (2026-10-07, SPEC-OBS-001); its IDs are kept there and are not used by code or tests.
