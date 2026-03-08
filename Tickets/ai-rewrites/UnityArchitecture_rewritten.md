# Unity Architecture Blueprint 

## 1) Technology Baseline

- Engine: Unity 6 LTS (or latest LTS)
- Language: C#
- Render: URP 2D
- Input: Unity Input System
- UI: uGUI (safe for fast production) or UI Toolkit (optional)
- Persistence: JSON save files (Application.persistentDataPath)

## 2) Proposed Folder Structure

```text
Assets/
  Art/
    Pixel/
    UI/
    VFX/
  Audio/
  Data/
    Classes/
    Items/
    Modifiers/
    Routes/
    Difficulty/
  Prefabs/
    Board/
    UI/
  Scenes/
    Boot.unity
    MainMenu.unity
    ClassSelect.unity
    RunMap.unity
    SudokuLevel.unity
    ItemRoll.unity
    Boss.unity
    Results.unity
  Scripts/
    Core/
    Sudoku/
    Run/
    Economy/
    Items/
    Classes/
    Boss/
    UI/
    Save/
```

## 3) Data-Driven Model (ScriptableObjects + Runtime State)

## 3.1 Static Definitions (ScriptableObjects)

- `ClassDefinition`
   - id, displayName, baseHP, basePencil, baseItemSlots, baseRerollTokens
   - passives list
   - unlockCondition

- `ItemDefinition`
   - id, itemType, rarity, charges, targetMode
   - effect payload (value tables per rarity)
   - consumable flags

- `ModifierDefinition`
   - id, tier, difficultyImpact
   - rule parameters (e.g., fogPercentMin/Max)
   - availability constraints

- `DifficultyDefinition`
   - difficultyTier, boardSize, baseGold
   - itemSlotRollCount

- `StarDefinition`
   - star, missingPercent
   - optional rarity/constraint scalar

- `RouteNodeDefinition`
   - nodeType, reward profile, penalty profile, min/max stars

## 3.2 Runtime State Models

- `RunState`
   - seed, depth, currentHP, currentGold, currentPencil
   - classId, level, xp
   - purchasesThisRun, rerollsThisRun
   - inventory, relics, routeHistory

- `LevelState`
   - boardSeed, difficultyTier, star, activeModifiers
   - mistakes, placements, revealedCells
   - currentSelection, pencilMode

- `BossState`
   - phaseIndex, modifierChoicePool, chosenModifier
   - hpPenaltyMultiplier, pencilPenalty

## 4) Core Systems

## 4.1 Sudoku System

Responsibilities:
- Generate solved board for NxN (5..9)
- Remove values by star density
- Validate placement against ruleset
- Support extra rule validators (modifiers as pluggable checks)

Key classes:
- `SudokuGenerator`
- `SudokuBoardModel`
- `SudokuValidator`
- `ConstraintEngine`

`ConstraintEngine` should evaluate:
- Base Sudoku constraints
- Active modifier constraints via interface:
   - `IConstraintRule.ValidateMove(board, cell, value)`

## 4.2 Input & Selection System

- Multi-select with drag and key expansion
- Selection operations: add/remove/invert/all/none
- Tool-sensitive double click behavior

Key classes:
- `SelectionController`
- `InputHandler`

## 4.3 Economy System

- Pencil mode + spend + depletion lock
- Mid-level pencil purchase
- Gold + XP formulas
- Item roll phase with pick/skip
- Basic reroll economy

Key classes:
- `EconomyManager`
- `PurchaseManager`

## 4.4 Classes & Leveling System

- Number Freak complete
- XP curve + level reward triggers
- Zen Master locked preview
- Path node selection and reward profiles

Key classes:
- `ClassesManager`
- `LevelManager`
- `RouteNode`

## 4.5 Boss Framework

- Modifier selection
- At least 3 modifiers implemented (Fog, Parity, Dutch)
- 3-phase final boss scaffold

Key classes:
- `BossManager`
- `ModifierChoiceManager`

## 5) UI Structure

## 5.1 Scenes & Canvases

- `MainMenu`: Play, Modes, Settings, Meta
- `ClassSelect`: class cards + locked preview
- `RunMap`: branching path node choice
- `SudokuLevel`: board, resource bar, inventory, numpad, pencil toggle
- `ItemRoll`: slot cards, reroll button, pick/skip
- `Boss`: modifier choice prephase + phase HUD
- `Results`: run summary, xp gains, unlocks

Main menu structure must include:
- Start Game
- Resume Game (conditional)
- Meta Progression
- Game Modes
- Options
- Credits
- Quit

## 5.2 HUD Elements (SudokuLevel)

- Top bar: HP, Pencil, Gold, XP progress
- Center: Sudoku grid + overlays
- Right panel: inventory slots and item tooltips
- Bottom panel: numpad + mode toggles + undo cues

## 5.3 Additional Menus

- `ModeSelect`: Garden Run, Endless Zen, Spirit Trials (with unlock gating)
- `SeedSelect`: random/entered seed and tutorial toggle
- `MetaProgression`: classes, relics, statistics
- `Options`: language, audio, graphics, gameplay, accessibility
- `Pause`: resume/options/view modifiers/view seed/restart level/abandon
- `EndRun`: run-over summary payload (depth/heat/rewards/stats)
- `Victory`: clear summary payload (modifier/peak heat/time/unlocks)

## 6) Testing Strategy

- Unit tests:
   - Formula outputs
   - Cost curve scaling
   - Item roll guarantee correctness
   - Constraint validators per modifier
- Simulation tests:
   - 1000 seeded rolls for economy sanity
   - Boss modifier selection distribution
- Playtest scripts:
   - early-run survivability
   - reroll abuse checks

## 7) MVP Milestones

## Milestone 1: Core Board Loop
- NxN generation (5..9)
- Input + placement validation
- HP penalty + game over

## Milestone 2: Pencil Economy
- Pencil mode + spend + depletion lock
- Mid-level pencil purchase

## Milestone 3: Run Rewards
- Gold + XP formulas
- Item roll phase with pick/skip
- Basic reroll economy

## Milestone 4: Classes + Leveling
- Number Freak complete
- XP curve + level reward triggers
- Zen Master locked preview

## Milestone 5: Branching Routes
- Path node selection and reward profiles

## Milestone 6: Boss Framework
- Modifier selection
- At least 3 modifiers implemented (Fog, Parity, Dutch)
- 3-phase final boss scaffold

## Milestone 7: Meta + Modes
- Relics and unlock persistence
- Endless Zen + Spirit Trials

## 8) Balancing Hooks (for Future Tuning)

Expose all tunables in data assets:
- HP penalties per mode/path
- Pencil reward curves
- rarity weights per depth
- modifier tier pools by run stage
- route reward profile multipliers

This allows adding stars/classes/items/corruption without modifying core game code.

Note: This is a high-level overview and some details might be subject to change as the project progresses.


