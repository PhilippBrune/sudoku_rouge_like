# Unity Implementation Specification

**Version:** 1.3 | **Date:** 2026-03-22 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.3 | 2026-03-22 | implemented | All architecture verified implemented: single-scene, ScreenManager, GameBootstrap entry point, panel groups |
| 1.2 | 2026-03-22 | review | Single-scene architecture: replaced two-scene setup with ScreenManager panel groups, eliminated LaunchRequestContext static shuttle, GameBootstrap is now single entry point with direct launch methods |
| 1.1 | 2026-03-22 | review | Resolved OPEN-A (package cleanup), OPEN-B (dead code removal), OPEN-C (PlayMode tests added) |
| 1.0 | 2026-03-22 | new | Initial specification derived from all 14 requirement documents |

---

## Overview

This document defines how Run of the Nine shall be implemented in Unity. It consolidates architectural decisions, service responsibilities, scene structure, UI construction patterns, data flow, and integration points derived from all requirement specifications.

**Cross-references:** [GameDesignSpec](GameDesignSpec.md), [PathSystem](PathSystem_GardenOverview.md), [ClassXpProgression](ClassXpProgressionSystem.md), [ItemsAndRelics](ItemsAndRelicsSystem.md), [TutorialMode](TutorialModeSystem.md), [BossMechanics](BossMechanicsSystem.md), [GoldEconomy](GoldEconomySystem.md), [SaveLoad](SaveLoadArchitecture.md), [MetaProgression](MetaProgressionSystem.md), [SpiritTrials](SpiritTrialsMode.md), [EndlessZen](EndlessZenMode.md), [SudokuPipeline](SudokuGenerationPipeline.md), [AudioVisual](AudioVisualDirection.md), [Accessibility](AccessibilitySpec.md)

---

## 1) Project Configuration

### Unity Version & Render Pipeline

| Setting | Value |
|---------|-------|
| Unity Version | 6000.x (Unity 6 LTS) |
| Render Pipeline | Built-in 2D |
| Scripting Backend | IL2CPP (release), Mono (editor) |
| API Compatibility | .NET Standard 2.1 |
| Target Platforms | Windows, macOS, Linux (Steam) |
| Resolution | 1920×1080 reference, 16:9 aspect ratio, letterbox on other ratios |

### Required Packages

| Package | Version | Purpose |
|---------|---------|---------|
| com.unity.ugui | 2.0.0 | Canvas-based procedural UI |
| com.unity.inputsystem | 1.11.2 | Keyboard, mouse, controller input + remapping |
| com.unity.test-framework | 1.6.0 | EditMode + PlayMode unit tests |
| com.unity.2d.sprite | 1.0.0 | Sprite rendering for icons, backgrounds |
| com.unity.ide.rider | 3.0.39 | IDE integration |

### Packages NOT Used

| Package | Reason |
|---------|--------|
| com.unity.ads | Not needed — premium game, no ads |
| com.unity.purchasing | Not needed — no IAP |
| com.unity.ai.navigation | Not needed — no pathfinding AI |
| com.unity.multiplayer.center | Not needed — single-player only |
| com.unity.xr.legacyinputhelpers | Not needed — no VR support |
| com.unity.timeline | Not needed — no cutscenes or timeline animations |
| Newtonsoft JSON | Forbidden — use `JsonUtility` or manual serialization |

### Serialization

- All save data uses Unity `JsonUtility` (no Newtonsoft, no `System.Text.Json` at runtime)
- `JsonUtility` requires `[Serializable]` on all data classes
- Nullable types (`BossModifierId?`) are not supported by `JsonUtility` — use bool + value pattern (e.g., `HasChosenBossModifier` + `ChosenBossModifierId`)
- `HashSet<T>` is not serialized — use `List<T>` with `[NonSerialized]` HashSet + sync methods
- `Dictionary<K,V>` is not serialized — use parallel `List<K>` + `List<V>` or custom wrapper

---

## 2) Scene Structure

The game uses a **single scene** ("Game"). All UI is procedurally built in C# — no scene-specific prefabs or assets require separate scenes. Screen transitions are handled by `ScreenManager`, which shows/hides three top-level panel groups (Menu, Game, EndScreen).

### Scene: Game

#### Always-Active Components

| Component | Type | Role |
|-----------|------|------|
| GameBootstrap | MonoBehaviour | Single entry point — loads profile on `Start()`, shows menu, provides `LaunchRun()` / `LaunchTutorial()` / `LaunchResume()` / `ReturnToMenu()` / `ShowEndScreen()` methods |
| ScreenManager | MonoBehaviour | Manages three panel groups (`menuGroup`, `gameGroup`, `endScreenGroup`), fires `ScreenChanged` event |

#### Menu Panel Group (active when `AppScreen.Menu`)

| Component | Type | Role |
|-----------|------|------|
| MainMenuBlueprintBuilder | MonoBehaviour | Procedurally builds entire menu UI hierarchy via `[ContextMenu("Build")]` |
| MainMenuController | MonoBehaviour | Wires button callbacks, panel transitions, data binding; calls `GameBootstrap.LaunchRun()` / `LaunchTutorial()` / `LaunchResume()` directly |
| MainMenuRuntimeAutoWire | MonoBehaviour | Auto-wires references at runtime if builder hasn't run |
| MainMenuAtmosphereController | MonoBehaviour | Background ambience, particles, music for menu |
| MenuMusicController | MonoBehaviour | Menu music playback and crossfade |
| MenuPanelAnimator | MonoBehaviour | Panel slide/fade transitions |
| MenuFlowService | Plain C# | Manages menu screen stack (push/pop navigation) |

**Menu screens** (all built procedurally under a single Canvas):

| Screen | Entry Point | Content |
|--------|-------------|---------|
| MainMenu | App launch | Start Game, Resume Game, Tutorial, Game Modes, Items (Codex), Meta Progression, Options, Credits, Quit |
| ClassSelect | Start Game button | 8 class cards (locked/unlocked), level display, XP text, passive description, irregular puzzles toggle |
| TutorialSetup | Tutorial button | Board size dropdown, star slider, region dropdown, 15 modifier toggles, resource mode selector |
| TutorialProgress | Tab within Tutorial | 5×7 completion grid, modifier training list, aggregate % |
| GameModes | Game Modes button | Spirit Trials (4 tiers) + Endless Zen (unlock gate) |
| SpiritTrialsTierSelect | Spirit Trials | Tier buttons (Apprentice/Adept/Master/Grandmaster), item mode selector |
| Codex | Items button | Unified item + relic discovery tracker, undiscovered as "???" |
| MetaProgression | Meta button | Class levels, prestige timeline, completion tracker, achievement list |
| Options | Options button | Audio (4 channels), Graphics (resolution, fullscreen, particle intensity), Accessibility, Input Remapping |
| Credits | Credits button | Scrolling credit text |

#### Game Panel Group (active when `AppScreen.Game`)

