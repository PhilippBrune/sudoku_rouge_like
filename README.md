# Run of the Nine — Unity Specification Pack

This repository contains a complete design and implementation blueprint for **Run of the Nine**, a Sudoku roguelike set in a pixel-art Japanese garden.

## Included Documents

- [Game Design Spec](docs/GameDesignSpec.md)
- [Production GDD](docs/ProductionGDD.md)
- [Complete Production Blueprint](docs/CompleteProductionBlueprint.md)
- [Design Bible (Chapters 1–17)](docs/DesignBible_Chapters_1-17.md)
- [Tutorial Mode System](docs/TutorialModeSystem.md)
- [Class Progression System](docs/ClassProgressionSystem.md)
- [Items Collection System](docs/ItemsCollectionSystem.md)
- [Core Systems Hardening](docs/CoreSystemsHardening.md)
- [Implementation Audit (Previous Prompt)](docs/ImplementationAudit_PreviousPrompt.md)
- [Implementation Progress (All Prompts)](docs/ImplementationProgress_AllPrompts.md)
- [Unity Architecture Blueprint](docs/UnityArchitecture.md)
- [Unity Install Guide (Windows)](docs/UnityInstall_Windows.md)
- [Unity Prototype Setup](docs/UnityPrototypeSetup.md)
- [Icon Prompt Pack](docs/run_of_the_nine_icon_prompts.md)
- [UI Art Pipeline](docs/run_of_the_nine_ui_art_pipeline.md)
- [Icon Creation Pipeline](docs/IconCreation_PixelGenerator.md)

## Project Goal

Build a Unity game that combines:

- Classic Sudoku solving
- Roguelike run progression
- RPG class and leveling systems
- Gold economy + item strategy
- Branching garden routes
- End boss Sudoku modifiers

## Recommended Build Sequence (MVP → Full)

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

Follow [Unity Install Guide (Windows)](docs/UnityInstall_Windows.md), create project, then implement Milestone 1 from [Unity Architecture Blueprint](docs/UnityArchitecture.md).

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

See [Unity Prototype Setup](docs/UnityPrototypeSetup.md) to run the current prototype immediately.

## Current Art Asset Status

- Generated icon assets are included under `Assets/Resources/GeneratedIcons`.
- Runtime UI icon loading now resolves through `Resources.Load("GeneratedIcons/<icon_name>")` from canonical `Assets` paths.
- Prompt/pipeline references for future art upgrades are documented in:
	- `docs/run_of_the_nine_icon_prompts.md`
	- `docs/run_of_the_nine_ui_art_pipeline.md`

## Script Tree Consolidation

- **Source of truth:** `Assets/Scripts`
- The former duplicate script tree has been retired from Unity import paths and moved to:
	- `retired/nested-script-tree/Scripts`

### Guardrails

- CI checks that the nested tree stays retired:
	- `.github/workflows/script-tree-guard.yml`
- CI also blocks any newly committed `retired/**/*.cs` files.
- A repo-managed pre-commit hook is provided:
	- `.githooks/pre-commit`

Enable the hook locally once per clone:

```bash
git config core.hooksPath .githooks
```

Manual check command:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
./tools/check-script-tree-drift.ps1 -ExpectRetired
```