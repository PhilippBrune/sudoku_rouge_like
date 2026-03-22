# Boss Mechanics System

**Version:** 1.0 | **Date:** 2026-03-21 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Boss nodes appear at the **end of each floor** (5 floors per run) on the garden path. Before facing a boss puzzle, the player is presented with a **modifier choice panel** where the number of options and selections scales with floor progression. Modifiers the player has never encountered before are displayed as **"???"** until played.

Each modifier adds visual overlays to the puzzle grid and enforces additional constraints beyond standard Sudoku rules. All 15 modifiers are eligible from any floor — there is no tier gating.

---

## Active Modifier Pool (15 Implemented)

All 15 modifiers are fully implemented with constraint rules, geometry generation, overlay rendering, and tutorial descriptions.

### Line Constraints

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 1 | **German Whispers** | Green lines (0.20, 0.72, 0.30, 0.55) | Adjacent digits on each line must differ by ≥5. **Restricted to boards ≥ 7×7.** |
| 2 | **Dutch Whispers** | Bright teal lines (0.10, 0.95, 0.78, 0.75) | Adjacent digits on each line must differ by ≥4 |
| 3 | **Parity Lines** | Blue lines (0.30, 0.40, 0.85, 0.55) | Adjacent digits on each line must alternate odd/even parity |
| 4 | **Renban Lines** | Pink lines (0.80, 0.35, 0.65, 0.55) | Digits on each line form a consecutive set in any order (no repeats) |
| 5 | **Palindrome** | Grey lines | Digits along the line read the same forward and backward |
| 6 | **Thermo** | Lines with bulb | Digits strictly increase from bulb to tip (each cell > previous) |
| 7 | **Between Lines** | White gradient line with circles at ends | Every digit on the line must be strictly between the digits in the two endpoint circles |

### Dot Constraints

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 8 | **Difference Kropki** | White dots (hollow circle) between cells | Connected cells must contain consecutive digits (differ by 1) |
| 9 | **Ratio Kropki** | Black dots (filled circle) between cells | Connected cells must be in a 1:2 ratio |

### Arithmetic Constraints

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 10 | **Killer Cages** | Dashed border regions + sum label (`CageSumBg` + `CageSumText`) | Digits in each cage must sum to the displayed value; no repeats within a cage. **Restricted to boards ≥ 7×7.** |
| 11 | **Arrow Sums** | Grey circle (hollow outline) + arrow path lines | Sum of digits along the arrow path must equal the digit in the circle |

### Cell-Level Constraints

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 12 | **Even Odd** | Blue squares (even) / Orange circles (odd) | Marked cells must contain even or odd digits respectively. Colors: Even=(0.35,0.65,0.90,0.55), Odd=(0.90,0.55,0.20,0.55) |

### Global Negative Constraints

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 13 | **Nonconsecutive** | No geometry (global rule) | No two orthogonally adjacent cells may contain consecutive digits |
| 14 | **Antiknight** | No geometry (global rule) | No two cells a chess knight's move apart may contain the same digit |

### Visibility Modifier

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 15 | **Fog of War** | Near-black cell overlay (0.06, 0.06, 0.08, 1.00) | Non-given cells are hidden; placing a correct digit reveals adjacent cells |

---

## Boss Gate Choice Flow

### Choice Scaling per Floor

The number of modifier options offered and the number the player must select scales with floor progression:

| Floor | Options Shown | Player Picks | Effect |
|:---:|:---:|:---:|--------|
| 1 | 2 | 1 | Gentle introduction — binary choice |
| 2 | 3 | 2 | Two active modifiers, more complex puzzle |
| 3 | 4 | 3 | Three modifiers, build complexity |
| 4 | 5 | 4 | Near-full modifier load |
| 5 | 6 | 5 | Maximum pressure — five simultaneous modifiers |

Formula: Floor N offers **N+1** options, player picks **N**.

### Flow Steps

