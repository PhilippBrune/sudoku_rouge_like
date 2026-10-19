# Plan: the 64 catalog modifiers (IDs 30–93)

**Version:** 1.1 | **Date:** 2026-10-19 | **Status:** implemented (Phase 0 steps 1, 2, 7 in part; batches B1 and B2) · B3–B6 open

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-10-19 | implemented | Owner decisions (2026-10-19): build Phase 0 + B1 + B2 now; new modifiers roll at equal weight; codex and completion grow; the drop list stands; line colours follow the setters' convention. Built: per-type presence (GAP-047, which exposed GAP-049), cage/arrow subtype filters, the 13 B1/B2 modifiers with rules, geometry, overlays, legend layers and five-language texts, blocked pairs, a soundness test per modifier (`CatalogModifierTests`) and a catalog completeness test. Rule details follow the spec (Entropic Line from 7×7, Difference ≥ K with K = 3, High-Low with 5 low on 9×9). Not yet: the catalog refactor of the name and abbreviation switches (step 3), golden geometry tests (step 5), the pair harness (step 7, [Explicit] part), the Custom Puzzle panel for 43 modifiers (needs a mockup), B3–B6. |
| 1.0 | 2026-10-18 | new | Owner request: a plan for bringing the defined-but-never-generated modifiers (enum IDs 30–93) into the game (roadmap RM-001). Made with ECC's `planner` from the code; facts carry file:line references. |

Short forms: PGS = `Assets/Scripts/Sudoku/PuzzleGenerationService.cs`, MGG = `Sudoku/ModifierGeometryGenerator.cs`, BVC = `UI/BoardViewController.cs`, BS = `Boss/BossService.cs`.

## Where we stand

Each of the 64 IDs exists only as an enum value (`Core/GameEnums.cs:62–125`), an icon name (BS:365–428) and a PNG in `Assets/Resources/modifier/`. None has a `ModifierData` entry (BS:19–79, the switch that lets a modifier roll), a rule (`ModifierFactory.cs:14–110`), geometry (MGG:61–140), a name, description or abbreviation (BS:505–666 falls back to the enum name, an empty text and "??") or localization keys. The enum comments "newly implemented" (GameEnums.cs:44, 61) are wrong.

