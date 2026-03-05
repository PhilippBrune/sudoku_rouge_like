# Run of the Nine — Full Game Design Specification

## 1) Vision

A serene but tense Sudoku roguelike where each solved board is a deeper step into a Japanese garden journey. The game uses calm visual presentation and strict mechanical pressure from HP loss and limited pencilmarks.

### Theme & Art Direction

- Pixel art visual language
- Palette: calm greens, stone neutrals, lantern gold for reward highlights
- Ambient effects: light petals, water ripple loops, subtle lantern glows
- Inspirations: Kyoto gardens, Ryoan-ji minimalism, Kinkaku-ji elegance

## 2) Core Loop

1. Start run with chosen class
2. Enter garden section (Sudoku level)
3. Solve board under HP + Pencil constraints
4. Gain Gold + XP on completion
5. Item roll phase (pick one slot or Nothing)
6. Optional reroll of eligible slots
7. Choose next route path (where applicable)
8. Progress until defeat or run completion

Run failure occurs when HP reaches 0.

## 3) Board Difficulty System

## 3.1 Structural Tier (Board Size)

- Difficulty 1: 5x5
- Difficulty 2: 6x6
- Difficulty 3: 7x7
- Difficulty 4: 8x8
- Difficulty 5: 9x9

## 3.2 Star Density (Missing Cells)

- 1★: 40% missing
- 2★: 50% missing
- 3★: 60% missing
- 4★: 70% missing
- 5★: 80% missing

Current release requirement:
- Star scaling uses only 1★..5★.

## 4) Controls Specification

## 4.1 Selection

- Mouse click/drag: select cells
- CTRL + click/drag: add/remove from selection
- Arrow keys: move selection cursor
- CTRL/SHIFT + Arrow: expand selection
- CTRL + A: select all
- CTRL + SHIFT + A: deselect all
- CTRL + I: invert selection
- Double click / long tap: tool-sensitive select

## 4.2 Number Input

- Keyboard `0-9`
- Numpad keys
- On-screen numpad buttons

## 4.3 Pencil Mode

- Toggle by hotkey (default `P`)
- UI toggle button mirrored with keyboard state

## 5) Resource Systems

## 5.1 HP

- Wrong number placement: `-1 HP` (unless modified by mode/item/boss)
- HP `= 0`: run ends
- Base Number Freak HP: `10`

## 5.2 Pencil Units

- Each pencilmark consumes `1 Pencil Unit`
- If units are `0`, pencilmark placement is blocked
- Pencil units are limited per run
- Awarded after level completion
- Can be bought with Gold during level

## 5.3 Gold

Run-based currency.

### Base Difficulty Gold

- Diff 1: 20
- Diff 2: 30
- Diff 3: 45
- Diff 4: 65
- Diff 5: 100

### Reward Formula

`Gold = BaseDifficultyGold × (1 + stars × 0.2)`

### Spend Formulas

- Pencil buy cost (during level):
  `20 + (20 × timesPurchasedThisRun)`
- Reroll cost (post-level):
  `20 + (20 × timesRerolledThisRun)`

## 6) Classes

## 6.1 Base Playable Class

### Number Freak

- HP: 10
- Pencil: 10
- Item Slots: 2
- Reroll Tokens: 1
- Max Level: 30

XP required:

`XP_to_next = 100 × level^1.5`

Level rewards:

- L3: +1 Pencil
- L7: +2 HP
- L10: +2 Pencil, +2 HP
- L15: +1 Reroll
- L20: +1 Item Slot
- L30: Start run with 1 Rare Solver

## 6.2 Locked Preview Class

### Zen Master (Not playable initially)

- HP: 8
- Pencil: 15
- Item Slots: 1
- Hidden trait

## 6.3 Expanded Playable Classes

### Garden Monk
- HP 14 / Pencil 5 / Slots 1
- Cannot buy Pencil mid-level
- Passive: Every 5 correct placements, +1 HP

### Shrine Archivist
- HP 8 / Pencil 15 / Slots 2
- Passive: First pencil in a cell is free

### Koi Gambler
- HP 9 / Pencil 8 / Slots 2
- Passive: 25% wrong placement no HP loss
- Passive: 25% correct placement grants +1 Gold

### Lantern Seer
- HP 7 / Pencil 12 / Slots 2
- Passive: Boss modifiers are 20% weaker

### Stone Gardener
- HP 11 / Pencil 8 / Slots 3
- Passive: First item used each level is not consumed

## 7) Item System

## 7.1 Core Item Types

### Solver
Use item, then click empty cell to fill correct value.

- Normal: +0 neighbors
- Rare: +1 neighbor (if valid)
- Epic: +2 neighbors (if valid)

### Finder
Use item, then click filled cell to highlight matching numbers.