1. Player reaches the **Boss Gate** node on the garden path.
2. `BossService.RollBossChoices(floor, runState)` selects N+1 distinct modifiers from the full pool of 15.
3. A modal panel displays all options. Unseen modifiers show **"???"** instead of their name and description.
4. Player selects N modifiers → stored in `RunState.ChosenBossModifier` (primary) and applied to the boss puzzle config.
5. Selected modifiers are added to `RunState.SeenBossModifiers` (persists across the run for "???" reveal tracking).
6. The choice is **locked on confirm** — no undo after the panel closes.
7. The boss puzzle launches with all chosen modifiers active simultaneously.

### Modifier Eligibility

- **All 15 modifiers** are eligible at every floor. There is no tier gating.
- Board size restrictions still apply: German Whispers and Killer Cages require boards ≥ 7×7. If the boss puzzle is on a smaller board, these are excluded from the roll pool.
- Modifiers already chosen on a previous floor in the same run **can** appear again (each floor is independent).

---

## Boss Modifier Intensity

Modifier geometry density scales with run progression via the `BossModifierIntensity` enum:

| Run Number | Intensity | Multiplier | Effect on Geometry |
|:---:|----------|:---:|------------|
| 1–2 | Low | ×0.5 | Fewer lines/dots/cages — gentler constraints |
| 3–5 | Medium | ×1.0 | Standard density |
| 6–8 | High | ×1.5 | More geometry elements — tighter constraints |
| 9+ | VeryHigh | ×2.0 | Maximum density — heavily constrained puzzles |

Intensity is set in `RunDirector.BuildLevelConfig` and passed to `ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)`.

The boss gate choice panel title includes the intensity label (e.g. "Boss Gate — High Intensity").

The **Crimson Fan** relic reduces intensity by one step (e.g. High → Medium).

---

## Fog of War — Special Behaviour

Fog of War is unique among modifiers because it affects **visibility** rather than placement rules:

- All non-given cells start fogged.
- ~⅓ of given cells have their adjacent neighbors pre-revealed.
- Fogged cells block digit input (player sees "This cell is hidden by fog").
- Placing a **correct** digit calls `RevealAdjacentFog()` — clears fog on the placed cell and its 4 orthogonal neighbors.
- On puzzle completion, all remaining fog is cleared instantly.
- The `FogOfWarRule` constraint always returns `true` — fog doesn't restrict which digit is valid, only whether the cell is accessible.
- The **Lantern of Clarity** item temporarily disables fog (Normal: 3 moves, Rare: 6, Epic: 10).

---

## Constraint Rule Architecture

All modifier rules implement `IOrderedConstraintRule` and are registered through `SudokuConstraintEngine` in a deterministic execution order defined by `ConstraintRuleCategory`:

| Order | Category | Modifiers |
|:---:|----------|-----------|
| 0 | BaseSudoku | Row / Column / Region (always active) |
| 1 | Region | (reserved for future region-based modifiers) |
| 2 | GlobalNegative | Antiknight, Nonconsecutive |
| 3 | Line | German Whispers, Dutch Whispers, Parity Lines, Renban Lines, Palindrome, Thermo, Between Lines |
| 4 | Dot | Difference Kropki, Ratio Kropki |
| 5 | Arithmetic | Killer Cages, Arrow Sums |
| 6 | CellLevel | Even Odd |
| 7 | FogPostProcess | Fog of War (visibility only; does not restrict placement) |

### Validation Path

- `RunDirector.PlaceNumber()` checks `Solution[row,col] == value` (correctness).
- `SudokuValidator.IsMoveValid()` accepts an optional `SudokuConstraintEngine` for modifier-aware pencil mark validation.
- `SudokuConstraintEngine.ValidateAll()` iterates all registered rules in category/order sequence.

---

## Geometry Generation

`ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` produces a `ModifierOverlayData` object from the solved board. The `intensity` parameter scales target counts.

