# GARDEN OF NUMBERS: Complete Production-Ready Game Design Blueprint

Genre: Run of the Nine (Sudoku Roguelike)  
Platform: PC (Steam)  
Target Audience: Mid-Core Puzzle Players  
Identity: Spiritual Mastery Through Discipline

## 1) Core Vision

Run of the Nine is a deterministic Sudoku roguelike where players traverse branching gardens of escalating logical trials. Each run culminates in a multi-phase boss puzzle layered with advanced Sudoku modifiers.

The game should:
- Encourage logical mastery over guessing
- Remove unfair randomness
- Scale difficulty mathematically
- Support 50+ hours of replay
- Deliver an emotional arc of confusion, focus, tension, clarity, enlightenment

## 2) Main Menu Structure

Main Menu:
- Start Game
- Tutorial
- Resume Game
- Options
- Exit Game

Options:
- Language: English / German
- Sound: Master / SFX / Music sliders
- Graphics: Resolution / Windowed / Fullscreen

Autosave supports mid-run and mid-boss resume.

## 3) Core Run Structure

### 3.1 Branching Garden Node Structure

Each run consists of 8-12 nodes:
- Start
- Branch A / Branch B
- Split again (risk path / safe path)
- Pre-Boss
- Boss

Node types:
- Puzzle Node
- Elite Puzzle Node
- Shop Node
- Rest Node
- Relic Node
- Event Node
- Boss Node

Branching visibility:
- Player sees next 2 layers only.

### 3.2 Risk vs Reward Structure

Safe Path:
- Lower difficulty
- Lower gold
- Fewer relics

Risk Path:
- Higher difficulty tier
- Higher gold multiplier
- Elite puzzle chance
- Better relic pool

## 4) Run Economy Loop

Core loop:
Solve Puzzle ÔåÆ Gain Gold + XP ÔåÆ Choose Path ÔåÆ Spend Gold (Shop) ÔåÆ Gain Relic ÔåÆ Boss ÔåÆ Meta Progression

Gold sinks:
- Item purchases
- Item rerolls (cost scaling)
- Relic purchases
- Emergency heals

Gold income scaling:
GoldIncome ÔåÆ PuzzleDifficulty ÔåÆ ModifierComplexity ÔåÆ Heat

## 5) Item System Architecture

### 5.1 Item Roll Slot System

Difficulty-based roll slots:
- 1-2 slots
- 2-3 slots
- 3-3 slots
- 4-4 slots
- 5-5 slots

Only ONE item may be selected. Unselected slots become inactive.

### 5.2 Nothing Slot Rule

One slot may roll Nothing.

If selected:
- Player gains small gold bonus
- Counts as chosen slot
- Slot becomes locked

Nothing acts as a controlled sacrifice mechanic.

### 5.3 Reroll Rules

Reroll costs gold.

If slot:
- Was picked or disabled
- Rolled Nothing

Remaining slots are rerollable.

### 5.4 Item Replacement Rule

At higher difficulty, player may replace an existing item with a new one.

Rules:
- Cannot exceed item capacity
- Replaced item is permanently lost for run

### 5.5 Consumables vs Relics

Consumables:
- Single-use
- Tactical
- Lower power
- Bought in shop

Relics:
- Permanent for run
- Passive effects
- Earned via relic nodes or elites

Economy tension requires:
- Limited gold
- Limited inventory capacity
- Scaling shop prices
- Diminishing return stacking

## 6) Class System

Only ONE class unlocked at start.

### 6.1 Class Balance Framework

Each class defined by:
- HP modifier
- Gold modifier
- Item synergy
- Modifier interaction bias
- Risk tolerance rating

### 6.2 Power Curve Model

Power curve:
- Early Game ÔåÆ Weak
- Mid Game ÔåÆ Synergy spike
- Late Game ÔåÆ Stabilized mastery

No class should be too powerful or too weak.

## 6.3 Multi-Stage Boss System

Boss has 3 escalating puzzles:

Phase 1:
- Base Sudoku + chosen modifier

Phase 2:
- Higher star + intensified modifier

Phase 3:
- Dual modifier + increased logical tier

Player chooses 1 of 2 modifier options before Phase 1. HP carries across phases.

## 7) Long-Term Meta Progression

Track:
- Boss clears per modifier
- Perfect clears
- Dual modifier clears
- 5-star clears
- No-item clears

Garden Completion % based on:
- All sizes
- All modifiers
- All classes
- All relics

## 8) Relic / Permanent Upgrade System

Meta currency earned from runs.

Permanent upgrades:
- +1 starting gold
- +1 starting HP
- +1 reroll discount
- Unlock relic pools
- Unlock advanced classes

Progression uses soft caps to avoid power creep.

## 9) Endless Zen Mode

- No HP
- No gold
- No boss

Infinite puzzle scaling via:
- Logical tier progression
- Modifier stacking
- Larger grid sizes

Depth leaderboard is tracked.

## 10) Time Attack / Spirit Trials

Mode focused on speed.

Rules:
- Fixed puzzle seed
- Fixed modifiers
- Timer visible
- Mistakes add time penalty

Rank tiers:
- S / A / B / C

Leaderboard supported.

## 11) Emotional Game Feel

Wrong Entry:
- Soft red pulse
- Subtle wooden sound

Perfect Solve:
- Blossom animation
- XP bonus

Low HP:
- Slight desaturation
- Tension music layer

Music layers:
- Calm / Focus / Tension / Boss percussion

## 12) Save Architecture

Two files:
- Profile Save
- Run Save

Versioned JSON with:
- Board state
- Pencil marks
- HP
- Gold
- Relics
- Modifier state
- Combo state

Auto-save on:
- Pause
- Quit
- Boss transition

Version migration is supported.

## 13) Steam-Ready Achievements

Beginner:
- First Puzzle
- First Boss
- Unlock Second Class

Intermediate:
- 20 streak
- Dual modifier clear
- Perfect boss

Advanced:
- 5-star modifier clears
- No relic run
- No HP loss run

Expert:
- 100% Garden Completion
- Max Heat clear
- Depth 20 Endless

Hidden:
- 9-9 5-star no pencil
- 1 HP entire run
- 60 min single puzzle focus

Target total: 40-60 achievements.

## 14) Design Principles

The game should:
- Feel fair
- Avoid RNG blame
- Encourage mastery
- Provide controlled risk
- Support replay
- Maintain spiritual identity

## Final Status

This blueprint is production-ready as a top-level design contract for implementation, balancing, QA validation, and Steam feature planning.


