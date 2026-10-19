# Boss Mechanics System

**Version:** 2.12 | **Date:** 2026-10-19 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.12 | 2026-10-19 | implemented | Owner request: catalog batch B5 goes live: X-Sudoku (30), Hyper Sudoku (31), Disjoint Groups (32), Extra Region (53) and Magic Square (68). X-Sudoku, Hyper, Disjoint Groups and the magic square shape the solution (the generator fills the grid with them; givens stay chosen under the base rules, owner decision) and count as board-shaping rules (rectangular regions, 7×7 raised to 8×8). Hyper, Disjoint Groups and the magic square need the 3×3 boxes of a 9×9; a magic 3×3 square needs the digits 1–9, so Magic Square is 9×9 only (the library said 7×7). Blocked pairs from the pair benchmark (`SolutionShapePairBenchmark`): X-Sudoku with Nonconsecutive, Anti-Knight, Nonconsec. Diagonal or Entropy; Hyper with Nonconsecutive, Nonconsec. Diagonal or Entropy; Disjoint Groups + Entropy (no such grid); Magic Square with Nonconsecutive or Nonconsec. Diagonal (its digits break them). A pre-baked boss board is reused only when every solution-shaping rule matches, Entropy included (GAP-051). With this batch every modifier of the catalog plan is built: 71 player modifiers, 70 clearable. |
| 2.11 | 2026-10-19 | implemented | Owner request: catalog batch B4 goes live: Quadruples (79, 9×9 as the library says), Skyscrapers (81), Sandwich (82), X-Sums (83) and Little Killer (84). Owner decision (2026-10-19): a board with outside clues is drawn about 12 % smaller and the clues stand in a ring round it (UI-STYLE-010); boards without them keep their size. One kind of outside clue per board (the four are blocked pairs among each other), so every number in the ring means the same thing. 66 player modifiers, 65 clearable. |
| 2.10 | 2026-10-19 | implemented | Owner request: catalog batch B6 goes live: K-Whispers (37), 10-Line (46), Sum Line (48), Ratio 1:3 (61), Distance Pairs (63), Ratio Cage (67), Pill Arrow (72), Opposite Sums (77), Counting Circles (78), Distance Cells (80) and the Inverted Zone (93). Owner decisions (2026-10-19): the Constraint Negation Zone becomes an inverted whisper zone (green whisper lines that flip to "less than 5" for a step between two cells of a shaded 3×3 area; 9×9 only); Index Parity (33) is dropped. Catalog lines never cross themselves (GAP-050). New blocked pairs: Inverted Zone + German Whispers, Counting Circles + Prime Cells, Counting Circles + Multiples of 3. 61 player modifiers, 60 clearable. |
| 2.9 | 2026-10-19 | implemented | Owner request: catalog batch B3 goes live: Region Sum Line (45), Hidden Killer (64), Product Cage (65), Difference Cage (66), Renban Cage (69), Arrow Average (70) and Double Arrow (73), with the min sizes of the library below. Each cage kind has its own outline colour and corner label (sum, "?", "×N", "−N", "R"); arrows keep the grey ring ("Ø" marks an average arrow, a double arrow has a ring at both ends); each kind is its own legend layer. A hidden sum is never checked, drawn or listed (the Pattern Scroll names cages by their label). New blocked pair: Hidden Killer + Killer Cages. 50 player modifiers, 49 clearable. |
| 2.8 | 2026-10-19 | implemented | Owner decision (2026-10-19): catalog batches B1 and B2 go live: 13 modifiers from the Extended Library (IDs 38, 40–44, 51, 52, 58–60, 74, 76) with rules, geometry, overlays, legend layers and texts in five languages; they roll at equal weight with the others and count toward the codex and completion (43 player modifiers, 42 clearable). The roll pool grew, so a seed from 0.1.5 (also a shared seed or an old Seed of the week) gives a different run in 0.2.0; saved runs keep their stored modifiers. Definitions as in the library below; Difference ≥ K uses K = 3, Fast Thermo N = 2. New blocked pairs keep one thermometer kind per puzzle and never stack a complete dot set with a dot rule of the same colour. The Custom Puzzle panel still lists the first 30 (a new layout waits for a mockup). |
| 2.7 | 2026-10-19 | implemented | A chosen modifier counts as generated only with its own geometry (per line, dot, pair, cage, arrow, marker and cell-pair type; BOSS-MOD-004, GAP-047). New blocked pair Nonconsecutive + Difference Kropki (no white dot can exist; GAP-049). |
| 2.6 | 2026-10-19 | implemented | Owner decision (GAP-048): line colours follow the convention puzzle setters use (Logic Masters, GM Puzzles): German Whispers green, Dutch Whispers orange, Renban pink, Palindrome grey, thermometers grey with a bulb; Consecutive Line moves to gold so it does not clash with Dutch Whispers. Lines are drawn as opaque bands a fifth of a cell wide through the cell centres with round joints (as in the chosen puzzle mockup, UI-STYLE-002); the colours below are the band colours on the paper board. Rule texts name the new colours in all five languages, and the Custom Puzzle tooltips use the same texts. |
| 2.5 | 2026-10-19 | implemented | Line colours on the paper board of the hanging-scroll puzzle screen (UI-STYLE-002): German and Dutch Whispers darker so they read on paper; Parity Lines drawn blue as specified (the game drew them teal, almost the colour of Dutch Whispers, and the rule text said "purple"; GAP-048). Renban and Palindrome still differ from this table (GAP-048, open). |
| 2.4 | 2026-10-15 | implemented | Killer cage visual: one dashed outline around all cells of a cage, joined across its cells (owner report 2026-10-15, layout-proposals.md UI-STYLE-006). |
| 2.3 | 2026-10-07 | implemented | 1.1 review decisions (2026-10-07): floor modifiers 0/1/1/1/2 kept from code (SPEC-CONF-001); boss options floor+1 shown / floor picked, no minimum (SPEC-CONF-002); 8-directional lines (SPEC-CONF-011); Entropy 9×9 (SPEC-CONF-012); eligibility table from BossService.ModifierData with blocked pairs, Modular Regions exclusion and structural-modifier region rule (SPEC-CONF-013, SPEC-AMB-014); Floor Progression Arc removed (SPEC-CONF-014); 30 player modifiers instead of 15 (SPEC-CONF-019); boss boards follow the floor range (SPEC-CONF-028); Anti-Bishop redefined to diagonal range 2, min 6×6 (SPEC-CONF-038); solution-based validation (SPEC-CONF-003); Endless Zen pool (SPEC-AMB-009); intensity label + boss debuff on the gate and modifier legend as built (GAP-025, GAP-026); file references updated (SPEC-CONF-036); SPICE table archived (SPEC-OBS-001). |
| 2.2 | 2026-04-27 | implemented | Grid-size compatibility analysis. New `BuildEligiblePool` restrictions: **Dutch Whispers, Anti-Bishop, Distance ≥ 2, Full Kropki, Fortress Cells** now require `minBoardSize ≥ 6` (5×5 classified INVALID — sparse/infeasible geometry). Ratio Kropki and XV Pairs retain 5×5 eligibility (LIMITED — sparse but functional, no code restriction). |
| 2.1 | 2026-04-15 | implemented | Added requirement IDs (BOSS-MOD-*, BOSS-INT-*, BOSS-FLOOR-*) inline |
| 2.0 | 2026-04-08 | implemented | Boss Debuff system added (see BossDebuffEffects.md). 5 debuffs implemented: RowWipe(94), ColWipe(95), DoublePenalty(96), CellLock(97), PencilBlind(98). These are *reactive penalty hooks* distinct from constraint modifiers — they fire on wrong placement. RunDirector gains ApplyMistakePenalty(row,col) overload, TickLockedCells(), GetLockedCells(). LevelState.LockedCells tracks per-cell lock counters. BoardViewController renders locked cells with deep magenta tint (GamePalette.CellLocked). BossService excludes debuff IDs (≥94) from normal modifier rolling pool. |
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