| Modifier | Generator Strategy | Base Target Count | Intensity Scaled |
|----------|--------------------|:---:|:---:|
| German / Dutch Whispers | Random walk along adjacent cells satisfying the difference constraint | 2–4 lines | Yes |
| Parity Lines | Random walk along cells with alternating parity | 2–4 lines | Yes |
| Renban Lines | Random walk ensuring consecutive digit set constraint | 2–3 lines | Yes |
| Palindrome | Random walk generating palindromic digit sequences | 2–3 lines | Yes |
| Thermo | Random walk of strictly increasing digits from bulb | 2–3 lines | Yes |
| Between Lines | Select two endpoint cells, walk between them satisfying between constraint | 2–3 lines | Yes |
| Difference Kropki | Scan all adjacent pairs for diff=1; shuffle and pick subset | 6–12 dots | Yes |
| Ratio Kropki | Scan all adjacent pairs for 1:2 ratio; shuffle and pick subset | 6–12 dots | Yes |
| Killer Cages | Grow connected regions with no repeating digits; compute sum | 3–5 cages | Yes |
| Arrow Sums | Place circle on high-value cell; grow path summing to circle value | 2–3 arrows | Yes |
| Even Odd | Place CellMarkers on cells matching parity criteria | 8–16 markers | Yes |
| Nonconsecutive | No geometry (global rule applied to all adjacent pairs) | — | No |
| Antiknight | No geometry (global rule applied to all knight-move pairs) | — | No |
| Fog of War | Fog all non-given cells; reveal neighbors of ~⅓ of given cells | Full board | No |

---

## Data Structures

### ModifierOverlayData

Central container holding all generated modifier geometry for the current puzzle:

```
Lines          → List<ModifierLine>       (whispers, parity, renban, palindrome, thermo, between)
Arrows         → List<ArrowConstraint>    (circle + path cells)
Cages          → List<KillerCage>         (cell list + sum)
Dots           → List<KropkiDot>          (cell pair + dot type)
CellMarkers    → List<CellMarker>         (even/odd markers)
FogCells       → HashSet<long>            (packed row/col coordinates)
```

### Supporting Types

| Type | Fields | Used By |
|------|--------|---------|
| `CellCoord` | Row, Col | All modifiers |
| `ModifierLine` | Type (LineType enum), Cells | Line constraints |
| `ArrowConstraint` | Circle (CellCoord), Path (list) | Arrow Sums |
| `KillerCage` | Sum (int), Cells (list) | Killer Cages |
| `KropkiDot` | CellA, CellB, Type (DotType: White/Black) | Kropki dots |
| `CellMarker` | Cell, Type (MarkerType: Even/Odd) | Even Odd |

### LineType Enum (7 values)

```
GermanWhispers, DutchWhispers, ParityLine, RenbanLine,
Palindrome, Thermo, BetweenLine
```

### BossModifierId Enum (15 values)

```
FogOfWar, ArrowSums, GermanWhispers, DutchWhispers,
ParityLines, RenbanLines, KillerCages, DifferenceKropki,
RatioKropki, Palindrome, Thermo, BetweenLines,
EvenOdd, Nonconsecutive, Antiknight
```

---

## Visual Rendering

Overlays are drawn on a `GridOverlay` RectTransform parented above the Sudoku grid. All overlay elements are non-interactive (`raycastTarget = false`).

| Overlay Element | Colour | Technique |
|----------------|--------|-----------|
| German Whispers lines | Green (0.20, 0.72, 0.30, 0.55) | Rotated Image rectangles between cell centers + dots on cells |
| Dutch Whispers lines | Teal (0.10, 0.95, 0.78, 0.75) | Same as above |
| Parity lines | Blue (0.30, 0.40, 0.85, 0.55) | Same as above |
| Renban lines | Pink (0.80, 0.35, 0.65, 0.55) | Same as above |
| Palindrome lines | Grey (0.60, 0.60, 0.60, 0.55) | Same as above |
| Thermo lines | Warm gradient (bulb → tip) | Circle at bulb + line segments |
| Between Lines | White gradient (0.90, 0.90, 0.90, 0.55) | Circles at endpoints + line between |
| Kropki white dots | White hollow (outer ring + transparent inner) | Circle at midpoint between cells |
| Kropki black dots | Black filled (0.10, 0.10, 0.10, 0.90) | Circle at midpoint between cells |
| Arrow circles | Grey hollow (outer ring + transparent inner) | Large circle on circle cell |
| Arrow paths | Grey (0.55, 0.55, 0.55, 0.45) | Lines between consecutive path cells + endpoint dot |
| Killer cage borders | White (0.90, 0.90, 0.90, 0.50) | Tint existing cell borders at cage edges |
| Killer cage sums | `CageSumBg` (Image) + `CageSumText` (Text child) | Top-left corner of top-left cage cell |
| Even markers | Blue squares (0.35, 0.65, 0.90, 0.55) | Square Image on cell |
| Odd markers | Orange circles (0.90, 0.55, 0.20, 0.55) | Circle Image on cell |
| Fog cells | Near-black (0.06, 0.06, 0.08, 1.00) | Cell background color override; hides digit and pencil text |

