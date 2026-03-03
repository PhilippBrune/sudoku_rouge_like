# Garden of Numbers — Production GDD

## 1. High Concept

**Working Title:** Garden of Numbers  
**Genre:** Puzzle × Roguelike × RPG  
**Theme:** Pixel-art Japanese garden journey inspired by Kyoto and Ryoan-ji aesthetics.  
**Platform:** PC (primary), mobile adaptation planned after core release stability.

Core fantasy: solve increasingly difficult Sudoku encounters while surviving resource pressure and building a run through items, classes, and route choices.

## 2. Core Gameplay Loop

1. Enter garden node
2. Solve Sudoku puzzle
3. Manage HP and Pencil resources
4. Earn Gold and XP
5. Enter item roll phase (pick/skip, optional reroll)
6. Choose next route branch
7. Repeat until death or run completion
8. Boss encounter and post-run meta progression

## 3. Core Systems

### 3.1 Sudoku Engine
- Variable board sizes 5x5 to 9x9
- Star-based missing density scaling
- Constraint-modifier injection architecture
- Pencilmark tracking and resource consumption

### 3.2 Resource System
- HP loss on mistakes (default -1, route/boss can increase)
- Pencil Units as finite strategic resource
- Gold as run-scoped economy
- Escalating costs for Pencil buy and reroll

### 3.3 Class System
- Class identity via base stats + passive philosophy
- Max level 30 progression per run profile
- Locked class presentation in class select

### 3.4 Item System
- Rarity tiers: Normal, Rare, Epic
- Inventory slots constrained by class/progression
- End-level roll guarantees and lock-aware reroll
- Boss clear guarantee for Rare+ / Epic events

### 3.5 Difficulty Model (HeatScore)
Use HeatScore for encounter pressure tracking, reward tuning, and progression spike control.

## 4. Difficulty Heatmap Model

## 4.1 Axes

- **G (Grid Complexity):** board size pressure
- **S (Information Density):** missing-cell pressure
- **C (Constraint Load):** modifier tier pressure
- **R (Resource Pressure):** HP + Pencil depletion pressure
- **I (Cognitive Interference):** fog/arithmetic/dual-modifier pressure

## 4.2 Base Scaling Functions

### Grid Complexity (G)
- 5x5: 1.0
- 6x6: 1.2
- 7x7: 1.5
- 8x8: 1.9
- 9x9: 2.4

### Star Density (S)
\[
S = 1 + (missing\_percentage \times 1.8)
\]
Examples:
- 10% => 1.18
- 50% => 1.90

### Constraint Load (C)
- Tier 1: 1.15
- Tier 2: 1.30
- Tier 3: 1.50
- Tier 4: 1.60
- Tier 5: 1.75

### Resource Pressure (R)
\[
R = 1 + ((1 - HP\_ratio) \times 0.5) + ((1 - Pencil\_ratio) \times 0.4)
\]
Where:
- \(HP\_ratio = currentHP / maxHP\)
- \(Pencil\_ratio = currentPencil / maxPencil\)

### Cognitive Interference (I)
- None: 1.0
- Arithmetic (Killer/Arrow): 1.15
- Fog: 1.25
- Dual modifiers: 1.40

## 4.3 Final Formula
\[
HeatScore = G \times S \times C \times I \times R
\]

## 4.4 Heat Bands
- 1.0-2.0: Relaxed
- 2.0-3.0: Focused
- 3.0-4.0: High tension
- 4.0-5.5: Critical
- 5.5+: Boss-tier stress

## 4.5 Progression Guardrails
- Non-boss levels: max +35% HeatScore growth level-over-level
- Boss encounters: up to +70% allowed

## 5. Garden Structure

Branching map with route identities:
- Bamboo: resource-biased
- Lantern: item-biased
- Koi: risk/reward-biased
- Stone: technical puzzle-biased
- Blossom: recovery-biased

## 6. Boss System

### Encounter Rules
- Always 9x9
- Always 4★ or 5★
- Player chooses one of two modifier options

### Multi-stage Final Boss
- Phase 1: Fog
- Phase 2: Whispers or Renban
- Phase 3: Tier 5 constraint climax

## 7. Meta Progression

Currency: Garden Essence

Unlock tracks:
- New classes
- Permanent relics
- Higher star cap
- Endless mode and advanced boss option pools

## 8. Game Modes

- **Garden Run:** structured run progression
- **Endless Zen:** infinite scaling, minimal economy, leaderboard focus
- **Spirit Trials:** timed challenge with score emphasis

## 9. UX/UI Requirements

- Pixel-art clarity and calm composition
- Constraint color coding + alternative symbols
- Always-visible resource HUD
- Modifier explanation overlays
- Clear pencil/candidate readability

## 10. Balancing Framework

HeatScore drives:
- Reward scaling
- XP scaling
- Drop rarity weighting
- Boss gating and progression pacing

Targets:
- Mid-run survival: 60-70%
- Final-boss survival: 20-30%

