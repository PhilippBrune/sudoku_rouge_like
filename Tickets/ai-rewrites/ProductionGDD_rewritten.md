# Garden of Numbers Production GDD

## 1. High Concept

**Working Title:** Garden of Numbers  
**Genre:** Puzzle/Roguelike/RPG  
**Theme:** Pixel-art Japanese garden journey inspired by Kyoto and Ryoan-ji aesthetics.  
**Platform:** PC (primary), mobile adaptation planned after core release stability.

The core fantasy is to solve increasingly difficult Sudoku encounters while surviving resource pressure and building a run through items, classes, and route choices.

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
- Owned/locked/cost system
- Synergy activation thresholds (2 same category: minor effect, 3: major effect, 4+: transformative effect)

### 3.5 Modifier System
- Dynamic control over Sudoku play (e.g., increased mistake penalty)
- Targets: Economy, Survival, Modifier, Combo, Chaos, Utility
- Node-local controlled variance: Safe path (expected heat ┬▒5%), Risk path (expected heat ┬▒15%)
- Rare spike event target: ~2% chance after elite context, temporary heat/modifier amplification with high reward compensation
- Mid-run adaptation systems include: Relic transformation node, Temporary class mutation, Modifier rerouting, Risky rebuild

### 3.6 Curse System
- Add controlled entropy and tension while preserving player agency
- Supported curse vectors: Cursed relic downside, Locked item slot, Temporary blindness, Increased mistake penalty
- Curse stack contributes to heat and raises rare-event probability

### 3.7 Progression System
- Archetype evolution: Economy (Merchant Monk), Modifier (Rule Bender), Survival (Enduring Sage), Combo (Flow Master), Secret: Chaos Monk
- Relic Category Synergy: Economy, Survival, Modifier, Combo, Chaos, Utility
- Legendary Relics: The Shifting Garden, The Silent Grid, The Golden Root
- Event Nodes: Sacrifice, Risk Amplification, Resource Trade
- Post-run analytics reinforce mastery: Heat curve, Mistake breakdown, Hardest puzzle marker, Modifier impact rating, Generated improvement suggestion
- Rare Meta Surprises: Hidden dual-modifier boss path, Corrupted path branch, Secret class unlock: Chaos Monk
- Endgame Sustainability: Ascension, Optional prestige reset loop, Cosmetic progression hooks, Seasonal challenge mode with fixed monthly seed

## 4. Assets Needed

- Pixel-art assets for the garden nodes, items, classes, and bosses
- Audio assets for the music, SFX, and voice-overs
- Fonts for the in-game text

## 5. Production Notes

This GDD is implementation-ready and maps to the current Unity code scaffold:
- HeatScore model implemented in runtime services
- Menu/meta/settings data models present in core state
- Run result payload supports end/victory screens and profile statistics

---