Facts that shape the plan:
- **Uniqueness is free for geometry read off the solution.** Boards are already unique under the base rules, and the modifier re-check is skipped for them (PGS:381–386). Rules that change the solution space (X-Sudoku, Hyper) must go into the generator's structural path (`SudokuGenerator.cs:8–11, 347–358`).
- **GAP-047:** the check that every chosen modifier got its geometry works per family, not per type: any line satisfies any line modifier, any dot any Kropki, unknown IDs pass (PGS:498–561, used by `RunDirector.LevelGeneration.cs:563, 914, 1186` and `RunDirector.cs:2574`). This already affects today's game when two modifiers of one family are chosen.
- Two rules ignore their subtype: `KillerCagesRule` ignores `CageType`, `ArrowSumsRule` ignores `ArrowType` (`ConstraintRules.cs:293–361`). Both must filter before new cage or arrow types ship.
- Conflict highlighting runs every rule (BVC:699–708), so a rule must not leak hidden data (for example a hidden cage sum).
- `Tier` and `Impact` in `ModifierData` have no callers; only `MinBoardSize` is used.
- The spec library numbers (`BossMechanicsSystem.md`, #16–95) differ from the enum IDs.

## Verdict per ID

Ship **28**, ship later **12**, rework **2**, drop **22** (keep the enum value and icon, never register). Fit = S ship, L later, W rework, D drop.

| ID | Modifier | Family | Fit | Reason |
|---|---|---|---|---|
| 30 | X-Sudoku | Global | S | Classic; two extra houses; rectangular regions only |
| 31 | Hyper Sudoku | Region | S | 9×9 only |
| 32 | Disjoint Groups | Global | L | Rectangular boxes only; weak visual |
| 33 | Index Parity | Global | W | Forces a checkerboard; impossible on odd sizes |
| 34 | Modular Global | Global | D | Rule undefined |
| 35 | Knight+King | Global | D | Same as Antiknight + Antiking together |
| 36 | Toroidal | Global | D | No effect on its own |
| 37 | Generalized Whisper | Line | L | A third whisper |
| 38 | Nonconsecutive Line | Line | S | Cheap, readable |
| 39 | Alternating Parity Line | Line | D | Duplicates Parity Lines |
| 40 | High/Low Line | Line | S | Middle digit on odd sizes to define |
| 41 | Nabner Line | Line | S | Lines up to ⌈n/2⌉ cells |
| 42 | Entropic Line | Line | S | 9×9 only (as SPEC-CONF-012) |
| 43 | Modular Line | Line | S | Cheap |
| 44 | Zipper Line | Line | S | Popular; needs a centre marker |
| 45 | Region Sum Line | Line | S | Popular; works with irregular regions |
| 46 | N-Line | Line | L | Variant of 45 |
| 47 | Index Line | Line | D | Same as givens |
| 48 | Sum Line | Line | L | Little new |
| 49 | Skyscraper Line | Line | D | Confusing next to 81 |
| 50 | Thermo Loop | Line | D | A cyclic ≥ chain forces equal digits |
| 51 | Fast Thermo | Line | S | Thermo with step 2 |
| 52 | Ambiguous Thermo | Line | S | Increases in one of the two directions |
| 53 | Extra Region | Region | S | Read off the solution; outlined |
| 54 | Region Sum Equality | Region | D | Always true |
| 55 | Region Parity Balance | Region | D | Fixed by region size |
| 56 | Region Index Rule | Region | D | Undefined |
| 57 | Connected Regions | Region | D | Impossible (equal digits cannot touch) |
| 58 | Full White Kropki | Dot | S | Reuses `GenerateFullKropki` (MGG:988) |
| 59 | Full Black Kropki | Dot | S | Same |
| 60 | Difference K | Pair | S | Cheap |
| 61 | Ratio N | Pair | L | Few valid pairs |
| 62 | Inequality Chains | Pair | D | 144 chevrons on 9×9 |
| 63 | Distance Pair | Pair | L | Needs a connector visual |
| 64 | Hidden Killer | Cage | S | No-repeat cages with hidden sums |
| 65 | Product Cage | Cage | S | Familiar from KenKen |
| 66 | Difference Cage | Cage | S | Two cells, any size |
| 67 | Ratio Cage | Cage | L | Overlaps the black dot |
| 68 | Magic Square | Region | L | 9×9; one box prefilled |
| 69 | Renban Cage | Cage | S | Reuses Renban logic |
| 70 | Arrow Average | Arrow | S | Cheap |
| 71 | Arrow Product | Arrow | D | Paths are almost only 1s and 2s |
| 72 | Pill Arrow | Arrow | L | Needs a pill visual, 8×8+ |
| 73 | Double Arrow | Arrow | S | Second circle |
| 74 | Multiple Cells | Cell | S | Like Prime Cells, 6×6+ |
| 75 | Index Cell | Cell | D | Breaks when Given Reveal adds givens |
| 76 | Mirror Cells | Cell | S | Readable |
| 77 | Opposite Cells Sum | Cell | L | Needs pair markers |
| 78 | Counting Circles | Cell | L | Hard to explain |
| 79 | Quadruples | Corner | S | Corner clues |
| 80 | Distance Cell | Cell | L | Medium readability |
| 81 | Skyscrapers | Outside | S | Classic |
| 82 | Sandwich | Outside | S | Sentinels 1 and n |
| 83 | X-Sums | Outside | S | Clue ring |
| 84 | Little Killer | Outside | S | Arrows at the corners |
| 85 | Battlefield | Outside | D | Hard to read |
| 86 | Outside Inequalities | Outside | D | Weak rule |
| 87–92 | Knight Path, Hamiltonian Path, Snake, Loop, Digit Graph, Self-Referential Grid | Path/Meta | D | Undefined or whole-grid rules; too slow and unreadable |
| 93 | Constraint Negation Zone | Meta | W | A good roguelike twist; needs a design for inverting rules |

## Phase 0: shared groundwork (nothing goes live)

1. **Presence check per type** (fixes GAP-047): one check replaces both copies (PGS:498–561 and `RunDirector.cs:2713–2786`), matching the line, cage, arrow, marker or pair type; unknown IDs return false.
2. **Subtype filters** in `KillerCagesRule` and `ArrowSumsRule` (plain Sum only; existing data defaults to Sum, so nothing changes today).
3. **One catalog entry per ID** (name, description, abbreviation, localization IDs, overlay group, legend colour, the Custom Puzzle list) with a contract test that every registered modifier has all of them.
4. **Structural rule set** replacing the generator's 7 flags, with extra houses and prefilled cells (for 30, 31, 32, 53, 68).
5. **Geometry helpers**: a shared path grower with a per-type check (from `TryBuildConsecutiveLine`, MGG:842); `MarkOverlayCellsUsed` for regions, cell pairs and corners.
6. **Rendering**: a clue ring outside the grid (the board shrinks when clues exist), corner circles, region outlines without a label (via `DrawCageOutline`), labels for line values, cage operators and second circles, paper-safe colours.
7. **Soundness harness** (EditMode): 50 seeds × each board size × each region variant; checks own geometry, that every rule accepts the solution (full and partial), uniqueness, and time (p95 under 1 s, max under 4 s). Each new modifier is also paired with every live one to find pairs that must be blocked.

The `ModifierData` entry is the release switch, so it is added last in each batch.

## Batches

| Batch | Modifiers (Tier / Impact / min size) | Risk | Notes |
|---|---|---|---|
| B1 line variants | 38 (1/.20/5), 40 (2/.25/6), 41 (2/.30/6), 42 (3/.40/9), 43 (2/.30/6), 44 (2/.35/6), 51 (2/.30/6), 52 (3/.40/6) | low | Paths grown on the solution; Fast Thermo double bulb, Ambiguous Thermo no bulb, Zipper centre dot |
| B2 dots, pairs, cells | 58 (3/.45/6), 59 (2/.35/6), 60 (1/.20/6, K=3), 74 (1/.15/6, N=3), 76 (1/.20/5) | low | Block 58/59 with Full Kropki and with each other |
| B3 cages and arrows | 45 (3/.45/6), 64 (3/.50/7), 65 (3/.45/6), 66 (1/.20/5), 69 (2/.35/6), 70 (3/.45/6), 73 (3/.45/6) | medium | Needs the subtype filters; block 64 with Killer Cages |
| B4 clue ring and corners | 79 (2/.30/5), 81, 82, 83 (3/.50/6), 84 (3/.45/6) | medium (layout) | Clues from the solution, count by intensity; rules must reject impossible partial rows so Custom 7★ stays fast |
| B5 extra houses | 30 (2/.35/6), 31 (3/.45/9), 53 (2/.30/5); then 32, 68 if benchmarks pass | high (generation time) | Rectangular regions only for 30–32; benchmark like Anti-Bishop |
| B6 later | 37, 46, 48, 61, 63, 67, 72, 77, 78, 80; reworks 33, 93 after owner decisions | — | — |

**Every batch:** rules in a new partial file, a `ModifierFactory` case, geometry per family, renderer, overlay group and legend colour, texts in all five languages, icon check, balance data, blocked pairs from the harness.

**Verification per batch:** rule unit tests (valid, invalid, partial, both cell orders); the soundness harness; screenshots at 6×6 and 9×9 with alternative symbols on (`LayoutScreenshotDiagnostics`); an in-run E2E (boss-gate pick, correct and wrong digit, conflict highlight, legend, Save & Quit, resume keeps overlay and rules, codex entry).

## Proposed requirement IDs

- BOSS-MOD-004 presence per type; BOSS-MOD-005 catalog completeness; BOSS-MOD-006 rule soundness (no false conflicts, no leaked hidden data).
- BOSS-LIB-NNN per shipped modifier, NNN = enum ID (BOSS-LIB-038 …).
- GEN-SOLVE-002 extra houses; GEN-SOLVE-003 prefilled region; GEN-MOD-002 batch-2 geometry from the solution.
- UI-STYLE-010 clue ring outside the grid; UI-STYLE-011 corner clues and labels (007–009 are taken by the screen proposals).

They go into `REQUIREMENT_MAP.md` before any test is tagged (`RequirementTraceabilityTests` rejects unknown IDs).

## Spec contradictions found (owner decides, not resolved here)

- Ambiguous Thermo: the enum comment (GameEnums.cs:84) and the spec (`BossMechanicsSystem.md:534`) differ.
- Thermo Loop: the spec says "strictly increasing, unsatisfiable" (531), the enum comment says "≥" (GameEnums.cs:82).
- Alternating Parity Line duplicates Parity Lines.
- The spec gives Quadruples a 9×9 minimum (598).
- The library numbering does not match the enum IDs.

To be filed as SPEC-CONF items once the owner looks at them.

## Risks

- **9×9 generation time:** cap the attempts for Zipper, Region Sum and Renban cage geometry; extra houses add dead ends (two board-shaping rules already force 8×8, `RunDirector.LevelGeneration.cs:506–513`).
- **Combining rules:** one list holds all cage types and one all line types, so the subtype filters are required; at most one thermo-family modifier per puzzle.
- **Codex and completion:** the codex grows from 30 to up to 58 entries and already counts Modular Regions, which never rolls (`ItemsMenuController.cs:113–123`); completion % drops for existing players (`CompletionService.cs:46`, `CompletionTrackingTests.cs:59` hard-codes 29).
- **Saves:** never renumber the enum; overlays already serialize the batch-2 fields, so v0.1.5 saves load; a new presence check may choose a different overlay seed for a floor in progress (check with `DeterminismAcrossResumeTests`).
- **Localization:** about 56 strings × 4 languages; check truncation on gate cards and in the legend.

## Owner decisions needed

1. Roll weights: should new modifiers unlock over time, be gated by Tier, or roll at equal weight (which halves the chance of each existing one)?
2. Codex and completion: grow the totals, or give new modifiers their own "library" page?
3. The drop list: approve (enum value and icon stay, never registered)?
4. Rule details: High/Low middle digit, Ambiguous Thermo, the K and N constants, Index Parity, Negation Zone.
5. Outside clues: shrink the board or move the HUD?
6. Modifiers add information to boards that are already unique, so puzzles get easier. Should clue removal count modifier rules (fewer givens with modifiers)?
7. Custom Puzzle, Endless Zen, Spirit Trials, Monthly Walk: new modifiers right away or later?
8. One batch per Steam update?
