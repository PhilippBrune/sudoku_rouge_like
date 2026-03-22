# Markdown Requirements Audit (2026-03-05)

Scope:
- Reviewed project-owned markdown files in `README.md` and `docs/*.md`.
- Excluded third-party/package cache markdown under `Library/PackageCache`.

Legend:
- `Implemented`: Requirements are represented in current code.
- `Partial`: Some requirements are implemented, but notable gaps remain.
- `Spec/Guide`: Design/setup guidance only; not expected to map 1:1 to runtime code.

## File-by-File Coverage

| File | Status | Notes |
|---|---|---|
| `README.md` | Implemented | Repo navigation/setup links are valid. |
| `docs/ClassProgressionSystem.md` | Partial | Core progression + class data exist; full balancing/polish not fully verifiable in runtime UI. |
| `docs/CompleteProductionBlueprint.md` | Partial | Large production-scope blueprint exceeds current prototype implementation. |
| `docs/CoreSystemsHardening.md` | Partial | Many hardening components exist, but full end-to-end hardening scope is broader than current validation. |
| `docs/DesignBible_Chapters_1-17.md` | Partial | High-level chapters include many aspirational systems not fully implemented. |
| `docs/GameDesignSpec.md` | Partial | Core loop and systems are implemented in prototype; several production-level targets remain broader than code. |
| `docs/GameUiRedesign_PathAndSudoku.md` | Implemented | Recent UI path/sudoku/game-over changes are represented in code. |
| `docs/IconCreation_PixelGenerator.md` | Implemented | Generated icon pipeline output exists under `Assets/Resources/GeneratedIcons`. |
| `docs/ImplementationAudit_PreviousPrompt.md` | Spec/Guide | Historical audit file; intentionally contains outdated findings. |
| `docs/ImplementationProgress_AllPrompts.md` | Spec/Guide | Historical progress log; not a live source of truth. |
| `docs/InGameUIFlowSetup.md` | Implemented | Controllers/builders/wiring documented here exist in code. |
| `docs/ItemsCollectionSystem.md` | Partial | Item archive/codex UI exists; full collection depth and balancing remain iterative. |
| `docs/MainMenuSetup.md` | Implemented | Builder-driven menu setup and wiring are implemented. |
| `docs/MainMenuUX_DeepDive.md` | Partial | Main flows are implemented; some edge-case UX details may still evolve. |
| `docs/MainMenu_Theme_RunOfTheNine.md` | Implemented | Theme/icon direction now mapped to runtime builder and icon usage. |
| `docs/ProductionGDD.md` | Partial | Product-scale GDD remains broader than current prototype implementation. |
| `docs/RunVariation_EventCurse_System.md` | Partial | Run variance/events/curses exist; full tuning and all variants are still iterative. |
| `docs/TutorialModeSystem.md` | Implemented | Tutorial setup/progress/service flow exists in current code. |
| `docs/UnityArchitecture.md` | Spec/Guide | Architecture blueprint spans future scope beyond current runtime completeness. |
| `docs/UnityInstall_Windows.md` | Spec/Guide | Environment setup guide (non-runtime requirement). |
| `docs/UnityPrototypeSetup.md` | Spec/Guide | Project setup guide (non-runtime requirement). |

## Requested Requirement Checks

1. Star mechanic update:
- Implemented in code with shared mapping (`StarDensityService`):
  - 1★ = 40%
  - 2★ = 50%
  - 3★ = 60%
  - 4★ = 70%
  - 5★ = 80%

2. Main menu rework (`main_menue.png`):
- Implemented in menu blueprint layout/style and icon wiring.

3. Icon rework (`icons.png`):
- Implemented by remapping runtime menu/class/mode button icon assignments to generated icon resources derived from the sheet.

## Remaining Gaps (High Level)

- Several design docs define full production scope (economy depth, balancing, content scale, analytics, and platform-ready UX), while current codebase remains a robust prototype foundation.
- Historical audit/progress docs should not be treated as final truth; they are snapshots.