## 11. Technical Architecture (High-Level)

- Modular Sudoku engine
- Constraint rule injection
- Resource/economy manager
- Drop-table manager
- Save/profile/meta manager
- Deterministic seed support

## 12. Main Menu Structure

## 12.1 Primary Menu
- Start Game
- Resume Game (visible only with valid active run)
- Meta Progression
- Game Modes
- Options
- Credits
- Quit

## 12.2 Start Game Flow
1. Mode select (Garden Run / Endless Zen / Spirit Trials)
2. Class select (locked classes shown disabled)
3. Seed select (random/entered seed, tutorial toggle)

## 12.3 Resume Game Rules
- Visible only if in-progress run exists
- If save invalid/corrupted: disable and surface warning

## 13. Options Menu Requirements

### Language
- English
- German

### Audio
- Master/Music/SFX/UI volume
- Mute all
- Output device (optional advanced)

### Graphics
- Resolution
- Fullscreen/Windowed/Borderless
- VSync
- Frame cap
- Pixel-perfect toggle
- UI scale
- Screen shake toggle
- Particle intensity

### Gameplay
- Confirm before wrong placement
- Auto pencil cleanup
- Highlight conflicts
- Show candidate count
- Show heat indicator (debug)
- Cursor snap

### Accessibility
- Colorblind mode
- High contrast mode
- Font scaling
- Reduce motion
- Alternative constraint symbols

## 14. Meta Progression Menu

Sections:
- Classes (unlock + stats)
- Relics (owned/locked/cost)
- Statistics (runs, clears, average mistakes, fastest time, highest heat)

## 15. Game Modes Menu

- Garden Run
- Endless Zen (leaderboard, highest depth/heat)
- Spirit Trials (daily, weekly, personal best)

## 16. In-Run Pause Menu

- Resume
- Options
- View modifiers
- View seed
- Restart level (token-cost)
- Abandon run
- Quit to main menu

## 17. End Screens

Include:
- Run over summary (depth, heat, rewards, mistakes)
- Victory summary (peak heat, time, boss phase)
- Post-run analytics summary and improvement tip

## 18. Run Variation & Entropy Framework

### 18.1 Archetype Evolution
Runs dynamically converge into one of four archetypes plus a secret archetype path:
- Economy (Merchant Monk)
- Modifier (Rule Bender)
- Survival (Enduring Sage)
- Combo (Flow Master)
- Secret: Chaos Monk

Archetype identity is derived from relic-category concentration and run-state behavior.

### 18.2 Relic Category Synergy
Relics are categorized as:
- Economy
- Survival
- Modifier
- Combo
- Chaos
- Utility

Synergy activation thresholds:
- 2 same category: minor effect
- 3: major effect
- 4+: transformative effect

### 18.3 Legendary Relics
Legendary relics are run-warping and target sub-5% appearance.
Examples:
- The Shifting Garden
- The Silent Grid
- The Golden Root

### 18.4 Event Nodes
Event nodes are narrative-lite with short text and 2-3 meaningful choices.

Categories:
- Sacrifice
- Risk Amplification
- Resource Trade

### 18.5 Curse Architecture
Curses add controlled entropy and tension while preserving player agency.

Supported curse vectors:
- Cursed relic downside
- Locked item slot
- Temporary blindness
- Increased mistake penalty

Curse stack contributes to heat and raises rare-event probability.

### 18.6 Difficulty Variance Bands
Node-local controlled variance:
- Safe path: expected heat ±5%
- Risk path: expected heat ±15%

Rare spike event target:
- ~2% chance after elite context
- Temporary heat/modifier amplification with high reward compensation

### 18.7 Mid-Run Adaptation
Systems include:
- Relic transformation node (2 -> 1 higher-tier, cursed risk)
- Temporary class mutation (node-limited duration)
- Modifier rerouting (meta strategic control)
- Risky rebuild (once per run, HP to 1, gain 2 legendary)

### 18.8 Failure Psychology
Post-run analytics reinforce mastery:
- Heat curve
- Mistake breakdown
- Hardest puzzle marker
- Modifier impact rating
- Generated improvement suggestion

### 18.9 Rare Meta Surprises
- Hidden dual-modifier boss path
- Corrupted path branch (high-risk/high-reward)
- Secret class unlock: Chaos Monk

### 18.10 Endgame Sustainability
- Ascension (Heat+ growth)
- Optional prestige reset loop
- Cosmetic progression hooks
- Seasonal challenge mode with fixed monthly seed

### Run Over (HP = 0)
Show depth, final heat, gold/xp/essence, boss phase reached, mistakes, and restart/main menu actions.

### Victory Screen
Show chosen modifier, peak heat, clear time, relic unlocks, class unlock opportunities, and epic drop presentation.

## 18. Production Notes

This GDD is implementation-ready and maps to the current Unity code scaffold:
- HeatScore model implemented in runtime services
- Menu/meta/settings data models present in core state
- Run result payload supports end/victory screens and profile statistics
