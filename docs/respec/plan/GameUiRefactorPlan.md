# Game UI Refactor Plan

**Version:** 1.0 | **Date:** 2026-04-07 | **Status:** plan — awaiting approval before implementation

---

## Goal

Move the game's UI from a large prototype-style codebase into a clean, maintainable, production-ready structure — without changing any gameplay, visual output, or mechanic. No player-facing regressions.

---

## Current State

| File | Lines | Problem |
|------|------:|---------|
| `PrototypeRunScreenController.cs` | ~4 700 | Single class owns board rendering, HUD, overlays, rewards, shop, rest, relic, cursed node, boss gate, game over — too many responsibilities |
| `InRunUiBlueprintBuilder.cs` | ~700 | Builds the UI hierarchy and then configures `PrototypeRunScreenController` — tightly coupled |
| `HeatCurveGraphController.cs` | ~10 | Empty stub, never referenced in game code |
| `PrototypeUiDebugHotkeys.cs` | ~120 | Debug-only; should not be in production scene |
| `PrototypeRunMapBootstrap.cs` | ~80 | Thin bootstrap helper; duplicates responsibility with `GameBootstrap` |
| `MenuPanelAnimator.cs` | Deleted | Already removed |
| `MainMenuRuntimeAutoWire.cs` | Deleted | Already removed |

---

## Problems to Fix

### 1. `PrototypeRunScreenController` is too large
A single MonoBehaviour of ~4 700 lines owns too much. It is:
- The board renderer (cells, overlays, pencil marks)
- The HUD updater (HP bar, gold, pencil, combo, passive label)
- The numpad (buttons, pencil toggle, mode button)
- The reward screen (item slots, reroll, nothing)
- The shop panel (offers, buy, emergency heal)
- The rest choice panel
- The relic choice panel
- The boss gate modifier picker
- The cursed node Accept/Decline panel
- The game over / victory screen
- The path overview (calls `RunMapController`)

This is hard to read, hard to test, and every new feature adds more lines.

### 2. "Prototype" prefix in class names
Class names like `PrototypeRunScreenController`, `PrototypeInputController`, `PrototypeUiDebugHotkeys`, `PrototypeRunMapBootstrap` signal unfinished work. The game has a name: **Run of the Nine**.

### 3. Color scheme inconsistency
- Main menu uses `BgColor = (0.06, 0.09, 0.08)` (very dark green)
- In-run uses `BgColor = (0.07, 0.11, 0.14)` (dark blue-grey)
- These have drifted independently. A shared `GamePalette` would keep them aligned.

### 4. Dead / stub files
- `HeatCurveGraphController.cs` — empty namespace, referenced by nothing
- `PrototypeUiDebugHotkeys.cs` — useful during dev but should not exist in a production build path

---

## Proposed Changes

### Phase A — Delete Dead Code
_Simple deletions. No risk._

| Action | File |
|--------|------|
| Delete | `Assets/Scripts/UI/HeatCurveGraphController.cs` + `.meta` |
| Move to Editor-only | `Assets/Scripts/UI/PrototypeUiDebugHotkeys.cs` → `Assets/Scripts/Editor/DebugHotkeys.cs` — wrap entire class in `#if UNITY_EDITOR` |
| Merge | `Assets/Scripts/UI/PrototypeRunMapBootstrap.cs` → fold its `Start()` logic into `InRunUiFlowController.Configure()` and delete the file |

---

### Phase B — Rename "Prototype" Classes
_Mechanical rename only. No logic changes._

| Old name | New name | File |
|----------|----------|------|
| `PrototypeRunScreenController` | `InRunController` | `InRunController.cs` |
| `PrototypeInputController` | `InRunInputController` | `InRunInputController.cs` |
| `PrototypeUiDebugHotkeys` | `DebugHotkeys` | (moved to Editor in Phase A) |
| `PrototypeRunMapBootstrap` | (deleted in Phase A) | — |