Modifiers add visual overlays to the puzzle grid and enforce additional constraints beyond standard Sudoku rules. There are **71 player modifiers** (`BossModifierId` 0–29 and the catalog batches 30–32, 37, 38, 40–46, 48, 51–53, 58–61, 63–70, 72–74, 76–84, 93); 70 of them can be generated (Modular Regions is excluded, see §Modifier Eligibility). Every generatable modifier is eligible from any floor — there is no tier gating, only the board-size and pairing rules below.

Modifiers appear in two ways:

1. **Floor Modifiers** — Starting from Floor 2, a set of modifiers is rolled automatically when the player enters a new floor. These apply to **every puzzle on that floor** (normal, elite, and boss). The counts per floor are 0, 1, 1, 1, 2 (§Floor Modifier Scaling).

2. **Boss Modifiers** — Before each boss puzzle, the player chooses additional modifiers from a choice panel (Floor N: pick N from N+1, so 1 of 2 on Floor 1). These are **stacked on top of** the floor modifiers for the boss puzzle only.

Modifiers the player has never encountered before are displayed as **"???"** until played.

---

## Active Modifier Pool (30 Implemented) `[BOSS-MOD-001]` `[BOSS-MOD-002]`

All 71 modifiers are fully implemented with constraint rules, geometry generation, overlay rendering and descriptions; the Custom Puzzle panel lists the first 30.

### Line Constraints

**General line rules** (apply to all 7 line modifiers):
- Lines connect cells that are adjacent in any of the **8 directions** (orthogonal or diagonal); every line generator steps through `ModifierGeometryGenerator.Dirs`, which includes the diagonals (SPEC-CONF-011, 2026-10-07; supersedes the v1.9 note).
- A line **must not cross itself** — no cell may appear more than once on the same line, and line segments must not intersect.

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 1 | **German Whispers** | Green lines (0.42, 0.72, 0.38) | Adjacent cells on each line must differ by ≥5. **Restricted to boards ≥ 7×7.** |
| 2 | **Dutch Whispers** | Orange lines (0.96, 0.55, 0.25) | Adjacent cells on each line must differ by ≥4. **Restricted to boards ≥ 6×6.** |
| 3 | **Parity Lines** | Blue lines (0.45, 0.58, 0.96) | Adjacent cells on each line must alternate odd/even parity |
| 4 | **Renban Lines** | Pink lines (0.93, 0.50, 0.76) | Cells on each line form a consecutive set in any order (no repeats) |
| 5 | **Palindrome** | Grey lines (0.56, 0.56, 0.58) | Digits along the line read the same forward and backward |
| 6 | **Thermo** | Light grey line with a filled bulb (0.72, 0.72, 0.72) | Digits strictly increase from bulb to tip (each cell > previous) |
| 7 | **Between Lines** | Taupe line (0.76, 0.60, 0.42) with a hollow circle at each end | Every digit on the line must be strictly between the digits in the two endpoint circles |

### Dot Constraints

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 8 | **Difference Kropki** | White dots (hollow circle) between cells | Connected cells must contain consecutive digits (differ by 1) |
| 9 | **Ratio Kropki** | Black dots (filled circle) between cells | Connected cells must be in a 1:2 ratio |

### Arithmetic Constraints

| # | Modifier | Visual | Rule |
|:---:|----------|--------|------|
| 10 | **Killer Cages** | One dashed outline around the whole cage (joined across its cells, UI-STYLE-006) + sum label (`CageSumBg` + `CageSumText`) | Digits in each cage must sum to the displayed value; no repeats within a cage. **Restricted to boards ≥ 7×7.** |
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
| 16 | **Anti-Bishop** | No geometry (global rule) | A digit may not repeat within **two diagonal steps** (bishop range 2; redefined 2026-10-07, SPEC-CONF-038 — the any-length rule has no solution on 6×6, 8×8 or 9×9) | 6×6 |
| 17 | **Nonconsec. Diagonal** | No geometry (global rule) | Diagonally adjacent cells cannot contain consecutive digits | 5×5 |
| 18 | **Distance ≥ 2** | No geometry (global rule) | Equal digits must be at Chebyshev distance ≥ 2 — no king-adjacent equal digits (identical to Anti-King; implemented as separate modifier for clarity) | 6×6 |
| 19 | **Entropy** | No geometry (global rule) | Every 3 consecutive cells in any row or column must contain one digit from {1–3}, one from {4–6}, one from {7–9} | 9×9 (the three digit groups need nine digits, SPEC-CONF-012) |
| 20 | **Modular Regions** | No geometry (global rule) | Every region must contain at least one digit from each set {1–3}, {4–6}, {7–9} | 7×7 — excluded from every roll pool (§Modifier Eligibility) |

### Line Constraints (Extended)

