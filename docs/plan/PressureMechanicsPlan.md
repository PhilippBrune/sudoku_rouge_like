# Implementation Plan — Pressure Mechanics

**Spec:** `docs/PressureMechanicsSpec.md` v0.7
**Date:** 2026-04-15
**Status:** fully implemented ✅

---

## Implementation State

| Phase | Status | Notes |
|-------|--------|-------|
| A — Data model | ✅ Done | All fields present |
| B — Rolling | ✅ Done | `RollPressureMechanic`, `BossService` entries, `LevelConfig` flags |
| C — Init | ✅ Done | All 4 mechanics + both curses initialised |
| D — Tick logic | ✅ Done | Correct + wrong path ticks, VeryHigh crumbling branch |
| E — UI Rendering | ✅ Done | All overlays, badges, flash effects, shake, wave ripple, strips, chain lines |
| F — Curses | ✅ Done | `counting_shadow`, `hollow_eye` wired |
| G — Save notes | ✅ Done | Comment in `RunAutoSaveCoordinator` |

---

## Open Gaps

None.

---

## Architecture Overview

```
BuildLevelConfig              → RollPressureMechanic() [PRESSURE-ROLL-001..005]
StartLevel                    → InitializePressureMechanics() [PRESSURE-INIT-001..008]
RunDirector.PlaceNumber
  correct path                → TickPressureThreats(r,c) [PRESSURE-TICK-001,003..009]
  incorrect path              → TickPressureThreats(r,c) [PRESSURE-TICK-002 ❌ missing]
ApplyMistakePenalty(row,col)  → TickPressureMistake(r,c) [PRESSURE-TICK-010..013]
InRunController.Refresh       → BuildPressureRenderData() [PRESSURE-UI-001]
BoardViewController           → RenderBoard(pressure) [PRESSURE-UI-002..011]
```

---

## Phase A — Data Model

### A1 — `GameEnums.cs` — Enums ✅

`ThreatScope` enum — `SingleCell`, `Box`, `Row`, `Column`, `Chain`
> Req: PRESSURE-INIT-001 (scope selection), PRESSURE-UI-010, PRESSURE-UI-011

`BossModifierId` pressure values:
- `CountdownFill = 103` — PRESSURE-ROLL-004
- `HauntedCell = 104` — PRESSURE-ROLL-004
- `CrumblingRegion = 105` — PRESSURE-ROLL-004
- `PressureWave = 106` — PRESSURE-ROLL-004, PRESSURE-ROLL-005

### A2 — `RuntimeModels.cs` — Models ✅ with gaps

`PressureThreat` class:
```csharp
public ThreatScope Scope;
public List<(int row, int col)> Cells;
public int RemainingPlacements;
public bool IsChain;      // [PRESSURE-TICK-004]
public bool IsWaveSpawned; // [PRESSURE-TICK-009]
// ❌ MISSING — needed for PRESSURE-UI-003:
// public int InitialPlacements;
```

`PressureRenderData` struct:
```csharp
public List<PressureThreat> ActiveThreats;     // [PRESSURE-UI-002..005]
public (int row, int col) HauntedCell;          // [PRESSURE-UI-006]
public Dictionary<int, int> CrumblingCells;     // [PRESSURE-UI-007,008]
public int CrumblingMaxFragility;
// ❌ MISSING — needed for PRESSURE-UI-009:
// public bool WaveRipplePending;
```