Update all references: `GameBootstrap.cs`, `InRunUiBlueprintBuilder.cs`, `InRunUiFlowController.cs`.

---

### Phase C — Split `InRunController` into Focused Components
_This is the main refactor. Extract responsibility groups into separate MonoBehaviours. `InRunController` becomes a thin orchestrator._

#### New files to create

| New Class | Responsibility | Extracted From |
|-----------|---------------|----------------|
| `BoardViewController` | Cell grid rendering, cell color updates (`RefreshBoardColors`), region borders, fog rendering, antiknight highlights | `InRunController` |
| `OverlayViewController` | All overlay objects (lines, dots, cages, arrows, kropki, cell markers) — `BuildOverlays`, `DrawOverlayX` methods | `InRunController` |
| `HudViewController` | HP bar, pencil bar, gold text, combo counter, passive label, floor modifier banner — `RefreshHud`, `EnsureComboCounter`, `EnsurePassiveLabel` | `InRunController` |
| `NumpadViewController` | Numpad buttons, pencil toggle, mode button, number selection state — `BuildNumpad`, `OnNumpadPressed` | `InRunController` |
| `RewardViewController` | Item reward panel, reroll, nothing slot, relic choice panel, rest choice panel, bag-swap panel — `ShowRewardScreen`, `ShowRelicChoicePanel`, `ShowRestChoicePanel` | `InRunController` |
| `ShopViewController` | Shop panel, offer display, buy logic — `ShowShop`, `RenderShopOffer` | `InRunController` |
| `BossGateViewController` | Boss modifier picker, cursed node Accept/Decline panel — `ShowBossGate`, `HandleCursedNode` | `InRunController` |
| `EndScreenViewController` | Game over and victory screen — `ShowGameOver`, `BuildRunScoreBreakdown` | `InRunController` (+ `EndScreenPresenter`) |

#### `InRunController` after split (~400 lines)
- Owns references to all sub-controllers
- Handles screen transitions: `ShowPath()`, `ShowSudoku()`, `ShowReward()`, `ShowShop()`, `ShowGameOver()`
- Delegates all rendering to the appropriate sub-controller
- Receives events from `RunDirector` and routes them

#### Communication pattern
- Sub-controllers are configured once via `Configure()` in `InRunController.Configure()`
- They call back via `Action` delegates (e.g. `OnRewardChosen`, `OnShopClosed`) — no direct service references in sub-controllers
- Data passed as plain values (not RunState references) to keep sub-controllers decoupled from game logic

---

### Phase D — Introduce `GamePalette`
_Central color constants, referenced by all UI builders._

```csharp
// Assets/Scripts/UI/GamePalette.cs
public static class GamePalette
{
    // Shared
    public static readonly Color Background    = new(0.06f, 0.09f, 0.10f, 1f);
    public static readonly Color Panel         = new(0.10f, 0.14f, 0.12f, 0.90f);
    public static readonly Color Button        = new(0.82f, 0.75f, 0.62f, 0.92f);
    public static readonly Color ButtonText    = new(0.12f, 0.10f, 0.08f, 1f);
    public static readonly Color AccentGold    = new(0.98f, 0.83f, 0.26f, 1f);
    public static readonly Color TextPrimary   = new(0.96f, 0.93f, 0.82f, 1f);
    public static readonly Color TextMuted     = new(0.60f, 0.57f, 0.50f, 1f);
    public static readonly Color HpRed         = new(0.75f, 0.22f, 0.22f, 1f);
    public static readonly Color PencilBlue    = new(0.22f, 0.55f, 0.75f, 1f);
    public static readonly Color DangerRed     = new(0.72f, 0.18f, 0.18f, 1f);
    public static readonly Color SuccessGreen  = new(0.30f, 0.72f, 0.40f, 1f);

    // Board-specific
    public static readonly Color CellEmpty     = new(0.12f, 0.18f, 0.14f, 1f);
    public static readonly Color CellGiven     = new(0.20f, 0.29f, 0.20f, 1f);
    public static readonly Color CellSelected  = new(0.73f, 0.49f, 0.18f, 1f);
    public static readonly Color CellRowCol    = new(0.18f, 0.34f, 0.56f, 1f);
    public static readonly Color CellMatch     = new(0.36f, 0.24f, 0.58f, 1f);
    public static readonly Color CellConflict  = new(0.72f, 0.18f, 0.18f, 1f);
    public static readonly Color CellFog       = new(0.06f, 0.06f, 0.08f, 1f);
    public static readonly Color RegionBorder  = new(1f,   0.78f, 0.24f, 0.72f);
    public static readonly Color KillerBorder  = new(0.85f, 0.15f, 0.12f, 0.90f);
}
```