| ID | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 21 | **Consecutive Line** | Gold line (0.86, 0.70, 0.10) | Adjacent cells on line must differ by exactly 1. Example: 3-4-5 or 7-6-5. | 5×5 |
| 22 | **Slow Thermo** | Violet line with an open bulb (0.70, 0.50, 0.92) | Non-strict increase from bulb — each cell must be ≥ the previous. Example: 2-2-3-5. | 5×5 |
| 23 | **Unique Set Line** | Sky-blue line with ring endpoints (0.34, 0.78, 0.90) | No digit may repeat anywhere on the line. | 5×5 |

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

### Catalog batches (B1 to B6, active since 2026-10-19) `[GEN-MOD-002]`

Forty-one modifiers from the Extended Library below, registered with their enum IDs (docs/design/modifier-catalog-plan.md). Geometry is read off the solution; lines run in 8 directions.

| ID | Modifier | Visual | Rule | Min Size | Rule Class |
|:---:|----------|--------|------|:---:|-----------|
| 38 | **Nonconsecutive Line** | Lime line with an ink cross on every step | Neighbours on the line are never consecutive | 6×6 | `NonconsecLineRule` |
| 40 | **High-Low Line** | Mint line with a paper stripe | Digits alternate between the low half (≤ midpoint, 1–5 on 9×9) and the high half | 6×6 | `HighLowLineRule` |
| 41 | **Nabner Line** | Red line | No two digits on the line repeat or are consecutive | 6×6 | `NabnerLineRule` |
| 42 | **Entropic Line** | Peach line | Any three neighbours hold one of {1–3}, {4–6}, {7–9} | 7×7 | `TriadLineRule` |
| 43 | **Modular Line** | Teal line | Any three neighbours hold one digit of each remainder mod 3 | 6×6 | `TriadLineRule` |
| 44 | **Zipper Line** | Lavender line, ink ring on the middle cell (or ink dot between the two middle cells) | Cells the same distance from the middle sum to the same total; a middle cell holds it | 6×6 | `ZipperLineRule` |
| 51 | **Fast Thermo** | Grey thermometer with a double bulb | Each digit is at least 2 above the one before (N = 2) | 6×6 | `FastThermoRule` |
| 52 | **Ambiguous Thermo** | Grey line, ink bar at both ends, no bulb | Strictly increasing in one of the two directions | 6×6 | `AmbiguousThermoRule` |
| 58 | **Full White Kropki** | White dot on every difference-1 pair | All difference-1 pairs are marked; no dot forbids a difference of 1 | 5×5 | `FullKropkiSetRule` + `DifferenceKropkiRule` |
| 59 | **Full Black Kropki** | Black dot on every 1:2 pair | All 1:2 pairs are marked; no dot forbids a 1:2 ratio | 6×6 | `FullKropkiSetRule` + `RatioKropkiRule` |
| 60 | **Difference ≥ 3** | Plum tag "≥3" on the shared edge | The two cells differ by at least K = 3 | 5×5 | `DifferenceKPairRule` |
| 74 | **Multiples of 3** | Ochre ring with "×3" | A marked cell holds 3, 6 or 9 | 5×5 | `MultipleCellsRule` |
| 76 | **Mirror Cells** | Indigo letter badge, same letter on both cells | A cell and its reflection through the grid centre hold the same digit | 5×5 | `MirrorCellsRule` |
| 45 | **Region Sum Line** | Navy line, ink tick where it crosses a region border | Every piece between region borders adds up to the same total | 7×7 | `RegionSumLineRule` |
| 64 | **Hidden Killer** | Red dashed cage, "?" in the corner | No digit repeats in the cage; the sum stays hidden | 7×7 | `TypedCageRule(HiddenSum)` |
| 65 | **Product Cage** | Teal dashed cage, "×N" | The digits multiply to N, no repeats | 6×6 | `TypedCageRule(Product)` |
| 66 | **Difference Cage** | Blue dashed two-cell cage, "−N" | The two digits differ by N | 5×5 | `TypedCageRule(Difference)` |
| 69 | **Renban Cage** | Pink dashed cage, "R" | The digits form a run of consecutive numbers | 5×5 | `TypedCageRule(RenbanSet)` |
| 70 | **Arrow Average** | Grey ring marked "Ø" with its path | The circle holds the average of the path | 7×7 | `TypedArrowRule(Average)` |
| 73 | **Double Arrow** | Grey ring at both ends of the path | The path adds up to both circle digits | 6×6 | `TypedArrowRule(DoubleEndpoint)` |
| 37 | **K-Whispers** | Magenta line with a "≥K" tag; one K per puzzle (2 or 3 up to 7×7, 3 or 6 above) | Neighbours differ by at least K | 6×6 | `WhisperKLineRule` |
| 46 | **10-Line** | Sienna line, ink ticks at region borders, "10" tag | Every piece between region borders adds up to 10 | 7×7 | `RegionSumLineRule(NLine)` |
| 48 | **Sum Line** | Olive line with the total on a tag at its end | The digits on the line add up to the total | 6×6 | `SumLineRule` |
| 61 | **Ratio 1:3** | Black dot with a white "3" on the shared edge | One digit is three times the other | 6×6 | `RatioNPairRule` |
| 63 | **Distance Pairs** | Brown link between two cells a diagonal step or a knight move apart, the steps on a badge | The two digits differ by the steps between the cells | 6×6 | `CellPairValueRule(Distance)` |
| 67 | **Ratio Cage** | Ink dashed two-cell cage, "1:N" | The larger digit is N times the smaller | 5×5 | `TypedCageRule(Ratio)` |
| 72 | **Pill Arrow** | Grey two-cell pill with its arrow path | The pill reads as a two-digit number (top or left first); the path adds up to it | 8×8 | `TypedArrowRule(Pill)` |
| 77 | **Opposite Sums** | Crimson badge with the total on a cell and on its mirror cell | A cell and its mirror through the centre add up to the badge | 5×5 | `CellPairValueRule(OppositeSum)` |
| 78 | **Counting Circles** | Orange ring round the cell | A circled digit d appears in exactly d circles | 6×6 | `CountingCirclesRule` |
| 80 | **Distance Cells** | Navy double ring on the target, small navy dot on each counting cell | A dotted cell holds its orthogonal step distance to the ring | 7×7 | `DistanceCellRule` |
| 79 | **Quadruples** | White circle with an ink rim on an inner corner, two to four digits | Every listed digit appears in one of the four cells round the corner | 9×9 | `QuadrupleRule` |
| 81 | **Skyscrapers** | Ink numbers in the ring beside rows and columns | The number counts the digits seen from that side; higher ones hide lower ones | 5×5 | `OutsideClueRule(Skyscrapers)` |
| 82 | **Sandwich** | Ink numbers in the ring | The sum of the digits between the 1 and the highest digit of the row or column | 6×6 | `OutsideClueRule(Sandwich)` |
| 83 | **X-Sums** | Ink numbers in the ring | The first digit from that side says how many digits to add; the number is their sum | 5×5 | `OutsideClueRule(XSums)` |
| 84 | **Little Killer** | Ink number with an arrow outside the corner of a diagonal (one clue per outside spot) | The digits along the diagonal (3 cells or more) add up to the number (repeats allowed) | 6×6 | `OutsideClueRule(LittleKiller)` |
| 30 | **X-Sudoku** | Two thin indigo diagonals | No digit repeats on either long diagonal | 5×5 | `ExtraHouseRule(XSudoku)` |
| 31 | **Hyper Sudoku** | Four teal-washed windows with dashed outlines | No digit repeats in each window | 9×9 | `ExtraHouseRule(HyperSudoku)` |
| 32 | **Disjoint Groups** | A slate pip in every cell at its place in the box | Cells in the same place of their box hold different digits | 9×9 | `ExtraHouseRule(DisjointGroups)` |
| 53 | **Extra Region** | Violet wash with a dashed outline | No digit repeats in the region (board-size cells, read off the solution) | 5×5 | `ExtraHouseRule(ExtraRegion)` |
| 68 | **Magic Square** | Gold-washed box with "15" in its corner | Every row, column and diagonal of the box adds up to 15 | 9×9 | `MagicSquareRule` |
| 93 | **Inverted Zone** | Green whisper lines and a shaded 3×3 area with a dashed green outline | Neighbours on the line differ by 5 or more, but by less than 5 for a step between two cells inside the area | 9×9 | `ZoneWhisperRule` |