| Component | Type | Role |
|-----------|------|------|
| InRunUiBlueprintBuilder | MonoBehaviour | Procedurally builds the in-run UI hierarchy via `[ContextMenu("Build")]` |
| PrototypeRunScreenController | MonoBehaviour | Main in-run UI controller (~4700 lines) — board rendering, input handling, overlays, HUD updates |
| PrototypeInputController | MonoBehaviour | Input routing (keyboard, mouse, controller) to game actions |
| RunMapController | MonoBehaviour | Path map rendering, tile interaction, player icon movement |
| PlayerIconController | MonoBehaviour | Class-based player icon on the path map |
| InRunUiFlowController | MonoBehaviour | Manages overlay transitions (board → rewards → shop → map) |
| RunAudioController | MonoBehaviour | In-run music layers, SFX playback |
| AmbientParticleController | MonoBehaviour | Floor-themed particle systems |
| ShopController | MonoBehaviour | Shop UI panel and purchase flow |
| PauseRunController | MonoBehaviour | Pause menu overlay with Save & Quit |
| KeyboardNavigationController | MonoBehaviour | Focus management for keyboard/controller navigation |
| TutorialRunBannerController | MonoBehaviour | Persistent "TUTORIAL MODE" banner during tutorial runs |

#### EndScreen Panel Group (active when `AppScreen.EndScreen`)

| Component | Type | Role |
|-----------|------|------|
| EndScreenPresenter | MonoBehaviour | End-of-run screen (victory/defeat), XP breakdown, stats |

### Screen Transitions

All transitions use `ScreenManager.Show(AppScreen)` — no `SceneManager.LoadScene()` calls exist in the codebase.

| Trigger | Method | Transition |
|---------|--------|------------|
| Player confirms class selection | `GameBootstrap.LaunchRun(request)` | Menu → Game |
| Player starts tutorial | `GameBootstrap.LaunchTutorial(setup)` | Menu → Game |
| Player presses Resume Game | `GameBootstrap.LaunchResume()` | Menu → Game |
| Run ends (victory/defeat) | `GameBootstrap.ShowEndScreen()` | Game → EndScreen |
| Player exits end screen or quits run | `GameBootstrap.ReturnToMenu()` | Game/EndScreen → Menu |

---

## 3) Architecture

### Layered Architecture

```
┌─────────────────────────────────────────────────┐
│  MonoBehaviour Layer (Unity lifecycle)           │
│  GameBootstrap, UI Controllers, Builders         │
├─────────────────────────────────────────────────┤
│  Orchestration Layer                             │
│  RunDirector (central state machine)             │
├─────────────────────────────────────────────────┤
│  Service Layer (plain C# classes, no MonoBehaviour) │
│  41 services across 11 domains                   │
├─────────────────────────────────────────────────┤
│  Data Layer                                      │
│  RuntimeModels, GameEnums, ModifierModels        │
│  HardeningModels, SudokuBoard                    │
├─────────────────────────────────────────────────┤
│  Persistence Layer                               │
│  SaveFileService, ProfileService                 │
│  RunAutoSaveCoordinator, SteamCloudSyncService   │
└─────────────────────────────────────────────────┘
```

### Dependency Rules

1. **Services are plain C# classes** — no `MonoBehaviour`, no `UnityEngine` dependencies where possible (except `JsonUtility`, `Random` seed bridging)
2. **Services do not reference UI** — data flows up via return values, never via direct UI calls
3. **UI controllers reference services** — never the reverse
4. **RunDirector owns service instances** — created in constructor, each with a unique seed offset
5. **No singletons except GameBootstrap** — services are passed by reference, not accessed statically
6. **No prefabs for game UI** — all UI is built procedurally in C# via Blueprint Builders

### Initialisation Flow (Single-Scene)

```
Game Scene
  └→ GameBootstrap.Start()
     ├→ Create RunAutoSaveCoordinator
     ├→ Find ScreenManager
     ├→ Load ProfileService (profile_save.json)
     └→ ScreenManager.ShowMenu()  ← app starts in menu mode

Menu → Run Launch (called directly by MainMenuController)
  ├→ GameBootstrap.LaunchRun(request)
  │   ├→ new RunDirector(seed) → creates all 41 services
  │   ├→ RunDirector.StartRun(classId, mode)
  │   ├→ RunDirector.BuildLevelConfig() + StartLevel()
  │   ├→ BindRuntimeControllers() → wires RunDirector to MonoBehaviours
  │   └→ ScreenManager.ShowGame()
  ├→ GameBootstrap.LaunchTutorial(setup)
  │   ├→ TutorialModeService.ValidateSetup() → fallback if invalid
  │   ├→ new RunDirector(seed) → StartTutorialRun(setup)
  │   ├→ BindRuntimeControllers()
  │   └→ ScreenManager.ShowGame()
  └→ GameBootstrap.LaunchResume()
      ├→ SaveFileService.TryLoadRun() → RunResumeService.TryResumeFromSave()
      ├→ BindRuntimeControllers()
      └→ ScreenManager.ShowGame()

Run → End Screen
  └→ GameBootstrap.ShowEndScreen() → ScreenManager.ShowEndScreen()

Any Screen → Menu
  └→ GameBootstrap.ReturnToMenu() → nulls RunDirector, ScreenManager.ShowMenu()
```

No `LaunchRequestContext` static shuttle is used — menu controllers hold a `[SerializeField]` reference to `GameBootstrap` and call its launch methods directly.

---

## 4) Service Registry

All services are plain C# classes instantiated by `RunDirector` (run-scoped) or `GameBootstrap` (session-scoped). Each run-scoped service receives a seeded `Random` instance for determinism.

### Run-Scoped Services (created per run in RunDirector)

| Service | Domain | Responsibility | Spec Reference |
|---------|--------|---------------|----------------|
| `ItemService` | Economy | Star-based reward slot rolling, item catalog, charge tracking | [ItemsAndRelics](ItemsAndRelicsSystem.md) |
| `RelicService` | Economy | Floor-based relic tier rolling, single-slot swap, passive effect queries | [ItemsAndRelics](ItemsAndRelicsSystem.md) |
| `ShopService` | Economy | Shop inventory generation (3 items + optional relic), price scaling | [GoldEconomy](GoldEconomySystem.md) |
| `XpService` | Economy | Per-tile XP calculation, XP curve, level derivation | [ClassXpProgression](ClassXpProgressionSystem.md) |
| `FormulaService` | Economy | Gold reward formula, pencil buy cost, reroll cost | [GoldEconomy](GoldEconomySystem.md) |
| `BossService` | Boss | Modifier pool rolling, choice generation, ModifierData catalog | [BossMechanics](BossMechanicsSystem.md) |
| `RouteService` | Route | Calm/Risk path generation, cross-link placement | [PathSystem](PathSystem_GardenOverview.md) |
| `RunGraphService` | Run | 5-floor graph topology, node positioning, reachability | [PathSystem](PathSystem_GardenOverview.md) |
| `RunArchetypeService` | Run | Run archetype scoring from relic/item/class usage | [GameDesignSpec](GameDesignSpec.md) |
| `CurseService` | Run | Curse application and removal (future system) | [GameDesignSpec](GameDesignSpec.md) |
| `RunEventService` | Run | Random event rolling and option resolution | [GameDesignSpec](GameDesignSpec.md) |
| `RunVarianceService` | Run | Variance band calculation for difficulty tuning | [GameDesignSpec](GameDesignSpec.md) |
| `MidRunAdaptationService` | Run | Dynamic difficulty mutation during a run | [GameDesignSpec](GameDesignSpec.md) |
| `RunFeelService` | Run | Combo tracking, music layer intensity, feel state | [AudioVisual](AudioVisualDirection.md) |
| `PostRunAnalyticsService` | Run | Per-run mistake analysis, improvement suggestions | [MetaProgression](MetaProgressionSystem.md) |
| `EndlessZenService` | Run | Endless Zen depth progression, star/modifier scaling | [EndlessZen](EndlessZenMode.md) |
| `SpiritTrialsService` | Run | Spirit Trials scoring, timer, tier configuration | [SpiritTrials](SpiritTrialsMode.md) |
| `GameModeService` | Run | Active game mode tracking (GardenRun/Tutorial/Endless/Trials) | [GameDesignSpec](GameDesignSpec.md) |