### Circle Overlay Rendering

All circle overlays (arrow bulbs, Kropki dots) use `BuildCircleSprite()` — a procedural 64×64 antialiased circle texture. The old `Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd")` was removed as it caused rendering issues.

---

## Endless Zen Mode Integration

Endless Zen mode draws from all 15 modifiers except German Whispers (excluded due to extreme constraint difficulty in random generation):

- Depth < 10: 1 random modifier per level.
- Depth ≥ 10: 2 random modifiers per level.
- Board size restrictions apply (Killer Cages requires ≥ 7×7).

---

## Floor Progression Arc

The boss encounter maps to the 5-floor garden run structure:

| Floor | Garden Theme | Grid Size | Boss Modifiers Active | Intensity |
|:---:|-------------|:---:|:---:|:---:|
| 1 | Bamboo Courtyard | 5×5–6×6 | 1 | Low |
| 2 | Moss Garden | 6×6–7×7 | 2 | Low–Medium |
| 3 | Koi Terrace | 7×7–8×8 | 3 | Medium |
| 4 | Stone Lantern Walk | 8×8–9×9 | 4 | Medium–High |
| 5 | Shrine Summit | 9×9 | 5 | High–VeryHigh |

---

## Boss Modifier Serialization

Boss modifier state is serialized for save/resume support:

- `RunState.HasChosenBossModifier` (bool) + `RunState.ChosenBossModifierId` (BossModifierId) — replaces nullable `BossModifierId?` for JSON compatibility
- `RunState.SeenBossModifierList` (List<BossModifierId>) — serializable version of `SeenBossModifiers` HashSet
- `RunAutoSaveCoordinator.Save` calls `SyncSeenModifiersToList()` before building the save envelope
- `RunResumeService.TryResumeFromSave` restores boss modifier fields and reconstructs the HashSet

---

## Modifier Descriptions