---

## Floor Modifier System `[BOSS-FLOOR-001]` `[BOSS-FLOOR-003]`

Starting from Floor 2, modifiers are applied to **all puzzles** on the floor — normal, elite, and boss. These are rolled automatically when the player enters the floor and persist until the floor is cleared.

### Floor Modifier Scaling

| Floor | Floor Modifiers | Effect |
|:---:|:---:|--------|
| 1 | 0 | No floor modifiers — gentle introduction |
| 2 | 1 | One modifier active on all puzzles |
| 3 | 1 | One modifier; difficulty rises through stars and board size |
| 4 | 1 | One modifier; boss picks keep rising |
| 5 | 2 | Two modifiers on all puzzles — maximum floor pressure |

Base counts `0, 1, 1, 1, 2` (`BossService.BaseFloorModifierCounts`), decided 2026-10-07 (SPEC-CONF-001): the code values were kept because the generation load is tested at that level; difficulty still rises through stars, board size and boss picks. Harmony adds floor modifiers on top (`HarmonyDifficultyService.GetBonusFloorModifiers`, HarmonyDifficultySystem §4.4).

### Floor Modifier Rolling

1. On **floor entry**, `BossService.RollFloorModifiers(floor, minBoardSize, harmonyLevel)` draws the floor's count of distinct modifiers from the generatable pool (§Modifier Eligibility).
2. Floor modifiers are stored in `RunState.ActiveFloorModifiers` and persisted in save data.
3. Board-size and pairing restrictions apply (§Modifier Eligibility): a modifier whose minimum size exceeds the floor's smallest board is excluded from the roll pool.
4. Floor modifiers from previous floors do **not** exclude modifiers from the current floor's pool — each floor is independent.
5. Floor modifiers are added to `RunState.SeenBossModifiers` for "???" reveal tracking.

### Floor Modifier Display

- A **floor modifier banner** is shown when entering a new floor, listing the active modifiers for the floor.
- The modifier description box (under the numpad "Mode" button) shows floor modifiers on all puzzles (not just boss puzzles).
- Floor modifiers use the same overlay rendering and constraint rules as boss modifiers.

---

## Boss Gate Choice Flow `[BOSS-FLOOR-002]`

### Choice Scaling per Floor

In addition to floor modifiers, the boss puzzle adds **boss-chosen modifiers** via a choice panel. The number of options and selections scales with floor:

| Floor | Boss Options Shown | Player Picks | Total Active on Boss (Floor + Boss) |
|:---:|:---:|:---:|:---:|
| 1 | 2 | 1 | 0 + 1 = **1** |
| 2 | 3 | 2 | 1 + 2 = **3** |
| 3 | 4 | 3 | 1 + 3 = **4** |
| 4 | 5 | 4 | 1 + 4 = **5** |
| 5 | 6 | 5 | 2 + 5 = **7** |

Boss choice formula: Floor N (1-based) offers **N+1** options and the player picks **N**, so exactly one option is always left out (`BossService.GetBossModifierCounts`). There is no minimum; the Floor 1 boss stays gentle (GDS §15). Harmony adds the same bonus to options and picks (HarmonyDifficultySystem §4.4). From Floor 3 the boss also carries one debuff (BossDebuffEffects §Delivery). Decided 2026-10-07 (SPEC-CONF-002).

### Flow Steps

1. Player reaches the **Boss Gate** node on the garden path.
2. `BossService.RollBossChoices(floor, runState)` selects N+1 distinct modifiers from the pool, **excluding modifiers already active as floor modifiers** (no duplicates).
3. A modal panel displays all options. Unseen modifiers show **"???"** instead of their name and description.
4. Player selects N modifiers → stored in `RunState.ChosenBossModifiers` and applied to the boss puzzle config.
5. Selected modifiers are added to `RunState.SeenBossModifiers` (persists across the run for "???" reveal tracking).
6. The choice is **locked on confirm** — no undo after the panel closes.
7. The boss puzzle launches with **floor modifiers + boss-chosen modifiers** active simultaneously.

### Modifier Eligibility

Authoritative table: `BossService.ModifierData` (decided 2026-10-07, SPEC-CONF-013). The larger minimums exist for generation reliability (GAP-003 benchmarks).