### Session-Scoped Services (created once in GameBootstrap, survive across runs)

| Service | Domain | Responsibility | Spec Reference |
|---------|--------|---------------|----------------|
| `ProfileService` | Save | Profile load/save, post-run update orchestration (12-step) | [SaveLoad](SaveLoadArchitecture.md) |
| `SaveFileService` | Save | File I/O, validation, sanitisation, backup rotation, atomic writes | [SaveLoad](SaveLoadArchitecture.md) |
| `RunAutoSaveCoordinator` | Save | Autosave event listener, pre-save sync | [SaveLoad](SaveLoadArchitecture.md) |
| `RunResumeService` | Save | Run state restoration from save envelope | [SaveLoad](SaveLoadArchitecture.md) |
| `SaveConflictService` | Save | Local vs cloud conflict resolution | [SaveLoad](SaveLoadArchitecture.md) |
| `SaveMigrationService` | Save | Save version migration (future-proofing) | [SaveLoad](SaveLoadArchitecture.md) |
| `SteamCloudSyncService` | Save | Steam Cloud upload/download via SteamRemoteStorage API | [SaveLoad](SaveLoadArchitecture.md) |
| `ClassGardenProgressionService` | Meta | Per-class XP application, level derivation, prestige checking | [ClassXpProgression](ClassXpProgressionSystem.md) |
| `ClassUnlockService` | Meta | 8 class unlock condition evaluation from cumulative counters | [MetaProgression](MetaProgressionSystem.md) |
| `AscensionService` | Meta | Global ascension level, MaxStarCap, seasonal seed generation | [MetaProgression](MetaProgressionSystem.md) |
| `CompletionService` | Meta | 4-check global completion tracker (25% each) | [MetaProgression](MetaProgressionSystem.md) |
| `MasteryService` | Meta | Modifier mastery tracking, badge tier derivation | [MetaProgression](MetaProgressionSystem.md) |
| `SteamAchievementService` | Meta | 52 achievement definitions and unlock evaluation | [MetaProgression](MetaProgressionSystem.md) |
| `StarDensityService` | Core | Star → missing percentage formula | [SudokuPipeline](SudokuGenerationPipeline.md) |
| `AccessibilityService` | Core | Apply accessibility settings to active UI | [Accessibility](AccessibilitySpec.md) |
| `InputRemapService` | Core | Input action rebinding, conflict detection, JSON persistence | [Accessibility](AccessibilitySpec.md) |
| `TutorialModeService` | Tutorial | Board sizes, stars, modifier availability, descriptions | [TutorialMode](TutorialModeSystem.md) |
| `TutorialProgressService` | Tutorial | Completion tracking by configuration key | [TutorialMode](TutorialModeSystem.md) |

### Sudoku Engine Services (stateless utilities)

| Service | Responsibility | Spec Reference |
|---------|---------------|----------------|
| `SudokuGenerator` | `CreatePuzzle()`, `BuildRegionMap()`, `GenerateSolvedBoard()`, `FillBoard()`, `CreatePuzzleWithUniquenessCheck()` | [SudokuPipeline](SudokuGenerationPipeline.md) |
| `SudokuConstraintEngine` | Central rule validation — `ValidateAll()`, `IsValidPlacement()` | [BossMechanics](BossMechanicsSystem.md) |
| `ModifierGeometryGenerator` | `Generate(board, modifiers, seed, intensity)` — overlay data creation | [BossMechanics](BossMechanicsSystem.md) |
| `ModifierFactory` | Maps `BossModifierId` → `IOrderedConstraintRule` instances | [BossMechanics](BossMechanicsSystem.md) |
| `SudokuValidator` | Board validation (row/column/region uniqueness) | [SudokuPipeline](SudokuGenerationPipeline.md) |
| `SudokuBacktrackingSolver` | MRV-based solver for uniqueness checking | [SudokuPipeline](SudokuGenerationPipeline.md) |
| `SudokuDifficultyGrader` | Technique-based difficulty grading | [SudokuPipeline](SudokuGenerationPipeline.md) |
| `SudokuLogicalAnalyzer` | Logical solve path analysis | [SudokuPipeline](SudokuGenerationPipeline.md) |

---

## 5) Data Models

All data classes live in `Assets/Scripts/Core/RuntimeModels.cs` and must be `[Serializable]` for `JsonUtility` compatibility.

### Core Run State

```
RunState
├── ClassId SelectedClass
├── GameMode Mode
├── int RunNumber
├── int CurrentFloor (0-4)
├── int CurrentDepth
├── int HP, MaxHP
├── int Pencil, MaxPencil
├── int Gold
├── int ItemSlotCount
├── List<ItemInstance> Inventory
├── bool HasRelic
├── RelicInstance HeldRelic
├── bool HasChosenBossModifier
├── BossModifierId ChosenBossModifierId
├── List<BossModifierId> SeenBossModifierList
├── bool AllowIrregularPuzzles
├── int PencilPurchaseCount
├── int RerollCount
└── List<TileXpEntry> TileXpLog
```

### Level Configuration

```
LevelConfig
├── int BoardSize (5-9)
├── int Stars (1-7)
├── float MissingPercent
├── int RegionVariant (0-3)
├── bool IsBoss
├── List<BossModifierId> ActiveModifiers
├── BossModifierIntensity Intensity
└── int Seed
```

### Save Envelope

```
SaveFileEnvelope
├── string SaveVersion ("1.0.0")
├── long TimestampUtc
├── PlayerProfile
├── MetaProgressionState
├── RunState ActiveRun (nullable for profile-only saves)
├── SudokuBoard ActivePuzzle
├── TutorialProgressState
├── ProfileStats Statistics
├── MasteryAchievementState Mastery
├── CompletionState Completion
├── OptionsState Options
└── SessionState Session
```

### Enum Registry (GameEnums.cs)