Replace all inline `new Color(...)` literals in `MainMenuBlueprintBuilder`, `InRunUiBlueprintBuilder`, and `InRunController` sub-controllers with `GamePalette.*` references.

---

### Phase E — `InRunUiBlueprintBuilder` Simplification
_The builder currently creates the UI hierarchy AND configures the screen controller. After Phase C, it should only build the hierarchy._

- Remove all calls to `runScreen.Configure(...)` — that becomes the responsibility of `InRunController.Awake()`
- The builder's only job: `new GameObject(...)`, `AddComponent<T>()`, `SetRect(...)` — pure structural setup
- After simplification the builder should be ~350 lines (down from ~700)

---

## Files Summary

### Delete
- `Assets/Scripts/UI/HeatCurveGraphController.cs` + `.meta`
- `Assets/Scripts/UI/PrototypeRunMapBootstrap.cs` + `.meta`

### Move
- `Assets/Scripts/UI/PrototypeUiDebugHotkeys.cs` → `Assets/Scripts/Editor/DebugHotkeys.cs`

### Rename
- `PrototypeRunScreenController.cs` → `InRunController.cs`
- `PrototypeInputController.cs` → `InRunInputController.cs`

### New
- `Assets/Scripts/UI/GamePalette.cs`
- `Assets/Scripts/UI/BoardViewController.cs`
- `Assets/Scripts/UI/OverlayViewController.cs`
- `Assets/Scripts/UI/HudViewController.cs`
- `Assets/Scripts/UI/NumpadViewController.cs`
- `Assets/Scripts/UI/RewardViewController.cs`
- `Assets/Scripts/UI/ShopViewController.cs`
- `Assets/Scripts/UI/BossGateViewController.cs`
- `Assets/Scripts/UI/EndScreenViewController.cs`

### Modify
- `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs` — remove controller configuration, keep hierarchy build
- `Assets/Scripts/Bootstrap/GameBootstrap.cs` — update class name references
- `Assets/Scripts/UI/InRunUiFlowController.cs` — update references, absorb `PrototypeRunMapBootstrap` logic

---

## Implementation Order

Each phase is independently releasable and compilable.

| Phase | Risk | Effort | Prerequisite |
|-------|:----:|:------:|-------------|
| A — Delete dead code | None | 1h | None |
| B — Rename classes | Very low | 2h | A |
| D — GamePalette | None | 2h | None (can run in parallel with A/B) |
| C — Split controller | Medium | 2–3 days | B |
| E — Simplify builder | Low | 3h | C |

**Recommended approach:** Do A, B, and D first (low risk, immediate improvement). Then C and E together in a focused session.

---

## Verification Checklist

After each phase:
- [ ] Project compiles with zero errors in Unity
- [ ] Main menu loads and all panels open correctly
- [ ] A full Garden Run can be completed (start → boss → end screen)
- [ ] Tutorial mode launches and completes
- [ ] Resume from save works
- [ ] All accessibility settings visually apply

---

## Out of Scope for This Refactor

- Visual redesign (colors, fonts, layout changes)
- New gameplay features
- Prefab migration (all UI remains procedural — no prefabs)
- Performance optimisation of the board renderer