`LevelState` pressure fields — all `[NonSerialized]` per PRESSURE-SAVE-001:
```csharp
public List<PressureThreat> ActiveThreats;      // [PRESSURE-TICK-001..009]
public (int row, int col) HauntedCell;           // [PRESSURE-TICK-010..011]
public Dictionary<int, int> CrumblingCells;      // [PRESSURE-TICK-012..015]
public int WaveCounter;                          // [PRESSURE-TICK-007]
public int WaveSpawnCount;                       // [PRESSURE-TICK-008,009]
public int WaveInterval;                         // [PRESSURE-TICK-007]
public int WaveThreatCounter;                    // [PRESSURE-TICK-007]
public int CrumblingStartFragility;              // [PRESSURE-UI-007]
// ❌ MISSING — needed for PRESSURE-TICK-013:
// public bool CrumblingVeryHigh;
// ❌ MISSING — needed for PRESSURE-UI-005:
// public List<(int,int)> ChainSuccessCells;
// ❌ MISSING — needed for PRESSURE-UI-008:
// public HashSet<(int,int)> CrumbledFlashCells;
```

`LevelConfig` additional fields ✅:
```csharp
public bool IsElite;    // [PRESSURE-ROLL-002]
public bool IsPreBoss;  // [PRESSURE-ROLL-002]
```

---

## Phase B — Rolling & Delivery

### B1 — `BossService.cs` — ModifierData entries ✅

```csharp
[BossModifierId.CountdownFill]   = (2, 0.35f),  // [PRESSURE-ROLL-004]
[BossModifierId.HauntedCell]     = (2, 0.30f),  // [PRESSURE-ROLL-004]
[BossModifierId.CrumblingRegion] = (3, 0.50f),  // [PRESSURE-ROLL-004]
[BossModifierId.PressureWave]    = (4, 0.60f)   // [PRESSURE-ROLL-004]
```

### B2 — `RunDirector.BuildLevelConfig` ✅

```csharp
RollPressureMechanic(config, floor, rng);
```

`RollPressureMechanic` implements PRESSURE-ROLL-001 through PRESSURE-ROLL-005.

---

## Phase C — Initialization (`StartLevel`) ✅

`InitializePressureMechanics(config, board, levelState, rng)` implements:
- PRESSURE-INIT-001 through PRESSURE-INIT-008

Helper methods: `PickCountdownScope`, `SelectThreatCells`, `CounterForScope`, `SelectHauntedCell`, `FragilityForIntensity`, `SelectCrumblingRegion`, `GetRegionEmptyCells`, `WaveIntervalForIntensity`, `PerThreatCounterForIntensity`

---

## Phase D — Core Tick Logic

### D1 — `RunDirector.PlaceNumber` — Correct path ✅ / Wrong path ❌

Correct path calls `TickPressureThreats(row, col)` → implements PRESSURE-TICK-001, 003–009.

**❌ PRESSURE-TICK-002 gap — wrong placement also must decrement counters.**

To implement: after `CurrentLevelState.Mistakes++` in the `isValid == false` path, add:

```csharp
// [REQ: PRESSURE-TICK-002] Wrong placements also decrement threat counters
TickPressureCountersOnly(row, col);
```

Where `TickPressureCountersOnly` is a new private method (or a `bool counterOnly` param on `TickPressureThreats`) that:
1. Decrements all `ActiveThreats[i].RemainingPlacements--`
2. Checks for expiry (cells still unfilled, counter ≤ 0) → penalty
3. Decrements `WaveCounter` and potentially spawns wave threat
4. Does NOT check for success (cell cannot be filled correctly on a wrong placement)

### D2 — `RunDirector.TickPressureMistake` — VH gap ❌

Implements PRESSURE-TICK-010..012 (done). Missing PRESSURE-TICK-013.

```csharp
// ❌ PRESSURE-TICK-013: VeryHigh — only first cell erodes
// After implementing, wrap the existing all-cells loop:
if (ls.CrumblingVeryHigh)
{
    var firstKey = int.MaxValue;
    foreach (var k in ls.CrumblingCells.Keys)
        if (k < firstKey) firstKey = k;
    if (firstKey != int.MaxValue)
    {
        ls.CrumblingCells[firstKey]--;
        if (ls.CrumblingCells[firstKey] <= 0)
            AutoFillCrumbledCell(firstKey / 100, firstKey % 100);
    }
}
else
{
    // existing loop
}
```

---

## Phase E — UI Rendering

### E1 — `PressureRenderData` DTO