| Enum | Values | Purpose |
|------|--------|---------|
| `ClassId` | NumberFreak, GardenMonk, ShrineArchivist, KoiGambler, StoneGardener, LanternSeer, ReedDuelist, QuietCartographer | 8 playable classes |
| `GameMode` | GardenRun, EndlessZen, SpiritTrials, Tutorial | 4 game modes |
| `BossModifierId` | FogOfWar, ArrowSums, GermanWhispers, DutchWhispers, ParityLines, RenbanLines, KillerCages, DifferenceKropki, RatioKropki, Palindrome, Thermo, BetweenLines, EvenOdd, Nonconsecutive, Antiknight | 15 boss modifiers |
| `BossModifierIntensity` | Low, Medium, High, VeryHigh | Modifier geometry density scaling |
| `ItemType` | Solver, Finder, InkWell, MeditationStone, WindChime, PatternScroll, KoiReflection, LanternOfClarity + 10 unique | 18 item types |
| `ItemRarity` | Normal, Rare, Epic | Item tier |
| `RelicId` | 20 values across 5 tiers | Relic identifiers |
| `RelicTier` | T1, T2, T3, T4, Legendary | Relic power tiers |
| `NodeType` | Start, Puzzle, ElitePuzzle, Shop, Rest, Relic, Event, PreBoss, Boss, CrossLink | Path map node types |
| `ConstraintRuleCategory` | BaseSudoku(0), Region(1), GlobalNegative(2), Line(3), Dot(4), Arithmetic(5), CellLevel(6), FogPostProcess(7) | Constraint execution order |
| `MenuScreen` | MainMenu, ClassSelect, TutorialSetup, TutorialProgress, GameModes, SpiritTrialsTierSelect, Codex, MetaProgression, Options, Credits | Menu navigation states |
| `SpiritTrialsTier` | Apprentice, Adept, Master, Grandmaster | Spirit Trials difficulty |

---

## 6) UI Construction

### Procedural UI Pattern

All game UI is built procedurally in C# — **no prefabs** for game UI elements. Two Blueprint Builder MonoBehaviours construct the UI hierarchies:

| Builder | Scene | Output |
|---------|-------|--------|
| `MainMenuBlueprintBuilder` | MainMenu | Full menu panel hierarchy under a root Canvas |
| `InRunUiBlueprintBuilder` | Prototype | Board grid, HUD, overlays, reward panels, shop, map |

**Build pattern:**
1. Builder has a `[ContextMenu("Build")]` method for editor-time preview
2. At runtime, `GameBootstrap` or the controller calls the build method
3. Builder creates `GameObject` hierarchy using `new GameObject()`, `AddComponent<T>()`, `RectTransform` anchoring
4. Builder stores references to key elements (board cells, HUD labels, panels) as fields
5. Controller reads these references to update UI state

### Canvas Setup

| Canvas | Scene | Render Mode | Sort Order | Purpose |
|--------|-------|-------------|------------|---------|
| MenuCanvas | MainMenu | ScreenSpaceOverlay | 0 | All menu panels |
| GameCanvas | Prototype | ScreenSpaceOverlay | 0 | Board, HUD, overlays |
| MapCanvas | Prototype | ScreenSpaceOverlay | 10 | Path map (shown over game) |
| OverlayCanvas | Prototype | ScreenSpaceOverlay | 20 | Reward, shop, pause, end screen panels |

### Procedural Sprite Generation

Several UI elements use runtime-generated sprites instead of asset files:

| Element | Method | Size | Notes |
|---------|--------|------|-------|
| Circle overlays (Kropki dots, arrow circles) | `BuildCircleSprite()` | 64×64 | Antialiased procedural circle texture |
| HP/Pencil bar fills | `EnsureBarFillSprite()` | 4×4 white | Fallback if Image has no sprite assigned |
| Menu buttons | `BuildMenuButton()` | N/A | Image alpha=0, ColorBlock: normal=transparent, hover=8% white, press=12% black |

### HUD Layout (In-Run)

```
┌─────────────────────────────────────────────┐
│ [HP Bar] [Pencil Bar] [Gold: 0]  [Items: 0] │  ← Top meta bar
├─────────────────────────────────────────────┤
│                                             │
│            [Sudoku Board Grid]              │  ← Centre, square aspect
│            [Modifier Overlays]              │
│                                             │
│                          [Legend Panel ▾]    │  ← Top-right, collapsible
├─────────────────────────────────────────────┤
│ [1] [2] [3] [4] [5] [6] [7] [8] [9]       │  ← Number input bar
│ [Pencil] [Undo] [Redo] [Hint] [Erase]     │  ← Action bar
└─────────────────────────────────────────────┘
```

### Board Cell Structure

Each cell in the Sudoku grid is a procedural `GameObject`:

```
CellRoot (RectTransform, anchored to grid position)
├── CellBackground (Image — white/given/selected/error colours)
├── RegionBorder (Image — thick borders on region edges)
├── ValueText (Text — placed digit, size scales with board)
├── PencilGrid (GridLayoutGroup — 3×3 mini-text for pencil marks)
├── OverlayLayer (child objects for modifier visuals)
│   ├── CageBorder (Image — killer cage dotted border)
│   ├── CageSumBg + CageSumText (cage sum label)
│   ├── KropkiDot (Image — circle sprite, white/black)
│   ├── LinePath (Image — stretched/rotated for line overlays)
│   ├── ArrowCircle (Image — hollow circle for arrow sums)
│   ├── EvenOddMarker (Image — blue square or orange circle)
│   └── FogOverlay (Image — near-black, hides cell content)
└── SelectionHighlight (Image — yellow border, toggled on selection)
```

### Modifier Overlay Z-Order

Overlays render in strict layer order (back to front):

| Z-Layer | Content | Example |
|---------|---------|---------|
| 0 | Cell background colours | Even/Odd markers, fog |
| 1 | Cage borders | Killer cage dotted outlines |
| 2 | Dots | Kropki dots (white hollow, black filled) |
| 3 | Warm-coloured lines | German Whispers, Thermo, Palindrome |
| 4 | Cool-coloured lines | Renban, Parity, Between Lines |
| 5 | Line endpoint bulbs | Arrow circles, thermo bulbs |

---

## 7) Game Flow State Machine

### RunDirector States

```
                    ┌──────────┐
                    │  IDLE    │
                    └────┬─────┘
                         │ StartRun() / StartTutorialRun()
                         ▼
                    ┌──────────┐
              ┌─────│  MAP     │◄────────────────────┐
              │     └────┬─────┘                     │
              │          │ Player selects node       │
              │          ▼                           │
              │     ┌──────────┐                     │
              │     │ LEVEL    │                     │
              │     │ (puzzle) │                     │
              │     └────┬─────┘                     │
              │          │ Puzzle complete            │
              │          ▼                           │
              │     ┌──────────┐                     │
              │     │ REWARDS  │                     │
              │     │ (items)  │                     │
              │     └────┬─────┘                     │
              │          │ Item selected/skipped     │
              │          ▼                           │
              │     ┌──────────────┐                 │
              │     │ SHOP / REST  │  (if node type) │
              │     └────┬─────────┘                 │
              │          │                           │
              │          ├── Not last floor ──────────┘
              │          │
              │          │ Floor 5 boss defeated
              │          ▼
              │     ┌──────────┐
              │     │ RUN END  │
              │     │ (victory)│
              │     └────┬─────┘
              │          │ ProfileService.PostRunUpdate()
              │          ▼
              │     ┌──────────┐
              │     │ END      │
              │     │ SCREEN   │
              │     └──────────┘
              │
              │ HP reaches 0
              ▼
         ┌──────────┐
         │ RUN END  │
         │ (defeat) │
         └────┬─────┘
              │ ProfileService.PostRunUpdate()
              ▼
         ┌──────────┐
         │ END      │
         │ SCREEN   │
         └──────────┘
```