| Min board | Modifiers |
|:---:|-----------|
| any (5×5) | Difference Kropki, Ratio Kropki, Even/Odd, Fog of War, Sum Kropki, XV Pairs, Prime Cells |
| 5×5 | Parity Lines, Renban Lines, Unique Set Line, Greater/Less Than |
| 6×6 | Nonconsecutive, Dutch Whispers, Palindrome, Antiknight, Arrow Sums, Thermo, Between Lines, Anti-King, Anti-Bishop (range 2), Nonconsec. Diagonal, Distance ≥ 2, Consecutive Line, Slow Thermo, Full Kropki, Fortress Cells |
| 7×7 | Killer Cages, German Whispers (Modular Regions: 7×7, but excluded) |
| 9×9 | Entropy |

- **Modular Regions** is excluded from every roll pool and from completion (70 clearable modifiers).
- **Blocked pairs** (never combined, `BossService.GetBlockedPairs`): Ratio Kropki + Distance ≥ 2, Distance ≥ 2 + Nonconsec. Diagonal, Anti-Bishop + Antiknight, Anti-Bishop + Nonconsecutive, Anti-Bishop + Entropy, Nonconsecutive + Difference Kropki; Fast Thermo with Thermo, Slow Thermo or Ambiguous Thermo; Ambiguous Thermo with Thermo, Slow Thermo or Palindrome; Full White Kropki with Full Kropki, Full Black Kropki, Difference Kropki, Ratio Kropki, Sum Dot or Nonconsecutive; Full Black Kropki with Full Kropki, Ratio Kropki, Difference Kropki or Sum Dot (never two dots on one edge); Hidden Killer + Killer Cages; Inverted Zone + German Whispers (both draw green whisper lines); Counting Circles + Prime Cells or Multiples of 3 (circled cells look alike); any two of Skyscrapers, Sandwich, X-Sums and Little Killer (one kind of outside clue per board); X-Sudoku with Nonconsecutive, Anti-Knight, Nonconsec. Diagonal or Entropy; Hyper Sudoku with Nonconsecutive, Nonconsec. Diagonal or Entropy; Disjoint Groups + Entropy; Magic Square with Nonconsecutive or Nonconsec. Diagonal (B5 pair benchmark).
- **Global structural modifiers** (Nonconsecutive, Antiknight, Anti-King, Anti-Bishop, Nonconsec. Diagonal, Distance ≥ 2, Entropy, and since 2.12 X-Sudoku, Hyper Sudoku, Disjoint Groups and Magic Square) use the rectangular region layout of the board size: no irregular template produced a puzzle with them within budget (SPEC-AMB-014). Two or more of the board-shaping ones need at least 8×8, and a 7×7 board with one is raised to 8×8.
- Modifiers already active as **floor modifiers** are excluded from the boss choice pool (no duplicates within a floor).
- Modifiers chosen on a previous floor **can** appear again on later floors (each floor is independent).
- **Boss board size** follows the floor's size range like any other puzzle (decided 2026-10-07, SPEC-CONF-028); the old "at least 8×8" boss clamp was removed. The only raises are the structural-modifier rules above. The map preview shows the size the boss is generated with.

---

## Boss Modifier Intensity `[BOSS-INT-001]` `[BOSS-INT-002]`

Modifier geometry density scales with run progression via the `BossModifierIntensity` enum:

| Run Number | Intensity | Multiplier | Effect on Geometry |
|:---:|----------|:---:|------------|
| 1–2 | Low | ×0.5 | Fewer lines/dots/cages — gentler constraints |
| 3–5 | Medium | ×1.0 | Standard density |
| 6–8 | High | ×1.5 | More geometry elements — tighter constraints |
| 9+ | VeryHigh | ×2.0 | Maximum density — heavily constrained puzzles |

Intensity is set in `RunDirector.BuildLevelConfig` and passed to `ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)`.

A line under the boss gate title shows the localized intensity ("Intensity: High") and, from Floor 3, the boss's debuff (GAP-025, GAP-026). Evidence: `InRunUiPlayModeTests.BossGate_ShowsIntensity_AndTheDebuffFromFloorThree`.

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

- `RunDirector.PlaceNumber()` checks `Solution[row,col] == value`. A different digit is **wrong**: it is rejected (not written to the board) and costs the mistake HP; debuffs and pressure hooks fire (SPEC-CONF-003, decided 2026-10-07). Only Custom Puzzle 7★ boards, which have no unique solution, stay rule-based (a digit is wrong when it breaks a rule).
- `SudokuValidator.IsMoveValid()` accepts an optional `SudokuConstraintEngine` for modifier-aware pencil mark validation.
- `SudokuConstraintEngine.ValidateAll()` iterates all registered rules in category/order sequence.

---

## Geometry Generation `[BOSS-MOD-003]`

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
| German Whispers lines | Green (0.42, 0.72, 0.38) | Opaque band, 0.2 × cell wide: rotated Image rectangles between cell centres + a round joint (dot of the band width) on every cell |
| Dutch Whispers lines | Orange (0.96, 0.55, 0.25) | Same as above |
| Parity lines | Blue (0.45, 0.58, 0.96) | Same as above |
| Renban lines | Pink (0.93, 0.50, 0.76) | Same as above |
| Palindrome lines | Grey (0.56, 0.56, 0.58) | Same as above |
| Thermo lines | Light grey (0.72, 0.72, 0.72) | Band + filled bulb (0.34 × cell radius) on the first cell |
| Between Lines | Taupe (0.76, 0.60, 0.42) | Band + hollow circle (paper inside) at both ends |
| Kropki white dots | White hollow (outer ring + transparent inner) | Circle at midpoint between cells |
| Kropki black dots | Black filled (0.10, 0.10, 0.10, 0.90) | Circle at midpoint between cells |
| Arrow circles | Grey hollow (outer ring + transparent inner) | Large circle on circle cell |
| Arrow paths | Grey (0.55, 0.55, 0.55, 0.45) | Lines between consecutive path cells + endpoint dot |
| Killer cage borders | Red dashed (`GamePalette.KillerBorder`) | One outline per cage, inset into its cells and joined across them (`CageOutline`) |
| Killer cage sums | `CageSumBg` (Image) + `CageSumText` (Text child) | Top-left corner of top-left cage cell |
| Even markers | Blue squares (0.35, 0.65, 0.90, 0.55) | Square Image on cell |
| Odd markers | Orange circles (0.90, 0.55, 0.20, 0.55) | Circle Image on cell |
| Fog cells | Near-black (0.06, 0.06, 0.08, 1.00) | Cell background color override; hides digit and pencil text |
| Consecutive Line | Gold (0.86, 0.70, 0.10) | Same line rendering as other line types |
| Slow Thermo | Violet (0.70, 0.50, 0.92) | Band + open bulb (hollow circle) on the first cell |
| Unique Set Line | Sky blue (0.34, 0.78, 0.90) | Band + ring at both endpoints |
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