Current fields ✅: `ActiveThreats`, `HauntedCell`, `CrumblingCells`, `CrumblingMaxFragility`

Needed additions:
- `bool WaveRipplePending` — PRESSURE-UI-009
- Passing `ChainSuccessCells` — PRESSURE-UI-005
- Passing `CrumbledFlashCells` — PRESSURE-UI-008

### E2 — `InRunController.BuildPressureRenderData` ✅ (partially)

Returns null when `RunNumber == 0` (PRESSURE-UI-001). Populates existing DTO fields.

Needs to populate new DTO fields when added.

### E3 — `BoardViewController.RenderBoard` — Existing ✅

Per cell (inside cell loop):
- HauntedCell pulse — PRESSURE-UI-006 ✅
- CrumblingCells tint — PRESSURE-UI-007 ✅
- Badge text + colour — PRESSURE-UI-002 ✅ / PRESSURE-UI-003 ⚠️

### E4 — `BoardViewController.RenderBoard` — Needed additions

After the cell loop (or as separate overlays):

| Req | Visual | New method |
|-----|--------|-----------|
| PRESSURE-UI-004 | Expiry animation on badge | coroutine triggered by threat removal detection |
| PRESSURE-UI-005 | Chain success gold flash | `ChainSuccessCells` colour override per cell |
| PRESSURE-UI-008 | Auto-fill green flash | `CrumbledFlashCells` colour override per cell |
| PRESSURE-UI-009 | Wave ripple ring | `DrawWaveRipple()` using full-board overlay `RectTransform` |
| PRESSURE-UI-010 | Region border strip | `DrawRegionBorderStrip(threat, color)` |
| PRESSURE-UI-011 | Chain line | `DrawChainThreatLine(threat, color)` — reuse Between Lines draw code |

**Badge colour fix (PRESSURE-UI-003):**
```csharp
// Add InitialPlacements to PressureThreat (set at creation time)
float ratio = threat.RemainingPlacements / (float)Math.Max(1, threat.InitialPlacements);
if (ratio > 0.5f)      badgeColor = new Color(0.70f, 0.70f, 0.70f, 0.90f); // grey
else if (ratio > 0.25f) badgeColor = new Color(1.00f, 0.65f, 0.10f, 0.90f); // amber
else                    badgeColor = new Color(0.95f, 0.20f, 0.20f, 0.90f); // red
```

---

## Phase F — Curse Wiring ✅

`counting_shadow` — PRESSURE-INIT-007 ✅
`hollow_eye` — PRESSURE-INIT-008 ✅

---

## Phase G — Save Notes ✅

All pressure fields `[NonSerialized]` — PRESSURE-SAVE-001 ✅
Comment in `RunAutoSaveCoordinator` — PRESSURE-SAVE-003 ✅
Resume behaviour documented — PRESSURE-SAVE-002 ✅

---

## Non-UI Functional Requirements Table