### Post-Run Update Sequence (ProfileService)

After every run (win or lose), `ProfileService.PostRunUpdate()` executes 12 steps in order:

1. Commit tile XP log to class progression (`ClassGardenProgressionService.AddXp`)
2. Update cumulative unlock counters (`ClassUnlockProgress`)
3. Evaluate class unlock conditions (`ClassUnlockService`)
4. Update codex discovery (items, relics encountered this run)
5. Update modifier mastery (`MasteryService`)
6. Update profile statistics (`ProfileStats`)
7. Evaluate completion checks (`CompletionService`)
8. Evaluate achievements (`SteamAchievementService`)
9. Check prestige eligibility
10. Check ascension eligibility
11. Save profile (`SaveFileService.SaveProfile`)
12. Delete run save file (if exists)

### Autosave Trigger Points

`RunAutoSaveCoordinator` listens for these events and saves `run_save.json`:

| Trigger | When |
|---------|------|
| Puzzle completion | After `CompleteLevelAndGrantRewards()` |
| Item selection | After reward item chosen |
| Shop purchase | After any buy transaction |
| Boss modifier choice | After modifier card selected |
| Floor transition | After moving to next floor |
| Save & Quit | Manual trigger from pause menu |

---

## 8) Game Modes

### Garden Run (Primary Mode)

| Property | Value |
|----------|-------|
| Floors | 5 |
| Board sizes | 5×5 through 9×9 (mixed per floor) |
| Stars | 1–6★ (scaling with floor + class level) |
| Region variants | 0–3 (unless irregular disabled → 0–1) |
| Boss modifiers | 1 per boss (choice panel, scaling with floor) |
| Items | Star-based reward slots after each puzzle |
| Gold | Earned per puzzle, spent at shops |
| XP | Earned per tile, committed at run end |
| Relics | Acquired at relic nodes (single slot) |
| Save/Resume | Yes — autosave at all trigger points |
| Target duration | 45–90 minutes |

### Tutorial Mode

| Property | Value |
|----------|-------|
| Floors | 1 (single puzzle) |
| Board sizes | 5×5 through 9×9 (player choice) |
| Stars | 1–7★ (7★ requires modifier) |
| Modifiers | Any of 15 (player toggle) |
| Items/Gold/XP/Relics | None |
| Save/Resume | No |
| Progression | Achievements only |

### Spirit Trials

| Property | Value |
|----------|-------|
| Floors | 1 (single puzzle) |
| Board size | Always 9×9 |
| Tiers | 4 (Apprentice/Adept/Master/Grandmaster) |
| Timer | Counts up, affects score via speed multiplier |
| Items | 1 starting item (Random Normal or Solver Start) |
| Save/Resume | No |
| Leaderboard | Steam Leaderboard API (5 boards) |
| Unlock | 1 full run complete OR Ascension Level 1 |

### Endless Zen

| Property | Value |
|----------|-------|
| Floors | Infinite |
| Board size | Always 9×9 |
| Stars | `Clamp(1 + floor(depth/4), 1, 5)` |
| Modifiers | Cap: 1 (depth 0–9), 2 (10–19), 3 (20+) |
| HP restore | +1 per level |
| Pencil restore | +3 per level |
| Items/Gold/XP/Relics | None |
| Save/Resume | No |
| Leaderboard | Steam Leaderboard API (1 board: `endless_zen_depth`) |
| Unlock | TotalRuns ≥ 10 |

---

## 9) Sudoku Engine

### Generation Pipeline (4 stages)

```
Input: (boardSize, stars, regionVariant, seed, modifiers?, intensity?)
         │
         ▼
┌─────────────────────────┐
│ Stage 1: Region Map     │  SudokuGenerator.BuildRegionMap(size, variant)
│ variant 0-3 → int[,]   │  Hardcoded templates for irregular variants
└────────┬────────────────┘
         ▼
┌─────────────────────────┐
│ Stage 2: Solved Board   │  SudokuGenerator.GenerateSolvedBoard(size, regions, rng)
│ MRV backtracking fill   │  Typical <5ms for 9×9
└────────┬────────────────┘
         ▼
┌─────────────────────────┐
│ Stage 3: Cell Removal   │  StarDensityService.MissingPercentForStars(stars)
│ Random removal to %     │  Formula: (stars + 3) × 0.1, clamped [0.01, 0.95]
└────────┬────────────────┘
         ▼
┌─────────────────────────┐
│ Stage 4: Modifier Geo   │  ModifierGeometryGenerator.Generate(board, mods, seed, intensity)
│ Lines, dots, cages etc  │  Up to 5 boards × 12 seed retries
└────────┬────────────────┘
         ▼
Output: SudokuBoard + ModifierOverlayData
```

### Constraint Validation

`SudokuConstraintEngine` validates placements in category order (0→7):

1. BaseSudoku (row/column uniqueness)
2. Region (region uniqueness)
3. GlobalNegative (Nonconsecutive, Antiknight — board-wide negative constraints)
4. Line (German/Dutch Whispers, Parity, Renban, Palindrome, Thermo, Between)
5. Dot (Kropki — difference and ratio)
6. Arithmetic (Killer cages — sum constraint, Arrow sums)
7. CellLevel (Even/Odd markers)
8. FogPostProcess (Fog of War — visibility only, does not restrict placement)

### Board Size Restrictions

| Modifier | Minimum Size | Reason |
|----------|:---:|--------|
| German Whispers | 7×7 | Requires digit difference ≥ 5 |
| Killer Cages | 7×7 | Cage sums need sufficient digit variety |
| All others | 5×5 | No minimum restriction |

---

## 10) Input System

### Input Actions

The game uses Unity Input System (`com.unity.inputsystem`) with the following action maps:

**Gameplay Action Map** (active during puzzles):

| Action | Default Keyboard | Default Controller | Function |
|--------|-----------------|-------------------|----------|
| SelectCell | Mouse click / Arrow keys | D-pad / Left stick | Select a cell |
| PlaceDigit | 1–9 keys | Cycle (LB/RB) or Radial (RT hold) | Place a number |
| TogglePencil | P | Y / Triangle | Toggle pencil mode |
| Undo | Ctrl+Z | LT | Undo last action |
| Redo | Ctrl+Y | LT + RB | Redo last undo |
| UseItem | Q / 1–4 (item slots) | X / Square | Use selected item |
| Erase | Delete / Backspace | A / Cross (on filled cell) | Clear cell |
| Pause | Escape | Start | Open pause menu |
| SkipAnimation | Space | A / Cross | Skip XP animation |
| ZoomBoard | Scroll wheel | Right stick | Zoom in/out (if supported) |