- Normal: 1 cell
- Rare: 3 cells
- Epic: 2 cells (intentionally retained per balance note)

## 7.2 New Item Set

### Resource Manipulation
- Ink Well: restore Pencil (+3/+6/+10)
- Meditation Stone: restore HP (+1/+2/+3)
- Wind Chime: refund last mistake (within 3 moves)
  - Normal: undo 1 wrong input
  - Rare: undo + restore 1 HP
  - Epic: undo + restore 1 HP + reveal 1 correct cell

### Information Control
- Pattern Scroll: highlight conflicts
  - N: 1 zone, R: 2 zones, E: full web
- Koi Reflection: reveal candidates without pencil cost
  - N: 1 cell, R: 2 cells, E: 3 cells
- Lantern of Clarity: disable fog temporarily
  - N: 3 moves, R: 6, E: 10

### Risk/Reward
- Tea of Focus: next 5 placements ignore HP loss on mistakes, but each placement consumes +1 extra Pencil
- Cherry Blossom Pact: lose 2 HP, gain 8 Pencil
- Fortune Envelope: random positive or negative outcome

### Board Manipulation
- Stone Shift: reroll one row missing layout (values unchanged)
- Harmony Charm: swap two filled correct digits
- Compass of Order: reveal region with highest unresolved conflicts

## 8) Post-Level Item Roll Phase

Slots by difficulty:
- Diff 1: 2 slots
- Diff 2: 3 slots
- Diff 3: 3 slots
- Diff 4: 4 slots
- Diff 5: 4 slots

Each slot rolls one result:
- Solver
- Finder
- Nothing

Guarantees:
- Diff 1: at least 1 item
- Diff 2+: at least 2 items
- Diff 4+: at least 1 Rare+
- Diff 5 and star >= 4: Epic eligible

Rules:
- Player picks exactly ONE slot, or chooses Nothing
- Picked and Nothing slots become locked
- Only unpicked, non-Nothing slots are reroll-eligible
- If inventory full: replace existing item or choose Nothing

## 9) XP and Progression

Level reward per board:

`XP = difficulty × stars × 50`

Additional dynamic model (for expanded systems):

`DifficultyScore = GridSizeFactor × StarDensityFactor × (1 + BossModifierImpact) × (1 + RunNumber × 0.05)`

Grid size factors:
- 5x5: 1.0
- 6x6: 1.2
- 7x7: 1.5
- 8x8: 1.9
- 9x9: 2.4

Star factors:
- 1★: 1.0
- 2★: 1.15
- 3★: 1.35
- 4★: 1.6
- 5★: 1.9

Use DifficultyScore as a multiplier input for:
- Gold reward
- XP gain
- Item rarity weighting
- Pencil rewards

## 10) Route Progression (Branching Garden)

After each level, pick one of two offered paths.

Path types:
- Bamboo Path: more Pencil, fewer items, lower gold
- Lantern Path: more item rolls, better rarity, slightly harder puzzles
- Koi Pond Path: higher star floors, more gold, mistake penalty -2 HP
- Stone Garden Path: harder technical constraints, more XP, less gold
- Blossom Path: healing and free reroll, lower XP

## 11) End Boss System

Final garden depth triggers Garden Spirit boss Sudoku.

Boss board:
- Always 9x9
- Always 4★ or 5★
- Player chooses 1 of 2 modifiers pre-fight

Boss can apply:
- Increased mistake penalty (`-2 HP`)
- Reduced pencil rewards
- Guaranteed Rare+ drop on win

## 11.1 Boss Modifiers

1. Fog of War
2. Arrow Sums
3. German Whispers
4. Dutch Whispers
5. Parity Lines
6. Renban Lines
7. Killer Cages
8. Difference Kropki
9. Ratio Kropki

## 11.2 Difficulty Impact Ranking

- Tier 1: Parity (+15%), Difference Kropki (+20%)
- Tier 2: Dutch (+30%), Renban (+35%), Ratio (+40%)
- Tier 3: Killer (+50%), Arrow (+55%)
- Tier 4: Fog (+60%)
- Tier 5: German (+75%)

## 11.3 Multi-Stage Final Boss

### Phase 1 — Mist Veil
- 9x9 4★
- Fog of War
- Standard HP loss

### Phase 2 — Whisper Roots
- 9x9 5★
- Renban OR Dutch Whispers
- Start with -1 Pencil

### Phase 3 — Spirit Core
- 9x9 5★
- German Whispers OR Killer+Ratio hybrid
- Mistakes cost -2 HP

Victory rewards:
- Guaranteed Epic
- Relic unlock
- New class unlock chance

## 12) 10-Run Progression Arc