| Req ID | Requirement | Status |
|--------|-------------|--------|
| PRESSURE-ROLL-001 | No floor 1 or tutorial activation | ✅ |
| PRESSURE-ROLL-002 | Elite/pre-boss/boss nodes only | ✅ |
| PRESSURE-ROLL-003 | Probability scales with floor | ✅ |
| PRESSURE-ROLL-004 | One mechanic per puzzle max | ✅ |
| PRESSURE-ROLL-005 | PressureWave from floor 3+ only | ✅ |
| PRESSURE-INIT-001 | CountdownFill initialisation | ✅ |
| PRESSURE-INIT-002 | HauntedCell initialisation | ✅ |
| PRESSURE-INIT-003 | CrumblingRegion initialisation | ✅ |
| PRESSURE-INIT-004 | PressureWave initialisation | ✅ |
| PRESSURE-INIT-005 | Chain length + fallback | ✅ |
| PRESSURE-INIT-006 | PresolvedCells excluded from selection | ✅ |
| PRESSURE-INIT-007 | counting_shadow curse | ✅ |
| PRESSURE-INIT-008 | hollow_eye curse | ✅ |
| PRESSURE-TICK-001 | Correct placement decrements counters | ✅ |
| PRESSURE-TICK-002 | Wrong placement also decrements counters | ✅ `TickPressureCountersOnly()` |
| PRESSURE-TICK-003 | Threat success on last cell filled | ✅ |
| PRESSURE-TICK-004 | Chain success +1 HP | ✅ |
| PRESSURE-TICK-005 | Threat expiry 2 HP per cell | ✅ |
| PRESSURE-TICK-006 | DoublePenalty doubles expiry penalty | ✅ |
| PRESSURE-TICK-007 | Wave counter tick + reset | ✅ |
| PRESSURE-TICK-008 | Wave cap at 3 concurrent | ✅ |
| PRESSURE-TICK-009 | WaveSpawnCount decrements on removal | ✅ |
| PRESSURE-TICK-010 | HauntedCell extra HP on mistake | ✅ |
| PRESSURE-TICK-011 | HauntedCell DoublePenalty stacking | ✅ |
| PRESSURE-TICK-012 | CrumblingRegion all-cell erosion | ✅ |
| PRESSURE-TICK-013 | CrumblingRegion VeryHigh first-cell only | ✅ `CrumblingVeryHigh` branch |
| PRESSURE-TICK-014 | Fragility 0 triggers AutoFill | ✅ |
| PRESSURE-TICK-015 | AutoFill places solution, costs HP | ✅ |
| PRESSURE-TICK-016 | AutoFill silently resolves threats | ✅ |
| PRESSURE-TICK-017 | AutoFill does not decrement counters | ✅ |
| PRESSURE-TICK-018 | AutoFill clears haunting | ✅ |
| PRESSURE-TICK-019 | All HP via ApplyMistakePenalty | ✅ |
| PRESSURE-SAVE-001 | Pressure fields NonSerialized | ✅ |
| PRESSURE-SAVE-002 | Resume reinitialises from seed | ✅ |
| PRESSURE-SAVE-003 | Save comment documents behaviour | ✅ |
| PRESSURE-EDGE-001 | Chain <3 cells fallback | ✅ |
| PRESSURE-EDGE-002 | CrumblingRegion no-region guard | ✅ |
| PRESSURE-EDGE-003 | Wave no-cell spawn skip | ✅ |
| PRESSURE-EDGE-004 | Haunted+Crumbling overlap | ✅ |
| PRESSURE-EDGE-005 | counting_shadow + modifier coexist | ✅ |
| PRESSURE-EDGE-006 | hollow_eye defers to modifier | ✅ |

## UI Requirements Table

| Req ID | Requirement | Status |
|--------|-------------|--------|
| PRESSURE-UI-001 | No visuals on tutorial | ✅ |
| PRESSURE-UI-002 | Count badge per threatened cell | ✅ |
| PRESSURE-UI-003 | Badge colour ramp by ratio | ✅ ratio-based |
| PRESSURE-UI-004 | Expiry red-flash + shake | ✅ `FlashCells` + `ShakeBadgeCells` |
| PRESSURE-UI-005 | Chain success gold-flash | ✅ FlashCells + ChainSuccessCells |
| PRESSURE-UI-006 | Haunted cell purple pulse | ✅ |
| PRESSURE-UI-007 | Crumbling brown crack tint | ✅ |
| PRESSURE-UI-008 | Auto-fill green flash | ✅ FlashCells + CrumbledFlashCells |
| PRESSURE-UI-009 | Wave ripple animation | ✅ TriggerWaveRipple() |
| PRESSURE-UI-010 | Region border strip | ✅ DrawPressureStrip in RenderPressureThreatOverlays |
| PRESSURE-UI-011 | Chain animated line | ✅ DrawPressureLine in RenderPressureThreatOverlays |

---

## Open Gaps Summary

None — all requirements implemented. ✅