**Menu Action Map** (active in menus):

| Action | Default Keyboard | Default Controller | Function |
|--------|-----------------|-------------------|----------|
| Navigate | Arrow keys / Tab | D-pad / Left stick | Move focus |
| Confirm | Enter / Space | A / Cross | Select focused element |
| Cancel | Escape | B / Circle | Go back |
| TabLeft | Q | LB | Previous tab |
| TabRight | E | RB | Next tab |

### Controller Number Entry

Two methods available (player choice in Options):

1. **Cycle Mode:** LB/RB cycles through 1–9, A/Cross confirms
2. **Radial Menu:** Hold RT/R2 to open radial overlay, left stick selects number, release to confirm

### Input Remapping

- All actions rebindable via Options → Input Remapping
- Stored as JSON in `{persistentDataPath}/input_bindings.json`
- Conflict detection: if two actions share the same binding, show swap dialog
- Reset to defaults button per action and globally

---

## 11) Audio System

### Audio Architecture

| Component | Type | Role |
|-----------|------|------|
| `RunAudioController` | MonoBehaviour | In-run music management, crossfade, layering |
| `MenuMusicController` | MonoBehaviour | Menu music playback |
| `ProceduralSfxLibrary` | MonoBehaviour | SFX playback, pooling, priority management |
| `FloorMusicGenerator` | MonoBehaviour | Floor theme selection based on `FloorThemeData` |

### Audio Channels

| Channel | Default Volume | Controls |
|---------|:-:|----------|
| Master | 1.0 | Global multiplier for all audio |
| Music | 0.8 | Background music and ambient drones |
| SFX | 0.7 | Gameplay sound effects |
| UI | 0.6 | Button clicks, panel transitions |

### Music Playback Rules

- Floor themes loop seamlessly (120–240s loops)
- Boss modifier activation adds an intensity layer over the floor theme (additive, not replacement)
- Music ducks 20% during XP breakdown animation
- Crossfade on floor transition: 800ms fade-out, 200ms silence, 800ms fade-in
- Only one music track active at a time (plus optional boss layer)

### SFX Rules

- Max 3 simultaneous SFX (priority-based: gameplay > feedback > ambient)
- All SFX below music layer volume
- Positive reinforcement principle: correct actions get satisfying sounds, wrong actions get soft/muted feedback
- No sudden loud sounds

---

## 12) Visual System

### Colour Palette

| Usage | Colour | Hex |
|-------|--------|-----|
| Board background | Warm cream | `#F5F0E8` |
| Cell selected | Lantern gold | `#FFD700` at 40% alpha |
| Cell given (locked) | Light grey | `#E8E4DC` |
| Cell error | Muted red | `#CC4444` at 30% alpha |
| Grid lines (thin) | Medium grey | `#999999` |
| Grid lines (region) | Dark charcoal | `#333333` |
| Fog cells | Near-black | `(0.06, 0.06, 0.08, 1.00)` |
| Even marker | Soft blue | `(0.35, 0.65, 0.90, 0.55)` |
| Odd marker | Warm orange | `(0.90, 0.55, 0.20, 0.55)` |
| Dutch Whispers line | Bright teal | `(0.10, 0.95, 0.78, 0.75)` |
| Calm path | Soft green | — |
| Risk path | Warm amber | — |

### Pixel Art Specifications

| Asset Type | Size | Notes |
|-----------|:---:|-------|
| Item icons | 16×16 | Rarity border colour (silver/gold/cyan-purple) |
| Relic icons | 16×16 | Tier-based border glow |
| Class portraits | 32×32 | Class-themed accent colour |
| Modifier icons | 16×16 | Used in legend panel and boss choice cards |
| Node type icons | 16×16 | Path map tile centres |
| Floor backgrounds | Tiling | 2–3 parallax layers, floor-themed |

### Particle Systems

| Particles | Floor | Elements |
|-----------|-------|----------|
| Floor 1 — Bamboo Courtyard | Petal drift, wind streaks | Light, airy |
| Floor 2 — Moss Garden | Rain droplets, mist wisps | Damp, contemplative |
| Floor 3 — Koi Terrace | Water ripples, light refractions | Flowing, bright |
| Floor 4 — Stone Lantern Walk | Firefly dots, lantern glow | Warm, dim |
| Floor 5 — Shrine Summit | Light rays, incense smoke | Sacred, intense |

**Limits:** Max 2 non-critical particle layers active simultaneously. `ParticleIntensity` setting (0.0–1.0) scales count. `ReduceMotion` disables all particles.

### Animation Timings

| Animation | Duration | Easing |
|-----------|:--------:|--------|
| Cell selection highlight | 100ms | EaseOut |
| Number placement | 150ms | EaseOutBack |
| Wrong placement shake | 200ms, 2–3px | EaseOut |
| Row/column/region flash (completion) | 300ms | EaseInOut |
| XP bar fill per level | 1500ms | EaseInOut |
| Floor transition | 800ms fade | Linear |
| Panel slide in | 300ms | EaseOutCubic |
| Panel slide out | 200ms | EaseInCubic |
| Reward item reveal | 200ms per slot, staggered | EaseOutBack |
| Boss activation | 500ms pulse | EaseInOut |

---

## 13) Save System

### File Locations

| File | Path | Size | Synced to Cloud |
|------|------|:---:|:---:|
| Profile save | `{persistentDataPath}/profile_save.json` | ~50KB | Yes |
| Run save | `{persistentDataPath}/run_save.json` | ~200KB | Yes |
| Profile backups | `{persistentDataPath}/backups/profile_save_{timestamp}.json` | ~50KB | No |
| Run backups | `{persistentDataPath}/backups/run_save_{timestamp}.json` | ~200KB | No |
| Input bindings | `{persistentDataPath}/input_bindings.json` | ~2KB | No |

### Atomic Write Pattern

```csharp
// 1. Write to temp file
File.WriteAllText(tempPath, json);
// 2. Atomic replace (old target becomes backup)
File.Replace(tempPath, targetPath, backupPath);
// 3. Rotate backups (keep max 5)
RotateBackups(backupDir, maxBackups: 5);
```

### Save Version Migration

- `SaveFileEnvelope.SaveVersion` = `"1.0.0"` (current)
- `SaveMigrationService.MigrateToVersion(envelope)` applies sequential migrations
- Unknown future fields are ignored (forward-compatible)
- Missing fields get default values (backward-compatible)

### Validation Clamps (on load)

| Field | Valid Range | Default |
|-------|------------|---------|
| Audio volumes | 0.0–1.0 | 0.8 |
| Board size | 5–9 | 9 |
| Star difficulty | 1–6 (7 tutorial only) | 1 |
| MaxStarCap | 1–10 | 6 |
| Class level | 1–40 | 1 |
| Prestige tier | 0–9 | 0 |
| Font scale | 0.8–1.5 | 1.0 |

