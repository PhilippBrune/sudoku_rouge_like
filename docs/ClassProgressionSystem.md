# Class Garden Progression System

This document defines the integrated class-garden progression model used by **Run of the Nine**.

## Goals

- Keep existing class unlock milestones intact.
- Add long-term class growth with deterministic XP formulas.
- Add prestige gating with clear requirements and a finite cap.
- Persist archive history for progression UI and future achievements.

## XP Curve

Level range and XP-to-next-level use a two-part curve:

1. **Level 1-20 (linear):**
   - `XP(level) = 120 + 20 * (level - 1)`
2. **Level 21-40 (quadratic):**
   - Let `d = level - 20`
   - `XP(level) = 500 + 10 * d^2`

Hard caps:
- Max level per prestige cycle: `40`
- Max prestige tier: `9`

## Per-Run XP Award Formula

Run XP is awarded through:

`base + modifiers + outcome bonuses - mistake penalty`

Concrete implementation inputs:

- Base: `60 + boardSize * 8 + stars * 30 + depthReached * 6`
- Modifier bonus: `+15` (single) / `+35` (dual)
- Victory bonus: `+40`
- Boss clear bonus: `+55`
- Perfect clear bonus: `+30`
- Mistake penalty: `-5 * mistakesMade`

Final XP is clamped to a minimum of `0`.

## Level & Passive Cadence

- Each run contributes to global garden progression and per-class progression.
- Level-up loop consumes `XP(level)` repeatedly until insufficient XP remains.
- Every 5 levels grants one passive tier increment.

## Prestige Rules

Prestige can trigger only when all are true:

- Current level is `40`
- Boss archive count meets tier gate:
  - `ArchiveBossesDefeated >= (PrestigeTier + 1) * 3`
- Current prestige tier is below cap (`9`)

On prestige:

- Prestige tier +1
- Level reset to `1`
- Current XP reset to `0`
- Passive tier +1

## Archive Tracking

Persistent archive counters:

- Total runs
- Seeds bloomed (victories)
- Bosses defeated
- Perfect runs
- Total XP earned

Per-class archive entries store:

- Class id
- Level
- Current XP
- Prestige tier
- Total XP earned

## Data Model & Code Mapping

- Save/runtime model additions:
  - `MetaProgressionState.GardenProgression`
  - `GardenClassProgressionState`
  - `ClassGardenProgressEntry`
- Run result class tagging:
  - `RunResult.PlayedClassId`
- Progression service:
  - `Assets/Scripts/Meta/ClassGardenProgressionService.cs`
- Run result production:
  - `Assets/Scripts/Run/RunDirector.cs`
- Profile update integration:
  - `Assets/Scripts/Save/ProfileService.cs`
- Save sanitization:
  - `Assets/Scripts/Save/SaveFileService.cs`

## Backward Compatibility

- Existing saves are supported via null-safe initialization of `GardenProgression`.
- New progression values are clamped/sanitized on load.
- Existing unlock progression (`ClassUnlocks`) remains unchanged and continues to gate class availability.
