# Boss Mechanics System

**Version:** 1.9 | **Date:** 2026-04-01 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.9 | 2026-04-01 | implemented | Corrected line adjacency spec: diagonal line connectivity is **not yet implemented**. `ModifierGeometryGenerator.Dirs` uses 4 orthogonal directions only. Spec v1.2 claim of 8-directional connectivity was aspirational; corrected to reflect actual implementation. |
| 1.8 | 2026-03-29 | implemented | Extended modifier pool implemented: 15 new BossModifierId values (IDs 15–29) promoted from Extended Library to active pool. Covers Global/Negative (Antiking, AntiBishop, NonconsecDiagonal, DistanceGe2, EntropyGlobal, ModularRegions), Line Extended (ConsecutiveLine, SlowThermo, UniqueSetLine), Dots (FullKropki, SumKropki), Pairs (GreaterLessThan, XVPairs), Cell (PrimeCells, FortressCells). New enums: PairConstraintType (GreaterThan/LessThan/SumX/SumV/SumK), LineType extended (+ConsecutiveLine/SlowThermo/UniqueSetLine), MarkerType extended (+Prime/Fortress). New model: AdjacentPairConstraint; KropkiDot.SumValue; ModifierOverlayData.FullKropkiNegativeInference. All 15 have constraint rules, geometry generators, BossService entries (name/description/tier/impact), tutorial toggles (panel reorganised to 6 cols × up to 9 rows), and visual rendering. |
| 1.7 | 2026-03-29 | implemented | Status updated to implemented. All 15 active modifiers confirmed present in tutorial panel (TutorialMenuController.GetConfig allModifiers array). Extended Modifier Library (#16–#90+) is explicitly future content — not planned for active pool in current release. Tutorial saves excluded from Resume Game flow: RunAutoSaveCoordinator.Save skips TutorialMode runs; SaveFileService.HasActiveRun ignores TutorialMode ActiveRunState. |
| 1.6 | 2026-03-28 | review | Extended Modifier Library reformatted to match active pool table structure (# / Modifier / Visual / Rule / Min Size columns, ### category headers, bold names). |
| 1.5 | 2026-03-29 | implemented | Floor modifier timing fixed: Floor 2 (not Floor 3) now receives 1 floor modifier. Formula: floor-index N → N modifiers (Floor 0 = 0, Floor 1 = 1, …). Modifier geometry pre-solve fix: Kropki dots and whisper lines skip cells where both endpoints are given. Line overlay rework: circles-per-cell rendering with thin spine replacing the dot+line visual. Anti-Knight forbidden-cell highlighting added: cells a knight's move away from selected are tinted purple when that value is already placed. |
| 1.4 | 2026-03-28 | under review | Extended Modifier Library expanded: 9 new categories (Global/Negative, Line, Region/Area, Adjacency/Pair, Arithmetic/Cage, Cell/Value, Outside Clue, Movement/Path, Meta/Exotic) with min-size recommendations and design notes. Full Kropki added as adjacency variant. Existing stub entries merged into new structure. |
| 1.3 | 2026-03-28 | implemented | Boss modifier choice scaling changed: Floor N offers N+2 options, player picks N+1 (Floor 0 = 1 of 2, Floor 1 = 2 of 3, etc.). Previously used Max(3,N+1)/Max(2,N). Unseen modifiers show "???" in boss gate UI. Boss gate now correctly opens the puzzle after confirmation without re-triggering the gate. |
| 1.2 | 2026-03-23 | implemented | Line adjacency: lines may connect orthogonally or diagonally adjacent cells; lines must not cross themselves. Updated line constraint rules, geometry generation, and rendering sections. |
| 1.1 | 2026-03-23 | implemented | Floor modifier system: floors 2–5 apply persistent modifiers to all puzzles. Boss choice scaling unchanged. Added Floor Modifier section. Implemented via BossService.RollFloorModifiers, RunDirector wiring, multi-select boss gate UI, floor modifier banner, modifier info box. |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Modifiers add visual overlays to the puzzle grid and enforce additional constraints beyond standard Sudoku rules. All 15 modifiers are eligible from any floor — there is no tier gating.

Modifiers appear in two ways:

1. **Floor Modifiers** — Starting from Floor 2, a set of modifiers is rolled automatically when the player enters a new floor. These apply to **every puzzle on that floor** (normal, elite, and boss). The count increases each floor (0 → 1 → 2 → 3 → 4).

2. **Boss Modifiers** — Before each boss puzzle, the player chooses additional modifiers from a choice panel (pick N from N+1, scaling with floor). These are **stacked on top of** the floor modifiers for the boss puzzle only.

Modifiers the player has never encountered before are displayed as **"???"** until played.

---

## Active Modifier Pool (30 Implemented)

All 30 modifiers are fully implemented with constraint rules, geometry generation, overlay rendering, and tutorial descriptions.

### Line Constraints

**General line rules** (apply to all 7 line modifiers):
- Lines connect cells that are **orthogonally adjacent** (4-directional: up, down, left, right). **Diagonal adjacency is not yet implemented** — `ModifierGeometryGenerator.Dirs` uses 4 directions only.
- A line **must not cross itself** — no cell may appear more than once on the same line, and line segments must not intersect.

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 1 | **German Whispers** | Green lines (0.20, 0.72, 0.30, 0.55) | Adjacent cells on each line must differ by ≥5. **Restricted to boards ≥ 7×7.** |
| 2 | **Dutch Whispers** | Bright teal lines (0.10, 0.95, 0.78, 0.75) | Adjacent cells on each line must differ by ≥4 |
| 3 | **Parity Lines** | Blue lines (0.30, 0.40, 0.85, 0.55) | Adjacent cells on each line must alternate odd/even parity |
| 4 | **Renban Lines** | Pink lines (0.80, 0.35, 0.65, 0.55) | Cells on each line form a consecutive set in any order (no repeats) |
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

## Extended Modifier Pool (IDs 15–29, Implemented)

These 15 modifiers extend the active pool beyond the original 15. All are eligible for floor rolls and boss choices. Tutorial panel reorganised to 6 columns to fit all 30 toggles.

### Global / Negative Constraints (Extended)

| ID | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 15 | **Anti-King** | No geometry (global rule) | No two cells a chess king's move apart (orthogonal or diagonal) may contain the same digit | 5×5 |
| 16 | **Anti-Bishop** | No geometry (global rule) | No two cells on the same diagonal (any length) may contain the same digit | 6×6 |
| 17 | **Nonconsec. Diagonal** | No geometry (global rule) | Diagonally adjacent cells cannot contain consecutive digits | 5×5 |
| 18 | **Distance ≥ 2** | No geometry (global rule) | Equal digits must be at Chebyshev distance ≥ 2 — no king-adjacent equal digits (identical to Anti-King; implemented as separate modifier for clarity) | 6×6 |
| 19 | **Entropy** | No geometry (global rule) | Every 3 consecutive cells in any row or column must contain one digit from {1–3}, one from {4–6}, one from {7–9} | 7×7 |
| 20 | **Modular Regions** | No geometry (global rule) | Every region must contain at least one digit from each set {1–3}, {4–6}, {7–9} | 6×6 |

### Line Constraints (Extended)

| ID | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 21 | **Consecutive Line** | Orange line (0.95, 0.65, 0.15, 0.70) | Adjacent cells on line must differ by exactly 1. Example: 3-4-5 or 7-6-5. | 5×5 |
| 22 | **Slow Thermo** | Purple line with open-ring bulb (0.70, 0.30, 0.80, 0.70) | Non-strict increase from bulb — each cell must be ≥ the previous. Example: 2-2-3-5. | 5×5 |
| 23 | **Unique Set Line** | Sky-blue line with ring endpoints (0.20, 0.70, 0.95, 0.65) | No digit may repeat anywhere on the line. | 5×5 |

### Adjacency / Pair Constraints

| ID | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 24 | **Full Kropki** | White/black dots on all valid pairs; absence = neither | All adjacent pairs that differ by 1 show a white dot; all 1:2 ratio pairs show a black dot. Absence of a dot forbids both relations. Sets `FullKropkiNegativeInference = true`. | 6×6 |
| 25 | **Sum Dot** | Grey dot with sum label | A dot labelled K between two adjacent cells means they sum to K. | 5×5 |
| 26 | **Greater/Less Than** | Orange > or < chevron between cells | A chevron shows which of two adjacent cells must be the larger digit. | 5×5 |
| 27 | **XV Pairs** | Cyan "X" (sum 10) or "V" (sum 5) between cells | X: two adjacent cells sum to 10. V: two adjacent cells sum to 5. | 5×5 |

### Cell / Value Semantics

| ID | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 28 | **Prime Cells** | Gold circle + "P" label on cell | Marked cells must contain a prime digit: 2, 3, 5, or 7. | 5×5 |
| 29 | **Fortress Cells** | Grey shaded square on cell | Shaded cells must be strictly greater than every orthogonally adjacent non-shaded cell. | 6×6 |

### Constraint Rule Registration

| ID | Modifier | Rule Class | Category |
|:---:|----------|-----------|----------|
| 15 | Anti-King | `AntikingRule` | GlobalNegative |
| 16 | Anti-Bishop | `AntiBishopRule` | GlobalNegative |
| 17 | Nonconsec. Diagonal | `NonconsecDiagonalRule` | GlobalNegative |
| 18 | Distance ≥ 2 | `DistanceGe2Rule` | GlobalNegative |
| 19 | Entropy | `EntropyGlobalRule` | GlobalNegative |
| 20 | Modular Regions | `ModularRegionsRule` | Region |
| 21 | Consecutive Line | `ConsecutiveLineRule` | Line |
| 22 | Slow Thermo | `SlowThermoRule` | Line |
| 23 | Unique Set Line | `UniqueSetLineRule` | Line |
| 24 | Full Kropki | `FullKropkiRule` | Dot |
| 25 | Sum Dot | `SumKropkiRule` | Dot |
| 26 | Greater/Less Than | `GreaterLessThanRule` | Arithmetic |
| 27 | XV Pairs | `XVPairsRule` | Arithmetic |
| 28 | Prime Cells | `PrimeCellsRule` | CellLevel |
| 29 | Fortress Cells | `FortressCellsRule` | CellLevel |

---

## Floor Modifier System

Starting from Floor 2, modifiers are applied to **all puzzles** on the floor — normal, elite, and boss. These are rolled automatically when the player enters the floor and persist until the floor is cleared.

### Floor Modifier Scaling

| Floor | Floor Modifiers | Effect |
|:---:|:---:|--------|
| 1 | 0 | No floor modifiers — gentle introduction |
| 2 | 1 | One modifier active on all puzzles |
| 3 | 2 | Two modifiers on all puzzles — complexity builds |
| 4 | 3 | Three modifiers on all puzzles — near-full load |
| 5 | 4 | Four modifiers on all puzzles — maximum floor pressure |

Formula: Floor N has **N−1** floor modifiers (Floor 1 = 0).

### Floor Modifier Rolling

1. On **floor entry**, `BossService.RollFloorModifiers(floor, runState)` draws N−1 distinct modifiers from the full pool of 15.
2. Floor modifiers are stored in `RunState.ActiveFloorModifiers` and persisted in save data.
3. Board size restrictions apply: German Whispers and Killer Cages require boards ≥ 7×7. If the floor's minimum board size is smaller, these are excluded from the roll pool.
4. Floor modifiers from previous floors do **not** exclude modifiers from the current floor's pool — each floor is independent.
5. Floor modifiers are added to `RunState.SeenBossModifiers` for "???" reveal tracking.

### Floor Modifier Display

- A **floor modifier banner** is shown when entering a new floor, listing the active modifiers for the floor.
- The modifier description box (under the numpad "Mode" button) shows floor modifiers on all puzzles (not just boss puzzles).
- Floor modifiers use the same overlay rendering and constraint rules as boss modifiers.

---

## Boss Gate Choice Flow

### Choice Scaling per Floor

In addition to floor modifiers, the boss puzzle adds **boss-chosen modifiers** via a choice panel. The number of options and selections scales with floor:

| Floor | Boss Options Shown | Player Picks | Total Active on Boss (Floor + Boss) |
|:---:|:---:|:---:|:---:|
| 1 | 3 | 2 | 0 + 2 = **2** |
| 2 | 3 | 2 | 1 + 2 = **3** |
| 3 | 4 | 3 | 2 + 3 = **5** |
| 4 | 5 | 4 | 3 + 4 = **7** |
| 5 | 6 | 5 | 4 + 5 = **9** |

Boss choice formula: Floor N offers **N+1** options (min 3), player picks **N** (min 2).

### Flow Steps

1. Player reaches the **Boss Gate** node on the garden path.
2. `BossService.RollBossChoices(floor, runState)` selects N+1 distinct modifiers from the pool, **excluding modifiers already active as floor modifiers** (no duplicates).
3. A modal panel displays all options. Unseen modifiers show **"???"** instead of their name and description.
4. Player selects N modifiers → stored in `RunState.ChosenBossModifiers` and applied to the boss puzzle config.
5. Selected modifiers are added to `RunState.SeenBossModifiers` (persists across the run for "???" reveal tracking).
6. The choice is **locked on confirm** — no undo after the panel closes.
7. The boss puzzle launches with **floor modifiers + boss-chosen modifiers** active simultaneously.

### Modifier Eligibility

- **All 15 modifiers** are eligible at every floor. There is no tier gating.
- Board size restrictions still apply: German Whispers and Killer Cages require boards ≥ 7×7. If the boss puzzle is on a smaller board, these are excluded from the roll pool.
- Modifiers already active as **floor modifiers** are excluded from the boss choice pool (no duplicates within a floor).
- Modifiers chosen on a previous floor **can** appear again on later floors (each floor is independent).

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
| 1 | Region | Modular Regions |
| 2 | GlobalNegative | Antiknight, Nonconsecutive, Anti-King, Anti-Bishop, Nonconsec. Diagonal, Distance ≥ 2, Entropy |
| 3 | Line | German Whispers, Dutch Whispers, Parity Lines, Renban Lines, Palindrome, Thermo, Between Lines, Consecutive Line, Slow Thermo, Unique Set Line |
| 4 | Dot | Difference Kropki, Ratio Kropki, Full Kropki, Sum Dot |
| 5 | Arithmetic | Killer Cages, Arrow Sums, Greater/Less Than, XV Pairs |
| 6 | CellLevel | Even Odd, Prime Cells, Fortress Cells |
| 7 | FogPostProcess | Fog of War (visibility only; does not restrict placement) |

### Validation Path

- `RunDirector.PlaceNumber()` checks `Solution[row,col] == value` (correctness).
- `SudokuValidator.IsMoveValid()` accepts an optional `SudokuConstraintEngine` for modifier-aware pencil mark validation.
- `SudokuConstraintEngine.ValidateAll()` iterates all registered rules in category/order sequence.

---

## Geometry Generation

`ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` produces a `ModifierOverlayData` object from the solved board. The `intensity` parameter scales target counts.

All line generators use **8-directional adjacency** (orthogonal + diagonal) and enforce the **no-self-crossing** constraint: each step checks that the new cell is not already on the line and that the segment does not cross an existing segment.

| Modifier | Generator Strategy | Base Target Count | Intensity Scaled |
|----------|--------------------|:---:|:---:|
| German / Dutch Whispers | Random walk along adjacent cells (8-dir) satisfying the difference constraint; no self-crossing | 2–4 lines | Yes |
| Parity Lines | Random walk (8-dir) along cells with alternating parity; no self-crossing | 2–4 lines | Yes |
| Renban Lines | Random walk (8-dir) ensuring consecutive digit set constraint; no self-crossing | 2–3 lines | Yes |
| Palindrome | Random walk (8-dir) generating palindromic digit sequences; no self-crossing | 2–3 lines | Yes |
| Thermo | Random walk (8-dir) of strictly increasing digits from bulb; no self-crossing | 2–3 lines | Yes |
| Between Lines | Select two endpoint cells, walk (8-dir) between them satisfying between constraint; no self-crossing | 2–3 lines | Yes |
| Difference Kropki | Scan all orthogonal adjacent pairs for diff=1; shuffle and pick subset | 6–12 dots | Yes |
| Ratio Kropki | Scan all orthogonal adjacent pairs for 1:2 ratio; shuffle and pick subset | 6–12 dots | Yes |
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
Lines              → List<ModifierLine>            (whispers, parity, renban, palindrome, thermo, between, + 3 extended)
Arrows             → List<ArrowConstraint>         (circle + path cells)
Cages              → List<KillerCage>              (cell list + sum)
Dots               → List<KropkiDot>               (cell pair + dot type + optional SumValue)
CellMarkers        → List<CellMarker>              (even/odd/prime/fortress markers)
PairConstraints    → List<AdjacentPairConstraint>  (chevrons and XV labels)
FogCells           → HashSet<long>                 (packed row/col coordinates)
FullKropkiNegativeInference → bool                (when true, absense of dot = neither diff=1 nor ratio=2)
```

### Supporting Types

| Type | Fields | Used By |
|------|--------|---------|
| `CellCoord` | Row, Col | All modifiers |
| `ModifierLine` | Type (LineType enum), Cells | Line constraints |
| `ArrowConstraint` | CircleCell (CellCoord), ArrowCells (list) | Arrow Sums |
| `KillerCage` | Sum (int), Cells (list) | Killer Cages |
| `KropkiDot` | CellA, CellB, IsBlack, SumValue (>0 for Sum Dot) | Kropki dots, Sum Dot, Full Kropki |
| `CellMarker` | Cell, Type (MarkerType: Even/Odd/Prime/Fortress) | Even Odd, Prime Cells, Fortress Cells |
| `AdjacentPairConstraint` | CellA, CellB, Type (PairConstraintType), Value | Greater/Less Than, XV Pairs |

### LineType Enum (10 values)

```
GermanWhispers, DutchWhispers, ParityLine, RenbanLine,
Palindrome, Thermo, BetweenLine,
ConsecutiveLine, SlowThermo, UniqueSetLine
```

### MarkerType Enum (4 values)

```
Even, Odd, Prime, Fortress
```

### PairConstraintType Enum (5 values)

```
GreaterThan, LessThan, SumX, SumV, SumK
```

### BossModifierId Enum (30 values)

```
// Original 15:
FogOfWar(0), ArrowSums(1), GermanWhispers(2), DutchWhispers(3),
ParityLines(4), RenbanLines(5), KillerCages(6), DifferenceKropki(7),
RatioKropki(8), Palindrome(9), Thermo(10), BetweenLines(11),
EvenOdd(12), Nonconsecutive(13), Antiknight(14)

// Extended 15:
Antiking(15), AntiBishop(16), NonconsecDiagonal(17), DistanceGe2(18),
EntropyGlobal(19), ModularRegions(20), ConsecutiveLine(21), SlowThermo(22),
UniqueSetLine(23), FullKropki(24), SumKropki(25), GreaterLessThan(26),
XVPairs(27), PrimeCells(28), FortressCells(29)
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
| Consecutive Line | Orange (0.95, 0.65, 0.15, 0.70) | Same line rendering as other line types |
| Slow Thermo | Purple (0.70, 0.30, 0.80, 0.70) | Line + open ring bulb at start (ring, not filled dot) |
| Unique Set Line | Sky blue (0.20, 0.70, 0.95, 0.65) | Line + ring at both endpoints |
| Sum Dot | Grey circle with white sum label | Circle + `DrawOverlayText` for sum value |
| Greater/Less Than | Orange chevron label (0.95, 0.70, 0.20, 0.90) | `DrawOverlayText` ">" or "<" at midpoint between cells |
| XV Pairs | Cyan label (0.20, 0.80, 0.90, 0.90) | `DrawOverlayText` "X" or "V" at midpoint between cells |
| Prime markers | Gold circle (0.85, 0.75, 0.15, 0.50) + "P" text | Circle + `DrawOverlayText` "P" |
| Fortress cells | Grey square (0.45, 0.45, 0.50, 0.45) | Square Image on cell (88% cell size) |
| Global negative rules | No geometry | Text indicator in HUD modifier bar only (Antiking, AntiBishop, etc.) |

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
| `Assets/Scripts/Sudoku/ConstraintRules.cs` | 30 IOrderedConstraintRule implementations |
| `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | Geometry generation from solved boards (accepts intensity parameter); 30 modifiers |
| `Assets/Scripts/Sudoku/ModifierFactory.cs` | Maps BossModifierId → constraint rule instances (30 cases) |
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

The following modifiers are cataloged for potential future integration. Entries marked **[IMPLEMENTED]** are now in the active 30-modifier pool. The table format matches the active pool: `# | Modifier | Visual | Rule | Min Size`.

**Min Size** is the smallest board on which the constraint remains interesting and non-trivial. **Visual** is a proposed rendering approach; all visuals are TBD until implementation.

---

### Global / Negative Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 16 | **X Sudoku** | No geometry (extra region highlight on diagonals) | Both main diagonals are extra regions — no digit may repeat on either diagonal | 5×5 |
| 17 | **Hyper Sudoku** | Dashed borders on 4 extra 3×3 regions | Four additional 3×3 regions overlapping the standard grid; no digit repeats within them | 9×9 |
| 18 | **Disjoint Groups** | Subtle shading per relative box position | Cells in the same relative position across all boxes cannot share a digit | 9×9 |
| 19 | **Antiking** ✅ | No geometry (global rule) | No two cells a chess king's move apart may contain the same digit | 5×5 |
| 20 | **Anti-Bishop** ✅ | No geometry (global rule) | No two cells on the same diagonal (any length) may contain the same digit | 6×6 |
| 21 | **Nonconsecutive (Diagonal)** ✅ | No geometry (global rule) | Diagonally adjacent cells cannot contain consecutive digits | 5×5 |
| 22 | **Index Parity** | No geometry (global rule) | All cells where (row + col) is even must share the same parity; same for (row + col) odd | 5×5 |
| 23 | **Modular Global (mod N)** | No geometry (global rule) | Cells in the same residue class mod N cannot repeat digits | 6×6 |
| 24 | **Distance ≥ 2** ✅ | No geometry (global rule) | Equal digits must be at least 2 cells apart in any direction (Chebyshev distance) | 6×6 |
| 25 | **Knight+King Combined** | No geometry (global rule) | Combines Antiknight + Antiking — no equal digit within a king's or knight's move | 5×5 |
| 26 | **Toroidal (Wrap-Around)** | Edge-connection indicators on grid borders | Rows and columns loop — the left edge is adjacent to the right edge; top to bottom | 6×6 |

- Antiking (#19) is the direct sibling of the active Antiknight and is the highest-priority addition.
- Anti-Bishop (#20) and Nonconsecutive Diagonal (#21) are no-geometry global rules — low implementation cost, good early-floor modifiers.
- Toroidal (#26) requires grid rendering changes to show wrap-around adjacency. Tag as late-floor or chaos-tier.
- Distance ≥ 2 (#24) generalises Nonconsecutive/Antiknight and scales well with larger boards.

---

### Line Constraints (Extended)

All extended line modifiers follow the same **8-directional adjacency, no self-crossing** rule as the active 7 line modifiers.

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 27 | **Whisper (Generalized)** | Coloured line (hue configurable) | Adjacent cells on the line differ by ≥ K, where K is set at run time | 6×6 |
| 28 | **Consecutive Line** ✅ | Orange line | Every pair of adjacent cells on the line differs by exactly 1 | 5×5 |
| 29 | **Nonconsecutive Line** | Crossed orange line | No pair of adjacent cells on the line are consecutive | 6×6 |
| 30 | **Alternating Parity Line** | Purple striped line | Digits strictly alternate odd/even along the line | 5×5 |
| 31 | **High-Low Alternating** | Bicolour line (warm/cool segments) | Digits alternate above/below the board midpoint (e.g. >5 and ≤5 on a 9×9) | 6×6 |
| 32 | **Nabner Line** | Dark red line | No digit on the line repeats or is consecutive with any other digit on the same line | 6×6 |
| 33 | **Entropic Line** | Tri-segment gradient line | Any 3 consecutive cells contain one digit from each set: {1–3}, {4–6}, {7–9} | 7×7 |
| 34 | **Modular Line** | Banded line (3 colours cycling) | Any 3 consecutive cells contain digits from different residue classes mod 3 | 6×6 |
| 35 | **Zipper Line** | Dashed line with central marker | Pairs of cells equidistant from the centre sum to the same value | 6×6 |
| 36 | **Region Sum Line** | Line with box-border tick marks | Each segment of the line divided by box borders sums to the same total | 7×7 |
| 37 | **N-Line (10-Line)** | Numbered line with segment ticks | Each box-bounded segment of the line sums to exactly N (default N=10) | 7×7 |
| 38 | **Index Line** | Line with position labels | The digit at position P on the line (1-indexed from one end) equals P | 7×7 |
| 39 | **Sum Line** | Line with total label at endpoint | The entire line sums to a fixed displayed value | 6×6 |
| 40 | **Unique Set Line** ✅ | Line with "U" endpoint marker | No digit repeats on the line, independent of Sudoku regions | 5×5 |
| 41 | **Skyscraper Line** | Line with building silhouette endpoints | Digits from each endpoint encode how many increasing "buildings" are visible from that end | 7×7 |
| 42 | **Thermo Loop** | Closed loop line with bulb marker | Digits must strictly increase cyclically around the loop — note: unsatisfiable unless relaxed to non-strict (≥) | 8×8 |
| 43 | **Slow Thermo** ✅ | Thermo line with open bulb | Non-strict increase — each cell must be ≥ the previous | 5×5 |
| 44 | **Fast Thermo** | Thermo line with double-arrow bulb | Each cell must increase by ≥ N from the previous (N configurable) | 6×6 |
| 45 | **Ambiguous Thermo** | Plain line, no bulb | Direction is unknown to the player — the line increases in one direction but neither end is marked | 6×6 |

---

### Region / Area Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 46 | **Irregular Regions (Jigsaw)** | Thick borders on irregular region shapes | Non-rectangular region shapes replace standard boxes (partially supported via region variants) | 5×5 |
| 47 | **Extra Region Overlay** | Dashed coloured border on extra regions | Additional arbitrary regions with no digit repeats, independent of standard boxes | 5×5 |
| 48 | **Region Sum Equality** | Region total labels; equality indicator | All regions (standard or extra) sum to the same total | 6×6 |
| 49 | **Region Parity Balance** | Parity shading overlay per region | Each region must contain equal counts of odd and even digits | 6×6 |
| 50 | **Region Index Rule** | Small index label per region cell | The digit at a cell equals the count of a defined feature in its region | 7×7 |
| 51 | **Connected Regions** | Connectivity arrows between equal-digit cells | All occurrences of each digit must form a single orthogonally connected group | 7×7 |

---

### Adjacency / Pair Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 52 | **Full Kropki** ✅ | White dots (diff=1) and black dots (ratio=1:2) on all valid pairs | All valid Kropki pairs between orthogonally adjacent cells are shown. Absence of a dot means the pair does **not** satisfy either relation | 6×6 |
| 53 | **Full White Kropki** | White dot on every diff=1 adjacent pair | All difference=1 pairs are marked; absence of a dot forbids diff=1 | 5×5 |
| 54 | **Full Black Kropki** | Black dot on every ratio=1:2 adjacent pair | All ratio=1:2 pairs are marked; absence of a dot forbids ratio=1:2 | 6×6 |
| 55 | **Sum Kropki** ✅ | Labelled dot between cells showing sum K | A dot indicates the two adjacent cells sum to K | 5×5 |
| 56 | **Difference ≥ K** | Coloured bar between cells | Adjacent orthogonal cells must differ by at least K | 5×5 |
| 57 | **Ratio (Generalized)** | Labelled black dot showing ratio N | One cell is exactly N times the other (not limited to 1:2) | 6×6 |
| 58 | **Inequality Chains** | Chevron symbols (> or <) between cells | Consecutive cells in a row or column are connected by > or < indicators | 5×5 |
| 59 | **Distance Pair** | Labelled connector between paired cells | The absolute difference \|A − B\| equals the taxicab distance between the two cells | 6×6 |
| 60 | **Greater/Less Than** ✅ | Single chevron (> or <) between adjacent cells | A chevron between two adjacent cells indicates which must be larger | 5×5 |
| 61 | **XV Pairs** ✅ | X or V label between adjacent cells | X: two cells sum to 10; V: two cells sum to 5 | 5×5 |

- Full Kropki (#52) is restricted to 6×6+ because on 5×5 the constraint density is overwhelming.

---

### Arithmetic / Cage Variants

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 62 | **Killer (Hidden Sum)** | Dashed cage, no sum label | Cage with no repeats; sum is hidden from the player | 7×7 |
| 63 | **Cage Product** | Dashed cage + product label (×) | Cells in a cage multiply to the displayed product; no repeats | 6×6 |
| 64 | **Cage Difference** | Two-cell cage + difference label (−) | Two-cell cage: the absolute difference of its two digits equals the label | 5×5 |
| 65 | **Cage Ratio** | Two-cell cage + ratio label (:) | Two-cell cage: the ratio of its two digits equals the label | 5×5 |
| 66 | **Magic Square Region** | Highlighted 3×3 sub-region | A 3×3 sub-region sums to 15 in every row, column, and diagonal | 7×7 |
| 67 | **Renban Cage** | Dashed cage with consecutive-set indicator | Cage digits form a consecutive set in any order | 5×5 |
| 68 | **Arrow Average** | Circle + arrow path | The digit in the circle equals the average of the digits along the arrow path | 7×7 |
| 69 | **Arrow Product** | Circle + arrow path (× symbol) | The digit in the circle equals the product of the digits along the arrow path | 6×6 |
| 70 | **Pill Arrow** | Two-cell pill bulb + arrow path | The two-cell bulb forms a two-digit number; the path sum equals that number | 8×8 |
| 71 | **Double Arrow** | Two circles connected by arrow path | Path sum equals the sum of both circle endpoint digits | 6×6 |

---

### Cell / Value Semantics

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 72 | **Prime Cells** ✅ | P-labelled cell markers | Marked cells must contain prime digits: 2, 3, 5, or 7 | 5×5 |
| 73 | **Multiples Cells** | ×N-labelled cell markers | Marked cells must contain a multiple of N | 5×5 |
| 74 | **Index Cell** | Self-referential circle marker | A cell's digit encodes the count of some defined feature in its row or column | 7×7 |
| 75 | **Mirror Cells** | Paired cell markers (matching colour) | Paired cells must contain the same digit | 5×5 |
| 76 | **Opposite Cells Sum** | Paired cell markers with sum label | Cells symmetric about the grid centre must sum to a fixed constant | 5×5 |
| 77 | **Counting Circles** | Small circle markers on cells | A circled cell's digit equals the count of circles whose value matches that digit | 6×6 |
| 78 | **Fortress** ✅ | Grey shading on fortress cells | Grey-shaded cells must be strictly greater than all orthogonally adjacent non-shaded cells | 6×6 |
| 79 | **Quadruples** | Corner circles shared by 4 adjacent cells | All digits listed inside the corner circle must appear in the 4 surrounding cells | 9×9 |
| 80 | **Distance Cell** | Cell marker with distance label | A cell's digit equals its taxicab distance to a specified target cell | 7×7 |

---

### Outside Clue Systems

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 81 | **Skyscrapers** | Numbers outside each row/column edge | A clue outside a row or column counts how many distinct increasing "buildings" are visible from that edge | 5×5 |
| 82 | **Sandwich** | Numbers outside each row/column edge | A clue gives the sum of digits strictly between two specified sentinel digits in the row or column | 6×6 |
| 83 | **X-Sums** | Numbers outside each row/column edge | The first digit equals X; the clue gives the sum of the first X digits in the row or column from that edge | 5×5 |
| 84 | **Little Killer** | Diagonal arrows outside the grid with sum labels | A clue outside the grid gives the sum along a diagonal of arbitrary length | 6×6 |
| 85 | **Battlefield** | Numbers outside each row/column edge | Clue gives the total length of consecutive digit runs in the row or column | 7×7 |
| 86 | **Outside Inequalities** | Chevrons (> or <) outside grid edges | Edge clue requires the first cell in the row or column to be greater or less than a reference value | 5×5 |

---

### Movement / Path Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 87 | **Knight Path** | Sequence of knight-move arrows through the grid | The digits 1 through N must be placed so that consecutive values are a chess knight's move apart | 7×7 |
| 88 | **Hamiltonian Path** | Numbered path arrows through all cells | Every cell is visited exactly once; consecutive path cells must contain consecutive digits | 7×7 |
| 89 | **Snake Constraint** | Highlighted snake path on grid | A defined digit sequence must form a connected, non-branching snake path through the grid | 6×6 |
| 90 | **Loop Constraint** | Highlighted loop path on grid | A set of specified digits must collectively form a single closed loop | 7×7 |

---

### Meta / Exotic Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 91 | **Entropy (Global)** ✅ | No geometry (global rule) | Every set of 3 consecutive cells in any row or column contains one low (1–3), one mid (4–6), one high (7–9) digit | 7×7 |
| 92 | **Modular Regions (mod 3)** ✅ | No geometry (global rule) | Every region must contain at least one digit from each residue class {0, 1, 2} mod 3 | 6×6 |
| 93 | **Digit Graph** | Graph overlay on specific cell pairs | Defines a graph over digits; cells whose values are adjacent in the graph must satisfy a positional rule | 8×8 |
| 94 | **Self-Referential Grid** | No geometry (global rule) | A digit encodes a count or structural property about the entire grid | 8×8 |
| 95 | **Constraint Negation Zone** | Coloured zone overlay marking inversion area | Cells inside the zone invert an active rule (e.g. a thermo decreases instead of increases within the zone) | 7×7 |

---

## Resolved Design Decisions

### Multi-Modifier Solvability
Puzzles with multiple active modifiers are **not required to be logically solvable**. This applies to both floor-modified puzzles and boss puzzles (which stack floor + boss modifiers). Conflicting modifier combinations (e.g. Nonconsecutive + Difference Kropki) or fog-obscured constraint geometry are intentional — items and relics exist to help the player through impossible-seeming states. The generator does not validate logical solvability.

### Modifier Pool Rolling — Equal Weights
All 15 modifiers have **equal probability** in both floor modifier rolls and boss choice rolls. There is no difficulty-based weighting or floor-based frequency adjustment. This is intentional — the variety and unpredictability are core to the roguelike experience.

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

### Self-Crossing Detection

Since lines use 8-directional adjacency, **segment crossing** must be checked during generation. Two diagonal segments cross if they form an X pattern (e.g. segment A1→B2 crosses segment A2→B1). The generator rejects steps that would create such crossings, ensuring every generated line has a clear, non-intersecting visual path.

### Overlap Fallback

When geometry physically overlaps (same cell used by multiple lines from **different** modifiers), the **higher z-order line renders on top**. The lower line remains visible at reduced opacity (50%) in the overlapping segment. Dots always render above lines.

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