Endless Zen draws from the generatable player modifiers for its board size (`BossService.GetGeneratablePlayerModifiers`), not from the full enum (SPEC-AMB-009, GAP-007).

- Depth 0-9: 1 random modifier per level.
- Depth 10-19: 2 random modifiers per level.
- Depth 20+: 3 random modifiers per level.
- Region generation is always 9x9, so the current Endless Zen builder does not apply additional board-size filtering beyond the fixed board size.

---

## Floor Progression Arc

Removed 2026-10-07 (SPEC-CONF-014): the table repeated data that lives in §Floor Modifier Scaling, §Choice Scaling per Floor and §Boss Modifier Intensity (intensity follows the run number, not the floor). Floor names and grid ranges are in GameDesignSpec §10.

---

## Boss Modifier Serialization

Boss modifier state is serialized for save/resume support:

- `RunState.HasChosenBossModifier` (bool) + `RunState.ChosenBossModifierId` (BossModifierId) — replaces nullable `BossModifierId?` for JSON compatibility
- `RunState.SeenBossModifierList` (List<BossModifierId>) — serializable version of `SeenBossModifiers` HashSet
- `RunAutoSaveCoordinator.Save` calls `SyncSeenModifiersToList()` before building the save envelope
- `RunResumeService.TryResumeFromSave` restores boss modifier fields and reconstructs the HashSet

---

## Modifier Descriptions

All 71 player modifiers and the 14 boss debuffs have descriptions via `BossService.GetModifierDescription()` (localized, `Boss.Description.*`). These are shown:
- In the boss gate choice panel
- In the tutorial modifier toggle tooltips
- In the in-run HUD modifier box, the modifier legend and the in-run View Modifiers list

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Sudoku/ModifierModels.cs` | Data structures (CellCoord, ModifierLine, ArrowConstraint, KillerCage, KropkiDot, CellMarker, ModifierOverlayData) |
| `Assets/Scripts/Sudoku/ConstraintRules.cs` (+ `.LineVariants.cs`, `.DotsPairsCells.cs`, `.Cages.cs`, `.ArrowsSums.cs`, `.CatalogB6.cs`, `.OutsideClues.cs`, `.ExtraHouses.cs`) | IOrderedConstraintRule implementations for all 71 player modifiers |
| `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` (+ `.Catalog.cs`, `.CagesArrows.cs`, `.CatalogB6.cs`, `.OutsideClues.cs`, `.ExtraHouses.cs`) | Geometry generation from solved boards (accepts intensity parameter); 71 modifiers |
| `Assets/Scripts/Sudoku/ModifierFactory.cs` | Maps BossModifierId → constraint rule instances (71 cases) |
| `Assets/Scripts/Sudoku/SudokuConstraintEngine.cs` | Central rule engine with ValidateAll |
| `Assets/Scripts/Boss/BossService.cs` | Modifier pool construction, choice rolling, ModifierData (min sizes, blocked pairs), debuff pool |
| `Assets/Scripts/Run/RunDirector.cs` | StartLevel wiring (overlay generation, engine creation, fog reveal, intensity assignment) |
| `Assets/Scripts/UI/InRunController.cs`, `BoardViewController.cs` (+ `.OverlayGroups.cs`), `HudViewController.cs` (+ `.Legend.cs`), `BossGateViewController.cs` | Overlay rendering per modifier layer, fog masking, input blocking, descriptions, legend, boss gate (`PrototypeRunScreenController` no longer exists, SPEC-CONF-036) |
| `Assets/Scripts/Core/GameEnums.cs` | BossModifierId (71 player modifiers: 0–29 and 30–32, 37, 38, 40–46, 48, 51–53, 58–61, 63–70, 72–74, 76–84, 93; other library IDs 30–93 not generated; debuffs 94–102/107–111, pressure 103–106), BossModifierIntensity, ConstraintRuleCategory enums |
| `Assets/Scripts/Core/RuntimeModels.cs` | Boss modifier serialization fields in RunState |
| `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | SyncSeenModifiersToList before save |
| `Assets/Scripts/Save/RunResumeService.cs` | Restore boss modifier state on resume |
| `Assets/Scripts/Tutorial/TutorialModeService.cs` | Modifier descriptions and board-size validation for tutorial |

---

## Extended Modifier Library (Future Content)

The following modifiers are cataloged for potential future integration. Entries marked ✅ are in the active pool (71 modifiers since 2.12; the ID in brackets is the enum ID). The table format matches the active pool: `# | Modifier | Visual | Rule | Min Size`.

**Min Size** is the smallest board on which the constraint remains interesting and non-trivial. **Visual** is a proposed rendering approach; all visuals are TBD until implementation.

---