- Run 1–2: Outer Gate (up to 6x6, Tier 1)
- Run 3–4: Bamboo Grove (up to 7x7, Tier 1–2, branching introduced)
- Run 5–6: Koi Pond (up to 8x8, Tier 2–3)
- Run 7–8: Stone Courtyard (9x9, Tier 3–4, epic possible)
- Run 9: Temple Ascent (9x9 4–5★, mandatory Tier 4)
- Run 10: Garden Spirit Core (9x9 5★, choose Tier 4–5, unlock Endless Zen + 6★ cap)

## 13) Meta Progression (Relics)

Unlocked via boss victories using Garden Essence.

Examples:
- Stone Seal: +1 starting HP permanently
- Ink Memory: +2 starting Pencil permanently
- Golden Lantern: +5% Rare chance permanently
- Spirit Compass: pre-reveal final modifier options
- Bamboo Discipline: reduce pencil buy scaling by 10%

## 14) Additional Modes

### Endless Zen
- Unlocked after Run 10
- 9x9 only
- Random modifiers, no item drops, minimal gold
- XP leaderboard focus
- Level 10+: dual modifiers

Scaling:
`EnemyPressure = 1 + (Level × 0.08)`

### Time Attack — Spirit Trials
- Fixed 9x9, baseline 3★
- Timer active, no gold, limited items

Score:
`Score = BasePoints × SpeedMultiplier × ConstraintBonus − MistakePenalty`

## 15) Balancing Guardrails

- Preserve early clarity: low stars + low modifier complexity in first runs
- Keep Pencil scarcity meaningful; avoid runaway refill loops
- Prevent reroll economy abuse with escalating costs and lock rules
- Keep mistake penalties readable and predictable
- Do not stack too many hidden constraints in early routes

## 16) Extensibility Requirements

All of these must be data-driven, not hard-coded:

- Classes and passives
- Item definitions and rarity behavior
- Star levels and missing-cell density mapping
- Modifier pools and tier availability
- Route node definitions and rewards
- Corruption mechanics (future)

Corruption framework placeholder:
- Add optional per-level corruption tags that modify visibility, candidate trust, or cost scaling
- Ensure corruption tags can be attached via level seed data without code refactor

## 17) Difficulty Heatmap Model

### Difficulty Axes
- `G` Grid Complexity (board size)
- `S` Information Density (missing percentage)
- `C` Constraint Load (modifier tier)
- `R` Resource Pressure (HP/Pencil ratios)
- `I` Cognitive Interference (arithmetic, fog, dual modifiers)

### Base Functions

Grid complexity:
- 5x5: 1.0
- 6x6: 1.2
- 7x7: 1.5
- 8x8: 1.9
- 9x9: 2.4

Star density:
`S = 1 + (missing_percentage × 1.8)`

Constraint load:
- Tier 1: 1.15
- Tier 2: 1.30
- Tier 3: 1.50
- Tier 4: 1.60
- Tier 5: 1.75

Resource pressure:
`R = 1 + ((1 - HP_ratio) × 0.5) + ((1 - Pencil_ratio) × 0.4)`

Interference:
- None: 1.0
- Arithmetic: 1.15
- Fog: 1.25
- Dual modifiers: 1.40

### Final Formula
`HeatScore = G × S × C × I × R`

### Heat Bands
- 1.0–2.0: Relaxed
- 2.0–3.0: Focused
- 3.0–4.0: High tension
- 4.0–5.5: Critical
- 5.5+: Boss-tier stress

### Progression Rule
- Non-boss encounters: max +35% heat increase between consecutive levels
- Boss encounters: max +70% heat increase allowed

## 18) Menus and UX Structure

### Main Menu
- Start Game
- Resume Game (only when run in progress)
- Meta Progression
- Game Modes
- Options
- Credits
- Quit Game

### Start Game Flow
1. Mode Select (Garden Run / Endless Zen / Spirit Trials)
2. Class Select (locked classes shown disabled)
3. Seed Select (random or entered seed + tutorial toggle)

### Options Structure
- Language (English/German)
- Audio (Master/Music/SFX/UI, mute, output device optional)
- Graphics (resolution, fullscreen/window/borderless, VSync, frame limit, pixel options)
- Gameplay (confirm wrong placement, auto pencil cleanup, conflict highlights, heat indicator)
- Accessibility (colorblind, high contrast, font scaling, reduce motion, alternative symbols)

### In-Run Pause Menu
- Resume
- Options
- View Modifiers
- View Seed
- Restart Level (token cost)
- Abandon Run
- Quit to Main Menu

### End Screens

Run Over:
- Depth reached
- Final heat score
- Gold/XP/Essence earned
- Boss phase reached
- Mistakes made

Victory:
- Chosen modifier
- Peak heat score
- Time taken
- Relic/Class unlock outcomes
- Epic drop presentation