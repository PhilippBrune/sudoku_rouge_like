# Daily Success Report - 2026-03-07

## JIRA-0006 — Puzzle Region Layout Variants & Box-Size Modifier

### Implemented
- Added `RegionVariant` field to `LevelConfig` and `PuzzleGenerationRequest`.
- `SudokuGenerator.BuildRegionMap()` now accepts a `variant` parameter.
- **6×6** boards: variant 0 = 2×3 boxes (original), variant 1 = 3×2 boxes.
- **8×8** boards: variant 0 = 2×4 boxes (original), variant 1 = 4×2 boxes.
- **5×5** boards: 2 distinct region templates (hourglass and diagonal-band), selected by variant.
- **7×7** boards: 2 distinct region templates (horizontal wave bands and vertical wave bands), selected by variant. **Both new templates fix the contiguity bug from JIRA-0005** where 5 of 7 regions had disconnected cells in the last row.
- **9×9** boards: 3×3 boxes (no variant — only valid factoring).
- `RunDirector.BuildLevelConfig()` randomly assigns `RegionVariant = random.Next(2)` per level.
- `SudokuGenerationService` passes variant through to solved board and region map generation.
- `RunMapController.CloneLevelConfig()` now copies `RegionVariant`.

### Files Modified
- `Assets/Scripts/Core/RuntimeModels.cs` — added `RegionVariant` to `LevelConfig`
- `Assets/Scripts/Core/HardeningModels.cs` — added `RegionVariant` to `PuzzleGenerationRequest`
- `Assets/Scripts/Sudoku/SudokuGenerator.cs` — reworked `BuildRegionMap` with variant param; added `Get5x5Template()`, `Get7x7Template()`; fixed broken 7×7 template
- `Assets/Scripts/Sudoku/SudokuGenerationService.cs` — pass `RegionVariant` through to solved board builder and region map builder
- `Assets/Scripts/Run/RunDirector.cs` — assign random `RegionVariant` in `BuildLevelConfig()`; pass to generation request and fallback
- `Assets/Scripts/UI/RunMapController.cs` — copy `RegionVariant` in config clone

## JIRA-0006 R5-R7 — Irregular Jigsaw Region Templates

### Implemented
- **6×6 jigsaw** (variant 2): 6 non-rectangular hexomino regions, staircase L-shapes, all orthogonally contiguous.
- **8×8 jigsaw** (variant 2): 8 non-rectangular octomino regions, symmetric staircase with mirrored L-shapes in top/bottom halves.
- **9×9 jigsaw** (variant 2): 9 non-rectangular nonomino regions, diagonal wave staircase spanning full grid, includes U-shaped region.
- All templates validated via PowerShell BFS contiguity check (cell counts AND connected-component verification).
- `RunDirector.BuildLevelConfig()` updated: `_random.Next(2)` → `_random.Next(3)` to include jigsaw variant.
- Added `Get6x6Template()`, `Get8x8Template()`, `Get9x9Template()` methods to `SudokuGenerator`.
- `BuildRegionMap()` dispatches to jigsaw templates when `variant == 2` for sizes 6, 8, 9.

### Files Modified
- `Assets/Scripts/Sudoku/SudokuGenerator.cs` — added jigsaw template methods and dispatch logic
- `Assets/Scripts/Run/RunDirector.cs` — updated variant range to include jigsaw

## Unity Slow Load Investigation
- Root cause: **88,000+ files / 3.9 GB** in `Assets/Scripts/` folder, of which **53,236 files (1,980 MB) in `Assets/Scripts/My project/`** and **34,790 files (1,932 MB) in `Assets/Scripts/My project_clean/`** are duplicate mirror trees.
- The actual game scripts are only ~96 files under the non-mirror directories.
- Recommendation: delete `My project/` and `My project_clean/` entirely to reduce load time from minutes to seconds. The `.asmdef` isolation from JIRA-0005 prevents compilation but Unity still scans and imports all files on load.

## JIRA-0006 R5–R7 — Irregular Jigsaw Region Templates
- Added **variant 2** (jigsaw / irregular) region templates for 6×6, 8×8, and 9×9 boards.
- **6×6**: 6 non-rectangular hexomino regions (staircase L-shapes).
- **8×8**: 8 non-rectangular octomino regions (symmetric mirrored L-shapes).
- **9×9**: 9 non-rectangular nonomino regions (diagonal wave staircase).
- All templates validated with BFS for correct cell counts and orthogonal contiguity.
- `RunDirector.BuildLevelConfig()` updated: `_random.Next(2)` → `_random.Next(3)` to include jigsaw variant.
- Files modified: `SudokuGenerator.cs`, `RunDirector.cs`.