### Global / Negative Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 16 | **X Sudoku** ✅ (ID 30) | No geometry (extra region highlight on diagonals) | Both main diagonals are extra regions — no digit may repeat on either diagonal | 5×5 |
| 17 | **Hyper Sudoku** ✅ (ID 31) | Dashed borders on 4 extra 3×3 regions | Four additional 3×3 regions overlapping the standard grid; no digit repeats within them | 9×9 |
| 18 | **Disjoint Groups** ✅ (ID 32) | A pip per cell at its relative box position | Cells in the same relative position across all boxes cannot share a digit | 9×9 |
| 19 | **Antiking** ✅ | No geometry (global rule) | No two cells a chess king's move apart may contain the same digit | 5×5 |
| 20 | **Anti-Bishop** ✅ | No geometry (global rule) | No two cells on the same diagonal (any length) may contain the same digit | 6×6 |
| 21 | **Nonconsecutive (Diagonal)** ✅ | No geometry (global rule) | Diagonally adjacent cells cannot contain consecutive digits | 5×5 |
| 22 | **Index Parity** 🗄️ dropped (owner decision 2026-10-19: forces a checkerboard, impossible on odd sizes) | No geometry (global rule) | All cells where (row + col) is even must share the same parity; same for (row + col) odd | 5×5 |
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
| 27 | **Whisper (Generalized)** ✅ (ID 37, K-Whispers) | Coloured line (hue configurable) | Adjacent cells on the line differ by ≥ K, where K is set at run time | 6×6 |
| 28 | **Consecutive Line** ✅ | Orange line | Every pair of adjacent cells on the line differs by exactly 1 | 5×5 |
| 29 | **Nonconsecutive Line** ✅ (ID 38) | Crossed lime line | No pair of adjacent cells on the line are consecutive | 6×6 |
| 30 | **Alternating Parity Line** | Purple striped line | Digits strictly alternate odd/even along the line | 5×5 |
| 31 | **High-Low Alternating** ✅ (ID 40) | Mint line with a paper stripe | Digits alternate above/below the board midpoint (e.g. >5 and ≤5 on a 9×9) | 6×6 |
| 32 | **Nabner Line** ✅ (ID 41) | Red line | No digit on the line repeats or is consecutive with any other digit on the same line | 6×6 |
| 33 | **Entropic Line** ✅ (ID 42) | Peach line | Any 3 consecutive cells contain one digit from each set: {1–3}, {4–6}, {7–9} | 7×7 |
| 34 | **Modular Line** ✅ (ID 43) | Teal line | Any 3 consecutive cells contain digits from different residue classes mod 3 | 6×6 |
| 35 | **Zipper Line** ✅ (ID 44) | Lavender line with central marker | Pairs of cells equidistant from the centre sum to the same value | 6×6 |
| 36 | **Region Sum Line** ✅ (ID 45) | Line with box-border tick marks | Each segment of the line divided by box borders sums to the same total | 7×7 |
| 37 | **N-Line (10-Line)** ✅ (ID 46) | Numbered line with segment ticks | Each box-bounded segment of the line sums to exactly N (default N=10) | 7×7 |
| 38 | **Index Line** | Line with position labels | The digit at position P on the line (1-indexed from one end) equals P | 7×7 |
| 39 | **Sum Line** ✅ (ID 48) | Line with total label at endpoint | The entire line sums to a fixed displayed value | 6×6 |
| 40 | **Unique Set Line** ✅ | Line with "U" endpoint marker | No digit repeats on the line, independent of Sudoku regions | 5×5 |
| 41 | **Skyscraper Line** | Line with building silhouette endpoints | Digits from each endpoint encode how many increasing "buildings" are visible from that end | 7×7 |
| 42 | **Thermo Loop** | Closed loop line with bulb marker | Digits must strictly increase cyclically around the loop — note: unsatisfiable unless relaxed to non-strict (≥) | 8×8 |
| 43 | **Slow Thermo** ✅ | Thermo line with open bulb | Non-strict increase — each cell must be ≥ the previous | 5×5 |
| 44 | **Fast Thermo** ✅ (ID 51) | Thermo line with a double bulb | Each cell must increase by ≥ N from the previous (N configurable) | 6×6 |
| 45 | **Ambiguous Thermo** ✅ (ID 52) | Plain grey line, bars at both ends, no bulb | Direction is unknown to the player — the line increases in one direction; both ends carry the same ink bar, so neither shows the direction | 6×6 |

---

### Region / Area Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 46 | **Irregular Regions (Jigsaw)** | Thick borders on irregular region shapes | Non-rectangular region shapes replace standard boxes (partially supported via region variants) | 5×5 |
| 47 | **Extra Region Overlay** ✅ (ID 53) | Dashed coloured border on extra regions | Additional arbitrary regions with no digit repeats, independent of standard boxes | 5×5 |
| 48 | **Region Sum Equality** | Region total labels; equality indicator | All regions (standard or extra) sum to the same total | 6×6 |
| 49 | **Region Parity Balance** | Parity shading overlay per region | Each region must contain equal counts of odd and even digits | 6×6 |
| 50 | **Region Index Rule** | Small index label per region cell | The digit at a cell equals the count of a defined feature in its region | 7×7 |
| 51 | **Connected Regions** | Connectivity arrows between equal-digit cells | All occurrences of each digit must form a single orthogonally connected group | 7×7 |

---

### Adjacency / Pair Constraints

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 52 | **Full Kropki** ✅ | White dots (diff=1) and black dots (ratio=1:2) on all valid pairs | All valid Kropki pairs between orthogonally adjacent cells are shown. Absence of a dot means the pair does **not** satisfy either relation | 6×6 |
| 53 | **Full White Kropki** ✅ (ID 58) | White dot on every diff=1 adjacent pair | All difference=1 pairs are marked; absence of a dot forbids diff=1 | 5×5 |
| 54 | **Full Black Kropki** ✅ (ID 59) | Black dot on every ratio=1:2 adjacent pair | All ratio=1:2 pairs are marked; absence of a dot forbids ratio=1:2 | 6×6 |
| 55 | **Sum Kropki** ✅ | Labelled dot between cells showing sum K | A dot indicates the two adjacent cells sum to K | 5×5 |
| 56 | **Difference ≥ K** ✅ (ID 60, K = 3) | Plum tag "≥3" on the edge | Adjacent orthogonal cells must differ by at least K | 5×5 |
| 57 | **Ratio (Generalized)** ✅ (ID 61, N = 3) | Labelled black dot showing ratio N | One cell is exactly N times the other (not limited to 1:2) | 6×6 |
| 58 | **Inequality Chains** | Chevron symbols (> or <) between cells | Consecutive cells in a row or column are connected by > or < indicators | 5×5 |
| 59 | **Distance Pair** ✅ (ID 63) | Labelled connector between paired cells | The absolute difference \|A − B\| equals the taxicab distance between the two cells | 6×6 |
| 60 | **Greater/Less Than** ✅ | Single chevron (> or <) between adjacent cells | A chevron between two adjacent cells indicates which must be larger | 5×5 |
| 61 | **XV Pairs** ✅ | X or V label between adjacent cells | X: two cells sum to 10; V: two cells sum to 5 | 5×5 |

