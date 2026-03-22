# Implementation Audit — Previous Master Prompt

Audit scope: verifies whether the previously provided production blueprint is implemented in game code (not just documentation).

Status legend:
- ✅ Implemented
- 🟡 Partial
- ❌ Missing

## 1) Main Menu + Options

- 🟡 Main flow states exist (Start, Tutorial, Resume, Options, Credits): [MenuFlowService](../Assets/Scripts/UI/MenuFlowService.cs#L10-L20)
- 🟡 Resume gating exists: [MenuFlowService](../Assets/Scripts/UI/MenuFlowService.cs#L22-L37)
- 🟡 Language/audio/graphics setting models exist: [RuntimeModels](../Assets/Scripts/Core/RuntimeModels.cs#L218-L259)
- ❌ No concrete UI controllers/views for sliders/buttons/exit handling in codebase

## 2) Branching Garden Node Structure (8–12 nodes, node types, 2-layer visibility)

- ❌ Not implemented as node graph
- 🟡 Route choice exists as simple pair roll and profile effect: [RouteService](../Assets/Scripts/Route/RouteService.cs#L17-L65)
- ❌ No node types (Shop/Rest/Relic/Event/Elite/Boss) as runtime map entities
- ❌ No “see next 2 layers” visibility system

## 3) Risk vs Reward Paths

- 🟡 Basic path effects implemented (gold/xp/stars/mistake penalty): [RouteService](../Assets/Scripts/Route/RouteService.cs#L33-L65)
- ❌ No elite chance weighting by route
- ❌ No relic pool quality by route

## 4) Run Economy Loop

- 🟡 Gold + XP + reroll/pencil spend implemented: [RunDirector](../Assets/Scripts/Run/RunDirector.cs#L304-L336), [FormulaService](../Assets/Scripts/Economy/FormulaService.cs)
- ❌ Shop node flow not implemented
- ❌ Relic purchase flow not implemented
- ❌ Emergency heal purchase flow not implemented

## 5) Item System Architecture

- 🟡 Item roll + reroll + lock behavior exists: [ItemService](../Assets/Scripts/Items/ItemService.cs#L15-L92)
- ❌ Slot count does not follow requested star mapping (currently by difficulty, max 4): [ItemService](../Assets/Scripts/Items/ItemService.cs#L17-L26)
- ❌ “Nothing” selected gold bonus not implemented
- 🟡 Replacement exists but is automatic FIFO when full (no player choice / higher-difficulty gate): [RunDirector](../Assets/Scripts/Run/RunDirector.cs#L485-L495)
- ❌ Consumables vs relic inventories are not fully separated as gameplay systems

## 6) Class System

- ✅ Single start class + gated unlock progression implemented: [ClassUnlockService](../Assets/Scripts/Classes/ClassUnlockService.cs), [ProfileService](../Assets/Scripts/Save/ProfileService.cs#L62-L67)
- 🟡 Class balance metadata exists (tier/complexity/passive): [ClassCatalog](../Assets/Scripts/Classes/ClassCatalog.cs)
- ❌ Gold modifier / modifier interaction bias / explicit risk tolerance are not fully modeled per class in runtime combat equations

## 7) Sudoku Engine Hard Requirements

- ✅ Deterministic constructive generation with incremental removal checks: [SudokuGenerationService](../Assets/Scripts/Sudoku/SudokuGenerationService.cs)
- ✅ Uniqueness verification via backtracking count: [SudokuBacktrackingSolver](../Assets/Scripts/Sudoku/SudokuBacktrackingSolver.cs)
- ✅ Logical solvability gate included in generation: [SudokuGenerationService](../Assets/Scripts/Sudoku/SudokuGenerationService.cs#L41-L63)
- 🟡 Logical technique execution currently Tier 1 only (Naked/Hidden Singles): [SudokuLogicalAnalyzer](../Assets/Scripts/Sudoku/SudokuLogicalAnalyzer.cs#L17-L33)
- 🟡 Tier 2–4 represented in enums/weights but not solved-by-technique execution yet: [GameEnums](../Assets/Scripts/Core/GameEnums.cs#L136-L145), [SudokuDifficultyGrader](../Assets/Scripts/Sudoku/SudokuDifficultyGrader.cs#L9-L18)

## 8) Mathematical Difficulty / Heat / Progression Arc

- ✅ Heat model service exists and is integrated in run updates: [HeatScoreService](../Assets/Scripts/Economy/HeatScoreService.cs), [RunDirector](../Assets/Scripts/Run/RunDirector.cs#L689-L716)
- 🟡 Difficulty heatmap concept exists in data/formulas, but no explicit 3-axis heatmap artifact/system used for balancing output
- ❌ 10-run arc with explicit node-phase content (elite introduction timing etc.) not fully implemented as run-graph progression

## 9) Multi-Stage Boss

- ✅ 3-phase boss scaffold implemented: [BossService](../Assets/Scripts/Boss/BossService.cs#L43-L79)
- 🟡 “Choose 1 of 2 modifier options before Phase 1” exists at service level but not fully wired to full phase execution loop/UI
- 🟡 HP carryover is supported by run HP model but full boss scene orchestration is not present

## 10) Long-Term Meta Progression

- ✅ Mastery/completion tracking scaffolding exists: [HardeningModels](../Assets/Scripts/Core/HardeningModels.cs#L61-L86), [MasteryService](../Assets/Scripts/Meta/MasteryService.cs), [CompletionService](../Assets/Scripts/Meta/CompletionService.cs)
- 🟡 Several counters are approximated/placeholder logic rather than fully event-accurate game telemetry

## 11) Relic / Permanent Upgrade System

- ❌ Full permanent upgrade economy/service not implemented
- 🟡 Relic IDs/state fields exist in models: [RuntimeModels](../Assets/Scripts/Core/RuntimeModels.cs#L30-L33)

## 12) Endless Zen Mode

- ❌ Full mode rules (no HP/gold/boss + infinite scaling loop) not implemented
- 🟡 Unlock flag and basic mode identifier exist: [RuntimeModels](../Assets/Scripts/Core/RuntimeModels.cs#L179-L180), [GameModeService](../Assets/Scripts/Run/GameModeService.cs#L19-L21)

## 13) Spirit Trials (Time Attack)

- ❌ Timer loop, fixed-seed challenge runner, time penalties, rank tiers, leaderboard integration not implemented
- 🟡 Mode flag/unlock state exists only

## 14) Emotional Game Feel

- 🟡 Combo, near-death, clear-mind and music-layer state logic implemented: [RunFeelService](../Assets/Scripts/Run/RunFeelService.cs)
- ❌ Visual/audio effect playback layer (red pulse, blossom animation, wooden SFX, desaturation, drum pulse) not implemented in render/audio controllers

## 15) Save Architecture

- ✅ Two-file save paths (profile/run) + versioned envelope + migration/backup: [SaveFileService](../Assets/Scripts/Save/SaveFileService.cs), [SaveMigrationService](../Assets/Scripts/Save/SaveMigrationService.cs)
- 🟡 Puzzle-save model includes board/pencil/combo/music fields: [HardeningModels](../Assets/Scripts/Core/HardeningModels.cs#L89-L101)
- ❌ Actual run save currently stores RunState only (not full board/pencil/modifier runtime snapshot): [RunAutoSaveCoordinator](../Assets/Scripts/Save/RunAutoSaveCoordinator.cs#L24-L33)
- 🟡 Auto-save trigger infrastructure exists but required trigger calls (pause/quit/boss transition) are not wired in gameplay controllers: [RunDirector](../Assets/Scripts/Run/RunDirector.cs#L29), [RunDirector](../Assets/Scripts/Run/RunDirector.cs#L471-L474)

## 16) Steam Achievements

- ❌ Steamworks achievement integration absent
- 🟡 Internal mastery/completion scaffolding can feed future achievements

## 17) Deterministic Modifier Injection Order

- 🟡 Constraint engine supports ordered rule list injection: [SudokuConstraintEngine](../Assets/Scripts/Sudoku/SudokuConstraintEngine.cs#L12-L19)
- ❌ No explicit enforcement utility of the required order (base → region → line → dot → arithmetic → fog)

## Summary

- ✅ Implemented foundations are strong: deterministic generation scaffold, run core, boss scaffold, tutorial isolation, class unlocks, heat model, save versioning.
- 🟡 Many systems are framework-complete but not fully gameplay-wired.
- ❌ Major production features still missing: node-map gameplay structure, full shop/relic economy loop, full Endless/Trials modes, Steam achievements, and full-board autosave/resume.

Overall implementation status vs previous prompt: **~55% (core systems present, content/scene integration incomplete).**
