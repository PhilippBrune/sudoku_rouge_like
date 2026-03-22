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

## Extended Modifier Library

This section catalogs all known Sudoku variant modifier types available for integration into the boss pool, endless mode, or future content tiers. Each entry describes the rule, visual treatment, and constraint category.

---

### Global / Negative Constraints

| Modifier | Visual | Rule |
|----------|--------|------|
| **X Sudoku** | Gold diagonal lines | One or both main diagonals are extra regions — digits 1–9 cannot repeat along them |
| **Hyper Sudoku / Windoku** | Shaded 3×3 overlays | Four additional 3×3 regions (offset from standard boxes) must each contain 1–9 |
| **Disjoint Groups** | Colour-tinted cell groups | Cells at the same relative position within each box cannot share a digit across boxes |
| **Antiknight** | Knight-move dash overlay | No two cells a chess knight's move apart may contain the same digit |
| **Antiking** | King-move dash overlay | No two cells a chess king's move (diagonal or orthogonal) apart may contain the same digit |
| **Nonconsecutive** | Red tick marks between adjacent cells | No two orthogonally adjacent cells may contain consecutive digits (global rule, no visual line) |

---

### Geometry / Structure Variants

| Modifier | Visual | Rule |
|----------|--------|------|
| **Clone** | Matching shaded regions | Two or more shaded regions must contain the identical digit arrangement (same digits in corresponding cells) |
| **Gattai / Samurai** | Multi-grid overlap | Two or more overlapping grids sharing a sub-region; each sub-grid must independently satisfy Sudoku rules |

---

### Line Constraints

| Modifier | Color | Rule |
|----------|-------|------|
| **Palindrome** | Gray lines | Digits along the line read the same forward and backward |
| **Alternating Parity** | Yellow lines | Digits on the line strictly alternate odd/even |
| **Nabner** | Sage / muted green lines | Digits on the line neither repeat nor are consecutive with any other digit on the line |
| **Entropic Lines** | Peach / warm orange lines | Any 3 consecutive digits on the line must include one from {1,2,3}, one from {4,5,6}, and one from {7,8,9} |
| **Modular Lines** | Teal lines | Any 3 consecutive digits on the line must all belong to different residue classes mod 3 |
| **Zipper Lines** | Lavender lines | Pairs of digits equidistant from the line's center must sum to the same value |
| **Region Sum Lines** | Blue lines | The line is divided into segments by box borders; each segment must sum to the same value |
| **10-Lines / N-Lines** | Orange segment lines | The line is divided into segments each summing to exactly 10 (or a designer-specified N) |
| **Between Lines** | White gradient line with circles at ends | Every digit on the line must be strictly between the digits in the two endpoint circles |

---

### Thermo Variants

All thermo lines increase from a **bulb** cell. Subvariants modify the increase rule:

| Variant | Rule |
|---------|------|
| **Thermo (standard)** | Digits strictly increase from bulb to tip (each cell > previous) |
| **Slow Thermo** | Digits non-strictly increase from bulb (each cell ≥ previous; equal allowed) |
| **Fast Thermo** | Digits increase by ≥ N per step from bulb (designer sets N, e.g. 2) |
| **Ambiguous Thermo** | No bulb is marked; player must determine which end is the bulb |

---

### Arrow Variants

All arrow constraints share the same visual: a **circle (bulb)** cell and an **arrow path**. The base rule is that path digits sum to the bulb digit. Subvariants replace the sum operation:

| Variant | Rule |
|---------|------|
| **Arrow Sums (standard)** | Sum of path digits equals the bulb digit |
| **Arrow Average** | Average of path digits equals the bulb digit |
| **Arrow Product** | Product of path digits equals the bulb digit |
| **Pill Arrow** | Two-cell bulb (pill shape); path digits sum to the two-digit number formed by the pill cells |
| **Double Arrow** | Two circles connected by a shared path; the path sum equals the sum of both circle digits |
| **Split Pea Arrow** | Arrow splits at one cell; all path segments must individually satisfy the sum rule relative to the shared bulb |

---

### Outside-Grid Clues