- Full Kropki (#52) is restricted to 6×6+ because on 5×5 the constraint density is overwhelming.

---

### Arithmetic / Cage Variants

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 62 | **Killer (Hidden Sum)** ✅ (ID 64) | Dashed cage, no sum label | Cage with no repeats; sum is hidden from the player | 7×7 |
| 63 | **Cage Product** ✅ (ID 65) | Dashed cage + product label (×) | Cells in a cage multiply to the displayed product; no repeats | 6×6 |
| 64 | **Cage Difference** ✅ (ID 66) | Two-cell cage + difference label (−) | Two-cell cage: the absolute difference of its two digits equals the label | 5×5 |
| 65 | **Cage Ratio** ✅ (ID 67) | Two-cell cage + ratio label (:) | Two-cell cage: the ratio of its two digits equals the label | 5×5 |
| 66 | **Magic Square Region** ✅ (ID 68, 9×9 only: a magic 3×3 square needs the digits 1–9) | Highlighted 3×3 sub-region | A 3×3 sub-region sums to 15 in every row, column, and diagonal | 7×7 |
| 67 | **Renban Cage** ✅ (ID 69) | Dashed cage with consecutive-set indicator | Cage digits form a consecutive set in any order | 5×5 |
| 68 | **Arrow Average** ✅ (ID 70) | Circle + arrow path | The digit in the circle equals the average of the digits along the arrow path | 7×7 |
| 69 | **Arrow Product** | Circle + arrow path (× symbol) | The digit in the circle equals the product of the digits along the arrow path | 6×6 |
| 70 | **Pill Arrow** ✅ (ID 72) | Two-cell pill bulb + arrow path | The two-cell bulb forms a two-digit number; the path sum equals that number | 8×8 |
| 71 | **Double Arrow** ✅ (ID 73) | Two circles connected by arrow path | Path sum equals the sum of both circle endpoint digits | 6×6 |

---

### Cell / Value Semantics

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 72 | **Prime Cells** ✅ | P-labelled cell markers | Marked cells must contain prime digits: 2, 3, 5, or 7 | 5×5 |
| 73 | **Multiples Cells** ✅ (ID 74, N = 3) | ×N-labelled cell markers | Marked cells must contain a multiple of N | 5×5 |
| 74 | **Index Cell** | Self-referential circle marker | A cell's digit encodes the count of some defined feature in its row or column | 7×7 |
| 75 | **Mirror Cells** ✅ (ID 76) | Paired letter badges (same letter) | Paired cells must contain the same digit | 5×5 |
| 76 | **Opposite Cells Sum** ✅ (ID 77) | Paired cell markers with sum label | Cells symmetric about the grid centre must sum to a fixed constant | 5×5 |
| 77 | **Counting Circles** ✅ (ID 78) | Small circle markers on cells | A circled cell's digit equals the count of circles whose value matches that digit | 6×6 |
| 78 | **Fortress** ✅ | Grey shading on fortress cells | Grey-shaded cells must be strictly greater than all orthogonally adjacent non-shaded cells | 6×6 |
| 79 | **Quadruples** ✅ (ID 79) | Corner circles shared by 4 adjacent cells | All digits listed inside the corner circle must appear in the 4 surrounding cells | 9×9 |
| 80 | **Distance Cell** ✅ (ID 80) | Cell marker with distance label | A cell's digit equals its taxicab distance to a specified target cell | 7×7 |

---

### Outside Clue Systems

| # | Modifier | Visual | Rule | Min Size |
|:---:|----------|--------|------|:---:|
| 81 | **Skyscrapers** ✅ (ID 81) | Numbers outside each row/column edge | A clue outside a row or column counts how many distinct increasing "buildings" are visible from that edge | 5×5 |
| 82 | **Sandwich** ✅ (ID 82) | Numbers outside each row/column edge | A clue gives the sum of digits strictly between two specified sentinel digits in the row or column | 6×6 |
| 83 | **X-Sums** ✅ (ID 83) | Numbers outside each row/column edge | The first digit equals X; the clue gives the sum of the first X digits in the row or column from that edge | 5×5 |
| 84 | **Little Killer** ✅ (ID 84) | Diagonal arrows outside the grid with sum labels | A clue outside the grid gives the sum along a diagonal of arbitrary length | 6×6 |
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
| 95 | **Constraint Negation Zone** ✅ (ID 93, Inverted Zone) | Shaded 3×3 area with green whisper lines | Built as an inverted whisper zone (owner decision 2026-10-19): neighbours on a green line differ by 5 or more, but by less than 5 for a step between two cells inside the area | 9×9 |

---

## Resolved Design Decisions

### Multi-Modifier Solvability
Every in-game puzzle has **exactly one solution**: clues are removed only while the base-rule solution stays unique, and modifier geometry is derived from that solution (SPEC-AMB-003). Puzzles are still **not required to be logically solvable** without guessing. This applies to both floor-modified puzzles and boss puzzles (which stack floor + boss modifiers). Conflicting modifier combinations (e.g. Nonconsecutive + Difference Kropki) or fog-obscured constraint geometry are intentional — items and relics exist to help the player through impossible-seeming states. The generator does not validate logical solvability.

### Modifier Pool Rolling — Equal Weights
All eligible modifiers have **equal probability** in both floor modifier rolls and boss choice rolls. There is no difficulty-based weighting or floor-based frequency adjustment. This is intentional — the variety and unpredictability are core to the roguelike experience.

### Endless Zen — Full Modifier Pool
Endless Zen uses the generatable player modifiers for its board size (§Endless Zen Mode Integration). Nonconsecutive and Antiknight (no visible geometry) display a text indicator in the HUD so the player knows the global constraint is active.

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

A collapsible **legend panel** is shown on the puzzle screen whenever at least one modifier is active (implemented 1.1, GAP-025; details in §Modifier Legend Panel):

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

### Implementation Notes (as built, 1.1)

- `HudViewController.Legend.cs`: a "Legend" button above the numpad (right column) opens the panel over the hint area; entries follow the order of `LevelConfig.ActiveModifiers` (debuffs included). No-geometry rules show a grey swatch instead of "∅".
- `BoardViewController.OverlayGroups.cs`: overlay geometry is drawn into one `CanvasGroup` layer per modifier; isolation sets the other layers to alpha 0.15 for 3 s. Pressure overlays and Fog of War are not layers and are never dimmed.
- Tapping the isolated entry again restores all layers. Not built: the 1.05 pulse on the isolated layer and cancel-by-board-tap (future polish).
- The panel collapses after 5 s without interaction; it is hidden while the `sealed_eyes` curse hides boss modifier descriptions.
- Evidence: `InRunUiPlayModeTests.Legend_ListsTheModifiers_AndTapIsolatesOneLayer`.

---



---

## Requirements Traceability

The canonical requirement IDs for this document are listed in `docs/REQUIREMENT_MAP.md`. The auto-generated `REQ-*` table from the retired SPICE pipeline was moved to `docs/archive/SPICE_REQUIREMENT_TABLES.md` (2026-10-07, SPEC-OBS-001); its IDs are kept there and are not used by code or tests.