All 15 modifiers have rich descriptions available via `GetBossModifierDescription()` including colour coding and examples. These are shown:
- In the boss gate choice panel
- In the tutorial modifier toggle tooltips
- In the in-run HUD modifier indicator

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Sudoku/ModifierModels.cs` | Data structures (CellCoord, ModifierLine, ArrowConstraint, KillerCage, KropkiDot, CellMarker, ModifierOverlayData) |
| `Assets/Scripts/Sudoku/ConstraintRules.cs` | 15 IOrderedConstraintRule implementations |
| `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | Geometry generation from solved boards (accepts intensity parameter) |
| `Assets/Scripts/Sudoku/ModifierFactory.cs` | Maps BossModifierId → constraint rule instances (15 cases) |
| `Assets/Scripts/Sudoku/SudokuConstraintEngine.cs` | Central rule engine with ValidateAll |
| `Assets/Scripts/Boss/BossService.cs` | Modifier pool construction, choice rolling, ModifierData for all 15 |
| `Assets/Scripts/Run/RunDirector.cs` | StartLevel wiring (overlay generation, engine creation, fog reveal, intensity assignment) |
| `Assets/Scripts/UI/PrototypeRunScreenController.cs` | Visual overlay rendering, fog masking, input blocking, modifier descriptions |
| `Assets/Scripts/Core/GameEnums.cs` | BossModifierId (15 values), BossModifierIntensity, ConstraintRuleCategory enums |
| `Assets/Scripts/Core/RuntimeModels.cs` | Boss modifier serialization fields in RunState |
| `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | SyncSeenModifiersToList before save |
| `Assets/Scripts/Save/RunResumeService.cs` | Restore boss modifier state on resume |
| `Assets/Scripts/Tutorial/TutorialModeService.cs` | Modifier descriptions and board-size validation for tutorial |

---

## Extended Modifier Library (Future Content)

The following modifiers are cataloged for potential future integration. They are **not implemented** and not in the active pool.

### Global / Negative Constraints (not implemented)

| Modifier | Rule |
|----------|------|
| X Sudoku | Main diagonals are extra regions |
| Hyper Sudoku / Windoku | Four additional 3×3 regions |
| Disjoint Groups | Same-position cells across boxes cannot share digits |
| Antiking | King-move cells cannot share digits |

### Line Constraints (not implemented)

| Modifier | Rule |
|----------|------|
| Alternating Parity | Digits strictly alternate odd/even on the line |
| Nabner | Digits neither repeat nor are consecutive with any other digit on the line |
| Entropic Lines | Any 3 consecutive digits include one from each {1-3}, {4-6}, {7-9} |
| Modular Lines | Any 3 consecutive digits belong to different residue classes mod 3 |
| Zipper Lines | Equidistant digit pairs from center sum to the same value |
| Region Sum Lines | Line segments divided by box borders each sum equally |
| 10-Lines / N-Lines | Line segments each sum to exactly 10 (or N) |

### Thermo Variants (not implemented)

| Variant | Rule |
|---------|------|
| Slow Thermo | Non-strict increase (≥ previous) |
| Fast Thermo | Increase by ≥ N per step |
| Ambiguous Thermo | No marked bulb |

### Arrow Variants (not implemented)

| Variant | Rule |
|---------|------|
| Arrow Average | Path average equals bulb |
| Arrow Product | Path product equals bulb |
| Pill Arrow | Two-cell bulb forming two-digit number |
| Double Arrow | Two circles, path sum equals both circle sum |

### Outside-Grid Clues (not implemented)

| Modifier | Rule |
|----------|------|
| Little Killer | Diagonal sum clues |
| X-Sums | Sum of first X digits where X is the first digit |
| Sandwich | Sum between 1 and 9 |

### Cell-Level (not implemented)

| Modifier | Rule |
|----------|------|
| Quadruples | Corner circle digits must appear in surrounding cells |
| Greater/Less Than | Chevron between cells indicating relative size |
| Fortress | Grey cells must be greater than adjacent white cells |
| XV Pairs | X = sum 10, V = sum 5 between cells |
| Counting Circles | Circled cell digit = count of circles containing that digit |

---

## Resolved Design Decisions

### Multi-Modifier Solvability
Puzzles with multiple active modifiers are **not required to be logically solvable**. Conflicting modifier combinations (e.g. Nonconsecutive + Difference Kropki) or fog-obscured constraint geometry are intentional — items and relics exist to help the player through impossible-seeming states. The generator does not validate logical solvability.

### Modifier Pool Rolling — Equal Weights
All 15 modifiers have **equal probability** in the boss choice roll. There is no difficulty-based weighting or floor-based frequency adjustment. This is intentional — the variety and unpredictability are core to the roguelike experience.

### Endless Zen — Full Modifier Pool
Endless Zen includes **all 15 modifiers** with no exclusions. Board size restrictions still apply (German Whispers and Killer Cages require ≥ 7×7). Nonconsecutive and Antiknight (no visible geometry) display a text indicator in the HUD so the player knows the global constraint is active.

### Boss Gate Multi-Select UI
The boss gate panel supports multi-select as specified in `PathSystem_GardenOverview.md`. Floor N shows N+1 modifier cards; the player taps to toggle selection (highlighted border = selected); a confirm button activates when exactly N are selected.

---

## Modifier Overlay Layering Strategy

When 3+ modifiers with geometry are active simultaneously, overlays must remain legible. The following strategy ensures the player can always identify which overlay belongs to which modifier.

### Layer Priority Order

Overlays render in a fixed z-order (back to front):

| Z-Order | Layer | Modifiers |
|:---:|-------|-----------|
| 0 (back) | Cell background | Fog of War, Even/Odd cell markers |
| 1 | Cage borders | Killer Cages (dashed outlines + sum labels) |
| 2 | Dots | Difference Kropki (white), Ratio Kropki (black) |
| 3 | Lines — warm | Thermo, Between Lines, Arrow paths |
| 4 | Lines — cool | German Whispers, Dutch Whispers, Parity, Renban, Palindrome |
| 5 (front) | Arrow/Thermo bulbs | Circle endpoints sit above all lines |

### Colour Uniqueness

Each modifier uses a **unique colour** (already defined in the Visual Rendering table). No two active modifiers share the same hue. When rendering, colours are preserved at full saturation — no automatic dimming.

### Modifier Legend Panel

A collapsible **legend panel** is shown in the top-right of the puzzle screen when 2+ modifiers are active:

- Each active modifier listed with its colour swatch + name
- Tapping a modifier entry in the legend **highlights only that modifier's geometry** (all others dim to 15% opacity) for 3 seconds, then restores
- This allows the player to isolate and inspect individual constraint layers

### Geometry Spatial Separation

`ModifierGeometryGenerator` applies spatial separation when multiple line-type modifiers are active:

- Each modifier's line generation starts from a **different random seed region** of the board (e.g. modifier 1 starts top-left, modifier 2 starts bottom-right)
- Line paths are biased away from cells already used by another modifier's lines (soft avoidance, not hard constraint)
- This reduces but does not eliminate overlap — visual clarity is primarily handled by the legend panel

### Overlap Fallback

When geometry physically overlaps (same cell used by multiple lines), the **higher z-order line renders on top**. The lower line remains visible at reduced opacity (50%) in the overlapping segment. Dots always render above lines.

---

## Modifier Legend Panel

### Layout & Position

- **Collapsed state:** Small stacked-layers icon button in the **top-right corner** of the puzzle screen, next to existing HUD elements.
- **Expanded state:** Vertical panel dropping down from the button, width ~120px, semi-transparent dark background `(0.05, 0.05, 0.08, 0.85)` with rounded corners.
- Panel auto-collapses after **5 seconds** of no interaction.

### Entry Design

Each modifier entry is a horizontal row:

```
[ ■ colour swatch 12×12 ] [ Modifier Name (truncated) ]
```

- Colour swatch matches the modifier's overlay colour from the Visual Rendering table.
- For no-geometry modifiers (Nonconsecutive, Antiknight): swatch shows a **"∅" icon** with white text instead.
- Entries listed in the same z-order as the layering strategy (back → front).

### Tap-to-Isolate Interaction

1. Player taps a legend entry.
2. **All other modifier geometry** dims to 15% opacity.
3. The selected modifier's geometry stays at full opacity + gets a brief pulse animation (scale 1.0 → 1.05 → 1.0 over 0.2s) to draw the eye.
4. The selected entry in the legend gets a highlight border.
5. After **3 seconds**, all layers restore to full opacity automatically.
6. Tapping the same entry again (or tapping anywhere on the board) cancels isolation early.

### Edge Cases

- **1 modifier active (Floor 1):** Legend still shows but only 1 entry — tap-to-isolate is a no-op (nothing to dim).
- **Fog of War active:** Fog layer is excluded from dimming — dimming it would reveal hidden cells.
- **5 modifiers (Floor 5):** Panel grows to ~5 rows — fits within screen height at 120px width.

### Implementation Notes

- Built in `PrototypeRunScreenController` via `BuildModifierLegend(List<BossModifierId> activeModifiers)`.
- Legend is a child of the existing `GridOverlay` parent (or a sibling at the same level).
- Overlay GameObjects grouped by modifier ID in a `Dictionary<BossModifierId, List<GameObject>>` so isolation can target them by key.
- Isolation uses a coroutine: set `CanvasGroup.alpha = 0.15f` on non-selected groups, restore after 3s.
- Toggle button sets legend panel `GameObject.SetActive(!active)`.

---



---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Linked Systems |
|--------|-------|----------------|
| REQ-BOSS-001 | LINE_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-002 | DOT_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-003 | ARITHMETIC_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-004 | CELLLEVEL_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-005 | GLOBALNEGATIVE_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-006 | FOGPOSTPROCESS_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-007 | BOSS_RUN_SETUP | BossMechanicsSystem,RunDirector |
| REQ-BOSS-008 | ROLLING_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-009 | INTENSITY_SCALING | BossMechanicsSystem,RunDirector |
| REQ-BOSS-010 | CHOICE_FLOW | BossMechanicsSystem,RunDirector |
| REQ-BOSS-011 | VALIDATION_PATH | BossMechanicsSystem,SudokuConstraintEngine,RunDirector |
| REQ-BOSS-012 | GAMEPLAY_TRIGGERING | BossMechanicsSystem,EventManager |
| REQ-BOSS-013 | OVERLAY_RENDERING | BossMechanicsSystem,RenderManager |
| REQ-BOSS-014 | MODIFIER_DISPLAY_UPDATES | BossMechanicsSystem,UIManager |
| REQ-BOSS-015 | FULLY_IMPLEMENTED_MODIFIERS | BossMechanicsSystem |
| REQ-BOSS-016 | LINKING_OF_GEOMETRIES | BossMechanicsSystem,SudokuConstraintEngine |
| REQ-BOSS-017 | VALIDATION_PATH_2 | BossMechanicsSystem,SudokuConstraintEngine,RunDirector |
| REQ-BOSS-018 | MULTIPLEXED_MODIFIERS | BossMechanicsSystem,SudokuConstraintEngine |
| REQ-BOSS-019 | LINKING_OF_GEOMETRIES_2 | BossMechanicsSystem,SudokuConstraintEngine |
| REQ-BOSS-020 | OVERLAY_RENDERING_2 | BossMechanicsSystem,RenderManager |
| REQ-BOSS-021 | GAMEPLAY_TRIGGERING_2 | BossMechanicsSystem,EventManager |
| REQ-BOSS-022 | VALIDATION_PATH_3 | BossMechanicsSystem,SudokuConstraintEngine,RunDirector |
| REQ-BOSS-023 | FULLY_IMPLEMENTED_MODIFIERS_2 | BossMechanicsSystem |
| REQ-BOSS-024 | LINKING_OF_GEOMETRIES_3 | BossMechanicsSystem,SudokuConstraintEngine |
| REQ-BOSS-025 | MODIFIER_DISPLAY_UPDATES_2 | BossMechanicsSystem,UIManager |
| REQ-BOSS-026 | OVERLAY_RENDERING_3 | BossMechanicsSystem,RenderManager |
| REQ-BOSS-027 | GAMEPLAY_TRIGGERING_3 | BossMechanicsSystem,EventManager |
| REQ-BOSS-028 | VALIDATION_PATH_4 | BossMechanicsSystem,SudokuConstraintEngine,RunDirector |
| REQ-BOSS-029 | FULLY_IMPLEMENTED_MODIFIERS_3 | BossMechanicsSystem |
| REQ-BOSS-030 | LINKING_OF_GEOMETRIES_4 | BossMechanicsSystem,SudokuConstraintEngine |
| REQ-BOSS-031 | MODIFIER_DISPLAY_UPDATES_3 | BossMechanicsSystem,UIManager |
| REQ-BOSS-032 | OVERLAY_RENDERING_4 | BossMechanicsSystem,RenderManager |
| REQ-BOSS-033 | GAMEPLAY_TRIGGERING_4 | BossMechanicsSystem,EventManager |
| REQ-BOSS-034 | VALIDATION_PATH_5 | BossMechanicsSystem,SudokuConstraintEngine,RunDirector |
| REQ-BOSS-035 | FULLY_IMPLEMENTED_MODIFIERS_4 | BossMechanicsSystem |