### Steam Cloud Sync

- Provider interface: `ICloudSaveProvider` with `Upload()`, `Download()`, `GetTimestamp()`, `IsAvailable()`
- Implementation: `SteamCloudSyncService` (Steam), `LocalCloudSaveProvider` (fallback)
- Conflict resolution: newer-wins with dialog showing floor/class/level/timestamp when both modified
- Sync on: app launch (download), app quit (upload), after profile save (upload)

---

## 14) Steam Integration

### Steam Features Used

| Feature | API | Purpose |
|---------|-----|---------|
| Achievements | `SteamUserStats.SetAchievement()` | 52 achievements |
| Leaderboards | `SteamUserStats.FindOrCreateLeaderboard()` | 6 boards (4 Spirit Trials tiers + 1 seasonal + 1 Endless Zen) |
| Cloud Save | `SteamRemoteStorage` | Profile + run save sync |
| Input | Steam Input API | Controller support (Xbox, PS, Switch, Steam Controller) |
| Overlay | `SteamFriends.ActivateGameOverlay()` | Shift+Tab overlay |

### Leaderboard Boards

| Board Name | Sort | Score | Tiebreaker |
|------------|------|-------|------------|
| `spirit_apprentice` | Descending | Total score | — |
| `spirit_adept` | Descending | Total score | — |
| `spirit_master` | Descending | Total score | — |
| `spirit_grandmaster` | Descending | Total score | — |
| `spirit_seasonal_{YYYY}_{MM}` | Descending | Total score | — |
| `endless_zen_depth` | Descending | Depth reached | TotalMistakes (ascending) |

### Anti-Cheat (Spirit Trials & Endless Zen)

- Timestamped move log: every placement recorded with `(row, col, value, timestamp)`
- SHA-256 hash of move log embedded in score submission
- Server-side replay verification (future): replay solver validates move sequence
- Suspicious pattern flagging: impossibly fast placements (<100ms between moves)

---

## 15) Accessibility Implementation

### Settings Storage

All accessibility settings are fields on `AccessibilitySettings` (serialized in `SaveFileEnvelope.Options`):

| Setting | Type | Default | Effect |
|---------|------|---------|--------|
| ColorblindMode | bool | false | Pattern overlays on lines, text labels on markers |
| HighContrastMode | bool | false | WCAG AA colours, 2× line width, pure black/white |
| FontScale | float | 1.0 | 0.8–1.5 multiplier on all text, 8pt minimum floor |
| ReduceMotion | bool | false | Disables all particles, animations, screen shake |
| AlternativeSymbols | bool | false | Text labels on constraints (E/O, 1/×, Σ, ?) |
| ScreenReaderEnabled | bool | false | Enables `AccessibilityAnnouncer` |
| AnnouncementVerbosity | enum | Medium | Low/Medium/High detail level |
| ConfirmBeforeWrongPlacement | bool | false | Extra confirmation on wrong digit |
| DoubleTapConfirmNumberEntry | bool | false | Require double-tap to place digit |

### AccessibilityService Application

`AccessibilityService.ApplyAccessibilitySettings()` runs on:
- Settings change (immediate, no restart)
- Scene load
- UI rebuild (after blueprint builder runs)

---

## 16) Testing

### Assembly Definitions

| Assembly | File | References | Scope |
|----------|------|------------|-------|
| Game | `Assets/Scripts/Game.asmdef` | Unity.InputSystem | All game code |
| Game.Editor | `Assets/Scripts/Editor/Game.Editor.asmdef` | Game | Editor-only tools |
| GameTests.EditMode | `Assets/Scripts/Tests/EditMode/GameTests.EditMode.asmdef` | Game | Editor unit tests |
| GameTests.PlayMode | `Assets/Scripts/Tests/PlayMode/GameTests.PlayMode.asmdef` | Game, UnityEngine.TestRunner, UnityEditor.TestRunner | Integration tests |

### Test Structure

```
Assets/Scripts/Tests/
├── EditMode/
│   └── GameTests.EditMode.asmdef
└── PlayMode/
    ├── GameTests.PlayMode.asmdef
    ├── TestDriver.cs                  — Base class with setup/teardown, helper assertions
    ├── ScenarioRunner.cs              — E2E step sequencer with timeout
    ├── RunFlowIntegrationTests.cs     — Run lifecycle, class stats, all game modes
    ├── SaveLoadIntegrationTests.cs    — Save round-trip, validation clamps, backup rotation
    └── EconomyIntegrationTests.cs     — XP curve, gold formula, star density
```

### Test Categories

| Category | Focus | Framework |
|----------|-------|-----------|
| Run flow | Run start, level config, board generation, class variations | NUnit (PlayMode) |
| Game modes | Garden Run, Tutorial, Endless Zen, Spirit Trials mode specifics | NUnit (PlayMode) |
| Save/Load | Envelope round-trip, RunState serialization, atomic writes, backup rotation | NUnit (PlayMode) |
| XP calculations | Curve totals (16,860), level derivation, star/boss/mod multipliers | NUnit (PlayMode) |
| Gold economy | Base gold by board size, pencil buy escalation, reroll escalation | NUnit (PlayMode) |
| Star density | Formula correctness for all 7 star levels | NUnit (PlayMode) |
| Sudoku generation | All board sizes, all region variants, determinism | NUnit (PlayMode) |
| Constraint rules | All 15 modifier rules validate correctly | NUnit (EditMode) |
| Uniqueness checker | `CreatePuzzleWithUniquenessCheck` produces unique solutions | NUnit (EditMode) |

### Critical Invariants to Test

1. `SudokuGenerator.GenerateSolvedBoard()` always produces a valid complete board
2. `BuildRegionMap()` for all variants produces regions of exactly `size` cells each
3. `XpService.DeriveLevel(16860)` = 40
4. `StarDensityService.MissingPercentForStars(7)` = 1.0 (100%)
5. `FormulaService.PencilBuyCost(n)` = `20 + 20n`
6. `SaveFileService` round-trip: save → load → save produces identical JSON
7. All 15 `IOrderedConstraintRule` implementations reject known-invalid placements
8. `ModifierGeometryGenerator.Generate()` with `HasAllModifiersPresent()` = true for all modifier IDs

---

## 17) Performance Budgets

| Metric | Target | Notes |
|--------|--------|-------|
| Puzzle generation (9×9) | < 50ms worst case | Typical < 5ms |
| Modifier geometry generation | < 200ms | Including retry loop |
| Frame rate | 60 FPS stable | 2D game, minimal GPU load |
| Memory footprint | < 500MB | Including all loaded sprites and audio |
| Save file write | < 100ms | Atomic write pattern |
| Scene transition | < 2s | MainMenu → Prototype |
| UI rebuild (blueprint builders) | < 500ms | Full procedural hierarchy creation |
| Particle systems | Max 2 concurrent | Configurable via ParticleIntensity |
| Audio streams | 1 music + 1 boss layer + 3 SFX | Hard limit |

---