## JIRA-0007 — Scene Setup Guide
- Both `.unity` scene files were lost after the mirror tree cleanup.
- Identified that both scenes are built at runtime by `MainMenuBlueprintBuilder` and `InRunUiBlueprintBuilder`.
- Created step-by-step scene setup instructions requiring only 2–3 empty GameObjects per scene.

## JIRA-0008 — UI Polish, Bug Fixes & Feature Additions

### Completed Requirements (17 of 29)

**Options Panel:**
- **R1**: Fixed Y-coordinate overlaps in Options panel — shifted Display, Language, Resolution, Accessibility, Highlight sections down by ~5% each.
- **R2**: Fixed invisible dropdown items — set `dropdown.itemImage = null` instead of `itemBackground` to prevent Unity from disabling the background Image.
- **R3**: Wired Highlight Errors toggle to puzzle rendering — added `HasConflict()` method checking row/col/region for duplicates, conflict color highlighting in `RenderBoard()`.

**Items Archive:**
- **R4**: Moved icon strip from y(0.62-0.74) to y(0.22-0.34) to eliminate overlap with filter buttons. Repositioned details text and tooltip to non-overlapping areas.

**Tutorial:**
- **R5**: Added "Region Layout" dropdown to tutorial setup — 3 options: Standard, Rectangular Alt, Irregular (Jigsaw). Added `RegionVariant` to `TutorialSetupConfig`, wired through `TutorialModeService.BuildLevelConfig()`.
- **R6**: Tutorial quit now sets PlayerPrefs key so MainMenu reopens tutorial progress panel.

**Puzzle Screen:**
- **R9**: Added `EnsureMainCamera()` to `InRunUiBlueprintBuilder` — creates Camera + AudioListener, fixing "Display 1 no cameras rendering" and enabling puzzle music.
- **R10**: Changed pencil toggle button from absolute anchoring to 10th child of numpad GridLayoutGroup (row 4, below 7-8-9).
- **R11**: Added tutorial mode check in `RebuildPuzzleItemBar()` — hides item bar when `TutorialMode` is true.
- **R15**: Changed lane text alignment from `UpperLeft` to `UpperCenter`.
- **R16**: Already implemented — confirmed LevelInfo text shows "Level: X  Depth: Y".

**Select Class:**
- **R18-R19**: Moved class select text to bottom of panel (y 0.20-0.42, font 15px). Added `<color=#FF4444>` rich text tags around unlock condition text.

**Reward Overview:**
- **R22**: Enhanced `DescribeRollSlot()` to include full item effect descriptions in hover tooltip (matching the item type switch from the item bar descriptions).

**Main Menu:**
- **R24**: Added debug hotkey P for auto-solve — `DebugAutoSolve()` fills empty non-given cells with solution values.
- **R25**: Enhanced background image loading with `TryLoadBackgroundTextureFromDocs()` fallback searching `docs/` and `docs/icons/` directories.
- **R26**: Increased menu music loop crossfade window from 0.35s to 2.0s and fraction from 1/8 to 1/4.

**Meta Progression:**
- **R27**: `RefreshView()` now shows only selected class details instead of all unlocked classes.

### Files Modified
- `InRunUiBlueprintBuilder.cs` — camera creation, lane text centering
- `MainMenuBlueprintBuilder.cs` — options layout, dropdown fix, background fallback, class select layout, tutorial region dropdown, items archive layout
- `PrototypeRunScreenController.cs` — conflict highlighting, pencil button, tutorial quit, item bar hiding, reward tooltips
- `MetaProgressionPanelController.cs` — selected class filtering
- `PrototypeUiDebugHotkeys.cs` — P key auto-solve
- `MainMenuController.cs` — red unlock condition text
- `RuntimeModels.cs` — RegionVariant field in TutorialSetupConfig
- `TutorialModeService.cs` — RegionVariant in BuildLevelConfig
- `TutorialMenuController.cs` — region dropdown field, Configure parameter, event wiring
- `MenuMusicController.cs` — increased crossfade window

### Still Open
- R7 (Boss mechanics in tutorial), R8 (Resource mode chooser), R12-R13 (Board size lock), R14 (Puzzle music loop — partially fixed by R9 camera), R17 (Item removal — verified already works), R20-R21 (Path tile spacing/boss gate), R23 (Relic functionality audit), R28-R29 (Sound/overlap audits)
