# Complete Rebuild Plan: Run of the Nine

## Context

The current codebase (105 C# files) has accumulated too many NullReferenceExceptions, broken auto-generated files, architectural inconsistencies, and initialization issues. Rather than continue patching, the user wants to archive the entire `Assets/` directory as `assets_backup/` and rebuild everything from scratch using the 15 specification documents in `docs/respec/` as the single source of truth. The plan will also be saved to `docs/respec/plan/`.

**Project:** Unity 6 (6000.x), C#, single-scene architecture, procedural UI (no prefabs), JsonUtility serialization (no Newtonsoft).

---

## Phase 0: Archive and Scaffold

**Goal:** Archive current code, create fresh folder structure + assembly definitions + empty scene.

1. Rename `Assets/` → `assets_backup/`
2. Create new `Assets/` structure:
```
Assets/
  Scenes/
    Game.unity                    (single scene, one empty "GameBootstrap" GameObject)
  Resources/
    GeneratedIcons/               (copy from backup)
    Icons/                        (copy from backup)
  Scripts/
    Game.asmdef                   (references: Unity.InputSystem)
    Bootstrap/
    Core/
    Sudoku/
    Boss/
    Classes/
    Economy/
    Items/
    Route/
    Run/
    Save/
    Meta/
    Tutorial/
    UI/
    Data/
    Editor/
      Game.Editor.asmdef
    Tests/
      EditMode/
        GameTests.EditMode.asmdef
      PlayMode/
        GameTests.PlayMode.asmdef
```
3. Copy `Game.asmdef` and test assembly definitions from backup
4. Configure EditorBuildSettings to include only `Game.unity`

**Compilable:** Yes (empty project)

---

## Phase 1: Core Data Layer (Enums, Models, Board)

**Goal:** All enums, serializable data models, and the SudokuBoard class. Pure C#, zero Unity dependencies except `[Serializable]`.

| File | Responsibility |
|------|---------------|
| `Core/GameEnums.cs` | All enums: `ClassId`(8), `GameMode`(4), `BossModifierId`(15), `BossModifierIntensity`(4), `ItemType`(18), `ItemRarity`(3), `RelicId`(23), `RelicTier`(5), `NodeType`(10), `RouteType`(5), `ConstraintRuleCategory`(8), `MenuScreen`, `SpiritTrialsTier`(4), `DifficultyTier`(5), `LineType`(7), `MarkerType`(2), `TutorialResourceMode`(2) |
| `Core/RuntimeModels.cs` | `RunState`, `LevelConfig`, `LevelState`, `RunNode`, `TileXpEntry`, `ItemInstance`, `RelicInstance`, `RunEvent`, `ShopOffer`, `TutorialSetupConfig` |
| `Core/SaveModels.cs` | `SaveFileEnvelope`, `PlayerProfile`, `MetaProgressionState`, `ClassGardenProgressEntry`, `ClassUnlockProgress`, `ProfileStats`, `MasteryAchievementState`, `CompletionTrackerState`, `TutorialProgressState`, `OptionsState`, `AudioSettingsModel`, `GraphicsSettingsModel`, `AccessibilitySettings`, `GameplaySettingsModel`, `PuzzleSaveState`, `ItemCodexEntry` |
| `Core/HardeningModels.cs` | Validation constants, clamp ranges, default values |
| `Core/StarDensityService.cs` | `MissingPercentForStars(stars)` = `(stars+3)*0.1` clamped 1-6 |
| `Sudoku/SudokuBoard.cs` | Size, Solution[,], Cells[,], GivenMask, RegionMap, pencil marks, IsComplete(), IsCorrectAt() |
| `Sudoku/ModifierModels.cs` | `ModifierOverlayData`, `ModifierLine`, `ArrowConstraint`, `KillerCage`, `KropkiDot`, `CellMarker` |
| `Sudoku/IOrderedConstraintRule.cs` | Interface for constraint validation |
| `Classes/ClassCatalog.cs` | Static catalog: `GetDefinition(ClassId)` → base HP, pencil, slots, passive |
| `Economy/XpTable.cs` | Base XP per board (5×5=30..9×9=120), star multipliers, level curve (L1-40), `DeriveLevel(totalXp)` |
| `Economy/GoldTable.cs` | Base gold per board size, star/modifier multipliers |
| `Data/FloorThemeData.cs` | Floor→theme mapping, board size ranges, palette data |

**Key rules:** All `[Serializable]` classes use JsonUtility-compatible types only. Nullable pattern: `bool Has + T Value`. HashSet sync: `[NonSerialized] HashSet` + serializable `List` + `Sync()`.

**Tests:** `EnumTests.cs`, `XpCurveTests.cs`, `StarDensityTests.cs`

---

## Phase 2: Sudoku Engine

**Goal:** Complete puzzle generation pipeline + all 15 constraint rules. Pure C#.

| File | Responsibility |
|------|---------------|
| `Sudoku/SudokuGenerator.cs` | `CreatePuzzle(size, missingPercent, seed, variant)`, `BuildRegionMap(size, variant)`, `GenerateSolvedBoard()`, all jigsaw templates (5×5–9×9 A/B) |
| `Sudoku/SudokuValidator.cs` | Row/col/region uniqueness validation |
| `Sudoku/SudokuConstraintEngine.cs` | Rule registry, ordered validation |
| `Sudoku/ConstraintRules.cs` | 15 rules: GermanWhispers, DutchWhispers, ParityLines, Renban, Palindrome, Thermo, BetweenLines, DifferenceKropki, RatioKropki, KillerCages, ArrowSums, EvenOdd, Nonconsecutive, Antiknight, FogOfWar |
| `Sudoku/ModifierFactory.cs` | `CreateRule(BossModifierId)` → rule instance |
| `Sudoku/ModifierGeometryGenerator.cs` | `Generate(solvedBoard, modifiers, seed, intensity)` → overlay data |
| `Sudoku/SudokuBacktrackingSolver.cs` | Uniqueness checking |
| `Sudoku/SudokuDifficultyGrader.cs` | Technique-based grading (basic) |
| `Sudoku/SudokuLogicalAnalyzer.cs` | Logical solve path analysis (basic) |

**Tests:** `SudokuGeneratorTests.cs`, `ConstraintRuleTests.cs`, `ModifierGeometryTests.cs`, `CellRemovalTests.cs`

---

## Phase 3: Run Logic and Services (No UI)

**Goal:** RunDirector state machine + all run/economy/save/meta services. Fully testable without UI.

**Run orchestration (7 files):** `RunDirector.cs`, `RunGraphService.cs`, `RunEventService.cs`, `RunVarianceService.cs`, `MidRunAdaptationService.cs`, `EndlessZenService.cs`, `SpiritTrialsService.cs`

**Additional run (5 files):** `RunArchetypeService.cs`, `CurseService.cs`, `PostRunAnalyticsService.cs`, `RunFeelService.cs`, `GameModeService.cs`

**Economy (4 files):** `FormulaService.cs`, `XpService.cs`, `ShopService.cs`, `RelicService.cs`

**Items (1 file):** `ItemService.cs`

**Boss (1 file):** `BossService.cs`

**Classes (2 files):** `ClassSelectService.cs`, `ClassUnlockService.cs`

**Route (1 file):** `RouteService.cs`

**Save (7 files):** `SaveFileService.cs`, `ProfileService.cs`, `RunAutoSaveCoordinator.cs`, `RunResumeService.cs`, `SaveConflictService.cs`, `SaveMigrationService.cs`, `SteamCloudSyncService.cs`

**Tutorial (2 files):** `TutorialModeService.cs`, `TutorialProgressService.cs`

**Meta (5 files):** `ClassGardenProgressionService.cs`, `AscensionService.cs`, `CompletionService.cs`, `MasteryService.cs`, `SteamAchievementService.cs`

**Tests:** `RunDirectorTests.cs`, `GoldEconomyTests.cs`, `ItemServiceTests.cs`, `RelicServiceTests.cs`, `SaveLoadTests.cs`, `PathGraphTests.cs`

---

## Phase 4: Scene Infrastructure and Menu UI

**Goal:** GameBootstrap, ScreenManager, all menu screens procedurally built. Navigate menus, select class, configure tutorial.

| File | Responsibility |
|------|---------------|
| `Bootstrap/GameBootstrap.cs` | Entry point. `Awake()` initializes services (NOT field initializers). Creates Camera, EventSystem, ScreenManager at runtime. Launch methods for all modes. |
| `UI/ScreenManager.cs` | 3 panel groups (menu/game/endScreen), `Show(AppScreen)` |
| `UI/MenuFlowService.cs` | Menu screen stack navigation |
| `UI/MenuPanelAnimator.cs` | Panel transitions |
| `UI/MainMenuBlueprintBuilder.cs` | Procedural menu hierarchy: MainMenu, ClassSelect, Tutorial, Modes, Codex, Meta, Options, Credits panels |
| `UI/MainMenuController.cs` | Button callbacks, panel transitions, data binding. **Services initialized in Awake(), NOT field initializers.** |
| `UI/MainMenuRuntimeAutoWire.cs` | Auto-wire button references at runtime |
| `UI/TutorialMenuController.cs` | Tutorial config (size/stars/region/15 toggles) |
| `UI/GameModesPanelController.cs` | Spirit Trials + Endless Zen panels |
| `UI/OptionsController.cs` | Audio/Graphics/Accessibility/Controls panels |
| `UI/MainMenuAtmosphereController.cs` | Background ambience |
| `UI/MenuMusicController.cs` | Menu music |
| `Core/AccessibilityService.cs` | Accessibility settings application |
| `Core/InputRemapService.cs` | Input rebinding |
| `Core/LaunchRequestContext.cs` | Run launch parameters |
| `UI/ItemsMenuController.cs` | Codex panel |

**CRITICAL: All MonoBehaviour service fields initialized in `Awake()`, never via `= new()` field initializers. Unity's `AddComponent<>()` can skip C# constructors.**

---

## Phase 5: In-Run UI and Core Gameplay Loop

**Goal:** Board rendering, number input, modifier overlays, HUD, rewards, shop, path map, boss choice, pause, end screen. Complete Garden Run playable.

| File | Responsibility |
|------|---------------|
| `UI/InRunUiBlueprintBuilder.cs` | Board grid, HUD, numpad, action bar, overlays |
| `UI/InRunUiFlowController.cs` | Board→rewards→shop→map transitions |
| `UI/PrototypeRunScreenController.cs` | Board rendering, input, overlays, HUD. **Keep modular — avoid 4700+ line monolith.** |
| `UI/PrototypeInputController.cs` | Keyboard/mouse input routing |
| `UI/RunMapController.cs` | Path map: node cards, edges, reachability |
| `UI/PlayerIconController.cs` | Player icon on map |
| `UI/PathNodeIconFactory.cs` | Node type icons |
| `UI/ShopController.cs` | Shop panel |
| `UI/PauseRunController.cs` | Pause menu |
| `UI/PauseMenuService.cs` | Pause state |
| `UI/EndScreenPresenter.cs` | End-of-run: XP breakdown, level-up, stats |
| `UI/ProceduralSpriteHelper.cs` | Circle/white sprite generation |
| `UI/RunAudioController.cs` | In-run music + SFX routing |
| `UI/ProceduralSfxLibrary.cs` | 20+ procedural sound effects |
| `UI/FloorMusicGenerator.cs` | Floor theme music |
| `UI/AmbientParticleController.cs` | Floor-themed particles |
| `UI/AnimationHelper.cs` | Easing, timing constants, coroutine helpers |

**Tests:** `RunFlowIntegrationTests.cs`, `EconomyIntegrationTests.cs`, `SaveLoadIntegrationTests.cs`

---

## Phase 6: Alternative Game Modes and Meta Progression

**Goal:** Tutorial, Endless Zen, Spirit Trials fully playable. Meta progression panel complete.

| File | Responsibility |
|------|---------------|
| `UI/TutorialRunBannerController.cs` | Tutorial mode banner |
| `UI/MetaProgressionPanelController.cs` | Class levels, prestige, completion, achievements |
| `UI/SudokuBoardPreviewController.cs` | Board preview for Spirit Trials |
| `UI/EventChoiceScreenController.cs` | Run event display |
| `UI/CursePanelController.cs` | Curse display (stub) |
| `UI/KeyboardNavigationController.cs` | Focus management for keyboard nav |

**Tests:** `EndlessZenTests.cs`, `SpiritTrialsTests.cs`

---

## Phase 7: Polish, Accessibility, and Editor Tools

**Goal:** Accessibility features, debug tools, editor tooling, Steam stubs.

| File | Responsibility |
|------|---------------|
| `UI/DropdownAutoSizeController.cs` | Auto-size dropdowns |
| `UI/PrototypeUiDebugHotkeys.cs` | Debug hotkeys |
| `UI/PrototypeRunMapBootstrap.cs` | Run map init helper |
| `UI/HeatCurveGraphController.cs` | Stub (compatibility) |
| `UI/FloorThemeIconGenerator.cs` | Floor theme icons |
| `Data/DefinitionAssets.cs` | ScriptableObject wrapper |
| `Editor/PixelIconSetGenerator.cs` | Icon generation pipeline |
| `Save/ICloudSaveProvider.cs` + `Save/LocalCloudSaveProvider.cs` | Cloud save interface + local fallback |

**Tests:** `AccessibilityTests.cs`

---

## Dependency Graph

```
Phase 0 (Scaffold)
    ↓
Phase 1 (Data Layer)      ← pure C#, no Unity deps
    ↓
Phase 2 (Sudoku Engine)   ← depends on Phase 1
    ↓
Phase 3 (Services)        ← depends on Phases 1+2
    ↓
Phase 4 (Menu UI)         ← depends on Phase 3
    ↓
Phase 5 (Game UI)         ← depends on Phases 3+4
    ↓
Phase 6 (Game Modes)      ← depends on Phases 3+5
    ↓
Phase 7 (Polish)          ← depends on all above
```

---

## Totals

| Phase | Files | Cumulative | Tests |
|-------|:-----:|:----------:|:-----:|
| 0 Scaffold | 5 asmdefs + scene | 6 | 0 |
| 1 Data | 12 | 18 | 3 |
| 2 Sudoku | 9 | 27 | 4 |
| 3 Services | 28 | 55 | 6 |
| 4 Menu UI | 16 | 71 | 1 |
| 5 Game UI | 17 | 88 | 3 |
| 6 Modes | 6 | 94 | 2 |
| 7 Polish | 9 | 103 | 1 |
| **Total** | **~103** | — | **20** |

---

## Key Architecture Rules

1. **Namespace:** `SudokuRoguelike.{FolderName}`
2. **Serialization:** `JsonUtility` only. All save data `[Serializable]`.
3. **Services:** Plain C# (no MonoBehaviour) unless they need Unity lifecycle.
4. **MonoBehaviour fields:** Initialize in `Awake()`, NEVER via `= new()` field initializers.
5. **No prefabs:** All UI procedural via `new GameObject()` + `AddComponent<T>()`.
6. **Single scene:** ScreenManager toggles panel groups. No `SceneManager.LoadScene()`.
7. **Seeded determinism:** Every `System.Random` derives from run seed + offset.
8. **Data flow:** Services never reference UI. Data flows up via return values.
9. **GameBootstrap:** Only entry point. Owns profile, creates RunDirector on launch.

---

## Verification

After each phase:
1. Build project in Unity — zero compilation errors
2. Run EditMode/PlayMode tests — all pass
3. Phase 4+: Launch game, navigate menus
4. Phase 5+: Complete a full Garden Run start to finish
5. Phase 6+: Play all 4 game modes
6. Phase 7: Toggle all accessibility options, verify visual changes
