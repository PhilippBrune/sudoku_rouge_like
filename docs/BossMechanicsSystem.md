# Boss Mechanics System

## Overview

Boss nodes appear at the end of each garden in the roguelike run. Before facing a boss puzzle, the player is presented with a **choice of two randomly rolled modifiers** that alter the Sudoku rules for that encounter. Modifiers the player has never encountered before are displayed as **"???"** until played.

Each modifier adds visual overlays to the puzzle grid and enforces additional constraints beyond standard Sudoku rules. The modifier pool scales with run progression, introducing harder mechanics as the player advances.

---

## Modifier Catalog

### Tier 1 — Introductory (Runs 1–2)

| Modifier | Difficulty Impact | Visual | Rule |
|----------|:-:|--------|------|
| **Parity Lines** | +15% | Blue lines on grid | Adjacent digits on each line must alternate odd/even parity |
| **Difference Kropki** | +20% | White dots between cells | Connected cells must contain consecutive digits (differ by 1) |

### Tier 2 — Intermediate (Runs 3–4)

| Modifier | Difficulty Impact | Visual | Rule |
|----------|:-:|--------|------|
| **Dutch Whispers** | +30% | Orange lines on grid | Adjacent digits on each line must differ by ≥4 |
| **Renban Lines** | +35% | Pink/purple lines on grid | Digits on each line form a consecutive set in any order (no repeats) |
| **Ratio Kropki** | +40% | Black dots between cells | Connected cells must be in a 1:2 ratio (one is double the other) |

### Tier 3 — Advanced (Runs 5–6)

| Modifier | Difficulty Impact | Visual | Rule |
|----------|:-:|--------|------|
| **Killer Cages** | +50% | Dashed border regions with sum label | Digits in each cage must sum to the displayed value; no repeats within a cage |
| **Arrow Sums** | +55% | Arrows with circle (bulb) and path | Sum of digits along the arrow path must equal the digit in the circle |

### Tier 4 — Expert (Runs 7–8)

| Modifier | Difficulty Impact | Visual | Rule |
|----------|:-:|--------|------|
| **Fog of War** | +60% | Dark fog covering non-given cells | Hidden cells cannot be edited; placing a correct digit reveals adjacent cells; all fog clears on puzzle completion |

### Tier 5 — Master (Runs 9–10)

| Modifier | Difficulty Impact | Visual | Rule |
|----------|:-:|--------|------|
| **German Whispers** | +75% | Green lines on grid | Adjacent digits on each line must differ by ≥5. Only available at 5★ or run 10+ |

---

## Boss Gate Choice Flow

1. Player reaches the **Boss Gate** node on the garden path.
2. `BossService.RollBossChoices(runNumber, stars)` selects two distinct modifiers from the tier-appropriate pool.
3. A modal panel displays both choices. Unseen modifiers show **"???"** instead of their name and description.
4. Player picks one modifier → stored in `RunState.ChosenBossModifier`.
5. Selected modifier is added to `RunState.SeenBossModifiers` (persists across the run).
6. The boss puzzle launches with the chosen modifier active.

### Pool Construction

The modifier pool is built based on run number:

| Run Number | Max Tier Available |
|:----------:|:------------------:|
| 1–2 | Tier 1 |
| 3–4 | Tier 2 |
| 5–6 | Tier 3 |
| 7–8 | Tier 4 |
| 9+ | Tier 5 |

German Whispers is gated behind 5★ difficulty or run 10.

---

## Constraint Rule Architecture

All modifier rules implement `IOrderedConstraintRule` and are registered through `SudokuConstraintEngine` in a deterministic execution order defined by `ConstraintRuleCategory`:

| Order | Category | Modifiers |
|:-----:|----------|-----------|
| 0 | BaseSudoku | Row / Column / Region (always active) |
| 1 | Region | Region variant constraints |
| 2 | Line | German Whispers, Dutch Whispers, Parity Lines, Renban Lines |
| 3 | Dot | Difference Kropki, Ratio Kropki |
| 4 | Arithmetic | Killer Cages, Arrow Sums |
| 5 | FogPostProcess | Fog of War (visibility only; does not restrict placement) |

### Validation Path

- `RunDirector.PlaceNumber()` checks `Solution[row,col] == value` (correctness).
- `SudokuValidator.IsMoveValid()` accepts an optional `SudokuConstraintEngine` for modifier-aware pencil mark validation.
- `SudokuConstraintEngine.ValidateAll()` iterates all registered rules in category/order sequence.

---

## Geometry Generation

`ModifierGeometryGenerator.Generate(board, modifiers, seed)` produces a `ModifierOverlayData` object from the solved board. Each modifier type has its own generation strategy:

| Modifier | Generator | Target Count |
|----------|-----------|:------------:|
| German / Dutch Whispers | Random walk along adjacent cells satisfying the difference constraint | 2–4 lines (scales with board size) |
| Parity Lines | Random walk along cells with alternating parity | 2–4 lines |
| Renban Lines | Random walk ensuring consecutive digit set constraint | 2–3 lines |
| Difference Kropki | Scan all adjacent pairs for diff=1; shuffle and pick subset | 6–12 dots |
| Ratio Kropki | Scan all adjacent pairs for 1:2 ratio; shuffle and pick subset | 6–12 dots |
| Killer Cages | Grow connected regions with no repeating digits; compute sum | 3–5 cages |
| Arrow Sums | Place circle on high-value cell; grow path summing to circle value | 2–3 arrows |
| Fog of War | Fog all non-given cells; reveal neighbors of ~⅓ of given cells | Full board coverage |

