# Run of the Nine â€” Unity Specification Pack

This repository contains a complete design and implementation blueprint for **Run of the Nine**, a Sudoku roguelike set in a pixel-art Japanese garden.

## Included Documents

- [Game Design Spec](docs/GameDesignSpec.md)
- [Tutorial Mode System](docs/TutorialModeSystem.md)
- [Class Progression Unlocks](docs/ClassProgressionUnlocks.md)
- [Class XP Progression System](docs/ClassXpProgressionSystem.md)
- [Items and Relics System](docs/ItemsAndRelicsSystem.md)
- [Unity Implementation Spec](docs/UnityImplementationSpec.md)
- [Accessibility Spec](docs/AccessibilitySpec.md)
- [Save/Load Architecture](docs/SaveLoadArchitecture.md)
- [Development Workflow](docs/development.md)
- [CI Validation](docs/ci-validation.md)
- [Audio/Visual Direction](docs/AudioVisualDirection.md)
- [Localization Catalog Workflow](docs/localization-catalog.md)

## Project Goal

Build a Unity game that combines:

- Classic Sudoku solving
- Roguelike run progression
- RPG class and leveling systems
- Gold economy + item strategy
- Branching garden routes
- End boss Sudoku modifiers

## Recommended Build Sequence (MVP â†’ Full)

1. Core Sudoku board generation + validation (5x5 to 9x9)
2. HP, mistakes, pencil unit resource system
3. Run flow (level complete, rewards, next level)
4. Item roll/pick/reroll flow
5. Class selection + level progression
6. Branching route nodes
7. Boss modifiers + final 3-phase boss
8. Meta progression (Relics), Endless Zen, Spirit Trials

## Unity Version Target

Use **Unity 6 LTS** (or latest Unity LTS available in Unity Hub).

## Immediate Next Step

Use [Development Workflow](docs/development.md) as the supported local validation guide. Unity is the authoritative build/test runner. Use `dotnet build sudoku_rouge_like.slnx --verbosity minimal` only as the direct static compile smoke. The retired root `sudoku_rouge_like.sln` file must remain absent and must not be used as a release gate.

## Implemented Now

The repository now includes a Unity-ready gameplay systems scaffold under `Assets/Scripts`:

- Sudoku board generation and validation for 5x5 to 9x9
- Run state, level state, and economy formulas (gold, XP, costs)
- Class stat presets and level-up rewards
- Item roll/reroll framework with guarantee rules
- Route choice and route effect application
- Boss modifier pool and 3-phase final boss structure
- Difficulty HeatScore model with spike guardrails
- Menu flow state for Start/Resume/Meta/Modes/Options/Credits/Pause/End/Victory
- Meta progression/profile/options runtime models
- Bootstrap MonoBehaviour for quick prototype startup

See [Development Workflow](docs/development.md) to run local validation and Unity batchmode tests.

## Current Art Asset Status

- Generated icon assets are included under `Assets/Resources/GeneratedIcons`.
- Runtime UI icon loading now resolves through `Resources.Load("GeneratedIcons/<icon_name>")` from canonical `Assets` paths.
- Art direction, audio strategy, and generated-asset caveats are documented in:
	- `docs/AudioVisualDirection.md`
	- `docs/audio-asset-pipeline.md`
	- `docs/font-asset-pipeline.md`

## Script Tree Consolidation

- **Source of truth:** `Assets/Scripts`
- The former duplicate script tree is not part of the active Unity import path.

### Build and Test Guardrails

- Unity is the authoritative build and test runner.
- Use `docs/development.md` for the supported workflow.
- The retired root `sudoku_rouge_like.sln` must remain absent; generated `.csproj` files are IDE/compile aids, not the release gate.
- `.github/workflows/static-validation.yml` provides lightweight repository checks.
- CI static checks are guardrails only; Unity batchmode results remain the authoritative release gate.

Run EditMode tests through Unity batchmode:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform EditMode
```

Run EditMode and PlayMode tests:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform All
```

## Dev Tools (`tools/`)

One-off maintenance and pipeline scripts. Run from the repository root with `.\tools\<script>`.

| Script | Purpose |
|--------|---------|
| `run-unity-tests.ps1` | Run Unity Test Runner in batchmode for EditMode, PlayMode, or both |
| `agent.ps1` | LangGraph AI pipeline shortcut / interactive REPL |
| `_lint_assets.ps1` | Audit PNG naming, prefix conventions, missing .meta, dangling Resources.Load paths |
| `_fix_filter_mode.py` | Set filterModeâ†’Point on all texture .meta files |
| `_fix_bg_meta.ps1` | Generate Unity .meta files for background sprites |
| `_fix_csv.ps1` | Rebuild the GeneratedIcons icon-map CSV |
| `_fix_paths.ps1` | Patch legacy GeneratedIcons/ string literals in MainMenuBlueprintBuilder |
| `_fix_node_icons.ps1` | Copy node icon placeholders |
| `_icon_cleanup.ps1` | Move/delete deprecated generated icon files |
| `_rename_bg.ps1` / `_rename_bg.py` | Batch-rename background assets to snake_case convention |
| `_create_modifier_icons.py` | Generate placeholder PNGs + .meta for modifier icons |
| `_create_placeholder_bgs.py` | Generate placeholder PNGs for missing background/UI slots |
| `_cleanup_folders.py` | Remove empty Resources sub-folders |
| `_probe.ps1` | Probe which icon files are present/missing under Resources |