| Modifier | Visual | Rule |
|----------|--------|------|
| **Little Killer** | Number with diagonal arrow outside the grid | The clue value is the sum of all digits along the indicated diagonal (repeats allowed on the diagonal) |
| **X-Sums** | Number outside a row/column | If the first digit in the row/column from that side is X, the clue equals the sum of the first X digits |
| **Sandwich** | Number outside a row/column | The clue is the sum of all digits strictly between the 1 and the 9 in that row/column |

---

### Cell-Level Constraints

| Modifier | Visual | Rule |
|----------|--------|------|
| **Even/Odd (Parity Cells)** | Squares on even cells, circles on odd cells | Cells marked with a square must contain an even digit; cells marked with a circle must contain an odd digit |
| **Quadruples** | Large circle at the corner of four cells | Every digit shown in the corner circle must appear at least once among the four surrounding cells |
| **Greater/Less Than** | Chevron (< or >) between adjacent cells | The pointed-to cell is smaller than the other cell |
| **Fortress** | Gray-shaded cells | Each gray cell must be greater than all of its orthogonally adjacent white (non-gray) cells |
| **XV Pairs** | "X" or "V" between adjacent cells | X: the two adjacent cells sum to 10. V: they sum to 5. Negative constraint optional: all unmarked adjacent pairs do not sum to 5 or 10 |
| **Counting Circles** | Circled cells | The digit in a circled cell equals the count of circled cells on the board that contain that digit (self-referential) |

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
| 1 | Region | X Sudoku diagonals, Hyper/Windoku extra boxes, Disjoint Groups, Clone regions |
| 2 | GlobalNegative | Antiknight, Antiking, Nonconsecutive |
| 3 | Line | German Whispers, Dutch Whispers, Parity Lines, Renban Lines, Palindrome, Alternating Parity, Nabner, Entropic, Modular, Zipper, Region Sum, 10-Lines, Between Lines, Thermo variants |
| 4 | Dot | Difference Kropki, Ratio Kropki, XV Pairs, Greater/Less Than |
| 5 | Arithmetic | Killer Cages, Arrow Sums (all arrow variants), Little Killer, X-Sums, Sandwich |
| 6 | CellLevel | Even/Odd, Quadruples, Fortress, Counting Circles |
| 7 | FogPostProcess | Fog of War (visibility only; does not restrict placement) |

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
Lines          → List<ModifierLine>       (whispers, parity, renban, palindrome, nabner, entropic, modular, zipper, region sum, 10-lines, between, thermo variants)
Arrows         → List<ArrowConstraint>    (circle/pill + path cells, variant type flag)
Cages          → List<KillerCage>         (cell list + sum)
Dots           → List<KropkiDot>          (cell pair + dot type)
CellMarkers    → List<CellMarker>         (even/odd parity cells, fortress cells)
Quadruples     → List<QuadrupleClue>      (corner coord + required digit set)
Comparisons    → List<ComparisonClue>     (cell pair + direction for greater/less)
OutsideClues   → List<OutsideClue>        (edge coord + value + clue type: sandwich/xsums/little killer)
CountingCircles→ List<CellCoord>          (all circled cells)
ExtraRegions   → List<List<CellCoord>>    (X diagonals, Hyper boxes, disjoint groups, clone pairs)
FogCells       → HashSet<long>            (packed row/col coordinates)
```

### Supporting Types

| Type | Fields | Used By |
|------|--------|---------|
| `CellCoord` | Row, Col | All modifiers |
| `ModifierLine` | Type (LineType enum), Cells, Metadata (e.g. N for fast thermo, sum for zipper) | All line constraints |
| `ArrowConstraint` | Circles (list of CellCoord for pill support), Path (list), Variant (ArrowVariant enum) | All arrow variants |
| `KillerCage` | Sum (int), Cells (list) | Killer Cages |
| `KropkiDot` | CellA, CellB, Type (DotType: White/Black/X/V) | Kropki, XV Pairs |
| `CellMarker` | Cell, Type (MarkerType: Even/Odd/Fortress) | Parity cells, Fortress |
| `QuadrupleClue` | CornerRow, CornerCol, RequiredDigits (list) | Quadruples |
| `ComparisonClue` | CellA, CellB, AIsGreater (bool) | Greater/Less Than |
| `OutsideClue` | Edge, Index, Value, Type (sandwich/x-sums/little-killer), Direction | Outside-grid clues |

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