---

## Data Structures

### ModifierOverlayData

Central container holding all generated modifier geometry for the current puzzle:

```
Lines          → List<ModifierLine>       (whispers, parity, renban)
Arrows         → List<ArrowConstraint>    (circle + path cells)
Cages          → List<KillerCage>         (cell list + sum)
Dots           → List<KropkiDot>          (cell pair + dot type)
FogCells       → HashSet<long>            (packed row/col coordinates)
```

### Supporting Types

| Type | Fields | Used By |
|------|--------|---------|
| `CellCoord` | Row, Col | All modifiers |
| `ModifierLine` | Type (LineType enum), Cells | Whispers, Parity, Renban |
| `ArrowConstraint` | Circle (CellCoord), Path (list) | Arrow Sums |
| `KillerCage` | Sum (int), Cells (list) | Killer Cages |
| `KropkiDot` | CellA, CellB, Type (DotType) | Kropki (White/Black) |

---

## Visual Rendering

Overlays are drawn on a `GridOverlay` RectTransform parented above the Sudoku grid. All overlay elements are non-interactive (`raycastTarget = false`).

| Overlay Element | Color | Technique |
|----------------|-------|-----------|
| German Whispers lines | Green (0.20, 0.72, 0.30, 0.55) | Rotated Image rectangles between cell centers + dots on cells |
| Dutch Whispers lines | Orange (0.90, 0.55, 0.15, 0.55) | Same as above |
| Parity lines | Blue (0.30, 0.40, 0.85, 0.55) | Same as above |
| Renban lines | Pink (0.80, 0.35, 0.65, 0.55) | Same as above |
| Kropki white dots | White (0.95, 0.95, 0.95, 0.85) | Circle at midpoint between cells |
| Kropki black dots | Black (0.10, 0.10, 0.10, 0.90) | Circle at midpoint between cells |
| Killer cage borders | White (0.90, 0.90, 0.90, 0.50) | Tint existing cell borders at cage edges |
| Killer cage sums | White (0.90, 0.90, 0.90, 0.50) | Text label in top-left corner of top-left cage cell |
| Arrow circles | Grey (0.70, 0.70, 0.70, 0.55) | Large circle on circle cell |
| Arrow paths | Grey (0.55, 0.55, 0.55, 0.45) | Lines between consecutive path cells + endpoint dot |
| Fog cells | Near-black (0.06, 0.06, 0.08, 1.00) | Cell background color override; hides digit and pencil text |

---

## Fog of War — Special Behavior

Fog of War is unique among modifiers because it affects **visibility** rather than placement rules:

- All non-given cells start fogged.
- ~⅓ of given cells have their adjacent neighbors pre-revealed.
- Fogged cells block digit input (player sees "This cell is hidden by fog").
- Placing a **correct** digit calls `RevealAdjacentFog()` — clears fog on the placed cell and its 4 orthogonal neighbors.
- On puzzle completion, all remaining fog is cleared instantly.
- The `FogOfWarRule` constraint always returns `true` — fog doesn't restrict which digit is valid, only whether the cell is accessible.

---

## Endless Zen Mode Integration

Endless Zen mode draws from 8 of the 9 modifiers (German Whispers excluded):

```
ParityLines, DifferenceKropki, DutchWhispers, RenbanLines,
RatioKropki, KillerCages, ArrowSums, FogOfWar
```

- Depth < 10: 1 random modifier per level.
- Depth ≥ 10: 2 random modifiers per level.

---

## 10-Run Progression Arc

| Runs | Garden | Grid | Modifier Tiers |
|:----:|--------|:----:|:--------------:|
| 1–2 | Outer Gate | up to 6×6 | Tier 1 |
| 3–4 | Bamboo Grove | up to 7×7 | Tier 1–2 |
| 5–6 | Koi Pond | up to 8×8 | Tier 2–3 |
| 7–8 | Stone Courtyard | 9×9 | Tier 3–4 |
| 9 | Temple Ascent | 9×9 4–5★ | Mandatory Tier 4 |
| 10 | Garden Spirit Core | 9×9 5★ | Choose Tier 4–5 |

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Sudoku/ModifierModels.cs` | Data structures (CellCoord, ModifierLine, ArrowConstraint, KillerCage, KropkiDot, ModifierOverlayData) |
| `Assets/Scripts/Sudoku/ConstraintRules.cs` | 9 IOrderedConstraintRule implementations |
| `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | Geometry generation from solved boards |
| `Assets/Scripts/Sudoku/ModifierFactory.cs` | Maps BossModifierId → constraint rule instances |
| `Assets/Scripts/Sudoku/ConstraintRuleRegistry.cs` | Deterministic ordering infrastructure |
| `Assets/Scripts/Sudoku/SudokuConstraintEngine.cs` | Central rule engine with ValidateAll |
| `Assets/Scripts/Boss/BossService.cs` | Modifier pool construction, choice rolling, tier/impact data |
| `Assets/Scripts/Run/RunDirector.cs` | StartLevel wiring (overlay generation, engine creation, fog reveal) |
| `Assets/Scripts/UI/PrototypeRunScreenController.cs` | Visual overlay rendering, fog masking, input blocking |
| `Assets/Scripts/Core/GameEnums.cs` | BossModifierId, BossModifierTier, ConstraintRuleCategory enums |