## 18) Folder Structure

```
Assets/
├── Scenes/
│   └── Game.unity
├── Scripts/
│   ├── Bootstrap/          GameBootstrap.cs
│   ├── Boss/               BossService.cs
│   ├── Classes/            ClassCatalog, ClassSelectService, ClassUnlockService
│   ├── Core/               GameEnums, RuntimeModels, HardeningModels,
│   │                       LaunchRequest, StarDensityService,
│   │                       AccessibilityService, InputRemapService
│   ├── Data/               DefinitionAssets, FloorThemeData
│   ├── Economy/            XpService, FormulaService, RelicService,
│   │                       ShopService
│   ├── Editor/             PixelIconSetGenerator, Game.Editor.asmdef
│   ├── Items/              ItemService
│   ├── Meta/               ClassGardenProgressionService, AscensionService,
│   │                       CompletionService, MasteryService,
│   │                       SteamAchievementService
│   ├── Route/              RouteService
│   ├── Run/                RunDirector, RunGraphService, EndlessZenService,
│   │                       SpiritTrialsService, GameModeService,
│   │                       RunArchetypeService, CurseService,
│   │                       RunEventService, RunVarianceService,
│   │                       MidRunAdaptationService, RunFeelService,
│   │                       PostRunAnalyticsService
│   ├── Save/               SaveFileService, ProfileService,
│   │                       RunAutoSaveCoordinator, RunResumeService,
│   │                       SaveConflictService, SaveMigrationService,
│   │                       SteamCloudSyncService, ICloudSaveProvider,
│   │                       LocalCloudSaveProvider
│   ├── Sudoku/             SudokuGenerator, SudokuBoard,
│   │                       SudokuConstraintEngine, ConstraintRules,
│   │                       ConstraintRuleRegistry, ModifierFactory,
│   │                       ModifierGeometryGenerator, ModifierModels,
│   │                       SudokuBacktrackingSolver, SudokuValidator,
│   │                       SudokuDifficultyGrader, SudokuLogicalAnalyzer,
│   │                       SudokuGenerationService
│   ├── Tutorial/           TutorialModeService, TutorialProgressService
│   ├── UI/                 (38 controllers — see Section 2)
│   ├── Tests/
│   │   ├── EditMode/       GameTests.EditMode.asmdef
│   │   └── PlayMode/       GameTests.PlayMode.asmdef, TestDriver,
│   │                       ScenarioRunner, RunFlowIntegrationTests,
│   │                       SaveLoadIntegrationTests, EconomyIntegrationTests
│   └── Game.asmdef         Root assembly definition
├── Resources/
│   ├── Icons/              16×16 pixel art icons
│   ├── GeneratedIcons/     Procedurally generated icons
│   └── BillingMode.json    (Unity default)
└── Packages/
    └── manifest.json       Package dependencies
```

---

## 19) Conventions

### Coding Standards

| Rule | Detail |
|------|--------|
| Naming | PascalCase for types, methods, properties; camelCase for locals; _camelCase for private fields |
| Services | Plain C# classes, constructor injection, no MonoBehaviour |
| MonoBehaviours | Only for scene-bound components (UI controllers, bootstrap) |
| Serialization | `[Serializable]` on all data classes; no Newtonsoft; `JsonUtility` only |
| Enums | All in `GameEnums.cs`; explicit integer values for serialization stability |
| Constants | Static readonly or const in the owning service class |
| Null handling | No nullable reference types; use bool + value pattern for optional fields |
| Random | All gameplay randomness via seeded `System.Random`; never `UnityEngine.Random` |
| Coroutines | Only in MonoBehaviours for animations; services use synchronous methods |
| Async | Not used — all operations are synchronous (file I/O is fast enough) |

### File Organisation

| Rule | Detail |
|------|--------|
| One class per file | Exception: small related types (e.g., `ItemInstance` + `RelicInstance` in `RuntimeModels.cs`) |
| Folder = domain | Services grouped by domain, not by pattern |
| No `Utils` or `Helpers` folders | Utility methods belong in the service that uses them |
| No `Interfaces` folder | Interfaces live alongside implementations (e.g., `ICloudSaveProvider` in `Save/`) |

### Commit & Branching

| Rule | Detail |
|------|--------|
| Main branch | `main` — always deployable |
| Feature branches | `feature/{ticket-id}-{short-description}` |
| Commit messages | Conventional: `feat:`, `fix:`, `refactor:`, `test:`, `docs:` |
| No force push to main | — |

---

## 20) Resolved Issues

### OPEN-A — Package Cleanup (Resolved)
Removed 6 unused packages from `Packages/manifest.json`: `com.unity.ads`, `com.unity.purchasing`, `com.unity.ai.navigation`, `com.unity.multiplayer.center`, `com.unity.timeline`, `com.unity.xr.legacyinputhelpers`. Also removed 18 unused Unity modules (3D physics, terrain, cloth, vehicles, VR, XR, etc.).

### OPEN-B — Dead Code Removal (Resolved)
Deleted 7 stub/dead files: `Generated/` folder (3 files), `HeatScoreService.cs`, `HeatCurveGraphController.cs`, `RelicCatalogService.cs`, `RelicSynergyService.cs` — all empty namespaces or deprecated stubs.

### OPEN-C — PlayMode Tests (Resolved)
Added assembly definitions (`Game.asmdef`, `Game.Editor.asmdef`) and 3 integration test files:
- `RunFlowIntegrationTests.cs` — 11 tests covering run lifecycle, all 8 classes, all 4 game modes, board generation, determinism
- `SaveLoadIntegrationTests.cs` — 5 tests covering profile round-trip, RunState serialization, validation clamps, atomic writes, backup rotation
- `EconomyIntegrationTests.cs` — 8 tests covering XP curve (16,860 total), star/boss/mod multipliers, gold formulas, pencil/reroll escalation, star density formula


---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Linked Systems |
|--------|-------|----------------|
| REQ-GENERAL-002 | 1. "UnityVersionChecker | GameBootstrap" |
| REQ-GENERAL-003 | 2. "RenderPipelineConfiguration | ProjectConfigurationService" |
| REQ-GENERAL-004 | 3. "RequiredPackagesInstallationChecker | GameBootstrap" |
| REQ-GENERAL-005 | 4. "MenuTransitionFlowValidator | ScreenManager" |
| REQ-GENERAL-006 | 5. "MainMenuButtonPressedValidator | MainMenuController" |
| REQ-GENERAL-007 | 6. "GameModeSelectionFlowControl | PrototypeRunScreenController" |
| REQ-GENERAL-008 | 7. "ClassSelectButtonPressedValidator | ClassSelectController" |
| REQ-GENERAL-009 | 8. "OptionsMenuConfigurationControl | OptionsMenuController" |
| REQ-GENERAL-010 | 9. "KeyboardAndMouseInputMappingValidator | PrototypeInputController" |
| REQ-GENERAL-011 | 10. "GameStatePersistenceChecker | SaveLoadService" |
| REQ-GENERAL-012 | 11. "EndScreenFlowControl | EndScreenPresenter". |
